using System.Net;
using System.Net.Http.Json;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

/// <summary>Erőforrás-szintű jogosultságok: idegen munkaterület olvasása, idegen meghívók kezelése, tulajdonos védelme.</summary>
public class ResourceAuthorizationTests : ApiTestBase
{
    public ResourceAuthorizationTests(ApiFactory factory) : base(factory) { }

    // ---- Munkaterület olvasása ----

    [Theory]
    [InlineData("")]
    [InlineData("/members")]
    [InlineData("/storage")]
    [InlineData("/projects")]
    public async Task WorkspaceRead_AsNonMember_Returns403(string suffix)
    {
        var victim = await CreateOwnerWithWorkspaceAsync();
        var attacker = await CreateOwnerWithWorkspaceAsync();
        var url = $"/api/workspaces/{victim.WorkspaceId}{suffix}";

        await AssertError(await WithToken(attacker.Owner.Token).GetAsync(url), HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await WithToken(attacker.Token).GetAsync(url), HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Theory]
    [InlineData("")]
    [InlineData("/members")]
    [InlineData("/storage")]
    [InlineData("/projects")]
    public async Task WorkspaceRead_AsMemberOrSystemAdmin_Returns200(string suffix)
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var viewer = await RegisterUserAsync("view");
        var viewerToken = await AddMemberAsync(ws, viewer, TestData.RoleViewer);
        var url = $"/api/workspaces/{ws.WorkspaceId}{suffix}";

        Assert.Equal(HttpStatusCode.OK, (await WithToken(ws.Token).GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await WithToken(viewerToken).GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await WithToken(viewer.Token).GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await (await AdminAsync()).GetAsync(url)).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/members")]
    [InlineData("/storage")]
    [InlineData("/projects")]
    public async Task WorkspaceRead_UnknownWorkspace_Returns404(string suffix)
    {
        var user = await RegisterUserAsync("unk");

        await AssertError(await WithToken(user.Token).GetAsync($"/api/workspaces/{TestData.Missing}{suffix}"),
            HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    // ---- Idegen meghívók ----

    private static object InvitationBody(int type, Guid targetId) =>
        new { type, targetId, invitedEmail = $"{Guid.NewGuid():N}@test.example.com", roleId = TestData.RoleMember };

    private async Task<Guid> InviteAsync(HttpClient client, int type, Guid targetId)
    {
        var res = await client.PostAsJsonAsync("/api/invitations", InvitationBody(type, targetId));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await ReadJson(res)).GetProperty("id").GetGuid();
    }

    private async Task<(OwnedWorkspace victim, Guid targetId)> VictimTargetAsync(int type)
    {
        var victim = await CreateOwnerWithWorkspaceAsync();
        return (victim, type == 1 ? victim.WorkspaceId : await CreateProjectAsync(victim));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Invitations_OfForeignTarget_ListReturns403(int type)
    {
        var (victim, targetId) = await VictimTargetAsync(type);
        await InviteAsync(WithToken(victim.Token), type, targetId);
        var attacker = await CreateOwnerWithWorkspaceAsync();

        await AssertError(await WithToken(attacker.Token).GetAsync($"/api/invitations/target/{targetId}?type={type}"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Invitations_ForForeignTarget_CreateReturns403(int type)
    {
        var (_, targetId) = await VictimTargetAsync(type);
        var attacker = await CreateOwnerWithWorkspaceAsync();

        await AssertError(await WithToken(attacker.Token).PostAsJsonAsync("/api/invitations", InvitationBody(type, targetId)),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Invitations_OfForeignTarget_CancelReturns403_AndKeepsInvitation(int type)
    {
        var (victim, targetId) = await VictimTargetAsync(type);
        var id = await InviteAsync(WithToken(victim.Token), type, targetId);
        var attacker = await CreateOwnerWithWorkspaceAsync();

        await AssertError(await WithToken(attacker.Token).DeleteAsync($"/api/invitations/{id}"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");

        var pending = await ReadJson(await WithToken(victim.Token).GetAsync($"/api/invitations/target/{targetId}?type={type}"));
        Assert.Contains(pending.EnumerateArray(), i => i.GetProperty("id").GetGuid() == id);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Invitations_SpoofedContextHeaders_Return403(int type)
    {
        var (_, targetId) = await VictimTargetAsync(type);
        var attacker = await CreateOwnerWithWorkspaceAsync();
        var attackerProject = await CreateProjectAsync(attacker);
        var client = WithToken(attacker.Token);
        client.DefaultRequestHeaders.Add("X-Workspace-Id", attacker.WorkspaceId.ToString());
        client.DefaultRequestHeaders.Add("X-Project-Id", attackerProject.ToString());

        await AssertError(await client.PostAsJsonAsync("/api/invitations", InvitationBody(type, targetId)),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await client.GetAsync($"/api/invitations/target/{targetId}?type={type}"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Invitations_ForUnknownTarget_AsManager_Return403(int type)
    {
        var ws = await CreateOwnerWithWorkspaceAsync();

        await AssertError(await WithToken(ws.Token).PostAsJsonAsync("/api/invitations", InvitationBody(type, TestData.Missing)),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Invitations_AsSystemAdmin_CanManageAnyTarget(int type)
    {
        var (_, targetId) = await VictimTargetAsync(type);
        var admin = await AdminAsync();

        var id = await InviteAsync(admin, type, targetId);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/invitations/target/{targetId}?type={type}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/invitations/{id}")).StatusCode);
    }

    [Fact]
    public async Task Invitations_AsWorkspaceOwner_CanManageProjectInvitations()
    {
        var (ws, projectId) = await VictimTargetAsync(2);
        var owner = WithToken(ws.Token);

        var id = await InviteAsync(owner, 2, projectId);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/invitations/target/{projectId}?type=2")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/invitations/{id}")).StatusCode);
    }

    [Fact]
    public async Task Invitations_AsProjectLevelManager_CanManageOnlyThatProject()
    {
        var (ws, projectId) = await VictimTargetAsync(2);
        var otherProjectId = await CreateProjectAsync(ws);
        var user = await RegisterUserAsync("pm");
        await AddMemberAsync(ws, user, TestData.RoleViewer);

        var inv = await ReadJson(await WithToken(ws.Token).PostAsJsonAsync("/api/invitations",
            new { type = 2, targetId = projectId, invitedEmail = user.Email, roleId = TestData.RoleAdmin }));
        var accept = await WithToken(user.Token).PostAsync($"/api/invitations/accept/{inv.GetProperty("token").GetString()}", null);
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        var client = WithToken(await WorkspaceTokenAsync(user.Email, user.Password, ws.WorkspaceId, cache: false));

        var id = await InviteAsync(client, 2, projectId);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/invitations/{id}")).StatusCode);

        await AssertError(await client.PostAsJsonAsync("/api/invitations", InvitationBody(2, otherProjectId)),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await client.PostAsJsonAsync("/api/invitations", InvitationBody(1, ws.WorkspaceId)),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    // ---- A tulajdonos védelme ----

    private async Task AssertOwnerStillOwnerRoleAsync(OwnedWorkspace ws)
    {
        var members = await ReadJson(await WithToken(ws.Token).GetAsync($"/api/workspaces/{ws.WorkspaceId}/members"));
        var owner = Assert.Single(members.EnumerateArray(), m => m.GetProperty("memberId").GetGuid() == ws.Owner.Id);
        Assert.Equal(TestData.RoleOwner, owner.GetProperty("role").GetGuid());
    }

    [Fact]
    public async Task Owner_CannotBeDemoted()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var admin = await RegisterUserAsync("adm");
        var adminToken = await AddMemberAsync(ws, admin, TestData.RoleAdmin);
        var url = $"/api/workspaces/{ws.WorkspaceId}/members/{ws.Owner.Id}";
        var body = new { roleId = TestData.RoleViewer };

        foreach (var client in new[] { WithToken(adminToken), WithToken(ws.Token), await AdminAsync() })
            await AssertError(await client.PutAsJsonAsync(url, body), HttpStatusCode.Conflict, "UTKOZES");

        await AssertOwnerStillOwnerRoleAsync(ws);
    }

    [Fact]
    public async Task Owner_CannotBeRemoved()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var admin = await RegisterUserAsync("adm");
        var adminToken = await AddMemberAsync(ws, admin, TestData.RoleAdmin);
        var url = $"/api/workspaces/{ws.WorkspaceId}/members/{ws.Owner.Id}";

        foreach (var client in new[] { WithToken(adminToken), WithToken(ws.Token), await AdminAsync() })
            await AssertError(await client.DeleteAsync(url), HttpStatusCode.Conflict, "UTKOZES");

        await AssertOwnerStillOwnerRoleAsync(ws);
    }

    [Fact]
    public async Task OwnershipTransfer_NewOwnerCanGetOwnerRole_AndPreviousOwnerCanBeDemoted()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var heir = await RegisterUserAsync("heir");
        await AddMemberAsync(ws, heir, TestData.RoleAdmin);
        var transfer = await WithToken(ws.Token).PutAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}/owner", new { newOwnerId = heir.Id });
        Assert.Equal(HttpStatusCode.OK, transfer.StatusCode);
        var admin = await AdminAsync();

        var promote = await admin.PutAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}/members/{heir.Id}", new { roleId = TestData.RoleOwner });
        Assert.Equal(HttpStatusCode.OK, promote.StatusCode);

        var demote = await admin.PutAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}/members/{ws.Owner.Id}", new { roleId = TestData.RoleMember });
        Assert.Equal(HttpStatusCode.OK, demote.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await admin.DeleteAsync($"/api/workspaces/{ws.WorkspaceId}/members/{ws.Owner.Id}")).StatusCode);
    }
}

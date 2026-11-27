using System.Net;
using System.Net.Http.Json;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

public class InvitationTests : ApiTestBase
{
    public InvitationTests(ApiFactory factory) : base(factory) { }

    private async Task<(OwnedWorkspace ws, string token, Guid id)> CreateInvitationAsync(string email, Guid? roleId = null)
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/invitations",
            new { type = 1, targetId = ws.WorkspaceId, invitedEmail = email, roleId = roleId ?? TestData.RoleMember });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var json = await ReadJson(res);
        return (ws, json.GetProperty("token").GetString()!, json.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Create_WorkspaceInvitation_Returns201WithPendingStatus()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/invitations",
            new { type = 1, targetId = ws.WorkspaceId, invitedEmail = "Meghivott@Example.com", roleId = TestData.RoleMember });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var json = await ReadJson(res);
        Assert.Equal(1, json.GetProperty("status").GetInt32());
        Assert.Equal("meghivott@example.com", json.GetProperty("invitedEmail").GetString());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Create_ProjectInvitation_Returns201()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);

        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/invitations",
            new { type = 2, targetId = projectId, invitedEmail = "projekt.tag@example.com", roleId = TestData.RoleMember });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidType_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/invitations",
            new { type = 7, targetId = ws.WorkspaceId, invitedEmail = "a@example.com" });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Create_InvalidEmail_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/invitations",
            new { type = 1, targetId = ws.WorkspaceId, invitedEmail = "nem-email" });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Create_EmptyTarget_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/invitations",
            new { type = 1, targetId = Guid.Empty, invitedEmail = "a@example.com" });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Create_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1)).PostAsJsonAsync("/api/invitations",
            new { type = 1, targetId = TestData.Ws1, invitedEmail = "a@example.com", roleId = TestData.RoleViewer });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Create_Anonymous_Returns401()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/invitations",
            new { type = 1, targetId = TestData.Ws1, invitedEmail = "a@example.com" });

        await AssertError(res, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task PendingByTarget_ListsCreatedInvitation_AndValidatesType()
    {
        var (ws, _, id) = await CreateInvitationAsync("pending@example.com");
        var owner = WithToken(ws.Token);

        var ok = await owner.GetAsync($"/api/invitations/target/{ws.WorkspaceId}?type=1");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Contains((await ReadJson(ok)).EnumerateArray(), i => i.GetProperty("id").GetGuid() == id);

        await AssertError(await owner.GetAsync($"/api/invitations/target/{ws.WorkspaceId}?type=9"),
            HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
        await AssertError(await owner.GetAsync($"/api/invitations/target/{Guid.Empty}?type=1"),
            HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task PendingByTarget_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1))
            .GetAsync($"/api/invitations/target/{TestData.Ws1}?type=1");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task GetByToken_IsPublic_Returns200()
    {
        var (ws, token, _) = await CreateInvitationAsync("public@example.com");

        var res = await Anonymous().GetAsync($"/api/invitations/token/{token}");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(ws.WorkspaceId, (await ReadJson(res)).GetProperty("targetId").GetGuid());
    }

    [Fact]
    public async Task GetByToken_Unknown_Returns404()
    {
        await AssertError(await Anonymous().GetAsync("/api/invitations/token/nem-letezik"),
            HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task My_ReturnsInvitationsForCurrentUser_Anonymous401()
    {
        var invited = await RegisterUserAsync("inv");
        var ws = await CreateOwnerWithWorkspaceAsync();
        await WithToken(ws.Token).PostAsJsonAsync("/api/invitations",
            new { type = 1, targetId = ws.WorkspaceId, invitedEmail = invited.Email, roleId = TestData.RoleMember });

        var res = await WithToken(invited.Token).GetAsync("/api/invitations/my");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Single((await ReadJson(res)).EnumerateArray());

        await AssertError(await Anonymous().GetAsync("/api/invitations/my"), HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task Accept_Valid_Returns200_AddsMembership_AndSecondAcceptReturns400()
    {
        var invited = await RegisterUserAsync("acc");
        var (ws, token, _) = await CreateInvitationAsync(invited.Email);
        var client = WithToken(invited.Token);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/invitations/accept/{token}", null)).StatusCode);

        var members = await ReadJson(await WithToken(ws.Token).GetAsync($"/api/workspaces/{ws.WorkspaceId}/members"));
        Assert.Contains(members.EnumerateArray(), m => m.GetProperty("memberId").GetGuid() == invited.Id);

        await AssertError(await client.PostAsync($"/api/invitations/accept/{token}", null),
            HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Accept_UnknownToken_Returns400()
    {
        var user = await RegisterUserAsync("acc");

        await AssertError(await WithToken(user.Token).PostAsync("/api/invitations/accept/nem-letezik", null),
            HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Accept_Anonymous_Returns401()
    {
        await AssertError(await Anonymous().PostAsync("/api/invitations/accept/valami", null),
            HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task Refuse_Valid_Returns200_AndSecondRefuseReturns400()
    {
        var invited = await RegisterUserAsync("ref");
        var (_, token, _) = await CreateInvitationAsync(invited.Email);
        var client = WithToken(invited.Token);

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/invitations/refuse/{token}", null)).StatusCode);
        await AssertError(await client.PostAsync($"/api/invitations/refuse/{token}", null),
            HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
        // elutasított meghívó nem fogadható el
        await AssertError(await client.PostAsync($"/api/invitations/accept/{token}", null),
            HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Cancel_Existing_Returns204_ThenReturns404()
    {
        var (ws, _, id) = await CreateInvitationAsync("cancel@example.com");
        var owner = WithToken(ws.Token);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/invitations/{id}")).StatusCode);
        await AssertError(await owner.DeleteAsync($"/api/invitations/{id}"), HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Cancel_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1))
            .DeleteAsync($"/api/invitations/{TestData.Missing}");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }
}

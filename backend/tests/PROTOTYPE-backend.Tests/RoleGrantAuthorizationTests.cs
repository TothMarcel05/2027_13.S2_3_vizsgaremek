using System.Net;
using System.Net.Http.Json;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

/// <summary>Szerepkör-kiosztás és tulajdonosváltás korlátai.</summary>
public class RoleGrantAuthorizationTests : ApiTestBase
{
    public RoleGrantAuthorizationTests(ApiFactory factory) : base(factory) { }

    private async Task<(OwnedWorkspace ws, TestUser admin, string adminToken)> WorkspaceWithAdminAsync()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var admin = await RegisterUserAsync("adm");
        return (ws, admin, await AddMemberAsync(ws, admin, TestData.RoleAdmin));
    }

    private static async Task AssertForbidden(HttpResponseMessage res) =>
        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Admin_CannotInviteWithOwnerRole(int type)
    {
        var (ws, _, adminToken) = await WorkspaceWithAdminAsync();
        var targetId = type == 1 ? ws.WorkspaceId : await CreateProjectAsync(ws);

        await AssertForbidden(await WithToken(adminToken).PostAsJsonAsync("/api/invitations",
            new { type, targetId, invitedEmail = $"{Guid.NewGuid():N}@test.example.com", roleId = TestData.RoleOwner }));
    }

    [Fact]
    public async Task Admin_CannotPromoteSelfOrOthersToOwnerRole()
    {
        var (ws, admin, adminToken) = await WorkspaceWithAdminAsync();
        var other = await RegisterUserAsync("oth");
        await AddMemberAsync(ws, other, TestData.RoleViewer);

        foreach (var userId in new[] { admin.Id, other.Id })
            await AssertForbidden(await WithToken(adminToken).PutAsJsonAsync(
                $"/api/workspaces/{ws.WorkspaceId}/members/{userId}", new { roleId = TestData.RoleOwner }));
    }

    private async Task<Guid> MemberRoleAsync(OwnedWorkspace ws, Guid userId)
    {
        var members = await ReadJson(await (await AdminAsync()).GetAsync($"/api/workspaces/{ws.WorkspaceId}/members"));
        return Assert.Single(members.EnumerateArray(), m => m.GetProperty("memberId").GetGuid() == userId)
            .GetProperty("role").GetGuid();
    }

    [Fact]
    public async Task Admin_CannotDemoteOrRemoveNonOwnerHoldingOwnerRole()
    {
        var (ws, _, adminToken) = await WorkspaceWithAdminAsync();
        var coOwner = await RegisterUserAsync("co");
        await AddMemberAsync(ws, coOwner, TestData.RoleOwner);
        var url = $"/api/workspaces/{ws.WorkspaceId}/members/{coOwner.Id}";

        await AssertForbidden(await WithToken(adminToken).PutAsJsonAsync(url, new { roleId = TestData.RoleViewer }));
        await AssertForbidden(await WithToken(adminToken).DeleteAsync(url));

        Assert.Equal(TestData.RoleOwner, await MemberRoleAsync(ws, coOwner.Id));
    }

    [Fact]
    public async Task Admin_CanStillGrantAndManageRolesWithinOwnPermissions()
    {
        var (ws, admin, adminToken) = await WorkspaceWithAdminAsync();
        var other = await RegisterUserAsync("oth");
        await AddMemberAsync(ws, other, TestData.RoleViewer);
        var client = WithToken(adminToken);
        var url = $"/api/workspaces/{ws.WorkspaceId}/members/{other.Id}";

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(url, new { roleId = TestData.RoleAdmin })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(url, new { roleId = TestData.RoleMember })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/invitations",
            new { type = 1, targetId = ws.WorkspaceId, invitedEmail = "uj.admin@test.example.com", roleId = TestData.RoleAdmin })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync($"/api/workspaces/{ws.WorkspaceId}/members/{admin.Id}")).StatusCode);
    }

    [Fact]
    public async Task ProjectLevelManager_CannotInviteWithRoleExceedingOwnPermissions()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);
        var manager = await RegisterUserAsync("pm");
        await AddMemberAsync(ws, manager, TestData.RoleViewer);
        var projectManagerRole = Guid.Parse("04000000-0000-4000-8000-000000000005");
        var contributorRole = Guid.Parse("04000000-0000-4000-8000-000000000006");
        var inv = await ReadJson(await WithToken(ws.Token).PostAsJsonAsync("/api/invitations",
            new { type = 2, targetId = projectId, invitedEmail = manager.Email, roleId = projectManagerRole }));
        Assert.Equal(HttpStatusCode.OK, (await WithToken(manager.Token)
            .PostAsync($"/api/invitations/accept/{inv.GetProperty("token").GetString()}", null)).StatusCode);
        var client = WithToken(await WorkspaceTokenAsync(manager.Email, manager.Password, ws.WorkspaceId, cache: false));
        object Body(Guid roleId) => new { type = 2, targetId = projectId, invitedEmail = $"{Guid.NewGuid():N}@test.example.com", roleId };

        await AssertForbidden(await client.PostAsJsonAsync("/api/invitations", Body(TestData.RoleOwner)));
        await AssertForbidden(await client.PostAsJsonAsync("/api/invitations", Body(TestData.RoleAdmin)));
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/invitations", Body(contributorRole))).StatusCode);
    }

    [Fact]
    public async Task UnknownRole_IsRejectedWith400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var member = await RegisterUserAsync("mem");
        await AddMemberAsync(ws, member, TestData.RoleViewer);
        var client = WithToken(ws.Token);

        await AssertError(await client.PostAsJsonAsync("/api/invitations",
            new { type = 1, targetId = ws.WorkspaceId, invitedEmail = "x@test.example.com", roleId = TestData.Missing }),
            HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
        await AssertError(await client.PutAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}/members/{member.Id}",
            new { roleId = TestData.Missing }), HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task OwnerAndSystemAdmin_CanGrantOwnerRole()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var member = await RegisterUserAsync("mem");
        await AddMemberAsync(ws, member, TestData.RoleViewer);
        var url = $"/api/workspaces/{ws.WorkspaceId}/members/{member.Id}";

        foreach (var client in new[] { WithToken(ws.Token), await AdminAsync() })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(url, new { roleId = TestData.RoleOwner })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(url, new { roleId = TestData.RoleViewer })).StatusCode);
        }
    }

    [Fact]
    public async Task OwnershipTransfer_SwapsOwnerAndAdminRoles()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var heir = await RegisterUserAsync("heir");
        await AddMemberAsync(ws, heir, TestData.RoleViewer);

        var res = await ReadJson(await WithToken(ws.Token).PutAsJsonAsync(
            $"/api/workspaces/{ws.WorkspaceId}/owner", new { newOwnerId = heir.Id }));

        Assert.Equal(TestData.RoleOwner, res.GetProperty("roleId").GetGuid());
        Assert.Equal(TestData.RoleOwner, await MemberRoleAsync(ws, heir.Id));
        Assert.Equal(TestData.RoleAdmin, await MemberRoleAsync(ws, ws.Owner.Id));

        // A régi tulajdonos új tokenje már nem tartalmazza a tulajdonjog átruházását
        var oldOwner = WithToken(await WorkspaceTokenAsync(ws.Owner.Email, ws.Owner.Password, ws.WorkspaceId, cache: false));
        await AssertForbidden(await oldOwner.PutAsJsonAsync(
            $"/api/workspaces/{ws.WorkspaceId}/owner", new { newOwnerId = ws.Owner.Id }));
    }

    [Fact]
    public async Task OwnershipTransfer_ToCurrentOwner_KeepsOwnerRole()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();

        Assert.Equal(HttpStatusCode.OK, (await WithToken(ws.Token).PutAsJsonAsync(
            $"/api/workspaces/{ws.WorkspaceId}/owner", new { newOwnerId = ws.Owner.Id })).StatusCode);

        Assert.Equal(TestData.RoleOwner, await MemberRoleAsync(ws, ws.Owner.Id));
    }

    [Fact]
    public async Task OwnershipTransfer_ToNonMember_IsRejected_AndKeepsOwner()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var outsider = await RegisterUserAsync("out");

        await AssertError(await WithToken(ws.Token).PutAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}/owner",
            new { newOwnerId = outsider.Id }), HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");

        var data = await ReadJson(await WithToken(ws.Token).GetAsync($"/api/workspaces/{ws.WorkspaceId}"));
        Assert.Equal(ws.Owner.Id, data.GetProperty("ownerId").GetGuid());
    }
}

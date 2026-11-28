using System.Net;
using System.Net.Http.Json;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

/// <summary>A munkaterületről eltávolított tag elveszíti az ottani projekt- és chat-hozzáférését is.</summary>
public class MemberRemovalTests : ApiTestBase
{
    public MemberRemovalTests(ApiFactory factory) : base(factory) { }

    private record Access(Guid ProjectId, Guid RoomId);

    /// <summary>A felhasználó tag lesz, kap egy projektszerepkört és bekerül egy csevegőszobába.</summary>
    private async Task<Access> GrantAccessAsync(OwnedWorkspace ws, TestUser user, Guid? workspaceRole = null)
    {
        await AddMemberAsync(ws, user, workspaceRole ?? TestData.RoleMember);

        var projectId = await CreateProjectAsync(ws);
        var inv = await ReadJson(await WithToken(ws.Token).PostAsJsonAsync("/api/invitations",
            new { type = 2, targetId = projectId, invitedEmail = user.Email, roleId = TestData.RoleViewer }));
        Assert.Equal(HttpStatusCode.OK, (await WithToken(user.Token)
            .PostAsync($"/api/invitations/accept/{inv.GetProperty("token").GetString()}", null)).StatusCode);

        var room = await WithToken(ws.Token).PostAsJsonAsync("/api/chat/rooms",
            new { workspaceId = ws.WorkspaceId, name = "Belső", type = 2, isPrivate = true, memberUserIds = new[] { user.Id } });
        Assert.Equal(HttpStatusCode.Created, room.StatusCode);

        return new Access(projectId, (await ReadJson(room)).GetProperty("id").GetGuid());
    }

    private async Task AssertHasAccessAsync(TestUser user, Access access)
    {
        var client = WithToken(user.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/chat/rooms/{access.RoomId}/messages")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/projects/{access.ProjectId}/tasks")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(
            $"/api/calendar?start=2031-01-01&end=2031-12-31&projectId={access.ProjectId}")).StatusCode);
    }

    private async Task AssertNoAccessAsync(OwnedWorkspace ws, TestUser user, Access access)
    {
        var client = WithToken(user.Token);
        await AssertError(await client.GetAsync($"/api/chat/rooms/{access.RoomId}/messages"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await client.PostAsJsonAsync($"/api/chat/rooms/{access.RoomId}/messages",
            new { roomId = access.RoomId, messageText = "még itt vagyok" }), HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await client.GetAsync($"/api/projects/{access.ProjectId}/tasks"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await client.GetAsync(
            $"/api/calendar?start=2031-01-01&end=2031-12-31&projectId={access.ProjectId}"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");

        var rooms = await ReadJson(await client.GetAsync($"/api/chat/rooms?workspaceId={ws.WorkspaceId}"));
        Assert.Empty(rooms.EnumerateArray());
    }

    private async Task RemoveAsync(OwnedWorkspace ws, Guid userId) =>
        Assert.Equal(HttpStatusCode.NoContent,
            (await WithToken(ws.Token).DeleteAsync($"/api/workspaces/{ws.WorkspaceId}/members/{userId}")).StatusCode);

    [Fact]
    public async Task RemovedMember_LosesProjectAndChatAccess()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var user = await RegisterUserAsync("gone");
        var access = await GrantAccessAsync(ws, user);
        await AssertHasAccessAsync(user, access);

        await RemoveAsync(ws, user.Id);

        await AssertNoAccessAsync(ws, user, access);
    }

    [Fact]
    public async Task ReaddedMember_DoesNotRegainOldProjectOrRoomMemberships()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var user = await RegisterUserAsync("back");
        var access = await GrantAccessAsync(ws, user);
        await RemoveAsync(ws, user.Id);

        await AddMemberAsync(ws, user, TestData.RoleViewer);

        await AssertError(await WithToken(user.Token).GetAsync($"/api/chat/rooms/{access.RoomId}/messages"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await WithToken(user.Token).GetAsync(
            $"/api/calendar?start=2031-01-01&end=2031-12-31&projectId={access.ProjectId}"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Removal_KeepsAccessInOtherWorkspaces_AndOtherMembers()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var otherWs = await CreateOwnerWithWorkspaceAsync();
        var user = await RegisterUserAsync("two");
        var colleague = await RegisterUserAsync("col");
        await GrantAccessAsync(ws, user);
        var colleagueAccess = await GrantAccessAsync(ws, colleague);
        var elsewhere = await GrantAccessAsync(otherWs, user);

        await RemoveAsync(ws, user.Id);

        await AssertHasAccessAsync(user, elsewhere);
        await AssertHasAccessAsync(colleague, colleagueAccess);
    }

    [Fact]
    public async Task RejectedRemoval_KeepsAccess()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var admin = await RegisterUserAsync("adm");
        var adminToken = await AddMemberAsync(ws, admin, TestData.RoleAdmin);
        var coOwner = await RegisterUserAsync("co");
        var access = await GrantAccessAsync(ws, coOwner, TestData.RoleOwner);

        await AssertError(await WithToken(adminToken).DeleteAsync($"/api/workspaces/{ws.WorkspaceId}/members/{coOwner.Id}"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");

        await AssertHasAccessAsync(coOwner, access);
    }
}

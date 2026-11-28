using System.Net;
using System.Net.Http.Json;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

/// <summary>A kliens által küldött X-Workspace-Id / X-Project-Id fejlécek nem adhatnak jogosultságot.</summary>
public class ContextHeaderAuthorizationTests : ApiTestBase
{
    public ContextHeaderAuthorizationTests(ApiFactory factory) : base(factory) { }

    private static HttpClient WithHeader(HttpClient client, string name, Guid value)
    {
        client.DefaultRequestHeaders.Add(name, value.ToString());
        return client;
    }

    private static async Task AssertForbidden(HttpResponseMessage res) =>
        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");

    /// <summary>Saját projekt a megadott projektszintű szerepkörrel; a visszaadott token már tartalmazza a projektkulcsot.</summary>
    private async Task<(string Token, Guid ProjectId)> GrantProjectRoleAsync(OwnedWorkspace ws, TestUser user, Guid roleId)
    {
        var projectId = await CreateProjectAsync(ws);
        // SystemAdmin hívja meg, hogy a teszt független legyen a kiosztható szerepkörök szabályaitól
        var inv = await ReadJson(await (await AdminAsync()).PostAsJsonAsync("/api/invitations",
            new { type = 2, targetId = projectId, invitedEmail = user.Email, roleId }));
        var accept = await WithToken(user.Token).PostAsync($"/api/invitations/accept/{inv.GetProperty("token").GetString()}", null);
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        return (await WorkspaceTokenAsync(user.Email, user.Password, ws.WorkspaceId, cache: false), projectId);
    }

    // ---- Projektszintű jogosultság nem érvényesülhet idegen munkaterület útvonalán ----

    [Fact]
    public async Task ProjectHeader_CannotApplyProjectRoleToForeignWorkspaceRoutes()
    {
        var victim = await CreateOwnerWithWorkspaceAsync();
        var victimMember = await RegisterUserAsync("vm");
        await AddMemberAsync(victim, victimMember, TestData.RoleViewer);
        var attacker = await CreateOwnerWithWorkspaceAsync();
        var (token, ownProjectId) = await GrantProjectRoleAsync(attacker, attacker.Owner, TestData.RoleOwner);
        var client = WithHeader(WithToken(token), "X-Project-Id", ownProjectId);
        var url = $"/api/workspaces/{victim.WorkspaceId}";

        await AssertForbidden(await client.PutAsJsonAsync($"{url}/owner", new { newOwnerId = attacker.Owner.Id }));
        await AssertForbidden(await client.PutAsJsonAsync($"{url}/members/{victimMember.Id}", new { roleId = TestData.RoleAdmin }));
        await AssertForbidden(await client.DeleteAsync($"{url}/members/{victimMember.Id}"));
        await AssertForbidden(await client.PostAsJsonAsync($"{url}/project", new { name = "Idegen" }));
        await AssertForbidden(await client.PutAsJsonAsync(url, new { workspaceName = "Hack" }));
        await AssertForbidden(await client.DeleteAsync(url));
    }

    // ---- Fejléc nem írhatja felül az erőforrás valódi munkaterületét ----

    [Theory]
    [InlineData("X-Workspace-Id")]
    [InlineData("X-Project-Id")]
    public async Task ContextHeader_CannotGrantAccessToForeignTask(string header)
    {
        var victim = await CreateOwnerWithWorkspaceAsync();
        var taskId = await CreateTaskAsync(victim, await CreateProjectAsync(victim));
        var attacker = await CreateOwnerWithWorkspaceAsync();
        var (token, ownProjectId) = await GrantProjectRoleAsync(attacker, attacker.Owner, TestData.RoleAdmin);
        var client = WithHeader(WithToken(token), header, header == "X-Workspace-Id" ? attacker.WorkspaceId : ownProjectId);

        await AssertForbidden(await client.PutAsJsonAsync($"/api/projects/task/{taskId}/status", new { newStatus = 2 }));
        await AssertForbidden(await client.PutAsJsonAsync($"/api/projects/tasks/{taskId}", new { title = "Hack", priority = 1, status = 1 }));
        await AssertForbidden(await client.DeleteAsync($"/api/projects/tasks/{taskId}"));
    }

    [Fact]
    public async Task WorkspaceHeader_CannotGrantAccessToForeignProject()
    {
        var victim = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(victim);
        var attacker = await CreateOwnerWithWorkspaceAsync();
        var client = WithHeader(WithToken(attacker.Token), "X-Workspace-Id", attacker.WorkspaceId);

        await AssertForbidden(await client.PostAsJsonAsync($"/api/projects/{projectId}/task", new { title = "Hack", priority = 1 }));
        await AssertForbidden(await client.PutAsJsonAsync($"/api/projects/{projectId}", new { name = "Hack", status = 1 }));
        await AssertForbidden(await client.DeleteAsync($"/api/projects/{projectId}"));
    }

    // ---- Azonosító nélküli útvonalak: fejléc nem emelheti ki a projektszerepkört ----

    [Theory]
    [InlineData("X-Workspace-Id")]
    [InlineData("X-Project-Id")]
    public async Task ContextHeader_CannotApplyProjectRoleToRoutesWithoutResourceId(string header)
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var user = await RegisterUserAsync("pr");
        await AddMemberAsync(ws, user, TestData.RoleViewer);
        var (token, projectId) = await GrantProjectRoleAsync(ws, user, TestData.RoleAdmin);
        var victim = await CreateOwnerWithWorkspaceAsync();
        var client = WithHeader(WithToken(token), header, projectId);

        await AssertForbidden(await client.GetAsync("/api/roles"));
        await AssertForbidden(await client.GetAsync("/api/auth/roles"));
        await AssertForbidden(await client.PostAsJsonAsync("/api/SoftDelete/delete",
            new { entityName = "Workspace", entityId = victim.WorkspaceId.ToString(), userId = user.Id }));
        await AssertForbidden(await client.PostAsJsonAsync("/api/storage/delete",
            new { entityName = "Workspace", entityId = victim.WorkspaceId.ToString(), userId = user.Id }));
        await AssertForbidden(await client.PostAsJsonAsync("/api/storage/folders",
            new { workspaceId = victim.WorkspaceId, folderName = "Hack" }));
        await AssertForbidden(await client.PostAsJsonAsync("/api/chat/rooms",
            new { workspaceId = victim.WorkspaceId, name = "Hack", type = 2 }));
    }
}

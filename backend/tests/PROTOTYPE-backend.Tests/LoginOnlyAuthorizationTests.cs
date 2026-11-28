using System.Net;
using System.Net.Http.Json;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

/// <summary>Csak bejelentkezést igénylő végpontok: kívülálló nem érheti el más munkaterület adatait.</summary>
public class LoginOnlyAuthorizationTests : ApiTestBase
{
    public LoginOnlyAuthorizationTests(ApiFactory factory) : base(factory) { }

    private static async Task AssertForbidden(HttpResponseMessage res) =>
        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");

    private async Task<(OwnedWorkspace ws, Guid projectId, Guid taskId)> VictimAsync()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);
        return (ws, projectId, await CreateTaskAsync(ws, projectId));
    }

    private static string[] ProjectReadUrls(Guid workspaceId, Guid projectId, Guid taskId) => new[]
    {
        $"/api/projects/{workspaceId}/projects",
        $"/api/projects/{projectId}",
        $"/api/projects/{projectId}/tasks",
        $"/api/projects/tasks/{taskId}"
    };

    // ---------- Projekt és feladat olvasása ----------

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task ProjectRead_AsOutsider_Returns403(int route)
    {
        var (ws, projectId, taskId) = await VictimAsync();
        var outsider = await CreateOwnerWithWorkspaceAsync();
        var url = ProjectReadUrls(ws.WorkspaceId, projectId, taskId)[route];

        await AssertForbidden(await WithToken(outsider.Token).GetAsync(url));
        await AssertForbidden(await WithToken(outsider.Owner.Token).GetAsync(url));
    }

    [Fact]
    public async Task ProjectRead_AsOwnerMemberOrSystemAdmin_Returns200()
    {
        var (ws, projectId, taskId) = await VictimAsync();
        var viewer = await RegisterUserAsync("view");
        await AddMemberAsync(ws, viewer, TestData.RoleViewer);

        foreach (var client in new[] { WithToken(ws.Token), WithToken(viewer.Token), await AdminAsync() })
            foreach (var url in ProjectReadUrls(ws.WorkspaceId, projectId, taskId))
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task ProjectRead_AsProjectOnlyMember_CanReadThatProject()
    {
        var (ws, projectId, taskId) = await VictimAsync();
        var guest = await RegisterUserAsync("guest");
        var inv = await ReadJson(await WithToken(ws.Token).PostAsJsonAsync("/api/invitations",
            new { type = 2, targetId = projectId, invitedEmail = guest.Email, roleId = TestData.RoleViewer }));
        Assert.Equal(HttpStatusCode.OK, (await WithToken(guest.Token)
            .PostAsync($"/api/invitations/accept/{inv.GetProperty("token").GetString()}", null)).StatusCode);

        foreach (var url in ProjectReadUrls(ws.WorkspaceId, projectId, taskId).Skip(1))
            Assert.Equal(HttpStatusCode.OK, (await WithToken(guest.Token).GetAsync(url)).StatusCode);
    }

    // ---------- Chat ----------

    [Fact]
    public async Task DirectRoom_InForeignWorkspace_Returns403()
    {
        var victim = await CreateOwnerWithWorkspaceAsync();
        var attacker = await CreateOwnerWithWorkspaceAsync();

        await AssertForbidden(await WithToken(attacker.Token).PostAsync(
            $"/api/chat/rooms/direct?workspaceId={victim.WorkspaceId}&targetUserId={victim.Owner.Id}", null));
    }

    [Fact]
    public async Task DirectRoom_UnknownWorkspace_Returns404()
    {
        var user = await RegisterUserAsync("dm");
        var other = await RegisterUserAsync("dm");

        await AssertError(await WithToken(user.Token).PostAsync(
            $"/api/chat/rooms/direct?workspaceId={TestData.Missing}&targetUserId={other.Id}", null),
            HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    // ---------- Naptár ----------

    private static object EventBody(Guid? projectId) => new
    {
        projectId,
        title = "Esemény",
        startTime = new DateTime(2031, 5, 1, 10, 0, 0),
        endTime = new DateTime(2031, 5, 1, 11, 0, 0),
        isAllDay = false
    };

    [Fact]
    public async Task Calendar_ForeignProject_CreateUpdateDelete_Return403()
    {
        var (ws, projectId, _) = await VictimAsync();
        var created = await WithToken(ws.Token).PostAsJsonAsync("/api/calendar", EventBody(projectId));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var eventId = (await ReadJson(created)).GetProperty("id").GetGuid();
        var attacker = WithToken((await CreateOwnerWithWorkspaceAsync()).Token);

        await AssertForbidden(await attacker.PostAsJsonAsync("/api/calendar", EventBody(projectId)));
        await AssertForbidden(await attacker.PutAsJsonAsync($"/api/calendar/{eventId}", EventBody(null)));
        await AssertForbidden(await attacker.DeleteAsync($"/api/calendar/{eventId}"));

        // Saját személyes esemény nem helyezhető át idegen projektbe
        var own = await ReadJson(await attacker.PostAsJsonAsync("/api/calendar", EventBody(null)));
        await AssertForbidden(await attacker.PutAsJsonAsync($"/api/calendar/{own.GetProperty("id").GetGuid()}", EventBody(projectId)));
    }
}

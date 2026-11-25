using System.Net;
using System.Net.Http.Json;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

public class ProjectTaskTests : ApiTestBase
{
    public ProjectTaskTests(ApiFactory factory) : base(factory) { }

    // ---------- Projektek ----------

    [Fact]
    public async Task WorkspaceProjects_Existing_Returns200()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync($"/api/projects/{TestData.Ws1}/projects");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.True((await ReadJson(res)).GetArrayLength() >= 1);
    }

    [Fact]
    public async Task WorkspaceProjects_UnknownWorkspace_Returns404()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync($"/api/projects/{TestData.Missing}/projects");

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task GetProject_Existing_Returns200()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync($"/api/projects/{TestData.Project1}");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Webes ügyfélportál", (await ReadJson(res)).GetProperty("projectName").GetString());
    }

    [Fact]
    public async Task GetProject_Unknown_Returns404()
    {
        await AssertError(await (await UserAsync(TestData.DevEmail)).GetAsync($"/api/projects/{TestData.Missing}"),
            HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task GetProject_Anonymous_Returns401()
    {
        await AssertError(await Anonymous().GetAsync($"/api/projects/{TestData.Project1}"),
            HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task UpdateProject_AsOwner_Returns200()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);

        var res = await WithToken(ws.Token).PutAsJsonAsync($"/api/projects/{projectId}",
            new { name = "Frissített", description = "új", status = 2, deadline = "2031-05-01" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Frissített", (await ReadJson(res)).GetProperty("projectName").GetString());
    }

    [Fact]
    public async Task UpdateProject_EmptyName_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);

        var res = await WithToken(ws.Token).PutAsJsonAsync($"/api/projects/{projectId}", new { name = "", status = 1 });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task UpdateProject_Unknown_AsOwner_Returns404()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();

        var res = await WithToken(ws.Token).PutAsJsonAsync($"/api/projects/{TestData.Missing}", new { name = "x", status = 1 });

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task UpdateProject_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1)).PutAsJsonAsync(
            $"/api/projects/{TestData.Project1}", new { name = "Tiltott", status = 1 });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task DeleteProject_AsOwner_Returns204_ThenReturns404()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);
        var owner = WithToken(ws.Token);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/projects/{projectId}")).StatusCode);
        await AssertError(await owner.GetAsync($"/api/projects/{projectId}"), HttpStatusCode.NotFound, "NEM_TALALHATO");
        await AssertError(await owner.DeleteAsync($"/api/projects/{projectId}"), HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task DeleteProject_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1)).DeleteAsync($"/api/projects/{TestData.Project1}");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    // ---------- Feladatok ----------

    [Fact]
    public async Task Tasks_List_ForSeededProject_Returns200()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync($"/api/projects/{TestData.Project1}/tasks");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.True((await ReadJson(res)).GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Task_CreateGetUpdateStatusDelete_FullLifecycle()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);
        var owner = WithToken(ws.Token);

        var create = await owner.PostAsJsonAsync($"/api/projects/{projectId}/task",
            new { title = "Első feladat", description = "leírás", priority = 3, dueDate = "2031-02-03T00:00:00" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var taskId = (await ReadJson(create)).GetProperty("id").GetGuid();

        var get = await owner.GetAsync($"/api/projects/tasks/{taskId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("Első feladat", (await ReadJson(get)).GetProperty("title").GetString());

        var status = await owner.PutAsJsonAsync($"/api/projects/task/{taskId}/status", new { newStatus = 2 });
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);

        var update = await owner.PutAsJsonAsync($"/api/projects/tasks/{taskId}",
            new { title = "Módosított", description = "x", priority = 1, status = 3, dueDate = "2031-03-04", assigneeUserId = (Guid?)null });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Módosított", (await ReadJson(update)).GetProperty("title").GetString());

        var list = await owner.GetAsync($"/api/projects/{projectId}/tasks");
        Assert.Single((await ReadJson(list)).EnumerateArray());

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/projects/tasks/{taskId}")).StatusCode);
        await AssertError(await owner.GetAsync($"/api/projects/tasks/{taskId}"), HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Task_Create_EmptyTitle_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);

        var res = await WithToken(ws.Token).PostAsJsonAsync($"/api/projects/{projectId}/task", new { title = "", priority = 1 });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Task_Create_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1)).PostAsJsonAsync(
            $"/api/projects/{TestData.Project1}/task", new { title = "Tiltott", priority = 1 });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Task_Create_Anonymous_Returns401()
    {
        var res = await Anonymous().PostAsJsonAsync($"/api/projects/{TestData.Project1}/task", new { title = "x", priority = 1 });

        await AssertError(res, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task Task_Get_Unknown_Returns404()
    {
        await AssertError(await (await UserAsync(TestData.DevEmail)).GetAsync($"/api/projects/tasks/{TestData.Missing}"),
            HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Task_UpdateStatus_Unknown_Returns404()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PutAsJsonAsync($"/api/projects/task/{TestData.Missing}/status", new { newStatus = 2 });

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Task_Update_Unknown_Returns404()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PutAsJsonAsync($"/api/projects/tasks/{TestData.Missing}",
            new { title = "x", priority = 1, status = 1 });

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Task_Update_EmptyTitle_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);
        var taskId = await CreateTaskAsync(ws, projectId);

        var res = await WithToken(ws.Token).PutAsJsonAsync($"/api/projects/tasks/{taskId}",
            new { title = "", priority = 1, status = 1 });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Task_Update_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1)).PutAsJsonAsync(
            $"/api/projects/task/{TestData.Missing}/status", new { newStatus = 2 });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Task_Delete_Unknown_Returns404()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();

        await AssertError(await WithToken(ws.Token).DeleteAsync($"/api/projects/tasks/{TestData.Missing}"),
            HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Task_Delete_AsMemberWithoutDeletePermission_Returns403()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);
        var taskId = await CreateTaskAsync(ws, projectId);
        var member = await RegisterUserAsync("mem");
        var memberToken = await AddMemberAsync(ws, member, TestData.RoleMember); // Member: nincs task.delete

        await AssertError(await WithToken(memberToken).DeleteAsync($"/api/projects/tasks/{taskId}"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }
}

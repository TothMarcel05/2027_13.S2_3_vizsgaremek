using System.Net;
using System.Net.Http.Json;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

public class WorkspaceTests : ApiTestBase
{
    public WorkspaceTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task MyWorkspaces_Returns200_ForSeededMember()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync("/api/workspaces/my");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.True((await ReadJson(res)).GetArrayLength() >= 1);
    }

    [Theory]
    [InlineData("page=0&limit=10")]
    [InlineData("page=1&limit=0")]
    [InlineData("page=1&limit=101")]
    public async Task MyWorkspaces_InvalidPaging_Returns400(string query)
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync($"/api/workspaces/my?{query}");

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task MyWorkspaces_Anonymous_Returns401()
    {
        await AssertError(await Anonymous().GetAsync("/api/workspaces/my"), HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task Create_Valid_Returns201_AndOwnerCanSwitchIntoIt()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();

        var my = await WithToken(ws.Owner.Token).GetAsync("/api/workspaces/my");
        var list = await ReadJson(my);
        Assert.Contains(list.EnumerateArray(), w => w.GetProperty("workspaceId").GetGuid() == ws.WorkspaceId);
    }

    [Fact]
    public async Task Create_EmptyName_Returns400()
    {
        var user = await RegisterUserAsync("wsn");
        var res = await WithToken(user.Token).PostAsJsonAsync("/api/workspaces", new { workspaceName = "", description = "x" });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Create_DuplicateNameForSameOwner_Returns409()
    {
        var user = await RegisterUserAsync("wsd");
        var client = WithToken(user.Token);
        var body = new { workspaceName = "Ugyanaz", description = "x" };

        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/workspaces", body)).StatusCode);
        await AssertError(await client.PostAsJsonAsync("/api/workspaces", body), HttpStatusCode.Conflict, "UTKOZES");
    }

    [Fact]
    public async Task Get_Existing_Returns200()
    {
        var res = await (await UserInWorkspaceAsync(TestData.DevEmail, TestData.Ws1)).GetAsync($"/api/workspaces/{TestData.Ws1}");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(TestData.Ws1, (await ReadJson(res)).GetProperty("workspaceId").GetGuid());
    }

    [Fact]
    public async Task Get_Unknown_Returns404()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync($"/api/workspaces/{TestData.Missing}");

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Members_Returns200_AndInvalidPagingReturns400()
    {
        var client = await UserAsync(TestData.DevEmail);

        var ok = await client.GetAsync($"/api/workspaces/{TestData.Ws1}/members");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.True((await ReadJson(ok)).GetArrayLength() >= 8);

        await AssertError(await client.GetAsync($"/api/workspaces/{TestData.Ws1}/members?limit=1000"),
            HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Storage_Existing_Returns200_Unknown_Returns404()
    {
        var client = await UserAsync(TestData.DevEmail);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/workspaces/{TestData.Ws1}/storage")).StatusCode);
        await AssertError(await client.GetAsync($"/api/workspaces/{TestData.Missing}/storage"),
            HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Projects_List_Returns200_AndInvalidPagingReturns400()
    {
        var client = await UserAsync(TestData.DevEmail);

        var ok = await client.GetAsync($"/api/workspaces/{TestData.Ws1}/projects");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.True((await ReadJson(ok)).GetArrayLength() >= 1);

        await AssertError(await client.GetAsync($"/api/workspaces/{TestData.Ws1}/projects?page=0"),
            HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task CreateProject_AsOwner_Returns201()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PostAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}/project",
            new { name = "Új projekt", description = "leírás", deadline = "2030-01-31" });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.Equal("Új projekt", (await ReadJson(res)).GetProperty("projectName").GetString());
    }

    [Fact]
    public async Task CreateProject_EmptyName_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PostAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}/project", new { name = "" });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task CreateProject_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1))
            .PostAsJsonAsync($"/api/workspaces/{TestData.Ws1}/project", new { name = "Tiltott" });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Switch_AsMember_Returns200WithToken()
    {
        var res = await (await UserAsync(TestData.DevEmail)).PostAsync($"/api/workspaces/{TestData.Ws1}/switch", null);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.False(string.IsNullOrEmpty((await ReadJson(res)).GetProperty("workspaceToken").GetString()));
    }

    [Fact]
    public async Task Switch_AsNonMember_Returns403()
    {
        var res = await (await UserAsync(TestData.OutsiderEmail)).PostAsync($"/api/workspaces/{TestData.Ws1}/switch", null);

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Switch_UnknownWorkspace_Returns404()
    {
        var res = await (await UserAsync(TestData.DevEmail)).PostAsync($"/api/workspaces/{TestData.Missing}/switch", null);

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Update_AsOwner_Returns200()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PutAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}",
            new { workspaceName = "Átnevezett", description = "új" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Átnevezett", (await ReadJson(res)).GetProperty("workspaceName").GetString());
    }

    [Fact]
    public async Task Update_EmptyName_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PutAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}", new { workspaceName = "" });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Update_AsMemberWithoutManage_Returns403()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var member = await RegisterUserAsync("mem");
        var memberToken = await AddMemberAsync(ws, member, TestData.RoleMember);

        var res = await WithToken(memberToken).PutAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}", new { workspaceName = "Hack" });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Update_UnknownWorkspace_AsAdmin_Returns404()
    {
        var res = await (await AdminAsync()).PutAsJsonAsync($"/api/workspaces/{TestData.Missing}", new { workspaceName = "x" });

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task TransferOwnership_AsOwner_Returns200_AdminRoleIsForbidden()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var heir = await RegisterUserAsync("heir");
        var heirToken = await AddMemberAsync(ws, heir, TestData.RoleAdmin);

        // Admin szerepkör nem ruházhat át tulajdont
        var forbidden = await WithToken(heirToken).PutAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}/owner", new { newOwnerId = heir.Id });
        await AssertError(forbidden, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");

        var ok = await WithToken(ws.Token).PutAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}/owner", new { newOwnerId = heir.Id });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(heir.Id, (await ReadJson(ok)).GetProperty("ownerId").GetGuid());
    }

    [Fact]
    public async Task TransferOwnership_EmptyGuid_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PutAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}/owner", new { newOwnerId = Guid.Empty });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task MemberRole_Update_Returns200_AndUnknownMemberReturns404()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var member = await RegisterUserAsync("mem");
        await AddMemberAsync(ws, member, TestData.RoleViewer);
        var owner = WithToken(ws.Token);

        var ok = await owner.PutAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}/members/{member.Id}", new { roleId = TestData.RoleMember });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(TestData.RoleMember, (await ReadJson(ok)).GetProperty("roleId").GetGuid());

        var missing = await owner.PutAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}/members/{TestData.Missing}", new { roleId = TestData.RoleMember });
        await AssertError(missing, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task MemberRole_Update_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1))
            .PutAsJsonAsync($"/api/workspaces/{TestData.Ws1}/members/{TestData.DevId}", new { roleId = TestData.RoleViewer });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task RemoveMember_Returns204_ThenSecondCallReturns404()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var member = await RegisterUserAsync("mem");
        await AddMemberAsync(ws, member, TestData.RoleMember);
        var owner = WithToken(ws.Token);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/workspaces/{ws.WorkspaceId}/members/{member.Id}")).StatusCode);
        await AssertError(await owner.DeleteAsync($"/api/workspaces/{ws.WorkspaceId}/members/{member.Id}"),
            HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Delete_AsOwner_Returns204_ThenGetReturns404()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var owner = WithToken(ws.Token);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/workspaces/{ws.WorkspaceId}")).StatusCode);
        await AssertError(await owner.GetAsync($"/api/workspaces/{ws.WorkspaceId}"), HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Delete_AsAdminRoleMember_Returns403()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var admin = await RegisterUserAsync("adm");
        var adminToken = await AddMemberAsync(ws, admin, TestData.RoleAdmin);

        await AssertError(await WithToken(adminToken).DeleteAsync($"/api/workspaces/{ws.WorkspaceId}"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Delete_UnknownWorkspace_AsSystemAdmin_Returns404()
    {
        await AssertError(await (await AdminAsync()).DeleteAsync($"/api/workspaces/{TestData.Missing}"),
            HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task ReserveTask_UnknownTask_Returns404()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);

        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/workspaces/tasks/reserve",
            new { id = TestData.Missing, projectId, assigneeId = ws.Owner.Id });

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task ReserveTask_UserNotInProject_Returns403()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);
        var taskId = await CreateTaskAsync(ws, projectId);

        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/workspaces/tasks/reserve",
            new { id = taskId, projectId, assigneeId = ws.Owner.Id });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task UnreserveTask_UnassignedTask_Returns409()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);
        var taskId = await CreateTaskAsync(ws, projectId);

        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/workspaces/tasks/unreserve",
            new { id = taskId, projectId, assigneeId = ws.Owner.Id });

        await AssertError(res, HttpStatusCode.Conflict, "UTKOZES");
    }

    [Fact]
    public async Task ReserveTask_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1)).PostAsJsonAsync(
            "/api/workspaces/tasks/reserve", new { id = TestData.Missing, projectId = TestData.Project1, assigneeId = TestData.ViewerId });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }
}

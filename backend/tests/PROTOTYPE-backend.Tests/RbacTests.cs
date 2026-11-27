using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

/// <summary>Szerepkör- és jogosultságalapú hozzáférés-vezérlés (JWT claimek + PermissionHandler).</summary>
public class RbacTests : ApiTestBase
{
    public RbacTests(ApiFactory factory) : base(factory) { }

    private static string ForgeToken(string secret, string role, string? workspaceId = null, string? permissionsJson = null,
        DateTime? expires = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, TestData.DevId.ToString()),
            new(ClaimTypes.Email, TestData.DevEmail),
            new(ClaimTypes.Role, role),
        };
        if (workspaceId != null) claims.Add(new Claim("active_workspace_id", workspaceId));
        if (permissionsJson != null) claims.Add(new Claim("all_roles_permissions", permissionsJson));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var token = new JwtSecurityToken(
            issuer: "OkligonApi",
            audience: "OkligonClient",
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-30),
            expires: expires ?? DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private const string RealSecret = "Okligon_Super_Secret_Key_VIZSGAREMEK_3f532f234sd";

    // ---------- Token ellenőrzés ----------

    [Fact]
    public async Task ForgedToken_WithWrongSignature_Returns401()
    {
        var forged = ForgeToken("egy-teljesen-mas-titkos-kulcs-amit-nem-ismer-a-szerver", "2");

        await AssertError(await WithToken(forged).GetAsync("/api/admin/stats"), HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task ExpiredToken_Returns401()
    {
        var expired = ForgeToken(RealSecret, "2", expires: DateTime.UtcNow.AddMinutes(-20));

        await AssertError(await WithToken(expired).GetAsync("/api/users/me"), HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task MissingAuthorizationHeader_Returns401OnProtectedEndpoints()
    {
        foreach (var url in new[] { "/api/workspaces/my", "/api/notifications", "/api/calendar?start=2031-01-01&end=2031-01-02", "/api/SoftDelete/trash" })
            await AssertError(await Anonymous().GetAsync(url), HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    // ---------- Globális szerepkörök ----------

    [Fact]
    public async Task SystemAdmin_PassesAnyPermissionPolicy()
    {
        var admin = await AdminAsync();

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/admin/stats")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/auth/roles")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/SoftDelete/trash")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/storage/nodes?workspaceId={TestData.Ws1}")).StatusCode);
    }

    [Fact]
    public async Task SystemAdmin_CanSwitchIntoAnyWorkspace()
    {
        // Marcel a WS1 tagja is; a második SystemAdmin (Milán) sem tagja a WS2-nek
        var milan = await LoginTokenAsync("milan.kecskemeti@example.com", "Milan2026!Admin");
        var ws2 = Guid.Parse("02000000-0000-4000-8000-000000000002");

        var res = await WithToken(milan).PostAsync($"/api/workspaces/{ws2}/switch", null);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task TokenWithoutPermissionClaim_IsDeniedPermissionEndpoints()
    {
        var bare = ForgeToken(RealSecret, "1");

        await AssertError(await WithToken(bare).GetAsync("/api/admin/users"), HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task TokenWithMalformedPermissionClaim_IsDenied()
    {
        var bad = ForgeToken(RealSecret, "1", TestData.Ws1.ToString(), "ez nem json");

        await AssertError(await WithToken(bad).GetAsync("/api/auth/roles"), HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task ValidlySignedToken_WithGrantedPermission_IsAllowed()
    {
        var ws = TestData.Ws1.ToString();
        var granted = ForgeToken(RealSecret, "1", ws, $"{{\"{ws}\":[\"workspace.roles.manage\"]}}");

        Assert.Equal(HttpStatusCode.OK, (await WithToken(granted).GetAsync("/api/auth/roles")).StatusCode);
    }

    // ---------- Munkaterület-szintű szerepkörök ----------

    [Fact]
    public async Task ViewerRole_CanRead_ButCannotWrite()
    {
        var viewer = await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1);

        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"/api/storage/nodes?workspaceId={TestData.Ws1}")).StatusCode);
        await AssertError(await viewer.PostAsJsonAsync("/api/storage/folders", new { workspaceId = TestData.Ws1, folderName = "x" }),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await viewer.PostAsJsonAsync($"/api/workspaces/{TestData.Ws1}/project", new { name = "x" }),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await viewer.DeleteAsync($"/api/workspaces/{TestData.Ws1}"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task MemberRole_CanCreateTasks_ButCannotDeleteOrManageMembers()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);
        var member = await RegisterUserAsync("mem");
        var memberClient = WithToken(await AddMemberAsync(ws, member, TestData.RoleMember));

        var create = await memberClient.PostAsJsonAsync($"/api/projects/{projectId}/task", new { title = "Tag feladata", priority = 1 });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var taskId = (await ReadJson(create)).GetProperty("id").GetGuid();

        await AssertError(await memberClient.DeleteAsync($"/api/projects/tasks/{taskId}"), HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await memberClient.DeleteAsync($"/api/workspaces/{ws.WorkspaceId}/members/{ws.Owner.Id}"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await memberClient.PostAsJsonAsync("/api/invitations",
            new { type = 1, targetId = ws.WorkspaceId, invitedEmail = "x@example.com" }), HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task AdminRole_CanManageMembers_ButCannotDeleteWorkspaceOrManageSystemRoles()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var adminUser = await RegisterUserAsync("adm");
        var target = await RegisterUserAsync("tgt");
        var adminClient = WithToken(await AddMemberAsync(ws, adminUser, TestData.RoleAdmin));
        await AddMemberAsync(ws, target, TestData.RoleViewer);

        var promote = await adminClient.PutAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}/members/{target.Id}", new { roleId = TestData.RoleMember });
        Assert.Equal(HttpStatusCode.OK, promote.StatusCode);

        await AssertError(await adminClient.DeleteAsync($"/api/workspaces/{ws.WorkspaceId}"), HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await adminClient.PostAsJsonAsync("/api/auth/roles",
            new { id = Guid.NewGuid(), name = "x", description = "x", isSystem = false }), HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");

        // workspace.roles.manage viszont megvan az Admin szerepkörnek
        Assert.Equal(HttpStatusCode.OK, (await adminClient.GetAsync("/api/auth/roles")).StatusCode);
    }

    [Fact]
    public async Task OwnerRole_HasFullWorkspaceControl()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var owner = WithToken(ws.Token);

        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}",
            new { workspaceName = "Teljes jogkör" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await owner.PostAsJsonAsync("/api/invitations",
            new { type = 1, targetId = ws.WorkspaceId, invitedEmail = "uj@example.com", roleId = TestData.RoleMember })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/workspaces/{ws.WorkspaceId}")).StatusCode);
    }

    [Fact]
    public async Task NonMember_CannotSwitchIntoWorkspace_NorPassPermissionPolicies()
    {
        var outsider = await RegisterUserAsync("out");
        var target = await CreateOwnerWithWorkspaceAsync();

        await AssertError(await WithToken(outsider.Token).PostAsync($"/api/workspaces/{target.WorkspaceId}/switch", null),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await WithToken(outsider.Token).PutAsJsonAsync($"/api/workspaces/{target.WorkspaceId}",
            new { workspaceName = "Átvétel" }), HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    // ---------- Egyedi szerepkör: a jogosultságok a tokenben jelennek meg ----------

    [Fact]
    public async Task CustomRole_WithOnlyStorageRead_CanListButNotWrite()
    {
        var admin = await AdminAsync();
        var roleId = Guid.NewGuid();
        var create = await admin.PostAsJsonAsync("/api/auth/roles",
            new { id = roleId, name = $"Csak olvas {roleId:N}", description = "teszt", isSystem = false });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var createdRoleId = (await ReadJson(create)).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/auth/roles/{createdRoleId}/permission",
            new { codes = new[] { "storage.read" } })).StatusCode);

        var ws = await CreateOwnerWithWorkspaceAsync();
        var reader = await RegisterUserAsync("rdr");
        var readerClient = WithToken(await AddMemberAsync(ws, reader, createdRoleId));

        Assert.Equal(HttpStatusCode.OK, (await readerClient.GetAsync($"/api/storage/nodes?workspaceId={ws.WorkspaceId}")).StatusCode);
        await AssertError(await readerClient.PostAsJsonAsync("/api/storage/folders",
            new { workspaceId = ws.WorkspaceId, folderName = "Tiltott" }), HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task RemovedMember_LosesAbilityToSwitchWorkspace()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var member = await RegisterUserAsync("rm");
        await AddMemberAsync(ws, member, TestData.RoleMember);

        Assert.Equal(HttpStatusCode.NoContent,
            (await WithToken(ws.Token).DeleteAsync($"/api/workspaces/{ws.WorkspaceId}/members/{member.Id}")).StatusCode);

        await AssertError(await WithToken(member.Token).PostAsync($"/api/workspaces/{ws.WorkspaceId}/switch", null),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task ProjectScopedPermissions_AreHonouredFromProjectMembership()
    {
        // Seed: Dávid a WS1 Project1 "Contributor" tagja; a projektjogosultság a project-id kulcs alatt szerepel a tokenben
        var token = await WorkspaceTokenAsync(TestData.DevEmail, TestData.UserPassword, TestData.Ws1);
        var payload = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var json = payload.Claims.First(c => c.Type == "all_roles_permissions").Value;

        Assert.Contains(TestData.Ws1.ToString(), json);
        Assert.Contains(TestData.Project1.ToString(), json);
    }
}

/// <summary>Munkaterületek közötti (cross-tenant) hozzáférés tiltása.</summary>
public class CrossWorkspaceRbacTests : ApiTestBase
{
    public CrossWorkspaceRbacTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task OwnerOfAnotherWorkspace_CannotCreateTaskInForeignProject()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();

        var res = await WithToken(ws.Token).PostAsJsonAsync($"/api/projects/{TestData.Project1}/task",
            new { title = "Idegen projekt", priority = 1 });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task OwnerOfAnotherWorkspace_CannotUpdateOrDeleteForeignProjectOrTask()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var foreignTask = await CreateTaskAsStoredAsync();
        var client = WithToken(ws.Token);

        await AssertError(await client.PutAsJsonAsync($"/api/projects/{TestData.Project1}", new { name = "Hack", status = 1 }),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await client.DeleteAsync($"/api/projects/{TestData.Project1}"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await client.PutAsJsonAsync($"/api/projects/task/{foreignTask}/status", new { newStatus = 2 }),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await client.PutAsJsonAsync($"/api/projects/tasks/{foreignTask}", new { title = "Hack", priority = 1, status = 1 }),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await client.DeleteAsync($"/api/projects/tasks/{foreignTask}"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task OwnerOfAnotherWorkspace_CannotManageForeignWorkspace()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var client = WithToken(ws.Token);

        await AssertError(await client.PutAsJsonAsync($"/api/workspaces/{TestData.Ws1}", new { workspaceName = "Hack" }),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await client.DeleteAsync($"/api/workspaces/{TestData.Ws1}"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await client.PostAsJsonAsync($"/api/workspaces/{TestData.Ws1}/project", new { name = "Hack" }),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task OwnerCanStillManageOwnProjectsAndTasks()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);
        var taskId = await CreateTaskAsync(ws, projectId);
        var client = WithToken(ws.Token);

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/projects/task/{taskId}/status", new { newStatus = 2 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/projects/tasks/{taskId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/projects/{projectId}")).StatusCode);
    }

    /// <summary>Egy másik munkaterületen lévő feladat létrehozása adminként.</summary>
    private async Task<Guid> CreateTaskAsStoredAsync()
    {
        var admin = await AdminAsync();
        var res = await admin.PostAsJsonAsync($"/api/projects/{TestData.Project1}/task", new { title = "Idegen feladat", priority = 1 });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await ReadJson(res)).GetProperty("id").GetGuid();
    }
}

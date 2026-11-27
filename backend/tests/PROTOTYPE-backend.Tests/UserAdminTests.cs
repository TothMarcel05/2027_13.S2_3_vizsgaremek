using System.Net;
using System.Net.Http.Json;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

public class UserAdminTests : ApiTestBase
{
    public UserAdminTests(ApiFactory factory) : base(factory) { }

    // ---------- /api/users ----------

    [Fact]
    public async Task Me_Authenticated_Returns200WithProfile()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await ReadJson(res);
        Assert.Equal(TestData.DevEmail, json.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Me_Anonymous_Returns401()
    {
        await AssertError(await Anonymous().GetAsync("/api/users/me"), HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task UpdateMe_ValidName_Returns200()
    {
        var user = await RegisterUserAsync("upd");
        var res = await WithToken(user.Token).PutAsJsonAsync("/api/users/me", new { name = "Új Név", avatarURL = (string?)null });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Új Név", (await ReadJson(res)).GetProperty("name").GetString());
    }

    [Fact]
    public async Task UpdateMe_EmptyName_Returns400()
    {
        var user = await RegisterUserAsync("upd");
        var res = await WithToken(user.Token).PutAsJsonAsync("/api/users/me", new { name = "", avatarURL = (string?)null });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task ChangePassword_Valid_Returns204_AndNewPasswordWorks()
    {
        var user = await RegisterUserAsync("pwd");
        var res = await WithToken(user.Token).PostAsJsonAsync("/api/users/me/change-password",
            new { currentPassword = user.Password, newPassword = "UjJelszo9999!" });

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        var login = await Anonymous().PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = "UjJelszo9999!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_Returns400()
    {
        var user = await RegisterUserAsync("pwd");
        var res = await WithToken(user.Token).PostAsJsonAsync("/api/users/me/change-password",
            new { currentPassword = "Rossz12345!", newPassword = "UjJelszo9999!" });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task ChangePassword_TooShort_Returns400()
    {
        var user = await RegisterUserAsync("pwd");
        var res = await WithToken(user.Token).PostAsJsonAsync("/api/users/me/change-password",
            new { currentPassword = user.Password, newPassword = "rovid" });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task ChangePassword_SameAsCurrent_Returns400()
    {
        var user = await RegisterUserAsync("pwd");
        var res = await WithToken(user.Token).PostAsJsonAsync("/api/users/me/change-password",
            new { currentPassword = user.Password, newPassword = user.Password });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task DeleteMe_Returns200_AndLoginIsRejectedAfterwards()
    {
        var user = await RegisterUserAsync("del");
        var res = await WithToken(user.Token).DeleteAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(user.Id, (await ReadJson(res)).GetProperty("userId").GetGuid());

        var login = await Anonymous().PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = user.Password });
        await AssertError(login, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task PublicProfile_Existing_Returns200()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync($"/api/users/{TestData.AdminId}");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task PublicProfile_Unknown_Returns404()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync($"/api/users/{TestData.Missing}");

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task PublicProfile_EmptyGuid_Returns400()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync($"/api/users/{Guid.Empty}");

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task SearchUsers_ValidQuery_Returns200()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync("/api/users/search?q=Tóth");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task SearchUsers_TooShortQuery_Returns400()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync("/api/users/search?q=a");

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    // ---------- /api/admin ----------

    [Fact]
    public async Task AdminUsers_AsAdmin_Returns200()
    {
        var res = await (await AdminAsync()).GetAsync("/api/admin/users?page=1");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task AdminUsers_InvalidPage_Returns400()
    {
        var res = await (await AdminAsync()).GetAsync("/api/admin/users?page=0");

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task AdminUsers_AsRegularMember_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.DevEmail, TestData.Ws1)).GetAsync("/api/admin/users");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task AdminUsers_Anonymous_Returns401()
    {
        await AssertError(await Anonymous().GetAsync("/api/admin/users"), HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task AdminStats_AsAdmin_Returns200WithCounters()
    {
        var res = await (await AdminAsync()).GetAsync("/api/admin/stats");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await ReadJson(res);
        Assert.True(json.GetProperty("totalUsers").GetInt32() >= 16);
    }

    [Fact]
    public async Task AdminStats_AsRegularUser_Returns403()
    {
        var user = await RegisterUserAsync("stat");
        await AssertError(await WithToken(user.Token).GetAsync("/api/admin/stats"), HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task AdminUpdateGlobalRole_Existing_Returns200()
    {
        var user = await RegisterUserAsync("rol");
        var res = await (await AdminAsync()).PutAsJsonAsync($"/api/admin/users/{user.Id}/role", new { newGlobalRole = 1 });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task AdminUpdateGlobalRole_Unknown_Returns404()
    {
        var res = await (await AdminAsync()).PutAsJsonAsync($"/api/admin/users/{TestData.Missing}/role", new { newGlobalRole = 1 });

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task AdminToggleStatus_SuspendsUser_AndLoginFails()
    {
        var user = await RegisterUserAsync("sus");
        var admin = await AdminAsync();

        var res = await admin.PatchAsync($"/api/admin/users/{user.Id}/status", null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var login = await Anonymous().PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = user.Password });
        await AssertError(login, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");

        var back = await admin.PatchAsync($"/api/admin/users/{user.Id}/status", null);
        Assert.Equal(HttpStatusCode.OK, back.StatusCode);
    }

    [Fact]
    public async Task AdminToggleStatus_Unknown_Returns404()
    {
        var res = await (await AdminAsync()).PatchAsync($"/api/admin/users/{TestData.Missing}/status", null);

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task AdminRemoveUser_Existing_Returns200_Unknown_Returns404()
    {
        var user = await RegisterUserAsync("rem");
        var admin = await AdminAsync();

        Assert.Equal(HttpStatusCode.OK, (await admin.DeleteAsync($"/api/admin/users/{user.Id}")).StatusCode);
        await AssertError(await admin.DeleteAsync($"/api/admin/users/{TestData.Missing}"), HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task AdminRemoveUser_AsRegularMember_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.DevEmail, TestData.Ws1)).DeleteAsync($"/api/admin/users/{TestData.ViewerId}");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    // ---------- /api/roles ----------

    [Fact]
    public async Task AdminRoles_List_AsAdmin_Returns200()
    {
        var res = await (await AdminAsync()).GetAsync("/api/roles");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task AdminRoles_CreateAndUpdatePermissions()
    {
        var admin = await AdminAsync();
        var create = await admin.PostAsJsonAsync("/api/roles",
            new { name = $"Szerep {Guid.NewGuid():N}", permissions = new[] { "storage.read" } });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var roleId = (await ReadJson(create)).GetProperty("roleId").GetGuid();

        var update = await admin.PutAsJsonAsync($"/api/roles/{roleId}/permissions",
            new { permissions = new[] { "storage.read", "storage.write" } });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
    }

    [Fact]
    public async Task AdminRoles_CreateWithEmptyName_Returns400()
    {
        var res = await (await AdminAsync()).PostAsJsonAsync("/api/roles", new { name = "", permissions = Array.Empty<string>() });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task AdminRoles_UpdatePermissions_UnknownRole_Returns404()
    {
        var res = await (await AdminAsync()).PutAsJsonAsync($"/api/roles/{TestData.Missing}/permissions",
            new { permissions = new[] { "storage.read" } });

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task AdminRoles_Create_AsRegularMember_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.DevEmail, TestData.Ws1)).PostAsJsonAsync("/api/roles",
            new { name = "Tiltott", permissions = Array.Empty<string>() });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }
}

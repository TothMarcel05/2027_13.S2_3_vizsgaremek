using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

public class AuthTests : ApiTestBase
{
    public AuthTests(ApiFactory factory) : base(factory) { }

    // ---------- Regisztráció ----------

    [Fact]
    public async Task Register_ValidData_Returns201()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var res = await Anonymous().PostAsJsonAsync("/api/auth/register",
            new { username = $"reg{suffix}", password = "Jelszo1234!", email = $"reg{suffix}@test.example.com" });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var json = await ReadJson(res);
        Assert.Equal($"reg{suffix}@test.example.com", json.GetProperty("email").GetString());
        Assert.NotEqual(Guid.Empty, json.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Register_ShortUsername_Returns400WithErrorSchema()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/register",
            new { username = "ab", password = "Jelszo1234!", email = "short@test.example.com" });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/register",
            new { username = "validname1", password = "Jelszo1234!", email = "nem-email" });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/register",
            new { username = "egyedinev12", password = "Jelszo1234!", email = TestData.AdminEmail });

        await AssertError(res, HttpStatusCode.Conflict, "UTKOZES");
    }

    [Fact]
    public async Task Register_DuplicateUsername_Returns409()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/register",
            new { username = "Tóth Marcel", password = "Jelszo1234!", email = $"{Guid.NewGuid():N}@test.example.com" });

        await AssertError(res, HttpStatusCode.Conflict, "UTKOZES");
    }

    // ---------- Bejelentkezés ----------

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new { email = TestData.DevEmail, password = TestData.UserPassword });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await ReadJson(res);
        Assert.False(json.GetProperty("requiresTwoFactor").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("auth").GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrEmpty(json.GetProperty("auth").GetProperty("refreshToken").GetString()));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new { email = TestData.DevEmail, password = "RosszJelszo!" });

        await AssertError(res, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task Login_UnknownUser_Returns401()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new { email = "nincs.ilyen@example.com", password = "Valami123!" });

        await AssertError(res, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task Login_SuspendedAccount_Returns401()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new { email = TestData.SuspendedEmail, password = TestData.UserPassword });

        await AssertError(res, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task Login_MissingPassword_Returns400()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/login", new { email = TestData.DevEmail });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Login_TwoFactorUser_RequiresSecondStep()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/login",
            new { email = TestData.TwoFactorEmail, password = TestData.UserPassword });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await ReadJson(res);
        Assert.True(json.GetProperty("requiresTwoFactor").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("twoFactorToken").GetString()));
    }

    [Fact]
    public async Task TwoFactorVerify_InvalidToken_Returns401()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/2fa/verify",
            new { twoFactorToken = "ervenytelen", code = "000000" });

        await AssertError(res, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    // ---------- Token frissítés / kijelentkezés ----------

    [Fact]
    public async Task Refresh_ValidToken_Returns200_AndLogoutRevokesIt()
    {
        var user = await RegisterUserAsync("ref");
        var login = await Anonymous().PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = user.Password });
        var auth = (await ReadJson(login)).GetProperty("auth");
        var refresh = auth.GetProperty("refreshToken").GetString()!;
        var access = auth.GetProperty("accessToken").GetString()!;

        var refreshed = await Anonymous().PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var newAuth = await ReadJson(refreshed);
        var newRefresh = newAuth.GetProperty("refreshToken").GetString()!;
        var newAccess = newAuth.GetProperty("accessToken").GetString()!;
        Assert.False(string.IsNullOrEmpty(newAccess));

        var logout = await WithToken(newAccess).PostAsJsonAsync("/api/auth/logout", new { refreshToken = newRefresh });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var again = await Anonymous().PostAsJsonAsync("/api/auth/refresh", new { refreshToken = newRefresh });
        await AssertError(again, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
        _ = access;
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/refresh", new { refreshToken = "nem-letezo-token" });

        await AssertError(res, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task Refresh_EmptyToken_Returns400()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/refresh", new { refreshToken = "  " });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Logout_WithoutToken_Returns401()
    {
        var res = await Anonymous().PostAsJsonAsync("/api/auth/logout", new { refreshToken = "x" });

        await AssertError(res, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    // ---------- Jelszó-visszaállítás ----------

    [Fact]
    public async Task PasswordReset_Request_AlwaysReturns200_EvenForUnknownEmail()
    {
        var res = await Anonymous().PostAsync("/api/auth/password-reset?email=nincs.ilyen@example.com", null);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task PasswordReset_Request_MissingEmail_Returns400()
    {
        var res = await Anonymous().PostAsync("/api/auth/password-reset?email=", null);

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task PasswordReset_Confirm_InvalidToken_Returns400()
    {
        var res = await Anonymous().PutAsJsonAsync(
            $"/api/auth/password-reset?email={TestData.DevEmail}&token=ervenytelen", new { newPwd = "UjJelszo123!" });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    // ---------- 2FA beállítás (védett végpontok) ----------

    [Fact]
    public async Task TwoFactorSetup_Authenticated_Returns200WithSecret()
    {
        var user = await RegisterUserAsync("tfa");
        var res = await WithToken(user.Token).PostAsync("/api/auth/2fa/setup", null);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await ReadJson(res);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("secret").GetString()));
        Assert.StartsWith("otpauth://", json.GetProperty("otpAuthUri").GetString());
    }

    [Fact]
    public async Task TwoFactorSetup_Anonymous_Returns401()
    {
        var res = await Anonymous().PostAsync("/api/auth/2fa/setup", null);

        await AssertError(res, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task TwoFactorEnable_WrongCode_Returns4xxWithErrorSchema()
    {
        var user = await RegisterUserAsync("tfe");
        var client = WithToken(user.Token);
        await client.PostAsync("/api/auth/2fa/setup", null);

        var res = await client.PostAsJsonAsync("/api/auth/2fa/enable", new { code = "000000" });

        Assert.True((int)res.StatusCode is >= 400 and < 500, $"Váratlan státusz: {(int)res.StatusCode}");
        await AssertError(res, res.StatusCode);
    }

    // ---------- Szerepkörök / jogosultságok (RBAC végpontok) ----------

    [Fact]
    public async Task Roles_List_AsSystemAdmin_Returns200()
    {
        var res = await (await AdminAsync()).GetAsync("/api/auth/roles");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Roles_List_Anonymous_Returns401()
    {
        var res = await Anonymous().GetAsync("/api/auth/roles");

        await AssertError(res, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task Roles_List_WithoutPermission_Returns403()
    {
        var user = await RegisterUserAsync("norole");
        var res = await WithToken(user.Token).GetAsync("/api/auth/roles");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Roles_List_AsViewerInWorkspace_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1)).GetAsync("/api/auth/roles");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Permissions_AsSystemAdmin_Returns200()
    {
        var res = await (await AdminAsync()).GetAsync("/api/auth/permissions?query=1");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Permissions_InvalidCategory_Returns400()
    {
        var res = await (await AdminAsync()).GetAsync("/api/auth/permissions?query=abc");

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Role_Lifecycle_CreateGetModifyAddPermissionDelete()
    {
        var admin = await AdminAsync();
        var roleId = Guid.NewGuid();

        var create = await admin.PostAsJsonAsync("/api/auth/roles",
            new { id = roleId, name = $"Teszt {roleId:N}", description = "teszt szerepkör", isSystem = false });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var createdId = (await ReadJson(create)).GetProperty("id").GetGuid();

        var get = await admin.GetAsync($"/api/auth/roles/{createdId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var modify = await admin.PatchAsJsonAsync($"/api/auth/roles/{createdId}/modify",
            new { name = $"Módosított {roleId:N}", description = "új leírás" });
        Assert.Equal(HttpStatusCode.OK, modify.StatusCode);

        var addPerm = await admin.PostAsJsonAsync($"/api/auth/roles/{createdId}/permission",
            new { codes = new[] { "storage.read" } });
        Assert.Equal(HttpStatusCode.OK, addPerm.StatusCode);

        var delete = await admin.DeleteAsync($"/api/auth/roles/{createdId}/remove");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var after = await admin.GetAsync($"/api/auth/roles/{createdId}");
        await AssertError(after, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Role_Get_Unknown_Returns404()
    {
        var res = await (await AdminAsync()).GetAsync($"/api/auth/roles/{TestData.Missing}");

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Role_Create_AsNonAdmin_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.DevEmail, TestData.Ws1)).PostAsJsonAsync("/api/auth/roles",
            new { id = Guid.NewGuid(), name = "Tiltott", description = "x", isSystem = false });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    // ---------- Munkamenetek (csak SystemAdmin) ----------

    [Fact]
    public async Task Sessions_List_AsAdmin_Returns200()
    {
        var res = await (await AdminAsync()).GetAsync("/api/auth/sessions?page=1&limit=10");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Sessions_List_InvalidPaging_Returns400()
    {
        var res = await (await AdminAsync()).GetAsync("/api/auth/sessions?page=0&limit=10");

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Sessions_List_AsNonAdmin_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.DevEmail, TestData.Ws1)).GetAsync("/api/auth/sessions");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    // ---------- Általános hibasémák ----------

    [Fact]
    public async Task UnknownRoute_Returns404WithErrorSchema()
    {
        var res = await Anonymous().GetAsync("/api/nincs-ilyen-vegpont");

        await AssertError(res, HttpStatusCode.NotFound, "NINCS_VEGPONT");
    }

    [Fact]
    public async Task WrongMethod_Returns405WithErrorSchema()
    {
        var res = await Anonymous().GetAsync("/api/auth/login");

        await AssertError(res, HttpStatusCode.MethodNotAllowed, "NEM_ENGEDELYEZETT_METODUS");
    }

    [Fact]
    public async Task InvalidJwt_Returns401WithErrorSchema()
    {
        var res = await WithToken("ez.nem.jwt").GetAsync("/api/users/me");

        await AssertError(res, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }
}

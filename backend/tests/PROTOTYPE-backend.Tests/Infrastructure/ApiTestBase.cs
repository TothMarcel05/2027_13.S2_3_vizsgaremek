using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace PROTOTYPE_backend.Tests.Infrastructure;

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}

/// <summary>Közös segédek: bejelentkezés, workspace-token, új felhasználó / workspace létrehozása, hibaséma-ellenőrzés.</summary>
[Collection(ApiCollection.Name)]
public abstract class ApiTestBase
{
    // bcrypt (cost 13) lassú -> a tokeneket cache-eljük
    private static readonly ConcurrentDictionary<string, string> TokenCache = new();

    protected readonly ApiFactory Factory;

    protected ApiTestBase(ApiFactory factory) => Factory = factory;

    protected HttpClient Anonymous() => Factory.CreateClient();

    protected HttpClient WithToken(string token)
    {
        var c = Factory.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    protected async Task<string> LoginTokenAsync(string email, string password)
    {
        var key = $"login:{email}";
        if (TokenCache.TryGetValue(key, out var cached)) return cached;

        var res = await Anonymous().PostAsJsonAsync("/api/auth/login", new { email, password });
        res.EnsureSuccessStatusCode();
        var doc = await res.Content.ReadFromJsonAsync<JsonElement>();
        var token = doc.GetProperty("auth").GetProperty("accessToken").GetString()!;
        return TokenCache[key] = token;
    }

    protected async Task<string> WorkspaceTokenAsync(string email, string password, Guid workspaceId, bool cache = true)
    {
        var key = $"ws:{email}:{workspaceId}";
        if (cache && TokenCache.TryGetValue(key, out var cached)) return cached;

        var global = await LoginTokenAsync(email, password);
        var res = await WithToken(global).PostAsync($"/api/workspaces/{workspaceId}/switch", null);
        res.EnsureSuccessStatusCode();
        var doc = await res.Content.ReadFromJsonAsync<JsonElement>();
        var token = doc.GetProperty("workspaceToken").GetString()!;
        if (cache) TokenCache[key] = token;
        return token;
    }

    protected async Task<HttpClient> AdminAsync() =>
        WithToken(await LoginTokenAsync(TestData.AdminEmail, TestData.AdminPassword));

    protected async Task<HttpClient> UserAsync(string email) =>
        WithToken(await LoginTokenAsync(email, TestData.UserPassword));

    protected async Task<HttpClient> UserInWorkspaceAsync(string email, Guid workspaceId) =>
        WithToken(await WorkspaceTokenAsync(email, TestData.UserPassword, workspaceId));

    /// <summary>Regisztrál egy új felhasználót és bejelentkezteti (globális token).</summary>
    protected async Task<TestUser> RegisterUserAsync(string prefix = "usr")
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var name = $"{prefix}{suffix}";
        var email = $"{name}@test.example.com";
        const string password = "Jelszo1234!";

        var reg = await Anonymous().PostAsJsonAsync("/api/auth/register", new { username = name, password, email });
        Assert.Equal(HttpStatusCode.Created, reg.StatusCode);
        var id = (await reg.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var token = await LoginTokenAsync(email, password);
        return new TestUser(id, name, email, password, token);
    }

    /// <summary>Új felhasználó + saját (Owner) workspace + workspace-token.</summary>
    protected async Task<OwnedWorkspace> CreateOwnerWithWorkspaceAsync()
    {
        var user = await RegisterUserAsync("own");
        var res = await WithToken(user.Token).PostAsJsonAsync("/api/workspaces",
            new { workspaceName = $"WS {Guid.NewGuid():N}", description = "teszt" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var wsId = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("workspaceId").GetGuid();

        var token = await WorkspaceTokenAsync(user.Email, user.Password, wsId, cache: false);
        return new OwnedWorkspace(user, wsId, token);
    }

    /// <summary>Meghívó létrehozása + elfogadása; visszaadja a tag workspace-tokenjét.</summary>
    protected async Task<string> AddMemberAsync(OwnedWorkspace ws, TestUser member, Guid roleId)
    {
        var inv = await WithToken(ws.Token).PostAsJsonAsync("/api/invitations",
            new { type = 1, targetId = ws.WorkspaceId, invitedEmail = member.Email, roleId });
        Assert.Equal(HttpStatusCode.Created, inv.StatusCode);
        var token = (await inv.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;

        var acc = await WithToken(member.Token).PostAsync($"/api/invitations/accept/{token}", null);
        Assert.Equal(HttpStatusCode.OK, acc.StatusCode);

        return await WorkspaceTokenAsync(member.Email, member.Password, ws.WorkspaceId, cache: false);
    }

    protected async Task<Guid> CreateProjectAsync(OwnedWorkspace ws, string? name = null)
    {
        var res = await WithToken(ws.Token).PostAsJsonAsync($"/api/workspaces/{ws.WorkspaceId}/project",
            new { name = name ?? $"Projekt {Guid.NewGuid():N}", description = "d" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid();
    }

    protected async Task<Guid> CreateTaskAsync(OwnedWorkspace ws, Guid projectId, string? title = null)
    {
        var res = await WithToken(ws.Token).PostAsJsonAsync($"/api/projects/{projectId}/task",
            new { title = title ?? "Feladat", description = "leírás", priority = 2 });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    /// <summary>Ellenőrzi a státuszkódot és a { error: { message, code } } sémát.</summary>
    protected static async Task AssertError(HttpResponseMessage res, HttpStatusCode status, string? code = null)
    {
        var body = await res.Content.ReadAsStringAsync();
        Assert.True(status == res.StatusCode, $"Várt: {(int)status}, kapott: {(int)res.StatusCode}. Törzs: {body}");

        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("error", out var error), $"Hiányzik az 'error' objektum: {body}");
        Assert.True(error.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String, $"Hiányzik az error.message: {body}");
        Assert.True(error.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String, $"Hiányzik az error.code: {body}");
        if (code != null) Assert.Equal(code, c.GetString());
    }

    protected static async Task<JsonElement> ReadJson(HttpResponseMessage res)
    {
        var body = await res.Content.ReadAsStringAsync();
        Assert.True(res.IsSuccessStatusCode, $"Sikertelen válasz: {(int)res.StatusCode} {body}");
        return JsonDocument.Parse(body).RootElement.Clone();
    }
}

public record TestUser(Guid Id, string Name, string Email, string Password, string Token);
public record OwnedWorkspace(TestUser Owner, Guid WorkspaceId, string Token);

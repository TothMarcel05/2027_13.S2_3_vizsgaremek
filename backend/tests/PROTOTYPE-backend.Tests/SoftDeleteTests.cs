using System.Net;
using System.Net.Http.Json;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

/// <summary>A SoftDeleteService pontos C# entitásneveket vár: "StorageNode", "AppUser" (+ "Workspace", "Project").</summary>
public class SoftDeleteTests : ApiTestBase
{
    public SoftDeleteTests(ApiFactory factory) : base(factory) { }

    private async Task<Guid> CreateFolderAsync(OwnedWorkspace ws)
    {
        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/storage/folders",
            new { workspaceId = ws.WorkspaceId, folderName = $"Mappa {Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await ReadJson(res)).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task StorageNode_DeleteTrashRestore_FullCycle()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var folderId = await CreateFolderAsync(ws);
        var owner = WithToken(ws.Token);

        var del = await owner.PostAsJsonAsync("/api/SoftDelete/delete", new { entityName = "StorageNode", entityId = folderId.ToString() });
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);

        var trash = await owner.GetAsync("/api/SoftDelete/trash");
        Assert.Equal(HttpStatusCode.OK, trash.StatusCode);
        var item = (await ReadJson(trash)).EnumerateArray().First(i => i.GetProperty("entityId").GetString() == folderId.ToString());
        Assert.Equal("StorageNode", item.GetProperty("entityName").GetString());
        Assert.True(item.GetProperty("canRestore").GetBoolean());

        // a törölt elem nem szerepel a tárhely-listában
        var nodes = await ReadJson(await owner.GetAsync($"/api/storage/nodes?workspaceId={ws.WorkspaceId}"));
        Assert.DoesNotContain(nodes.EnumerateArray(), n => n.GetProperty("id").GetGuid() == folderId);

        var restore = await owner.PostAsJsonAsync("/api/SoftDelete/restore", new { entityName = "StorageNode", entityId = folderId.ToString() });
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);

        var nodesAfter = await ReadJson(await owner.GetAsync($"/api/storage/nodes?workspaceId={ws.WorkspaceId}"));
        Assert.Contains(nodesAfter.EnumerateArray(), n => n.GetProperty("id").GetGuid() == folderId);
    }

    [Fact]
    public async Task StorageNode_DeleteTwice_SecondReturns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var folderId = await CreateFolderAsync(ws);
        var owner = WithToken(ws.Token);
        var body = new { entityName = "StorageNode", entityId = folderId.ToString() };

        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync("/api/SoftDelete/delete", body)).StatusCode);
        await AssertError(await owner.PostAsJsonAsync("/api/SoftDelete/delete", body), HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Restore_NotDeletedNode_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var folderId = await CreateFolderAsync(ws);

        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/SoftDelete/restore",
            new { entityName = "StorageNode", entityId = folderId.ToString() });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task AppUser_DeleteAndRestore_AsSystemAdmin()
    {
        var user = await RegisterUserAsync("sd");
        var admin = await AdminAsync();

        var del = await admin.PostAsJsonAsync("/api/SoftDelete/delete", new { entityName = "AppUser", entityId = user.Id.ToString() });
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);

        var loginWhileDeleted = await Anonymous().PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = user.Password });
        await AssertError(loginWhileDeleted, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");

        var restore = await admin.PostAsJsonAsync("/api/SoftDelete/restore", new { entityName = "AppUser", entityId = user.Id.ToString() });
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);

        var loginAfter = await Anonymous().PostAsJsonAsync("/api/auth/login", new { email = user.Email, password = user.Password });
        Assert.Equal(HttpStatusCode.OK, loginAfter.StatusCode);
    }

    [Fact]
    public async Task AppUser_DeleteUnknown_Returns400()
    {
        var res = await (await AdminAsync()).PostAsJsonAsync("/api/SoftDelete/delete",
            new { entityName = "AppUser", entityId = TestData.Missing.ToString() });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Theory]
    [InlineData("storagenode")]   // kis-nagybetű érzékeny
    [InlineData("User")]
    [InlineData("Nemletezo")]
    public async Task Delete_UnsupportedEntityName_Returns400(string entityName)
    {
        var res = await (await AdminAsync()).PostAsJsonAsync("/api/SoftDelete/delete",
            new { entityName, entityId = Guid.NewGuid().ToString() });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Delete_MissingFields_Returns400()
    {
        var res = await (await AdminAsync()).PostAsJsonAsync("/api/SoftDelete/delete", new { entityName = "", entityId = "" });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Restore_MissingFields_Returns400()
    {
        var res = await (await AdminAsync()).PostAsJsonAsync("/api/SoftDelete/restore", new { entityName = "", entityId = "" });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Trash_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1)).GetAsync("/api/SoftDelete/trash");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Delete_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1)).PostAsJsonAsync(
            "/api/SoftDelete/delete", new { entityName = "StorageNode", entityId = Guid.NewGuid().ToString() });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Trash_Anonymous_Returns401()
    {
        await AssertError(await Anonymous().GetAsync("/api/SoftDelete/trash"), HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task PurgeExpired_AsAdmin_Returns200WithCount()
    {
        var res = await (await AdminAsync()).DeleteAsync("/api/SoftDelete/purge-expired");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.True((await ReadJson(res)).GetProperty("deletedCount").GetInt32() >= 0);
    }

    [Fact]
    public async Task PurgeExpired_AsOwner_Returns403()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();

        await AssertError(await WithToken(ws.Token).DeleteAsync("/api/SoftDelete/purge-expired"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }
}

public class SoftDeleteUserRestrictionTests : ApiTestBase
{
    public SoftDeleteUserRestrictionTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task AppUser_SoftDelete_AsWorkspaceOwner_Returns403_AndUserStaysActive()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var victim = await RegisterUserAsync("vic");

        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/SoftDelete/delete",
            new { entityName = "AppUser", entityId = victim.Id.ToString() });
        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");

        var login = await Anonymous().PostAsJsonAsync("/api/auth/login", new { email = victim.Email, password = victim.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task AppUser_SoftDelete_ViaStorageEndpoint_AsWorkspaceOwner_Returns403()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var victim = await RegisterUserAsync("vic");

        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/storage/delete",
            new { entityName = "AppUser", entityId = victim.Id.ToString() });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task AppUser_Restore_AsWorkspaceOwner_Returns403()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var victim = await RegisterUserAsync("vic");
        Assert.Equal(HttpStatusCode.OK, (await (await AdminAsync()).PostAsJsonAsync("/api/SoftDelete/delete",
            new { entityName = "AppUser", entityId = victim.Id.ToString() })).StatusCode);

        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/SoftDelete/restore",
            new { entityName = "AppUser", entityId = victim.Id.ToString() });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task StorageNode_SoftDelete_StillAllowedForWorkspaceOwner()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var folder = await WithToken(ws.Token).PostAsJsonAsync("/api/storage/folders",
            new { workspaceId = ws.WorkspaceId, folderName = "Törlendő" });
        var id = (await ReadJson(folder)).GetProperty("id").GetGuid();

        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/SoftDelete/delete",
            new { entityName = "StorageNode", entityId = id.ToString() });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}

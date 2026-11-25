using System.Net;
using System.Net.Http.Json;
using System.Text;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

public class StorageTests : ApiTestBase
{
    public StorageTests(ApiFactory factory) : base(factory) { }

    private async Task<Guid> CreateFolderAsync(OwnedWorkspace ws, string? name = null)
    {
        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/storage/folders",
            new { workspaceId = ws.WorkspaceId, folderName = name ?? $"Mappa {Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await ReadJson(res)).GetProperty("id").GetGuid();
    }

    private static MultipartFormDataContent UploadForm(Guid workspaceId, string fileName, string content, bool emptyFile = false)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(workspaceId.ToString()), "WorkspaceId" },
            { new StringContent(Guid.Empty.ToString()), "UserId" },
            { new ByteArrayContent(emptyFile ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(content)), "File", fileName }
        };
        return form;
    }

    private async Task<Guid> UploadAsync(OwnedWorkspace ws, string fileName = "teszt.txt", string content = "tartalom")
    {
        var res = await WithToken(ws.Token).PostAsync("/api/storage/upload", UploadForm(ws.WorkspaceId, fileName, content));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await ReadJson(res)).GetProperty("id").GetGuid();
    }

    // ---------- Listázás ----------

    [Fact]
    public async Task Nodes_AsMemberWithReadPermission_Returns200()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1))
            .GetAsync($"/api/storage/nodes?workspaceId={TestData.Ws1}");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.True((await ReadJson(res)).GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Nodes_MissingWorkspaceId_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();

        await AssertError(await WithToken(ws.Token).GetAsync("/api/storage/nodes"), HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Nodes_Anonymous_Returns401()
    {
        await AssertError(await Anonymous().GetAsync($"/api/storage/nodes?workspaceId={TestData.Ws1}"),
            HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task Nodes_GlobalTokenWithoutWorkspace_Returns403()
    {
        var res = await (await UserAsync(TestData.OutsiderEmail)).GetAsync($"/api/storage/nodes?workspaceId={TestData.Ws1}");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    // ---------- Mappa / feltöltés / letöltés ----------

    [Fact]
    public async Task Folder_Create_Returns201_AndAppearsInListing()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var folderId = await CreateFolderAsync(ws, "Dokumentumok");

        var list = await ReadJson(await WithToken(ws.Token).GetAsync($"/api/storage/nodes?workspaceId={ws.WorkspaceId}"));
        Assert.Contains(list.EnumerateArray(), n => n.GetProperty("id").GetGuid() == folderId);
    }

    [Fact]
    public async Task Folder_Create_EmptyName_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/storage/folders", new { workspaceId = ws.WorkspaceId, folderName = " " });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Folder_Create_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1)).PostAsJsonAsync(
            "/api/storage/folders", new { workspaceId = TestData.Ws1, folderName = "Tiltott" });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Upload_Download_RoundTrip()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var fileId = await UploadAsync(ws, "uzenet.txt", "Helló, tárhely!");

        var dl = await WithToken(ws.Token).GetAsync($"/api/storage/nodes/{fileId}/download");

        Assert.Equal(HttpStatusCode.OK, dl.StatusCode);
        Assert.Equal("Helló, tárhely!", await dl.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Upload_EmptyFile_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PostAsync("/api/storage/upload", UploadForm(ws.WorkspaceId, "ures.txt", "", emptyFile: true));

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Upload_UnknownWorkspace_Returns404()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PostAsync("/api/storage/upload", UploadForm(TestData.Missing, "a.txt", "x"));

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Upload_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1))
            .PostAsync("/api/storage/upload", UploadForm(TestData.Ws1, "a.txt", "x"));

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Download_Unknown_Returns404()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();

        await AssertError(await WithToken(ws.Token).GetAsync($"/api/storage/nodes/{TestData.Missing}/download"),
            HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    // ---------- Módosítás / megosztás / törlés ----------

    [Fact]
    public async Task ChangeData_RenamesNode_AndUnknownNodeReturns404()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var folderId = await CreateFolderAsync(ws);
        var owner = WithToken(ws.Token);

        var ok = await owner.PutAsJsonAsync("/api/storage/nodes/change-data",
            new { workspaceId = ws.WorkspaceId, nodeId = folderId, name = "Átnevezve" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("Átnevezve", (await ReadJson(ok)).GetProperty("name").GetString());

        var missing = await owner.PutAsJsonAsync("/api/storage/nodes/change-data",
            new { workspaceId = ws.WorkspaceId, nodeId = TestData.Missing, name = "x" });
        await AssertError(missing, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Share_WithRole_Returns200_UnknownNodeReturns404()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var folderId = await CreateFolderAsync(ws);
        var owner = WithToken(ws.Token);

        Assert.Equal(HttpStatusCode.OK,
            (await owner.PostAsync($"/api/storage/nodes/{folderId}/share/{TestData.RoleMember}", null)).StatusCode);
        await AssertError(await owner.PostAsync($"/api/storage/nodes/{TestData.Missing}/share/{TestData.RoleMember}", null),
            HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Delete_Node_Returns204_ThenReturns404_AndRestoreWorks()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var folderId = await CreateFolderAsync(ws);
        var owner = WithToken(ws.Token);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/storage/nodes/{folderId}")).StatusCode);
        await AssertError(await owner.DeleteAsync($"/api/storage/nodes/{folderId}"), HttpStatusCode.NotFound, "NEM_TALALHATO");

        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsync($"/api/storage/nodes/{folderId}/restore", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/storage/nodes/{folderId}")).StatusCode);
    }

    [Fact]
    public async Task Delete_Node_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1))
            .DeleteAsync($"/api/storage/nodes/{Guid.NewGuid()}");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Restore_UnknownNode_Returns404()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();

        await AssertError(await WithToken(ws.Token).PostAsync($"/api/storage/nodes/{TestData.Missing}/restore", null),
            HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    // ---------- Lomtár (storage modul) ----------

    [Fact]
    public async Task Trash_SoftDeleteListRestore_ViaStorageEndpoints()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var folderId = await CreateFolderAsync(ws);
        var owner = WithToken(ws.Token);

        var del = await owner.PostAsJsonAsync("/api/storage/delete", new { entityName = "StorageNode", entityId = folderId.ToString() });
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);

        var trash = await owner.GetAsync("/api/storage/trash");
        Assert.Equal(HttpStatusCode.OK, trash.StatusCode);
        Assert.Contains((await ReadJson(trash)).EnumerateArray(),
            i => i.GetProperty("entityId").GetString() == folderId.ToString());

        var restore = await owner.PostAsJsonAsync("/api/storage/restore", new { entityName = "StorageNode", entityId = folderId.ToString() });
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
    }

    [Fact]
    public async Task Trash_Delete_InvalidInputs_Return400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var owner = WithToken(ws.Token);

        await AssertError(await owner.PostAsJsonAsync("/api/storage/delete", new { entityName = "", entityId = "" }),
            HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
        await AssertError(await owner.PostAsJsonAsync("/api/storage/delete", new { entityName = "StorageNode", entityId = "nem-guid" }),
            HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
        await AssertError(await owner.PostAsJsonAsync("/api/storage/restore", new { entityName = "StorageNode", entityId = TestData.Missing.ToString() }),
            HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Trash_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1)).GetAsync("/api/storage/trash");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task PurgeExpired_AsSystemAdmin_Returns200_AsOwnerReturns403()
    {
        Assert.Equal(HttpStatusCode.OK, (await (await AdminAsync()).DeleteAsync("/api/storage/purge-expired")).StatusCode);

        var ws = await CreateOwnerWithWorkspaceAsync();
        await AssertError(await WithToken(ws.Token).DeleteAsync("/api/storage/purge-expired"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }
}

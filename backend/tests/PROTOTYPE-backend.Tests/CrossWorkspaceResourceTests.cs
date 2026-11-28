using System.Net;
using System.Net.Http.Json;
using System.Text;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

/// <summary>
/// A workspaceId nélküli útvonalakon a jogosultságnak az erőforrás saját munkaterületére kell vonatkoznia,
/// nem a hívó aktív munkaterületére.
/// </summary>
public class CrossWorkspaceResourceTests : ApiTestBase
{
    public CrossWorkspaceResourceTests(ApiFactory factory) : base(factory) { }

    private static async Task AssertForbidden(HttpResponseMessage res) =>
        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");

    private static async Task AssertBadRequest(HttpResponseMessage res) =>
        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");

    private static MultipartFormDataContent UploadForm(Guid workspaceId, Guid? projectId = null, Guid? parentId = null)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(workspaceId.ToString()), "WorkspaceId" },
            { new StringContent(Guid.Empty.ToString()), "UserId" },
            { new ByteArrayContent(Encoding.UTF8.GetBytes("titok")), "File", "titok.txt" }
        };
        if (projectId != null) form.Add(new StringContent(projectId.Value.ToString()), "ProjectId");
        if (parentId != null) form.Add(new StringContent(parentId.Value.ToString()), "ParentId");
        return form;
    }

    private async Task<Guid> FolderAsync(OwnedWorkspace ws)
    {
        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/storage/folders",
            new { workspaceId = ws.WorkspaceId, folderName = $"Mappa {Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await ReadJson(res)).GetProperty("id").GetGuid();
    }

    private async Task<Guid> FileAsync(OwnedWorkspace ws)
    {
        var res = await WithToken(ws.Token).PostAsync("/api/storage/upload", UploadForm(ws.WorkspaceId));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await ReadJson(res)).GetProperty("id").GetGuid();
    }

    private async Task<(OwnedWorkspace victim, HttpClient attacker, OwnedWorkspace own)> ActorsAsync()
    {
        var victim = await CreateOwnerWithWorkspaceAsync();
        var own = await CreateOwnerWithWorkspaceAsync();
        return (victim, WithToken(own.Token), own);
    }

    // ---------- Tárhely ----------

    [Fact]
    public async Task Storage_ListOfForeignWorkspace_Returns403()
    {
        var (victim, attacker, _) = await ActorsAsync();

        await AssertForbidden(await attacker.GetAsync($"/api/storage/nodes?workspaceId={victim.WorkspaceId}"));
    }

    [Fact]
    public async Task Storage_UploadIntoForeignWorkspace_Returns403()
    {
        var (victim, attacker, _) = await ActorsAsync();

        await AssertForbidden(await attacker.PostAsync("/api/storage/upload", UploadForm(victim.WorkspaceId)));
    }

    [Fact]
    public async Task Storage_FolderInForeignWorkspace_Returns403()
    {
        var (victim, attacker, _) = await ActorsAsync();

        await AssertForbidden(await attacker.PostAsJsonAsync("/api/storage/folders",
            new { workspaceId = victim.WorkspaceId, folderName = "Hack" }));
    }

    [Fact]
    public async Task Storage_DownloadForeignFile_Returns403()
    {
        var (victim, attacker, _) = await ActorsAsync();
        var fileId = await FileAsync(victim);

        await AssertForbidden(await attacker.GetAsync($"/api/storage/nodes/{fileId}/download"));
    }

    [Fact]
    public async Task Storage_RenameForeignNode_Returns403_EvenWithOwnWorkspaceInBody()
    {
        var (victim, attacker, own) = await ActorsAsync();
        var nodeId = await FolderAsync(victim);

        await AssertForbidden(await attacker.PutAsJsonAsync("/api/storage/nodes/change-data",
            new { workspaceId = own.WorkspaceId, nodeId, name = "Hack" }));
    }

    [Fact]
    public async Task Storage_ShareDeleteRestoreForeignNode_Returns403()
    {
        var (victim, attacker, _) = await ActorsAsync();
        var nodeId = await FolderAsync(victim);

        await AssertForbidden(await attacker.PostAsync($"/api/storage/nodes/{nodeId}/share/{TestData.RoleMember}", null));
        await AssertForbidden(await attacker.DeleteAsync($"/api/storage/nodes/{nodeId}"));
        Assert.Equal(HttpStatusCode.NoContent, (await WithToken(victim.Token).DeleteAsync($"/api/storage/nodes/{nodeId}")).StatusCode);
        await AssertForbidden(await attacker.PostAsync($"/api/storage/nodes/{nodeId}/restore", null));
    }

    [Theory]
    [InlineData("parent")]
    [InlineData("project")]
    public async Task Storage_ForeignParentOrProject_InOwnWorkspace_Returns400(string kind)
    {
        var (victim, attacker, own) = await ActorsAsync();
        Guid? parentId = kind == "parent" ? await FolderAsync(victim) : null;
        Guid? projectId = kind == "project" ? await CreateProjectAsync(victim) : null;

        await AssertBadRequest(await attacker.PostAsJsonAsync("/api/storage/folders",
            new { workspaceId = own.WorkspaceId, folderName = "Hack", parentId, projectId }));
        await AssertBadRequest(await attacker.PostAsync("/api/storage/upload", UploadForm(own.WorkspaceId, projectId, parentId)));
    }

    [Fact]
    public async Task Storage_OwnParentAndProject_StillWork()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var parentId = await FolderAsync(ws);
        var projectId = await CreateProjectAsync(ws);
        var owner = WithToken(ws.Token);

        Assert.Equal(HttpStatusCode.Created, (await owner.PostAsJsonAsync("/api/storage/folders",
            new { workspaceId = ws.WorkspaceId, folderName = "Almappa", parentId, projectId })).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await owner.PostAsync("/api/storage/upload", UploadForm(ws.WorkspaceId, projectId, parentId))).StatusCode);
    }

    [Fact]
    public async Task Storage_FolderInUnknownWorkspace_Returns404()
    {
        var (_, attacker, _) = await ActorsAsync();

        await AssertError(await attacker.PostAsJsonAsync("/api/storage/folders",
            new { workspaceId = TestData.Missing, folderName = "x" }), HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    // ---------- Lomtár ----------

    [Theory]
    [InlineData("/api/SoftDelete", "StorageNode")]
    [InlineData("/api/SoftDelete", "Project")]
    [InlineData("/api/SoftDelete", "Workspace")]
    [InlineData("/api/storage", "StorageNode")]
    [InlineData("/api/storage", "Project")]
    [InlineData("/api/storage", "Workspace")]
    public async Task Trash_ForeignEntity_DeleteAndRestore_Return403(string prefix, string entityName)
    {
        var (victim, attacker, _) = await ActorsAsync();
        var entityId = entityName switch
        {
            "StorageNode" => await FolderAsync(victim),
            "Project" => await CreateProjectAsync(victim),
            _ => victim.WorkspaceId
        };
        var body = new { entityName, entityId = entityId.ToString() };

        await AssertForbidden(await attacker.PostAsJsonAsync($"{prefix}/delete", body));

        Assert.Equal(HttpStatusCode.OK,
            (await (await AdminAsync()).PostAsJsonAsync($"{prefix}/delete", body)).StatusCode);
        await AssertForbidden(await attacker.PostAsJsonAsync($"{prefix}/restore", body));
    }

    [Theory]
    [InlineData("/api/SoftDelete")]
    [InlineData("/api/storage")]
    public async Task Trash_OwnerCanRestoreOwnDeletedWorkspace_FromAnotherWorkspaceToken(string prefix)
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Owner.Token).PostAsJsonAsync("/api/workspaces",
            new { workspaceName = $"WS {Guid.NewGuid():N}" });
        var otherId = (await ReadJson(res)).GetProperty("workspaceId").GetGuid();
        var other = WithToken(await WorkspaceTokenAsync(ws.Owner.Email, ws.Owner.Password, otherId, cache: false));
        var body = new { entityName = "Workspace", entityId = ws.WorkspaceId.ToString() };

        Assert.Equal(HttpStatusCode.NoContent, (await WithToken(ws.Token).DeleteAsync($"/api/workspaces/{ws.WorkspaceId}")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await other.PostAsJsonAsync($"{prefix}/restore", body)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await other.PostAsJsonAsync($"{prefix}/delete", body)).StatusCode);
    }

    // ---------- Chat ----------

    [Fact]
    public async Task Chat_RoomInForeignWorkspace_Returns403()
    {
        var (victim, attacker, _) = await ActorsAsync();

        await AssertForbidden(await attacker.PostAsJsonAsync("/api/chat/rooms",
            new { workspaceId = victim.WorkspaceId, name = "Hack", type = 2 }));
    }

    [Fact]
    public async Task Chat_RoomForForeignProject_InOwnWorkspace_Returns400()
    {
        var (victim, attacker, own) = await ActorsAsync();
        var projectId = await CreateProjectAsync(victim);

        await AssertBadRequest(await attacker.PostAsJsonAsync("/api/chat/rooms",
            new { workspaceId = own.WorkspaceId, name = "Hack", type = 3, projectId }));
    }

    [Fact]
    public async Task Chat_RoomInUnknownWorkspace_Returns404()
    {
        var (_, attacker, _) = await ActorsAsync();

        await AssertError(await attacker.PostAsJsonAsync("/api/chat/rooms",
            new { workspaceId = TestData.Missing, name = "x", type = 2 }), HttpStatusCode.NotFound, "NEM_TALALHATO");
    }
}

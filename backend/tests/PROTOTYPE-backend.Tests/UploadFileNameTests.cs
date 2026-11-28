using System.Net;
using System.Text;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

/// <summary>A feltöltött fájl neve nem vezethet ki az Uploads mappából.</summary>
public class UploadFileNameTests : ApiTestBase
{
    public UploadFileNameTests(ApiFactory factory) : base(factory) { }

    private static string UploadsFolder => Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

    private static MultipartFormDataContent UploadForm(Guid workspaceId, string fileName, string content) => new()
    {
        { new StringContent(workspaceId.ToString()), "WorkspaceId" },
        { new StringContent(Guid.Empty.ToString()), "UserId" },
        { new ByteArrayContent(Encoding.UTF8.GetBytes(content)), "File", fileName }
    };

    // Csak a teszt saját munkakönyvtárában lévő, egyedi nevű "csali" fájlt célozzuk.
    [Theory]
    [InlineData("a/../../{0}")]
    [InlineData("a\\..\\..\\{0}")]
    public async Task Upload_TraversalName_CannotOverwriteFileOutsideUploads(string pattern)
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var canaryName = $"canary-{Guid.NewGuid():N}.txt";
        var canaryPath = Path.Combine(Directory.GetCurrentDirectory(), canaryName);
        await File.WriteAllTextAsync(canaryPath, "eredeti");

        try
        {
            var res = await WithToken(ws.Token).PostAsync("/api/storage/upload",
                UploadForm(ws.WorkspaceId, string.Format(pattern, canaryName), "felulirva"));

            Assert.Equal("eredeti", await File.ReadAllTextAsync(canaryPath));
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var body = await ReadJson(res);
            Assert.Equal(canaryName, body.GetProperty("name").GetString());

            var stored = Assert.Single(Directory.GetFiles(UploadsFolder, $"*_{canaryName}"));
            Assert.Equal("felulirva", await File.ReadAllTextAsync(stored));
            var download = await WithToken(ws.Token).GetAsync($"/api/storage/nodes/{body.GetProperty("id").GetGuid()}/download");
            Assert.Equal("felulirva", await download.Content.ReadAsStringAsync());
        }
        finally
        {
            File.Delete(canaryPath);
        }
    }

    [Theory]
    [InlineData("..")]
    [InlineData("mappa/")]
    [InlineData("mappa/..")]
    public async Task Upload_NameWithoutUsableFileName_Returns400(string fileName)
    {
        var ws = await CreateOwnerWithWorkspaceAsync();

        await AssertError(await WithToken(ws.Token).PostAsync("/api/storage/upload", UploadForm(ws.WorkspaceId, fileName, "x")),
            HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Upload_TooLongName_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();

        await AssertError(await WithToken(ws.Token).PostAsync("/api/storage/upload",
            UploadForm(ws.WorkspaceId, new string('a', 300) + ".txt", "x")), HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Theory]
    [InlineData("adat:rejtett.txt")]
    [InlineData("kérdés?.txt")]
    public async Task Upload_CharactersInvalidOnDisk_AreStoredSafely(string fileName)
    {
        var ws = await CreateOwnerWithWorkspaceAsync();

        var res = await WithToken(ws.Token).PostAsync("/api/storage/upload", UploadForm(ws.WorkspaceId, fileName, "tartalom"));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var download = await WithToken(ws.Token).GetAsync(
            $"/api/storage/nodes/{(await ReadJson(res)).GetProperty("id").GetGuid()}/download");
        Assert.Equal("tartalom", await download.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Upload_OrdinaryUnicodeName_IsKept()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        const string fileName = "Árvíztűrő jegyzet (2).v1.txt";

        var res = await WithToken(ws.Token).PostAsync("/api/storage/upload", UploadForm(ws.WorkspaceId, fileName, "x"));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.Equal(fileName, (await ReadJson(res)).GetProperty("name").GetString());
    }
}

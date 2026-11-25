using System.Net;
using System.Net.Http.Json;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

public class ChatTests : ApiTestBase
{
    // Seed: a "Vizsgaremek Fejlesztők" szoba tagjai: Bence, Dávid, Nóra
    private static readonly Guid SeedRoom = Guid.Parse("0E000000-0000-4000-8000-000000000002");
    private const string NoraEmail = TestData.OutsiderEmail;

    public ChatTests(ApiFactory factory) : base(factory) { }

    private async Task<(OwnedWorkspace ws, Guid roomId)> CreateGroupRoomAsync(params Guid[] memberIds)
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/chat/rooms",
            new { workspaceId = ws.WorkspaceId, name = "Csapat", type = 2, isPrivate = true, memberUserIds = memberIds });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (ws, (await ReadJson(res)).GetProperty("id").GetGuid());
    }

    // ---------- Szobák ----------

    [Fact]
    public async Task Rooms_List_ForMember_Returns200()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync($"/api/chat/rooms?workspaceId={TestData.Ws3}");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains((await ReadJson(res)).EnumerateArray(), r => r.GetProperty("id").GetGuid() == SeedRoom);
    }

    [Fact]
    public async Task Rooms_List_MissingWorkspaceId_Returns400()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync("/api/chat/rooms");

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Rooms_List_Anonymous_Returns401()
    {
        await AssertError(await Anonymous().GetAsync($"/api/chat/rooms?workspaceId={TestData.Ws3}"),
            HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task Rooms_Create_AsOwner_Returns201()
    {
        var (_, roomId) = await CreateGroupRoomAsync();

        Assert.NotEqual(Guid.Empty, roomId);
    }

    [Fact]
    public async Task Rooms_Create_EmptyWorkspaceId_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/chat/rooms",
            new { workspaceId = Guid.Empty, name = "x", type = 2 });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Rooms_Create_AsViewer_Returns403()
    {
        var res = await (await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1)).PostAsJsonAsync("/api/chat/rooms",
            new { workspaceId = TestData.Ws1, name = "Tiltott", type = 2 });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task DirectRoom_GetOrCreate_IsIdempotent()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var other = await RegisterUserAsync("dm");
        var client = WithToken(ws.Owner.Token);
        var url = $"/api/chat/rooms/direct?workspaceId={ws.WorkspaceId}&targetUserId={other.Id}";

        var first = await client.PostAsync(url, null);
        var second = await client.PostAsync(url, null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal((await ReadJson(first)).GetProperty("id").GetGuid(), (await ReadJson(second)).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task DirectRoom_WithSelf_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Owner.Token).PostAsync(
            $"/api/chat/rooms/direct?workspaceId={ws.WorkspaceId}&targetUserId={ws.Owner.Id}", null);

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task DirectRoom_MissingParameters_Returns400()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var res = await WithToken(ws.Owner.Token).PostAsync($"/api/chat/rooms/direct?workspaceId={ws.WorkspaceId}", null);

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    // ---------- Üzenetek ----------

    [Fact]
    public async Task Messages_History_ForMember_Returns200()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync($"/api/chat/rooms/{SeedRoom}/messages");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Messages_History_ForNonMember_Returns403()
    {
        var res = await (await UserAsync(TestData.ViewerEmail)).GetAsync($"/api/chat/rooms/{SeedRoom}/messages");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Messages_History_InvalidPaging_Returns400()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync($"/api/chat/rooms/{SeedRoom}/messages?page=0");

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Messages_Send_ThenReadBack()
    {
        var (ws, roomId) = await CreateGroupRoomAsync();
        var owner = WithToken(ws.Owner.Token);

        var send = await owner.PostAsJsonAsync($"/api/chat/rooms/{roomId}/messages", new { roomId, messageText = "Szia mindenki!" });
        Assert.Equal(HttpStatusCode.Created, send.StatusCode);
        Assert.Equal("Szia mindenki!", (await ReadJson(send)).GetProperty("messageText").GetString());

        var history = await ReadJson(await owner.GetAsync($"/api/chat/rooms/{roomId}/messages"));
        Assert.Contains(history.EnumerateArray(), m => m.GetProperty("messageText").GetString() == "Szia mindenki!");
    }

    [Fact]
    public async Task Messages_Send_EmptyText_Returns400()
    {
        var (ws, roomId) = await CreateGroupRoomAsync();

        var res = await WithToken(ws.Owner.Token).PostAsJsonAsync($"/api/chat/rooms/{roomId}/messages", new { roomId, messageText = "" });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Messages_Send_AsNonMember_Returns403()
    {
        var res = await (await UserAsync(TestData.ViewerEmail)).PostAsJsonAsync($"/api/chat/rooms/{SeedRoom}/messages",
            new { roomId = SeedRoom, messageText = "Betolakodó" });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Messages_Send_Anonymous_Returns401()
    {
        var res = await Anonymous().PostAsJsonAsync($"/api/chat/rooms/{SeedRoom}/messages", new { roomId = SeedRoom, messageText = "x" });

        await AssertError(res, HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task Messages_Delete_OwnMessage_Returns204_ThenReturns404()
    {
        var (ws, roomId) = await CreateGroupRoomAsync();
        var owner = WithToken(ws.Owner.Token);
        var sent = await owner.PostAsJsonAsync($"/api/chat/rooms/{roomId}/messages", new { roomId, messageText = "Törlendő" });
        var messageId = (await ReadJson(sent)).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/chat/messages/{messageId}")).StatusCode);
        await AssertError(await owner.DeleteAsync($"/api/chat/messages/{messageId}"), HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Messages_Delete_OthersMessage_Returns403()
    {
        var dave = await UserAsync(TestData.DevEmail);
        var sent = await dave.PostAsJsonAsync($"/api/chat/rooms/{SeedRoom}/messages", new { roomId = SeedRoom, messageText = "Dávid üzenete" });
        Assert.Equal(HttpStatusCode.Created, sent.StatusCode);
        var messageId = (await ReadJson(sent)).GetProperty("id").GetGuid();

        var res = await (await UserAsync(NoraEmail)).DeleteAsync($"/api/chat/messages/{messageId}");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Messages_Delete_Unknown_Returns404()
    {
        var res = await (await UserAsync(TestData.DevEmail)).DeleteAsync($"/api/chat/messages/{TestData.Missing}");

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }
}

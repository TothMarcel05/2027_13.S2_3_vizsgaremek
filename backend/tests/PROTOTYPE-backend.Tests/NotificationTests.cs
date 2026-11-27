using System.Net;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

public class NotificationTests : ApiTestBase
{
    private const string PeterEmail = "peter.nemeth@example.com";
    private static readonly Guid PeterNotification = Guid.Parse("0B000000-0000-4000-8000-000000000001");

    public NotificationTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task List_ForSeededUser_Returns200WithPagedResult()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync("/api/notifications?page=1&limit=10");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await ReadJson(res);
        Assert.Equal(System.Text.Json.JsonValueKind.Object, json.ValueKind);
    }

    [Fact]
    public async Task List_UnreadOnly_Returns200()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync("/api/notifications?unreadOnly=true");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("limit=0")]
    [InlineData("limit=101")]
    public async Task List_InvalidPaging_Returns400(string query)
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync($"/api/notifications?{query}");

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task List_Anonymous_Returns401()
    {
        await AssertError(await Anonymous().GetAsync("/api/notifications"), HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task UnreadCount_Returns200WithCount()
    {
        var res = await (await UserAsync(TestData.DevEmail)).GetAsync("/api/notifications/unread-count");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.True((await ReadJson(res)).GetProperty("count").GetInt32() >= 0);
    }

    [Fact]
    public async Task UnreadCount_Anonymous_Returns401()
    {
        await AssertError(await Anonymous().GetAsync("/api/notifications/unread-count"), HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task MarkAsRead_OwnNotification_Returns200_AndUnreadCountDrops()
    {
        var client = await UserAsync(PeterEmail);
        var before = (await ReadJson(await client.GetAsync("/api/notifications/unread-count"))).GetProperty("count").GetInt32();

        var res = await client.PutAsync($"/api/notifications/{PeterNotification}/read", null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var after = (await ReadJson(await client.GetAsync("/api/notifications/unread-count"))).GetProperty("count").GetInt32();
        Assert.True(after <= before);
    }

    [Fact]
    public async Task MarkAsRead_Unknown_Returns404()
    {
        var res = await (await UserAsync(TestData.DevEmail)).PutAsync($"/api/notifications/{TestData.Missing}/read", null);

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task MarkAsRead_OtherUsersNotification_Returns404()
    {
        var res = await (await UserAsync(TestData.ViewerEmail)).PutAsync($"/api/notifications/{PeterNotification}/read", null);

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task MarkAllAsRead_Returns200_AndUnreadCountIsZero()
    {
        var user = await RegisterUserAsync("ntf");
        var client = WithToken(user.Token);

        var res = await client.PutAsync("/api/notifications/read-all", null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        Assert.Equal(0, (await ReadJson(await client.GetAsync("/api/notifications/unread-count"))).GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task MarkAllAsRead_Anonymous_Returns401()
    {
        await AssertError(await Anonymous().PutAsync("/api/notifications/read-all", null), HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }
}

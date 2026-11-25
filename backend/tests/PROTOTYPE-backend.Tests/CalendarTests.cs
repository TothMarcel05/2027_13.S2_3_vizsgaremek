using System.Net;
using System.Net.Http.Json;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

public class CalendarTests : ApiTestBase
{
    public CalendarTests(ApiFactory factory) : base(factory) { }

    private static object PersonalEvent(string title = "Személyes esemény") => new
    {
        title,
        description = "leírás",
        location = "Iroda",
        startTime = "2031-06-01T09:00:00",
        endTime = "2031-06-01T10:00:00",
        isAllDay = false
    };

    private async Task<Guid> CreatePersonalAsync(HttpClient client)
    {
        var res = await client.PostAsJsonAsync("/api/calendar", PersonalEvent());
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await ReadJson(res)).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Create_PersonalEvent_Returns201()
    {
        var user = await RegisterUserAsync("cal");
        var res = await WithToken(user.Token).PostAsJsonAsync("/api/calendar", PersonalEvent("Fogorvos"));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var json = await ReadJson(res);
        Assert.Equal("Fogorvos", json.GetProperty("title").GetString());
        Assert.Equal(user.Id, json.GetProperty("createdBy").GetGuid());
    }

    [Fact]
    public async Task Create_EndBeforeStart_Returns400()
    {
        var user = await RegisterUserAsync("cal");
        var res = await WithToken(user.Token).PostAsJsonAsync("/api/calendar",
            new { title = "Hibás", startTime = "2031-06-01T10:00:00", endTime = "2031-06-01T09:00:00" });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Create_TooShortTitle_Returns400()
    {
        var user = await RegisterUserAsync("cal");
        var res = await WithToken(user.Token).PostAsJsonAsync("/api/calendar", PersonalEvent("ab"));

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Create_Anonymous_Returns401()
    {
        await AssertError(await Anonymous().PostAsJsonAsync("/api/calendar", PersonalEvent()),
            HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }

    [Fact]
    public async Task Create_ProjectEvent_AsOwner_Returns201_AsViewerReturns403()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var projectId = await CreateProjectAsync(ws);
        var body = new
        {
            projectId,
            title = "Projekt megbeszélés",
            startTime = "2031-06-02T09:00:00",
            endTime = "2031-06-02T10:00:00"
        };

        Assert.Equal(HttpStatusCode.Created, (await WithToken(ws.Token).PostAsJsonAsync("/api/calendar", body)).StatusCode);

        var viewer = await UserInWorkspaceAsync(TestData.ViewerEmail, TestData.Ws1);
        var forbidden = await viewer.PostAsJsonAsync("/api/calendar", new
        {
            projectId = TestData.Project1,
            title = "Tiltott esemény",
            startTime = "2031-06-02T09:00:00",
            endTime = "2031-06-02T10:00:00"
        });
        await AssertError(forbidden, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Get_OwnEvent_Returns200_OtherUserReturns403_UnknownReturns404()
    {
        var owner = await RegisterUserAsync("cal");
        var stranger = await RegisterUserAsync("cal");
        var id = await CreatePersonalAsync(WithToken(owner.Token));

        Assert.Equal(HttpStatusCode.OK, (await WithToken(owner.Token).GetAsync($"/api/calendar/{id}")).StatusCode);
        await AssertError(await WithToken(stranger.Token).GetAsync($"/api/calendar/{id}"), HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
        await AssertError(await WithToken(owner.Token).GetAsync($"/api/calendar/{TestData.Missing}"), HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Get_ProjectEvent_AsSystemAdmin_Returns200()
    {
        var res = await (await AdminAsync()).GetAsync("/api/calendar/11000000-0000-4000-8000-000000000001");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Get_ProjectEvent_AsNonProjectMember_Returns403()
    {
        var res = await (await UserAsync(TestData.OutsiderEmail)).GetAsync("/api/calendar/11000000-0000-4000-8000-000000000001");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Update_OwnEvent_Returns200()
    {
        var user = await RegisterUserAsync("cal");
        var client = WithToken(user.Token);
        var id = await CreatePersonalAsync(client);

        var res = await client.PutAsJsonAsync($"/api/calendar/{id}", new
        {
            title = "Módosított esemény",
            startTime = "2031-06-03T09:00:00",
            endTime = "2031-06-03T11:00:00",
            isAllDay = true
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var json = await ReadJson(res);
        Assert.Equal("Módosított esemény", json.GetProperty("title").GetString());
        Assert.True(json.GetProperty("isAllDay").GetBoolean());
    }

    [Fact]
    public async Task Update_Unknown_Returns404()
    {
        var user = await RegisterUserAsync("cal");
        var res = await WithToken(user.Token).PutAsJsonAsync($"/api/calendar/{TestData.Missing}", new
        {
            title = "Valami",
            startTime = "2031-06-03T09:00:00",
            endTime = "2031-06-03T11:00:00",
            isAllDay = false
        });

        await AssertError(res, HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Update_EndBeforeStart_Returns400()
    {
        var user = await RegisterUserAsync("cal");
        var client = WithToken(user.Token);
        var id = await CreatePersonalAsync(client);

        var res = await client.PutAsJsonAsync($"/api/calendar/{id}", new
        {
            title = "Hibás idő",
            startTime = "2031-06-03T12:00:00",
            endTime = "2031-06-03T11:00:00",
            isAllDay = false
        });

        await AssertError(res, HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Update_OthersPersonalEvent_Returns403()
    {
        var owner = await RegisterUserAsync("cal");
        var stranger = await RegisterUserAsync("cal");
        var id = await CreatePersonalAsync(WithToken(owner.Token));

        var res = await WithToken(stranger.Token).PutAsJsonAsync($"/api/calendar/{id}", new
        {
            title = "Átvett esemény",
            startTime = "2031-06-03T09:00:00",
            endTime = "2031-06-03T11:00:00",
            isAllDay = false
        });

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Delete_OwnEvent_Returns204_ThenReturns404()
    {
        var user = await RegisterUserAsync("cal");
        var client = WithToken(user.Token);
        var id = await CreatePersonalAsync(client);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/calendar/{id}")).StatusCode);
        await AssertError(await client.DeleteAsync($"/api/calendar/{id}"), HttpStatusCode.NotFound, "NEM_TALALHATO");
    }

    [Fact]
    public async Task Delete_OthersPersonalEvent_Returns403()
    {
        var owner = await RegisterUserAsync("cal");
        var stranger = await RegisterUserAsync("cal");
        var id = await CreatePersonalAsync(WithToken(owner.Token));

        await AssertError(await WithToken(stranger.Token).DeleteAsync($"/api/calendar/{id}"),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Range_ReturnsOnlyEventsInInterval()
    {
        var user = await RegisterUserAsync("cal");
        var client = WithToken(user.Token);
        var id = await CreatePersonalAsync(client);

        var inside = await client.GetAsync("/api/calendar?start=2031-06-01T00:00:00&end=2031-06-30T23:59:59");
        Assert.Equal(HttpStatusCode.OK, inside.StatusCode);
        Assert.Contains((await ReadJson(inside)).EnumerateArray(), e => e.GetProperty("id").GetGuid() == id);

        var outside = await client.GetAsync("/api/calendar?start=2032-01-01T00:00:00&end=2032-01-31T23:59:59");
        Assert.Equal(HttpStatusCode.OK, outside.StatusCode);
        Assert.DoesNotContain((await ReadJson(outside)).EnumerateArray(), e => e.GetProperty("id").GetGuid() == id);
    }

    [Fact]
    public async Task Range_MissingDates_Returns400()
    {
        var user = await RegisterUserAsync("cal");

        await AssertError(await WithToken(user.Token).GetAsync("/api/calendar"), HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Range_EndBeforeStart_Returns400()
    {
        var user = await RegisterUserAsync("cal");

        await AssertError(await WithToken(user.Token).GetAsync("/api/calendar?start=2031-06-30T00:00:00&end=2031-06-01T00:00:00"),
            HttpStatusCode.BadRequest, "ERVENYTELEN_KERES");
    }

    [Fact]
    public async Task Range_ForeignProject_Returns403()
    {
        var user = await RegisterUserAsync("cal");

        var res = await WithToken(user.Token).GetAsync(
            $"/api/calendar?start=2031-06-01T00:00:00&end=2031-06-30T00:00:00&projectId={TestData.Project1}");

        await AssertError(res, HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");
    }

    [Fact]
    public async Task Range_Anonymous_Returns401()
    {
        await AssertError(await Anonymous().GetAsync("/api/calendar?start=2031-06-01&end=2031-06-02"),
            HttpStatusCode.Unauthorized, "NINCS_HITELESITVE");
    }
}

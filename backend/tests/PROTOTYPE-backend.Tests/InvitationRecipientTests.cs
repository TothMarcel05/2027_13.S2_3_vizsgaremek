using System.Net;
using System.Net.Http.Json;
using PROTOTYPE_backend.Tests.Infrastructure;
using Xunit;

namespace PROTOTYPE_backend.Tests;

/// <summary>Meghívót csak a meghívott e-mail címhez tartozó fiók fogadhat el vagy utasíthat el.</summary>
public class InvitationRecipientTests : ApiTestBase
{
    public InvitationRecipientTests(ApiFactory factory) : base(factory) { }

    private async Task<string> InviteAsync(OwnedWorkspace ws, int type, Guid targetId, string email, Guid roleId)
    {
        var res = await WithToken(ws.Token).PostAsJsonAsync("/api/invitations",
            new { type, targetId, invitedEmail = email, roleId });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await ReadJson(res)).GetProperty("token").GetString()!;
    }

    private async Task<bool> IsMemberAsync(OwnedWorkspace ws, Guid userId)
    {
        var members = await ReadJson(await WithToken(ws.Token).GetAsync($"/api/workspaces/{ws.WorkspaceId}/members"));
        return members.EnumerateArray().Any(m => m.GetProperty("memberId").GetGuid() == userId);
    }

    [Theory]
    [InlineData(1, "accept")]
    [InlineData(1, "refuse")]
    [InlineData(2, "accept")]
    [InlineData(2, "refuse")]
    public async Task OtherAccount_CannotRespond_AndInvitationStaysUsable(int type, string action)
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var targetId = type == 1 ? ws.WorkspaceId : await CreateProjectAsync(ws);
        var invited = await RegisterUserAsync("inv");
        var other = await RegisterUserAsync("oth");
        var token = await InviteAsync(ws, type, targetId, invited.Email, TestData.RoleViewer);

        await AssertError(await WithToken(other.Token).PostAsync($"/api/invitations/{action}/{token}", null),
            HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");

        Assert.False(await IsMemberAsync(ws, other.Id));
        Assert.Equal(HttpStatusCode.OK,
            (await WithToken(invited.Token).PostAsync($"/api/invitations/accept/{token}", null)).StatusCode);
    }

    [Fact]
    public async Task Admin_CannotClaimPendingOwnerInvitationWithAnotherAccount()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var admin = await RegisterUserAsync("adm");
        var adminToken = await AddMemberAsync(ws, admin, TestData.RoleAdmin);
        const string email = "leendo.tulajdonos@test.example.com";
        await InviteAsync(ws, 1, ws.WorkspaceId, email, TestData.RoleOwner);
        var pending = await ReadJson(await WithToken(adminToken)
            .GetAsync($"/api/invitations/target/{ws.WorkspaceId}?type=1"));
        var seen = pending.EnumerateArray().First(i => i.GetProperty("invitedEmail").GetString() == email)
            .GetProperty("token").GetString();
        var second = await RegisterUserAsync("alt");

        foreach (var client in new[] { WithToken(second.Token), WithToken(adminToken), await AdminAsync() })
            await AssertError(await client.PostAsync($"/api/invitations/accept/{seen}", null),
                HttpStatusCode.Forbidden, "NINCS_JOGOSULTSAG");

        Assert.False(await IsMemberAsync(ws, second.Id));
    }

    [Fact]
    public async Task EmailMatch_IsCaseInsensitive()
    {
        var ws = await CreateOwnerWithWorkspaceAsync();
        var invited = await RegisterUserAsync("case");
        var token = await InviteAsync(ws, 1, ws.WorkspaceId, $"  {invited.Email.ToUpperInvariant()}  ".Trim(), TestData.RoleViewer);

        Assert.Equal(HttpStatusCode.OK,
            (await WithToken(invited.Token).PostAsync($"/api/invitations/accept/{token}", null)).StatusCode);
        Assert.True(await IsMemberAsync(ws, invited.Id));
    }
}

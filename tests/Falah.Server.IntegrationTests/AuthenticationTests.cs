namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Treasury;
using Xunit;

/// <summary>
/// The server used to take the acting user's identity from the request itself - an actorUserId the
/// caller writes - and the user list was readable without logging in. Anyone who could reach the API
/// could read the manager's id and pass every permission check. The PIN screen in the client was the
/// only thing in the way, and a screen guards nothing a caller can skip.
///
/// These tests use a RAW client with no token handler attached, so they exercise what an outsider
/// actually gets.
/// </summary>
public class AuthenticationTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _raw = default!;
    private string _managerId = default!;
    private string _cashierId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        // Deliberately NOT CreateAuthenticatedClient: this is the outsider's view.
        _raw = _factory.CreateClient();
        _managerId = await _factory.SeedUserAsync("مدير", "manager", "all");
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private HttpClient AuthedAs(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId));
        return client;
    }

    [Fact]
    public async Task The_user_list_is_not_readable_without_logging_in()
    {
        // This was the first step of the whole attack: read the list, take the manager's id.
        var response = await _raw.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Naming_a_manager_without_holding_their_session_gets_nowhere()
    {
        var response = await _raw.GetAsync($"/api/reports/owner-dashboard?actorUserId={_managerId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_forged_token_is_refused()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", new string('A', 64));

        var response = await client.GetAsync($"/api/reports/owner-dashboard?actorUserId={_managerId}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_cashier_cannot_act_under_the_managers_name()
    {
        // The token is real and the session is valid - it just is not the manager's. This is the
        // difference between "you may not do this" and "you are not who you say you are", and the
        // second one has to be caught before any permission check runs.
        var cashier = AuthedAs(_cashierId);

        var read = await cashier.GetAsync($"/api/reports/owner-dashboard?actorUserId={_managerId}");
        Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);

        var write = await cashier.PostAsJsonAsync("/api/treasury/withdraw",
            new WithdrawalRequest($"wd-{Guid.NewGuid():N}", 100, "tre-main", "سحب", null, _managerId, null, null));
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
    }

    [Fact]
    public async Task Acting_under_your_own_name_still_reaches_the_ordinary_permission_check()
    {
        // The gate must not swallow authorisation: a cashier acting as themselves is authenticated
        // fine and then refused for the real reason.
        var response = await AuthedAs(_cashierId).GetAsync($"/api/reports/owner-dashboard?actorUserId={_cashierId}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Logging_out_stops_the_token_working_immediately()
    {
        var client = AuthedAs(_managerId);
        (await client.GetAsync($"/api/reports/owner-dashboard?actorUserId={_managerId}")).EnsureSuccessStatusCode();

        (await client.PostAsync("/api/auth/logout", content: null)).EnsureSuccessStatusCode();

        var after = await client.GetAsync($"/api/reports/owner-dashboard?actorUserId={_managerId}");
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    [Fact]
    public async Task A_manager_approval_now_needs_the_managers_actual_pin()
    {
        // The approval endpoint used to name its approver, so a cashier could mint a manager
        // approval for themselves without knowing any manager's PIN.
        var cashier = AuthedAs(_cashierId);

        var forged = await cashier.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest("000000", "orders_comp", "محاولة"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, forged.StatusCode);

        var real = await cashier.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "orders_comp", "ضيافة"));
        real.EnsureSuccessStatusCode();

        var result = await real.Content.ReadFromJsonAsync<IssueManagerApprovalResult>();
        // The server decided who approved, from the PIN - the caller never said.
        Assert.Equal(_managerId, result!.ApproverUserId);
    }

    [Fact]
    public async Task A_cashiers_pin_cannot_approve_something_a_cashier_may_not_do()
    {
        var response = await AuthedAs(_cashierId).PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_cashierId), "treasury_manage", "محاولة"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Logging_in_returns_a_token_and_health_stays_open()
    {
        // Health is how a station checks it can reach the server before anyone has logged in.
        (await _raw.GetAsync("/health")).EnsureSuccessStatusCode();

        var login = await _raw.PostAsJsonAsync("/api/auth/login-by-pin",
            new Falah.Application.Users.LoginByPinRequest(_factory.PinFor(_managerId)));
        login.EnsureSuccessStatusCode();

        var result = await login.Content.ReadFromJsonAsync<Falah.Application.Users.LoginResultDto>();
        Assert.Equal(_managerId, result!.Id);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }
}

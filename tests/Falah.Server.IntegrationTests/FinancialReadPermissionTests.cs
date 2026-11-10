namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Falah.Application.Shifts;
using Xunit;

/// <summary>
/// SEC6. Money a screen hides must not be one request away. The tills only show treasury balances to
/// treasury_manage / reports_view holders, but the server answered any session: a cashier could read
/// the drawer balance - the expected cash a blind count must not know - or the main treasury, the
/// counted cash of every handover, a closed day's frozen reconciliation, its cash counts and
/// exceptions, and the security audit trail. Each now needs the permission its screen needs.
///
/// FIN5. The close preview shows the difference after the count, and a cashier may count again (a
/// miscount happens); every count typed is now recorded so the manager sees them all.
/// </summary>
public class FinancialReadPermissionTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _controllerId = default!;
    private string _treasurerId = default!;
    private string _dayId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _managerId = await _factory.SeedUserAsync("مدير", "manager", "reports_view", "shifts_manage", "users_manage");
        _controllerId = await _factory.SeedUserAsync("مراقب اليوم", "supervisor", "control_center");
        _treasurerId = await _factory.SeedUserAsync("أمين الخزينة", "supervisor", "treasury_manage");
        _dayId = await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private HttpClient As(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId));
        return client;
    }

    [Theory]
    [InlineData("tre-drawer")]
    [InlineData("tre-main")]
    [InlineData("tre-bank")]
    public async Task A_till_cannot_read_a_treasury_balance(string account)
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await As(_cashierId).GetAsync($"/api/treasury/balance?accountId={account}")).StatusCode);
    }

    [Fact]
    public async Task Those_whose_screens_show_balances_still_read_them()
    {
        Assert.Equal(HttpStatusCode.OK, (await As(_managerId).GetAsync("/api/treasury/balance?accountId=tre-drawer")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await As(_treasurerId).GetAsync("/api/treasury/balance?accountId=tre-main")).StatusCode);
    }

    [Fact]
    public async Task A_till_cannot_read_the_handover_history()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await As(_cashierId).GetAsync("/api/treasury/handovers?limit=10")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await As(_managerId).GetAsync("/api/treasury/handovers?limit=10")).StatusCode);
    }

    [Theory]
    [InlineData("close-blockers")]
    [InlineData("close-snapshots")]
    [InlineData("checklist")]
    [InlineData("cash-counts")]
    [InlineData("exceptions")]
    public async Task A_till_cannot_read_the_control_center_figures_of_a_day(string section)
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await As(_cashierId).GetAsync($"/api/business-day/{_dayId}/{section}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await As(_controllerId).GetAsync($"/api/business-day/{_dayId}/{section}")).StatusCode);
    }

    [Fact]
    public async Task A_till_cannot_read_a_closed_days_snapshot()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await As(_cashierId).GetAsync($"/api/business-day/{_dayId}/close-snapshot")).StatusCode);
        // No snapshot yet for an open day: an allowed reader is told so.
        Assert.Equal(HttpStatusCode.NotFound, (await As(_managerId).GetAsync($"/api/business-day/{_dayId}/close-snapshot")).StatusCode);
    }

    [Fact]
    public async Task A_till_cannot_read_the_security_and_control_audit_trail()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await As(_cashierId).GetAsync("/api/control-center/audit-events")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await As(_managerId).GetAsync("/api/control-center/audit-events")).StatusCode);
    }

    [Fact]
    public async Task Every_cash_count_typed_for_a_close_is_recorded_in_order_for_the_manager()
    {
        var till = As(_cashierId);
        var shift = await (await till.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();

        (await till.PostAsJsonAsync("/api/shifts/prepare-close", new PrepareShiftCloseRequest(shift!.ShiftId, 40000))).EnsureSuccessStatusCode();
        (await till.PostAsJsonAsync("/api/shifts/prepare-close", new PrepareShiftCloseRequest(shift.ShiftId, 50000))).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await till.GetAsync($"/api/shifts/{shift.ShiftId}/cash-declarations")).StatusCode);
        var declarations = (await As(_managerId).GetFromJsonAsync<List<ShiftCashDeclarationDto>>($"/api/shifts/{shift.ShiftId}/cash-declarations"))!;
        Assert.Equal(new long[] { 40000, 50000 }, declarations.Select(d => d.CountedPiastres));
        Assert.Equal(new[] { 1, 2 }, declarations.Select(d => d.Sequence));
        Assert.All(declarations, d => Assert.Equal("كاشير", d.DeclaredBy));
    }
}

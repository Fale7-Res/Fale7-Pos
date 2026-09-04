namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.CashDrawer;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Ported from the 'cash_in'/'cash_out' cases of addCashMovement in lib/store.tsx - a real gap
/// found during the legacy-parity audit: this cashier-level drawer-vs-main-treasury transfer had
/// no C# equivalent at all, even though ShiftReconciliation.ComputeExpectedCashAsync already read
/// Treasury movements of exactly this type for blind-close expected-cash math.
/// </summary>
public class CashierCashMovementVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "cash_drawer");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private CashierCashMovementRequest Movement(string type, long amount, string? requestId = null) =>
        new(requestId ?? $"cm-{Guid.NewGuid():N}", _shiftId, type, amount, "سبب اختباري", _cashierId, null, null);

    [Fact]
    public async Task A_cash_in_posts_exactly_one_movement_of_type_cash_in()
    {
        var response = await _client.PostAsJsonAsync("/api/cash-drawer/cash-movement", Movement("cash_in", 5000));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CashierCashMovementResult>();
        Assert.False(result!.WasReplayed);
        Assert.Equal("cash_in", result.MovementType);

        var count = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ShiftId == _shiftId && m.Type == "cash_in"));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task A_cash_in_moves_money_from_main_to_drawer()
    {
        var mainBefore = await _factory.QueryAsync(async db =>
            (await db.TreasuryMovements.Where(m => m.DestinationAccountId == "tre-main").SumAsync(m => (long?)m.AmountPiastres) ?? 0)
            - await db.TreasuryMovements.Where(m => m.SourceAccountId == "tre-main").SumAsync(m => m.AmountPiastres));
        (await _client.PostAsJsonAsync("/api/cash-drawer/cash-movement", Movement("cash_in", 5000))).EnsureSuccessStatusCode();
        var movement = await _factory.QueryAsync(db => db.TreasuryMovements.FirstAsync(m => m.ShiftId == _shiftId && m.Type == "cash_in"));
        Assert.Equal("tre-main", movement.SourceAccountId);
        Assert.Equal("tre-drawer", movement.DestinationAccountId);
        Assert.Equal(5000, movement.AmountPiastres);
    }

    [Fact]
    public async Task A_cash_out_moves_money_from_drawer_to_main()
    {
        (await _client.PostAsJsonAsync("/api/cash-drawer/cash-movement", Movement("cash_out", 5000))).EnsureSuccessStatusCode();
        var movement = await _factory.QueryAsync(db => db.TreasuryMovements.FirstAsync(m => m.ShiftId == _shiftId && m.Type == "cash_out"));
        Assert.Equal("tre-drawer", movement.SourceAccountId);
        Assert.Equal("tre-main", movement.DestinationAccountId);
    }

    [Fact]
    public async Task An_unknown_movement_type_is_rejected()
    {
        var response = await _client.PostAsJsonAsync("/api/cash-drawer/cash-movement", Movement("cash_sideways", 5000));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_cash_movement_requires_the_cash_drawer_permission()
    {
        var noPermissionUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");
        var response = await _client.PostAsJsonAsync("/api/cash-drawer/cash-movement",
            new CashierCashMovementRequest($"cm-{Guid.NewGuid():N}", _shiftId, "cash_in", 5000, "سبب", noPermissionUser, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_cash_movement_requires_the_shift_to_be_open()
    {
        (await _client.PostAsJsonAsync("/api/shifts/close",
            new Falah.Application.Shifts.CloseShiftRequest($"creq-{Guid.NewGuid():N}", _shiftId, _cashierId, 50000, null, null, null))).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("/api/cash-drawer/cash-movement", Movement("cash_in", 5000));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_cash_out_larger_than_the_drawers_balance_is_rejected()
    {
        var response = await _client.PostAsJsonAsync("/api/cash-drawer/cash-movement", Movement("cash_out", 999_999));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Retrying_the_same_cash_movement_request_id_never_posts_twice()
    {
        var requestId = $"cm-{Guid.NewGuid():N}";
        var first = await (await _client.PostAsJsonAsync("/api/cash-drawer/cash-movement", Movement("cash_in", 5000, requestId)))
            .Content.ReadFromJsonAsync<CashierCashMovementResult>();
        var second = await (await _client.PostAsJsonAsync("/api/cash-drawer/cash-movement", Movement("cash_in", 5000, requestId)))
            .Content.ReadFromJsonAsync<CashierCashMovementResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        var count = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ShiftId == _shiftId && m.Type == "cash_in"));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Concurrent_cash_in_requests_with_the_same_request_id_post_exactly_once()
    {
        var requestId = $"cm-{Guid.NewGuid():N}";
        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/cash-drawer/cash-movement", Movement("cash_in", 5000, requestId)));
        var responses = await Task.WhenAll(tasks);
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));

        var count = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ShiftId == _shiftId && m.Type == "cash_in"));
        Assert.Equal(1, count);
    }

    // The whole point of this feature: ShiftReconciliation.ComputeExpectedCashAsync already read
    // 'cash_in'/'cash_out' movement types before this handler existed to ever write them.
    [Fact]
    public async Task A_cash_in_increases_the_shifts_expected_drawer_cash_and_cash_out_decreases_it()
    {
        var baseline = await (await _client.PostAsJsonAsync("/api/shifts/prepare-close",
            new PrepareShiftCloseRequest(_shiftId, 50000))).Content.ReadFromJsonAsync<PrepareShiftCloseResult>();
        Assert.Equal(50000, baseline!.ExpectedCashPiastres);

        (await _client.PostAsJsonAsync("/api/cash-drawer/cash-movement", Movement("cash_in", 5000))).EnsureSuccessStatusCode();
        var afterIn = await (await _client.PostAsJsonAsync("/api/shifts/prepare-close",
            new PrepareShiftCloseRequest(_shiftId, 55000))).Content.ReadFromJsonAsync<PrepareShiftCloseResult>();
        Assert.Equal(55000, afterIn!.ExpectedCashPiastres);

        (await _client.PostAsJsonAsync("/api/cash-drawer/cash-movement", Movement("cash_out", 2000))).EnsureSuccessStatusCode();
        var afterOut = await (await _client.PostAsJsonAsync("/api/shifts/prepare-close",
            new PrepareShiftCloseRequest(_shiftId, 53000))).Content.ReadFromJsonAsync<PrepareShiftCloseResult>();
        Assert.Equal(53000, afterOut!.ExpectedCashPiastres);
    }
}

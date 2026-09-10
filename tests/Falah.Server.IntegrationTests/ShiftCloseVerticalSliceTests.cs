namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// The blind-close vertical slice: prepare (read-only preview) then close (commits). Exercises the
/// exact ledger mechanics ported from the current app's closeShiftBlind (see CloseShiftHandler's
/// own doc comment) - reconciliation movement direction by shortage/overage, handover of the
/// ACTUAL counted amount, discrepancy-threshold approval gating, and idempotent replay.
/// </summary>
public class ShiftCloseVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> OpenShiftAsync(long openingCashPiastres = 50000) =>
        (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, openingCashPiastres)))
            .Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;

    private async Task<CheckoutOrderResult> CheckoutCashAsync(string shiftId, long totalPiastres)
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: totalPiastres, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    private CloseShiftRequest CloseRequest(string shiftId, long actualPiastres, string requestId, string? reason = null, string? approvalId = null, string? approverId = null) =>
        new(requestId, shiftId, _cashierId, actualPiastres, reason, approvalId, approverId);

    [Fact]
    public async Task Prepare_close_reports_expected_cash_including_cash_sales_from_this_shift_only()
    {
        var shiftId = await OpenShiftAsync(50000);
        await CheckoutCashAsync(shiftId, 20000);

        var response = await _client.PostAsJsonAsync("/api/shifts/prepare-close", new PrepareShiftCloseRequest(shiftId, 70000));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PrepareShiftCloseResult>();

        Assert.Equal(70000, result!.ExpectedCashPiastres); // 50000 opening + 20000 cash sale
        Assert.Equal(0, result.DifferencePiastres);
        Assert.False(result.RequiresManagerApproval);
    }

    [Fact]
    public async Task Closing_with_no_orders_and_matching_cash_needs_no_approval_and_posts_only_a_handover()
    {
        var shiftId = await OpenShiftAsync(50000);
        var response = await _client.PostAsJsonAsync("/api/shifts/close", CloseRequest(shiftId, 50000, $"creq-{Guid.NewGuid():N}"));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CloseShiftResult>();

        Assert.Equal("Closed", result!.Status);
        Assert.Equal(0, result.DifferencePiastres);
        Assert.False(result.WasReplayed);

        var reconciliationCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ShiftId == shiftId && m.ReferenceType == "shift_reconciliation"));
        Assert.Equal(0, reconciliationCount); // no difference -> no reconciliation movement at all

        var handover = await _factory.QueryAsync(db => db.TreasuryMovements.FirstAsync(m => m.ShiftId == shiftId && m.Type == "shift_handover"));
        Assert.Equal("tre-drawer", handover.SourceAccountId);
        Assert.Equal("tre-main", handover.DestinationAccountId);
        Assert.Equal(50000, handover.AmountPiastres);
    }

    [Fact]
    public async Task A_shortage_within_threshold_needs_no_approval_but_still_posts_a_reconciliation_movement_debiting_the_drawer()
    {
        var shiftId = await OpenShiftAsync(50000);
        // Expected 50000, actual 49000 -> shortage of 1000, well under the 5000-piastre default threshold.
        var response = await _client.PostAsJsonAsync("/api/shifts/close", CloseRequest(shiftId, 49000, $"creq-{Guid.NewGuid():N}"));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CloseShiftResult>();
        Assert.Equal(-1000, result!.DifferencePiastres);

        var reconciliation = await _factory.QueryAsync(db => db.TreasuryMovements.FirstAsync(m => m.ShiftId == shiftId && m.ReferenceType == "shift_reconciliation"));
        Assert.Equal("cash_shortage", reconciliation.Type);
        Assert.Equal("tre-drawer", reconciliation.SourceAccountId);
        Assert.Null(reconciliation.DestinationAccountId);
        Assert.Equal(1000, reconciliation.AmountPiastres);

        var handover = await _factory.QueryAsync(db => db.TreasuryMovements.FirstAsync(m => m.ShiftId == shiftId && m.Type == "shift_handover"));
        Assert.Equal(49000, handover.AmountPiastres); // the ACTUAL count, not the expected figure
    }

    [Fact]
    public async Task An_overage_credits_the_drawer_instead_of_debiting_it()
    {
        var shiftId = await OpenShiftAsync(50000);
        var response = await _client.PostAsJsonAsync("/api/shifts/close", CloseRequest(shiftId, 51000, $"creq-{Guid.NewGuid():N}"));
        response.EnsureSuccessStatusCode();

        var reconciliation = await _factory.QueryAsync(db => db.TreasuryMovements.FirstAsync(m => m.ShiftId == shiftId && m.ReferenceType == "shift_reconciliation"));
        Assert.Equal("cash_overage", reconciliation.Type);
        Assert.Null(reconciliation.SourceAccountId);
        Assert.Equal("tre-drawer", reconciliation.DestinationAccountId);
        Assert.Equal(1000, reconciliation.AmountPiastres);
    }

    [Fact]
    public async Task Closing_beyond_threshold_without_approval_is_rejected_and_the_shift_stays_open()
    {
        var shiftId = await OpenShiftAsync(50000);
        // Off by 10000 piastres (100 EGP), well beyond the 5000-piastre default threshold.
        var response = await _client.PostAsJsonAsync("/api/shifts/close", CloseRequest(shiftId, 40000, $"creq-{Guid.NewGuid():N}"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var shift = await _factory.QueryAsync(db => db.Shifts.AsNoTracking().FirstAsync(s => s.Id == shiftId));
        Assert.Equal("Open", shift.Status);
        // Exactly the one shift_opening_float movement from OpenShiftAsync itself - the rejected
        // close attempt must not have posted a reconciliation or handover movement on top of it.
        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ShiftId == shiftId));
        Assert.Equal(1, movementCount);
    }

    [Fact]
    public async Task Closing_beyond_threshold_with_a_valid_manager_approval_succeeds_and_records_the_approver()
    {
        var shiftId = await OpenShiftAsync(50000);
        var approval = (await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "shifts_manage", "فرق كبير")))
            .Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!;

        var response = await _client.PostAsJsonAsync("/api/shifts/close",
            CloseRequest(shiftId, 40000, $"creq-{Guid.NewGuid():N}", "عجز كبير - تم التحقق", approval.ApprovalId, _managerId));
        response.EnsureSuccessStatusCode();

        var shift = await _factory.QueryAsync(db => db.Shifts.AsNoTracking().FirstAsync(s => s.Id == shiftId));
        Assert.Equal("Closed", shift.Status);
        Assert.Equal("المدير", shift.DifferenceAuthorizedBy);
    }

    [Fact]
    public async Task Retrying_the_exact_same_close_request_never_posts_movements_twice()
    {
        var shiftId = await OpenShiftAsync(50000);
        var requestId = $"creq-{Guid.NewGuid():N}";
        var first = await (await _client.PostAsJsonAsync("/api/shifts/close", CloseRequest(shiftId, 49000, requestId))).Content.ReadFromJsonAsync<CloseShiftResult>();
        var second = await (await _client.PostAsJsonAsync("/api/shifts/close", CloseRequest(shiftId, 49000, requestId))).Content.ReadFromJsonAsync<CloseShiftResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(first.DifferencePiastres, second.DifferencePiastres);

        // shift_opening_float (from OpenShiftAsync) + reconciliation + handover - never doubled by the retry.
        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ShiftId == shiftId));
        Assert.Equal(3, movementCount);
    }

    [Fact]
    public async Task Concurrent_close_requests_with_the_same_idempotency_key_race_to_exactly_one_close()
    {
        var shiftId = await OpenShiftAsync(50000);
        var requestId = $"creq-{Guid.NewGuid():N}";
        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/shifts/close", CloseRequest(shiftId, 50000, requestId)));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<CloseShiftResult>()));
        Assert.Equal(1, results.Count(r => !r!.WasReplayed));

        var handoverCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ShiftId == shiftId && m.Type == "shift_handover"));
        Assert.Equal(1, handoverCount);
    }

    [Fact]
    public async Task Closing_an_already_closed_shift_under_a_new_request_id_is_rejected_not_silently_reclosed()
    {
        var shiftId = await OpenShiftAsync(50000);
        await _client.PostAsJsonAsync("/api/shifts/close", CloseRequest(shiftId, 50000, $"creq-{Guid.NewGuid():N}"));

        var second = await _client.PostAsJsonAsync("/api/shifts/close", CloseRequest(shiftId, 50000, $"creq-{Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);

        var handoverCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ShiftId == shiftId && m.Type == "shift_handover"));
        Assert.Equal(1, handoverCount);
    }

    // ---- Single system-wide active shift (a prerequisite for Call Center to unambiguously find
    // "the" open shift) ----

    [Fact]
    public async Task Opening_a_second_shift_while_one_is_already_open_is_rejected()
    {
        await OpenShiftAsync(50000);
        var second = await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "مسائية", _cashierId, 0));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Opening_a_shift_after_the_previous_one_closed_succeeds()
    {
        var shiftId = await OpenShiftAsync(50000);
        await _client.PostAsJsonAsync("/api/shifts/close", CloseRequest(shiftId, 50000, $"creq-{Guid.NewGuid():N}"));

        var second = await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "مسائية", _cashierId, 0));
        second.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Concurrent_open_attempts_for_different_shifts_let_exactly_one_through()
    {
        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 0)));
        var responses = await Task.WhenAll(tasks);

        Assert.Equal(1, responses.Count(r => r.IsSuccessStatusCode));
        var openCount = await _factory.QueryAsync(db => db.Shifts.CountAsync(s => s.Status == "Open"));
        Assert.Equal(1, openCount);
    }

    [Fact]
    public async Task Current_shift_endpoint_reports_the_open_shift_and_404s_when_none_is_open()
    {
        var none = await _client.GetAsync("/api/shifts/current");
        Assert.Equal(HttpStatusCode.NotFound, none.StatusCode);

        var shiftId = await OpenShiftAsync(50000);
        var current = await (await _client.GetAsync("/api/shifts/current")).Content.ReadFromJsonAsync<CurrentShiftDto>();
        Assert.Equal(shiftId, current!.ShiftId);

        await _client.PostAsJsonAsync("/api/shifts/close", CloseRequest(shiftId, 50000, $"creq-{Guid.NewGuid():N}"));
        var afterClose = await _client.GetAsync("/api/shifts/current");
        Assert.Equal(HttpStatusCode.NotFound, afterClose.StatusCode);
    }

    // Legacy-parity audit (2026-09-04): OpenShiftHandler had no permission check at all - any user
    // id could open a shift regardless of role. Fixed via AuthorizationPolicy.AuthorizeShiftOpen
    // (shifts_manage OR shifts_open_own, no approval fallback, matching openNewShift in store.tsx).
    [Fact]
    public async Task Opening_a_shift_without_shifts_manage_or_shifts_open_own_is_rejected()
    {
        var noPermissionUser = await _factory.SeedUserAsync("موظف بلا صلاحية", "cashier", "pos");
        var response = await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", noPermissionUser, 50000));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var count = await _factory.QueryAsync(db => db.Shifts.CountAsync(s => s.OpenedByUserId == noPermissionUser));
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task A_manager_holding_shifts_manage_but_not_shifts_open_own_can_still_open_a_shift()
    {
        var manager = await _factory.SeedUserAsync("مدير مناوبة", "manager", "shifts_manage");
        var response = await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", manager, 0));
        response.EnsureSuccessStatusCode();
    }
}

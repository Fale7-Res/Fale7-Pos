namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.BusinessDay;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Ported from closeBusinessDay/reopenBusinessDay/evaluateCloseBlockers/buildControlExceptions/
/// completeChecklistItem/setExceptionStatus in lib/enterprise-store.tsx + lib/control-layer-domain.mjs,
/// composed on top of the already-tested Falah.Domain.BusinessDay.BusinessDayRules and the new
/// Falah.Domain.ControlCenter.ControlCenterRules. Deliberately deferred out of the Treasury slice
/// (see BusinessDayEntity's own original note) as a distinctly separate, larger workflow. Covers
/// every close blocker this system can evidence (open shift, unsettled driver custody, pending
/// electronic payment, incomplete required checklist), the reopen/exception review paths (both
/// explicit and permissioned), idempotent replay, and full auditability - never posting or mutating
/// any financial fact itself.
/// </summary>
public class BusinessDayCloseVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _dispatcherId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _dispatcherId = await _factory.SeedUserAsync("موظف التوصيل", "dispatcher", "delivery_manage");
        // Funds tre-main WITHOUT opening a business day - these tests open their own business day
        // through the real endpoint under test, but shifts/purchases/etc. still draw opening floats
        // and other movements from tre-main.
        await _factory.FundMainAccountAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> OpenBusinessDayAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/business-day/open", new OpenBusinessDayRequest($"bd-{Guid.NewGuid():N}", _managerId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OpenBusinessDayResult>())!.BusinessDayId;
    }

    private async Task<string> OpenShiftAsync(long openingCashPiastres = 50000)
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/open", new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, openingCashPiastres));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    private Task<HttpResponseMessage> CloseShiftAsync(string shiftId, long actualPiastres) =>
        _client.PostAsJsonAsync("/api/shifts/close", new CloseShiftRequest($"creq-{Guid.NewGuid():N}", shiftId, _cashierId, actualPiastres, null, null, null));

    private Task<HttpResponseMessage> CompleteChecklistAsync(string businessDayId, string itemId, bool completed = true, string? actorId = null) =>
        _client.PostAsJsonAsync($"/api/business-day/{businessDayId}/checklist/{itemId}",
            new CompleteChecklistItemRequest(businessDayId, itemId, completed, actorId ?? _managerId));

    private async Task CompleteRequiredClosingChecklistAsync(string businessDayId)
    {
        foreach (var itemId in new[] { "close-shifts", "close-drawer", "close-delivery" })
            (await CompleteChecklistAsync(businessDayId, itemId)).EnsureSuccessStatusCode();
    }

    private Task<HttpResponseMessage> CloseBusinessDayAsync(string businessDayId, string? requestId = null, string? actorId = null) =>
        _client.PostAsJsonAsync($"/api/business-day/{businessDayId}/close",
            new CloseBusinessDayRequest(requestId ?? $"close-{Guid.NewGuid():N}", businessDayId, actorId ?? _managerId, null, null));

    private Task<HttpResponseMessage> ReopenBusinessDayAsync(string businessDayId, string reason, string? actorId = null, string? approvalId = null, string? approverId = null) =>
        _client.PostAsJsonAsync($"/api/business-day/{businessDayId}/reopen",
            new ReopenBusinessDayRequest($"reopen-{Guid.NewGuid():N}", businessDayId, reason, actorId ?? _managerId, approvalId, approverId));

    private async Task<string> IssueApprovalAsync(string action) =>
        (await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), action, "اعتماد اختبار"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!.ApprovalId;

    /// <summary>A day with all shifts closed and the required closing checklist complete - the
    /// minimal "should be closeable" baseline every blocker-specific test starts from, then adds
    /// exactly the one condition it means to prove blocks (or doesn't).</summary>
    private async Task<string> CleanCloseableDayAsync()
    {
        var dayId = await OpenBusinessDayAsync();
        var shiftId = await OpenShiftAsync();
        (await CloseShiftAsync(shiftId, 50000)).EnsureSuccessStatusCode();
        await CompleteRequiredClosingChecklistAsync(dayId);
        return dayId;
    }

    // ---- Open: the real permission gap found while wiring close/reopen next to it ----

    [Fact]
    public async Task Opening_a_business_day_without_business_day_open_permission_is_rejected()
    {
        var noPermissionUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");
        var response = await _client.PostAsJsonAsync("/api/business-day/open", new OpenBusinessDayRequest($"bd-{Guid.NewGuid():N}", noPermissionUser));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---- Close blockers ----

    [Fact]
    public async Task Closed_days_remain_discoverable_for_review_and_reopen_after_current_day_returns_not_found()
    {
        var dayId = await CleanCloseableDayAsync();
        (await CloseBusinessDayAsync(dayId)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/business-day/current")).StatusCode);
        var days = await _client.GetFromJsonAsync<List<BusinessDayDto>>("/api/business-days");
        var closed = Assert.Single(days!);
        Assert.Equal(dayId, closed.Id);
        Assert.Equal("Closed", closed.Status);
        Assert.NotNull(closed.SnapshotId);
        (await ReopenBusinessDayAsync(closed.Id, "مراجعة اليوم المغلق")).EnsureSuccessStatusCode();
        var reopened = Assert.Single((await _client.GetFromJsonAsync<List<BusinessDayDto>>("/api/business-days"))!);
        Assert.Equal(dayId, reopened.Id);
        Assert.Equal("Reopened", reopened.Status);
    }

    [Fact]
    public async Task An_open_shift_blocks_business_day_close()
    {
        var dayId = await OpenBusinessDayAsync();
        await OpenShiftAsync(); // deliberately never closed
        await CompleteRequiredClosingChecklistAsync(dayId);

        var response = await CloseBusinessDayAsync(dayId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BlockedResponse>();
        Assert.Contains(body!.blockers, b => b.Code == "OPEN_SHIFT");
    }

    [Fact]
    public async Task An_incomplete_required_checklist_item_blocks_close()
    {
        var dayId = await OpenBusinessDayAsync();
        var shiftId = await OpenShiftAsync();
        (await CloseShiftAsync(shiftId, 50000)).EnsureSuccessStatusCode();
        // Deliberately complete only 2 of the 3 required closing items.
        await CompleteChecklistAsync(dayId, "close-shifts");
        await CompleteChecklistAsync(dayId, "close-drawer");

        var response = await CloseBusinessDayAsync(dayId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BlockedResponse>();
        Assert.Contains(body!.blockers, b => b.Code == "CHECKLIST_INCOMPLETE");
    }

    [Fact]
    public async Task An_unsettled_driver_custody_blocks_close()
    {
        // Deliberately does NOT use CleanCloseableDayAsync/close this shift - CloseShiftHandler
        // itself already refuses to close a shift with unsettled custody (row 3b's own guard), so a
        // shift that owns unsettled custody can never legitimately reach "Closed" anyway. This test
        // proves the SEPARATE, business-day-level guard also catches it (defense in depth) alongside
        // the (expected, unavoidable) OPEN_SHIFT blocker for that same still-open shift.
        var dayId = await OpenBusinessDayAsync();
        var shiftId = await OpenShiftAsync();
        var productId = await _factory.SeedProductAsync(10000);
        var checkout = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "delivery", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: "عميل", CustomerPhone: "01000000000", DeliveryAddress: "شارع", DeliveryArea: "منطقة", BusinessDayId: null,
            DeliveryStreet: "شارع 1", DeliveryServiceId: "svc-1", DeliveryServiceName: "توصيل"));
        checkout.EnsureSuccessStatusCode();
        var order = await checkout.Content.ReadFromJsonAsync<CheckoutOrderResult>();
        var deliveryId = await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == order!.OrderId).Select(d => d.Id).FirstAsync());

        var driver = await (await _client.PostAsJsonAsync("/api/delivery/drivers", new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق", _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>();
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign", new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driver!.Id, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        // Custody now exists and is deliberately left unsettled.
        await CompleteRequiredClosingChecklistAsync(dayId);

        var response = await CloseBusinessDayAsync(dayId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BlockedResponse>();
        Assert.Contains(body!.blockers, b => b.Code == "UNSETTLED_DRIVER_CUSTODY");
    }

    [Fact]
    public async Task A_pending_electronic_payment_blocks_close()
    {
        var dayId = await CleanCloseableDayAsync();
        var shiftId = await OpenShiftAsync();
        var productId = await _factory.SeedProductAsync(15000);
        var checkout = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: "instapay",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 15000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null));
        checkout.EnsureSuccessStatusCode(); // left unconfirmed on purpose
        // An unconfirmed InstaPay sale is a claim, not money - it never adds to the drawer's
        // expected cash, so the shift still closes clean against its unchanged 50000 opening float.
        (await CloseShiftAsync(shiftId, 50000)).EnsureSuccessStatusCode();

        var response = await CloseBusinessDayAsync(dayId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BlockedResponse>();
        Assert.Contains(body!.blockers, b => b.Code == "PENDING_ELECTRONIC_PAYMENT");
    }

    [Fact]
    public async Task The_close_blockers_preview_reflects_exactly_what_a_close_attempt_would_hit()
    {
        var dayId = await OpenBusinessDayAsync();
        await OpenShiftAsync();

        var preview = await (await _client.GetAsync($"/api/business-day/{dayId}/close-blockers")).Content.ReadFromJsonAsync<List<CloseBlockerDto>>();
        Assert.Contains(preview!, b => b.Code == "OPEN_SHIFT");

        var closeResponse = await CloseBusinessDayAsync(dayId);
        var body = await closeResponse.Content.ReadFromJsonAsync<BlockedResponse>();
        Assert.Equal(preview!.Select(b => b.Code).OrderBy(x => x), body!.blockers.Select(b => b.Code).OrderBy(x => x));
    }

    // ---- A clean day can actually close, and never posts financial side effects ----

    [Fact]
    public async Task A_fully_clean_day_closes_and_produces_a_reproducible_snapshot()
    {
        var dayId = await CleanCloseableDayAsync();
        var treasuryBefore = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());

        var response = await CloseBusinessDayAsync(dayId);
        response.EnsureSuccessStatusCode();
        var snapshot = await response.Content.ReadFromJsonAsync<BusinessDayCloseSnapshotDto>();

        Assert.Equal(1, snapshot!.CloseRevision);
        Assert.True(snapshot.Authoritative);
        Assert.False(snapshot.WasReplayed);
        Assert.Contains("GrossSalesPiastres", snapshot.ReconciliationJson);

        var treasuryAfter = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());
        Assert.Equal(treasuryBefore, treasuryAfter); // closing posts NO financial movement itself

        var day = await (await _client.GetAsync($"/api/business-day/{dayId}")).Content.ReadFromJsonAsync<BusinessDayDto>();
        Assert.Equal("Closed", day!.Status);

        var fetchedSnapshot = await (await _client.GetAsync($"/api/business-day/{dayId}/close-snapshot")).Content.ReadFromJsonAsync<BusinessDayCloseSnapshotDto>();
        Assert.Equal(snapshot.Id, fetchedSnapshot!.Id);
    }

    [Fact]
    public async Task Retrying_the_same_close_request_id_replays_the_same_snapshot_not_a_second_one()
    {
        var dayId = await CleanCloseableDayAsync();
        var requestId = $"close-{Guid.NewGuid():N}";

        var first = await (await CloseBusinessDayAsync(dayId, requestId)).Content.ReadFromJsonAsync<BusinessDayCloseSnapshotDto>();
        var second = await (await CloseBusinessDayAsync(dayId, requestId)).Content.ReadFromJsonAsync<BusinessDayCloseSnapshotDto>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(first.Id, second.Id);
        var snapshotCount = await _factory.QueryAsync(db => db.BusinessDayCloseSnapshots.CountAsync(s => s.BusinessDayId == dayId));
        Assert.Equal(1, snapshotCount);
    }

    [Fact]
    public async Task Concurrent_close_requests_with_the_same_request_id_produce_exactly_one_snapshot()
    {
        var dayId = await CleanCloseableDayAsync();
        var requestId = $"close-{Guid.NewGuid():N}";

        var tasks = Enumerable.Range(0, 6).Select(_ => CloseBusinessDayAsync(dayId, requestId));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<BusinessDayCloseSnapshotDto>()));
        Assert.Equal(1, results.Count(r => !r!.WasReplayed));
        var snapshotCount = await _factory.QueryAsync(db => db.BusinessDayCloseSnapshots.CountAsync(s => s.BusinessDayId == dayId));
        Assert.Equal(1, snapshotCount);
        var day = await (await _client.GetAsync($"/api/business-day/{dayId}")).Content.ReadFromJsonAsync<BusinessDayDto>();
        Assert.Equal(1, day!.CloseRevision); // never incremented past 1 despite 6 concurrent posts
    }

    [Fact]
    public async Task Closing_an_already_closed_day_without_reopening_first_is_rejected()
    {
        var dayId = await CleanCloseableDayAsync();
        (await CloseBusinessDayAsync(dayId)).EnsureSuccessStatusCode();

        var response = await CloseBusinessDayAsync(dayId); // a genuinely NEW request id, not a retry
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---- Reopen ----

    [Fact]
    public async Task Reopening_a_day_requires_a_non_empty_reason()
    {
        var dayId = await CleanCloseableDayAsync();
        (await CloseBusinessDayAsync(dayId)).EnsureSuccessStatusCode();

        var response = await ReopenBusinessDayAsync(dayId, "");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Reopening_an_open_day_is_rejected()
    {
        var dayId = await OpenBusinessDayAsync();
        var response = await ReopenBusinessDayAsync(dayId, "مراجعة");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Reopening_a_closed_day_succeeds_and_clears_its_closing_checklist()
    {
        var dayId = await CleanCloseableDayAsync();
        (await CloseBusinessDayAsync(dayId)).EnsureSuccessStatusCode();

        var response = await ReopenBusinessDayAsync(dayId, "مراجعة فاتورة عميل");
        response.EnsureSuccessStatusCode();
        var reopened = await response.Content.ReadFromJsonAsync<BusinessDayDto>();
        Assert.Equal("Reopened", reopened!.Status);
        Assert.Equal("مراجعة فاتورة عميل", reopened.ReopenReason);

        var checklist = await (await _client.GetAsync($"/api/business-day/{dayId}/checklist")).Content.ReadFromJsonAsync<List<ChecklistItemStatusDto>>();
        Assert.All(checklist!.Where(c => c.Phase == "Closing"), c => Assert.False(c.Completed));
    }

    [Fact]
    public async Task Reopening_without_business_day_reopen_permission_requires_a_valid_approval()
    {
        var dayId = await CleanCloseableDayAsync();
        (await CloseBusinessDayAsync(dayId)).EnsureSuccessStatusCode();
        var noPermissionUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");

        var withoutApproval = await ReopenBusinessDayAsync(dayId, "مراجعة", noPermissionUser);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, withoutApproval.StatusCode);

        var approvalId = await IssueApprovalAsync("business_day_reopen");
        var withApproval = await ReopenBusinessDayAsync(dayId, "مراجعة", noPermissionUser, approvalId, _managerId);
        withApproval.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Closing_again_after_a_reopen_produces_a_new_higher_revision_snapshot()
    {
        var dayId = await CleanCloseableDayAsync();
        (await CloseBusinessDayAsync(dayId)).EnsureSuccessStatusCode();
        (await ReopenBusinessDayAsync(dayId, "مراجعة")).EnsureSuccessStatusCode();

        // The closing checklist was cleared by the reopen - must be redone before closing again.
        await CompleteRequiredClosingChecklistAsync(dayId);
        var secondClose = await (await CloseBusinessDayAsync(dayId)).Content.ReadFromJsonAsync<BusinessDayCloseSnapshotDto>();

        Assert.Equal(2, secondClose!.CloseRevision);
        var snapshots = await (await _client.GetAsync($"/api/business-day/{dayId}/close-snapshots")).Content.ReadFromJsonAsync<List<BusinessDayCloseSnapshotDto>>();
        Assert.Equal(2, snapshots!.Count);
        Assert.True(snapshots.Single(s => s.CloseRevision == 2).Authoritative);
    }

    // ---- Checklist ----

    [Fact]
    public async Task Completing_a_checklist_item_persists_and_is_readable_back()
    {
        var dayId = await OpenBusinessDayAsync();
        (await CompleteChecklistAsync(dayId, "close-drawer")).EnsureSuccessStatusCode();

        var checklist = await (await _client.GetAsync($"/api/business-day/{dayId}/checklist")).Content.ReadFromJsonAsync<List<ChecklistItemStatusDto>>();
        Assert.NotNull(checklist);
        Assert.True(checklist.Single(c => c.Id == "close-drawer").Completed);
        Assert.False(checklist.Single(c => c.Id == "close-shifts").Completed);
    }

    // ---- Control exceptions ----

    [Fact]
    public async Task Acknowledging_an_exception_requires_a_reason()
    {
        var response = await _client.PostAsJsonAsync("/api/control-exceptions/some-id/status",
            new SetExceptionStatusRequest("some-id", "acknowledged", "", _managerId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Acknowledging_an_exception_persists_its_state_and_is_overlaid_on_the_next_read()
    {
        var dayId = await CleanCloseableDayAsync();
        var shiftId = await OpenShiftAsync();
        var productId = await _factory.SeedProductAsync(60_000);
        var checkout = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 60_000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null));
        checkout.EnsureSuccessStatusCode();
        var order = await checkout.Content.ReadFromJsonAsync<CheckoutOrderResult>();
        Assert.NotNull(order);

        // A large refund (>= 50,000 piastres) is a control exception, ported verbatim from
        // CONTROL_THRESHOLDS.largeRefundPiastres.
        var refundResponse = await _client.PostAsJsonAsync("/api/orders/refund", new RefundOrderRequest(
            $"ref-{Guid.NewGuid():N}", order.OrderId, 60_000, "طلب العميل", _managerId, null, null));
        refundResponse.EnsureSuccessStatusCode();

        var exceptions = await (await _client.GetAsync($"/api/business-day/{dayId}/exceptions")).Content.ReadFromJsonAsync<List<ControlExceptionDto>>();
        var exception = exceptions!.Single(e => e.EntityId == order.OrderId);
        Assert.Equal("open", exception.Status);

        var ackResponse = await _client.PostAsJsonAsync($"/api/control-exceptions/{exception.Id}/status",
            new SetExceptionStatusRequest(exception.Id, "acknowledged", "تمت المراجعة مع العميل", _managerId, null, null));
        ackResponse.EnsureSuccessStatusCode();

        var afterAck = await (await _client.GetAsync($"/api/business-day/{dayId}/exceptions")).Content.ReadFromJsonAsync<List<ControlExceptionDto>>();
        var updated = afterAck!.Single(e => e.Id == exception.Id);
        Assert.Equal("acknowledged", updated.Status);
        Assert.Equal("تمت المراجعة مع العميل", updated.Reason);
    }

    // ---- Audit trail ----

    [Fact]
    public async Task Closing_a_business_day_writes_started_and_closed_audit_events()
    {
        var dayId = await CleanCloseableDayAsync();
        (await CloseBusinessDayAsync(dayId)).EnsureSuccessStatusCode();

        var events = await (await _client.GetAsync("/api/control-center/audit-events")).Content.ReadFromJsonAsync<List<ControlAuditEventDto>>();
        Assert.Contains(events!, e => e.Action == "business_day_close_started" && e.EntityId == dayId);
        Assert.Contains(events!, e => e.Action == "business_day_closed" && e.EntityId == dayId);
    }

    [Fact]
    public async Task A_blocked_close_writes_a_blocked_audit_event_with_the_exact_blocker_codes()
    {
        var dayId = await OpenBusinessDayAsync();
        await OpenShiftAsync();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await CloseBusinessDayAsync(dayId)).StatusCode);

        var events = await (await _client.GetAsync("/api/control-center/audit-events")).Content.ReadFromJsonAsync<List<ControlAuditEventDto>>();
        var blocked = events!.First(e => e.Action == "business_day_close_blocked" && e.EntityId == dayId);
        Assert.Contains("OPEN_SHIFT", blocked.BlockerCodes!);
    }

    // ---- Delivery exceptions (ready_unassigned/out_too_long/failed/returned - a real gap found
    // during the legacy-parity audit: selectDeliveryExceptions' per-delivery types need no "trip"
    // concept at all, unlike the trip-scoped types this system genuinely cannot port) ----

    [Fact]
    public async Task A_ready_delivery_with_no_driver_is_a_control_exception()
    {
        var dayId = await OpenBusinessDayAsync();
        var shiftId = await OpenShiftAsync();
        var productId = await _factory.SeedProductAsync(10000);
        var checkout = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "delivery", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: "عميل", CustomerPhone: "01000000000", DeliveryAddress: "شارع", DeliveryArea: "منطقة", BusinessDayId: null,
            DeliveryStreet: "شارع 1", DeliveryServiceId: "svc-1", DeliveryServiceName: "توصيل"));
        checkout.EnsureSuccessStatusCode();
        var order = await checkout.Content.ReadFromJsonAsync<CheckoutOrderResult>();
        var deliveryId = await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == order!.OrderId).Select(d => d.Id).FirstAsync());
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null))).EnsureSuccessStatusCode();

        var exceptions = await (await _client.GetAsync($"/api/business-day/{dayId}/exceptions")).Content.ReadFromJsonAsync<List<ControlExceptionDto>>();
        Assert.Contains(exceptions!, e => e.EntityId == deliveryId && e.Category == "delivery" && e.Severity == "warning");
    }

    [Fact]
    public async Task An_assigned_out_for_delivery_order_is_not_flagged_as_ready_unassigned()
    {
        var dayId = await OpenBusinessDayAsync();
        var shiftId = await OpenShiftAsync();
        var productId = await _factory.SeedProductAsync(10000);
        var checkout = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "delivery", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: "عميل", CustomerPhone: "01000000000", DeliveryAddress: "شارع", DeliveryArea: "منطقة", BusinessDayId: null,
            DeliveryStreet: "شارع 1", DeliveryServiceId: "svc-1", DeliveryServiceName: "توصيل"));
        checkout.EnsureSuccessStatusCode();
        var order = await checkout.Content.ReadFromJsonAsync<CheckoutOrderResult>();
        var deliveryId = await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == order!.OrderId).Select(d => d.Id).FirstAsync());
        var driver = await (await _client.PostAsJsonAsync("/api/delivery/drivers", new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق", _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>();
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign", new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driver!.Id, _dispatcherId, null, null))).EnsureSuccessStatusCode();

        var exceptions = await (await _client.GetAsync($"/api/business-day/{dayId}/exceptions")).Content.ReadFromJsonAsync<List<ControlExceptionDto>>();
        Assert.DoesNotContain(exceptions!, e => e.EntityId == deliveryId && e.Message.Contains("جاهز دون سائق"));
    }

    // ---- Settings (updateSettings - a real gap found during the legacy-parity audit: no Settings
    // feature existed at all, restaurant identity was hardcoded, and the shift discrepancy threshold
    // was only a deploy-time config value) ----

    [Fact]
    public async Task Reading_settings_before_any_save_returns_the_historical_defaults()
    {
        var settings = await (await _client.GetAsync("/api/settings")).Content.ReadFromJsonAsync<Falah.Application.Settings.SettingsDto>();
        Assert.Equal("فالح أبو العنبه", settings!.RestaurantName);
        Assert.Equal(5000, settings.CashDiscrepancyThresholdPiastres);
    }

    [Fact]
    public async Task Updating_settings_requires_settings_manage_except_for_the_grill_sound_toggle()
    {
        var noPermissionUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");
        var blocked = await _client.PutAsJsonAsync("/api/settings",
            new Falah.Application.Settings.UpdateSettingsRequest(noPermissionUser, RestaurantName: "اسم جديد"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blocked.StatusCode);

        // The ONE exception: toggling GrillScreenSoundAlert alone needs no permission at all,
        // matching legacy's own `if (!Object.keys(newSettings).every(key => key ===
        // 'grillScreenSoundAlert')) authorizeCommand(...)`.
        var allowed = await _client.PutAsJsonAsync("/api/settings",
            new Falah.Application.Settings.UpdateSettingsRequest(noPermissionUser, GrillScreenSoundAlert: false));
        allowed.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Updating_the_discrepancy_threshold_changes_what_shift_close_requires_approval_for()
    {
        (await _client.PutAsJsonAsync("/api/settings",
            new Falah.Application.Settings.UpdateSettingsRequest(_managerId, CashDiscrepancyThresholdPiastres: 100))).EnsureSuccessStatusCode();

        await OpenBusinessDayAsync();
        var shiftId = await OpenShiftAsync(50000);
        // A 200-piastre shortfall now exceeds the new, much lower threshold and needs manager
        // approval - proving PrepareShiftCloseHandler reads the live Settings value, not the
        // historical 5000-piastre default.
        var prepared = await (await _client.PostAsJsonAsync("/api/shifts/prepare-close",
            new Falah.Application.Shifts.PrepareShiftCloseRequest(shiftId, 49800))).Content.ReadFromJsonAsync<Falah.Application.Shifts.PrepareShiftCloseResult>();
        Assert.True(prepared!.RequiresManagerApproval);
    }

    // ---- Egyptian-denomination cash count (saveCashCount - a real gap found during the legacy-
    // parity audit: this feature had no C# equivalent at all) ----

    [Fact]
    public async Task Saving_a_cash_count_computes_totals_and_is_idempotent_by_id()
    {
        var dayId = await OpenBusinessDayAsync();
        var countId = $"count-{Guid.NewGuid():N}";
        var request = new Falah.Application.BusinessDay.SaveCashCountRequest(
            countId, dayId, null,
            new[] { new Falah.Application.BusinessDay.DenominationRowInput(20000, 2), new Falah.Application.BusinessDay.DenominationRowInput(500, 3) },
            41500, _managerId);

        var first = await (await _client.PostAsJsonAsync($"/api/business-day/{dayId}/cash-counts", request))
            .Content.ReadFromJsonAsync<Falah.Application.BusinessDay.CashDenominationCountDto>();
        Assert.Equal(41500, first!.TotalPiastres);
        Assert.Equal(0, first.DifferencePiastres);
        Assert.False(first.WasReplayed);

        var second = await (await _client.PostAsJsonAsync($"/api/business-day/{dayId}/cash-counts", request))
            .Content.ReadFromJsonAsync<Falah.Application.BusinessDay.CashDenominationCountDto>();
        Assert.True(second!.WasReplayed);

        var listed = await (await _client.GetAsync($"/api/business-day/{dayId}/cash-counts")).Content.ReadFromJsonAsync<List<Falah.Application.BusinessDay.CashDenominationCountDto>>();
        Assert.Single(listed!, c => c.Id == countId);
    }

    [Fact]
    public async Task Saving_a_cash_count_requires_the_dedicated_cash_count_permission()
    {
        var dayId = await OpenBusinessDayAsync();
        var noPermissionUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");
        var request = new Falah.Application.BusinessDay.SaveCashCountRequest(
            $"count-{Guid.NewGuid():N}", dayId, null,
            new[] { new Falah.Application.BusinessDay.DenominationRowInput(500, 1) }, 500, noPermissionUser);
        var response = await _client.PostAsJsonAsync($"/api/business-day/{dayId}/cash-counts", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task The_fixed_denomination_list_matches_egyptian_currency()
    {
        var denominations = await (await _client.GetAsync("/api/business-day/cash-count-denominations")).Content.ReadFromJsonAsync<List<long>>();
        Assert.Equal(new long[] { 20000, 10000, 5000, 2000, 1000, 500, 200, 100, 50, 25 }, denominations);
    }

    private sealed record BlockedResponse(string error, List<CloseBlockerDto> blockers);
}

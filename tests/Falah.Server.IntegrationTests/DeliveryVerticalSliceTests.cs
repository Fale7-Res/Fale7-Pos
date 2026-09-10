namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Ported from the reference app's per-order dispatch path (assignDelivery/reassignDelivery/
/// recordDeliveryResult/settleDriverCustody in lib/enterprise-store.tsx) composed with the
/// already-tested Falah.Domain.Delivery state machine. Covers the full lifecycle (New -> Ready ->
/// OutForDelivery -> Delivered/Failed), reassignment/transfer history, driver cash custody and
/// settlement math, idempotency/concurrency safety, permission escalation (delivery_manage vs
/// delivery_override vs delivery_settlement_approve), and the shift-close unsettled-custody guard.
/// </summary>
public class DeliveryVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _dispatcherId = default!;
    private string _managerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _dispatcherId = await _factory.SeedUserAsync("موظف التوصيل", "dispatcher", "delivery_manage");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> OpenShiftAsync(long openingCashPiastres = 50000) =>
        (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, openingCashPiastres)))
            .Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;

    private async Task<CheckoutOrderResult> CheckoutDeliveryOrderAsync(string shiftId, long totalPiastres, string paymentMethod = "cash")
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        var amountPaid = paymentMethod == "cash" ? 0 : totalPiastres; // cash-on-delivery defers all money
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "delivery", PaymentMethod: paymentMethod,
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: amountPaid, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: "عميل اختبار", CustomerPhone: "01000000000", DeliveryAddress: "شارع الاختبار", DeliveryArea: "المنطقة أ", BusinessDayId: null,
            DeliveryStreet: "شارع 9", DeliveryServiceId: "svc-1", DeliveryServiceName: "توصيل سريع");
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    private Task<string> GetDeliveryIdForOrderAsync(string orderId) =>
        _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == orderId).Select(d => d.Id).FirstAsync());

    private async Task<string> CreateDriverAsync(string name = "سائق اختبار")
    {
        var result = await (await _client.PostAsJsonAsync("/api/delivery/drivers",
            new CreateDriverRequest($"req-{Guid.NewGuid():N}", name, _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>();
        return result!.Id;
    }

    private Task<HttpResponseMessage> MarkReadyAsync(string deliveryId, string? actorId = null) =>
        _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready",
            new MarkDeliveryReadyRequest(deliveryId, actorId ?? _dispatcherId, null, null));

    private Task<HttpResponseMessage> AssignAsync(string deliveryId, string driverId, string? requestId = null, string? actorId = null) =>
        _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign",
            new AssignDeliveryRequest(requestId ?? $"req-{Guid.NewGuid():N}", deliveryId, driverId, actorId ?? _dispatcherId, null, null));

    private Task<HttpResponseMessage> ReassignAsync(string deliveryId, string newDriverId, string reason, string actorId, string? approvalId = null, string? approverId = null) =>
        _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/reassign",
            new ReassignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, newDriverId, reason, actorId, approvalId, approverId));

    private Task<HttpResponseMessage> RecordResultAsync(string deliveryId, string result, long? collectedAmountPiastres = null, string? reason = null, string? notes = null, string? actorId = null) =>
        _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result",
            new RecordDeliveryResultRequest($"req-{Guid.NewGuid():N}", deliveryId, result, collectedAmountPiastres, reason, notes, actorId ?? _dispatcherId, null, null));

    private Task<HttpResponseMessage> SettleAsync(string driverId, long actualCashPiastres, string? reason = null, string? actorId = null, string? approvalId = null, string? approverId = null) =>
        _client.PostAsJsonAsync($"/api/delivery/drivers/{driverId}/settle",
            new SettleDriverCustodyRequest($"req-{Guid.NewGuid():N}", driverId, actualCashPiastres, reason, actorId ?? _dispatcherId, approvalId, approverId));

    private async Task<string> IssueApprovalAsync(string action) =>
        (await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), action, "اعتماد اختبار"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!.ApprovalId;

    // ---- Lifecycle ----

    [Fact]
    public async Task Checking_out_a_delivery_order_auto_creates_a_delivery_row_in_new_status()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 10000);

        var delivery = await _factory.QueryAsync(db => db.Deliveries.FirstAsync(d => d.OrderId == order.OrderId));
        Assert.Equal("New", delivery.Status);
        Assert.Equal(shiftId, delivery.ShiftId);
    }

    [Fact]
    public async Task A_non_delivery_order_creates_no_delivery_row()
    {
        var shiftId = await OpenShiftAsync();
        var productId = await _factory.SeedProductAsync(10000);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 10000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        var order = (await (await _client.PostAsJsonAsync("/api/orders/checkout", request)).Content.ReadFromJsonAsync<CheckoutOrderResult>())!;

        var count = await _factory.QueryAsync(db => db.Deliveries.CountAsync(d => d.OrderId == order.OrderId));
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Marking_ready_transitions_new_to_ready()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 10000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);

        var response = await MarkReadyAsync(deliveryId);
        response.EnsureSuccessStatusCode();

        var delivery = await _factory.QueryAsync(db => db.Deliveries.FirstAsync(d => d.Id == deliveryId));
        Assert.Equal("Ready", delivery.Status);
    }

    [Fact]
    public async Task Marking_ready_twice_is_a_harmless_no_op()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 10000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);

        var response = await MarkReadyAsync(deliveryId);
        response.EnsureSuccessStatusCode();

        var eventCount = await _factory.QueryAsync(db => db.DeliveryEvents.CountAsync(e => e.DeliveryId == deliveryId));
        Assert.Equal(1, eventCount); // only the first ready transition wrote an event
    }

    [Fact]
    public async Task Assigning_a_driver_to_a_not_yet_ready_delivery_is_rejected()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 10000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        var driverId = await CreateDriverAsync();

        var response = await AssignAsync(deliveryId, driverId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Assigning_a_driver_to_a_cash_on_delivery_order_moves_directly_to_out_for_delivery_and_creates_full_custody()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000, "cash");
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();

        var response = await AssignAsync(deliveryId, driverId);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AssignDeliveryResult>();

        Assert.Equal("OutForDelivery", result!.Status);
        Assert.Equal(driverId, result.DriverId);
        Assert.False(result.WasReplayed);

        var custody = await _factory.QueryAsync(db => db.DriverCustodies.FirstAsync(c => c.DeliveryId == deliveryId));
        Assert.Equal(15000, custody.ExpectedPiastres);
        Assert.Equal("Open", custody.Status);
    }

    [Fact]
    public async Task Assigning_a_driver_to_a_card_paid_order_creates_no_custody_row()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000, "card"); // paid up front - nothing for the driver to collect
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();

        var response = await AssignAsync(deliveryId, driverId);
        response.EnsureSuccessStatusCode();

        var custodyCount = await _factory.QueryAsync(db => db.DriverCustodies.CountAsync(c => c.DeliveryId == deliveryId));
        Assert.Equal(0, custodyCount);
    }

    [Fact]
    public async Task Retrying_the_same_assignment_request_id_never_double_assigns()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        var requestId = $"req-{Guid.NewGuid():N}";

        var first = await (await AssignAsync(deliveryId, driverId, requestId)).Content.ReadFromJsonAsync<AssignDeliveryResult>();
        var second = await (await AssignAsync(deliveryId, driverId, requestId)).Content.ReadFromJsonAsync<AssignDeliveryResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        var custodyCount = await _factory.QueryAsync(db => db.DriverCustodies.CountAsync(c => c.DeliveryId == deliveryId));
        Assert.Equal(1, custodyCount);
    }

    [Fact]
    public async Task Two_dispatchers_racing_to_assign_different_drivers_to_the_same_delivery_only_one_wins()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverA = await CreateDriverAsync("سائق أ");
        var driverB = await CreateDriverAsync("سائق ب");

        // Six concurrent attempts split across two DIFFERENT drivers, each with its own RequestId
        // (a genuinely different attempt, not a retry of the same one) - the UPDLOCK on the
        // delivery row must let exactly one through; every other one must see the delivery is no
        // longer Ready and fail cleanly, never silently overwrite the winner's assignment.
        var tasks = Enumerable.Range(0, 6).Select(i => AssignAsync(deliveryId, i % 2 == 0 ? driverA : driverB));
        var responses = await Task.WhenAll(tasks);

        Assert.Equal(1, responses.Count(r => r.IsSuccessStatusCode));
        Assert.Equal(5, responses.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity));

        var delivery = await _factory.QueryAsync(db => db.Deliveries.AsNoTracking().FirstAsync(d => d.Id == deliveryId));
        Assert.Equal("OutForDelivery", delivery.Status);
        Assert.True(delivery.DriverId == driverA || delivery.DriverId == driverB);
        var custodyCount = await _factory.QueryAsync(db => db.DriverCustodies.CountAsync(c => c.DeliveryId == deliveryId));
        Assert.Equal(1, custodyCount); // exactly one custody row, never one per losing attempt
    }

    [Fact]
    public async Task Recording_a_delivered_result_defaults_the_collected_amount_to_what_was_expected()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);

        var response = await RecordResultAsync(deliveryId, "Delivered");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RecordDeliveryResultResult>();
        Assert.Equal("Delivered", result!.Status);

        var delivery = await _factory.QueryAsync(db => db.Deliveries.FirstAsync(d => d.Id == deliveryId));
        Assert.Equal(15000, delivery.CollectedAmountPiastres);
        var custody = await _factory.QueryAsync(db => db.DriverCustodies.FirstAsync(c => c.DeliveryId == deliveryId));
        Assert.Equal(15000, custody.CollectedPiastres);
    }

    [Fact]
    public async Task Recording_a_delivered_result_with_a_different_amount_requires_a_reason()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);

        var withoutReason = await RecordResultAsync(deliveryId, "Delivered", collectedAmountPiastres: 14000);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, withoutReason.StatusCode);

        var withReason = await RecordResultAsync(deliveryId, "Delivered", collectedAmountPiastres: 14000, reason: "خصم توصيل");
        withReason.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Recording_a_failed_result_requires_a_recognized_reason_code()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);

        var badReason = await RecordResultAsync(deliveryId, "Failed", reason: "غير معروف");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badReason.StatusCode);

        var goodReason = await RecordResultAsync(deliveryId, "Failed", reason: "customer_unreachable");
        goodReason.EnsureSuccessStatusCode();

        var delivery = await _factory.QueryAsync(db => db.Deliveries.FirstAsync(d => d.Id == deliveryId));
        Assert.Equal("Failed", delivery.Status);
    }

    [Fact]
    public async Task Retrying_the_same_result_request_id_never_double_records()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);

        var requestId = $"req-{Guid.NewGuid():N}";
        var request = new RecordDeliveryResultRequest(requestId, deliveryId, "Delivered", null, null, null, _dispatcherId, null, null);
        var first = await (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result", request)).Content.ReadFromJsonAsync<RecordDeliveryResultResult>();
        var second = await (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result", request)).Content.ReadFromJsonAsync<RecordDeliveryResultResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        var eventCount = await _factory.QueryAsync(db => db.DeliveryEvents.CountAsync(e => e.DeliveryId == deliveryId && e.Type == "delivered"));
        Assert.Equal(1, eventCount);
    }

    // ---- Reassignment / transfer history ----

    [Fact]
    public async Task Reassigning_to_the_same_driver_is_a_no_op_and_writes_no_history()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);

        var response = await ReassignAsync(deliveryId, driverId, "نفس السائق", _dispatcherId);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReassignDeliveryResult>();
        Assert.True(result!.WasNoOp);

        var historyCount = await _factory.QueryAsync(db => db.DeliveryReassignments.CountAsync(h => h.DeliveryId == deliveryId));
        Assert.Equal(0, historyCount);
    }

    [Fact]
    public async Task Reassigning_while_in_flight_transfers_custody_to_the_new_driver_and_writes_history()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var oldDriverId = await CreateDriverAsync("السائق الأول");
        var newDriverId = await CreateDriverAsync("السائق الثاني");
        await AssignAsync(deliveryId, oldDriverId);

        var response = await ReassignAsync(deliveryId, newDriverId, "السائق الأول غير متاح", _dispatcherId);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReassignDeliveryResult>();
        Assert.False(result!.WasNoOp);
        Assert.Equal(newDriverId, result.DriverId);

        var custody = await _factory.QueryAsync(db => db.DriverCustodies.FirstAsync(c => c.DeliveryId == deliveryId));
        Assert.Equal(newDriverId, custody.DriverId);

        var history = await _factory.QueryAsync(db => db.DeliveryReassignments.FirstAsync(h => h.DeliveryId == deliveryId));
        Assert.Equal(oldDriverId, history.OldDriverId);
        Assert.Equal(newDriverId, history.NewDriverId);
    }

    [Fact]
    public async Task Reassigning_a_settled_delivery_is_rejected()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000, "card"); // no custody, settles cleanly with zero cash
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);
        await RecordResultAsync(deliveryId, "Delivered");
        await SettleAsync(driverId, 0);

        var newDriverId = await CreateDriverAsync("سائق آخر");
        var response = await ReassignAsync(deliveryId, newDriverId, "محاولة نقل بعد التسوية", _dispatcherId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Reassigning_after_an_outcome_is_recorded_needs_delivery_override_not_just_delivery_manage()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var oldDriverId = await CreateDriverAsync("السائق الأول");
        var newDriverId = await CreateDriverAsync("السائق الثاني");
        await AssignAsync(deliveryId, oldDriverId);
        await RecordResultAsync(deliveryId, "Delivered");

        // The dispatcher holds delivery_manage but NOT delivery_override - moving a driver AFTER the
        // outcome is already recorded is the exceptional case and must be refused on that alone.
        var withoutOverride = await ReassignAsync(deliveryId, newDriverId, "تصحيح بعد التسليم", _dispatcherId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, withoutOverride.StatusCode);

        var approvalId = await IssueApprovalAsync("delivery_override");
        var withOverride = await ReassignAsync(deliveryId, newDriverId, "تصحيح بعد التسليم", _dispatcherId, approvalId, _managerId);
        withOverride.EnsureSuccessStatusCode();
    }

    // ---- Driver settlement ----

    [Fact]
    public async Task Settling_a_driver_posts_a_movement_to_main_treasury_and_marks_custody_and_delivery_settled()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);
        await RecordResultAsync(deliveryId, "Delivered");

        var response = await SettleAsync(driverId, 15000);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SettleDriverCustodyResult>();
        Assert.Equal(15000, result!.ExpectedPiastres);
        Assert.Equal(15000, result.ActualPiastres);
        Assert.Equal(0, result.DifferencePiastres);
        Assert.NotNull(result.TreasuryMovementId);

        var movement = await _factory.QueryAsync(db => db.TreasuryMovements.FirstAsync(m => m.Id == result.TreasuryMovementId));
        Assert.Equal("driver_settlement", movement.Type);
        Assert.Equal("tre-main", movement.DestinationAccountId);
        Assert.Equal(15000, movement.AmountPiastres);

        var custody = await _factory.QueryAsync(db => db.DriverCustodies.FirstAsync(c => c.DeliveryId == deliveryId));
        Assert.Equal("Settled", custody.Status);
        var delivery = await _factory.QueryAsync(db => db.Deliveries.FirstAsync(d => d.Id == deliveryId));
        Assert.Equal("Settled", delivery.Status);
    }

    [Fact]
    public async Task Settling_refuses_when_the_driver_still_has_an_order_out_for_delivery()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId); // still out_for_delivery - no result recorded yet

        var response = await SettleAsync(driverId, 15000);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_settlement_difference_requires_a_reason_and_delivery_settlement_approve_on_top_of_delivery_manage()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);
        await RecordResultAsync(deliveryId, "Delivered");

        var noReason = await SettleAsync(driverId, 14000);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noReason.StatusCode);

        var noApproval = await SettleAsync(driverId, 14000, reason: "عجز نقدية");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noApproval.StatusCode);

        var approvalId = await IssueApprovalAsync("delivery_settlement_approve");
        var withApproval = await SettleAsync(driverId, 14000, reason: "عجز نقدية", approvalId: approvalId, approverId: _managerId);
        withApproval.EnsureSuccessStatusCode();
        var result = await withApproval.Content.ReadFromJsonAsync<SettleDriverCustodyResult>();
        Assert.Equal(-1000, result!.DifferencePiastres);
    }

    [Fact]
    public async Task Retrying_the_same_settlement_request_id_never_posts_twice()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);
        await RecordResultAsync(deliveryId, "Delivered");

        var requestId = $"req-{Guid.NewGuid():N}";
        var request = new SettleDriverCustodyRequest(requestId, driverId, 15000, null, _dispatcherId, null, null);
        var first = await (await _client.PostAsJsonAsync($"/api/delivery/drivers/{driverId}/settle", request)).Content.ReadFromJsonAsync<SettleDriverCustodyResult>();
        var second = await (await _client.PostAsJsonAsync($"/api/delivery/drivers/{driverId}/settle", request)).Content.ReadFromJsonAsync<SettleDriverCustodyResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "driver_settlement" && m.ReferenceId == driverId));
        Assert.Equal(1, movementCount);
    }

    [Fact]
    public async Task Concurrent_settlement_requests_for_the_same_driver_with_different_request_ids_post_exactly_once()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);
        await RecordResultAsync(deliveryId, "Delivered");

        // Different RequestIds deliberately - the idempotency-key replay path cannot protect against
        // this; only the UPDLOCK row-set lock inside the handler can. See
        // SettleDriverCustodyHandler's own doc comment for the concurrency gap this closes.
        var tasks = Enumerable.Range(0, 6).Select(_ => SettleAsync(driverId, 15000));
        var responses = await Task.WhenAll(tasks);

        Assert.Equal(1, responses.Count(r => r.IsSuccessStatusCode));
        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "driver_settlement" && m.ReferenceId == driverId));
        Assert.Equal(1, movementCount);
    }

    // ---- Shift-close guard ----

    [Fact]
    public async Task Closing_a_shift_with_an_unsettled_driver_custody_for_that_shift_is_blocked()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);
        await RecordResultAsync(deliveryId, "Delivered"); // custody exists, not yet settled

        var response = await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"creq-{Guid.NewGuid():N}", shiftId, _cashierId, 50000, null, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var shift = await _factory.QueryAsync(db => db.Shifts.FirstAsync(s => s.Id == shiftId));
        Assert.Equal("Open", shift.Status);
    }

    [Fact]
    public async Task After_settling_the_driver_the_shift_can_close()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);
        await RecordResultAsync(deliveryId, "Delivered");
        await SettleAsync(driverId, 15000);

        var response = await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"creq-{Guid.NewGuid():N}", shiftId, _cashierId, 50000, null, null, null));
        response.EnsureSuccessStatusCode();

        var shift = await _factory.QueryAsync(db => db.Shifts.FirstAsync(s => s.Id == shiftId));
        Assert.Equal("Closed", shift.Status);
    }

    // ---- Queries ----

    [Fact]
    public async Task Ready_for_dispatch_lists_only_deliveries_currently_in_ready_status()
    {
        var shiftId = await OpenShiftAsync();
        var readyOrder = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var readyDeliveryId = await GetDeliveryIdForOrderAsync(readyOrder.OrderId);
        await MarkReadyAsync(readyDeliveryId);
        var newOrder = await CheckoutDeliveryOrderAsync(shiftId, 12000); // left in New - not ready yet

        var list = await (await _client.GetAsync("/api/delivery/ready")).Content.ReadFromJsonAsync<List<DeliveryDto>>();

        Assert.Contains(list!, d => d.DeliveryId == readyDeliveryId);
        Assert.DoesNotContain(list!, d => d.OrderId == newOrder.OrderId);
    }

    [Fact]
    public async Task A_drivers_active_list_shows_only_that_drivers_currently_in_flight_deliveries()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);

        var active = await (await _client.GetAsync($"/api/delivery/drivers/{driverId}/active")).Content.ReadFromJsonAsync<List<DeliveryDto>>();
        Assert.Single(active!);
        Assert.Equal(deliveryId, active!.Single().DeliveryId);

        await RecordResultAsync(deliveryId, "Delivered");
        var afterDelivered = await (await _client.GetAsync($"/api/delivery/drivers/{driverId}/active")).Content.ReadFromJsonAsync<List<DeliveryDto>>();
        Assert.Empty(afterDelivered!);
    }

    [Fact]
    public async Task The_timeline_records_every_lifecycle_event_in_order()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 15000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);
        await RecordResultAsync(deliveryId, "Delivered");
        await SettleAsync(driverId, 15000);

        var timeline = await (await _client.GetAsync($"/api/delivery/{deliveryId}/timeline")).Content.ReadFromJsonAsync<List<DeliveryEventDto>>();
        var types = timeline!.Select(e => e.Type).ToList();

        Assert.Equal(new[] { "ready", "assignment", "delivered", "settlement" }, types);
    }

    // ---- Driver deactivation (setDriverActive - a real gap found during the legacy-parity audit:
    // a driver could be created but never deactivated) ----

    [Fact]
    public async Task Deactivating_a_driver_flips_active_to_false_and_is_idempotent()
    {
        var driverId = await CreateDriverAsync();
        var deactivated = await (await _client.PostAsJsonAsync($"/api/delivery/drivers/{driverId}/set-active",
            new SetDriverActiveRequest(driverId, false, _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>();
        Assert.False(deactivated!.Active);

        // Setting the same state again is a harmless no-op, matching legacy's own idempotent-by-
        // nature soft-archive idiom (same as Categories/Products).
        var again = await (await _client.PostAsJsonAsync($"/api/delivery/drivers/{driverId}/set-active",
            new SetDriverActiveRequest(driverId, false, _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>();
        Assert.False(again!.Active);

        var reactivated = await (await _client.PostAsJsonAsync($"/api/delivery/drivers/{driverId}/set-active",
            new SetDriverActiveRequest(driverId, true, _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>();
        Assert.True(reactivated!.Active);
    }

    [Fact]
    public async Task Deactivating_a_driver_without_delivery_manage_is_rejected()
    {
        var driverId = await CreateDriverAsync();
        var noPermissionUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");
        var response = await _client.PostAsJsonAsync($"/api/delivery/drivers/{driverId}/set-active",
            new SetDriverActiveRequest(driverId, false, noPermissionUser));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_deactivated_driver_still_appears_in_the_all_drivers_list_but_not_the_active_one()
    {
        var driverId = await CreateDriverAsync("سائق سيُعطّل");
        (await _client.PostAsJsonAsync($"/api/delivery/drivers/{driverId}/set-active",
            new SetDriverActiveRequest(driverId, false, _dispatcherId))).EnsureSuccessStatusCode();

        var active = await (await _client.GetAsync("/api/delivery/drivers")).Content.ReadFromJsonAsync<List<DriverDto>>();
        Assert.DoesNotContain(active!, d => d.Id == driverId);

        var all = await (await _client.GetAsync("/api/delivery/drivers/all")).Content.ReadFromJsonAsync<List<DriverDto>>();
        Assert.Contains(all!, d => d.Id == driverId && !d.Active);
    }

    // ---- Delivery-area/service management CRUD (addDeliveryArea/updateDeliveryArea - a real gap
    // found during the legacy-parity audit: Order already carried DeliveryServiceId/
    // DeliveryServiceName snapshot fields with no management surface to create/edit/retire them) ----

    [Fact]
    public async Task Creating_a_delivery_service_succeeds_and_is_idempotent_by_request_id()
    {
        var requestId = $"req-{Guid.NewGuid():N}";
        var request = new CreateDeliveryServiceRequest(requestId, "توصيل سريع", 2000, _dispatcherId, null, null);
        var first = await (await _client.PostAsJsonAsync("/api/delivery/services", request)).Content.ReadFromJsonAsync<DeliveryServiceDto>();
        var second = await (await _client.PostAsJsonAsync("/api/delivery/services", request)).Content.ReadFromJsonAsync<DeliveryServiceDto>();
        Assert.Equal(first!.Id, second!.Id);

        var all = await (await _client.GetAsync("/api/delivery/services")).Content.ReadFromJsonAsync<List<DeliveryServiceDto>>();
        Assert.Single(all!, s => s.Id == first.Id);
    }

    [Fact]
    public async Task Editing_a_delivery_services_fee_never_changes_an_already_frozen_order_snapshot()
    {
        var service = await (await _client.PostAsJsonAsync("/api/delivery/services",
            new CreateDeliveryServiceRequest($"req-{Guid.NewGuid():N}", "توصيل عادي", 1500, _dispatcherId, null, null)))
            .Content.ReadFromJsonAsync<DeliveryServiceDto>();

        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 10000);

        await _client.PutAsJsonAsync($"/api/delivery/services/{service!.Id}",
            new UpdateDeliveryServiceRequest(service.Id, null, 5000, null, _dispatcherId, null, null));

        var reloaded = await (await _client.GetAsync($"/api/orders/{order.OrderId}")).Content.ReadFromJsonAsync<OrderDetailsDto>();
        // The order was checked out with its own DeliveryServiceId (svc-1, not this new service) -
        // this proves editing a DIFFERENT service's fee never reaches back into any order snapshot.
        Assert.Equal("svc-1", reloaded!.DeliveryServiceId);
    }

    [Fact]
    public async Task Deactivating_a_delivery_service_is_a_soft_archive_never_a_hard_delete()
    {
        var service = await (await _client.PostAsJsonAsync("/api/delivery/services",
            new CreateDeliveryServiceRequest($"req-{Guid.NewGuid():N}", "خدمة ستُعطّل", 1000, _dispatcherId, null, null)))
            .Content.ReadFromJsonAsync<DeliveryServiceDto>();

        await _client.PutAsJsonAsync($"/api/delivery/services/{service!.Id}",
            new UpdateDeliveryServiceRequest(service.Id, null, null, false, _dispatcherId, null, null));

        var active = await (await _client.GetAsync("/api/delivery/services")).Content.ReadFromJsonAsync<List<DeliveryServiceDto>>();
        Assert.DoesNotContain(active!, s => s.Id == service.Id);

        var all = await (await _client.GetAsync("/api/delivery/services/all")).Content.ReadFromJsonAsync<List<DeliveryServiceDto>>();
        Assert.Contains(all!, s => s.Id == service.Id && !s.Active);
    }

    // ---- Driver account view (driverAccountView/driverOperationalStatus - a real gap found during
    // the legacy-parity audit: the underlying rows all existed, only this read-model was missing) ----

    [Fact]
    public async Task A_driver_with_no_activity_is_available_and_cannot_be_settled()
    {
        var driverId = await CreateDriverAsync();
        var account = await (await _client.GetAsync($"/api/delivery/drivers/{driverId}/account")).Content.ReadFromJsonAsync<DriverAccountDto>();
        Assert.Equal(DriverOperationalStatus.Available, account!.OperationalStatus);
        Assert.False(account.CanSettle);
        Assert.Equal(0, account.ActiveOrderCount);
    }

    [Fact]
    public async Task A_driver_out_for_delivery_is_out_and_blocks_settlement()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 10000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);

        var account = await (await _client.GetAsync($"/api/delivery/drivers/{driverId}/account")).Content.ReadFromJsonAsync<DriverAccountDto>();
        Assert.Equal(DriverOperationalStatus.Out, account!.OperationalStatus);
        Assert.Equal(1, account.ActiveOrderCount);
        Assert.False(account.CanSettle);
        Assert.Single(account.BlockingOrders, b => b.DeliveryId == deliveryId);
    }

    [Fact]
    public async Task A_driver_with_a_delivered_unsettled_order_can_be_settled()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 10000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);
        await MarkReadyAsync(deliveryId);
        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);
        await RecordResultAsync(deliveryId, "Delivered");

        var account = await (await _client.GetAsync($"/api/delivery/drivers/{driverId}/account")).Content.ReadFromJsonAsync<DriverAccountDto>();
        Assert.True(account!.CanSettle);
        Assert.Equal(1, account.CompletedUnsettledCount);
        Assert.Equal(10000, account.CashCustodyPiastres);

        await SettleAsync(driverId, 10000);
        var afterSettle = await (await _client.GetAsync($"/api/delivery/drivers/{driverId}/account")).Content.ReadFromJsonAsync<DriverAccountDto>();
        Assert.Equal(0, afterSettle!.CompletedUnsettledCount);
        Assert.Equal(1, afterSettle.AllTimeSettledOrderCount);
        Assert.Equal(10000, afterSettle.AllTimeSettledCashPiastres);
    }

    /// <summary>
    /// Cash basis, end to end: New -> Ready -> OutForDelivery -> Delivered -> driver settlement.
    /// A cash-on-delivery sale must never be reported as money the restaurant holds until the
    /// settlement actually moves it into the restaurant's treasury. Before that it is with the
    /// customer (undelivered) or in the driver's custody (delivered, unsettled) - so the report has
    /// to show it as CashWithDrivers and keep it out of CollectedSales at every step.
    /// </summary>
    [Fact]
    public async Task Cash_on_delivery_is_not_reported_as_collected_until_the_driver_settles()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 20000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);

        async Task<Falah.Application.Reports.SalesReportDto> SalesAsync() =>
            (await (await _client.GetAsync($"/api/reports/sales?actorUserId={_managerId}")).Content
                .ReadFromJsonAsync<Falah.Application.Reports.SalesReportDto>())!;

        // New - the customer still has the cash.
        var atNew = await SalesAsync();
        Assert.Equal(20000, atNew.NetSalesPiastres);
        Assert.Equal(20000, atNew.CashWithDriversPiastres);
        Assert.Equal(0, atNew.CollectedSalesPiastres);

        await MarkReadyAsync(deliveryId);
        Assert.Equal(0, (await SalesAsync()).CollectedSalesPiastres);

        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);
        Assert.Equal(0, (await SalesAsync()).CollectedSalesPiastres);

        // Delivered but unsettled: the driver holds it, not the restaurant.
        await RecordResultAsync(deliveryId, "Delivered");
        var afterDelivery = await SalesAsync();
        Assert.Equal(20000, afterDelivery.CashWithDriversPiastres);
        Assert.Equal(0, afterDelivery.CollectedSalesPiastres);

        // Settlement moves it into the restaurant's own treasury - only now is it collected.
        (await SettleAsync(driverId, 20000)).EnsureSuccessStatusCode();
        var afterSettlement = await SalesAsync();
        Assert.Equal(0, afterSettlement.CashWithDriversPiastres);
        Assert.Equal(20000, afterSettlement.CollectedSalesPiastres);
        Assert.Equal(20000, afterSettlement.NetSalesPiastres);
    }

    /// <summary>A delivery order paid electronically up front is not cash-on-delivery, so it must be
    /// governed by the pending-transfer rule alone and never double-excluded by the new one.</summary>
    [Fact]
    public async Task A_prepaid_delivery_order_is_not_treated_as_cash_with_drivers()
    {
        var shiftId = await OpenShiftAsync();
        await CheckoutDeliveryOrderAsync(shiftId, 15000, paymentMethod: "instapay");

        var sales = (await (await _client.GetAsync($"/api/reports/sales?actorUserId={_managerId}")).Content
            .ReadFromJsonAsync<Falah.Application.Reports.SalesReportDto>())!;

        Assert.Equal(0, sales.CashWithDriversPiastres);
        Assert.Equal(15000, sales.PendingElectronicSalesPiastres);
        Assert.Equal(0, sales.CollectedSalesPiastres);
    }

    /// <summary>The dispatch board must show a delivery from the moment checkout creates it - while
    /// it is still `New` and before anyone has marked it ready. /api/delivery/ready deliberately
    /// cannot do this (it filters to Ready only), which left a dispatcher unable to see the very
    /// orders they were supposed to act on.</summary>
    [Fact]
    public async Task The_dispatch_board_lists_a_delivery_while_it_is_still_new()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 12500);

        var board = await (await _client.GetAsync("/api/delivery/board")).Content.ReadFromJsonAsync<List<DeliveryDto>>();
        var ready = await (await _client.GetAsync("/api/delivery/ready")).Content.ReadFromJsonAsync<List<DeliveryDto>>();

        var onBoard = Assert.Single(board!);
        Assert.Equal(order.OrderId, onBoard.OrderId);
        Assert.Equal("New", onBoard.Status);
        Assert.Empty(ready!);
    }

    /// <summary>The board tracks the delivery across the whole live lifecycle and then drops it once
    /// it reaches a terminal state - the board is the LIVE view, not a history.</summary>
    [Fact]
    public async Task The_dispatch_board_follows_the_lifecycle_and_excludes_terminal_deliveries()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryOrderAsync(shiftId, 8000);
        var deliveryId = await GetDeliveryIdForOrderAsync(order.OrderId);

        await MarkReadyAsync(deliveryId);
        var afterReady = await (await _client.GetAsync("/api/delivery/board")).Content.ReadFromJsonAsync<List<DeliveryDto>>();
        Assert.Equal("Ready", Assert.Single(afterReady!).Status);

        var driverId = await CreateDriverAsync();
        await AssignAsync(deliveryId, driverId);
        var afterAssign = await (await _client.GetAsync("/api/delivery/board")).Content.ReadFromJsonAsync<List<DeliveryDto>>();
        var assigned = Assert.Single(afterAssign!);
        Assert.Equal("OutForDelivery", assigned.Status);
        Assert.Equal(driverId, assigned.DriverId);

        await RecordResultAsync(deliveryId, "Delivered");
        var afterDelivered = await (await _client.GetAsync("/api/delivery/board")).Content.ReadFromJsonAsync<List<DeliveryDto>>();
        Assert.Empty(afterDelivered!);
    }
}

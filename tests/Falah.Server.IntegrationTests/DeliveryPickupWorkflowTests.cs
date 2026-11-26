namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Workflow redesign, owner decisions 2026-11-24. Delivery: no card; the kitchen paper prints automatically; cancelling
/// an unpaid order needs no admin code before the kitchen has its paper (every ingredient back) and a manager after it
/// (the operator says whether the food was prepared); failure reasons with "other" typed; the order reads completed when
/// delivered; a paid one that failed is flagged for a refund. Pickup: the kitchen paper prints when it is taken, the
/// customer's receipt when it is handed over; paying in advance is not collecting, so a prepaid pickup stays open until
/// handed over; the queue shows every order with its real stage and next action.
/// </summary>
public class DeliveryPickupWorkflowTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private HttpClient _ticketClient = default!;
    private string _cashierId = default!;
    private string _dispatcherId = default!;
    private string _managerId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _dispatcherId = await _factory.SeedUserAsync("موظف التوصيل", "dispatcher", "delivery_manage");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        var agent = await _factory.SeedUserAsync("وكيل", "cashier", "print_agent");
        _ticketClient = _factory.CreateClient();
        _ticketClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.TokenFor(agent));
        _factory.StationUserId = _cashierId;
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        await _factory.SeedDeliveryServiceAsync("svc-1", 0, "توصيل سريع");
        _shiftId = (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50_000))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    // ---- helpers ----

    private async Task<HttpResponseMessage> PostOrderAsync(string orderType, string paymentMethod, string productId, long total,
        string customer = "عميل", bool payOnCollection = false)
    {
        var delivery = orderType == "delivery";
        return await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: orderType, PaymentMethod: paymentMethod,
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: payOnCollection || (delivery && paymentMethod == "cash") ? 0 : total, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: customer, CustomerPhone: "0100" + Random.Shared.Next(1_000_000, 9_999_999),
            DeliveryAddress: null, DeliveryArea: delivery ? "المنطقة أ" : null, BusinessDayId: null,
            DeliveryStreet: delivery ? "شارع 9" : null, DeliveryServiceId: delivery ? "svc-1" : null, DeliveryServiceName: delivery ? "توصيل سريع" : null,
            PayOnCollection: payOnCollection));
    }

    private async Task<CheckoutOrderResult> OrderAsync(string orderType, string paymentMethod, string productId, long total,
        string customer = "عميل", bool payOnCollection = false)
    {
        var response = await PostOrderAsync(orderType, paymentMethod, productId, total, customer, payOnCollection);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    private Task<List<QueueOrderDto>?> QueueAsync() => _client.GetFromJsonAsync<List<QueueOrderDto>>("/api/orders/queue");
    private async Task<QueueOrderDto> QueuedAsync(string orderId) => (await QueueAsync())!.Single(q => q.OrderId == orderId);

    /// <summary>What the print agent does: the order's papers reach the printer.</summary>
    private Task KitchenPrintsAsync(string orderId) => _factory.QueryAsync(db =>
        db.PrintJobs.Where(j => j.OrderId == orderId && j.Status == "Pending").ExecuteUpdateAsync(s => s.SetProperty(j => j.Status, "Succeeded")));

    private Task<string> DeliveryIdAsync(string orderId) =>
        _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == orderId).Select(d => d.Id).FirstAsync());

    private async Task<string> ApprovalAsync(string action) =>
        (await (await _factory.PostAsSessionOfAsync(_client, _cashierId, "/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), action, "اعتماد"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!.ApprovalId;

    private Task<long> StockAsync(string itemId) => _factory.QueryAsync(db => db.StockMovements.Where(m => m.InventoryItemId == itemId)
        .SumAsync(m => m.Direction == "In" ? m.QuantityMilliUnits : -m.QuantityMilliUnits));

    private Task<int> MovementsAsync(string orderId, string type) =>
        _factory.QueryAsync(db => db.StockMovements.CountAsync(m => m.ReferenceId == orderId && m.Type == type));

    private Task<HttpResponseMessage> CancelDeliveryAsync(string orderId, string? disposition, string? approvalId = null) =>
        _client.PostAsJsonAsync("/api/orders/cancel-undispatched-delivery", new CancelUndispatchedDeliveryRequest(
            $"cud-{Guid.NewGuid():N}", orderId, "العميل لغى", _cashierId, disposition, approvalId, approvalId is null ? null : _managerId));

    // ---- Delivery ----

    [Fact]
    public async Task A_delivery_paid_by_card_is_refused_and_nothing_is_recorded()
    {
        var product = await _factory.SeedProductAsync(10_000);

        var response = await PostOrderAsync("delivery", "card", product, 10_000);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("البطاقة", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, await _factory.QueryAsync(db => db.Orders.CountAsync()));
    }

    [Fact]
    public async Task Several_deliveries_and_pickups_sit_in_the_queue_each_with_its_own_customer_stage_and_next_action()
    {
        var product = await _factory.SeedProductAsync(10_000);
        var d1 = await OrderAsync("delivery", "cash", product, 10_000, "أحمد");
        var d2 = await OrderAsync("delivery", "instapay", product, 10_000, "محمد");
        var p1 = await OrderAsync("pickup", "cash", product, 10_000, "علي", payOnCollection: true);
        var p2 = await OrderAsync("pickup", "cash", product, 10_000, "عمر");
        await KitchenPrintsAsync(d1.OrderId);
        await KitchenPrintsAsync(p1.OrderId);
        await KitchenPrintsAsync(p2.OrderId);

        var queue = (await QueueAsync())!;

        Assert.Equal(4, queue.Count);
        var q1 = queue.Single(q => q.OrderId == d1.OrderId);
        Assert.Equal(("أحمد", "preparing", "mark_ready", "cash_on_delivery"), (q1.CustomerName, q1.Stage, q1.NextAction, q1.PaymentState));
        // Its kitchen paper never printed: shown as new with a print button beside it, but never held back (owner review).
        var q2 = queue.Single(q => q.OrderId == d2.OrderId);
        Assert.Equal(("محمد", "new", "mark_ready", "pending_transfer", "print_kitchen"),
            (q2.CustomerName, q2.Stage, q2.NextAction, q2.PaymentState, q2.SecondaryAction));
        Assert.Equal(("علي", "collect", "pay_at_counter"), (queue.Single(q => q.OrderId == p1.OrderId).CustomerName, queue.Single(q => q.OrderId == p1.OrderId).NextAction, queue.Single(q => q.OrderId == p1.OrderId).PaymentState));
        Assert.Equal(("عمر", "hand_over", "paid"), (queue.Single(q => q.OrderId == p2.OrderId).CustomerName, queue.Single(q => q.OrderId == p2.OrderId).NextAction, queue.Single(q => q.OrderId == p2.OrderId).PaymentState));

        // Moving one leaves the others where they were.
        (await _client.PostAsJsonAsync($"/api/delivery/{await DeliveryIdAsync(d1.OrderId)}/ready",
            new MarkDeliveryReadyRequest(await DeliveryIdAsync(d1.OrderId), _dispatcherId, null, null))).EnsureSuccessStatusCode();
        var after = (await QueueAsync())!;
        Assert.Equal("ready", after.Single(q => q.OrderId == d1.OrderId).Stage);
        Assert.Equal("new", after.Single(q => q.OrderId == d2.OrderId).Stage);
        Assert.Equal("preparing", after.Single(q => q.OrderId == p1.OrderId).Stage);
    }

    /// <summary>Owner review 2026-11-24: with the printer off, an order must still be prepared, dispatched and handed over.</summary>
    [Fact]
    public async Task A_printer_that_is_off_never_holds_an_order_back()
    {
        var product = await _factory.SeedProductAsync(10_000);
        var delivery = await OrderAsync("delivery", "cash", product, 10_000);
        var pickup = await OrderAsync("pickup", "cash", product, 10_000);   // no kitchen paper printed for either

        var d = await QueuedAsync(delivery.OrderId);
        var p = await QueuedAsync(pickup.OrderId);
        Assert.Equal(("mark_ready", "print_kitchen", false), (d.NextAction, d.SecondaryAction, d.CancelNeedsAdmin));
        Assert.Equal(("hand_over", "print_kitchen"), (p.NextAction, p.SecondaryAction));
        Assert.Contains("ورقة المطبخ", d.Attention);

        (await _client.PostAsJsonAsync("/api/orders/hand-over-pickup",
            new HandOverPickupRequest($"ho-{Guid.NewGuid():N}", pickup.OrderId, _cashierId))).EnsureSuccessStatusCode();
        var deliveryId = await DeliveryIdAsync(delivery.OrderId);
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        // Ready and assignable; the missing paper is still offered beside it while the order is at the restaurant.
        Assert.Equal(("ready", "assign", "print_kitchen"), ((await QueuedAsync(delivery.OrderId)).Stage, (await QueuedAsync(delivery.OrderId)).NextAction, (await QueuedAsync(delivery.OrderId)).SecondaryAction));
    }

    [Fact]
    public async Task A_delivery_goes_through_ready_driver_delivered_and_settled_and_then_leaves_the_queue_completed()
    {
        var product = await _factory.SeedProductAsync(15_000);
        var order = await OrderAsync("delivery", "cash", product, 15_000);
        await KitchenPrintsAsync(order.OrderId);
        var deliveryId = await DeliveryIdAsync(order.OrderId);
        Assert.Equal("preparing", await _factory.QueryAsync(db => db.Orders.Where(o => o.Id == order.OrderId).Select(o => o.Status).FirstAsync()));

        _factory.StationUserId = _dispatcherId;
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        var driver = (await (await _client.PostAsJsonAsync("/api/delivery/drivers", new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق", _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>())!.Id;
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign", new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driver, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        Assert.Equal(("out", "record_result"), ((await QueuedAsync(order.OrderId)).Stage, (await QueuedAsync(order.OrderId)).NextAction));
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result", new RecordDeliveryResultRequest(
            $"req-{Guid.NewGuid():N}", deliveryId, "delivered", null, null, null, _dispatcherId, null, null, null))).EnsureSuccessStatusCode();
        // Owner, 2026-11-25: no settlement step on the queue - the driver is settled from his account.
        Assert.Equal(("delivered", null), ((await QueuedAsync(order.OrderId)).Stage, (await QueuedAsync(order.OrderId)).NextAction));
        Assert.Equal("completed", await _factory.QueryAsync(db => db.Orders.Where(o => o.Id == order.OrderId).Select(o => o.Status).FirstAsync()));

        (await _client.PostAsJsonAsync($"/api/delivery/drivers/{driver}/settle",
            new SettleDriverCustodyRequest($"req-{Guid.NewGuid():N}", driver, 15_000, null, _dispatcherId, null, null))).EnsureSuccessStatusCode();

        Assert.DoesNotContain((await QueueAsync())!, q => q.OrderId == order.OrderId);
    }

    // ---- «ترحيل» (owner, 2026-11-25) ----

    private async Task<string> DispatchAsync(string orderId)
    {
        var deliveryId = await DeliveryIdAsync(orderId);
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        var driver = (await (await _client.PostAsJsonAsync("/api/delivery/drivers", new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق", _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>())!.Id;
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign", new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driver, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        return deliveryId;
    }

    private Task<HttpResponseMessage> DeliveredAsync(string deliveryId) => _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result",
        new RecordDeliveryResultRequest($"req-{Guid.NewGuid():N}", deliveryId, "delivered", null, null, null, _dispatcherId, null, null, null));

    private Task<HttpResponseMessage> ClearAsync(string requestId, bool deliveries, bool pickups) =>
        _client.PostAsJsonAsync("/api/orders/queue/clear", new ClearOrderQueueRequest(requestId, deliveries, pickups, _cashierId));

    [Fact]
    public async Task Clearing_deliveries_removes_the_finished_ones_keeps_one_still_out_until_it_finishes_and_deletes_nothing()
    {
        var product = await _factory.SeedProductAsync(10_000);
        var done = await OrderAsync("delivery", "cash", product, 10_000, "خلص");
        var outWithDriver = await OrderAsync("delivery", "cash", product, 10_000, "مع السائق");
        var pickup = await OrderAsync("pickup", "cash", product, 10_000, "استلام");
        (await DeliveredAsync(await DispatchAsync(done.OrderId))).EnsureSuccessStatusCode();
        var stillOut = await DispatchAsync(outWithDriver.OrderId);
        (await _client.PostAsJsonAsync("/api/orders/hand-over-pickup",
            new HandOverPickupRequest($"ho-{Guid.NewGuid():N}", pickup.OrderId, _cashierId))).EnsureSuccessStatusCode();
        var ordersBefore = await _factory.QueryAsync(db => db.Orders.CountAsync());

        var response = await ClearAsync($"qclr-{Guid.NewGuid():N}", deliveries: true, pickups: false);

        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<ClearOrderQueueResult>())!;
        Assert.Equal((1, 1, false), (result.Removed, result.Kept, result.WasReplayed));
        var queue = (await QueueAsync())!;
        Assert.DoesNotContain(queue, q => q.OrderId == done.OrderId);
        Assert.Equal("out", queue.Single(q => q.OrderId == outWithDriver.OrderId).Stage);
        Assert.Equal("completed", queue.Single(q => q.OrderId == pickup.OrderId).Stage);   // pickups were not chosen
        Assert.Equal(ordersBefore, await _factory.QueryAsync(db => db.Orders.CountAsync()));
        Assert.Equal(1, await _factory.QueryAsync(db => db.AuditEvents.CountAsync(a => a.Category == "order_queue_clear" && a.ReferenceId == "delivery")));

        // A new order after the clear shows; the one still out leaves by itself once its cycle finishes.
        var next = await OrderAsync("delivery", "cash", product, 10_000, "جديد");
        (await DeliveredAsync(stillOut)).EnsureSuccessStatusCode();
        var after = (await QueueAsync())!;
        Assert.DoesNotContain(after, q => q.OrderId == outWithDriver.OrderId);
        Assert.Contains(after, q => q.OrderId == next.OrderId);
    }

    [Fact]
    public async Task Clearing_pickups_removes_the_handed_over_ones_and_keeps_one_the_customer_has_not_collected()
    {
        var product = await _factory.SeedProductAsync(10_000);
        var collected = await OrderAsync("pickup", "cash", product, 10_000, "استلم");
        var waiting = await OrderAsync("pickup", "cash", product, 10_000, "لسه", payOnCollection: true);
        var delivery = await OrderAsync("delivery", "cash", product, 10_000);
        (await _client.PostAsJsonAsync("/api/orders/hand-over-pickup",
            new HandOverPickupRequest($"ho-{Guid.NewGuid():N}", collected.OrderId, _cashierId))).EnsureSuccessStatusCode();

        var result = (await (await ClearAsync($"qclr-{Guid.NewGuid():N}", deliveries: false, pickups: true)).Content.ReadFromJsonAsync<ClearOrderQueueResult>())!;

        Assert.Equal((1, 1), (result.Removed, result.Kept));
        var queue = (await QueueAsync())!;
        Assert.DoesNotContain(queue, q => q.OrderId == collected.OrderId);
        Assert.Contains(queue, q => q.OrderId == waiting.OrderId);
        Assert.Contains(queue, q => q.OrderId == delivery.OrderId);
    }

    [Fact]
    public async Task A_clear_replays_by_its_request_id_and_needs_a_kind_chosen()
    {
        var product = await _factory.SeedProductAsync(10_000);
        var done = await OrderAsync("delivery", "cash", product, 10_000);
        (await DeliveredAsync(await DispatchAsync(done.OrderId))).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await ClearAsync($"qclr-{Guid.NewGuid():N}", false, false)).StatusCode);

        var id = $"qclr-{Guid.NewGuid():N}";
        (await ClearAsync(id, true, true)).EnsureSuccessStatusCode();
        var replay = (await (await ClearAsync(id, true, true)).Content.ReadFromJsonAsync<ClearOrderQueueResult>())!;

        Assert.Equal((1, 0, true), (replay.Removed, replay.Kept, replay.WasReplayed));
        Assert.Equal(2, await _factory.QueryAsync(db => db.AuditEvents.CountAsync(a => a.Category == "order_queue_clear")));
    }

    [Fact]
    public async Task Cancelling_before_the_kitchen_has_its_paper_needs_no_admin_and_gives_every_ingredient_back_once()
    {
        var (product, _, item) = await _factory.SeedProductWithRecipeAsync(12_000, 250);
        var before = await StockAsync(item);
        var order = await OrderAsync("delivery", "cash", product, 12_000);
        Assert.Equal(before - 250, await StockAsync(item));
        Assert.Equal((true, false), ((await QueuedAsync(order.OrderId)).CanCancel, (await QueuedAsync(order.OrderId)).CancelNeedsAdmin));

        (await CancelDeliveryAsync(order.OrderId, disposition: null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await CancelDeliveryAsync(order.OrderId, null)).StatusCode);   // already cancelled

        Assert.Equal(before, await StockAsync(item));
        Assert.Equal(1, await MovementsAsync(order.OrderId, "UsageReversal"));
        Assert.Equal(0, await MovementsAsync(order.OrderId, "Waste"));
        Assert.DoesNotContain((await QueueAsync())!, q => q.OrderId == order.OrderId);
    }

    [Theory]
    [InlineData("prepared")]
    [InlineData("not_prepared")]
    public async Task Cancelling_after_the_kitchen_paper_needs_a_manager_and_the_prepared_answer_decides_the_stock(string disposition)
    {
        var (product, _, item) = await _factory.SeedProductWithRecipeAsync(12_000, 250);
        var before = await StockAsync(item);
        var order = await OrderAsync("delivery", "cash", product, 12_000);
        await KitchenPrintsAsync(order.OrderId);
        Assert.True((await QueuedAsync(order.OrderId)).CancelNeedsAdmin);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await CancelDeliveryAsync(order.OrderId, disposition)).StatusCode);   // no admin code
        (await CancelDeliveryAsync(order.OrderId, disposition, await ApprovalAsync("delivery_override"))).EnsureSuccessStatusCode();

        Assert.Equal(1, await MovementsAsync(order.OrderId, "UsageReversal"));
        if (disposition == "prepared")
        {
            Assert.Equal(1, await MovementsAsync(order.OrderId, "Waste"));       // the food was made: it stays out of stock, as a loss
            Assert.Equal(before - 250, await StockAsync(item));
        }
        else
        {
            Assert.Equal(0, await MovementsAsync(order.OrderId, "Waste"));
            Assert.Equal(before, await StockAsync(item));
        }
    }

    [Fact]
    public async Task A_paid_delivery_cannot_be_cancelled_from_the_queue_it_is_refunded()
    {
        var product = await _factory.SeedProductAsync(12_000);
        var order = await OrderAsync("delivery", "instapay", product, 12_000);
        (await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment", new ConfirmElectronicPaymentRequest(order.OrderId, _cashierId))).EnsureSuccessStatusCode();

        Assert.False((await QueuedAsync(order.OrderId)).CanCancel);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await CancelDeliveryAsync(order.OrderId, null)).StatusCode);
    }

    [Fact]
    public async Task A_failure_needs_a_known_reason_and_other_needs_it_typed_and_a_cash_failure_voids_once()
    {
        var (product, _, item) = await _factory.SeedProductWithRecipeAsync(12_000, 250);
        var before = await StockAsync(item);
        var order = await OrderAsync("delivery", "cash", product, 12_000);
        await KitchenPrintsAsync(order.OrderId);
        var deliveryId = await DeliveryIdAsync(order.OrderId);
        _factory.StationUserId = _dispatcherId;
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        var driver = (await (await _client.PostAsJsonAsync("/api/delivery/drivers", new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق", _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>())!.Id;
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign", new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driver, _dispatcherId, null, null))).EnsureSuccessStatusCode();

        Task<HttpResponseMessage> Fail(string result, string reason, string? notes) => _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result",
            new RecordDeliveryResultRequest($"req-{Guid.NewGuid():N}", deliveryId, result, null, reason, notes, _dispatcherId, null, null, "prepared"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Fail("failed", "other", null)).StatusCode);
        (await Fail("failed", "phone_off", null)).EnsureSuccessStatusCode();
        (await Fail("returned", "رجع بالأكل", null)).EnsureSuccessStatusCode();

        Assert.Equal("cancelled", await _factory.QueryAsync(db => db.Orders.Where(o => o.Id == order.OrderId).Select(o => o.Status).FirstAsync()));
        Assert.Equal(1, await MovementsAsync(order.OrderId, "UsageReversal"));
        Assert.Equal(1, await MovementsAsync(order.OrderId, "Waste"));
        Assert.Equal(before - 250, await StockAsync(item));
    }

    [Fact]
    public async Task A_paid_delivery_that_failed_is_flagged_for_a_refund_in_the_queue()
    {
        var product = await _factory.SeedProductAsync(12_000);
        var order = await OrderAsync("delivery", "wallet", product, 12_000);
        (await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment", new ConfirmElectronicPaymentRequest(order.OrderId, _cashierId))).EnsureSuccessStatusCode();
        var deliveryId = await DeliveryIdAsync(order.OrderId);
        _factory.StationUserId = _dispatcherId;
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        var driver = (await (await _client.PostAsJsonAsync("/api/delivery/drivers", new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق", _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>())!.Id;
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign", new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driver, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        Assert.Equal(0, await _factory.QueryAsync(db => db.DriverCustodies.CountAsync(c => c.DeliveryId == deliveryId)));   // nothing on the driver
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result", new RecordDeliveryResultRequest(
            $"req-{Guid.NewGuid():N}", deliveryId, "failed", null, "customer_unreachable", null, _dispatcherId, null, null, null))).EnsureSuccessStatusCode();

        var queued = await QueuedAsync(order.OrderId);
        Assert.True(queued.NeedsRefund);
        Assert.Contains("مرتجع", queued.Attention);
    }

    // ---- Pickup ----

    [Fact]
    public async Task A_pickup_prints_its_kitchen_slip_when_taken_and_the_customer_receipt_only_when_handed_over()
    {
        var product = await _factory.SeedProductAsync(9_000);
        var order = await OrderAsync("pickup", "cash", product, 9_000);   // paid at the till in advance
        var taken = await _factory.QueryAsync(db => db.PrintJobs.Where(j => j.OrderId == order.OrderId).Select(j => j.Id).ToListAsync());
        var slip = await _ticketClient.GetFromJsonAsync<ComposedTicketDto>($"/api/print-jobs/{taken.Single()}/ticket");
        Assert.Contains(slip!.Lines, l => l.Text?.Contains("تحضير طلب") == true);
        await KitchenPrintsAsync(order.OrderId);
        Assert.Equal(("preparing", "hand_over"), ((await QueuedAsync(order.OrderId)).Stage, (await QueuedAsync(order.OrderId)).NextAction));
        Assert.Equal("preparing", await _factory.QueryAsync(db => db.Orders.Where(o => o.Id == order.OrderId).Select(o => o.Status).FirstAsync()));

        var request = new HandOverPickupRequest($"ho-{Guid.NewGuid():N}", order.OrderId, _cashierId);
        (await _client.PostAsJsonAsync("/api/orders/hand-over-pickup", request)).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync("/api/orders/hand-over-pickup", request)).EnsureSuccessStatusCode();   // a retry replays
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _client.PostAsJsonAsync("/api/orders/hand-over-pickup",
            request with { RequestId = $"ho-{Guid.NewGuid():N}" })).StatusCode);                              // a second press does not

        var receipts = await _factory.QueryAsync(db => db.PrintJobs.Where(j => j.OrderId == order.OrderId && j.DocumentKind == "PickupReceipt").Select(j => j.Id).ToListAsync());
        var receipt = await _ticketClient.GetFromJsonAsync<ComposedTicketDto>($"/api/print-jobs/{receipts.Single()}/ticket");
        Assert.DoesNotContain(receipt!.Lines, l => l.Text?.Contains("تحضير طلب") == true);
        Assert.Equal("completed", await _factory.QueryAsync(db => db.Orders.Where(o => o.Id == order.OrderId).Select(o => o.Status).FirstAsync()));
        Assert.Equal(("completed", (string?)null), ((await QueuedAsync(order.OrderId)).Stage, (await QueuedAsync(order.OrderId)).NextAction));
    }

    [Fact]
    public async Task An_unpaid_pickup_is_paid_and_handed_over_in_one_step_with_its_receipt()
    {
        var product = await _factory.SeedProductAsync(9_000);
        var order = await OrderAsync("pickup", "cash", product, 9_000, payOnCollection: true);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _client.PostAsJsonAsync("/api/orders/hand-over-pickup",
            new HandOverPickupRequest($"ho-{Guid.NewGuid():N}", order.OrderId, _cashierId))).StatusCode);   // not paid yet

        (await _client.PostAsJsonAsync("/api/orders/collect-pickup", new CollectPickupPaymentRequest(
            $"col-{Guid.NewGuid():N}", order.OrderId, _cashierId, _shiftId, "card", 9_000))).EnsureSuccessStatusCode();

        var row = await _factory.QueryAsync(db => db.Orders.FirstAsync(o => o.Id == order.OrderId));
        Assert.NotNull(row.HandedOverAtUtc);
        Assert.Equal("completed", row.Status);
        Assert.True(row.IsPaid);
        Assert.Equal(1, await _factory.QueryAsync(db => db.PrintJobs.CountAsync(j => j.OrderId == order.OrderId && j.DocumentKind == "PickupReceipt")));
    }

    [Fact]
    public async Task An_unpaid_pickup_is_cancelled_without_admin_before_its_kitchen_paper_and_only_with_a_manager_after()
    {
        var product = await _factory.SeedProductAsync(9_000);
        var early = await OrderAsync("pickup", "cash", product, 9_000, payOnCollection: true);
        (await _client.PostAsJsonAsync("/api/orders/cancel-uncollected-pickup", new CancelUncollectedPickupRequest(
            $"cp-{Guid.NewGuid():N}", early.OrderId, "العميل لغى", null!, _cashierId, null, null))).EnsureSuccessStatusCode();

        var late = await OrderAsync("pickup", "cash", product, 9_000, payOnCollection: true);
        await KitchenPrintsAsync(late.OrderId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _client.PostAsJsonAsync("/api/orders/cancel-uncollected-pickup", new CancelUncollectedPickupRequest(
            $"cp-{Guid.NewGuid():N}", late.OrderId, "العميل ما جاش", "prepared", _cashierId, null, null))).StatusCode);
        (await _client.PostAsJsonAsync("/api/orders/cancel-uncollected-pickup", new CancelUncollectedPickupRequest(
            $"cp-{Guid.NewGuid():N}", late.OrderId, "العميل ما جاش", "prepared", _cashierId, await ApprovalAsync("pickup_cancel"), _managerId))).EnsureSuccessStatusCode();

        var queue = (await QueueAsync())!;
        Assert.DoesNotContain(queue, q => q.OrderId == early.OrderId || q.OrderId == late.OrderId);
    }

    [Fact]
    public async Task A_pickup_whose_transfer_is_unconfirmed_is_cancelled_with_its_transfer_under_the_same_rule()
    {
        var product = await _factory.SeedProductAsync(9_000);
        var order = await OrderAsync("pickup", "instapay", product, 9_000);
        await KitchenPrintsAsync(order.OrderId);
        Assert.Equal(("pending_transfer", true), ((await QueuedAsync(order.OrderId)).PaymentState, (await QueuedAsync(order.OrderId)).CanCancel));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _client.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment",
            new CancelPendingElectronicPaymentRequest(order.OrderId, "ما وصلش", _cashierId, "not_prepared"))).StatusCode);
        (await _client.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment",
            new CancelPendingElectronicPaymentRequest(order.OrderId, "ما وصلش", _cashierId, "not_prepared", await ApprovalAsync("pickup_cancel"), _managerId))).EnsureSuccessStatusCode();
        Assert.DoesNotContain((await QueueAsync())!, q => q.OrderId == order.OrderId);
    }

    [Fact]
    public async Task A_prepaid_pickup_refunded_in_full_is_not_offered_or_accepted_for_handover_and_stays_refunded()
    {
        var product = await _factory.SeedProductAsync(9_000);
        var order = await OrderAsync("pickup", "cash", product, 9_000);   // paid at the till in advance
        await KitchenPrintsAsync(order.OrderId);
        (await _factory.PostAsSessionOfAsync(_client, _managerId, "/api/orders/refund", new RefundOrderRequest(
            $"rreq-{Guid.NewGuid():N}", order.OrderId, 9_000, "العميل ما جاش", _managerId, null, null))).EnsureSuccessStatusCode();
        Assert.Equal("refunded", await _factory.QueryAsync(db => db.Orders.Where(o => o.Id == order.OrderId).Select(o => o.Status).FirstAsync()));

        Assert.DoesNotContain((await QueueAsync())!, q => q.OrderId == order.OrderId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _client.PostAsJsonAsync("/api/orders/hand-over-pickup",
            new HandOverPickupRequest($"ho-{Guid.NewGuid():N}", order.OrderId, _cashierId))).StatusCode);

        var row = await _factory.QueryAsync(db => db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.OrderId));
        Assert.Equal("refunded", row.Status);
        Assert.Null(row.HandedOverAtUtc);
        Assert.Equal(0, await _factory.QueryAsync(db => db.PrintJobs.CountAsync(j => j.OrderId == order.OrderId && j.DocumentKind == "PickupReceipt")));
    }

    [Fact]
    public async Task A_handover_request_id_used_for_one_pickup_is_refused_for_another_and_leaves_it_open()
    {
        var product = await _factory.SeedProductAsync(9_000);
        var first = await OrderAsync("pickup", "cash", product, 9_000);
        var second = await OrderAsync("pickup", "cash", product, 9_000);
        var requestId = $"ho-{Guid.NewGuid():N}";
        (await _client.PostAsJsonAsync("/api/orders/hand-over-pickup", new HandOverPickupRequest(requestId, first.OrderId, _cashierId))).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _client.PostAsJsonAsync("/api/orders/hand-over-pickup",
            new HandOverPickupRequest(requestId, second.OrderId, _cashierId))).StatusCode);
        Assert.Null(await _factory.QueryAsync(db => db.Orders.Where(o => o.Id == second.OrderId).Select(o => o.HandedOverAtUtc).FirstAsync()));
        Assert.Equal("hand_over", (await QueuedAsync(second.OrderId)).NextAction);
    }

    [Fact]
    public async Task Two_stations_handing_over_the_same_pickup_at_once_hand_it_over_once_and_print_one_receipt()
    {
        var product = await _factory.SeedProductAsync(9_000);
        var order = await OrderAsync("pickup", "cash", product, 9_000);

        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => _client.PostAsJsonAsync("/api/orders/hand-over-pickup",
            new HandOverPickupRequest($"ho-{Guid.NewGuid():N}", order.OrderId, _cashierId))));

        Assert.Equal(1, results.Count(r => r.IsSuccessStatusCode));
        Assert.Equal(1, await _factory.QueryAsync(db => db.PrintJobs.CountAsync(j => j.OrderId == order.OrderId && j.DocumentKind == "PickupReceipt")));
    }
}

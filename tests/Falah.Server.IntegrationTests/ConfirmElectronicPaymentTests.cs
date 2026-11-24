namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ConfirmElectronicPaymentTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<CheckoutOrderResult> InstaPayOrderAsync(long totalPiastres = 15000, IReadOnlyList<CheckoutSplitRequest>? splits = null)
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: splits is null ? "instapay" : "multi",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: totalPiastres, SplitPayments: splits, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    [Fact]
    public async Task Confirming_a_pending_instapay_order_marks_it_paid_and_posts_exactly_one_movement()
    {
        var order = await InstaPayOrderAsync();
        var response = await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment",
            new ConfirmElectronicPaymentRequest(order.OrderId, _cashierId));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ConfirmElectronicPaymentResult>();

        Assert.False(result!.WasReplayed);
        Assert.Equal("confirmed", result.ElectronicPaymentStatus);
        Assert.True(result.IsPaid);

        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == order.OrderId));
        Assert.Equal(1, movementCount);
    }

    // Audit 2026-11-18: confirming a transfer posts money and was the one money command with no line in
    // the audit trail - a cashier could ring cash as InstaPay, keep the cash and confirm a transfer that
    // never came, and nothing named who confirmed it. Exactly one line, also after a replay.
    [Fact]
    public async Task Confirming_a_transfer_writes_one_audit_line_naming_who_confirmed_it_and_how_much()
    {
        var order = await InstaPayOrderAsync(15000);
        var request = new ConfirmElectronicPaymentRequest(order.OrderId, _cashierId);
        (await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment", request)).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment", request)).EnsureSuccessStatusCode();

        var lines = await _factory.QueryAsync(db => db.AuditEvents.AsNoTracking()
            .Where(e => e.ReferenceId == order.OrderId && e.Action == "تأكيد تحصيل إلكتروني").ToListAsync());
        var line = Assert.Single(lines);
        Assert.Equal(_cashierId, line.UserId);
        Assert.Contains("15000", line.Details);
        Assert.Contains(order.OrderNumber, line.Details);
    }

    [Fact]
    public async Task Retrying_confirm_sequentially_never_posts_a_second_movement()
    {
        var order = await InstaPayOrderAsync();
        var first = await (await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment", new ConfirmElectronicPaymentRequest(order.OrderId, _cashierId))).Content.ReadFromJsonAsync<ConfirmElectronicPaymentResult>();
        var second = await (await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment", new ConfirmElectronicPaymentRequest(order.OrderId, _cashierId))).Content.ReadFromJsonAsync<ConfirmElectronicPaymentResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(first.TreasuryMovementId, second.TreasuryMovementId);

        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == order.OrderId));
        Assert.Equal(1, movementCount);
    }

    [Fact]
    public async Task Concurrent_confirm_requests_on_the_same_order_post_exactly_one_movement()
    {
        var order = await InstaPayOrderAsync();
        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment", new ConfirmElectronicPaymentRequest(order.OrderId, _cashierId)));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<ConfirmElectronicPaymentResult>()));
        Assert.Equal(1, results.Count(r => !r!.WasReplayed));

        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == order.OrderId));
        Assert.Equal(1, movementCount);
    }

    [Fact]
    public async Task Confirming_a_cash_order_with_no_electronic_leg_is_rejected()
    {
        var cashProductId = await _factory.SeedProductAsync(10000);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(cashProductId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 10000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        var checkoutResponse = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        var cashOrder = (await checkoutResponse.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;

        var response = await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment", new ConfirmElectronicPaymentRequest(cashOrder.OrderId, _cashierId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == cashOrder.OrderId));
        Assert.Equal(1, movementCount); // only the original (never-deferred) cash movement from checkout
    }

    // Legacy-parity audit (2026-09-04): this endpoint had no permission check and no open-shift
    // check at all - any caller who knew a valid orderId + any existing cashierId could confirm a
    // claimed electronic payment and post real money. Fixed to require 'pos'/'cash_drawer_sale' and
    // an active shift, matching confirmElectronicPayment in lib/store.tsx.
    [Fact]
    public async Task Confirming_without_pos_or_cash_drawer_sale_permission_is_rejected()
    {
        var order = await InstaPayOrderAsync();
        var noPermissionUser = await _factory.SeedUserAsync("موظف بلا صلاحية", "cashier");
        var response = await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment",
            new ConfirmElectronicPaymentRequest(order.OrderId, noPermissionUser));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == order.OrderId));
        Assert.Equal(0, movementCount);
    }

    [Fact]
    public async Task Confirming_with_no_open_shift_anywhere_is_rejected()
    {
        var order = await InstaPayOrderAsync();
        var closeResponse = await _client.PostAsJsonAsync("/api/shifts/close",
            new Falah.Application.Shifts.CloseShiftRequest($"creq-{Guid.NewGuid():N}", _shiftId, _cashierId, 50000, null, null, null));
        closeResponse.EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment",
            new ConfirmElectronicPaymentRequest(order.OrderId, _cashierId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // cancelPendingElectronicPayment - ported from lib/store.tsx, previously fully unreachable (the
    // domain method existed and was unit-tested, but no Application/Handler/endpoint wired it up).
    [Fact]
    public async Task Cancelling_a_pending_instapay_order_marks_it_cancelled_and_posts_no_movement()
    {
        var order = await InstaPayOrderAsync();
        var response = await _client.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment",
            new CancelPendingElectronicPaymentRequest(order.OrderId, "لم يصل التحويل", _cashierId, StockDisposition: "not_prepared"));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CancelPendingElectronicPaymentResult>();

        Assert.False(result!.WasReplayed);
        Assert.Equal("cancelled", result.Status);
        Assert.Equal("cancelled", result.ElectronicPaymentStatus);

        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == order.OrderId));
        Assert.Equal(0, movementCount);
    }

    [Fact]
    public async Task Cancelling_the_same_pending_order_twice_is_a_harmless_replay()
    {
        var order = await InstaPayOrderAsync();
        var first = await (await _client.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment",
            new CancelPendingElectronicPaymentRequest(order.OrderId, "سبب", _cashierId, StockDisposition: "not_prepared"))).Content.ReadFromJsonAsync<CancelPendingElectronicPaymentResult>();
        var second = await (await _client.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment",
            new CancelPendingElectronicPaymentRequest(order.OrderId, "سبب آخر", _cashierId, StockDisposition: "not_prepared"))).Content.ReadFromJsonAsync<CancelPendingElectronicPaymentResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
    }

    [Fact]
    public async Task Cancelling_an_already_confirmed_payment_is_rejected_use_refund_instead()
    {
        var order = await InstaPayOrderAsync();
        (await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment",
            new ConfirmElectronicPaymentRequest(order.OrderId, _cashierId))).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment",
            new CancelPendingElectronicPaymentRequest(order.OrderId, "سبب", _cashierId, StockDisposition: "not_prepared"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Cancelling_a_pending_payment_requires_a_reason()
    {
        var order = await InstaPayOrderAsync();
        var response = await _client.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment",
            new CancelPendingElectronicPaymentRequest(order.OrderId, "", _cashierId, StockDisposition: "not_prepared"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Cancelling_without_pos_or_cash_drawer_sale_permission_is_rejected()
    {
        var order = await InstaPayOrderAsync();
        var noPermissionUser = await _factory.SeedUserAsync("موظف بلا صلاحية", "cashier");
        var response = await _client.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment",
            new CancelPendingElectronicPaymentRequest(order.OrderId, "سبب", noPermissionUser, StockDisposition: "not_prepared"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_split_electronic_leg_posts_once_and_replays_its_own_movement()
    {
        var order = await InstaPayOrderAsync(15000, new[] { new CheckoutSplitRequest("cash", 5000), new CheckoutSplitRequest("instapay", 10000) });
        var request = new ConfirmElectronicPaymentRequest(order.OrderId, _cashierId);
        var response = await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment", request);
        response.EnsureSuccessStatusCode();
        var first = (await response.Content.ReadFromJsonAsync<ConfirmElectronicPaymentResult>())!;
        var replay = (await (await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment", request)).Content.ReadFromJsonAsync<ConfirmElectronicPaymentResult>())!;
        Assert.True(first.IsPaid);
        Assert.True(replay.WasReplayed);
        Assert.Equal(first.TreasuryMovementId, replay.TreasuryMovementId);
        var movements = await _factory.QueryAsync(db => db.TreasuryMovements.Where(m => m.ReferenceId == order.OrderId).ToListAsync());
        Assert.Equal(2, movements.Count);
        Assert.Equal(10000, Assert.Single(movements.Where(m => m.Id == replay.TreasuryMovementId)).AmountPiastres);
        Assert.Equal(15000, movements.Sum(m => m.AmountPiastres));
    }

    [Theory]
    [InlineData("wallet")]
    [InlineData("instapay")]
    public async Task A_split_cannot_accept_two_electronic_claims(string secondMethod)
    {
        var productId = await _factory.SeedProductAsync(15000);
        var request = new CheckoutOrderRequest($"req-{Guid.NewGuid():N}", _cashierId, _shiftId, "takeaway", "multi",
            new[] { new CheckoutItemRequest(productId, null, 1, null, null) }, 0, 0, 0, null, null, null, 15000,
            new[] { new CheckoutSplitRequest("instapay", 5000), new CheckoutSplitRequest(secondMethod, 10000) }, null, null, null, null, null, null, null);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.False(await _factory.QueryAsync(db => db.Orders.AnyAsync(o => o.CheckoutRequestId == request.CheckoutRequestId)));
    }


    /// <summary>Owner decisions 2026-11-24: a split is refused on a new delivery (D3), and a delivery whose transfer is
    /// not confirmed does not go out (D1). A split delivery written before those rules - its InstaPay leg still pending -
    /// is therefore held at the restaurant until the transfer is confirmed, instead of going out with a custody.</summary>
    [Fact]
    public async Task A_split_delivery_from_before_the_rules_does_not_go_out_while_its_transfer_is_pending()
    {
        var order = await InstaPayOrderAsync(15000, new[] { new CheckoutSplitRequest("cash", 5000), new CheckoutSplitRequest("instapay", 10000) });
        // Reuse the actual checkout snapshot as a delivery order: the composer must not charge the cash twice.
        await _factory.QueryAsync(async db => {
            await db.Orders.Where(o => o.Id == order.OrderId).ExecuteUpdateAsync(s => s.SetProperty(o => o.OrderType, "delivery"));
            db.Deliveries.Add(new Falah.Infrastructure.Persistence.Entities.DeliveryEntity { Id = $"del-{Guid.NewGuid():N}", OrderId = order.OrderId, ShiftId = _shiftId, Status = "Ready", CreatedBy = "test", CreatedAtUtc = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync(); return true;
        });
        var dispatcher = await _factory.SeedUserAsync("التوصيل", "manager", "all");
        var driverResponse = await _client.PostAsJsonAsync("/api/delivery/drivers", new Falah.Application.Delivery.CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق", dispatcher));
        driverResponse.EnsureSuccessStatusCode();
        var driver = (await driverResponse.Content.ReadFromJsonAsync<Falah.Application.Delivery.DriverDto>())!;
        var deliveryId = await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == order.OrderId).Select(d => d.Id).FirstAsync());
        var response = await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign", new Falah.Application.Delivery.AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driver.Id, dispatcher, null, null));
        Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await _factory.QueryAsync(db => db.DriverCustodies.CountAsync(c => c.DeliveryId == deliveryId)));
    }

}

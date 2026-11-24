namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Delivery audit 2026-11-24, owner decisions the same day:
/// D1 - a delivery whose InstaPay/wallet transfer is not confirmed cannot be handed to a driver (it used to open a
///      cash custody for the whole order on him, so a later confirmation counted the money twice);
/// D2 - cancelling that transfer cancels the delivery still at the restaurant, so it leaves the board and can never
///      be sent out;
/// D3 - a split payment is refused on a delivery order (its cash leg used to post into the till's drawer at once,
///      while the driver is the one who collects it);
/// D4 - the business day does not close while a delivery is unfinished or its driver unsettled, so an order of a
///      closed day can never be voided the next day.
/// </summary>
public class DeliveryPaymentIntegrityTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _dispatcherId = default!;
    /// <summary>The Control Center's reader of the close blockers - a manager's session.</summary>
    private HttpClient _manager = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _dispatcherId = await _factory.SeedUserAsync("موظف التوصيل", "dispatcher", "delivery_manage");
        var managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _manager = _factory.CreateClient();
        _manager.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.TokenFor(managerId));
        _factory.StationUserId = _dispatcherId;
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        await _factory.SeedDeliveryServiceAsync("svc-1", 0, "توصيل سريع");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> OpenShiftAsync() =>
        (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000)))
            .Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;

    private async Task<HttpResponseMessage> PostDeliveryCheckoutAsync(string shiftId, long totalPiastres, string paymentMethod,
        IReadOnlyList<CheckoutSplitRequest>? splits = null)
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        return await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "delivery", PaymentMethod: paymentMethod,
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: paymentMethod == "cash" ? 0 : totalPiastres, SplitPayments: splits, WalletProvider: null,
            TableNumber: null, CustomerName: "عميل اختبار", CustomerPhone: "01000000000", DeliveryAddress: null,
            DeliveryArea: "المنطقة أ", BusinessDayId: null, DeliveryStreet: "شارع 9",
            DeliveryServiceId: "svc-1", DeliveryServiceName: "توصيل سريع"));
    }

    private async Task<CheckoutOrderResult> CheckoutDeliveryAsync(string shiftId, long totalPiastres, string paymentMethod)
    {
        var response = await PostDeliveryCheckoutAsync(shiftId, totalPiastres, paymentMethod);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    private Task<string> DeliveryIdOfAsync(string orderId) =>
        _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == orderId).Select(d => d.Id).FirstAsync());

    private async Task<string> CreateDriverAsync() =>
        (await (await _client.PostAsJsonAsync("/api/delivery/drivers",
            new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق اختبار", _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>())!.Id;

    private Task<HttpResponseMessage> MarkReadyAsync(string deliveryId) =>
        _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null));

    private Task<HttpResponseMessage> AssignAsync(string deliveryId, string driverId) =>
        _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign",
            new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driverId, _dispatcherId, null, null));

    private Task<string> OpenDayIdAsync() =>
        _factory.QueryAsync(db => db.BusinessDays.Where(d => d.Status == "Open").Select(d => d.Id).FirstAsync());

    // ---- D1 ----

    [Fact]
    public async Task A_delivery_whose_transfer_is_not_confirmed_cannot_be_given_to_a_driver()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryAsync(shiftId, 12000, "instapay");
        var deliveryId = await DeliveryIdOfAsync(order.OrderId);
        (await MarkReadyAsync(deliveryId)).EnsureSuccessStatusCode();
        var driverId = await CreateDriverAsync();

        var response = await AssignAsync(deliveryId, driverId);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("التحويل", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, await _factory.QueryAsync(db => db.DriverCustodies.CountAsync(c => c.DeliveryId == deliveryId)));
        Assert.Equal("Ready", await _factory.QueryAsync(db => db.Deliveries.Where(d => d.Id == deliveryId).Select(d => d.Status).FirstAsync()));
    }

    [Fact]
    public async Task Once_the_transfer_is_confirmed_the_delivery_goes_out_with_nothing_for_the_driver_to_collect()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryAsync(shiftId, 12000, "instapay");
        var deliveryId = await DeliveryIdOfAsync(order.OrderId);
        (await MarkReadyAsync(deliveryId)).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment",
            new ConfirmElectronicPaymentRequest(order.OrderId, _cashierId))).EnsureSuccessStatusCode();
        var driverId = await CreateDriverAsync();

        (await AssignAsync(deliveryId, driverId)).EnsureSuccessStatusCode();

        Assert.Equal(0, await _factory.QueryAsync(db => db.DriverCustodies.CountAsync(c => c.DeliveryId == deliveryId)));
    }

    // ---- D2 ----

    [Fact]
    public async Task Cancelling_the_transfer_of_a_delivery_still_at_the_restaurant_cancels_the_delivery_too()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryAsync(shiftId, 12000, "instapay");
        var deliveryId = await DeliveryIdOfAsync(order.OrderId);
        (await MarkReadyAsync(deliveryId)).EnsureSuccessStatusCode();

        (await _client.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment",
            new CancelPendingElectronicPaymentRequest(order.OrderId, "التحويل ما وصلش", _cashierId, "not_prepared"))).EnsureSuccessStatusCode();

        Assert.Equal("Cancelled", await _factory.QueryAsync(db => db.Deliveries.Where(d => d.Id == deliveryId).Select(d => d.Status).FirstAsync()));
        var board = await _client.GetFromJsonAsync<List<DeliveryDto>>("/api/delivery/board");
        Assert.DoesNotContain(board!, d => d.DeliveryId == deliveryId);
        var driverId = await CreateDriverAsync();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await AssignAsync(deliveryId, driverId)).StatusCode);
    }

    // ---- D3 ----

    [Fact]
    public async Task A_split_payment_is_refused_on_a_delivery_order_and_nothing_is_recorded()
    {
        var shiftId = await OpenShiftAsync();

        var response = await PostDeliveryCheckoutAsync(shiftId, 10000, "multi",
            new[] { new CheckoutSplitRequest("cash", 5000), new CheckoutSplitRequest("card", 5000) });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await _factory.QueryAsync(db => db.Orders.CountAsync(o => o.OrderType == "delivery")));
        Assert.Equal(0, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "sale_collection")));
    }

    // ---- D4 ----

    [Fact]
    public async Task A_cash_delivery_that_has_not_left_blocks_the_business_day_close()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryAsync(shiftId, 12000, "cash");

        var blockers = await _manager.GetStringAsync($"/api/business-day/{await OpenDayIdAsync()}/close-blockers");

        Assert.Contains("UNFINISHED_DELIVERY", blockers);
        Assert.Contains(order.OrderNumber, blockers);
    }

    [Fact]
    public async Task A_paid_delivery_delivered_but_not_settled_with_its_driver_blocks_the_close_until_settled()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryAsync(shiftId, 12000, "card");
        var deliveryId = await DeliveryIdOfAsync(order.OrderId);
        (await MarkReadyAsync(deliveryId)).EnsureSuccessStatusCode();
        var driverId = await CreateDriverAsync();
        (await AssignAsync(deliveryId, driverId)).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result", new RecordDeliveryResultRequest(
            $"req-{Guid.NewGuid():N}", deliveryId, "delivered", 0, null, null, _dispatcherId, null, null, null))).EnsureSuccessStatusCode();
        var dayId = await OpenDayIdAsync();

        Assert.Contains("UNFINISHED_DELIVERY", await _manager.GetStringAsync($"/api/business-day/{dayId}/close-blockers"));

        (await _client.PostAsJsonAsync($"/api/delivery/drivers/{driverId}/settle",
            new SettleDriverCustodyRequest($"req-{Guid.NewGuid():N}", driverId, 0, null, _dispatcherId, null, null))).EnsureSuccessStatusCode();

        Assert.DoesNotContain("UNFINISHED_DELIVERY", await _manager.GetStringAsync($"/api/business-day/{dayId}/close-blockers"));
    }

    [Fact]
    public async Task A_delivery_cancelled_before_it_left_does_not_block_the_close()
    {
        var shiftId = await OpenShiftAsync();
        var order = await CheckoutDeliveryAsync(shiftId, 12000, "instapay");
        (await _client.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment",
            new CancelPendingElectronicPaymentRequest(order.OrderId, "التحويل ما وصلش", _cashierId, "not_prepared"))).EnsureSuccessStatusCode();

        Assert.DoesNotContain("UNFINISHED_DELIVERY", await _manager.GetStringAsync($"/api/business-day/{await OpenDayIdAsync()}/close-blockers"));
    }
}

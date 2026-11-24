namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Shifts;

/// <summary>Owner decision 2026-11-23 (independent audit F-08): without orders_view_all, a user sees only the
/// orders of the shift that is open now - the order list already worked that way, but opening an order by id
/// or by its number returned any order ever taken, with the customer's name, phone and address. Work still in
/// progress from an earlier shift (a pickup waiting to be paid, a transfer waiting to be confirmed) stays
/// reachable, or the till could not finish it.</summary>
public class OrderVisibilityTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private string _managerId = default!;
    private string _firstCashierId = default!;
    private string _secondCashierId = default!;
    private string _productId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        Assert.StartsWith("FalahTest_", _factory.DatabaseName);
        _managerId = await _factory.SeedUserAsync("مدير", "manager", "all");
        _firstCashierId = await _factory.SeedUserAsync("كاشير الصبح", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _secondCashierId = await _factory.SeedUserAsync("كاشير الليل", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        _productId = await _factory.SeedProductAsync(5_000);
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private HttpClient As(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId));
        return client;
    }

    private async Task<string> OpenShiftAsync(string userId) =>
        (await (await As(userId).PostAsJsonAsync("/api/shifts/open", new OpenShiftRequest($"open-{Guid.NewGuid():N}", "وردية", userId, 0)))
            .Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;

    private async Task<CheckoutOrderResult> SellAsync(string userId, string shiftId, string paymentMethod = "cash", bool payOnCollection = false)
    {
        var response = await As(userId).PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: userId, ShiftId: shiftId,
            OrderType: "pickup", PaymentMethod: paymentMethod,
            Items: new[] { new CheckoutItemRequest(_productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null, AmountPaidPiastres: 5_000,
            SplitPayments: null, WalletProvider: null, TableNumber: null, CustomerName: "سارة", CustomerPhone: "01011112222",
            DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null, PayOnCollection: payOnCollection));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    private async Task CloseShiftAsync(string userId, string shiftId, long counted) =>
        (await As(userId).PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"close-{Guid.NewGuid():N}", shiftId, userId, counted, null, null, null))).EnsureSuccessStatusCode();

    [Fact]
    public async Task A_cashier_cannot_open_an_order_from_an_earlier_shift_by_id_or_number()
    {
        var morning = await OpenShiftAsync(_firstCashierId);
        var old = await SellAsync(_firstCashierId, morning);
        await CloseShiftAsync(_firstCashierId, morning, 5_000);
        await OpenShiftAsync(_secondCashierId);

        var byId = await As(_secondCashierId).GetAsync($"/api/orders/{old.OrderId}");
        var byNumber = await As(_secondCashierId).GetAsync($"/api/orders/lookup?orderNumber={Uri.EscapeDataString(old.OrderNumber)}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, byId.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, byNumber.StatusCode);
        Assert.DoesNotContain("01011112222", await byId.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_holder_of_orders_view_all_still_opens_any_order()
    {
        var morning = await OpenShiftAsync(_firstCashierId);
        var old = await SellAsync(_firstCashierId, morning);
        await CloseShiftAsync(_firstCashierId, morning, 5_000);

        Assert.Equal(HttpStatusCode.OK, (await As(_managerId).GetAsync($"/api/orders/{old.OrderId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await As(_managerId).GetAsync($"/api/orders/lookup?orderNumber={Uri.EscapeDataString(old.OrderNumber)}")).StatusCode);
    }

    [Fact]
    public async Task An_order_of_the_open_shift_is_visible_to_any_till()
    {
        var shift = await OpenShiftAsync(_firstCashierId);
        var order = await SellAsync(_firstCashierId, shift);

        Assert.Equal(HttpStatusCode.OK, (await As(_secondCashierId).GetAsync($"/api/orders/{order.OrderId}")).StatusCode);
    }

    [Fact]
    public async Task Unfinished_work_from_an_earlier_shift_stays_reachable()
    {
        var morning = await OpenShiftAsync(_firstCashierId);
        var awaitingPickup = await SellAsync(_firstCashierId, morning, payOnCollection: true);
        var pendingTransfer = await SellAsync(_firstCashierId, morning, paymentMethod: "instapay");
        await CloseShiftAsync(_firstCashierId, morning, 0);
        await OpenShiftAsync(_secondCashierId);

        Assert.Equal(HttpStatusCode.OK, (await As(_secondCashierId).GetAsync($"/api/orders/{awaitingPickup.OrderId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await As(_secondCashierId).GetAsync($"/api/orders/{pendingTransfer.OrderId}")).StatusCode);
    }
}

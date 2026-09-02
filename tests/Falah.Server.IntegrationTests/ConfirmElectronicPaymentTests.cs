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
        _client = _factory.CreateClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos");
        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<CheckoutOrderResult> InstaPayOrderAsync(long totalPiastres = 15000)
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "instapay",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: totalPiastres, SplitPayments: null, WalletProvider: null,
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
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
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        var checkoutResponse = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        var cashOrder = (await checkoutResponse.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;

        var response = await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment", new ConfirmElectronicPaymentRequest(cashOrder.OrderId, _cashierId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == cashOrder.OrderId));
        Assert.Equal(1, movementCount); // only the original (never-deferred) cash movement from checkout
    }
}

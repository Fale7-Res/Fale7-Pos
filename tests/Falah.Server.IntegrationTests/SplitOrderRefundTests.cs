namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Xunit;

/// <summary>FIN-0005, owner decision 2026-11-22: an order paid partly cash and partly card is refunded
/// through ONE of those methods, chosen at the refund. The money leaves that method's account (card: the
/// bank) and no other; a method the order was not paid with is refused, and so is a refund naming none.
/// Before this, every refund of a split order was refused outright.</summary>
public class SplitOrderRefundTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _orderId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _managerId = await _factory.SeedUserAsync("مدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var shiftId = _shiftId = (await (await As(_cashierId).PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "وردية", _cashierId, 50_000))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
        var productId = await _factory.SeedProductAsync(20_000);
        var sale = await As(_cashierId).PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: "multi",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 20_000,
            SplitPayments: new[] { new CheckoutSplitRequest("cash", 8_000), new CheckoutSplitRequest("card", 12_000) },
            WalletProvider: null, TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null));
        sale.EnsureSuccessStatusCode();
        _orderId = (await sale.Content.ReadFromJsonAsync<CheckoutOrderResult>())!.OrderId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private HttpClient As(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId));
        return client;
    }

    private async Task<long> BalanceAsync(string account)
    {
        using var body = JsonDocument.Parse(await As(_managerId).GetStringAsync($"/api/treasury/balance?accountId={account}"));
        return body.RootElement.GetProperty("balancePiastres").GetInt64();
    }

    private Task<HttpResponseMessage> RefundAsync(long amount, string? method) =>
        As(_managerId).PostAsJsonAsync("/api/orders/refund", new RefundOrderRequest(
            $"rreq-{Guid.NewGuid():N}", _orderId, amount, "العميل رجّع جزء", _managerId, null, null, RefundMethod: method));

    [Fact]
    public async Task A_split_order_is_refunded_through_the_one_method_chosen()
    {
        var bankBefore = await BalanceAsync("tre-bank");
        var drawerBefore = await BalanceAsync("tre-drawer");

        var response = await RefundAsync(5_000, "card");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(bankBefore - 5_000, await BalanceAsync("tre-bank"));
        Assert.Equal(drawerBefore, await BalanceAsync("tre-drawer"));
    }

    [Theory]
    [InlineData("instapay")]
    [InlineData(null)]
    public async Task A_split_order_is_not_refunded_through_a_method_it_was_not_paid_with_or_none(string? method)
    {
        var response = await RefundAsync(5_000, method);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    /// <summary>Owner, 2026-11-23 (independent audit F-01): the whole split order may go back through any one of
    /// the methods it was paid with - and the drawer must then expect exactly what physically left it, so the
    /// blind close shows no false shortage. 500 ج opening + 80 ج cash leg - 200 ج cash refund = 380 ج.</summary>
    [Fact]
    public async Task A_whole_split_order_refunded_in_cash_leaves_no_false_shortage_at_the_close()
    {
        var refund = await RefundAsync(20_000, "cash");
        Assert.Equal(HttpStatusCode.OK, refund.StatusCode);

        var prepare = await As(_cashierId).PostAsJsonAsync("/api/shifts/prepare-close", new PrepareShiftCloseRequest(_shiftId, 38_000));
        prepare.EnsureSuccessStatusCode();
        var preview = (await prepare.Content.ReadFromJsonAsync<PrepareShiftCloseResult>())!;

        Assert.Equal(38_000, preview.ExpectedCashPiastres);
        Assert.Equal(0, preview.DifferencePiastres);
    }
}

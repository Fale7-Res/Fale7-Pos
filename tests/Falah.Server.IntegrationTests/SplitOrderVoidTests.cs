namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// FIN3b. A split sale - part cash (or card), part InstaPay / wallet - takes its cash at checkout,
/// into the drawer, while the electronic part stays a claim. When that claim never arrives the
/// cashier cancels it, and the whole order is voided: no sale. But the cash part stayed posted in the
/// drawer account, nothing recorded handing it back, and a split order cannot be refunded - so the
/// drawer account kept money the restaurant no longer had a sale for, and a business day whose shifts
/// had all closed no longer netted its drawer to zero.
///
/// The void now hands every collected leg back: a "void_return" from the account it went into,
/// posted against the shift that is open when it happens (the drawer the cash actually leaves), the
/// same way a refund of an earlier shift's sale is. The shift that took the cash still counts it.
/// </summary>
public class SplitOrderVoidTests : IAsyncLifetime
{
    private const long Float = 50_000;
    private FalahApiFactory _factory = default!;
    private string _cashierId = default!;
    private string _managerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _managerId = await _factory.SeedUserAsync("مدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private HttpClient As(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId));
        return client;
    }

    private async Task<string> OpenShiftAsync() =>
        (await (await As(_cashierId).PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "وردية", _cashierId, Float))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;

    private async Task CloseShiftAsync(string shiftId, long counted) =>
        (await As(_cashierId).PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"close-{Guid.NewGuid():N}", shiftId, _cashierId, counted, null, null, null))).EnsureSuccessStatusCode();

    private async Task<string> SplitSaleAsync(string shiftId, string collectedMethod, long collected, long electronic)
    {
        var productId = await _factory.SeedProductAsync(collected + electronic);
        var response = await As(_cashierId).PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: "multi",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: collected + electronic,
            SplitPayments: new[] { new CheckoutSplitRequest(collectedMethod, collected), new CheckoutSplitRequest("instapay", electronic) },
            WalletProvider: null, TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!.OrderId;
    }

    private Task<HttpResponseMessage> CancelAsync(string orderId) =>
        As(_cashierId).PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment",
            new CancelPendingElectronicPaymentRequest(orderId, "التحويل لم يصل", _cashierId, "not_prepared"));

    private async Task<long> BalanceAsync(string account)
    {
        using var body = JsonDocument.Parse(await As(_managerId).GetStringAsync($"/api/treasury/balance?accountId={account}"));
        return body.RootElement.GetProperty("balancePiastres").GetInt64();
    }

    private async Task<long> ExpectedCashAsync(string shiftId) =>
        (await (await As(_cashierId).PostAsJsonAsync("/api/shifts/prepare-close", new PrepareShiftCloseRequest(shiftId, 0)))
            .Content.ReadFromJsonAsync<PrepareShiftCloseResult>())!.ExpectedCashPiastres;

    private static long Optional(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt64() : 0;

    [Fact]
    public async Task Cancelling_the_unconfirmed_part_hands_the_cash_part_back_out_of_the_drawer()
    {
        var shiftId = await OpenShiftAsync();
        var orderId = await SplitSaleAsync(shiftId, "cash", 5_000, 10_000);
        Assert.Equal(Float + 5_000, await BalanceAsync("tre-drawer"));

        var cancel = await CancelAsync(orderId);
        cancel.EnsureSuccessStatusCode();
        using (var body = JsonDocument.Parse(await cancel.Content.ReadAsStringAsync()))
            Assert.Equal(5_000, Optional(body.RootElement, "cashToHandBackPiastres"));

        Assert.Equal(Float, await BalanceAsync("tre-drawer"));
        var returned = Assert.Single(await _factory.QueryAsync(db => db.TreasuryMovements.AsNoTracking()
            .Where(m => m.Type == "void_return" && m.ReferenceId == orderId).ToListAsync()));
        Assert.Equal(("tre-drawer", 5_000L, shiftId), (returned.SourceAccountId, returned.AmountPiastres, returned.ShiftId));
        // Cash in, cash back out, same drawer: what it should hold is the float.
        Assert.Equal(Float, await ExpectedCashAsync(shiftId));
    }

    [Fact]
    public async Task Once_every_shift_is_closed_the_days_drawer_holds_nothing()
    {
        var shiftId = await OpenShiftAsync();
        var orderId = await SplitSaleAsync(shiftId, "cash", 5_000, 10_000);
        (await CancelAsync(orderId)).EnsureSuccessStatusCode();

        await CloseShiftAsync(shiftId, counted: Float);

        Assert.Equal(0, await BalanceAsync("tre-drawer"));
    }

    [Fact]
    public async Task Cancelled_in_a_later_shift_the_cash_leaves_the_drawer_that_hands_it_back()
    {
        var first = await OpenShiftAsync();
        var orderId = await SplitSaleAsync(first, "cash", 5_000, 10_000);
        // The first shift took the cash and is closed with it counted.
        Assert.Equal(Float + 5_000, await ExpectedCashAsync(first));
        await CloseShiftAsync(first, counted: Float + 5_000);

        var second = await OpenShiftAsync();
        (await CancelAsync(orderId)).EnsureSuccessStatusCode();

        Assert.Equal(Float - 5_000, await ExpectedCashAsync(second));
        Assert.Equal(Float - 5_000, await BalanceAsync("tre-drawer"));
        Assert.Equal(second, (await _factory.QueryAsync(db => db.TreasuryMovements.AsNoTracking()
            .FirstAsync(m => m.Type == "void_return" && m.ReferenceId == orderId))).ShiftId);
    }

    [Fact]
    public async Task With_no_shift_open_there_is_no_drawer_to_hand_the_cash_back_from()
    {
        var shiftId = await OpenShiftAsync();
        var orderId = await SplitSaleAsync(shiftId, "cash", 5_000, 10_000);
        await CloseShiftAsync(shiftId, counted: Float + 5_000);

        var refused = await CancelAsync(orderId);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.NotEqual("cancelled", await _factory.QueryAsync(db => db.Orders.AsNoTracking().Where(o => o.Id == orderId).Select(o => o.Status).FirstAsync()));
        Assert.False(await _factory.QueryAsync(db => db.TreasuryMovements.AnyAsync(m => m.Type == "void_return")));
    }

    [Fact]
    public async Task A_card_part_is_handed_back_from_the_bank_account_it_went_into()
    {
        var shiftId = await OpenShiftAsync();
        var bankBefore = await BalanceAsync("tre-bank");
        var orderId = await SplitSaleAsync(shiftId, "card", 7_000, 3_000);

        (await CancelAsync(orderId)).EnsureSuccessStatusCode();

        Assert.Equal(bankBefore, await BalanceAsync("tre-bank"));
        var returned = await _factory.QueryAsync(db => db.TreasuryMovements.AsNoTracking()
            .FirstAsync(m => m.Type == "void_return" && m.ReferenceId == orderId));
        Assert.Equal(("tre-bank", 7_000L), (returned.SourceAccountId, returned.AmountPiastres));
        // Nothing left the drawer.
        Assert.Equal(Float, await ExpectedCashAsync(shiftId));
    }

    [Fact]
    public async Task Cancelling_twice_hands_the_cash_back_once_and_the_void_is_not_a_refund()
    {
        var shiftId = await OpenShiftAsync();
        var orderId = await SplitSaleAsync(shiftId, "cash", 5_000, 10_000);

        (await CancelAsync(orderId)).EnsureSuccessStatusCode();
        (await CancelAsync(orderId)).EnsureSuccessStatusCode();

        Assert.Equal(1, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "void_return")));
        using var sales = JsonDocument.Parse(await As(_managerId).GetStringAsync($"/api/reports/sales?actorUserId={_managerId}"));
        Assert.Equal(0, sales.RootElement.GetProperty("refundsPiastres").GetInt64());
        Assert.Equal(0, sales.RootElement.GetProperty("grossSalesPiastres").GetInt64());
    }

    /// <summary>FIN3: the voids report says what kind of void it was from the order's own payment state,
    /// and how much of it was handed back.</summary>
    [Fact]
    public async Task The_voids_report_shows_a_voided_split_sale_and_what_was_handed_back()
    {
        var shiftId = await OpenShiftAsync();
        var orderId = await SplitSaleAsync(shiftId, "cash", 5_000, 10_000);
        (await CancelAsync(orderId)).EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await As(_managerId).GetStringAsync($"/api/reports/voids?actorUserId={_managerId}"));
        var row = body.RootElement.GetProperty("voids").EnumerateArray().Single(v => v.GetProperty("orderId").GetString() == orderId);
        Assert.Equal("electronic_unconfirmed", row.GetProperty("kind").GetString());
        Assert.Equal(15_000, row.GetProperty("totalPiastres").GetInt64());
        Assert.Equal(5_000, Optional(row, "returnedPiastres"));
    }

    [Fact]
    public async Task A_cancellation_with_no_electronic_payment_is_not_reported_as_one()
    {
        var shiftId = await OpenShiftAsync();
        var orderId = $"ord-{Guid.NewGuid():N}";
        using (var scope = _factory.Services.CreateScope())
        {
            // A cancelled cash takeaway: no path produces one today, which is exactly why the report
            // must read the order's own payment state rather than assume "not cash on delivery means
            // an InstaPay that never arrived".
            var db = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                .GetRequiredService<Falah.Infrastructure.Persistence.FalahDbContext>(scope.ServiceProvider);
            db.Orders.Add(new Falah.Infrastructure.Persistence.Entities.OrderEntity
            {
                Id = orderId, OrderNumber = "#9001", CheckoutRequestId = $"req-{Guid.NewGuid():N}", ShiftId = shiftId,
                CashierId = _cashierId, OrderType = "takeaway", PaymentMethod = "cash", Status = "cancelled",
                SubtotalPiastres = 4_000, TotalPiastres = 4_000, IsPaid = false,
                CreatedAtUtc = DateTimeOffset.UtcNow, CancelledAtUtc = DateTimeOffset.UtcNow, CancelReason = "اختبار", CancelledBy = "اختبار",
            });
            await db.SaveChangesAsync();
        }

        using var body = JsonDocument.Parse(await As(_managerId).GetStringAsync($"/api/reports/voids?actorUserId={_managerId}"));
        var row = body.RootElement.GetProperty("voids").EnumerateArray().Single(v => v.GetProperty("orderId").GetString() == orderId);
        Assert.NotEqual("electronic_unconfirmed", row.GetProperty("kind").GetString());
        Assert.NotEqual("undelivered_cash_on_delivery", row.GetProperty("kind").GetString());
    }

    [Fact]
    public async Task An_electronic_only_sale_has_nothing_to_hand_back()
    {
        var shiftId = await OpenShiftAsync();
        var productId = await _factory.SeedProductAsync(9_000);
        var order = await (await As(_cashierId).PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: "instapay",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 9_000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null)))
            .Content.ReadFromJsonAsync<CheckoutOrderResult>();

        var cancel = await CancelAsync(order!.OrderId);
        cancel.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await cancel.Content.ReadAsStringAsync());
        Assert.Equal(0, Optional(body.RootElement, "cashToHandBackPiastres"));
        Assert.False(await _factory.QueryAsync(db => db.TreasuryMovements.AnyAsync(m => m.Type == "void_return")));
    }
}

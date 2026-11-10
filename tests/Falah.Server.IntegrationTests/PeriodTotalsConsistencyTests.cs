namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>FIN-0043: a period's totals must equal the sum of its days - including sales placed
/// exactly on a Cairo midnight and refunds made on a later day than their sale. Real checkouts and a
/// real refund through the API; only their timestamps are then moved onto earlier days.</summary>
public class PeriodTotalsConsistencyTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الفترات", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _managerId = await _factory.SeedUserAsync("مدير الفترات", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> SellAsync(long piastres, string method, string type = "takeaway")
    {
        var productId = await _factory.SeedProductAsync(piastres);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            $"req-{Guid.NewGuid():N}", _cashierId, _shiftId, type, method,
            new[] { new CheckoutItemRequest(productId, null, 1, null, null) }, 0, 0, 0, null, null, null,
            piastres, null, null, null, type == "pickup" ? "عميل" : null, type == "pickup" ? "01012345678" : null,
            null, null, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!.OrderId;
    }

    private Task MoveOrderAsync(string orderId, DateTimeOffset at) => _factory.QueryAsync(db =>
        db.Orders.Where(o => o.Id == orderId).ExecuteUpdateAsync(s => s.SetProperty(o => o.CreatedAtUtc, at)));

    private Task MoveRefundsAsync(string orderId, DateTimeOffset at) => _factory.QueryAsync(db =>
        db.OrderRefunds.Where(r => r.OrderId == orderId).ExecuteUpdateAsync(s => s.SetProperty(r => r.CreatedAtUtc, at)));

    private async Task<SalesReportDto> SalesAsync(DateOnly from, DateOnly to) =>
        (await (await _client.GetAsync($"/api/reports/sales?actorUserId={_managerId}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"))
            .EnsureSuccessStatusCode().Content.ReadFromJsonAsync<SalesReportDto>())!;

    private static readonly (string Name, Func<SalesReportDto, long> Get)[] AdditiveFields =
    {
        ("OrderCount", r => r.OrderCount), ("Gross", r => r.GrossSalesPiastres), ("Refunds", r => r.RefundsPiastres),
        ("Net", r => r.NetSalesPiastres), ("List", r => r.ListSalesPiastres), ("Offers", r => r.OffersValuePiastres),
        ("Comps", r => r.CompsValuePiastres), ("ManualDiscounts", r => r.ManualDiscountsPiastres),
        ("PendingElectronic", r => r.PendingElectronicSalesPiastres), ("CashWithDrivers", r => r.CashWithDriversPiastres),
        ("Collected", r => r.CollectedSalesPiastres), ("Cash", r => r.CashPiastres), ("Card", r => r.CardPiastres),
        ("InstaPay", r => r.InstaPayPiastres), ("Wallet", r => r.WalletPiastres), ("Pickup", r => r.PickupPiastres),
        ("Takeaway", r => r.TakeawayPiastres), ("Delivery", r => r.DeliveryPiastres),
    };

    [Fact]
    public async Task A_periods_totals_equal_the_sum_of_its_days_across_midnight_boundaries_and_late_refunds()
    {
        var today = BusinessClock.Today();
        var d2 = today.AddDays(-2); var d1 = today.AddDays(-1);

        var a = await SellAsync(30000, "cash");                 // two days ago, refunded yesterday
        var b = await SellAsync(12000, "card");                 // exactly at yesterday's first instant
        var c = await SellAsync(8000, "cash", "pickup");        // yesterday's very last tick
        var d = await SellAsync(5000, "instapay");              // pending transfer, today
        var e = await SellAsync(4000, "cash");                  // today

        var refund = await _client.PostAsJsonAsync("/api/orders/refund",
            new RefundOrderRequest($"rf-{Guid.NewGuid():N}", a, 7000, "صنف ناقص", _managerId, null, null));
        refund.EnsureSuccessStatusCode();

        await MoveOrderAsync(a, BusinessClock.UtcRangeForBusinessDate(d2).Start.AddHours(10));
        await MoveRefundsAsync(a, BusinessClock.UtcRangeForBusinessDate(d1).Start.AddHours(9));
        await MoveOrderAsync(b, BusinessClock.UtcRangeForBusinessDate(d1).Start);
        await MoveOrderAsync(c, BusinessClock.UtcRangeForBusinessDate(d1).End.AddTicks(-1));

        var period = await SalesAsync(d2, today);
        var days = new[] { await SalesAsync(d2, d2), await SalesAsync(d1, d1), await SalesAsync(today, today) };

        foreach (var (name, get) in AdditiveFields)
            Assert.True(get(period) == days.Sum(get), $"{name}: period {get(period)} != sum of days {days.Sum(get)}");

        // And the figures are the transactions themselves, not merely consistent with each other.
        Assert.Equal(5, period.OrderCount);
        Assert.Equal(30000 + 12000 + 8000 + 5000 + 4000, period.GrossSalesPiastres);
        Assert.Equal(7000, period.RefundsPiastres);
        Assert.Equal(1, days[0].OrderCount);
        Assert.Equal(2, days[1].OrderCount);    // both boundary sales land on yesterday, neither leaks
        Assert.Equal(7000, days[1].RefundsPiastres); // a refund counts on the day it was made
        Assert.Equal(0, days[0].RefundsPiastres);
        Assert.Equal(2, days[2].OrderCount);
        _ = d; _ = e;
    }

    // RPT-0001: the sales report treats a reversed range as the same range (BusinessClock swaps it);
    // the product reports turned it into an empty window and silently answered "nothing sold".
    [Fact]
    public async Task A_reversed_date_range_reports_the_same_as_the_forward_one()
    {
        var today = BusinessClock.Today();
        await SellAsync(9000, "cash");
        string Q(DateOnly f, DateOnly t) => $"/api/reports/top-items?actorUserId={_managerId}&from={f:yyyy-MM-dd}&to={t:yyyy-MM-dd}";

        var forward = await (await _client.GetAsync(Q(today.AddDays(-2), today))).EnsureSuccessStatusCode().Content.ReadAsStringAsync();
        var reversed = await (await _client.GetAsync(Q(today, today.AddDays(-2)))).EnsureSuccessStatusCode().Content.ReadAsStringAsync();

        Assert.NotEqual("[]", forward);
        Assert.Equal(forward, reversed);
    }}

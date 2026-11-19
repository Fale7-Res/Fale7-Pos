namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A refund is an event of the day, and the shift, it happens in. The sale it reverses keeps its own
/// day.
///
/// Every sales figure used to subtract an order's CUMULATIVE refund from the day the order was SOLD:
/// a refund today of last week's sale lowered last week's net sales - a day already closed, counted
/// and handed over - while today's net sales did not move at all, even though today's refund list
/// showed it and today's drawer paid it. The owner dashboard then put that order-dated amount next to
/// a refund-dated count. These tests sell "yesterday" (by dating the order and its business day back
/// in this isolated test database) and refund today.
/// </summary>
public class RefundDateBasisTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _todayDayId = default!;
    private string _shiftId = default!;

    private static readonly DateOnly Today = BusinessClock.Today();
    private static readonly DateOnly Yesterday = Today.AddDays(-1);

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?>
        {
            ["Backup:Automatic:Enabled"] = "false",
        });
        var database = await _factory.QueryAsync(db => Task.FromResult(db.Database.GetDbConnection().Database));
        Assert.StartsWith("FalahTest_", database);

        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "orders_refund", "shifts_open_own", "shifts_blind_close");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _todayDayId = await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        _shiftId = await OpenShiftAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task A_refund_today_of_yesterdays_sale_counts_today_and_leaves_yesterday_as_it_was_sold()
    {
        var order = await SellYesterdayAsync(10000);
        await RefundAsync(order, 4000);

        var yesterday = await SalesAsync(Yesterday);
        Assert.Equal(10000, yesterday.GrossSalesPiastres);
        Assert.Equal(0, yesterday.RefundsPiastres);
        Assert.Equal(10000, yesterday.NetSalesPiastres);
        Assert.Equal(10000, yesterday.CashPiastres);

        var today = await SalesAsync(Today);
        Assert.Equal(0, today.GrossSalesPiastres);
        Assert.Equal(4000, today.RefundsPiastres);
        Assert.Equal(-4000, today.NetSalesPiastres);
        Assert.Equal(-4000, today.CashPiastres);
        Assert.Equal(-4000, today.CollectedSalesPiastres);
    }

    [Fact]
    public async Task The_refund_list_and_the_sales_figure_agree_on_which_day_a_refund_belongs_to()
    {
        var order = await SellYesterdayAsync(10000);
        await RefundAsync(order, 4000);

        var refundsToday = await _client.GetFromJsonAsync<List<RefundActivityDto>>(
            $"/api/reports/refunds?actorUserId={_managerId}&businessDate={Today:yyyy-MM-dd}");
        Assert.Equal(refundsToday!.Sum(r => r.AmountPiastres), (await SalesAsync(Today)).RefundsPiastres);
    }

    [Fact]
    public async Task A_refund_row_says_when_the_sale_it_reverses_was_made()
    {
        // A refund today of yesterday's sale is today's money out, but the owner reading it needs to
        // see at a glance that the sale itself was not today's.
        var order = await SellYesterdayAsync(10000);
        await RefundAsync(order, 4000);

        var refunds = await _client.GetFromJsonAsync<List<RefundActivityDto>>(
            $"/api/reports/refunds?actorUserId={_managerId}&businessDate={Today:yyyy-MM-dd}");

        var row = Assert.Single(refunds!);
        Assert.Equal(Yesterday, BusinessClock.BusinessDate(row.OriginalSaleAtUtc!.Value));
    }

    [Fact]
    public async Task The_dashboards_refund_amount_and_refund_count_describe_the_same_refunds()
    {
        var order = await SellYesterdayAsync(10000);
        await RefundAsync(order, 4000);

        var dashboard = await _client.GetFromJsonAsync<OwnerDashboardDto>(
            $"/api/reports/owner-dashboard?actorUserId={_managerId}&businessDate={Today:yyyy-MM-dd}");

        Assert.Equal(1, dashboard!.Leakage.RefundCount);
        Assert.Equal(4000, dashboard.Leakage.RefundsPiastres);
        // The hourly curve is drawn from the same events, so its points add up to the headline.
        Assert.Equal(dashboard.Cash.NetSalesPiastres, dashboard.Trend.Series.Sum(p => p.NetSalesPiastres));
    }

    [Fact]
    public async Task A_refund_belongs_to_the_business_day_it_happens_in_not_to_the_closed_day_of_the_sale()
    {
        var order = await SellYesterdayAsync(10000);
        await RefundAsync(order, 4000);

        var movementDay = await _factory.QueryAsync(db => db.TreasuryMovements
            .Where(m => m.Type == "refund" && m.ReferenceId == order).Select(m => m.BusinessDayId).FirstAsync());
        Assert.Equal(_todayDayId, movementDay);

        var closedDay = await ReconciliationAsync(await YesterdayDayIdAsync());
        Assert.Equal(0, closedDay.Sales.RefundsPiastres);
        Assert.Equal(10000, closedDay.Sales.NetSalesPiastres);

        var openDay = await ReconciliationAsync(_todayDayId);
        Assert.Equal(4000, openDay.Sales.RefundsPiastres);
    }

    [Fact]
    public async Task A_shift_summary_counts_the_refunds_processed_in_that_shift()
    {
        var order = await CheckoutCashAsync(10000);
        await CloseAsync(_shiftId, 50000 + 10000);
        var laterShift = await OpenShiftAsync();
        await RefundAsync(order.OrderId, 4000);

        var original = await _client.GetFromJsonAsync<ShiftSummaryDto>($"/api/reports/shifts/{_shiftId}?actorUserId={_managerId}");
        var processing = await _client.GetFromJsonAsync<ShiftSummaryDto>($"/api/reports/shifts/{laterShift}?actorUserId={_managerId}");

        Assert.Equal(0, original!.Sales.RefundsPiastres);
        Assert.Equal(10000, original.Sales.NetSalesPiastres);
        Assert.Equal(4000, processing!.Sales.RefundsPiastres);
    }

    // ---------- Helpers ----------

    /// <summary>A cash sale rung up now, then dated back to yesterday together with a business day
    /// that already closed - the shape a real sale from yesterday has by the time it is refunded.</summary>
    private async Task<string> SellYesterdayAsync(long totalPiastres)
    {
        var order = await CheckoutCashAsync(totalPiastres);
        var yesterdayDayId = await YesterdayDayIdAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var entity = await db.Orders.FirstAsync(o => o.Id == order.OrderId);
        var (start, _) = BusinessClock.UtcRangeForBusinessDate(Yesterday);
        entity.CreatedAtUtc = start.AddHours(12);
        entity.BusinessDayId = yesterdayDayId;
        foreach (var item in await db.TreasuryMovements.Where(m => m.ReferenceId == order.OrderId).ToListAsync())
        {
            item.CreatedAtUtc = entity.CreatedAtUtc;
            item.BusinessDayId = yesterdayDayId;
        }
        await db.SaveChangesAsync();
        return order.OrderId;
    }

    private async Task<string> YesterdayDayIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var existing = await db.BusinessDays.FirstOrDefaultAsync(d => d.BusinessDate == Yesterday);
        if (existing is not null) return existing.Id;
        var id = $"day-{Guid.NewGuid():N}";
        db.BusinessDays.Add(new BusinessDayEntity
        {
            Id = id, BusinessDate = Yesterday, OpenedBy = "seed", OpenedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            Status = "Closed", CloseRevision = 1, OpenRequestId = $"seed-{Guid.NewGuid():N}",
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<SalesReportDto> SalesAsync(DateOnly date) =>
        (await _client.GetFromJsonAsync<SalesReportDto>($"/api/reports/sales?actorUserId={_managerId}&businessDate={date:yyyy-MM-dd}"))!;

    private async Task<BusinessDayReconciliationDto> ReconciliationAsync(string dayId) =>
        (await _client.GetFromJsonAsync<BusinessDayReconciliationDto>($"/api/reports/business-day/{dayId}?actorUserId={_managerId}"))!;

    private async Task<string> OpenShiftAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "وردية", _cashierId, 50000));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    private async Task<CheckoutOrderResult> CheckoutCashAsync(long totalPiastres)
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: totalPiastres, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    private async Task CloseAsync(string shiftId, long counted)
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"creq-{Guid.NewGuid():N}", shiftId, _cashierId, counted, null, null, null));
        response.EnsureSuccessStatusCode();
    }

    private async Task RefundAsync(string orderId, long amountPiastres)
    {
        var response = await _client.PostAsJsonAsync("/api/orders/refund",
            new RefundOrderRequest($"rreq-{Guid.NewGuid():N}", orderId, amountPiastres, "طلب العميل", _cashierId, null, null));
        response.EnsureSuccessStatusCode();
    }
}

namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Xunit;

/// <summary>
/// The owner dashboard composes figures the other reports already produce, so these tests are not
/// about arithmetic - they are about the two things a composed screen can get wrong.
///
/// It must agree with the reports it is built from. A dashboard that quietly disagrees with the
/// sales screen is worse than no dashboard, because it gets believed.
///
/// And it must refuse to state what it does not know. A restaurant with unpriced items has no
/// knowable food cost, and reporting zero there would announce a hundred percent margin.
/// </summary>
public class OwnerDashboardVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _ownerId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _ownerId = await _factory.SeedUserAsync("المالك", "owner", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();

        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000)))
            .Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task SellAsync(long totalPiastres)
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: totalPiastres, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null,
            BusinessDayId: null));
        response.EnsureSuccessStatusCode();
    }

    private Task<OwnerDashboardDto?> DashboardAsync(string? actorUserId = null) =>
        _client.GetFromJsonAsync<OwnerDashboardDto>(
            $"/api/reports/owner-dashboard?actorUserId={actorUserId ?? _ownerId}");

    private static string Today => Falah.Domain.Common.BusinessClock.Today().ToString("yyyy-MM-dd");

    [Fact]
    public async Task Reading_the_dashboard_needs_the_same_permission_every_other_report_needs()
    {
        var noAccess = await _factory.SeedUserAsync("موظف", "cashier", "pos");

        var response = await _client.GetAsync($"/api/reports/owner-dashboard?actorUserId={noAccess}");

        // The same refusal every other report gives - this endpoint gets no gate of its own.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Every_headline_figure_matches_the_report_it_came_from()
    {
        await SellAsync(30_000);
        await SellAsync(12_500);

        var dashboard = await DashboardAsync();
        var sales = await _client.GetFromJsonAsync<SalesReportDto>(
            $"/api/reports/sales?actorUserId={_ownerId}&businessDate={Today}");

        // The dashboard must never be a second opinion about the same rows.
        Assert.Equal(sales!.NetSalesPiastres, dashboard!.Cash.NetSalesPiastres);
        Assert.Equal(sales.CollectedSalesPiastres, dashboard.Cash.CollectedSalesPiastres);
        Assert.Equal(sales.CashWithDriversPiastres, dashboard.Cash.CashWithDriversPiastres);
        Assert.Equal(sales.OrderCount, dashboard.Cash.OrderCount);
        Assert.Equal(42_500, dashboard.Cash.NetSalesPiastres);
    }

    [Fact]
    public async Task Sales_with_no_priced_recipe_report_no_food_cost_and_no_margin()
    {
        // The seeded product carries no recipe, so nothing about its cost is knowable. Zero here
        // would read as a hundred percent gross margin - the most flattering possible lie.
        await SellAsync(30_000);

        var dashboard = await DashboardAsync();

        Assert.Null(dashboard!.Profitability.KnownFoodCostPiastres);
        Assert.Null(dashboard.Profitability.GrossProfitPiastres);
        Assert.Null(dashboard.Profitability.FoodCostPercent);
        Assert.Null(dashboard.Profitability.OperatingResultPiastres);
        Assert.False(dashboard.Profitability.IsFoodCostComplete);
        Assert.Equal(0, dashboard.Profitability.KnownCostLineCount);
        // Net sales are perfectly knowable and stay reported.
        Assert.Equal(30_000, dashboard.Profitability.NetSalesPiastres);
    }

    [Fact]
    public async Task The_comparable_period_is_the_same_weekday_one_week_earlier()
    {
        var dashboard = await DashboardAsync();

        var today = Falah.Domain.Common.BusinessClock.Today();
        Assert.Equal(today.AddDays(-7).ToString("yyyy-MM-dd"), dashboard!.Trend.ComparedFromBusinessDate);
        Assert.Equal(today.AddDays(-7).ToString("yyyy-MM-dd"), dashboard.Trend.ComparedToBusinessDate);
    }

    [Fact]
    public async Task Growth_from_a_period_with_no_trade_reports_no_percentage()
    {
        // Nothing was sold a week ago in a freshly seeded database, so today's sales are an increase
        // with no ratio. A percentage here would be the largest number on the dashboard.
        await SellAsync(50_000);

        var dashboard = await DashboardAsync();

        Assert.Equal(50_000, dashboard!.Trend.NetSales.Current);
        Assert.Equal(0, dashboard.Trend.NetSales.Previous);
        Assert.Equal(50_000, dashboard.Trend.NetSales.Delta);
        Assert.Null(dashboard.Trend.NetSales.Percent);
    }

    [Fact]
    public async Task A_single_business_date_is_charted_by_the_hour()
    {
        await SellAsync(20_000);

        var dashboard = await DashboardAsync();

        Assert.Equal("hour", dashboard!.Trend.SeriesGranularity);
        Assert.Equal(24, dashboard.Trend.Series.Count);
        // The buckets add up to the headline, so the curve and the number can never disagree.
        Assert.Equal(dashboard.Cash.NetSalesPiastres, dashboard.Trend.Series.Sum(p => p.NetSalesPiastres));
    }

    [Fact]
    public async Task A_span_of_days_is_charted_by_day_and_closed_days_are_zero_not_missing()
    {
        await SellAsync(20_000);
        var today = Falah.Domain.Common.BusinessClock.Today();
        var from = today.AddDays(-6);

        var dashboard = await _client.GetFromJsonAsync<OwnerDashboardDto>(
            $"/api/reports/owner-dashboard?actorUserId={_ownerId}&from={from:yyyy-MM-dd}&to={today:yyyy-MM-dd}");

        Assert.Equal("day", dashboard!.Trend.SeriesGranularity);
        // A gap in a curve reads as missing data; a zero reads as a closed day, which is the truth.
        Assert.Equal(7, dashboard.Trend.Series.Count);
        Assert.Equal(dashboard.Cash.NetSalesPiastres, dashboard.Trend.Series.Sum(p => p.NetSalesPiastres));
    }

    [Fact]
    public async Task Treasury_balances_are_current_not_scoped_to_the_reported_period()
    {
        // Asking "how much cash do I have" and getting last Tuesday's answer would be worse than
        // useless, so the balances ignore the period entirely.
        var today = Falah.Domain.Common.BusinessClock.Today();
        var longAgo = today.AddDays(-90);

        var dashboard = await _client.GetFromJsonAsync<OwnerDashboardDto>(
            $"/api/reports/owner-dashboard?actorUserId={_ownerId}&from={longAgo:yyyy-MM-dd}&to={longAgo:yyyy-MM-dd}");
        var current = await DashboardAsync();

        Assert.Equal(current!.Cash.TreasuryNow.MainPiastres, dashboard!.Cash.TreasuryNow.MainPiastres);
        Assert.Equal(current.Cash.TreasuryNow.DrawerPiastres, dashboard.Cash.TreasuryNow.DrawerPiastres);
    }

    [Fact]
    public async Task An_open_shift_and_its_blockers_are_surfaced_as_work_to_do()
    {
        var dashboard = await DashboardAsync();

        Assert.True(dashboard!.Attention.HasOpenBusinessDay);
        Assert.Equal(1, dashboard.Attention.OpenShiftCount);
        // An open shift is itself a reason the day cannot close, and the evaluator says so.
        Assert.True(dashboard.Attention.CloseBlockerCount > 0);
    }

    [Fact]
    public async Task A_database_with_no_backup_reports_that_rather_than_a_blank()
    {
        var dashboard = await DashboardAsync();

        Assert.Null(dashboard!.Health.LastCompletedBackupAtUtc);
        Assert.Null(dashboard.Health.LastBackupAgeHours);
        // Every print route is listed even when it has never run, so a silent printer is visible.
        Assert.Equal(4, dashboard.Health.PrintRoutes.Count);
        Assert.All(dashboard.Health.PrintRoutes, r => Assert.Null(r.LastSuccessAtUtc));
    }
}

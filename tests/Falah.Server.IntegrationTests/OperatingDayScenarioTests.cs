namespace Falah.Server.IntegrationTests;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Falah.Application.BusinessDay;
using Falah.Application.Expenses;
using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Application.Shifts;
using Falah.Application.Treasury;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// OPS1, scenario A: one whole cash trading day driven only through the API, the way the tills drive
/// it - open the day, open a shift with a float, sell (cash and card), queue the receipts, pay an
/// expense from the drawer, drop cash in the safe, count, close the shift, close the day - and then
/// every report that describes that day must tell the same story to the piastre.
///
/// The numbers: float 500 ج; cash sales 100 + 250 + 75 = 425 ج; one card sale 120 ج; a drawer
/// expense 30 ج; a safe drop 200 ج. The drawer should then hold 500 + 425 - 30 - 200 = 695 ج, and
/// that is what is counted.
/// </summary>
public class OperatingDayScenarioTests : IAsyncLifetime
{
    private const long Float = 50_000, Expense = 3_000, Drop = 20_000;
    private static readonly long[] CashSales = { 10_000, 25_000, 7_500 };
    private const long CardSale = 12_000;
    private static readonly long ExpectedInDrawer = Float + CashSales.Sum() - Expense - Drop; // 69 500

    private FalahApiFactory _factory = default!;
    private string _cashierId = default!;
    private string _managerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "expenses_add_drawer", "safe_drop");
        _managerId = await _factory.SeedUserAsync("مدير", "manager", "all");
        await _factory.FundMainAccountAsync(1_000_000);
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private HttpClient As(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId));
        return client;
    }

    private static async Task<T> Ok<T>(Task<HttpResponseMessage> call)
    {
        var response = await call;
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<JsonElement> ReadAsync(string url)
    {
        var text = await As(_managerId).GetStringAsync(url);
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    private async Task<string> SellAsync(string shiftId, long price, string method)
    {
        var productId = await _factory.SeedProductAsync(price);
        var sale = await Ok<CheckoutOrderResult>(As(_cashierId).PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: method,
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: price, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null)));
        Assert.Equal(price, sale.TotalPiastres);
        return sale.OrderId;
    }

    [Fact]
    public async Task A_whole_cash_day_reconciles_across_every_report()
    {
        var till = As(_cashierId);
        var manager = As(_managerId);

        // 1. The manager opens the day; 2. the cashier opens a shift with a float from the main treasury.
        var day = await Ok<OpenBusinessDayResult>(manager.PostAsJsonAsync("/api/business-day/open", new OpenBusinessDayRequest($"bd-{Guid.NewGuid():N}", _managerId)));
        var shift = await Ok<OpenShiftResult>(till.PostAsJsonAsync("/api/shifts/open", new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, Float)));

        // 3-4. Sales, cash and card.
        var orders = new List<string>();
        foreach (var price in CashSales) orders.Add(await SellAsync(shift.ShiftId, price, "cash"));
        orders.Add(await SellAsync(shift.ShiftId, CardSale, "card"));

        // 5. Receipts are queued (that the printer printed them is an on-site check).
        foreach (var orderId in orders)
            await Ok<CreatePrintJobsResult>(till.PostAsJsonAsync($"/api/orders/{orderId}/print-jobs",
                new CreatePrintJobsRequest($"pj-{Guid.NewGuid():N}", orderId, "Receipt", false, _cashierId, IncludeKitchenRoutes: false)));
        Assert.True(await _factory.QueryAsync(db => db.PrintJobs.CountAsync(j => orders.Contains(j.OrderId) && j.Route == "Receipt")) >= orders.Count);

        // 6. An operating expense from the drawer; 7. cash into the safe.
        await Ok<AddExpenseResult>(till.PostAsJsonAsync("/api/expenses", new AddExpenseRequest(
            $"exp-{Guid.NewGuid():N}", shift.ShiftId, "نظافة ومطهرات", Expense, "منظفات", "tre-drawer", _cashierId, null, null)));
        await Ok<SafeDropResult>(till.PostAsJsonAsync("/api/treasury/safe-drop", new SafeDropRequest(
            $"drop-{Guid.NewGuid():N}", shift.ShiftId, Drop, "إيداع منتصف الوردية", _cashierId, null, null)));

        // 8. Count; the preview agrees with the arithmetic above.
        var preview = await Ok<PrepareShiftCloseResult>(till.PostAsJsonAsync("/api/shifts/prepare-close", new PrepareShiftCloseRequest(shift.ShiftId, ExpectedInDrawer)));
        Assert.Equal((ExpectedInDrawer, 0L, false), (preview.ExpectedCashPiastres, preview.DifferencePiastres, preview.RequiresManagerApproval));

        // 9. Close the shift; 10. reconcile expected, declared and handed over.
        await Ok<CloseShiftResult>(till.PostAsJsonAsync("/api/shifts/close", new CloseShiftRequest(
            $"close-{Guid.NewGuid():N}", shift.ShiftId, _cashierId, ExpectedInDrawer, null, null, null)));
        var handover = await _factory.QueryAsync(db => db.TreasuryMovements.AsNoTracking()
            .SingleAsync(m => m.Type == "shift_handover" && m.ShiftId == shift.ShiftId));
        Assert.Equal(("tre-drawer", "tre-main", ExpectedInDrawer), (handover.SourceAccountId, handover.DestinationAccountId, handover.AmountPiastres));

        // 11. Close the day.
        foreach (var itemId in new[] { "close-shifts", "close-drawer", "close-delivery" })
            (await manager.PostAsJsonAsync($"/api/business-day/{day.BusinessDayId}/checklist/{itemId}",
                new CompleteChecklistItemRequest(day.BusinessDayId, itemId, true, _managerId))).EnsureSuccessStatusCode();
        (await manager.PostAsJsonAsync($"/api/business-day/{day.BusinessDayId}/close",
            new CloseBusinessDayRequest($"close-{Guid.NewGuid():N}", day.BusinessDayId, _managerId, null, null))).EnsureSuccessStatusCode();

        // 12. Every report tells the same story.
        var grossSales = CashSales.Sum() + CardSale;

        var reconciliation = await ReadAsync($"/api/reports/business-day/{day.BusinessDayId}?actorUserId={_managerId}");
        var sales = reconciliation.GetProperty("sales");
        Assert.Equal((grossSales, grossSales, CashSales.Sum(), CardSale, 4),
            (sales.GetProperty("grossSalesPiastres").GetInt64(), sales.GetProperty("netSalesPiastres").GetInt64(),
             sales.GetProperty("cashPiastres").GetInt64(), sales.GetProperty("cardPiastres").GetInt64(), sales.GetProperty("orderCount").GetInt32()));
        Assert.Equal(Expense, reconciliation.GetProperty("expensesTotalPiastres").GetInt64());
        var treasury = reconciliation.GetProperty("treasury");
        // The day's own movements: the drawer ends where it started; main gained the cash taken less
        // the cash spent; the bank holds the card sale.
        Assert.Equal(0, treasury.GetProperty("drawerPiastres").GetInt64());
        Assert.Equal(CashSales.Sum() - Expense, treasury.GetProperty("mainPiastres").GetInt64());
        Assert.Equal(CardSale, treasury.GetProperty("bankPiastres").GetInt64());
        Assert.Equal((ExpectedInDrawer, ExpectedInDrawer, 0L, 0L),
            (reconciliation.GetProperty("shiftsExpectedCashPiastres").GetInt64(), reconciliation.GetProperty("shiftsActualCashPiastres").GetInt64(),
             reconciliation.GetProperty("shiftsShortagePiastres").GetInt64(), reconciliation.GetProperty("shiftsOveragePiastres").GetInt64()));

        var summary = await ReadAsync($"/api/reports/daily-summary/{day.BusinessDayId}?actorUserId={_managerId}");
        Assert.Equal(grossSales, summary.GetProperty("sales").GetProperty("netSalesPiastres").GetInt64());
        Assert.Equal(Expense, summary.GetProperty("expensesTotalPiastres").GetInt64());

        Assert.Equal(System.Net.HttpStatusCode.OK, (await manager.GetAsync($"/api/business-day/{day.BusinessDayId}/close-snapshot")).StatusCode);

        // Calendar view of today (the whole day happened today).
        var cashFlow = await ReadAsync($"/api/reports/cash-flow?actorUserId={_managerId}");
        var drawer = cashFlow.GetProperty("accounts").EnumerateArray().Single(a => a.GetProperty("accountId").GetString() == "tre-drawer");
        Assert.Equal((0L, Float + CashSales.Sum(), Expense + Drop + ExpectedInDrawer, 0L),
            (drawer.GetProperty("openingPiastres").GetInt64(), drawer.GetProperty("inflowPiastres").GetInt64(),
             drawer.GetProperty("outflowPiastres").GetInt64(), drawer.GetProperty("closingPiastres").GetInt64()));
        foreach (var account in cashFlow.GetProperty("accounts").EnumerateArray())
        {
            var id = account.GetProperty("accountId").GetString()!;
            using var balance = JsonDocument.Parse(await manager.GetStringAsync($"/api/treasury/balance?accountId={id}"));
            Assert.Equal(balance.RootElement.GetProperty("balancePiastres").GetInt64(), account.GetProperty("closingPiastres").GetInt64());
        }

        var dashboard = await ReadAsync($"/api/reports/owner-dashboard?actorUserId={_managerId}");
        Assert.Equal(grossSales, dashboard.GetProperty("cash").GetProperty("netSalesPiastres").GetInt64());
        Assert.Equal(grossSales, dashboard.GetProperty("cash").GetProperty("collectedSalesPiastres").GetInt64());
        Assert.Equal(Expense, dashboard.GetProperty("leakage").GetProperty("expensesPiastres").GetInt64());
    }
}

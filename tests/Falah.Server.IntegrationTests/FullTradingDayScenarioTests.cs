namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Falah.Application.Approvals;
using Falah.Application.BusinessDay;
using Falah.Application.Delivery;
using Falah.Application.Expenses;
using Falah.Application.Orders;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Application.Treasury;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Manual acceptance test 8 in docs/DAILY_OPERATIONS_VERIFICATION.md, automated (2026-11-18): a whole
/// trading day driven only through the API the tills use, then every report checked against the cash
/// counted, to the piastre.
///
/// Day 1 is a short day whose only job is to leave yesterday's sale behind (800 ج cash).
/// Day 2: morning shift A on a 1,000 ج float sells cash 100, card 120, a split 200 (cash 80 + card 120),
/// InstaPay 150 (confirmed later, during shift B) and InstaPay 90 (the transfer never comes: cancelled,
/// food not prepared, stock back); a 10 ج discount within the 10% limit on 100 (cash 90); a 50 ج discount on
/// 200, refused without a manager and accepted with one (cash 150); a cash-on-delivery order of 200 + 20 fee
/// delivered, and one of 100 + 15 that fails (voided, not prepared). Yesterday's 800 ج sale is refunded today:
/// 200 by the cashier, then 600, refused without a manager and accepted with one. A 30 ج expense and a
/// 200 ج safe drop. The driver hands over 200 (220 collected less his 20 fee).
/// Shift A's drawer: 1,000 + 100 + 80 + 90 + 150 + 200 - 800 - 30 - 200 = 590 ج. The cashier first counts
/// 580, recounts 590; the recount needs a manager.
/// Afternoon shift B on a 500 ج float sells cash 250 and card 50 and closes on 750 ج.
/// </summary>
public class FullTradingDayScenarioTests : IAsyncLifetime
{
    private const long Funding = 1_000_000;
    private const long ExpectedDrawerA = 100_000 + 10_000 + 8_000 + 9_000 + 15_000 + 20_000 - 80_000 - 3_000 - 20_000; // 59 000
    private const long ExpectedDrawerB = 50_000 + 25_000; // 75 000

    private FalahApiFactory _factory = default!;
    private string _cashierA = default!, _cashierB = default!, _dispatcher = default!, _manager = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        string[] cashier = { "pos", "shifts_open_own", "shifts_blind_close", "orders_refund", "orders_discount", "expenses_add_drawer", "safe_drop" };
        _cashierA = await _factory.SeedUserAsync("كاشير الصباح", "cashier", cashier);
        _cashierB = await _factory.SeedUserAsync("كاشير المساء", "cashier", cashier);
        _dispatcher = await _factory.SeedUserAsync("موظف التوصيل", "dispatcher", "delivery_manage");
        _manager = await _factory.SeedUserAsync("المدير", "manager", "all");
        _factory.StationUserId = _manager;
        await _factory.FundMainAccountAsync(Funding);
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    // ---------- the till's calls ----------

    private HttpClient As(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId));
        return client;
    }

    private static async Task<T> Ok<T>(Task<HttpResponseMessage> call)
    {
        var response = await call;
        Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage?.RequestUri?.AbsolutePath} -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task Ok(Task<HttpResponseMessage> call)
    {
        var response = await call;
        Assert.True(response.IsSuccessStatusCode, $"{response.RequestMessage?.RequestUri?.AbsolutePath} -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    private static async Task Refused(Task<HttpResponseMessage> call, string expectedMessagePart)
    {
        var response = await call;
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.UnprocessableEntity, $"{(int)response.StatusCode}: {body}");
        Assert.Contains(expectedMessagePart, body);
    }

    private Task<string> ApprovalAsync(string requestedBy, string action, string reason) =>
        Ok<IssueManagerApprovalResult>(As(requestedBy).PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_manager), action, reason))).ContinueWith(t => t.Result.ApprovalId);

    private static CheckoutOrderRequest Order(string cashierId, string shiftId, string productId, string method, long paid,
        long discount = 0, IReadOnlyList<CheckoutSplitRequest>? splits = null) => new(
        CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: cashierId, ShiftId: shiftId,
        OrderType: "takeaway", PaymentMethod: method,
        Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
        DiscountPiastres: discount, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
        CompApprovalId: null, CompApproverUserId: null, CompReason: null,
        AmountPaidPiastres: paid, SplitPayments: splits, WalletProvider: null,
        TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);

    private async Task<string> SellAsync(string cashierId, string shiftId, long price, string method, long discount = 0,
        IReadOnlyList<CheckoutSplitRequest>? splits = null)
    {
        var productId = await _factory.SeedProductAsync(price);
        var sale = await Ok<CheckoutOrderResult>(As(cashierId).PostAsJsonAsync("/api/orders/checkout",
            Order(cashierId, shiftId, productId, method, price - discount, discount, splits)));
        Assert.Equal(price - discount, sale.TotalPiastres);
        return sale.OrderId;
    }

    private async Task<(string OrderId, string DeliveryId)> DeliveryOrderAsync(string shiftId, long food, long fee, string area)
    {
        await _factory.SeedDeliveryServiceAsync($"svc-{area}", fee, area);
        var productId = await _factory.SeedProductAsync(food);
        var sale = await Ok<CheckoutOrderResult>(As(_cashierA).PostAsJsonAsync("/api/orders/checkout",
            Order(_cashierA, shiftId, productId, "cash", 0) with
            {
                OrderType = "delivery", DeliveryFeePiastres = fee, CustomerName = "عميل", CustomerPhone = "01000000000",
                DeliveryAddress = "شارع", DeliveryArea = area, DeliveryStreet = "شارع 9", DeliveryServiceId = $"svc-{area}", DeliveryServiceName = area,
            }));
        Assert.Equal(food + fee, sale.TotalPiastres);
        var deliveryId = await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == sale.OrderId).Select(d => d.Id).FirstAsync());
        return (sale.OrderId, deliveryId);
    }

    private async Task<PrepareShiftCloseResult> CountAsync(string cashierId, string shiftId, long counted) =>
        await Ok<PrepareShiftCloseResult>(As(cashierId).PostAsJsonAsync("/api/shifts/prepare-close", new PrepareShiftCloseRequest(shiftId, counted)));

    private async Task CloseDayAsync(string dayId)
    {
        var manager = As(_manager);
        foreach (var itemId in new[] { "close-shifts", "close-drawer", "close-delivery" })
            await Ok(manager.PostAsJsonAsync($"/api/business-day/{dayId}/checklist/{itemId}", new CompleteChecklistItemRequest(dayId, itemId, true, _manager)));
        await Ok(manager.PostAsJsonAsync($"/api/business-day/{dayId}/close", new CloseBusinessDayRequest($"close-{Guid.NewGuid():N}", dayId, _manager, null, null)));
    }

    private Task<BusinessDayReconciliationDto> ReconciliationAsync(string dayId) =>
        As(_manager).GetFromJsonAsync<BusinessDayReconciliationDto>($"/api/reports/business-day/{dayId}?actorUserId={_manager}")!;

    private async Task<long> BalanceAsync(string accountId)
    {
        using var balance = JsonDocument.Parse(await As(_manager).GetStringAsync($"/api/treasury/balance?accountId={accountId}"));
        return balance.RootElement.GetProperty("balancePiastres").GetInt64();
    }

    [Fact]
    public async Task Two_business_days_with_every_kind_of_sale_reconcile_to_the_counted_cash()
    {
        var manager = As(_manager);
        var tillA = As(_cashierA);
        var tillB = As(_cashierB);

        // ================= Day 1: yesterday's sale =================
        var day1 = await Ok<OpenBusinessDayResult>(manager.PostAsJsonAsync("/api/business-day/open", new OpenBusinessDayRequest($"bd-{Guid.NewGuid():N}", _manager)));
        var shift1 = await Ok<OpenShiftResult>(tillA.PostAsJsonAsync("/api/shifts/open", new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierA, 100_000)));
        var yesterdaysSale = await SellAsync(_cashierA, shift1.ShiftId, 80_000, "cash");
        Assert.False((await CountAsync(_cashierA, shift1.ShiftId, 180_000)).RequiresManagerApproval);
        await Ok<CloseShiftResult>(tillA.PostAsJsonAsync("/api/shifts/close", new CloseShiftRequest($"close-{Guid.NewGuid():N}", shift1.ShiftId, _cashierA, 180_000, null, null, null)));
        await CloseDayAsync(day1.BusinessDayId);
        var day1AtClose = await ReconciliationAsync(day1.BusinessDayId);
        Assert.Equal((80_000L, 0L, 80_000L), (day1AtClose.Sales.GrossSalesPiastres, day1AtClose.Sales.RefundsPiastres, day1AtClose.Sales.NetSalesPiastres));

        // ================= Day 2, shift A =================
        var day2 = await Ok<OpenBusinessDayResult>(manager.PostAsJsonAsync("/api/business-day/open", new OpenBusinessDayRequest($"bd-{Guid.NewGuid():N}", _manager)));
        var shiftA = await Ok<OpenShiftResult>(tillA.PostAsJsonAsync("/api/shifts/open", new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierA, 100_000)));
        Assert.Equal(100_000, shiftA.OpeningCashPiastres);

        await SellAsync(_cashierA, shiftA.ShiftId, 10_000, "cash");
        await SellAsync(_cashierA, shiftA.ShiftId, 12_000, "card");
        await SellAsync(_cashierA, shiftA.ShiftId, 20_000, "multi", splits: new[] { new CheckoutSplitRequest("cash", 8_000), new CheckoutSplitRequest("card", 12_000) });
        var instapayConfirmedLater = await SellAsync(_cashierA, shiftA.ShiftId, 15_000, "instapay");

        // InstaPay whose transfer never arrives: a recipe product, so cancelling "not prepared" must give the stock back.
        var (recipeProduct, _, inventoryItem) = await _factory.SeedProductWithRecipeAsync(9_000, 250);
        var instapayCancelled = (await Ok<CheckoutOrderResult>(tillA.PostAsJsonAsync("/api/orders/checkout",
            Order(_cashierA, shiftA.ShiftId, recipeProduct, "instapay", 9_000)))).OrderId;

        // Discounts: within the 10% limit no manager; above it the manager's PIN at the till.
        await SellAsync(_cashierA, shiftA.ShiftId, 10_000, "cash", discount: 1_000);
        var bigDiscountProduct = await _factory.SeedProductAsync(20_000);
        await Refused(tillA.PostAsJsonAsync("/api/orders/checkout", Order(_cashierA, shiftA.ShiftId, bigDiscountProduct, "cash", 15_000, 5_000)), "يتطلب اعتماد مدير");
        var discountApproval = await ApprovalAsync(_cashierA, "orders_discount", "عميل دائم");
        var bigDiscount = await Ok<CheckoutOrderResult>(tillA.PostAsJsonAsync("/api/orders/checkout",
            Order(_cashierA, shiftA.ShiftId, bigDiscountProduct, "cash", 15_000, 5_000) with { DiscountApprovalId = discountApproval, DiscountApproverUserId = _manager }));
        Assert.Equal(15_000, bigDiscount.TotalPiastres);

        // Delivery: one cash-on-delivery order delivered, one that fails (voided, food not prepared).
        var driverId = (await Ok<DriverDto>(As(_dispatcher).PostAsJsonAsync("/api/delivery/drivers", new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق", _dispatcher)))).Id;
        var delivered = await DeliveryOrderAsync(shiftA.ShiftId, 20_000, 2_000, "المنطقة-1");
        var failed = await DeliveryOrderAsync(shiftA.ShiftId, 10_000, 1_500, "المنطقة-2");
        foreach (var deliveryId in new[] { delivered.DeliveryId, failed.DeliveryId })
        {
            await Ok(As(_dispatcher).PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcher, null, null)));
            await Ok(As(_dispatcher).PostAsJsonAsync($"/api/delivery/{deliveryId}/assign", new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driverId, _dispatcher, null, null)));
        }
        await Ok(As(_dispatcher).PostAsJsonAsync($"/api/delivery/{delivered.DeliveryId}/result",
            new RecordDeliveryResultRequest($"req-{Guid.NewGuid():N}", delivered.DeliveryId, "Delivered", null, null, null, _dispatcher, null, null)));
        await Ok(As(_dispatcher).PostAsJsonAsync($"/api/delivery/{failed.DeliveryId}/result",
            new RecordDeliveryResultRequest($"req-{Guid.NewGuid():N}", failed.DeliveryId, "Failed", null, "customer_unreachable", null, _dispatcher, null, null, "not_prepared")));

        // The InstaPay transfer that never came.
        await Ok(tillA.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment",
            new CancelPendingElectronicPaymentRequest(instapayCancelled, "لم يصل التحويل", _cashierA, StockDisposition: "not_prepared")));

        // Yesterday's 800 ج sale refunded today: 200 by the cashier alone, the remaining 600 only with a manager.
        await Ok<RefundOrderResult>(tillA.PostAsJsonAsync("/api/orders/refund", new RefundOrderRequest($"rreq-{Guid.NewGuid():N}", yesterdaysSale, 20_000, "طلب العميل", _cashierA, null, null)));
        await Refused(tillA.PostAsJsonAsync("/api/orders/refund", new RefundOrderRequest($"rreq-{Guid.NewGuid():N}", yesterdaysSale, 60_000, "طلب العميل", _cashierA, null, null)), "مدير");
        var refundApproval = await ApprovalAsync(_cashierA, "orders_refund", "مرتجع كبير");
        var bigRefund = await Ok<RefundOrderResult>(tillA.PostAsJsonAsync("/api/orders/refund",
            new RefundOrderRequest($"rreq-{Guid.NewGuid():N}", yesterdaysSale, 60_000, "طلب العميل", _cashierA, refundApproval, _manager)));
        Assert.Equal(0, bigRefund.RemainingRefundablePiastres);

        // Expense from the drawer, cash into the safe, the driver hands in his collection less his fee.
        await Ok<AddExpenseResult>(tillA.PostAsJsonAsync("/api/expenses", new AddExpenseRequest($"exp-{Guid.NewGuid():N}", shiftA.ShiftId, "نظافة ومطهرات", 3_000, "منظفات", "tre-drawer", _cashierA, null, null)));
        await Ok<SafeDropResult>(tillA.PostAsJsonAsync("/api/treasury/safe-drop", new SafeDropRequest($"drop-{Guid.NewGuid():N}", shiftA.ShiftId, 20_000, "إيداع منتصف الوردية", _cashierA, null, null)));
        var account = await As(_dispatcher).GetFromJsonAsync<DriverAccountDto>($"/api/delivery/drivers/{driverId}/account?actorUserId={_dispatcher}");
        Assert.NotNull(account);
        await Ok(As(_dispatcher).PostAsJsonAsync($"/api/delivery/drivers/{driverId}/settle",
            new SettleDriverCustodyRequest($"set-{Guid.NewGuid():N}", driverId, 20_000, null, _dispatcher, null, null, 0, false)));

        // Stock: the cancelled, unprepared InstaPay order took its ingredients and gave them back.
        var stockNet = await _factory.QueryAsync(db => db.StockMovements.Where(m => m.InventoryItemId == inventoryItem)
            .SumAsync(m => m.Direction == "In" ? m.QuantityMilliUnits : -m.QuantityMilliUnits));
        var stockMovements = await _factory.QueryAsync(db => db.StockMovements.CountAsync(m => m.InventoryItemId == inventoryItem));
        Assert.True(stockMovements >= 2, $"expected a usage and its reversal, found {stockMovements} movements");
        Assert.Equal(0, stockNet);

        // Count. The first count is 10 ج short; the recount finds the 10 ج and needs the manager.
        Assert.Equal(ExpectedDrawerA, await _factory.ExpectedDrawerCashAsync(shiftA.ShiftId));
        var firstCount = await CountAsync(_cashierA, shiftA.ShiftId, ExpectedDrawerA - 1_000);
        Assert.Equal(-1_000, firstCount.DifferencePiastres);
        var recount = await CountAsync(_cashierA, shiftA.ShiftId, ExpectedDrawerA);
        Assert.True(recount.RequiresManagerApproval);
        await Refused(tillA.PostAsJsonAsync("/api/shifts/close", new CloseShiftRequest($"close-{Guid.NewGuid():N}", shiftA.ShiftId, _cashierA, ExpectedDrawerA, null, null, null)), "إعادة العدّ");
        var recountApproval = await ApprovalAsync(_cashierA, "shifts_manage", "إعادة عدّ أمام المدير");
        var closeA = await Ok<CloseShiftResult>(tillA.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"close-{Guid.NewGuid():N}", shiftA.ShiftId, _cashierA, ExpectedDrawerA, "أعيد العدّ أمام المدير", recountApproval, _manager)));
        Assert.Equal((ExpectedDrawerA, ExpectedDrawerA, 0L), (closeA.ExpectedCashPiastres, closeA.ActualCashPiastres, closeA.DifferencePiastres));

        // ================= Day 2, shift B =================
        var shiftB = await Ok<OpenShiftResult>(tillB.PostAsJsonAsync("/api/shifts/open", new OpenShiftRequest($"open-{Guid.NewGuid():N}", "مسائية", _cashierB, 50_000)));
        await Ok(tillB.PostAsJsonAsync("/api/orders/confirm-electronic-payment", new ConfirmElectronicPaymentRequest(instapayConfirmedLater, _cashierB)));
        await SellAsync(_cashierB, shiftB.ShiftId, 25_000, "cash");
        await SellAsync(_cashierB, shiftB.ShiftId, 5_000, "card");
        Assert.False((await CountAsync(_cashierB, shiftB.ShiftId, ExpectedDrawerB)).RequiresManagerApproval);
        var closeB = await Ok<CloseShiftResult>(tillB.PostAsJsonAsync("/api/shifts/close", new CloseShiftRequest($"close-{Guid.NewGuid():N}", shiftB.ShiftId, _cashierB, ExpectedDrawerB, null, null, null)));
        Assert.Equal(0, closeB.DifferencePiastres);

        await CloseDayAsync(day2.BusinessDayId);

        // ================= The books =================
        var d2 = await ReconciliationAsync(day2.BusinessDayId);
        var s = d2.Sales;
        // Counted sales: 100 + 120 + 200 + 150 + 90 + 150 + 220 + 250 + 50 = 1,330 ج. The cancelled InstaPay and the
        // failed delivery are in no sales figure. Today's refunds are of yesterday's sale.
        Assert.Equal((133_000L, 80_000L, 53_000L), (s.GrossSalesPiastres, s.RefundsPiastres, s.NetSalesPiastres));
        Assert.Equal(6_000, s.ManualDiscountsPiastres);
        Assert.Equal(0, s.PendingElectronicSalesPiastres);
        Assert.Equal((29_000L, 15_000L), (s.CardPiastres, s.InstaPayPiastres));
        Assert.Equal(3_000, d2.ExpensesTotalPiastres);
        Assert.Equal(2, d2.ShiftCount);
        Assert.Equal((ExpectedDrawerA + ExpectedDrawerB, ExpectedDrawerA + ExpectedDrawerB, 0L, 0L),
            (d2.ShiftsExpectedCashPiastres, d2.ShiftsActualCashPiastres, d2.ShiftsShortagePiastres, d2.ShiftsOveragePiastres));
        Assert.Equal(0, d2.Treasury.DrawerPiastres);

        // Yesterday, already closed, is as it was sold.
        var d1 = await ReconciliationAsync(day1.BusinessDayId);
        Assert.Equal((80_000L, 0L, 80_000L), (d1.Sales.GrossSalesPiastres, d1.Sales.RefundsPiastres, d1.Sales.NetSalesPiastres));

        // Every account: the drawer is empty, the safe holds exactly the cash that physically came in
        // (800 yesterday; today 100 + 80 + 90 + 150 + 200 from the driver + 250, less 800 refunded and 30 spent),
        // the bank the card takings, InstaPay the confirmed transfer.
        Assert.Equal(0, await BalanceAsync("tre-drawer"));
        Assert.Equal(Funding + 80_000 + (10_000 + 8_000 + 9_000 + 15_000 + 20_000 + 25_000) - 80_000 - 3_000, await BalanceAsync("tre-main"));
        Assert.Equal(29_000, await BalanceAsync("tre-bank"));
        Assert.Equal(15_000, await BalanceAsync("tre-instapay"));

        // The cash-flow report ends where the balances are; the dashboard's curve adds up to its headline.
        using var cashFlow = JsonDocument.Parse(await manager.GetStringAsync($"/api/reports/cash-flow?actorUserId={_manager}"));
        foreach (var row in cashFlow.RootElement.GetProperty("accounts").EnumerateArray())
            Assert.Equal(await BalanceAsync(row.GetProperty("accountId").GetString()!), row.GetProperty("closingPiastres").GetInt64());
        var dashboard = await manager.GetFromJsonAsync<OwnerDashboardDto>($"/api/reports/owner-dashboard?actorUserId={_manager}");
        Assert.Equal(dashboard!.Cash.NetSalesPiastres, dashboard.Trend.Series.Sum(p => p.NetSalesPiastres));

        // Both closes froze a snapshot; the approvals are named where the owner reads them.
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync($"/api/business-day/{day1.BusinessDayId}/close-snapshot")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync($"/api/business-day/{day2.BusinessDayId}/close-snapshot")).StatusCode);
        var shiftARow = await _factory.QueryAsync(db => db.Shifts.AsNoTracking().FirstAsync(x => x.Id == shiftA.ShiftId));
        Assert.Equal("المدير", shiftARow.DifferenceAuthorizedBy);
        var refundMovement = await _factory.QueryAsync(db => db.TreasuryMovements.AsNoTracking()
            .Where(m => m.Type == "refund" && m.ReferenceId == yesterdaysSale && m.AmountPiastres == 60_000).FirstAsync());
        Assert.Equal("المدير", refundMovement.ApprovedBy);
    }
}

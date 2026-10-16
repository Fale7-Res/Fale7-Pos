namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Delivery;
using Falah.Application.Expenses;
using Falah.Application.Inventory;
using Falah.Application.Orders;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Ported from lib/reporting-domain.mjs (selectSalesReport/selectExpensesReport/selectRefundActivity)
/// and lib/control-layer-domain.mjs (reconcileBusinessDay, minus its purchasing/payroll sub-sections
/// - neither Suppliers/Purchases nor Payroll exist in this system). Every figure asserted here is
/// checked against the SAME persisted rows the command handlers of earlier slices already wrote -
/// this suite exists to prove the report reconciles to that truth, never a second one.
/// </summary>
public class ReportingVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _shiftId = default!;
    private string _businessDayId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "expenses_add_drawer", "orders_discount");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _businessDayId = await _factory.SeedOpenBusinessDayWithFundedMainAsync();

        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<CheckoutOrderResult> CashOrderAsync(long totalPiastres, string orderType = "takeaway")
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: orderType, PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: totalPiastres, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    // ---- Sales report ----

    [Fact]
    public async Task Sales_report_reflects_a_real_cash_sale_for_this_shift()
    {
        await CashOrderAsync(15000);
        var report = await (await _client.GetAsync($"/api/reports/sales?actorUserId={_managerId}&shiftId={_shiftId}"))
            .Content.ReadFromJsonAsync<SalesReportDto>();

        Assert.Equal(1, report!.OrderCount);
        Assert.Equal(15000, report.GrossSalesPiastres);
        Assert.Equal(15000, report.NetSalesPiastres);
        Assert.Equal(15000, report.CashPiastres);
        Assert.Equal(15000, report.TakeawayPiastres);
    }

    [Fact]
    public async Task Sales_report_separates_offers_comps_and_manual_discounts_into_three_buckets()
    {
        var approval = await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "orders_comp", "ضيافة"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>();
        var productId = await _factory.SeedProductAsync(10000);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 500, DeliveryFeePiastres: 0, CompAmountPiastres: 1000,
            CompApprovalId: approval!.ApprovalId, CompApproverUserId: _managerId, CompReason: "ضيافة",
            AmountPaidPiastres: 8500, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null,
            CompReasonCode: "hospitality");
        (await _client.PostAsJsonAsync("/api/orders/checkout", request)).EnsureSuccessStatusCode();

        var report = await (await _client.GetAsync($"/api/reports/sales?actorUserId={_managerId}&shiftId={_shiftId}"))
            .Content.ReadFromJsonAsync<SalesReportDto>();
        Assert.Equal(1000, report!.CompsValuePiastres);
        Assert.Equal(500, report.ManualDiscountsPiastres);
    }

    [Fact]
    public async Task Sales_report_requires_reports_view()
    {
        var floorCashier = await _factory.SeedUserAsync("كاشير آخر", "cashier", "pos");
        var response = await _client.GetAsync($"/api/reports/sales?actorUserId={floorCashier}&shiftId={_shiftId}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---- Expense report ----

    [Fact]
    public async Task Expense_report_totals_and_lists_every_expense_for_the_shift()
    {
        await _client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest($"req-{Guid.NewGuid():N}", _shiftId, "مستلزمات", 3000, "أدوات تنظيف", "tre-drawer", _cashierId, null, null));
        await _client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest($"req-{Guid.NewGuid():N}", _shiftId, "صيانة", 2000, "إصلاح باب", "tre-drawer", _cashierId, null, null));

        var report = await (await _client.GetAsync($"/api/reports/expenses?actorUserId={_managerId}&shiftId={_shiftId}"))
            .Content.ReadFromJsonAsync<ExpenseReportDto>();
        Assert.Equal(5000, report!.TotalPiastres);
        Assert.Equal(2, report.Expenses.Count);
    }

    // ---- Refund activity ----

    [Fact]
    public async Task Refund_activity_reports_the_refund_with_its_order_and_shift_context()
    {
        var order = await CashOrderAsync(20000);
        var approval = await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "orders_refund", "طلب العميل"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>();
        await _client.PostAsJsonAsync("/api/orders/refund", new RefundOrderRequest(
            $"refreq-{Guid.NewGuid():N}", order.OrderId, 5000, "طلب العميل", _managerId, approval!.ApprovalId, _managerId));

        var activity = await (await _client.GetAsync($"/api/reports/refunds?actorUserId={_managerId}&shiftId={_shiftId}"))
            .Content.ReadFromJsonAsync<List<RefundActivityDto>>();
        Assert.NotNull(activity);
        Assert.Single(activity);
        Assert.Equal(order.OrderId, activity!.Single().OrderId);
        Assert.Equal(5000, activity.Single().AmountPiastres);
    }

    // ---- Business day reconciliation ----

    [Fact]
    public async Task Business_day_reconciliation_reflects_sales_expenses_and_treasury_balances()
    {
        await CashOrderAsync(15000);
        await _client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest($"req-{Guid.NewGuid():N}", _shiftId, "مستلزمات", 3000, "أدوات", "tre-drawer", _cashierId, null, null));

        var day = await (await _client.GetAsync($"/api/reports/business-day/{_businessDayId}?actorUserId={_managerId}"))
            .Content.ReadFromJsonAsync<BusinessDayReconciliationDto>();

        Assert.Equal(_businessDayId, day!.BusinessDayId);
        Assert.Equal(1, day.Sales.OrderCount);
        Assert.Equal(15000, day.Sales.NetSalesPiastres);
        Assert.Equal(3000, day.ExpensesTotalPiastres);
        Assert.Equal(1, day.ShiftCount);
    }

    [Fact]
    public async Task Business_day_reconciliation_404s_for_an_unknown_day()
    {
        var response = await _client.GetAsync($"/api/reports/business-day/no-such-day?actorUserId={_managerId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Top items / category sales ----

    [Fact]
    public async Task Top_selling_items_and_category_sales_aggregate_across_orders()
    {
        var order = await CashOrderAsync(12000);

        var topItems = await (await _client.GetAsync($"/api/reports/top-items?actorUserId={_managerId}")).Content.ReadFromJsonAsync<List<TopSellingItemDto>>();
        Assert.Contains(topItems!, i => i.TotalPiastres == 12000);

        var categories = await (await _client.GetAsync($"/api/reports/category-sales?actorUserId={_managerId}")).Content.ReadFromJsonAsync<List<CategorySalesDto>>();
        Assert.Contains(categories!, c => c.TotalPiastres >= 12000);
    }

    /// <summary>Top Selling says what sold and the dashboard trend says when the day was busy, but
    /// neither could put the two together. This one asks the product x hour endpoint and checks that
    /// a sale lands in the hour the Cairo wall clock was showing when it was rung up - not the UTC
    /// hour, which is one or two columns to the left of it.</summary>
    [Fact]
    public async Task Product_hourly_sales_put_a_sale_in_its_own_cairo_hour()
    {
        await CashOrderAsync(12000);

        var rows = await (await _client.GetAsync($"/api/reports/product-hourly?actorUserId={_managerId}"))
            .Content.ReadFromJsonAsync<List<ProductHourlySalesDto>>();

        var expectedHour = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Falah.Domain.Common.BusinessClock.CairoTimeZone).Hour;
        var sold = Assert.Single(rows!, r => r.TotalPiastres == 12000);
        Assert.Equal(expectedHour, sold.Hour);
        Assert.Equal(1, sold.Quantity);

        // Every hour it reports is a real hour, and no hour is reported twice for one product.
        Assert.All(rows!, r => Assert.InRange(r.Hour, 0, 23));
        Assert.Equal(rows!.Count, rows.Select(r => (r.ProductName, r.Hour)).Distinct().Count());
    }

    /// <summary>The whole reason the screen can now ask for a week: a range that excludes today has
    /// to come back empty rather than quietly falling back to everything.</summary>
    [Fact]
    public async Task Product_hourly_sales_respect_the_requested_range()
    {
        await CashOrderAsync(12000);

        var today = Falah.Domain.Common.BusinessClock.Today();
        var lastWeek = today.AddDays(-7);

        var inRange = await (await _client.GetAsync(
            $"/api/reports/product-hourly?actorUserId={_managerId}&from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}"))
            .Content.ReadFromJsonAsync<List<ProductHourlySalesDto>>();
        Assert.Contains(inRange!, r => r.TotalPiastres == 12000);

        var outOfRange = await (await _client.GetAsync(
            $"/api/reports/product-hourly?actorUserId={_managerId}&from={lastWeek:yyyy-MM-dd}&to={lastWeek:yyyy-MM-dd}"))
            .Content.ReadFromJsonAsync<List<ProductHourlySalesDto>>();
        Assert.Empty(outOfRange!);
    }

    // ---- Food cost report ----

    [Fact]
    public async Task Food_cost_report_computes_cogs_from_sold_items_current_recipe_cost()
    {
        var (productId, recipeId, inventoryItemId) = await _factory.SeedProductWithRecipeAsync(5000, quantityMilliUnitsPerUnit: 200);
        await _factory.QueryAsync(async db =>
        {
            db.StockMovements.Add(new Falah.Infrastructure.Persistence.Entities.StockMovementEntity
            {
                IdempotencyKey = $"seed-{Guid.NewGuid():N}", InventoryItemId = inventoryItemId, Type = "PurchaseReceipt",
                Direction = "In", QuantityMilliUnits = 10000, UnitCostPiastres = 1000, ReferenceType = "seed",
                ReferenceId = "seed", Reason = "seed", CreatedBy = "seed", CreatedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
            return 0;
        });

        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 5000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        (await _client.PostAsJsonAsync("/api/orders/checkout", request)).EnsureSuccessStatusCode();

        var report = await (await _client.GetAsync($"/api/reports/food-cost?actorUserId={_managerId}")).Content.ReadFromJsonAsync<FoodCostReportDto>();
        Assert.Equal(200, report!.TotalFoodCostPiastres); // 200 milliUnits * 1000 piastres / 1000
        Assert.Empty(report.Missing);
    }

    [Fact]
    public async Task Food_cost_report_names_a_sold_item_with_unknown_ingredient_cost_instead_of_folding_it_in_as_zero()
    {
        var (productId, _, _) = await _factory.SeedProductWithRecipeAsync(5000, quantityMilliUnitsPerUnit: 200); // no purchase receipt seeded - unknown cost
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 5000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        (await _client.PostAsJsonAsync("/api/orders/checkout", request)).EnsureSuccessStatusCode();

        var report = await (await _client.GetAsync($"/api/reports/food-cost?actorUserId={_managerId}")).Content.ReadFromJsonAsync<FoodCostReportDto>();
        Assert.Equal(0, report!.TotalFoodCostPiastres);
        Assert.Single(report.Missing);
    }

    /// <summary>The most expensive kind of wrong number: not a missing one, but a confident one.
    /// A dish attached to a recipe with no ingredients was counted as a dish whose cost is KNOWN to
    /// be zero, so a menu of them reported a hundred percent gross margin. Recipes like this cannot
    /// be saved any more, but ones already stored still have to be read honestly.</summary>
    [Fact]
    public async Task A_dish_whose_recipe_has_no_ingredients_is_unknown_cost_not_zero_cost()
    {
        var (productId, recipeId, _) = await _factory.SeedProductWithRecipeAsync(5000, quantityMilliUnitsPerUnit: 200);
        await _factory.QueryAsync(async db =>
        {
            var recipe = await db.Recipes.Include(r => r.Items).FirstAsync(r => r.Id == recipeId);
            db.RecipeItems.RemoveRange(recipe.Items);
            await db.SaveChangesAsync();
            return 0;
        });

        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 5000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        (await _client.PostAsJsonAsync("/api/orders/checkout", request)).EnsureSuccessStatusCode();

        var report = await (await _client.GetAsync($"/api/reports/food-cost?actorUserId={_managerId}")).Content.ReadFromJsonAsync<FoodCostReportDto>();
        Assert.Equal(1, report!.SoldLineCount);
        Assert.Equal(0, report.KnownLineCount);   // it was 1 - "known to cost nothing"
        Assert.Single(report.Missing);
    }

    // ---- Significant actions ----

    [Fact]
    public async Task Significant_actions_lists_a_manager_approved_drawer_expense()
    {
        var approval = await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "expenses_add_drawer", "اعتماد"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>();
        var noPermCashier = await _factory.SeedUserAsync("كاشير بلا صلاحية", "cashier", "pos");
        await _client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest($"req-{Guid.NewGuid():N}", _shiftId, "مستلزمات", 4000, "بند حساس", "tre-drawer", noPermCashier, approval!.ApprovalId, _managerId));

        var actions = await (await _client.GetAsync($"/api/reports/significant-actions?actorUserId={_managerId}")).Content.ReadFromJsonAsync<List<SignificantActionDto>>();
        Assert.Contains(actions!, a => a.Category == "expense" && a.Approver == "المدير" && a.AmountPiastres == 4000);
        // The person, never the internal user id - every other source in this report names a person.
        Assert.Contains(actions!, a => a.Category == "expense" && a.AmountPiastres == 4000 && a.Actor == "كاشير بلا صلاحية");
    }

    [Fact]
    public async Task An_unapproved_expense_never_appears_in_significant_actions()
    {
        await _client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest($"req-{Guid.NewGuid():N}", _shiftId, "مستلزمات", 1000, "بند عادي", "tre-drawer", _cashierId, null, null));
        var actions = await (await _client.GetAsync($"/api/reports/significant-actions?actorUserId={_managerId}")).Content.ReadFromJsonAsync<List<SignificantActionDto>>();
        Assert.DoesNotContain(actions!, a => a.AmountPiastres == 1000);
    }

    // ---- Shift summary ----

    [Fact]
    public async Task Shift_summary_reports_sales_expenses_and_opening_cash_for_one_shift()
    {
        await CashOrderAsync(10000);
        await _client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest($"req-{Guid.NewGuid():N}", _shiftId, "مستلزمات", 2000, "أدوات", "tre-drawer", _cashierId, null, null));

        var summary = await (await _client.GetAsync($"/api/reports/shifts/{_shiftId}?actorUserId={_managerId}")).Content.ReadFromJsonAsync<ShiftSummaryDto>();
        Assert.Equal(50000, summary!.OpeningCashPiastres);
        Assert.Equal(1, summary.Sales.OrderCount);
        Assert.Equal(10000, summary.Sales.NetSalesPiastres);
        Assert.Equal(2000, summary.ExpensesTotalPiastres);
    }

    /// <summary>The Shifts screen's history table - the same per-shift figures
    /// Shift_summary_reports_sales_expenses_and_opening_cash_for_one_shift proves for one shift,
    /// here for the whole list, plus the display fields (who opened it, when) that list needs and
    /// the single-shift report never did.</summary>
    [Fact]
    public async Task Shifts_history_lists_the_open_shift_with_its_name_and_totals()
    {
        await CashOrderAsync(15000);

        var history = await (await _client.GetAsync($"/api/reports/shifts?actorUserId={_managerId}"))
            .Content.ReadFromJsonAsync<List<ShiftSummaryDto>>();

        var row = Assert.Single(history!, s => s.ShiftId == _shiftId);
        Assert.Equal("Open", row.Status);
        Assert.Equal("كاشير الاختبار", row.OpenedByUserName);
        Assert.Null(row.ClosedByUserName);
        Assert.Null(row.ClosedAtUtc);
        Assert.Equal(50000, row.OpeningCashPiastres);
        Assert.Equal(15000, row.Sales.NetSalesPiastres);
    }

    /// <summary>Closed shifts carry a closer's name distinct from the opener's - the history table's
    /// whole reason for showing both columns.</summary>
    [Fact]
    public async Task Shifts_history_shows_who_closed_a_shift_separately_from_who_opened_it()
    {
        await (await _client.PostAsJsonAsync("/api/shifts/prepare-close",
            new PrepareShiftCloseRequest(_shiftId, 50000))).Content.ReadFromJsonAsync<PrepareShiftCloseResult>();
        await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"close-{Guid.NewGuid():N}", _shiftId, _managerId, 50000, null, null, null));

        var history = await (await _client.GetAsync($"/api/reports/shifts?actorUserId={_managerId}"))
            .Content.ReadFromJsonAsync<List<ShiftSummaryDto>>();

        var row = Assert.Single(history!, s => s.ShiftId == _shiftId);
        Assert.Equal("Closed", row.Status);
        Assert.Equal("كاشير الاختبار", row.OpenedByUserName);
        Assert.Equal("المدير", row.ClosedByUserName);
        Assert.NotNull(row.ClosedAtUtc);
    }

    /// <summary>Gated exactly like the single-shift report it is built from - sales/cash figures are
    /// management information, whichever shift they belong to.</summary>
    [Fact]
    public async Task Shifts_history_is_refused_without_reports_view()
    {
        var noPermCashier = await _factory.SeedUserAsync("كاشير بلا صلاحية", "cashier", "pos");
        var response = await _client.GetAsync($"/api/reports/shifts?actorUserId={noPermCashier}");
        Assert.False(response.IsSuccessStatusCode);
    }

    /// <summary>The current-shift endpoint used to report only an id - a manager who never opened
    /// the shift themselves had no way to see whose it was without reading the history table.</summary>
    [Fact]
    public async Task Current_shift_reports_who_opened_it_by_name()
    {
        var current = await (await _client.GetAsync("/api/shifts/current")).Content.ReadFromJsonAsync<CurrentShiftDto>();
        Assert.Equal("كاشير الاختبار", current!.OpenedByUserName);
    }

    // ---- Delivery outcomes ----

    [Fact]
    public async Task Delivery_outcomes_report_counts_delivered_orders_and_unsettled_custody()
    {
        var productId = await _factory.SeedProductAsync(15000);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "delivery", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 1000, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: "أحمد", CustomerPhone: "01000000000", DeliveryAddress: null, DeliveryArea: "المعادي",
            BusinessDayId: null, DeliveryStreet: "شارع 9", DeliveryServiceId: "svc-1", DeliveryServiceName: "توصيل");
        var order = await (await _client.PostAsJsonAsync("/api/orders/checkout", request)).Content.ReadFromJsonAsync<CheckoutOrderResult>();

        var deliveryId = await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == order!.OrderId).Select(d => d.Id).FirstAsync());
        await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _managerId, null, null));
        var driver = await (await _client.PostAsJsonAsync("/api/delivery/drivers", new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق", _managerId))).Content.ReadFromJsonAsync<DriverDto>();
        await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign", new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driver!.Id, _managerId, null, null));
        await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result", new RecordDeliveryResultRequest($"req-{Guid.NewGuid():N}", deliveryId, "Delivered", null, null, null, _managerId, null, null));

        var report = await (await _client.GetAsync($"/api/reports/delivery-outcomes?actorUserId={_managerId}")).Content.ReadFromJsonAsync<DeliveryOutcomesReportDto>();
        Assert.Equal(1, report!.DeliveredCount);
        Assert.Equal(1, report.UnsettledCustodyCount); // delivered but not yet settled
        Assert.Equal(16000, report.UnsettledCustodyExpectedPiastres); // 15000 product + 1000 delivery fee, cash-on-delivery

        // Ported from deliveryReport's settledCashPiastres/shortages/overages/trips (here:
        // SettlementCount, since this per-order model has no separate "trip" concept) - a real gap
        // found during the legacy-parity audit.
        await _client.PostAsJsonAsync($"/api/delivery/drivers/{driver!.Id}/settle",
            new SettleDriverCustodyRequest($"req-{Guid.NewGuid():N}", driver.Id, 16000, null, _managerId, null, null));

        var afterSettle = await (await _client.GetAsync($"/api/reports/delivery-outcomes?actorUserId={_managerId}")).Content.ReadFromJsonAsync<DeliveryOutcomesReportDto>();
        Assert.Equal(0, afterSettle!.UnsettledCustodyCount);
        Assert.Equal(1, afterSettle.SettlementCount);
        Assert.Equal(16000, afterSettle.SettledCashPiastres);
        Assert.Equal(0, afterSettle.ShortagesPiastres);
        Assert.Equal(0, afterSettle.OveragesPiastres);
    }

    // ---- Tax report ----

    [Fact]
    public async Task Tax_report_computes_gross_taxable_and_tax_from_the_orders_own_frozen_snapshot()
    {
        var taxRule = await (await _client.PostAsJsonAsync("/api/tax-rules",
            new Falah.Application.Pricing.CreateTaxRuleRequest($"req-{Guid.NewGuid():N}", "ضريبة اختبار", 1400, "Exclusive", true, _managerId)))
            .Content.ReadFromJsonAsync<Falah.Application.Pricing.CreateTaxRuleResult>();
        Assert.NotNull(taxRule);

        var productId = await _factory.SeedProductAsync(10000);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 11400, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        (await _client.PostAsJsonAsync("/api/orders/checkout", request)).EnsureSuccessStatusCode();

        var report = await (await _client.GetAsync($"/api/reports/tax?actorUserId={_managerId}&shiftId={_shiftId}"))
            .Content.ReadFromJsonAsync<TaxReportDto>();
        Assert.Equal(10000, report!.TaxablePiastres);
        Assert.Equal(1400, report.TaxPiastres);
        Assert.Equal(11400, report.GrossPiastres);
    }

    [Fact]
    public async Task Tax_report_requires_reports_view()
    {
        var noReportsUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");
        var response = await _client.GetAsync($"/api/reports/tax?actorUserId={noReportsUser}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---- P0 regression: every Reports-screen panel must agree for the same business date ----

    /// <summary>The confirmed pre-go-live bug: ReportsViewModel's own GetSalesReportAsync call
    /// always appended "&shiftId=" even with no shift chosen - present but empty, not absent - so
    /// ASP.NET Core bound the server's shiftId to "" rather than null, and LoadSalesOrdersAsync's
    /// "if (shiftId is not null)" then filtered to ShiftId == "", matching zero orders. Every sibling
    /// panel (top-items, product-hourly, delivery-outcomes, tax, food-cost) has no shiftId parameter
    /// at all and kept reading the same rows correctly - exactly "headline zero, everything below it
    /// real." Reproduces the exact historic query string (?shiftId= with no value) against every
    /// panel for the same day and proves they now all agree.</summary>
    [Fact]
    public async Task All_report_panels_agree_for_the_same_business_date_even_with_an_empty_shiftId_query_value()
    {
        var (productId, recipeId, inventoryItemId) = await _factory.SeedProductWithRecipeAsync(10000, quantityMilliUnitsPerUnit: 200);
        await _factory.QueryAsync(async db =>
        {
            db.StockMovements.Add(new Falah.Infrastructure.Persistence.Entities.StockMovementEntity
            {
                IdempotencyKey = $"seed-{Guid.NewGuid():N}", InventoryItemId = inventoryItemId, Type = "PurchaseReceipt",
                Direction = "In", QuantityMilliUnits = 10000, UnitCostPiastres = 1000, ReferenceType = "seed",
                ReferenceId = "seed", Reason = "seed", CreatedBy = "seed", CreatedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
            return 0;
        });
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 10000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        (await _client.PostAsJsonAsync("/api/orders/checkout", request)).EnsureSuccessStatusCode();

        var today = Falah.Domain.Common.BusinessClock.Today();
        var q = $"actorUserId={_managerId}&from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}";

        // The exact historic client bug, reproduced literally: shiftId present, value empty.
        var sales = await (await _client.GetAsync($"/api/reports/sales?{q}&shiftId="))
            .Content.ReadFromJsonAsync<SalesReportDto>();
        var topItems = await (await _client.GetAsync($"/api/reports/top-items?{q}")).Content.ReadFromJsonAsync<List<TopSellingItemDto>>();
        var hourly = await (await _client.GetAsync($"/api/reports/product-hourly?{q}")).Content.ReadFromJsonAsync<List<ProductHourlySalesDto>>();
        var tax = await (await _client.GetAsync($"/api/reports/tax?{q}")).Content.ReadFromJsonAsync<TaxReportDto>();
        var foodCost = await (await _client.GetAsync($"/api/reports/food-cost?{q}")).Content.ReadFromJsonAsync<FoodCostReportDto>();

        // The headline must no longer read zero while the panels below it are real.
        Assert.Equal(1, sales!.OrderCount);
        Assert.Equal(10000, sales.NetSalesPiastres);
        Assert.Equal(10000, sales.CollectedSalesPiastres);

        Assert.Contains(topItems!, i => i.TotalPiastres == 10000);
        Assert.Contains(hourly!, h => h.TotalPiastres == 10000);
        Assert.Equal(10000, tax!.GrossPiastres);
        Assert.Equal(200, foodCost!.TotalFoodCostPiastres); // 200 milliUnits * 1000 piastres / 1000, same as the dedicated food-cost test

        // And an explicit shiftId (present, non-empty) must still filter correctly - the fix must
        // not have turned the filter itself into a no-op.
        var wrongShift = await (await _client.GetAsync($"/api/reports/sales?{q}&shiftId=no-such-shift"))
            .Content.ReadFromJsonAsync<SalesReportDto>();
        Assert.Equal(0, wrongShift!.OrderCount);
        var rightShift = await (await _client.GetAsync($"/api/reports/sales?{q}&shiftId={_shiftId}"))
            .Content.ReadFromJsonAsync<SalesReportDto>();
        Assert.Equal(1, rightShift!.OrderCount);
        _ = recipeId;
    }
}

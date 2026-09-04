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
        _client = _factory.CreateClient();
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
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
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
            new IssueManagerApprovalRequest(_managerId, "orders_comp", "ضيافة"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>();
        var productId = await _factory.SeedProductAsync(10000);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 500, DeliveryFeePiastres: 0, CompAmountPiastres: 1000,
            CompApprovalId: approval!.ApprovalId, CompApproverUserId: _managerId, CompReason: "ضيافة",
            AmountPaidPiastres: 8500, SplitPayments: null, WalletProvider: null,
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null,
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
            new IssueManagerApprovalRequest(_managerId, "orders_refund", "طلب العميل"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>();
        await _client.PostAsJsonAsync("/api/orders/refund", new RefundOrderRequest(
            $"refreq-{Guid.NewGuid():N}", order.OrderId, 5000, "طلب العميل", _managerId, approval!.ApprovalId, _managerId));

        var activity = await (await _client.GetAsync($"/api/reports/refunds?actorUserId={_managerId}&shiftId={_shiftId}"))
            .Content.ReadFromJsonAsync<List<RefundActivityDto>>();
        Assert.Single(activity!);
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
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
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
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        (await _client.PostAsJsonAsync("/api/orders/checkout", request)).EnsureSuccessStatusCode();

        var report = await (await _client.GetAsync($"/api/reports/food-cost?actorUserId={_managerId}")).Content.ReadFromJsonAsync<FoodCostReportDto>();
        Assert.Equal(0, report!.TotalFoodCostPiastres);
        Assert.Single(report.Missing);
    }

    // ---- Significant actions ----

    [Fact]
    public async Task Significant_actions_lists_a_manager_approved_drawer_expense()
    {
        var approval = await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_managerId, "expenses_add_drawer", "اعتماد"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>();
        var noPermCashier = await _factory.SeedUserAsync("كاشير بلا صلاحية", "cashier", "pos");
        await _client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest($"req-{Guid.NewGuid():N}", _shiftId, "مستلزمات", 4000, "بند حساس", "tre-drawer", noPermCashier, approval!.ApprovalId, _managerId));

        var actions = await (await _client.GetAsync($"/api/reports/significant-actions?actorUserId={_managerId}")).Content.ReadFromJsonAsync<List<SignificantActionDto>>();
        Assert.Contains(actions!, a => a.Category == "expense" && a.Approver == "المدير" && a.AmountPiastres == 4000);
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
            CustomerName: "أحمد", CustomerPhone: "01000000000", DeliveryAddress: null, DeliveryArea: "المعادي",
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
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
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
}

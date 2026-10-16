namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Delivery;
using Falah.Application.Employees;
using Falah.Application.Expenses;
using Falah.Application.Inventory;
using Falah.Application.Orders;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Application.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// "كشف اليوم" (/api/reports/daily-statement) - proves DailyStatementQueryService reconciles to the
/// SAME persisted rows every other report/screen already reads (see that class's own doc comment),
/// and specifically covers the areas this feature pass touched: the widened complete product list,
/// day-scoped treasury/employee/audit views, and the two confirmed bug fixes (waste's Cairo boundary,
/// discount auditing). Sales-formula correctness itself (cash-basis, cancelled-exclusion, pending-
/// electronic, COD) is already proven exhaustively by ReportingVerticalSliceTests against the
/// underlying SalesRules - this suite only proves the daily statement surfaces those same, unchanged
/// semantics correctly, not that the formulas themselves are right (that would duplicate coverage).
/// </summary>
public class DailyStatementVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _hrUserId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "expenses_add_drawer", "orders_discount");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _hrUserId = await _factory.SeedUserAsync("الموارد البشرية", "supervisor", "employees_manage", "employees_ledger");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private sealed record BalanceResponse(string employeeId, long balancePiastres);

    private async Task<DailyStatementDto> GetTodayAsync() =>
        (await (await _client.GetAsync($"/api/reports/daily-statement?actorUserId={_managerId}"))
            .Content.ReadFromJsonAsync<DailyStatementDto>())!;

    private async Task<CheckoutOrderResult> CheckoutAsync(
        string productName, long totalPiastres, string paymentMethod = "cash", string orderType = "takeaway", long amountPaid = -1)
    {
        var productId = await _factory.SeedProductAsync(totalPiastres, productName);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: orderType, PaymentMethod: paymentMethod,
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: amountPaid < 0 ? totalPiastres : amountPaid, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    // ---- Permission gate ----

    [Fact]
    public async Task Daily_statement_requires_reports_view()
    {
        var floorCashier = await _factory.SeedUserAsync("كاشير بلا صلاحية", "cashier", "pos");
        var response = await _client.GetAsync($"/api/reports/daily-statement?actorUserId={floorCashier}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---- Complete sold-product list (Section 2 - not just top-N) ----

    [Fact]
    public async Task Sold_products_list_is_complete_with_category_and_order_count_not_just_top_items()
    {
        await CheckoutAsync("كفتة", 12000);
        await CheckoutAsync("كفتة", 12000); // a second, separate order of the same product

        var statement = await GetTodayAsync();
        var row = Assert.Single(statement.SoldProducts, p => p.ProductName == "كفتة");
        Assert.Equal(2, row.Quantity);
        Assert.Equal(2, row.OrderCount); // two distinct orders, not one order of quantity 2
        Assert.Equal(24000, row.TotalPiastres);
        Assert.InRange(row.PeakHour, 0, 23);

        Assert.Contains(statement.ProductHourly, h => h.ProductName == "كفتة");
    }

    // ---- Expense detail (Section 4 - itemized, not just a total) ----

    [Fact]
    public async Task Expense_lines_are_itemized_with_the_entering_users_real_name()
    {
        await _client.PostAsJsonAsync("/api/expenses",
            new AddExpenseRequest($"req-{Guid.NewGuid():N}", _shiftId, "مستلزمات", 3000, "أدوات تنظيف", "tre-drawer", _cashierId, null, null));

        var statement = await GetTodayAsync();
        Assert.Equal(3000, statement.Expenses.TotalPiastres);
        var line = Assert.Single(statement.ExpenseLines);
        Assert.Equal("مستلزمات", line.Category);
        Assert.Equal("أدوات تنظيف", line.Description);
        Assert.Equal(3000, line.AmountPiastres);
        Assert.Equal("كاشير الاختبار", line.EnteredByName); // resolved from CreatedByUserId, not the raw id
    }

    // ---- Shifts (Section 5 - per-shift rows, never netted into one figure) ----

    [Fact]
    public async Task Shift_rows_and_day_totals_both_appear_never_only_the_netted_total()
    {
        await CheckoutAsync("شاورما", 10000);

        var statement = await GetTodayAsync();
        var shift = Assert.Single(statement.Shifts, s => s.ShiftId == _shiftId);
        Assert.Equal(50000, shift.OpeningCashPiastres);
        Assert.Equal(10000, shift.Sales.NetSalesPiastres);

        Assert.Equal(1, statement.ShiftTotals.ShiftCount);
        Assert.Equal(50000, statement.ShiftTotals.OpeningCashPiastres);
    }

    // ---- Treasury (Section 6 - day-scoped movement filtering) ----

    [Fact]
    public async Task Treasury_movements_are_filtered_to_the_requested_day_a_different_days_movement_is_excluded()
    {
        var approval = await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new Falah.Application.Approvals.IssueManagerApprovalRequest(_factory.PinFor(_managerId), "treasury_manage", "إيداع اختبار")))
            .Content.ReadFromJsonAsync<Falah.Application.Approvals.IssueManagerApprovalResult>();
        await _client.PostAsJsonAsync("/api/treasury/deposit", new Falah.Application.Treasury.DepositRequest(
            $"req-{Guid.NewGuid():N}", "owner_deposit", 5000, "tre-main", "إيداع اليوم", null, _managerId, approval!.ApprovalId, _managerId));

        // A movement really posted, but three days ago - must never leak into today's statement.
        var oldMovementId = $"tm-old-{Guid.NewGuid():N}";
        await _factory.QueryAsync(async db =>
        {
            db.TreasuryMovements.Add(new Falah.Infrastructure.Persistence.Entities.TreasuryMovementEntity
            {
                Id = oldMovementId, IdempotencyKey = oldMovementId, OperationId = $"op-{oldMovementId}", Type = "owner_deposit", Status = "Posted",
                DestinationAccountId = "tre-main", AmountPiastres = 9999, ReferenceType = "seed", ReferenceId = "seed",
                Reason = "قديم", CreatedBy = "seed", CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-3),
            });
            await db.SaveChangesAsync();
            return 0;
        });

        var statement = await GetTodayAsync();
        Assert.Contains(statement.TreasuryMovements, m => m.AmountPiastres == 5000 && m.Type == "owner_deposit");
        Assert.DoesNotContain(statement.TreasuryMovements, m => m.Id == oldMovementId);
    }

    // ---- Employee day (Section 7) ----

    [Fact]
    public async Task Employee_row_reflects_todays_ledger_entries_and_the_real_running_balance()
    {
        var employee = (await (await _client.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest(
            $"req-{Guid.NewGuid():N}", "أحمد النادل", "EMP-1", "نادل", "01000000000", new DateOnly(2024, 1, 1),
            "Daily", 20000, 15000, null, false, "cashier", null, null, null, null, null, null, null, null, _hrUserId)))
            .Content.ReadFromJsonAsync<EmployeeDto>())!;

        await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/ledger-entries",
            new RecordManualLedgerEntryRequest(employee.Id, "DailyWage", 20000, true, "يومية اليوم", _hrUserId, null, null));
        await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/ledger-entries",
            new RecordManualLedgerEntryRequest(employee.Id, "Bonus", 5000, true, "مكافأة", _hrUserId, null, null));
        await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/ledger-entries",
            new RecordManualLedgerEntryRequest(employee.Id, "Deduction", 2000, false, "خصم", _hrUserId, null, null));

        var statement = await GetTodayAsync();
        var row = Assert.Single(statement.Employees, e => e.EmployeeId == employee.Id);
        Assert.Equal(20000, row.DailyWageAccruedPiastres);
        Assert.Equal(5000, row.BonusPiastres);
        Assert.Equal(2000, row.DeductionPiastres);
        Assert.Equal(23000, row.RunningBalancePiastres); // 20000 + 5000 - 2000, same all-time balance the Employees screen reads

        var balance = await (await _client.GetAsync($"/api/employees/{employee.Id}/balance")).Content.ReadFromJsonAsync<BalanceResponse>();
        Assert.Equal(row.RunningBalancePiastres, balance!.balancePiastres); // never a second, disagreeing computation

        (await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/pay",
            new PayEmployeeRequest($"pay-{Guid.NewGuid():N}", employee.Id, 3000, "tre-main", _hrUserId,
                "دفعة من المستحقات", null, null))).EnsureSuccessStatusCode();
        var paidRow = Assert.Single((await GetTodayAsync()).Employees, e => e.EmployeeId == employee.Id);
        Assert.Equal(3000, paidRow.SettlementPaidTodayPiastres);
        Assert.Equal(3000, paidRow.TotalPaidTodayPiastres);
        Assert.Equal(20000, paidRow.RunningBalancePiastres);
    }

    // ---- Delivery / COD / driver custody (Section 9) ----

    [Fact]
    public async Task Delivery_and_cod_custody_figures_match_the_dedicated_delivery_outcomes_report()
    {
        var productId = await _factory.SeedProductAsync(15000, "برجر توصيل");
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "delivery", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 1000, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: "عميل", CustomerPhone: "01000000000", DeliveryAddress: null, DeliveryArea: "المعادي",
            BusinessDayId: null, DeliveryStreet: "شارع 9", DeliveryServiceId: "svc-1", DeliveryServiceName: "توصيل");
        var order = await (await _client.PostAsJsonAsync("/api/orders/checkout", request)).Content.ReadFromJsonAsync<CheckoutOrderResult>();
        var deliveryId = await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == order!.OrderId).Select(d => d.Id).FirstAsync());
        await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _managerId, null, null));
        var driver = await (await _client.PostAsJsonAsync("/api/delivery/drivers", new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق", _managerId))).Content.ReadFromJsonAsync<DriverDto>();
        await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign", new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driver!.Id, _managerId, null, null));
        await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result", new RecordDeliveryResultRequest($"req-{Guid.NewGuid():N}", deliveryId, "Delivered", null, null, null, _managerId, null, null));

        var statement = await GetTodayAsync();
        Assert.Equal(16000, statement.Sales.CashWithDriversPiastres); // still with the driver, not the restaurant
        Assert.Equal(1, statement.Delivery.DeliveredCount);
        Assert.Equal(1, statement.Delivery.UnsettledCustodyCount);
        Assert.Equal(16000, statement.Delivery.UnsettledCustodyExpectedPiastres);
    }

    // ---- Pending electronic + cancelled-order exclusion (Sections 1/10 semantics) ----

    [Fact]
    public async Task Pending_electronic_sale_counts_in_net_but_not_collected_and_disappears_once_cancelled()
    {
        var order = await CheckoutAsync("مشروب معلق", 9000, paymentMethod: "instapay");

        var pending = await GetTodayAsync();
        Assert.Equal(1, pending.Sales.OrderCount);
        Assert.Equal(9000, pending.Sales.NetSalesPiastres);
        Assert.Equal(9000, pending.Sales.PendingElectronicSalesPiastres);
        Assert.Equal(0, pending.Sales.CollectedSalesPiastres); // not actually held yet
        Assert.Contains(pending.SoldProducts, p => p.ProductName == "مشروب معلق");

        await _client.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment",
            new CancelPendingElectronicPaymentRequest(order.OrderId, "لم يصل التحويل", _cashierId));

        var afterCancel = await GetTodayAsync();
        Assert.Equal(0, afterCancel.Sales.OrderCount); // cancelled orders are excluded entirely, everywhere
        Assert.Equal(0, afterCancel.Sales.PendingElectronicSalesPiastres);
        Assert.DoesNotContain(afterCancel.SoldProducts, p => p.ProductName == "مشروب معلق");
    }

    // ---- Refund / net-sales consistency ----

    [Fact]
    public async Task A_refund_reduces_net_sales_by_exactly_the_refunded_amount_and_appears_in_the_refund_list()
    {
        var order = await CheckoutAsync("طاجن", 20000);
        var approval = await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new Falah.Application.Approvals.IssueManagerApprovalRequest(_factory.PinFor(_managerId), "orders_refund", "طلب العميل")))
            .Content.ReadFromJsonAsync<Falah.Application.Approvals.IssueManagerApprovalResult>();
        // Checked, not fire-and-forget: if the refund itself is rejected the interesting failure is
        // "the refund did not post", and leaving it unchecked reported that as a confusing assertion
        // on RefundsPiastres being 0 several lines later.
        var refundResponse = await _client.PostAsJsonAsync("/api/orders/refund", new RefundOrderRequest(
            $"refreq-{Guid.NewGuid():N}", order.OrderId, 5000, "طلب العميل", _managerId, approval!.ApprovalId, _managerId));
        refundResponse.EnsureSuccessStatusCode();

        var statement = await GetTodayAsync();
        Assert.Equal(20000, statement.Sales.GrossSalesPiastres);
        Assert.Equal(5000, statement.Sales.RefundsPiastres);
        Assert.Equal(15000, statement.Sales.NetSalesPiastres);
        Assert.Single(statement.Refunds, r => r.OrderId == order.OrderId && r.AmountPiastres == 5000);
    }

    // ---- Discount auditing (a confirmed gap this pass fixed) ----

    [Fact]
    public async Task A_manual_discount_now_appears_in_the_unified_audit_trail()
    {
        var order = await CheckoutDiscountedAsync();
        var statement = await GetTodayAsync();
        Assert.Contains(statement.AuditTrail, e => e.Source == "audit_event" && e.Action == "خصم يدوي على طلب");

        var auditScreen = await (await _client.GetAsync($"/api/audit/events?actorUserId={_managerId}&category=order"))
            .Content.ReadFromJsonAsync<List<Falah.Application.Audit.AuditEventDto>>();
        Assert.Contains(auditScreen!, e => e.Action == "خصم يدوي على طلب");
        _ = order;
    }

    private async Task<CheckoutOrderResult> CheckoutDiscountedAsync()
    {
        var productId = await _factory.SeedProductAsync(10000, "وجبة مخفضة");
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 1000, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 9000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    // ---- Unified audit trail (Section 10 - both AuditEvent and ControlAuditEventEntity, unmigrated) ----

    [Fact]
    public async Task Audit_trail_unions_both_the_general_log_and_the_control_center_log_without_merging_the_tables()
    {
        await CheckoutDiscountedAsync(); // writes to AuditEvents (category "order")
        await _client.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest("0000")); // a wrong PIN writes to ControlAuditEvents

        var statement = await GetTodayAsync();
        Assert.Contains(statement.AuditTrail, e => e.Source == "audit_event");
        Assert.Contains(statement.AuditTrail, e => e.Source == "control_audit_event" && e.Action == "pin_login_failed");

        // Neither underlying table was touched by reading the statement - still two separate tables.
        var generalCount = await _factory.QueryAsync(db => db.AuditEvents.CountAsync());
        var controlCount = await _factory.QueryAsync(db => db.ControlAuditEvents.CountAsync());
        Assert.True(generalCount > 0);
        Assert.True(controlCount > 0);
    }

    // ---- Waste's Cairo-timezone day-boundary fix ----

    /// <summary>Before this pass, GetLossSummaryAsync built its day window from
    /// businessDate.ToDateTime(TimeOnly.MinValue) treated as if it were already UTC - a waste
    /// movement in the 22:00-23:59 UTC band (which IS already the next Cairo day, since Cairo is
    /// UTC+2) was silently attributed to the wrong day. This seeds exactly such a movement at a
    /// fixed, deterministic instant (no dependency on the real clock) and proves it lands on the
    /// correct Cairo business date.</summary>
    [Fact]
    public async Task A_waste_movement_just_before_utc_midnight_is_attributed_to_the_correct_cairo_day()
    {
        var cairoDay = new DateOnly(2026, 3, 10);
        var lateUtcInstant = new DateTimeOffset(2026, 3, 9, 23, 30, 0, TimeSpan.Zero); // Cairo 2026-03-10 01:30

        var itemId = (await (await _client.PostAsJsonAsync("/api/inventory/items",
            new CreateInventoryItemRequest($"req-{Guid.NewGuid():N}", "بطاطس", "خامات", "kg", 0, false, "Purchase", null, _managerId)))
            .Content.ReadFromJsonAsync<CreateInventoryItemResult>())!.Id;

        await _factory.QueryAsync(async db =>
        {
            db.StockMovements.Add(new Falah.Infrastructure.Persistence.Entities.StockMovementEntity
            {
                IdempotencyKey = $"waste-{Guid.NewGuid():N}", InventoryItemId = itemId, Type = "Waste",
                Direction = "Out", QuantityMilliUnits = 1000, LossValuePiastres = 750,
                ReferenceType = "seed", ReferenceId = "seed", Reason = "اختبار الحد الزمني", CreatedBy = "seed",
                CreatedAtUtc = lateUtcInstant,
            });
            await db.SaveChangesAsync();
            return 0;
        });

        var summary = await (await _client.GetAsync($"/api/inventory/loss-summary?businessDate={cairoDay:yyyy-MM-dd}&actorUserId={_managerId}"))
            .Content.ReadFromJsonAsync<LossSummaryDto>();
        Assert.Equal(1, summary!.MovementCount);
        Assert.Equal(750, summary.TotalLossValuePiastres);

        // And it must NOT also (or instead) show up on the UTC-calendar day the old bug would have
        // used - the fix moves it, it does not duplicate it.
        var wrongDay = new DateOnly(2026, 3, 9);
        var wrongSummary = await (await _client.GetAsync($"/api/inventory/loss-summary?businessDate={wrongDay:yyyy-MM-dd}&actorUserId={_managerId}"))
            .Content.ReadFromJsonAsync<LossSummaryDto>();
        Assert.Equal(0, wrongSummary!.MovementCount);
    }

    // ---- Cairo business-day scoping of the endpoint itself ----

    [Fact]
    public async Task Daily_statement_for_a_day_with_no_activity_returns_a_real_empty_statement_not_an_error()
    {
        await CheckoutAsync("طلب اليوم", 5000); // activity exists today

        var lastWeek = Falah.Domain.Common.BusinessClock.Today().AddDays(-7);
        var response = await _client.GetAsync($"/api/reports/daily-statement?actorUserId={_managerId}&businessDate={lastWeek:yyyy-MM-dd}");
        response.EnsureSuccessStatusCode();
        var statement = await response.Content.ReadFromJsonAsync<DailyStatementDto>();

        Assert.Equal(lastWeek.ToString("yyyy-MM-dd"), statement!.BusinessDate);
        Assert.Equal(0, statement.Sales.OrderCount);
        Assert.Empty(statement.SoldProducts);
    }
}

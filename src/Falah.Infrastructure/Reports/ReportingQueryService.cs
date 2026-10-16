namespace Falah.Infrastructure.Reports;

using Falah.Application.Expenses;
using Falah.Application.Reports;
using Falah.Domain.Common;
using Falah.Domain.Delivery;
using Falah.Domain.Employees;
using Falah.Domain.Financial;
using Falah.Domain.Inventory;
using Falah.Domain.Orders;
using Falah.Domain.Reports;
using Falah.Infrastructure.Delivery;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Treasury;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Read-only reporting over already-persisted transactions - every figure here is derived at query
/// time from the same Orders/Shifts/Expenses/TreasuryMovements/Deliveries/StockMovements tables
/// every command handler in this codebase already writes to. Nothing is stored as a second,
/// separately-maintained "reporting" truth (no duplicate financial truth per screen - a report and
/// the ledger it summarizes can never silently disagree because they are the same rows, read two
/// ways). Business-day grouping resolves a Cairo calendar date to a UTC instant range via
/// BusinessClock.UtcRangeForBusinessDate, translated straight into SQL. Formulas are ported
/// verbatim from lib/reporting-domain.mjs/lib/control-layer-domain.mjs - see each method's own doc
/// comment for its source; nothing here invents a KPI the reference app did not already define.
/// </summary>
public sealed class ReportingQueryService
{
    private readonly FalahDbContext _db;
    public ReportingQueryService(FalahDbContext db) => _db = db;

    private async Task<List<OrderEntity>> LoadSalesOrdersAsync(BusinessDateRange? range, string? shiftId, CancellationToken ct)
    {
        var query = _db.Orders.AsNoTracking().Include(o => o.Items)
            .Where(o => o.Status != nameof(OrderStatus.Cancelled));
        if (range is { } period)
        {
            // Cairo midnight boundaries, so a month's total is exactly the sum of the days inside it.
            var (start, end) = period.ToUtcRange();
            query = query.Where(o => o.CreatedAtUtc >= start && o.CreatedAtUtc < end);
        }
        // Not "is not null": an empty-string shiftId (a query parameter present with no value, e.g.
        // "&shiftId=") must mean "no shift filter" just as an absent one does - a strict null check
        // instead turned it into "ShiftId == ''", which matches no order ever. This was the P0
        // reporting-consistency bug (headline zero, every other panel correct) - see ApiClient's own
        // fix for the client-side leak this guards against a future recurrence of.
        if (!string.IsNullOrEmpty(shiftId)) query = query.Where(o => o.ShiftId == shiftId);
        return await query.ToListAsync(ct);
    }

    /// <summary>
    /// Which of these orders is cash-on-delivery money the restaurant does not hold yet. An order
    /// qualifies when CheckoutRules itself classified it as cash-on-delivery (delivery + cash + a
    /// positive payable total - reused rather than re-expressed here, so this can never drift from
    /// what checkout actually decided) and no SETTLED driver-custody row exists for it.
    ///
    /// Settlement is the moment the money becomes the restaurant's: SettleDriverCustodyHandler posts
    /// the driver_settlement movement into tre-main. Before that the cash is with the customer
    /// (undelivered) or the driver (delivered, unsettled). A settlement shortage is deliberately not
    /// deducted again here - it is already reported as its own figure by the delivery outcomes
    /// report, and double-counting it would understate collected sales twice.
    /// </summary>
    private async Task<IReadOnlySet<string>> LoadOrderIdsWithCashOutsideRestaurantAsync(
        IReadOnlyList<Order> orders, CancellationToken ct)
    {
        var codOrderIds = orders
            .Where(o => CheckoutRules.IsCashOnDelivery(o.OrderType, o.PaymentMethod, o.Total))
            .Select(o => o.Id)
            .ToHashSet();
        if (codOrderIds.Count == 0) return codOrderIds;

        var settled = await _db.DriverCustodies.AsNoTracking()
            .Where(c => codOrderIds.Contains(c.OrderId) && c.Status == nameof(CustodyStatus.Settled))
            .Select(c => c.OrderId)
            .ToListAsync(ct);

        codOrderIds.ExceptWith(settled);
        return codOrderIds;
    }

    /// <summary>Ported from selectSalesReport in lib/reporting-domain.mjs.</summary>
    public Task<SalesReportDto> GetSalesReportAsync(DateOnly? businessDate, string? shiftId, CancellationToken ct = default) =>
        GetSalesReportAsync(businessDate is { } d ? BusinessDateRange.SingleDay(d) : null, shiftId, ct);

    public async Task<SalesReportDto> GetSalesReportAsync(BusinessDateRange? range, string? shiftId, CancellationToken ct = default)
    {
        var entities = await LoadSalesOrdersAsync(range, shiftId, ct);
        var orders = entities.Select(OrderMapper.ToDomain).ToList();
        var report = SalesRules.ComputeSalesReport(orders, await LoadOrderIdsWithCashOutsideRestaurantAsync(orders, ct));
        return ToDto(report);
    }

    /// <summary>Ported from TaxScreen.tsx's own report - reads only orders' frozen TaxAmount/
    /// TaxableBase (see SalesRules.ComputeTaxReport's own doc comment for why this must never
    /// recompute from today's live tax rule). A real gap found during the legacy-parity audit: no
    /// C# endpoint existed for this at all.</summary>
    public Task<TaxReportDto> GetTaxReportAsync(DateOnly? businessDate, string? shiftId, CancellationToken ct = default) =>
        GetTaxReportAsync(businessDate is { } d ? BusinessDateRange.SingleDay(d) : null, shiftId, ct);

    public async Task<TaxReportDto> GetTaxReportAsync(BusinessDateRange? range, string? shiftId, CancellationToken ct = default)
    {
        var entities = await LoadSalesOrdersAsync(range, shiftId, ct);
        var orders = entities.Select(OrderMapper.ToDomain).ToList();
        var report = SalesRules.ComputeTaxReport(orders);
        return new TaxReportDto(report.Gross.Piastres, report.Taxable.Piastres, report.Tax.Piastres,
            report.RefundTaxImpact.Piastres, report.NetTax.Piastres);
    }

    private static SalesReportDto ToDto(SalesRules.SalesReport r) => new(
        r.OrderCount, r.GrossSales.Piastres, r.Refunds.Piastres, r.NetSales.Piastres, r.ListSales.Piastres,
        r.OffersValue.Piastres, r.CompsValue.Piastres, r.ManualDiscounts.Piastres, r.PendingElectronicSales.Piastres,
        r.CashWithDrivers.Piastres, r.CollectedSales.Piastres, r.AverageOrderValue.Piastres, r.Cash.Piastres, r.Card.Piastres, r.InstaPay.Piastres,
        r.Wallet.Piastres, r.Pickup.Piastres, r.Takeaway.Piastres, r.Delivery.Piastres);

    /// <summary>Ported from selectExpensesReport - a plain total + list, no cashier/manager
    /// visibility split (that rule belongs to ExpenseQueryService for the operational drawer view;
    /// this is the management-report total across everyone, gated on reports_view at the endpoint).</summary>
    public Task<ExpenseReportDto> GetExpenseReportAsync(DateOnly? businessDate, string? shiftId, CancellationToken ct = default) =>
        GetExpenseReportAsync(businessDate is { } d ? BusinessDateRange.SingleDay(d) : null, shiftId, ct);

    public async Task<ExpenseReportDto> GetExpenseReportAsync(BusinessDateRange? range, string? shiftId, CancellationToken ct = default)
    {
        var query = _db.Expenses.AsNoTracking().AsQueryable();
        // Expenses carry their own business date, so they are filtered on it directly rather than
        // through the UTC instant range the timestamped tables need.
        if (range is { } period) query = query.Where(e => e.BusinessDate >= period.From && e.BusinessDate <= period.To);
        if (!string.IsNullOrEmpty(shiftId)) query = query.Where(e => e.ShiftId == shiftId);

        var rows = await query.OrderByDescending(e => e.CreatedAtUtc).ToListAsync(ct);
        var dtos = rows.Select(e => new ExpenseDto(e.Id, e.Category, e.AmountPiastres, e.Description, e.ShiftId,
            e.PaidFromDrawer, e.PaymentAccountId, e.CreatedByUserId, e.BusinessDate.ToString("yyyy-MM-dd"), e.CreatedAtUtc)).ToList();
        return new ExpenseReportDto(rows.Sum(e => e.AmountPiastres), dtos);
    }

    /// <summary>Ported from selectRefundActivity - every refund across every order, with the
    /// original sale's own shift alongside the shift that actually processed the refund (a closed
    /// shift's refund is processed under the CURRENT shift, per RefundOrderHandler).</summary>
    public Task<List<RefundActivityDto>> GetRefundActivityAsync(DateOnly? businessDate, string? shiftId, CancellationToken ct = default) =>
        GetRefundActivityAsync(businessDate is { } d ? BusinessDateRange.SingleDay(d) : null, shiftId, ct);

    public async Task<List<RefundActivityDto>> GetRefundActivityAsync(BusinessDateRange? range, string? shiftId, CancellationToken ct = default)
    {
        DateTimeOffset? start = null, end = null;
        if (range is { } period) (start, end) = period.ToUtcRange();

        var query = _db.OrderRefunds.AsNoTracking()
            .Where(r => (string.IsNullOrEmpty(shiftId) || r.OperationalShiftId == shiftId)
                && (start == null || r.CreatedAtUtc >= start) && (end == null || r.CreatedAtUtc < end))
            .Join(_db.Orders.AsNoTracking(), r => r.OrderId, o => o.Id, (r, o) => new { Refund = r, o.OrderNumber });

        var rows = await query.ToListAsync(ct);
        return rows.Select(x => new RefundActivityDto(x.Refund.OrderId, x.OrderNumber, x.Refund.AmountPiastres,
            x.Refund.PaymentMethod, x.Refund.Reason, x.Refund.CreatedBy, x.Refund.AuthorizedBy, x.Refund.CreatedAtUtc,
            x.Refund.OriginalShiftId, x.Refund.OperationalShiftId)).ToList();
    }

    /// <summary>The read-only equivalent of reconcileBusinessDay in lib/control-layer-domain.mjs -
    /// "where is the money" for one business day. Deliberately excludes the reference app's
    /// purchasing/payroll sub-sections: neither Suppliers/Purchases nor Payroll exist in this system
    /// yet (see IMPLEMENTATION_MATRIX.md rows 8/9's own deferral notes).</summary>
    public async Task<BusinessDayReconciliationDto> GetBusinessDayReconciliationAsync(string businessDayId, CancellationToken ct = default)
    {
        var day = await _db.BusinessDays.AsNoTracking().FirstOrDefaultAsync(d => d.Id == businessDayId, ct)
            ?? throw new InvalidOperationException("يوم العمل غير موجود.");

        var orderEntities = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .Where(o => o.Status != nameof(OrderStatus.Cancelled) && o.BusinessDayId == businessDayId).ToListAsync(ct);
        var domainOrders = orderEntities.Select(OrderMapper.ToDomain).ToList();
        var sales = SalesRules.ComputeSalesReport(
            domainOrders, await LoadOrderIdsWithCashOutsideRestaurantAsync(domainOrders, ct));

        var expensesTotal = await _db.Expenses.AsNoTracking().Where(e => e.BusinessDayId == businessDayId).SumAsync(e => (long?)e.AmountPiastres, ct) ?? 0;

        var movements = await _db.TreasuryMovements.AsNoTracking()
            .Where(m => m.BusinessDayId == businessDayId && m.Status == nameof(MovementStatus.Posted)).ToListAsync(ct);
        long Balance(string accountId) => movements.Sum(m => (m.DestinationAccountId == accountId ? m.AmountPiastres : 0) - (m.SourceAccountId == accountId ? m.AmountPiastres : 0));
        var treasury = new TreasuryBalancesDto(Balance(TreasuryAccounts.Main), Balance(TreasuryAccounts.Drawer),
            Balance(TreasuryAccounts.Bank), Balance(TreasuryAccounts.InstaPay), Balance(TreasuryAccounts.Wallet));

        var dayShifts = await _db.Shifts.AsNoTracking().Where(s => s.BusinessDate == day.BusinessDate).ToListAsync(ct);
        var shiftsExpected = dayShifts.Sum(s => s.ExpectedCashPiastres ?? 0);
        var shiftsActual = dayShifts.Sum(s => s.ActualCashPiastres ?? 0);
        var shortage = dayShifts.Where(s => (s.CashDifferencePiastres ?? 0) < 0).Sum(s => Math.Abs(s.CashDifferencePiastres ?? 0));
        var overage = dayShifts.Where(s => (s.CashDifferencePiastres ?? 0) > 0).Sum(s => s.CashDifferencePiastres ?? 0);

        // Only cash actually collected by a driver is restaurant money awaiting settlement - a
        // failed/returned order's original amount never creates a fictitious cash shortage here.
        var dayOrderIds = orderEntities.Select(o => o.Id).ToHashSet();
        var unsettledCustodies = await _db.DriverCustodies.AsNoTracking()
            .Where(c => c.Status != nameof(CustodyStatus.Settled)).ToListAsync(ct);
        var expectedDriverCash = unsettledCustodies.Where(c => dayOrderIds.Contains(c.OrderId)).Sum(c => c.CollectedPiastres);
        var settledDriverCash = await _db.TreasuryMovements.AsNoTracking()
            .Where(m => m.BusinessDayId == businessDayId && m.Type == "driver_settlement" && m.Status == nameof(MovementStatus.Posted))
            .SumAsync(m => (long?)m.AmountPiastres, ct) ?? 0;

        // Ported from reconcileBusinessDay's purchasing.paidPiastres in lib/control-layer-domain.mjs
        // - a real gap found during the legacy-parity audit. Sourced from the same posted-movements
        // list already loaded above (Type "purchase_payment", posted by both ReceivePurchaseHandler's
        // pay-at-receipt leg and PayPurchaseHandler's later settlements).
        var purchasingPaid = movements.Where(m => m.Type == "purchase_payment").Sum(m => m.AmountPiastres);

        return new BusinessDayReconciliationDto(day.Id, day.BusinessDate.ToString("yyyy-MM-dd"), ToDto(sales), expensesTotal,
            treasury, expectedDriverCash, settledDriverCash, dayShifts.Count, shiftsExpected, shiftsActual, shortage, overage,
            purchasingPaid);
    }

    /// <summary>Ported from DailySummaryScreen.tsx - reuses GetBusinessDayReconciliationAsync for
    /// sales/expenses/shift-discrepancy rather than re-deriving them, and adds only the employee-
    /// ledger totals and cashier names that reconciliation doesn't carry. EmployeeLedgerEntryEntity
    /// has no BusinessDayId of its own, so entries are scoped by the same Cairo-midnight UTC range
    /// every other CreatedAtUtc-only table in this file already uses (see Range/UtcRangeForBusinessDate).</summary>
    public async Task<DailySummaryDto> GetDailySummaryAsync(string businessDayId, CancellationToken ct = default)
    {
        var reconciliation = await GetBusinessDayReconciliationAsync(businessDayId, ct);
        var day = await _db.BusinessDays.AsNoTracking().FirstOrDefaultAsync(d => d.Id == businessDayId, ct)
            ?? throw new InvalidOperationException("يوم العمل غير موجود.");

        var (start, end) = BusinessClock.UtcRangeForBusinessDate(day.BusinessDate);
        var ledgerEntries = await _db.EmployeeLedgerEntries.AsNoTracking()
            .Where(l => l.CreatedAtUtc >= start && l.CreatedAtUtc < end).ToListAsync(ct);
        long SumType(string type, bool credit) => ledgerEntries.Where(l => l.Type == type)
            .Sum(l => credit ? l.CreditPiastres : l.DebitPiastres);
        var employeeLedger = new EmployeeLedgerTotalsDto(
            SumType(nameof(LedgerEntryType.DailyWage), credit: true),
            SumType(nameof(LedgerEntryType.Overtime), credit: true),
            SumType(nameof(LedgerEntryType.Advance), credit: false),
            SumType(nameof(LedgerEntryType.Withdrawal), credit: false));

        var dayShifts = await _db.Shifts.AsNoTracking().Where(s => s.BusinessDate == day.BusinessDate).ToListAsync(ct);
        var cashierUserIds = dayShifts.Select(s => s.OpenedByUserId).Distinct();
        var cashierNames = await _db.Users.AsNoTracking().Where(u => cashierUserIds.Contains(u.Id))
            .Select(u => u.Name).Distinct().ToListAsync(ct);

        return new DailySummaryDto(day.Id, day.BusinessDate.ToString("yyyy-MM-dd"), reconciliation.Sales, reconciliation.ExpensesTotalPiastres,
            reconciliation.ShiftsShortagePiastres, reconciliation.ShiftsOveragePiastres, employeeLedger, cashierNames);
    }

    private (DateTimeOffset? Start, DateTimeOffset? End) Range(DateOnly? from, DateOnly? to)
    {
        DateTimeOffset? start = from is { } f ? BusinessClock.UtcRangeForBusinessDate(f).Start : null;
        DateTimeOffset? end = to is { } t ? BusinessClock.UtcRangeForBusinessDate(t).End : null;
        return (start, end);
    }

    /// <summary>Ported from the Top Selling Items panel in ReportsScreen.tsx - grouped by product
    /// NAME (the reference app's own key), not id, so variants of the same product name roll up
    /// together exactly as the original screen shows them.</summary>
    public async Task<List<TopSellingItemDto>> GetTopSellingItemsAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var (start, end) = Range(from, to);
        var rows = await _db.OrderItems.AsNoTracking()
            .Join(_db.Orders.AsNoTracking().Where(o => o.Status != nameof(OrderStatus.Cancelled)
                    && (start == null || o.CreatedAtUtc >= start) && (end == null || o.CreatedAtUtc < end)),
                i => i.OrderId, o => o.Id, (i, o) => new { i.ProductName, i.Quantity, i.TotalPricePiastres })
            .ToListAsync(ct);

        return rows.GroupBy(r => r.ProductName)
            .Select(g => new TopSellingItemDto(g.Key, g.Sum(r => r.Quantity), g.Sum(r => r.TotalPricePiastres)))
            .OrderByDescending(d => d.Quantity).ToList();
    }

    /// <summary>Every product sold in range, not just the top N - GetTopSellingItemsAsync's own rows
    /// widened with category and order-count, for the daily statement's "what did we actually make
    /// today" section. Same product-NAME grouping key as GetTopSellingItemsAsync/
    /// GetProductHourlySalesAsync/GetCategorySalesAsync, so none of the four can ever disagree about
    /// what one "product" is. Category comes from whichever order line's product still resolves to
    /// one (a product can be recategorized or removed after the sale; the line itself never lies
    /// about what sold).</summary>
    public async Task<List<SoldProductLineDto>> GetSoldProductsAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var (start, end) = Range(from, to);
        var rows = await _db.OrderItems.AsNoTracking()
            .Join(_db.Orders.AsNoTracking().Where(o => o.Status != nameof(OrderStatus.Cancelled)
                    && (start == null || o.CreatedAtUtc >= start) && (end == null || o.CreatedAtUtc < end)),
                i => i.OrderId, o => o.Id,
                (i, o) => new { i.OrderId, i.ProductId, i.ProductName, i.Quantity, i.TotalPricePiastres, o.CreatedAtUtc })
            .ToListAsync(ct);

        var productCategoryIds = await _db.Products.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.CategoryId, ct);
        var categoryNames = await _db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        return rows.GroupBy(r => r.ProductName)
            .Select(g =>
            {
                var categoryId = g.Select(r => productCategoryIds.GetValueOrDefault(r.ProductId)).FirstOrDefault(c => c is not null);
                var peakHour = g.GroupBy(r => TimeZoneInfo.ConvertTime(r.CreatedAtUtc, BusinessClock.CairoTimeZone).Hour)
                    .OrderByDescending(h => h.Sum(x => x.Quantity)).First().Key;
                return new SoldProductLineDto(
                    g.Key, categoryId, categoryId is null ? null : categoryNames.GetValueOrDefault(categoryId, categoryId),
                    g.Sum(r => r.Quantity), g.Select(r => r.OrderId).Distinct().Count(), g.Sum(r => r.TotalPricePiastres), peakHour);
            })
            .OrderByDescending(d => d.TotalPiastres)
            .ToList();
    }

    /// <summary>What sold, and at what hour of the day - the same rows GetTopSellingItemsAsync
    /// returns, split by the hour on a Cairo wall clock rather than rolled into a single total.
    /// Grouped by product NAME for the same reason that one is: variants of a product roll up
    /// together, matching what the Top Selling panel above already shows.
    /// <para>The hour is computed after materialising, not in SQL: the conversion is a time-zone
    /// rule (DST included), and the dashboard's own hourly trend buckets the identical way. Sending
    /// only the hours that sold keeps this proportional to what happened rather than to
    /// products x 24.</para></summary>
    public async Task<List<ProductHourlySalesDto>> GetProductHourlySalesAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var (start, end) = Range(from, to);
        var rows = await _db.OrderItems.AsNoTracking()
            .Join(_db.Orders.AsNoTracking().Where(o => o.Status != nameof(OrderStatus.Cancelled)
                    && (start == null || o.CreatedAtUtc >= start) && (end == null || o.CreatedAtUtc < end)),
                i => i.OrderId, o => o.Id,
                (i, o) => new { i.ProductName, i.Quantity, i.TotalPricePiastres, o.CreatedAtUtc })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => new { r.ProductName, Hour = TimeZoneInfo.ConvertTime(r.CreatedAtUtc, BusinessClock.CairoTimeZone).Hour })
            .Select(g => new ProductHourlySalesDto(g.Key.ProductName, g.Key.Hour, g.Sum(r => r.Quantity), g.Sum(r => r.TotalPricePiastres)))
            .OrderBy(d => d.ProductName).ThenBy(d => d.Hour)
            .ToList();
    }

    /// <summary>Ported from the Sales by Category panel in ReportsScreen.tsx.</summary>
    public async Task<List<CategorySalesDto>> GetCategorySalesAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var (start, end) = Range(from, to);
        var rows = await _db.OrderItems.AsNoTracking()
            .Join(_db.Orders.AsNoTracking().Where(o => o.Status != nameof(OrderStatus.Cancelled)
                    && (start == null || o.CreatedAtUtc >= start) && (end == null || o.CreatedAtUtc < end)),
                i => i.OrderId, o => o.Id, (i, _) => i)
            .Join(_db.Products.AsNoTracking(), i => i.ProductId, p => p.Id, (i, p) => new { p.CategoryId, i.Quantity, i.TotalPricePiastres })
            .ToListAsync(ct);

        var categories = await _db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        return rows.GroupBy(r => r.CategoryId)
            .Select(g => new CategorySalesDto(g.Key, categories.GetValueOrDefault(g.Key, g.Key), g.Sum(r => r.Quantity), g.Sum(r => r.TotalPricePiastres)))
            .OrderByDescending(d => d.TotalPiastres).ToList();
    }

    /// <summary>Ported from ReportsScreen.tsx's own totalFoodCost, adapted to how this system
    /// actually costs stock: usage movements here never carry a unit cost (only purchase-receipt/
    /// loss movements do - a deliberate design from the Inventory slice), so cost is instead
    /// resolved the same way CostingRules already resolves a recipe's cost for display - at the
    /// ingredient's CURRENT price. Same accepted limitation as theoreticalIngredientUsage: uses
    /// today's recipe/cost for old sales, never historically versioned. "Unknown cost is not zero" -
    /// an item whose recipe has ANY incomplete-cost line is named in Missing, never folded into the
    /// total as a silent zero.</summary>
    public async Task<FoodCostReportDto> GetFoodCostReportAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var (start, end) = Range(from, to);
        var soldLines = await _db.OrderItems.AsNoTracking()
            .Join(_db.Orders.AsNoTracking().Where(o => o.Status != nameof(OrderStatus.Cancelled)
                    && (start == null || o.CreatedAtUtc >= start) && (end == null || o.CreatedAtUtc < end)),
                i => i.OrderId, o => o.Id, (i, _) => new { i.ProductId, i.VariantId, i.ProductName, i.Quantity })
            .ToListAsync(ct);

        // Order-level packaging - the delivery bag - is consumed once per qualifying order, not per
        // item, so it is counted here rather than inside the per-line loop. It used to be counted
        // nowhere at all: the rule could be configured and no report ever read it.
        var soldOrderTypes = await _db.Orders.AsNoTracking()
            .Where(o => o.Status != nameof(OrderStatus.Cancelled)
                && (start == null || o.CreatedAtUtc >= start) && (end == null || o.CreatedAtUtc < end))
            .GroupBy(o => o.OrderType)
            .Select(g => new { OrderType = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var packagingRules = await _db.OrderPackagingRules.AsNoTracking()
            .Select(r => new OrderPackagingRule(r.OrderType, r.InventoryItemId, r.QuantityMilliUnits, r.Active))
            .ToListAsync(ct);

        var productRecipeIds = await _db.Products.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.RecipeId, ct);
        var variantRecipeIds = await _db.ProductVariants.AsNoTracking().ToDictionaryAsync(v => v.Id, v => v.RecipeId, ct);
        var recipes = await _db.Recipes.AsNoTracking().Include(r => r.Items).ToDictionaryAsync(r => r.Id, ct);

        var items = await _db.InventoryItems.AsNoTracking()
            .Select(i => new InventoryItem(i.Id, i.Name, Enum.Parse<CostSource>(i.CostSource), i.EstimatedUnitCostPiastres)).ToListAsync(ct);
        var preps = await _db.PrepRecipes.AsNoTracking().Select(p => new PrepRecipe(p.Id, p.OutputInventoryItemId, p.OutputQuantityMilliUnits, p.Active)).ToListAsync(ct);
        var prepLines = await _db.PrepRecipeLines.AsNoTracking().Select(l => new PrepRecipeLine(l.PrepRecipeId, l.InventoryItemId, l.QuantityMilliUnits)).ToListAsync(ct);
        var purchaseMovements = await _db.StockMovements.AsNoTracking()
            .Where(m => m.Type == nameof(StockMovementType.PurchaseReceipt) && m.UnitCostPiastres != null)
            .Select(m => new { m.InventoryItemId, m.UnitCostPiastres, m.CreatedAtUtc }).ToListAsync(ct);
        var movementFacts = purchaseMovements.Select(m => new StockMovementFacts(m.InventoryItemId, StockMovementType.PurchaseReceipt, Money.FromPiastres(m.UnitCostPiastres!.Value), m.CreatedAtUtc)).ToList();
        var costingCtx = new CostingContext(items, preps, prepLines, movementFacts);

        long totalCost = 0;
        var missing = new HashSet<string>();
        int knownLines = 0, totalLines = 0;
        foreach (var line in soldLines)
        {
            // Every sold line counts, priced or not. Skipping the unpriced ones before this made a
            // restaurant that had costed nothing report zero sold lines - which reads as complete
            // coverage of an empty menu, and therefore as a hundred percent margin. An item with no
            // recipe is not absent from the report; its cost is unknown, exactly like an item whose
            // recipe has an unpriced ingredient below.
            totalLines++;

            var recipeId = (line.VariantId is not null && variantRecipeIds.TryGetValue(line.VariantId, out var vr) && vr is not null) ? vr : productRecipeIds.GetValueOrDefault(line.ProductId);
            if (recipeId is null || !recipes.TryGetValue(recipeId, out var recipe))
            {
                missing.Add(line.ProductName);
                continue;
            }

            var recipeLines = recipe.Items.Select(i => new RecipeLine(i.InventoryItemId, i.QuantityMilliUnits, Enum.Parse<RecipeLineKind>(i.Kind), i.YieldUnitsPerBaseUnit)).ToList();
            var cost = CostingRules.ComputeRecipeCost(recipeLines, costingCtx);
            // Not "is anything missing" - a recipe with no lines is missing nothing and knows
            // nothing. IsComplete asks the question that was meant: is this the whole cost?
            if (!cost.IsComplete) { missing.Add(line.ProductName); continue; }
            knownLines++;
            totalCost += cost.KnownCost.Piastres * line.Quantity;
        }

        foreach (var orderType in soldOrderTypes)
        {
            var (cost, known, name) = CostingRules.OrderPackagingCost(orderType.OrderType, packagingRules, costingCtx);
            // The same rule as everywhere else: packaging whose cost is unknown is named, never
            // added as a zero that makes the total look complete.
            if (!known) { missing.Add(name ?? orderType.OrderType); continue; }
            totalCost += cost.Piastres * orderType.Count;
        }

        return new FoodCostReportDto(totalCost, totalLines, knownLines, missing.ToList());
    }

    /// <summary>One row per manager-authorized or otherwise noteworthy action, unioned from every
    /// table that already records it on itself - never a second, separately-maintained audit log.
    /// "Who performed/approved important actions" for a period.</summary>
    public async Task<List<SignificantActionDto>> GetSignificantActionsAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var (start, end) = Range(from, to);
        var actions = new List<SignificantActionDto>();

        var treasuryRows = await _db.TreasuryMovements.AsNoTracking()
            .Where(m => m.ApprovedBy != null && (start == null || m.CreatedAtUtc >= start) && (end == null || m.CreatedAtUtc < end))
            .ToListAsync(ct);
        actions.AddRange(treasuryRows.Select(m =>
            new SignificantActionDto("treasury", m.Type, m.AmountPiastres, m.CreatedBy, m.ApprovedBy, m.Reason, m.CreatedAtUtc, m.ReferenceId)));

        var expenseRows = await _db.Expenses.AsNoTracking()
            .Where(e => e.ApprovedBy != null && (start == null || e.CreatedAtUtc >= start) && (end == null || e.CreatedAtUtc < end))
            .ToListAsync(ct);
        // Expenses store the creator's user id, not their name like the other sources here, so it is
        // resolved - otherwise this report printed "user-demo-cashier" where every other row names a person.
        var expenseCreatorIds = expenseRows.Select(e => e.CreatedByUserId).Distinct().ToList();
        var creatorNames = await _db.Users.AsNoTracking().Where(u => expenseCreatorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, ct);
        actions.AddRange(expenseRows.Select(e =>
            new SignificantActionDto("expense", e.Category, e.AmountPiastres,
                creatorNames.GetValueOrDefault(e.CreatedByUserId, e.CreatedByUserId), e.ApprovedBy, e.Description, e.CreatedAtUtc, e.Id)));

        var stockRows = await _db.StockMovements.AsNoTracking()
            .Where(m => m.ApprovedBy != null && (start == null || m.CreatedAtUtc >= start) && (end == null || m.CreatedAtUtc < end))
            .ToListAsync(ct);
        actions.AddRange(stockRows.Select(m =>
            new SignificantActionDto("inventory", m.Type, m.LossValuePiastres, m.CreatedBy, m.ApprovedBy, m.Reason, m.CreatedAtUtc, m.InventoryItemId)));

        var deliveryRows = await _db.DeliveryEvents.AsNoTracking()
            .Where(e => e.Approver != null && (start == null || e.CreatedAtUtc >= start) && (end == null || e.CreatedAtUtc < end))
            .ToListAsync(ct);
        actions.AddRange(deliveryRows.Select(e =>
            new SignificantActionDto("delivery", e.Type, e.AmountPiastres, e.Actor, e.Approver, e.Reason, e.CreatedAtUtc, e.DeliveryId)));

        return actions.OrderByDescending(a => a.CreatedAtUtc).ToList();
    }

    /// <summary>Ported from the per-shift figures in DailySummaryScreen.tsx/ShiftsScreen.tsx - a
    /// cashier's own sales + expenses + discrepancy for their shift. Payroll-derived figures the
    /// reference app also shows here (daily wages, advances, overtime) are deliberately omitted -
    /// Payroll does not exist in this system yet.</summary>
    public async Task<ShiftSummaryDto> GetShiftSummaryAsync(string shiftId, CancellationToken ct = default)
    {
        var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == shiftId, ct)
            ?? throw new InvalidOperationException("الوردية غير موجودة.");
        var userIds = new[] { shift.OpenedByUserId, shift.ClosedByUserId }.Where(id => id != null).Cast<string>().Distinct();
        var userNames = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, ct);
        return await BuildShiftSummaryAsync(shift, userNames, ct);
    }

    /// <summary>The Shifts screen's own history table - every shift's operational record plus the
    /// same sales/expense figures GetShiftSummaryAsync computes for one shift, for as many shifts as
    /// this restaurant is realistically read against at once. Capped at the most recent
    /// <paramref name="take"/>: nothing bounds this table in the reference (a client-side array that
    /// simply grows), but that reference is a single-tenant local-storage demo - a real restaurant
    /// running for months must not have every shift it ever worked re-summed from Orders on every
    /// screen open. The cap is a safeguard against that, not a feature the reference has an opinion
    /// on either way.</summary>
    public async Task<List<ShiftSummaryDto>> GetShiftsHistoryAsync(int take = 90, CancellationToken ct = default)
    {
        var shifts = await _db.Shifts.AsNoTracking()
            .OrderByDescending(s => s.OpenedAtUtc)
            .Take(take)
            .ToListAsync(ct);

        var userIds = shifts.Select(s => s.OpenedByUserId)
            .Concat(shifts.Select(s => s.ClosedByUserId).Where(id => id != null).Cast<string>())
            .Distinct();
        var userNames = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        var result = new List<ShiftSummaryDto>(shifts.Count);
        foreach (var shift in shifts)
            result.Add(await BuildShiftSummaryAsync(shift, userNames, ct));
        return result;
    }

    /// <summary>Every shift opened on one Cairo business date, for the daily statement's Section 5
    /// (per-shift rows, never netted into one figure). Same BuildShiftSummaryAsync every other shift
    /// read already uses, filtered by ShiftEntity.BusinessDate directly rather than a UTC instant
    /// range - shifts already carry their own Cairo business date, exactly like Expenses.</summary>
    public async Task<List<ShiftSummaryDto>> GetShiftsForBusinessDateAsync(DateOnly businessDate, CancellationToken ct = default)
    {
        var shifts = await _db.Shifts.AsNoTracking()
            .Where(s => s.BusinessDate == businessDate)
            .OrderBy(s => s.OpenedAtUtc)
            .ToListAsync(ct);

        var userIds = shifts.Select(s => s.OpenedByUserId)
            .Concat(shifts.Select(s => s.ClosedByUserId).Where(id => id != null).Cast<string>())
            .Distinct();
        var userNames = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        var result = new List<ShiftSummaryDto>(shifts.Count);
        foreach (var shift in shifts)
            result.Add(await BuildShiftSummaryAsync(shift, userNames, ct));
        return result;
    }

    private async Task<ShiftSummaryDto> BuildShiftSummaryAsync(
        ShiftEntity shift, IReadOnlyDictionary<string, string> userNames, CancellationToken ct)
    {
        var orderEntities = await LoadSalesOrdersAsync(null, shift.Id, ct);
        var domainOrders = orderEntities.Select(OrderMapper.ToDomain).ToList();
        var sales = SalesRules.ComputeSalesReport(
            domainOrders, await LoadOrderIdsWithCashOutsideRestaurantAsync(domainOrders, ct));
        var expensesTotal = await _db.Expenses.AsNoTracking().Where(e => e.ShiftId == shift.Id).SumAsync(e => (long?)e.AmountPiastres, ct) ?? 0;

        return new ShiftSummaryDto(shift.Id, shift.ShiftName, shift.OpenedByUserId, shift.Status, shift.OpeningCashPiastres,
            shift.ExpectedCashPiastres, shift.ActualCashPiastres, shift.CashDifferencePiastres, ToDto(sales), expensesTotal,
            userNames.GetValueOrDefault(shift.OpenedByUserId),
            shift.ClosedByUserId is { } closedBy ? userNames.GetValueOrDefault(closedBy) : null,
            shift.OpenedAtUtc, shift.ClosedAtUtc, shift.DifferenceReason);
    }

    /// <summary>Delivery outcome counts + unsettled driver custody for a period - "delivery
    /// outcomes" and "driver custody" from real persisted Delivery/DriverCustody rows.</summary>
    public async Task<DeliveryOutcomesReportDto> GetDeliveryOutcomesReportAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var (start, end) = Range(from, to);
        var deliveredIds = await _db.Deliveries.AsNoTracking()
            .Where(d => d.DeliveredAtUtc != null && (start == null || d.DeliveredAtUtc >= start) && (end == null || d.DeliveredAtUtc < end))
            .Select(d => d.OrderId).ToListAsync(ct);
        var failedCount = await _db.Deliveries.AsNoTracking()
            .CountAsync(d => d.FailedAtUtc != null && (start == null || d.FailedAtUtc >= start) && (end == null || d.FailedAtUtc < end), ct);
        var returnedCount = await _db.Deliveries.AsNoTracking()
            .CountAsync(d => d.ReturnedAtUtc != null && (start == null || d.ReturnedAtUtc >= start) && (end == null || d.ReturnedAtUtc < end), ct);

        var deliveryFees = deliveredIds.Count == 0 ? 0 :
            await _db.Orders.AsNoTracking().Where(o => deliveredIds.Contains(o.Id)).SumAsync(o => (long?)o.DeliveryFeePiastres, ct) ?? 0;

        var unsettled = await _db.DriverCustodies.AsNoTracking().Where(c => c.Status != nameof(CustodyStatus.Settled)).ToListAsync(ct);

        // Ported from deliveryReport's own averageDeliveryMinutes: only deliveries with BOTH
        // timestamps count, and null (not zero) when none qualify.
        var durationsMinutes = await _db.Deliveries.AsNoTracking()
            .Where(d => d.OutForDeliveryAtUtc != null && d.DeliveredAtUtc != null
                && (start == null || d.DeliveredAtUtc >= start) && (end == null || d.DeliveredAtUtc < end))
            .Select(d => new { d.OutForDeliveryAtUtc, d.DeliveredAtUtc })
            .ToListAsync(ct);
        var validDurations = durationsMinutes
            .Select(d => (d.DeliveredAtUtc!.Value - d.OutForDeliveryAtUtc!.Value).TotalMinutes)
            .Where(m => m >= 0).ToList();
        long? averageDeliveryMinutes = validDurations.Count == 0 ? null : (long)Math.Round(validDurations.Average());

        var settlements = await _db.DriverSettlements.AsNoTracking()
            .Where(s => start == null || s.CreatedAtUtc >= start).Where(s => end == null || s.CreatedAtUtc < end)
            .Select(s => new { s.ActualPiastres, s.DifferencePiastres }).ToListAsync(ct);

        return new DeliveryOutcomesReportDto(
            deliveredIds.Count, failedCount, returnedCount, deliveryFees,
            unsettled.Count, unsettled.Sum(c => c.CollectedPiastres),
            averageDeliveryMinutes,
            settlements.Sum(s => s.ActualPiastres),
            settlements.Sum(s => Math.Max(0, -s.DifferencePiastres)),
            settlements.Sum(s => Math.Max(0, s.DifferencePiastres)),
            settlements.Count);
    }
}

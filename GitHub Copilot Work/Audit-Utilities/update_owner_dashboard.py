# -*- coding: utf-8 -*-
import os
import re

review_path = r"c:\Users\Mohamed\Desktop\Falah-Restaurant-CSharp\GitHub Copilot Work\02-Callable-Inventory-and-Reviews\File-Reviews\src\Falah.Infrastructure\Reports\OwnerDashboardQueryService.cs.md"

with open(review_path, 'r', encoding='utf-8') as f:
    content = f.read()

replacements = {
    # 1. Constructor
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.OwnerDashboardQueryService\(Falah\.Infrastructure\.Persistence\.FalahDbContext, Falah\.Infrastructure\.Reports\.ReportingQueryService\) across lines 32-36\.":
        "- **Behavior**: Initializes the query service by binding the EF Core `FalahDbContext` instance and `ReportingQueryService` domain reporter dependency. Maintains zero mutable state and acts as an executive composition facade.\n"
        "  ```csharp\n"
        "  // Lines 32-36: Constructor initialization\n"
        "  public OwnerDashboardQueryService(FalahDbContext db, ReportingQueryService reports)\n"
        "  {\n"
        "      _db = db;\n"
        "      _reports = reports;\n"
        "  }\n"
        "  ```",

    # 2. GetAsync
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.GetAsync\(Falah\.Domain\.Common\.BusinessDateRange, CancellationToken\) across lines 38-71\.":
        "- **Behavior**: Top-level composition orchestrator for the Executive Owner Dashboard. Aggregates five foundational reports from `ReportingQueryService` (`SalesReportDto`, `ExpenseReportDto`, `RefundActivityDto`, `FoodCostReportDto`, and significant actions count). Converts the requested `BusinessDateRange` to UTC bounds via `range.ToUtcRange()`. Awaits `CurrentTreasuryAsync(ct)` and `LoadSettledFromDriversAsync(start, end, ct)` to populate `CashPositionDto`. Assembles operational leakage via `LoadLeakageAsync`, calculates labor liability via `LoadWagesPostedAsync`, and invokes pure projection `BuildProfitability`. In parallel, loads operational alerts (`LoadAttentionAsync`), low-stock valuation (`LoadStockAsync`), outstanding obligations (`LoadObligationsAsync`), workforce attendance (`LoadPeopleAsync`), system backup and print queue health (`LoadHealthAsync`), and period comparative trends (`LoadTrendAsync`).\n"
        "  ```csharp\n"
        "  // Lines 38-71: Master dashboard query composition\n"
        "  public async Task<OwnerDashboardDto> GetAsync(BusinessDateRange range, CancellationToken ct = default)\n"
        "  {\n"
        "      var sales = await _reports.GetSalesReportAsync(range, null, ct);\n"
        "      var expenses = await _reports.GetExpenseReportAsync(range, null, ct);\n"
        "      var refunds = await _reports.GetRefundActivityAsync(range, null, ct);\n"
        "      var foodCost = await _reports.GetFoodCostReportAsync(range.From, range.To, ct);\n"
        "      var significantActions = await _reports.GetSignificantActionsAsync(range.From, range.To, ct);\n"
        "      var (start, end) = range.ToUtcRange();\n"
        "      ...\n"
        "  }\n"
        "  ```\n"
        "  - **Financial & Transaction Integrity**: Executes multiple independent asynchronous read queries without an ambient transaction or snapshot isolation. Does not modify database state.",

    # 3. LoadTrendAsync
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.LoadTrendAsync\(Falah\.Domain\.Common\.BusinessDateRange, Falah\.Application\.Reports\.SalesReportDto, long, CancellationToken\) across lines 76-107\.":
        "- **Behavior**: Computes period-over-period comparative financial metrics. Invokes domain rule `TrendRules.ComparablePeriod(range)` to determine the previous baseline timeframe. Awaits comparative sales and expense reports from `_reports`. Evaluates net change in piastres and percentage delta across Net Sales, Collected Sales, Driver Cash, Expenses, and Order Count using local function `Change`. Evaluates granularity (`\"hour\"` for single day, `\"day\"` for multi-day) and awaits corresponding time-series generator (`LoadHourlySeriesAsync` or `LoadDailySeriesAsync`).\n"
        "  ```csharp\n"
        "  // Lines 76-107: Comparative trend calculation\n"
        "  private async Task<DashboardTrendDto> LoadTrendAsync(\n"
        "      BusinessDateRange range, SalesReportDto sales, long expensesTotal, CancellationToken ct)\n"
        "  {\n"
        "      var previousRange = TrendRules.ComparablePeriod(range);\n"
        "      var previousSales = await _reports.GetSalesReportAsync(previousRange, null, ct);\n"
        "      var previousExpenses = await _reports.GetExpenseReportAsync(previousRange, null, ct);\n"
        "      ...\n"
        "  }\n"
        "  ```",

    # 4. Change local function
    r"- \*\*Behavior\*\*: Executes logic for Change\(long, long\) across lines 87-91\.":
        "- **Behavior**: Local helper invoking `TrendRules.Compare(current, previous)` to compute absolute currency difference in piastres and percentage delta, returning an immutable `ChangeDto`.\n"
        "  ```csharp\n"
        "  // Lines 87-91: Pure trend comparison\n"
        "  static ChangeDto Change(long current, long previous)\n"
        "  {\n"
        "      var c = TrendRules.Compare(current, previous);\n"
        "      return new ChangeDto(c.CurrentPiastres, c.PreviousPiastres, c.DeltaPiastres, c.Percent);\n"
        "  }\n"
        "  ```",

    # 5. LoadDailySeriesAsync
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.LoadDailySeriesAsync\(Falah\.Domain\.Common\.BusinessDateRange, CancellationToken\) across lines 111-132\.":
        "- **Behavior**: Queries non-cancelled orders within UTC interval `[start, end)`. Groups rows in memory by Cairo business date via `BusinessClock.BusinessDate(r.CreatedAtUtc)`. Computes net sales (`TotalPiastres - RefundAmountPiastres`) and order count for each business date. Iterates over all dates in `range.Days()` to ensure zero-revenue days receive explicit `TrendPointDto(day, 0, 0)` data points rather than gaps in the sparkline curve.\n"
        "  ```csharp\n"
        "  // Lines 111-132: Daily time-series generation\n"
        "  private async Task<List<TrendPointDto>> LoadDailySeriesAsync(BusinessDateRange range, CancellationToken ct)\n"
        "  {\n"
        "      var (start, end) = range.ToUtcRange();\n"
        "      var rows = await _db.Orders.AsNoTracking()\n"
        "          .Where(o => o.Status != nameof(OrderStatus.Cancelled) && o.CreatedAtUtc >= start && o.CreatedAtUtc < end)\n"
        "          .Select(o => new { o.CreatedAtUtc, o.TotalPiastres, o.RefundAmountPiastres })\n"
        "          .ToListAsync(ct);\n"
        "      ...\n"
        "  }\n"
        "  ```",

    # 6. Lambda 115:20
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 115-115\.":
        "- **Behavior**: Inline predicate lambda `o => o.Status != nameof(OrderStatus.Cancelled) && o.CreatedAtUtc >= start && o.CreatedAtUtc < end`. Filters database orders to exclude cancelled sales and restrict records to the requested UTC date range.",

    # 7. Lambda 121:27
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 121-121\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline projection lambda `g => (Net: g.Sum(r => r.TotalPiastres - r.RefundAmountPiastres), Orders: g.Count())`. Computes aggregate net revenue and order count for a business date group.\n- **Validations**: None.\n- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}",

    # 8. Lambda 121:39
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 121-121\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline selector lambda `r => r.TotalPiastres - r.RefundAmountPiastres`. Subtracts order refund amount from total gross piastres to determine net revenue per order row.",

    # 9. Lambda 121:56
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 121-121\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline aggregation lambda `g => g.Count()`. Counts distinct order records within a business date group.",

    # 10. Lambda 126:21
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 126-130\.":
        "- **Behavior**: Inline projection lambda mapping each `DateOnly` in `range.Days()` to a `TrendPointDto`. Safely retrieves net sales and order count from the `byDate` dictionary or defaults to `(Net: 0L, Orders: 0)`.",

    # 11. LoadHourlySeriesAsync
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.LoadHourlySeriesAsync\(Falah\.Domain\.Common\.BusinessDateRange, CancellationToken\) across lines 134-154\.":
        "- **Behavior**: Queries non-cancelled orders within UTC interval `[start, end)`. Groups orders in memory by local wall-clock hour using `TimeZoneInfo.ConvertTime(r.CreatedAtUtc, BusinessClock.CairoTimeZone).Hour`. Aggregates net sales and order count. Projects all 24 hours (00:00 to 23:00) into `TrendPointDto` entries, populating zero-volume hours with zero metrics.\n"
        "  ```csharp\n"
        "  // Lines 134-154: Hourly time-series generation\n"
        "  private async Task<List<TrendPointDto>> LoadHourlySeriesAsync(BusinessDateRange range, CancellationToken ct)\n"
        "  {\n"
        "      var (start, end) = range.ToUtcRange();\n"
        "      var rows = await _db.Orders.AsNoTracking()\n"
        "          .Where(o => o.Status != nameof(OrderStatus.Cancelled) && o.CreatedAtUtc >= start && o.CreatedAtUtc < end)\n"
        "          .Select(o => new { o.CreatedAtUtc, o.TotalPiastres, o.RefundAmountPiastres })\n"
        "          .ToListAsync(ct);\n"
        "      ...\n"
        "  }\n"
        "  ```",

    # 12. Lambda 138:20
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 138-138\.":
        "- **Behavior**: Inline predicate lambda `o => o.Status != nameof(OrderStatus.Cancelled) && o.CreatedAtUtc >= start && o.CreatedAtUtc < end`. Filters orders for the hourly series query.",

    # 13. Lambda 144:22
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 144-144\.":
        "- **Behavior**: Inline grouping lambda `r => TimeZoneInfo.ConvertTime(r.CreatedAtUtc, BusinessClock.CairoTimeZone).Hour`. Extracts local Cairo wall-clock hour (0-23) from order UTC creation timestamp.",

    # 14. Lambda 145:27
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 145-145\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline projection lambda `g => (Net: g.Sum(r => r.TotalPiastres - r.RefundAmountPiastres), Orders: g.Count())`. Computes hourly net sales and order count.",

    # 15. Lambda 145:39
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 145-145\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline selector lambda `r => r.TotalPiastres - r.RefundAmountPiastres`. Computes net piastres for an order row.",

    # 16. Lambda 145:56
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 145-145\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline aggregation lambda `g => g.Count()`. Counts order rows falling within an hourly bucket.",

    # 17. Lambda 148:21
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 148-152\.":
        "- **Behavior**: Inline projection lambda `hour => ... new TrendPointDto($\"{hour:00}\", found.Net, found.Orders)`. Formats each hour index 0-23 into a 2-digit string and attaches aggregated metrics.",

    # 18. CurrentTreasuryAsync
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.CurrentTreasuryAsync\(CancellationToken\) across lines 158-169\.":
        "- **Behavior**: Queries all posted `TreasuryMovements` across entire database lifetime (`AsNoTracking()`). Uses local function `Balance(account)` to compute running balances for Main, Drawer, Bank, InstaPay, and Wallet accounts. Deliberately not day-scoped, answering 'what does the restaurant hold right now'.\n"
        "  ```csharp\n"
        "  // Lines 158-169: Current live treasury balance query\n"
        "  private async Task<TreasuryBalancesDto> CurrentTreasuryAsync(CancellationToken ct)\n"
        "  {\n"
        "      var movements = await _db.TreasuryMovements.AsNoTracking()\n"
        "          .Where(m => m.Status == nameof(MovementStatus.Posted))\n"
        "          .Select(m => new { m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres })\n"
        "          .ToListAsync(ct);\n"
        "      long Balance(string account) => movements.Sum(m =>\n"
        "          (m.DestinationAccountId == account ? m.AmountPiastres : 0) -\n"
        "          (m.SourceAccountId == account ? m.AmountPiastres : 0));\n"
        "      return new TreasuryBalancesDto(Balance(TreasuryAccounts.Main), Balance(TreasuryAccounts.Drawer),\n"
        "          Balance(TreasuryAccounts.Bank), Balance(TreasuryAccounts.InstaPay), Balance(TreasuryAccounts.Wallet));\n"
        "  }\n"
        "  ```\n"
        "  - **Financial Scalability Risk**: Full in-memory table scan of all posted movements without SQL-level `GROUP BY` or account index filtering.",

    # 19. Lambda 162:21
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 162-162\.":
        "- **Behavior**: Inline projection lambda `m => new { m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres }`. Extracts only necessary movement attributes for balance calculation.",

    # 20. Local function Balance
    r"- \*\*Behavior\*\*: Executes logic for Balance\(string\) across lines 164-166\.":
        "- **Behavior**: In-memory balance aggregator evaluating `DestinationAccountId == account ? AmountPiastres : 0` minus `SourceAccountId == account ? AmountPiastres : 0` across loaded movements.",

    # 21. Lambda 164:55
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 164-166\.":
        "- **Behavior**: Inline aggregation expression lambda `m => (m.DestinationAccountId == account ? m.AmountPiastres : 0) - (m.SourceAccountId == account ? m.AmountPiastres : 0)`. Sums signed delta for an account.",

    # 22. LoadLeakageAsync
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.LoadLeakageAsync\(Falah\.Domain\.Common\.BusinessDateRange, Falah\.Application\.Reports\.SalesReportDto, long, int, DateTimeOffset, DateTimeOffset, CancellationToken\) across lines 171-184\.":
        "- **Behavior**: Aggregates promotional discounts, comps, manual discounts, and refunds from `sales` alongside stock loss movements (Waste, Spoilage, Breakage) logged within `[start, end)`. Sums loss value via `losses.Sum(v => v ?? 0)`.\n"
        "  ```csharp\n"
        "  // Lines 171-184: Operational leakage compilation\n"
        "  private async Task<LeakageDto> LoadLeakageAsync(\n"
        "      BusinessDateRange range, SalesReportDto sales, long expensesTotal, int refundCount,\n"
        "      DateTimeOffset start, DateTimeOffset end, CancellationToken ct)\n"
        "  {\n"
        "      var lossTypes = new[] { nameof(StockMovementType.Waste), nameof(StockMovementType.Spoilage), nameof(StockMovementType.Breakage) };\n"
        "      var losses = await _db.StockMovements.AsNoTracking()\n"
        "          .Where(m => lossTypes.Contains(m.Type) && m.CreatedAtUtc >= start && m.CreatedAtUtc < end)\n"
        "          .Select(m => m.LossValuePiastres).ToListAsync(ct);\n"
        "      ...\n"
        "  }\n"
        "  ```\n"
        "  - **Defect INV-FIN-001**: Line 183 executes `losses.Sum(v => v ?? 0)`. If inventory loss movements lack purchase pricing (`LossValuePiastres == null`), they are coerced to zero, masking financial spoilage without indicating uncosted movements.",

    # 23. Lambda 177:20
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 177-177\.":
        "- **Behavior**: Inline filter lambda `m => lossTypes.Contains(m.Type) && m.CreatedAtUtc >= start && m.CreatedAtUtc < end`. Restricts stock movements to loss types within the date window.",

    # 24. LoadWagesPostedAsync
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.LoadWagesPostedAsync\(DateTimeOffset, DateTimeOffset, CancellationToken\) across lines 191-203\.":
        "- **Behavior**: Queries `EmployeeLedgerEntries` within `[start, end)` for wage types: `DailyWage`, `Salary`, `Overtime`, `ExtraShift`, `Bonus`, `OtherEarning`. Specifically excludes employee advances and withdrawals to avoid double-counting earnings already credited to employee accounts. Returns `(Total, Count)`.\n"
        "  ```csharp\n"
        "  // Lines 191-203: Labor cost aggregation\n"
        "  private async Task<(long Total, int Count)> LoadWagesPostedAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)\n"
        "  {\n"
        "      var wageTypes = new[]\n"
        "      {\n"
        "          nameof(LedgerEntryType.DailyWage), nameof(LedgerEntryType.Salary),\n"
        "          nameof(LedgerEntryType.Overtime), nameof(LedgerEntryType.ExtraShift),\n"
        "          nameof(LedgerEntryType.Bonus), nameof(LedgerEntryType.OtherEarning),\n"
        "      };\n"
        "      var rows = await _db.EmployeeLedgerEntries.AsNoTracking()\n"
        "          .Where(e => wageTypes.Contains(e.Type) && e.CreatedAtUtc >= start && e.CreatedAtUtc < end)\n"
        "          .Select(e => e.CreditPiastres).ToListAsync(ct);\n"
        "      return (rows.Sum(), rows.Count);\n"
        "  }\n"
        "  ```",

    # 25. Lambda 200:20
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 200-200\.":
        "- **Behavior**: Inline filter lambda `e => wageTypes.Contains(e.Type) && e.CreatedAtUtc >= start && e.CreatedAtUtc < end`. Filters employee ledger entries by recognized wage types and date window.",

    # 26. BuildProfitability
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.BuildProfitability\(Falah\.Application\.Reports\.SalesReportDto, Falah\.Application\.Reports\.FoodCostReportDto, long, long, int\) across lines 205-224\.":
        "- **Behavior**: Pure domain projection method. Converts sales and food cost figures to `Money` domain objects and invokes `ProfitabilityRules.ComputeGrossProfit` and `ProfitabilityRules.ComputeOperatingMargin`. Propagates completeness and missing ingredient warnings.\n"
        "  ```csharp\n"
        "  // Lines 205-224: Profitability synthesis\n"
        "  private static ProfitabilityDto BuildProfitability(\n"
        "      SalesReportDto sales, FoodCostReportDto foodCost, long expenses, long wages, int wagePostingCount)\n"
        "  {\n"
        "      var netSales = Money.FromPiastres(sales.NetSalesPiastres);\n"
        "      var knownFoodCost = Money.FromPiastres(foodCost.TotalFoodCostPiastres);\n"
        "      var gross = ProfitabilityRules.ComputeGrossProfit(netSales, knownFoodCost, foodCost.SoldLineCount, foodCost.KnownLineCount);\n"
        "      var operating = ProfitabilityRules.ComputeOperatingMargin(netSales, knownFoodCost, Money.FromPiastres(expenses), Money.FromPiastres(wages), foodCost.SoldLineCount, foodCost.KnownLineCount, wagePostingCount);\n"
        "      ...\n"
        "  }\n"
        "  ```",

    # 27. LoadSettledFromDriversAsync
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.LoadSettledFromDriversAsync\(DateTimeOffset, DateTimeOffset, CancellationToken\) across lines 230-234\.":
        "- **Behavior**: Queries and sums `AmountPiastres` from `TreasuryMovements` where `Type == \"driver_settlement\"` within `[start, end)`. Represents cash-on-delivery custody reconciled and posted into restaurant treasury.\n"
        "  ```csharp\n"
        "  // Lines 230-234: Driver cash settlement aggregation\n"
        "  private async Task<long> LoadSettledFromDriversAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct) =>\n"
        "      await _db.TreasuryMovements.AsNoTracking()\n"
        "          .Where(m => m.Type == \"driver_settlement\" && m.CreatedAtUtc >= start && m.CreatedAtUtc < end)\n"
        "          .SumAsync(m => (long?)m.AmountPiastres, ct) ?? 0;\n"
        "  ```",

    # 28. Lambda 232:20
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 232-232\.":
        "- **Behavior**: Inline predicate lambda `m => m.Type == \"driver_settlement\" && m.CreatedAtUtc >= start && m.CreatedAtUtc < end`. Filters driver settlement treasury movements within UTC date bounds.",

    # 29. LoadAttentionAsync
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.LoadAttentionAsync\(Falah\.Domain\.Common\.BusinessDateRange, CancellationToken\) across lines 235-295\.":
        "- **Behavior**: Collects high-priority operational anomalies and blockers: checks for an open business day and measures its age against `BusinessClock.Today()`; counts inventory cost warning audit events; reads shift discrepancy threshold; analyzes shift cash variances; sums unsettled driver custody; counts unconfirmed pending electronic orders; counts open control exceptions; and executes `BusinessDayCloseEvaluator.EvaluateAsync` to identify closing blockers.\n"
        "  ```csharp\n"
        "  // Lines 235-295: Operational attention compilation\n"
        "  private async Task<AttentionDto> LoadAttentionAsync(BusinessDateRange range, CancellationToken ct)\n"
        "  {\n"
        "      var openDay = await _db.BusinessDays.AsNoTracking()\n"
        "          .FirstOrDefaultAsync(d => d.Status == nameof(Falah.Domain.BusinessDay.BusinessDayStatus.Open), ct);\n"
        "      ...\n"
        "  }\n"
        "  ```",

    # 30. Lambda 238:34
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 238-238\.":
        "- **Behavior**: Inline predicate lambda `d => d.Status == nameof(Falah.Domain.BusinessDay.BusinessDayStatus.Open)`. Identifies active open business day.",

    # 31. Lambda 246:25
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 246-247\.":
        "- **Behavior**: Inline predicate lambda `e => e.Category == \"inventory_cost\" && e.Severity == \"warning\" && e.TimestampUtc >= costStart && e.TimestampUtc < costEnd`. Selects warning-level cost change audit events.",

    # 32. Lambda 253:21
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 253-253\.":
        "- **Behavior**: Inline projection lambda `s => new { s.Status, s.CashDifferencePiastres, s.DifferenceAuthorizedBy }`. Projects shift cash discrepancy details.",

    # 33. Lambda 256:43
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 256-256\.":
        "- **Behavior**: Inline predicate lambda `s => s.CashDifferencePiastres != null && s.CashDifferencePiastres != 0`. Filters shifts exhibiting non-zero drawer variances.",

    # 34. Lambda 269:34
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 269-269\.":
        "- **Behavior**: Inline predicate lambda `d => d.Status == nameof(Falah.Domain.BusinessDay.BusinessDayStatus.Open)`. Re-queries open business day for evaluation by `BusinessDayCloseEvaluator`.",

    # 35. Lambda 281:57
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 281-281\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline predicate lambda `s => s.CashDifferencePiastres < 0`. Filters shifts with cash shortages.",

    # 36. Lambda 281:96
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 281-281\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline selector lambda `s => -s.CashDifferencePiastres!.Value`. Negates negative shortage value to express shortage as a positive piastre magnitude.",

    # 37. Lambda 282:56
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 282-282\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline predicate lambda `s => s.CashDifferencePiastres > 0`. Filters shifts with cash surpluses/overages.",

    # 38. Lambda 282:95
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 282-282\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline selector lambda `s => s.CashDifferencePiastres!.Value`. Selects positive cash surplus amount.",

    # 39. Lambda 285:69
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 285-286\.":
        "- **Behavior**: Inline predicate lambda `s => s.DifferenceAuthorizedBy == null && (s.CashDifferencePiastres > threshold || s.CashDifferencePiastres < -threshold)`. Identifies unauthorized shift cash discrepancies exceeding manager threshold.",

    # 40. LoadStockAsync
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.LoadStockAsync\(CancellationToken\) across lines 297-315\.":
        "- **Behavior**: Queries active inventory items with minimum thresholds and calculates stock balances by grouping `StockMovements`. Filters items where balance is below minimum stock to generate low-stock alerts.\n"
        "  ```csharp\n"
        "  // Lines 297-315: Inventory stock position query\n"
        "  private async Task<StockPositionDto> LoadStockAsync(CancellationToken ct)\n"
        "  {\n"
        "      var items = await _db.InventoryItems.AsNoTracking().Where(i => i.Active)\n"
        "          .Select(i => new { i.Id, i.Name, i.Unit, i.MinimumMilliUnits }).ToListAsync(ct);\n"
        "      var balances = await _db.StockMovements.AsNoTracking()\n"
        "          .GroupBy(m => m.InventoryItemId)\n"
        "          .Select(g => new { InventoryItemId = g.Key, Balance = g.Sum(m => m.QuantityMilliUnits) })\n"
        "          .ToDictionaryAsync(x => x.InventoryItemId, x => x.Balance, ct);\n"
        "      ...\n"
        "  }\n"
        "  ```\n"
        "  > [!CRITICAL]\n"
        "  > **Defect FIN-013 / FIN-029 Sign Inversion**: Line 303 executes `.Select(g => new { InventoryItemId = g.Key, Balance = g.Sum(m => m.QuantityMilliUnits) })` without checking `m.Direction`. Outbound stock movements (`Direction == \"Out\"`) are added instead of subtracted, inverting inventory usage and causing depleted items to report increased stock, breaking low-stock warnings.",

    # 41. Lambda 299:67
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 299-299\.":
        "- **Behavior**: Inline predicate lambda `i => i.Active`. Restricts inventory stock evaluation to currently active items.",

    # 42. Lambda 300:21
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 300-300\.":
        "- **Behavior**: Inline projection lambda `i => new { i.Id, i.Name, i.Unit, i.MinimumMilliUnits }`. Extracts core inventory item fields.",

    # 43. Lambda 303:21
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 303-303\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline projection lambda `g => new { InventoryItemId = g.Key, Balance = g.Sum(m => m.QuantityMilliUnits) }`. Embodies defect **FIN-013**: sums raw quantities unsigned across all stock movements regardless of direction.\n- **Validations**: None.\n- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}",

    # 44. Lambda 303:73
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 303-303\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline selector lambda `m => m.QuantityMilliUnits`. Passes unsigned movement quantity directly into sum aggregation without directional negation.",

    # 45. Lambda 307:21
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 307-307\.":
        "- **Behavior**: Inline projection lambda `i => new { i.Id, i.Name, i.Unit, i.MinimumMilliUnits, Balance = balances.GetValueOrDefault(i.Id) }`. Binds calculated balance to each inventory item.",

    # 46. Lambda 311:21
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 311-311\.":
        "- **Behavior**: Inline projection lambda `i => new LowStockItemDto(i.Id, i.Name, i.Unit, i.Balance, i.MinimumMilliUnits)`. Constructs low stock notification DTO.",

    # 47. LoadObligationsAsync
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.LoadObligationsAsync\(DateTimeOffset, DateTimeOffset, CancellationToken\) across lines 317-357\.":
        "- **Behavior**: Assembles accounts payable and liability figures: aggregates supplier debts from `SupplierLedgerEntries` (`DebitPiastres - CreditPiastres`); sums purchase invoice totals and payments; aggregates employee ledger liabilities (`CreditPiastres - DebitPiastres`); evaluates overdue supplier invoices using `PayablesAging.DaysOverdue` against `BusinessClock.Today()`; and counts unscheduled invoices lacking due dates.\n"
        "  ```csharp\n"
        "  // Lines 317-357: Outstanding liabilities and payables aging query\n"
        "  private async Task<ObligationsDto> LoadObligationsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)\n"
        "  {\n"
        "      var supplierRows = await _db.SupplierLedgerEntries.AsNoTracking()\n"
        "          .GroupBy(e => e.SupplierId)\n"
        "          .Select(g => new { SupplierId = g.Key, Outstanding = g.Sum(e => e.DebitPiastres - e.CreditPiastres) })\n"
        "          .ToListAsync(ct);\n"
        "      ...\n"
        "  }\n"
        "  ```",

    # 48. Lambda 323:21
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 323-323\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline projection lambda `g => new { SupplierId = g.Key, Outstanding = g.Sum(e => e.DebitPiastres - e.CreditPiastres) }`. Calculates net outstanding debt owed to each supplier.",

    # 49. Lambda 323:72
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 323-323\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline selector lambda `e => e.DebitPiastres - e.CreditPiastres`. Calculates signed balance per supplier ledger entry.",

    # 50. Lambda 333:21
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 333-333\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline projection lambda `g => new { EmployeeId = g.Key, Balance = g.Sum(e => e.CreditPiastres - e.DebitPiastres) }`. Computes running employee wage liability owed by the restaurant.",

    # 51. Lambda 333:68
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 333-333\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline selector lambda `e => e.CreditPiastres - e.DebitPiastres`. Subtracts employee debits (payouts/settlements) from credits (wages) to compute balance.",

    # 52. Lambda 345:21
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 345-345\.":
        "- **Behavior**: Inline projection lambda `p => new { p.RemainingAmountPiastres, Days = Falah.Domain.Purchasing.PayablesAging.DaysOverdue(p.DueDate, asOf) }`. Calculates overdue days for unpaid supplier invoices.",

    # 53. Lambda 354:37
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 354-354\.":
        "- **Behavior**: Inline aggregation lambda `p => p.RemainingAmountPiastres`. Sums remaining unpaid amounts for invoices lacking explicit due dates.",

    # 54. LoadPeopleAsync
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.LoadPeopleAsync\(Falah\.Domain\.Common\.BusinessDateRange, long, CancellationToken\) across lines 358-373\.":
        "- **Behavior**: Queries `AttendanceRecords` within `[range.From, range.To]`. Aggregates attendance statistics (total records, Present, Late, Absent, MissingFingerprint), calculates cumulative late minutes, and binds period posted wages to build `PeopleDto`.\n"
        "  ```csharp\n"
        "  // Lines 358-373: Workforce attendance query\n"
        "  private async Task<PeopleDto> LoadPeopleAsync(BusinessDateRange range, long wages, CancellationToken ct)\n"
        "  {\n"
        "      var records = await _db.AttendanceRecords.AsNoTracking()\n"
        "          .Where(a => a.Date >= range.From && a.Date <= range.To)\n"
        "          .Select(a => new { a.Status, a.LateMinutes }).ToListAsync(ct);\n"
        "      ...\n"
        "  }\n"
        "  ```",

    # 55. LoadHealthAsync
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.LoadHealthAsync\(CancellationToken\) across lines 374-419\.":
        "- **Behavior**: Evaluates database backup freshness and print route telemetry: queries `BackupRecords` for latest successful backup timestamp, calculates hours elapsed, and counts failed backups. For each route in `Enum.GetNames<PrintRoute>()`, queries `PrintJobs` and `PrintJobAttempts` to compute pending count, maximum queue wait time, last success/failure timestamps, and failed job count.\n"
        "  ```csharp\n"
        "  // Lines 374-419: Operational health query\n"
        "  private async Task<SystemHealthDto> LoadHealthAsync(CancellationToken ct)\n"
        "  {\n"
        "      var backups = await _db.BackupRecords.AsNoTracking()\n"
        "          .Where(b => b.Kind == nameof(Persistence.Entities.BackupRecordKind.Backup))\n"
        "          .Select(b => new { b.Status, b.CompletedAtUtc, b.VerificationStatus })\n"
        "          .ToListAsync(ct);\n"
        "      ...\n"
        "  }\n"
        "  ```",

    # 56. Lambda 382:20
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 382-382\.":
        "- **Behavior**: Inline predicate lambda `b => b.Status == nameof(Persistence.Entities.BackupRecordStatus.Succeeded) && b.CompletedAtUtc != null`. Identifies completed successful database backups.",

    # 57. Lambda 390:65
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 390-390\.":
        "- **Behavior**: Inline filter lambda `j => j.Route == route`. Filters print jobs belonging to the current print route under evaluation.",

    # 58. Lambda 392:38
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 392-392\.":
        "- **Behavior**: Inline predicate lambda `j => j.Status == nameof(PrintJobStatus.Pending)`. Filters print jobs currently awaiting physical spooler submission.",

    # 59. Lambda 400:46
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 400-400\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline predicate lambda `a => a.Succeeded`. Filters successful physical print attempts.",

    # 60. Lambda 400:82
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 400-400\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline selector lambda `a => a.AttemptedAtUtc`. Orders print attempts to identify the most recent successful print event.",

    # 61. Lambda 401:46
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 401-401\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline predicate lambda `a => !a.Succeeded`. Filters failed physical print attempts.",

    # 62. Lambda 401:83
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 401-401\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline selector lambda `a => a.AttemptedAtUtc`. Orders failed print attempts to identify the most recent error timestamp.",

    # 63. Lambda 406:67
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 406-406\.":
        "- **Behavior**: Inline selector lambda `p => p.CreatedAtUtc`. Determines oldest pending print job timestamp to calculate queue latency in minutes.",

    # 64. Lambda 417:27
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 417-417\.":
        "- **Behavior**: Inline predicate lambda `b => b.Status == nameof(Persistence.Entities.BackupRecordStatus.Failed)`. Counts failed database backup attempts in history."
}

updated_content = content
applied = 0
for pat, repl in replacements.items():
    if re.search(pat, updated_content):
        updated_content = re.sub(pat, repl, updated_content)
        applied += 1
    else:
        print("MISSING PATTERN:", pat[:70])

remaining_generic = len(re.findall(r"Executes logic for.*", updated_content))
print(f"Applied: {applied}/{len(replacements)}. Remaining generic entries: {remaining_generic}")

with open(review_path, 'w', encoding='utf-8') as f:
    f.write(updated_content)
print("Updated OwnerDashboardQueryService.cs.md successfully.")

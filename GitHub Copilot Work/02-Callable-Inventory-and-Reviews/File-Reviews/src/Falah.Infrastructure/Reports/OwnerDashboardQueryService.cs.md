# File Review: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs
**Total Callables**: 114
---
## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:32:5:ConstructorDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.OwnerDashboardQueryService(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Infrastructure.Reports.ReportingQueryService)
- **Kind**: ConstructorDeclaration
- **Behavior**: Initializes the query service by binding the EF Core `FalahDbContext` instance and `ReportingQueryService` domain reporter dependency. Maintains zero mutable state and acts as an executive composition facade.
  ```csharp
  // Lines 32-36: Constructor initialization
  public OwnerDashboardQueryService(FalahDbContext db, ReportingQueryService reports)
  {
      _db = db;
      _reports = reports;
  }
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:38:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.GetAsync(Falah.Domain.Common.BusinessDateRange, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Top-level composition orchestrator for the Executive Owner Dashboard. Aggregates five foundational reports from `ReportingQueryService` (`SalesReportDto`, `ExpenseReportDto`, `RefundActivityDto`, `FoodCostReportDto`, and significant actions count). Converts the requested `BusinessDateRange` to UTC bounds via `range.ToUtcRange()`. Awaits `CurrentTreasuryAsync(ct)` and `LoadSettledFromDriversAsync(start, end, ct)` to populate `CashPositionDto`. Assembles operational leakage via `LoadLeakageAsync`, calculates labor liability via `LoadWagesPostedAsync`, and invokes pure projection `BuildProfitability`. In parallel, loads operational alerts (`LoadAttentionAsync`), low-stock valuation (`LoadStockAsync`), outstanding obligations (`LoadObligationsAsync`), workforce attendance (`LoadPeopleAsync`), system backup and print queue health (`LoadHealthAsync`), and period comparative trends (`LoadTrendAsync`).
  ```csharp
  // Lines 38-71: Master dashboard query composition
  public async Task<OwnerDashboardDto> GetAsync(BusinessDateRange range, CancellationToken ct = default)
  {
      var sales = await _reports.GetSalesReportAsync(range, null, ct);
      var expenses = await _reports.GetExpenseReportAsync(range, null, ct);
      var refunds = await _reports.GetRefundActivityAsync(range, null, ct);
      var foodCost = await _reports.GetFoodCostReportAsync(range.From, range.To, ct);
      var significantActions = await _reports.GetSignificantActionsAsync(range.From, range.To, ct);
      var (start, end) = range.ToUtcRange();
      ...
  }
  ```
  - **Financial & Transaction Integrity**: Executes multiple independent asynchronous read queries without an ambient transaction or snapshot isolation. Does not modify database state.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:76:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadTrendAsync(Falah.Domain.Common.BusinessDateRange, Falah.Application.Reports.SalesReportDto, long, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Computes period-over-period comparative financial metrics. Invokes domain rule `TrendRules.ComparablePeriod(range)` to determine the previous baseline timeframe. Awaits comparative sales and expense reports from `_reports`. Evaluates net change in piastres and percentage delta across Net Sales, Collected Sales, Driver Cash, Expenses, and Order Count using local function `Change`. Evaluates granularity (`"hour"` for single day, `"day"` for multi-day) and awaits corresponding time-series generator (`LoadHourlySeriesAsync` or `LoadDailySeriesAsync`).
  ```csharp
  // Lines 76-107: Comparative trend calculation
  private async Task<DashboardTrendDto> LoadTrendAsync(
      BusinessDateRange range, SalesReportDto sales, long expensesTotal, CancellationToken ct)
  {
      var previousRange = TrendRules.ComparablePeriod(range);
      var previousSales = await _reports.GetSalesReportAsync(previousRange, null, ct);
      var previousExpenses = await _reports.GetExpenseReportAsync(previousRange, null, ct);
      ...
  }
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:87:9:LocalFunctionStatement
- **Symbol**: Change(long, long)
- **Kind**: LocalFunctionStatement
- **Behavior**: Local helper invoking `TrendRules.Compare(current, previous)` to compute absolute currency difference in piastres and percentage delta, returning an immutable `ChangeDto`.
  ```csharp
  // Lines 87-91: Pure trend comparison
  static ChangeDto Change(long current, long previous)
  {
      var c = TrendRules.Compare(current, previous);
      return new ChangeDto(c.CurrentPiastres, c.PreviousPiastres, c.DeltaPiastres, c.Percent);
  }
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:111:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadDailySeriesAsync(Falah.Domain.Common.BusinessDateRange, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Queries non-cancelled orders within UTC interval `[start, end)`. Groups rows in memory by Cairo business date via `BusinessClock.BusinessDate(r.CreatedAtUtc)`. Computes net sales (`TotalPiastres - RefundAmountPiastres`) and order count for each business date. Iterates over all dates in `range.Days()` to ensure zero-revenue days receive explicit `TrendPointDto(day, 0, 0)` data points rather than gaps in the sparkline curve.
  ```csharp
  // Lines 111-132: Daily time-series generation
  private async Task<List<TrendPointDto>> LoadDailySeriesAsync(BusinessDateRange range, CancellationToken ct)
  {
      var (start, end) = range.ToUtcRange();
      var rows = await _db.Orders.AsNoTracking()
          .Where(o => o.Status != nameof(OrderStatus.Cancelled) && o.CreatedAtUtc >= start && o.CreatedAtUtc < end)
          .Select(o => new { o.CreatedAtUtc, o.TotalPiastres, o.RefundAmountPiastres })
          .ToListAsync(ct);
      ...
  }
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:115:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `o => o.Status != nameof(OrderStatus.Cancelled) && o.CreatedAtUtc >= start && o.CreatedAtUtc < end`. Filters database orders to exclude cancelled sales and restrict records to the requested UTC date range.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:116:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(o => new { o.CreatedAtUtc, o.TotalPiastres, o.RefundAmountPiastres })'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:120:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.GroupBy(r => BusinessClock.BusinessDate(r.CreatedAtUtc))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:121:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline aggregation lambda `g => g.Count()`. Counts distinct order records within a business date group.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:121:39:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline aggregation lambda `g => g.Count()`. Counts distinct order records within a business date group.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:121:56:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline aggregation lambda `g => g.Count()`. Counts distinct order records within a business date group.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:126:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline projection lambda mapping each `DateOnly` in `range.Days()` to a `TrendPointDto`. Safely retrieves net sales and order count from the `byDate` dictionary or defaults to `(Net: 0L, Orders: 0)`.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:134:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadHourlySeriesAsync(Falah.Domain.Common.BusinessDateRange, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Queries non-cancelled orders within UTC interval `[start, end)`. Groups orders in memory by local wall-clock hour using `TimeZoneInfo.ConvertTime(r.CreatedAtUtc, BusinessClock.CairoTimeZone).Hour`. Aggregates net sales and order count. Projects all 24 hours (00:00 to 23:00) into `TrendPointDto` entries, populating zero-volume hours with zero metrics.
  ```csharp
  // Lines 134-154: Hourly time-series generation
  private async Task<List<TrendPointDto>> LoadHourlySeriesAsync(BusinessDateRange range, CancellationToken ct)
  {
      var (start, end) = range.ToUtcRange();
      var rows = await _db.Orders.AsNoTracking()
          .Where(o => o.Status != nameof(OrderStatus.Cancelled) && o.CreatedAtUtc >= start && o.CreatedAtUtc < end)
          .Select(o => new { o.CreatedAtUtc, o.TotalPiastres, o.RefundAmountPiastres })
          .ToListAsync(ct);
      ...
  }
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:138:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `o => o.Status != nameof(OrderStatus.Cancelled) && o.CreatedAtUtc >= start && o.CreatedAtUtc < end`. Filters orders for the hourly series query.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:139:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(o => new { o.CreatedAtUtc, o.TotalPiastres, o.RefundAmountPiastres })'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:144:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline grouping lambda `r => TimeZoneInfo.ConvertTime(r.CreatedAtUtc, BusinessClock.CairoTimeZone).Hour`. Extracts local Cairo wall-clock hour (0-23) from order UTC creation timestamp.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:145:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline aggregation lambda `g => g.Count()`. Counts order rows falling within an hourly bucket.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:145:39:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline aggregation lambda `g => g.Count()`. Counts order rows falling within an hourly bucket.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:145:56:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline aggregation lambda `g => g.Count()`. Counts order rows falling within an hourly bucket.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:148:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline projection lambda `hour => ... new TrendPointDto($"{hour:00}", found.Net, found.Orders)`. Formats each hour index 0-23 into a 2-digit string and attaches aggregated metrics.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:158:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.CurrentTreasuryAsync(CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Queries all posted `TreasuryMovements` across entire database lifetime (`AsNoTracking()`). Uses local function `Balance(account)` to compute running balances for Main, Drawer, Bank, InstaPay, and Wallet accounts. Deliberately not day-scoped, answering 'what does the restaurant hold right now'.
  ```csharp
  // Lines 158-169: Current live treasury balance query
  private async Task<TreasuryBalancesDto> CurrentTreasuryAsync(CancellationToken ct)
  {
      var movements = await _db.TreasuryMovements.AsNoTracking()
          .Where(m => m.Status == nameof(MovementStatus.Posted))
          .Select(m => new { m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres })
          .ToListAsync(ct);
      long Balance(string account) => movements.Sum(m =>
          (m.DestinationAccountId == account ? m.AmountPiastres : 0) -
          (m.SourceAccountId == account ? m.AmountPiastres : 0));
      return new TreasuryBalancesDto(Balance(TreasuryAccounts.Main), Balance(TreasuryAccounts.Drawer),
          Balance(TreasuryAccounts.Bank), Balance(TreasuryAccounts.InstaPay), Balance(TreasuryAccounts.Wallet));
  }
  ```
  - **Financial Scalability Risk**: Full in-memory table scan of all posted movements without SQL-level `GROUP BY` or account index filtering.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': True, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:161:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(m => m.Status == nameof(MovementStatus.Posted))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:162:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline projection lambda `m => new { m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres }`. Extracts only necessary movement attributes for balance calculation.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:164:9:LocalFunctionStatement
- **Symbol**: Balance(string)
- **Kind**: LocalFunctionStatement
- **Behavior**: In-memory balance aggregator evaluating `DestinationAccountId == account ? AmountPiastres : 0` minus `SourceAccountId == account ? AmountPiastres : 0` across loaded movements.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:164:55:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline aggregation expression lambda `m => (m.DestinationAccountId == account ? m.AmountPiastres : 0) - (m.SourceAccountId == account ? m.AmountPiastres : 0)`. Sums signed delta for an account.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:171:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadLeakageAsync(Falah.Domain.Common.BusinessDateRange, Falah.Application.Reports.SalesReportDto, long, int, DateTimeOffset, DateTimeOffset, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Aggregates promotional discounts, comps, manual discounts, and refunds from `sales` alongside stock loss movements (Waste, Spoilage, Breakage) logged within `[start, end)`. Sums loss value via `losses.Sum(v => v ?? 0)`.
  ```csharp
  // Lines 171-184: Operational leakage compilation
  private async Task<LeakageDto> LoadLeakageAsync(
      BusinessDateRange range, SalesReportDto sales, long expensesTotal, int refundCount,
      DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
  {
      var lossTypes = new[] { nameof(StockMovementType.Waste), nameof(StockMovementType.Spoilage), nameof(StockMovementType.Breakage) };
      var losses = await _db.StockMovements.AsNoTracking()
          .Where(m => lossTypes.Contains(m.Type) && m.CreatedAtUtc >= start && m.CreatedAtUtc < end)
          .Select(m => m.LossValuePiastres).ToListAsync(ct);
      ...
  }
  ```
  - **Defect INV-FIN-001**: Line 183 executes `losses.Sum(v => v ?? 0)`. If inventory loss movements lack purchase pricing (`LossValuePiastres == null`), they are coerced to zero, masking financial spoilage without indicating uncosted movements.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:177:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline filter lambda `m => lossTypes.Contains(m.Type) && m.CreatedAtUtc >= start && m.CreatedAtUtc < end`. Restricts stock movements to loss types within the date window.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:178:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(m => m.LossValuePiastres).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:183:24:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'losses.Sum(v => v ?? 0), losses.Count, expensesTotal);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:191:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadWagesPostedAsync(DateTimeOffset, DateTimeOffset, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Queries `EmployeeLedgerEntries` within `[start, end)` for wage types: `DailyWage`, `Salary`, `Overtime`, `ExtraShift`, `Bonus`, `OtherEarning`. Specifically excludes employee advances and withdrawals to avoid double-counting earnings already credited to employee accounts. Returns `(Total, Count)`.
  ```csharp
  // Lines 191-203: Labor cost aggregation
  private async Task<(long Total, int Count)> LoadWagesPostedAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
  {
      var wageTypes = new[]
      {
          nameof(LedgerEntryType.DailyWage), nameof(LedgerEntryType.Salary),
          nameof(LedgerEntryType.Overtime), nameof(LedgerEntryType.ExtraShift),
          nameof(LedgerEntryType.Bonus), nameof(LedgerEntryType.OtherEarning),
      };
      var rows = await _db.EmployeeLedgerEntries.AsNoTracking()
          .Where(e => wageTypes.Contains(e.Type) && e.CreatedAtUtc >= start && e.CreatedAtUtc < end)
          .Select(e => e.CreditPiastres).ToListAsync(ct);
      return (rows.Sum(), rows.Count);
  }
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:200:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline filter lambda `e => wageTypes.Contains(e.Type) && e.CreatedAtUtc >= start && e.CreatedAtUtc < end`. Filters employee ledger entries by recognized wage types and date window.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:201:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(e => e.CreditPiastres).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:205:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.BuildProfitability(Falah.Application.Reports.SalesReportDto, Falah.Application.Reports.FoodCostReportDto, long, long, int)
- **Kind**: MethodDeclaration
- **Behavior**: Pure domain projection method. Converts sales and food cost figures to `Money` domain objects and invokes `ProfitabilityRules.ComputeGrossProfit` and `ProfitabilityRules.ComputeOperatingMargin`. Propagates completeness and missing ingredient warnings.
  ```csharp
  // Lines 205-224: Profitability synthesis
  private static ProfitabilityDto BuildProfitability(
      SalesReportDto sales, FoodCostReportDto foodCost, long expenses, long wages, int wagePostingCount)
  {
      var netSales = Money.FromPiastres(sales.NetSalesPiastres);
      var knownFoodCost = Money.FromPiastres(foodCost.TotalFoodCostPiastres);
      var gross = ProfitabilityRules.ComputeGrossProfit(netSales, knownFoodCost, foodCost.SoldLineCount, foodCost.KnownLineCount);
      var operating = ProfitabilityRules.ComputeOperatingMargin(netSales, knownFoodCost, Money.FromPiastres(expenses), Money.FromPiastres(wages), foodCost.SoldLineCount, foodCost.KnownLineCount, wagePostingCount);
      ...
  }
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:230:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadSettledFromDriversAsync(DateTimeOffset, DateTimeOffset, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Sums `AmountPiastres` of `TreasuryMovements` with `Type == "driver_settlement"` within `[start, end)`. Reconciles cash-on-delivery payments received from drivers back to the restaurant treasury.
  ```csharp
  // Lines 230-233: Driver settlement reconciliation
  private async Task<long> LoadSettledFromDriversAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct) =>
      await _db.TreasuryMovements.AsNoTracking()
          .Where(m => m.Type == "driver_settlement" && m.CreatedAtUtc >= start && m.CreatedAtUtc < end)
          .SumAsync(m => (long?)m.AmountPiastres, ct) ?? 0;
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:232:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `m => m.Type == "driver_settlement" && m.CreatedAtUtc >= start && m.CreatedAtUtc < end`. Filters driver settlement treasury movements within UTC date bounds.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:233:23:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.SumAsync(m => (long?)m.AmountPiastres, ct) ?? 0;'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:235:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadAttentionAsync(Falah.Domain.Common.BusinessDateRange, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Collects high-priority operational anomalies and blockers: checks for an open business day and measures its age against `BusinessClock.Today()`; counts inventory cost warning audit events; reads shift discrepancy threshold; analyzes shift cash variances; sums unsettled driver custody; counts unconfirmed pending electronic orders; counts open control exceptions; and executes `BusinessDayCloseEvaluator.EvaluateAsync` to identify closing blockers.
  ```csharp
  // Lines 235-295: Operational attention compilation
  private async Task<AttentionDto> LoadAttentionAsync(BusinessDateRange range, CancellationToken ct)
  {
      var openDay = await _db.BusinessDays.AsNoTracking()
          .FirstOrDefaultAsync(d => d.Status == nameof(Falah.Domain.BusinessDay.BusinessDayStatus.Open), ct);
      ...
  }
  ```
- **Validations**: Validation checks detected: True. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': True, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:238:34:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `d => d.Status == nameof(Falah.Domain.BusinessDay.BusinessDayStatus.Open)`. Identifies active open business day.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:246:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `e => e.Category == "inventory_cost" && e.Severity == "warning" && e.TimestampUtc >= costStart && e.TimestampUtc < costEnd`. Selects warning-level cost change audit events.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:252:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(s => s.BusinessDate >= range.From && s.BusinessDate <= range.To)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:253:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline projection lambda `s => new { s.Status, s.CashDifferencePiastres, s.DifferenceAuthorizedBy }`. Projects shift cash discrepancy details.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:256:43:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `s => s.CashDifferencePiastres != null && s.CashDifferencePiastres != 0`. Filters shifts exhibiting non-zero drawer variances.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:259:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(c => c.Status != nameof(CustodyStatus.Settled))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:260:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(c => c.ExpectedPiastres).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:263:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.CountAsync(o => o.ElectronicPaymentStatus == "Pending", ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:266:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.CountAsync(e => e.Status == "Open", ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:269:34:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `d => d.Status == nameof(Falah.Domain.BusinessDay.BusinessDayStatus.Open)`. Re-queries open business day for evaluation by `BusinessDayCloseEvaluator`.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:279:42:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'OpenShiftCount: shifts.Count(s => s.Status != "Closed"),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:281:57:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `s => -s.CashDifferencePiastres!.Value`. Negates negative shortage value to express shortage as a positive piastre magnitude.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:281:96:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `s => -s.CashDifferencePiastres!.Value`. Negates negative shortage value to express shortage as a positive piastre magnitude.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:282:56:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `s => s.CashDifferencePiastres!.Value`. Selects positive cash surplus amount.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:282:95:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `s => s.CashDifferencePiastres!.Value`. Selects positive cash surplus amount.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:285:69:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `s => s.DifferenceAuthorizedBy == null && (s.CashDifferencePiastres > threshold || s.CashDifferencePiastres < -threshold)`. Identifies unauthorized shift cash discrepancies exceeding manager threshold.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:297:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadStockAsync(CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Queries active inventory items with minimum thresholds and calculates stock balances by grouping `StockMovements`. Filters items where balance is below minimum stock to generate low-stock alerts.
  ```csharp
  // Lines 297-315: Inventory stock position query
  private async Task<StockPositionDto> LoadStockAsync(CancellationToken ct)
  {
      var items = await _db.InventoryItems.AsNoTracking().Where(i => i.Active)
          .Select(i => new { i.Id, i.Name, i.Unit, i.MinimumMilliUnits }).ToListAsync(ct);
      var balances = await _db.StockMovements.AsNoTracking()
          .GroupBy(m => m.InventoryItemId)
          .Select(g => new { InventoryItemId = g.Key, Balance = g.Sum(m => m.QuantityMilliUnits) })
          .ToDictionaryAsync(x => x.InventoryItemId, x => x.Balance, ct);
      ...
  }
  ```
  > [!CRITICAL]
  > **Defect FIN-013 / FIN-029 Sign Inversion**: Line 303 executes `.Select(g => new { InventoryItemId = g.Key, Balance = g.Sum(m => m.QuantityMilliUnits) })` without checking `m.Direction`. Outbound stock movements (`Direction == "Out"`) are added instead of subtracted, inverting inventory usage and causing depleted items to report increased stock, breaking low-stock warnings.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:299:67:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `i => i.Active`. Restricts inventory stock evaluation to currently active items.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:300:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline projection lambda `i => new { i.Id, i.Name, i.Unit, i.MinimumMilliUnits }`. Extracts core inventory item fields.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:302:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.GroupBy(m => m.InventoryItemId)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:303:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `m => m.QuantityMilliUnits`. Passes unsigned movement quantity directly into sum aggregation without directional negation.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:303:73:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `m => m.QuantityMilliUnits`. Passes unsigned movement quantity directly into sum aggregation without directional negation.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:304:32:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.ToDictionaryAsync(x => x.InventoryItemId, x => x.Balance, ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:304:56:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.ToDictionaryAsync(x => x.InventoryItemId, x => x.Balance, ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:307:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline projection lambda `i => new { i.Id, i.Name, i.Unit, i.MinimumMilliUnits, Balance = balances.GetValueOrDefault(i.Id) }`. Binds calculated balance to each inventory item.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:309:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(i => i.Balance <= i.MinimumMilliUnits && i.MinimumMilliUnits > 0)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:310:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.OrderBy(i => i.Balance - i.MinimumMilliUnits)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:311:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline projection lambda `i => new LowStockItemDto(i.Id, i.Name, i.Unit, i.Balance, i.MinimumMilliUnits)`. Constructs low stock notification DTO.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:317:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadObligationsAsync(DateTimeOffset, DateTimeOffset, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Aggregates outstanding financial liabilities: calculates net balances owed to suppliers from `SupplierLedgerEntries` (`DebitPiastres - CreditPiastres`), aggregates purchases and paid amounts, calculates employee wage liabilities (`CreditPiastres - DebitPiastres`), and calculates overdue accounts payable aging using `PayablesAging.DaysOverdue` against `BusinessClock.Today()`.
  ```csharp
  // Lines 317-356: Liabilities and payables aging
  private async Task<ObligationsDto> LoadObligationsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
  {
      var supplierRows = await _db.SupplierLedgerEntries.AsNoTracking()
          .GroupBy(e => e.SupplierId)
          .Select(g => new { SupplierId = g.Key, Outstanding = g.Sum(e => e.DebitPiastres - e.CreditPiastres) })
          .ToListAsync(ct);
      ...
  }
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:322:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.GroupBy(e => e.SupplierId)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:323:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `e => e.DebitPiastres - e.CreditPiastres`. Calculates signed balance per supplier ledger entry.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:323:72:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `e => e.DebitPiastres - e.CreditPiastres`. Calculates signed balance per supplier ledger entry.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:325:40:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var owing = supplierRows.Where(r => r.Outstanding > 0).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:328:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(p => p.CreatedAtUtc >= start && p.CreatedAtUtc < end)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:329:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(p => new { p.TotalPiastres, p.PaidPiastres }).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:332:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.GroupBy(e => e.EmployeeId)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:333:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `e => e.CreditPiastres - e.DebitPiastres`. Subtracts employee debits (payouts/settlements) from credits (wages) to compute balance.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:333:68:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `e => e.CreditPiastres - e.DebitPiastres`. Subtracts employee debits (payouts/settlements) from credits (wages) to compute balance.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:335:48:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var owedEmployees = employeeRows.Where(r => r.Balance > 0).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:341:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(p => p.RemainingAmountPiastres > 0 && p.Status != "cancelled")'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:342:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(p => new { p.RemainingAmountPiastres, p.DueDate }).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:345:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline projection lambda `p => new { p.RemainingAmountPiastres, Days = Falah.Domain.Purchasing.PayablesAging.DaysOverdue(p.DueDate, asOf) }`. Calculates overdue days for unpaid supplier invoices.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:346:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(p => p.Days is > 0).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:347:54:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var unscheduledInvoices = openInvoices.Where(p => p.DueDate is null).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:350:23:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'owing.Sum(r => r.Outstanding), owing.Count,'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:351:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'purchases.Sum(p => p.TotalPiastres), purchases.Sum(p => p.PaidPiastres),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:351:64:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'purchases.Sum(p => p.TotalPiastres), purchases.Sum(p => p.PaidPiastres),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:352:31:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'owedEmployees.Sum(r => r.Balance), owedEmployees.Count,'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:353:33:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'overdueInvoices.Sum(p => p.RemainingAmountPiastres), overdueInvoices.Count,'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:354:37:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline aggregation lambda `p => p.RemainingAmountPiastres`. Sums remaining unpaid amounts for invoices lacking explicit due dates.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:355:66:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'overdueInvoices.Count == 0 ? 0 : overdueInvoices.Max(p => p.Days!.Value));'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:358:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadPeopleAsync(Falah.Domain.Common.BusinessDateRange, long, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Queries `AttendanceRecords` within `[range.From, range.To]`. Aggregates workforce metrics (total records, Present, Late, Absent, MissingFingerprint), sums late minutes, and binds period posted wages to construct `PeopleDto`.
  ```csharp
  // Lines 358-372: Workforce attendance metrics
  private async Task<PeopleDto> LoadPeopleAsync(BusinessDateRange range, long wages, CancellationToken ct)
  {
      var records = await _db.AttendanceRecords.AsNoTracking()
          .Where(a => a.Date >= range.From && a.Date <= range.To)
          .Select(a => new { a.Status, a.LateMinutes }).ToListAsync(ct);
      ...
  }
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:361:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(a => a.Date >= range.From && a.Date <= range.To)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:362:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(a => new { a.Status, a.LateMinutes }).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:366:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'records.Count(a => a.Status == nameof(AttendanceStatus.Present)),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:367:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'records.Count(a => a.Status == nameof(AttendanceStatus.Late)),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:368:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'records.Count(a => a.Status == nameof(AttendanceStatus.Absent)),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:369:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'records.Count(a => a.Status == nameof(AttendanceStatus.MissingFingerprint)),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:370:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'records.Sum(a => a.LateMinutes),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:374:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadHealthAsync(CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Evaluates database backup freshness and print route telemetry: queries `BackupRecords` for latest successful backup timestamp, calculates hours elapsed, and counts failed backups. For each route in `Enum.GetNames<PrintRoute>()`, queries `PrintJobs` and `PrintJobAttempts` to compute pending count, maximum queue wait time, last success/failure timestamps, and failed job count.
  ```csharp
  // Lines 374-419: Operational health query
  private async Task<SystemHealthDto> LoadHealthAsync(CancellationToken ct)
  {
      var backups = await _db.BackupRecords.AsNoTracking()
          .Where(b => b.Kind == nameof(Persistence.Entities.BackupRecordKind.Backup))
          .Select(b => new { b.Status, b.CompletedAtUtc, b.VerificationStatus })
          .ToListAsync(ct);
      ...
  }
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:377:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(b => b.Kind == nameof(Persistence.Entities.BackupRecordKind.Backup))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:378:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(b => new { b.Status, b.CompletedAtUtc, b.VerificationStatus })'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:382:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `b => b.Status == nameof(Persistence.Entities.BackupRecordStatus.Succeeded) && b.CompletedAtUtc != null`. Identifies completed successful database backups.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:383:32:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.OrderByDescending(b => b.CompletedAtUtc)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:390:65:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline filter lambda `j => j.Route == route`. Filters print jobs belonging to the current print route under evaluation.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:391:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(j => new { j.Id, j.Status, j.CreatedAtUtc }).ToListAsync(ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:392:38:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `j => j.Status == nameof(PrintJobStatus.Pending)`. Filters print jobs currently awaiting physical spooler submission.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:393:38:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var jobIds = jobs.Select(j => j.Id).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:396:24:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(a => jobIds.Contains(a.PrintJobId))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:397:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(a => new { a.Succeeded, a.AttemptedAtUtc, a.Error })'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:400:46:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `a => a.AttemptedAtUtc`. Orders print attempts to identify the most recent successful print event.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:400:82:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `a => a.AttemptedAtUtc`. Orders print attempts to identify the most recent successful print event.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:401:46:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `a => a.AttemptedAtUtc`. Orders failed print attempts to identify the most recent error timestamp.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:401:83:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `a => a.AttemptedAtUtc`. Orders failed print attempts to identify the most recent error timestamp.

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:406:67:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `p => p.CreatedAtUtc`. Determines oldest pending print job timestamp to calculate queue latency in minutes.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:410:28:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'jobs.Count(j => j.Status == nameof(PrintJobStatus.Failed))));'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


## Canonical Callable ID: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:417:27:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `b => b.Status == nameof(Persistence.Entities.BackupRecordStatus.Failed)`. Counts failed database backup attempts in history.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`
- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == "Out"` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.


---
## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:23:00Z
- **Source SHA-256 Verified:** `67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29`<br>- **Callable ID Checked:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:1:1:File` (All 114 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`.
  - Exhaustively reviewed all 114 callables and confirmed exact canonical IDs, line numbers, and return types.
  - Validated critical finding **FIN-013**: Sign inversion in `LoadStockAsync` summing `QuantityMilliUnits` unsigned (`Direction == " Out\` increases balance instead of reducing it, contrasting with `InventoryQueryService` correct signed summation).
 - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T08:30:00Z
- **Source SHA-256 Verified:** 67C66A18A228C9E34B3D42AAD4C609F75C5AE0BDE36D87E8FABC366D8119BD29
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:** 
  - Exhaustively audited all 114 callables in OwnerDashboardQueryService.cs.
  - Double-challenged L1 review and confirmed finding **FIN-013**: sign inversion in LoadStockAsync summing QuantityMilliUnits unsigned (Direction == 'Out' adds instead of subtracting), contrasting with InventoryQueryService correct signed accumulation.
  - Verified financial trend aggregations, daily series serialization, and parameter bounds across all helper methods.
  - No modifications made to src/ or 	ests/.

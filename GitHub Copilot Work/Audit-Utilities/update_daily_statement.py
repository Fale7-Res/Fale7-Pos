# -*- coding: utf-8 -*-
import os
import re

review_path = r"c:\Users\Mohamed\Desktop\Falah-Restaurant-CSharp\GitHub Copilot Work\02-Callable-Inventory-and-Reviews\File-Reviews\src\Falah.Infrastructure\Reports\DailyStatementQueryService.cs.md"

with open(review_path, 'r', encoding='utf-8') as f:
    content = f.read()

replacements = {
    # 1. Constructor (lines 45-58)
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.DailyStatementQueryService\.DailyStatementQueryService\(.*?\) across lines 45-58\.":
        "- **Behavior**: Injects eight foundational services (`FalahDbContext`, `ReportingQueryService`, `InventoryQueryService`, `ControlCenterQueryService`, `AuditQueryService`, `PrintJobQueryService`, `BackupQueryService`, `EmployeeQueryService`) to compose the comprehensive daily statement ('كشف اليوم'). Maintains pure composition with zero independent mutable state.\n"
        "  ```csharp\n"
        "  // Lines 45-58: Constructor dependency injection\n"
        "  public DailyStatementQueryService(\n"
        "      FalahDbContext db, ReportingQueryService reports, InventoryQueryService inventory,\n"
        "      ControlCenterQueryService controlCenter, AuditQueryService audit, PrintJobQueryService printJobs,\n"
        "      BackupQueryService backups, EmployeeQueryService employees)\n"
        "  {\n"
        "      _db = db;\n"
        "      _reports = reports;\n"
        "      _inventory = inventory;\n"
        "      _controlCenter = controlCenter;\n"
        "      _audit = audit;\n"
        "      _printJobs = printJobs;\n"
        "      _backups = backups;\n"
        "      _employees = employees;\n"
        "  }\n"
        "  ```",

    # 2. GetAsync (lines 60-90)
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.DailyStatementQueryService\.GetAsync\(DateOnly, CancellationToken\) across lines 60-90\.":
        "- **Behavior**: Orchestrates daily business reporting by aggregating 15 sub-queries into a unified `DailyStatementDto`: converts `businessDate` to a single-day `BusinessDateRange` and computes UTC bounds `[start, end)`. Retrieves `BusinessDayEntity` status; fetches sales summary, VAT tax analysis, sold products breakdown, and hourly distribution from `_reports`. Builds expense line details (`BuildExpenseLinesAsync`), shift reconciliations and totals (`BuildShiftTotals`), treasury movements (`LoadTreasuryMovementsAsync`), all-time treasury balances (`LoadCurrentTreasuryBalancesAsync`), workforce ledger metrics (`BuildEmployeeRowsAsync`), inventory and loss metrics (`BuildInventoryAsync`), delivery outcomes, refund activity, unified audit trail (`BuildAuditTrailAsync`), print queue telemetry (`BuildPrintingAsync`), and backup status (`BuildBackupAsync`).\n"
        "  ```csharp\n"
        "  // Lines 60-90: Daily statement assembly\n"
        "  public async Task<DailyStatementDto> GetAsync(DateOnly businessDate, CancellationToken ct = default)\n"
        "  {\n"
        "      var range = BusinessDateRange.SingleDay(businessDate);\n"
        "      var (start, end) = range.ToUtcRange();\n"
        "      var day = await _db.BusinessDays.AsNoTracking().FirstOrDefaultAsync(d => d.BusinessDate == businessDate, ct);\n"
        "      ...\n"
        "  }\n"
        "  ```\n"
        "  - **Financial & Transaction Integrity**: Issues uncoordinated independent queries across EF Core context without an ambient transaction or snapshot isolation.",

    # 3. Lambda 65:77
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 65-65\.":
        "- **Behavior**: Inline predicate lambda `d => d.BusinessDate == businessDate`. Matches database business day record for the requested date.",

    # 4. BuildExpenseLinesAsync (lines 92-103)
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.DailyStatementQueryService\.BuildExpenseLinesAsync\(DateTimeOffset, DateTimeOffset, CancellationToken\) across lines 92-103\.":
        "- **Behavior**: Queries `Expenses` rows where `CreatedAtUtc >= start && CreatedAtUtc < end` ordered descending by creation date. Batches distinct `CreatedByUserId`s and maps user names from `Users` table in a single lookup to eliminate N+1 user resolution overhead. Projects rows into `DailyExpenseLineDto`.\n"
        "  ```csharp\n"
        "  // Lines 92-103: Daily expense lines query with user resolution\n"
        "  private async Task<List<DailyExpenseLineDto>> BuildExpenseLinesAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)\n"
        "  {\n"
        "      var rows = await _db.Expenses.AsNoTracking()\n"
        "          .Where(e => e.CreatedAtUtc >= start && e.CreatedAtUtc < end)\n"
        "          .OrderByDescending(e => e.CreatedAtUtc).ToListAsync(ct);\n"
        "      var userIds = rows.Select(e => e.CreatedByUserId).Distinct();\n"
        "      var userNames = await _db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, ct);\n"
        "      return rows.Select(e => new DailyExpenseLineDto(...)).ToList();\n"
        "  }\n"
        "  ```",

    # 5. Lambda 98:62
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 98-98\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True\}":
        "- **Behavior**: Inline predicate lambda `u => userIds.Contains(u.Id)`. Filters `Users` table to resolve user names for expense creators.",

    # 6. Lambda 98:109
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 98-98\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True\}":
        "- **Behavior**: Inline selector lambda `u => u.Id`. Selects user identifier as dictionary key.",

    # 7. Lambda 98:120
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 98-98\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True\}":
        "- **Behavior**: Inline selector lambda `u => u.Name`. Selects user name as dictionary value.",

    # 8. Lambda 100:28
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 100-102\.":
        "- **Behavior**: Inline projection lambda mapping `ExpenseEntity` to `DailyExpenseLineDto`, resolving user name with fallback to `CreatedByUserId`.",

    # 9. BuildShiftTotals (lines 105-111)
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.DailyStatementQueryService\.BuildShiftTotals\(IReadOnlyList<Falah\.Application\.Reports\.ShiftSummaryDto>\) across lines 105-111\.":
        "- **Behavior**: Pure aggregation method computing shift cash figures: total shifts, opening cash sum, expected cash sum (coercing nulls to 0), actual cash counted, absolute cash shortages (`(CashDifferencePiastres ?? 0) < 0`), and cash overages (`(CashDifferencePiastres ?? 0) > 0`).\n"
        "  ```csharp\n"
        "  // Lines 105-111: Shift totals aggregation\n"
        "  private static ShiftDayTotalsDto BuildShiftTotals(IReadOnlyList<ShiftSummaryDto> shifts) => new(\n"
        "      shifts.Count,\n"
        "      shifts.Sum(s => s.OpeningCashPiastres),\n"
        "      shifts.Sum(s => s.ExpectedCashPiastres ?? 0),\n"
        "      shifts.Sum(s => s.ActualCashPiastres ?? 0),\n"
        "      shifts.Where(s => (s.CashDifferencePiastres ?? 0) < 0).Sum(s => Math.Abs(s.CashDifferencePiastres ?? 0)),\n"
        "      shifts.Where(s => (s.CashDifferencePiastres ?? 0) > 0).Sum(s => s.CashDifferencePiastres ?? 0));\n"
        "  ```",

    # 10. Lambda 110:22
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 110-110\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline predicate lambda `s => (s.CashDifferencePiastres ?? 0) < 0`. Filters shifts exhibiting cash shortages.",

    # 11. Lambda 110:68
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 110-110\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline selector lambda `s => Math.Abs(s.CashDifferencePiastres ?? 0)`. Converts negative cash discrepancy to positive magnitude for aggregation.",

    # 12. Lambda 111:22
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 111-111\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline predicate lambda `s => (s.CashDifferencePiastres ?? 0) > 0`. Filters shifts exhibiting cash surpluses.",

    # 13. Lambda 111:68
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 111-111\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline selector lambda `s => s.CashDifferencePiastres ?? 0`. Selects positive cash surplus amount for aggregation.",

    # 14. LoadTreasuryMovementsAsync (lines 117-136)
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.DailyStatementQueryService\.LoadTreasuryMovementsAsync\(DateTimeOffset, DateTimeOffset, CancellationToken\) across lines 117-136\.":
        "- **Behavior**: Queries `TreasuryMovements` logged within `[start, end)`. Batches distinct `ReferenceId` values where `ReferenceType == \"order\"` and queries `Orders` table to map human-readable order numbers (`#0001`) onto movements, avoiding N+1 roundtrips. Projects rows into `TreasuryMovementDto`.\n"
        "  ```csharp\n"
        "  // Lines 117-136: Day-scoped treasury movements query\n"
        "  private async Task<List<TreasuryMovementDto>> LoadTreasuryMovementsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)\n"
        "  {\n"
        "      var rows = await _db.TreasuryMovements.AsNoTracking()\n"
        "          .Where(m => m.CreatedAtUtc >= start && m.CreatedAtUtc < end)\n"
        "          .OrderByDescending(m => m.CreatedAtUtc).ToListAsync(ct);\n"
        "      var orderIds = rows.Where(m => m.ReferenceType == \"order\" && !string.IsNullOrEmpty(m.ReferenceId))\n"
        "          .Select(m => m.ReferenceId).Distinct().ToList();\n"
        "      var orderNumbers = orderIds.Count == 0\n"
        "          ? new Dictionary<string, string>()\n"
        "          : await _db.Orders.AsNoTracking().Where(o => orderIds.Contains(o.Id))\n"
        "              .ToDictionaryAsync(o => o.Id, o => o.OrderNumber, ct);\n"
        "      ...\n"
        "  }\n"
        "  ```",

    # 15. Lambda 125:35
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 125-125\.":
        "- **Behavior**: Inline predicate lambda `m => m.ReferenceType == \"order\" && !string.IsNullOrEmpty(m.ReferenceId)`. Filters movements linked to order lifecycle events.",

    # 16. Lambda 129:53
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 129-129\.":
        "- **Behavior**: Inline predicate lambda `o => orderIds.Contains(o.Id)`. Batches order lookups to resolve order numbers for treasury movements.",

    # 17. Lambda 132:28
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 132-135\.":
        "- **Behavior**: Inline projection lambda mapping `TreasuryMovementEntity` to `TreasuryMovementDto`, attaching human-readable order numbers.",

    # 18. LoadCurrentTreasuryBalancesAsync (lines 141-150)
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.DailyStatementQueryService\.LoadCurrentTreasuryBalancesAsync\(CancellationToken\) across lines 141-150\.":
        "- **Behavior**: Evaluates all-time posted `TreasuryMovements` across entire system lifetime (`AsNoTracking()`). Uses local helper `Balance(accountId)` to compute live ledger balances for Main, Drawer, Bank, InstaPay, and Wallet accounts. Represents current liquid assets.\n"
        "  ```csharp\n"
        "  // Lines 141-150: Live treasury running balances\n"
        "  private async Task<TreasuryBalancesDto> LoadCurrentTreasuryBalancesAsync(CancellationToken ct)\n"
        "  {\n"
        "      var movements = await _db.TreasuryMovements.AsNoTracking()\n"
        "          .Where(m => m.Status == nameof(MovementStatus.Posted))\n"
        "          .Select(m => new { m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres }).ToListAsync(ct);\n"
        "      long Balance(string accountId) => movements.Sum(m =>\n"
        "          (m.DestinationAccountId == accountId ? m.AmountPiastres : 0) - (m.SourceAccountId == accountId ? m.AmountPiastres : 0));\n"
        "      return new TreasuryBalancesDto(Balance(TreasuryAccounts.Main), Balance(TreasuryAccounts.Drawer),\n"
        "          Balance(TreasuryAccounts.Bank), Balance(TreasuryAccounts.InstaPay), Balance(TreasuryAccounts.Wallet));\n"
        "  }\n"
        "  ```\n"
        "  - **Scalability Risk**: In-memory summation across all posted treasury movements without database-level `GROUP BY`.",

    # 19. Lambda 145:21
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 145-145\.":
        "- **Behavior**: Inline projection lambda `m => new { m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres }`. Extracts only source, destination, and amount for balance calculation.",

    # 20. Local function Balance
    r"- \*\*Behavior\*\*: Executes logic for Balance\(string\) across lines 146-147\.":
        "- **Behavior**: Local helper computing running balance for an account by summing inflows and subtracting outflows across posted movements.",

    # 21. Lambda 146:57
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 146-147\.":
        "- **Behavior**: Inline aggregation lambda `m => (m.DestinationAccountId == accountId ? m.AmountPiastres : 0) - (m.SourceAccountId == accountId ? m.AmountPiastres : 0)`. Sums signed delta for account.",

    # 22. BuildEmployeeRowsAsync (lines 155-187)
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.DailyStatementQueryService\.BuildEmployeeRowsAsync\(DateOnly, DateTimeOffset, DateTimeOffset, CancellationToken\) across lines 155-187\.":
        "- **Behavior**: Queries employees and daily attendance for `businessDate`. Loads employee ledger entries within `[start, end)`. Groups entries by `EmployeeId`. For each employee, calculates daily credits (wages, overtime, bonuses) and debits (deductions, advances, withdrawals, payroll settlements) via local functions `SumCredit` and `SumDebit`. Calls `_employees.GetBalanceAsync` to attach live running balance.\n"
        "  ```csharp\n"
        "  // Lines 155-187: Employee attendance and labor ledger synthesis\n"
        "  private async Task<List<DailyEmployeeRowDto>> BuildEmployeeRowsAsync(\n"
        "      DateOnly businessDate, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)\n"
        "  {\n"
        "      var employees = await _employees.ListAsync(ct);\n"
        "      var attendance = await _db.AttendanceRecords.AsNoTracking()\n"
        "          .Where(a => a.Date == businessDate).ToListAsync(ct);\n"
        "      ...\n"
        "  }\n"
        "  ```\n"
        "  - **Performance N+1 Pattern**: Line 174 executes `await _employees.GetBalanceAsync(employee.Id, ct)` inside a `foreach` loop over all employees, generating N separate database queries.",

    # 23. Lambda 165:51
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 165-165\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True\}":
        "- **Behavior**: Inline grouping key selector `l => l.EmployeeId`. Groups ledger rows by employee identifier.",

    # 24. Lambda 165:83
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 165-165\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True\}":
        "- **Behavior**: Inline dictionary key selector `g => g.Key`. Binds employee ID as dictionary key.",

    # 25. Lambda 165:95
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 165-165\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True\}":
        "- **Behavior**: Inline dictionary value selector `g => g.ToList()`. Groups ledger entries list per employee.",

    # 26. Local function SumCredit
    r"- \*\*Behavior\*\*: Executes logic for SumCredit\(string\) across lines 172-172\.":
        "- **Behavior**: Local helper filtering employee ledger entries by specified credit type and summing `CreditPiastres`.",

    # 27. Lambda 172:58
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 172-172\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline predicate lambda `l => l.Type == type`. Filters employee ledger entries by credit entry type.",

    # 28. Lambda 172:83
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 172-172\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline selector lambda `l => l.CreditPiastres`. Selects credited wage amount for summation.",

    # 29. Local function SumDebit
    r"- \*\*Behavior\*\*: Executes logic for SumDebit\(string\) across lines 173-173\.":
        "- **Behavior**: Local helper filtering employee ledger entries by specified debit type and summing `DebitPiastres`.",

    # 30. Lambda 173:57
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 173-173\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline predicate lambda `l => l.Type == type`. Filters employee ledger entries by debit entry type.",

    # 31. Lambda 173:82
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 173-173\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False\}":
        "- **Behavior**: Inline selector lambda `l => l.DebitPiastres`. Selects debited payout/settlement amount for summation.",

    # 32. BuildInventoryAsync (lines 192-217)
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.DailyStatementQueryService\.BuildInventoryAsync\(DateOnly, DateTimeOffset, DateTimeOffset, CancellationToken\) across lines 192-217\.":
        "- **Behavior**: Synthesizes daily inventory movements, losses, and stock positions: queries `StockMovements` within `[start, end)`. Loads active packaging IDs into a `HashSet`. Uses local helper `SumQuantity` to aggregate usage for ingredients vs packaging, receipts, waste, spoilage, and breakage. Evaluates physical count variance via `NetAdjustment()`. Calls `_inventory.GetLossSummaryAsync` (inheriting finding INV-FIN-001), sums purchase invoice totals, and queries `_inventory.GetInventoryOverviewAsync` for low-stock and uncosted item counts.\n"
        "  ```csharp\n"
        "  // Lines 192-217: Daily inventory activity and loss compilation\n"
        "  private async Task<DailyInventoryDto> BuildInventoryAsync(DateOnly businessDate, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)\n"
        "  {\n"
        "      var movements = await _db.StockMovements.AsNoTracking()\n"
        "          .Where(m => m.CreatedAtUtc >= start && m.CreatedAtUtc < end)\n"
        "          .Select(m => new { m.InventoryItemId, m.Type, m.Direction, m.QuantityMilliUnits }).ToListAsync(ct);\n"
        "      var packagingIds = (await _db.InventoryItems.AsNoTracking().Where(i => i.IsPackaging).Select(i => i.Id).ToListAsync(ct)).ToHashSet();\n"
        "      ...\n"
        "  }\n"
        "  ```",

    # 33. Lambda 196:21
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 196-196\.":
        "- **Behavior**: Inline projection lambda `m => new { m.InventoryItemId, m.Type, m.Direction, m.QuantityMilliUnits }`. Extracts core stock movement fields for in-memory categorization.",

    # 34. Lambda 197:75
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 197-197\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True\}":
        "- **Behavior**: Inline predicate lambda `i => i.IsPackaging`. Filters inventory items designated as packaging materials.",

    # 35. Lambda 197:102
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 197-197\.\s+- \*\*Validations\*\*: Validation checks detected: False\. Throws: None detected in body\.\s+- \*\*Risk Profile\*\*: \{'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True\}":
        "- **Behavior**: Inline selector lambda `i => i.Id`. Extracts packaging inventory item ID for hash set membership testing.",

    # 36. Local function SumQuantity
    r"- \*\*Behavior\*\*: Executes logic for SumQuantity\(Falah\.Domain\.Inventory\.StockMovementType, bool\?\) across lines 199-201\.":
        "- **Behavior**: Local helper filtering loaded movements by `StockMovementType` and packaging flag, summing `QuantityMilliUnits`.",

    # 37. Lambda 200:20
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 200-200\.":
        "- **Behavior**: Inline predicate lambda `m => m.Type == type.ToString() && (isPackaging == null || packagingIds.Contains(m.InventoryItemId) == isPackaging)`. Segregates ingredient usage from packaging usage.",

    # 38. Local function NetAdjustment
    r"- \*\*Behavior\*\*: Executes logic for NetAdjustment\(\) across lines 202-203\.":
        "- **Behavior**: Local helper summing signed physical count corrections (`Direction == In ? Quantity : -Quantity`) to determine net unexplained inventory count drift.",

    # 39. Lambda 202:49
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 202-202\.":
        "- **Behavior**: Inline predicate lambda `m => m.Type == nameof(StockMovementType.CountAdjustment)`. Filters movements resulting from physical stock counts.",

    # 40. Lambda 203:18
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 203-203\.":
        "- **Behavior**: Inline aggregation lambda `m => m.Direction == nameof(StockDirection.In) ? m.QuantityMilliUnits : -m.QuantityMilliUnits`. Correctly negates outbound count adjustments while adding inbound adjustments.",

    # 41. BuildAuditTrailAsync (lines 224-238)
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.DailyStatementQueryService\.BuildAuditTrailAsync\(DateTimeOffset, DateTimeOffset, CancellationToken\) across lines 224-238\.":
        "- **Behavior**: Compiles a unified audit timeline for the day by querying both general operational audit events (`_audit.ListAsync`) and control center administrative events (`_controlCenter.ListAuditEventsAsync`) within `[start, end)`. Normalizes records into `DailyAuditEntryDto` ordered descending by creation timestamp.\n"
        "  ```csharp\n"
        "  // Lines 224-238: Unified audit trail synthesis\n"
        "  private async Task<List<DailyAuditEntryDto>> BuildAuditTrailAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)\n"
        "  {\n"
        "      var generalEvents = await _audit.ListAsync(null, null, null, start, end, limit: 500, ct: ct);\n"
        "      var controlEvents = await _controlCenter.ListAuditEventsAsync(limit: 500, from: start, to: end, ct: ct);\n"
        "      var rows = new List<DailyAuditEntryDto>(generalEvents.Count + controlEvents.Count);\n"
        "      rows.AddRange(generalEvents.Select(e => new DailyAuditEntryDto(\"audit_event\", ...)));\n"
        "      rows.AddRange(controlEvents.Select(e => new DailyAuditEntryDto(\"control_audit_event\", ...)));\n"
        "      return rows.OrderByDescending(r => r.CreatedAtUtc).ToList();\n"
        "  }\n"
        "  ```",

    # 42. Lambda 230:44
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 230-232\.":
        "- **Behavior**: Inline projection lambda mapping `AuditEvent` to unified `DailyAuditEntryDto` with source tag `\"audit_event\"`.",

    # 43. Lambda 233:44
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 233-235\.":
        "- **Behavior**: Inline projection lambda mapping `ControlAuditEventEntity` to unified `DailyAuditEntryDto` with source tag `\"control_audit_event\"`.",

    # 44. BuildPrintingAsync (lines 240-246)
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.DailyStatementQueryService\.BuildPrintingAsync\(DateTimeOffset, DateTimeOffset, CancellationToken\) across lines 240-246\.":
        "- **Behavior**: Queries print telemetry from `_printJobs.GetOverviewAsync` and counts order ticket reprints from `AuditEvents` where `Category == \"order\" && Action == \"إعادة طباعة طلب\"` within `[start, end)`. Assembles `DailyPrintingDto`.\n"
        "  ```csharp\n"
        "  // Lines 240-246: Print queue and reprint telemetry\n"
        "  private async Task<DailyPrintingDto> BuildPrintingAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)\n"
        "  {\n"
        "      var overview = await _printJobs.GetOverviewAsync(ct);\n"
        "      var reprintCount = await _db.AuditEvents.AsNoTracking()\n"
        "          .CountAsync(e => e.Category == \"order\" && e.Action == \"إعادة طباعة طلب\" && e.TimestampUtc >= start && e.TimestampUtc < end, ct);\n"
        "      return new DailyPrintingDto(overview.TotalPendingCount, overview.TotalFailedCount, overview.TotalStuckCount, reprintCount, overview.Routes);\n"
        "  }\n"
        "  ```",

    # 45. Lambda 244:25
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 244-244\.":
        "- **Behavior**: Inline predicate lambda `e => e.Category == \"order\" && e.Action == \"إعادة طباعة طلب\" && e.TimestampUtc >= start && e.TimestampUtc < end`. Identifies order ticket reprint events.",

    # 46. BuildBackupAsync (lines 248-261)
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.DailyStatementQueryService\.BuildBackupAsync\(DateTimeOffset, DateTimeOffset, CancellationToken\) across lines 248-261\.":
        "- **Behavior**: Assesses backup operations: retrieves all backup records from `_backups.ListAsync`, identifies the most recent valid backup (`Status == \"Succeeded\" && VerificationStatus == \"Valid\"`), computes elapsed age in hours, and counts total and failed backups requested within `[start, end)`.\n"
        "  ```csharp\n"
        "  // Lines 248-261: Daily backup verification and health query\n"
        "  private async Task<DailyBackupDto> BuildBackupAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)\n"
        "  {\n"
        "      var all = await _backups.ListAsync(ct);\n"
        "      var backups = all.Where(b => b.Kind == \"Backup\").ToList();\n"
        "      var lastVerified = backups.Where(b => b.Status == \"Succeeded\" && b.VerificationStatus == \"Valid\" && b.FileSizeBytes != null)\n"
        "          .OrderByDescending(b => b.CompletedAtUtc).FirstOrDefault();\n"
        "      ...\n"
        "  }\n"
        "  ```",

    # 47. Lambda 252:42
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 252-252\.":
        "- **Behavior**: Inline predicate lambda `b => b.Status == \"Succeeded\" && b.VerificationStatus == \"Valid\" && b.FileSizeBytes != null`. Identifies valid verified backup files.",

    # 48. Lambda 256:35
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 256-256\.":
        "- **Behavior**: Inline predicate lambda `b => b.RequestedAtUtc >= start && b.RequestedAtUtc < end`. Filters backup requests made during the requested business date."
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
print(f"DailyStatement applied: {applied}/{len(replacements)}. Remaining generic entries: {remaining_generic}")

with open(review_path, 'w', encoding='utf-8') as f:
    f.write(updated_content)
print("Updated DailyStatementQueryService.cs.md successfully.")

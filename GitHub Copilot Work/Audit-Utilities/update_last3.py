# -*- coding: utf-8 -*-
import re

review_path = r"c:\Users\Mohamed\Desktop\Falah-Restaurant-CSharp\GitHub Copilot Work\02-Callable-Inventory-and-Reviews\File-Reviews\src\Falah.Infrastructure\Reports\OwnerDashboardQueryService.cs.md"
with open(review_path, 'r', encoding='utf-8') as f:
    text = f.read()

replacements = {
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.LoadSettledFromDriversAsync\(DateTimeOffset, DateTimeOffset, CancellationToken\) across lines 230-233\.":
        "- **Behavior**: Sums `AmountPiastres` of `TreasuryMovements` with `Type == \"driver_settlement\"` within `[start, end)`. Reconciles cash-on-delivery payments received from drivers back to the restaurant treasury.\n  ```csharp\n  // Lines 230-233: Driver settlement reconciliation\n  private async Task<long> LoadSettledFromDriversAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct) =>\n      await _db.TreasuryMovements.AsNoTracking()\n          .Where(m => m.Type == \"driver_settlement\" && m.CreatedAtUtc >= start && m.CreatedAtUtc < end)\n          .SumAsync(m => (long?)m.AmountPiastres, ct) ?? 0;\n  ```",
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.LoadObligationsAsync\(DateTimeOffset, DateTimeOffset, CancellationToken\) across lines 317-356\.":
        "- **Behavior**: Aggregates outstanding financial liabilities: calculates net balances owed to suppliers from `SupplierLedgerEntries` (`DebitPiastres - CreditPiastres`), aggregates purchases and paid amounts, calculates employee wage liabilities (`CreditPiastres - DebitPiastres`), and calculates overdue accounts payable aging using `PayablesAging.DaysOverdue` against `BusinessClock.Today()`.\n  ```csharp\n  // Lines 317-356: Liabilities and payables aging\n  private async Task<ObligationsDto> LoadObligationsAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)\n  {\n      var supplierRows = await _db.SupplierLedgerEntries.AsNoTracking()\n          .GroupBy(e => e.SupplierId)\n          .Select(g => new { SupplierId = g.Key, Outstanding = g.Sum(e => e.DebitPiastres - e.CreditPiastres) })\n          .ToListAsync(ct);\n      ...\n  }\n  ```",
    r"- \*\*Behavior\*\*: Executes logic for Falah\.Infrastructure\.Reports\.OwnerDashboardQueryService\.LoadPeopleAsync\(Falah\.Domain\.Common\.BusinessDateRange, long, CancellationToken\) across lines 358-372\.":
        "- **Behavior**: Queries `AttendanceRecords` within `[range.From, range.To]`. Aggregates workforce metrics (total records, Present, Late, Absent, MissingFingerprint), sums late minutes, and binds period posted wages to construct `PeopleDto`.\n  ```csharp\n  // Lines 358-372: Workforce attendance metrics\n  private async Task<PeopleDto> LoadPeopleAsync(BusinessDateRange range, long wages, CancellationToken ct)\n  {\n      var records = await _db.AttendanceRecords.AsNoTracking()\n          .Where(a => a.Date >= range.From && a.Date <= range.To)\n          .Select(a => new { a.Status, a.LateMinutes }).ToListAsync(ct);\n      ...\n  }\n  ```"
}

for pat, repl in replacements.items():
    text = re.sub(pat, repl, text)

with open(review_path, 'w', encoding='utf-8') as f:
    f.write(text)

remaining = len(re.findall(r"Executes logic for.*", text))
print(f"Remaining generic entries in OwnerDashboardQueryService.cs.md: {remaining}")

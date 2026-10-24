# src/Falah.Application/Reports/ReportingDtos.cs

Reports API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `0a615dd0d71fc11bd9382cf3d832a4e116620c4b13b804a2431458cfb1f6d160`. Working SHA-256 `39c9da5ec2dfc6012cf6b630b0f9d8a8924c54016c991ceacaad1632ec308726`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-100.

Symbols indexed (index is not a claim every method was reviewed): SalesReportDto@11, ExpenseReportDto@18, TaxReportDto@20, RefundActivityDto@22, TreasuryBalancesDto@30, BusinessDayReconciliationDto@32, EmployeeLedgerTotalsDto@42, DailySummaryDto@48, TopSellingItemDto@53, ProductHourlySalesDto@63, CategorySalesDto@65, FoodCostReportDto@70, SignificantActionDto@75, ShiftSummaryDto@79, DeliveryOutcomesReportDto@97.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 11: `public sealed record SalesReportDto(`
- Line 18: `public sealed record ExpenseReportDto(long TotalPiastres, IReadOnlyList<Falah.Application.Expenses.ExpenseDto> Expenses);`
- Line 20: `public sealed record TaxReportDto(long GrossPiastres, long TaxablePiastres, long TaxPiastres, long RefundTaxImpactPiastres, long NetTaxPiastres);`
- Line 22: `public sealed record RefundActivityDto(`
- Line 30: `public sealed record TreasuryBalancesDto(long MainPiastres, long DrawerPiastres, long BankPiastres, long InstaPayPiastres, long WalletPiastres);`
- Line 32: `public sealed record BusinessDayReconciliationDto(`
- Line 42: `public sealed record EmployeeLedgerTotalsDto(long DailyWagePiastres, long OvertimePiastres, long AdvancePiastres, long WithdrawalPiastres);`
- Line 48: `public sealed record DailySummaryDto(`
- Line 53: `public sealed record TopSellingItemDto(string ProductName, int Quantity, long TotalPiastres);`
- Line 63: `public sealed record ProductHourlySalesDto(string ProductName, int Hour, int Quantity, long TotalPiastres);`
- Line 65: `public sealed record CategorySalesDto(string CategoryId, string CategoryName, int Quantity, long TotalPiastres);`
- Line 70: `public sealed record FoodCostReportDto(long TotalFoodCostPiastres, int SoldLineCount, int KnownLineCount, IReadOnlyList<string> Missing);`
- Line 75: `public sealed record SignificantActionDto(`
- Line 79: `public sealed record ShiftSummaryDto(`
- Line 97: `public sealed record DeliveryOutcomesReportDto(`

Read-only reports distinguish unknown cost/null percentages, current balances versus date-scoped flows, driver custody and pending electronics. Several comments claim missing payroll/purchasing despite existing projects: comments are stale evidence. Owner print-health comment claims physical success though agent only reports transport completion (PRT-002). Aggregate correctness lives outside DTOs.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

# src/Falah.Infrastructure/Employees/EmployeeQueryService.cs

**path**: src/Falah.Infrastructure/Employees/EmployeeQueryService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: fbb1200253239f442fdb0933abb5270f12c7d404e5cd549150c71a66c96c6341

**git_blob_sha256**: fbb1200253239f442fdb0933abb5270f12c7d404e5cd549150c71a66c96c6341

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 103]]

**functionality_and_reasoning**: Full103 lines read without truncation. Financial report active employees only though inactive may retain liabilities; loads all ledger every request and per-employee scans. Adjustment credits excluded from all earning/addition buckets but balance includes, so bucket reconciliation fails FIN-012. Balance intentionally alltime even range asked; historical statement not asof. No existence check balance =>unknown employee0. PII query gates in root endpoints.

**significant_methods**: Constructor; GetFinancialReportAsync; local SumCredit/SumDebit; ListAsync; GetAsync; ListLedgerEntriesAsync; GetBalanceAsync; ToDto; ToLedgerDto; ListAttendanceAsync; ToAttendanceDto

**interactions_and_risks**: Full103 lines read without truncation. Financial report active employees only though inactive may retain liabilities; loads all ledger every request and per-employee scans. Adjustment credits excluded from all earning/addition buckets but balance includes, so bucket reconciliation fails FIN-012. Balance intentionally alltime even range asked; historical statement not asof. No existence check balance =>unknown employee0. PII query gates in root endpoints.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared correction postings and payroll. No user records read.

**recommendations**: Explicit net Adjustment bucket and inactive outstanding inclusion; SQL grouped aggregate/date filtering; distinguish missing entity from zero.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.

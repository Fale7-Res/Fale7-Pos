# src/Falah.Domain/Employees/Employee.cs

**path**: src/Falah.Domain/Employees/Employee.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 78762ea2dbc46e2747db8c3bedbd6f771be2cd18fbe5fb4fee2644a1d944e64c

**git_blob_sha256**: 78762ea2dbc46e2747db8c3bedbd6f771be2cd18fbe5fb4fee2644a1d944e64c

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 52]]

**functionality_and_reasoning**: Full52 lines read. Separate compensation/employment/lifecycle and IsActive can disagree; handlers must synchronize. Hourly compensation enum exists but payroll CalculateLines only Monthly vs all others, Hourly effectively wage-credit path not rate-driven payroll. Ledger nonnegative credit/debit described but record no guard. Optional UserId no command linkage by design.

**significant_methods**: CompensationType; EmploymentArrangement; EmployeeLifecycleStatus; LedgerEntryType; Employee; EmployeeLedgerEntry

**interactions_and_risks**: Full52 lines read. Separate compensation/employment/lifecycle and IsActive can disagree; handlers must synchronize. Hourly compensation enum exists but payroll CalculateLines only Monthly vs all others, Hourly effectively wage-credit path not rate-driven payroll. Ledger nonnegative credit/debit described but record no guard. Optional UserId no command linkage by design.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared PayrollRules and employee payout handlers.

**recommendations**: Validate coherent lifecycle and compensated basis; explicit hourly support status instead of implying enum suffices.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.

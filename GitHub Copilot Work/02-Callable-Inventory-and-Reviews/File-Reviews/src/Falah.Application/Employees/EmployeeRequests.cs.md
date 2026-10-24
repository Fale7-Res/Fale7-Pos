# src/Falah.Application/Employees/EmployeeRequests.cs

Employees API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `61ce8fbbe0c380340ded6e3de6be6863c9b328c094894bd8e54d5f32e32efdc0`. Working SHA-256 `61ce8fbbe0c380340ded6e3de6be6863c9b328c094894bd8e54d5f32e32efdc0`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-78.

Symbols indexed (index is not a claim every method was reviewed): never@4, CreateEmployeeRequest@6, UpdateEmployeeRequest@16, EmployeeDto@23, EmployeeLedgerEntryDto@30, RecordManualLedgerEntryRequest@43, RecordEmployeeCashOutRequest@51, EmployeeCashOutResult@55, PayEmployeeRequest@64, PayEmployeeResult@68, EmployeeFinancialReportLineDto@76.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 6: `public sealed record CreateEmployeeRequest(`
- Line 16: `public sealed record UpdateEmployeeRequest(`
- Line 23: `public sealed record EmployeeDto(`
- Line 30: `public sealed record EmployeeLedgerEntryDto(`
- Line 43: `public sealed record RecordManualLedgerEntryRequest(`
- Line 51: `public sealed record RecordEmployeeCashOutRequest(`
- Line 55: `public sealed record EmployeeCashOutResult(string LedgerEntryId, long BalanceAfterPiastres, string? TreasuryMovementId, bool WasReplayed);`
- Line 64: `public sealed record PayEmployeeRequest(`
- Line 68: `public sealed record PayEmployeeResult(string LedgerEntryId, long BalanceAfterPiastres, string TreasuryMovementId, bool WasReplayed);`
- Line 76: `public sealed record EmployeeFinancialReportLineDto(`

Employee and attendance payloads include pay rates, biometrics identifiers, national ID and address; do not expose to every authenticated station. Integer money avoids floating currency, while hours/multipliers need finite/range checks. Payroll status contract accepts arbitrary string; FIN-006 independent trace confirms Paid must route through payment handler. Manual ledger entry intentionally lacks request ID: retry safety is not guaranteed by DTO.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

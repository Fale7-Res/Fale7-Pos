# src/Falah.Application/Employees/PayrollRequests.cs

Employees API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `8f229e3600888b01eb00735c83baf400be80501f8958b4d3deddf9bbc2d204dd`. Working SHA-256 `8f229e3600888b01eb00735c83baf400be80501f8958b4d3deddf9bbc2d204dd`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-35.

Symbols indexed (index is not a claim every method was reviewed): CreatePayrollRequest@10, PayrollRunDto@12, PayrollLineDto@17, TransitionPayrollRequest@26, PayPayrollRequest@33, PayPayrollResult@35.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 10: `public sealed record CreatePayrollRequest(string RequestId, string Period, string ActorUserId);`
- Line 12: `public sealed record PayrollRunDto(`
- Line 17: `public sealed record PayrollLineDto(`
- Line 26: `public sealed record TransitionPayrollRequest(string PayrollRunId, string Status, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);`
- Line 33: `public sealed record PayPayrollRequest(string PayrollRunId, string PaymentAccountId, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);`
- Line 35: `public sealed record PayPayrollResult(string TreasuryMovementId, long TotalPaidPiastres, int SettlementCount, bool WasReplayed);`

Employee and attendance payloads include pay rates, biometrics identifiers, national ID and address; do not expose to every authenticated station. Integer money avoids floating currency, while hours/multipliers need finite/range checks. Payroll status contract accepts arbitrary string; FIN-006 independent trace confirms Paid must route through payment handler. Manual ledger entry intentionally lacks request ID: retry safety is not guaranteed by DTO.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

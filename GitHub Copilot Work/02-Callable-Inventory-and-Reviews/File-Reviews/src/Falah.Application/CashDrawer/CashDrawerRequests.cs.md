# src/Falah.Application/CashDrawer/CashDrawerRequests.cs

CashDrawer API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `2017b7d096025be994e729fd69cf9446b30775b4aabaafa99b965bc49c08b157`. Working SHA-256 `2017b7d096025be994e729fd69cf9446b30775b4aabaafa99b965bc49c08b157`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-30.

Symbols indexed (index is not a claim every method was reviewed): OpenCashDrawerRequest@10, CashDrawerOpenDto@12, ReportCashDrawerResultRequest@17, CashierCashMovementRequest@26, CashierCashMovementResult@30.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 10: `public sealed record OpenCashDrawerRequest(string RequestId, string ShiftId, string Reason, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);`
- Line 12: `public sealed record CashDrawerOpenDto(string Id, string ShiftId, string Reason, string RequestedBy, string? ApprovedBy, string Status, string? LastError, DateTimeOffset RequestedAtUtc);`
- Line 17: `public sealed record ReportCashDrawerResultRequest(string CashDrawerOpenId, bool Succeeded, string? Error, string ReportedByUserId);`
- Line 26: `public sealed record CashierCashMovementRequest(`
- Line 30: `public sealed record CashierCashMovementResult(string TreasuryMovementId, string MovementType, long AmountPiastres, bool WasReplayed);`

Separates drawer-open intent/result from cash transfer. Actor and approving identity are different fields; hardware success is externally reported. Machine restriction is documented but not encoded. Treat reported success as an observation with provenance, not proof cash moved.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

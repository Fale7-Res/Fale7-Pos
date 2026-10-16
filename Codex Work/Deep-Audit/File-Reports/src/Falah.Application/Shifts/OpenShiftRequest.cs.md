# src/Falah.Application/Shifts/OpenShiftRequest.cs

Shifts API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `8c4a6da228e3ffed2e762157d7d035cf9c87f825be0e53049068aa861edcd8f7`. Working SHA-256 `8c4a6da228e3ffed2e762157d7d035cf9c87f825be0e53049068aa861edcd8f7`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-25.

Symbols indexed (index is not a claim every method was reviewed): OpenShiftRequest@5, OpenShiftResult@7, CurrentShiftDto@12, ShiftAlreadyOpenException@22.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 5: `public sealed record OpenShiftRequest(string RequestId, string ShiftName, string OpenedByUserId, long OpeningCashPiastres);`
- Line 7: `public sealed record OpenShiftResult(string ShiftId, string ShiftName, string Status, long OpeningCashPiastres, bool WasReplayed);`
- Line 12: `public sealed record CurrentShiftDto(string ShiftId, string ShiftName, string OpenedByUserId, long OpeningCashPiastres, DateTimeOffset OpenedAtUtc,`
- Line 24: `public ShiftAlreadyOpenException(string message) : base(message) { }`

Blind prepare-close is read-only, close is a separate keyed command with optional approval. ClosedByUserId is not in AuthenticationMiddleware actor field list; security coordinator should assess handler identity binding. CurrentShiftDto supports shared call-center shift lookup. Contract has no locking.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

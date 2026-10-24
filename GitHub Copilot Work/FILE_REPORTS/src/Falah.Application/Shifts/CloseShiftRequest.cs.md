# src/Falah.Application/Shifts/CloseShiftRequest.cs

Shifts API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `69006fb2c4bbe317d32b5a544d9e07e272ef5bf7e1b71a3fbf6c1d2baa8ff932`. Working SHA-256 `69006fb2c4bbe317d32b5a544d9e07e272ef5bf7e1b71a3fbf6c1d2baa8ff932`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-26.

Symbols indexed (index is not a claim every method was reviewed): PrepareShiftCloseRequest@5, PrepareShiftCloseResult@7, CloseShiftRequest@15, CloseShiftResult@19, ShiftCloseConflictException@23.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 5: `public sealed record PrepareShiftCloseRequest(string ShiftId, long ActualCashCountedPiastres);`
- Line 7: `public sealed record PrepareShiftCloseResult(`
- Line 15: `public sealed record CloseShiftRequest(`
- Line 19: `public sealed record CloseShiftResult(`
- Line 25: `public ShiftCloseConflictException(string message) : base(message) { }`

Blind prepare-close is read-only, close is a separate keyed command with optional approval. ClosedByUserId is not in AuthenticationMiddleware actor field list; security coordinator should assess handler identity binding. CurrentShiftDto supports shared call-center shift lookup. Contract has no locking.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

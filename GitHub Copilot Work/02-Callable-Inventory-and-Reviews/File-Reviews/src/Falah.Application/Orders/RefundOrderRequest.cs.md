# src/Falah.Application/Orders/RefundOrderRequest.cs

Orders API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `31ccd7179a21eb71ed5208ea6f04056eaf6518d75da1c408d2cf7dd22a028d10`. Working SHA-256 `31ccd7179a21eb71ed5208ea6f04056eaf6518d75da1c408d2cf7dd22a028d10`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-26.

Symbols indexed (index is not a claim every method was reviewed): RefundOrderRequest@5, RefundOrderResult@14, RefundConflictException@23.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 5: `public sealed record RefundOrderRequest(`
- Line 14: `public sealed record RefundOrderResult(`
- Line 25: `public RefundConflictException(string message) : base(message) { }`

Checkout contract creates and pays in one call, with electronic/COD deferral only in downstream rules; RequestedPickupAtUtc is metadata, not a reservation lifecycle. PrintJobsQueued is enqueue outcome only. Refund and confirmation have different idempotency keys. OrderDetailsDto is read-only and exposes frozen splits, pickup time and customer data; no API here settles cash pickup later.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

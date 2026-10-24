# src/Falah.Application/Orders/ConfirmElectronicPaymentRequest.cs

Orders API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `68e0c3ba5b014d60604f18bc199befd9e4210ef7f6b858fdbfbf73dd03849747`. Working SHA-256 `68e0c3ba5b014d60604f18bc199befd9e4210ef7f6b858fdbfbf73dd03849747`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-15.

Symbols indexed (index is not a claim every method was reviewed): ConfirmElectronicPaymentRequest@7, ConfirmElectronicPaymentResult@9, ConfirmConflictException@12.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 7: `public sealed record ConfirmElectronicPaymentRequest(string OrderId, string CashierId);`
- Line 9: `public sealed record ConfirmElectronicPaymentResult(`
- Line 14: `public ConfirmConflictException(string message) : base(message) { }`

Checkout contract creates and pays in one call, with electronic/COD deferral only in downstream rules; RequestedPickupAtUtc is metadata, not a reservation lifecycle. PrintJobsQueued is enqueue outcome only. Refund and confirmation have different idempotency keys. OrderDetailsDto is read-only and exposes frozen splits, pickup time and customer data; no API here settles cash pickup later.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

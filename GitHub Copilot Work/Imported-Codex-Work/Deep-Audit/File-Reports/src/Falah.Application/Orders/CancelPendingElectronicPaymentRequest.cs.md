# src/Falah.Application/Orders/CancelPendingElectronicPaymentRequest.cs

Orders API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `7a7b7485b4ea1de6125622683f6e75bb0865dd54337f307c73f494d4d22505e9`. Working SHA-256 `7a7b7485b4ea1de6125622683f6e75bb0865dd54337f307c73f494d4d22505e9`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-11.

Symbols indexed (index is not a claim every method was reviewed): CancelPendingElectronicPaymentRequest@8, CancelPendingElectronicPaymentResult@10.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 8: `public sealed record CancelPendingElectronicPaymentRequest(string OrderId, string Reason, string CashierId);`
- Line 10: `public sealed record CancelPendingElectronicPaymentResult(`

Checkout contract creates and pays in one call, with electronic/COD deferral only in downstream rules; RequestedPickupAtUtc is metadata, not a reservation lifecycle. PrintJobsQueued is enqueue outcome only. Refund and confirmation have different idempotency keys. OrderDetailsDto is read-only and exposes frozen splits, pickup time and customer data; no API here settles cash pickup later.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

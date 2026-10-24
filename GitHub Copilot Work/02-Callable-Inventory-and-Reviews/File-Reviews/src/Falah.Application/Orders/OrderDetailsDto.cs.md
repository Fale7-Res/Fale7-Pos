# src/Falah.Application/Orders/OrderDetailsDto.cs

Orders API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `64bc01c1e49feaa0ac26e62c7b93ef20e65f1430a4b0313f8e08464b3e62dbae`. Working SHA-256 `2c3fe061e51f9e6f3abce30fd44b33bfa78f94dab132d8df24ada8079ba7aeea`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-30.

Symbols indexed (index is not a claim every method was reviewed): OrderItemDto@6, OrderDetailsDto@10, OrderSplitPaymentDto@30.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 6: `public sealed record OrderItemDto(`
- Line 10: `public sealed record OrderDetailsDto(`
- Line 30: `public sealed record OrderSplitPaymentDto(string Method, long AmountPiastres);`

Checkout contract creates and pays in one call, with electronic/COD deferral only in downstream rules; RequestedPickupAtUtc is metadata, not a reservation lifecycle. PrintJobsQueued is enqueue outcome only. Refund and confirmation have different idempotency keys. OrderDetailsDto is read-only and exposes frozen splits, pickup time and customer data; no API here settles cash pickup later.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

# src/Falah.Application/Orders/CheckoutOrderRequest.cs

Orders API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `6fa4b76e9a4abdce701c4db9571c19ab2bd3ff8694f64dff9aacc7e9745fadb5`. Working SHA-256 `66cd885af3b6ddf84ea4a1629232490ba92e333be7626715be8614587b91dae1`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-101.

Symbols indexed (index is not a claim every method was reviewed): CheckoutItemRequest@9, CheckoutSplitRequest@14, CheckoutOrderRequest@21, CheckoutOrderResult@73, CheckoutConflictException@93, ShiftNotOpenException@98.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 9: `public sealed record CheckoutItemRequest(`
- Line 14: `public sealed record CheckoutSplitRequest(string Method, long AmountPiastres);`
- Line 21: `public sealed record CheckoutOrderRequest(`
- Line 73: `public sealed record CheckoutOrderResult(`
- Line 95: `public CheckoutConflictException(string message) : base(message) { }`
- Line 100: `public ShiftNotOpenException(string message) : base(message) { }`

Checkout contract creates and pays in one call, with electronic/COD deferral only in downstream rules; RequestedPickupAtUtc is metadata, not a reservation lifecycle. PrintJobsQueued is enqueue outcome only. Refund and confirmation have different idempotency keys. OrderDetailsDto is read-only and exposes frozen splits, pickup time and customer data; no API here settles cash pickup later.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

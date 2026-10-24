# src/Falah.Application/Menu/ProductImageRequests.cs

Menu API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `32f0ccacf4f828651adabe03277176df07dfc7bdfee36bbb3249ada114790ef2`. Working SHA-256 `32f0ccacf4f828651adabe03277176df07dfc7bdfee36bbb3249ada114790ef2`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-16.

Symbols indexed (index is not a claim every method was reviewed): SetProductImageRequest@8, SetProductImageResult@14, RemoveProductImageRequest@16.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 8: `public sealed record SetProductImageRequest(`
- Line 14: `public sealed record SetProductImageResult(string ProductId, string Version);`
- Line 16: `public sealed record RemoveProductImageRequest(string ActorUserId, string ProductId);`

Client menu prices are display-only and checkout sends product/variant references for server resolution. Images use version stamps rather than inline bytes in menu reads; upload accepts base64 and requires size/type checks in handler. Full variant replacement requires historical snapshot isolation in persistence.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

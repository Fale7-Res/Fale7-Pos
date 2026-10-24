# src/Falah.Application/Printing/ProductStationRequests.cs

Printing API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `640ebb48c65fb395976807001487d5a2d5e6984f8effef341063955e39a021f6`. Working SHA-256 `640ebb48c65fb395976807001487d5a2d5e6984f8effef341063955e39a021f6`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-8.

Symbols indexed (index is not a claim every method was reviewed): SetProductStationRequest@8.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 8: `public sealed record SetProductStationRequest(string? Station, string ActorUserId);`

Ticket wire types carry content rather than device bytes. Shared mapping handles text/row/rule/cut/QR and enum strings in both directions; unknown elements fall back to text. ComposedTicketDto comment omits qr despite real mapping support. Create/report request IDs distinguish queue creation from attempt reporting. Boolean Succeeded cannot prove physical output; PRT-002.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

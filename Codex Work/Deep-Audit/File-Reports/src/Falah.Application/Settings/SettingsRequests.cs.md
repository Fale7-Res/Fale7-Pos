# src/Falah.Application/Settings/SettingsRequests.cs

Settings API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `cd48a1f2c7510cbbe7b5cd51c6e83c44936b900945144bb123688477cd8be7a9`. Working SHA-256 `07a9b6aebd5f131a91d8d367c684c288def590106472c548026883b86e47e790`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-45.

Symbols indexed (index is not a claim every method was reviewed): SettingsDto@7, UpdateSettingsRequest@32.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 7: `public sealed record SettingsDto(`
- Line 32: `public sealed record UpdateSettingsRequest(`

System-wide partial settings carry print width/copies/autoprint/hold behavior and menu URL. Null update means unchanged, empty menu clears link. Hold setting suppresses initial electronic kitchen ticket, but confirm endpoint does not automatically release it (FLOW-002). Copies and width are not consumed by inspected agent/autoprint path (PRT-006).

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

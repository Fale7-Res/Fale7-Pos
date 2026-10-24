# src/Falah.Application/Inventory/InventoryItemRequests.cs

Inventory API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `73b19c1ed74f1318815e794f065fe191ffe4cccfa801be9b4ff95f883189f317`. Working SHA-256 `1db9910fe257d0e653719898cf3ba32f622bdeafe7028fd62936e7ff26445992`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-33.

Symbols indexed (index is not a claim every method was reviewed): CreateInventoryItemRequest@8, CreateInventoryItemResult@12, UpdateInventoryItemRequest@17, InventoryItemDto@21, SetEstimatedCostRequest@32.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 8: `public sealed record CreateInventoryItemRequest(`
- Line 12: `public sealed record CreateInventoryItemResult(string Id, bool WasReplayed);`
- Line 17: `public sealed record UpdateInventoryItemRequest(`
- Line 21: `public sealed record InventoryItemDto(`
- Line 32: `public sealed record SetEstimatedCostRequest(`

Milli-unit quantities and piastre costs are explicit; null cost means unknown. Recipes replace entire line sets; prep and packaging contracts are distinct. Stock adjustment directions and count deltas are client input and need server bounds/authorization. DTOs perform no stock locking or calculation; do not claim correctness from record types.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

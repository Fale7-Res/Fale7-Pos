# src/Falah.Application/Inventory/InventoryOverviewDtos.cs

Inventory API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `3e0e818b2807732a722701ed310d39c1d7ff50bd6d70fca9624561cef681e15a`. Working SHA-256 `d610a40ad83dfa0a5babf782f05730015cd25def8bc025bc4a39eecc46746a93`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-47.

Symbols indexed (index is not a claim every method was reviewed): InventoryOverviewRowDto@11, InventoryOverviewDto@21, StockMovementRowDto@33, InventoryItemDetailDto@41.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 11: `public sealed record InventoryOverviewRowDto(`
- Line 21: `public sealed record InventoryOverviewDto(`
- Line 33: `public sealed record StockMovementRowDto(`
- Line 41: `public sealed record InventoryItemDetailDto(`

Milli-unit quantities and piastre costs are explicit; null cost means unknown. Recipes replace entire line sets; prep and packaging contracts are distinct. Stock adjustment directions and count deltas are client input and need server bounds/authorization. DTOs perform no stock locking or calculation; do not claim correctness from record types.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

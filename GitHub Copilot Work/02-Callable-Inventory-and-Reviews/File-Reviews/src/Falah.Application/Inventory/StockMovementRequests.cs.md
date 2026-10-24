# src/Falah.Application/Inventory/StockMovementRequests.cs

Inventory API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `7b0a7cea219d68cf44f1c34e7d998226805dff9a7bad1591633d0d7b46d8af63`. Working SHA-256 `7b0a7cea219d68cf44f1c34e7d998226805dff9a7bad1591633d0d7b46d8af63`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-31.

Symbols indexed (index is not a claim every method was reviewed): RecordCountAdjustmentRequest@7, RecordCountAdjustmentResult@11, RecordWasteRequest@16, RecordWasteResult@20, StockMovementDto@23, InventoryConflictException@28.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 7: `public sealed record RecordCountAdjustmentRequest(`
- Line 11: `public sealed record RecordCountAdjustmentResult(string MovementId, string InventoryItemId, bool WasReplayed);`
- Line 16: `public sealed record RecordWasteRequest(`
- Line 20: `public sealed record RecordWasteResult(`
- Line 23: `public sealed record StockMovementDto(`
- Line 30: `public InventoryConflictException(string message) : base(message) { }`

Milli-unit quantities and piastre costs are explicit; null cost means unknown. Recipes replace entire line sets; prep and packaging contracts are distinct. Stock adjustment directions and count deltas are client input and need server bounds/authorization. DTOs perform no stock locking or calculation; do not claim correctness from record types.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

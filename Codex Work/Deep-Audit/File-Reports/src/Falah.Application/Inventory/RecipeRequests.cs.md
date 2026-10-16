# src/Falah.Application/Inventory/RecipeRequests.cs

Inventory API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `3a0099b774799cb401d8ecb6364bb8056bf159528a04442f3599e18e7a0c894f`. Working SHA-256 `3a0099b774799cb401d8ecb6364bb8056bf159528a04442f3599e18e7a0c894f`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-39.

Symbols indexed (index is not a claim every method was reviewed): RecipeLineInput@6, SaveRecipeRequest@13, RecipeLineDto@17, RecipeDto@19, PrepRecipeLineInput@23, SavePrepRecipeRequest@25, PrepRecipeLineDto@29, PrepRecipeDto@31, SaveOrderPackagingRuleRequest@35, OrderPackagingRuleDto@39.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 6: `public sealed record RecipeLineInput(string InventoryItemId, long QuantityMilliUnits, string Kind, long? YieldUnitsPerBaseUnit);`
- Line 13: `public sealed record SaveRecipeRequest(`
- Line 17: `public sealed record RecipeLineDto(string InventoryItemId, long QuantityMilliUnits, string Kind, long? YieldUnitsPerBaseUnit);`
- Line 19: `public sealed record RecipeDto(string Id, string ProductId, string Name, bool Active, IReadOnlyList<RecipeLineDto> Lines);`
- Line 23: `public sealed record PrepRecipeLineInput(string InventoryItemId, long QuantityMilliUnits);`
- Line 25: `public sealed record SavePrepRecipeRequest(`
- Line 29: `public sealed record PrepRecipeLineDto(string InventoryItemId, long QuantityMilliUnits);`
- Line 31: `public sealed record PrepRecipeDto(string Id, string Name, string OutputInventoryItemId, long OutputQuantityMilliUnits, bool Active, IReadOnlyList<PrepRecipeLineDto> Lines);`
- Line 35: `public sealed record SaveOrderPackagingRuleRequest(`
- Line 39: `public sealed record OrderPackagingRuleDto(string Id, string OrderType, string InventoryItemId, long QuantityMilliUnits, bool Active);`

Milli-unit quantities and piastre costs are explicit; null cost means unknown. Recipes replace entire line sets; prep and packaging contracts are distinct. Stock adjustment directions and count deltas are client input and need server bounds/authorization. DTOs perform no stock locking or calculation; do not claim correctness from record types.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

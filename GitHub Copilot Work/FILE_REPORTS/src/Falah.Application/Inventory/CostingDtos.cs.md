# src/Falah.Application/Inventory/CostingDtos.cs

Inventory API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `5c207202cca8b32126224204153b695dc48018d4e35383b6d893e584ad3bf825`. Working SHA-256 `fc2b3cd97e9a0e7058dba345a68ed7f3cb10a36f75c4dbcc2a7c14f2da0fcbda`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-75.

Symbols indexed (index is not a claim every method was reviewed): IngredientCostDto@6, RecipeCostDto@8, PrepRecipeCostDto@12, MenuItemProfitabilityDto@17, MovementTotalsDto@20, VarianceDto@22, LossSummaryDto@25, SharedConsumptionDto@27, RecipeCostLineDto@47, RecipeCostDetailDto@55, RecipeCostSummaryDto@65, RecipeCostCoverageDto@73.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 6: `public sealed record IngredientCostDto(string InventoryItemId, long? UnitCostPiastres, string Source);`
- Line 8: `public sealed record RecipeCostDto(`
- Line 12: `public sealed record PrepRecipeCostDto(`
- Line 17: `public sealed record MenuItemProfitabilityDto(double? FoodCostPercent, long? ContributionPiastres, double? ContributionPercent,`
- Line 20: `public sealed record MovementTotalsDto(long PurchasedMilliUnits, long UsageMilliUnits, long WasteMilliUnits, long CountAdjustmentNetMilliUnits);`
- Line 22: `public sealed record VarianceDto(`
- Line 25: `public sealed record LossSummaryDto(long TotalLossValuePiastres, int MovementCount, IReadOnlyDictionary<string, long> ValueByType);`
- Line 27: `public sealed record SharedConsumptionDto(`
- Line 47: `public sealed record RecipeCostLineDto(`
- Line 55: `public sealed record RecipeCostDetailDto(`
- Line 65: `public sealed record RecipeCostSummaryDto(`
- Line 73: `public sealed record RecipeCostCoverageDto(`

Milli-unit quantities and piastre costs are explicit; null cost means unknown. Recipes replace entire line sets; prep and packaging contracts are distinct. Stock adjustment directions and count deltas are client input and need server bounds/authorization. DTOs perform no stock locking or calculation; do not claim correctness from record types.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

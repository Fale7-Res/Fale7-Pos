namespace Falah.Application.Inventory;

/// <summary>Read-only projections of Falah.Domain.Inventory.CostingRules'/InventoryRules' pure
/// results. UnitCostPiastres/UnitCost=null (not zero) is a load-bearing distinction throughout this
/// module - "unknown cost" is a real, displayable state, never silently rendered as free.</summary>
public sealed record IngredientCostDto(string InventoryItemId, long? UnitCostPiastres, string Source);

public sealed record RecipeCostDto(
    long KnownCostPiastres, long FoodCostPiastres, long PackagingCostPiastres, int CompletenessPercent,
    IReadOnlyList<string> Missing, int LineCount, int KnownLineCount);

public sealed record PrepRecipeCostDto(
    long? UnitCostPiastres, long KnownCostPiastres, double Completeness, IReadOnlyList<string> Missing, int LineCount, int KnownLineCount);

/// <summary>Every figure here is null while the dish's cost is incomplete. CostCompletenessPercent
/// and Missing say why, so a blank margin can be acted on instead of guessed at.</summary>
public sealed record MenuItemProfitabilityDto(double? FoodCostPercent, long? ContributionPiastres, double? ContributionPercent,
    int CostCompletenessPercent = 100, IReadOnlyList<string>? Missing = null);

public sealed record MovementTotalsDto(long PurchasedMilliUnits, long UsageMilliUnits, long WasteMilliUnits, long CountAdjustmentNetMilliUnits);

public sealed record VarianceDto(
    long TheoreticalUsageMilliUnits, long RecordedConsumptionMilliUnits, long VarianceMilliUnits, long CountAdjustmentNetMilliUnits);

public sealed record LossSummaryDto(long TotalLossValuePiastres, int MovementCount, IReadOnlyDictionary<string, long> ValueByType);

public sealed record SharedConsumptionDto(
    long? ActualDailyAvgMilliUnits, long? StandardDailyUsageMilliUnits, long? VarianceMilliUnitsPerDay, long ConsumedMilliUnits);

// ---------------------------------------------------------------------------------------------
// Costing read models
//
// The recipes screen used to ask for a recipe's total cost, then that recipe's margin, then repeat
// both for every other recipe - two round trips per dish, and still no answer to the question the
// owner actually opens the screen with: what is this made of, and which part of the cost is a real
// invoice rather than somebody's guess. These carry the whole answer in one read.
// ---------------------------------------------------------------------------------------------

/// <summary>One ingredient line of a recipe, priced.
/// <para><paramref name="CostSource"/> is Purchase (a real invoice), Estimated (a figure the owner
/// typed while waiting for one), Prep (resolved from a batch recipe), Unknown (no basis at all) or
/// Circular (the batch consumes its own output, directly or around a chain). Only Purchase and Prep
/// are facts.</para>
/// <para><paramref name="BaseQuantityMilliUnits"/> is what a Yield line ("4 pieces") converts to in
/// the ingredient's own unit; null means the conversion ratio is missing, which is why the line has
/// no cost even when the ingredient itself is priced.</para></summary>
public sealed record RecipeCostLineDto(
    string InventoryItemId, string ItemName, string Unit, long QuantityMilliUnits, string Kind,
    long? YieldUnitsPerBaseUnit, long? BaseQuantityMilliUnits,
    long? UnitCostPiastres, string CostSource, DateTimeOffset? CostAsOfUtc,
    long? ExtendedCostPiastres, string? MissingReason);

/// <summary>A recipe, its lines, its cost and its margin - with the margin present only when the
/// cost behind it is complete.</summary>
public sealed record RecipeCostDetailDto(
    string RecipeId, string Name, bool Active, string ProductId, string ProductName, string? CategoryName,
    long? SellingPricePiastres, string PriceBasis,
    IReadOnlyList<RecipeCostLineDto> Lines,
    long KnownCostPiastres, long FoodCostPiastres, long PackagingCostPiastres,
    int CompletenessPercent, IReadOnlyList<string> Missing, int LineCount, int KnownLineCount,
    double? FoodCostPercent, long? ContributionPiastres, double? ContributionPercent);

/// <summary>One row of the recipes list. Everything the list shows, for every recipe, in one call
/// instead of two per row.</summary>
public sealed record RecipeCostSummaryDto(
    string RecipeId, string Name, bool Active, string ProductId, string ProductName, string? CategoryName,
    long? SellingPricePiastres, long KnownCostPiastres, long FoodCostPiastres, long PackagingCostPiastres,
    int CompletenessPercent, int LineCount, int KnownLineCount, IReadOnlyList<string> Missing,
    double? FoodCostPercent, long? ContributionPiastres, double? ContributionPercent);

/// <summary>What the recipes screen says about the menu as a whole before the owner reads a single
/// row: how much of the menu can be trusted for profitability at all.</summary>
public sealed record RecipeCostCoverageDto(
    int RecipeCount, int FullyCostedCount, int PartlyCostedCount, int UncostedCount,
    int ProductsWithoutRecipeCount, int IngredientsWithoutCostCount);

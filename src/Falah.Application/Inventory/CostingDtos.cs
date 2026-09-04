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

public sealed record MenuItemProfitabilityDto(double? FoodCostPercent, long? ContributionPiastres, double? ContributionPercent);

public sealed record MovementTotalsDto(long PurchasedMilliUnits, long UsageMilliUnits, long WasteMilliUnits, long CountAdjustmentNetMilliUnits);

public sealed record VarianceDto(
    long TheoreticalUsageMilliUnits, long RecordedConsumptionMilliUnits, long VarianceMilliUnits, long CountAdjustmentNetMilliUnits);

public sealed record LossSummaryDto(long TotalLossValuePiastres, int MovementCount, IReadOnlyDictionary<string, long> ValueByType);

public sealed record SharedConsumptionDto(
    long? ActualDailyAvgMilliUnits, long? StandardDailyUsageMilliUnits, long? VarianceMilliUnitsPerDay, long ConsumedMilliUnits);

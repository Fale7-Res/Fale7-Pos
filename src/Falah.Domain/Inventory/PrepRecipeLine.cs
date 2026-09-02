namespace Falah.Domain.Inventory;

public sealed record PrepRecipeLine(string PrepRecipeId, string InventoryItemId, long QuantityMilliUnits);

public sealed record CostingContext(
    IReadOnlyList<InventoryItem> InventoryItems,
    IReadOnlyList<PrepRecipe> PrepRecipes,
    IReadOnlyList<PrepRecipeLine> PrepRecipeLines,
    IReadOnlyList<StockMovementFacts> StockMovements);

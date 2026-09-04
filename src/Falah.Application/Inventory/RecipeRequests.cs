namespace Falah.Application.Inventory;

/// <summary>One line of a menu recipe as submitted by the editor. Kind is one of Direct/Yield/
/// Packaging (see Falah.Domain.Inventory.RecipeLineKind); YieldUnitsPerBaseUnit is required and
/// only meaningful when Kind is Yield.</summary>
public sealed record RecipeLineInput(string InventoryItemId, long QuantityMilliUnits, string Kind, long? YieldUnitsPerBaseUnit);

/// <summary>Ported from saveRecipe - a "replace the whole line set" save, not incremental add/
/// remove-line commands. RecipeId absent means create (id minted as `recipe-{RequestId}`); present
/// means edit in place. Idempotent by construction: re-applying the exact same save (same RequestId)
/// reproduces the exact same resulting rows, so no separate duplicate-submit guard is needed the way
/// a financial posting needs one - see SaveRecipeHandler's own doc comment.</summary>
public sealed record SaveRecipeRequest(
    string RequestId, string? RecipeId, string ProductId, string Name, bool Active,
    IReadOnlyList<RecipeLineInput> Lines, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record RecipeLineDto(string InventoryItemId, long QuantityMilliUnits, string Kind, long? YieldUnitsPerBaseUnit);

public sealed record RecipeDto(string Id, string ProductId, string Name, bool Active, IReadOnlyList<RecipeLineDto> Lines);

/// <summary>Ported from savePrepRecipe - same replace-whole-set shape as SaveRecipeRequest, for a
/// batch/prep recipe (see Falah.Domain.Inventory.PrepRecipe's own doc comment).</summary>
public sealed record PrepRecipeLineInput(string InventoryItemId, long QuantityMilliUnits);

public sealed record SavePrepRecipeRequest(
    string RequestId, string? PrepRecipeId, string Name, string OutputInventoryItemId, long OutputQuantityMilliUnits,
    bool Active, IReadOnlyList<PrepRecipeLineInput> Lines, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record PrepRecipeLineDto(string InventoryItemId, long QuantityMilliUnits);

public sealed record PrepRecipeDto(string Id, string Name, string OutputInventoryItemId, long OutputQuantityMilliUnits, bool Active, IReadOnlyList<PrepRecipeLineDto> Lines);

/// <summary>Ported from saveOrderPackagingRule - order-level (not item-level) packaging, applied
/// once per qualifying order type.</summary>
public sealed record SaveOrderPackagingRuleRequest(
    string RequestId, string? RuleId, string OrderType, string InventoryItemId, long QuantityMilliUnits,
    bool Active, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record OrderPackagingRuleDto(string Id, string OrderType, string InventoryItemId, long QuantityMilliUnits, bool Active);

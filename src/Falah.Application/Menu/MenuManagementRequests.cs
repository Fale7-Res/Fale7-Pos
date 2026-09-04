namespace Falah.Application.Menu;

/// <summary>Ported from addCategory/updateCategory/deleteCategory in the reference app's
/// lib/store.tsx - all three gated on `menu_manage` with no approval-token fallback (a bare
/// `authorizeCommand({permission:'menu_manage'})`, matching SetProductStationHandler's existing
/// gate exactly). `deleteCategory` never hard-deletes - it flips Active to false, same as this
/// system's SetCategoryActiveRequest below.</summary>
public sealed record CreateCategoryRequest(string RequestId, string Name, string? Icon, int SortOrder, string ActorUserId);

public sealed record CreateCategoryResult(string CategoryId, bool WasReplayed);

public sealed record UpdateCategoryRequest(string CategoryId, string Name, string? Icon, int SortOrder, string ActorUserId);

public sealed record SetCategoryActiveRequest(string CategoryId, bool Active, string ActorUserId);

public sealed record CategoryDetailsDto(string Id, string Name, string? Icon, int SortOrder, bool Active);

/// <summary>Ported from addProduct/updateProduct/deleteProduct. `updateProduct` replaces the WHOLE
/// product object in the reference app, variants array included (there is no separate add/remove-
/// variant command anywhere in that codebase) - Variants here is the same "replace the whole set"
/// idiom SaveRecipeHandler already uses for recipe lines, not a set of per-variant CRUD endpoints.
/// `deleteProduct` never hard-deletes - it flips IsAvailable to false (SetProductAvailabilityRequest
/// below), the exact same soft-delete shape as categories.</summary>
public sealed record ProductVariantInput(string? Id, string Name, long PricePiastres, string? RecipeId);

public sealed record CreateProductRequest(
    string RequestId, string CategoryId, string Name, long BasePricePiastres, bool SendToGrill,
    string? PreparationStation, int SortOrder, string? RecipeId, IReadOnlyList<ProductVariantInput> Variants, string ActorUserId);

public sealed record CreateProductResult(string ProductId, bool WasReplayed);

public sealed record UpdateProductRequest(
    string ProductId, string CategoryId, string Name, long BasePricePiastres, bool SendToGrill,
    string? PreparationStation, int SortOrder, string? RecipeId, IReadOnlyList<ProductVariantInput> Variants, string ActorUserId);

public sealed record SetProductAvailabilityRequest(string ProductId, bool IsAvailable, string ActorUserId);

public sealed record ProductVariantDetailsDto(string Id, string Name, long PricePiastres, string? RecipeId);

public sealed record ProductDetailsDto(
    string Id, string CategoryId, string Name, long BasePricePiastres, bool SendToGrill, string? PreparationStation,
    bool IsAvailable, int SortOrder, string? RecipeId, IReadOnlyList<ProductVariantDetailsDto> Variants);

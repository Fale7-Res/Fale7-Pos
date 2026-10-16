# src/Falah.Application/Menu/MenuManagementRequests.cs

Menu API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `5d87a2470c394522cb3a0dd091aad67334d65f20c2f516e813e63b976cdb3700`. Working SHA-256 `398de8204e5ef0ccc50008587189be6047a811d07769ec458f1e585f9d363280`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-45.

Symbols indexed (index is not a claim every method was reviewed): CreateCategoryRequest@8, CreateCategoryResult@10, UpdateCategoryRequest@12, SetCategoryActiveRequest@14, CategoryDetailsDto@16, ProductVariantInput@24, CreateProductRequest@26, CreateProductResult@30, UpdateProductRequest@32, SetProductAvailabilityRequest@36, ProductVariantDetailsDto@38, ProductDetailsDto@40.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 8: `public sealed record CreateCategoryRequest(string RequestId, string Name, string? Icon, int SortOrder, string ActorUserId);`
- Line 10: `public sealed record CreateCategoryResult(string CategoryId, bool WasReplayed);`
- Line 12: `public sealed record UpdateCategoryRequest(string CategoryId, string Name, string? Icon, int SortOrder, string ActorUserId, string? RequestId = null);`
- Line 14: `public sealed record SetCategoryActiveRequest(string CategoryId, bool Active, string ActorUserId, string? RequestId = null);`
- Line 16: `public sealed record CategoryDetailsDto(string Id, string Name, string? Icon, int SortOrder, bool Active);`
- Line 24: `public sealed record ProductVariantInput(string? Id, string Name, long PricePiastres, string? RecipeId);`
- Line 26: `public sealed record CreateProductRequest(`
- Line 30: `public sealed record CreateProductResult(string ProductId, bool WasReplayed);`
- Line 32: `public sealed record UpdateProductRequest(`
- Line 36: `public sealed record SetProductAvailabilityRequest(string ProductId, bool IsAvailable, string ActorUserId, string? RequestId = null);`
- Line 38: `public sealed record ProductVariantDetailsDto(string Id, string Name, long PricePiastres, string? RecipeId);`
- Line 40: `public sealed record ProductDetailsDto(`

Client menu prices are display-only and checkout sends product/variant references for server resolution. Images use version stamps rather than inline bytes in menu reads; upload accepts base64 and requires size/type checks in handler. Full variant replacement requires historical snapshot isolation in persistence.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.

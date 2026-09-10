using System.Net.Http.Json;
using Falah.Application.Menu;
using Falah.Application.Pricing;

namespace Falah.Client.CashierWinUI.Services;

public sealed partial class ApiClient
{
    public Task<ApiResult<IReadOnlyList<CategoryDetailsDto>>> GetAllCategoriesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<CategoryDetailsDto>>(() => _http.GetAsync("/api/categories/all", ct), ct);
    public Task<ApiResult<IReadOnlyList<ProductDetailsDto>>> GetAllProductsAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<ProductDetailsDto>>(() => _http.GetAsync("/api/products/all", ct), ct);
    public Task<ApiResult<CreateCategoryResult>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default) =>
        SendAsync<CreateCategoryResult>(() => _http.PostAsJsonAsync("/api/categories", request, ct), ct);
    public Task<ApiResult<bool>> UpdateCategoryAsync(UpdateCategoryRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PutAsJsonAsync($"/api/categories/{Uri.EscapeDataString(request.CategoryId)}", request, ct), ct);
    public Task<ApiResult<bool>> SetCategoryActiveAsync(SetCategoryActiveRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PostAsJsonAsync($"/api/categories/{Uri.EscapeDataString(request.CategoryId)}/active", request, ct), ct);
    public Task<ApiResult<CreateProductResult>> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default) =>
        SendAsync<CreateProductResult>(() => _http.PostAsJsonAsync("/api/products", request, ct), ct);
    public Task<ApiResult<bool>> UpdateProductAsync(UpdateProductRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PutAsJsonAsync($"/api/products/{Uri.EscapeDataString(request.ProductId)}", request, ct), ct);
    public Task<ApiResult<SetProductImageResult>> SetProductImageAsync(SetProductImageRequest request, CancellationToken ct = default) =>
        SendAsync<SetProductImageResult>(() => _http.PutAsJsonAsync($"/api/products/{Uri.EscapeDataString(request.ProductId)}/image", request, ct), ct);
    public Task<ApiResult<bool>> RemoveProductImageAsync(string productId, string actorUserId, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.DeleteAsync($"/api/products/{Uri.EscapeDataString(productId)}/image?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);
    public Task<ApiResult<CreateTaxRuleResult>> CreateTaxRuleAsync(CreateTaxRuleRequest request, CancellationToken ct = default) =>
        SendAsync<CreateTaxRuleResult>(() => _http.PostAsJsonAsync("/api/tax-rules", request, ct), ct);
    public Task<ApiResult<bool>> UpdateTaxRuleAsync(UpdateTaxRuleRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PutAsJsonAsync($"/api/tax-rules/{Uri.EscapeDataString(request.TaxRuleId)}", request, ct), ct);
    public Task<ApiResult<PriceRuleDto>> SavePriceRuleAsync(SavePriceRuleRequest request, CancellationToken ct = default) =>
        SendAsync<PriceRuleDto>(() => _http.PostAsJsonAsync("/api/price-rules", request, ct), ct);
}

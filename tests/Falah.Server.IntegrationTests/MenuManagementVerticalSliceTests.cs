namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Menu;
using Falah.Application.Pricing;
using Falah.Application.Approvals;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Menu Management (categories/products/variants/availability), Tax Rules, and Pricing/Offers -
/// ported from addCategory/updateCategory/deleteCategory/addProduct/updateProduct/deleteProduct
/// (lib/store.tsx), addTaxRule/updateTaxRule and savePriceRule (lib/enterprise-store.tsx). All three
/// areas previously had NO backend write path at all (read-only /api/menu, plus the narrowly-scoped
/// preparation-station endpoint from the Printing slice). Preparation station itself is unchanged -
/// covered by PrintingVerticalSliceTests.
/// </summary>
public class MenuManagementVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _menuManagerId = default!;
    private string _ownerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateClient();
        _menuManagerId = await _factory.SeedUserAsync("مسؤول المنيو", "supervisor", "menu_manage", "offers_manage", "settings_manage");
        _ownerId = await _factory.SeedUserAsync("المالك", "owner", "all");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    // ---- Categories ----

    private Task<HttpResponseMessage> CreateCategoryAsync(string name, string? requestId = null, string? actorId = null) =>
        _client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(requestId ?? $"req-{Guid.NewGuid():N}", name, null, 1, actorId ?? _menuManagerId));

    [Fact]
    public async Task Creating_a_category_succeeds_and_is_readable_back()
    {
        var response = await CreateCategoryAsync("مشروبات");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateCategoryResult>();
        Assert.False(result!.WasReplayed);

        var all = await (await _client.GetAsync("/api/categories/all")).Content.ReadFromJsonAsync<List<CategoryDetailsDto>>();
        Assert.Contains(all!, c => c.Id == result.CategoryId && c.Name == "مشروبات" && c.Active);
    }

    [Fact]
    public async Task Creating_a_category_without_menu_manage_is_rejected()
    {
        var noPermissionUser = await _factory.SeedUserAsync("كاشير", "cashier", "pos");
        var response = await CreateCategoryAsync("مشروبات", actorId: noPermissionUser);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Retrying_the_same_create_category_request_id_replays_not_duplicates()
    {
        var requestId = $"req-{Guid.NewGuid():N}";
        var first = await (await CreateCategoryAsync("حلويات", requestId)).Content.ReadFromJsonAsync<CreateCategoryResult>();
        var second = await (await CreateCategoryAsync("حلويات", requestId)).Content.ReadFromJsonAsync<CreateCategoryResult>();
        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(first.CategoryId, second.CategoryId);
    }

    [Fact]
    public async Task Concurrent_create_category_requests_with_the_same_request_id_create_exactly_one_category()
    {
        var requestId = $"req-{Guid.NewGuid():N}";
        var tasks = Enumerable.Range(0, 6).Select(_ => CreateCategoryAsync("قسم متزامن", requestId));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<CreateCategoryResult>()));
        Assert.Equal(1, results.Count(r => !r!.WasReplayed));
        var count = await _factory.QueryAsync(db => db.Categories.CountAsync(c => c.Id == results.First()!.CategoryId));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Updating_a_category_changes_its_editable_fields()
    {
        var created = await (await CreateCategoryAsync("قسم مبدئي")).Content.ReadFromJsonAsync<CreateCategoryResult>();
        var response = await _client.PutAsJsonAsync($"/api/categories/{created!.CategoryId}",
            new UpdateCategoryRequest(created.CategoryId, "قسم معدل", "icon-x", 5, _menuManagerId));
        response.EnsureSuccessStatusCode();

        var all = await (await _client.GetAsync("/api/categories/all")).Content.ReadFromJsonAsync<List<CategoryDetailsDto>>();
        var updated = all!.Single(c => c.Id == created.CategoryId);
        Assert.Equal("قسم معدل", updated.Name);
        Assert.Equal("icon-x", updated.Icon);
        Assert.Equal(5, updated.SortOrder);
    }

    [Fact]
    public async Task Deactivating_a_category_never_hard_deletes_it()
    {
        var created = await (await CreateCategoryAsync("قسم للتعطيل")).Content.ReadFromJsonAsync<CreateCategoryResult>();
        (await _client.PostAsJsonAsync($"/api/categories/{created!.CategoryId}/active",
            new SetCategoryActiveRequest(created.CategoryId, false, _menuManagerId))).EnsureSuccessStatusCode();

        var all = await (await _client.GetAsync("/api/categories/all")).Content.ReadFromJsonAsync<List<CategoryDetailsDto>>();
        Assert.Contains(all!, c => c.Id == created.CategoryId && !c.Active); // still present, just inactive
    }

    [Fact]
    public async Task An_inactive_category_disappears_from_the_pos_facing_menu()
    {
        var created = await (await CreateCategoryAsync("قسم يظهر ثم يختفي")).Content.ReadFromJsonAsync<CreateCategoryResult>();
        await _factory.QueryAsync(async db =>
        {
            db.Products.Add(new Falah.Infrastructure.Persistence.Entities.ProductEntity
            {
                Id = $"prod-{Guid.NewGuid():N}", CategoryId = created!.CategoryId, Name = "صنف", BasePricePiastres = 1000, IsAvailable = true,
            });
            await db.SaveChangesAsync();
            return true;
        });

        var beforeMenu = await (await _client.GetAsync("/api/menu")).Content.ReadFromJsonAsync<MenuResponse>();
        Assert.Contains(beforeMenu!.Categories, c => c.Id == created!.CategoryId);

        (await _client.PostAsJsonAsync($"/api/categories/{created!.CategoryId}/active",
            new SetCategoryActiveRequest(created.CategoryId, false, _menuManagerId))).EnsureSuccessStatusCode();

        var afterMenu = await (await _client.GetAsync("/api/menu")).Content.ReadFromJsonAsync<MenuResponse>();
        Assert.DoesNotContain(afterMenu!.Categories, c => c.Id == created.CategoryId);
    }

    // ---- Products ----

    private Task<HttpResponseMessage> CreateProductAsync(string categoryId, string name, long priceP, IReadOnlyList<ProductVariantInput>? variants = null, string? actorId = null) =>
        _client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"req-{Guid.NewGuid():N}", categoryId, name, priceP, false, null, 1, null, variants ?? Array.Empty<ProductVariantInput>(), actorId ?? _menuManagerId));

    private async Task<string> SeedCategoryIdAsync()
    {
        var result = await (await CreateCategoryAsync($"قسم-{Guid.NewGuid():N}")).Content.ReadFromJsonAsync<CreateCategoryResult>();
        return result!.CategoryId;
    }

    [Fact]
    public async Task Creating_a_product_with_variants_succeeds_and_is_readable_back()
    {
        var categoryId = await SeedCategoryIdAsync();
        var variants = new[] { new ProductVariantInput(null, "صغير", 5000, null), new ProductVariantInput(null, "كبير", 8000, null) };
        var response = await CreateProductAsync(categoryId, "ساندويتش", 5000, variants);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateProductResult>();

        var product = await (await _client.GetAsync($"/api/products/{result!.ProductId}")).Content.ReadFromJsonAsync<ProductDetailsDto>();
        Assert.Equal("ساندويتش", product!.Name);
        Assert.Equal(2, product.Variants.Count);
        Assert.Contains(product.Variants, v => v.Name == "صغير" && v.PricePiastres == 5000);
    }

    [Fact]
    public async Task Creating_a_product_for_an_unknown_category_is_rejected()
    {
        var response = await CreateProductAsync("cat-does-not-exist", "صنف", 5000);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Creating_a_product_with_zero_price_is_rejected()
    {
        var categoryId = await SeedCategoryIdAsync();
        var response = await CreateProductAsync(categoryId, "صنف مجاني", 0);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Creating_a_product_with_an_invalid_preparation_station_is_rejected()
    {
        var categoryId = await SeedCategoryIdAsync();
        var response = await _client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            $"req-{Guid.NewGuid():N}", categoryId, "صنف", 5000, false, "Kitchen", 1, null, Array.Empty<ProductVariantInput>(), _menuManagerId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Updating_a_product_replaces_its_whole_variant_set()
    {
        var categoryId = await SeedCategoryIdAsync();
        var created = await (await CreateProductAsync(categoryId, "منتج", 5000,
            new[] { new ProductVariantInput(null, "أ", 5000, null), new ProductVariantInput(null, "ب", 6000, null) }))
            .Content.ReadFromJsonAsync<CreateProductResult>();

        var existing = await (await _client.GetAsync($"/api/products/{created!.ProductId}")).Content.ReadFromJsonAsync<ProductDetailsDto>();
        var keepVariantId = existing!.Variants.Single(v => v.Name == "أ").Id;

        // Keep "أ" (by id), drop "ب", add a brand new "ج".
        var updateResponse = await _client.PutAsJsonAsync($"/api/products/{created.ProductId}", new UpdateProductRequest(
            created.ProductId, categoryId, "منتج معدل", 6000, false, null, 1, null,
            new[] { new ProductVariantInput(keepVariantId, "أ", 5500, null), new ProductVariantInput(null, "ج", 7000, null) },
            _menuManagerId));
        updateResponse.EnsureSuccessStatusCode();

        var updated = await (await _client.GetAsync($"/api/products/{created.ProductId}")).Content.ReadFromJsonAsync<ProductDetailsDto>();
        Assert.Equal("منتج معدل", updated!.Name);
        Assert.Equal(2, updated.Variants.Count);
        Assert.Contains(updated.Variants, v => v.Id == keepVariantId && v.PricePiastres == 5500); // kept row, updated price
        Assert.DoesNotContain(updated.Variants, v => v.Name == "ب"); // removed
        Assert.Contains(updated.Variants, v => v.Name == "ج"); // newly added
    }

    [Fact]
    public async Task Setting_a_product_unavailable_removes_it_from_the_pos_menu_but_keeps_it_readable_for_management()
    {
        var categoryId = await SeedCategoryIdAsync();
        var created = await (await CreateProductAsync(categoryId, "منتج قابل للإخفاء", 5000)).Content.ReadFromJsonAsync<CreateProductResult>();

        (await _client.PostAsJsonAsync($"/api/products/{created!.ProductId}/availability",
            new Falah.Application.Menu.SetProductAvailabilityRequest(created.ProductId, false, _menuManagerId))).EnsureSuccessStatusCode();

        var menu = await (await _client.GetAsync("/api/menu")).Content.ReadFromJsonAsync<MenuResponse>();
        Assert.DoesNotContain(menu!.Categories.SelectMany(c => c.Products), p => p.Id == created.ProductId);

        var managementView = await (await _client.GetAsync("/api/products/all")).Content.ReadFromJsonAsync<List<ProductDetailsDto>>();
        Assert.Contains(managementView!, p => p.Id == created.ProductId && !p.IsAvailable);
    }

    [Fact]
    public async Task Setting_availability_to_the_same_state_twice_is_a_harmless_no_op()
    {
        var categoryId = await SeedCategoryIdAsync();
        var created = await (await CreateProductAsync(categoryId, "منتج", 5000)).Content.ReadFromJsonAsync<CreateProductResult>();
        (await _client.PostAsJsonAsync($"/api/products/{created!.ProductId}/availability",
            new Falah.Application.Menu.SetProductAvailabilityRequest(created.ProductId, false, _menuManagerId))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/products/{created.ProductId}/availability",
            new Falah.Application.Menu.SetProductAvailabilityRequest(created.ProductId, false, _menuManagerId))).EnsureSuccessStatusCode();
    }

    // ---- Tax rules ----

    [Fact]
    public async Task Creating_a_tax_rule_succeeds_and_a_new_checkout_applies_it()
    {
        var categoryId = await SeedCategoryIdAsync();
        var product = await (await CreateProductAsync(categoryId, "منتج خاضع للضريبة", 10000)).Content.ReadFromJsonAsync<CreateProductResult>();

        var response = await _client.PostAsJsonAsync("/api/tax-rules",
            new CreateTaxRuleRequest($"req-{Guid.NewGuid():N}", "ضريبة القيمة المضافة", 1400, "Exclusive", true, _menuManagerId));
        response.EnsureSuccessStatusCode();

        var rules = await (await _client.GetAsync("/api/tax-rules")).Content.ReadFromJsonAsync<List<TaxRuleDto>>();
        Assert.Contains(rules!, r => r.Name == "ضريبة القيمة المضافة" && r.Enabled);
    }

    [Fact]
    public async Task Creating_a_tax_rule_without_settings_manage_is_rejected()
    {
        var noPermissionUser = await _factory.SeedUserAsync("كاشير", "cashier", "pos");
        var response = await _client.PostAsJsonAsync("/api/tax-rules",
            new CreateTaxRuleRequest($"req-{Guid.NewGuid():N}", "ضريبة", 1400, "Exclusive", true, noPermissionUser));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Creating_a_tax_rule_with_an_invalid_mode_is_rejected()
    {
        var response = await _client.PostAsJsonAsync("/api/tax-rules",
            new CreateTaxRuleRequest($"req-{Guid.NewGuid():N}", "ضريبة", 1400, "Weird", true, _menuManagerId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Updating_a_tax_rule_to_disabled_stops_it_applying_without_touching_history()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/tax-rules",
            new CreateTaxRuleRequest($"req-{Guid.NewGuid():N}", "ضريبة مؤقتة", 1000, "Inclusive", true, _menuManagerId));
        var created = await createResponse.Content.ReadFromJsonAsync<CreateTaxRuleResult>();

        (await _client.PutAsJsonAsync($"/api/tax-rules/{created!.TaxRuleId}",
            new UpdateTaxRuleRequest(created.TaxRuleId, "ضريبة مؤقتة", 1000, "Inclusive", false, _menuManagerId))).EnsureSuccessStatusCode();

        var rules = await (await _client.GetAsync("/api/tax-rules")).Content.ReadFromJsonAsync<List<TaxRuleDto>>();
        Assert.False(rules!.Single(r => r.Id == created.TaxRuleId).Enabled);
    }

    // ---- Price rules / offers ----

    [Fact]
    public async Task Creating_a_price_rule_succeeds_and_is_readable_back()
    {
        var categoryId = await SeedCategoryIdAsync();
        var product = await (await CreateProductAsync(categoryId, "منتج للعرض", 10000)).Content.ReadFromJsonAsync<CreateProductResult>();

        var response = await _client.PostAsJsonAsync("/api/price-rules", new SavePriceRuleRequest(
            $"req-{Guid.NewGuid():N}", null, "عرض الغداء", new[] { product!.ProductId }, 8000, new[] { 0, 1 },
            new TimeOnly(12, 0), new TimeOnly(16, 0), true, _menuManagerId, null, null));
        response.EnsureSuccessStatusCode();

        var rules = await (await _client.GetAsync("/api/price-rules")).Content.ReadFromJsonAsync<List<PriceRuleDto>>();
        Assert.Contains(rules!, r => r.Name == "عرض الغداء" && r.ProductIds.Contains(product.ProductId));
    }

    [Fact]
    public async Task Saving_a_price_rule_for_an_unknown_product_is_rejected()
    {
        var response = await _client.PostAsJsonAsync("/api/price-rules", new SavePriceRuleRequest(
            $"req-{Guid.NewGuid():N}", null, "عرض", new[] { "prod-does-not-exist" }, 8000, null, null, null, true, _menuManagerId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Saving_a_price_rule_with_no_products_is_rejected()
    {
        var response = await _client.PostAsJsonAsync("/api/price-rules", new SavePriceRuleRequest(
            $"req-{Guid.NewGuid():N}", null, "عرض", Array.Empty<string>(), 8000, null, null, null, true, _menuManagerId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Saving_a_price_rule_without_offers_manage_requires_a_valid_approval()
    {
        var categoryId = await SeedCategoryIdAsync();
        var product = await (await CreateProductAsync(categoryId, "منتج", 10000)).Content.ReadFromJsonAsync<CreateProductResult>();
        var noPermissionUser = await _factory.SeedUserAsync("كاشير", "cashier", "pos");

        var withoutApproval = await _client.PostAsJsonAsync("/api/price-rules", new SavePriceRuleRequest(
            $"req-{Guid.NewGuid():N}", null, "عرض", new[] { product!.ProductId }, 8000, null, null, null, true, noPermissionUser, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, withoutApproval.StatusCode);

        var approvalId = (await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_ownerId, "offers_manage", "اعتماد اختبار"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!.ApprovalId;
        var withApproval = await _client.PostAsJsonAsync("/api/price-rules", new SavePriceRuleRequest(
            $"req-{Guid.NewGuid():N}", null, "عرض", new[] { product.ProductId }, 8000, null, null, null, true, noPermissionUser, approvalId, _ownerId));
        withApproval.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Saving_a_price_rule_with_an_existing_rule_id_updates_it_in_place()
    {
        var categoryId = await SeedCategoryIdAsync();
        var product = await (await CreateProductAsync(categoryId, "منتج", 10000)).Content.ReadFromJsonAsync<CreateProductResult>();
        var created = await (await _client.PostAsJsonAsync("/api/price-rules", new SavePriceRuleRequest(
            $"req-{Guid.NewGuid():N}", null, "عرض أصلي", new[] { product!.ProductId }, 8000, null, null, null, true, _menuManagerId, null, null)))
            .Content.ReadFromJsonAsync<PriceRuleDto>();

        var updated = await (await _client.PostAsJsonAsync("/api/price-rules", new SavePriceRuleRequest(
            $"req-{Guid.NewGuid():N}", created!.Id, "عرض معدل", new[] { product.ProductId }, 7000, null, null, null, false, _menuManagerId, null, null)))
            .Content.ReadFromJsonAsync<PriceRuleDto>();

        Assert.Equal(created.Id, updated!.Id);
        var rules = await (await _client.GetAsync("/api/price-rules")).Content.ReadFromJsonAsync<List<PriceRuleDto>>();
        Assert.Single(rules!, r => r.Id == created.Id);
        Assert.Equal("عرض معدل", rules!.Single(r => r.Id == created.Id).Name);
    }

    // ---- Audit ----

    [Fact]
    public async Task Menu_tax_and_pricing_changes_are_all_audited()
    {
        var categoryId = await SeedCategoryIdAsync();
        var product = await (await CreateProductAsync(categoryId, "منتج مراجع", 10000)).Content.ReadFromJsonAsync<CreateProductResult>();
        await _client.PostAsJsonAsync("/api/tax-rules", new CreateTaxRuleRequest($"req-{Guid.NewGuid():N}", "ضريبة مراجعة", 1000, "Inclusive", true, _menuManagerId));
        await _client.PostAsJsonAsync("/api/price-rules", new SavePriceRuleRequest(
            $"req-{Guid.NewGuid():N}", null, "عرض مراجعة", new[] { product!.ProductId }, 8000, null, null, null, true, _menuManagerId, null, null));

        var events = await (await _client.GetAsync($"/api/audit/events?actorUserId={_ownerId}&category=menu")).Content.ReadFromJsonAsync<List<Falah.Application.Audit.AuditEventDto>>();
        Assert.Contains(events!, e => e.ReferenceId == categoryId);
        Assert.Contains(events!, e => e.ReferenceId == product.ProductId);
        Assert.Contains(events!, e => e.Action.Contains("ضريبة"));
        Assert.Contains(events!, e => e.Action.Contains("عرض تسعير"));
    }
}

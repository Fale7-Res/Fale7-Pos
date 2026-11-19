namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Menu;
using Falah.Application.Pricing;
using Xunit;

public class MenuRetrySafetyTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _actor = default!;
    private static string Key() => Guid.NewGuid().ToString("N");
    public async Task InitializeAsync()
    {
        _factory = new(); _client = _factory.CreateAuthenticatedClient();
        _actor = await _factory.SeedUserAsync("مراجع", "owner", "all");
    }
    public Task DisposeAsync() { _client.Dispose(); _factory.Dispose(); return Task.CompletedTask; }
    private async Task<string> Category()
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(Key(), "قسم", null, 1, _actor));
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<CreateCategoryResult>())!.CategoryId;
    }

    [Fact]
    public async Task Old_category_update_does_not_replace_a_later_edit()
    {
        var id = await Category();
        var first = new UpdateCategoryRequest(id, "الأول", "A", 3, _actor, Key());
        var latest = first with { Name = "الأحدث", Icon = "B", SortOrder = 5, RequestId = Key() };
        foreach (var request in new[] { first, latest, first })
            (await _client.PutAsJsonAsync($"/api/categories/{id}", request)).EnsureSuccessStatusCode();
        var current = (await _client.GetFromJsonAsync<List<CategoryDetailsDto>>("/api/categories/all"))!.Single(c => c.Id == id);
        Assert.Equal(latest.Name, current.Name); Assert.Equal(latest.SortOrder, current.SortOrder); Assert.Equal(latest.Icon, current.Icon);
    }

    [Fact]
    public async Task Old_product_update_preserves_newer_prices_and_variant_identities()
    {
        var id = await _factory.SeedProductAsync(4500);
        var original = (await _client.GetFromJsonAsync<ProductDetailsDto>($"/api/products/{id}"))!;
        var first = new UpdateProductRequest(id, original.CategoryId, "الأول", 6000, false, null, 1, null,
            new[] { new ProductVariantInput(null, "حجم أول", 6000, null) }, _actor, Key());
        var later = first with { Name = "الأحدث", BasePricePiastres = 7000, RequestId = Key(),
            Variants = new[] { new ProductVariantInput(null, "حجم أحدث", 7000, null) } };
        (await _client.PutAsJsonAsync($"/api/products/{id}", first)).EnsureSuccessStatusCode();
        (await _client.PutAsJsonAsync($"/api/products/{id}", later)).EnsureSuccessStatusCode();
        var before = (await _client.GetFromJsonAsync<ProductDetailsDto>($"/api/products/{id}"))!;
        (await _client.PutAsJsonAsync($"/api/products/{id}", first)).EnsureSuccessStatusCode();
        var after = (await _client.GetFromJsonAsync<ProductDetailsDto>($"/api/products/{id}"))!;
        Assert.Equal(before.Name, after.Name); Assert.Equal(before.BasePricePiastres, after.BasePricePiastres);
        Assert.Equal(before.Variants, after.Variants);
    }

    [Fact]
    public async Task Old_tax_update_does_not_reenable_or_replace_a_later_rule()
    {
        var created = await _client.PostAsJsonAsync("/api/tax-rules", new CreateTaxRuleRequest(Key(), "ضريبة", 1000, "Exclusive", false, _actor));
        created.EnsureSuccessStatusCode(); var id = (await created.Content.ReadFromJsonAsync<CreateTaxRuleResult>())!.TaxRuleId;
        var first = new UpdateTaxRuleRequest(id, "الأول", 1400, "Exclusive", true, _actor, Key());
        var latest = first with { Name = "الأحدث", RateBasisPoints = 1500, Mode = "Inclusive", Enabled = false, RequestId = Key() };
        foreach (var request in new[] { first, latest, first })
            (await _client.PutAsJsonAsync($"/api/tax-rules/{id}", request)).EnsureSuccessStatusCode();
        var current = (await _client.GetFromJsonAsync<List<TaxRuleDto>>("/api/tax-rules"))!.Single(t => t.Id == id);
        Assert.False(current.Enabled); Assert.Equal(latest.RateBasisPoints, current.RateBasisPoints); Assert.Equal(latest.Mode, current.Mode);
    }

    [Fact]
    public async Task Price_rule_replay_returns_its_receipt_without_restoring_an_older_offer()
    {
        var product = await _factory.SeedProductAsync(4500);
        var first = new SavePriceRuleRequest(Key(), null, "الأول", new[] { product }, 3500, null, null, null, true, _actor, null, null);
        var initial = await _client.PostAsJsonAsync("/api/price-rules", first); initial.EnsureSuccessStatusCode();
        var saved = (await initial.Content.ReadFromJsonAsync<PriceRuleDto>())!;
        var latest = first with { RequestId = Key(), RuleId = saved.Id, Name = "الأحدث", Active = false, FixedPricePiastres = 3000 };
        (await _client.PostAsJsonAsync("/api/price-rules", latest)).EnsureSuccessStatusCode();
        var replay = await _client.PostAsJsonAsync("/api/price-rules", first); replay.EnsureSuccessStatusCode();
        Assert.Equal(first.FixedPricePiastres, (await replay.Content.ReadFromJsonAsync<PriceRuleDto>())!.FixedPricePiastres);
        var current = (await _client.GetFromJsonAsync<List<PriceRuleDto>>("/api/price-rules"))!.Single(r => r.Id == saved.Id);
        Assert.False(current.Active); Assert.Equal(latest.Name, current.Name);
    }

    [Theory]
    [InlineData("category")]
    [InlineData("product")]
    public async Task Even_a_no_change_status_request_is_recorded_before_a_later_status_change(string kind)
    {
        if (kind == "category")
        {
            var id = await Category(); var first = new SetCategoryActiveRequest(id, true, _actor, Key());
            foreach (var request in new[] { first, first with { Active = false, RequestId = Key() }, first })
                (await _client.PostAsJsonAsync($"/api/categories/{id}/active", request)).EnsureSuccessStatusCode();
            Assert.False((await _client.GetFromJsonAsync<List<CategoryDetailsDto>>("/api/categories/all"))!.Single(r => r.Id == id).Active);
        }
        else
        {
            var id = await _factory.SeedProductAsync(4500); var first = new SetProductAvailabilityRequest(id, true, _actor, Key());
            foreach (var request in new[] { first, first with { IsAvailable = false, RequestId = Key() }, first })
                (await _client.PostAsJsonAsync($"/api/products/{id}/availability", request)).EnsureSuccessStatusCode();
            Assert.False((await _client.GetFromJsonAsync<ProductDetailsDto>($"/api/products/{id}"))!.IsAvailable);
        }
    }

    [Fact]
    public async Task Removing_a_sold_size_retires_it_without_breaking_history_or_future_checkout()
    {
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var opened = await _client.PostAsJsonAsync("/api/shifts/open", new Falah.Application.Shifts.OpenShiftRequest(Key(), "وردية", _actor, 0));
        opened.EnsureSuccessStatusCode();
        var shift = (await opened.Content.ReadFromJsonAsync<Falah.Application.Shifts.OpenShiftResult>())!.ShiftId;
        var product = await _factory.SeedProductAsync(4500);
        var variant = await _factory.SeedVariantAsync(product, 6000, "كبير");
        var checkout = new Falah.Application.Orders.CheckoutOrderRequest(Key(), _actor, shift, "takeaway", "cash",
            new[] { new Falah.Application.Orders.CheckoutItemRequest(product, variant, 1, null, null) }, 0, 0, 0, null, null, null, 6000, null, null, null, null, null, null, null, null);
        var sale = await _client.PostAsJsonAsync("/api/orders/checkout", checkout);
        sale.EnsureSuccessStatusCode();
        var original = (await _client.GetFromJsonAsync<ProductDetailsDto>($"/api/products/{product}"))!;
        var edit = new UpdateProductRequest(product, original.CategoryId, original.Name, original.BasePricePiastres, false, null, 1, null,
            Array.Empty<ProductVariantInput>(), _actor, Key());
        var saved = await _client.PutAsJsonAsync($"/api/products/{product}", edit);
        Assert.True(saved.IsSuccessStatusCode, await saved.Content.ReadAsStringAsync());
        (await _client.PutAsJsonAsync($"/api/products/{product}", edit)).EnsureSuccessStatusCode();
        var current = (await _client.GetFromJsonAsync<ProductDetailsDto>($"/api/products/{product}"))!;
        Assert.Empty(current.Variants);
        var historical = await _factory.QueryAsync(db => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.OrderItems, i => i.VariantId == variant));
        Assert.True(historical);
        var staleSale = await _client.PostAsJsonAsync("/api/orders/checkout", checkout with { CheckoutRequestId = Key() });
        Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity, staleSale.StatusCode);
        var replay = await _client.PostAsJsonAsync("/api/orders/checkout", checkout);
        replay.EnsureSuccessStatusCode();
    }


    [Fact]
    public async Task The_variant_retirement_snapshot_matches_the_current_database_model()
    {
        Assert.False(await _factory.QueryAsync(db => Task.FromResult(Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.HasPendingModelChanges(db.Database))));
    }

}

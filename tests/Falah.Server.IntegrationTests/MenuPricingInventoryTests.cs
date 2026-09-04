namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class MenuPricingInventoryTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private CheckoutOrderRequest Checkout(string productId, string? variantId, int qty, string? priceRuleId = null, string? checkoutRequestId = null, long amountPaid = 100_000) => new(
        CheckoutRequestId: checkoutRequestId ?? $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
        OrderType: "takeaway", PaymentMethod: "cash",
        Items: new[] { new CheckoutItemRequest(productId, variantId, qty, priceRuleId, null) },
        DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
        CompApprovalId: null, CompApproverUserId: null, CompReason: null,
        AmountPaidPiastres: amountPaid, SplitPayments: null, WalletProvider: null,
        CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);

    // ---------- Server-side pricing authority ----------

    [Fact]
    public async Task Order_total_comes_from_the_products_stored_price_not_anything_the_client_could_have_sent()
    {
        var productId = await _factory.SeedProductAsync(7500);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", Checkout(productId, null, 2));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CheckoutOrderResult>();
        Assert.Equal(15000, result!.TotalPiastres);
    }

    [Fact]
    public async Task A_variants_own_price_overrides_the_products_base_price()
    {
        var productId = await _factory.SeedProductAsync(5000);
        var variantId = await _factory.SeedVariantAsync(productId, 9000, "حجم كبير");
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", Checkout(productId, variantId, 1));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CheckoutOrderResult>();
        Assert.Equal(9000, result!.TotalPiastres);
    }

    [Fact]
    public async Task A_valid_active_price_rule_overrides_the_list_price_and_is_recorded_on_the_order_item()
    {
        var productId = await _factory.SeedProductAsync(10000);
        var ruleId = await _factory.SeedPriceRuleAsync(productId, 6000);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", Checkout(productId, null, 1, priceRuleId: ruleId));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CheckoutOrderResult>();

        Assert.Equal(6000, result!.TotalPiastres);
        var item = await _factory.QueryAsync(db => db.OrderItems.AsNoTracking().FirstAsync(i => i.OrderId == result.OrderId));
        Assert.Equal(ruleId, item.PriceRuleId);
        Assert.Equal(10000, item.ListUnitPricePiastres);
        Assert.Equal(6000, item.UnitPricePiastres);
    }

    [Fact]
    public async Task An_inactive_price_rule_is_ignored_and_the_list_price_is_charged()
    {
        var productId = await _factory.SeedProductAsync(10000);
        var ruleId = await _factory.SeedPriceRuleAsync(productId, 6000, active: false);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", Checkout(productId, null, 1, priceRuleId: ruleId));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CheckoutOrderResult>();
        Assert.Equal(10000, result!.TotalPiastres);
    }

    [Fact]
    public async Task A_nonexistent_product_is_rejected()
    {
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", Checkout("prod-does-not-exist", null, 1));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_variant_that_does_not_belong_to_the_requested_product_is_rejected()
    {
        var productA = await _factory.SeedProductAsync(5000);
        var productB = await _factory.SeedProductAsync(5000);
        var variantOfB = await _factory.SeedVariantAsync(productB, 6000);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", Checkout(productA, variantOfB, 1));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task An_unavailable_product_cannot_be_checked_out()
    {
        var productId = await _factory.SeedProductAsync(5000);
        await _factory.QueryAsync(async db =>
        {
            var product = await db.Products.FirstAsync(p => p.Id == productId);
            product.IsAvailable = false;
            await db.SaveChangesAsync();
            return true;
        });

        var response = await _client.PostAsJsonAsync("/api/orders/checkout", Checkout(productId, null, 1));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---------- Inventory deduction ----------

    [Fact]
    public async Task Checkout_deducts_exactly_the_recipes_quantity_times_the_sold_amount()
    {
        var (productId, _, inventoryItemId) = await _factory.SeedProductWithRecipeAsync(5000, quantityMilliUnitsPerUnit: 200);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", Checkout(productId, null, 3));
        response.EnsureSuccessStatusCode();

        var movement = await _factory.QueryAsync(db => db.StockMovements.AsNoTracking().FirstAsync(m => m.InventoryItemId == inventoryItemId));
        Assert.Equal(600, movement.QuantityMilliUnits); // 200 * 3
        Assert.Equal("Usage", movement.Type);
    }

    [Fact]
    public async Task Retrying_the_same_checkout_request_sequentially_never_deducts_stock_twice()
    {
        var (productId, _, inventoryItemId) = await _factory.SeedProductWithRecipeAsync(5000, 100);
        var request = Checkout(productId, null, 1);

        await (await _client.PostAsJsonAsync("/api/orders/checkout", request)).Content.ReadAsStringAsync();
        await (await _client.PostAsJsonAsync("/api/orders/checkout", request)).Content.ReadAsStringAsync();

        var count = await _factory.QueryAsync(db => db.StockMovements.CountAsync(m => m.InventoryItemId == inventoryItemId));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Concurrent_identical_checkout_requests_never_double_deduct_stock()
    {
        var (productId, _, inventoryItemId) = await _factory.SeedProductWithRecipeAsync(5000, 100);
        var request = Checkout(productId, null, 1);

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/orders/checkout", request)));
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode));

        var count = await _factory.QueryAsync(db => db.StockMovements.CountAsync(m => m.InventoryItemId == inventoryItemId));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Two_concurrent_orders_for_the_same_product_from_different_devices_each_deduct_their_own_share_with_no_collision()
    {
        // Simulates the Cashier and Call Center selling the exact same product at the same instant.
        var (productId, _, inventoryItemId) = await _factory.SeedProductWithRecipeAsync(5000, 100);
        var cashierRequest = Checkout(productId, null, 2); // 200 milliunits
        var callCenterRequest = Checkout(productId, null, 3); // 300 milliunits

        var responses = await Task.WhenAll(
            _client.PostAsJsonAsync("/api/orders/checkout", cashierRequest),
            _client.PostAsJsonAsync("/api/orders/checkout", callCenterRequest));
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<CheckoutOrderResult>()));

        Assert.NotEqual(results[0]!.OrderNumber, results[1]!.OrderNumber);
        Assert.NotEqual(results[0]!.SequenceNumber, results[1]!.SequenceNumber);

        var totalDeducted = await _factory.QueryAsync(db => db.StockMovements.Where(m => m.InventoryItemId == inventoryItemId).SumAsync(m => m.QuantityMilliUnits));
        Assert.Equal(500, totalDeducted); // 200 + 300, neither lost nor doubled
        var movementCount = await _factory.QueryAsync(db => db.StockMovements.CountAsync(m => m.InventoryItemId == inventoryItemId));
        Assert.Equal(2, movementCount);
    }

    // ---------- Historical immutability ----------

    [Fact]
    public async Task Changing_a_products_price_after_the_sale_never_changes_the_old_orders_frozen_total()
    {
        var productId = await _factory.SeedProductAsync(8000);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", Checkout(productId, null, 1));
        var order = (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;

        await _factory.QueryAsync(async db =>
        {
            var product = await db.Products.FirstAsync(p => p.Id == productId);
            product.BasePricePiastres = 999_00;
            await db.SaveChangesAsync();
            return true;
        });

        var reloaded = await _factory.QueryAsync(db => db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.OrderId));
        Assert.Equal(8000, reloaded.TotalPiastres);
    }

    [Fact]
    public async Task Renaming_a_product_after_the_sale_never_changes_the_old_orders_frozen_item_name()
    {
        var productId = await _factory.SeedProductAsync(5000, name: "شاورما فراخ");
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", Checkout(productId, null, 1));
        var order = (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;

        await _factory.QueryAsync(async db =>
        {
            var product = await db.Products.FirstAsync(p => p.Id == productId);
            product.Name = "اسم جديد بعد التعديل";
            await db.SaveChangesAsync();
            return true;
        });

        var item = await _factory.QueryAsync(db => db.OrderItems.AsNoTracking().FirstAsync(i => i.OrderId == order.OrderId));
        Assert.Equal("شاورما فراخ", item.ProductName);
    }

    [Fact]
    public async Task Deactivating_a_price_rule_after_the_sale_never_changes_the_old_orders_frozen_price()
    {
        var productId = await _factory.SeedProductAsync(10000);
        var ruleId = await _factory.SeedPriceRuleAsync(productId, 6000);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", Checkout(productId, null, 1, priceRuleId: ruleId));
        var order = (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
        Assert.Equal(6000, order.TotalPiastres);

        await _factory.QueryAsync(async db =>
        {
            var rule = await db.PriceRules.FirstAsync(r => r.Id == ruleId);
            rule.Active = false;
            await db.SaveChangesAsync();
            return true;
        });

        var reloaded = await _factory.QueryAsync(db => db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.OrderId));
        Assert.Equal(6000, reloaded.TotalPiastres); // untouched by the later deactivation
    }

    [Fact]
    public async Task Changing_a_recipes_quantity_after_the_sale_never_changes_the_old_orders_recorded_stock_movement()
    {
        var (productId, recipeId, inventoryItemId) = await _factory.SeedProductWithRecipeAsync(5000, 100);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", Checkout(productId, null, 1));
        response.EnsureSuccessStatusCode();

        var originalMovement = await _factory.QueryAsync(db => db.StockMovements.AsNoTracking().FirstAsync(m => m.InventoryItemId == inventoryItemId));
        Assert.Equal(100, originalMovement.QuantityMilliUnits);

        await _factory.QueryAsync(async db =>
        {
            var line = await db.RecipeItems.FirstAsync(i => i.RecipeId == recipeId);
            line.QuantityMilliUnits = 9999;
            await db.SaveChangesAsync();
            return true;
        });

        var reloadedMovement = await _factory.QueryAsync(db => db.StockMovements.AsNoTracking().FirstAsync(m => m.InventoryItemId == inventoryItemId));
        Assert.Equal(100, reloadedMovement.QuantityMilliUnits); // the historical record never gets rewritten
    }
}

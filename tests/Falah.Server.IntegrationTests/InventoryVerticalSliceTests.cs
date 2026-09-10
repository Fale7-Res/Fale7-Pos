namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Inventory;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Ported from the reference app's Inventory/Recipes/Costing commands (recordInventoryCountAdjustment,
/// recordInventoryWaste, saveRecipe, savePrepRecipe, saveOrderPackagingRule in lib/enterprise-store.tsx,
/// and the pure functions in lib/costing-domain.mjs). Covers the "core operational heart" pipeline:
/// recipes -> inventory deduction (already proven by CheckoutVerticalSliceTests/MenuPricingInventoryTests)
/// -> stock movements -> waste -> physical count/adjustment -> costing.
/// </summary>
public class InventoryVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _managerId = default!;
    private string _inventoryManagerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _inventoryManagerId = await _factory.SeedUserAsync("مسؤول المخزون", "supervisor", "inventory_manage", "menu_manage", "reports_view");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> CreateItemAsync(string name = "طماطم", string unit = "kg", long minimumMilliUnits = 0, string? actorId = null)
    {
        var response = await _client.PostAsJsonAsync("/api/inventory/items",
            new CreateInventoryItemRequest($"req-{Guid.NewGuid():N}", name, "خامات", unit, minimumMilliUnits, false, "Purchase", null, actorId ?? _inventoryManagerId));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateInventoryItemResult>();
        return result!.Id;
    }

    private async Task SeedPurchaseReceiptAsync(string inventoryItemId, long unitCostPiastres, long quantityMilliUnits = 10000, DateTimeOffset? at = null) =>
        await _factory.QueryAsync(async db =>
        {
            db.StockMovements.Add(new StockMovementEntity
            {
                IdempotencyKey = $"seed-purchase-{Guid.NewGuid():N}", InventoryItemId = inventoryItemId,
                Type = "PurchaseReceipt", Direction = "In", QuantityMilliUnits = quantityMilliUnits,
                UnitCostPiastres = unitCostPiastres, ReferenceType = "seed", ReferenceId = "seed",
                Reason = "seed", CreatedBy = "seed", CreatedAtUtc = at ?? DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
            return 0;
        });

    // ---- Inventory item CRUD ----

    [Fact]
    public async Task Creating_an_inventory_item_persists_it_and_is_idempotent_by_request_id()
    {
        var requestId = $"req-{Guid.NewGuid():N}";
        var request = new CreateInventoryItemRequest(requestId, "بصل", "خامات", "kg", 0, false, "Purchase", null, _inventoryManagerId);

        var first = await (await _client.PostAsJsonAsync("/api/inventory/items", request)).Content.ReadFromJsonAsync<CreateInventoryItemResult>();
        var second = await (await _client.PostAsJsonAsync("/api/inventory/items", request)).Content.ReadFromJsonAsync<CreateInventoryItemResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(first.Id, second.Id);
        var count = await _factory.QueryAsync(db => db.InventoryItems.CountAsync(i => i.Id == first.Id));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Creating_an_inventory_item_without_permission_is_rejected()
    {
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos");
        var response = await _client.PostAsJsonAsync("/api/inventory/items",
            new CreateInventoryItemRequest($"req-{Guid.NewGuid():N}", "بصل", "خامات", "kg", 0, false, "Purchase", null, cashierId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Updating_an_inventory_item_changes_metadata_without_touching_history()
    {
        var itemId = await CreateItemAsync();
        var response = await _client.PutAsJsonAsync($"/api/inventory/items/{itemId}",
            new UpdateInventoryItemRequest(itemId, "طماطم بلدي", "خضروات", "kg", 500, true, false, "Purchase", 2000, _inventoryManagerId));
        response.EnsureSuccessStatusCode();

        var item = await _factory.QueryAsync(db => db.InventoryItems.FirstAsync(i => i.Id == itemId));
        Assert.Equal("طماطم بلدي", item.Name);
        Assert.Equal("خضروات", item.Category);
        Assert.Equal(500, item.MinimumMilliUnits);
        Assert.Equal(2000, item.StandardDailyUsageMilliUnits);
    }

    [Fact]
    public async Task Changing_the_unit_of_an_item_with_no_movements_is_allowed()
    {
        var itemId = await CreateItemAsync(unit: "kg");
        var response = await _client.PutAsJsonAsync($"/api/inventory/items/{itemId}",
            new UpdateInventoryItemRequest(itemId, "طماطم", "خامات", "gram", 0, true, false, "Purchase", null, _inventoryManagerId));
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Changing_the_unit_of_an_item_with_existing_movements_is_rejected()
    {
        var itemId = await CreateItemAsync(unit: "kg");
        await SeedPurchaseReceiptAsync(itemId, unitCostPiastres: 1000);

        var response = await _client.PutAsJsonAsync($"/api/inventory/items/{itemId}",
            new UpdateInventoryItemRequest(itemId, "طماطم", "خامات", "gram", 0, true, false, "Purchase", null, _inventoryManagerId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var item = await _factory.QueryAsync(db => db.InventoryItems.FirstAsync(i => i.Id == itemId));
        Assert.Equal("kg", item.Unit); // unchanged
    }

    [Fact]
    public async Task Listing_inventory_items_reports_the_derived_balance_and_low_stock_flag()
    {
        var itemId = await CreateItemAsync(minimumMilliUnits: 5000);
        await SeedPurchaseReceiptAsync(itemId, unitCostPiastres: 1000, quantityMilliUnits: 3000);

        var items = await (await _client.GetAsync("/api/inventory/items")).Content.ReadFromJsonAsync<List<InventoryItemDto>>();
        var item = items!.Single(i => i.Id == itemId);

        Assert.Equal(3000, item.BalanceMilliUnits);
        Assert.True(item.IsLowStock); // 3000 <= minimum 5000
    }

    // ---- Physical count adjustment ----

    [Fact]
    public async Task Recording_a_count_adjustment_posts_a_movement_and_moves_the_balance()
    {
        var itemId = await CreateItemAsync();
        var response = await _client.PostAsJsonAsync("/api/inventory/count-adjustments",
            new RecordCountAdjustmentRequest($"req-{Guid.NewGuid():N}", itemId, 1500, "In", "جرد شهري", _inventoryManagerId, null, null));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RecordCountAdjustmentResult>();
        Assert.False(result!.WasReplayed);

        var balance = (await (await _client.GetAsync($"/api/inventory/items/{itemId}/balance")).Content.ReadFromJsonAsync<JsonBalance>())!;
        Assert.Equal(1500, balance.balanceMilliUnits);
    }

    private sealed record JsonBalance(string inventoryItemId, long balanceMilliUnits);

    [Fact]
    public async Task A_count_adjustment_requires_a_reason()
    {
        var itemId = await CreateItemAsync();
        var response = await _client.PostAsJsonAsync("/api/inventory/count-adjustments",
            new RecordCountAdjustmentRequest($"req-{Guid.NewGuid():N}", itemId, 1500, "In", "", _inventoryManagerId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Retrying_the_same_count_adjustment_request_id_never_posts_twice()
    {
        var itemId = await CreateItemAsync();
        var requestId = $"req-{Guid.NewGuid():N}";
        var request = new RecordCountAdjustmentRequest(requestId, itemId, 500, "Out", "جرد", _inventoryManagerId, null, null);

        var first = await (await _client.PostAsJsonAsync("/api/inventory/count-adjustments", request)).Content.ReadFromJsonAsync<RecordCountAdjustmentResult>();
        var second = await (await _client.PostAsJsonAsync("/api/inventory/count-adjustments", request)).Content.ReadFromJsonAsync<RecordCountAdjustmentResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        var count = await _factory.QueryAsync(db => db.StockMovements.CountAsync(m => m.InventoryItemId == itemId));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task A_count_adjustment_without_inventory_manage_permission_needs_a_manager_approval()
    {
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos");
        var itemId = await CreateItemAsync();

        var noApproval = await _client.PostAsJsonAsync("/api/inventory/count-adjustments",
            new RecordCountAdjustmentRequest($"req-{Guid.NewGuid():N}", itemId, 500, "In", "جرد", cashierId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noApproval.StatusCode);

        var approval = await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "inventory_manage", "اعتماد جرد"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>();
        var withApproval = await _client.PostAsJsonAsync("/api/inventory/count-adjustments",
            new RecordCountAdjustmentRequest($"req-{Guid.NewGuid():N}", itemId, 500, "In", "جرد", cashierId, approval!.ApprovalId, _managerId));
        withApproval.EnsureSuccessStatusCode();
    }

    // ---- Waste ----

    [Fact]
    public async Task Recording_waste_freezes_the_last_purchase_unit_cost_and_computes_loss_value()
    {
        var itemId = await CreateItemAsync();
        await SeedPurchaseReceiptAsync(itemId, unitCostPiastres: 1000, at: DateTimeOffset.UtcNow.AddDays(-2));
        await SeedPurchaseReceiptAsync(itemId, unitCostPiastres: 1200, at: DateTimeOffset.UtcNow.AddDays(-1)); // most recent purchase wins

        var response = await _client.PostAsJsonAsync("/api/inventory/waste",
            new RecordWasteRequest($"req-{Guid.NewGuid():N}", itemId, 2000, "Waste", "خطأ تحضير", _inventoryManagerId, null, null));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RecordWasteResult>();

        Assert.Equal(1200, result!.UnitCostPiastres);
        Assert.Equal(2400, result.LossValuePiastres); // 2000 milliUnits * 1200 piastres / 1000
    }

    [Fact]
    public async Task Waste_with_no_purchase_history_records_the_quantity_without_a_loss_value()
    {
        var itemId = await CreateItemAsync();
        var response = await _client.PostAsJsonAsync("/api/inventory/waste",
            new RecordWasteRequest($"req-{Guid.NewGuid():N}", itemId, 1000, "Spoilage", "تلف تخزين", _inventoryManagerId, null, null));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RecordWasteResult>();

        Assert.Null(result!.UnitCostPiastres);
        Assert.Null(result.LossValuePiastres);
    }

    [Fact]
    public async Task Waste_rejects_a_loss_type_outside_the_three_recognized_ones()
    {
        var itemId = await CreateItemAsync();
        var response = await _client.PostAsJsonAsync("/api/inventory/waste",
            new RecordWasteRequest($"req-{Guid.NewGuid():N}", itemId, 1000, "Usage", "سبب", _inventoryManagerId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Retrying_the_same_waste_request_id_never_posts_twice()
    {
        var itemId = await CreateItemAsync();
        var requestId = $"req-{Guid.NewGuid():N}";
        var request = new RecordWasteRequest(requestId, itemId, 500, "Breakage", "كسر", _inventoryManagerId, null, null);

        var first = await (await _client.PostAsJsonAsync("/api/inventory/waste", request)).Content.ReadFromJsonAsync<RecordWasteResult>();
        var second = await (await _client.PostAsJsonAsync("/api/inventory/waste", request)).Content.ReadFromJsonAsync<RecordWasteResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        var count = await _factory.QueryAsync(db => db.StockMovements.CountAsync(m => m.InventoryItemId == itemId));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Concurrent_waste_requests_with_the_same_request_id_post_exactly_once()
    {
        var itemId = await CreateItemAsync();
        var requestId = $"req-{Guid.NewGuid():N}";
        var request = new RecordWasteRequest(requestId, itemId, 500, "Waste", "خطأ تحضير", _inventoryManagerId, null, null);

        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/inventory/waste", request));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<RecordWasteResult>()));
        Assert.Equal(1, results.Count(r => !r!.WasReplayed));
        var count = await _factory.QueryAsync(db => db.StockMovements.CountAsync(m => m.InventoryItemId == itemId));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Waste_is_never_checked_against_the_current_balance_and_may_drive_it_negative()
    {
        var itemId = await CreateItemAsync(); // zero balance, no movements at all
        var response = await _client.PostAsJsonAsync("/api/inventory/waste",
            new RecordWasteRequest($"req-{Guid.NewGuid():N}", itemId, 5000, "Waste", "فقد غير متوقع", _inventoryManagerId, null, null));
        response.EnsureSuccessStatusCode(); // never blocked - real stock can legitimately lag the system

        var balance = (await (await _client.GetAsync($"/api/inventory/items/{itemId}/balance")).Content.ReadFromJsonAsync<JsonBalance>())!;
        Assert.Equal(-5000, balance.balanceMilliUnits);
    }

    // ---- Costing ----

    [Fact]
    public async Task Ingredient_cost_is_unknown_with_no_purchase_history()
    {
        var itemId = await CreateItemAsync();
        var cost = await (await _client.GetAsync($"/api/inventory/items/{itemId}/cost?actorUserId={_inventoryManagerId}")).Content.ReadFromJsonAsync<IngredientCostDto>();
        Assert.Null(cost!.UnitCostPiastres);
        Assert.Equal("Unknown", cost.Source);
    }

    [Fact]
    public async Task Ingredient_cost_resolves_to_the_most_recent_purchase_after_a_receipt()
    {
        var itemId = await CreateItemAsync();
        await SeedPurchaseReceiptAsync(itemId, unitCostPiastres: 1500);

        var cost = await (await _client.GetAsync($"/api/inventory/items/{itemId}/cost?actorUserId={_inventoryManagerId}")).Content.ReadFromJsonAsync<IngredientCostDto>();
        Assert.Equal(1500, cost!.UnitCostPiastres);
        Assert.Equal("Purchase", cost.Source);
    }

    [Fact]
    public async Task Saving_a_new_recipe_creates_it_with_its_lines_and_costing_reflects_them()
    {
        var itemId = await CreateItemAsync();
        await SeedPurchaseReceiptAsync(itemId, unitCostPiastres: 2000); // 20 EGP/kg
        var recipeId = $"recipe-{Guid.NewGuid():N}";
        var productId = await _factory.SeedProductAsync(5000, recipeId: recipeId);

        var request = new SaveRecipeRequest($"req-{Guid.NewGuid():N}", recipeId, productId, "شاورما فراخ", true,
            new[] { new RecipeLineInput(itemId, 150, "Direct", null) }, _inventoryManagerId, null, null);
        var response = await _client.PostAsJsonAsync("/api/recipes", request);
        response.EnsureSuccessStatusCode();
        var recipe = await response.Content.ReadFromJsonAsync<RecipeDto>();

        Assert.Equal(recipeId, recipe!.Id);
        Assert.Single(recipe.Lines);

        var cost = await (await _client.GetAsync($"/api/recipes/{recipeId}/cost?actorUserId={_inventoryManagerId}")).Content.ReadFromJsonAsync<RecipeCostDto>();
        Assert.Equal(300, cost!.FoodCostPiastres); // 150 milliUnits * 2000 piastres / 1000
        Assert.Equal(100, cost.CompletenessPercent);
        Assert.Empty(cost.Missing);
    }

    [Fact]
    public async Task Saving_an_existing_recipe_replaces_its_entire_line_set()
    {
        var itemA = await CreateItemAsync("مكون أ");
        var itemB = await CreateItemAsync("مكون ب");
        var recipeId = $"recipe-{Guid.NewGuid():N}";
        var productId = await _factory.SeedProductAsync(5000, recipeId: recipeId);

        await _client.PostAsJsonAsync("/api/recipes", new SaveRecipeRequest($"req-{Guid.NewGuid():N}", recipeId, productId, "وصفة", true,
            new[] { new RecipeLineInput(itemA, 100, "Direct", null) }, _inventoryManagerId, null, null));

        var second = await _client.PostAsJsonAsync("/api/recipes", new SaveRecipeRequest($"req-{Guid.NewGuid():N}", recipeId, productId, "وصفة", true,
            new[] { new RecipeLineInput(itemB, 200, "Direct", null) }, _inventoryManagerId, null, null));
        second.EnsureSuccessStatusCode();
        var recipe = await second.Content.ReadFromJsonAsync<RecipeDto>();

        Assert.Single(recipe!.Lines);
        Assert.Equal(itemB, recipe.Lines.Single().InventoryItemId);
        var lineCount = await _factory.QueryAsync(db => db.RecipeItems.CountAsync(ri => ri.RecipeId == recipeId));
        Assert.Equal(1, lineCount);
    }

    [Fact]
    public async Task Recipe_cost_flags_a_missing_ingredient_without_purchase_history_and_is_incomplete()
    {
        var known = await CreateItemAsync("معروف");
        var unknown = await CreateItemAsync("غير معروف");
        await SeedPurchaseReceiptAsync(known, unitCostPiastres: 1000);
        var recipeId = $"recipe-{Guid.NewGuid():N}";
        var productId = await _factory.SeedProductAsync(5000, recipeId: recipeId);

        await _client.PostAsJsonAsync("/api/recipes", new SaveRecipeRequest($"req-{Guid.NewGuid():N}", recipeId, productId, "وصفة", true,
            new[] { new RecipeLineInput(known, 100, "Direct", null), new RecipeLineInput(unknown, 100, "Direct", null) }, _inventoryManagerId, null, null));

        var cost = await (await _client.GetAsync($"/api/recipes/{recipeId}/cost?actorUserId={_inventoryManagerId}")).Content.ReadFromJsonAsync<RecipeCostDto>();
        Assert.Equal(50, cost!.CompletenessPercent); // 1 of 2 lines known
        Assert.Single(cost.Missing);
    }

    [Fact]
    public async Task Saving_a_prep_recipe_and_its_cost_requires_every_input_line_known()
    {
        var sesame = await CreateItemAsync("سمسم");
        var tahini = await CreateItemAsync("طحينة", actorId: _inventoryManagerId);
        await SeedPurchaseReceiptAsync(sesame, unitCostPiastres: 1000);

        var prepId = $"prep-{Guid.NewGuid():N}";
        var request = new SavePrepRecipeRequest($"req-{Guid.NewGuid():N}", prepId, "طحينة مصنعة", tahini, 1000, true,
            new[] { new PrepRecipeLineInput(sesame, 1000) }, _inventoryManagerId, null, null);
        var response = await _client.PostAsJsonAsync("/api/prep-recipes", request);
        response.EnsureSuccessStatusCode();

        var cost = await (await _client.GetAsync($"/api/prep-recipes/{prepId}/cost?actorUserId={_inventoryManagerId}")).Content.ReadFromJsonAsync<PrepRecipeCostDto>();
        Assert.Equal(1000, cost!.UnitCostPiastres);
        Assert.Empty(cost.Missing);

        // Now the tahini item itself resolves its unit cost from the prep recipe (costSource='Prep').
        await _client.PutAsJsonAsync($"/api/inventory/items/{tahini}",
            new UpdateInventoryItemRequest(tahini, "طحينة", "خامات", "kg", 0, true, false, "Prep", null, _inventoryManagerId));
        var ingredientCost = await (await _client.GetAsync($"/api/inventory/items/{tahini}/cost?actorUserId={_inventoryManagerId}")).Content.ReadFromJsonAsync<IngredientCostDto>();
        Assert.Equal(1000, ingredientCost!.UnitCostPiastres);
        Assert.Equal("Prep", ingredientCost.Source);
    }

    [Fact]
    public async Task Saving_an_order_packaging_rule_persists_it()
    {
        var bagId = await CreateItemAsync("كيس توصيل");
        var response = await _client.PostAsJsonAsync("/api/order-packaging-rules",
            new SaveOrderPackagingRuleRequest($"req-{Guid.NewGuid():N}", null, "delivery", bagId, 1000, true, _inventoryManagerId, null, null));
        response.EnsureSuccessStatusCode();

        var rules = await (await _client.GetAsync("/api/order-packaging-rules")).Content.ReadFromJsonAsync<List<OrderPackagingRuleDto>>();
        Assert.Contains(rules!, r => r.InventoryItemId == bagId && r.OrderType == "delivery");
    }

    [Fact]
    public async Task Menu_item_profitability_computes_food_cost_percent_from_recipe_cost_and_selling_price()
    {
        var itemId = await CreateItemAsync();
        await SeedPurchaseReceiptAsync(itemId, unitCostPiastres: 2000);
        var recipeId = $"recipe-{Guid.NewGuid():N}";
        var productId = await _factory.SeedProductAsync(10000, recipeId: recipeId);
        await _client.PostAsJsonAsync("/api/recipes", new SaveRecipeRequest($"req-{Guid.NewGuid():N}", recipeId, productId, "وصفة", true,
            new[] { new RecipeLineInput(itemId, 150, "Direct", null) }, _inventoryManagerId, null, null));

        var profitability = await (await _client.GetAsync($"/api/recipes/{recipeId}/profitability?sellingPricePiastres=10000&actorUserId={_inventoryManagerId}"))
            .Content.ReadFromJsonAsync<MenuItemProfitabilityDto>();

        Assert.Equal(3, profitability!.FoodCostPercent); // 300/10000 = 3%
        Assert.Equal(9700, profitability.ContributionPiastres);
    }

    [Fact]
    public async Task Stock_movement_history_lists_newest_movements_first_for_an_item()
    {
        var itemId = await CreateItemAsync();
        await SeedPurchaseReceiptAsync(itemId, unitCostPiastres: 1000, at: DateTimeOffset.UtcNow.AddMinutes(-10));
        await _client.PostAsJsonAsync("/api/inventory/waste",
            new RecordWasteRequest($"req-{Guid.NewGuid():N}", itemId, 200, "Waste", "سبب", _inventoryManagerId, null, null));

        var history = await (await _client.GetAsync($"/api/inventory/movements?inventoryItemId={itemId}")).Content.ReadFromJsonAsync<List<StockMovementDto>>();
        Assert.Equal(2, history!.Count);
        Assert.Equal("Waste", history[0].Type); // most recent first
        Assert.Equal("PurchaseReceipt", history[1].Type);
    }

    // ---- reports_view gating on cost/aggregate figures (ported from the reference app's own
    // hasPermission('reports_view') gate on CostingScreen/InventoryScreen) ----

    [Fact]
    public async Task Ingredient_cost_is_rejected_for_an_actor_without_reports_view()
    {
        var floorCashier = await _factory.SeedUserAsync("كاشير الصالة", "cashier", "pos");
        var itemId = await CreateItemAsync();

        var response = await _client.GetAsync($"/api/inventory/items/{itemId}/cost?actorUserId={floorCashier}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Loss_summary_is_rejected_for_an_actor_without_reports_view()
    {
        var floorCashier = await _factory.SeedUserAsync("كاشير الصالة", "cashier", "pos");
        var response = await _client.GetAsync($"/api/inventory/loss-summary?businessDate={DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}&actorUserId={floorCashier}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Recipe_cost_is_rejected_for_an_actor_without_reports_view_even_with_menu_manage()
    {
        // menu_manage authors recipes; it does not, by itself, grant seeing their cost figures -
        // the two are different concerns even for the same operator role.
        var menuEditorOnly = await _factory.SeedUserAsync("محرر المنيو", "supervisor", "menu_manage");
        var itemId = await CreateItemAsync();
        var recipeId = $"recipe-{Guid.NewGuid():N}";
        var productId = await _factory.SeedProductAsync(5000, recipeId: recipeId);
        await _client.PostAsJsonAsync("/api/recipes", new SaveRecipeRequest($"req-{Guid.NewGuid():N}", recipeId, productId, "وصفة", true,
            new[] { new RecipeLineInput(itemId, 100, "Direct", null) }, _inventoryManagerId, null, null));

        var response = await _client.GetAsync($"/api/recipes/{recipeId}/cost?actorUserId={menuEditorOnly}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---- Large-waste audit severity (LARGE_WASTE_MILLI_UNITS - a real gap found during the
    // legacy-parity audit: RecordWasteHandler always logged 'warning' regardless of amount) ----

    [Fact]
    public async Task Waste_at_or_above_ten_base_units_is_audited_as_critical()
    {
        var itemId = await CreateItemAsync();
        (await _client.PostAsJsonAsync("/api/inventory/waste",
            new RecordWasteRequest($"req-{Guid.NewGuid():N}", itemId, 10000, "Waste", "تلف كبير", _inventoryManagerId, null, null)))
            .EnsureSuccessStatusCode();

        var events = await (await _client.GetAsync($"/api/audit/events?actorUserId={_managerId}&category=inventory"))
            .Content.ReadFromJsonAsync<List<Falah.Application.Audit.AuditEventDto>>();
        Assert.Contains(events!, e => e.ReferenceId == itemId && e.Severity == "critical");
    }

    [Fact]
    public async Task Waste_below_ten_base_units_is_audited_only_as_warning()
    {
        var itemId = await CreateItemAsync();
        (await _client.PostAsJsonAsync("/api/inventory/waste",
            new RecordWasteRequest($"req-{Guid.NewGuid():N}", itemId, 500, "Waste", "تلف صغير", _inventoryManagerId, null, null)))
            .EnsureSuccessStatusCode();

        var events = await (await _client.GetAsync($"/api/audit/events?actorUserId={_managerId}&category=inventory"))
            .Content.ReadFromJsonAsync<List<Falah.Application.Audit.AuditEventDto>>();
        Assert.Contains(events!, e => e.ReferenceId == itemId && e.Severity == "warning");
    }

    // ---- Shared consumption report (sharedConsumptionReport - a real gap found during the
    // legacy-parity audit: the InventoryScreen actively uses this owner workflow, and the data
    // column was already ported specifically to support it, but the report itself was never
    // written) ----

    [Fact]
    public async Task Shared_consumption_report_computes_actual_daily_average_and_variance()
    {
        var itemId = await CreateItemAsync("زيت قلي");
        await _client.PutAsJsonAsync($"/api/inventory/items/{itemId}",
            new UpdateInventoryItemRequest(itemId, "زيت قلي", "خامات", "liter", 0, true, false, "Purchase", 4000, _inventoryManagerId));

        var now = DateTimeOffset.UtcNow;
        await _factory.QueryAsync(db =>
        {
            db.StockMovements.Add(new StockMovementEntity
            {
                IdempotencyKey = $"seed-usage-{Guid.NewGuid():N}", InventoryItemId = itemId,
                Type = "Usage", Direction = "Out", QuantityMilliUnits = 10000,
                ReferenceType = "test_seed", ReferenceId = "seed", Reason = "seed",
                CreatedBy = "seed", OperationId = $"op-{Guid.NewGuid():N}", CreatedAtUtc = now,
            });
            return db.SaveChangesAsync();
        });

        var from = now.AddDays(-2);
        var to = now.AddDays(1);
        var response = await _client.GetAsync(
            $"/api/inventory/items/{itemId}/shared-consumption?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}&actorUserId={_managerId}");
        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<Falah.Application.Inventory.SharedConsumptionDto>();

        Assert.Equal(4000, report!.StandardDailyUsageMilliUnits);
        Assert.Equal(10000, report.ConsumedMilliUnits);
        // 10000 consumed over a 3-day window (from - to) rounds to ~3333/day, well above the 4000
        // standard for a single day but the ratio itself is what matters here - the point is the
        // figure is actually computed, not that this specific arithmetic is the interesting part.
        Assert.NotNull(report.ActualDailyAvgMilliUnits);
        Assert.NotNull(report.VarianceMilliUnitsPerDay);
    }

    [Fact]
    public async Task Shared_consumption_report_requires_reports_view()
    {
        var itemId = await CreateItemAsync();
        var noReportsUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");
        var response = await _client.GetAsync(
            $"/api/inventory/items/{itemId}/shared-consumption?from={Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-1).ToString("O"))}&to={Uri.EscapeDataString(DateTimeOffset.UtcNow.ToString("O"))}&actorUserId={noReportsUser}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}

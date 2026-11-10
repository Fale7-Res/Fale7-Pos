namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Inventory;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>INV-0003 end to end: a recipe that uses an ingredient by YIELD (three portions per kilo)
/// sold three at a time must take exactly one kilo out of stock, not 999 g.</summary>
public class YieldDeductionTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير العائد", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _managerId = await _factory.SeedUserAsync("مدير العائد", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        _shiftId = (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    [Theory]
    [InlineData(3, 1000)]
    [InlineData(7, 2333)]
    public async Task Selling_portions_of_a_yield_recipe_deducts_the_whole_weight_rounded_once(int quantity, long expectedMilli)
    {
        var item = await (await _client.PostAsJsonAsync("/api/inventory/items", new CreateInventoryItemRequest(
            $"req-{Guid.NewGuid():N}", "دجاج مشوي", "خامات", "kg", 0, false, "Purchase", null, _managerId)))
            .EnsureSuccessStatusCode().Content.ReadFromJsonAsync<CreateInventoryItemResult>();
        var productId = await _factory.SeedProductAsync(5000);
        (await _client.PostAsJsonAsync("/api/recipes", new SaveRecipeRequest($"req-{Guid.NewGuid():N}", $"recipe-{Guid.NewGuid():N}", productId,
            "ربع فرخة", true, new[] { new RecipeLineInput(item!.Id, 1000, "Yield", 3) }, _managerId, null, null))).EnsureSuccessStatusCode();

        var sale = await (await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            $"req-{Guid.NewGuid():N}", _cashierId, _shiftId, "takeaway", "cash",
            new[] { new CheckoutItemRequest(productId, null, quantity, null, null) }, 0, 0, 0, null, null, null,
            5000L * quantity, null, null, null, null, null, null, null, null)))
            .EnsureSuccessStatusCode().Content.ReadFromJsonAsync<CheckoutOrderResult>();

        var deducted = await _factory.QueryAsync(db => db.StockMovements
            .Where(m => m.ReferenceId == sale!.OrderId && m.InventoryItemId == item.Id && m.Type == "Usage")
            .SumAsync(m => m.QuantityMilliUnits));
        Assert.Equal(expectedMilli, deducted);
    }
}

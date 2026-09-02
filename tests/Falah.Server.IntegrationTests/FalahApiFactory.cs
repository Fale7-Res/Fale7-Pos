namespace Falah.Server.IntegrationTests;

using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Boots the REAL Falah.Server (real minimal-API endpoints, real EF Core, a real SQLite file on
/// disk - not an in-memory provider) so these tests exercise the exact path a physical Cashier/
/// Call Center request takes, migrations included. Each factory instance gets its own SQLite file
/// under the repo (never the system temp dir - the dev machine's C: drive has been observed to run
/// out of space entirely) so tests never share state unless a test deliberately shares a factory.
/// </summary>
public sealed class FalahApiFactory : WebApplicationFactory<Program>
{
    private static readonly string DataDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".testdata");
    public string DbPath { get; }
    private readonly bool _ownsFile;

    /// <param name="reuseDbPath">Pass another factory's DbPath to boot a second, independent
    /// server instance against the SAME database file - the cleanest way to prove that something
    /// (e.g. a persisted manager approval) genuinely survives a full Server restart, not just a
    /// method call within the same running process. The reusing instance does not delete the file
    /// on dispose; only the original owner does, once both are done with it.</param>
    public FalahApiFactory(string? reuseDbPath = null)
    {
        Directory.CreateDirectory(DataDir);
        _ownsFile = reuseDbPath is null;
        DbPath = reuseDbPath ?? Path.Combine(DataDir, $"falah-test-{Guid.NewGuid():N}.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Falah"] = $"Data Source={DbPath};Cache=Shared",
            });
        });
    }

    public async Task<string> SeedUserAsync(string name, string role, params string[] permissions)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var id = $"user-{Guid.NewGuid():N}";
        // A random-but-unique pin - no test in this suite exercises PIN login itself, so any value
        // satisfying the unique constraint is fine here.
        db.Users.Add(new UserEntity { Id = id, Name = name, Role = role, Active = true, Permissions = string.Join(",", permissions), Pin = Guid.NewGuid().ToString("N")[..6] });
        await db.SaveChangesAsync();
        return id;
    }

    /// <summary>Seeds a minimal, available, grill-flagged-as-requested product at an exact price -
    /// the price the server will look up and freeze, never something the test's HTTP request
    /// carries. Most tests only need one product at a specific total for a quantity-1 line.</summary>
    public async Task<string> SeedProductAsync(long pricePiastres, string name = "منتج اختبار", bool sendToGrill = false, string? recipeId = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var categoryId = $"cat-{Guid.NewGuid():N}";
        db.Categories.Add(new CategoryEntity { Id = categoryId, Name = "قسم اختبار", SortOrder = 0, Active = true });
        var productId = $"prod-{Guid.NewGuid():N}";
        db.Products.Add(new ProductEntity
        {
            Id = productId, CategoryId = categoryId, Name = name, BasePricePiastres = pricePiastres,
            SendToGrill = sendToGrill, IsAvailable = true, SortOrder = 0, RecipeId = recipeId,
        });
        await db.SaveChangesAsync();
        return productId;
    }

    public async Task<string> SeedVariantAsync(string productId, long pricePiastres, string name = "حجم كبير", string? recipeId = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var variantId = $"var-{Guid.NewGuid():N}";
        db.ProductVariants.Add(new ProductVariantEntity { Id = variantId, ProductId = productId, Name = name, PricePiastres = pricePiastres, RecipeId = recipeId });
        await db.SaveChangesAsync();
        return variantId;
    }

    public async Task<string> SeedPriceRuleAsync(string productId, long fixedPricePiastres, bool active = true, TimeOnly? start = null, TimeOnly? end = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var ruleId = $"rule-{Guid.NewGuid():N}";
        db.PriceRules.Add(new PriceRuleEntity
        {
            Id = ruleId, Name = "عرض اختبار", FixedPricePiastres = fixedPricePiastres, StartTime = start, EndTime = end, Active = active,
            Products = { new PriceRuleProductEntity { PriceRuleId = ruleId, ProductId = productId } },
        });
        await db.SaveChangesAsync();
        return ruleId;
    }

    /// <summary>Seeds a product whose recipe consumes `quantityMilliUnitsPerUnit` of one fresh
    /// inventory item per unit sold - everything a checkout needs to exercise real stock
    /// deduction end to end.</summary>
    public async Task<(string ProductId, string RecipeId, string InventoryItemId)> SeedProductWithRecipeAsync(long pricePiastres, long quantityMilliUnitsPerUnit)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var categoryId = $"cat-{Guid.NewGuid():N}";
        db.Categories.Add(new CategoryEntity { Id = categoryId, Name = "قسم اختبار", SortOrder = 0, Active = true });
        var inventoryItemId = $"inv-{Guid.NewGuid():N}";
        db.InventoryItems.Add(new InventoryItemEntity { Id = inventoryItemId, Name = "مكوّن اختبار", CostSource = "Purchase" });
        var productId = $"prod-{Guid.NewGuid():N}";
        var recipeId = $"recipe-{Guid.NewGuid():N}";
        db.Recipes.Add(new RecipeEntity
        {
            Id = recipeId, ProductId = productId, Name = "وصفة اختبار", Active = true,
            Items = { new RecipeItemEntity { RecipeId = recipeId, InventoryItemId = inventoryItemId, QuantityMilliUnits = quantityMilliUnitsPerUnit, Kind = "Direct" } },
        });
        db.Products.Add(new ProductEntity
        {
            Id = productId, CategoryId = categoryId, Name = "منتج بوصفة", BasePricePiastres = pricePiastres,
            SendToGrill = false, IsAvailable = true, SortOrder = 0, RecipeId = recipeId,
        });
        await db.SaveChangesAsync();
        return (productId, recipeId, inventoryItemId);
    }

    public async Task<T> QueryAsync<T>(Func<FalahDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        return await query(db);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!_ownsFile) return; // a "restart" factory reusing another's file never deletes it
        foreach (var suffix in new[] { "", "-shm", "-wal", "-journal" })
        {
            try { File.Delete(DbPath + suffix); } catch { /* best-effort cleanup */ }
        }
    }
}

namespace Falah.Infrastructure.Persistence;

using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Minimal sample menu/users so a fresh install has something to sell immediately - a real
/// restaurant will replace this via a future Menu Management screen, not by editing this file.
/// Only runs once, when Products is empty, so it never overwrites real data on a second run.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedIfEmptyAsync(FalahDbContext db, CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(ct)) return;

        var cashierId = "user-demo-cashier";
        var managerId = "user-demo-manager";
        db.Users.AddRange(
            // PINs match the login/lock-screen pattern of the reference Falah app exactly: the
            // screen asks for a PIN alone, never a username - these are simply memorable demo values.
            new UserEntity { Id = cashierId, Name = "أحمد الكاشير", Role = "cashier", Active = true, Permissions = "pos,orders_refund,orders_comp", Pin = "1234" },
            new UserEntity { Id = managerId, Name = "مدير المحل", Role = "manager", Active = true, Permissions = "all", Pin = "9999" });

        var sandwichCategoryId = "cat-demo-sandwiches";
        var drinksCategoryId = "cat-demo-drinks";
        db.Categories.AddRange(
            new CategoryEntity { Id = sandwichCategoryId, Name = "ساندوتشات", Icon = "🥪", SortOrder = 0, Active = true },
            new CategoryEntity { Id = drinksCategoryId, Name = "مشروبات", Icon = "🥤", SortOrder = 1, Active = true });

        var chickenInventoryId = "inv-demo-chicken";
        db.InventoryItems.Add(new InventoryItemEntity { Id = chickenInventoryId, Name = "لحم فراخ", CostSource = "Purchase" });

        var shawarmaId = "prod-demo-shawarma";
        var shawarmaRecipeId = "recipe-demo-shawarma";
        db.Recipes.Add(new RecipeEntity
        {
            Id = shawarmaRecipeId, ProductId = shawarmaId, Name = "وصفة شاورما فراخ", Active = true,
            Items = { new RecipeItemEntity { RecipeId = shawarmaRecipeId, InventoryItemId = chickenInventoryId, QuantityMilliUnits = 150, Kind = "Direct" } },
        });
        db.Products.Add(new ProductEntity
        {
            Id = shawarmaId, CategoryId = sandwichCategoryId, Name = "شاورما فراخ", BasePricePiastres = 5000,
            SendToGrill = true, IsAvailable = true, SortOrder = 0, RecipeId = shawarmaRecipeId,
            Variants =
            {
                new ProductVariantEntity { Id = "var-demo-shawarma-somoon", ProductId = shawarmaId, Name = "صمون", PricePiastres = 5000 },
                new ProductVariantEntity { Id = "var-demo-shawarma-frensawy", ProductId = shawarmaId, Name = "عيش فرنساوي", PricePiastres = 5500 },
            },
        });

        var kebdaId = "prod-demo-kebda";
        db.Products.Add(new ProductEntity
        {
            Id = kebdaId, CategoryId = sandwichCategoryId, Name = "كبدة اسكندراني", BasePricePiastres = 4500,
            SendToGrill = true, IsAvailable = true, SortOrder = 1,
        });

        var colaId = "prod-demo-cola";
        db.Products.Add(new ProductEntity
        {
            Id = colaId, CategoryId = drinksCategoryId, Name = "كولا", BasePricePiastres = 1000,
            SendToGrill = false, IsAvailable = true, SortOrder = 0,
        });

        var offerId = "rule-demo-shawarma-offer";
        db.PriceRules.Add(new PriceRuleEntity
        {
            Id = offerId, Name = "عرض الشاورما", FixedPricePiastres = 4000, Active = true,
            Products = { new PriceRuleProductEntity { PriceRuleId = offerId, ProductId = shawarmaId } },
        });

        await db.SaveChangesAsync(ct);
    }
}

namespace Falah.Infrastructure.Persistence;

using Falah.Domain.Authorization;
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
        // PINs match the login/lock-screen pattern of the reference Falah app exactly: the screen
        // asks for a PIN alone, never a username - these are simply memorable demo values, hashed
        // exactly like a real user's PIN would be (PinHasher.AssertValidPin also rejects an
        // all-repeated-digit PIN like the reference app's own "9999" demo value used to be - changed
        // to "4321" here for that reason).
        var cashierPin = PinHasher.Hash("1234");
        var managerPin = PinHasher.Hash("4321");
        db.Users.AddRange(
            new UserEntity
            {
                Id = cashierId, Name = "أحمد الكاشير", Role = "cashier", Active = true,
                Permissions = "pos,orders_refund,orders_comp,shifts_open_own,shifts_blind_close,expenses_add_drawer,safe_drop",
                PinHash = cashierPin.HashBase64, PinSalt = cashierPin.SaltBase64, PinIterations = cashierPin.Iterations,
            },
            new UserEntity
            {
                Id = managerId, Name = "مدير المحل", Role = "manager", Active = true, Permissions = "all",
                PinHash = managerPin.HashBase64, PinSalt = managerPin.SaltBase64, PinIterations = managerPin.Iterations,
            });

        var sandwichCategoryId = "cat-demo-sandwiches";
        var drinksCategoryId = "cat-demo-drinks";

        // Real Falah category/product breadth (drawn from the approved reference app's own seed
        // data, lib/mock-data.ts) so the WinUI catalogue can be designed and tested against
        // realistic scale - many categories, many products, long Arabic names, mixed
        // variant/no-variant items - not just the two-category slice the first cut shipped with.
        var meatCategoryId = "cat-demo-meat-sandwiches";
        var grillsCategoryId = "cat-demo-grills";
        var mealsCategoryId = "cat-demo-meals";
        var platesCategoryId = "cat-demo-plates";
        var pastriesCategoryId = "cat-demo-pastries";
        var riceCategoryId = "cat-demo-rice";
        var pastaCategoryId = "cat-demo-pasta";
        var appetizersCategoryId = "cat-demo-appetizers";

        db.Categories.AddRange(
            new CategoryEntity { Id = sandwichCategoryId, Name = "ساندوتشات فالح", Icon = "🥪", SortOrder = 0, Active = true },
            new CategoryEntity { Id = meatCategoryId, Name = "ساندوتشات اللحوم", Icon = "🥩", SortOrder = 1, Active = true },
            new CategoryEntity { Id = grillsCategoryId, Name = "مشويات فالح", Icon = "🍗", SortOrder = 2, Active = true },
            new CategoryEntity { Id = mealsCategoryId, Name = "وجبات فالح", Icon = "🍱", SortOrder = 3, Active = true },
            new CategoryEntity { Id = platesCategoryId, Name = "أطباق فالح", Icon = "🥗", SortOrder = 4, Active = true },
            new CategoryEntity { Id = pastriesCategoryId, Name = "المعجنات", Icon = "🥟", SortOrder = 5, Active = true },
            new CategoryEntity { Id = riceCategoryId, Name = "الأرز", Icon = "🍚", SortOrder = 6, Active = true },
            new CategoryEntity { Id = pastaCategoryId, Name = "المكرونات", Icon = "🍝", SortOrder = 7, Active = true },
            new CategoryEntity { Id = appetizersCategoryId, Name = "المقبلات", Icon = "🥣", SortOrder = 8, Active = true },
            new CategoryEntity { Id = drinksCategoryId, Name = "المشروبات", Icon = "🥤", SortOrder = 9, Active = true });

        var chickenInventoryId = "inv-demo-chicken";
        db.InventoryItems.Add(new InventoryItemEntity
        {
            Id = chickenInventoryId, Name = "لحم فراخ", CostSource = "Purchase",
            Category = "خامات", Unit = "kg", MinimumMilliUnits = 0, Active = true,
        });

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
            SendToGrill = true, PreparationStation = "Grill", IsAvailable = true, SortOrder = 0, RecipeId = shawarmaRecipeId,
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
            SendToGrill = true, PreparationStation = "Charcoal", IsAvailable = true, SortOrder = 1,
        });

        var colaId = "prod-demo-cola";
        db.Products.Add(new ProductEntity
        {
            Id = colaId, CategoryId = drinksCategoryId, Name = "كولا", BasePricePiastres = 1000,
            SendToGrill = false, IsAvailable = true, SortOrder = 0,
        });

        // ---- Everything below is additional real Falah menu breadth (names/prices drawn from
        // the reference app's own lib/mock-data.ts) so the catalogue can be designed and tested
        // against realistic scale rather than the original two-category slice. ----

        // Every existing SendToGrill=true demo product defaults to the Grill station - a reasonable,
        // low-risk mapping that preserves the flag's existing meaning while making print-job routing
        // real out of the box (kebda above is the one deliberate Charcoal exception, for demo/test
        // variety across both kitchen stations).
        void Simple(string id, string categoryId, string name, bool grill, int sortOrder, long priceEgp) =>
            db.Products.Add(new ProductEntity
            {
                Id = id, CategoryId = categoryId, Name = name, BasePricePiastres = priceEgp * 100,
                SendToGrill = grill, PreparationStation = grill ? "Grill" : null, IsAvailable = true, SortOrder = sortOrder,
            });

        void WithVariants(string id, string categoryId, string name, bool grill, int sortOrder, params (string Id, string Name, long PriceEgp)[] variants)
        {
            var product = new ProductEntity
            {
                Id = id, CategoryId = categoryId, Name = name, BasePricePiastres = variants[0].PriceEgp * 100,
                SendToGrill = grill, PreparationStation = grill ? "Grill" : null, IsAvailable = true, SortOrder = sortOrder,
            };
            foreach (var v in variants)
                product.Variants.Add(new ProductVariantEntity { Id = v.Id, ProductId = id, Name = v.Name, PricePiastres = v.PriceEgp * 100 });
            db.Products.Add(product);
        }

        // ساندوتشات فالح - more of the same category as شاورما/كبدة above.
        WithVariants("prod-demo-falafel", sandwichCategoryId, "ساندوتش فلافل", false, 2,
            ("var-falafel-samoon", "صمون", 25), ("var-falafel-saj", "صاج", 25), ("var-falafel-french", "فرنساوي", 30));
        WithVariants("prod-demo-potato-mozzarella", sandwichCategoryId, "ساندوتش بطاطس موزاريلا", false, 3,
            ("var-pm-samoon", "صمون", 45), ("var-pm-saj", "صاج", 45), ("var-pm-french", "فرنساوي", 50));
        Simple("prod-demo-labneh-sw", sandwichCategoryId, "ساندوتش لبنة", false, 4, 25);

        // ساندوتشات اللحوم - long compound names, grill items, 3 variants each.
        WithVariants("prod-demo-kofta-charcoal-sw", meatCategoryId, "ساندوتش كفتة ع الفحم", true, 0,
            ("var-kc-samoon", "صمون", 90), ("var-kc-saj", "صاج", 90), ("var-kc-french", "فرنساوي", 95));
        WithVariants("prod-demo-kofta-mozzarella-sw", meatCategoryId, "ساندوتش كفتة موزاريلا", true, 1,
            ("var-km-samoon", "صمون", 100), ("var-km-saj", "صاج", 100), ("var-km-french", "فرنساوي", 105));
        WithVariants("prod-demo-shish-tawook-sw", meatCategoryId, "ساندوتش شيش طاووق", true, 2,
            ("var-st-samoon", "صمون", 90), ("var-st-saj", "صاج", 90), ("var-st-french", "فرنساوي", 100));
        WithVariants("prod-demo-shish-mozzarella-sw", meatCategoryId, "ساندوتش شيش موزاريلا", true, 3,
            ("var-sm-samoon", "صمون", 100), ("var-sm-saj", "صاج", 100), ("var-sm-french", "فرنساوي", 105));

        // مشويات فالح - no variants, always grill, wider price range, "تشمل سلطة + خبز" is real
        // menu phrasing but Notes isn't modeled on ProductEntity yet, so it's omitted here.
        Simple("prod-demo-quarter-thigh", grillsCategoryId, "ربع فرخة وراك (مشوي)", true, 0, 100);
        Simple("prod-demo-half-chicken", grillsCategoryId, "نص فرخة مشوية ع الفحم", true, 1, 165);
        Simple("prod-demo-quarter-kofta", grillsCategoryId, "ربع كيلو كفتة مشوية", true, 2, 150);
        Simple("prod-demo-whole-charcoal", grillsCategoryId, "فرخة فحم كاملة", true, 3, 340);
        Simple("prod-demo-whole-shish", grillsCategoryId, "فرخة شيش كاملة", true, 4, 345);

        // وجبات فالح
        Simple("prod-demo-meal-econ-thigh", mealsCategoryId, "وجبة اقتصادية ورك", true, 0, 110);
        Simple("prod-demo-meal-kabsa", mealsCategoryId, "وجبة كبسة", true, 1, 190);
        Simple("prod-demo-meal-family", mealsCategoryId, "وجبة عائلية فالح", true, 2, 380);
        Simple("prod-demo-meal-kofta", mealsCategoryId, "وجبة كفتة مشوية", true, 3, 170);

        // أطباق فالح
        Simple("prod-demo-plate-fries", platesCategoryId, "طبق بطاطس", false, 0, 25);
        Simple("prod-demo-plate-omelette-cheese", platesCategoryId, "طبق أومليت بالجبنة", false, 1, 40);
        Simple("prod-demo-plate-kibbeh", platesCategoryId, "طبق كبيبة 3 قطع", false, 2, 75);
        Simple("prod-demo-plate-liver", platesCategoryId, "طبق كبدة جريل", true, 3, 70);

        // المعجنات - cheap, no variants, high tap-frequency items.
        Simple("prod-demo-pastry-white-cheese", pastriesCategoryId, "فطيرة جبنة بيضة", false, 0, 15);
        Simple("prod-demo-pastry-rumi", pastriesCategoryId, "فطيرة جبنة رومي", false, 1, 15);
        Simple("prod-demo-pastry-zaatar", pastriesCategoryId, "منقوشة زعتر", false, 2, 15);
        Simple("prod-demo-pastry-lahm", pastriesCategoryId, "لحمة بعجين", false, 3, 20);
        Simple("prod-demo-pastry-musakhan", pastriesCategoryId, "مسخن دجاج", false, 4, 50);

        // الأرز - deliberately a single-product category (tests a sparse/short grid).
        Simple("prod-demo-rice", riceCategoryId, "طبق أرز فالح المبهر", false, 0, 40);

        // المكرونات
        Simple("prod-demo-pasta-bechamel", pastaCategoryId, "مكرونة بشاميل", false, 0, 50);
        Simple("prod-demo-pasta-negresco", pastaCategoryId, "مكرونة نجرسكو فراخ", false, 1, 50);

        // المقبلات - small/large two-variant pattern (distinct from the 3-variant sandwich pattern).
        WithVariants("prod-demo-app-baba-ghanoush", appetizersCategoryId, "بابا غنوج", false, 0,
            ("var-bg-small", "صغير", 25), ("var-bg-large", "كبير", 40));
        WithVariants("prod-demo-app-toum", appetizersCategoryId, "تومية سورية", false, 1,
            ("var-toum-small", "صغير", 25), ("var-toum-large", "كبير", 30));
        Simple("prod-demo-app-kibbeh-piece", appetizersCategoryId, "قرص كبيبة مقلي", false, 2, 25);

        // المشروبات - the long, real, multi-choice product name that most stresses card layout.
        Simple("prod-demo-bev-water", drinksCategoryId, "مياه معدنية صغيرة", false, 1, 10);
        WithVariants("prod-demo-bev-can", drinksCategoryId, "كانز (بيبسي / سفن / ميرندا)", false, 2,
            ("var-can-pepsi", "بيبسي", 20), ("var-can-7up", "سفن آب", 20), ("var-can-mirinda", "ميرندا برتقال", 20));
        WithVariants("prod-demo-bev-liter", drinksCategoryId, "مشروب غازي 1 لتر", false, 3,
            ("var-1l-pepsi", "بيبسي 1 لتر", 35), ("var-1l-7up", "سفن 1 لتر", 35));

        var offerId = "rule-demo-shawarma-offer";
        db.PriceRules.Add(new PriceRuleEntity
        {
            Id = offerId, Name = "عرض الشاورما", FixedPricePiastres = 4000, Active = true,
            Products = { new PriceRuleProductEntity { PriceRuleId = offerId, ProductId = shawarmaId } },
        });

        var koftaOfferId = "rule-demo-kofta-offer";
        db.PriceRules.Add(new PriceRuleEntity
        {
            Id = koftaOfferId, Name = "عرض الكفتة", FixedPricePiastres = 8000, Active = true,
            Products = { new PriceRuleProductEntity { PriceRuleId = koftaOfferId, ProductId = "prod-demo-kofta-charcoal-sw" } },
        });

        await db.SaveChangesAsync(ct);
    }
}

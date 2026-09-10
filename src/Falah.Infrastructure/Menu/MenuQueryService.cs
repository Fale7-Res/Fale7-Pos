namespace Falah.Infrastructure.Menu;

using Falah.Application.Menu;
using Falah.Domain.Common;
using Falah.Domain.Pricing;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Read-only catalog query for a client to render a menu. Prices shown here are for
/// display only - MenuPricingService, not this service, is what checkout actually trusts.</summary>
public sealed class MenuQueryService
{
    private readonly FalahDbContext _db;
    public MenuQueryService(FalahDbContext db) => _db = db;

    public async Task<MenuResponse> GetMenuAsync(CancellationToken ct)
    {
        var categories = await _db.Categories.AsNoTracking().Where(c => c.Active).OrderBy(c => c.SortOrder).ToListAsync(ct);
        var products = await _db.Products.AsNoTracking().Include(p => p.Variants).Where(p => p.IsAvailable).OrderBy(p => p.SortOrder).ToListAsync(ct);
        var priceRuleEntities = await _db.PriceRules.AsNoTracking().Include(r => r.Products).Include(r => r.Days).Where(r => r.Active).ToListAsync(ct);
        // Stamps only. Projecting the two scalar columns keeps the photo bytes out of the menu
        // response entirely - the client fetches each image on its own, once, and caches it.
        var imageVersions = await _db.ProductImages.AsNoTracking()
            .Select(i => new { i.ProductId, i.Version }).ToDictionaryAsync(i => i.ProductId, i => i.Version, ct);
        var domainRules = priceRuleEntities.Select(MenuPricingService.ToDomainRule).ToList();
        var now = DateTimeOffset.UtcNow;

        var categoryDtos = categories.Select(category =>
        {
            var categoryProducts = products.Where(p => p.CategoryId == category.Id).Select(product =>
            {
                var offers = PricingRules.ApplicableRules(product.Id, domainRules, now, BusinessClock.CairoTimeZone)
                    .Select(r => new MenuOfferDto(r.Id, r.Name, r.FixedPrice.Piastres)).ToList();
                var variants = product.Variants.Select(v => new MenuVariantDto(v.Id, v.Name, v.PricePiastres)).ToList();
                return new MenuProductDto(product.Id, product.Name, product.BasePricePiastres, product.SendToGrill, product.IsAvailable, variants, offers, product.PreparationStation,
                    imageVersions.TryGetValue(product.Id, out var imageVersion) ? imageVersion : null);
            }).ToList();
            return new MenuCategoryDto(category.Id, category.Name, category.Icon, categoryProducts);
        }).ToList();

        return new MenuResponse(categoryDtos, await LoadBestSellerIdsAsync(products.Select(p => p.Id).ToHashSet(), ct));
    }

    /// <summary>How far back the "best sellers" shortcut looks. Two weeks is long enough to survive
    /// a quiet Tuesday and short enough that a menu change or a seasonal shift actually moves the
    /// list - a lifetime ranking would freeze whatever sold most in the restaurant's first year.</summary>
    private const int BestSellerWindowDays = 14;

    /// <summary>How many products the shortcut holds. It is a shortcut to the handful of things
    /// people order constantly, not a second copy of the menu ranked differently.</summary>
    private const int BestSellerCount = 12;

    private async Task<List<string>> LoadBestSellerIdsAsync(HashSet<string> sellableProductIds, CancellationToken ct)
    {
        if (sellableProductIds.Count == 0) return new List<string>();

        var (start, _) = BusinessClock.UtcRangeForBusinessDates(
            BusinessClock.Today().AddDays(-(BestSellerWindowDays - 1)), BusinessClock.Today());

        var rows = await _db.OrderItems.AsNoTracking()
            .Join(_db.Orders.AsNoTracking().Where(o => o.Status != nameof(Falah.Domain.Orders.OrderStatus.Cancelled) && o.CreatedAtUtc >= start),
                i => i.OrderId, o => o.Id, (i, _) => new { i.ProductId, i.Quantity })
            .ToListAsync(ct);

        return rows
            // A product that has been taken off the menu since is not a shortcut anyone can use.
            .Where(r => sellableProductIds.Contains(r.ProductId))
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(r => r.Quantity) })
            .OrderByDescending(x => x.Quantity).ThenBy(x => x.ProductId)
            .Take(BestSellerCount)
            .Select(x => x.ProductId)
            .ToList();
    }

    /// <summary>Management view - unlike GetMenuAsync (the POS-facing menu, active categories and
    /// available products only), this includes inactive categories too, since an administrator
    /// reviewing or reactivating one needs to see it.</summary>
    public async Task<List<CategoryDetailsDto>> ListAllCategoriesAsync(CancellationToken ct) =>
        (await _db.Categories.AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync(ct))
            .Select(c => new CategoryDetailsDto(c.Id, c.Name, c.Icon, c.SortOrder, c.Active)).ToList();

    /// <summary>Management view - includes unavailable products too, unlike GetMenuAsync.</summary>
    public async Task<List<ProductDetailsDto>> ListAllProductsAsync(CancellationToken ct)
    {
        var products = await _db.Products.AsNoTracking().Include(p => p.Variants).OrderBy(p => p.SortOrder).ToListAsync(ct);
        var imageVersions = await _db.ProductImages.AsNoTracking()
            .Select(i => new { i.ProductId, i.Version }).ToDictionaryAsync(i => i.ProductId, i => i.Version, ct);
        return products.Select(p => ToProductDetailsDto(p, imageVersions.GetValueOrDefault(p.Id))).ToList();
    }

    public async Task<ProductDetailsDto?> GetProductAsync(string id, CancellationToken ct)
    {
        var product = await _db.Products.AsNoTracking().Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return null;
        var version = await _db.ProductImages.AsNoTracking().Where(i => i.ProductId == id).Select(i => i.Version).FirstOrDefaultAsync(ct);
        return ToProductDetailsDto(product, version);
    }

    private static ProductDetailsDto ToProductDetailsDto(Falah.Infrastructure.Persistence.Entities.ProductEntity p, string? imageVersion) => new(
        p.Id, p.CategoryId, p.Name, p.BasePricePiastres, p.SendToGrill, p.PreparationStation, p.IsAvailable, p.SortOrder, p.RecipeId,
        p.Variants.Select(v => new ProductVariantDetailsDto(v.Id, v.Name, v.PricePiastres, v.RecipeId)).ToList(),
        imageVersion);
}

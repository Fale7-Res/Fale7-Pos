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
        var domainRules = priceRuleEntities.Select(MenuPricingService.ToDomainRule).ToList();
        var now = DateTimeOffset.UtcNow;

        var categoryDtos = categories.Select(category =>
        {
            var categoryProducts = products.Where(p => p.CategoryId == category.Id).Select(product =>
            {
                var offers = PricingRules.ApplicableRules(product.Id, domainRules, now, BusinessClock.CairoTimeZone)
                    .Select(r => new MenuOfferDto(r.Id, r.Name, r.FixedPrice.Piastres)).ToList();
                var variants = product.Variants.Select(v => new MenuVariantDto(v.Id, v.Name, v.PricePiastres)).ToList();
                return new MenuProductDto(product.Id, product.Name, product.BasePricePiastres, product.SendToGrill, product.IsAvailable, variants, offers);
            }).ToList();
            return new MenuCategoryDto(category.Id, category.Name, category.Icon, categoryProducts);
        }).ToList();

        return new MenuResponse(categoryDtos);
    }
}

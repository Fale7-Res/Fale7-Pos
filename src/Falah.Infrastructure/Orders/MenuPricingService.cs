namespace Falah.Infrastructure.Orders;

using Falah.Application.Orders;
using Falah.Domain.Common;
using Falah.Domain.Orders;
using Falah.Domain.Pricing;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The server-side pricing authority for checkout: a client sends a product/variant selection and
/// a quantity, never a price. This service is the ONLY place that turns that selection into a
/// frozen OrderItem - it looks up the real catalog price, re-validates any requested offer against
/// what is actually live right now (ItemPricingRules), and hands back the recipe id (if any) the
/// caller needs for the separate inventory-deduction step.
/// </summary>
public sealed class MenuPricingService
{
    private readonly FalahDbContext _db;
    public MenuPricingService(FalahDbContext db) => _db = db;

    public sealed record ResolvedItem(OrderItem Item, string? RecipeId);

    public async Task<List<ResolvedItem>> ResolveAsync(IReadOnlyList<CheckoutItemRequest> requests, DateTimeOffset nowUtc, CancellationToken ct)
    {
        if (requests.Count == 0) throw new DomainException("السلة فارغة. الرجاء إضافة أصناف قبل الدفع.");

        var productIds = requests.Select(r => r.ProductId).Distinct().ToList();
        var products = await _db.Products.Include(p => p.Variants)
            .Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        var priceRules = await _db.PriceRules.Include(r => r.Products).Include(r => r.Days)
            .Where(r => r.Active).ToListAsync(ct);

        var resolved = new List<ResolvedItem>();
        foreach (var request in requests)
        {
            if (request.Quantity <= 0) throw new DomainException("الكمية يجب أن تكون أكبر من صفر.");

            if (!products.TryGetValue(request.ProductId, out var product))
                throw new DomainException($"المنتج غير موجود: {request.ProductId}");
            if (!product.IsAvailable)
                throw new DomainException($"المنتج غير متاح حاليًا: {product.Name}");

            ProductVariantEntity? variant = null;
            if (request.VariantId is not null)
            {
                variant = product.Variants.FirstOrDefault(v => v.Id == request.VariantId)
                    ?? throw new DomainException($"الخيار غير موجود لهذا المنتج: {request.VariantId}");
            }

            var listPrice = Money.FromPiastres(variant?.PricePiastres ?? product.BasePricePiastres);
            var applicableRules = priceRules.Where(r => r.Products.Any(p => p.ProductId == product.Id)).Select(ToDomainRule).ToList();
            var price = ItemPricingRules.Resolve(product.Id, listPrice, request.RequestedPriceRuleId, applicableRules, nowUtc, BusinessClock.CairoTimeZone);

            var orderItem = new OrderItem(
                $"item-{Guid.NewGuid():N}", product.Id, variant?.Id, product.Name, variant?.Name,
                price.EffectiveUnitPrice, price.ListUnitPrice, request.Quantity,
                price.EffectiveUnitPrice * request.Quantity, product.SendToGrill,
                price.AppliedPriceRuleId, price.AppliedPriceRuleName, request.Notes);

            resolved.Add(new ResolvedItem(orderItem, variant?.RecipeId ?? product.RecipeId));
        }

        return resolved;
    }

    public static PriceRule ToDomainRule(PriceRuleEntity entity) => new(
        entity.Id, entity.Name, entity.Products.Select(p => p.ProductId).ToList(),
        Money.FromPiastres(entity.FixedPricePiastres),
        entity.Days.Count > 0 ? entity.Days.Select(d => (DayOfWeek)d.DayOfWeek).ToList() : null,
        entity.StartTime, entity.EndTime, entity.Active);
}

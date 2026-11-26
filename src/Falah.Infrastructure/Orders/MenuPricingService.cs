namespace Falah.Infrastructure.Orders;

using Falah.Application.Orders;
using Falah.Domain.Common;
using Falah.Domain.Orders;
using Falah.Domain.Pricing;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Pricing;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The server-side pricing authority for checkout: a client sends a product/variant selection and
/// a quantity, never a price. This service is the ONLY place that turns that selection into a
/// frozen OrderItem - it looks up the real catalog price, re-validates any requested offer against
/// what is actually live right now (ItemPricingRules), and hands back the recipe id (if any) the
/// caller needs for the separate inventory-deduction step.
///
/// Owner, 2026-11-25: an offer the cashier chose that is no longer allowed - ended, stopped, not for this order type or
/// station, over its per-order limit - is REFUSED with its name, never silently charged at another price: what the
/// screen showed is what is charged, or the cashier is told. A gift line (an OrderGift offer) is priced 0 with the
/// product's own price kept as its list price; whether the order earns it is checked by CheckoutOrderHandler once the
/// total is known (<see cref="ResolvedItem.GiftRule"/>).
/// </summary>
public sealed class MenuPricingService
{
    private readonly FalahDbContext _db;
    public MenuPricingService(FalahDbContext db) => _db = db;

    public sealed record ResolvedItem(OrderItem Item, string? RecipeId, PriceRule? GiftRule = null, PriceRule? OfferRule = null);

    public Task<List<ResolvedItem>> ResolveAsync(IReadOnlyList<CheckoutItemRequest> requests, DateTimeOffset nowUtc, CancellationToken ct) =>
        ResolveAsync(requests, nowUtc, null, "till", ct);

    public async Task<List<ResolvedItem>> ResolveAsync(IReadOnlyList<CheckoutItemRequest> requests, DateTimeOffset nowUtc,
        OrderType? orderType, string station, CancellationToken ct)
    {
        if (requests is null || requests.Count == 0) throw new DomainException("السلة فارغة. الرجاء إضافة أصناف قبل الدفع.");
        // QA-11 (audit 2026-11-23): a note prints on the kitchen ticket; it has a sane upper bound.
        foreach (var line in requests) InputRules.AssertMaxLength(line?.Notes, InputRules.MaxNoteLength, "ملاحظة الصنف");

        var productIds = requests.Select(r => r.ProductId).Distinct().ToList();
        var products = await _db.Products.Include(p => p.Variants)
            .Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        var requestedRuleIds = requests.Where(r => !string.IsNullOrEmpty(r.RequestedPriceRuleId)).Select(r => r.RequestedPriceRuleId!).Distinct().ToList();
        var rules = requestedRuleIds.Count == 0 ? new Dictionary<string, PriceRule>()
            : (await PriceRuleMapping.Full(_db).AsNoTracking().AsSplitQuery().Where(r => requestedRuleIds.Contains(r.Id)).ToListAsync(ct))
                .Select(PriceRuleMapping.ToDomain).ToDictionary(r => r.Id);

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
                variant = product.Variants.FirstOrDefault(v => v.Id == request.VariantId && v.Active)
                    ?? throw new DomainException($"الخيار غير موجود لهذا المنتج: {request.VariantId}");
            }

            var listPrice = Money.FromPiastres(variant?.PricePiastres ?? product.BasePricePiastres);
            var rule = request.RequestedPriceRuleId is { Length: > 0 } ruleId ? Live(ruleId, rules, nowUtc, orderType, station) : null;

            Money effective = listPrice;
            PriceRule? giftRule = null, offerRule = null;
            if (rule is { Kind: OfferKind.OrderGift })
            {
                // Which tier it belongs to is checked against the order's total afterwards; here, only that it is one
                // of the offer's gifts at all.
                if (!rule.GiftTiers.Any(t => t.ProductId == product.Id && (t.VariantId ?? DefaultVariant(product)) == variant?.Id))
                    throw new DomainException($"{product.Name} مش من هدايا «{rule.Name}».");
                effective = Money.Zero;
                giftRule = rule;
            }
            else if (rule is not null)
            {
                if (!PricingRules.Targets(rule, product.Id, product.CategoryId))
                    throw new DomainException($"العرض «{rule.Name}» مش على {product.Name}.");
                effective = PricingRules.OfferUnitPrice(rule, listPrice);
                offerRule = rule;
            }

            var orderItem = new OrderItem(
                $"item-{Guid.NewGuid():N}", product.Id, variant?.Id, product.Name, variant?.Name,
                effective, listPrice, request.Quantity,
                effective * request.Quantity, product.SendToGrill,
                rule?.Id, rule?.Name, request.Notes,
                product.PreparationStation);

            resolved.Add(new ResolvedItem(orderItem, variant?.RecipeId ?? product.RecipeId, giftRule, offerRule));
        }

        // Per-order limit: the pieces of one offer in this order, across all its lines.
        foreach (var group in resolved.Where(r => r.OfferRule is { MaxPerOrder: not null }).GroupBy(r => r.OfferRule!.Id))
        {
            var rule = group.First().OfferRule!;
            if (group.Sum(r => r.Item.Quantity) > rule.MaxPerOrder)
                throw new DomainException($"العرض «{rule.Name}» على {rule.MaxPerOrder} قطعة بس في الطلب. خلّي الباقي بالسعر العادي.");
        }
        return resolved;
    }

    /// <summary>The size a gift tier without one means: the product's only size, if it has exactly one.</summary>
    private static string? DefaultVariant(ProductEntity product)
    {
        var active = product.Variants.Where(v => v.Active).ToList();
        return active.Count == 1 ? active[0].Id : null;
    }

    private static PriceRule Live(string ruleId, IReadOnlyDictionary<string, PriceRule> rules, DateTimeOffset nowUtc, OrderType? orderType, string station)
    {
        if (!rules.TryGetValue(ruleId, out var rule) || !rule.Active)
            throw new DomainException("عرض في الطلب اتوقف أو اتمسح. شيله من الصنف أو كمّل بالسعر العادي.");
        if (!PricingRules.IsActiveAt(rule, nowUtc, BusinessClock.CairoTimeZone))
            throw new DomainException($"العرض «{rule.Name}» مش شغال دلوقتي (خلص وقته). شيله من الصنف أو كمّل بالسعر العادي.");
        if (orderType is { } type && !PricingRules.AllowsOrder(rule, type, station))
            throw new DomainException($"العرض «{rule.Name}» مش متاح للطلب ده (نوع الطلب أو الجهاز).");
        return rule;
    }

    public static PriceRule ToDomainRule(PriceRuleEntity entity) => PriceRuleMapping.ToDomain(entity);
}

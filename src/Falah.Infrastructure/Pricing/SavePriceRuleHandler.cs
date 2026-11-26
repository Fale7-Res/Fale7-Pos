namespace Falah.Infrastructure.Pricing;

using Falah.Application.Pricing;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Pricing;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from savePriceRule - "replace the whole rule" (products + days included), the
/// same idiom SaveRecipeHandler already uses for recipe lines. Deliberately does NOT reject
/// overlapping rules on the same product/window - see PricingRules.AssertPriceRuleInput's own doc
/// comment for why that would be inventing a policy this system's evidence does not support.</summary>
public sealed class SavePriceRuleHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public SavePriceRuleHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<PriceRuleDto> HandleAsync(SavePriceRuleRequest request, CancellationToken ct = default)
    {
        var ruleId = request.RuleId ?? $"price-rule-{request.RequestId}";
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var receiptId = CommandReceipt.Id("price-rule-save", request.RequestId);
        var receipt = await CommandReceipt.FindAsync(_db, receiptId, ruleId, request.ActorUserId, ct);
        if (receipt is not null) return System.Text.Json.JsonSerializer.Deserialize<PriceRuleDto>(receipt.NewValue!)
            ?? throw new InvalidOperationException("Stored price-rule receipt is invalid.");
        // Owner, 2026-11-25: offers of every shape - the whole offer is checked by the pricing rules before anything else.
        if (request.DaysOfWeek is { Count: > 0 } days && days.Any(d => d is < 0 or > 6)) throw new DomainException("أيام العرض غير صالحة.");
        var rule = PriceRuleMapping.FromRequest(ruleId, request);
        PricingRules.AssertOfferInput(rule);
        var productIds = rule.ProductIds.ToList();

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "offers_manage", null);
        }
        // Audit 2026-11-23, F-05: only an approval the check actually validated is named as the authoriser.
        var approvedBy = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "offers_manage", approval, requireApproval: true).Approver?.ApproverName;

        var isItem = rule.Kind is OfferKind.ItemFixedPrice or OfferKind.ItemPercent;
        var knownProductCount = isItem ? await _db.Products.AsNoTracking().CountAsync(p => productIds.Contains(p.Id), ct) : 0;
        if (isItem && knownProductCount != productIds.Count) throw new DomainException("يوجد صنف غير موجود ضمن أصناف العرض.");
        if (rule.Kind is OfferKind.CategoryAmount or OfferKind.CategoryPercent)
        {
            var categoryIds = rule.CategoryIds.ToList();
            if (await _db.Categories.AsNoTracking().CountAsync(c => categoryIds.Contains(c.Id), ct) != categoryIds.Count)
                throw new DomainException("يوجد قسم غير موجود ضمن أقسام العرض.");
        }
        if (rule.Kind == OfferKind.OrderGift)
            foreach (var tier in rule.GiftTiers)
            {
                var product = await _db.Products.AsNoTracking().Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == tier.ProductId, ct)
                    ?? throw new DomainException("صنف الهدية غير موجود.");
                if (tier.VariantId is not null && !product.Variants.Any(v => v.Id == tier.VariantId))
                    throw new DomainException($"الحجم المختار للهدية مش موجود في {product.Name}.");
                if (tier.VariantId is null && product.Variants.Count(v => v.Active) > 1)
                    throw new DomainException($"{product.Name} ليه أكتر من حجم: اختار حجم الهدية.");
            }

        var entity = await PriceRuleMapping.Full(_db).FirstOrDefaultAsync(r => r.Id == ruleId, ct);
        if (entity is null)
        {
            entity = new PriceRuleEntity { Id = ruleId };
            _db.PriceRules.Add(entity);
        }
        else
        {
            _db.PriceRuleProducts.RemoveRange(entity.Products);
            _db.PriceRuleDays.RemoveRange(entity.Days);
            _db.PriceRuleCategories.RemoveRange(entity.Categories);
            _db.PriceRuleGiftTiers.RemoveRange(entity.GiftTiers);
        }
        PriceRuleMapping.Apply(entity, rule, request.OrderTypes);

        _auditLog.Append(new AuditEvent(
            receiptId, actorUser.Id, actorUser.Name, actorUser.Role,
            $"حفظ عرض تسعير: {entity.Name}", "menu",
            $"النوع: {entity.Kind} - السعر: {entity.FixedPricePiastres} قرش - النسبة: {entity.PercentBasisPoints / 100m:0.##}% - الخصم: {entity.AmountOffPiastres} قرش"
                + $" - أصناف: {entity.Products.Count} - أقسام: {entity.Categories.Count} - هدايا: {entity.GiftTiers.Count} - مفعّل: {(entity.Active ? "نعم" : "لا")}",
            "normal", DateTimeOffset.UtcNow, AuthorizedBy: approvedBy, ReferenceId: ruleId,
            OperationId: request.RequestId, NewValue: System.Text.Json.JsonSerializer.Serialize(ToDto(entity))));

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return ToDto(entity);
    }

    internal static PriceRuleDto ToDto(PriceRuleEntity e) => PriceRuleMapping.ToDto(e);
}

public sealed class PriceRuleQueryService
{
    private readonly FalahDbContext _db;
    public PriceRuleQueryService(FalahDbContext db) => _db = db;

    public async Task<List<PriceRuleDto>> ListAsync(CancellationToken ct) =>
        (await PriceRuleMapping.Full(_db).AsNoTracking().AsSplitQuery().ToListAsync(ct))
            .Select(SavePriceRuleHandler.ToDto).ToList();
}

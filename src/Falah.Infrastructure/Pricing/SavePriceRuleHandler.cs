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
        var productIds = request.ProductIds.Distinct().ToList();
        var fixedPrice = Money.FromPiastres(request.FixedPricePiastres);
        PricingRules.AssertPriceRuleInput(request.Name, productIds, fixedPrice, request.DaysOfWeek);

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
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "offers_manage", approval, requireApproval: true);

        var knownProductCount = await _db.Products.AsNoTracking().CountAsync(p => productIds.Contains(p.Id), ct);
        if (knownProductCount != productIds.Count) throw new DomainException("يوجد صنف غير موجود ضمن أصناف العرض.");


        var entity = await _db.PriceRules.Include(r => r.Products).Include(r => r.Days).FirstOrDefaultAsync(r => r.Id == ruleId, ct);
        if (entity is null)
        {
            entity = new PriceRuleEntity { Id = ruleId };
            _db.PriceRules.Add(entity);
        }
        else
        {
            _db.PriceRuleProducts.RemoveRange(entity.Products);
            _db.PriceRuleDays.RemoveRange(entity.Days);
        }
        entity.Name = request.Name.Trim();
        entity.FixedPricePiastres = fixedPrice.Piastres;
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.Active = request.Active;
        entity.Products = productIds.Select(id => new PriceRuleProductEntity { PriceRuleId = ruleId, ProductId = id }).ToList();
        entity.Days = (request.DaysOfWeek ?? Array.Empty<int>()).Distinct()
            .Select(d => new PriceRuleDayEntity { PriceRuleId = ruleId, DayOfWeek = d }).ToList();

        _auditLog.Append(new AuditEvent(
            receiptId, actorUser.Id, actorUser.Name, actorUser.Role,
            $"حفظ عرض تسعير: {entity.Name}", "menu",
            $"السعر: {entity.FixedPricePiastres} قرش - عدد الأصناف: {productIds.Count} - مفعّل: {(entity.Active ? "نعم" : "لا")}",
            "normal", DateTimeOffset.UtcNow, AuthorizedBy: approval?.ApproverName, ReferenceId: ruleId,
            OperationId: request.RequestId, NewValue: System.Text.Json.JsonSerializer.Serialize(ToDto(entity))));

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return ToDto(entity);
    }

    internal static PriceRuleDto ToDto(PriceRuleEntity e) => new(
        e.Id, e.Name, e.Products.Select(p => p.ProductId).ToList(), e.FixedPricePiastres,
        e.Days.Count > 0 ? e.Days.Select(d => d.DayOfWeek).ToList() : null, e.StartTime, e.EndTime, e.Active);
}

public sealed class PriceRuleQueryService
{
    private readonly FalahDbContext _db;
    public PriceRuleQueryService(FalahDbContext db) => _db = db;

    public async Task<List<PriceRuleDto>> ListAsync(CancellationToken ct) =>
        (await _db.PriceRules.AsNoTracking().Include(r => r.Products).Include(r => r.Days).ToListAsync(ct))
            .Select(SavePriceRuleHandler.ToDto).ToList();
}

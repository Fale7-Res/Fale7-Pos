namespace Falah.Infrastructure.Inventory;

using Falah.Application.Inventory;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from saveOrderPackagingRule - order-level (not item-level) packaging, e.g. one
/// delivery bag per delivery order regardless of item count. Upsert keyed on a stable id, gated on
/// menu_manage, matching the reference app exactly.</summary>
public sealed class SaveOrderPackagingRuleHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public SaveOrderPackagingRuleHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<OrderPackagingRuleDto> HandleAsync(SaveOrderPackagingRuleRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.InventoryItemId)) throw new DomainException("يجب اختيار صنف التغليف.");
        if (request.QuantityMilliUnits <= 0) throw new DomainException("الكمية غير صالحة.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "menu_manage", null);
        }
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "menu_manage", approval, requireApproval: true);

        var ruleId = request.RuleId ?? $"pkg-rule-{request.RequestId}";
        var entity = await _db.OrderPackagingRules.FirstOrDefaultAsync(r => r.Id == ruleId, ct);
        if (entity is null)
        {
            entity = new OrderPackagingRuleEntity { Id = ruleId };
            _db.OrderPackagingRules.Add(entity);
        }
        entity.OrderType = request.OrderType;
        entity.InventoryItemId = request.InventoryItemId;
        entity.QuantityMilliUnits = request.QuantityMilliUnits;
        entity.Active = request.Active;

        await _db.SaveChangesAsync(ct);

        return new OrderPackagingRuleDto(entity.Id, entity.OrderType, entity.InventoryItemId, entity.QuantityMilliUnits, entity.Active);
    }
}

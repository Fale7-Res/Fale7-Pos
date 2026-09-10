namespace Falah.Infrastructure.Inventory;

using Falah.Application.Inventory;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Sets or clears an ingredient's estimated unit cost.
///
/// This exists so a restaurant can see an approximate food cost before it has recorded a single
/// supplier invoice. The alternative - the only one available before this - was to fabricate
/// purchase invoices, which would post supplier obligations, move treasury money and add stock that
/// never arrived. An estimate is a note about what something probably costs; an invoice is a claim
/// that goods and money changed hands, and the two must never be confused.
///
/// It is therefore deliberately NOT a financial command: no treasury movement, no stock movement,
/// no supplier ledger entry, no business-day requirement. It is audited because it changes every
/// recipe cost and the reported margin.
///
/// A real purchase always overrides it (see CostingRules.IngredientUnitCost), so this can never
/// quietly contradict what the restaurant actually paid.
/// </summary>
public sealed class SetEstimatedCostHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public SetEstimatedCostHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task HandleAsync(SetEstimatedCostRequest request, CancellationToken ct = default)
    {
        if (request.EstimatedUnitCostPiastres is < 0)
            throw new DomainException("التكلفة التقديرية لا يمكن أن تكون بالسالب.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "inventory_manage");

        var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Id == request.InventoryItemId, ct)
            ?? throw new DomainException("الخامة غير موجودة.");

        var previous = item.EstimatedUnitCostPiastres;
        if (previous == request.EstimatedUnitCostPiastres) return;

        item.EstimatedUnitCostPiastres = request.EstimatedUnitCostPiastres;
        _auditLog.Append(new AuditEvent(
            $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
            request.EstimatedUnitCostPiastres is null ? "إلغاء التكلفة التقديرية لخامة" : "تحديد تكلفة تقديرية لخامة",
            "inventory_cost",
            $"الخامة {item.Name} - تقديرية وليست من فاتورة؛ أول فاتورة شراء فعلية تلغيها تلقائيًا.",
            "normal", DateTimeOffset.UtcNow, ReferenceId: item.Id,
            PreviousValue: previous?.ToString(), NewValue: request.EstimatedUnitCostPiastres?.ToString()));

        await _db.SaveChangesAsync(ct);
    }
}

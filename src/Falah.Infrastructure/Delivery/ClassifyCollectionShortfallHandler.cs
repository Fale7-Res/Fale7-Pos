namespace Falah.Infrastructure.Delivery;

using Falah.Application.Delivery;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// AUD-03: a delivered order's collection shortfall is classified by a manager, with evidence, as what it really is
/// (CollectionShortfallRules) - never assumed to be a loss. The classification is recorded beside the delivery and
/// its custody, which stay exactly as they were: it changes how the gap is REPORTED (unresolved / still owed /
/// given up), not any balance, settlement or drawer.
///
/// - delivery_settlement_approve, or a manager's approval issued for it.
/// - Evidence is required; the amount is the custody's own gap, read here, never sent by the client.
/// - Once per delivery (unique index); a retried request replays the classification it made.
/// </summary>
public sealed class ClassifyCollectionShortfallHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public ClassifyCollectionShortfallHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<CollectionShortfallDto> HandleAsync(ClassifyCollectionShortfallRequest request, CancellationToken ct = default)
    {
        var replay = await _db.CollectionShortfallResolutions.AsNoTracking().FirstOrDefaultAsync(r => r.RequestId == request.RequestId, ct);
        if (replay is not null) return await GetAsync(replay.DeliveryId, wasReplayed: true, ct);

        CollectionShortfallRules.EnsureValid(request.Classification, request.Evidence);

        var delivery = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DeliveryId, ct)
            ?? throw new InvalidOperationException("التوصيل غير موجود.");
        var custody = await _db.DriverCustodies.AsNoTracking().FirstOrDefaultAsync(c => c.DeliveryId == delivery.Id, ct);
        if (delivery.DeliveredAtUtc is null || custody is null || custody.CollectedPiastres >= custody.ExpectedPiastres)
            throw new DomainException("لا يوجد فرق تحصيل على هذا التوصيل.");
        if (await _db.CollectionShortfallResolutions.AsNoTracking().AnyAsync(r => r.DeliveryId == delivery.Id, ct))
            throw new DomainException("تم تصنيف فرق التحصيل لهذا التوصيل بالفعل.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "delivery_settlement_approve", request.Evidence);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser),
            "delivery_settlement_approve", approval, requireApproval: true);
        var approvedBy = authorization.Approver?.ApproverName ?? actorUser.Name;

        var now = DateTimeOffset.UtcNow;
        var amount = custody.ExpectedPiastres - custody.CollectedPiastres;
        _db.CollectionShortfallResolutions.Add(new CollectionShortfallResolutionEntity
        {
            Id = $"csr-{delivery.Id}", DeliveryId = delivery.Id, OrderId = delivery.OrderId, DriverId = custody.DriverId,
            AmountPiastres = amount, Classification = request.Classification, Evidence = request.Evidence.Trim(),
            RequestId = request.RequestId, ClassifiedByUserId = actorUser.Id, ClassifiedByName = actorUser.Name,
            ApprovedBy = approvedBy, CreatedAtUtc = now,
        });
        _auditLog.Append(new AuditEvent(
            $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
            "تصنيف فرق تحصيل", "delivery",
            $"فرق تحصيل {amount} قرش على الطلب {delivery.OrderId}: {request.Classification} - {request.Evidence.Trim()}",
            "critical", now, Reason: request.Evidence.Trim(), AuthorizedBy: approvedBy, ReferenceId: delivery.Id,
            NewValue: request.Classification));
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            var after = await _db.CollectionShortfallResolutions.AsNoTracking().FirstOrDefaultAsync(r => r.RequestId == request.RequestId, ct);
            if (after is not null) return await GetAsync(after.DeliveryId, wasReplayed: true, ct);
            throw new DomainException("تم تصنيف فرق التحصيل لهذا التوصيل بالفعل.");
        }
        return await GetAsync(delivery.Id, wasReplayed: false, ct);
    }

    public async Task<CollectionShortfallListDto> ListAsync(CancellationToken ct = default)
    {
        var items = await LoadAsync(null, ct);
        var unresolved = items.Where(i => i.Classification is null).ToList();
        return new CollectionShortfallListDto(
            unresolved.Sum(i => i.ShortfallPiastres), unresolved.Count,
            items.Where(i => i.Classification is { } c && CollectionShortfallRules.IsReceivable(c)).Sum(i => i.ShortfallPiastres),
            items.Where(i => i.Classification is { } c && CollectionShortfallRules.IsRealisedReduction(c)).Sum(i => i.ShortfallPiastres),
            items);
    }

    private async Task<CollectionShortfallDto> GetAsync(string deliveryId, bool wasReplayed, CancellationToken ct) =>
        (await LoadAsync(deliveryId, ct)).Single() with { WasReplayed = wasReplayed };

    private async Task<List<CollectionShortfallDto>> LoadAsync(string? deliveryId, CancellationToken ct)
    {
        var rows = await (from d in _db.Deliveries.AsNoTracking()
                          join c in _db.DriverCustodies.AsNoTracking() on d.Id equals c.DeliveryId
                          join o in _db.Orders.AsNoTracking() on d.OrderId equals o.Id
                          join r in _db.CollectionShortfallResolutions.AsNoTracking() on d.Id equals r.DeliveryId into rs
                          from r in rs.DefaultIfEmpty()
                          where d.DeliveredAtUtc != null && c.CollectedPiastres < c.ExpectedPiastres
                              && (deliveryId == null || d.Id == deliveryId)
                          orderby d.DeliveredAtUtc descending
                          select new
                          {
                              d.Id, d.OrderId, o.OrderNumber, c.DriverId, d.DriverNameAtHandover, c.ExpectedPiastres, c.CollectedPiastres,
                              d.DeliveredAtUtc, Classification = r == null ? null : r.Classification, Evidence = r == null ? null : r.Evidence,
                              ClassifiedBy = r == null ? null : r.ClassifiedByName, ApprovedBy = r == null ? null : r.ApprovedBy,
                              ClassifiedAtUtc = r == null ? (DateTimeOffset?)null : r.CreatedAtUtc,
                          }).Take(500).ToListAsync(ct);
        return rows.Select(x => new CollectionShortfallDto(
            x.Id, x.OrderId, x.OrderNumber, x.DriverId, x.DriverNameAtHandover, x.ExpectedPiastres, x.CollectedPiastres,
            x.ExpectedPiastres - x.CollectedPiastres, x.DeliveredAtUtc, x.Classification is null ? "unresolved" : "classified",
            x.Classification, x.Evidence, x.ClassifiedBy, x.ApprovedBy, x.ClassifiedAtUtc)).ToList();
    }
}

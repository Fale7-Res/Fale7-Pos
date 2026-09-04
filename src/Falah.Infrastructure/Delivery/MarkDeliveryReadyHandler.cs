namespace Falah.Infrastructure.Delivery;

using System.Data;
using Falah.Application.Delivery;
using Falah.Domain.Authorization;
using Falah.Domain.Delivery;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from markDeliveryReady. No financial effect, so - matching the reference app - a
/// double-click is handled as a plain "already ready" no-op rather than a minted idempotency key;
/// the UPDLOCK read still makes that check race-safe against a genuinely concurrent second call.</summary>
public sealed class MarkDeliveryReadyHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public MarkDeliveryReadyHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task HandleAsync(MarkDeliveryReadyRequest request, CancellationToken ct = default)
    {
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new Falah.Domain.Common.DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "delivery_manage", null);
        }
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "delivery_manage", approval, requireApproval: true);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var delivery = await _db.Deliveries
            .FromSqlRaw("SELECT * FROM Deliveries WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.DeliveryId)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("طلب الدليفري غير موجود.");

        if (delivery.Status == nameof(DeliveryStatus.Ready)) { await transaction.CommitAsync(ct); return; }

        DeliveryTransitions.AssertTransition(Enum.Parse<DeliveryStatus>(delivery.Status), DeliveryStatus.Ready);
        delivery.Status = nameof(DeliveryStatus.Ready);
        delivery.Version++;

        _db.DeliveryEvents.Add(new Persistence.Entities.DeliveryEventEntity
        {
            Id = $"delivery-event-{Guid.NewGuid():N}", DeliveryId = delivery.Id, OrderId = delivery.OrderId,
            Type = "ready", Actor = actorUser.Name, Approver = approval?.ApproverName, CreatedAtUtc = DateTimeOffset.UtcNow,
        });

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}

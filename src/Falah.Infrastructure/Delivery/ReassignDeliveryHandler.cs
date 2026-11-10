namespace Falah.Infrastructure.Delivery;

using Falah.Application.Delivery;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Delivery;
using Falah.Domain.Printing;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Ported from reassignDelivery. The transfer-history row's own id
/// ("delivery-reassignment-{RequestId}") is the idempotency key. Reassigning to the SAME driver is a
/// harmless no-op (not an error, and writes no history) - matches the reference app's own guard
/// against a same-task double-click on "transfer" corrupting the "did it move from A to B" record
/// with a phantom second transfer.
/// </summary>
public sealed class ReassignDeliveryHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public ReassignDeliveryHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<ReassignDeliveryResult> HandleAsync(ReassignDeliveryRequest request, CancellationToken ct = default)
    {
        var historyId = $"delivery-reassignment-{request.RequestId}";
        var existingHistory = await _db.DeliveryReassignments.AsNoTracking().FirstOrDefaultAsync(h => h.Id == historyId, ct);
        if (existingHistory is not null)
        {
            var replayed = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DeliveryId, ct)
                ?? throw new InvalidOperationException("طلب الدليفري غير موجود.");
            return new ReassignDeliveryResult(replayed.Id, replayed.DriverId ?? "", replayed.DriverNameAtHandover ?? "", WasReplayed: true, WasNoOp: false);
        }

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        try
        {
            var delivery = await _db.Deliveries
                .FromSqlRaw("SELECT * FROM Deliveries WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.DeliveryId)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("طلب الدليفري غير موجود.");

            var newDriver = await _db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.NewDriverId && d.Active, ct);
            if (delivery.DriverId is null || newDriver is null || string.IsNullOrWhiteSpace(request.Reason))
                throw new DomainException("بيانات إعادة التعيين غير مكتملة.");

            // Already with this driver - harmless no-op, no history/event written.
            if (newDriver.Id == delivery.DriverId)
            {
                await transaction.CommitAsync(ct);
                return new ReassignDeliveryResult(delivery.Id, delivery.DriverId, delivery.DriverNameAtHandover ?? "", WasReplayed: false, WasNoOp: true);
            }

            var status = Enum.Parse<DeliveryStatus>(delivery.Status);
            if (status is DeliveryStatus.Settled or DeliveryStatus.Cancelled)
                throw new DomainException("لا يمكن نقل طلب تمت تسويته أو إلغاؤه بالفعل.");

            // Still in flight -> ordinary dispatch correction (delivery_manage). Outcome already
            // recorded -> the genuinely exceptional case (delivery_override) - resolved from the
            // delivery's own current status server-side, never trusted from the client.
            var afterOutcome = status is DeliveryStatus.Delivered or DeliveryStatus.Failed or DeliveryStatus.Returned;
            var permission = afterOutcome ? "delivery_override" : "delivery_manage";

            ManagerApproval? approval = null;
            if (!string.IsNullOrEmpty(request.ManagerApprovalId))
            {
                var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                    ?? throw new DomainException("المعتمد غير موجود.");
                approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                    approverUser.Role, DateTimeOffset.UtcNow, permission, request.Reason);
            }
            AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, permission, approval, requireApproval: true);

            var oldDriverId = delivery.DriverId;
            var oldDriver = await _db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == oldDriverId, ct);
            var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == delivery.OrderId, ct)
                ?? throw new InvalidOperationException("الطلب المرتبط بالدليفري غير موجود.");
            var now = DateTimeOffset.UtcNow;

            var custody = await _db.DriverCustodies.FirstOrDefaultAsync(c => c.DeliveryId == delivery.Id, ct);
            if (custody is not null && custody.Status != nameof(CustodyStatus.Settled))
                custody.DriverId = newDriver.Id;

            delivery.DriverId = newDriver.Id;
            delivery.DriverNameAtHandover = newDriver.Name;
            delivery.AssignedBy = actorUser.Name;
            delivery.AssignedAtUtc = now;
            delivery.Version++;

            _db.DeliveryReassignments.Add(new DeliveryReassignmentEntity
            {
                Id = historyId, OrderId = delivery.OrderId, DeliveryId = delivery.Id, OldDriverId = oldDriverId,
                OldDriverName = oldDriver?.Name, NewDriverId = newDriver.Id, NewDriverName = newDriver.Name,
                Reason = request.Reason.Trim(), ChangedBy = actorUser.Name, ChangedAtUtc = now,
            });
            _db.DeliveryEvents.Add(new DeliveryEventEntity
            {
                Id = $"delivery-event-{Guid.NewGuid():N}", DeliveryId = delivery.Id, OrderId = delivery.OrderId,
                DriverId = newDriver.Id, TripId = delivery.TripId, Type = "reassignment", Reason = request.Reason.Trim(),
                Actor = actorUser.Name, Approver = approval?.ApproverName, CreatedAtUtc = now,
            });

            // A fresh dispatch ticket for the NEW driver - ComposeTicketContentAsync marks it
            // "إعادة إسناد" by detecting an earlier DriverDispatch job already exists for this
            // order, so the driver holding it can never mistake it for the original dispatch.
            // Keyed on this reassignment's own RequestId - the same idempotency the reassignment
            // itself already has, so a retried/duplicate reassignment never prints twice.
            _db.PrintJobs.Add(new PrintJobEntity
            {
                Id = $"printjob-{request.RequestId}-{PrintRoute.DriverDispatch}", CreateRequestId = request.RequestId,
                OrderId = order.Id, OrderNumber = order.OrderNumber, Route = nameof(PrintRoute.DriverDispatch),
                Status = nameof(PrintJobStatus.Pending), AttemptCount = 0, IsReprint = false,
                CreatedBy = actorUser.Name, CreatedAtUtc = now,
            });

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                "نقل طلب دليفري لسائق آخر", "order",
                $"نقل من {oldDriver?.Name ?? oldDriverId} إلى {newDriver.Name} - {request.Reason}",
                afterOutcome ? "critical" : "normal", now, Reason: request.Reason, AuthorizedBy: approval?.ApproverName,
                ReferenceId: delivery.Id));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new ReassignDeliveryResult(delivery.Id, delivery.DriverId, delivery.DriverNameAtHandover, WasReplayed: false, WasNoOp: false);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            var replay = await _db.DeliveryReassignments.AsNoTracking().FirstOrDefaultAsync(h => h.Id == historyId, ct);
            if (replay is not null)
            {
                var delivery = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DeliveryId, ct)
                    ?? throw new InvalidOperationException("طلب الدليفري غير موجود.");
                return new ReassignDeliveryResult(delivery.Id, delivery.DriverId ?? "", delivery.DriverNameAtHandover ?? "", WasReplayed: true, WasNoOp: false);
            }
            throw new DeliveryConflictException("تعذر نقل الطلب بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }
    }
}

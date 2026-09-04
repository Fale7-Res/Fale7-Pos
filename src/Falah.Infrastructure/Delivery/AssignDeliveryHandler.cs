namespace Falah.Infrastructure.Delivery;

using Falah.Application.Delivery;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Delivery;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Ported from assignDelivery in the reference app: the modern per-order dispatch path. Assignment
/// IS physical receipt - atomically Ready -> Assigned -> OutForDelivery in one step, no separate
/// handover confirmation. Strictly per-order (no global "current driver" selector - a deliberate fix
/// in the reference app itself, for exactly the multi-driver-rush bleed-through bug its own comments
/// describe). The assignment DeliveryEvent's id ("delivery-assignment-{RequestId}") IS the
/// idempotency key, matching the reference app's own dedupe key exactly - the PK's own uniqueness is
/// the race-safety net for two concurrent identical requests.
/// </summary>
public sealed class AssignDeliveryHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public AssignDeliveryHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<AssignDeliveryResult> HandleAsync(AssignDeliveryRequest request, CancellationToken ct = default)
    {
        var eventId = $"delivery-assignment-{request.RequestId}";
        var existingEvent = await _db.DeliveryEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (existingEvent is not null)
        {
            var replayed = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DeliveryId, ct)
                ?? throw new InvalidOperationException("طلب الدليفري غير موجود.");
            return ToResult(replayed, wasReplayed: true);
        }

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "delivery_manage", null);
        }
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "delivery_manage", approval, requireApproval: true);

        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        try
        {
            var delivery = await _db.Deliveries
                .FromSqlRaw("SELECT * FROM Deliveries WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.DeliveryId)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("طلب الدليفري غير موجود.");
            if (delivery.Status != nameof(DeliveryStatus.Ready))
                throw new DomainException("يمكن تعيين السائق للطلبات الجاهزة فقط.");

            var driverEntity = await _db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DriverId, ct);
            var driver = driverEntity is null ? null : new Driver(driverEntity.Id, driverEntity.Name, driverEntity.Active, driverEntity.EmployeeId);
            // EmployeeId is always null in this system (no Employees/HR feature yet - see
            // CreateDriverHandler's own doc comment), so this always resolves via the driver's own
            // Active flag, exactly as AssertDriverAssignable's documented behavior for that case.
            DeliveryRules.AssertDriverAssignable(driver, employee: null);

            DeliveryTransitions.AssertTransition(DeliveryStatus.Ready, DeliveryStatus.Assigned);
            DeliveryTransitions.AssertTransition(DeliveryStatus.Assigned, DeliveryStatus.OutForDelivery);

            var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == delivery.OrderId, ct)
                ?? throw new InvalidOperationException("الطلب المرتبط بالدليفري غير موجود.");
            var facts = DeliveryFactsComposer.Compose(delivery, order);

            var now = DateTimeOffset.UtcNow;
            var tripId = $"delivery-trip-auto-{request.RequestId}";
            var custody = DeliveryRules.CreateCashCustody($"custody-assign-{request.RequestId}", tripId, facts, driver!.Id, now);

            delivery.Status = nameof(DeliveryStatus.OutForDelivery);
            delivery.DriverId = driver.Id;
            delivery.DriverNameAtHandover = driver.Name;
            delivery.TripId = tripId;
            delivery.AssignedBy = actorUser.Name;
            delivery.AssignedAtUtc = now;
            delivery.OutForDeliveryAtUtc = now;
            delivery.Version++;

            if (custody is not null)
            {
                _db.DriverCustodies.Add(new DriverCustodyEntity
                {
                    Id = custody.Id, TripId = custody.TripId, DriverId = custody.DriverId, OrderId = custody.OrderId,
                    DeliveryId = custody.DeliveryId, ExpectedPiastres = custody.Expected.Piastres,
                    CollectedPiastres = custody.Collected.Piastres, SettledPiastres = custody.Settled.Piastres,
                    DifferencePiastres = custody.Difference.Piastres, Status = nameof(CustodyStatus.Open), CreatedAtUtc = now,
                });
            }

            _db.DeliveryEvents.Add(new DeliveryEventEntity
            {
                Id = eventId, DeliveryId = delivery.Id, OrderId = delivery.OrderId, DriverId = driver.Id, TripId = tripId,
                Type = "assignment", Actor = actorUser.Name, Approver = approval?.ApproverName,
                AmountPiastres = facts.AmountToCollect.Piastres, CreatedAtUtc = now,
            });

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                "تعيين سائق لطلب دليفري", "order", $"تعيين {driver.Name} لطلب {delivery.OrderId}",
                "normal", now, AuthorizedBy: approval?.ApproverName, ReferenceId: delivery.Id));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ToResult(delivery, wasReplayed: false);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            var replay = await _db.DeliveryEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, ct);
            if (replay is not null)
            {
                var delivery = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DeliveryId, ct)
                    ?? throw new InvalidOperationException("طلب الدليفري غير موجود.");
                return ToResult(delivery, wasReplayed: true);
            }
            throw new DeliveryConflictException("تعذر تعيين السائق بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }
    }

    private static AssignDeliveryResult ToResult(DeliveryEntity delivery, bool wasReplayed) =>
        new(delivery.Id, delivery.Status, delivery.DriverId ?? "", delivery.DriverNameAtHandover ?? "", delivery.TripId ?? "", wasReplayed);
}

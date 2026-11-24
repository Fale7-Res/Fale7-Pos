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
    /// <summary>Owner decision 2026-11-24: the cash one driver may hold before a new order is refused (1,500 ج).</summary>
    /// <summary>The approval a manager gives at the dispatcher's screen to send one more order past the limit.</summary>
    public const string CustodyLimitApprovalAction = "driver_custody_limit";

    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;
    private readonly long _custodyLimitPiastres;

    public AssignDeliveryHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
        _custodyLimitPiastres = Falah.Infrastructure.Settings.OperatingLimits.MaxDriverCustodyPiastres(configuration);
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
        // Audit 2026-11-23, F-05: only an approval the check actually validated is named as the authoriser.
        var approvedBy = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "delivery_manage", approval, requireApproval: true).Approver?.ApproverName;

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
            // Delivery audit, owner decisions 2026-11-24. A cancelled order never leaves with a driver (D2), and an
            // InstaPay/wallet transfer nobody confirmed is not handed out either (D1): the driver used to be made
            // answerable for the whole order in cash, and the later confirmation counted the same money again.
            if (order.Status == "cancelled")
                throw new DomainException("الطلب ده اتلغى، ما ينفعش يتسند لسائق.");
            if (order.ElectronicPaymentStatus == "pending")
                throw new DomainException("التحويل الإلكتروني للطلب ده لسه ما اتأكدش. أكّد وصول التحويل الأول، وبعدين اسند الطلب للسائق.");
            var facts = DeliveryFactsComposer.Compose(delivery, order);

            var now = DateTimeOffset.UtcNow;
            var tripId = $"delivery-trip-auto-{request.RequestId}";
            var custody = DeliveryRules.CreateCashCustody($"custody-assign-{request.RequestId}", tripId, facts, driver!.Id, now);

            // Owner decision 2026-11-24: past the custody cap a new cash order is refused, unless a manager lets this
            // one through with their PIN (an approval for driver_custody_limit, spent here). Checked last, so a refused
            // assignment never spends it. A prepaid order adds no cash and always goes.
            string? capApprovedBy = null;
            if (custody is not null && _custodyLimitPiastres > 0)
            {
                var open = await _db.DriverCustodies.AsNoTracking()
                    .Where(c => c.DriverId == driver.Id && c.Status != nameof(CustodyStatus.Settled))
                    .Select(c => new { c.ExpectedPiastres, c.CollectedPiastres }).ToListAsync(ct);
                var carried = await DriverBalances.CarriedForDriverAsync(_db, driver.Id, ct);
                var holding = open.Sum(c => c.CollectedPiastres > 0 ? c.CollectedPiastres : c.ExpectedPiastres) + Math.Max(0, carried);
                var after = holding + custody.Expected.Piastres;
                if (after > _custodyLimitPiastres)
                {
                    if (AuthorizationPolicy.CanIssueApproval(actor, CustodyLimitApprovalAction)) capApprovedBy = actorUser.Name;
                    else if (approval is null)
                        throw new DomainException(
                            $"السائق {driver.Name} معاه {holding / 100m:0.##} ج، والطلب ده هيخليها {after / 100m:0.##} ج - أكتر من حد العهدة ({_custodyLimitPiastres / 100m:0.##} ج). سوّي السائق الأول، أو خلّي المدير يعتمد برمزه.");
                    else capApprovedBy = AuthorizationPolicy.ValidateApproval(_approvalRegistry, CustodyLimitApprovalAction, approval, actorUser.Id).ApproverName;
                    _auditLog.Append(new AuditEvent(
                        $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                        "تعدي حد عهدة السائق", "cash",
                        $"{driver.Name}: {holding} قرش + {custody.Expected.Piastres} لطلب {order.OrderNumber} - الحد {_custodyLimitPiastres}",
                        "warning", now, AuthorizedBy: capApprovedBy, ReferenceId: delivery.Id));
                }
            }

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
                Type = "assignment", Actor = actorUser.Name, Approver = approvedBy,
                AmountPiastres = facts.AmountToCollect.Piastres, CreatedAtUtc = now,
            });

            // The driver dispatch ticket (final pre-go-live pass) - one per assignment, keyed on
            // this same RequestId so it shares the handler's own idempotency: a retried/duplicate
            // assignment request never prints a second dispatch ticket. Printing never touches the
            // order/payment/inventory it describes, matching CreatePrintJobsHandler's own invariant.
            _db.PrintJobs.Add(new PrintJobEntity
            {
                Id = $"printjob-{request.RequestId}-{PrintRoute.DriverDispatch}", CreateRequestId = request.RequestId,
                OrderId = order.Id, OrderNumber = order.OrderNumber, Route = nameof(PrintRoute.DriverDispatch),
                Status = nameof(PrintJobStatus.Pending), AttemptCount = 0, IsReprint = false,
                CreatedBy = actorUser.Name, CreatedAtUtc = now,
            });

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                // The order NUMBER, not its internal id: this line is read by an owner in the audit
                // screen, and "ord-b111c8..." names nothing they can look up.
                "تعيين سائق لطلب دليفري", "order", $"تعيين {driver.Name} لطلب {order.OrderNumber}",
                "normal", now, AuthorizedBy: approvedBy, ReferenceId: delivery.Id));

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

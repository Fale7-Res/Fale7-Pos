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
/// Ported from recordDeliveryResult. Two idempotency layers, deliberately: the reference app's own
/// "already in that status" no-op check is preserved (kept for single-threaded fidelity), but this
/// adds a proper minted-RequestId key on top of it - the reference app's version had none at all
/// despite this command recording money (CollectedAmountPiastres), unlike literally every other
/// financial command in that same file. That is treated as an omission worth closing, not a
/// deviation: the "already there" check alone cannot tell a genuine retry of the exact same
/// submission apart from a second, different one that happens to land on the same status.
///
/// "delivered"'s expected amount is computed via DeliveryRules.DeliveryCashResponsibility (the
/// domain's own single source of truth, already used at assignment time) rather than the reference
/// app's slightly simpler inline expression there - see the handler body for why using two different
/// formulas for "how much should the driver be holding" would risk a subtle inconsistency.
/// </summary>
public sealed class RecordDeliveryResultHandler
{
    private static readonly IReadOnlySet<string> ValidFailureReasons = new HashSet<string>
    {
        "customer_unreachable", "customer_refused", "wrong_address", "order_problem", "driver_problem", "other",
    };

    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public RecordDeliveryResultHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<RecordDeliveryResultResult> HandleAsync(RecordDeliveryResultRequest request, CancellationToken ct = default)
    {
        var eventId = $"delivery-result-{request.RequestId}";
        var existingEvent = await _db.DeliveryEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, ct);
        if (existingEvent is not null)
        {
            var replayed = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DeliveryId, ct)
                ?? throw new InvalidOperationException("طلب الدليفري غير موجود.");
            return new RecordDeliveryResultResult(replayed.Id, replayed.Status, WasReplayed: true);
        }

        if (!Enum.TryParse<DeliveryStatus>(request.Result, ignoreCase: true, out var targetStatus)
            || targetStatus is not (DeliveryStatus.Delivered or DeliveryStatus.Failed or DeliveryStatus.Returned))
            throw new DomainException("نتيجة التوصيل غير صالحة.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "delivery_manage", request.Reason);
        }
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "delivery_manage", approval, requireApproval: true);

        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        try
        {
            var delivery = await _db.Deliveries
                .FromSqlRaw("SELECT * FROM Deliveries WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.DeliveryId)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("طلب الدليفري غير موجود.");

            // Preserved from the reference app: already in the requested status is a plain no-op.
            if (delivery.Status == targetStatus.ToString()) { await transaction.CommitAsync(ct); return new RecordDeliveryResultResult(delivery.Id, delivery.Status, WasReplayed: false); }

            var currentStatus = Enum.Parse<DeliveryStatus>(delivery.Status);
            DeliveryTransitions.AssertTransition(currentStatus, targetStatus);

            var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == delivery.OrderId, ct)
                ?? throw new InvalidOperationException("الطلب المرتبط بالدليفري غير موجود.");
            var facts = DeliveryFactsComposer.Compose(delivery, order);
            var now = DateTimeOffset.UtcNow;
            long? collectedForEvent = null;

            if (targetStatus == DeliveryStatus.Delivered)
            {
                var expected = DeliveryRules.DeliveryCashResponsibility(facts.PaymentMethod, facts.PaymentAlreadySettled, facts.AmountToCollect);
                var collected = request.CollectedAmountPiastres.HasValue ? Money.FromPiastres(request.CollectedAmountPiastres.Value) : expected;
                DeliveryRules.AssertCollectionAmount(expected, collected, request.Reason);

                delivery.DeliveredAtUtc = now;
                delivery.CollectedAmountPiastres = collected.Piastres;
                delivery.FailureNotes = request.Reason; // reused as "variance explanation", matching the reference app
                collectedForEvent = collected.Piastres;

                var custody = await _db.DriverCustodies.FirstOrDefaultAsync(c => c.DeliveryId == delivery.Id, ct);
                if (custody is not null)
                {
                    custody.CollectedPiastres = collected.Piastres;
                    custody.DifferencePiastres = collected.Piastres - custody.ExpectedPiastres;
                }
            }
            else if (targetStatus == DeliveryStatus.Failed)
            {
                if (request.Reason is null || !ValidFailureReasons.Contains(request.Reason))
                    throw new DomainException("سبب فشل التوصيل غير صالح.");
                delivery.FailedAtUtc = now;
                delivery.FailureReason = request.Reason;
                delivery.FailureNotes = request.Notes;
            }
            else // Returned
            {
                if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب المرتجع مطلوب.");
                delivery.ReturnedAtUtc = now;
                delivery.ReturnReason = request.Reason;
            }

            delivery.Status = targetStatus.ToString();
            delivery.Version++;

            _db.DeliveryEvents.Add(new DeliveryEventEntity
            {
                Id = eventId, DeliveryId = delivery.Id, OrderId = delivery.OrderId, DriverId = delivery.DriverId,
                TripId = delivery.TripId, Type = targetStatus.ToString().ToLowerInvariant(), Reason = request.Reason,
                AmountPiastres = collectedForEvent, Actor = actorUser.Name, Approver = approval?.ApproverName, CreatedAtUtc = now,
            });

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                // Both the outcome and the order are said in the reader's own terms: the status as an
                // Arabic word rather than the bare enum name, and the order by its NUMBER rather
                // than its internal id.
                $"تسجيل نتيجة توصيل: {DescribeOutcome(targetStatus)}", "order", $"طلب {order.OrderNumber} - {request.Reason}",
                targetStatus == DeliveryStatus.Delivered ? "normal" : "warning", now,
                Reason: request.Reason, AuthorizedBy: approval?.ApproverName, ReferenceId: delivery.Id));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new RecordDeliveryResultResult(delivery.Id, delivery.Status, WasReplayed: false);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            var replay = await _db.DeliveryEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId, ct);
            if (replay is not null)
            {
                var delivery = await _db.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.DeliveryId, ct)
                    ?? throw new InvalidOperationException("طلب الدليفري غير موجود.");
                return new RecordDeliveryResultResult(delivery.Id, delivery.Status, WasReplayed: true);
            }
            throw new DeliveryConflictException("تعذر تسجيل نتيجة التوصيل بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }
    }

    /// <summary>The outcome in Arabic for the audit trail. The enum name itself is unchanged and
    /// still what is stored on the delivery row and the event - this only governs the sentence a
    /// person reads.</summary>
    private static string DescribeOutcome(DeliveryStatus status) => status switch
    {
        DeliveryStatus.Delivered => "تم التسليم",
        DeliveryStatus.Failed => "فشل التسليم",
        DeliveryStatus.Returned => "مرتجع",
        _ => status.ToString(),
    };
}

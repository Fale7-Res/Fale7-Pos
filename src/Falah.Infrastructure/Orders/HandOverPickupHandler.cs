namespace Falah.Infrastructure.Orders;

using System.Data;
using Falah.Application.Orders;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Workflow redesign (owner decision 2026-11-24): a pickup's life ends when its customer takes the bag. Paying in
/// advance is not collecting, so a prepaid pickup stays in the active queue until someone at the counter presses
/// "handed over"; this records who and when, sets the order completed (it leaves the queue, the row stays for every
/// report) and the caller prints the customer's receipt. An unpaid pickup is handed over by its collection instead.
///
/// No money moves here. A transfer still waiting for confirmation does not stop the handover (the same as a
/// collection paid by transfer): it stays a pending payment, listed and blocking the day close until confirmed.
/// Replays by RequestId before anything is validated; a second, different press on a handed-over pickup is refused.
/// </summary>
public sealed class HandOverPickupHandler
{
    private readonly FalahDbContext _db;
    private readonly IAuditLog _auditLog;

    public HandOverPickupHandler(FalahDbContext db, IAuditLog auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<HandOverPickupResult> HandleAsync(HandOverPickupRequest request, CancellationToken ct = default)
    {
        InputRules.AssertRequestId(request.RequestId);
        if (await ReplayAsync(request.RequestId, request.OrderId, ct) is { } replay) return replay;

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        if (!AuthorizationPolicy.HasPermission(OrderMapper.ToActor(actorUser), "pos"))
            throw new DomainException("ليس لديك صلاحية تسليم الطلبات.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var order = await _db.Orders
            .FromSqlRaw("SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.OrderId)
            .FirstOrDefaultAsync(ct)
            ?? throw new DomainException("الطلب غير موجود.");
        if (await ReplayAsync(request.RequestId, request.OrderId, ct) is { } lockedReplay) { await transaction.CommitAsync(ct); return lockedReplay; }

        if (order.OrderType != OrderType.Pickup.ToWireString()) throw new DomainException("التسليم من هنا لطلبات الاستلام بس.");
        if (order.Status == OrderStatus.Cancelled.ToWireString()) throw new DomainException("الطلب ده اتلغى.");
        // Refunded in full: the customer's money went back, so there is nothing to hand over - and recording a handover
        // would overwrite "refunded" with "completed". A partly refunded pickup keeps its status and is handed over.
        if (order.Status == OrderStatus.Refunded.ToWireString()) throw new DomainException("الطلب ده اترجّعت فلوسه بالكامل، ما ينفعش يتسلّم.");
        if (order.AwaitingCollection) throw new DomainException("الطلب ده لسه ما اتدفعش. حصّل من العميل الأول، والتحصيل بيسلّمه.");
        if (order.HandedOverAtUtc is not null)
            throw new DomainException($"الطلب ده اتسلّم للعميل قبل كده ({order.HandedOverBy}).");

        var now = DateTimeOffset.UtcNow;
        order.HandedOverAtUtc = now;
        order.HandedOverBy = actorUser.Name;
        order.HandoverRequestId = request.RequestId;
        order.Status = OrderStatus.Completed.ToWireString();
        order.Version++;

        _auditLog.Append(new AuditEvent(
            $"audit-handover-{order.Id}", actorUser.Id, actorUser.Name, actorUser.Role,
            $"تسليم طلب استلام {order.OrderNumber}", "order",
            $"اتسلّم للعميل {order.CustomerName}" + (order.ElectronicPaymentStatus == "pending" ? " - التحويل لسه ما اتأكدش" : ""),
            "normal", now, ReferenceId: order.Id));

        try
        {
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            if (await ReplayAsync(request.RequestId, request.OrderId, ct) is { } conflictReplay) return conflictReplay;
            throw new DomainException("الطلب ده اتسلّم من جهاز تاني في نفس اللحظة. حدّث القائمة.");
        }
        return new HandOverPickupResult(order.Id, order.OrderNumber, now, WasReplayed: false);
    }

    /// <summary>A retry carries the same request id for the same order and gets the first answer back. The same id
    /// sent for a DIFFERENT order is not a retry (audit 2026-11-25): answering it with the other order's handover would
    /// tell the till this pickup was handed over when it was not.</summary>
    private async Task<HandOverPickupResult?> ReplayAsync(string requestId, string orderId, CancellationToken ct)
    {
        var done = await _db.Orders.AsNoTracking().Where(o => o.HandoverRequestId == requestId)
            .Select(o => new { o.Id, o.OrderNumber, o.HandedOverAtUtc }).FirstOrDefaultAsync(ct);
        if (done is null) return null;
        if (done.Id != orderId) throw new DomainException("رقم العملية ده اتستخدم لطلب تاني. حدّث القائمة وجرّب تاني.");
        return new HandOverPickupResult(done.Id, done.OrderNumber, done.HandedOverAtUtc!.Value, WasReplayed: true);
    }
}

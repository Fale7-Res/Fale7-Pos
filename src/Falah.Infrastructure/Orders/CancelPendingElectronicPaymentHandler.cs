namespace Falah.Infrastructure.Orders;

using System.Data;
using Falah.Application.Orders;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Wires up Order.CancelElectronicPayment (a fully-ported, tested domain method that had no
/// Application/Infrastructure/endpoint at all until this pass - a real gap found during the
/// legacy-parity audit: a cashier had no way to void a bad InstaPay/wallet claim). Ported from
/// cancelPendingElectronicPayment in lib/store.tsx: 'pos' or 'cash_drawer_sale' permission, reason
/// required, idempotent no-op if already cancelled, blocked if already confirmed (use a refund
/// instead). No inventory reversal - matches legacy exactly, which never restores stock here either
/// (checkout's own deduction is unconditional and never undone by this path).
/// </summary>
public sealed class CancelPendingElectronicPaymentHandler
{
    private readonly FalahDbContext _db;
    private readonly IAuditLog _auditLog;

    public CancelPendingElectronicPaymentHandler(FalahDbContext db, IAuditLog auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<CancelPendingElectronicPaymentResult> HandleAsync(CancelPendingElectronicPaymentRequest request, CancellationToken ct = default)
    {
        var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CashierId, ct)
            ?? throw new InvalidOperationException("الكاشير غير موجود.");
        var actor = OrderMapper.ToActor(cashierUser);
        if (!AuthorizationPolicy.HasPermission(actor, "pos") && !AuthorizationPolicy.HasPermission(actor, "cash_drawer_sale"))
            throw new DomainException("ليس لديك صلاحية إلغاء محاولة الدفع.");

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        var orderEntity = await _db.Orders
            .FromSqlRaw("SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.OrderId)
            .Include(o => o.Items).Include(o => o.Refunds)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("الطلب غير موجود.");

        var wasAlreadyCancelled = orderEntity.Status == "cancelled";
        var order = OrderMapper.ToDomain(orderEntity);
        var now = DateTimeOffset.UtcNow;
        order.CancelElectronicPayment(request.Reason, cashierUser.Name, now); // throws if confirmed/no leg/empty reason

        orderEntity.Status = order.Status.ToWireString();
        orderEntity.IsPaid = order.IsPaid;
        orderEntity.ElectronicPaymentStatus = order.ElectronicPayment?.Status.ToWireString();
        orderEntity.CancelledAtUtc = order.CancelledAtUtc;
        orderEntity.CancelReason = order.CancelReason;
        orderEntity.CancelledBy = order.CancelledBy;
        orderEntity.Version++;

        if (!wasAlreadyCancelled)
        {
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", cashierUser.Id, cashierUser.Name, cashierUser.Role,
                $"إلغاء محاولة دفع إلكتروني {orderEntity.OrderNumber}", "cash",
                $"لم يصل التحويل - {order.ElectronicPayment?.Amount.Piastres} قرش - السبب: {request.Reason.Trim()}",
                "warning", now, ShiftId: orderEntity.ShiftId, Reason: request.Reason.Trim(), ReferenceId: orderEntity.Id));
        }

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new CancelPendingElectronicPaymentResult(
            orderEntity.Id, orderEntity.Status, orderEntity.ElectronicPaymentStatus!, WasReplayed: wasAlreadyCancelled);
    }
}

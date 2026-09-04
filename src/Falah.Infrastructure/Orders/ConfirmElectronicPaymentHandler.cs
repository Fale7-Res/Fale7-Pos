namespace Falah.Infrastructure.Orders;

using System.Data;
using Falah.Application.Orders;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Confirms a still-pending InstaPay/wallet claim and, in the same atomic step, posts the
/// deferred sale_collection Treasury movement that checkout intentionally never posted for it.
/// Reuses the SAME idempotency key checkout would have used had the payment settled immediately
/// ("sale-{CheckoutRequestId}") - a claim can only ever be confirmed once, so it is exactly the
/// financial event checkout deferred, not a new one.
/// </summary>
public sealed class ConfirmElectronicPaymentHandler
{
    private readonly FalahDbContext _db;
    public ConfirmElectronicPaymentHandler(FalahDbContext db) => _db = db;

    public async Task<ConfirmElectronicPaymentResult> HandleAsync(ConfirmElectronicPaymentRequest request, CancellationToken ct = default)
    {
        var preCheck = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new InvalidOperationException("الطلب غير موجود.");
        if (preCheck.ElectronicPaymentStatus == "confirmed")
            return await BuildReplayResultAsync(preCheck, ct);

        var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CashierId, ct)
            ?? throw new InvalidOperationException("الكاشير غير موجود.");

        // Ported from confirmElectronicPayment in lib/store.tsx: `pos` or `cash_drawer_sale`, and
        // an active shift, both required before any money is confirmed/posted - a real gap found
        // during the legacy-parity audit, since neither check existed here at all: any caller who
        // knew a valid orderId + any existing cashierId could confirm a claimed electronic payment
        // and post real money with zero authorization.
        var actor = OrderMapper.ToActor(cashierUser);
        if (!AuthorizationPolicy.HasPermission(actor, "pos") && !AuthorizationPolicy.HasPermission(actor, "cash_drawer_sale"))
            throw new DomainException("ليس لديك صلاحية تأكيد التحصيل الإلكتروني.");
        if (!await _db.Shifts.AsNoTracking().AnyAsync(s => s.Status == "Open", ct))
            throw new DomainException("يجب فتح وردية قبل تأكيد التحصيل.");

        // The initial read below takes an UPDLOCK (plus ROWLOCK/HOLDLOCK) hint - SQL Server's
        // idiomatic "select for update" - so a second concurrent confirm on the same order blocks
        // right there instead of both reading "still pending" and both trying to post the
        // movement. Plain IsolationLevel.Serializable was deliberately avoided: on SQL Server it
        // takes locks progressively rather than upfront, which can deadlock two concurrent
        // read-then-write transactions on the same row instead of simply queuing one behind the
        // other the way SQLite's BEGIN IMMEDIATE did.
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var orderEntity = await _db.Orders
                .FromSqlRaw("SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.OrderId)
                .Include(o => o.Items).Include(o => o.Refunds)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("الطلب غير موجود.");

            if (orderEntity.ElectronicPaymentStatus == "confirmed")
            {
                await transaction.RollbackAsync(ct);
                return await BuildReplayResultAsync(orderEntity, ct);
            }

            var order = OrderMapper.ToDomain(orderEntity);
            order.ConfirmElectronicPayment(); // throws DomainException if there is no pending leg
            var leg = order.ElectronicPayment!;

            orderEntity.ElectronicPaymentStatus = leg.Status.ToWireString();
            orderEntity.IsPaid = order.IsPaid;
            orderEntity.Version++;

            var saleKey = $"sale-{orderEntity.CheckoutRequestId}";
            var journal = new EfFinancialJournal(_db);
            if (!journal.TryStart(saleKey, "sale"))
                throw new ConfirmConflictException("مفتاح العملية المالية لهذا الطلب مستخدم بالفعل رغم أن الدفع لم يكن مؤكدًا - يحتاج مراجعة يدوية.");

            var movement = TreasuryMovementFactory.BuildSaleMovement(
                $"tm-{Guid.NewGuid():N}", $"op-{Guid.NewGuid():N}", orderEntity.CheckoutRequestId, orderEntity.Id,
                leg.Method, leg.Amount, orderEntity.BusinessDayId, orderEntity.ShiftId, cashierUser.Name, DateTimeOffset.UtcNow);
            _db.TreasuryMovements.Add(OrderMapper.ToEntity(movement));
            journal.Complete(saleKey);

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new ConfirmElectronicPaymentResult(orderEntity.Id, orderEntity.ElectronicPaymentStatus, orderEntity.IsPaid, movement.Id, WasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbUpdateConcurrencyException or ConfirmConflictException)
        {
            await transaction.RollbackAsync(ct);
            var reloaded = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);
            if (reloaded?.ElectronicPaymentStatus == "confirmed") return await BuildReplayResultAsync(reloaded, ct);
            throw new ConfirmConflictException("تعذر تأكيد الدفع الإلكتروني بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }
    }

    private async Task<ConfirmElectronicPaymentResult> BuildReplayResultAsync(OrderEntity order, CancellationToken ct)
    {
        var movementId = await _db.TreasuryMovements.AsNoTracking()
            .Where(m => m.ReferenceId == order.Id && m.Type == "sale_collection")
            .Select(m => m.Id).FirstOrDefaultAsync(ct);
        return new ConfirmElectronicPaymentResult(order.Id, order.ElectronicPaymentStatus!, order.IsPaid, movementId, WasReplayed: true);
    }
}

namespace Falah.Infrastructure.Orders;

using System.Data;
using Falah.Application.Orders;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.Treasury;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Wires up Order.CancelElectronicPayment (a fully-ported, tested domain method that had no
/// Application/Infrastructure/endpoint at all until this pass - a real gap found during the
/// legacy-parity audit: a cashier had no way to void a bad InstaPay/wallet claim). Ported from
/// cancelPendingElectronicPayment in lib/store.tsx: 'pos' or 'cash_drawer_sale' permission, reason
/// required, idempotent no-op if already cancelled, blocked if already confirmed (use a refund
/// instead). Unlike legacy, the ingredients checkout took out of stock are accounted for: the
/// operator says whether the food was prepared (recorded as waste) or not (restored to stock) - the
/// owner's decision, applied by OrderStockDisposition.
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

        await FinancialAdmission.AcquireAsync(_db, ct);

        var orderEntity = await _db.Orders
            .FromSqlRaw("SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.OrderId)
            .Include(o => o.Items).Include(o => o.Refunds)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("الطلب غير موجود.");

        if (orderEntity.Status == "cancelled")
        {
            var returnedCash = await _db.TreasuryMovements.AsNoTracking()
                .Where(m => m.ReferenceId == orderEntity.Id && m.Type == "void_return" && m.SourceAccountId == TreasuryAccounts.Drawer)
                .SumAsync(m => (long?)m.AmountPiastres, ct) ?? 0;
            await transaction.CommitAsync(ct);
            return new CancelPendingElectronicPaymentResult(orderEntity.Id, orderEntity.Status, orderEntity.ElectronicPaymentStatus ?? "cancelled", true, returnedCash);
        }
        var disposition = Falah.Domain.Inventory.InventoryRules.ParseStockDisposition(request.StockDisposition);
        var wasAlreadyCancelled = false;
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

        long cashToHandBack = 0;
        if (!wasAlreadyCancelled)
        {
            // Reverse only money that actually posted, never the unconfirmed electronic claim.
            var collected = await _db.TreasuryMovements.AsNoTracking()
                .Where(m => m.ReferenceId == orderEntity.Id && m.Type == "sale_collection" && m.Status == nameof(MovementStatus.Posted))
                .OrderBy(m => m.DestinationAccountId).ThenBy(m => m.Id).ToListAsync(ct);
            if (collected.Count > 0)
            {
                var dayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
                var activeShift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Status == "Open", ct);
                if (collected.Any(m => m.DestinationAccountId == TreasuryAccounts.Drawer) && activeShift is null)
                    throw new DomainException("يجب فتح وردية قبل رد الجزء النقدي من الطلب الملغي.");
                foreach (var paid in collected)
                {
                    var source = paid.DestinationAccountId ?? throw new DomainException("حساب التحصيل الأصلي غير موجود.");
                    var amount = Money.FromPiastres(paid.AmountPiastres);
                    var balance = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, source, ct);
                    FinancialRules.AssertProtectedSourceBalance(balance, source, amount);
                    var reversal = TreasuryMovementFactory.BuildCashLifecycleMovement(
                        $"tm-{Guid.NewGuid():N}", $"void-return-{paid.Id}", $"op-{Guid.NewGuid():N}", "void_return",
                        source, null, amount, "order_void", orderEntity.Id, dayId, activeShift?.Id,
                        request.Reason.Trim(), cashierUser.Name, null, now);
                    _db.TreasuryMovements.Add(OrderMapper.ToEntity(reversal));
                    // Make each debit visible to the next balance check in this same transaction.
                    await _db.SaveChangesAsync(ct);
                    if (source == TreasuryAccounts.Drawer) cashToHandBack += paid.AmountPiastres;
                }
            }
            await OrderStockDisposition.ApplyAsync(_db, orderEntity.Id, disposition, request.Reason.Trim(), cashierUser.Name, now, ct);
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", cashierUser.Id, cashierUser.Name, cashierUser.Role,
                $"إلغاء محاولة دفع إلكتروني {orderEntity.OrderNumber}", "cash",
                $"لم يصل التحويل - {order.ElectronicPayment?.Amount.Piastres} قرش - السبب: {request.Reason.Trim()}",
                "warning", now, ShiftId: orderEntity.ShiftId, Reason: request.Reason.Trim(), ReferenceId: orderEntity.Id));
        }

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new CancelPendingElectronicPaymentResult(
            orderEntity.Id, orderEntity.Status, orderEntity.ElectronicPaymentStatus!, WasReplayed: wasAlreadyCancelled, CashToHandBackPiastres: cashToHandBack);
    }
}

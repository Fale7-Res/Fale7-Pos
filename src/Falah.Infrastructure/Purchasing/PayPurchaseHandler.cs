namespace Falah.Infrastructure.Purchasing;

using Falah.Application.Purchasing;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Inventory;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Treasury;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from payPurchase - a later settlement against a purchase already on the books
/// (typically one received on credit, or only partially paid at receipt time). Outstanding is always
/// read fresh from the supplier ledger (PurchasingRules.SupplierOutstandingFromLedger), never from
/// the purchase's own cached RemainingAmountPiastres, so a stale in-memory value can never let a
/// payment overshoot what is genuinely still owed.</summary>
public sealed class PayPurchaseHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public PayPurchaseHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<PurchaseDto> HandleAsync(PayPurchaseRequest request, CancellationToken ct = default)
    {
        var purchase = await _db.Purchases.Include(p => p.Lines).Include(p => p.Payments)
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseId, ct)
            ?? throw new InvalidOperationException("فاتورة الشراء غير موجودة.");
        if (purchase.Status == "cancelled") throw new DomainException("فاتورة الشراء غير متاحة للسداد.");

        var idempotencyKey = $"purchase-payment-{purchase.Id}-{request.RequestId}";
        var existingPayment = await _db.PurchasePayments.AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct);
        if (existingPayment is not null) return ReceivePurchaseHandler.ToDto(purchase, wasReplayed: true);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "purchasing_manage", purchase.InvoiceNumber);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "purchasing_manage", approval, requireApproval: true);

        var ledgerEntries = await _db.SupplierLedgerEntries.AsNoTracking()
            .Where(e => e.SupplierId == purchase.SupplierId).ToListAsync(ct);
        var outstanding = PurchasingRules.SupplierOutstandingFromLedger(
            ledgerEntries.Select(e => new SupplierLedgerEntry(e.SupplierId, e.PurchaseId, e.ReferenceId, Money.FromPiastres(e.DebitPiastres), Money.FromPiastres(e.CreditPiastres))),
            purchase.SupplierId, purchase.Id);

        var amount = Money.FromPiastres(request.AmountPiastres);
        PurchasingRules.AssertPurchasePayment(amount, outstanding);
        if (!amount.IsPositive) throw new DomainException("مبلغ السداد يجب أن يكون أكبر من صفر.");

        var accountId = PurchasingRules.PurchasePaymentAccountFor(request.PaymentMethod)
            ?? throw new DomainException("يجب تحديد حساب دفع فعلي.");
        var accountMovements = await TreasuryBalanceQuery.LoadAccountMovementsAsync(_db, accountId, ct);
        FinancialRules.AssertProtectedSourceBalance(accountMovements, accountId, amount);
        var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var journal = new Falah.Infrastructure.Persistence.EfFinancialJournal(_db);
            if (!journal.TryStart(idempotencyKey, "purchase_payment"))
                throw new PurchasingConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            var now = DateTimeOffset.UtcNow;
            var operationId = $"op-{Guid.NewGuid():N}";
            var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                $"tm-{Guid.NewGuid():N}", idempotencyKey, operationId, "purchase_payment",
                accountId, destinationAccountId: null, amount, "purchase_payment", purchase.Id, businessDayId, shiftId: null,
                $"سداد مورد للفاتورة {purchase.InvoiceNumber ?? purchase.Id}", actorUser.Name, authorization.Approver?.ApproverName, now);
            _db.TreasuryMovements.Add(OrderMapper.ToEntity(movement));

            var remaining = outstanding - amount;
            purchase.PaidPiastres += amount.Piastres;
            purchase.RemainingAmountPiastres = remaining.Piastres;
            purchase.PaymentStatus = remaining.Piastres == 0 ? "paid" : "partially_paid";
            purchase.Status = purchase.PaymentStatus;
            purchase.Payments.Add(new PurchasePaymentEntity
            {
                Id = $"payment-{request.RequestId}", IdempotencyKey = idempotencyKey, PurchaseId = purchase.Id,
                SupplierId = purchase.SupplierId, AmountPiastres = amount.Piastres, PaymentAccountId = accountId,
                TreasuryMovementId = movement.Id, OperationId = operationId, CreatedBy = actorUser.Name,
                ApprovedBy = authorization.Approver?.ApproverName, CreatedAtUtc = now,
            });

            _db.SupplierLedgerEntries.Add(new SupplierLedgerEntryEntity
            {
                Id = $"ledger-{idempotencyKey}", IdempotencyKey = idempotencyKey, SupplierId = purchase.SupplierId,
                Type = "supplier_payment", DebitPiastres = 0, CreditPiastres = amount.Piastres, PurchaseId = purchase.Id,
                ReferenceId = purchase.Id, Reason = "سداد لاحق لفاتورة مشتريات", CreatedBy = actorUser.Name,
                OperationId = operationId, TreasuryMovementId = movement.Id, CreatedAtUtc = now,
            });

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                "سداد مورد", "cash", $"سداد {amount.Piastres} قرش لفاتورة {purchase.Id} من {accountId}",
                "normal", now, AuthorizedBy: authorization.Approver?.ApproverName, OperationId: operationId, ReferenceId: purchase.Id));

            journal.Complete(idempotencyKey);
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ReceivePurchaseHandler.ToDto(purchase, wasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or PurchasingConflictException)
        {
            await transaction.RollbackAsync(ct);
            var replay = await _db.PurchasePayments.AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct);
            if (replay is not null)
            {
                var refreshed = await _db.Purchases.AsNoTracking().Include(p => p.Lines).Include(p => p.Payments)
                    .FirstAsync(p => p.Id == purchase.Id, ct);
                return ReceivePurchaseHandler.ToDto(refreshed, wasReplayed: true);
            }
            throw new PurchasingConflictException("تعذر تسجيل سداد المورد بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }
    }
}

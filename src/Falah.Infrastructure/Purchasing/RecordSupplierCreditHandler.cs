namespace Falah.Infrastructure.Purchasing;

using Falah.Application.Purchasing;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Inventory;
using Falah.Domain.Purchasing;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// A purchasing error, a supplier's credit, or goods handed back - recorded as a credit on the supplier ledger linked
/// to the invoice (SupplierCreditRules). The invoice as received, its lines, its payments and its stock receipt stay
/// exactly as they were; what is owed is the ledger's debit minus credits, so the payables screen, the aging and the
/// dashboard's supplier liabilities all follow without a second rule. No treasury movement: nothing was paid.
///
/// Goods returned also leave stock (SupplierReturn, Out) at the invoice's own unit cost, never more than the invoice
/// received less what was already returned. purchasing_manage, or a manager's approval issued for it.
/// </summary>
public sealed class RecordSupplierCreditHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public RecordSupplierCreditHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<SupplierCreditResult> HandleAsync(RecordSupplierCreditRequest request, CancellationToken ct = default)
    {
        var idempotencyKey = $"supplier-credit-{request.RequestId}";
        var existing = await FindAsync(idempotencyKey, ct);
        if (existing is not null) return await ToResultAsync(existing, wasReplayed: true, ct);

        var ledgerType = SupplierCreditRules.LedgerTypeFor(request.Kind);
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب التصحيح مطلوب.");

        var purchase = await _db.Purchases.AsNoTracking().Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == request.PurchaseId, ct)
            ?? throw new InvalidOperationException("فاتورة الشراء غير موجودة.");
        if (purchase.Status == "cancelled") throw new DomainException("فاتورة الشراء ملغاة.");

        // Goods returned: each line must be on the invoice, and its value is the invoice's own unit cost.
        var returns = new List<(string ItemId, long Quantity, long UnitCost)>();
        Money amount;
        if (request.Kind == SupplierCreditRules.GoodsReturned)
        {
            if (request.Lines is not { Count: > 0 }) throw new DomainException("حدد الأصناف المرتجعة وكمياتها.");
            var alreadyReturned = await _db.StockMovements.AsNoTracking()
                .Where(m => m.ReferenceType == "purchase" && m.ReferenceId == purchase.Id && m.Type == nameof(StockMovementType.SupplierReturn))
                .GroupBy(m => m.InventoryItemId).Select(g => new { g.Key, Quantity = g.Sum(m => m.QuantityMilliUnits) })
                .ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);
            foreach (var line in request.Lines.GroupBy(l => l.InventoryItemId))
            {
                var quantity = line.Sum(l => l.QuantityMilliUnits);
                var invoiced = purchase.Lines.Where(l => l.InventoryItemId == line.Key).ToList();
                if (invoiced.Count == 0) throw new DomainException("صنف مرتجع غير موجود في هذه الفاتورة.");
                if (quantity <= 0) throw new DomainException("كمية المرتجع يجب أن تكون أكبر من صفر.");
                var received = invoiced.Sum(l => l.QuantityMilliUnits);
                if (quantity > received - alreadyReturned.GetValueOrDefault(line.Key))
                    throw new DomainException("كمية المرتجع أكبر من الكمية المستلمة في الفاتورة (بعد المرتجعات السابقة).");
                returns.Add((line.Key, quantity, invoiced[0].UnitCostPiastres));
            }
            amount = Money.FromPiastres(returns.Sum(r => PurchasingRules.PurchaseLineTotal(r.Quantity, Money.FromPiastres(r.UnitCost)).Piastres));
            if (request.AmountPiastres is { } stated && stated != amount.Piastres)
                throw new DomainException($"قيمة المرتجع بأسعار الفاتورة {amount.Piastres / 100m:0.##} ج وليست {stated / 100m:0.##} ج.");
        }
        else
        {
            if (request.Lines is { Count: > 0 }) throw new DomainException("الأصناف تُحدَّد للمرتجعات فقط.");
            amount = Money.FromPiastres(request.AmountPiastres ?? 0);
        }

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "purchasing_manage", request.Reason);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "purchasing_manage", approval, requireApproval: true);
        var approvedBy = authorization.Approver?.ApproverName;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await FinancialAdmission.AcquireAsync(_db, ct);
            var tracked = await _db.Purchases
                .FromSqlRaw("SELECT * FROM Purchases WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", purchase.Id)
                .SingleAsync(ct);
            var admissionReplay = await FindAsync(idempotencyKey, ct);
            if (admissionReplay is not null) return await ToResultAsync(admissionReplay, wasReplayed: true, ct);
            await BusinessDayGate.RequireOpenAsync(_db, ct);

            var ledger = await _db.SupplierLedgerEntries.AsNoTracking()
                .Where(e => e.SupplierId == tracked.SupplierId && e.PurchaseId == tracked.Id)
                .Select(e => new { e.DebitPiastres, e.CreditPiastres }).ToListAsync(ct);
            var outstanding = Money.FromPiastres(ledger.Sum(e => e.DebitPiastres - e.CreditPiastres));
            SupplierCreditRules.AssertWithinOutstanding(amount, outstanding);

            var now = DateTimeOffset.UtcNow;
            var operationId = $"op-{Guid.NewGuid():N}";
            var entry = new SupplierLedgerEntryEntity
            {
                Id = $"ledger-credit-{request.RequestId}", IdempotencyKey = idempotencyKey, SupplierId = tracked.SupplierId,
                Type = ledgerType, DebitPiastres = 0, CreditPiastres = amount.Piastres, PurchaseId = tracked.Id,
                ReferenceId = tracked.Id, Reason = request.Reason.Trim(), CreatedBy = actorUser.Name,
                OperationId = operationId, CreatedAtUtc = now,
            };
            _db.SupplierLedgerEntries.Add(entry);

            var remaining = outstanding - amount;
            tracked.RemainingAmountPiastres = remaining.Piastres;
            tracked.PaymentStatus = remaining.IsZero ? "paid" : tracked.PaidPiastres > 0 ? "partially_paid" : "unpaid";
            tracked.Status = remaining.IsZero ? "paid" : tracked.PaidPiastres > 0 ? "partially_paid" : "received";

            if (returns.Count > 0)
            {
                await Falah.Infrastructure.Inventory.InventoryWriteLock.AcquireAsync(_db, ct);
                foreach (var r in returns)
                {
                    _db.StockMovements.Add(new StockMovementEntity
                    {
                        IdempotencyKey = $"supplier-return-{request.RequestId}-{r.ItemId}", InventoryItemId = r.ItemId,
                        Type = nameof(StockMovementType.SupplierReturn), Direction = nameof(StockDirection.Out),
                        QuantityMilliUnits = r.Quantity, UnitCostPiastres = r.UnitCost,
                        ReferenceType = "purchase", ReferenceId = tracked.Id, SupplierId = tracked.SupplierId,
                        Reason = $"مرتجع للمورد - {request.Reason.Trim()}", CreatedBy = actorUser.Name, ApprovedBy = approvedBy,
                        OperationId = operationId, CreatedAtUtc = now,
                    });
                }
            }

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                "تصحيح فاتورة مورد", "cash",
                $"{ledgerType}: {amount.Piastres} قرش على الفاتورة {tracked.InvoiceNumber ?? tracked.Id} - المتبقي {outstanding.Piastres} ← {remaining.Piastres} - {request.Reason.Trim()}",
                "critical", now, Reason: request.Reason.Trim(), AuthorizedBy: approvedBy, OperationId: operationId, ReferenceId: tracked.Id,
                PreviousValue: outstanding.Piastres.ToString(), NewValue: remaining.Piastres.ToString()));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return await ToResultAsync(entry, wasReplayed: false, ct);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            var replay = await FindAsync(idempotencyKey, ct);
            if (replay is not null) return await ToResultAsync(replay, wasReplayed: true, ct);
            throw new PurchasingConflictException("تعذر تسجيل التصحيح بسبب تعارض - يحتاج مراجعة يدوية.");
        }
    }

    private Task<SupplierLedgerEntryEntity?> FindAsync(string idempotencyKey, CancellationToken ct) =>
        _db.SupplierLedgerEntries.AsNoTracking().FirstOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey, ct);

    private async Task<SupplierCreditResult> ToResultAsync(SupplierLedgerEntryEntity entry, bool wasReplayed, CancellationToken ct)
    {
        var remaining = await _db.Purchases.AsNoTracking().Where(p => p.Id == entry.PurchaseId).Select(p => p.RemainingAmountPiastres).FirstOrDefaultAsync(ct);
        var kind = entry.Type switch
        {
            "purchase_correction" => SupplierCreditRules.InvoiceError,
            "supplier_return" => SupplierCreditRules.GoodsReturned,
            _ => SupplierCreditRules.SupplierCredit,
        };
        return new SupplierCreditResult(entry.Id, entry.PurchaseId!, kind, entry.CreditPiastres, remaining, wasReplayed);
    }
}

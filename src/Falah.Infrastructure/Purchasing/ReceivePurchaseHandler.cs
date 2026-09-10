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

/// <summary>Ported from receivePurchase in the reference app's lib/enterprise-store.tsx: receiving a
/// supplier invoice is three facts posted together, atomically - a supplier-ledger invoice entry
/// (debit), a stock receipt per line (PurchaseRules.PurchaseLineTotal for each), and, only if
/// something was actually paid at receipt time, a treasury movement + a matching ledger credit. Every
/// throwable validation runs BEFORE the financial journal opens, same invariant as every other
/// financial command in this codebase - a bad request must never leave a 'pending' journal entry
/// behind.</summary>
public sealed class ReceivePurchaseHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public ReceivePurchaseHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<PurchaseDto> HandleAsync(ReceivePurchaseRequest request, CancellationToken ct = default)
    {
        var purchaseId = $"purchase-{request.RequestId}";
        var existing = await FindPurchaseAsync(purchaseId, ct);
        if (existing is not null) return ToDto(existing, wasReplayed: true);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "purchasing_manage", request.InvoiceNumber);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "purchasing_manage", approval, requireApproval: true);

        var supplier = await _db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.SupplierId, ct)
            ?? throw new DomainException("المورد المحدد غير موجود.");
        if (request.Lines.Count == 0) throw new DomainException("يجب إضافة صنف واحد على الأقل للفاتورة.");

        var lines = request.Lines.Select(l => new
        {
            l.InventoryItemId, l.QuantityMilliUnits, l.UnitCostPiastres,
            LineTotal = PurchasingRules.PurchaseLineTotal(l.QuantityMilliUnits, Money.FromPiastres(l.UnitCostPiastres)),
        }).ToList();
        var itemIds = lines.Select(l => l.InventoryItemId).Distinct().ToList();
        var knownItemCount = await _db.InventoryItems.AsNoTracking().CountAsync(i => itemIds.Contains(i.Id), ct);
        if (knownItemCount != itemIds.Count) throw new DomainException("توجد خامة غير موجودة ضمن أصناف الفاتورة.");

        var total = Money.FromPiastres(lines.Sum(l => l.LineTotal.Piastres));
        var paid = Money.FromPiastres(request.PaidPiastres);
        PurchasingRules.AssertPurchasePayment(paid, total);

        var accountId = PurchasingRules.PurchasePaymentAccountFor(request.PaymentMethod);
        if (paid.IsPositive && accountId is null) throw new DomainException("يجب تحديد حساب دفع فعلي للمبلغ المدفوع.");
        if (accountId is not null)
        {
            var accountMovements = await TreasuryBalanceQuery.LoadAccountMovementsAsync(_db, accountId, ct);
            FinancialRules.AssertProtectedSourceBalance(accountMovements, accountId, paid);
        }
        // Read each ingredient's current cost basis BEFORE this receipt is written, so the change
        // this invoice causes can be recorded. See PurchasingRules.DescribeUnitCostChange for why
        // this matters more here than it looks: one line rewrites every recipe using that item.
        var previousUnitCosts = new Dictionary<string, Money?>();
        foreach (var itemId in itemIds)
        {
            var lastCost = await _db.StockMovements.AsNoTracking()
                .Where(m => m.InventoryItemId == itemId
                    && m.Type == nameof(StockMovementType.PurchaseReceipt) && m.UnitCostPiastres != null)
                .OrderByDescending(m => m.CreatedAtUtc)
                .Select(m => m.UnitCostPiastres)
                .FirstOrDefaultAsync(ct);
            previousUnitCosts[itemId] = lastCost is { } c ? Money.FromPiastres(c) : null;
        }

        var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var idempotencyKey = $"purchase-{purchaseId}";
            var journal = new Falah.Infrastructure.Persistence.EfFinancialJournal(_db);
            if (!journal.TryStart(idempotencyKey, "purchase"))
                throw new PurchasingConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            var now = DateTimeOffset.UtcNow;
            var operationId = $"op-{Guid.NewGuid():N}";

            TreasuryMovement? paymentMovement = null;
            if (paid.IsPositive && accountId is not null)
            {
                paymentMovement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                    $"tm-{Guid.NewGuid():N}", $"purchase-payment-{purchaseId}-initial", operationId, "purchase_payment",
                    accountId, destinationAccountId: null, paid, "purchase", purchaseId, businessDayId, shiftId: null,
                    $"دفعة فاتورة {request.InvoiceNumber ?? purchaseId}", actorUser.Name, authorization.Approver?.ApproverName, now);
                _db.TreasuryMovements.Add(OrderMapper.ToEntity(paymentMovement));
            }

            var paymentStatus = paid.Piastres == total.Piastres ? "paid" : paid.IsPositive ? "partially_paid" : "unpaid";
            var status = paymentStatus == "paid" ? "paid" : paymentStatus == "partially_paid" ? "partially_paid" : "received";

            var purchase = new PurchaseEntity
            {
                Id = purchaseId, IdempotencyKey = idempotencyKey, SupplierId = supplier.Id, InvoiceNumber = request.InvoiceNumber,
                SubtotalPiastres = total.Piastres, TotalPiastres = total.Piastres, PaidPiastres = paid.Piastres,
                RemainingAmountPiastres = total.Piastres - paid.Piastres, PaymentMethod = request.PaymentMethod,
                PaymentAccountId = accountId, Status = status, PaymentStatus = paymentStatus, ReceivedBy = actorUser.Name,
                Notes = request.Notes, CreatedBy = actorUser.Name, OperationId = operationId,
                TreasuryMovementId = paymentMovement?.Id, CreatedAtUtc = now, DueDate = request.DueDate,
                Lines = lines.Select(l => new PurchaseLineEntity
                {
                    PurchaseId = purchaseId, InventoryItemId = l.InventoryItemId,
                    QuantityMilliUnits = l.QuantityMilliUnits, UnitCostPiastres = l.UnitCostPiastres, LineTotalPiastres = l.LineTotal.Piastres,
                }).ToList(),
            };
            if (paymentMovement is not null)
            {
                purchase.Payments.Add(new PurchasePaymentEntity
                {
                    Id = $"payment-{request.RequestId}-initial", IdempotencyKey = paymentMovement.IdempotencyKey,
                    PurchaseId = purchaseId, SupplierId = supplier.Id, AmountPiastres = paid.Piastres,
                    PaymentAccountId = accountId!, TreasuryMovementId = paymentMovement.Id, OperationId = operationId,
                    CreatedBy = actorUser.Name, ApprovedBy = authorization.Approver?.ApproverName, CreatedAtUtc = now,
                });
            }
            _db.Purchases.Add(purchase);

            _db.SupplierLedgerEntries.Add(new SupplierLedgerEntryEntity
            {
                Id = $"ledger-invoice-{purchaseId}", IdempotencyKey = $"purchase-invoice-{purchaseId}", SupplierId = supplier.Id,
                Type = "purchase_invoice", DebitPiastres = total.Piastres, CreditPiastres = 0, PurchaseId = purchaseId,
                ReferenceId = purchaseId, Reason = $"فاتورة مشتريات {request.InvoiceNumber ?? purchaseId}",
                CreatedBy = actorUser.Name, OperationId = operationId, CreatedAtUtc = now,
            });
            if (paymentMovement is not null)
            {
                _db.SupplierLedgerEntries.Add(new SupplierLedgerEntryEntity
                {
                    Id = $"ledger-payment-{purchaseId}", IdempotencyKey = paymentMovement.IdempotencyKey, SupplierId = supplier.Id,
                    Type = "supplier_payment", DebitPiastres = 0, CreditPiastres = paid.Piastres, PurchaseId = purchaseId,
                    ReferenceId = purchaseId, Reason = "دفعة عند استلام المشتريات", CreatedBy = actorUser.Name,
                    OperationId = operationId, TreasuryMovementId = paymentMovement.Id, CreatedAtUtc = now,
                });
            }

            foreach (var line in lines)
            {
                _db.StockMovements.Add(new Falah.Infrastructure.Persistence.Entities.StockMovementEntity
                {
                    IdempotencyKey = $"purchase-stock-{purchaseId}-{line.InventoryItemId}", InventoryItemId = line.InventoryItemId,
                    Type = nameof(StockMovementType.PurchaseReceipt), Direction = nameof(StockDirection.In),
                    QuantityMilliUnits = line.QuantityMilliUnits, UnitCostPiastres = line.UnitCostPiastres,
                    ReferenceType = "purchase", ReferenceId = purchaseId, SupplierId = supplier.Id,
                    Reason = $"استلام مشتريات {request.InvoiceNumber ?? purchaseId}", CreatedBy = actorUser.Name,
                    OperationId = operationId, CreatedAtUtc = now,
                });
            }

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                "استلام فاتورة مشتريات", "cash", $"من المورد {supplier.Id} - الإجمالي {total.Piastres} قرش - المدفوع {paid.Piastres} قرش",
                "normal", now, AuthorizedBy: authorization.Approver?.ApproverName, OperationId: operationId, ReferenceId: purchaseId));

            // One audit line per ingredient whose cost basis this invoice actually moved. Recording
            // it is the whole point: before this, a mistyped unit cost changed every recipe's cost,
            // the food-cost percentage and the gross margin with nothing anywhere saying it had.
            // A tenfold move is marked 'warning' as a data-entry signal - not a verdict on the price.
            foreach (var line in lines)
            {
                var change = PurchasingRules.DescribeUnitCostChange(
                    previousUnitCosts.GetValueOrDefault(line.InventoryItemId),
                    Money.FromPiastres(line.UnitCostPiastres));
                if (!change.HasChanged) continue;

                _auditLog.Append(new AuditEvent(
                    $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                    "تغيّر أساس تكلفة خامة", "inventory_cost",
                    $"الخامة {line.InventoryItemId}: من {change.PreviousUnitCost!.Value.Piastres} إلى {change.NewUnitCost.Piastres} قرش للوحدة"
                        + $" (×{change.Factor}) - الفاتورة {request.InvoiceNumber ?? purchaseId}"
                        + (change.IsOrderOfMagnitude ? " - فرق بعشرة أضعاف أو أكثر، راجع إدخال السعر" : ""),
                    change.IsOrderOfMagnitude ? "warning" : "normal", now,
                    OperationId: operationId, ReferenceId: line.InventoryItemId,
                    PreviousValue: change.PreviousUnitCost!.Value.Piastres.ToString(),
                    NewValue: change.NewUnitCost.Piastres.ToString()));
            }

            journal.Complete(idempotencyKey);
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ToDto(purchase, wasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or PurchasingConflictException)
        {
            await transaction.RollbackAsync(ct);
            var replay = await FindPurchaseAsync(purchaseId, ct);
            if (replay is not null) return ToDto(replay, wasReplayed: true);
            throw new PurchasingConflictException("تعذر استلام فاتورة المشتريات بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }
    }

    private Task<PurchaseEntity?> FindPurchaseAsync(string purchaseId, CancellationToken ct) =>
        _db.Purchases.AsNoTracking().Include(p => p.Lines).Include(p => p.Payments)
            .FirstOrDefaultAsync(p => p.Id == purchaseId, ct);

    internal static PurchaseDto ToDto(PurchaseEntity p, bool wasReplayed) => new(
        p.Id, p.SupplierId, p.InvoiceNumber,
        p.Lines.Select(l => new PurchaseLineDto(l.InventoryItemId, l.QuantityMilliUnits, l.UnitCostPiastres, l.LineTotalPiastres)).ToList(),
        p.SubtotalPiastres, p.TotalPiastres, p.PaidPiastres, p.RemainingAmountPiastres, p.PaymentMethod, p.PaymentAccountId,
        p.Status, p.PaymentStatus, p.ReceivedBy, p.CreatedAtUtc,
        p.Payments.Select(pp => new PurchasePaymentDto(pp.Id, pp.AmountPiastres, pp.PaymentAccountId, pp.ApprovedBy, pp.CreatedAtUtc)).ToList(),
        wasReplayed, p.DueDate?.ToString("yyyy-MM-dd"));
}

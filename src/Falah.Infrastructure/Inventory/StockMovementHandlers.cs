namespace Falah.Infrastructure.Inventory;

using Falah.Application.Inventory;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Inventory;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from recordInventoryCountAdjustment. Direction/QuantityMilliUnits are trusted as
/// the human's observed correction - see RecordCountAdjustmentRequest's own doc comment for why this
/// command never recomputes them. No Treasury/financial effect at all, so - unlike every cash
/// command in this codebase - there is no EfFinancialJournal step; the StockMovements table's own
/// unique IdempotencyKey constraint is the entire race-safety net, exactly mirroring how checkout's
/// usage deduction already relies on that same constraint.</summary>
public sealed class RecordCountAdjustmentHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public RecordCountAdjustmentHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<RecordCountAdjustmentResult> HandleAsync(RecordCountAdjustmentRequest request, CancellationToken ct = default)
    {
        var idempotencyKey = $"inventory-count-{request.RequestId}";
        var existing = await _db.StockMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null) return new RecordCountAdjustmentResult(existing.Id.ToString(), existing.InventoryItemId, WasReplayed: true);

        if (!Enum.TryParse<StockDirection>(request.Direction, ignoreCase: true, out var direction))
            throw new DomainException("اتجاه حركة المخزون غير صالح.");
        InventoryRules.AssertCountAdjustmentInput(request.QuantityMilliUnits, request.Reason);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        var item = await _db.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == request.InventoryItemId, ct)
            ?? throw new InvalidOperationException("الخامة غير موجودة.");

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "inventory_manage", request.Reason);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "inventory_manage", approval, requireApproval: true);

        var entity = new StockMovementEntity
        {
            IdempotencyKey = idempotencyKey, InventoryItemId = item.Id, Type = nameof(StockMovementType.CountAdjustment),
            Direction = direction.ToString(), QuantityMilliUnits = request.QuantityMilliUnits,
            ReferenceType = "inventory_count", ReferenceId = item.Id, Reason = request.Reason.Trim(),
            CreatedBy = actorUser.Name, ApprovedBy = authorization.Approver?.ApproverName,
            OperationId = $"op-{Guid.NewGuid():N}", CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        try
        {
            _db.StockMovements.Add(entity);
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                "فرق جرد", "inventory", $"الخامة {item.Id}: {direction} {request.QuantityMilliUnits} - {request.Reason}",
                "normal", entity.CreatedAtUtc, Reason: request.Reason, AuthorizedBy: authorization.Approver?.ApproverName,
                ReferenceId: item.Id));
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.StockMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
            if (replay is not null) return new RecordCountAdjustmentResult(replay.Id.ToString(), replay.InventoryItemId, WasReplayed: true);
            throw new InventoryConflictException("تعذر تسجيل فرق الجرد بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }
        return new RecordCountAdjustmentResult(entity.Id.ToString(), entity.InventoryItemId, WasReplayed: false);
    }
}

/// <summary>Ported from recordInventoryWaste. Loss is deliberately not one bucket - waste (prep
/// error), spoilage (storage/expiry), breakage (handling accident) point at three different fixes.
/// Valued at the LAST ACTUAL PURCHASE unit cost, frozen at posting time - a later purchase at a
/// different price must never restate this loss (PurchasingRules.ValueStockLoss's own doc comment).
/// Never checked against the current balance: real stock can legitimately lag the system, and
/// blocking on that would hide the very discrepancy this command exists to record.</summary>
public sealed class RecordWasteHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public RecordWasteHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<RecordWasteResult> HandleAsync(RecordWasteRequest request, CancellationToken ct = default)
    {
        var idempotencyKey = $"inventory-waste-{request.RequestId}";
        var existing = await _db.StockMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null)
            return new RecordWasteResult(existing.Id.ToString(), existing.InventoryItemId, existing.UnitCostPiastres, existing.LossValuePiastres, WasReplayed: true);

        if (!Enum.TryParse<StockMovementType>(request.LossType, ignoreCase: true, out var lossType))
            throw new DomainException("نوع الفقد غير صالح.");
        InventoryRules.AssertWasteInput(request.QuantityMilliUnits, request.Reason, lossType);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        var item = await _db.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == request.InventoryItemId, ct)
            ?? throw new InvalidOperationException("الخامة غير موجودة.");

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "inventory_manage", request.Reason);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "inventory_manage", approval, requireApproval: true);

        var purchaseMovements = await _db.StockMovements.AsNoTracking()
            .Where(m => m.InventoryItemId == item.Id && m.Type == nameof(StockMovementType.PurchaseReceipt) && m.UnitCostPiastres != null)
            .Select(m => new { m.UnitCostPiastres, m.CreatedAtUtc })
            .ToListAsync(ct);
        var facts = purchaseMovements.Select(m => new StockMovementFacts(item.Id, StockMovementType.PurchaseReceipt, Money.FromPiastres(m.UnitCostPiastres!.Value), m.CreatedAtUtc));
        var unitCost = PurchasingRules.LastPurchaseUnitCost(facts, item.Id);
        var lossValue = PurchasingRules.ValueStockLoss(request.QuantityMilliUnits, unitCost);

        var entity = new StockMovementEntity
        {
            IdempotencyKey = idempotencyKey, InventoryItemId = item.Id, Type = lossType.ToString(),
            Direction = nameof(StockDirection.Out), QuantityMilliUnits = request.QuantityMilliUnits,
            UnitCostPiastres = unitCost?.Piastres, LossValuePiastres = lossValue?.Piastres,
            ReferenceType = "inventory_waste", ReferenceId = item.Id, Reason = request.Reason.Trim(),
            CreatedBy = actorUser.Name, ApprovedBy = authorization.Approver?.ApproverName,
            OperationId = $"op-{Guid.NewGuid():N}", CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        try
        {
            _db.StockMovements.Add(entity);
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                $"تسجيل هالك: {lossType}", "inventory",
                $"الخامة {item.Id}: {request.QuantityMilliUnits} - القيمة {(lossValue?.Piastres.ToString() ?? "غير معروفة")} - {request.Reason}",
                request.QuantityMilliUnits >= InventoryRules.LargeWasteMilliUnitsThreshold ? "critical" : "warning",
                entity.CreatedAtUtc, Reason: request.Reason, AuthorizedBy: authorization.Approver?.ApproverName,
                ReferenceId: item.Id));
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.StockMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
            if (replay is not null)
                return new RecordWasteResult(replay.Id.ToString(), replay.InventoryItemId, replay.UnitCostPiastres, replay.LossValuePiastres, WasReplayed: true);
            throw new InventoryConflictException("تعذر تسجيل الهالك بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }
        return new RecordWasteResult(entity.Id.ToString(), entity.InventoryItemId, entity.UnitCostPiastres, entity.LossValuePiastres, WasReplayed: false);
    }
}

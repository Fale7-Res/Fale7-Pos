namespace Falah.Infrastructure.Treasury;

using Falah.Application.Treasury;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>A cashier dropping excess drawer cash into the safe mid-shift. Ported from
/// postSafeDrop in the reference app: gated on its own 'safe_drop' permission (deliberately
/// distinct from 'treasury_manage' - this is a routine cashier action, not a manager-only one),
/// always sourced from tre-drawer. The shift referenced only needs to exist (a referential-integrity
/// check, matching how other handlers validate foreign ids) - the reference app does not itself
/// require that shift still be open, and this does not invent a stricter rule than that.</summary>
public sealed class SafeDropHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public SafeDropHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<SafeDropResult> HandleAsync(SafeDropRequest request, CancellationToken ct = default)
    {
        var idempotencyKey = $"safe-drop-{request.DropRequestId}";
        var existing = await FindExistingAsync(idempotencyKey, ct);
        if (existing is not null) return ToResult(existing, wasReplayed: true);

        if (request.AmountPiastres <= 0) throw new DomainException("مبلغ الإيداع الآمن يجب أن يكون أكبر من صفر.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب الإيداع الآمن مطلوب.");

        var createdByUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CreatedByUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(createdByUser);
        _ = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
            ?? throw new InvalidOperationException("الوردية غير موجودة.");

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "safe_drop", request.Reason);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "safe_drop", approval, requireApproval: true);

        var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Read under a lock, inside the transaction, immediately before spending.
            // Outside it, concurrent commands each read the same balance, each decide it covers
            // them, and each write - which is how an account holding 30 paid out three separate
            // transfers of 20. See TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync.
            var drawerMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, TreasuryAccounts.Drawer, ct);
            FinancialRules.AssertProtectedSourceBalance(drawerMovements, TreasuryAccounts.Drawer, Money.FromPiastres(request.AmountPiastres));

            var journal = new EfFinancialJournal(_db);
            if (!journal.TryStart(idempotencyKey, "safe_drop"))
                throw new TreasuryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            var now = DateTimeOffset.UtcNow;
            var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                $"tm-{Guid.NewGuid():N}", idempotencyKey, $"op-{Guid.NewGuid():N}", "safe_drop",
                // The destination is the whole point of a safe drop: cash leaves the till and goes
                // INTO the main treasury. Passing null here meant the drawer fell and nothing rose -
                // the money left the books entirely - while the audit line below said, and still
                // says, that it went to the main treasury. A movement that disagrees with its own
                // audit record is worse than either being wrong alone.
                TreasuryAccounts.Drawer, TreasuryAccounts.Main, Money.FromPiastres(request.AmountPiastres),
                "safe_drop", request.DropRequestId, businessDayId, request.ShiftId,
                request.Reason, createdByUser.Name, authorization.Approver?.ApproverName, now);

            var entity = OrderMapper.ToEntity(movement);
            _db.TreasuryMovements.Add(entity);
            journal.Complete(idempotencyKey);

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", createdByUser.Id, createdByUser.Name, createdByUser.Role,
                "إيداع آمن", "cash", $"إيداع {request.AmountPiastres} قرش من الدرج إلى الخزينة الرئيسية - {request.Reason}",
                "normal", now, ShiftId: request.ShiftId, Reason: request.Reason, AuthorizedBy: authorization.Approver?.ApproverName,
                OperationId: movement.OperationId, ReferenceId: entity.Id));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ToResult(entity, wasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or TreasuryConflictException)
        {
            await transaction.RollbackAsync(ct);
            var replay = await FindExistingAsync(idempotencyKey, ct);
            if (replay is not null) return ToResult(replay, wasReplayed: true);
            throw new TreasuryConflictException("تعذر تسجيل الإيداع الآمن بسبب تعارض في مفتاح منع التكرار، ولا يوجد إيداع سابق لإعادته - يحتاج مراجعة يدوية.");
        }
    }

    private Task<TreasuryMovementEntity?> FindExistingAsync(string idempotencyKey, CancellationToken ct) =>
        _db.TreasuryMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);

    private static SafeDropResult ToResult(TreasuryMovementEntity entity, bool wasReplayed) =>
        new(entity.Id, entity.AmountPiastres, wasReplayed);
}

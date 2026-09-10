namespace Falah.Infrastructure.CashDrawer;

using Falah.Application.CashDrawer;
using Falah.Application.Treasury;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Shifts;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Treasury;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from the 'cash_in'/'cash_out' branches of addCashMovement in lib/store.tsx: gated
/// on `cash_drawer` (permission OR a matching approval - same shape as OpenCashDrawerHandler's own
/// gate for the sibling 'drawer_open' case), requires the referenced shift to be currently open
/// (`if (!activeShift) throw`), requires an open Business Day (same postCashLifecycleMovement path
/// every other cash-lifecycle command uses), and checks the source account's protected balance
/// before posting. Posted movement Type is the literal "cash_in"/"cash_out" - matching what
/// ShiftReconciliation.ComputeExpectedCashAsync already queries for, not legacy's own
/// TreasuryMovement.type string ("cash_in_transfer"/"cash_out_transfer", used only inside its
/// separate parallel CashMovement list) - this port has one ledger, not two, so the type that the
/// reconciliation math already depends on is the one written here.</summary>
public sealed class CashierCashMovementHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public CashierCashMovementHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<CashierCashMovementResult> HandleAsync(CashierCashMovementRequest request, CancellationToken ct = default)
    {
        if (request.MovementType is not ("cash_in" or "cash_out"))
            throw new DomainException("نوع حركة النقدية غير صالح.");

        var idempotencyKey = $"{request.MovementType.Replace('_', '-')}-cm-{request.RequestId}";
        var existing = await FindExistingAsync(idempotencyKey, ct);
        if (existing is not null) return ToResult(existing, wasReplayed: true);

        if (request.AmountPiastres <= 0) throw new DomainException("المبلغ يجب أن يكون أكبر من صفر.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب حركة النقدية مطلوب.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
            ?? throw new InvalidOperationException("الوردية غير موجودة.");
        if (shift.Status != nameof(ShiftStatus.Open))
            throw new DomainException("يجب فتح وردية قبل تسجيل حركة خزنة.");

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "cash_drawer", request.Reason);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "cash_drawer", approval, requireApproval: true);

        var amount = Money.FromPiastres(request.AmountPiastres);
        var isCashIn = request.MovementType == "cash_in";
        var sourceAccount = isCashIn ? TreasuryAccounts.Main : TreasuryAccounts.Drawer;
        var destinationAccount = isCashIn ? TreasuryAccounts.Drawer : TreasuryAccounts.Main;

        var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Read under a lock, inside the transaction, immediately before spending.
            // Outside it, concurrent commands each read the same balance, each decide it covers
            // them, and each write - which is how an account holding 30 paid out three separate
            // transfers of 20. See TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync.
            var sourceMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, sourceAccount, ct);
            FinancialRules.AssertProtectedSourceBalance(sourceMovements, sourceAccount, amount);

            var journal = new EfFinancialJournal(_db);
            if (!journal.TryStart(idempotencyKey, request.MovementType))
                throw new TreasuryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            var now = DateTimeOffset.UtcNow;
            var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                $"tm-{Guid.NewGuid():N}", idempotencyKey, $"op-{Guid.NewGuid():N}", request.MovementType,
                sourceAccount, destinationAccount, amount,
                "cash_movement", $"cm-{request.RequestId}", businessDayId, request.ShiftId,
                request.Reason, actorUser.Name, authorization.Approver?.ApproverName, now);

            var entity = OrderMapper.ToEntity(movement);
            _db.TreasuryMovements.Add(entity);
            journal.Complete(idempotencyKey);

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                isCashIn ? "إيداع نقدية في الدرج" : "سحب نقدية من الدرج", "cash",
                $"{request.Reason} - المبلغ: {request.AmountPiastres} قرش", "normal", now,
                ShiftId: request.ShiftId, Reason: request.Reason, AuthorizedBy: authorization.Approver?.ApproverName,
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
            throw new TreasuryConflictException(
                "تعذر تسجيل حركة النقدية بسبب تعارض في مفتاح منع التكرار، ولا توجد حركة سابقة لإعادتها - يحتاج مراجعة يدوية.");
        }
    }

    private Task<TreasuryMovementEntity?> FindExistingAsync(string idempotencyKey, CancellationToken ct) =>
        _db.TreasuryMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);

    private static CashierCashMovementResult ToResult(TreasuryMovementEntity entity, bool wasReplayed) =>
        new(entity.Id, entity.Type, entity.AmountPiastres, wasReplayed);
}

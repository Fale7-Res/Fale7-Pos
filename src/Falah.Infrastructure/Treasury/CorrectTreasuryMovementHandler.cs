namespace Falah.Infrastructure.Treasury;

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
using Microsoft.EntityFrameworkCore;

/// <summary>
/// A manual treasury entry recorded by mistake - a transfer that never happened, a deposit typed twice, a safe drop of
/// the wrong amount - is corrected by its linked opposite (TreasuryCorrectionRules). The original stays exactly as
/// it was; the correction moves the same (or part of the) amount back the other way, so every balance derived from
/// movements is right again and both facts stay in the history.
///
/// - Only for an entry that did NOT physically happen, which the operator must confirm. Money that did move and was
///   later moved back is recorded as a new ordinary entry instead: both moves happened.
/// - Once per entry: the correction's id is derived from the original's (<c>tm-corr-{id}</c>).
/// - An entry tagged with a shift (a safe drop, a cash in/out) is corrected only while that shift is open, against
///   the same shift, so its expected cash is right at the close (ShiftReconciliation reads the correction). A closed
///   shift never moves.
/// - The account the correction draws from must hold the money (the same protected-balance check every spend uses).
/// - treasury_manage, or a manager's approval issued for it.
/// </summary>
public sealed class CorrectTreasuryMovementHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public CorrectTreasuryMovementHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public static string CorrectionIdFor(string movementId) => $"tm-corr-{movementId}";

    public async Task<CorrectTreasuryMovementResult> HandleAsync(CorrectTreasuryMovementRequest request, CancellationToken ct = default)
    {
        var idempotencyKey = $"treasury-correction-{request.RequestId}";
        var existing = await FindExistingAsync(idempotencyKey, ct);
        if (existing is not null) return await ToResultAsync(existing, wasReplayed: true, ct);

        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب التصحيح مطلوب.");
        if (!request.ConfirmNotPhysicallyMoved)
            throw new DomainException("التصحيح يعني أن الحركة لم تحدث فعلًا. إذا تحرك المال فعلًا ثم عاد، سجّل حركة جديدة (تحويل أو إيداع) بدل التصحيح.");

        var original = await _db.TreasuryMovements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == request.MovementId, ct)
            ?? throw new InvalidOperationException("الحركة غير موجودة.");
        TreasuryCorrectionRules.EnsureCorrectable(original.Type, original.Status);
        var amount = TreasuryCorrectionRules.CorrectedAmount(Money.FromPiastres(original.AmountPiastres), request.ReversedAmountPiastres);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "treasury_manage", request.Reason);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "treasury_manage", approval, requireApproval: true);
        var approvedBy = authorization.Approver?.ApproverName ?? actorUser.Name;

        // The opposite of the original: what went in comes out, what came out goes back.
        var source = original.DestinationAccountId;
        var destination = original.SourceAccountId;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await FinancialAdmission.AcquireAsync(_db, ct);
            var admissionReplay = await FindExistingAsync(idempotencyKey, ct);
            if (admissionReplay is not null) return await ToResultAsync(admissionReplay, wasReplayed: true, ct);

            var correctionId = CorrectionIdFor(original.Id);
            if (await _db.TreasuryMovements.AsNoTracking().AnyAsync(m => m.Id == correctionId, ct))
                throw new DomainException("تم تصحيح هذه الحركة بالفعل.");

            if (original.ShiftId is not null)
            {
                var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == original.ShiftId, ct);
                if (shift?.Status != nameof(ShiftStatus.Open))
                    throw new DomainException("لا يمكن تصحيح حركة وردية مغلقة؛ الوردية المغلقة لا تتغير. سجّل التصحيح بقرار من المدير والمحاسب.");
            }
            var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);

            if (source is not null)
            {
                var sourceMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, source, ct);
                FinancialRules.AssertProtectedSourceBalance(sourceMovements, source, amount);
            }

            var journal = new EfFinancialJournal(_db);
            if (!journal.TryStart(idempotencyKey, TreasuryCorrectionRules.MovementType))
                throw new TreasuryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            var now = DateTimeOffset.UtcNow;
            var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                correctionId, idempotencyKey, $"op-{Guid.NewGuid():N}", TreasuryCorrectionRules.MovementType,
                source, destination, amount, "treasury_movement", original.Id, businessDayId, original.ShiftId,
                $"تصحيح حركة مسجلة بالخطأ ({original.Type}) - {request.Reason.Trim()}", actorUser.Name, approvedBy, now);
            var entity = OrderMapper.ToEntity(movement);
            _db.TreasuryMovements.Add(entity);

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                "تصحيح حركة خزينة", "cash",
                $"تصحيح الحركة {original.Id} ({original.Type}) بمبلغ {amount.Piastres} من {original.AmountPiastres} قرش - {request.Reason.Trim()}",
                "critical", now, ShiftId: original.ShiftId, Reason: request.Reason.Trim(), AuthorizedBy: approvedBy,
                OperationId: movement.OperationId, ReferenceId: original.Id,
                PreviousValue: original.AmountPiastres.ToString(), NewValue: (original.AmountPiastres - amount.Piastres).ToString()));

            journal.Complete(idempotencyKey);
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return await ToResultAsync(entity, wasReplayed: false, ct);
        }
        catch (Exception ex) when (ex is DbUpdateException or TreasuryConflictException || BalanceLockContention.IsContention(ex))
        {
            await transaction.RollbackAsync(ct);
            var replay = await FindExistingAsync(idempotencyKey, ct);
            if (replay is null && BalanceLockContention.IsContention(ex))
                replay = await BalanceLockContention.AwaitSiblingAsync(async () => await FindExistingAsync(idempotencyKey, ct), ct);
            if (replay is not null) return await ToResultAsync(replay, wasReplayed: true, ct);
            if (BalanceLockContention.IsContention(ex)) throw new TreasuryConflictException(BalanceLockContention.RetryMessage);
            if (await _db.TreasuryMovements.AsNoTracking().AnyAsync(m => m.Id == CorrectionIdFor(request.MovementId), ct))
                throw new DomainException("تم تصحيح هذه الحركة بالفعل.");
            throw new TreasuryConflictException("تعذر تسجيل التصحيح بسبب تعارض - يحتاج مراجعة يدوية.");
        }
    }

    private Task<TreasuryMovementEntity?> FindExistingAsync(string idempotencyKey, CancellationToken ct) =>
        _db.TreasuryMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);

    private async Task<CorrectTreasuryMovementResult> ToResultAsync(TreasuryMovementEntity correction, bool wasReplayed, CancellationToken ct)
    {
        var originalType = await _db.TreasuryMovements.AsNoTracking()
            .Where(m => m.Id == correction.ReferenceId).Select(m => m.Type).FirstOrDefaultAsync(ct) ?? "";
        return new CorrectTreasuryMovementResult(correction.Id, correction.ReferenceId, originalType, correction.AmountPiastres,
            correction.SourceAccountId, correction.DestinationAccountId, correction.ApprovedBy, wasReplayed);
    }
}

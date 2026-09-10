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

/// <summary>Moving money between two of the business's own tracked accounts. Ported from
/// transferTreasury in the reference app.</summary>
public sealed class TransferHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public TransferHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<TransferResult> HandleAsync(TransferRequest request, CancellationToken ct = default)
    {
        var idempotencyKey = $"treasury-transfer-{request.TransferRequestId}";
        var existing = await FindExistingAsync(idempotencyKey, ct);
        if (existing is not null) return ToResult(existing, wasReplayed: true);

        if (request.SourceAccountId == request.DestinationAccountId) throw new DomainException("يجب أن يختلف حساب المصدر عن حساب الوجهة.");
        if (!TreasuryAccountValidation.IsKnownAccount(request.SourceAccountId) || !TreasuryAccountValidation.IsKnownAccount(request.DestinationAccountId))
            throw new DomainException("حساب الخزينة المحدد غير صالح.");
        if (request.AmountPiastres <= 0) throw new DomainException("المبلغ يجب أن يكون أكبر من صفر.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب التحويل مطلوب.");

        var createdByUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CreatedByUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(createdByUser);

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "treasury_manage", request.Reason);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "treasury_manage", approval, requireApproval: true);

        var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Read under a lock, inside the transaction, immediately before spending.
            // Outside it, concurrent commands each read the same balance, each decide it covers
            // them, and each write - which is how an account holding 30 paid out three separate
            // transfers of 20. See TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync.
            var accountMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, request.SourceAccountId, ct);
            FinancialRules.AssertProtectedSourceBalance(accountMovements, request.SourceAccountId, Money.FromPiastres(request.AmountPiastres));

            var journal = new EfFinancialJournal(_db);
            if (!journal.TryStart(idempotencyKey, "treasury_transfer"))
                throw new TreasuryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            var now = DateTimeOffset.UtcNow;
            var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                $"tm-{Guid.NewGuid():N}", idempotencyKey, $"op-{Guid.NewGuid():N}", "transfer",
                request.SourceAccountId, request.DestinationAccountId, Money.FromPiastres(request.AmountPiastres),
                "treasury_transfer", request.TransferRequestId, businessDayId, shiftId: null,
                request.Reason, createdByUser.Name, authorization.Approver?.ApproverName, now);

            var entity = OrderMapper.ToEntity(movement);
            _db.TreasuryMovements.Add(entity);
            journal.Complete(idempotencyKey);

            // Ported from TreasuryScreen.tsx's own createHandover call, right after a transfer -
            // legacy's one and only call site for this record (never for other movement types).
            _db.CashHandovers.Add(new CashHandoverEntity
            {
                Id = $"handover-{Guid.NewGuid():N}", FromAccountId = request.SourceAccountId, ToAccountId = request.DestinationAccountId,
                AmountPiastres = request.AmountPiastres, HandedBy = createdByUser.Name,
                ReceivedBy = authorization.Approver?.ApproverName ?? createdByUser.Name,
                Notes = request.Reason, TreasuryMovementId = entity.Id, CreatedAtUtc = now,
            });

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", createdByUser.Id, createdByUser.Name, createdByUser.Role,
                "تحويل بين حسابات الخزينة", "cash", $"تحويل {request.AmountPiastres} قرش من {request.SourceAccountId} إلى {request.DestinationAccountId} - {request.Reason}",
                "normal", now, Reason: request.Reason, AuthorizedBy: authorization.Approver?.ApproverName,
                OperationId: movement.OperationId, ReferenceId: entity.Id));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ToResult(entity, wasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or TreasuryConflictException
                                   || Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
        {
            await transaction.RollbackAsync(ct);
            var replay = await FindExistingAsync(idempotencyKey, ct);
            // The loser of a balance-lock race carries the same request id as the sibling that
            // won it, and that sibling may not have committed yet at this instant. Looking once
            // and giving up turns "somebody else is doing it" into an error.
            if (replay is null && Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
                replay = await Falah.Infrastructure.Treasury.BalanceLockContention.AwaitSiblingAsync(
                    async () => await FindExistingAsync(idempotencyKey, ct), ct);
            if (replay is not null) return ToResult(replay, wasReplayed: true);
            // Losing the balance-lock race wrote nothing at all, so telling the operator to go and
            // review a half-finished movement would send them looking for something that is not there.
            if (Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
                throw new TreasuryConflictException(Falah.Infrastructure.Treasury.BalanceLockContention.RetryMessage);
            throw new TreasuryConflictException("تعذر تسجيل التحويل بسبب تعارض في مفتاح منع التكرار، ولا يوجد تحويل سابق لإعادته - يحتاج مراجعة يدوية.");
        }
    }

    private Task<TreasuryMovementEntity?> FindExistingAsync(string idempotencyKey, CancellationToken ct) =>
        _db.TreasuryMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);

    private static TransferResult ToResult(TreasuryMovementEntity entity, bool wasReplayed) =>
        new(entity.Id, entity.AmountPiastres, entity.SourceAccountId!, entity.DestinationAccountId!, wasReplayed);
}

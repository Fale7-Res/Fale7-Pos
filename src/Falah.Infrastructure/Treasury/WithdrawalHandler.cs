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

/// <summary>The owner drawing capital out of the business. Ported from postOwnerWithdrawal in the
/// reference app - source is always one of the three external-capital accounts, never the drawer
/// (a cashier's operational float) or wallet.</summary>
public sealed class WithdrawalHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public WithdrawalHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<WithdrawalResult> HandleAsync(WithdrawalRequest request, CancellationToken ct = default)
    {
        var idempotencyKey = $"owner-withdrawal-{request.WithdrawalRequestId}";
        var existing = await FindExistingAsync(idempotencyKey, ct);
        if (existing is not null) return ToResult(existing, wasReplayed: true);

        if (!TreasuryAccountValidation.ExternalCapitalAccounts.Contains(request.SourceAccountId))
            throw new DomainException("حساب المصدر غير صالح لسحب المالك.");
        if (request.AmountPiastres <= 0) throw new DomainException("المبلغ يجب أن يكون أكبر من صفر.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب السحب مطلوب.");

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
        var accountMovements = await TreasuryBalanceQuery.LoadAccountMovementsAsync(_db, request.SourceAccountId, ct);
        FinancialRules.AssertProtectedSourceBalance(accountMovements, request.SourceAccountId, Money.FromPiastres(request.AmountPiastres));

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var journal = new EfFinancialJournal(_db);
            if (!journal.TryStart(idempotencyKey, "owner_withdrawal"))
                throw new TreasuryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            var now = DateTimeOffset.UtcNow;
            var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                $"tm-{Guid.NewGuid():N}", idempotencyKey, $"op-{Guid.NewGuid():N}", "owner_withdrawal",
                sourceAccountId: request.SourceAccountId, destinationAccountId: null, Money.FromPiastres(request.AmountPiastres),
                "owner_withdrawal", request.Reference ?? request.WithdrawalRequestId, businessDayId, shiftId: null,
                request.Reason, createdByUser.Name, authorization.Approver?.ApproverName, now);

            var entity = OrderMapper.ToEntity(movement);
            _db.TreasuryMovements.Add(entity);
            journal.Complete(idempotencyKey);

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", createdByUser.Id, createdByUser.Name, createdByUser.Role,
                "سحب مالك", "cash", $"سحب {request.AmountPiastres} قرش من {request.SourceAccountId} - {request.Reason}",
                "critical", now, Reason: request.Reason, AuthorizedBy: authorization.Approver?.ApproverName,
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
            throw new TreasuryConflictException("تعذر تسجيل السحب بسبب تعارض في مفتاح منع التكرار، ولا يوجد سحب سابق لإعادته - يحتاج مراجعة يدوية.");
        }
    }

    private Task<TreasuryMovementEntity?> FindExistingAsync(string idempotencyKey, CancellationToken ct) =>
        _db.TreasuryMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);

    private static WithdrawalResult ToResult(TreasuryMovementEntity entity, bool wasReplayed) =>
        new(entity.Id, entity.AmountPiastres, entity.SourceAccountId!, wasReplayed);
}

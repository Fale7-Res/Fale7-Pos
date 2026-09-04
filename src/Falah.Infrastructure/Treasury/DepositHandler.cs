namespace Falah.Infrastructure.Treasury;

using Falah.Application.Treasury;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>External capital entering the business (owner deposit, capital injection, other
/// inflow). Ported from postExternalFunding in the reference app's lib/enterprise-store.tsx: every
/// validation runs BEFORE the financial journal opens, so a bad request can never leave a
/// 'pending' journal entry behind - the whole thing is one SQL transaction here, so a thrown
/// exception rolls the journal insert back too; there is no separate discard step needed.</summary>
public sealed class DepositHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public DepositHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<DepositResult> HandleAsync(DepositRequest request, CancellationToken ct = default)
    {
        var idempotencyKey = $"{request.Type.Replace('_', '-')}-{request.DepositRequestId}";
        var existing = await FindExistingAsync(idempotencyKey, ct);
        if (existing is not null) return ToResult(existing, wasReplayed: true);

        if (!TreasuryAccountValidation.ValidDepositTypes.Contains(request.Type))
            throw new DomainException("نوع الإيداع غير صالح.");
        if (!TreasuryAccountValidation.ExternalCapitalAccounts.Contains(request.DestinationAccountId))
            throw new DomainException("حساب الوجهة غير صالح لهذا النوع من الإيداع.");
        if (request.AmountPiastres <= 0) throw new DomainException("المبلغ يجب أن يكون أكبر من صفر.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب الإيداع مطلوب.");

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
            var journal = new EfFinancialJournal(_db);
            if (!journal.TryStart(idempotencyKey, request.Type))
                throw new TreasuryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            var now = DateTimeOffset.UtcNow;
            var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                $"tm-{Guid.NewGuid():N}", idempotencyKey, $"op-{Guid.NewGuid():N}", request.Type,
                sourceAccountId: null, destinationAccountId: request.DestinationAccountId, Money.FromPiastres(request.AmountPiastres),
                "external_funding", request.Reference ?? request.DepositRequestId, businessDayId, shiftId: null,
                request.Reason, createdByUser.Name, authorization.Approver?.ApproverName, now);

            var entity = OrderMapper.ToEntity(movement);
            _db.TreasuryMovements.Add(entity);
            journal.Complete(idempotencyKey);

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", createdByUser.Id, createdByUser.Name, createdByUser.Role,
                "إيداع خارجي", "cash", $"إيداع {request.AmountPiastres} قرش في {request.DestinationAccountId} ({request.Type}) - {request.Reason}",
                "normal", now, Reason: request.Reason, AuthorizedBy: authorization.Approver?.ApproverName,
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
            throw new TreasuryConflictException("تعذر تسجيل الإيداع بسبب تعارض في مفتاح منع التكرار، ولا يوجد إيداع سابق لإعادته - يحتاج مراجعة يدوية.");
        }
    }

    private Task<Persistence.Entities.TreasuryMovementEntity?> FindExistingAsync(string idempotencyKey, CancellationToken ct) =>
        _db.TreasuryMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);

    private static DepositResult ToResult(Persistence.Entities.TreasuryMovementEntity entity, bool wasReplayed) =>
        new(entity.Id, entity.Type, entity.AmountPiastres, entity.DestinationAccountId!, wasReplayed);
}

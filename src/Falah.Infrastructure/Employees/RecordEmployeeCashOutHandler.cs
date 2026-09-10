namespace Falah.Infrastructure.Employees;

using Falah.Application.Employees;
using Falah.Application.Treasury;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Employees;
using Falah.Domain.Financial;
using Falah.Domain.Shifts;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Treasury;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from postEmployeeCashOut in lib/store.tsx - recordAdvance ("advance") and
/// recordWithdrawal ("withdrawal") share this one implementation there, and share it here too.
/// Always sourced from tre-drawer, requires an open shift and business day (same
/// postCashLifecycleMovement path every other cash-lifecycle command uses), gated on the dedicated
/// `employees_ledger` permission. A withdrawal additionally cannot exceed the employee's current
/// ledger balance (a real advance has no such bound - it is money given in expectation of FUTURE
/// work, matching legacy's own asymmetric guard).</summary>
public sealed class RecordEmployeeCashOutHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public RecordEmployeeCashOutHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<EmployeeCashOutResult> HandleAsync(RecordEmployeeCashOutRequest request, CancellationToken ct = default)
    {
        if (request.Kind is not ("advance" or "withdrawal"))
            throw new DomainException("نوع حركة الصرف غير صالح.");

        var idempotencyKey = $"employee-{request.Kind}-led-{request.Kind}-{request.RequestId}";
        var existing = await _db.EmployeeLedgerEntries.AsNoTracking().FirstOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null) return new EmployeeCashOutResult(existing.Id, existing.BalanceAfterPiastres, existing.TreasuryMovementId, WasReplayed: true);

        var amount = Money.FromPiastres(request.AmountPiastres);
        EmployeeRules.AssertLedgerAmount(amount);
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new DomainException(request.Kind == "advance" ? "سبب السلفة مطلوب." : "سبب السحب مطلوب.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new InvalidOperationException("الموظف غير موجود.");

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // An empty ledger has no stable row to serialize on. Concurrent first
            // advances can deadlock while converting its index-range locks. Lock the
            // existing employee row first, then inspect the ledger and replay receipt.
            await _db.Employees
                .FromSqlRaw("SELECT * FROM Employees WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.EmployeeId)
                .Select(e => e.Id).SingleAsync(ct);
            var completed = await _db.EmployeeLedgerEntries.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey, ct);
            if (completed is not null)
                return new EmployeeCashOutResult(completed.Id, completed.BalanceAfterPiastres, completed.TreasuryMovementId, WasReplayed: true);

            var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
                ?? throw new InvalidOperationException("الوردية غير موجودة.");
            if (shift.Status != nameof(ShiftStatus.Open))
                throw new DomainException(request.Kind == "advance" ? "يجب فتح وردية قبل صرف سلفة." : "يجب فتح وردية قبل سحب مبلغ من رصيد موظف.");

            ManagerApproval? approval = null;
            if (!string.IsNullOrEmpty(request.ManagerApprovalId))
            {
                var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                    ?? throw new DomainException("المعتمد غير موجود.");
                approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                    approverUser.Role, DateTimeOffset.UtcNow, "employees_ledger", request.Reason);
            }
            var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "employees_ledger", approval, requireApproval: true);

            var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
            var drawerMovements = await TreasuryBalanceQuery.LoadAccountMovementsAsync(_db, TreasuryAccounts.Drawer, ct);
            FinancialRules.AssertProtectedSourceBalance(drawerMovements, TreasuryAccounts.Drawer, amount);

            // Two concurrent cash-outs for the SAME employee (different RequestIds, so the
            // idempotency-key check above cannot catch this) must not both read the same stale
            // balance and both pass AssertWithdrawalWithinBalance. UPDLOCK on this employee's own
            // ledger rows - the same "lock what you're about to recompute from" pattern
            // RefundOrderHandler/SettleDriverCustodyHandler already use - makes the second request
            // block here until the first fully commits, then see the truly-updated balance.
            var lockedEntries = await _db.EmployeeLedgerEntries
                .FromSqlRaw("SELECT * FROM EmployeeLedgerEntries WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE EmployeeId = {0}", request.EmployeeId)
                .Select(e => new { e.CreditPiastres, e.DebitPiastres })
                .ToListAsync(ct);
            var currentBalance = Money.FromPiastres(lockedEntries.Sum(e => e.CreditPiastres - e.DebitPiastres));
            if (request.Kind == "withdrawal")
                EmployeeRules.AssertWithdrawalWithinBalance(amount, currentBalance);

            var journal = new EfFinancialJournal(_db);
            if (!journal.TryStart(idempotencyKey, request.Kind == "advance" ? "employee_advance" : "employee_withdrawal"))
                throw new TreasuryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            var now = DateTimeOffset.UtcNow;
            var description = request.Kind == "advance" ? $"سلفة نقدية عاجلة: {request.Reason}" : $"سحب نقدي من الرصيد: {request.Reason}";
            var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                $"tm-{Guid.NewGuid():N}", idempotencyKey, $"op-{Guid.NewGuid():N}", request.Kind == "advance" ? "employee_advance" : "employee_withdrawal",
                TreasuryAccounts.Drawer, destinationAccountId: null, amount,
                "employee_ledger", idempotencyKey, businessDayId, request.ShiftId,
                $"{description} - {employee.Name}", actorUser.Name, authorization.Approver?.ApproverName, now);
            var movementEntity = OrderMapper.ToEntity(movement);
            _db.TreasuryMovements.Add(movementEntity);
            journal.Complete(idempotencyKey);

            var newBalance = currentBalance - amount;
            var ledgerEntity = new EmployeeLedgerEntryEntity
            {
                Id = $"led-{request.Kind}-{request.RequestId}", EmployeeId = request.EmployeeId, Type = request.Kind == "advance" ? nameof(LedgerEntryType.Advance) : nameof(LedgerEntryType.Withdrawal),
                Description = description, DebitPiastres = amount.Piastres, CreditPiastres = 0,
                BalanceAfterPiastres = newBalance.Piastres, RecordedBy = actorUser.Name,
                AuthorizedBy = authorization.Approver?.ApproverName, ShiftId = request.ShiftId,
                TreasuryMovementId = movementEntity.Id, IdempotencyKey = idempotencyKey, CreatedAtUtc = now,
            };
            _db.EmployeeLedgerEntries.Add(ledgerEntity);

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                request.Kind == "advance" ? $"صرف سلفة للموظف {employee.Name}" : $"سحب من رصيد الموظف {employee.Name}",
                "employee", $"{description} - الرصيد الجديد: {newBalance.Piastres} قرش", "warning", now,
                ShiftId: request.ShiftId, Reason: request.Reason, AuthorizedBy: authorization.Approver?.ApproverName,
                OperationId: movement.OperationId, ReferenceId: ledgerEntity.Id));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new EmployeeCashOutResult(ledgerEntity.Id, ledgerEntity.BalanceAfterPiastres, movementEntity.Id, WasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or TreasuryConflictException)
        {
            await transaction.RollbackAsync(ct);
            var replay = await _db.EmployeeLedgerEntries.AsNoTracking().FirstOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey, ct);
            if (replay is not null) return new EmployeeCashOutResult(replay.Id, replay.BalanceAfterPiastres, replay.TreasuryMovementId, WasReplayed: true);
            throw new TreasuryConflictException("تعذر تسجيل حركة الموظف بسبب تعارض في مفتاح منع التكرار، ولا توجد حركة سابقة لإعادتها - يحتاج مراجعة يدوية.");
        }
    }
}

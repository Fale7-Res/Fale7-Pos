namespace Falah.Infrastructure.Employees;

using Falah.Application.Employees;
using Falah.Application.Treasury;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Employees;
using Falah.Domain.Financial;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Treasury;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The standalone "PAY EMPLOYEE" flow (final pre-go-live business rule): the employee's continuous
/// Employee Ledger balance may be paid down at ANY time, in ANY amount up to that balance - no
/// payroll run required, no mandatory interval. Deliberately modelled as a hybrid of the two
/// existing patterns it sits between: RecordEmployeeCashOutHandler's own balance-locking/idempotency
/// discipline (this is a per-employee, any-time action, not a bulk run), and PayPayrollHandler's own
/// account set and BusinessDayGate-only scoping (this pays from the restaurant's own accounts, not
/// the shift drawer - a drawer-sourced, shift-scoped payment against balance already exists as
/// RecordEmployeeCashOutRequest's "withdrawal" kind and is untouched by this flow).
///
/// Posts exactly one EmployeeLedgerEntry (Type=Settlement, a debit - distinct from Advance,
/// Withdrawal, and PayrollSettlement, so a payment through THIS flow is never confused with any of
/// the other three in the ledger's own history) and exactly one TreasuryMovement, atomically, keyed
/// on the caller's own RequestId so a retry or a double-click can never double-pay.
/// </summary>
public sealed class PayEmployeeHandler
{
    private static readonly IReadOnlySet<string> ValidAccounts = new HashSet<string> { "tre-main", "tre-bank", "tre-instapay" };

    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public PayEmployeeHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<PayEmployeeResult> HandleAsync(PayEmployeeRequest request, CancellationToken ct = default)
    {
        if (!ValidAccounts.Contains(request.PaymentAccountId))
            throw new DomainException("حساب الدفع غير صالح لدفع مستحقات الموظف.");

        var ledgerEntryId = $"led-employee-payment-{request.RequestId}";
        var idempotencyKey = $"employee-payment-{request.RequestId}";

        async Task<PayEmployeeResult?> LoadReplayAsync()
        {
            var entry = await _db.EmployeeLedgerEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == ledgerEntryId, ct);
            if (entry is null) return null;
            var movement = await _db.TreasuryMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
            return new PayEmployeeResult(entry.Id, entry.BalanceAfterPiastres, movement?.Id ?? "", WasReplayed: true);
        }

        if (await LoadReplayAsync() is { } alreadyPaid) return alreadyPaid;

        var amount = Money.FromPiastres(request.AmountPiastres);
        EmployeeRules.AssertLedgerAmount(amount);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new InvalidOperationException("الموظف غير موجود.");

        var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Same "lock the employee row first" discipline as RecordEmployeeCashOutHandler: an
            // employee with no ledger rows yet has no stable row of their own to serialize two
            // concurrent first-payments on.
            await _db.Employees
                .FromSqlRaw("SELECT * FROM Employees WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.EmployeeId)
                .Select(e => e.Id).SingleAsync(ct);
            if (await LoadReplayAsync() is { } settledConcurrently) return settledConcurrently;

            ManagerApproval? approval = null;
            if (!string.IsNullOrEmpty(request.ManagerApprovalId))
            {
                var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                    ?? throw new DomainException("المعتمد غير موجود.");
                approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                    approverUser.Role, DateTimeOffset.UtcNow, "employees_ledger", request.Reason ?? "دفع مستحقات موظف");
            }
            var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "employees_ledger", approval, requireApproval: true);

            // Two concurrent payments for the SAME employee (different RequestIds) must not both
            // read the same stale balance and both pass the balance check - same UPDLOCK-on-the-
            // ledger pattern RecordEmployeeCashOutHandler already uses.
            var lockedEntries = await _db.EmployeeLedgerEntries
                .FromSqlRaw("SELECT * FROM EmployeeLedgerEntries WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE EmployeeId = {0}", request.EmployeeId)
                .Select(e => new { e.CreditPiastres, e.DebitPiastres })
                .ToListAsync(ct);
            var currentBalance = Money.FromPiastres(lockedEntries.Sum(e => e.CreditPiastres - e.DebitPiastres));
            EmployeeRules.AssertPaymentWithinBalance(amount, currentBalance);

            var accountMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, request.PaymentAccountId, ct);
            FinancialRules.AssertProtectedSourceBalance(accountMovements, request.PaymentAccountId, amount);

            var journal = new EfFinancialJournal(_db);
            if (!journal.TryStart(idempotencyKey, "employee_payment"))
                throw new TreasuryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            var now = DateTimeOffset.UtcNow;
            var description = string.IsNullOrWhiteSpace(request.Reason)
                ? $"دفع مستحقات: {employee.Name}" : $"دفع مستحقات: {employee.Name} - {request.Reason}";
            var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                $"tm-{Guid.NewGuid():N}", idempotencyKey, $"op-{Guid.NewGuid():N}", "employee_payment",
                request.PaymentAccountId, destinationAccountId: null, amount,
                "employee", ledgerEntryId, businessDayId, shiftId: null,
                description, actorUser.Name, authorization.Approver?.ApproverName, now);
            var movementEntity = OrderMapper.ToEntity(movement);
            _db.TreasuryMovements.Add(movementEntity);
            journal.Complete(idempotencyKey);

            var newBalance = currentBalance - amount;
            var ledgerEntity = await EmployeeLedgerPoster.PostIfNewAsync(_db, ledgerEntryId, request.EmployeeId,
                nameof(LedgerEntryType.Settlement), description, amount, Money.Zero,
                actorUser.Name, authorization.Approver?.ApproverName, null, movementEntity.Id, null, null, null, ct)
                ?? throw new TreasuryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                $"دفع مستحقات للموظف {employee.Name}", "employee",
                $"{description} - الرصيد المتبقي: {newBalance.Piastres} قرش", "warning", now,
                Reason: request.Reason, AuthorizedBy: authorization.Approver?.ApproverName,
                OperationId: movement.OperationId, ReferenceId: ledgerEntity.Id));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new PayEmployeeResult(ledgerEntity.Id, ledgerEntity.BalanceAfterPiastres, movementEntity.Id, WasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or TreasuryConflictException
                                   || BalanceLockContention.IsContention(ex))
        {
            await transaction.RollbackAsync(ct);
            if (await LoadReplayAsync() is { } replay) return replay;
            if (BalanceLockContention.IsContention(ex)
                && await BalanceLockContention.AwaitSiblingAsync(LoadReplayAsync, ct) is { } sibling)
                return sibling;
            if (BalanceLockContention.IsContention(ex))
                throw new TreasuryConflictException(BalanceLockContention.RetryMessage);
            throw new TreasuryConflictException("تعذر دفع مستحقات الموظف بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }
    }
}

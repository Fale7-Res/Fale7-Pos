namespace Falah.Infrastructure.Employees;

using Falah.Application.Employees;
using Falah.Application.Treasury;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Employees;
using Falah.Domain.Financial;
using Falah.Domain.Payroll;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Treasury;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from payPayroll in lib/enterprise-store.tsx. Gated on `payroll_pay`, requires
/// the run to already be Approved, requires an open business day, checks the paying account's
/// protected balance for the run's WHOLE total, posts exactly one Treasury movement for that total,
/// then one 'payroll_settlement' ledger debit per line (reducing each paid employee's own running
/// balance by what they were just paid) - never one movement per employee, matching legacy's own
/// single-movement-for-the-whole-run design. Idempotent on `payroll-payment-{runId}`.</summary>
public sealed class PayPayrollHandler
{
    private static readonly IReadOnlySet<string> ValidAccounts = new HashSet<string> { "tre-main", "tre-bank", "tre-instapay" };

    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public PayPayrollHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<PayPayrollResult> HandleAsync(PayPayrollRequest request, CancellationToken ct = default)
    {
        if (!ValidAccounts.Contains(request.PaymentAccountId))
            throw new DomainException("حساب الدفع غير صالح لصرف Payroll.");

        var idempotencyKey = $"payroll-payment-{request.PayrollRunId}";

        async Task<PayPayrollResult?> LoadReplayAsync()
        {
            var movement = await _db.TreasuryMovements.AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
            if (movement is null) return null;
            var settledCount = await _db.PayrollLines.AsNoTracking()
                .CountAsync(l => l.PayrollRunId == request.PayrollRunId && l.Status == "paid", ct);
            return new PayPayrollResult(movement.Id, movement.AmountPiastres, settledCount, WasReplayed: true);
        }

        if (await LoadReplayAsync() is { } alreadyPaid) return alreadyPaid;

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        var run = await _db.PayrollRuns.FirstOrDefaultAsync(r => r.Id == request.PayrollRunId, ct)
            ?? throw new InvalidOperationException("مسير الرواتب غير موجود.");
        if (run.Status == nameof(PayrollRunStatus.Paid))
        {
            // A concurrent request settled this run between the idempotency lookup above and this
            // read. A paid run is not an unapproved one: the caller must see the same result the
            // winning request saw, not a rejection that would send an operator to pay it again.
            // Reading Paid here proves the winner committed, and its movement commits in the same
            // transaction, so the replay is guaranteed to be visible.
            if (await LoadReplayAsync() is { } settledConcurrently) return settledConcurrently;
        }
        if (run.Status != nameof(PayrollRunStatus.Approved))
            throw new DomainException("يجب اعتماد مسير الرواتب قبل الدفع.");

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "payroll_pay", $"صرف Payroll {run.Period}");
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "payroll_pay", approval, requireApproval: true);

        var lines = await _db.PayrollLines.Where(l => l.PayrollRunId == request.PayrollRunId).ToListAsync(ct);
        var total = lines.Sum(l => l.NetPayablePiastres);
        if (total <= 0)
        {
            // Refusing is right - there is nothing to pay. But "the total is invalid" tells an
            // operator nothing about why, and the commonest cause by far is silent: an employee
            // marked Monthly whose monthly salary was never entered computes a base of zero, no
            // matter how many daily-wage entries their ledger holds (see
            // PayrollRules.CalculatePayrollLines). Naming them turns a dead end into a task.
            var employeeIds = lines.Select(l => l.EmployeeId).ToList();
            var unpriced = await _db.Employees.AsNoTracking()
                .Where(e => employeeIds.Contains(e.Id)
                    && e.CompensationType == nameof(CompensationType.Monthly)
                    && (e.MonthlySalaryPiastres == null || e.MonthlySalaryPiastres == 0))
                .Select(e => e.Name).ToListAsync(ct);

            throw new DomainException(lines.Count == 0
                ? $"لا توجد بنود في مسير {run.Period} - لم تُسجَّل أجور لهذه الفترة بعد."
                : unpriced.Count > 0
                    ? $"إجمالي المسير صفر. لا يوجد راتب شهري مُسجَّل للموظفين: {string.Join("، ", unpriced)}."
                    : "إجمالي المسير صفر بعد الاستقطاعات، فلا يوجد مبلغ للصرف.");
        }
        var totalMoney = Money.FromPiastres(total);

        var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Read under a lock, inside the transaction, immediately before spending.
            // Outside it, concurrent commands each read the same balance, each decide it covers
            // them, and each write - which is how an account holding 30 paid out three separate
            // transfers of 20. See TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync.
            var accountMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, request.PaymentAccountId, ct);
            FinancialRules.AssertProtectedSourceBalance(accountMovements, request.PaymentAccountId, totalMoney);

            var journal = new EfFinancialJournal(_db);
            if (!journal.TryStart(idempotencyKey, "payroll_payment"))
                throw new TreasuryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            var now = DateTimeOffset.UtcNow;
            var operationId = $"op-{Guid.NewGuid():N}";
            var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                $"tm-{Guid.NewGuid():N}", idempotencyKey, operationId, "payroll_payment",
                request.PaymentAccountId, destinationAccountId: null, totalMoney,
                "payroll", run.Id, businessDayId, shiftId: null,
                $"صرف Payroll {run.Period}", actorUser.Name, authorization.Approver?.ApproverName, now);
            var movementEntity = OrderMapper.ToEntity(movement);
            _db.TreasuryMovements.Add(movementEntity);
            journal.Complete(idempotencyKey);

            var settlementCount = 0;
            foreach (var line in lines.Where(l => l.NetPayablePiastres > 0))
            {
                line.Status = "paid";
                line.PaidAtUtc = now;
                var settlementId = $"payroll-settlement-{run.Id}-{line.Id}";
                line.SettlementEntryId = settlementId;
                await EmployeeLedgerPoster.PostIfNewAsync(_db, settlementId, line.EmployeeId, nameof(LedgerEntryType.PayrollSettlement),
                    $"تسوية Payroll {run.Period}", Money.FromPiastres(line.NetPayablePiastres), Money.Zero,
                    actorUser.Name, authorization.Approver?.ApproverName, null, run.Id, null, null, null, ct);
                settlementCount++;
            }

            run.Status = nameof(PayrollRunStatus.Paid);
            run.PaidAtUtc = now;
            run.OperationId = operationId;
            run.TreasuryMovementId = movementEntity.Id;
            run.PaymentAccountId = request.PaymentAccountId;
            run.IdempotencyKey = idempotencyKey;

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                $"صرف Payroll {run.Period}", "employee", $"إجمالي {totalMoney.Piastres} قرش لعدد {settlementCount} موظف من {request.PaymentAccountId}",
                "critical", now, AuthorizedBy: authorization.Approver?.ApproverName, OperationId: operationId, ReferenceId: run.Id));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new PayPayrollResult(movementEntity.Id, totalMoney.Piastres, settlementCount, WasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or TreasuryConflictException
                                   || Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
        {
            await transaction.RollbackAsync(ct);
            if (await LoadReplayAsync() is { } replay) return replay;
            // The loser of a balance-lock race carries the same request id as the sibling that won
            // it, and that sibling may not have committed yet at this instant.
            if (Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex)
                && await Falah.Infrastructure.Treasury.BalanceLockContention.AwaitSiblingAsync(
                    async () => await LoadReplayAsync(), ct) is { } sibling)
                return sibling;
            // Losing the balance-lock race wrote nothing at all, so telling the operator to go and
            // review a half-finished movement would send them looking for something that is not there.
            if (Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
                throw new TreasuryConflictException(Falah.Infrastructure.Treasury.BalanceLockContention.RetryMessage);
            throw new TreasuryConflictException("تعذر صرف Payroll بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }
    }
}

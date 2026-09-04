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
        var existingMovement = await _db.TreasuryMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
        if (existingMovement is not null)
        {
            var settledCount = await _db.PayrollLines.AsNoTracking().CountAsync(l => l.PayrollRunId == request.PayrollRunId && l.Status == "paid", ct);
            return new PayPayrollResult(existingMovement.Id, existingMovement.AmountPiastres, settledCount, WasReplayed: true);
        }

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        var run = await _db.PayrollRuns.FirstOrDefaultAsync(r => r.Id == request.PayrollRunId, ct)
            ?? throw new InvalidOperationException("مسير الرواتب غير موجود.");
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
        if (total <= 0) throw new DomainException("إجمالي Payroll غير صالح للدفع.");
        var totalMoney = Money.FromPiastres(total);

        var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
        var accountMovements = await TreasuryBalanceQuery.LoadAccountMovementsAsync(_db, request.PaymentAccountId, ct);
        FinancialRules.AssertProtectedSourceBalance(accountMovements, request.PaymentAccountId, totalMoney);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
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
        catch (Exception ex) when (ex is DbUpdateException or TreasuryConflictException)
        {
            await transaction.RollbackAsync(ct);
            var replay = await _db.TreasuryMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
            if (replay is not null)
            {
                var settledCount = await _db.PayrollLines.AsNoTracking().CountAsync(l => l.PayrollRunId == request.PayrollRunId && l.Status == "paid", ct);
                return new PayPayrollResult(replay.Id, replay.AmountPiastres, settledCount, WasReplayed: true);
            }
            throw new TreasuryConflictException("تعذر صرف Payroll بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
        }
    }
}

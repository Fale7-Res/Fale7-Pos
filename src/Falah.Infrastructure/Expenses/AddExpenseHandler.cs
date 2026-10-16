namespace Falah.Infrastructure.Expenses;

using Falah.Application.Expenses;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.ControlCenter;
using Falah.Domain.Financial;
using Falah.Domain.Shifts;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Treasury;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Ported from addExpense in the reference app's lib/store.tsx. Unlike every Treasury command so
/// far, authorization branches on the payment source BEFORE anything else runs - see
/// AuthorizationPolicy.AuthorizeExpense's own doc comment for why a main-treasury expense has no
/// approval-token fallback at all, unlike a drawer expense.
/// </summary>
public sealed class AddExpenseHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public AddExpenseHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<AddExpenseResult> HandleAsync(AddExpenseRequest request, CancellationToken ct = default)
    {
        var expenseId = $"exp-{request.RequestId}";
        var existing = await _db.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == expenseId, ct);
        if (existing is not null) return ToResult(existing, wasReplayed: true);

        var paidFromDrawer = request.PaymentAccountId == TreasuryAccounts.Drawer;
        if (!TreasuryAccounts.Protected.Contains(request.PaymentAccountId))
            throw new DomainException("حساب الدفع غير صالح لتسجيل مصروف.");
        if (request.AmountPiastres <= 0) throw new DomainException("مبلغ المصروف يجب أن يكون أكبر من صفر.");
        if (string.IsNullOrWhiteSpace(request.Description)) throw new DomainException("بيان المصروف مطلوب.");
        if (string.IsNullOrWhiteSpace(request.Category)) throw new DomainException("فئة المصروف مطلوبة.");

        var createdByUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CreatedByUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(createdByUser);

        var shiftEntity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
            ?? throw new InvalidOperationException("الوردية غير موجودة.");
        if (shiftEntity.Status != nameof(ShiftStatus.Open)) throw new DomainException("يجب فتح وردية قبل تسجيل مصروف.");

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "expenses_add_drawer", request.Description);
        }
        // Only a drawer expense ever looks at `approval` - AuthorizeExpense ignores it entirely for
        // a main-treasury expense, by design (see its own doc comment).
        var authorization = AuthorizationPolicy.AuthorizeExpense(_approvalRegistry, actor, paidFromDrawer, approval);

        var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Read under a lock, inside the transaction, immediately before spending.
            // Outside it, concurrent commands each read the same balance, each decide it covers
            // them, and each write - which is how an account holding 30 paid out three separate
            // transfers of 20. See TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync.
            var accountMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, request.PaymentAccountId, ct);
            FinancialRules.AssertProtectedSourceBalance(accountMovements, request.PaymentAccountId, Money.FromPiastres(request.AmountPiastres));

            var idempotencyKey = $"expense-{expenseId}";
            var journal = new EfFinancialJournal(_db);
            if (!journal.TryStart(idempotencyKey, "expense"))
                throw new ExpenseConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            var now = DateTimeOffset.UtcNow;
            var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                $"tm-{Guid.NewGuid():N}", idempotencyKey, $"op-{Guid.NewGuid():N}", "expense_payment",
                request.PaymentAccountId, destinationAccountId: null, Money.FromPiastres(request.AmountPiastres),
                "expense", expenseId, businessDayId, request.ShiftId,
                request.Description, createdByUser.Name, authorization.Approver?.ApproverName, now);
            var movementEntity = OrderMapper.ToEntity(movement);
            _db.TreasuryMovements.Add(movementEntity);

            var expenseEntity = new ExpenseEntity
            {
                Id = expenseId,
                BusinessDayId = businessDayId,
                BusinessDate = BusinessClock.BusinessDate(now),
                Category = request.Category,
                AmountPiastres = request.AmountPiastres,
                Description = request.Description,
                CreatedByUserId = createdByUser.Id,
                ShiftId = request.ShiftId,
                PaidFromDrawer = paidFromDrawer,
                PaymentAccountId = request.PaymentAccountId,
                OperationId = movement.OperationId,
                IdempotencyKey = idempotencyKey,
                TreasuryMovementId = movementEntity.Id,
                ApprovedBy = authorization.Approver?.ApproverName,
                CreatedAtUtc = now,
            };
            _db.Expenses.Add(expenseEntity);

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", createdByUser.Id, createdByUser.Name, createdByUser.Role,
                "تسجيل مصروف", "cash", $"مصروف {request.Category}: {request.AmountPiastres} قرش من {request.PaymentAccountId} - {request.Description}",
                request.AmountPiastres >= ControlCenterRules.LargeExpenseThresholdPiastres ? "critical" : "normal", now,
                ShiftId: request.ShiftId, Reason: request.Description, AuthorizedBy: authorization.Approver?.ApproverName,
                OperationId: movement.OperationId, ReferenceId: expenseEntity.Id));

            journal.Complete(idempotencyKey);

            // The voucher for this expense. Printing it posts nothing: the money moved when the
            // expense was recorded above, and this row only reproduces it on paper.
            _db.PrintJobs.Add(Falah.Infrastructure.Printing.DocumentPrintJobs.Build(
                Falah.Domain.Printing.PrintDocumentKind.Expense, expenseEntity.Id, Falah.Domain.Printing.PrintRules.ExpenseLabel,
                createdByUser.Name, now));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ToResult(expenseEntity, wasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or ExpenseConflictException
                                   || Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
        {
            await transaction.RollbackAsync(ct);
            var replay = await _db.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == expenseId, ct);
            // The loser of a balance-lock race carries the same request id as the sibling that
            // won it, and that sibling may not have committed yet at this instant. Looking once
            // and giving up turns "somebody else is doing it" into an error.
            if (replay is null && Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
                replay = await Falah.Infrastructure.Treasury.BalanceLockContention.AwaitSiblingAsync(
                    async () => await _db.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == expenseId, ct), ct);
            if (replay is not null) return ToResult(replay, wasReplayed: true);
            // Losing the balance-lock race wrote nothing at all, so telling the operator to go and
            // review a half-finished movement would send them looking for something that is not there.
            if (Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
                throw new ExpenseConflictException(Falah.Infrastructure.Treasury.BalanceLockContention.RetryMessage);
            throw new ExpenseConflictException("تعذر تسجيل المصروف بسبب تعارض في مفتاح منع التكرار، ولا يوجد مصروف سابق لإعادته - يحتاج مراجعة يدوية.");
        }
    }

    private static AddExpenseResult ToResult(ExpenseEntity entity, bool wasReplayed) => new(
        entity.Id, entity.Category, entity.AmountPiastres, entity.PaymentAccountId, entity.PaidFromDrawer,
        entity.TreasuryMovementId, wasReplayed);
}

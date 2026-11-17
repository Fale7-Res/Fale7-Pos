namespace Falah.Infrastructure.Expenses;

using Falah.Application.Expenses;
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
/// AUD-04: an expense recorded by mistake (a wrong amount, a duplicate, the wrong account) is corrected by a linked
/// reversal, never by deleting it.
///
/// - The reversal is an expense row of the same category, shift, account and business day with a NEGATIVE amount,
///   so every figure that sums expenses (the drawer's expected cash, the shift and day totals, the expense report, the
///   dashboard) nets the two without a second rule anywhere. Its id is derived from the original
///   (<c>exp-rev-{id}</c>), so an expense can be reversed once, and a retried request replays that one reversal.
/// - What happened to the money is stated, not assumed (ExpenseCorrectionRules): a posting error ("not_paid" - it
///   never left: an <c>expense_correction</c> credit) or a real refund ("cash_returned" - it left and came back: an
///   <c>expense_refund</c> credit). Only these two credit the account, because only in these two is the money
///   physically there; crediting the drawer for money that left and stayed gone would fabricate cash that the count
///   then shows as a shortage. Neither is a sale or a customer collection (MoneyFlowRules).
/// - A part of the expense can be reversed (5000 typed, 500 paid: 4500), so a wrong amount is fixed in one step and
///   the drawer is never credited for the 500 that really left.
/// - Only while the original's shift is still open. A closed shift's figures and its frozen closing statement never
///   move; how a mistake found after the close is corrected is the owner's and the accountant's decision (recorded in
///   docs/ASSESSMENT.md), so it is refused here rather than guessed.
/// - Authority: a drawer expense needs shifts_manage, or a manager's approval issued for it; a main-treasury/bank
///   expense needs expenses_main_treasury itself, with no approval fallback - the same owner-only rule that governs
///   recording one (AuthorizationPolicy.AuthorizeExpense).
/// </summary>
public sealed class ReverseExpenseHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public ReverseExpenseHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public static string ReversalIdFor(string expenseId) => $"exp-rev-{expenseId}";

    public async Task<ReverseExpenseResult> HandleAsync(ReverseExpenseRequest request, CancellationToken ct = default)
    {
        var idempotencyKey = $"expense-reversal-{request.RequestId}";
        var replay = await FindByKeyAsync(idempotencyKey, ct);
        if (replay is not null) return await ReplayAsync(replay, ct);

        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب إلغاء المصروف مطلوب.");
        var movementType = ExpenseCorrectionRules.MovementTypeFor(request.CashDisposition);
        var original = await _db.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.ExpenseId, ct)
            ?? throw new InvalidOperationException("المصروف غير موجود.");
        if (original.AmountPiastres <= 0 || original.Id.StartsWith("exp-rev-", StringComparison.Ordinal))
            throw new DomainException("هذا قيد إلغاء ولا يمكن إلغاؤه.");
        var amount = ExpenseCorrectionRules.ReversedAmount(Money.FromPiastres(original.AmountPiastres), request.ReversedAmountPiastres);
        var dispositionLabel = request.CashDisposition == ExpenseCorrectionRules.NotPaid ? "خطأ تسجيل - لم يُصرف" : "صُرف ثم عاد نقدًا";

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        string? approvedBy;
        if (original.PaidFromDrawer)
        {
            ManagerApproval? approval = null;
            if (!string.IsNullOrEmpty(request.ManagerApprovalId))
            {
                var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                    ?? throw new DomainException("المعتمد غير موجود.");
                approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                    approverUser.Role, DateTimeOffset.UtcNow, "shifts_manage", request.Reason);
            }
            var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "shifts_manage", approval, requireApproval: true);
            approvedBy = authorization.Approver?.ApproverName ?? actorUser.Name;
        }
        else
        {
            if (!AuthorizationPolicy.HasPermission(actor, "expenses_main_treasury"))
                throw new DomainException("إلغاء مصروف من الخزينة الرئيسية أو البنك للمالك فقط.");
            approvedBy = actorUser.Name;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await FinancialAdmission.AcquireAsync(_db, ct);
            var admissionReplay = await FindByKeyAsync(idempotencyKey, ct);
            if (admissionReplay is not null) return await ReplayAsync(admissionReplay, ct);

            var reversalId = ReversalIdFor(original.Id);
            if (await _db.Expenses.AsNoTracking().AnyAsync(e => e.Id == reversalId, ct))
                throw new DomainException("تم إلغاء هذا المصروف بالفعل.");

            var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == original.ShiftId, ct);
            if (shift?.Status != nameof(ShiftStatus.Open))
                throw new DomainException("لا يمكن إلغاء مصروف بعد إغلاق ورديته؛ الوردية المغلقة لا تتغير. سجّل التصحيح بقرار من المدير والمحاسب.");
            var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);

            var journal = new EfFinancialJournal(_db);
            if (!journal.TryStart(idempotencyKey, movementType))
                throw new ExpenseConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            var now = DateTimeOffset.UtcNow;
            var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                $"tm-{Guid.NewGuid():N}", idempotencyKey, $"op-{Guid.NewGuid():N}", movementType,
                sourceAccountId: null, destinationAccountId: original.PaymentAccountId, amount,
                "expense", original.Id, businessDayId, original.ShiftId,
                $"إلغاء مصروف ({dispositionLabel}): {original.Description} - {request.Reason.Trim()}", actorUser.Name, approvedBy, now);
            var movementEntity = OrderMapper.ToEntity(movement);
            _db.TreasuryMovements.Add(movementEntity);

            var reversal = new ExpenseEntity
            {
                Id = reversalId,
                BusinessDayId = businessDayId,
                // The original's own date, so the two net in every date-filtered report, not across two days.
                BusinessDate = original.BusinessDate,
                Category = original.Category,
                AmountPiastres = -amount.Piastres,
                Description = $"إلغاء مصروف ({original.Description}) [{dispositionLabel}] - {request.Reason.Trim()}",
                CreatedByUserId = actorUser.Id,
                ShiftId = original.ShiftId,
                PaidFromDrawer = original.PaidFromDrawer,
                PaymentAccountId = original.PaymentAccountId,
                OperationId = movement.OperationId,
                IdempotencyKey = idempotencyKey,
                TreasuryMovementId = movementEntity.Id,
                ApprovedBy = approvedBy,
                CreatedAtUtc = now,
            };
            _db.Expenses.Add(reversal);

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                "إلغاء مصروف", "cash",
                $"إلغاء المصروف {original.Id} ({original.Category}) بمبلغ {amount.Piastres} من {original.AmountPiastres} قرش إلى {original.PaymentAccountId} [{dispositionLabel}] - {request.Reason.Trim()}",
                "critical", now, ShiftId: original.ShiftId, Reason: request.Reason.Trim(), AuthorizedBy: approvedBy,
                OperationId: movement.OperationId, ReferenceId: original.Id,
                PreviousValue: original.AmountPiastres.ToString(), NewValue: (original.AmountPiastres - amount.Piastres).ToString()));

            journal.Complete(idempotencyKey);
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ToResult(reversal, wasReplayed: false, movementType);
        }
        catch (Exception ex) when (ex is DbUpdateException or ExpenseConflictException)
        {
            await transaction.RollbackAsync(ct);
            var after = await FindByKeyAsync(idempotencyKey, ct);
            if (after is not null) return await ReplayAsync(after, ct);
            if (await _db.Expenses.AsNoTracking().AnyAsync(e => e.Id == ReversalIdFor(request.ExpenseId), ct))
                throw new DomainException("تم إلغاء هذا المصروف بالفعل.");
            throw new ExpenseConflictException("تعذر إلغاء المصروف بسبب تعارض - يحتاج مراجعة يدوية.");
        }
    }

    private Task<ExpenseEntity?> FindByKeyAsync(string idempotencyKey, CancellationToken ct) =>
        _db.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey && e.Id.StartsWith("exp-rev-"), ct);

    private async Task<ReverseExpenseResult> ReplayAsync(ExpenseEntity reversal, CancellationToken ct)
    {
        var type = await _db.TreasuryMovements.AsNoTracking()
            .Where(m => m.Id == reversal.TreasuryMovementId).Select(m => m.Type).FirstOrDefaultAsync(ct);
        return ToResult(reversal, wasReplayed: true, type);
    }

    private static ReverseExpenseResult ToResult(ExpenseEntity reversal, bool wasReplayed, string? movementType) => new(
        reversal.Id, reversal.Id["exp-rev-".Length..], -reversal.AmountPiastres, reversal.PaymentAccountId,
        reversal.TreasuryMovementId, reversal.ApprovedBy, wasReplayed,
        movementType == ExpenseCorrectionRules.PostingErrorMovementType ? ExpenseCorrectionRules.NotPaid
            : movementType == ExpenseCorrectionRules.CashReturnedMovementType ? ExpenseCorrectionRules.CashReturned : null);
}

namespace Falah.Application.Expenses;

/// <summary>Ported from addExpense in the reference app's lib/store.tsx. PaymentAccountId is one of
/// tre-drawer/tre-main/tre-bank/tre-instapay (never tre-wallet - that's a customer-facing electronic
/// payment rail, never something a restaurant pays its own expenses from). RequestId is minted once
/// per submission and must be reused unchanged by a retry - ExpenseId itself is derived from it
/// ("exp-{RequestId}"), matching the reference app's own id shape exactly.</summary>
public sealed record AddExpenseRequest(
    string RequestId, string ShiftId, string Category, long AmountPiastres, string Description,
    string PaymentAccountId, string CreatedByUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record AddExpenseResult(
    string ExpenseId, string Category, long AmountPiastres, string PaymentAccountId, bool PaidFromDrawer,
    string? TreasuryMovementId, bool WasReplayed);

public sealed record ExpenseDto(
    string ExpenseId, string Category, long AmountPiastres, string Description, string ShiftId,
    bool PaidFromDrawer, string PaymentAccountId, string CreatedByUserId, string BusinessDate, DateTimeOffset CreatedAtUtc);

/// <summary>AUD-04: cancel an expense recorded by mistake, while its shift is still open. Nothing is deleted: a linked
/// reversal (negative) is recorded and the account it was charged to is credited back.
///
/// CashDisposition says what happened to the money, and is required, because it is what makes crediting the drawer
/// true: "not_paid" (a posting error - the money never left) or "cash_returned" (it left and has physically come
/// back). Money that left and stays gone is not reversible: the expense is true. ReversedAmountPiastres reverses part
/// of it (5000 typed, 500 paid: 4500); null = all of it.</summary>
public sealed record ReverseExpenseRequest(
    string RequestId, string ExpenseId, string Reason, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId,
    string? CashDisposition = null, long? ReversedAmountPiastres = null);

public sealed record ReverseExpenseResult(
    string ReversalExpenseId, string OriginalExpenseId, long AmountPiastres, string PaymentAccountId,
    string? TreasuryMovementId, string? ApprovedBy, bool WasReplayed, string? CashDisposition = null);

public sealed class ExpenseConflictException : Exception
{
    public ExpenseConflictException(string message) : base(message) { }
}

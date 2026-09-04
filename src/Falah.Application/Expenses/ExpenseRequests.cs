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

public sealed class ExpenseConflictException : Exception
{
    public ExpenseConflictException(string message) : base(message) { }
}

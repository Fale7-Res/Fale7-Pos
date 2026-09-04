using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Approvals;
using Falah.Application.Expenses;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Drawer expense entry only - a routine cashier action (petty cash: supplies, transport, cleaning,
/// urgent purchases). Main-treasury expenses are owner/manager-only money with no approval-token
/// fallback at all (see AuthorizationPolicy.AuthorizeExpense) and belong in a future Management
/// screen, not here. If the logged-in cashier doesn't hold expenses_add_drawer directly, a manager
/// PIN is resolved and exchanged for an approval first - same shape as the shift-close discrepancy
/// flow in CashierViewModel.ShiftClose.cs.
/// </summary>
public partial class CashierViewModel
{
    [ObservableProperty] private bool _isAddExpenseOpen;
    [ObservableProperty] private string _expenseCategory = "مستلزمات";
    [ObservableProperty] private string _expenseAmountText = "0";
    [ObservableProperty] private string _expenseDescription = "";
    [ObservableProperty] private string _expenseManagerPin = "";
    [ObservableProperty] private string? _expenseError;
    [ObservableProperty] private bool _isSubmittingExpense;

    public bool ExpenseNeedsApproval => !_session.HasPermission("expenses_add_drawer");

    [RelayCommand]
    private void OpenAddExpense()
    {
        ExpenseCategory = "مستلزمات";
        ExpenseAmountText = "0";
        ExpenseDescription = "";
        ExpenseManagerPin = "";
        ExpenseError = null;
        IsAddExpenseOpen = true;
        OnPropertyChanged(nameof(ExpenseNeedsApproval));
    }

    [RelayCommand]
    private void SetExpenseCategory(string category) => ExpenseCategory = category;

    [RelayCommand]
    private void CancelAddExpense() => IsAddExpenseOpen = false;

    [RelayCommand]
    private async Task SubmitExpenseAsync()
    {
        if (!decimal.TryParse(ExpenseAmountText, out var amountEgp) || amountEgp <= 0)
        {
            ExpenseError = "أدخل مبلغًا صحيحًا أكبر من صفر.";
            return;
        }
        if (string.IsNullOrWhiteSpace(ExpenseDescription))
        {
            ExpenseError = "أدخل بيان المصروف.";
            return;
        }

        IsSubmittingExpense = true;
        ExpenseError = null;
        try
        {
            string? approvalId = null;
            string? approverUserId = null;

            if (ExpenseNeedsApproval)
            {
                if (ExpenseManagerPin.Length < 4)
                {
                    ExpenseError = "أدخل رمز المدير المخوّل باعتماد هذا المصروف.";
                    return;
                }
                var managerLogin = await _session.Api.LoginByPinAsync(ExpenseManagerPin);
                if (!managerLogin.Success || managerLogin.Value is null)
                {
                    ExpenseError = "رمز المدير غير صحيح.";
                    return;
                }
                var approval = await _session.Api.IssueManagerApprovalAsync(
                    new IssueManagerApprovalRequest(managerLogin.Value.Id, "expenses_add_drawer", ExpenseDescription));
                if (!approval.Success || approval.Value is null)
                {
                    ExpenseError = approval.Error ?? "هذا المستخدم غير مخول باعتماد هذا المصروف.";
                    return;
                }
                approvalId = approval.Value.ApprovalId;
                approverUserId = managerLogin.Value.Id;
            }

            var request = new AddExpenseRequest(
                RequestId: $"exp-{Guid.NewGuid():N}", ShiftId: _session.ShiftId!, Category: ExpenseCategory,
                AmountPiastres: (long)Math.Round(amountEgp * 100m), Description: ExpenseDescription,
                PaymentAccountId: "tre-drawer", CreatedByUserId: _session.CurrentUser!.Id,
                ManagerApprovalId: approvalId, ManagerApproverUserId: approverUserId);
            var result = await _session.Api.AddExpenseAsync(request);
            if (!result.Success || result.Value is null)
            {
                ExpenseError = result.Error ?? "تعذر تسجيل المصروف.";
                return;
            }

            IsAddExpenseOpen = false;
        }
        finally
        {
            IsSubmittingExpense = false;
        }
    }
}

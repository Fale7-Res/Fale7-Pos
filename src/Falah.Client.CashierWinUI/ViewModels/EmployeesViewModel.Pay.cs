using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Employees;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// The standalone "PAY EMPLOYEE" flow (final pre-go-live business rule) - deliberately its OWN
/// dialog, not a sixth kind folded into the advance/withdrawal/daily-wage modal above: a payment is
/// sourced from the restaurant's own accounts (Main/Bank/InstaPay), needs no open shift, and must
/// never read to an owner as the same action as an advance. "Payroll Run must no longer be the only
/// way to pay employees" - this is that other way, reachable directly from the roster, any employee,
/// any time, any amount up to their current balance.
/// </summary>
public partial class EmployeesViewModel
{
    [ObservableProperty] private EmployeeRow? _payEmployee;
    [ObservableProperty] private string _payAmountText = "";
    [ObservableProperty] private string _payAccount = "tre-main";
    [ObservableProperty] private string _payReason = "";
    [ObservableProperty] private string _payManagerPin = "";
    [ObservableProperty] private string? _payError;
    public bool IsPayOpen => PayEmployee is not null;

    /// <summary>
    /// A balance of zero or less means the employee has already been paid everything they have
    /// earned - PayEmployeeHandler's own AssertPaymentWithinBalance would reject any amount, so the
    /// dialog says so up front instead of prefilling the box with a negative figure and only failing
    /// at submit. (A negative balance is normal here: withdrawals and advances are debits and may
    /// legitimately run ahead of earnings.)
    /// </summary>
    public bool PayHasNothingDue => PayEmployee is { BalancePiastres: <= 0 };

    partial void OnPayEmployeeChanged(EmployeeRow? value)
    {
        OnPropertyChanged(nameof(IsPayOpen));
        OnPropertyChanged(nameof(PayHasNothingDue));
    }

    public sealed record PaymentAccountOption(string Id, string Label);
    // Exactly the accounts PayEmployeeHandler accepts - the same set PayPayrollHandler already
    // validates. No drawer: a drawer-sourced payment against balance already exists as "withdrawal".
    public IReadOnlyList<PaymentAccountOption> PayAccounts { get; } = new[]
    {
        new PaymentAccountOption("tre-main", "الخزينة الرئيسية"),
        new PaymentAccountOption("tre-bank", "البنك"),
        new PaymentAccountOption("tre-instapay", "إنستاباي"),
    };

    public bool PayNeedsApproval => !CanManageLedger;
    private string? _payRequestId;
    private readonly RetainedRequest<PayEmployeeRequest> _payRequest = new("employees.pay");

    [RelayCommand]
    private void OpenPay(EmployeeRow row)
    {
        if (!CanEditAction) return;
        PayError = null;
        PayAmountText = row.BalancePiastres > 0 ? CashInput.Format(row.BalancePiastres) : "";
        PayAccount = "tre-main";
        PayReason = "";
        PayManagerPin = "";
        _payRequestId = $"pay-{Guid.NewGuid():N}";
        PayEmployee = row;
        OnPropertyChanged(nameof(PayNeedsApproval));
    }

    [RelayCommand]
    private void ClosePay() { if (CanEditAction) PayEmployee = null; }

    [RelayCommand]
    private void SetPayAccount(string accountId) { if (CanEditAction) PayAccount = accountId; }

    [RelayCommand]
    private async Task SubmitPayAsync()
    {
        if (IsBusy) return;
        if (_payRequest.Value is { } pending) { await RunBusyAsync(() => CompletePayAsync(pending)); return; }
        if (PayEmployee is null) return;

        if (!CashInput.TryParse(PayAmountText, out var amount) || amount <= 0)
        {
            PayError = "أدخل المبلغ بشكل صحيح.";
            return;
        }
        if (amount > PayEmployee.BalancePiastres)
        {
            // Caught here for a fast, friendly message - PayEmployeeHandler enforces the same cap
            // server-side regardless, so a stale client-side balance can never let an overpayment
            // through even if this check is skipped or the number shown here is out of date.
            PayError = $"لا يمكن دفع أكثر من الرصيد الحالي المستحق ({CashInput.Format(PayEmployee.BalancePiastres)} ج).";
            return;
        }

        await RunBusyAsync(async () =>
        {
            PayError = null;
            var (approvalId, approverId, ok) = await ResolveApprovalAsync(
                PayNeedsApproval, PayManagerPin, "employees_ledger", PayReason, e => PayError = e);
            if (!ok) return;

            await CompletePayAsync(new PayEmployeeRequest(
                _payRequestId!, PayEmployee.Employee.Id, amount, PayAccount, _session.CurrentUser!.Id,
                string.IsNullOrWhiteSpace(PayReason) ? null : PayReason, approvalId, approverId));
        });
    }

    private async Task CompletePayAsync(PayEmployeeRequest request)
    {
        var result = await _payRequest.SendAsync(request, r => _session.Api.PayEmployeeAsync(r));
        if (!result.Success || result.Value is null) { PayError = _payRequest.FailureMessage(result.Error); return; }
        PayError = null;
        ActionNote = $"تم الدفع. الرصيد المتبقي: {CashInput.Format(result.Value.BalanceAfterPiastres)} ج";
        PayEmployee = null;
        _payRequestId = null;
        await ReloadAsync();
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Employees;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// The employee roster with each person's running ledger balance, and the one financial action a
/// manager performs against it from here: a cash-out (advance or withdrawal) from the drawer.
///
/// A withdrawal is bounded server-side by the employee's own balance
/// (AssertWithdrawalWithinBalance) while an advance is not - that asymmetry is real and is surfaced
/// rather than smoothed over, because it is the difference between paying out money the employee
/// has already earned and lending money against future work. Both require an open shift, since the
/// cash physically leaves the drawer.
/// </summary>
public partial class EmployeesViewModel : ObservableObject
{
    private readonly AppSession _session;

    public EmployeesViewModel(AppSession session)
    {
        _session = session;
        Editor = new EmployeeEditorViewModel(session);
        // A saved record changes the roster and can change who appears at all (an employee moved to
        // inactive), so the list is re-read from the server rather than patched locally.
        Editor.Saved += () => _ = ReloadAsync();
    }

    /// <summary>Roster authoring - creating a person and correcting their record - gated on
    /// `employees_manage`, which is a different permission from the `employees_ledger` one that
    /// governs the money actions on this same screen.</summary>
    public EmployeeEditorViewModel Editor { get; }

    public bool CanOpenEditor => Editor.CanManage;

    [RelayCommand]
    private void OpenEditor() => Editor.Open(_all.Select(r => r.Employee));

    public sealed partial class EmployeeRow : ObservableObject
    {
        public required EmployeeDto Employee { get; init; }
        [ObservableProperty] private long _balancePiastres;
        public string BalanceDisplay => CashInput.Format(BalancePiastres) + " ج";
        partial void OnBalancePiastresChanged(long value) => OnPropertyChanged(nameof(BalanceDisplay));
    }

    public ObservableCollection<EmployeeRow> Employees { get; } = new();
    public ObservableCollection<EmployeeLedgerEntryDto> Ledger { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _actionNote;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _showInactive;

    private readonly List<EmployeeRow> _all = new();

    public bool HasNoEmployees => !IsLoading && LoadError is null && Employees.Count == 0;
    public bool CanManageLedger => _session.HasPermission("employees_ledger");

    partial void OnShowInactiveChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        Employees.Clear();
        foreach (var r in _all.Where(r => ShowInactive || r.Employee.IsActive)) Employees.Add(r);
        OnPropertyChanged(nameof(HasNoEmployees));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!CanEditAction) return;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        try
        {
            var employees = await _session.Api.GetEmployeesAsync();
            if (!employees.Success || employees.Value is null)
            {
                LoadError = employees.Error ?? "تعذر تحميل الموظفين.";
                return;
            }
            _all.Clear();
            Employees.Clear();
            foreach (var e in employees.Value) _all.Add(new EmployeeRow { Employee = e });

            foreach (var row in _all)
            {
                var balance = await _session.Api.GetEmployeeBalanceAsync(row.Employee.Id);
                if (!balance.Success || balance.Value is null) { LoadError = balance.Error ?? "تعذر تحميل رصيد الموظف."; return; }
                if (balance.Success && balance.Value is not null) row.BalancePiastres = balance.Value.BalancePiastres;
            }
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoEmployees));
        }
    }

    // ---------------- Ledger drill-down ----------------

    [ObservableProperty] private EmployeeRow? _ledgerEmployee;
    [ObservableProperty] private bool _isLoadingLedger;
    [ObservableProperty] private string? _ledgerError;
    private int _ledgerVersion;
    public bool IsLedgerOpen => LedgerEmployee is not null;
    public bool HasNoLedgerEntries => IsLedgerOpen && !IsLoadingLedger && LedgerError is null && Ledger.Count == 0;
    partial void OnLedgerEmployeeChanged(EmployeeRow? value) => OnPropertyChanged(nameof(IsLedgerOpen));

    [RelayCommand]
    private async Task OpenLedgerAsync(EmployeeRow row)
    {
        if (!CanEditAction) return;
        LedgerEmployee = row;
        await ReloadLedgerAsync(row);
    }

    private async Task ReloadLedgerAsync(EmployeeRow row)
    {
        var version = ++_ledgerVersion;
        Ledger.Clear();
        LedgerError = null;
        IsLoadingLedger = true;
        OnPropertyChanged(nameof(HasNoLedgerEntries));
        try
        {
            var entries = await _session.Api.GetEmployeeLedgerAsync(row.Employee.Id);
            if (version != _ledgerVersion || LedgerEmployee != row) return;
            if (!entries.Success || entries.Value is null)
            { LedgerError = entries.Error ?? "تعذر تحميل كشف حساب الموظف."; return; }
            foreach (var e in entries.Value.OrderByDescending(e => e.CreatedAtUtc)) Ledger.Add(e);
        }
        finally
        {
            if (version == _ledgerVersion)
            {
                IsLoadingLedger = false;
                OnPropertyChanged(nameof(HasNoLedgerEntries));
            }
        }
    }

    [RelayCommand]
    private void CloseLedger()
    {
        ++_ledgerVersion;
        LedgerEmployee = null;
        IsLoadingLedger = false;
        Ledger.Clear();
        LedgerError = null;
        OnPropertyChanged(nameof(HasNoLedgerEntries));
    }

    [RelayCommand]
    private async Task RetryLedgerAsync()
    {
        if (CanEditAction && !IsLoadingLedger && LedgerEmployee is { } row) await ReloadLedgerAsync(row);
    }

    // ---------------- Cash out (advance / withdrawal) ----------------

    [ObservableProperty] private EmployeeRow? _cashOutEmployee;
    [ObservableProperty] private string _cashOutKind = "advance";
    [ObservableProperty] private string _cashOutAmountText = "";
    [ObservableProperty] private string _cashOutReason = "";
    [ObservableProperty] private string _cashOutManagerPin = "";
    [ObservableProperty] private string? _cashOutError;
    public bool IsCashOutOpen => CashOutEmployee is not null;
    partial void OnCashOutEmployeeChanged(EmployeeRow? value) => OnPropertyChanged(nameof(IsCashOutOpen));

    private string? _cashOutRequestId;
    private readonly RetainedRequest<RecordEmployeeCashOutRequest> _cashOutRequest = new("employees.cash-out");
    public bool CanEditAction => !IsBusy && !_cashOutRequest.HasPending;
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEditAction));
        _session.SetManagementOperationPending(this, IsBusy || _cashOutRequest.HasPending);
    }
    public bool CashOutNeedsApproval => !CanManageLedger;

    public bool IsWithdrawal => CashOutKind == "withdrawal";
    partial void OnCashOutKindChanged(string value) => OnPropertyChanged(nameof(IsWithdrawal));

    /// <summary>Cash physically leaves the drawer, so the server requires an open shift - saying so
    /// up front beats a rejection after the amount has been typed.</summary>
    public bool HasOpenShift => _session.ShiftId is not null;

    [RelayCommand]
    private void OpenCashOut(EmployeeRow row)
    {
        if (!CanEditAction) return;
        CashOutError = null;
        CashOutKind = "advance";
        CashOutAmountText = "";
        CashOutReason = "";
        CashOutManagerPin = "";
        _cashOutRequestId = $"eco-{Guid.NewGuid():N}";
        CashOutEmployee = row;
        OnPropertyChanged(nameof(CashOutNeedsApproval));
        OnPropertyChanged(nameof(HasOpenShift));
    }

    [RelayCommand]
    private void CloseCashOut() { if (CanEditAction) CashOutEmployee = null; }

    [RelayCommand]
    private void SetCashOutKind(string kind) { if (CanEditAction) CashOutKind = kind; }

    [RelayCommand]
    private async Task SubmitCashOutAsync()
    {
        if (IsBusy) return;
        if (_cashOutRequest.Value is { } pending)
        {
            IsBusy = true;
            try { await CompleteCashOutAsync(pending); }
            finally { IsBusy = false; }
            return;
        }
        if (CashOutEmployee is null) return;
        if (!HasOpenShift)
        {
            CashOutError = "لا توجد وردية مفتوحة - لا يمكن الصرف من الدرج.";
            return;
        }
        if (!CashInput.TryParse(CashOutAmountText, out var amount) || amount <= 0)
        {
            CashOutError = "أدخل المبلغ بشكل صحيح.";
            return;
        }
        if (string.IsNullOrWhiteSpace(CashOutReason))
        {
            CashOutError = "السبب إلزامي.";
            return;
        }

        IsBusy = true;
        CashOutError = null;
        try
        {
            var (approvalId, approverId, ok) = await ResolveApprovalAsync(
                CashOutNeedsApproval, CashOutManagerPin, "employees_ledger", CashOutReason, e => CashOutError = e);
            if (!ok) return;

            await CompleteCashOutAsync(new RecordEmployeeCashOutRequest(
                _cashOutRequestId!, CashOutEmployee.Employee.Id, CashOutKind, amount, CashOutReason,
                _session.ShiftId!, _session.CurrentUser!.Id, approvalId, approverId));
        }
        finally { IsBusy = false; }
    }

    private async Task CompleteCashOutAsync(RecordEmployeeCashOutRequest request)
    {
            CashOutError = null;
            var result = await _cashOutRequest.SendAsync(request, r => _session.Api.EmployeeCashOutAsync(r));
            if (!result.Success || result.Value is null)
            {
                CashOutError = _cashOutRequest.FailureMessage(result.Error);
                return;
            }
            ActionNote = $"تم الصرف. الرصيد بعد العملية: {CashInput.Format(result.Value.BalanceAfterPiastres)} ج";
            CashOutEmployee = null;
            _cashOutRequestId = null;
            await ReloadAsync();
    }

    private async Task<(string? ApprovalId, string? ApproverUserId, bool Ok)> ResolveApprovalAsync(
        bool needsApproval, string managerPin, string permission, string reason, Action<string> onError)
    {
        if (!needsApproval) return (null, null, true);
        if (managerPin.Length < 4)
        {
            onError("أدخل رمز المدير المخوّل باعتماد هذا الإجراء.");
            return (null, null, false);
        }
        var login = await _session.Api.LoginByPinAsync(managerPin);
        if (!login.Success || login.Value is null)
        {
            onError(login.Error ?? "تعذر التحقق من رمز المدير.");
            return (null, null, false);
        }
        var approval = await _session.Api.IssueManagerApprovalAsync(
            new Falah.Application.Approvals.IssueManagerApprovalRequest(login.Value.Id, permission, reason));
        if (!approval.Success || approval.Value is null)
        {
            onError(approval.Error ?? "هذا المستخدم غير مخول باعتماد هذا الإجراء.");
            return (null, null, false);
        }
        return (approval.Value.ApprovalId, login.Value.Id, true);
    }
}

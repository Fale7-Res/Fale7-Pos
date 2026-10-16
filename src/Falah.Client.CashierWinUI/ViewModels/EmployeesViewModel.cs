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
    private long _overtimeRatePiastres;

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
            var settings = await _session.Api.GetSettingsAsync();
            if (settings.Success && settings.Value is not null) _overtimeRatePiastres = settings.Value.OvertimeHourlyRatePiastres;

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

    // ---------------- Quick actions: daily wage / overtime / extra shift / advance / withdrawal ----------------
    // One shared modal for all five, matching the reference's handleOpenAction - each of the five
    // row buttons opens it pre-set to its own kind rather than the modal offering an in-place switch.

    [ObservableProperty] private EmployeeRow? _cashOutEmployee;
    [ObservableProperty] private string _cashOutKind = "advance";
    [ObservableProperty] private string _cashOutAmountText = "";
    [ObservableProperty] private int _overtimeHours = 2;
    [ObservableProperty] private string _cashOutReason = "";
    [ObservableProperty] private string _cashOutManagerPin = "";
    [ObservableProperty] private string? _cashOutError;
    public bool IsCashOutOpen => CashOutEmployee is not null;
    partial void OnCashOutEmployeeChanged(EmployeeRow? value) => OnPropertyChanged(nameof(IsCashOutOpen));

    private string? _cashOutRequestId;
    private readonly RetainedRequest<RecordEmployeeCashOutRequest> _cashOutRequest = new("employees.cash-out");
    private readonly RetainedRequest<RecordDailyWageRequest> _dailyWageRequest = new("employees.daily-wage");
    private readonly RetainedRequest<RecordOvertimeRequest> _overtimeRequest = new("employees.overtime");
    private readonly RetainedRequest<RecordExtraShiftRequest> _extraShiftRequest = new("employees.extra-shift");
    public bool CanEditAction => !IsBusy && !_cashOutRequest.HasPending && !_dailyWageRequest.HasPending
        && !_overtimeRequest.HasPending && !_extraShiftRequest.HasPending && !_payRequest.HasPending;
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEditAction));
        _session.SetManagementOperationPending(this, IsBusy || !CanEditAction);
    }
    public bool CashOutNeedsApproval => !CanManageLedger;

    public bool IsWithdrawal => CashOutKind == "withdrawal";
    public bool IsAdvance => CashOutKind == "advance";
    public bool IsDailyWage => CashOutKind == "daily_wage";
    public bool IsOvertime => CashOutKind == "overtime";
    public bool IsExtraShift => CashOutKind == "extra_shift";
    /// <summary>Only advance/withdrawal move cash out of the drawer; a daily-wage/overtime/extra-shift
    /// entry is a ledger posting of money owed, settled later - the reference imposes the same
    /// no-open-shift block only on its two cash actions (DailyWagesPanel.tsx's handleConfirmAction).</summary>
    public bool RequiresOpenShift => IsAdvance || IsWithdrawal;
    public bool ShowNoShiftAlert => RequiresOpenShift && !HasOpenShift;
    public bool ReasonIsRequired => IsAdvance || IsWithdrawal;
    public bool ShowAmountInput => IsDailyWage || IsAdvance || IsWithdrawal;
    public string ReasonHeader => ReasonIsRequired ? "السبب (إلزامي)" : "ملاحظة (اختياري)";
    public string ActionTitle => CashOutKind switch
    {
        "daily_wage" => "تسجيل يومية عمل مستحقة (+)",
        "overtime" => "تسجيل ساعات إضافية (+)",
        "extra_shift" => "تسجيل تطبيق وردية (+)",
        "withdrawal" => "سحب نقدي من الرصيد (-)",
        _ => "صرف سلفة نقدية (-)",
    };
    partial void OnCashOutKindChanged(string value)
    {
        OnPropertyChanged(nameof(IsWithdrawal));
        OnPropertyChanged(nameof(IsAdvance));
        OnPropertyChanged(nameof(IsDailyWage));
        OnPropertyChanged(nameof(IsOvertime));
        OnPropertyChanged(nameof(IsExtraShift));
        OnPropertyChanged(nameof(RequiresOpenShift));
        OnPropertyChanged(nameof(ShowNoShiftAlert));
        OnPropertyChanged(nameof(ReasonIsRequired));
        OnPropertyChanged(nameof(ShowAmountInput));
        OnPropertyChanged(nameof(ReasonHeader));
        OnPropertyChanged(nameof(ActionTitle));
    }
    partial void OnOvertimeHoursChanged(int value) => OnPropertyChanged(nameof(OvertimePreviewAmount));

    /// <summary>Cash physically leaves the drawer, so the server requires an open shift for
    /// advance/withdrawal - saying so up front beats a rejection after the amount has been typed.</summary>
    public bool HasOpenShift => _session.ShiftId is not null;

    private long ShiftValueOrDailyWage(EmployeeDto employee) =>
        employee.ShiftValuePiastres > 0 ? employee.ShiftValuePiastres : employee.DailyWagePiastres;

    /// <summary>Overtime's total is always rate x hours server-side (RecordOvertimeHandler) - shown
    /// here for the manager to see before confirming, not sent back as an override.</summary>
    public string OvertimePreviewAmount => CashInput.Format(_overtimeRatePiastres * OvertimeHours) + " ج";
    public string ExtraShiftPreviewAmount => CashOutEmployee is null ? "" : CashInput.Format(ShiftValueOrDailyWage(CashOutEmployee.Employee)) + " ج";

    private void OpenAction(EmployeeRow row, string kind)
    {
        if (!CanEditAction) return;
        CashOutError = null;
        CashOutKind = kind;
        CashOutAmountText = kind switch
        {
            "daily_wage" => CashInput.Format(row.Employee.DailyWagePiastres),
            "advance" => "100",
            "withdrawal" => "200",
            _ => "",
        };
        OvertimeHours = 2;
        CashOutReason = "";
        CashOutManagerPin = "";
        _cashOutRequestId = $"eco-{Guid.NewGuid():N}";
        CashOutEmployee = row;
        OnPropertyChanged(nameof(CashOutNeedsApproval));
        OnPropertyChanged(nameof(HasOpenShift));
        OnPropertyChanged(nameof(ShowNoShiftAlert));
        OnPropertyChanged(nameof(OvertimePreviewAmount));
        OnPropertyChanged(nameof(ExtraShiftPreviewAmount));
    }

    [RelayCommand] private void OpenDailyWage(EmployeeRow row) => OpenAction(row, "daily_wage");
    [RelayCommand] private void OpenOvertime(EmployeeRow row) => OpenAction(row, "overtime");
    [RelayCommand] private void OpenExtraShift(EmployeeRow row) => OpenAction(row, "extra_shift");
    [RelayCommand] private void OpenAdvance(EmployeeRow row) => OpenAction(row, "advance");
    [RelayCommand] private void OpenWithdrawal(EmployeeRow row) => OpenAction(row, "withdrawal");

    [RelayCommand]
    private void CloseCashOut() { if (CanEditAction) CashOutEmployee = null; }

    [RelayCommand]
    private void SetOvertimeHours(string hours) { if (CanEditAction && int.TryParse(hours, out var h)) OvertimeHours = h; }

    [RelayCommand]
    private async Task SubmitCashOutAsync()
    {
        if (IsBusy) return;
        if (_cashOutRequest.Value is { } pendingCashOut) { await RunBusyAsync(() => CompleteCashOutAsync(pendingCashOut)); return; }
        if (_dailyWageRequest.Value is { } pendingWage) { await RunBusyAsync(() => CompleteDailyWageAsync(pendingWage)); return; }
        if (_overtimeRequest.Value is { } pendingOvertime) { await RunBusyAsync(() => CompleteOvertimeAsync(pendingOvertime)); return; }
        if (_extraShiftRequest.Value is { } pendingExtraShift) { await RunBusyAsync(() => CompleteExtraShiftAsync(pendingExtraShift)); return; }

        if (CashOutEmployee is null) return;
        if (RequiresOpenShift && !HasOpenShift)
        {
            CashOutError = "لا توجد وردية مفتوحة - لا يمكن الصرف من الدرج.";
            return;
        }
        if (ReasonIsRequired && string.IsNullOrWhiteSpace(CashOutReason))
        {
            CashOutError = "السبب إلزامي.";
            return;
        }

        await RunBusyAsync(async () =>
        {
            CashOutError = null;
            var (approvalId, approverId, ok) = await ResolveApprovalAsync(
                CashOutNeedsApproval, CashOutManagerPin, "employees_ledger", CashOutReason, e => CashOutError = e);
            if (!ok) return;

            switch (CashOutKind)
            {
                case "daily_wage":
                    if (!CashInput.TryParse(CashOutAmountText, out var wage) || wage <= 0)
                    { CashOutError = "أدخل المبلغ بشكل صحيح."; return; }
                    await CompleteDailyWageAsync(new RecordDailyWageRequest(
                        CashOutEmployee.Employee.Id, wage, _session.ShiftId, _session.CurrentUser!.Id, approvalId, approverId));
                    break;
                case "overtime":
                    await CompleteOvertimeAsync(new RecordOvertimeRequest(
                        _cashOutRequestId!, CashOutEmployee.Employee.Id, OvertimeHours, null,
                        string.IsNullOrWhiteSpace(CashOutReason) ? null : CashOutReason, _session.CurrentUser!.Id, approvalId, approverId));
                    break;
                case "extra_shift":
                    await CompleteExtraShiftAsync(new RecordExtraShiftRequest(
                        _cashOutRequestId!, CashOutEmployee.Employee.Id, 1,
                        string.IsNullOrWhiteSpace(CashOutReason) ? null : CashOutReason, _session.CurrentUser!.Id, approvalId, approverId));
                    break;
                default:
                    if (!CashInput.TryParse(CashOutAmountText, out var amount) || amount <= 0)
                    { CashOutError = "أدخل المبلغ بشكل صحيح."; return; }
                    await CompleteCashOutAsync(new RecordEmployeeCashOutRequest(
                        _cashOutRequestId!, CashOutEmployee.Employee.Id, CashOutKind, amount, CashOutReason,
                        _session.ShiftId!, _session.CurrentUser!.Id, approvalId, approverId));
                    break;
            }
        });
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        IsBusy = true;
        try { await action(); }
        finally { IsBusy = false; }
    }

    private async Task CompleteCashOutAsync(RecordEmployeeCashOutRequest request)
    {
        var result = await _cashOutRequest.SendAsync(request, r => _session.Api.EmployeeCashOutAsync(r));
        if (!result.Success || result.Value is null) { CashOutError = _cashOutRequest.FailureMessage(result.Error); return; }
        await FinishActionAsync($"تم الصرف. الرصيد بعد العملية: {CashInput.Format(result.Value.BalanceAfterPiastres)} ج");
    }

    private async Task CompleteDailyWageAsync(RecordDailyWageRequest request)
    {
        var result = await _dailyWageRequest.SendAsync(request, r => _session.Api.RecordDailyWageAsync(r));
        if (!result.Success || result.Value is null) { CashOutError = _dailyWageRequest.FailureMessage(result.Error); return; }
        await FinishActionAsync("تم تسجيل اليومية.");
    }

    private async Task CompleteOvertimeAsync(RecordOvertimeRequest request)
    {
        var result = await _overtimeRequest.SendAsync(request, r => _session.Api.RecordOvertimeAsync(r));
        if (!result.Success || result.Value is null) { CashOutError = _overtimeRequest.FailureMessage(result.Error); return; }
        await FinishActionAsync("تم تسجيل الساعات الإضافية.");
    }

    private async Task CompleteExtraShiftAsync(RecordExtraShiftRequest request)
    {
        var result = await _extraShiftRequest.SendAsync(request, r => _session.Api.RecordExtraShiftAsync(r));
        if (!result.Success || result.Value is null) { CashOutError = _extraShiftRequest.FailureMessage(result.Error); return; }
        await FinishActionAsync("تم تسجيل تطبيق الوردية الإضافية.");
    }

    private async Task FinishActionAsync(string note)
    {
        CashOutError = null;
        ActionNote = note;
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
        // One call, not two: the server checks the PIN and decides who approved. Verifying it
        // here and then naming the manager made the check a formality anything could skip.
        var approval = await _session.Api.IssueManagerApprovalAsync(
            new Falah.Application.Approvals.IssueManagerApprovalRequest(managerPin, permission, reason));
        if (!approval.Success || approval.Value is null)
        {
            onError(approval.Error ?? "رمز المدير غير صحيح أو غير مخوّل باعتماد هذا الإجراء.");
            return (null, null, false);
        }

        return (approval.Value.ApprovalId, approval.Value.ApproverUserId, true);
    }
}

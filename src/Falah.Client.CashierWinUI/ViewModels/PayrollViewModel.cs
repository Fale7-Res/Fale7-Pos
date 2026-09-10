using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Employees;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Payroll runs and their lines. A run walks draft → calculated → reviewed → approved → paid, and
/// the server owns that state machine (PAYROLL_TRANSITIONS) - this screen only offers the ONE
/// transition that is legal from the run's current status, so an illegal step is never presented
/// rather than being offered and then rejected.
///
/// Lines are computed once, when the run is created, from that period's own ledger entries, and are
/// never recomputed afterwards. Paying is a separate, more heavily gated action than approving
/// (payroll_pay stacked on top), so it is deliberately not folded into the same button.
/// </summary>
public partial class PayrollViewModel : ObservableObject
{
    private readonly AppSession _session;
    private readonly RetainedRequest<CreatePayrollRequest> _createRequest = new("payroll.create");
    private readonly RetainedRequest<TransitionPayrollRequest> _transitionRequest = new("payroll.transition");
    private readonly RetainedRequest<PayPayrollRequest> _payRequest = new("payroll.pay");
    public bool CanEditAction => !IsBusy && !_createRequest.HasPending && !_transitionRequest.HasPending && !_payRequest.HasPending;
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEditAction));
        OnPropertyChanged(nameof(CanAdvance));
        OnPropertyChanged(nameof(CanOpenPay));
        _session.SetManagementOperationPending(this, !CanEditAction);
    }

    public PayrollViewModel(AppSession session)
    {
        _session = session;
        var cairo = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        var today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, cairo);
        _newPeriod = $"{today.Year:0000}-{today.Month:00}";
    }

    public ObservableCollection<PayrollRunDto> Runs { get; } = new();
    /// <summary>One employee's line, in words. It used to be the raw DTO, which meant the sheet
    /// named people by their id and showed a "deductions and advances" column bound only to the
    /// advances - so a late penalty was labelled as shown and was not.</summary>
    public sealed class PayrollLineRow
    {
        public required PayrollLineDto Line { get; init; }
        public required string EmployeeName { get; init; }

        public long NetPayablePiastres => Line.NetPayablePiastres;

        public string BaseDisplay => CashInput.Format(Line.BasePiastres) + " ج";

        /// <summary>Everything added on top of the base, itemised underneath so a disputed figure
        /// can be traced without opening the ledger.</summary>
        public long AdditionsPiastres => Line.OvertimePiastres + Line.ExtraShiftsPiastres + Line.BonusesPiastres;
        public string AdditionsDisplay => CashInput.Format(AdditionsPiastres) + " ج";

        public string AdditionsBreakdown => Describe(new[]
        {
            ("إضافي", Line.OvertimePiastres),
            ("ورديات", Line.ExtraShiftsPiastres),
            ("مكافآت", Line.BonusesPiastres),
        });

        public long SubtractionsPiastres => Line.DeductionsPiastres + Line.AdvanceDeductionsPiastres;
        public string SubtractionsDisplay => CashInput.Format(SubtractionsPiastres) + " ج";

        public string SubtractionsBreakdown => Describe(new[]
        {
            ("خصومات", Line.DeductionsPiastres),
            ("سلف", Line.AdvanceDeductionsPiastres),
        });

        public string NetDisplay => CashInput.Format(Line.NetPayablePiastres) + " ج";

        public bool IsPaid => Line.Status == "paid";
        public string StatusLabel => IsPaid
            ? (Line.PaidAtUtc is { } at ? $"مدفوع {at.ToLocalTime():MM/dd}" : "مدفوع")
            : "غير مدفوع";

        private static string Describe((string Label, long Amount)[] parts)
        {
            var shown = parts.Where(p => p.Amount != 0)
                .Select(p => $"{p.Label} {CashInput.Format(p.Amount)}");
            return string.Join(" · ", shown);
        }
    }

    public ObservableCollection<PayrollLineRow> Lines { get; } = new();

    /// <summary>Employee names, so a payroll sheet can name the people on it.</summary>
    private readonly Dictionary<string, string> _employeeNames = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _actionNote;
    [ObservableProperty] private string? _actionError;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _newPeriod;

    [ObservableProperty] private PayrollRunDto? _selectedRun;
    [ObservableProperty] private bool _isLoadingLines;

    public bool HasNoRuns => !IsLoading && LoadError is null && Runs.Count == 0;
    public bool CanManage => _session.HasPermission("employees_manage");
    public bool CanApprove => _session.HasPermission("payroll_approve");
    public bool CanPay => _session.HasPermission("payroll_pay");

    /// <summary>The single legal next status for the selected run, or null when it is terminal.
    /// Mirrors the server's own transition table rather than inventing a parallel one.</summary>
    public string? NextStatus => SelectedRun?.Status switch
    {
        "draft" => "calculated",
        "calculated" => "reviewed",
        "reviewed" => "approved",
        _ => null,
    };

    public string? NextStatusLabel => NextStatus switch
    {
        "calculated" => "احتساب الرواتب",
        "reviewed" => "مراجعة",
        "approved" => "اعتماد",
        _ => null,
    };

    public bool CanAdvance => !IsBusy && !_createRequest.HasPending && !_payRequest.HasPending && CanManage && NextStatus is not null && (NextStatus != "approved" || CanApprove);
    public bool IsApprovedAndUnpaid => SelectedRun?.Status == "approved";
    public bool CanOpenPay => CanEditAction && CanPay && IsApprovedAndUnpaid && !IsLoadingLines && LoadError is null;
    partial void OnIsLoadingLinesChanged(bool value) => OnPropertyChanged(nameof(CanOpenPay));
    partial void OnLoadErrorChanged(string? value) => OnPropertyChanged(nameof(CanOpenPay));

    public long SelectedRunTotalPiastres => Lines.Sum(l => l.NetPayablePiastres);
    public string SelectedRunTotalDisplay => IsLoadingLines || LoadError is not null || SelectedRun is null
        ? "—" : CashInput.Format(SelectedRunTotalPiastres) + " ج";
    private int _linesVersion;

    partial void OnSelectedRunChanged(PayrollRunDto? value)
    {
        OnPropertyChanged(nameof(NextStatus));
        OnPropertyChanged(nameof(NextStatusLabel));
        OnPropertyChanged(nameof(CanAdvance));
        OnPropertyChanged(nameof(IsApprovedAndUnpaid));
        OnPropertyChanged(nameof(CanOpenPay));
        if (value is not null) _ = LoadLinesAsync(value);
        else { _linesVersion++; Lines.Clear(); IsLoadingLines = false; OnPropertyChanged(nameof(SelectedRunTotalDisplay)); }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!CanEditAction || IsPayOpen) return;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        try
        {
            var runs = await _session.Api.GetPayrollRunsAsync();
            if (!runs.Success || runs.Value is null)
            {
                LoadError = runs.Error ?? "تعذر تحميل مسيّرات الرواتب.";
                return;
            }
            Runs.Clear();
            foreach (var r in runs.Value.OrderByDescending(r => r.CreatedAtUtc)) Runs.Add(r);
            SelectedRun = Runs.FirstOrDefault(r => r.Id == SelectedRun?.Id) ?? Runs.FirstOrDefault();
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoRuns));
        }
    }

    private async Task LoadLinesAsync(PayrollRunDto run)
    {
        var version = ++_linesVersion;
        IsLoadingLines = true;
        LoadError = null;
        OnPropertyChanged(nameof(SelectedRunTotalDisplay));
        Lines.Clear();
        try
        {
            var lines = await _session.Api.GetPayrollLinesAsync(run.Id);
            if (version != _linesVersion) return;
            if (!lines.Success || lines.Value is null) { LoadError = lines.Error ?? "تعذر تحميل بنود مسيّر الرواتب."; return; }
            await EnsureEmployeeNamesAsync();
            foreach (var l in lines.Value)
                Lines.Add(new PayrollLineRow
                {
                    Line = l,
                    EmployeeName = _employeeNames.GetValueOrDefault(l.EmployeeId, l.EmployeeId),
                });
        }
        finally
        {
            if (version == _linesVersion)
            {
                IsLoadingLines = false;
                OnPropertyChanged(nameof(SelectedRunTotalPiastres));
                OnPropertyChanged(nameof(SelectedRunTotalDisplay));
            }
        }
    }

    /// <summary>Read once per screen and reused: a payroll run does not change who works here.
    /// A failure leaves the ids in place rather than blanking the sheet - a row named by its id is
    /// still a row that can be paid, and refusing to show the run would be the worse outcome.</summary>
    private async Task EnsureEmployeeNamesAsync()
    {
        if (_employeeNames.Count > 0) return;
        var employees = await _session.Api.GetEmployeesAsync();
        if (!employees.Success || employees.Value is null) return;
        foreach (var employee in employees.Value) _employeeNames[employee.Id] = employee.Name;
    }

    [RelayCommand]
    private void SelectRun(PayrollRunDto run) { if (CanEditAction && !IsPayOpen) SelectedRun = run; }

    [RelayCommand]
    private async Task CreateRunAsync()
    {
        if (IsBusy || IsPayOpen || _transitionRequest.HasPending || _payRequest.HasPending || !CanManage) return;
        if (_createRequest.Value is { } pending)
        {
            IsBusy = true;
            try { await CompleteCreateAsync(pending); }
            finally { IsBusy = false; }
            return;
        }
        var period = NewPeriod?.Trim() ?? "";
        if (!System.Text.RegularExpressions.Regex.IsMatch(period, @"^\d{4}-(0[1-9]|1[0-2])$"))
        {
            ActionError = "صيغة الفترة يجب أن تكون YYYY-MM.";
            return;
        }
        IsBusy = true;
        ActionError = null;
        try
        {
            await CompleteCreateAsync(new CreatePayrollRequest($"pr-{Guid.NewGuid():N}", period, _session.CurrentUser!.Id));
        }
        finally { IsBusy = false; }
    }

    private async Task CompleteCreateAsync(CreatePayrollRequest request)
    {
            ActionError = null;
            var result = await _createRequest.SendAsync(request, r => _session.Api.CreatePayrollRunAsync(r));
            if (!result.Success || result.Value is null)
            {
                ActionError = _createRequest.FailureMessage(result.Error);
                return;
            }
            ActionNote = $"تم إنشاء مسيّر رواتب {request.Period}.";
            await ReloadAsync();
    }

    [RelayCommand]
    private async Task AdvanceRunAsync()
    {
        if (!CanAdvance || IsPayOpen) return;
        IsBusy = true;
        ActionError = null;
        try
        {
            var wasUnresolved = _transitionRequest.HasPending;
            var request = _transitionRequest.Value ?? new TransitionPayrollRequest(
                SelectedRun!.Id, NextStatus!, _session.CurrentUser!.Id, null, null);
            var result = await _transitionRequest.SendAsync(request, async r =>
            {
                var write = await _session.Api.TransitionPayrollAsync(r);
                // This endpoint has no request key. A committed transition rejects a replay;
                // reconcile its monotonic status from the authoritative single-run read.
                if (!write.Success && (wasUnresolved || write.OutcomeUnknown))
                {
                    var read = await _session.Api.GetPayrollRunAsync(r.PayrollRunId);
                    if (read.Success && read.Value is { } run && run.Id == r.PayrollRunId &&
                        StatusRank(run.Status) >= StatusRank(r.Status) && StatusRank(r.Status) > 0)
                        return read;
                }
                return write;
            });
            if (!result.Success || result.Value is null)
            {
                ActionError = _transitionRequest.FailureMessage(result.Error);
                return;
            }
            ActionNote = $"تم تحديث حالة المسيّر إلى {NextStatusLabel}.";
            await ReloadAsync();
        }
        finally { IsBusy = false; }
    }

    private static int StatusRank(string status) => status.ToLowerInvariant() switch
    {
        "draft" => 0, "calculated" => 1, "reviewed" => 2, "approved" => 3, "paid" => 4, _ => -1
    };

    public sealed record PaymentAccountOption(string Id, string Label);
    // Exactly the accounts accepted by PayPayrollHandler; no drawer or wallet disbursement.
    public IReadOnlyList<PaymentAccountOption> PaymentAccounts { get; } = new[]
    {
        new PaymentAccountOption("tre-main", "الخزينة الرئيسية"),
        new PaymentAccountOption("tre-bank", "البنك"),
        new PaymentAccountOption("tre-instapay", "إنستاباي")
    };
    [ObservableProperty] private bool _isPayOpen;
    [ObservableProperty] private PaymentAccountOption? _paymentAccount;
    [ObservableProperty] private string? _payError;

    [RelayCommand]
    private void OpenPay()
    {
        if (!CanOpenPay) return;
        PaymentAccount = null;
        PayError = null;
        IsPayOpen = true;
    }

    [RelayCommand]
    private void ClosePay() { if (CanEditAction) IsPayOpen = false; }

    [RelayCommand]
    private async Task PayRunAsync()
    {
        if (IsBusy || _createRequest.HasPending || _transitionRequest.HasPending || !CanPay || !IsPayOpen) return;
        var request = _payRequest.Value;
        if (request is null)
        {
            if (!CanOpenPay || SelectedRun is null) return;
            if (PaymentAccount is null) { PayError = "اختر حساب صرف الرواتب."; return; }
            request = new PayPayrollRequest(SelectedRun.Id, PaymentAccount.Id, _session.CurrentUser!.Id, null, null);
        }
        IsBusy = true;
        PayError = null;
        try
        {
            var result = await _payRequest.SendAsync(request, r => _session.Api.PayPayrollAsync(r));
            if (!result.Success || result.Value is null)
            {
                PayError = _payRequest.FailureMessage(result.Error);
                return;
            }
            IsPayOpen = false;
            ActionError = null;
            ActionNote = $"تم صرف {CashInput.Format(result.Value.TotalPaidPiastres)} ج لعدد {result.Value.SettlementCount} موظف.";
            await ReloadAsync();
        }
        finally { IsBusy = false; }
    }
}

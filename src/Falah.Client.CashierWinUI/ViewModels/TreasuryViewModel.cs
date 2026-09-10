using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Treasury;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Where the money is, and the ledger behind it. Balances are derived server-side from the movement
/// log, so this screen reads them rather than tracking any running total of its own.
///
/// The three actions here are deliberately the OWNER-level ones the Cashier station has no business
/// offering: an external deposit into the business, an owner withdrawal out of it, and a transfer
/// between the business's own accounts. Routine cashier cash handling (safe drop, drawer in/out)
/// stays on the Cashier screen where the drawer physically is. The account pickers only offer the
/// accounts each command actually accepts - the server rejects the drawer and wallet as a deposit
/// destination or a withdrawal source, so offering them here would only manufacture failures.
/// </summary>
public partial class TreasuryViewModel : ObservableObject
{
    private readonly AppSession _session;

    public TreasuryViewModel(AppSession session) => _session = session;

    public ObservableCollection<TreasuryAccountBalanceDto> Balances { get; } = new();
    public ObservableCollection<TreasuryMovementDto> Movements { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _actionNote;
    [ObservableProperty] private bool _isBusy;
    private Func<Task<(bool Success, bool OutcomeUnknown, string? Error)>>? _pendingCommit;
    private bool _outcomeUnknown;
    private long _pendingAmount;
    public bool CanEditAction => !IsBusy && _pendingCommit is null;
    partial void OnIsBusyChanged(bool value) => RefreshOperationState();

    private void RefreshOperationState()
    {
        OnPropertyChanged(nameof(CanEditAction));
        _session.SetManagementOperationPending(this, IsBusy || _pendingCommit is not null);
    }

    public bool CanManage => _session.HasPermission("treasury_manage");

    /// <summary>Deposits land in main/bank/instapay only - never the drawer (walk-in cash) or the
    /// wallet, matching the server's own rule rather than discovering it via a rejection.</summary>
    public IReadOnlyList<string> FundingAccounts { get; } = new[] { "tre-main", "tre-bank", "tre-instapay" };

    public IReadOnlyList<string> AllAccounts { get; } = new[] { "tre-main", "tre-drawer", "tre-bank", "tre-instapay", "tre-wallet" };

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        try
        {
            var actor = _session.CurrentUser!.Id;
            var balances = await _session.Api.GetTreasuryBalancesAsync(actor);
            if (!balances.Success || balances.Value is null)
            {
                LoadError = balances.Error ?? "تعذر تحميل أرصدة الخزينة.";
                return;
            }
            Balances.Clear();
            foreach (var b in balances.Value) Balances.Add(b);

            var movements = await _session.Api.GetTreasuryMovementsAsync(actor, limit: 100);
            if (!movements.Success || movements.Value is null) { LoadError = movements.Error ?? "تعذر تحميل حركة الخزينة."; return; }
            Movements.Clear();
            if (movements.Success && movements.Value is not null)
                foreach (var m in movements.Value) Movements.Add(m);
        }
        finally { IsLoading = false; }
    }

    // ---------------- Deposit / withdraw / transfer ----------------

    /// <summary>"deposit" | "withdraw" | "transfer" - null when no dialog is open.</summary>
    [ObservableProperty] private string? _openAction;
    [ObservableProperty] private string _amountText = "";
    [ObservableProperty] private string _reason = "";
    [ObservableProperty] private string _reference = "";
    [ObservableProperty] private string _sourceAccount = "tre-main";
    [ObservableProperty] private string _destinationAccount = "tre-main";
    [ObservableProperty] private string _depositType = "owner_deposit";

    /// <summary>Exactly the three types TreasuryMapper.ValidDepositTypes accepts - anything else is
    /// rejected server-side, so the UI offers the real set rather than a free-text field.</summary>
    public sealed record DepositTypeOption(string Code, string Label);

    public IReadOnlyList<DepositTypeOption> DepositTypes { get; } = new[]
    {
        new DepositTypeOption("owner_deposit", "إيداع من المالك"),
        new DepositTypeOption("capital_injection", "ضخ رأس مال"),
        new DepositTypeOption("other_external_inflow", "تدفق خارجي آخر"),
    };

    [RelayCommand]
    private void SetDepositType(string code) { if (CanEditAction) DepositType = code; }
    [ObservableProperty] private string _managerPin = "";
    [ObservableProperty] private string? _actionError;

    public bool IsActionOpen => OpenAction is not null;
    public bool IsDeposit => OpenAction == "deposit";
    public bool IsWithdraw => OpenAction == "withdraw";
    public bool IsTransfer => OpenAction == "transfer";
    public string ActionTitle => OpenAction switch
    {
        "deposit" => "إيداع خارجي في الخزينة",
        "withdraw" => "سحب المالك من الخزينة",
        "transfer" => "تحويل بين حسابات الخزينة",
        _ => "",
    };

    partial void OnOpenActionChanged(string? value)
    {
        OnPropertyChanged(nameof(IsActionOpen));
        OnPropertyChanged(nameof(IsDeposit));
        OnPropertyChanged(nameof(IsWithdraw));
        OnPropertyChanged(nameof(IsTransfer));
        OnPropertyChanged(nameof(ActionTitle));
    }

    /// <summary>Minted once per dialog and reused by a retry - the server keys its idempotency on
    /// this, so a lost response can never post the money twice.</summary>
    private string? _requestId;

    public bool NeedsApproval => !CanManage;

    [RelayCommand]
    private void OpenTreasuryAction(string action)
    {
        if (!CanEditAction || action is not ("deposit" or "withdraw" or "transfer")) return;
        ActionError = null;
        AmountText = "";
        Reason = "";
        Reference = "";
        ManagerPin = "";
        SourceAccount = action == "withdraw" ? "tre-main" : "tre-main";
        DestinationAccount = action == "transfer" ? "tre-bank" : "tre-main";
        _requestId = $"tre-{Guid.NewGuid():N}";
        OpenAction = action;
        OnPropertyChanged(nameof(NeedsApproval));
    }

    [RelayCommand]
    private void CloseTreasuryAction() { if (CanEditAction) OpenAction = null; }

    [RelayCommand]
    private void SetSourceAccount(string account) { if (CanEditAction) SourceAccount = account; }

    [RelayCommand]
    private void SetDestinationAccount(string account) { if (CanEditAction) DestinationAccount = account; }

    [RelayCommand]
    private async Task SubmitTreasuryActionAsync()
    {
        if (IsBusy) return;
        if (_pendingCommit is not null)
        {
            IsBusy = true;
            try { await SendPendingAsync(); }
            finally { IsBusy = false; }
            return;
        }
        if (OpenAction is null) return;
        if (!CashInput.TryParse(AmountText, out var amount) || amount <= 0)
        {
            ActionError = "أدخل المبلغ بشكل صحيح.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Reason))
        {
            ActionError = "السبب إلزامي.";
            return;
        }
        if (IsTransfer && SourceAccount == DestinationAccount)
        {
            ActionError = "اختر حسابين مختلفين للتحويل.";
            return;
        }

        IsBusy = true;
        ActionError = null;
        try
        {
            var (approvalId, approverId, ok) = await ResolveApprovalAsync("treasury_manage");
            if (!ok) return;

            var actor = _session.CurrentUser!.Id;
            var reference = string.IsNullOrWhiteSpace(Reference) ? null : Reference;
            if (IsDeposit)
            {
                var request = new DepositRequest(
                    _requestId!, DepositType, amount, DestinationAccount, Reason, reference, actor, approvalId, approverId);
                _pendingCommit = () => NormalizeAsync(_session.Api.TreasuryDepositAsync(request));
            }
            else if (IsWithdraw)
            {
                var request = new WithdrawalRequest(
                    _requestId!, amount, SourceAccount, Reason, reference, actor, approvalId, approverId);
                _pendingCommit = () => NormalizeAsync(_session.Api.TreasuryWithdrawAsync(request));
            }
            else
            {
                var request = new TransferRequest(
                    _requestId!, SourceAccount, DestinationAccount, amount, Reason, actor, approvalId, approverId);
                _pendingCommit = () => NormalizeAsync(_session.Api.TreasuryTransferAsync(request));
            }
            _pendingAmount = amount;
            await SendPendingAsync();
        }
        finally { IsBusy = false; }
    }

    private static async Task<(bool Success, bool OutcomeUnknown, string? Error)> NormalizeAsync<T>(Task<ApiClient.ApiResult<T>> task)
    {
        var result = await task;
        return (result.Success && result.Value is not null, result.OutcomeUnknown, result.Error);
    }

    private async Task SendPendingAsync()
    {
        ActionError = null;
        var result = await _pendingCommit!();
        if (!result.Success)
        {
            _outcomeUnknown |= result.OutcomeUnknown;
            if (!_outcomeUnknown) _pendingCommit = null;
            ActionError = _outcomeUnknown
                ? "لم تُحسم نتيجة حركة الخزينة. أعد المحاولة بنفس الطلب للتحقق قبل مغادرة العملية."
                : result.Error ?? "تعذر تسجيل حركة الخزينة.";
            RefreshOperationState();
            return;
        }
        _pendingCommit = null;
        _outcomeUnknown = false;
        ActionNote = $"تم تسجيل العملية بمبلغ {CashInput.Format(_pendingAmount)} ج.";
        OpenAction = null;
        _requestId = null;
        RefreshOperationState();
        await LoadAsync();
    }

    private async Task<(string? ApprovalId, string? ApproverUserId, bool Ok)> ResolveApprovalAsync(string permission)
    {
        if (!NeedsApproval) return (null, null, true);
        if (ManagerPin.Length < 4)
        {
            ActionError = "أدخل رمز المدير المخوّل باعتماد هذه العملية.";
            return (null, null, false);
        }
        var login = await _session.Api.LoginByPinAsync(ManagerPin);
        if (!login.Success || login.Value is null)
        {
            ActionError = login.Error ?? "تعذر التحقق من رمز المدير.";
            return (null, null, false);
        }
        var approval = await _session.Api.IssueManagerApprovalAsync(
            new Falah.Application.Approvals.IssueManagerApprovalRequest(login.Value.Id, permission, Reason));
        if (!approval.Success || approval.Value is null)
        {
            ActionError = approval.Error ?? "هذا المستخدم غير مخول باعتماد هذه العملية.";
            return (null, null, false);
        }
        return (approval.Value.ApprovalId, login.Value.Id, true);
    }
}

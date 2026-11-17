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

    public TreasuryViewModel(AppSession session)
    {
        _session = session;
        // UI-0001: a movement whose outcome was never learned survives a crash or power cut. It comes
        // back as the open action, to be re-sent with its ORIGINAL request id (the server replays it),
        // never retyped as a new one that would record the money twice.
        if (_write.Value is { } recovered)
        {
            OpenAction = recovered switch { DepositRequest => "deposit", WithdrawalRequest => "withdraw", _ => "transfer" };
            _pendingAmount = recovered switch
            {
                DepositRequest d => d.AmountPiastres, WithdrawalRequest w => w.AmountPiastres,
                TransferRequest t => t.AmountPiastres, _ => 0,
            };
            // Shown exactly as it will be re-sent (the stored request, not these fields, is what goes).
            AmountText = CashInput.Format(_pendingAmount);
            (Reason, Reference) = recovered switch
            {
                DepositRequest d => (d.Reason, d.Reference ?? ""), WithdrawalRequest w => (w.Reason, w.Reference ?? ""),
                TransferRequest t => (t.Reason, ""), _ => ("", ""),
            };
            switch (recovered)
            {
                case DepositRequest d: DepositType = d.Type; DestinationAccount = d.DestinationAccountId; break;
                case WithdrawalRequest w: SourceAccount = w.SourceAccountId; break;
                case TransferRequest t: SourceAccount = t.SourceAccountId; DestinationAccount = t.DestinationAccountId; break;
            }
            _pendingCommit = () => SendDurableAsync(recovered);
            _outcomeUnknown = true;
            ActionError = "حركة خزينة سابقة لم تُحسم نتيجتها قبل إغلاق البرنامج. أعد المحاولة بنفس الطلب للتحقق.";
            RefreshOperationState();
        }
    }

    private readonly RetainedRequest<object> _write = new("treasury.action");



    private async Task<(bool Success, bool OutcomeUnknown, string? Error)> SendDurableAsync(object request)
    {
        var result = await _write.SendAsync(request, async r => r switch
        {
            DepositRequest d => Confirm(await _session.Api.TreasuryDepositAsync(d)),
            WithdrawalRequest w => Confirm(await _session.Api.TreasuryWithdrawAsync(w)),
            TransferRequest t => Confirm(await _session.Api.TreasuryTransferAsync(t)),
            _ => throw new InvalidOperationException("Unsupported treasury command."),
        });
        return (result.Success, result.OutcomeUnknown || _write.HasPending, result.Error);

        static ApiClient.ApiResult<bool> Confirm<T>(ApiClient.ApiResult<T> r) =>
            new(r.Success && r.Value is not null, r.Success && r.Value is not null, r.Error, r.OutcomeUnknown, r.StatusCode);
    }

    public ObservableCollection<TreasuryAccountBalanceDto> Balances { get; } = new();
    public ObservableCollection<MovementRow> Movements { get; } = new();

    /// <summary>One posted movement, said in full. The ledger used to show type, from, to, amount
    /// and reason - and never WHEN it happened, WHO posted it, who approved it, or which sale or
    /// invoice it belongs to. Four of the five columns a reconciliation actually needs were absent.</summary>
    public sealed class MovementRow
    {
        public required TreasuryMovementDto Movement { get; init; }

        public string TypeLabel => Movement.Type switch
        {
            // "sale"/"expense"/"supplier_payment" kept for any historical rows written before the
            // handlers below settled on their current type strings - the switch must still label
            // old data, not just new.
            "sale" or "sale_collection" => "مبيعات",
            "refund" => "مرتجع",
            "expense" or "expense_payment" => "مصروف",
            "expense_reversal" => "إلغاء مصروف",
            "expense_correction" => "تصحيح مصروف مسجل بالخطأ",
            "expense_refund" => "مبلغ مصروف عاد نقدًا",
            "movement_correction" => "تصحيح حركة مسجلة بالخطأ",
            "owner_deposit" => "إيداع من المالك",
            "capital_injection" => "ضخ رأس مال",
            "other_external_inflow" => "تدفق خارجي",
            "owner_withdrawal" => "سحب المالك",
            "transfer" => "تحويل داخلي",
            "safe_drop" => "توريد للخزنة",
            "driver_settlement" => "توريد سائق",
            "supplier_payment" or "purchase_payment" => "سداد مورد",
            "payroll_payment" => "صرف رواتب",
            "cash_in" => "إيداع نقدية بالدرج",
            "cash_out" => "سحب نقدية من الدرج",
            "employee_advance" => "سلفة موظف",
            "employee_withdrawal" => "سحب موظف من رصيده",
            // Shift float/handover, reconciliation, drawer transfers, employee and settlement lines:
            // the shared vocabulary names them, so this ledger never prints "shift_opening_float".
            _ => Services.DisplayLabels.TreasuryMovementType(Movement.Type),
        };

        public string AmountDisplay => CashInput.Format(Movement.AmountPiastres) + " ج";
        public string WhenDisplay => Movement.CreatedAtUtc.ToLocalTime().ToString("yyyy/MM/dd HH:mm");

        public string ActorDisplay => Movement.ApprovedBy is { Length: > 0 } approver
            ? $"{Movement.CreatedBy} · اعتماد {approver}"
            : Movement.CreatedBy;

        /// <summary>The document behind the money, so a line can be traced to the thing that caused
        /// it rather than taken on trust. An order is named by the NUMBER the server resolved
        /// (#0172), falling back to the raw id only when the order could not be resolved at all -
        /// which is still better than showing nothing, but should not be the normal case.</summary>
        public string ReferenceDisplay => Movement.ReferenceType switch
        {
            "order" => "طلب " + (Movement.ReferenceLabel ?? Movement.ReferenceId),
            // A refund references its order too; it used to fall through and print "order_refund ord-…".
            "order_refund" => "استرجاع طلب " + (Movement.ReferenceLabel ?? Movement.ReferenceId),
            "purchase" => "فاتورة " + Movement.ReferenceId,
            "expense" => "مصروف " + Movement.ReferenceId,
            "payroll" => "مسيّر " + Movement.ReferenceId,
            "employee" => "موظف",
            "employee_ledger" => "كشف حساب موظف",
            "external_funding" => "تمويل خارجي",
            "shift" or "shift_handover" or "shift_opening" => "وردية",
            "safe_drop" => "إيداع آمن",
            "transfer" or "treasury_transfer" => "تحويل بين الحسابات",
            "driver_settlement" or "delivery_trip" => "تسوية سائق",
            "cash_movement" or "cash_drawer" => "حركة درج",
            // Never an internal type and id ("external_funding 7bd4…") in the owner's ledger
            // (native review 2026-11-07); the reason column already describes the movement.
            _ => "",
        };

        /// <summary>
        /// The movement's own stored reason, with any internal order id swapped for the order number
        /// the server resolved. Sale and refund reasons are composed at write time ("تحصيل طلب
        /// ord-b111c9ca..."), so the raw id is baked into rows that already exist; rewriting them is
        /// not an option and the id is meaningless to an owner reading the ledger. Substituting here
        /// keeps the stored value untouched and fixes historical rows as well as new ones.
        /// </summary>
        public string Reason => Movement.ReferenceLabel is { Length: > 0 } number
                                && Movement.ReferenceId is { Length: > 0 } id
            ? Movement.Reason.Replace(id, number, StringComparison.Ordinal)
            : Movement.Reason;
        /// <summary>A hand-typed entry (TreasuryCorrectionRules) - the only kind corrected from this screen; the server
        /// re-checks it, and refuses a second correction of the same entry.</summary>
        public bool AlreadyCorrected { get; init; }
        public bool IsCorrectable => !AlreadyCorrected && Falah.Domain.Financial.TreasuryCorrectionRules.CorrectableTypes.Contains(Movement.Type);

        public string? SourceAccountId => Movement.SourceAccountId;
        public string? DestinationAccountId => Movement.DestinationAccountId;
    }

    [ObservableProperty] private TreasuryPositionDto? _position;

    /// <summary>Money the restaurant is counting on that is in no account: an electronic transfer
    /// nobody has confirmed, and cash a driver is still carrying.</summary>
    public string PendingElectronicDisplay =>
        Position is { } p ? CashInput.Format(p.PendingElectronicPiastres) + " ج" : "—";

    public string PendingElectronicNote =>
        Position is { PendingElectronicOrderCount: > 0 } p ? $"{p.PendingElectronicOrderCount} طلب بانتظار التأكيد" : "";

    public string DriverCustodyDisplay =>
        Position is { } p ? CashInput.Format(p.UnsettledDriverCustodyPiastres) + " ج" : "—";

    public string DriverCustodyNote =>
        Position is { UnsettledDriverCount: > 0 } p ? $"مع {p.UnsettledDriverCount} سائق" : "";

    public string TotalInAccountsDisplay =>
        Position is { } p ? CashInput.Format(p.TotalInAccountsPiastres) + " ج" : "—";

    public bool HasMoneyOutside =>
        Position is { } p && (p.PendingElectronicPiastres > 0 || p.UnsettledDriverCustodyPiastres > 0);

    // Ledger filters: a reconciliation looks at one account, or one kind of movement, not at
    // everything at once.
    [ObservableProperty] private string _accountFilter = "";
    [ObservableProperty] private string _searchText = "";

    public bool HasAccountFilter => AccountFilter.Length > 0;

    partial void OnAccountFilterChanged(string value)
    {
        OnPropertyChanged(nameof(HasAccountFilter));
        ApplyMovementFilter();
    }
    partial void OnSearchTextChanged(string value) => ApplyMovementFilter();

    private readonly List<MovementRow> _allMovements = new();

    public bool HasNoMovements => !IsLoading && LoadError is null && _allMovements.Count == 0;
    public bool HasNoMatchingMovements => !IsLoading && LoadError is null && _allMovements.Count > 0 && Movements.Count == 0;

    private void ApplyMovementFilter()
    {
        var term = SearchText.Trim();
        var rows = _allMovements.Where(m =>
            (AccountFilter.Length == 0 || m.SourceAccountId == AccountFilter || m.DestinationAccountId == AccountFilter)
            && (term.Length == 0
                || m.TypeLabel.Contains(term, StringComparison.OrdinalIgnoreCase)
                || m.Reason.Contains(term, StringComparison.OrdinalIgnoreCase)
                || m.ReferenceDisplay.Contains(term, StringComparison.OrdinalIgnoreCase)
                || m.ActorDisplay.Contains(term, StringComparison.OrdinalIgnoreCase)));

        Movements.Clear();
        foreach (var row in rows) Movements.Add(row);
        OnPropertyChanged(nameof(HasNoMovements));
        OnPropertyChanged(nameof(HasNoMatchingMovements));
    }

    [RelayCommand]
    private void FilterByAccount(string accountId) => AccountFilter = AccountFilter == accountId ? "" : accountId;

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

            var position = await _session.Api.GetTreasuryPositionAsync(actor);
            if (!position.Success || position.Value is null)
                LoadError = position.Error ?? "تعذر تحميل المبالغ خارج الحسابات.";
            Position = position.Value;

            var movements = await _session.Api.GetTreasuryMovementsAsync(actor, limit: 200);
            if (!movements.Success || movements.Value is null) { LoadError = movements.Error ?? "تعذر تحميل حركة الخزينة."; return; }
            _allMovements.Clear();
            // An entry already corrected offers no second correction (the server would refuse it anyway).
            var corrected = movements.Value.Where(m => m.Type == Falah.Domain.Financial.TreasuryCorrectionRules.MovementType)
                .Select(m => m.ReferenceId).ToHashSet();
            foreach (var m in movements.Value) _allMovements.Add(new MovementRow { Movement = m, AlreadyCorrected = corrected.Contains(m.Id) });
            ApplyMovementFilter();
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoMovements));
            OnPropertyChanged(nameof(HasNoMatchingMovements));
            OnPropertyChanged(nameof(PendingElectronicDisplay));
            OnPropertyChanged(nameof(PendingElectronicNote));
            OnPropertyChanged(nameof(DriverCustodyDisplay));
            OnPropertyChanged(nameof(DriverCustodyNote));
            OnPropertyChanged(nameof(TotalInAccountsDisplay));
            OnPropertyChanged(nameof(HasMoneyOutside));
        }
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
                _pendingCommit = () => SendDurableAsync(request);
            }
            else if (IsWithdraw)
            {
                var request = new WithdrawalRequest(
                    _requestId!, amount, SourceAccount, Reason, reference, actor, approvalId, approverId);
                _pendingCommit = () => SendDurableAsync(request);
            }
            else
            {
                var request = new TransferRequest(
                    _requestId!, SourceAccount, DestinationAccount, amount, Reason, actor, approvalId, approverId);
                _pendingCommit = () => SendDurableAsync(request);
            }
            _pendingAmount = amount;
            await SendPendingAsync();
        }
        finally { IsBusy = false; }
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
        // One call, not two: the server checks the PIN and decides who approved. Verifying it
        // here and then naming the manager made the check a formality anything could skip.
        var approval = await _session.Api.IssueManagerApprovalAsync(
            new Falah.Application.Approvals.IssueManagerApprovalRequest(ManagerPin, permission, Reason));
        if (!approval.Success || approval.Value is null)
        {
            ActionError = approval.Error ?? "رمز المدير غير صحيح أو غير مخوّل باعتماد هذا الإجراء.";
            return (null, null, false);
        }

        return (approval.Value.ApprovalId, approval.Value.ApproverUserId, true);
    }
}

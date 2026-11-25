using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Delivery;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// The dispatch board: every delivery still in play, plus the driver roster and their cash custody.
/// Purely a client over the existing Delivery API - the state machine, custody math, approval gates
/// and idempotency all live server-side and are never reproduced here. The board is a read
/// (GET /api/delivery/board) refreshed after every write, rather than a locally-mutated list, so what
/// the dispatcher sees is always the server's own view of the state machine.
/// </summary>
public partial class DeliveryViewModel : ObservableObject
{
    private readonly AppSession _session;
    private readonly RetainedRequest<AssignDeliveryRequest> _assignRequest = new("delivery.assign");
    private readonly RetainedRequest<RecordDeliveryResultRequest> _outcomeRequest = new("delivery.outcome");
    private readonly RetainedRequest<SettleDriverCustodyRequest> _settleRequest = new("delivery.custody-settle");
    public bool CanEditAction => !IsBusy && !_assignRequest.HasPending && !_outcomeRequest.HasPending && !_settleRequest.HasPending
        && !_collectRequest.HasPending;
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEditAction));
        _session.SetManagementOperationPending(this, !CanEditAction);
    }

    /// <summary>UI-REQ-001: a driver's daily statement (screen and paper).</summary>
    public DriverStatementViewModel DriverStatement { get; }

    [RelayCommand]
    private Task OpenDriverStatementAsync() => DriverStatement.OpenAsync();

    public DeliveryViewModel(AppSession session)
    {
        _session = session;
        session.TrackManagementOwner(this);
        Directory = new DeliveryDirectoryViewModel(session);
        DriverStatement = new DriverStatementViewModel(session);
        // Adding or retiring a driver changes who this board can assign, so the board re-reads.
        Directory.Changed += () => _ = LoadAsync();
    }

    /// <summary>Driver and delivery-service authoring. Both are `delivery_manage`, the same
    /// permission this board's own dispatch actions already use.</summary>
    public DeliveryDirectoryViewModel Directory { get; }

    public bool CanOpenDirectory => Directory.CanOpen;

    [RelayCommand]
    private Task OpenDirectoryAsync() => Directory.OpenAsync();

    /// <summary>One row of the board. It used to be the raw DTO, which carried the delivery's own
    /// state and the money and nothing about where the food was going - a dispatcher assigning a
    /// driver had to open each order separately to find out which side of town it was on.</summary>
    public sealed class BoardRow
    {
        public required DeliveryDto Delivery { get; init; }

        public string DeliveryId => Delivery.DeliveryId;
        public string? OrderNumber => Delivery.OrderNumber;
        public string Status => Delivery.Status;
        public string? DriverName => Delivery.DriverName;

        public string CustomerDisplay => Delivery.CustomerName is { Length: > 0 } name ? name : "عميل غير مسجّل";
        public string PhoneDisplay => Delivery.CustomerPhone ?? "";

        public string AddressDisplay => string.Join(" · ", new[] { Delivery.DeliveryArea, Delivery.DeliveryAddress }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

        public bool HasAddress => AddressDisplay.Length > 0;

        public string AmountToCollectDisplay =>
            CollectPiastres(Delivery) > 0 ? CashInput.Display(CollectPiastres(Delivery)) + " ج" : "—";

        /// <summary>Three states, not two. "Paid" and "collect on delivery" are both settled
        /// questions; a transfer nobody has confirmed is neither, and handing that order to a driver
        /// without saying so is how an order goes out with nothing to collect and nothing collected.</summary>
        public string PaymentStateLabel =>
            IsPaymentUnconfirmed ? "تحويل لم يُؤكَّد"
            : Delivery.PaymentAlreadySettled ? "مدفوع"
            : "تحصيل عند التسليم";

        public bool IsPaymentUnconfirmed => string.Equals(Delivery.ElectronicPaymentStatus, "pending", StringComparison.OrdinalIgnoreCase);

        /// <summary>FIN-0051: an unpaid cash-on-delivery order not yet with a driver can be cancelled.</summary>
        public bool CanCancel => CanCancelBeforeDispatch(Delivery);

        /// <summary>How long this order has been on the board. The single most useful number on a
        /// dispatch screen and the one it did not have.</summary>
        public string ElapsedDisplay
        {
            get
            {
                var elapsed = DateTimeOffset.UtcNow - Delivery.CreatedAtUtc;
                if (elapsed < TimeSpan.Zero) return "";
                var minutes = (int)elapsed.TotalMinutes;
                return minutes < 60 ? $"{minutes} د" : $"{minutes / 60} س {minutes % 60} د";
            }
        }

        public bool IsSlow => DateTimeOffset.UtcNow - Delivery.CreatedAtUtc > TimeSpan.FromMinutes(45)
            && Delivery.Status is "New" or "Ready" or "OutForDelivery";
    }

    public ObservableCollection<BoardRow> Board { get; } = new();
    public ObservableCollection<DriverDto> Drivers { get; } = new();

    /// <summary>What the supervisor needs before reading a single row: how many are waiting to be
    /// prepared, ready to go, and already out.</summary>
    public string BoardSummary
    {
        get
        {
            if (Board.Count == 0) return "";
            var parts = new List<string>();
            var counts = new (string Status, string Label)[]
            {
                ("New", "جديد"), ("Ready", "جاهز"), ("OutForDelivery", "بالطريق"),
            };
            foreach (var (status, label) in counts)
            {
                var n = Board.Count(r => r.Status == status);
                if (n > 0) parts.Add($"{n} {label}");
            }
            var slow = Board.Count(r => r.IsSlow);
            if (slow > 0) parts.Add($"{slow} متأخر");
            return string.Join(" · ", parts);
        }
    }

    public bool HasBoardSummary => BoardSummary.Length > 0;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _actionError;
    [ObservableProperty] private string? _actionNote;
    [ObservableProperty] private bool _isBusy;

    public bool HasNoDeliveries => !IsLoading && LoadError is null && Board.Count == 0;

    /// <summary>Audit 2026-11-23, F-11: the drivers panel said nothing when there was nobody to assign.</summary>
    public bool HasNoDrivers => !IsLoading && LoadError is null && Drivers.Count == 0;

    /// <summary>Audit 2026-11-23, F-16: how often the board reads itself again while it is on screen. A dispatcher
    /// used to work from orders that had already moved on until someone pressed "تحديث".</summary>
    public static readonly TimeSpan AutoRefreshInterval = TimeSpan.FromSeconds(30);

    /// <summary>Never under the dispatcher's hands: not while anything is loading or being sent, an answer is still
    /// awaited, or a dialog about one delivery or driver is open.</summary>
    public bool CanAutoRefresh => !IsLoading && !IsBusy && CanEditAction
        && CancelTarget is null && AssignTarget is null && OutcomeTarget is null && AccountDriver is null
        && QueueCancelTarget is null && CollectTarget is null;

    [RelayCommand]
    private async Task AutoRefreshAsync()
    {
        if (CanAutoRefresh) await LoadAsync();
    }

    /// <summary>Writes are gated on delivery_manage; the server enforces this too (and accepts a
    /// manager approval instead), this only decides whether the buttons are offered at all.</summary>
    public bool CanDispatch => _session.HasPermission("delivery_manage");

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        OnPropertyChanged(nameof(HasNoDeliveries));
        try
        {
            var board = await _session.Api.GetDispatchBoardAsync();
            if (!board.Success || board.Value is null)
            {
                LoadError = board.Error ?? "تعذر تحميل لوحة التوصيل.";
                return;
            }
            Board.Clear();
            foreach (var d in board.Value) Board.Add(new BoardRow { Delivery = d });
            OnPropertyChanged(nameof(BoardSummary));
            OnPropertyChanged(nameof(HasBoardSummary));

            // Workflow redesign (owner, 2026-11-24): the staged delivery and pickup queue, over the same board.
            await LoadQueueAsync();

            var drivers = await _session.Api.GetActiveDriversAsync();
            if (!drivers.Success || drivers.Value is null) { LoadError = drivers.Error ?? "تعذر تحميل السائقين."; return; }
            if (drivers.Success && drivers.Value is not null)
            {
                Drivers.Clear();
                foreach (var d in drivers.Value) Drivers.Add(d);
            }
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoDeliveries));
            OnPropertyChanged(nameof(HasNoDrivers));
            OnPropertyChanged(nameof(HasNoPickups));
        }
    }

    // ---------------- Mark ready ----------------

    [RelayCommand]
    private async Task MarkReadyAsync(DeliveryDto delivery)
    {
        if (!CanEditAction || !CanDispatch) return;
        IsBusy = true;
        ActionError = null;
        ActionNote = null;
        try
        {
            var result = await _session.Api.MarkDeliveryReadyAsync(
                new MarkDeliveryReadyRequest(delivery.DeliveryId, _session.CurrentUser!.Id, null, null));
            if (!result.Success)
            {
                ActionError = result.Error ?? "تعذر تجهيز الطلب.";
                return;
            }
            ActionNote = $"تم تجهيز {delivery.OrderNumber} للتوصيل.";
            await LoadAsync();
        }
        finally { IsBusy = false; }
    }

    // ---------------- Cancel before it leaves (FIN-0051, owner decision 2026-11-22) ----------------

    [ObservableProperty] private DeliveryDto? _cancelTarget;
    [ObservableProperty] private string _cancelReason = "";
    [ObservableProperty] private string? _cancelStockDisposition;
    [ObservableProperty] private string _cancelManagerPin = "";
    [ObservableProperty] private string? _cancelError;
    private string? _cancelRequestId;

    public bool IsCancelOpen => CancelTarget is not null;
    /// <summary>Without delivery_override a manager approves the cancellation with their PIN.</summary>
    public bool CancelNeedsApproval => !_session.HasPermission("delivery_override");
    partial void OnCancelTargetChanged(DeliveryDto? value) => OnPropertyChanged(nameof(IsCancelOpen));

    public bool IsCancelFoodPrepared
    {
        get => CancelStockDisposition == "prepared";
        set { if (value) CancelStockDisposition = "prepared"; }
    }

    public bool IsCancelFoodNotPrepared
    {
        get => CancelStockDisposition == "not_prepared";
        set { if (value) CancelStockDisposition = "not_prepared"; }
    }

    partial void OnCancelStockDispositionChanged(string? value)
    {
        OnPropertyChanged(nameof(IsCancelFoodPrepared));
        OnPropertyChanged(nameof(IsCancelFoodNotPrepared));
    }

    /// <summary>Only an unpaid cash-on-delivery order still on the board (not yet with a driver).</summary>
    public static bool CanCancelBeforeDispatch(DeliveryDto delivery) =>
        delivery.Status is "New" or "Preparing" or "Ready"
        && delivery is { PaymentMethod: "cash", PaymentAlreadySettled: false }
        && !string.Equals(delivery.ElectronicPaymentStatus, "pending", StringComparison.OrdinalIgnoreCase);

    [RelayCommand]
    private void OpenCancel(DeliveryDto delivery)
    {
        if (!CanEditAction || !CanCancelBeforeDispatch(delivery)) return;
        CancelReason = "";
        CancelStockDisposition = null;
        CancelManagerPin = "";
        CancelError = null;
        _cancelRequestId = $"cud-{Guid.NewGuid():N}";
        CancelTarget = delivery;
    }

    [RelayCommand]
    private void CloseCancel() { if (CanEditAction) CancelTarget = null; }

    [RelayCommand]
    private async Task ConfirmCancelAsync()
    {
        if (CancelTarget is null || !CanEditAction) return;
        if (string.IsNullOrWhiteSpace(CancelReason)) { CancelError = "اكتب سبب الإلغاء."; return; }
        if (CancelStockDisposition is null) { CancelError = "اختر: هل الأكل اتحضّر ولا لأ؟"; return; }
        IsBusy = true;
        CancelError = null;
        try
        {
            string? approvalId = null, approverId = null;
            if (CancelNeedsApproval)
            {
                var pin = CancelManagerPin.Trim();
                if (pin.Length < 4) { CancelError = "الإلغاء يحتاج رمز المدير."; return; }
                var approval = await _session.Api.IssueManagerApprovalAsync(
                    new Falah.Application.Approvals.IssueManagerApprovalRequest(pin, "delivery_override", CancelReason.Trim()));
                if (!approval.Success || approval.Value is null)
                {
                    CancelError = approval.Error ?? "رمز المدير غير صحيح أو غير مخوّل باعتماد الإلغاء.";
                    return;
                }
                (approvalId, approverId) = (approval.Value.ApprovalId, approval.Value.ApproverUserId);
            }
            var result = await _session.Api.CancelUndispatchedDeliveryAsync(new Falah.Application.Orders.CancelUndispatchedDeliveryRequest(
                _cancelRequestId!, CancelTarget.OrderId, CancelReason.Trim(), _session.CurrentUser!.Id, CancelStockDisposition, approvalId, approverId));
            if (!result.Success)
            {
                CancelError = result.Error ?? "تعذر إلغاء الطلب.";
                return;
            }
            ActionNote = $"تم إلغاء الطلب {CancelTarget.OrderNumber} قبل خروجه.";
            CancelTarget = null;
            await LoadAsync();
        }
        finally { IsBusy = false; }
    }

    // ---------------- Assign to a driver ----------------

    [ObservableProperty] private DeliveryDto? _assignTarget;
    [ObservableProperty] private DriverDto? _assignDriver;
    /// <summary>Only needed when the driver already holds the custody limit (1,500 ج); empty otherwise.</summary>
    [ObservableProperty] private string _assignManagerPin = "";
    [ObservableProperty] private string? _assignError;
    /// <summary>What the driver actually has to bring back - the same rule the server uses to open a
    /// custody (DeliveryRules.DeliveryCashResponsibility): cash on delivery always, any other method
    /// only while its money has not arrived. A paid card order shows nothing to collect.</summary>
    public static long CollectPiastres(DeliveryDto d) =>
        d.PaymentMethod == "cash" || !d.PaymentAlreadySettled ? d.AmountToCollectPiastres : 0;

    public string AssignCollectDisplay => AssignTarget is { } t && CollectPiastres(t) > 0
        ? CashInput.Display(CollectPiastres(t)) + " ج" : "لا شيء (مدفوع)";

    public bool IsAssignOpen => AssignTarget is not null;
    partial void OnAssignTargetChanged(DeliveryDto? value) { OnPropertyChanged(nameof(IsAssignOpen)); OnPropertyChanged(nameof(AssignCollectDisplay)); }

    /// <summary>Minted once per assignment dialog and reused by a retry, so a lost response cannot
    /// become a second assignment - the server keys its own idempotency on this value.</summary>
    private string? _assignRequestId;

    [RelayCommand]
    private void OpenAssign(DeliveryDto delivery)
    {
        if (!CanEditAction || !CanDispatch) return;
        AssignError = null;
        AssignDriver = null;
        AssignManagerPin = "";
        _assignRequestId = $"asg-{Guid.NewGuid():N}";
        AssignTarget = delivery;
    }

    [RelayCommand]
    private void CloseAssign() { if (CanEditAction) AssignTarget = null; }

    [RelayCommand]
    private async Task ConfirmAssignAsync()
    {
        if (IsBusy || _outcomeRequest.HasPending || _settleRequest.HasPending || !CanDispatch) return;
        if (_assignRequest.Value is { } pending)
        {
            IsBusy = true;
            try { await CompleteAssignAsync(pending); }
            finally { IsBusy = false; }
            return;
        }
        if (AssignTarget is null) return;
        if (AssignDriver is null) { AssignError = "اختر السائق."; return; }
        IsBusy = true;
        AssignError = null;
        try
        {
            // Owner, 2026-11-24: past the driver's custody limit a manager lets this one order through with a PIN.
            string? approvalId = null, approverId = null;
            var pin = AssignManagerPin.Trim();
            if (pin.Length > 0)
            {
                if (pin.Length < 4) { AssignError = "رمز المدير ناقص."; return; }
                var approval = await _session.Api.IssueManagerApprovalAsync(
                    new Falah.Application.Approvals.IssueManagerApprovalRequest(pin, "driver_custody_limit", "تعدي حد عهدة السائق"));
                if (!approval.Success || approval.Value is null)
                {
                    AssignError = approval.Error ?? "رمز المدير غير صحيح.";
                    return;
                }
                (approvalId, approverId) = (approval.Value.ApprovalId, approval.Value.ApproverUserId);
            }
            await CompleteAssignAsync(new AssignDeliveryRequest(
                _assignRequestId!, AssignTarget.DeliveryId, AssignDriver.Id, _session.CurrentUser!.Id, approvalId, approverId));
            AssignManagerPin = "";
        }
        finally { IsBusy = false; }
    }

    private async Task CompleteAssignAsync(AssignDeliveryRequest request)
    {
            AssignError = null;
            var result = await _assignRequest.SendAsync(request, r => _session.Api.AssignDeliveryAsync(r));
            if (!result.Success || result.Value is null)
            {
                AssignError = _assignRequest.FailureMessage(result.Error);
                return;
            }
            ActionNote = $"تم إسناد الطلب إلى {result.Value.DriverName}.";
            AssignTarget = null;
            _assignRequestId = null;
            await LoadAsync();
    }

    // ---------------- Record the outcome ----------------

    [ObservableProperty] private DeliveryDto? _outcomeTarget;
    [ObservableProperty] private string _outcomeResult = "delivered";
    [ObservableProperty] private string _outcomeCollectedText = "";
    [ObservableProperty] private string _outcomeManagerPin = "";

    /// <summary>AUD-03: the typed amount is below what the driver had to collect. The server then needs the authority
    /// that approves a settlement difference, so a user without it is asked for a manager's PIN.</summary>
    public bool IsOutcomeShortfall =>
        IsDeliveredOutcome && OutcomeTarget is { } t
        && CashInput.TryParse(OutcomeCollectedText, out var collected) && collected < CollectPiastres(t);

    public bool OutcomeShortfallNeedsApproval => IsOutcomeShortfall && !_session.HasPermission("delivery_settlement_approve");

    public string OutcomeShortfallLabel =>
        OutcomeTarget is { } t && CashInput.TryParse(OutcomeCollectedText, out var collected) && collected < CollectPiastres(t)
            ? $"المحصّل أقل من المستحق بـ {Egp(CollectPiastres(t) - collected)}. يحتاج اعتماد مدير مخوّل باعتماد فروق التسليم."
            : "";

    partial void OnOutcomeCollectedTextChanged(string value) => RaiseShortfall();

    private void RaiseShortfall()
    {
        OnPropertyChanged(nameof(IsOutcomeShortfall));
        OnPropertyChanged(nameof(OutcomeShortfallNeedsApproval));
        OnPropertyChanged(nameof(OutcomeShortfallLabel));
    }

    [ObservableProperty] private string _outcomeReason = "";
    [ObservableProperty] private string _outcomeNotes = "";
    [ObservableProperty] private string? _outcomeError;
    public bool IsOutcomeOpen => OutcomeTarget is not null;
    partial void OnOutcomeTargetChanged(DeliveryDto? value)
    {
        OnPropertyChanged(nameof(IsOutcomeOpen));
        OnPropertyChanged(nameof(NeedsFoodDisposition));
        RaiseShortfall();
    }

    public bool IsDeliveredOutcome => OutcomeResult == "delivered";
    public bool IsFailedOutcome => OutcomeResult == "failed";
    partial void OnOutcomeResultChanged(string value)
    {
        OnPropertyChanged(nameof(IsDeliveredOutcome));
        OnPropertyChanged(nameof(IsFailedOutcome));
        OnPropertyChanged(nameof(NeedsFoodDisposition));
        RaiseShortfall();
    }

    /// <summary>A cash-on-delivery order that failed or came back is voided by the server, which then
    /// needs to know what became of its food - prepared (waste) or not (back to stock). A paid order
    /// is not voided here (its money goes back through a refund), so it is not asked.</summary>
    public bool NeedsFoodDisposition =>
        OutcomeResult is "failed" or "returned"
        && OutcomeTarget is { PaymentMethod: "cash", PaymentAlreadySettled: false, AmountToCollectPiastres: > 0 };

    [ObservableProperty] private string? _outcomeStockDisposition;

    public bool IsOutcomeFoodPrepared
    {
        get => OutcomeStockDisposition == "prepared";
        set { if (value) OutcomeStockDisposition = "prepared"; }
    }

    public bool IsOutcomeFoodNotPrepared
    {
        get => OutcomeStockDisposition == "not_prepared";
        set { if (value) OutcomeStockDisposition = "not_prepared"; }
    }

    partial void OnOutcomeStockDispositionChanged(string? value)
    {
        OnPropertyChanged(nameof(IsOutcomeFoodPrepared));
        OnPropertyChanged(nameof(IsOutcomeFoodNotPrepared));
    }

    private string? _outcomeRequestId;

    /// <summary>A plain record rather than a ValueTuple - XAML binds by reflection and a tuple's
    /// element names are compiler sugar, not real CLR properties (same reasoning as CompReasonOption).</summary>
    public sealed record FailureReasonOption(string Code, string Label);

    /// <summary>Workflow redesign (owner, 2026-11-24): the dispatcher's list - codes RecordDeliveryResultHandler
    /// accepts; "other" needs the reason typed (OutcomeNotes), which the server enforces too.</summary>
    public IReadOnlyList<FailureReasonOption> FailureReasons { get; } = new[]
    {
        new FailureReasonOption("customer_unreachable", "العميل ما بيردش"),
        new FailureReasonOption("phone_off", "التليفون مقفول"),
        new FailureReasonOption("customer_refused", "العميل رفض الاستلام"),
        new FailureReasonOption("wrong_address", "العنوان غلط"),
        new FailureReasonOption("other", "سبب تاني (اكتبه)"),
    };

    [RelayCommand]
    private void OpenOutcome(DeliveryDto delivery)
    {
        if (!CanEditAction || !CanDispatch) return;
        OutcomeError = null;
        OutcomeResult = "delivered";
        // Audit 2026-11-24 (#5): what the driver actually has to bring - nothing on a paid order.
        OutcomeCollectedText = CashInput.Format(CollectPiastres(delivery));
        OutcomeReason = "";
        OutcomeNotes = "";
        OutcomeManagerPin = "";
        OutcomeStockDisposition = null;
        _outcomeRequestId = $"res-{Guid.NewGuid():N}";
        OutcomeTarget = delivery;
    }

    [RelayCommand]
    private void CloseOutcome() { if (CanEditAction) OutcomeTarget = null; }

    [RelayCommand]
    private void SetOutcomeResult(string result) { if (CanEditAction) OutcomeResult = result; }

    [RelayCommand]
    private void SetFailureReason(string reason) { if (CanEditAction) OutcomeReason = reason; }

    [RelayCommand]
    private async Task ConfirmOutcomeAsync()
    {
        if (IsBusy || _assignRequest.HasPending || _settleRequest.HasPending || !CanDispatch) return;
        if (_outcomeRequest.Value is { } pending)
        {
            IsBusy = true;
            try { await CompleteOutcomeAsync(pending); }
            finally { IsBusy = false; }
            return;
        }
        if (OutcomeTarget is null) return;

        long? collected = null;
        if (IsDeliveredOutcome)
        {
            if (!CashInput.TryParse(OutcomeCollectedText, out var piastres))
            {
                OutcomeError = "أدخل المبلغ المحصّل بشكل صحيح.";
                return;
            }
            collected = piastres;
        }
        // Workflow redesign (owner, 2026-11-24): "other" carries the reason typed.
        if (OutcomeResult == "failed" && OutcomeReason == "other" && string.IsNullOrWhiteSpace(OutcomeNotes))
        {
            OutcomeError = "اكتب سبب فشل التوصيل.";
            return;
        }
        if (NeedsFoodDisposition && OutcomeStockDisposition is null)
        {
            OutcomeError = "حدد هل تم تحضير الطلب (يُسجَّل هالكًا) أم لم يُحضَّر (يعود للمخزون).";
            return;
        }

        var shortfallNeedsApproval = OutcomeShortfallNeedsApproval;
        if (IsOutcomeShortfall && string.IsNullOrWhiteSpace(OutcomeReason))
        {
            OutcomeError = "المحصّل أقل من المستحق - اكتب السبب.";
            return;
        }
        if (shortfallNeedsApproval && OutcomeManagerPin.Length < 4)
        {
            OutcomeError = "أدخل رمز المدير المخوّل باعتماد التحصيل الأقل من المستحق.";
            return;
        }

        IsBusy = true;
        OutcomeError = null;
        try
        {
            string? approvalId = null, approverUserId = null;
            if (shortfallNeedsApproval)
            {
                // The server checks the PIN and names the approver; the approval is spent by this one result.
                var approval = await _session.Api.IssueManagerApprovalAsync(
                    new Falah.Application.Approvals.IssueManagerApprovalRequest(
                        OutcomeManagerPin, "delivery_settlement_approve", OutcomeReason));
                if (!approval.Success || approval.Value is null)
                {
                    OutcomeError = approval.Error ?? "رمز المدير غير صحيح أو غير مخوّل باعتماد فروق التسليم.";
                    return;
                }
                approvalId = approval.Value.ApprovalId;
                approverUserId = approval.Value.ApproverUserId;
            }

            await CompleteOutcomeAsync(new RecordDeliveryResultRequest(
                _outcomeRequestId!, OutcomeTarget.DeliveryId, OutcomeResult, collected,
                string.IsNullOrWhiteSpace(OutcomeReason) ? null : OutcomeReason,
                string.IsNullOrWhiteSpace(OutcomeNotes) ? null : OutcomeNotes,
                _session.CurrentUser!.Id, approvalId, approverUserId,
                NeedsFoodDisposition ? OutcomeStockDisposition : null));
            OutcomeManagerPin = "";
        }
        finally { IsBusy = false; }
    }

    private async Task CompleteOutcomeAsync(RecordDeliveryResultRequest request)
    {
            OutcomeError = null;
            var result = await _outcomeRequest.SendAsync(request, r => _session.Api.RecordDeliveryResultAsync(r));
            if (!result.Success || result.Value is null)
            {
                OutcomeError = _outcomeRequest.FailureMessage(result.Error);
                return;
            }
            ActionNote = "تم تسجيل نتيجة التوصيل.";
            OutcomeTarget = null;
            _outcomeRequestId = null;
            await LoadAsync();
    }

    // ---------------- Driver account + settlement ----------------

    [ObservableProperty] private DriverDto? _accountDriver;
    [ObservableProperty] private DriverAccountDto? _driverAccount;
    [ObservableProperty] private bool _isLoadingAccount;
    [ObservableProperty] private string _settleActualCashText = "";
    [ObservableProperty] private string _settleReason = "";
    [ObservableProperty] private string _settleManagerPin = "";
    [ObservableProperty] private string? _settleError;
    [ObservableProperty] private string? _settleNote;
    // Owner rule (2026-11-08): the driver keeps the full delivery fee of each completed delivery, so he hands over
    // collected cash less his fees (plus any balance carried from a partial settlement). When that is negative the
    // restaurant owes him and pays it out of the drawer explicitly; nothing is ever clamped to zero.
    [ObservableProperty] private string _settlePaidToDriverText = "0";
    [ObservableProperty] private bool _settleKeepRemainderOpen;
    public bool IsDriverAccountOpen => AccountDriver is not null;

    private static string Egp(long piastres) => CashInput.Display(piastres) + " ج";
    public string SettleCollectedText => DriverAccount is null ? "—" : Egp(DriverAccount.CashCustodyPiastres);
    public string SettleFeesText => DriverAccount is null ? "—" : Egp(DriverAccount.PendingFeesPiastres);
    public string SettleCarriedText => DriverAccount is null ? "—" : DriverAccount.CarriedBalancePiastres switch
    {
        > 0 => $"عليه {Egp(DriverAccount.CarriedBalancePiastres)}", < 0 => $"له {Egp(-DriverAccount.CarriedBalancePiastres)}", _ => Egp(0),
    };
    public string SettleNetLabel => DriverAccount is { NetDuePiastres: < 0 } ? "مستحق للسائق يُصرف له" : "صافي يسلّمه السائق";
    public string SettleNetText => DriverAccount is null ? "—" : Egp(Math.Abs(DriverAccount.NetDuePiastres));
    public bool RestaurantOwesDriver => DriverAccount is { NetDuePiastres: < 0 };
    partial void OnDriverAccountChanged(DriverAccountDto? value)
    {
        foreach (var name in new[] { nameof(SettleCollectedText), nameof(SettleFeesText), nameof(SettleCarriedText),
                     nameof(SettleNetLabel), nameof(SettleNetText), nameof(RestaurantOwesDriver) })
            OnPropertyChanged(name);
    }
    partial void OnAccountDriverChanged(DriverDto? value) => OnPropertyChanged(nameof(IsDriverAccountOpen));

    private string? _settleRequestId;
    private int _accountVersion;

    /// <summary>A settlement DIFFERENCE needs delivery_settlement_approve stacked on top of
    /// delivery_manage - so a dispatcher without it must supply an approving manager's PIN, exactly
    /// like the shift-close discrepancy flow.</summary>
    public bool SettlementNeedsApproval => !_session.HasPermission("delivery_settlement_approve");

    [RelayCommand]
    private async Task OpenDriverAccountAsync(DriverDto driver)
    {
        if (!CanEditAction) return;
        AccountDriver = driver;
        DriverAccount = null;
        SettleError = null;
        SettleNote = null;
        SettleReason = "";
        SettleManagerPin = "";
        _settleRequestId = $"set-{Guid.NewGuid():N}";
        // The statistics panel shares this dialog: default it to today every time it opens, so a
        // period left on "custom" from a previous driver never silently applies to a new one.
        DriverStatistics = null;
        StatisticsError = null;
        SelectedStatsPeriod = StatsPeriods[0];
        await LoadDriverAccountAsync(driver.Id);
        await LoadDriverStatisticsAsync(driver.Id);
    }

    private async Task LoadDriverAccountAsync(string driverId)
    {
        var version = ++_accountVersion;
        IsLoadingAccount = true;
        OnPropertyChanged(nameof(SettlementNeedsApproval));
        try
        {
            var result = await _session.Api.GetDriverAccountAsync(driverId);
            if (version != _accountVersion) return;
            if (!result.Success || result.Value is null)
            {
                SettleError = result.Error ?? "تعذر تحميل حساب السائق.";
                return;
            }
            DriverAccount = result.Value;
            SettleActualCashText = CashInput.Format(Math.Max(0, result.Value.NetDuePiastres));
            SettlePaidToDriverText = CashInput.Format(Math.Max(0, -result.Value.NetDuePiastres));
            SettleKeepRemainderOpen = false;
        }
        finally { if (version == _accountVersion) IsLoadingAccount = false; }
    }

    [RelayCommand]
    private void CloseDriverAccount()
    {
        if (!CanEditAction) return;
        _accountVersion++;
        _statisticsVersion++;
        IsLoadingAccount = false;
        IsLoadingStatistics = false;
        DriverStatistics = null;
        AccountDriver = null;
    }

    [RelayCommand]
    private async Task SettleDriverAsync()
    {
        if (IsBusy || _assignRequest.HasPending || _outcomeRequest.HasPending || !CanDispatch) return;
        if (_settleRequest.Value is { } pending)
        {
            IsBusy = true;
            try { await CompleteSettlementAsync(pending); }
            finally { IsBusy = false; }
            return;
        }
        if (IsLoadingAccount || AccountDriver is null || DriverAccount is null) return;
        if (!CashInput.TryParse(SettleActualCashText, out var actual))
        {
            SettleError = "أدخل المبلغ المسلَّم بشكل صحيح.";
            return;
        }

        if (!CashInput.TryParse(SettlePaidToDriverText, out var paidToDriver) || paidToDriver < 0 || actual < 0)
        {
            SettleError = "أدخل المبالغ بشكل صحيح.";
            return;
        }
        if (actual > 0 && paidToDriver > 0)
        {
            SettleError = "إما أن يسلّم السائق نقدية أو يُصرف له مستحقه - لا الاثنان في نفس التسوية.";
            return;
        }
        if (paidToDriver > Math.Max(0, -DriverAccount.NetDuePiastres))
        {
            SettleError = "لا يمكن صرف أكثر من مستحق السائق.";
            return;
        }

        // A partial settlement carries what is left to the next one; only a final settlement has a difference.
        var difference = SettleKeepRemainderOpen ? 0 : actual - paidToDriver - DriverAccount.NetDuePiastres;
        if (difference != 0 && string.IsNullOrWhiteSpace(SettleReason))
        {
            SettleError = "يوجد فارق في التسليم - سبب الفارق إلزامي.";
            return;
        }

        IsBusy = true;
        SettleError = null;
        try
        {
            string? approvalId = null, approverUserId = null;
            if (difference != 0 && SettlementNeedsApproval)
            {
                if (SettleManagerPin.Length < 4)
                {
                    SettleError = "أدخل رمز المدير المخوّل باعتماد فارق التسليم.";
                    return;
                }
                // One call, not two: the server checks the PIN and decides who approved.
                var approval = await _session.Api.IssueManagerApprovalAsync(
                    new Falah.Application.Approvals.IssueManagerApprovalRequest(
                        SettleManagerPin, "delivery_settlement_approve", SettleReason));
                if (!approval.Success || approval.Value is null)
                {
                    SettleError = approval.Error ?? "رمز المدير غير صحيح أو غير مخوّل باعتماد فارق التسليم.";
                    return;
                }
                approvalId = approval.Value.ApprovalId;
                approverUserId = approval.Value.ApproverUserId;
            }

            await CompleteSettlementAsync(new SettleDriverCustodyRequest(
                _settleRequestId!, AccountDriver.Id, actual,
                string.IsNullOrWhiteSpace(SettleReason) ? null : SettleReason,
                _session.CurrentUser!.Id, approvalId, approverUserId,
                PaidToDriverPiastres: paidToDriver, KeepRemainderOpen: SettleKeepRemainderOpen));
        }
        finally { IsBusy = false; }
    }

    private async Task CompleteSettlementAsync(SettleDriverCustodyRequest request)
    {
            SettleError = null;
            var result = await _settleRequest.SendAsync(request, r => _session.Api.SettleDriverCustodyAsync(r));
            if (!result.Success || result.Value is null)
            {
                SettleError = _settleRequest.FailureMessage(result.Error);
                return;
            }

            var v = result.Value;
            SettleNote = $"تمت التسوية: {v.OrderCount} طلب - أجر توصيل {CashInput.Display(v.FeeEntitlementPiastres)} ج"
                + (v.ActualPiastres > 0 ? $" - استُلم {CashInput.Display(v.ActualPiastres)} ج" : "")
                + (v.PaidToDriverPiastres > 0 ? $" - صُرف للسائق {CashInput.Display(v.PaidToDriverPiastres)} ج" : "")
                + (v.CarriedOutPiastres != 0 ? $" - مرحّل {CashInput.Display(v.CarriedOutPiastres)} ج" : "")
                + (v.DifferencePiastres != 0 ? $" - فارق {CashInput.Display(v.DifferencePiastres)} ج" : "") + ".";
            _settleRequestId = $"set-{Guid.NewGuid():N}";
            DriverAccount = null;
            await LoadDriverAccountAsync(request.DriverId);
            // The statistics panel in the same dialog states current custody and the settlement count; left as
            // loaded at opening it still showed the custody this settlement just cleared.
            await LoadDriverStatisticsAsync(request.DriverId);
            await LoadAsync();
    }
}

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
    public bool CanEditAction => !IsBusy && !_assignRequest.HasPending && !_outcomeRequest.HasPending && !_settleRequest.HasPending;
    partial void OnIsBusyChanged(bool value)
    {
        OnPropertyChanged(nameof(CanEditAction));
        _session.SetManagementOperationPending(this, !CanEditAction);
    }

    public DeliveryViewModel(AppSession session)
    {
        _session = session;
        Directory = new DeliveryDirectoryViewModel(session);
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
            Delivery.AmountToCollectPiastres > 0 ? CashInput.Format(Delivery.AmountToCollectPiastres) + " ج" : "—";

        /// <summary>Three states, not two. "Paid" and "collect on delivery" are both settled
        /// questions; a transfer nobody has confirmed is neither, and handing that order to a driver
        /// without saying so is how an order goes out with nothing to collect and nothing collected.</summary>
        public string PaymentStateLabel =>
            Delivery.ElectronicPaymentStatus == "Pending" ? "تحويل لم يُؤكَّد"
            : Delivery.PaymentAlreadySettled ? "مدفوع"
            : "تحصيل عند التسليم";

        public bool IsPaymentUnconfirmed => Delivery.ElectronicPaymentStatus == "Pending";

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

    // ---------------- Assign to a driver ----------------

    [ObservableProperty] private DeliveryDto? _assignTarget;
    [ObservableProperty] private DriverDto? _assignDriver;
    [ObservableProperty] private string? _assignError;
    public bool IsAssignOpen => AssignTarget is not null;
    partial void OnAssignTargetChanged(DeliveryDto? value) => OnPropertyChanged(nameof(IsAssignOpen));

    /// <summary>Minted once per assignment dialog and reused by a retry, so a lost response cannot
    /// become a second assignment - the server keys its own idempotency on this value.</summary>
    private string? _assignRequestId;

    [RelayCommand]
    private void OpenAssign(DeliveryDto delivery)
    {
        if (!CanEditAction || !CanDispatch) return;
        AssignError = null;
        AssignDriver = null;
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
            await CompleteAssignAsync(new AssignDeliveryRequest(
                _assignRequestId!, AssignTarget.DeliveryId, AssignDriver.Id, _session.CurrentUser!.Id, null, null));
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
    [ObservableProperty] private string _outcomeReason = "";
    [ObservableProperty] private string _outcomeNotes = "";
    [ObservableProperty] private string? _outcomeError;
    public bool IsOutcomeOpen => OutcomeTarget is not null;
    partial void OnOutcomeTargetChanged(DeliveryDto? value) => OnPropertyChanged(nameof(IsOutcomeOpen));

    public bool IsDeliveredOutcome => OutcomeResult == "delivered";
    public bool IsFailedOutcome => OutcomeResult == "failed";
    partial void OnOutcomeResultChanged(string value)
    {
        OnPropertyChanged(nameof(IsDeliveredOutcome));
        OnPropertyChanged(nameof(IsFailedOutcome));
    }

    private string? _outcomeRequestId;

    /// <summary>A plain record rather than a ValueTuple - XAML binds by reflection and a tuple's
    /// element names are compiler sugar, not real CLR properties (same reasoning as CompReasonOption).</summary>
    public sealed record FailureReasonOption(string Code, string Label);

    /// <summary>Exactly the six codes RecordDeliveryResultHandler.ValidFailureReasons accepts - a
    /// failure reason outside this set is rejected server-side, so the UI offers the real set rather
    /// than a free-text box that would fail on submit.</summary>
    public IReadOnlyList<FailureReasonOption> FailureReasons { get; } = new[]
    {
        new FailureReasonOption("customer_unreachable", "العميل لا يرد"),
        new FailureReasonOption("customer_refused", "العميل رفض الاستلام"),
        new FailureReasonOption("wrong_address", "عنوان خاطئ"),
        new FailureReasonOption("order_problem", "مشكلة في الطلب"),
        new FailureReasonOption("driver_problem", "مشكلة لدى السائق"),
        new FailureReasonOption("other", "سبب آخر"),
    };

    [RelayCommand]
    private void OpenOutcome(DeliveryDto delivery)
    {
        if (!CanEditAction || !CanDispatch) return;
        OutcomeError = null;
        OutcomeResult = "delivered";
        OutcomeCollectedText = CashInput.Format(delivery.AmountToCollectPiastres);
        OutcomeReason = "";
        OutcomeNotes = "";
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

        IsBusy = true;
        OutcomeError = null;
        try
        {
            await CompleteOutcomeAsync(new RecordDeliveryResultRequest(
                _outcomeRequestId!, OutcomeTarget.DeliveryId, OutcomeResult, collected,
                string.IsNullOrWhiteSpace(OutcomeReason) ? null : OutcomeReason,
                string.IsNullOrWhiteSpace(OutcomeNotes) ? null : OutcomeNotes,
                _session.CurrentUser!.Id, null, null));
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
    public bool IsDriverAccountOpen => AccountDriver is not null;
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
        await LoadDriverAccountAsync(driver.Id);
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
            SettleActualCashText = CashInput.Format(result.Value.CashCustodyPiastres);
        }
        finally { if (version == _accountVersion) IsLoadingAccount = false; }
    }

    [RelayCommand]
    private void CloseDriverAccount()
    {
        if (!CanEditAction) return;
        _accountVersion++;
        IsLoadingAccount = false;
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

        var difference = actual - DriverAccount.CashCustodyPiastres;
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
                _session.CurrentUser!.Id, approvalId, approverUserId));
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

            SettleNote = $"تمت التسوية: {result.Value.OrderCount} طلب، فارق {CashInput.Format(result.Value.DifferencePiastres)} ج.";
            _settleRequestId = $"set-{Guid.NewGuid():N}";
            DriverAccount = null;
            await LoadDriverAccountAsync(request.DriverId);
            await LoadAsync();
    }
}

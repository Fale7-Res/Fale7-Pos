using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Workflow redesign (owner decision 2026-11-24): the order queue - every delivery and pickup at once, grouped by the
/// stage the SERVER says it is in (GET /api/orders/queue), each card offering the one next step that makes sense now
/// and, when allowed, Cancel. Nothing here decides a state: the stage, the next action, whether a cancel is allowed and
/// whether it needs a manager all come from the server's records; the existing delivery dialogs (assign, result, driver
/// settlement) do the work they always did.
/// </summary>
public partial class DeliveryViewModel
{
    /// <summary>One order on the queue.</summary>
    public sealed class QueueCard
    {
        public required QueueOrderDto Order { get; init; }
        /// <summary>The delivery's own row from the dispatch board, for the dialogs that take it (delivery only).</summary>
        public DeliveryDto? Delivery { get; init; }

        public string OrderId => Order.OrderId;
        public string OrderNumber => Order.OrderNumber;
        public bool IsDelivery => Order.OrderType == "delivery";
        public string CustomerDisplay => Order.CustomerName is { Length: > 0 } n ? n : "عميل غير مسجّل";
        public string PhoneDisplay => Order.CustomerPhone ?? "";
        // Audit 2026-11-25 U26: the composed address already names the area ("المنطقة ..."), so "area · address" printed it
        // twice; the area alone is kept only when there is no address.
        public string AddressDisplay => Order.DeliveryAddress is { Length: > 0 } address
            ? (Order.DeliveryArea is { Length: > 0 } area && !address.Contains(area) ? $"{area} · {address}" : address)
            : Order.DeliveryArea ?? "";
        public bool HasAddress => AddressDisplay.Length > 0;
        public string AmountDisplay => CashInput.Display(Order.TotalPiastres) + " ج";
        public string StageLabel => Order.StageLabel;
        public string PaymentLabel => Order.PaymentState switch
        {
            "cash_on_delivery" => "كاش مع السائق",
            "pay_at_counter" => "يدفع وهو بيستلم",
            "pending_transfer" => "تحويل لسه ما اتأكدش",
            _ => "مدفوع",
        };
        public bool IsPaymentWarning => Order.PaymentState == "pending_transfer";
        public string? DriverName => Order.DriverName;
        public bool HasDriver => !string.IsNullOrEmpty(Order.DriverName);
        public string? Attention => Order.Attention;
        public bool HasAttention => !string.IsNullOrEmpty(Order.Attention);
        public bool IsAttentionCritical => Order.NeedsRefund;
        public string? NextActionLabel => Order.NextActionLabel;
        public bool HasNextAction => Order.NextAction is not null;
        public bool CanCancel => Order.CanCancel;
        public string? SecondaryActionLabel => Order.SecondaryActionLabel;
        public bool HasSecondaryAction => Order.SecondaryAction is not null;
        public string CancelLabel => Order.CancelNeedsAdmin ? "إلغاء (رمز المدير)" : "إلغاء الطلب";
        public bool CanReassign { get; init; }

        public string ElapsedDisplay
        {
            get
            {
                var elapsed = DateTimeOffset.UtcNow - Order.CreatedAtUtc;
                if (elapsed < TimeSpan.Zero) return "";
                var minutes = (int)elapsed.TotalMinutes;
                return minutes < 60 ? $"{minutes} د" : $"{minutes / 60} س {minutes % 60} د";
            }
        }
    }

    /// <summary>A stage column of the queue, in the order of an order's life.</summary>
    public sealed class QueueStage
    {
        public required string Key { get; init; }
        public required string Title { get; init; }
        public ObservableCollection<QueueCard> Cards { get; } = new();
        public string Header => $"{Title} ({Cards.Count})";
    }

    private static readonly (string Key, string Title)[] DeliveryStageOrder =
    {
        ("new", "جديد"), ("preparing", "قيد التحضير"), ("ready", "جاهز للتسليم"), ("out", "مع السائق"),
        ("delivered", "اتسلّم للعميل"), ("failed", "فشل التوصيل"), ("returned", "رجع للمطعم"),
    };

    private static readonly (string Key, string Title)[] PickupStageOrder =
    {
        ("new", "جديد"), ("preparing", "قيد التحضير - مستني العميل"), ("completed", "اتسلّم النهارده"),
    };

    public ObservableCollection<QueueStage> DeliveryStages { get; } = new();
    public ObservableCollection<QueueStage> PickupStages { get; } = new();
    [ObservableProperty] private string? _queueSummary;
    public bool HasNoPickups => !IsLoading && LoadError is null && PickupStages.Count == 0;

    private async Task LoadQueueAsync()
    {
        var queue = await _session.Api.GetOrderQueueAsync();
        if (!queue.Success || queue.Value is null) { LoadError = queue.Error ?? "تعذر تحميل طابور الطلبات."; return; }
        var boardById = Board.ToDictionary(r => r.DeliveryId, r => r.Delivery);

        Fill(DeliveryStages, DeliveryStageOrder, queue.Value.Where(q => q.OrderType == "delivery")
            .Select(q => new QueueCard
            {
                Order = q, Delivery = q.DeliveryId is { } id ? boardById.GetValueOrDefault(id) : null,
                CanReassign = CanDispatch && q.Stage == "out" && q.DriverId is not null && q.DeliveryId is not null,
            }));
        Fill(PickupStages, PickupStageOrder, queue.Value.Where(q => q.OrderType == "pickup").Select(q => new QueueCard { Order = q }));

        var deliveries = queue.Value.Count(q => q.OrderType == "delivery");
        var pickups = queue.Value.Count(q => q.OrderType == "pickup" && q.Stage != "completed");
        var refunds = queue.Value.Count(q => q.NeedsRefund);
        QueueSummary = $"{deliveries} توصيل · {pickups} استلام مفتوح" + (refunds > 0 ? $" · {refunds} محتاج مرتجع" : "");
        OnPropertyChanged(nameof(HasNoPickups));
    }

    private static void Fill(ObservableCollection<QueueStage> target, (string Key, string Title)[] order, IEnumerable<QueueCard> cards)
    {
        target.Clear();
        var byStage = cards.GroupBy(c => c.Order.Stage).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var (key, title) in order)
        {
            if (!byStage.TryGetValue(key, out var list) || list.Count == 0) continue;
            var stage = new QueueStage { Key = key, Title = title };
            foreach (var card in list) stage.Cards.Add(card);
            target.Add(stage);
        }
    }

    // ---------------- the one next step ----------------

    /// <summary>A pickup's handover press, minted once and reused by a retry until it is answered.</summary>
    private readonly Dictionary<string, string> _handoverRequestIds = new();

    [RelayCommand]
    private Task RunNextActionAsync(QueueCard card) => card.Order.NextAction is { } action ? RunActionAsync(card, action) : Task.CompletedTask;

    /// <summary>The optional step beside the main one (a kitchen paper that never reached the printer).</summary>
    [RelayCommand]
    private Task RunSecondaryActionAsync(QueueCard card) => card.Order.SecondaryAction is { } action ? RunActionAsync(card, action) : Task.CompletedTask;

    private async Task RunActionAsync(QueueCard card, string action)
    {
        if (!CanEditAction) return;
        ActionError = null;
        ActionNote = null;
        switch (action)
        {
            case "mark_ready" when card.Delivery is { } d: await MarkReadyAsync(d); return;
            case "assign" when card.Delivery is { } d: OpenAssign(d); return;
            case "record_result" when card.Delivery is { } d: OpenOutcome(d); return;
            case "collect": OpenCollect(card); return;
        }

        IsBusy = true;
        try
        {
            switch (action)
            {
                case "print_kitchen":
                {
                    var r = await _session.Api.SendKitchenInvoiceAsync(new SendKitchenInvoiceRequest(
                        $"ki-{Guid.NewGuid():N}", card.OrderId, _session.CurrentUser!.Id, "Receipt"));
                    if (!r.Success) { ActionError = r.Error ?? "تعذر إرسال ورقة المطبخ."; return; }
                    ActionNote = $"ورقة المطبخ للطلب {card.OrderNumber} اتبعتت للطابعة.";
                    break;
                }
                case "confirm_transfer":
                {
                    var r = await _session.Api.ConfirmElectronicPaymentAsync(new ConfirmElectronicPaymentRequest(card.OrderId, _session.CurrentUser!.Id));
                    if (!r.Success) { ActionError = r.Error ?? "تعذر تأكيد التحويل."; return; }
                    ActionNote = $"اتأكد تحويل الطلب {card.OrderNumber}.";
                    break;
                }
                case "hand_over":
                {
                    if (!_handoverRequestIds.TryGetValue(card.OrderId, out var requestId))
                        _handoverRequestIds[card.OrderId] = requestId = $"ho-{Guid.NewGuid():N}";
                    var r = await _session.Api.HandOverPickupAsync(new HandOverPickupRequest(requestId, card.OrderId, _session.CurrentUser!.Id));
                    if (!r.Success || r.Value is null)
                    {
                        if (!r.OutcomeUnknown) _handoverRequestIds.Remove(card.OrderId);
                        ActionError = r.Error ?? "تعذر تسليم الطلب.";
                        return;
                    }
                    _handoverRequestIds.Remove(card.OrderId);
                    ActionNote = $"اتسلّم الطلب {card.OrderNumber} للعميل" + (r.Value.PrintJobsQueued ? " وإيصاله اتبعت للطباعة." : " - الإيصال ما اتبعتش، اطبعه من البحث عن طلب.");
                    break;
                }
                default: return;
            }
        }
        finally { IsBusy = false; }
        await LoadAsync();
    }

    // ---------------- Cancel (owner, 2026-11-24): unpaid only; a manager once the kitchen has its paper ----------------

    [ObservableProperty] private QueueCard? _queueCancelTarget;
    [ObservableProperty] private string _queueCancelReason = "";
    [ObservableProperty] private string? _queueCancelDisposition;
    [ObservableProperty] private string _queueCancelManagerPin = "";
    [ObservableProperty] private string? _queueCancelError;
    private string? _queueCancelRequestId;

    public bool IsQueueCancelOpen => QueueCancelTarget is not null;
    public bool QueueCancelNeedsAdmin => QueueCancelTarget?.Order.CancelNeedsAdmin == true;
    /// <summary>A manager (or a holder of delivery_override for a delivery) is the authority themselves.</summary>
    public bool QueueCancelAsksForPin => QueueCancelNeedsAdmin && !IsManagerOnScreen(QueueCancelTarget);
    public string QueueCancelTitle => QueueCancelTarget is { } t ? $"إلغاء الطلب {t.OrderNumber}؟" : "";
    public string QueueCancelEffect => QueueCancelTarget is null ? ""
        : !QueueCancelNeedsAdmin ? "ورقة المطبخ ما اتطبعتش: الطلب يتلغي وكل الخامات ترجع للمخزون."
        : QueueCancelDisposition == "prepared" ? "اتحضّر: الخامات ما ترجعش للمخزون وتتسجل هالك."
        : QueueCancelDisposition == "not_prepared" ? "ما اتحضّرش: الخامات ترجع للمخزون."
        : "ورقة المطبخ اتطبعت: محتاج رمز المدير، وبعدين حدد هل الأكل اتحضّر.";
    public bool IsQueueCancelPrepared { get => QueueCancelDisposition == "prepared"; set { if (value) QueueCancelDisposition = "prepared"; } }
    public bool IsQueueCancelNotPrepared { get => QueueCancelDisposition == "not_prepared"; set { if (value) QueueCancelDisposition = "not_prepared"; } }

    partial void OnQueueCancelTargetChanged(QueueCard? value)
    {
        foreach (var n in new[] { nameof(IsQueueCancelOpen), nameof(QueueCancelNeedsAdmin), nameof(QueueCancelAsksForPin), nameof(QueueCancelTitle), nameof(QueueCancelEffect) })
            OnPropertyChanged(n);
    }
    partial void OnQueueCancelDispositionChanged(string? value)
    {
        OnPropertyChanged(nameof(IsQueueCancelPrepared)); OnPropertyChanged(nameof(IsQueueCancelNotPrepared)); OnPropertyChanged(nameof(QueueCancelEffect));
    }

    private bool IsManagerOnScreen(QueueCard? card) =>
        _session.CurrentUser?.Role is "manager" or "owner"
        || (card?.IsDelivery == true && card.Order.PaymentState == "cash_on_delivery" && _session.HasPermission("delivery_override"));

    [RelayCommand]
    private void OpenQueueCancel(QueueCard card)
    {
        if (!CanEditAction || !card.CanCancel) return;
        QueueCancelReason = "";
        QueueCancelDisposition = null;
        QueueCancelManagerPin = "";
        QueueCancelError = null;
        _queueCancelRequestId = $"qc-{Guid.NewGuid():N}";
        QueueCancelTarget = card;
    }

    [RelayCommand]
    private void CloseQueueCancel() { if (CanEditAction) QueueCancelTarget = null; }

    [RelayCommand]
    private async Task ConfirmQueueCancelAsync()
    {
        if (QueueCancelTarget is not { } card || !CanEditAction) return;
        if (string.IsNullOrWhiteSpace(QueueCancelReason)) { QueueCancelError = "اكتب سبب الإلغاء."; return; }
        if (QueueCancelNeedsAdmin && QueueCancelDisposition is null) { QueueCancelError = "حدد: هل الأكل اتحضّر ولا لأ؟"; return; }
        if (QueueCancelAsksForPin && QueueCancelManagerPin.Trim().Length < 4) { QueueCancelError = "الإلغاء بعد ورقة المطبخ محتاج رمز المدير."; return; }
        IsBusy = true;
        QueueCancelError = null;
        try
        {
            var reason = QueueCancelReason.Trim();
            var pendingTransfer = card.Order.PaymentState == "pending_transfer";
            var action = card.IsDelivery ? "delivery_override" : "pickup_cancel";
            string? approvalId = null, approverId = null;
            if (QueueCancelAsksForPin)
            {
                var approval = await _session.Api.IssueManagerApprovalAsync(
                    new Falah.Application.Approvals.IssueManagerApprovalRequest(QueueCancelManagerPin.Trim(), action, reason));
                if (!approval.Success || approval.Value is null) { QueueCancelError = approval.Error ?? "رمز المدير غير صحيح."; return; }
                (approvalId, approverId) = (approval.Value.ApprovalId, approval.Value.ApproverUserId);
            }
            var disposition = QueueCancelNeedsAdmin ? QueueCancelDisposition : "not_prepared";
            string? error;
            if (pendingTransfer)
            {
                var r = await _session.Api.CancelPendingElectronicPaymentAsync(new CancelPendingElectronicPaymentRequest(
                    card.OrderId, reason, _session.CurrentUser!.Id, disposition, approvalId, approverId));
                error = r.Success ? null : r.Error ?? "تعذر إلغاء الطلب.";
            }
            else if (card.IsDelivery)
            {
                var r = await _session.Api.CancelUndispatchedDeliveryAsync(new CancelUndispatchedDeliveryRequest(
                    _queueCancelRequestId!, card.OrderId, reason, _session.CurrentUser!.Id, disposition, approvalId, approverId));
                error = r.Success ? null : r.Error ?? "تعذر إلغاء الطلب.";
            }
            else
            {
                var r = await _session.Api.CancelUncollectedPickupAsync(new CancelUncollectedPickupRequest(
                    _queueCancelRequestId!, card.OrderId, reason, disposition!, _session.CurrentUser!.Id, approvalId, approverId));
                error = r.Success ? null : r.Error ?? "تعذر إلغاء الطلب.";
            }
            if (error is not null) { QueueCancelError = error; return; }
            ActionNote = $"اتلغى الطلب {card.OrderNumber}.";
            QueueCancelTarget = null;
        }
        finally { IsBusy = false; }
        await LoadAsync();
    }

    // ---------------- A pickup's customer pays and takes the bag (one step) ----------------

    private readonly RetainedRequest<CollectPickupPaymentRequest> _collectRequest = new("queue.collect");
    [ObservableProperty] private QueueCard? _collectTarget;
    [ObservableProperty] private string _collectMethod = "cash";
    [ObservableProperty] private string _collectTenderText = "";
    [ObservableProperty] private string? _collectError;
    private string? _collectRequestId;

    public bool IsCollectOpen => CollectTarget is not null;
    public bool IsCollectCash => CollectMethod == "cash";
    public string CollectTitle => CollectTarget is { } t ? $"الطلب {t.OrderNumber} - {t.CustomerDisplay} - {t.AmountDisplay}" : "";
    public string CollectChangeDisplay => CollectTarget is { } t && IsCollectCash && CashInput.TryParse(CollectTenderText, out var paid) && paid >= t.Order.TotalPiastres
        ? $"الباقي للعميل {CashInput.Display(paid - t.Order.TotalPiastres)} ج" : "";
    partial void OnCollectTargetChanged(QueueCard? value) { OnPropertyChanged(nameof(IsCollectOpen)); OnPropertyChanged(nameof(CollectTitle)); OnPropertyChanged(nameof(CollectChangeDisplay)); }
    partial void OnCollectMethodChanged(string value) { OnPropertyChanged(nameof(IsCollectCash)); OnPropertyChanged(nameof(CollectChangeDisplay)); }
    partial void OnCollectTenderTextChanged(string value) => OnPropertyChanged(nameof(CollectChangeDisplay));

    private void OpenCollect(QueueCard card)
    {
        if (!CanEditAction) return;
        if (_session.ShiftId is null) { ActionError = "افتح وردية على الجهاز ده الأول علشان تحصّل من العميل."; return; }
        CollectMethod = "cash";
        CollectTenderText = CashInput.Format(card.Order.TotalPiastres);
        CollectError = null;
        _collectRequestId = $"col-{Guid.NewGuid():N}";
        CollectTarget = card;
    }

    [RelayCommand]
    private void SetCollectMethod(string method) { if (CanEditAction) CollectMethod = method; }

    [RelayCommand]
    private void CloseCollect()
    {
        if (!CanEditAction) return;
        if (_collectRequest.HasPending) { CollectError = "لم تُحسم نتيجة التحصيل؛ أعد المحاولة للتحقق قبل الإغلاق."; return; }
        CollectTarget = null;
    }

    [RelayCommand]
    private async Task ConfirmCollectAsync()
    {
        if (IsBusy) return;
        if (_collectRequest.Value is { } pending) { IsBusy = true; try { await CompleteCollectAsync(pending); } finally { IsBusy = false; } return; }
        if (CollectTarget is not { } card) return;
        long paid = card.Order.TotalPiastres;
        if (IsCollectCash && (!CashInput.TryParse(CollectTenderText, out paid) || paid < card.Order.TotalPiastres))
        { CollectError = "المبلغ المستلم أقل من قيمة الطلب."; return; }
        IsBusy = true;
        try
        {
            await CompleteCollectAsync(new CollectPickupPaymentRequest(_collectRequestId!, card.OrderId, _session.CurrentUser!.Id,
                _session.ShiftId!, CollectMethod, paid));
        }
        finally { IsBusy = false; }
    }

    private async Task CompleteCollectAsync(CollectPickupPaymentRequest request)
    {
        CollectError = null;
        var result = await _collectRequest.SendAsync(request, r => _session.Api.CollectPickupAsync(r));
        if (!result.Success || result.Value is null) { CollectError = _collectRequest.FailureMessage(result.Error); return; }
        var change = result.Value.ChangeDuePiastres > 0 ? $" - الباقي للعميل {CashInput.Display(result.Value.ChangeDuePiastres)} ج" : "";
        ActionNote = result.Value.ElectronicPaymentStatus == "pending"
            ? $"اتسلّم الطلب {result.Value.OrderNumber} واتسجل التحويل؛ أكّده لما يوصل."
            : $"اتحصّل واتسلّم الطلب {result.Value.OrderNumber}{change}.";
        CollectTarget = null;
        await LoadAsync();
    }
}

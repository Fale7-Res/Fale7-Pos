using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Orders;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

public partial class CashierViewModel
{
    [ObservableProperty] private bool _isCheckoutOpen;
    [ObservableProperty] private string _walletProvider = "";
    private bool _replaceTenderOnNextKey;
    partial void OnAmountPaidTextChanged(string value) { _replaceTenderOnNextKey = false; OnPropertyChanged(nameof(ChangeDuePiastres)); }
    [ObservableProperty] private string _paymentMethod = "cash";
    [ObservableProperty] private string _amountPaidText = string.Empty;
    [ObservableProperty] private string? _checkoutError;
    [ObservableProperty] private bool _isProcessingCheckout;

    // ---------------- Manual discount (gated on "orders_discount") ----------------

    [ObservableProperty] private bool _isDiscountEnabled;
    [ObservableProperty] private string _discountAmountText = "0";
    [ObservableProperty] private string _discountManagerPin = "";
    public bool DiscountNeedsApproval => !_session.HasPermission("orders_discount");
    public long DiscountAmountPiastres =>
        IsDiscountEnabled && CashInput.TryParse(DiscountAmountText, out var v) ? v : 0;

    partial void OnIsDiscountEnabledChanged(bool value)
    {
        if (!value) { DiscountAmountText = "0"; DiscountManagerPin = ""; }
        OnPropertyChanged(nameof(DiscountAmountPiastres));
        OnPropertyChanged(nameof(GrandTotalPiastres));
        OnPropertyChanged(nameof(GrandTotalDisplay));
        NotifyTotals();
    }

    partial void OnDiscountAmountTextChanged(string value)
    {
        OnPropertyChanged(nameof(DiscountAmountPiastres));
        OnPropertyChanged(nameof(GrandTotalPiastres));
        OnPropertyChanged(nameof(GrandTotalDisplay));
        NotifyTotals();
    }

    // ---------------- Comp / hospitality (gated on "orders_comp") ----------------

    [ObservableProperty] private bool _isCompEnabled;
    [ObservableProperty] private string _compAmountText = "0";
    [ObservableProperty] private string _compReasonCode = "hospitality";
    [ObservableProperty] private string _compReasonText = "";
    [ObservableProperty] private string _compManagerPin = "";
    // Checkout requires approval identifiers for every positive comp, including privileged users.
    public bool CompNeedsApproval => true;
    public long CompAmountPiastres =>
        IsCompEnabled && CashInput.TryParse(CompAmountText, out var v) ? v : 0;

    /// <summary>A plain record, not a ValueTuple - XAML's DisplayMemberPath/SelectedValuePath bind by
    /// reflection at runtime, and a named ValueTuple's element names ("Code"/"Label") are C# compiler
    /// sugar only, not real CLR property names (the actual members are Item1/Item2), so a tuple here
    /// would silently fail to bind. Matches the reference app's own compReasonCode taxonomy - see
    /// CheckoutOrderRequest's doc comment on CompReasonCode for the exact five codes the server
    /// accepts.</summary>
    public sealed record CompReasonOption(string Code, string Label);

    public IReadOnlyList<CompReasonOption> CompReasonCodes { get; } = new[]
    {
        new CompReasonOption("hospitality", "ضيافة"), new CompReasonOption("complaint", "شكوى عميل"),
        new CompReasonOption("kitchen_error", "خطأ مطبخ"), new CompReasonOption("staff_meal", "وجبة موظف"),
        new CompReasonOption("owner_guest", "ضيف صاحب المحل"),
    };

    [RelayCommand]
    private void SetCompReasonCode(string code) => CompReasonCode = code;

    partial void OnIsCompEnabledChanged(bool value)
    {
        if (!value) { CompAmountText = "0"; CompReasonText = ""; CompManagerPin = ""; }
        OnPropertyChanged(nameof(CompAmountPiastres));
        OnPropertyChanged(nameof(GrandTotalPiastres));
        OnPropertyChanged(nameof(GrandTotalDisplay));
        NotifyTotals();
    }

    partial void OnCompAmountTextChanged(string value)
    {
        OnPropertyChanged(nameof(CompAmountPiastres));
        OnPropertyChanged(nameof(GrandTotalPiastres));
        OnPropertyChanged(nameof(GrandTotalDisplay));
        NotifyTotals();
    }

    // ---------------- Split payment ----------------

    [ObservableProperty] private bool _isSplitPaymentEnabled;
    [ObservableProperty] private string _splitMethod1 = "cash";
    [ObservableProperty] private string _splitAmount1Text = "";
    [ObservableProperty] private string _splitMethod2 = "card";
    [ObservableProperty] private string _splitAmount2Text = "";

    [RelayCommand]
    private void SetSplitMethod1(string method) => SplitMethod1 = method;

    [RelayCommand]
    private void SetSplitMethod2(string method) => SplitMethod2 = method;

    // ---------------- Pending electronic payment confirm/cancel ----------------

    [ObservableProperty] private bool _isConfirmingElectronicPayment;
    [ObservableProperty] private bool _isCancellingElectronicPayment;
    [ObservableProperty] private string _cancelPendingReason = "";
    [ObservableProperty] private string? _pendingPaymentActionError;

    public bool IsElectronicPaymentPending => LastCheckoutResult?.ElectronicPaymentStatus == "pending";
    private readonly RetainedRequest<ConfirmElectronicPaymentRequest> _pendingElectronicConfirm = new("cashier.electronic-confirm");
    private readonly RetainedRequest<CancelPendingElectronicPaymentRequest> _pendingElectronicCancel = new("cashier.electronic-cancel");
    public bool CanConfirmPendingPayment => !IsOperationBusy && !_pendingElectronicCancel.HasPending && IsElectronicPaymentPending &&
        (_pendingElectronicConfirm.HasPending || CanEditOperation);
    public bool CanCancelPendingPayment => !IsOperationBusy && !_pendingElectronicConfirm.HasPending && IsElectronicPaymentPending &&
        (_pendingElectronicCancel.HasPending || CanEditOperation);

    [RelayCommand]
    private async Task ConfirmElectronicPaymentAsync()
    {
        if (!CanConfirmPendingPayment) return;
        if (LastCheckoutResult is null) return;
        IsConfirmingElectronicPayment = true;
        PendingPaymentActionError = null;
        try
        {
            var request = _pendingElectronicConfirm.Value ?? new ConfirmElectronicPaymentRequest(LastCheckoutResult.OrderId, _session.CurrentUser!.Id);
            var result = await _pendingElectronicConfirm.SendAsync(request, r => _session.Api.ConfirmElectronicPaymentAsync(r));
            if (!result.Success || result.Value is null)
            {
                PendingPaymentActionError = _pendingElectronicConfirm.FailureMessage(result.Error);
                return;
            }
            LastCheckoutResult = LastCheckoutResult with { ElectronicPaymentStatus = result.Value.ElectronicPaymentStatus, IsPaid = result.Value.IsPaid };
            OnPropertyChanged(nameof(IsElectronicPaymentPending));
        }
        finally
        {
            IsConfirmingElectronicPayment = false;
        }
    }

    [RelayCommand]
    private async Task CancelPendingElectronicPaymentAsync()
    {
        if (!CanCancelPendingPayment) return;
        if (LastCheckoutResult is null) return;
        if (!_pendingElectronicCancel.HasPending && string.IsNullOrWhiteSpace(CancelPendingReason))
        {
            PendingPaymentActionError = "أدخل سبب الإلغاء.";
            return;
        }
        IsCancellingElectronicPayment = true;
        PendingPaymentActionError = null;
        try
        {
            var request = _pendingElectronicCancel.Value ?? new CancelPendingElectronicPaymentRequest(LastCheckoutResult.OrderId, CancelPendingReason, _session.CurrentUser!.Id);
            var result = await _pendingElectronicCancel.SendAsync(request, r => _session.Api.CancelPendingElectronicPaymentAsync(r));
            if (!result.Success || result.Value is null)
            {
                PendingPaymentActionError = _pendingElectronicCancel.FailureMessage(result.Error);
                return;
            }
            LastCheckoutResult = null;
            CancelPendingReason = "";
        }
        finally
        {
            IsCancellingElectronicPayment = false;
        }
    }

    /// <summary>Minted once when the payment screen opens and deliberately never rotated by a
    /// retry - a same-task double click or a network retry of the SAME attempt must reuse this
    /// exact value so the server's idempotency check collapses them into one order. Only clearing
    /// the cart (a genuinely new order) mints a new one.</summary>
    private string? CurrentCheckoutRequestId;

    [ObservableProperty] private CheckoutOrderResult? _lastCheckoutResult;
    public bool HasCheckoutResult => LastCheckoutResult is not null;
    partial void OnLastCheckoutResultChanged(CheckoutOrderResult? value)
    {
        OnPropertyChanged(nameof(HasCheckoutResult));
        OnPropertyChanged(nameof(CheckoutTitle));
        OnPropertyChanged(nameof(CheckoutSubtitle));
        OnPropertyChanged(nameof(IsElectronicPaymentPending));
        OnPropertyChanged(nameof(CanConfirmPendingPayment));
        OnPropertyChanged(nameof(CanCancelPendingPayment));
        OnPropertyChanged(nameof(HasPrintWarning));
        NotifyCheckoutAvailability();
    }

    public long ChangeDuePiastres
    {
        get
        {
            if (!CashInput.TryParse(AmountPaidText, out var paidPiastresInput)) return 0;
            var diff = paidPiastresInput - GrandTotalPiastres;
            return diff > 0 ? diff : 0;
        }
    }

    /// <summary>Whether the primary action can actually do anything. The command already refuses an
    /// empty cart, but a full-strength green button that refuses on tap teaches an operator to
    /// distrust the button - so the guard is visible before it is enforced.</summary>
    public bool CanOpenCheckout =>
        CanEditOperation && Cart.Count > 0 && !IsLoadingMenu && MenuError is null && !HasCheckoutResult;

    /// <summary>Why the primary action is unavailable, in the operator's words. Null when it is
    /// available - the line disappears rather than reassuring anyone that nothing is wrong.</summary>
    public string? CheckoutBlockedReason =>
        !CanEditOperation ? "أكمل التحقق من العملية الحالية أولًا"
        : HasCheckoutResult ? "ابدأ طلبًا جديدًا أولًا"
        : IsLoadingMenu ? "جاري تحميل القائمة والتسعير…"
        : MenuError is not null ? "حدّث القائمة والتسعير قبل الدفع"
        : Cart.Count == 0 ? "أضف صنفًا واحدًا على الأقل"
        : null;

    public void NotifyCheckoutAvailability()
    {
        OnPropertyChanged(nameof(CanOpenCheckout));
        OnPropertyChanged(nameof(CheckoutBlockedReason));
    }

    [RelayCommand]
    private void OpenCheckout()
    {
        if (!CanEditOperation) return;
        if (Cart.Count == 0) { CheckoutError = "السلة فارغة."; return; }
        if (IsLoadingMenu || MenuError is not null) { CheckoutError = "حدّث القائمة والتسعير قبل الدفع."; return; }
        if (IsDelivery && (string.IsNullOrWhiteSpace(CustomerName) || string.IsNullOrWhiteSpace(CustomerPhone) ||
            string.IsNullOrWhiteSpace(DeliveryArea) || string.IsNullOrWhiteSpace(DeliveryStreet) || SelectedDeliveryService is null))
        { OpenDeliveryEditor(); return; }
        CheckoutError = null;
        PaymentMethod = "cash";
        AmountPaidText = CashInput.Format(GrandTotalPiastres);
        _replaceTenderOnNextKey = true;
        CurrentCheckoutRequestId ??= $"req-{Guid.NewGuid():N}";
        IsCheckoutOpen = true;
    }

    [RelayCommand]
    private void CloseCheckout() { if (IsOperationBusy) return; if (_pendingCheckout is not null) { CheckoutError = "لم تُحسم نتيجة العملية؛ أعد المحاولة للتحقق قبل الإغلاق."; return; } IsCheckoutOpen = false; }

    [RelayCommand]
    private void SetPaymentMethod(string method)
    {
        if (!CanEditOperation) return;
        PaymentMethod = method;
        // Non-cash methods always collect exactly the total - only cash needs a typed amount.
        if (method != "cash") AmountPaidText = CashInput.Format(GrandTotalPiastres);
        OnPropertyChanged(nameof(ChangeDuePiastres));
    }

    [RelayCommand]
    private void PressAmountKey(string key)
    {
        if (!CanEnterTender) return;
        if (_replaceTenderOnNextKey && key is not "back" and not "clear")
        { AmountPaidText = key == "." ? "0." : key; return; }
        AmountPaidText = key switch
        {
            "clear" => string.Empty,
            "back" => AmountPaidText.Length > 0 ? AmountPaidText[..^1] : AmountPaidText,
            _ => AmountPaidText + key,
        };
        OnPropertyChanged(nameof(ChangeDuePiastres));
    }

    [RelayCommand]
    private void QuickCash(string amountEgp)
    {
        if (!CanEnterTender) return;
        AmountPaidText = amountEgp == "exact" ? CashInput.Format(GrandTotalPiastres) : amountEgp;
        _replaceTenderOnNextKey = true;
        OnPropertyChanged(nameof(ChangeDuePiastres));
    }

    private bool CanSubmitPayment() => !IsProcessingCheckout;

    [RelayCommand(CanExecute = nameof(CanSubmitPayment))]
    private async Task SubmitPaymentAsync()
    {
        // Synchronous guard against a double click within the same UI tick, BEFORE the async
        // server round trip even starts - real duplicate prevention is the server's idempotency
        // key, but the UI must never let two clicks race ahead of the first response either.
        if (IsProcessingCheckout) return;
        IsProcessingCheckout = true;
        CheckoutError = null;
        try
        {
            if (_pendingCheckout is not null) { await CompleteCheckoutAsync(_pendingCheckout); return; }

            if (IsCompEnabled && string.IsNullOrWhiteSpace(CompReasonText))
            {
                CheckoutError = "أدخل سبب الإكرام/الضيافة.";
                return;
            }

            if (DiscountAmountPiastres > 0 && !CanDiscount)
            { CheckoutError = "لا تملك صلاحية تطبيق الخصم."; return; }
            if (IsDiscountEnabled && !CashInput.TryParse(DiscountAmountText, out _) ||
                IsCompEnabled && !CashInput.TryParse(CompAmountText, out _))
            { CheckoutError = "أدخل قيمة الخصم والضيافة بصورة صحيحة."; return; }

            string? compApprovalId = null, compApproverUserId = null;
            if (CompAmountPiastres > 0)
            {
                var (aid, auid, ok) = await ResolveManagerApprovalIfNeededAsync(
                    CompNeedsApproval, CompManagerPin, "orders_comp", CompReasonText, e => CheckoutError = e);
                if (!ok) return;
                compApprovalId = aid; compApproverUserId = auid;
            }

            IReadOnlyList<CheckoutSplitRequest>? splitPayments = null;
            if (IsSplitPaymentEnabled)
            {
                if (!CashInput.TryParse(SplitAmount1Text, out var a1) || a1 <= 0 ||
                    !CashInput.TryParse(SplitAmount2Text, out var a2) || a2 <= 0)
                {
                    CheckoutError = "أدخل مبلغين صحيحين للدفع المقسم.";
                    return;
                }
                splitPayments = new[]
                {
                    new CheckoutSplitRequest(SplitMethod1, a1),
                    new CheckoutSplitRequest(SplitMethod2, a2),
                };
            }

            if (!CashInput.TryParse(AmountPaidText, out var enteredPiastres) && !IsSplitPaymentEnabled)
            { CheckoutError = "أدخل المبلغ المدفوع بصورة صحيحة."; return; }
            var amountPiastres = IsSplitPaymentEnabled
                ? splitPayments!.Sum(s => s.AmountPiastres)
                : PaymentMethod == "cash" && !IsCashOnDelivery ? enteredPiastres : GrandTotalPiastres;

            var isDelivery = OrderType == "delivery";

            var request = new CheckoutOrderRequest(
                CheckoutRequestId: CurrentCheckoutRequestId!,
                CashierId: _session.CurrentUser!.Id,
                ShiftId: _session.ShiftId!,
                OrderType: OrderType,
                PaymentMethod: IsSplitPaymentEnabled ? "multi" : PaymentMethod,
                Items: Cart.Select(l => new CheckoutItemRequest(l.ProductId, l.VariantId, l.Quantity, l.AppliedOfferId, l.Notes)).ToList(),
                DiscountPiastres: DiscountAmountPiastres,
                DeliveryFeePiastres: isDelivery ? SelectedDeliveryService?.FeePiastres ?? 0 : 0,
                CompAmountPiastres: CompAmountPiastres,
                CompApprovalId: compApprovalId,
                CompApproverUserId: compApproverUserId,
                CompReason: CompAmountPiastres > 0 ? CompReasonText : null,
                AmountPaidPiastres: amountPiastres,
                SplitPayments: splitPayments,
                WalletProvider: PaymentMethod == "wallet" || IsSplitPaymentEnabled && (SplitMethod1 == "wallet" || SplitMethod2 == "wallet") ? NullIfEmpty(WalletProvider) : null,
                CustomerName: isDelivery && !string.IsNullOrWhiteSpace(CustomerName) ? CustomerName : null,
                CustomerPhone: isDelivery && !string.IsNullOrWhiteSpace(CustomerPhone) ? CustomerPhone : null,
                DeliveryAddress: null,
                DeliveryArea: isDelivery ? NullIfEmpty(DeliveryArea) : null,
                BusinessDayId: null,
                DeliveryStreet: isDelivery ? NullIfEmpty(DeliveryStreet) : null,
                DeliveryBuilding: isDelivery ? NullIfEmpty(DeliveryBuilding) : null,
                DeliveryFloor: isDelivery ? NullIfEmpty(DeliveryFloor) : null,
                DeliveryApartment: isDelivery ? NullIfEmpty(DeliveryApartment) : null,
                DeliveryLandmark: isDelivery ? NullIfEmpty(DeliveryLandmark) : null,
                DeliveryNotes: isDelivery ? NullIfEmpty(DeliveryNotes) : null,
                DeliveryServiceId: isDelivery ? SelectedDeliveryService?.Id : null,
                DeliveryServiceName: isDelivery ? SelectedDeliveryService?.Name : null,
                CompReasonCode: CompAmountPiastres > 0 ? CompReasonCode : null,
                // The server creates this order's tickets as part of the checkout call itself, so a
                // ticket cannot be lost by this app dying between the sale and the print.
                CustomerPrintRoute: "Receipt");

            await CompleteCheckoutAsync(request);
        }
        finally
        {
            IsProcessingCheckout = false;
        }
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    [RelayCommand]
    private void StartNewOrder()
    {
        if (!CanEditOperation) return;
        LastCheckoutResult = null;
        CheckoutError = null;
        ResetDelivery();
    }
    private CheckoutOrderRequest? _pendingCheckout;
    private async Task CompleteCheckoutAsync(CheckoutOrderRequest request)
    {
        var wasUnresolved = _pendingCheckout is not null;
        _pendingCheckout = request;
        RefreshOperationState();
        var result = await _session.Api.CheckoutAsync(request);
        if (result.Success || (!wasUnresolved && !result.OutcomeUnknown)) _pendingCheckout = null;
        RefreshOperationState();
        if (!result.Success || result.Value is null)
        {
            // The request id is kept so a retry of the SAME payment can never become a second
            // order - only "إلغاء" / starting a new order clears it.
            CheckoutError = result.Error ?? "تعذر إتمام عملية الدفع.";
            return;
        }

        _reviewingExistingOrder = false;
        LastCheckoutResult = result.Value;
        IsCheckoutOpen = false;
        Cart.Clear();
        RecomputeCartTotals();
        CurrentCheckoutRequestId = null;
        IsDiscountEnabled = false;
        IsCompEnabled = false;
        IsSplitPaymentEnabled = false;
    }

}

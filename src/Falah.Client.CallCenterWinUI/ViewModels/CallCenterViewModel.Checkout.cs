using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Customers;
using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Client.CallCenterWinUI.Services;

namespace Falah.Client.CallCenterWinUI.ViewModels;

/// <summary>
/// Submits the phone order as a real Delivery checkout. Cash defaults to cash-on-delivery (nothing
/// is collected here - see CheckoutRules.IsCashOnDelivery/DefersAllMoney - the driver collects and a
/// later RecordDeliveryResult posts the money), matching what this station can actually observe: it
/// never touches physical cash. InstaPay/Wallet are sent as a claimed, pending electronic payment,
/// exactly like CashierWinUI's own non-cash checkout path - a manager confirms receipt afterward.
/// </summary>
public partial class CallCenterViewModel
{
    [ObservableProperty] private string _paymentMethod = "cash";
    [ObservableProperty] private string? _walletProvider;
    [ObservableProperty] private string? _checkoutError;
    [ObservableProperty] private bool _isProcessingCheckout;
    [ObservableProperty] private CheckoutOrderResult? _lastCheckoutResult;

    public bool HasCheckoutResult => LastCheckoutResult is not null;

    /// <summary>Shown only when the order was recorded but its tickets could not be queued. The
    /// order is never failed for a printing problem, so without this the operator cannot tell a
    /// confirmation copy that printed from one that was never created.</summary>
    public bool HasPrintWarning => LastCheckoutResult is { PrintJobsQueued: false };

    public string PrintWarningText => "تم تسجيل الطلب لكن لم تُنشأ تذاكر الطباعة. أعد الطباعة من «البحث عن طلب».";

    partial void OnLastCheckoutResultChanged(CheckoutOrderResult? value)
    {
        OnPropertyChanged(nameof(HasCheckoutResult));
        OnPropertyChanged(nameof(HasPrintWarning));
        SubmitOrderCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void SetPaymentMethod(string method)
    {
        if (!CanEditOperation) return;
        PaymentMethod = method;
        if (method != "wallet") WalletProvider = null;
    }

    /// <summary>Minted once per order and never rotated by a retry, matching CashierWinUI's own
    /// CurrentCheckoutRequestId - a same-task double click or a network retry of the same attempt
    /// must reuse this exact value so the server's idempotency check collapses them into one
    /// order.</summary>
    private string? CurrentCheckoutRequestId;

    /// <summary>The exact request whose outcome we never learned (transport failure / 5xx). While
    /// this is non-null the order is frozen and the ONLY way forward is retrying this same payload,
    /// so the server's idempotency key can tell us whether the first attempt actually landed.</summary>
    private CheckoutOrderRequest? _pendingCheckout;

    /// <summary>Keyed by operator, because this station keeps one order draft per logged-in operator
    /// and two of them must never recover each other's unresolved checkout.</summary>
    private string CheckoutOwnerKey => $"callcenter.checkout.{OwnerUserId}";
    private string PrintOwnerKey => $"callcenter.print.{OwnerUserId}";

    /// <summary>Written before the request goes out, not after it fails: the window this protects is
    /// the one where the request has left and the answer has not come back, which is exactly when a
    /// power cut used to destroy the only evidence that the question existed.</summary>
    private void PersistPending(string ownerKey, object? request)
    {
        if (request is null) RetainedRequestStorage.Current?.Clear(ownerKey);
        else RetainedRequestStorage.Current?.Save(ownerKey, request);
    }

    /// <summary>Called once when this operator's desk is created. A file left behind means an attempt
    /// was in flight when the process ended, so its outcome is unknown and the screen must offer the
    /// same retry it would have offered had the process survived.</summary>
    private void RecoverPendingOperations()
    {
        _pendingCheckout = RetainedRequestStorage.Current?.Load(CheckoutOwnerKey) as CheckoutOrderRequest;
        if (_pendingCheckout is not null)
        {
            CurrentCheckoutRequestId = _pendingCheckout.CheckoutRequestId;
            CheckoutError = "طلب سابق لم يصل تأكيده قبل إغلاق البرنامج. اضغط \"إعادة المحاولة\" للتحقق منه قبل تسجيل أي طلب جديد.";
        }

        _pendingPrint = RetainedRequestStorage.Current?.Load(PrintOwnerKey) as CreatePrintJobsRequest;
        if (_pendingPrint is not null)
        {
            _hasUnresolvedPrint = true;
            ReprintNote = "طلب طباعة سابق لم يصل تأكيده. أعد المحاولة بنفس الطلب للتحقق دون إنشاء نسخة إضافية.";
        }
    }

    public bool HasPendingCheckout => _pendingCheckout is not null;

    private bool CanSubmitOrder() => !IsOperationBusy && !HasCheckoutResult && _pendingPrint is null;

    [RelayCommand(CanExecute = nameof(CanSubmitOrder))]
    private async Task SubmitOrderAsync()
    {
        if (!CanSubmitOrder()) return;
        IsProcessingCheckout = true;
        CheckoutError = null;
        try
        {
            // An unresolved previous attempt is re-sent byte-for-byte, never rebuilt from the
            // (possibly since-edited) form.
            if (_pendingCheckout is not null)
            {
                await SendCheckoutAsync(_pendingCheckout);
                return;
            }

            if (Cart.Count == 0) { CheckoutError = "السلة فارغة."; return; }
            if (string.IsNullOrWhiteSpace(CustomerPhone)) { CheckoutError = "أدخل رقم هاتف العميل."; return; }
            if (string.IsNullOrWhiteSpace(CustomerName)) { CheckoutError = "أدخل اسم العميل."; return; }
            if (SelectedDeliveryService is null) { CheckoutError = "اختر خدمة التوصيل."; return; }
            if (string.IsNullOrWhiteSpace(DeliveryArea)) { CheckoutError = "أدخل المنطقة."; return; }
            if (string.IsNullOrWhiteSpace(DeliveryStreet)) { CheckoutError = "أدخل الشارع."; return; }

            // Re-check right before committing money to an order - the shift this station has no
            // control over may have closed since the screen loaded.
            if (!await _session.RefreshCurrentShiftAsync())
            {
                IsShiftOpen = false;
                CheckoutError = "لا توجد وردية مفتوحة حاليًا - لا يمكن تسجيل الطلب.";
                return;
            }
            IsShiftOpen = true;

            var isElectronic = PaymentMethod is "instapay" or "wallet";
            var amountPaidPiastres = isElectronic ? GrandTotalPiastres : 0;

            CurrentCheckoutRequestId ??= $"req-{Guid.NewGuid():N}";

            var request = new CheckoutOrderRequest(
                CheckoutRequestId: CurrentCheckoutRequestId,
                CashierId: _session.CurrentUser!.Id,
                ShiftId: _session.ShiftId!,
                OrderType: "delivery",
                PaymentMethod: PaymentMethod,
                Items: Cart.Select(l => new CheckoutItemRequest(l.ProductId, l.VariantId, l.Quantity, l.AppliedOfferId, l.Notes)).ToList(),
                DiscountPiastres: 0,
                DeliveryFeePiastres: SelectedDeliveryService.FeePiastres ?? 0,
                CompAmountPiastres: 0,
                CompApprovalId: null,
                CompApproverUserId: null,
                CompReason: null,
                AmountPaidPiastres: amountPaidPiastres,
                SplitPayments: null,
                WalletProvider: PaymentMethod == "wallet" ? WalletProvider : null,
                TableNumber: null,   // the Call Center takes delivery orders; there is no table
                CustomerName: CustomerName,
                CustomerPhone: CustomerPhone,
                DeliveryAddress: null,
                DeliveryArea: DeliveryArea,
                BusinessDayId: null,
                DeliveryStreet: DeliveryStreet,
                DeliveryBuilding: NullIfEmpty(DeliveryBuilding),
                DeliveryFloor: NullIfEmpty(DeliveryFloor),
                DeliveryApartment: NullIfEmpty(DeliveryApartment),
                DeliveryLandmark: NullIfEmpty(DeliveryLandmark),
                DeliveryNotes: NullIfEmpty(DeliveryNotes),
                DeliveryServiceId: SelectedDeliveryService.Id,
                DeliveryServiceName: SelectedDeliveryService.Name,
                CompReasonCode: null,
                // This station's customer copy is the phoned-in confirmation, not a till receipt.
                CustomerPrintRoute: "CallCenter");

            await SendCheckoutAsync(request);
        }
        finally
        {
            IsProcessingCheckout = false;
        }
    }

    private async Task SendCheckoutAsync(CheckoutOrderRequest request)
    {
        var wasUnresolved = _pendingCheckout is not null;
        PersistPending(CheckoutOwnerKey, request);
        var result = await _session.Api.CheckoutAsync(request);
        if (!result.Success || result.Value is null)
        {
            if (result.OutcomeUnknown || wasUnresolved)
            {
                // Do NOT clear the cart or the form: we genuinely do not know whether this order
                // exists on the server. Retrying the identical request is what resolves it.
                _pendingCheckout = request;
                OnPropertyChanged(nameof(HasPendingCheckout));
                RefreshOperationState();
                CheckoutError = "لم يصل تأكيد من السيرفر. اضغط \"إعادة المحاولة\" بنفس البيانات للتحقق من الطلب.";
                return;
            }

            // A definite server rejection (validation/permission): the order was not created, so the
            // operator is free to correct the form and try again.
            _pendingCheckout = null;
            PersistPending(CheckoutOwnerKey, null);
            OnPropertyChanged(nameof(HasPendingCheckout));
            RefreshOperationState();
            CheckoutError = result.Error ?? "تعذر تسجيل الطلب.";
            return;
        }

        _pendingCheckout = null;
        PersistPending(CheckoutOwnerKey, null);
        OnPropertyChanged(nameof(HasPendingCheckout));
        RefreshOperationState();
        LastCheckoutResult = result.Value;

        // Persist the customer + this address for next time the same number calls - a plain upsert,
        // never blocking the order's own success on it (see UpsertCustomerRequest's own doc comment:
        // idempotent by construction, safe to fire-and-forget after the order that actually matters
        // is already committed). Sourced from the REQUEST, not the live form, so a retry saves
        // exactly what was ordered.
        _ = _session.Api.SaveCustomerAsync(new UpsertCustomerRequest(
            request.CustomerPhone!, request.CustomerName!, request.DeliveryArea, request.DeliveryStreet,
            request.DeliveryBuilding, request.DeliveryFloor, request.DeliveryApartment, request.DeliveryLandmark,
            request.DeliveryServiceId, request.DeliveryServiceName));

        // The tickets are no longer requested from here. The server creates them as part of the
        // checkout call itself (OrderAutoPrintService), keyed off this same CheckoutRequestId, so a
        // confirmation copy cannot be lost by this app dying between the sale and the print - and
        // so there is exactly ONE place that decides what an order prints. The CallCenter route is
        // still a distinct customer-facing ticket from the till's Receipt route; the checkout
        // request says so via CustomerPrintRoute.
        //
        // The retained-print machinery below stays: it still backs the manual reprint path, which
        // is a genuinely client-initiated action.
        Cart.Clear();
        RecomputeCartTotals();
        ResetCustomerAndDelivery();
        CurrentCheckoutRequestId = null;
    }

    [RelayCommand]
    private void StartNewOrder()
    {
        if (!CanEditOperation) return;
        ClearResolvedPrint();
        LastCheckoutResult = null;
        CheckoutError = null;
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    // ---------------- Order lookup / reprint (e.g. "the customer called back about #0134") ----------------

    [ObservableProperty] private bool _isOrderLookupOpen;
    [ObservableProperty] private string _lookupOrderNumber = "";
    [ObservableProperty] private OrderDetailsDto? _lookedUpOrder;

    /// <summary>Whether the money for a looked-up order is settled - which an operator taking a
    /// follow-up call has to know before promising anything. Three states, not two: an electronic
    /// transfer nobody has confirmed is neither paid nor collect-on-delivery.</summary>
    public string LookedUpPaymentLine
    {
        get
        {
            if (LookedUpOrder is not { } order) return "";
            var how = order.SplitPayments is { Count: > 0 } splits
                ? string.Join(" + ", splits.Select(s => $"{s.Method} {CashInput.Format(s.AmountPiastres)} ج"))
                : order.PaymentMethod;
            return order.ElectronicPaymentStatus == "pending" ? $"{how} - تحويل لم يُؤكَّد"
                : order.IsPaid ? $"{how} - مدفوع"
                : $"{how} - تحصيل عند التسليم";
        }
    }

    public bool HasLookedUpPaymentLine => LookedUpPaymentLine.Length > 0;
    public bool IsLookedUpPaymentUnconfirmed => LookedUpOrder?.ElectronicPaymentStatus == "pending";

    partial void OnLookedUpOrderChanged(OrderDetailsDto? value)
    {
        OnPropertyChanged(nameof(LookedUpPaymentLine));
        OnPropertyChanged(nameof(HasLookedUpPaymentLine));
        OnPropertyChanged(nameof(IsLookedUpPaymentUnconfirmed));
    }
    [ObservableProperty] private string? _lookupError;
    [ObservableProperty] private bool _isLookingUpOrder;
    [ObservableProperty] private bool _isReprinting;
    [ObservableProperty] private string? _reprintNote;
    private CreatePrintJobsRequest? _pendingPrint;
    private bool _hasUnresolvedPrint;
    public bool HasPendingPrint => _pendingPrint is not null;

    [RelayCommand]
    private void OpenOrderLookup() { if (CanEditOperation) IsOrderLookupOpen = true; }

    [RelayCommand]
    private void CloseOrderLookup()
    {
        if (!CanEditOperation) return;
        IsOrderLookupOpen = false;
        ClearOrderLookup();
    }

    [RelayCommand]
    private async Task LookupOrderAsync()
    {
        if (!CanEditOperation || string.IsNullOrWhiteSpace(LookupOrderNumber)) return;
        ClearResolvedPrint();
        IsLookingUpOrder = true;
        LookupError = null;
        LookedUpOrder = null;
        ReprintNote = null;
        try
        {
            var result = await _session.Api.LookupOrderByNumberAsync(LookupOrderNumber.Trim());
            if (!result.Success || result.Value is null)
            {
                LookupError = result.Error ?? "لم يتم العثور على الطلب.";
                return;
            }
            LookedUpOrder = result.Value;
        }
        finally
        {
            IsLookingUpOrder = false;
        }
    }

    [RelayCommand]
    private async Task ReprintLookedUpOrderAsync()
    {
        if (IsOperationBusy || _pendingCheckout is not null) return;
        if (_pendingPrint is not null) { await SendPrintAsync(_pendingPrint); return; }
        if (LookedUpOrder is null) return;
        await SendPrintAsync(new CreatePrintJobsRequest($"pj-{Guid.NewGuid():N}",
            LookedUpOrder.Id, "CallCenter", true, _session.CurrentUser!.Id));
    }

    [RelayCommand]
    private async Task RetryPrintAsync()
    {
        if (IsOperationBusy || _pendingPrint is null) return;
        await SendPrintAsync(_pendingPrint);
    }

    private async Task SendPrintAsync(CreatePrintJobsRequest request)
    {
        IsReprinting = true;
        ReprintNote = null;
        try
        {
            PersistPending(PrintOwnerKey, request);
            var result = await _session.Api.CreatePrintJobsAsync(request.OrderId, request);
            if (result.Success && result.Value is not null)
            {
                _pendingPrint = null;
                _hasUnresolvedPrint = false;
                PersistPending(PrintOwnerKey, null);
                ReprintNote = "تم تسجيل طلب الطباعة في قائمة الانتظار.";
            }
            else
            {
                _pendingPrint = request;
                _hasUnresolvedPrint |= result.OutcomeUnknown;
                ReprintNote = _hasUnresolvedPrint
                    ? "لم يصل تأكيد طلب الطباعة. أعد المحاولة بنفس الطلب للتحقق دون إنشاء نسخة إضافية."
                    : result.Error ?? "تعذر تسجيل طلب الطباعة. يمكنك إعادة المحاولة.";
            }
        }
        finally
        {
            OnPropertyChanged(nameof(HasPendingPrint));
            IsReprinting = false;
        }
    }

    private void ClearResolvedPrint()
    {
        if (_hasUnresolvedPrint) return;
        _pendingPrint = null;
        ReprintNote = null;
        OnPropertyChanged(nameof(HasPendingPrint));
        RefreshOperationState();
    }

    [RelayCommand]
    private void ClearOrderLookup()
    {
        if (!CanEditOperation) return;
        ClearResolvedPrint();
        LookupOrderNumber = "";
        LookedUpOrder = null;
        LookupError = null;
        ReprintNote = null;
    }
}

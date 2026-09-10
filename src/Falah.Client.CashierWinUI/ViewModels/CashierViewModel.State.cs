using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

public partial class CashierViewModel
{
    partial void OnIsProcessingCheckoutChanged(bool value) => RefreshOperationState();
    partial void OnIsOpeningShiftChanged(bool value) => RefreshOperationState();
    partial void OnIsClosingShiftChanged(bool value) => RefreshOperationState();
    partial void OnIsSubmittingExpenseChanged(bool value) => RefreshOperationState();
    partial void OnIsSubmittingSafeDropChanged(bool value) => RefreshOperationState();
    partial void OnIsSubmittingCashMovementChanged(bool value) => RefreshOperationState();
    partial void OnIsSubmittingCashDrawerChanged(bool value) => RefreshOperationState();
    partial void OnIsReprintingChanged(bool value) => RefreshOperationState();
    partial void OnIsConfirmingElectronicPaymentChanged(bool value) => RefreshOperationState();
    partial void OnIsCancellingElectronicPaymentChanged(bool value) => RefreshOperationState();
    public bool IsOperationBusy => IsRefunding || IsProcessingCheckout || IsOpeningShift || IsClosingShift || IsSubmittingExpense ||
        IsSubmittingSafeDrop || IsSubmittingCashMovement || IsSubmittingCashDrawer || IsReprinting ||
        IsConfirmingElectronicPayment || IsCancellingElectronicPayment;
    public bool CanEditOperation => !IsOperationBusy && _pendingRefund is null && _pendingReprint is null && _pendingCheckout is null && _pendingExpense is null &&
        _pendingSafeDrop is null && _pendingCashMovement is null && _pendingCashDrawer is null &&
        _pendingOpenShift is null && _pendingCloseShift is null && !_pendingElectronicConfirm.HasPending && !_pendingElectronicCancel.HasPending;
    public bool CanDiscount => _session.HasPermission("orders_discount");
    public bool CanViewDrawerBalance => _session.HasPermission("treasury_manage") || _session.HasPermission("reports_view");
    public bool UsesWallet => IsSplitPaymentEnabled ? SplitMethod1 == "wallet" || SplitMethod2 == "wallet" : PaymentMethod == "wallet";
    public bool IsCashOnDelivery => IsDelivery && !IsSplitPaymentEnabled && PaymentMethod == "cash" && GrandTotalPiastres > 0;
    public bool CanEnterTender => CanEditOperation && !IsCashOnDelivery && !IsSplitPaymentEnabled && PaymentMethod == "cash";
    public string TenderLabel => IsCashOnDelivery ? "يُحصّل عند التوصيل بالجنيه" : "المبلغ المستلم بالجنيه";
    private void NotifyPaymentPresentation()
    {
        OnPropertyChanged(nameof(UsesWallet)); OnPropertyChanged(nameof(IsCashOnDelivery));
        OnPropertyChanged(nameof(CanEnterTender)); OnPropertyChanged(nameof(TenderLabel));
    }
    partial void OnPaymentMethodChanged(string value) => NotifyPaymentPresentation();
    partial void OnSplitMethod1Changed(string value) => OnPropertyChanged(nameof(UsesWallet));
    partial void OnSplitMethod2Changed(string value) => OnPropertyChanged(nameof(UsesWallet));
    partial void OnIsSplitPaymentEnabledChanged(bool value) => NotifyPaymentPresentation();
    public bool CanOpenShift => _session.HasPermission("shifts_open_own") || _session.HasPermission("shifts_manage");
    public bool CanCloseShift => _session.HasPermission("shifts_blind_close");
    public bool CanCountCash => _session.HasPermission("cash_count");
    public bool IsDelivery => OrderType == "delivery";
    public string OrderTypeLabel => OrderType switch { "delivery" => "توصيل", "dine_in" => "صالة", _ => "تيك أواي" };
    public string DeliveryFeeDisplay => CashInput.Format(IsDelivery ? SelectedDeliveryService?.FeePiastres ?? 0 : 0) + " ج";
    public string DiscountDisplay => CashInput.Format(DiscountAmountPiastres) + " ج";
    public string CompDisplay => CashInput.Format(CompAmountPiastres) + " ج";
    public bool HasTax => _taxRule is { Enabled: true, RateBasisPoints: > 0 };
    public string TaxLabel => _taxRule?.Mode == Falah.Domain.Financial.TaxMode.Inclusive ? "الضريبة (مشمولة)" : "الضريبة";
    public string TaxDisplay => CashInput.Format(Falah.Domain.Financial.TaxEngine.Calculate(
        Falah.Domain.Common.Money.FromPiastres(Math.Max(0, CartSubtotalPiastres - DiscountAmountPiastres)), _taxRule).Tax.Piastres) + " ج";
    public string CheckoutTitle => IsElectronicPaymentPending ? "بانتظار تأكيد التحويل" : "تم تسجيل الطلب";
    public string CheckoutSubtitle => IsElectronicPaymentPending ? "الطلب مسجل؛ المبلغ لم يُحصّل بعد" : "راجع رقم الطلب والمبلغ المسجل";

    /// <summary>Shown only when the sale succeeded but its tickets could not be queued. The sale is
    /// never failed for a printing problem, so without this line a ticket that never reached the
    /// kitchen would look exactly like one that did.</summary>
    public bool HasPrintWarning => LastCheckoutResult is { PrintJobsQueued: false };

    public string PrintWarningText => "تم تسجيل الطلب لكن لم تُنشأ تذاكر الطباعة. أعد الطباعة من «البحث عن طلب».";
    public string CatalogueTitle => string.IsNullOrWhiteSpace(SearchQuery) ? SelectedCategory?.Name ?? "قائمة الطعام" : "نتائج البحث";
    [ObservableProperty] private bool _isInitializing;
    [ObservableProperty] private string? _sessionError;
    [ObservableProperty] private bool _isClearCartOpen;

    private void RefreshOperationState()
    {
        OnPropertyChanged(nameof(CanEditOperation)); OnPropertyChanged(nameof(CanEnterTender));
        OnPropertyChanged(nameof(CanConfirmPendingPayment)); OnPropertyChanged(nameof(CanCancelPendingPayment));
        NotifyCheckoutAvailability();
        _header.Refresh();
    }
    private void NotifyTotals()
    {
        OnPropertyChanged(nameof(GrandTotalPiastres)); OnPropertyChanged(nameof(GrandTotalDisplay));
        OnPropertyChanged(nameof(DeliveryFeeDisplay)); OnPropertyChanged(nameof(DiscountDisplay));
        OnPropertyChanged(nameof(CompDisplay)); OnPropertyChanged(nameof(ChangeDuePiastres));
        OnPropertyChanged(nameof(HasTax)); OnPropertyChanged(nameof(TaxLabel)); OnPropertyChanged(nameof(TaxDisplay));
        NotifyPaymentPresentation();
        NotifyCheckoutAvailability();
        if (IsCheckoutOpen && (PaymentMethod != "cash" || IsCashOnDelivery) && CanEditOperation)
            AmountPaidText = CashInput.Format(GrandTotalPiastres);
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (IsInitializing) return;
        IsInitializing = true;
        SessionError = null;
        try
        {
            var shift = await _session.Api.GetCurrentShiftAsync();
            if (!shift.Success && shift.StatusCode != 404) { SessionError = shift.Error; return; }
            _session.ShiftId = shift.Value?.ShiftId;
            _session.ShiftName = shift.Value?.ShiftName;
            OnPropertyChanged(nameof(IsShiftOpen));
            _header.Refresh();
            var settings = await _session.Api.GetSettingsAsync();
            if (settings.Success && settings.Value is not null) _header.SetIdentity(settings.Value.RestaurantName);
            if (IsShiftOpen) await LoadMenuAsync();
        }
        finally { IsInitializing = false; }
    }

    [RelayCommand] private void RequestClearCart() { if (CanEditOperation && Cart.Count > 0) IsClearCartOpen = true; }
    [RelayCommand] private void CancelClearCart() => IsClearCartOpen = false;
    [RelayCommand] private void ConfirmClearCart() { if (!CanEditOperation) return; ClearCart(); IsClearCartOpen = false; }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Orders;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

public partial class CashierViewModel
{
    [ObservableProperty] private bool _isRefundOpen;
    [ObservableProperty] private bool _isRefunding;
    [ObservableProperty] private string _refundAmountText = "";
    [ObservableProperty] private string _refundReason = "";
    [ObservableProperty] private string _refundManagerPin = "";
    [ObservableProperty] private string? _refundError;
    private RefundOrderRequest? _pendingRefund;
    private string _refundRequestId = Guid.NewGuid().ToString("N");
    private bool _reviewingExistingOrder;
    /// <summary>Without orders_refund every refund needs a manager; with it, a refund of 500 ج or more
    /// still does (owner, 2026-11-18 - RefundRules.NeedsManagerApproval, which the server applies too).</summary>
    public bool RefundNeedsApproval => !_session.HasPermission("orders_refund")
        || (!_session.CanApproveOwn("orders_refund") && CashInput.TryParse(RefundAmountText, out var amount)
            && Falah.Domain.Orders.RefundRules.NeedsManagerApproval(Falah.Domain.Common.Money.FromPiastres(amount)));
    partial void OnRefundAmountTextChanged(string value) => OnPropertyChanged(nameof(RefundNeedsApproval));
    public bool CanRefundOrder => OrderLookupResult is { PaymentMethod: not "multi" } order
        && (order.IsPaid || order.CustomerFundsCollected)
        && !string.Equals(order.Status, "cancelled", StringComparison.OrdinalIgnoreCase)
        && order.TotalPiastres > order.RefundAmountPiastres;
    public bool LookupHasPendingPayment => OrderLookupResult?.ElectronicPaymentStatus == "pending";

    /// <summary>How a split-paid order was actually paid. The server has recorded the breakdown all
    /// along and no screen has ever shown it, so an order looked up later said only "multi" - and
    /// "multi" cannot be reconciled against a drawer or answered to a customer asking which card
    /// was charged.</summary>
    public string LookupPaymentBreakdown
    {
        get
        {
            if (OrderLookupResult?.SplitPayments is not { Count: > 0 } splits)
                return OrderLookupResult is { } o ? MethodLabel(o.PaymentMethod) : "";
            return string.Join(" + ", splits.Select(s => $"{MethodLabel(s.Method)} {CashInput.Format(s.AmountPiastres)} ج"));
        }
    }

    public bool HasLookupPaymentBreakdown => LookupPaymentBreakdown.Length > 0;

    /// <summary>The table a dine-in order was served at, as it was recorded at the time.</summary>
    public string LookupTableLine =>
        OrderLookupResult?.TableNumber is { Length: > 0 } table ? $"طاولة {table}" : "";

    public bool HasLookupTable => LookupTableLine.Length > 0;

    /// <summary>When the customer said they would collect a pickup order, on a Cairo wall clock -
    /// the same clock the counter and the customer agreed it on.</summary>
    public string LookupRequestedPickupLine =>
        OrderLookupResult?.RequestedPickupAtUtc is { } at
            ? TimeZoneInfo.ConvertTime(at, Falah.Domain.Common.BusinessClock.CairoTimeZone).ToString("yyyy-MM-dd HH:mm")
            : "";

    public bool HasLookupRequestedPickup => LookupRequestedPickupLine.Length > 0;

    private static string MethodLabel(string method) => method switch
    {
        "cash" => "كاش",
        "card" => "فيزا",
        "instapay" => "إنستاباي",
        "wallet" => "محفظة",
        "multi" => "دفع مقسّم",
        _ => method,
    };
    public string RefundRemainingDisplay => CashInput.Format(Math.Max(0, (OrderLookupResult?.TotalPiastres ?? 0) - (OrderLookupResult?.RefundAmountPiastres ?? 0))) + " ج";
    partial void OnIsRefundingChanged(bool value) => RefreshOperationState();
    partial void OnOrderLookupResultChanged(OrderDetailsDto? value)
    {
        OnPropertyChanged(nameof(CanRefundOrder)); OnPropertyChanged(nameof(LookupHasPendingPayment));
        OnPropertyChanged(nameof(RefundRemainingDisplay));
        OnPropertyChanged(nameof(LookupPaymentBreakdown)); OnPropertyChanged(nameof(HasLookupPaymentBreakdown));
        OnPropertyChanged(nameof(LookupTableLine)); OnPropertyChanged(nameof(HasLookupTable));
        OnPropertyChanged(nameof(LookupRequestedPickupLine)); OnPropertyChanged(nameof(HasLookupRequestedPickup));
    }

    [RelayCommand] private void OpenRefund()
    {
        if (!CanEditOperation || !CanRefundOrder) return;
        RefundAmountText = CashInput.Format(OrderLookupResult!.TotalPiastres - OrderLookupResult.RefundAmountPiastres);
        RefundReason = RefundManagerPin = ""; RefundError = null; IsRefundOpen = true;
    }
    [RelayCommand] private void CancelRefund()
    {
        if (IsRefunding) return;
        if (_pendingRefund is not null) { RefundError = "أعد المحاولة للتحقق من الاسترداد قبل الإغلاق."; return; }
        IsRefundOpen = false; RefundManagerPin = "";
    }
    [RelayCommand] private async Task SubmitRefundAsync()
    {
        // A recovered refund is retried as it was sent; only a NEW refund needs the looked-up order.
        if (IsRefunding || (OrderLookupResult is null && _pendingRefund is null)) return;
        IsRefunding = true; RefundError = null;
        try
        {
            if (_pendingRefund is null)
            {
                if (OrderLookupResult is not { } order) return;
                if (!CashInput.TryParse(RefundAmountText, out var amount) || amount <= 0 ||
                    amount > order.TotalPiastres - order.RefundAmountPiastres)
                { RefundError = "أدخل مبلغاً ضمن الرصيد المتاح للاسترداد."; return; }
                if (string.IsNullOrWhiteSpace(RefundReason)) { RefundError = "أدخل سبب الاسترداد."; return; }
                var (approvalId, approver, ok) = await ResolveManagerApprovalIfNeededAsync(RefundNeedsApproval,
                    RefundManagerPin, "orders_refund", RefundReason, e => RefundError = e);
                if (!ok) return;
                var request = new RefundOrderRequest(_refundRequestId, order.Id, amount, RefundReason,
                    _session.CurrentUser!.Id, approvalId, approver);
                await CompleteRefundAsync(request, false);
            }
            else await CompleteRefundAsync(_pendingRefund, true);
        }
        finally { IsRefunding = false; }
    }
    private async Task CompleteRefundAsync(RefundOrderRequest request, bool wasUnresolved)
    {
        _pendingRefund = request; Remember(RefundOperation, request); RefreshOperationState();
        var result = await _session.Api.RefundAsync(request);
        if (result.Success || (!wasUnresolved && !result.OutcomeUnknown)) { _pendingRefund = null; Remember(RefundOperation, null); }
        NoteRetryOutcome(RefundOperation, wasUnresolved, result.Success, result.OutcomeUnknown, result.Error);
        RefreshOperationState();
        if (!result.Success || result.Value is null) { RefundError = result.Error; return; }
        _refundRequestId = Guid.NewGuid().ToString("N");
        // A refund recovered after a restart has no looked-up order on screen to update.
        if (OrderLookupResult is { } looked && looked.Id == result.Value.OrderId)
            OrderLookupResult = looked with { Status = result.Value.OrderStatus,
                RefundAmountPiastres = looked.TotalPiastres - result.Value.RemainingRefundablePiastres };
        ReprintStatusMessage = "تم تسجيل استرداد " + CashInput.Format(result.Value.AmountPiastres) + " ج";
        IsRefundOpen = false; RefundManagerPin = "";
    }

    [RelayCommand] private void ReviewPendingOrder()
    {
        if (!CanEditOperation || !LookupHasPendingPayment || OrderLookupResult is not { } order) return;
        _reviewingExistingOrder = true;
        LastCheckoutResult = new(order.Id, order.OrderNumber, order.SequenceNumber, order.TotalPiastres,
            order.AmountPaidPiastres, order.ChangeDuePiastres, order.Status, order.IsPaid, order.ElectronicPaymentStatus, false);
        PendingPaymentActionError = null; CancelPendingReason = ""; IsOrderLookupOpen = false;
    }
    [RelayCommand] private void DismissCheckoutResult()
    {
        if (!CanEditOperation) return;
        LastCheckoutResult = null;
        if (!_reviewingExistingOrder) ResetDelivery();
        _reviewingExistingOrder = false;
    }
}

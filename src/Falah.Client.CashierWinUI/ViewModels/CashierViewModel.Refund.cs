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
    public bool RefundNeedsApproval => !_session.HasPermission("orders_refund");
    public bool CanRefundOrder => OrderLookupResult is { IsPaid: true, PaymentMethod: not "multi", Status: not "cancelled" } order &&
        order.TotalPiastres > order.RefundAmountPiastres;
    public bool LookupHasPendingPayment => OrderLookupResult?.ElectronicPaymentStatus == "pending";
    public string RefundRemainingDisplay => CashInput.Format(Math.Max(0, (OrderLookupResult?.TotalPiastres ?? 0) - (OrderLookupResult?.RefundAmountPiastres ?? 0))) + " ج";
    partial void OnIsRefundingChanged(bool value) => RefreshOperationState();
    partial void OnOrderLookupResultChanged(OrderDetailsDto? value)
    {
        OnPropertyChanged(nameof(CanRefundOrder)); OnPropertyChanged(nameof(LookupHasPendingPayment));
        OnPropertyChanged(nameof(RefundRemainingDisplay));
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
        if (IsRefunding || OrderLookupResult is null) return;
        IsRefunding = true; RefundError = null;
        try
        {
            if (_pendingRefund is null)
            {
                if (!CashInput.TryParse(RefundAmountText, out var amount) || amount <= 0 ||
                    amount > OrderLookupResult.TotalPiastres - OrderLookupResult.RefundAmountPiastres)
                { RefundError = "أدخل مبلغاً ضمن الرصيد المتاح للاسترداد."; return; }
                if (string.IsNullOrWhiteSpace(RefundReason)) { RefundError = "أدخل سبب الاسترداد."; return; }
                var (approvalId, approver, ok) = await ResolveManagerApprovalIfNeededAsync(RefundNeedsApproval,
                    RefundManagerPin, "orders_refund", RefundReason, e => RefundError = e);
                if (!ok) return;
                var request = new RefundOrderRequest(_refundRequestId, OrderLookupResult.Id, amount, RefundReason,
                    _session.CurrentUser!.Id, approvalId, approver);
                await CompleteRefundAsync(request, false);
            }
            else await CompleteRefundAsync(_pendingRefund, true);
        }
        finally { IsRefunding = false; }
    }
    private async Task CompleteRefundAsync(RefundOrderRequest request, bool wasUnresolved)
    {
        _pendingRefund = request; RefreshOperationState();
        var result = await _session.Api.RefundAsync(request);
        if (result.Success || (!wasUnresolved && !result.OutcomeUnknown)) _pendingRefund = null;
        RefreshOperationState();
        if (!result.Success || result.Value is null) { RefundError = result.Error; return; }
        _refundRequestId = Guid.NewGuid().ToString("N");
        OrderLookupResult = OrderLookupResult! with { Status = result.Value.OrderStatus,
            RefundAmountPiastres = OrderLookupResult.TotalPiastres - result.Value.RemainingRefundablePiastres };
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

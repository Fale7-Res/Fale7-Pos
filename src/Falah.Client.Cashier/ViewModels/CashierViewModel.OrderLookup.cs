using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Orders;

namespace Falah.Client.Cashier.ViewModels;

public partial class CashierViewModel
{
    [ObservableProperty] private bool _isOrderLookupOpen;
    [ObservableProperty] private string _lookupOrderNumber = string.Empty;
    [ObservableProperty] private OrderDetailsDto? _lookedUpOrder;
    [ObservableProperty] private string? _lookupError;
    [ObservableProperty] private bool _isLookingUp;

    [ObservableProperty] private string _refundAmountText = string.Empty;
    [ObservableProperty] private string _refundReason = string.Empty;
    [ObservableProperty] private string? _refundError;
    [ObservableProperty] private string? _refundSuccessMessage;
    [ObservableProperty] private bool _isRefunding;

    public long RefundableRemainingPiastres =>
        LookedUpOrder is null ? 0 : LookedUpOrder.TotalPiastres - LookedUpOrder.RefundAmountPiastres;

    [RelayCommand]
    private void OpenOrderLookup()
    {
        IsOrderLookupOpen = true;
        LookupOrderNumber = string.Empty;
        LookedUpOrder = null;
        LookupError = null;
        RefundError = null;
        RefundSuccessMessage = null;
    }

    [RelayCommand]
    private void CloseOrderLookup() => IsOrderLookupOpen = false;

    [RelayCommand]
    private async Task LookupOrderAsync()
    {
        if (string.IsNullOrWhiteSpace(LookupOrderNumber)) { LookupError = "أدخل رقم الطلب."; return; }
        IsLookingUp = true;
        LookupError = null;
        LookedUpOrder = null;
        RefundSuccessMessage = null;
        try
        {
            var result = await _session.Api.LookupOrderByNumberAsync(LookupOrderNumber.Trim());
            if (!result.Success || result.Value is null)
            {
                LookupError = result.Error ?? "الطلب غير موجود.";
                return;
            }
            LookedUpOrder = result.Value;
            OnPropertyChanged(nameof(RefundableRemainingPiastres));
            RefundAmountText = (RefundableRemainingPiastres / 100m).ToString("0.00");
        }
        finally
        {
            IsLookingUp = false;
        }
    }

    private bool CanRefund() => LookedUpOrder is not null && !IsRefunding;

    [RelayCommand(CanExecute = nameof(CanRefund))]
    private async Task SubmitRefundAsync()
    {
        if (LookedUpOrder is null) return;
        if (!decimal.TryParse(RefundAmountText, out var amountEgp) || amountEgp <= 0)
        {
            RefundError = "أدخل مبلغًا صحيحًا للاسترجاع.";
            return;
        }
        if (string.IsNullOrWhiteSpace(RefundReason)) { RefundError = "سبب الاسترجاع مطلوب."; return; }

        IsRefunding = true;
        RefundError = null;
        try
        {
            var request = new RefundOrderRequest(
                RefundRequestId: $"rreq-{Guid.NewGuid():N}",
                OrderId: LookedUpOrder.Id,
                AmountPiastres: (long)Math.Round(amountEgp * 100m),
                Reason: RefundReason.Trim(),
                CashierId: _session.CurrentUser!.Id,
                ManagerApprovalId: null,
                ManagerApproverUserId: null);

            var result = await _session.Api.RefundAsync(request);
            if (!result.Success || result.Value is null)
            {
                RefundError = result.Error ?? "تعذر تنفيذ الاسترجاع.";
                return;
            }

            RefundSuccessMessage = $"تم استرجاع {result.Value.AmountPiastres / 100m:0.00} ج بنجاح. الحالة الحالية: {TranslateStatus(result.Value.OrderStatus)}.";
            // Refresh the on-screen order so the remaining-refundable figure reflects the server's
            // new truth, not a locally-guessed subtraction.
            await LookupOrderAsync();
        }
        finally
        {
            IsRefunding = false;
        }
    }

    private static string TranslateStatus(string status) => status switch
    {
        "completed" => "مكتمل",
        "refunded" => "مسترجَع بالكامل",
        "cancelled" => "ملغي",
        "preparing" => "قيد التحضير",
        "ready" => "جاهز",
        _ => status,
    };
}

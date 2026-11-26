using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Domain.Common;

namespace Falah.Client.CallCenterWinUI.ViewModels;

/// <summary>
/// Owner request 2026-11-24: the desk takes two kinds of phone order, chosen first and shown at the top -
/// a delivery (address, delivery service and its fee; cash is paid to the driver) or a pickup (name and
/// phone, an optional time; cash is paid at the counter on collection, UI-0025). The desk used to send
/// every order as a delivery, and its cash button read "عند الاستلام", which the owner took for "pickup".
/// The payment buttons now say who is paid and when, and a line under them says what happens next.
/// </summary>
public partial class CallCenterViewModel
{
    /// <summary>"delivery" or "pickup" - the wire values CheckoutOrderRequest.OrderType takes.</summary>
    [ObservableProperty] private string _orderType = "delivery";

    public bool IsDelivery => OrderType == "delivery";
    public bool IsPickup => OrderType == "pickup";

    [RelayCommand]
    private void SetOrderType(string type)
    {
        if (!CanEditOperation || type is not ("delivery" or "pickup")) return;
        OrderType = type;
    }

    partial void OnOrderTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IsDelivery));
        OnPropertyChanged(nameof(IsPickup));
        OnPropertyChanged(nameof(CashPaymentLabel));
        OnPropertyChanged(nameof(PaymentHint));
        OnPropertyChanged(nameof(OrderTitle));
        OnPropertyChanged(nameof(CustomerEditorTitle));
        OnPropertyChanged(nameof(SubmitLabel));
        OnPropertyChanged(nameof(DeliveryFeeDisplay));
        OnPropertyChanged(nameof(GrandTotalPiastres));
        OnPropertyChanged(nameof(GrandTotalDisplay));
        NotifyDeliverySummary();
        // Owner, 2026-11-25: an offer may be limited to order types; a gift may count the delivery fee.
        RefreshVisibleProducts();
        ReconcileGifts();
    }

    partial void OnPaymentMethodChanged(string value) => OnPropertyChanged(nameof(PaymentHint));

    public string OrderTitle => IsPickup ? "طلب استلام من المطعم" : "طلب توصيل للبيت";
    public string CustomerEditorTitle => IsPickup ? "بيانات العميل والاستلام" : "بيانات العميل والتوصيل";
    public string SubmitLabel => IsPickup ? "تسجيل طلب الاستلام" : "تسجيل طلب التوصيل";

    /// <summary>The cash button names who receives the cash: the driver at the door, or the counter.</summary>
    public string CashPaymentLabel => IsPickup ? "يدفع في المطعم" : "كاش مع الطيار";

    /// <summary>Owner 2026-11-24: the desk offers no payment choice - the buttons were clutter. One line says how the
    /// order is paid. (The view model still sends InstaPay/wallet if PaymentMethod is set; nothing on screen sets it.)</summary>
    public string PaymentHint => (PaymentMethod, IsPickup) switch
    {
        ("cash", false) => "الدفع: كاش مع الطيار لما الأكل يوصل.",
        ("cash", true) => "الدفع: في المطعم لما العميل ييجي ياخد الطلب. الطلب يدخل المطبخ دلوقتي.",
        ("instapay", _) => "الدفع: تحويل إنستاباي، يتأكد بعدين.",
        ("wallet", _) => "الدفع: تحويل من محفظة، يتأكد بعدين.",
        _ => "",
    };

    /// <summary>Delivery only: a pickup never carries the zone's fee, whatever service was chosen before the switch.</summary>
    private long DeliveryFeePiastres => IsDelivery ? SelectedDeliveryService?.FeePiastres ?? 0 : 0;

    public string DeliveryFeeDisplay => IsDelivery && SelectedDeliveryService is not null
        ? $"{DeliveryFeePiastres / 100m:0.00} ج" : "—";

    // ---------------- Pickup time (optional) ----------------

    /// <summary>Optional, as at the till: most pickups are "on the way" and no time is invented for them. Both
    /// pickers must be set for a time to count; what was picked is Cairo wall-clock time.</summary>
    [ObservableProperty] private DateTimeOffset? _pickupDate;
    [ObservableProperty] private TimeSpan? _pickupTime;

    partial void OnPickupDateChanged(DateTimeOffset? value) => NotifyDeliverySummary();
    partial void OnPickupTimeChanged(TimeSpan? value) => NotifyDeliverySummary();

    public DateTimeOffset? RequestedPickupAtUtc
    {
        get
        {
            if (PickupDate is not { } date || PickupTime is not { } time) return null;
            var local = DateTime.SpecifyKind(date.Date.Add(time), DateTimeKind.Unspecified);
            if (BusinessClock.CairoTimeZone.IsInvalidTime(local)) return null;
            return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, BusinessClock.CairoTimeZone), TimeSpan.Zero);
        }
    }

    private string? PickupTimeLine => RequestedPickupAtUtc is { } at
        ? $"موعد الاستلام: {TimeZoneInfo.ConvertTime(at, BusinessClock.CairoTimeZone):yyyy-MM-dd HH:mm}"
        : PickupDate is not null && PickupTime is not null ? "الوقت المختار غير موجود بسبب التوقيت الصيفي. اختر وقتًا آخر." : null;

    [RelayCommand]
    private void ClearPickupTime()
    {
        PickupDate = null;
        PickupTime = null;
    }
}

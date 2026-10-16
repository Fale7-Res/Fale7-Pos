using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Domain.Common;
using Falah.Domain.Delivery;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Pickup order-type details. A pickup is collected at the counter by someone who ordered ahead, so
/// it needs exactly the two fields that let staff match a waiting customer to a waiting bag - the
/// name and the phone, both already owned by the delivery partial and reused here rather than
/// duplicated. No address, no delivery service, and no table: the type that had one is gone.
/// The same fields are re-validated on the server (see CheckoutOrderHandler), because the Call
/// Center reaches the checkout endpoint without passing through this form at all.
/// </summary>
public partial class CashierViewModel
{
    [ObservableProperty] private bool _isPickupEditorOpen;
    [ObservableProperty] private string? _pickupError;

    /// <summary>Optional collection time, split the way the two WinUI pickers want it. Optional is
    /// the normal case - most pickups are "on the way" and must not have a time invented for them,
    /// so both stay null until the cashier actually sets them.</summary>
    [ObservableProperty] private DateTimeOffset? _pickupDate;
    [ObservableProperty] private TimeSpan? _pickupTime;

    partial void OnPickupDateChanged(DateTimeOffset? value) => OnPropertyChanged(nameof(PickupSummary));
    partial void OnPickupTimeChanged(TimeSpan? value) => OnPropertyChanged(nameof(PickupSummary));

    /// <summary>The two pickers as one instant, in UTC. Cairo is the clock the cashier and the
    /// customer share, so what was typed is read as Cairo local and converted once, here - never
    /// stored or compared in local time.
    /// <para>A date with no time is not a booking: someone who opened the calendar and picked a day
    /// has not said WHEN, and half of a time is worse than none of one.</para></summary>
    public DateTimeOffset? RequestedPickupAtUtc
    {
        get
        {
            if (PickupDate is not { } date || PickupTime is not { } time) return null;
            var local = DateTime.SpecifyKind(date.Date.Add(time), DateTimeKind.Unspecified);
            return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, BusinessClock.CairoTimeZone), TimeSpan.Zero);
        }
    }

    private void ClearRequestedPickupTime()
    {
        PickupDate = null;
        PickupTime = null;
    }

    public string PickupSummary
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CustomerPhone)) return "أضف اسم العميل ورقم هاتفه";
            var who = $"{CustomerName} · {CustomerPhone}";
            return RequestedPickupAtUtc is { } at
                ? $"{who}" + Environment.NewLine + $"موعد الاستلام: {TimeZoneInfo.ConvertTime(at, BusinessClock.CairoTimeZone):yyyy-MM-dd HH:mm}"
                : who;
        }
    }

    [RelayCommand]
    private void OpenPickupEditor()
    {
        if (!CanEditOperation) return;
        PickupError = null;
        IsPickupEditorOpen = true;
    }

    [RelayCommand]
    private void ClosePickupEditor() => IsPickupEditorOpen = false;

    /// <summary>Refuses to close on incomplete details rather than closing and failing later at the
    /// payment step, which is where the old delivery form used to strand a cashier.</summary>
    [RelayCommand]
    private void SavePickupDetails()
    {
        var missing = DeliveryRules.FirstMissingPickupField(CustomerPhone, CustomerName);
        if (missing is not null) { PickupError = missing; return; }

        if ((PickupDate is null) != (PickupTime is null))
        {
            PickupError = "موعد الاستلام يحتاج التاريخ والوقت معًا - أو اتركهما فارغين.";
            return;
        }

        try
        {
            Falah.Domain.Orders.CheckoutRules.AssertRequestedPickupTime(
                Falah.Domain.Orders.OrderType.Pickup, RequestedPickupAtUtc, DateTimeOffset.UtcNow);
        }
        catch (Falah.Domain.Common.DomainException ex)
        {
            // Same rule the server applies, run here first so the cashier is told at the form rather
            // than after the payment screen has already opened.
            PickupError = ex.Message;
            return;
        }

        CustomerName = CustomerName.Trim();
        CustomerPhone = CustomerPhone.Trim();
        PickupError = null;
        IsPickupEditorOpen = false;
        OnPropertyChanged(nameof(PickupSummary));
    }
}

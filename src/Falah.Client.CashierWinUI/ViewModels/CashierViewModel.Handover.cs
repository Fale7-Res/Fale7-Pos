using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Orders;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Audit 2026-11-25 (a full trading day on the real apps).
///
/// G1 - handing over a prepaid pickup from the till. The workflow redesign put the handover on the order queue,
/// which lives in the shell; a cashier holding only "pos" lands on the till and never sees the shell, so a prepaid
/// pickup could not be handed over by the person standing at the counter: it stayed "preparing" for ever and its
/// customer's receipt (printed by the handover) never came out. The order lookup the cashier already uses for
/// collecting, refunding and reprinting now hands it over too. No money moves; the server decides (paid, not
/// refunded or cancelled, not handed over) and this only offers the button when it would succeed. One press is one
/// request id, kept until the server answers definitively, so a lost answer is retried as the same handover.
///
/// U15 - an order already partly refunded showed only its total, inviting a second refund of the same item.
/// </summary>
public partial class CashierViewModel
{
    [ObservableProperty] private string? _handOverError;
    [ObservableProperty] private bool _isHandingOver;
    private string _handOverRequestId = Guid.NewGuid().ToString("N");
    private string? _handOverOrderId;

    public bool CanHandOverPickup => OrderLookupResult is
    {
        OrderType: "pickup", IsPaid: true, AwaitingCollection: false, HandedOverAtUtc: null,
    } order && order.Status is not ("cancelled" or "refunded");

    public string LookupRefundLine => OrderLookupResult is { RefundAmountPiastres: > 0 } o
        ? $"اترجّع {CashInput.Display(o.RefundAmountPiastres)} ج — المتبقي {CashInput.Display(Math.Max(0, o.TotalPiastres - o.RefundAmountPiastres))} ج"
        : "";
    public bool HasLookupRefund => LookupRefundLine.Length > 0;

    public string LookupHandedOverLine => OrderLookupResult?.HandedOverAtUtc is { } at
        ? $"اتسلّم للعميل {TimeZoneInfo.ConvertTime(at, Falah.Domain.Common.BusinessClock.CairoTimeZone):HH:mm}"
          + (OrderLookupResult.HandedOverBy is { Length: > 0 } by ? $" ({by})" : "")
        : "";
    public bool HasLookupHandedOver => LookupHandedOverLine.Length > 0;

    /// <summary>Called whenever a different order is looked up (OnOrderLookupResultChanged).</summary>
    private void NotifyLookupHandoverAndRefund(OrderDetailsDto? order)
    {
        OnPropertyChanged(nameof(CanHandOverPickup));
        OnPropertyChanged(nameof(LookupRefundLine)); OnPropertyChanged(nameof(HasLookupRefund));
        OnPropertyChanged(nameof(LookupHandedOverLine)); OnPropertyChanged(nameof(HasLookupHandedOver));
        if (order?.Id != _handOverOrderId) { HandOverError = null; _handOverOrderId = order?.Id; _handOverRequestId = Guid.NewGuid().ToString("N"); }
    }

    partial void OnIsHandingOverChanged(bool value) => RefreshOperationState();

    [RelayCommand]
    private async Task HandOverPickupAsync()
    {
        if (IsHandingOver || !CanHandOverPickup || OrderLookupResult is not { } order) return;
        IsHandingOver = true;
        HandOverError = null;
        try
        {
            var result = await _session.Api.HandOverPickupAsync(new HandOverPickupRequest(_handOverRequestId, order.Id, _session.CurrentUser!.Id));
            if (!result.Success || result.Value is not { } done)
            {
                // A definitive refusal ends this press; a lost answer keeps its id so the retry is the same handover.
                if (!result.OutcomeUnknown) _handOverRequestId = Guid.NewGuid().ToString("N");
                HandOverError = result.Error ?? "تعذر تسليم الطلب. أعد المحاولة.";
                return;
            }
            _handOverRequestId = Guid.NewGuid().ToString("N");
            OrderLookupResult = order with { Status = "completed", HandedOverAtUtc = done.HandedOverAtUtc, HandedOverBy = _session.CurrentUser.Name };
            ReprintStatusMessage = $"اتسلّم الطلب {done.OrderNumber} للعميل"
                + (done.PrintJobsQueued ? " واتبعت إيصاله للطباعة." : " - الإيصال ما اتبعتش للطباعة، اطبعه من هنا.");
        }
        finally { IsHandingOver = false; }
    }
}

using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>Audit 2026-11-25 - findings from a full trading day driven through the real Cashier app.</summary>
public partial class CashierViewModel
{
    /// <summary>An order number such as "#0130" (or a date) kept left-to-right inside Arabic text, where the leading
    /// "#" is a neutral character and was laid out on the wrong side ("0130#") - U10. Left-to-right marks on both
    /// sides: WinUI's text layout ignored the isolate marks (U+2066/U+2069) tried first.</summary>
    internal static string Ltr(string? text) => string.IsNullOrEmpty(text) ? "" : LeftToRightMark + text + LeftToRightMark;
    private static readonly string LeftToRightMark = ((char)0x200E).ToString();

    public string LastCheckoutOrderNumberDisplay => Ltr(LastCheckoutResult?.OrderNumber);

    // U9: the result card showed a green tick for a transfer NOT received and for a cancelled order.
    public bool ShowCancelledMark => LastCheckoutResult?.Status == "cancelled";
    public bool ShowPendingMark => !ShowCancelledMark && IsElectronicPaymentPending;
    public bool ShowSuccessMark => LastCheckoutResult is not null && !ShowCancelledMark && !ShowPendingMark;

    private void NotifyResultCard()
    {
        OnPropertyChanged(nameof(LastCheckoutOrderNumberDisplay));
        OnPropertyChanged(nameof(ShowCancelledMark)); OnPropertyChanged(nameof(ShowPendingMark)); OnPropertyChanged(nameof(ShowSuccessMark));
    }

    // U14: the amount defaults to the whole refundable balance; the button says what it will refund.
    public string RefundConfirmText => CashInput.TryParse(RefundAmountText, out var amount) && amount > 0
        ? $"تأكيد استرداد {CashInput.Display(amount)} ج" : "تأكيد الاسترداد";

    // U21: what the till itself put in "amount received" (the total when the payment opened). While the cashier has
    // not typed over it, a change of total (a comp, a discount) moves it with the total.
    private long? _prefilledTenderPiastres;

    private void FollowTotalWithPrefilledTender()
    {
        if (!IsCheckoutOpen || PaymentMethod != "cash" || !CanEditOperation || _prefilledTenderPiastres is not { } prefilled) return;
        if (!CashInput.TryParse(AmountPaidText, out var current) || current != prefilled || prefilled == GrandTotalPiastres) return;
        AmountPaidText = CashInput.Format(GrandTotalPiastres);
        _prefilledTenderPiastres = GrandTotalPiastres;
        _replaceTenderOnNextKey = true;
    }
}

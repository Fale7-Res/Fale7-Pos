using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Orders;

namespace Falah.Client.Cashier.ViewModels;

public partial class CashierViewModel
{
    [ObservableProperty] private bool _isCheckoutOpen;
    [ObservableProperty] private string _paymentMethod = "cash";
    [ObservableProperty] private string _amountPaidText = string.Empty;
    [ObservableProperty] private string? _checkoutError;
    [ObservableProperty] private bool _isProcessingCheckout;

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
        OnPropertyChanged(nameof(ShowConfirmElectronicPaymentButton));
    }

    public long ChangeDuePiastres
    {
        get
        {
            if (!long.TryParse(AmountPaidText, out var paidPiastresInput)) return 0;
            var diff = paidPiastresInput - GrandTotalPiastres;
            return diff > 0 ? diff : 0;
        }
    }

    [RelayCommand]
    private void OpenCheckout()
    {
        if (Cart.Count == 0) { CheckoutError = "السلة فارغة."; return; }
        CheckoutError = null;
        PaymentMethod = "cash";
        AmountPaidText = (GrandTotalPiastres / 100m).ToString("0.00");
        CurrentCheckoutRequestId ??= $"req-{Guid.NewGuid():N}";
        IsCheckoutOpen = true;
    }

    [RelayCommand]
    private void CloseCheckout() => IsCheckoutOpen = false;

    [RelayCommand]
    private void SetPaymentMethod(string method)
    {
        PaymentMethod = method;
        // Non-cash methods always collect exactly the total - only cash needs a typed amount.
        if (method != "cash") AmountPaidText = (GrandTotalPiastres / 100m).ToString("0.00");
    }

    [RelayCommand]
    private void PressAmountKey(string key)
    {
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
        AmountPaidText = amountEgp;
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
            if (!decimal.TryParse(AmountPaidText, out var amountEgp)) amountEgp = 0;
            var amountPiastres = (long)Math.Round(amountEgp * 100m);

            var request = new CheckoutOrderRequest(
                CheckoutRequestId: CurrentCheckoutRequestId!,
                CashierId: _session.CurrentUser!.Id,
                ShiftId: _session.ShiftId!,
                OrderType: OrderType,
                PaymentMethod: PaymentMethod,
                Items: Cart.Select(l => new CheckoutItemRequest(l.ProductId, l.VariantId, l.Quantity, l.AppliedOfferId, l.Notes)).ToList(),
                DiscountPiastres: 0,
                DeliveryFeePiastres: 0,
                CompAmountPiastres: 0,
                CompApprovalId: null,
                CompApproverUserId: null,
                CompReason: null,
                AmountPaidPiastres: amountPiastres,
                SplitPayments: null,
                WalletProvider: null,
                // This client has no table field of its own; the WinUI till is where a dine-in
                // order is taken. Sending nothing is the honest answer, not a made-up table.
                TableNumber: null,
                CustomerName: null,
                CustomerPhone: null,
                DeliveryAddress: null,
                DeliveryArea: null,
                BusinessDayId: null);

            var result = await _session.Api.CheckoutAsync(request);
            if (!result.Success || result.Value is null)
            {
                // The request id is kept so a retry of the SAME payment can never become a second
                // order - only "إلغاء" / starting a new order clears it.
                CheckoutError = result.Error ?? "تعذر إتمام عملية الدفع.";
                return;
            }

            LastCheckoutResult = result.Value;
            IsCheckoutOpen = false;
            Cart.Clear();
            RecomputeCartTotals();
            CurrentCheckoutRequestId = null;
        }
        finally
        {
            IsProcessingCheckout = false;
        }
    }

    [RelayCommand]
    private void StartNewOrder()
    {
        LastCheckoutResult = null;
        CheckoutError = null;
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Orders;

namespace Falah.Client.Cashier.ViewModels;

public partial class CashierViewModel
{
    [ObservableProperty] private bool _isConfirmingElectronicPayment;
    [ObservableProperty] private string? _confirmElectronicPaymentError;

    public bool ShowConfirmElectronicPaymentButton =>
        LastCheckoutResult is { ElectronicPaymentStatus: "pending" };

    [RelayCommand]
    private async Task ConfirmElectronicPaymentAsync()
    {
        if (LastCheckoutResult is null) return;
        IsConfirmingElectronicPayment = true;
        ConfirmElectronicPaymentError = null;
        try
        {
            var request = new ConfirmElectronicPaymentRequest(LastCheckoutResult.OrderId, _session.CurrentUser!.Id);
            var result = await _session.Api.ConfirmElectronicPaymentAsync(request);
            if (!result.Success || result.Value is null)
            {
                ConfirmElectronicPaymentError = result.Error ?? "تعذر تأكيد الدفع الإلكتروني.";
                return;
            }

            LastCheckoutResult = LastCheckoutResult with
            {
                IsPaid = result.Value.IsPaid,
                ElectronicPaymentStatus = result.Value.ElectronicPaymentStatus,
            };
        }
        finally
        {
            IsConfirmingElectronicPayment = false;
        }
    }
}

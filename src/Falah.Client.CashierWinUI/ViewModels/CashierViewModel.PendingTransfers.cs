using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Orders;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>Owner decision 2026-11-25 (audit U19): a transfer the cashier chose to "review later" could only be found again
/// by remembering its order number. The order lookup lists every InstaPay/wallet transfer still waiting; picking one
/// opens it there, where confirming or cancelling it already lives (the same commands, durability and approvals).</summary>
public partial class CashierViewModel
{
    public ObservableCollection<PendingTransferDto> PendingTransfers { get; } = new();
    public bool HasPendingTransfers => PendingTransfers.Count > 0;

    [RelayCommand]
    private async Task LoadPendingTransfersAsync()
    {
        var result = await _session.Api.GetPendingTransfersAsync();
        PendingTransfers.Clear();
        if (result.Success && result.Value is not null) foreach (var row in result.Value) PendingTransfers.Add(row);
        else OrderLookupError = result.Error ?? "تعذر تحميل التحويلات المعلّقة.";
        OnPropertyChanged(nameof(HasPendingTransfers));
        if (result.Success && PendingTransfers.Count == 0) ReprintStatusMessage = "ما فيش تحويلات مستنية تأكيد.";
    }

    [RelayCommand]
    private async Task OpenPendingTransferAsync(PendingTransferDto? transfer)
    {
        if (transfer is null) return;
        OrderLookupNumber = transfer.OrderNumber;
        await SearchOrderAsync();
    }
}

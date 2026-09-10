using CommunityToolkit.Mvvm.ComponentModel;

namespace Falah.Client.CallCenterWinUI.ViewModels;

/// <summary>
/// Operation-state gating, mirroring CashierWinUI's own CashierViewModel.State: while a financial
/// command is in flight - or worse, has come back with an UNKNOWN outcome - the order must not be
/// editable, cleared, or abandoned, because re-entering it from scratch is exactly how a phoned-in
/// order becomes two real orders. The retained checkout request id is what makes the retry safe;
/// this flag is what stops anyone from bypassing it.
/// </summary>
public partial class CallCenterViewModel
{
    /// <summary>Which operator this retained draft belongs to - AppSession hands the same desk back
    /// only to the same user, so unlocking as someone else never inherits their half-taken order.</summary>
    public string OwnerUserId { get; }

    [ObservableProperty] private bool _isInitializing;
    [ObservableProperty] private string? _sessionError;

    public bool IsOperationBusy => IsProcessingCheckout || IsReprinting || IsLookingUpOrder;

    /// <summary>False while anything is in flight or an outcome is still unresolved. The views bind
    /// their IsHitTestVisible to this rather than each individual busy flag.</summary>
    public bool CanEditOperation => !IsOperationBusy && _pendingCheckout is null && !_hasUnresolvedPrint;

    partial void OnIsProcessingCheckoutChanged(bool value) => RefreshOperationState();
    partial void OnIsReprintingChanged(bool value) => RefreshOperationState();
    partial void OnIsLookingUpOrderChanged(bool value) => RefreshOperationState();

    private void RefreshOperationState()
    {
        OnPropertyChanged(nameof(IsOperationBusy));
        OnPropertyChanged(nameof(CanEditOperation));
        SubmitOrderCommand.NotifyCanExecuteChanged();
        _header.Refresh();
    }
}

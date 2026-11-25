using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Delivery;
using Falah.Client.CallCenterWinUI.Services;

namespace Falah.Client.CallCenterWinUI.ViewModels;

/// <summary>
/// Owner's trial 2026-11-25: an order out with one driver goes to another. The server has always done it
/// (POST /api/delivery/{id}/reassign: the same custody moves to the new driver, a dispatch slip prints, the audit log
/// names both drivers and the reason); no screen called it. Offered to a dispatcher (delivery_manage) for an order that
/// is out - after its outcome is recorded the server asks for delivery_override, which this screen does not offer.
/// Kept on disk until its answer is known, like the assignment, so a lost answer is resent, never sent twice.
/// </summary>
public partial class DeliveryViewModel
{
    private readonly RetainedRequest<ReassignDeliveryRequest> _reassignRequest = new("delivery.reassign");

    [ObservableProperty] private QueueCard? _reassignTarget;
    [ObservableProperty] private DriverDto? _reassignDriver;
    [ObservableProperty] private string _reassignReason = "";
    [ObservableProperty] private string? _reassignError;
    private string? _reassignRequestId;

    public bool IsReassignOpen => ReassignTarget is not null;
    public string ReassignTitle => ReassignTarget is { } t ? $"نقل الطلب {t.OrderNumber} من {t.DriverName}" : "";
    /// <summary>Every active driver except the one who has the order now.</summary>
    public IReadOnlyList<DriverDto> ReassignDriverOptions => ReassignTarget is { } t
        ? Drivers.Where(d => d.Id != t.Order.DriverId).ToList() : Array.Empty<DriverDto>();

    partial void OnReassignTargetChanged(QueueCard? value)
    {
        OnPropertyChanged(nameof(IsReassignOpen)); OnPropertyChanged(nameof(ReassignTitle)); OnPropertyChanged(nameof(ReassignDriverOptions));
        OnPropertyChanged(nameof(CanAutoRefresh));
    }

    [RelayCommand]
    private void OpenReassign(QueueCard card)
    {
        if (!CanEditAction || !CanDispatch || !card.CanReassign) return;
        ActionError = null;
        ActionNote = null;
        ReassignDriver = null;
        ReassignReason = "";
        ReassignError = null;
        _reassignRequestId = $"rsg-{Guid.NewGuid():N}";
        ReassignTarget = card;
    }

    [RelayCommand]
    private void CloseReassign()
    {
        if (_reassignRequest.HasPending) { ReassignError = _reassignRequest.FailureMessage(null); return; }
        if (CanEditAction) ReassignTarget = null;
    }

    [RelayCommand]
    private async Task ConfirmReassignAsync()
    {
        if (IsBusy || !CanDispatch) return;
        if (_reassignRequest.Value is { } pending)
        {
            IsBusy = true;
            try { await CompleteReassignAsync(pending); }
            finally { IsBusy = false; }
            return;
        }
        if (ReassignTarget is not { } card || card.Order.DeliveryId is not { } deliveryId) return;
        if (ReassignDriver is null) { ReassignError = "اختار السائق اللي هياخد الطلب."; return; }
        if (string.IsNullOrWhiteSpace(ReassignReason)) { ReassignError = "اكتب سبب النقل."; return; }
        IsBusy = true;
        try
        {
            await CompleteReassignAsync(new ReassignDeliveryRequest(_reassignRequestId!, deliveryId, ReassignDriver.Id,
                ReassignReason.Trim(), _session.CurrentUser!.Id, null, null));
        }
        finally { IsBusy = false; }
    }

    private async Task CompleteReassignAsync(ReassignDeliveryRequest request)
    {
        ReassignError = null;
        var result = await _reassignRequest.SendAsync(request, r => _session.Api.ReassignDeliveryAsync(r));
        if (!result.Success || result.Value is null) { ReassignError = _reassignRequest.FailureMessage(result.Error); return; }
        var number = ReassignTarget?.OrderNumber ?? "";
        ActionNote = result.Value.WasNoOp
            ? $"الطلب {number} مع {result.Value.DriverName} أصلًا."
            : $"اتنقل الطلب {number} إلى {result.Value.DriverName} - ورقة التسليم اتبعتت للطباعة.";
        ReassignTarget = null;
        _reassignRequestId = null;
        await LoadAsync();
    }
}

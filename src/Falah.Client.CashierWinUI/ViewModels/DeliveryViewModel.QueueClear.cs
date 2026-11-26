using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Orders;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Owner, 2026-11-25 («ترحيل»): at the end of a shift the queue's screen is cleared for the next one. The operator
/// ticks delivery, pickup or both; the window warns how many orders are still in progress - those stay and leave by
/// themselves once they finish. Nothing is deleted (POST /api/orders/queue/clear records who cleared and when).
/// </summary>
public partial class DeliveryViewModel
{
    [ObservableProperty] private bool _isQueueClearOpen;
    [ObservableProperty] private bool _queueClearDeliveries = true;
    [ObservableProperty] private bool _queueClearPickups = true;
    [ObservableProperty] private string? _queueClearError;
    private string? _queueClearRequestId;

    /// <summary>The same rule as the server's OrderQueueQueryService.IsFinished.</summary>
    internal static bool IsFinished(QueueOrderDto q) => q.OrderType == "delivery"
        ? q.Stage is "delivered" or "failed" or "returned" && !q.NeedsRefund
        : q.Stage == "completed";

    private IEnumerable<QueueOrderDto> QueueClearChosen() =>
        (QueueClearDeliveries ? DeliveryStages.SelectMany(s => s.Cards) : Enumerable.Empty<QueueCard>())
        .Concat(QueueClearPickups ? PickupStages.SelectMany(s => s.Cards) : Enumerable.Empty<QueueCard>())
        .Select(c => c.Order);

    public int QueueClearFinishedCount => QueueClearChosen().Count(IsFinished);
    public int QueueClearInProgressCount => QueueClearChosen().Count(q => !IsFinished(q));
    public bool HasQueueClearWarning => QueueClearInProgressCount > 0;
    public string QueueClearSummary => $"هيتشال من الواجهة {QueueClearFinishedCount} طلب خلص. الطلبات ما بتتمسحش، وبتفضل في التقارير وحساب السائق.";
    public string QueueClearWarning => $"تنبيه: {QueueClearInProgressCount} طلب لسه ما خلصش (بيتحضّر أو جاهز أو مع السائق أو مستني العميل). هيفضلوا في الطابور، وكل واحد يتشال لوحده أول ما يخلص.";

    partial void OnIsQueueClearOpenChanged(bool value) => OnPropertyChanged(nameof(CanAutoRefresh));
    partial void OnQueueClearDeliveriesChanged(bool value) => NotifyQueueClear();
    partial void OnQueueClearPickupsChanged(bool value) => NotifyQueueClear();

    private void NotifyQueueClear()
    {
        foreach (var n in new[] { nameof(QueueClearFinishedCount), nameof(QueueClearInProgressCount), nameof(HasQueueClearWarning),
                     nameof(QueueClearSummary), nameof(QueueClearWarning) })
            OnPropertyChanged(n);
    }

    [RelayCommand]
    private void OpenQueueClear()
    {
        if (!CanEditAction) return;
        ActionError = null;
        ActionNote = null;
        QueueClearDeliveries = true;
        QueueClearPickups = true;
        QueueClearError = null;
        _queueClearRequestId = $"qclr-{Guid.NewGuid():N}";
        NotifyQueueClear();
        IsQueueClearOpen = true;
    }

    [RelayCommand]
    private void CloseQueueClear() { if (!IsBusy) IsQueueClearOpen = false; }

    [RelayCommand]
    private async Task ConfirmQueueClearAsync()
    {
        if (IsBusy || !IsQueueClearOpen) return;
        if (!QueueClearDeliveries && !QueueClearPickups) { QueueClearError = "اختار التوصيل أو الاستلام من الفرع."; return; }
        IsBusy = true;
        QueueClearError = null;
        try
        {
            var result = await _session.Api.ClearOrderQueueAsync(new ClearOrderQueueRequest(
                _queueClearRequestId!, QueueClearDeliveries, QueueClearPickups, _session.CurrentUser!.Id));
            if (!result.Success || result.Value is null) { QueueClearError = result.Error ?? "تعذر ترحيل الطابور."; return; }
            ActionNote = $"اتشال من الواجهة {result.Value.Removed} طلب خلص"
                + (result.Value.Kept > 0 ? $"، وفضل {result.Value.Kept} لسه شغال." : ".");
            IsQueueClearOpen = false;
            _queueClearRequestId = null;
        }
        finally { IsBusy = false; }
        await LoadAsync();
    }
}

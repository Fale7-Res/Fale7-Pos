using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Falah.Application.Delivery;

namespace Falah.Client.CallCenterWinUI.ViewModels;

/// <summary>Owner, 2026-11-26: about 50 delivery services - the order taker types part of the name and picks from
/// suggestions that show the fee (<see cref="DeliveryServiceSearch"/>). Editing the text drops a service chosen
/// before, so its fee never stays on the order unseen; typing a whole name selects it.</summary>
public partial class CallCenterViewModel
{
    [ObservableProperty] private string _deliveryServiceQuery = "";
    public ObservableCollection<DeliveryServiceDto> DeliveryServiceSuggestions { get; } = new();
    private bool _typingDeliveryService;

    public void SearchDeliveryServices(string? text)
    {
        _typingDeliveryService = true;
        try
        {
            DeliveryServiceQuery = text ?? "";
            var exact = DeliveryServiceSearch.Exact(DeliveryServices, DeliveryServiceQuery);
            if (exact is not null) SelectedDeliveryService = exact;
            else if (SelectedDeliveryService is not null) SelectedDeliveryService = null;
        }
        finally { _typingDeliveryService = false; }
        DeliveryServiceSuggestions.Clear();
        foreach (var s in DeliveryServiceSearch.Find(DeliveryServices, DeliveryServiceQuery)) DeliveryServiceSuggestions.Add(s);
    }

    private void SyncDeliveryServiceQuery(DeliveryServiceDto? value)
    {
        if (!_typingDeliveryService) DeliveryServiceQuery = value?.Name ?? "";
    }
}

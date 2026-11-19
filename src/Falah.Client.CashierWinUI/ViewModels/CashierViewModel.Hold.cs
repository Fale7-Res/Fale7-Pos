using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Delivery;
using Falah.Client.CashierWinUI.Models;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>Everything one basket remembers while another is on screen.</summary>
public sealed class BasketContents
{
    public required IReadOnlyList<CartLine> Lines { get; init; }
    public required string OrderType { get; init; }
    public string CustomerName { get; init; } = "";
    public string CustomerPhone { get; init; } = "";
    public string DeliveryArea { get; init; } = "";
    public string DeliveryStreet { get; init; } = "";
    public string DeliveryBuilding { get; init; } = "";
    public string DeliveryFloor { get; init; } = "";
    public string DeliveryApartment { get; init; } = "";
    public string DeliveryLandmark { get; init; } = "";
    public string DeliveryNotes { get; init; } = "";
    public DeliveryServiceDto? DeliveryService { get; init; }
    public DateTimeOffset? PickupDate { get; init; }
    public TimeSpan? PickupTime { get; init; }
}

/// <summary>One basket tab above the cart: «سلة 1», «سلة 2» …</summary>
public sealed partial class BasketTab : ObservableObject
{
    public required int Number { get; init; }
    public string Title => $"سلة {Number}";
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private int _itemCount;
    [ObservableProperty] private bool _canClose;
    /// <summary>What the basket holds while it is not on screen; null while it is the active one.</summary>
    public BasketContents? Stored { get; set; }
    public string Badge => ItemCount > 0 ? ItemCount.ToString() : "";
    public bool HasItems => ItemCount > 0;
    partial void OnItemCountChanged(int value) { OnPropertyChanged(nameof(Badge)); OnPropertyChanged(nameof(HasItems)); }
}

/// <summary>
/// Owner, 2026-11-17/18: more than one order at a time on the till. He chose fixed basket tabs above
/// the cart - «سلة 1 | سلة 2 | … | +» - over a hold button. The cart on screen is always the active
/// basket's; the others keep their items, type and customer details until tapped. Baskets live only
/// on this screen: nothing is sent to the server and no money moves until a basket is paid, exactly as
/// for an order still being rung up, and they do not survive closing the app or logging out. Switching
/// is refused on the payment screen and while any operation is unresolved (CanEditOperation). A basket
/// closes with × only when it is empty and not the last one; an order is dropped with «إلغاء الطلب».
/// </summary>
public partial class CashierViewModel
{
    public ObservableCollection<BasketTab> Baskets { get; } = new() { new BasketTab { Number = 1, IsActive = true } };
    private BasketTab ActiveBasket => Baskets.First(b => b.IsActive);
    private bool CanSwitchBaskets => CanEditOperation && !IsCheckoutOpen;

    [RelayCommand]
    private void AddBasket()
    {
        if (!CanSwitchBaskets) return;
        var current = ActiveBasket;
        current.Stored = SnapshotCurrentOrder();
        current.IsActive = false;
        Baskets.Add(new BasketTab { Number = Baskets.Max(b => b.Number) + 1, IsActive = true });
        StartEmptyOrder();
        RefreshBasketTabs();
    }

    [RelayCommand]
    private void SwitchBasket(BasketTab? target)
    {
        if (target is null || target.IsActive || !CanSwitchBaskets || !Baskets.Contains(target)) return;
        var current = ActiveBasket;
        current.Stored = SnapshotCurrentOrder();
        current.IsActive = false;
        target.IsActive = true;
        var restore = target.Stored;
        target.Stored = null;
        StartEmptyOrder();
        if (restore is not null) Restore(restore);
        RefreshBasketTabs();
    }

    [RelayCommand]
    private void CloseBasket(BasketTab? basket)
    {
        if (basket is null || !CanSwitchBaskets || Baskets.Count < 2 || !Baskets.Contains(basket)) return;
        if (BasketItemCount(basket) > 0) return;
        var index = Baskets.IndexOf(basket);
        var wasActive = basket.IsActive;
        Baskets.Remove(basket);
        if (wasActive)
        {
            var next = Baskets[Math.Min(index, Baskets.Count - 1)];
            next.IsActive = true;
            var restore = next.Stored;
            next.Stored = null;
            StartEmptyOrder();
            if (restore is not null) Restore(restore);
        }
        RefreshBasketTabs();
    }

    private int BasketItemCount(BasketTab basket) =>
        basket.IsActive ? Cart.Sum(l => l.Quantity) : basket.Stored?.Lines.Sum(l => l.Quantity) ?? 0;

    /// <summary>Badges and × buttons follow the cart; called whenever the cart's totals change.</summary>
    private void RefreshBasketTabs()
    {
        foreach (var basket in Baskets)
        {
            basket.ItemCount = BasketItemCount(basket);
            basket.CanClose = Baskets.Count > 1 && basket.ItemCount == 0;
        }
    }

    private void Restore(BasketContents held)
    {
        OrderType = held.OrderType;
        CustomerName = held.CustomerName; CustomerPhone = held.CustomerPhone;
        DeliveryArea = held.DeliveryArea; DeliveryStreet = held.DeliveryStreet; DeliveryBuilding = held.DeliveryBuilding;
        DeliveryFloor = held.DeliveryFloor; DeliveryApartment = held.DeliveryApartment; DeliveryLandmark = held.DeliveryLandmark;
        DeliveryNotes = held.DeliveryNotes; SelectedDeliveryService = held.DeliveryService;
        PickupDate = held.PickupDate; PickupTime = held.PickupTime;
        // Changing the type opens its details editor; a restored basket already has its details.
        IsDeliveryEditorOpen = false; IsPickupEditorOpen = false;
        foreach (var line in held.Lines) Cart.Add(line);
        RecomputeCartTotals();
        OnPropertyChanged(nameof(DeliverySummary));
    }

    private BasketContents SnapshotCurrentOrder() => new()
    {
        Lines = Cart.ToList(), OrderType = OrderType,
        CustomerName = CustomerName, CustomerPhone = CustomerPhone, DeliveryArea = DeliveryArea,
        DeliveryStreet = DeliveryStreet, DeliveryBuilding = DeliveryBuilding, DeliveryFloor = DeliveryFloor,
        DeliveryApartment = DeliveryApartment, DeliveryLandmark = DeliveryLandmark, DeliveryNotes = DeliveryNotes,
        DeliveryService = SelectedDeliveryService, PickupDate = PickupDate, PickupTime = PickupTime,
    };

    /// <summary>The same reset as clearing the cart, plus back to takeaway: a new basket is a new customer.</summary>
    private void StartEmptyOrder()
    {
        Cart.Clear();
        IsDiscountEnabled = false; IsCompEnabled = false; IsSplitPaymentEnabled = false;
        OrderType = "takeaway";
        ResetDelivery();
        IsDeliveryEditorOpen = false; IsPickupEditorOpen = false;
        RecomputeCartTotals();
        CurrentCheckoutRequestId = null;
    }
}

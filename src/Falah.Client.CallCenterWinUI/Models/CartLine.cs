using CommunityToolkit.Mvvm.ComponentModel;

namespace Falah.Client.CallCenterWinUI.Models;

/// <summary>
/// A client-side cart line. UnitPricePiastres here is DISPLAY ONLY, read from the last menu
/// snapshot this client fetched - it can be stale or wrong with zero financial consequence,
/// because checkout never sends a price. The server independently re-resolves the real price from
/// ProductId/VariantId and freezes whatever it finds at that moment; this value only helps the
/// cashier see a running total before they commit.
/// </summary>
public partial class CartLine : ObservableObject
{
    public required string ProductId { get; init; }
    public string? VariantId { get; init; }
    public required string ProductName { get; init; }
    public string? VariantName { get; init; }
    public bool SendToGrill { get; init; }
    public long UnitPricePiastres { get; init; }
    public string? AppliedOfferId { get; init; }
    public string? AppliedOfferName { get; init; }

    [ObservableProperty]
    private int _quantity = 1;

    [ObservableProperty]
    private string? _notes;

    public long LineTotalPiastres => UnitPricePiastres * Quantity;
    public string LineTotalDisplay => $"{LineTotalPiastres / 100m:0.00} ج";
    public string UnitPriceDisplay => $"{UnitPricePiastres / 100m:0.00} ج";

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(LineTotalPiastres));
        OnPropertyChanged(nameof(LineTotalDisplay));
    }
}

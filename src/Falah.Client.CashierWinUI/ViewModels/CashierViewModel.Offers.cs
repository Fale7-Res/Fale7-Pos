using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Menu;
using Falah.Client.CashierWinUI.Models;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Owner, 2026-11-25: offers of every shape on this station. An item offer is a button on the item, shown only for the
/// order types and stations it allows, priced by its own kind for the size chosen (the same rule the server charges).
/// A gift offer follows the order's total (with or without the delivery fee, as the offer says): the highest tier
/// reached is put in the cart by itself, or offered as a button, as the offer says; a gift the order no longer earns
/// leaves the cart. The menu - and with it every offer - is refreshed every minute, so an offer added, changed, started
/// or ended reaches the till without a new shift.
/// </summary>
public partial class CashierViewModel
{
    /// <summary>Which station this is, for an offer limited to one.</summary>
    public const string Station = "till";
    public static readonly TimeSpan MenuRefreshInterval = TimeSpan.FromMinutes(1);

    private IReadOnlyList<MenuGiftOfferDto> _giftOffers = Array.Empty<MenuGiftOfferDto>();
    private bool _reconcilingGifts;

    [ObservableProperty] private string? _giftSuggestion;
    private (MenuGiftOfferDto Offer, MenuGiftTierDto Tier)? _suggestedGift;
    public bool HasGiftSuggestion => GiftSuggestion is not null;
    partial void OnGiftSuggestionChanged(string? value) => OnPropertyChanged(nameof(HasGiftSuggestion));

    /// <summary>The item offers this order may use.</summary>
    private IEnumerable<MenuOfferDto> OffersFor(MenuProductDto product) => product.ApplicableOffers.Where(o => o.AllowsOrder(OrderType, Station));

    private long GiftBasis(MenuGiftOfferDto offer)
    {
        var fee = OrderType == "delivery" ? SelectedDeliveryService?.FeePiastres ?? 0 : 0;
        return offer.IncludesDelivery ? GrandTotalPiastres : GrandTotalPiastres - fee;
    }

    /// <summary>Brings the cart's gifts in line with the total: an auto gift is added or resized, a gift the order no
    /// longer earns is removed, and a gift the cashier adds by hand is offered.</summary>
    private void ReconcileGifts()
    {
        if (_reconcilingGifts) return;
        _reconcilingGifts = true;
        try
        {
            (MenuGiftOfferDto, MenuGiftTierDto)? suggestion = null;
            var changed = false;
            foreach (var line in Cart.Where(l => l.IsGift && !_giftOffers.Any(g => g.Id == l.AppliedOfferId && g.AllowsOrder(OrderType, Station))).ToList())
            { Cart.Remove(line); changed = true; }

            foreach (var offer in _giftOffers.Where(g => g.AllowsOrder(OrderType, Station)))
            {
                var lines = Cart.Where(l => l.IsGift && l.AppliedOfferId == offer.Id).ToList();
                var tier = Cart.Any(l => !l.IsGift) ? offer.TierFor(GiftBasis(offer)) : null;
                foreach (var stale in lines.Where(l => tier is null || l.ProductId != tier.ProductId || l.VariantId != tier.VariantId).ToList())
                { Cart.Remove(stale); lines.Remove(stale); changed = true; }
                if (tier is null) continue;

                var current = lines.FirstOrDefault();
                if (current is not null && current.Quantity > tier.Quantity) { current.Quantity = tier.Quantity; changed = true; }
                if (offer.AutoAdd)
                {
                    if (current is null) { Cart.Add(GiftLine(offer, tier)); changed = true; }
                    else if (current.Quantity < tier.Quantity) { current.Quantity = tier.Quantity; changed = true; }
                }
                else if (current is null || current.Quantity < tier.Quantity) suggestion ??= (offer, tier);
            }

            _suggestedGift = suggestion;
            GiftSuggestion = suggestion is { } s
                ? $"🎁 الطلب يستحق هدية «{s.Item1.Name}»: {s.Item2.Quantity} × {s.Item2.ProductName}{(s.Item2.VariantName is { } v ? $" ({v})" : "")}"
                : null;
            if (changed) RecomputeCartTotals();
        }
        finally { _reconcilingGifts = false; }
    }

    private static CartLine GiftLine(MenuGiftOfferDto offer, MenuGiftTierDto tier) => new()
    {
        ProductId = tier.ProductId, VariantId = tier.VariantId, ProductName = tier.ProductName, VariantName = tier.VariantName,
        UnitPricePiastres = 0, AppliedOfferId = offer.Id, AppliedOfferName = offer.Name, IsGift = true, Quantity = tier.Quantity,
    };

    [RelayCommand]
    private void AddGift()
    {
        if (!CanEditOperation || _suggestedGift is not { } s) return;
        var current = Cart.FirstOrDefault(l => l.IsGift && l.AppliedOfferId == s.Offer.Id);
        if (current is null) Cart.Add(GiftLine(s.Offer, s.Tier)); else current.Quantity = s.Tier.Quantity;
        RecomputeCartTotals();
    }

    /// <summary>The menu again, without the loading state and keeping the section on screen - the till's offers stay
    /// current through the shift. A failure keeps the menu already shown.</summary>
    [RelayCommand]
    private Task RefreshMenuQuietlyAsync() => IsLoadingMenu || !IsShiftOpen || IsVariantPickerOpen ? Task.CompletedTask : LoadMenuAsync(quiet: true);
}

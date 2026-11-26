using CommunityToolkit.Mvvm.ComponentModel;
using Falah.Application.Menu;

namespace Falah.Client.CashierWinUI.Models;

/// <summary>Pre-pairs a product with its currently-applicable offers so the view never has to walk
/// back up a nested items list to find "which product does this offer button belong to" - that
/// pairing is decided once here, in C#, instead of via fragile ancestor lookups in the view.</summary>
public sealed record OfferChoice(MenuProductDto Product, MenuOfferDto Offer)
{
    /// <summary>What a screen reader and UI Automation call the offer pill: what is printed on it.</summary>
    public override string ToString() => $"{Offer.Name} {Offer.PriceLabel}";
    public string AccessibleName => ToString();
}

public sealed record ProductTile(MenuProductDto Product, IReadOnlyList<OfferChoice> Offers, Uri? ImageUri = null, string? CategoryIcon = null, bool IsBestSeller = false)
{
    /// <summary>Whether this product has a photo. Not having one is the ordinary case, not a
    /// failure: a restaurant that has not photographed its menu still has to be able to sell from
    /// it, so the card falls back to the category's icon rather than to an empty frame. The Uri
    /// itself is turned into an image by the view - a model has no business holding a bitmap.</summary>
    public bool HasImage => ImageUri is not null;

    /// <summary>The tile's accessible name. A GridView item with no explicit name is named by its data
    /// item's ToString(); the compiler-generated record dump was being read out instead.</summary>
    public override string ToString() => $"{Product.Name}، {StartingPriceDisplay}";

    /// <summary>What the card shows in the photo's place. The category's own icon says something
    /// true about the item; a generic placeholder would say nothing.</summary>
    public string FallbackIcon => string.IsNullOrWhiteSpace(CategoryIcon) ? "🍽️" : CategoryIcon!;

    public string StartingPriceDisplay => (Product.Variants.Count > 1 ? "من " : "") +
        Services.CashInput.Display(Product.Variants.Count > 0 ? Product.Variants.Min(v => v.PricePiastres) : Product.BasePricePiastres) + " ج";

    /// <summary>Which kitchen station this item is actually routed to, from the product's real
    /// PreparationStation (the same field that decides its print job - see PrintRules). Null for
    /// items with no kitchen routing, so the card shows no badge at all rather than a decorative
    /// icon that means nothing.</summary>
    public string? StationLabel => Product.PreparationStation switch
    {
        "Grill" => "مشويات",
        "Charcoal" => "فحم",
        _ => null,
    };
}

/// <summary>Wraps a category with a per-item IsSelected flag the ViewModel keeps in sync with
/// SelectedCategory. WinUI has no MultiBinding, so comparing "is this the selected category" can't
/// be done in XAML from two independent bound values - the ViewModel resolves it once instead and
/// the view just binds this single bool per tile.</summary>
public sealed partial class CategoryTile : ObservableObject
{
    public MenuCategoryDto Category { get; }
    public CategoryTile(MenuCategoryDto category) => Category = category;

    /// <summary>Accessible name of the category chip (was the type name).</summary>
    public override string ToString() => Category.Name;

    [ObservableProperty] private bool _isSelected;
}

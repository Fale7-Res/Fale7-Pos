using CommunityToolkit.Mvvm.ComponentModel;
using Falah.Application.Menu;

namespace Falah.Client.CashierWinUI.Models;

/// <summary>Pre-pairs a product with its currently-applicable offers so the view never has to walk
/// back up a nested items list to find "which product does this offer button belong to" - that
/// pairing is decided once here, in C#, instead of via fragile ancestor lookups in the view.</summary>
public sealed record OfferChoice(MenuProductDto Product, MenuOfferDto Offer);

public sealed record ProductTile(MenuProductDto Product, IReadOnlyList<OfferChoice> Offers);

/// <summary>Wraps a category with a per-item IsSelected flag the ViewModel keeps in sync with
/// SelectedCategory. WinUI has no MultiBinding, so comparing "is this the selected category" can't
/// be done in XAML from two independent bound values - the ViewModel resolves it once instead and
/// the view just binds this single bool per tile.</summary>
public sealed partial class CategoryTile : ObservableObject
{
    public MenuCategoryDto Category { get; }
    public CategoryTile(MenuCategoryDto category) => Category = category;

    [ObservableProperty] private bool _isSelected;
}

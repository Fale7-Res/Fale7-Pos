using Falah.Application.Menu;

namespace Falah.Client.Cashier.Models;

/// <summary>Pre-pairs a product with its currently-applicable offers so the view never has to walk
/// back up a nested ItemsControl to find "which product does this offer button belong to" - that
/// pairing is decided once here, in C#, instead of via fragile XAML ancestor lookups.</summary>
public sealed record OfferChoice(MenuProductDto Product, MenuOfferDto Offer);

public sealed record ProductTile(MenuProductDto Product, IReadOnlyList<OfferChoice> Offers);

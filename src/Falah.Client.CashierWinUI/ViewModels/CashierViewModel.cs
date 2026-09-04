using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Menu;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Falah.Client.CashierWinUI.Models;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// The Cashier POS screen. Every write (open shift, checkout, refund, confirm payment) goes
/// through ApiClient/Falah.Server - this class never computes a price, a total, an order number,
/// or a payment status on its own; it only DISPLAYS what the server decided. Local totals shown
/// before checkout are informational estimates from the last menu snapshot, nothing more.
///
/// Order type is dine-in/takeaway only in this slice - delivery intake (customer lookup, saved
/// addresses, delivery-area pricing) is a deliberately separate, not-yet-built UI (see
/// docs/LEGACY_PARITY_MAP.md); OrderType still round-trips through checkout as a plain string so
/// adding "delivery" later needs no server or DTO change.
/// </summary>
public partial class CashierViewModel : ObservableObject
{
    private readonly AppSession _session;
    private readonly HeaderViewModel _header;

    public CashierViewModel(AppSession session, HeaderViewModel header)
    {
        _session = session;
        _header = header;
        Cart.CollectionChanged += (_, _) => RecomputeCartTotals();
    }

    public string CurrentUserName => _session.CurrentUser?.Name ?? "";
    public bool IsShiftOpen => _session.ShiftId is not null;

    [ObservableProperty] private bool _isLoadingMenu;
    [ObservableProperty] private string? _menuError;
    public ObservableCollection<MenuCategoryDto> Categories { get; } = new();

    /// <summary>Mirrors Categories 1:1 with a per-tile IsSelected flag - see CategoryTile.</summary>
    public ObservableCollection<CategoryTile> CategoryTiles { get; } = new();

    [ObservableProperty] private MenuCategoryDto? _selectedCategory;
    [ObservableProperty] private string _searchQuery = string.Empty;
    public ObservableCollection<ProductTile> VisibleProducts { get; } = new();

    partial void OnSelectedCategoryChanged(MenuCategoryDto? value)
    {
        RefreshVisibleProducts();
        foreach (var tile in CategoryTiles) tile.IsSelected = ReferenceEquals(tile.Category, value);
    }

    partial void OnSearchQueryChanged(string value) => RefreshVisibleProducts();

    private void RefreshVisibleProducts()
    {
        VisibleProducts.Clear();
        IEnumerable<MenuProductDto> source = string.IsNullOrWhiteSpace(SearchQuery)
            ? SelectedCategory?.Products ?? Enumerable.Empty<MenuProductDto>()
            : Categories.SelectMany(c => c.Products).Where(p => p.Name.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase));
        foreach (var p in source)
            VisibleProducts.Add(new ProductTile(p, p.ApplicableOffers.Select(o => new OfferChoice(p, o)).ToList()));
        OnPropertyChanged(nameof(HasNoResults));
    }

    /// <summary>Drives the "no results" empty state - deliberately excludes the loading/error cases
    /// (each has its own state) and the "menu never loaded yet" case (Categories still empty).</summary>
    public bool HasNoResults => !IsLoadingMenu && MenuError is null && Categories.Count > 0 && VisibleProducts.Count == 0;

    [RelayCommand]
    private Task RetryLoadMenuAsync() => LoadMenuAsync();

    public async Task LoadMenuAsync()
    {
        IsLoadingMenu = true;
        MenuError = null;
        OnPropertyChanged(nameof(HasNoResults));
        try
        {
            var result = await _session.Api.GetMenuAsync();
            if (!result.Success || result.Value is null)
            {
                MenuError = result.Error ?? "تعذر تحميل المنيو.";
                return;
            }
            Categories.Clear();
            CategoryTiles.Clear();
            foreach (var c in result.Value.Categories)
            {
                Categories.Add(c);
                CategoryTiles.Add(new CategoryTile(c));
            }
            SelectedCategory = Categories.FirstOrDefault();
        }
        finally
        {
            IsLoadingMenu = false;
            OnPropertyChanged(nameof(HasNoResults));
        }
    }

    // ---------------- Variant picker ----------------

    [ObservableProperty] private MenuProductDto? _variantPickerProduct;
    public bool IsVariantPickerOpen => VariantPickerProduct is not null;
    partial void OnVariantPickerProductChanged(MenuProductDto? value) => OnPropertyChanged(nameof(IsVariantPickerOpen));

    [RelayCommand]
    private void SelectProduct(MenuProductDto product)
    {
        if (product.Variants.Count > 1) { VariantPickerProduct = product; return; }
        AddToCart(product, product.Variants.Count == 1 ? product.Variants[0] : null, null);
    }

    [RelayCommand]
    private void SelectOffer(OfferChoice choice)
    {
        if (choice.Product.Variants.Count > 1)
        {
            VariantPickerProduct = choice.Product;
            PendingOffer = choice.Offer;
            return;
        }
        AddToCart(choice.Product, choice.Product.Variants.Count == 1 ? choice.Product.Variants[0] : null, choice.Offer.Id);
    }

    [ObservableProperty] private MenuOfferDto? _pendingOffer;

    // The variant list still shows each bread/size's own price (the cashier is choosing which one
    // the kitchen makes, nothing else) - but once an offer fixed the final price, showing those
    // per-variant prices unqualified would read as "this is what it costs," which is wrong the
    // moment a variant is tapped. This banner is the only thing that tells the cashier the real
    // price before they commit to a choice.
    public bool HasPendingOffer => PendingOffer is not null;
    public string? PendingOfferBanner => PendingOffer is null ? null
        : $"🎁 {PendingOffer.Name} — السعر الثابت {PendingOffer.FixedPricePiastres / 100m:0.00} ج بغض النظر عن الاختيار";
    partial void OnPendingOfferChanged(MenuOfferDto? value)
    {
        OnPropertyChanged(nameof(HasPendingOffer));
        OnPropertyChanged(nameof(PendingOfferBanner));
    }

    [RelayCommand]
    private void SelectVariant(MenuVariantDto variant)
    {
        if (VariantPickerProduct is null) return;
        AddToCart(VariantPickerProduct, variant, PendingOffer?.Id);
        VariantPickerProduct = null;
        PendingOffer = null;
    }

    [RelayCommand]
    private void CloseVariantPicker() { VariantPickerProduct = null; PendingOffer = null; }

    private void AddToCart(MenuProductDto product, MenuVariantDto? variant, string? offerId)
    {
        var offer = offerId is null ? null : product.ApplicableOffers.FirstOrDefault(o => o.Id == offerId);
        var unitPrice = offer?.FixedPricePiastres ?? variant?.PricePiastres ?? product.BasePricePiastres;
        Cart.Add(new CartLine
        {
            ProductId = product.Id,
            VariantId = variant?.Id,
            ProductName = product.Name,
            VariantName = variant?.Name,
            SendToGrill = product.SendToGrill,
            UnitPricePiastres = unitPrice,
            AppliedOfferId = offer?.Id,
            AppliedOfferName = offer?.Name,
        });
    }

    // ---------------- Cart ----------------

    public ObservableCollection<CartLine> Cart { get; } = new();

    [ObservableProperty] private long _cartSubtotalPiastres;
    public string CartSubtotalDisplay => $"{CartSubtotalPiastres / 100m:0.00} ج";

    private void RecomputeCartTotals()
    {
        CartSubtotalPiastres = Cart.Sum(l => l.LineTotalPiastres);
        OnPropertyChanged(nameof(CartSubtotalDisplay));
        OnPropertyChanged(nameof(GrandTotalDisplay));
        OnPropertyChanged(nameof(GrandTotalPiastres));
        OnPropertyChanged(nameof(CartItemCount));
    }

    public int CartItemCount => Cart.Sum(l => l.Quantity);
    public long GrandTotalPiastres => CartSubtotalPiastres; // delivery fee is 0 in this slice - see OrderType notes
    public string GrandTotalDisplay => $"{GrandTotalPiastres / 100m:0.00} ج";

    [RelayCommand]
    private void IncreaseQuantity(CartLine line) { line.Quantity++; RecomputeCartTotals(); }

    [RelayCommand]
    private void DecreaseQuantity(CartLine line)
    {
        if (line.Quantity > 1) line.Quantity--; else Cart.Remove(line);
        RecomputeCartTotals();
    }

    [RelayCommand]
    private void RemoveLine(CartLine line) { Cart.Remove(line); RecomputeCartTotals(); }

    /// <summary>Quick modifier chips ("بدون بصل" etc.) call this directly from the view's
    /// code-behind rather than a RelayCommand, since each chip needs both the line and its own
    /// fixed note text together.</summary>
    public void AppendNoteText(CartLine line, string note)
    {
        line.Notes = string.IsNullOrWhiteSpace(line.Notes) ? note : $"{line.Notes}، {note}";
    }

    [RelayCommand]
    private void ClearCart()
    {
        Cart.Clear();
        RecomputeCartTotals();
        CurrentCheckoutRequestId = null;
    }

    // ---------------- Order type ----------------

    [ObservableProperty] private string _orderType = "takeaway";
    [ObservableProperty] private string _tableNumber = string.Empty;

    [RelayCommand]
    private void SetOrderType(string orderType) => OrderType = orderType;

    [RelayCommand]
    private void SelectCategory(CategoryTile tile) => SelectedCategory = tile.Category;
}

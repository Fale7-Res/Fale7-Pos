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
    private Falah.Domain.Financial.TaxRule? _taxRule;

    public CashierViewModel(AppSession session, HeaderViewModel header)
    {
        _session = session;
        _header = header;
        Cart.CollectionChanged += (_, _) => { RecomputeCartTotals(); NotifyCheckoutAvailability(); };
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

    // Both feed CanOpenCheckout, so the footer has to hear about them - a menu that failed to load
    // must not leave a green checkout button sitting over an empty catalogue.
    partial void OnIsLoadingMenuChanged(bool value) => NotifyCheckoutAvailability();
    partial void OnMenuErrorChanged(string? value) => NotifyCheckoutAvailability();

    /// <summary>The synthetic "best sellers" category, or null when the restaurant has no sales
    /// history to rank by. Null means the chip is simply absent - an empty shortcut would be a
    /// button that does nothing.</summary>
    private MenuCategoryDto? _bestSellers;
    private HashSet<string> _bestSellerIds = new(StringComparer.Ordinal);

    private MenuCategoryDto? BuildBestSellers(IReadOnlyList<string> productIds)
    {
        if (productIds.Count == 0) return null;
        var byId = Categories.SelectMany(c => c.Products).GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
        // Order is the server's ranking, not the menu's - that ranking is the whole point.
        var products = productIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        return products.Count == 0 ? null : new MenuCategoryDto(BestSellersCategoryId, "الأكثر مبيعًا", "🔥", products);
    }

    public const string BestSellersCategoryId = "__best_sellers";

    private void RefreshVisibleProducts()
    {
        VisibleProducts.Clear();
        IEnumerable<MenuProductDto> source = string.IsNullOrWhiteSpace(SearchQuery)
            ? SelectedCategory?.Products ?? Enumerable.Empty<MenuProductDto>()
            : Categories.SelectMany(c => c.Products).Where(p => p.Name.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase));
        // A search spans categories, so the icon a product falls back to is looked up from the
        // category it actually belongs to, not from whichever one happens to be selected.
        var iconByProduct = Categories
            .SelectMany(c => c.Products.Select(p => (p.Id, c.Icon)))
            .GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First().Icon);
        foreach (var p in source)
            VisibleProducts.Add(new ProductTile(
                p,
                p.ApplicableOffers.Select(o => new OfferChoice(p, o)).ToList(),
                _session.Api.ProductImageUri(p.Id, p.ImageVersion),
                iconByProduct.TryGetValue(p.Id, out var icon) ? icon : null,
                _bestSellerIds.Contains(p.Id)));
        OnPropertyChanged(nameof(HasNoResults));
        OnPropertyChanged(nameof(CatalogueTitle));
    }

    /// <summary>Drives the "no results" empty state - deliberately excludes the loading/error cases
    /// (each has its own state) and the "menu never loaded yet" case (Categories still empty).</summary>
    public bool HasNoResults => !IsLoadingMenu && MenuError is null && VisibleProducts.Count == 0;

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
            var taxes = await _session.Api.GetTaxRulesAsync();
            if (!taxes.Success || taxes.Value is null) { MenuError = taxes.Error; return; }
            var tax = taxes.Value.FirstOrDefault(t => t.Enabled);
            _taxRule = tax is null ? null : new(tax.Id, tax.Name, tax.RateBasisPoints,
                tax.Mode == "Inclusive" ? Falah.Domain.Financial.TaxMode.Inclusive : Falah.Domain.Financial.TaxMode.Exclusive, tax.Enabled);
            NotifyTotals();
            Categories.Clear();
            CategoryTiles.Clear();
            foreach (var c in result.Value.Categories)
                Categories.Add(c);

            // The best-sellers shortcut is a view over products that already belong to real
            // categories, so it is deliberately NOT added to Categories - putting it there would
            // make every one of its products appear twice in a search, and would give it a share of
            // its own in anything that walks the category list.
            // Null only if a server older than this field answered; treated as "no ranking yet",
            // which is a state the shortcut already knows how to be absent for.
            var bestSellerIds = result.Value.BestSellerProductIds ?? Array.Empty<string>();
            _bestSellers = BuildBestSellers(bestSellerIds);
            _bestSellerIds = bestSellerIds.ToHashSet(StringComparer.Ordinal);
            if (_bestSellers is not null) CategoryTiles.Add(new CategoryTile(_bestSellers));
            foreach (var c in Categories) CategoryTiles.Add(new CategoryTile(c));

            SelectedCategory = _bestSellers ?? Categories.FirstOrDefault();
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
        if (!CanEditOperation) return;
        if (product.Variants.Count > 1) { VariantPickerProduct = product; return; }
        AddToCart(product, product.Variants.Count == 1 ? product.Variants[0] : null, null);
    }

    [RelayCommand]
    private void SelectOffer(OfferChoice choice)
    {
        if (!CanEditOperation) return;
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
        if (!CanEditOperation) return;
        if (VariantPickerProduct is null) return;
        AddToCart(VariantPickerProduct, variant, PendingOffer?.Id);
        VariantPickerProduct = null;
        PendingOffer = null;
    }

    [RelayCommand]
    private void CloseVariantPicker() { VariantPickerProduct = null; PendingOffer = null; }

    private void AddToCart(MenuProductDto product, MenuVariantDto? variant, string? offerId)
    {
        if (!CanEditOperation) return;
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
        NotifyTotals();
    }

    public int CartItemCount => Cart.Sum(l => l.Quantity);

    /// <summary>Informational estimate only, same as every other total in this class - the server is
    /// the sole authority on what a checkout actually costs (see CheckoutOrderHandler). Delivery fee
    /// is whatever the selected DeliveryService's own FeePiastres is, not computed here.</summary>
    public long GrandTotalPiastres
    {
        get
        {
            var deliveryFee = OrderType == "delivery" ? SelectedDeliveryService?.FeePiastres ?? 0 : 0;
            var taxable = Falah.Domain.Common.Money.FromPiastres(Math.Max(0, CartSubtotalPiastres - DiscountAmountPiastres));
            var total = Falah.Domain.Financial.TaxEngine.Calculate(taxable, _taxRule).Final.Piastres + deliveryFee - CompAmountPiastres;
            return total > 0 ? total : 0;
        }
    }

    public string GrandTotalDisplay => $"{GrandTotalPiastres / 100m:0.00} ج";

    [RelayCommand]
    private void IncreaseQuantity(CartLine line) { if (!CanEditOperation) return; line.Quantity++; RecomputeCartTotals(); }

    [RelayCommand]
    private void DecreaseQuantity(CartLine line)
    {
        if (!CanEditOperation) return;
        if (line.Quantity > 1) line.Quantity--; else Cart.Remove(line);
        RecomputeCartTotals();
    }

    [RelayCommand]
    private void RemoveLine(CartLine line) { if (!CanEditOperation) return; Cart.Remove(line); RecomputeCartTotals(); }

    /// <summary>Quick modifier chips ("بدون بصل" etc.) call this directly from the view's
    /// code-behind rather than a RelayCommand, since each chip needs both the line and its own
    /// fixed note text together.</summary>
    public void AppendNoteText(CartLine line, string note)
    {
        if (!CanEditOperation) return;
        line.Notes = string.IsNullOrWhiteSpace(line.Notes) ? note : $"{line.Notes}، {note}";
    }

    [RelayCommand]
    private void ClearCart()
    {
        if (!CanEditOperation) return;
        Cart.Clear();
        IsDiscountEnabled = false; IsCompEnabled = false; IsSplitPaymentEnabled = false;
        ResetDelivery();
        RecomputeCartTotals();
        CurrentCheckoutRequestId = null;
    }

    // ---------------- Order type ----------------

    [ObservableProperty] private string _orderType = "takeaway";

    [RelayCommand]
    private void SetOrderType(string orderType) { if (CanEditOperation) OrderType = orderType; }

    [RelayCommand]
    private void SelectCategory(CategoryTile tile) => SelectedCategory = tile.Category;
}

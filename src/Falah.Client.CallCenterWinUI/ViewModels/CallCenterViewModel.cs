using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Menu;
using Falah.Client.CallCenterWinUI.Models;
using Falah.Client.CallCenterWinUI.Services;

namespace Falah.Client.CallCenterWinUI.ViewModels;

/// <summary>
/// The Call Center order-intake screen. Every write (customer upsert, checkout, print job) goes
/// through ApiClient/Falah.Server - this class never computes a price, a total, an order number, or
/// a delivery fee on its own; it only displays what the server/menu snapshot said. Scope is
/// deliberately phone/delivery intake only (see docs/IMPLEMENTATION_MATRIX.md row 9 and
/// docs/UI_TAKEOVER_AUDIT.md's Call Center row): this station has no drawer or shift of its own, and
/// every order it creates is OrderType.Delivery against whichever single shift is currently open
/// system-wide.
/// </summary>
public partial class CallCenterViewModel : ObservableObject
{
    private readonly AppSession _session;
    private readonly HeaderViewModel _header;
    private Falah.Domain.Financial.TaxRule? _taxRule;

    public CallCenterViewModel(AppSession session, HeaderViewModel header)
    {
        _session = session;
        _header = header;
        OwnerUserId = session.CurrentUser?.Id ?? "";
        Cart.CollectionChanged += (_, _) => RecomputeCartTotals();
        // An operation that was still in flight when this station last stopped comes back here, so
        // the desk offers the same retry it would have offered had the process survived.
        RecoverPendingOperations();
    }

    public string CurrentUserName => _session.CurrentUser?.Name ?? "";

    /// <summary>Runs once right after login: discovers the currently open shift (this station has
    /// none of its own) and loads the menu. Both are independent of each other, so a menu-load
    /// failure doesn't hide a "no shift open" state or vice versa.</summary>
    public async Task InitializeAsync()
    {
        if (IsInitializing) return;
        IsInitializing = true;
        try
        {
            await RefreshShiftAsync();

            // Restaurant identity comes from real Settings, not a hardcoded client string - the same
            // source the Cashier station reads, so both stations always show one identity.
            var settings = await _session.Api.GetSettingsAsync();
            if (settings.Success && settings.Value is not null) _header.SetIdentity(settings.Value.RestaurantName);

            await LoadMenuAsync();
            await LoadDeliveryServicesAsync();
        }
        finally { IsInitializing = false; }
    }

    [ObservableProperty] private bool _isShiftOpen;
    [ObservableProperty] private string? _shiftError;

    [RelayCommand]
    private async Task RefreshShiftAsync()
    {
        var found = await _session.RefreshCurrentShiftAsync();
        IsShiftOpen = found;
        ShiftError = found ? null : "لا توجد وردية مفتوحة حاليًا - لا يمكن تسجيل طلبات حتى يفتح الكاشير ورديته.";
        _header.Refresh();
    }

    // ---------------- Menu browsing ----------------

    [ObservableProperty] private bool _isLoadingMenu;
    [ObservableProperty] private string? _menuError;
    public ObservableCollection<MenuCategoryDto> Categories { get; } = new();
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
                iconByProduct.TryGetValue(p.Id, out var icon) ? icon : null));
        OnPropertyChanged(nameof(HasNoResults));
    }

    public bool HasNoResults => !IsLoadingMenu && MenuError is null && VisibleProducts.Count == 0;

    [RelayCommand]
    private Task RetryLoadMenuAsync() => LoadMenuAsync();

    private async Task LoadMenuAsync()
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
            if (!taxes.Success || taxes.Value is null)
            {
                MenuError = taxes.Error ?? "تعذر تحميل الضريبة.";
                return;
            }
            var tax = taxes.Value.FirstOrDefault(t => t.Enabled);
            _taxRule = tax is null ? null : new(tax.Id, tax.Name, tax.RateBasisPoints,
                tax.Mode == "Inclusive" ? Falah.Domain.Financial.TaxMode.Inclusive : Falah.Domain.Financial.TaxMode.Exclusive, tax.Enabled);
            RecomputeCartTotals();
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

    [RelayCommand]
    private void SelectCategory(CategoryTile tile) => SelectedCategory = tile.Category;

    // ---------------- Variant picker ----------------

    [ObservableProperty] private MenuProductDto? _variantPickerProduct;
    public bool IsVariantPickerOpen => VariantPickerProduct is not null;
    partial void OnVariantPickerProductChanged(MenuProductDto? value) => OnPropertyChanged(nameof(IsVariantPickerOpen));

    [RelayCommand]
    private void SelectProduct(MenuProductDto product)
    {
        if (!CanEditOperation || !product.IsAvailable) return;
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
        if (!CanEditOperation || !product.IsAvailable) return;
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

    /// <summary>Informational estimate only - the server is the sole authority on what a checkout
    /// actually costs. Delivery fee is whatever the selected DeliveryService's own FeePiastres is,
    /// never computed here.</summary>
    public long GrandTotalPiastres
    {
        get
        {
            var deliveryFee = SelectedDeliveryService?.FeePiastres ?? 0;
            var taxed = Falah.Domain.Financial.TaxEngine.Calculate(Falah.Domain.Common.Money.FromPiastres(CartSubtotalPiastres), _taxRule);
            var total = taxed.Final.Piastres + deliveryFee;
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

    public void AppendNoteText(CartLine line, string note)
    {
        if (!CanEditOperation) return;
        line.Notes = string.IsNullOrWhiteSpace(line.Notes) ? note : $"{line.Notes}، {note}";
    }

    [RelayCommand]
    private void ClearOrder()
    {
        if (!CanEditOperation) return;
        Cart.Clear();
        RecomputeCartTotals();
        ResetCustomerAndDelivery();
        CurrentCheckoutRequestId = null;
        CheckoutError = null;
        LastCheckoutResult = null;
    }
}

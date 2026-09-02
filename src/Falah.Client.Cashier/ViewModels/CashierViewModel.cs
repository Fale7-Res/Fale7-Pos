using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Menu;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Falah.Client.Cashier.Models;
using Falah.Client.Cashier.Services;

namespace Falah.Client.Cashier.ViewModels;

/// <summary>
/// The Cashier POS screen. Every write (open shift, checkout, refund, confirm payment) goes
/// through ApiClient/Falah.Server - this class never computes a price, a total, an order number,
/// or a payment status on its own; it only DISPLAYS what the server decided. Local totals shown
/// before checkout are informational estimates from the last menu snapshot, nothing more.
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

    [ObservableProperty] private MenuCategoryDto? _selectedCategory;
    [ObservableProperty] private string _searchQuery = string.Empty;
    public ObservableCollection<ProductTile> VisibleProducts { get; } = new();

    partial void OnSelectedCategoryChanged(MenuCategoryDto? value) => RefreshVisibleProducts();
    partial void OnSearchQueryChanged(string value) => RefreshVisibleProducts();

    private void RefreshVisibleProducts()
    {
        VisibleProducts.Clear();
        IEnumerable<MenuProductDto> source = string.IsNullOrWhiteSpace(SearchQuery)
            ? SelectedCategory?.Products ?? Enumerable.Empty<MenuProductDto>()
            : Categories.SelectMany(c => c.Products).Where(p => p.Name.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase));
        foreach (var p in source)
            VisibleProducts.Add(new ProductTile(p, p.ApplicableOffers.Select(o => new OfferChoice(p, o)).ToList()));
    }

    public async Task LoadMenuAsync()
    {
        IsLoadingMenu = true;
        MenuError = null;
        try
        {
            var result = await _session.Api.GetMenuAsync();
            if (!result.Success || result.Value is null)
            {
                MenuError = result.Error ?? "تعذر تحميل المنيو.";
                return;
            }
            Categories.Clear();
            foreach (var c in result.Value.Categories) Categories.Add(c);
            SelectedCategory = Categories.FirstOrDefault();
        }
        finally
        {
            IsLoadingMenu = false;
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
        if (choice.Product.Variants.Count > 1) { VariantPickerProduct = choice.Product; PendingOfferId = choice.Offer.Id; return; }
        AddToCart(choice.Product, choice.Product.Variants.Count == 1 ? choice.Product.Variants[0] : null, choice.Offer.Id);
    }

    private string? PendingOfferId;

    [RelayCommand]
    private void SelectVariant(MenuVariantDto variant)
    {
        if (VariantPickerProduct is null) return;
        AddToCart(VariantPickerProduct, variant, PendingOfferId);
        VariantPickerProduct = null;
        PendingOfferId = null;
    }

    [RelayCommand]
    private void CloseVariantPicker() { VariantPickerProduct = null; PendingOfferId = null; }

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
    }

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

    [RelayCommand]
    private void AppendNote((CartLine? Line, string? Note) args)
    {
        if (args.Line is null || args.Note is null) return;
        args.Line.Notes = string.IsNullOrWhiteSpace(args.Line.Notes) ? args.Note : $"{args.Line.Notes}، {args.Note}";
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
    private void SelectCategory(MenuCategoryDto category) => SelectedCategory = category;
}

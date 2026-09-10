using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Menu;
using Falah.Application.Pricing;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// The live menu as the POS sees it, with the tax rule and price rules that shape what customers
/// actually pay, plus the one menu write a manager performs daily: taking an item off the menu when
/// the kitchen runs out.
///
/// Prices are shown, never edited here. A price change is a pricing decision with a different
/// authorization path (`menu_manage` for the list price, `offers_manage` for a price rule) and it
/// rewrites what every future order is charged - it does not belong behind the same casual tap as
/// "we ran out of chicken". Availability is the genuinely operational, reversible, per-shift action,
/// so that is what this screen offers.
/// </summary>
public partial class MenuViewModel : ObservableObject
{
    private readonly AppSession _session;

    public MenuViewModel(AppSession session)
    {
        _session = session;
        Editor = new MenuEditorViewModel(session);
        // Authoring rewrites what this screen shows, so the live view reloads from the server once
        // the editor closes rather than trusting a locally patched copy of what was just saved.
        Editor.Closed += () => _ = LoadAsync();
    }

    /// <summary>Price/tax/offer authoring lives behind this explicit editor, not behind the casual
    /// availability tap on the list - see the class remarks for why the two are deliberately
    /// separated. The editor enforces its own per-kind permission.</summary>
    public MenuEditorViewModel Editor { get; }

    public bool CanOpenEditor => Editor.CanOpen;

    [RelayCommand]
    private Task OpenEditorAsync() => Editor.OpenAsync();

    public sealed partial class MenuProductRow : ObservableObject
    {
        public required MenuProductDto Product { get; init; }
        public required string CategoryName { get; init; }
        [ObservableProperty] private bool _isAvailable;

        public string PriceDisplay => (Product.Variants.Count > 1 ? "من " : "") +
            CashInput.Format(Product.Variants.Count > 0 ? Product.Variants.Min(v => v.PricePiastres) : Product.BasePricePiastres) + " ج";

        public string StationLabel => Product.PreparationStation switch
        {
            "Grill" => "مشويات",
            "Charcoal" => "فحم",
            _ => "—",
        };
    }

    public ObservableCollection<MenuProductRow> Products { get; } = new();
    public ObservableCollection<TaxRuleDto> TaxRules { get; } = new();
    public ObservableCollection<PriceRuleDto> PriceRules { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _actionNote;
    [ObservableProperty] private string? _actionError;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _searchQuery = "";

    private readonly List<MenuProductRow> _all = new();

    public bool HasNoProducts => !IsLoading && LoadError is null && Products.Count == 0;
    public bool CanManageMenu => _session.HasPermission("menu_manage");

    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Products.Clear();
        IEnumerable<MenuProductRow> source = _all;
        if (!string.IsNullOrWhiteSpace(SearchQuery))
            source = source.Where(r => r.Product.Name.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase)
                                    || r.CategoryName.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase));
        foreach (var r in source) Products.Add(r);
        OnPropertyChanged(nameof(HasNoProducts));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        try
        {
            var menu = await _session.Api.GetMenuAsync();
            if (!menu.Success || menu.Value is null)
            {
                LoadError = menu.Error ?? "تعذر تحميل القائمة.";
                return;
            }
            _all.Clear();
            foreach (var c in menu.Value.Categories)
                foreach (var p in c.Products)
                    _all.Add(new MenuProductRow { Product = p, CategoryName = c.Name, IsAvailable = p.IsAvailable });
            ApplyFilter();

            var tax = await _session.Api.GetTaxRulesAsync();
            if (!tax.Success || tax.Value is null) { LoadError = tax.Error ?? "تعذر تحميل الضريبة."; return; }
            TaxRules.Clear();
            if (tax.Success && tax.Value is not null)
                foreach (var t in tax.Value) TaxRules.Add(t);

            var rules = await _session.Api.GetPriceRulesAsync();
            if (!rules.Success || rules.Value is null) { LoadError = rules.Error ?? "تعذر تحميل العروض."; return; }
            PriceRules.Clear();
            if (rules.Success && rules.Value is not null)
                foreach (var r in rules.Value) PriceRules.Add(r);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoProducts));
        }
    }

    [RelayCommand]
    private async Task ToggleAvailabilityAsync(MenuProductRow row)
    {
        if (IsBusy || !CanManageMenu) return;
        IsBusy = true;
        ActionError = null;
        ActionNote = null;
        try
        {
            var target = !row.IsAvailable;
            var result = await _session.Api.SetProductAvailabilityAsync(
                new SetProductAvailabilityRequest(row.Product.Id, target, _session.CurrentUser!.Id));
            if (!result.Success)
            {
                ActionError = result.Error ?? "تعذر تغيير حالة توفر الصنف.";
                return;
            }
            row.IsAvailable = target;
            ActionNote = target ? $"تم إتاحة {row.Product.Name}." : $"تم إيقاف {row.Product.Name} مؤقتًا.";
        }
        finally { IsBusy = false; }
    }
}

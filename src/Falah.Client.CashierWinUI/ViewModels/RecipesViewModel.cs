using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Inventory;
using Falah.Application.Menu;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Recipes with their real ingredient cost and food-cost percentage.
///
/// The load-bearing rule this screen must not soften is "unknown cost is not zero": a recipe whose
/// ingredients have no purchase history has an INCOMPLETE cost, and the server says so via
/// Missing/CompletenessPercent. Rendering that as a confident "0.00 ج" or a flattering food-cost
/// percentage would be worse than showing nothing, so incomplete recipes are labelled as incomplete
/// and their percentage is withheld rather than estimated.
/// </summary>
public partial class RecipesViewModel : ObservableObject
{
    private readonly AppSession _session;

    public RecipesViewModel(AppSession session) => _session = session;

    /// <summary>A recipe paired with the server's costing verdict for it.</summary>
    public sealed partial class RecipeRow : ObservableObject
    {
        public required RecipeDto Recipe { get; init; }
        [ObservableProperty] private RecipeCostDto? _cost;
        [ObservableProperty] private MenuItemProfitabilityDto? _profitability;
        [ObservableProperty] private long _sellingPricePiastres;

        public string LineCountLabel => $"{Recipe.Lines.Count} مكوّن";

        public bool IsCostComplete => Cost is { CompletenessPercent: 100 };
        public bool IsCostIncomplete => Cost is not null && Cost.CompletenessPercent < 100;

        /// <summary>Nothing priced at all. Distinct from "partly priced" because the number the
        /// server returns in that case - zero - is not a cheap recipe, it is no answer.</summary>
        public bool IsCostUnknown => Cost is null || Cost.CompletenessPercent == 0;

        /// <summary>A recipe whose ingredients are only partly priced costs AT LEAST what is known
        /// so far. Printing that figure bare would read as the whole cost; printing nothing would
        /// throw away a real lower bound. So it is shown, and shown as a floor.</summary>
        public string CostDisplay =>
            IsCostUnknown ? "—"
            : IsCostComplete ? CashInput.Format(Cost!.FoodCostPiastres) + " ج"
            : "من " + CashInput.Format(Cost!.FoodCostPiastres) + " ج";

        /// <summary>Withheld, not estimated, while any ingredient cost is unknown.</summary>
        public string FoodCostPercentDisplay =>
            !IsCostComplete || Profitability?.FoodCostPercent is null ? "—"
            : $"{Profitability.FoodCostPercent.Value:0.#}%";

        public string IncompleteLabel =>
            Cost is null ? ""
            : Cost.CompletenessPercent == 0 ? "التكلفة غير معروفة - لا يوجد سعر لأي مكوّن"
            : $"تكلفة غير مكتملة ({Cost.CompletenessPercent}%)";

        public void Refreshed()
        {
            OnPropertyChanged(nameof(IsCostComplete));
            OnPropertyChanged(nameof(IsCostIncomplete));
            OnPropertyChanged(nameof(IsCostUnknown));
            OnPropertyChanged(nameof(CostDisplay));
            OnPropertyChanged(nameof(FoodCostPercentDisplay));
            OnPropertyChanged(nameof(IncompleteLabel));
        }
    }

    public ObservableCollection<RecipeRow> Recipes { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;

    public bool HasNoRecipes => !IsLoading && LoadError is null && Recipes.Count == 0;
    public bool CanViewCost => _session.HasPermission("reports_view");

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        try
        {
            var actor = _session.CurrentUser!.Id;
            var recipes = await _session.Api.GetRecipesAsync();
            if (!recipes.Success || recipes.Value is null)
            {
                LoadError = recipes.Error ?? "تعذر تحميل الوصفات.";
                return;
            }

            // Selling price comes from the live menu, so food-cost % is measured against what the
            // item is actually sold for rather than a number typed into this screen.
            var menu = await _session.Api.GetMenuAsync();
            if (!menu.Success || menu.Value is null) { LoadError = menu.Error ?? "تعذر تحميل أسعار القائمة."; return; }
            var priceByProduct = new Dictionary<string, long>();
            if (menu.Success && menu.Value is not null)
                foreach (var c in menu.Value.Categories)
                    foreach (var p in c.Products)
                        priceByProduct[p.Id] = p.Variants.Count > 0 ? p.Variants.Min(v => v.PricePiastres) : p.BasePricePiastres;

            Recipes.Clear();
            foreach (var r in recipes.Value)
            {
                var row = new RecipeRow
                {
                    Recipe = r,
                    SellingPricePiastres = priceByProduct.TryGetValue(r.ProductId, out var price) ? price : 0,
                };
                Recipes.Add(row);
            }

            if (!CanViewCost) return;

            foreach (var row in Recipes)
            {
                var cost = await _session.Api.GetRecipeCostAsync(row.Recipe.Id, actor);
                if (!cost.Success || cost.Value is null) { LoadError = cost.Error ?? "تعذر تحميل تكلفة الوصفة."; return; }
                if (cost.Success) row.Cost = cost.Value;

                if (row.SellingPricePiastres > 0)
                {
                    var profit = await _session.Api.GetRecipeProfitabilityAsync(row.Recipe.Id, row.SellingPricePiastres, actor);
                    if (!profit.Success || profit.Value is null) { LoadError = profit.Error ?? "تعذر تحميل نسبة تكلفة الوصفة."; return; }
                    if (profit.Success) row.Profitability = profit.Value;
                }
                row.Refreshed();
            }
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoRecipes));
        }
    }
}

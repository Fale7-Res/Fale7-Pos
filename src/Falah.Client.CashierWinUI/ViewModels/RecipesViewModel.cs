using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Inventory;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// What every dish is made of, what it costs, and how much of that answer can be trusted.
///
/// The screen used to be a four-column list: name, ingredient count, cost, percentage. It could not
/// answer the question an owner actually opens it with - WHY is this cost incomplete, and what do I
/// do about it - because it never showed a single ingredient. Nor could anyone author a recipe from
/// it; the only way ingredients ever reached the database was a script.
///
/// The rule it must not soften is "unknown cost is not zero". A partly-priced recipe shows what is
/// known as a FLOOR ("من ..."), never as the cost, and its margin is withheld rather than computed
/// against a cost that is missing pieces. Every line that has no cost says which of the three
/// reasons it is - no invoice, a circular batch, or a piece-count line with no conversion - because
/// those need three different actions.
/// </summary>
public partial class RecipesViewModel : ObservableObject
{
    private readonly AppSession _session;

    public RecipesViewModel(AppSession session) => _session = session;

    // ---------------------------------------------------------------- list

    /// <summary>One recipe as the list shows it. Everything here comes from a single cost-summary
    /// call for the whole menu - the previous screen asked the server twice per recipe, which on
    /// this restaurant's menu was two hundred requests to draw one list.</summary>
    public sealed partial class RecipeRow : ObservableObject
    {
        public required RecipeCostSummaryDto Cost { get; init; }

        public string Name => Cost.ProductName;
        public string RecipeName => Cost.Name;
        public string CategoryName => Cost.CategoryName ?? "";
        public string LineCountLabel => Cost.LineCount == 0 ? "بدون مكوّنات" : $"{Cost.LineCount} مكوّن";

        public bool IsComplete => Cost.LineCount > 0 && Cost.KnownLineCount == Cost.LineCount;
        public bool IsPartlyKnown => Cost.KnownLineCount > 0 && Cost.KnownLineCount < Cost.LineCount;
        public bool IsUnknown => Cost.LineCount == 0 || Cost.KnownLineCount == 0;

        /// <summary>A partly-priced recipe costs AT LEAST what is known. Printing that bare would
        /// read as the whole cost; printing nothing would throw away a real floor. So it is shown,
        /// and shown as a floor.</summary>
        public string CostDisplay =>
            IsUnknown ? "—"
            : IsComplete ? CashInput.Format(Cost.KnownCostPiastres) + " ج"
            : "من " + CashInput.Format(Cost.KnownCostPiastres) + " ج";

        public string PriceDisplay => Cost.SellingPricePiastres is { } p and > 0 ? CashInput.Format(p) + " ج" : "—";

        public string FoodCostPercentDisplay =>
            Cost.FoodCostPercent is { } percent ? $"{percent:0.#}%" : "—";

        public string ContributionDisplay =>
            Cost.ContributionPiastres is { } c ? CashInput.Format(c) + " ج" : "—";

        public string StatusLabel =>
            Cost.LineCount == 0 ? "وصفة بدون مكوّنات"
            : IsComplete ? "تكلفة مكتملة"
            : Cost.KnownLineCount == 0 ? "لا توجد تكلفة لأي مكوّن"
            : $"تكلفة غير مكتملة ({Cost.CompletenessPercent}%)";

        /// <summary>The first two names are enough to act on; the rest is a count. A list row is
        /// read at a glance, and eleven ingredient names in it is not a glance.</summary>
        public string MissingSummary =>
            Cost.Missing.Count == 0 ? ""
            : Cost.Missing.Count <= 2 ? string.Join("، ", Cost.Missing)
            : $"{string.Join("، ", Cost.Missing.Take(2))} و{Cost.Missing.Count - 2} غيرهم";
    }

    private readonly List<RecipeRow> _allRecipes = new();
    public ObservableCollection<RecipeRow> Recipes { get; } = new();

    [ObservableProperty] private RecipeCostCoverageDto? _coverage;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string _searchText = "";

    /// <summary>0 = every recipe, 1 = only the ones whose cost cannot be trusted, 2 = only the ones
    /// with no cost at all. The owner's job on this screen is usually the second or third of those.</summary>
    [ObservableProperty] private int _filterIndex;

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnFilterIndexChanged(int value) => ApplyFilter();

    public bool HasNoRecipes => !IsLoading && LoadError is null && _allRecipes.Count == 0;
    public bool HasNoMatches => !IsLoading && LoadError is null && _allRecipes.Count > 0 && Recipes.Count == 0;
    public bool CanViewCost => _session.HasPermission("reports_view");
    public bool CanEditRecipes => _session.HasPermission("menu_manage");

    public string CoverageHeadline =>
        Coverage is not { } c ? ""
        : c.RecipeCount == 0 ? "لا توجد وصفات بعد"
        : $"{c.FullyCostedCount} من {c.RecipeCount} وصفة تكلفتها مكتملة";

    public string CoverageDetail =>
        Coverage is not { } c ? ""
        : string.Join(" · ", new[]
        {
            c.PartlyCostedCount > 0 ? $"{c.PartlyCostedCount} غير مكتملة" : null,
            c.UncostedCount > 0 ? $"{c.UncostedCount} بلا تكلفة" : null,
            c.ProductsWithoutRecipeCount > 0 ? $"{c.ProductsWithoutRecipeCount} صنف بلا وصفة" : null,
            c.IngredientsWithoutCostCount > 0 ? $"{c.IngredientsWithoutCostCount} خامة بلا سعر" : null,
        }.Where(x => x is not null));

    public bool HasCoverageWarning =>
        Coverage is { } c && (c.PartlyCostedCount > 0 || c.UncostedCount > 0 || c.ProductsWithoutRecipeCount > 0 || c.IngredientsWithoutCostCount > 0);

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        try
        {
            if (!CanViewCost)
            {
                LoadError = "لا تملك صلاحية عرض التكاليف.";
                return;
            }

            var actor = _session.CurrentUser!.Id;
            var summaries = await _session.Api.GetRecipeCostSummariesAsync(actor);
            if (!summaries.Success || summaries.Value is null)
            {
                LoadError = summaries.Error ?? "تعذر تحميل الوصفات وتكلفتها.";
                return;
            }

            // Secondary, but not silent: a missing coverage line would just be absent from the top
            // of the screen, and the owner would have no way to tell that from "nothing is wrong".
            var coverage = await _session.Api.GetRecipeCostCoverageAsync(actor);
            if (!coverage.Success || coverage.Value is null)
                LoadError = coverage.Error ?? "تعذر تحميل ملخّص تغطية التكاليف.";
            Coverage = coverage.Value;

            _allRecipes.Clear();
            foreach (var summary in summaries.Value.OrderBy(s => s.CategoryName).ThenBy(s => s.ProductName))
                _allRecipes.Add(new RecipeRow { Cost = summary });

            ApplyFilter();
            await LoadIngredientsAsync();
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoRecipes));
            OnPropertyChanged(nameof(HasNoMatches));
            OnPropertyChanged(nameof(CoverageHeadline));
            OnPropertyChanged(nameof(CoverageDetail));
            OnPropertyChanged(nameof(HasCoverageWarning));
        }
    }

    private void ApplyFilter()
    {
        var term = SearchText.Trim();
        var matches = _allRecipes.Where(r =>
            (term.Length == 0
                || r.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || r.RecipeName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || r.CategoryName.Contains(term, StringComparison.OrdinalIgnoreCase))
            && FilterIndex switch
            {
                1 => !r.IsComplete,
                2 => r.IsUnknown,
                _ => true,
            });

        Recipes.Clear();
        foreach (var row in matches) Recipes.Add(row);
        OnPropertyChanged(nameof(HasNoMatches));
    }

    // ---------------------------------------------------------------- detail

    /// <summary>One ingredient line, priced, with the reason it has no price when it has none.</summary>
    public sealed class CostLineRow
    {
        public required RecipeCostLineDto Line { get; init; }

        public string ItemName => Line.ItemName;

        public string QuantityDisplay => Line.Kind == "Yield"
            ? $"{MilliUnits.Format(Line.QuantityMilliUnits)} قطعة"
            : $"{MilliUnits.Format(Line.QuantityMilliUnits)} {Line.Unit}";

        /// <summary>What a piece-count line actually draws from stock. Shown only for those, since
        /// for every other line it repeats the quantity above.</summary>
        public string BaseQuantityDisplay => Line.Kind != "Yield" ? ""
            : Line.BaseQuantityMilliUnits is { } q ? $"= {MilliUnits.Format(q)} {Line.Unit}"
            : "معامل التحويل ناقص";

        public string KindLabel => Line.Kind switch
        {
            "Packaging" => "تغليف",
            "Yield" => "بالقطعة",
            _ => "مباشر",
        };

        public bool IsPackaging => Line.Kind == "Packaging";

        public string UnitCostDisplay => Line.UnitCostPiastres is { } c ? $"{CashInput.Format(c)} ج / {Line.Unit}" : "—";

        public string ExtendedCostDisplay => Line.ExtendedCostPiastres is { } c ? CashInput.Format(c) + " ج" : "—";

        /// <summary>Where the number came from matters as much as the number. A price nobody has
        /// paid yet must never look like one somebody has.</summary>
        public string SourceLabel => Line.CostSource switch
        {
            "Purchase" => "فاتورة شراء",
            "Estimated" => "تقديري",
            "Prep" => "خلطة تجهيز",
            "Circular" => "خلطة دائرية",
            _ => "غير معروف",
        };

        public bool IsCostFactual => Line.CostSource is "Purchase" or "Prep";
        public bool IsCostEstimated => Line.CostSource == "Estimated";
        public bool IsCostMissing => Line.UnitCostPiastres is null || Line.BaseQuantityMilliUnits is null;

        public string CostAsOfDisplay => Line.CostAsOfUtc is { } at
            ? $"آخر شراء {at.ToLocalTime():yyyy/MM/dd}"
            : "";

        public string MissingReason => Line.MissingReason ?? "";
        public bool HasMissingReason => !string.IsNullOrEmpty(Line.MissingReason);
    }

    [ObservableProperty] private RecipeRow? _selectedRecipe;
    [ObservableProperty] private RecipeCostDetailDto? _detail;
    [ObservableProperty] private bool _isDetailLoading;
    [ObservableProperty] private string? _detailError;

    public ObservableCollection<CostLineRow> DetailLines { get; } = new();

    public bool HasSelection => SelectedRecipe is not null;
    public bool HasDetail => Detail is not null && !IsDetailLoading;

    /// <summary>Set while the list is being rebuilt under a selection that has not actually
    /// changed - re-selecting the same recipe after a save must not throw away the detail that was
    /// just reloaded, nor cancel an edit the user is still in.</summary>
    private bool _reselecting;

    partial void OnSelectedRecipeChanged(RecipeRow? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        if (_reselecting) return;
        CancelEdit();
        _ = LoadDetailAsync();
    }

    partial void OnDetailChanged(RecipeCostDetailDto? value)
    {
        DetailLines.Clear();
        foreach (var line in value?.Lines ?? Array.Empty<RecipeCostLineDto>())
            DetailLines.Add(new CostLineRow { Line = line });

        OnPropertyChanged(nameof(HasDetail));
        OnPropertyChanged(nameof(DetailCostDisplay));
        OnPropertyChanged(nameof(DetailFoodCostDisplay));
        OnPropertyChanged(nameof(DetailPackagingCostDisplay));
        OnPropertyChanged(nameof(DetailPriceDisplay));
        OnPropertyChanged(nameof(DetailPriceBasisNote));
        OnPropertyChanged(nameof(DetailContributionDisplay));
        OnPropertyChanged(nameof(DetailFoodCostPercentDisplay));
        OnPropertyChanged(nameof(DetailCompletenessLabel));
        OnPropertyChanged(nameof(DetailMissingLabel));
        OnPropertyChanged(nameof(HasDetailMissing));
        OnPropertyChanged(nameof(HasMarginWithheldReason));
        OnPropertyChanged(nameof(IsDetailComplete));
        OnPropertyChanged(nameof(IsDetailIncomplete));
        OnPropertyChanged(nameof(HasPackagingCost));
        OnPropertyChanged(nameof(MarginWithheldReason));
    }

    public bool IsDetailComplete => Detail is { LineCount: > 0 } d && d.KnownLineCount == d.LineCount;
    public bool IsDetailIncomplete => Detail is not null && !IsDetailComplete;
    public bool HasPackagingCost => Detail is { PackagingCostPiastres: > 0 };

    public string DetailCostDisplay =>
        Detail is not { } d ? "—"
        : IsDetailComplete ? CashInput.Format(d.KnownCostPiastres) + " ج"
        : d.KnownLineCount == 0 ? "—"
        : "من " + CashInput.Format(d.KnownCostPiastres) + " ج";

    public string DetailFoodCostDisplay => Detail is { } d ? CashInput.Format(d.FoodCostPiastres) + " ج" : "—";
    public string DetailPackagingCostDisplay => Detail is { } d ? CashInput.Format(d.PackagingCostPiastres) + " ج" : "—";

    public string DetailPriceDisplay =>
        Detail?.SellingPricePiastres is { } p and > 0 ? CashInput.Format(p) + " ج" : "—";

    /// <summary>A margin measured against the cheapest size is not the margin of the dish; say so
    /// rather than let the figure be read as one number for the whole product.</summary>
    public string DetailPriceBasisNote => Detail?.PriceBasis switch
    {
        "variant" => "أرخص حجم",
        "none" => "لا يوجد سعر بيع مرتبط",
        _ => "",
    };

    public string DetailContributionDisplay =>
        Detail?.ContributionPiastres is { } c ? CashInput.Format(c) + " ج" : "—";

    public string DetailFoodCostPercentDisplay =>
        Detail?.FoodCostPercent is { } percent ? $"{percent:0.#}%" : "—";

    /// <summary>Why the two figures above are blank. A dash with no explanation is what sends people
    /// to a spreadsheet.</summary>
    public string MarginWithheldReason =>
        Detail is not { } d ? ""
        : d.SellingPricePiastres is null or <= 0 ? "لا يمكن حساب الهامش: الوصفة غير مرتبطة بصنف له سعر بيع."
        : IsDetailComplete ? ""
        : "الهامش محجوب لأن تكلفة الوصفة غير مكتملة - أكمل أسعار المكوّنات الناقصة أولًا.";

    public string DetailCompletenessLabel =>
        Detail is not { } d ? ""
        : d.LineCount == 0 ? "وصفة بدون مكوّنات"
        : $"{d.KnownLineCount} من {d.LineCount} مكوّن لها تكلفة ({d.CompletenessPercent}%)";

    public string DetailMissingLabel =>
        Detail is { } d && d.Missing.Count > 0 ? "ناقص: " + string.Join("، ", d.Missing) : "";

    // The converter set has "visible when EMPTY" but no inverse; these say it once here rather than
    // adding a converter used by one screen.
    public bool HasDetailMissing => DetailMissingLabel.Length > 0;
    public bool HasMarginWithheldReason => MarginWithheldReason.Length > 0;

    [RelayCommand]
    public async Task LoadDetailAsync()
    {
        if (SelectedRecipe is not { } row) { Detail = null; return; }

        IsDetailLoading = true;
        DetailError = null;
        OnPropertyChanged(nameof(HasDetail));
        try
        {
            var result = await _session.Api.GetRecipeCostDetailAsync(row.Cost.RecipeId, _session.CurrentUser!.Id);
            if (!result.Success || result.Value is null)
            {
                DetailError = result.Error ?? "تعذر تحميل تفاصيل الوصفة.";
                Detail = null;
                return;
            }
            Detail = result.Value;
        }
        finally
        {
            IsDetailLoading = false;
            OnPropertyChanged(nameof(HasDetail));
        }
    }

    // ---------------------------------------------------------------- editing

    /// <summary>One line being edited. Quantity is held as the text the user typed so a half-typed
    /// "0." does not become a zero behind their back; it is parsed once, on save.</summary>
    public sealed partial class EditableLine : ObservableObject
    {
        public required string InventoryItemId { get; init; }
        public required string ItemName { get; init; }
        public required string Unit { get; init; }

        [ObservableProperty] private string _quantityText = "";
        /// <summary>0 = مباشر, 1 = بالقطعة, 2 = تغليف - the order the picker shows them in.</summary>
        [ObservableProperty] private int _kindIndex;
        [ObservableProperty] private string _yieldRatioText = "";

        public bool IsYield => KindIndex == 1;
        partial void OnKindIndexChanged(int value) => OnPropertyChanged(nameof(IsYield));

        public string UnitLabel => IsYield ? "قطعة" : Unit;
    }

    public ObservableCollection<EditableLine> EditorLines { get; } = new();
    public ObservableCollection<InventoryItemDto> Ingredients { get; } = new();

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string? _saveError;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private InventoryItemDto? _ingredientToAdd;
    [ObservableProperty] private string _ingredientSearchText = "";

    private readonly List<InventoryItemDto> _allIngredients = new();

    partial void OnIngredientSearchTextChanged(string value) => ApplyIngredientFilter();

    private void ApplyIngredientFilter()
    {
        var term = IngredientSearchText.Trim();
        var chosen = EditorLines.Select(l => l.InventoryItemId).ToHashSet(StringComparer.Ordinal);
        Ingredients.Clear();
        foreach (var item in _allIngredients
            .Where(i => i.Active && !chosen.Contains(i.Id)
                        && (term.Length == 0 || i.Name.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Take(60))
            Ingredients.Add(item);
    }

    /// <summary>The ingredient list the editor picks from. Without it the screen still reads
    /// correctly but cannot be edited, so a failure here is reported rather than swallowed.</summary>
    private async Task LoadIngredientsAsync()
    {
        if (_allIngredients.Count > 0) return;
        var items = await _session.Api.GetInventoryItemsAsync();
        if (!items.Success || items.Value is null)
        {
            LoadError ??= items.Error ?? "تعذر تحميل قائمة الخامات.";
            return;
        }
        _allIngredients.AddRange(items.Value);
        ApplyIngredientFilter();
    }

    public bool CanStartEdit => CanEditRecipes && Detail is not null && !IsEditing;

    [RelayCommand]
    public void StartEdit()
    {
        if (Detail is not { } d || !CanEditRecipes) return;

        EditorLines.Clear();
        foreach (var line in d.Lines)
            EditorLines.Add(new EditableLine
            {
                InventoryItemId = line.InventoryItemId,
                ItemName = line.ItemName,
                Unit = line.Unit,
                QuantityText = MilliUnits.Format(line.QuantityMilliUnits),
                KindIndex = line.Kind switch { "Yield" => 1, "Packaging" => 2, _ => 0 },
                YieldRatioText = line.YieldUnitsPerBaseUnit is { } r ? MilliUnits.Format(r * 1000) : "",
            });

        SaveError = null;
        IsEditing = true;
        ApplyIngredientFilter();
        OnPropertyChanged(nameof(CanStartEdit));
    }

    [RelayCommand]
    public void CancelEdit()
    {
        IsEditing = false;
        SaveError = null;
        EditorLines.Clear();
        OnPropertyChanged(nameof(CanStartEdit));
    }

    [RelayCommand]
    public void AddIngredient()
    {
        if (IngredientToAdd is not { } item) return;
        if (EditorLines.Any(l => l.InventoryItemId == item.Id)) return;

        EditorLines.Add(new EditableLine
        {
            InventoryItemId = item.Id,
            ItemName = item.Name,
            Unit = item.Unit,
            QuantityText = "",
            KindIndex = item.IsPackaging ? 2 : 0,
        });
        IngredientToAdd = null;
        IngredientSearchText = "";
        ApplyIngredientFilter();
    }

    [RelayCommand]
    public void RemoveLine(EditableLine? line)
    {
        if (line is null) return;
        EditorLines.Remove(line);
        ApplyIngredientFilter();
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (Detail is not { } d || IsSaving) return;
        SaveError = null;

        // Every reason a save would be refused, checked here so the user is told about all of them
        // at once rather than discovering them one server round trip at a time.
        if (EditorLines.Count == 0)
        {
            SaveError = "أضف مكوّنًا واحدًا على الأقل. لفصل الوصفة عن الصنف استخدم إيقاف التفعيل.";
            return;
        }

        var lines = new List<RecipeLineInput>();
        foreach (var line in EditorLines)
        {
            if (!MilliUnits.TryParse(line.QuantityText, out var quantity) || quantity <= 0)
            {
                SaveError = $"كمية غير صالحة لـ {line.ItemName}.";
                return;
            }

            long? yieldRatio = null;
            if (line.IsYield)
            {
                if (!MilliUnits.TryParse(line.YieldRatioText, out var ratioMilli) || ratioMilli <= 0)
                {
                    SaveError = $"سطر بالقطعة يحتاج عدد القطع في الوحدة الواحدة لـ {line.ItemName}.";
                    return;
                }
                yieldRatio = ratioMilli / 1000;
                if (yieldRatio <= 0)
                {
                    SaveError = $"عدد القطع في الوحدة يجب أن يكون رقمًا صحيحًا أكبر من صفر لـ {line.ItemName}.";
                    return;
                }
            }

            lines.Add(new RecipeLineInput(
                line.InventoryItemId, quantity,
                line.KindIndex switch { 1 => "Yield", 2 => "Packaging", _ => "Direct" },
                yieldRatio));
        }

        IsSaving = true;
        try
        {
            var result = await _session.Api.SaveRecipeAsync(new SaveRecipeRequest(
                $"req-{Guid.NewGuid():N}", d.RecipeId, d.ProductId, d.Name, d.Active,
                lines, _session.CurrentUser!.Id, null, null));

            if (!result.Success)
            {
                SaveError = result.Error ?? "تعذر حفظ الوصفة.";
                return;
            }

            IsEditing = false;
            EditorLines.Clear();
            // The cost of this dish, and the menu's coverage figure above it, both just changed.
            await LoadDetailAsync();
            await RefreshSummaryAsync();
        }
        finally
        {
            IsSaving = false;
            OnPropertyChanged(nameof(CanStartEdit));
        }
    }

    /// <summary>Re-reads the list without losing the selection, so saving a recipe does not throw
    /// the owner back to the top of a hundred-row list.</summary>
    private async Task RefreshSummaryAsync()
    {
        var actor = _session.CurrentUser!.Id;
        var summaries = await _session.Api.GetRecipeCostSummariesAsync(actor);
        if (!summaries.Success || summaries.Value is null) return;

        var selectedId = SelectedRecipe?.Cost.RecipeId;
        _allRecipes.Clear();
        foreach (var summary in summaries.Value.OrderBy(s => s.CategoryName).ThenBy(s => s.ProductName))
            _allRecipes.Add(new RecipeRow { Cost = summary });
        ApplyFilter();

        if (selectedId is not null)
        {
            var again = Recipes.FirstOrDefault(r => r.Cost.RecipeId == selectedId);
            if (again is not null)
            {
                _reselecting = true;
                try { SelectedRecipe = again; }
                finally { _reselecting = false; }
            }
        }

        var coverage = await _session.Api.GetRecipeCostCoverageAsync(actor);
        Coverage = coverage.Value;
        OnPropertyChanged(nameof(CoverageHeadline));
        OnPropertyChanged(nameof(CoverageDetail));
        OnPropertyChanged(nameof(HasCoverageWarning));
    }
}

using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Inventory;
using Falah.Application.Menu;
using Falah.Application.Pricing;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>Explicit management editor over the existing menu/pricing contracts. Saves capture
/// a complete immutable DTO; retries never read the mutable form again.</summary>
public partial class MenuEditorViewModel : ObservableObject
{
    private readonly AppSession _session;
    private readonly RetainedRequest<object> _write = new("menu.authoring");
    private IReadOnlyList<CategoryDetailsDto> _categories = Array.Empty<CategoryDetailsDto>();
    private IReadOnlyList<ProductDetailsDto> _products = Array.Empty<ProductDetailsDto>();
    private IReadOnlyList<TaxRuleDto> _tax = Array.Empty<TaxRuleDto>();
    private IReadOnlyList<PriceRuleDto> _prices = Array.Empty<PriceRuleDto>();
    private object? _editing;
    public MenuEditorViewModel(AppSession session) => _session = session;
    public event Action? Closed;
    public sealed record Choice(string? Id, string Label);
    public sealed record CatalogRow(string Name, string Detail, object Value);
    public sealed partial class ProductChoice : ObservableObject
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        [ObservableProperty] private bool _selected;
    }
    public sealed partial class DayChoice : ObservableObject
    {
        public required int Value { get; init; }
        public required string Name { get; init; }
        [ObservableProperty] private bool _selected;
    }
    public sealed partial class VariantRow : ObservableObject
    {
        public string? Id { get; init; }
        [ObservableProperty] private string _name = "";
        [ObservableProperty] private string _priceText = "";
        [ObservableProperty] private Choice? _recipe;
    }
    public ObservableCollection<CatalogRow> Rows { get; } = new();
    public ObservableCollection<Choice> CategoryChoices { get; } = new();
    public ObservableCollection<Choice> RecipeChoices { get; } = new();
    public ObservableCollection<VariantRow> Variants { get; } = new();
    public ObservableCollection<ProductChoice> PriceProducts { get; } = new();
    public IReadOnlyList<Choice> Stations { get; } = new[] { new Choice(null, "بدون محطة تجهيز"), new Choice("Grill", "مشويات"), new Choice("Charcoal", "فحم") };
    public IReadOnlyList<Choice> TaxModes { get; } = new[] { new Choice("Exclusive", "تُضاف إلى السعر"), new Choice("Inclusive", "مشمولة في السعر") };
    public IReadOnlyList<DayChoice> Days { get; } = new[] { "الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" }
        .Select((name, index) => new DayChoice { Value = index, Name = name }).ToArray();
    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isReady;
    [ObservableProperty] private bool _hasEditor;
    [ObservableProperty] private string _kind = "products";
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _actionError;
    [ObservableProperty] private string? _actionNote;
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _icon = "";
    [ObservableProperty] private string _sortOrderText = "";
    [ObservableProperty] private string _priceText = "";
    [ObservableProperty] private string _taxRateText = "";
    [ObservableProperty] private string _startTimeText = "";
    [ObservableProperty] private string _endTimeText = "";
    [ObservableProperty] private bool _sendToGrill;
    [ObservableProperty] private bool _active;
    [ObservableProperty] private Choice? _category;
    [ObservableProperty] private Choice? _recipe;
    [ObservableProperty] private Choice? _station;
    [ObservableProperty] private Choice? _taxMode;
    public bool IsProduct => Kind == "products";
    public bool IsCategory => Kind == "categories";
    public bool IsTax => Kind == "tax";
    public bool IsPriceRule => Kind == "prices";
    public bool HasSortOrder => IsProduct || IsCategory;
    public bool HasPrice => IsProduct || IsPriceRule;
    public bool HasActive => IsTax || IsPriceRule;
    public bool CanOpen => new[] { "menu_manage", "settings_manage", "offers_manage" }.Any(_session.HasPermission);
    public bool CanManageKind => _session.HasPermission(IsTax ? "settings_manage" : IsPriceRule ? "offers_manage" : "menu_manage");
    public bool CanBrowse => !IsBusy && !IsLoading && !_write.HasPending;
    public bool CanEdit => CanBrowse && IsReady && CanManageKind;
    public bool HasNoRows => IsReady && !IsLoading && Rows.Count == 0 && SearchText.Trim().Length == 0;

    /// <summary>What the right-hand half says when nothing is selected. It used to say nothing at
    /// all - half the dialog was blank, which is the "giant empty dialog" this screen was known for.</summary>
    public string EmptySelectionHint => Kind switch
    {
        "categories" => "اختر قسمًا من القائمة لتعديل اسمه أو ترتيبه أو إيقافه، أو اضغط إضافة لقسم جديد.",
        "tax" => "اختر قاعدة ضريبة لتعديل نسبتها وطريقة احتسابها، أو اضغط إضافة لقاعدة جديدة.",
        "prices" => "اختر عرض سعر لتعديل سعره ومواعيده، أو اضغط إضافة لعرض جديد.",
        _ => "اختر صنفًا من القائمة لتعديل سعره وقسمه ومحطة تجهيزه وصورته وأحجامه ووصفته، أو اضغط إضافة لصنف جديد.",
    };
    public bool CanToggleStatus => CanEdit && _editing is CategoryDetailsDto or ProductDetailsDto;
    public string StatusActionLabel => _editing switch { CategoryDetailsDto c => c.Active ? "إيقاف القسم" : "تفعيل القسم", ProductDetailsDto p => p.IsAvailable ? "إيقاف الصنف" : "إتاحة الصنف", _ => "" };
    public string EditorTitle => _editing is null ? "إضافة جديدة" : "تعديل البيانات";
    public string SaveLabel => IsBusy ? "جاري الحفظ والتحقق..." : _write.HasPending ? "التحقق من الحفظ السابق" : "حفظ البيانات";
    public string KindTitle => Kind switch { "categories" => "الأقسام", "tax" => "قواعد الضريبة", "prices" => "عروض الأسعار", _ => "الأصناف والأحجام" };
    partial void OnIsBusyChanged(bool value) => NotifyState();
    partial void OnIsLoadingChanged(bool value) => NotifyState();
    private void NotifyState()
    {
        foreach (var name in new[] { nameof(CanBrowse), nameof(CanEdit), nameof(CanToggleStatus), nameof(HasNoRows), nameof(SaveLabel), nameof(StatusActionLabel), nameof(EditorTitle), nameof(CanEditImage), nameof(ImageHint) }) OnPropertyChanged(name);
        _session.SetManagementOperationPending(this, IsBusy || _write.HasPending);
    }
    public async Task OpenAsync()
    {
        if (!CanOpen || _session.HasUnresolvedOperation) return;
        IsOpen = true; HasEditor = false; ActionError = ActionNote = null;
        await LoadAsync();
    }
    [RelayCommand] private void Close()
    {
        if (!CanBrowse) return;
        IsOpen = false; HasEditor = false; Closed?.Invoke();
    }
    [RelayCommand] public async Task LoadAsync()
    {
        if (!CanBrowse) return;
        await ReloadAsync();
    }
    private async Task ReloadAsync()
    {
        IsLoading = true; IsReady = false; LoadError = null; Rows.Clear();
        try
        {
            var categories = await _session.Api.GetAllCategoriesAsync();
            if (!categories.Success || categories.Value is null) { LoadError = categories.Error; return; }
            var products = await _session.Api.GetAllProductsAsync();
            if (!products.Success || products.Value is null) { LoadError = products.Error; return; }
            var recipes = await _session.Api.GetRecipesAsync();
            if (!recipes.Success || recipes.Value is null) { LoadError = recipes.Error; return; }
            var tax = await _session.Api.GetTaxRulesAsync();
            if (!tax.Success || tax.Value is null) { LoadError = tax.Error; return; }
            var prices = await _session.Api.GetPriceRulesAsync();
            if (!prices.Success || prices.Value is null) { LoadError = prices.Error; return; }
            _categories = categories.Value; _products = products.Value; _tax = tax.Value; _prices = prices.Value;
            CategoryChoices.Clear(); foreach (var c in _categories.OrderBy(c => c.SortOrder)) CategoryChoices.Add(new(c.Id, c.Name + (c.Active ? "" : " (متوقف)")));
            RecipeChoices.Clear(); RecipeChoices.Add(new(null, "بدون وصفة مرتبطة"));
            foreach (var r in recipes.Value) RecipeChoices.Add(new(r.Id, r.Name));
            foreach (var id in _products.SelectMany(p => p.Variants.Select(v => v.RecipeId).Append(p.RecipeId)).Where(id => id is not null).Distinct())
                if (!RecipeChoices.Any(c => c.Id == id)) RecipeChoices.Add(new(id, "وصفة مرتبطة غير متاحة في القائمة"));
            PriceProducts.Clear(); foreach (var p in _products) PriceProducts.Add(new() { Id = p.Id, Name = p.Name });
            IsReady = true; RefreshRows();
        }
        finally { IsLoading = false; }
    }
    [RelayCommand] private void SetKind(string kind)
    {
        if (!CanBrowse || kind is not ("products" or "categories" or "tax" or "prices")) return;
        Kind = kind; HasEditor = false; _editing = null; ActionError = ActionNote = null; ProductImageUri = null;
        foreach (var name in new[] { nameof(IsProduct), nameof(IsCategory), nameof(IsTax), nameof(IsPriceRule), nameof(HasSortOrder), nameof(HasPrice), nameof(HasActive), nameof(CanManageKind), nameof(KindTitle), nameof(EmptySelectionHint) }) OnPropertyChanged(name);
        RefreshRows(); NotifyState();
    }
    /// <summary>A hundred-odd products in a scroll-only list is a scroll-only list. Filtering by
    /// name is how anyone actually finds the one they came to edit.</summary>
    [ObservableProperty] private string _searchText = "";

    partial void OnSearchTextChanged(string value) => RefreshRows();

    public bool HasNoMatches => IsReady && !IsLoading && Rows.Count == 0 && SearchText.Trim().Length > 0;

    private void RefreshRows()
    {
        Rows.Clear();
        IEnumerable<CatalogRow> rows = Kind switch
        {
            "categories" => _categories.OrderBy(c => c.SortOrder).Select(c => new CatalogRow(c.Name, c.Active ? "نشط" : "متوقف", c)),
            "tax" => _tax.Select(t => new CatalogRow(t.Name, $"{t.RateBasisPoints / 100m:0.##}% • {(t.Enabled ? "مفعّلة" : "متوقفة")}", t)),
            "prices" => _prices.Select(p => new CatalogRow(p.Name, CashInput.Format(p.FixedPricePiastres) + " ج • " + (p.Active ? "نشط" : "متوقف"), p)),
            _ => _products.OrderBy(p => p.SortOrder).Select(p => new CatalogRow(p.Name, CashInput.Format(p.BasePricePiastres) + " ج • " + (p.IsAvailable ? "متاح" : "متوقف"), p))
        };
        var term = SearchText.Trim();
        if (term.Length > 0)
            rows = rows.Where(r => r.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                                || r.Detail.Contains(term, StringComparison.CurrentCultureIgnoreCase));

        foreach (var row in rows) Rows.Add(row);
        OnPropertyChanged(nameof(HasNoRows));
        OnPropertyChanged(nameof(HasNoMatches));
    }
    [RelayCommand] private void New()
    {
        if (!CanEdit) return;
        ResetForm(); _editing = null; HasEditor = true; NotifyState(); NotifyImageState();
    }
    private void ResetForm()
    {
        Name = Icon = PriceText = TaxRateText = StartTimeText = EndTimeText = ""; SortOrderText = "0";
        Category = null; Recipe = RecipeChoices.FirstOrDefault(); Station = Stations[0]; TaxMode = null;
        SendToGrill = Active = false; ActionError = ActionNote = null; Variants.Clear(); ProductImageUri = null;
        foreach (var p in PriceProducts) p.Selected = false;
        foreach (var d in Days) d.Selected = false;
    }
    [RelayCommand] private void SelectRow(CatalogRow row)
    {
        if (!CanBrowse || !IsReady) return;
        ResetForm(); _editing = row.Value; HasEditor = true;
        switch (row.Value)
        {
            case CategoryDetailsDto c: Name = c.Name; Icon = c.Icon ?? ""; SortOrderText = c.SortOrder.ToString(CultureInfo.InvariantCulture); break;
            case ProductDetailsDto p:
                Name = p.Name; PriceText = CashInput.Format(p.BasePricePiastres); SortOrderText = p.SortOrder.ToString(CultureInfo.InvariantCulture);
                Category = CategoryChoices.FirstOrDefault(c => c.Id == p.CategoryId); Recipe = RecipeChoices.FirstOrDefault(r => r.Id == p.RecipeId);
                Station = Stations.FirstOrDefault(s => s.Id == p.PreparationStation); SendToGrill = p.SendToGrill;
                foreach (var v in p.Variants) Variants.Add(new() { Id = v.Id, Name = v.Name, PriceText = CashInput.Format(v.PricePiastres), Recipe = RecipeChoices.FirstOrDefault(r => r.Id == v.RecipeId) });
                LoadProductImage(p);
                break;
            case TaxRuleDto t: Name = t.Name; TaxRateText = CashInput.Format(t.RateBasisPoints); TaxMode = TaxModes.FirstOrDefault(m => m.Id == t.Mode); Active = t.Enabled; break;
            case PriceRuleDto p:
                Name = p.Name; PriceText = CashInput.Format(p.FixedPricePiastres); Active = p.Active;
                StartTimeText = p.StartTime?.ToString("HH:mm") ?? ""; EndTimeText = p.EndTime?.ToString("HH:mm") ?? "";
                foreach (var product in PriceProducts) product.Selected = p.ProductIds.Contains(product.Id);
                foreach (var day in Days) day.Selected = p.DaysOfWeek?.Contains(day.Value) == true;
                break;
        }
        NotifyState();
    }
    // ---- Product photo ----
    // Deliberately NOT part of the form's save: the picture is uploaded the moment it is chosen and
    // deleted the moment it is removed. Carrying half a megabyte of bytes through RetainedRequest -
    // which exists so an ambiguous WRITE can be replayed verbatim - would put an image in a durable
    // retry file, and a photo that failed to upload is a photo the user simply picks again.

    [ObservableProperty] private Uri? _productImageUri;
    [ObservableProperty] private bool _isImageBusy;
    public bool HasProductImage => ProductImageUri is not null;
    public bool CanEditImage => IsProduct && HasEditor && _editing is ProductDetailsDto && CanEdit && !IsImageBusy;
    /// <summary>Where the operator is told what happens when there is no photo, instead of the card
    /// simply looking unfinished.</summary>
    public string ImageHint => _editing is not ProductDetailsDto
        ? "احفظ الصنف أولًا ثم أضف صورته."
        : HasProductImage ? "تظهر هذه الصورة على زرار الصنف في نقطة البيع."
        : "بدون صورة يظهر رمز القسم على زرار الصنف - وهذا مقبول تمامًا.";

    partial void OnProductImageUriChanged(Uri? value) => NotifyImageState();
    partial void OnIsImageBusyChanged(bool value) => NotifyImageState();
    private void NotifyImageState()
    {
        foreach (var name in new[] { nameof(HasProductImage), nameof(CanEditImage), nameof(ImageHint) }) OnPropertyChanged(name);
    }

    private void LoadProductImage(ProductDetailsDto? product)
    {
        ProductImageUri = product is null ? null : _session.Api.ProductImageUri(product.Id, product.ImageVersion);
    }

    /// <summary>Reads the file the user picked. Set by the view, which owns the file picker - a
    /// picker needs the window handle, and the view model has no business holding one.</summary>
    public Func<Task<(string ContentType, byte[] Bytes)?>>? PickImageAsync { get; set; }

    [RelayCommand] private async Task ChooseImageAsync()
    {
        if (!CanEditImage || PickImageAsync is null || _editing is not ProductDetailsDto product || _session.CurrentUser is null) return;
        ActionError = ActionNote = null;
        (string ContentType, byte[] Bytes)? picked;
        try { picked = await PickImageAsync(); }
        catch (Exception ex) { ActionError = "تعذّر قراءة الملف: " + ex.Message; return; }
        if (picked is null) return;

        if (picked.Value.Bytes.Length > Falah.Domain.Menu.ProductImageRules.MaxBytes)
        {
            // Refused here as well as on the server, because the operator should learn the limit
            // before spending the upload, not after.
            ActionError = $"حجم الصورة أكبر من الحد المسموح ({Falah.Domain.Menu.ProductImageRules.MaxBytes / 1024} كيلوبايت). اختر صورة أصغر.";
            return;
        }

        IsImageBusy = true;
        try
        {
            var result = await _session.Api.SetProductImageAsync(new SetProductImageRequest(
                _session.CurrentUser.Id, product.Id, picked.Value.ContentType, Convert.ToBase64String(picked.Value.Bytes)));
            if (!result.Success || result.Value is null) { ActionError = result.Error ?? "تعذّر حفظ الصورة."; return; }

            // The new version stamp makes a different URL, so the picture that comes back is the
            // one just uploaded rather than the one the image cache already holds.
            _editing = product with { ImageVersion = result.Value.Version };
            LoadProductImage((ProductDetailsDto)_editing);
            ActionNote = "تم حفظ الصورة.";
            await ReloadAsync();
        }
        finally { IsImageBusy = false; }
    }

    [RelayCommand] private async Task RemoveImageAsync()
    {
        if (!CanEditImage || !HasProductImage || _editing is not ProductDetailsDto product || _session.CurrentUser is null) return;
        ActionError = ActionNote = null;
        IsImageBusy = true;
        try
        {
            var result = await _session.Api.RemoveProductImageAsync(product.Id, _session.CurrentUser.Id);
            if (!result.Success) { ActionError = result.Error ?? "تعذّر حذف الصورة."; return; }

            _editing = product with { ImageVersion = null };
            ProductImageUri = null;
            ActionNote = "تم حذف الصورة؛ يظهر رمز القسم بدلًا منها.";
            await ReloadAsync();
        }
        finally { IsImageBusy = false; }
    }

    [RelayCommand] private void AddVariant() { if (CanEdit && IsProduct) Variants.Add(new() { Recipe = RecipeChoices.FirstOrDefault() }); }
    [RelayCommand] private void RemoveVariant(VariantRow row) { if (CanEdit && IsProduct) Variants.Remove(row); }
    private static bool TryTime(string text, out TimeOnly? time)
    {
        time = null; if (string.IsNullOrWhiteSpace(text)) return true;
        if (!TimeOnly.TryParseExact(text.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return false;
        time = parsed; return true;
    }
    private object? CreateRequest()
    {
        if (!CanEdit || !HasEditor || _session.CurrentUser is null) return null;
        if (string.IsNullOrWhiteSpace(Name)) { ActionError = "الاسم مطلوب."; return null; }
        var key = $"menu-{Guid.NewGuid():N}"; var actor = _session.CurrentUser.Id;
        var sort = 0; long price = 0;
        if (HasSortOrder && !int.TryParse(SortOrderText, NumberStyles.Integer, CultureInfo.InvariantCulture, out sort)) { ActionError = "أدخل ترتيب عرض صحيحًا."; return null; }
        if (HasPrice && (!CashInput.TryParse(PriceText, out price) || price <= 0)) { ActionError = "أدخل سعرًا صحيحًا أكبر من صفر."; return null; }
        if (IsCategory) return _editing is CategoryDetailsDto c
            ? new UpdateCategoryRequest(c.Id, Name.Trim(), string.IsNullOrWhiteSpace(Icon) ? null : Icon.Trim(), sort, actor, key)
            : new CreateCategoryRequest(key, Name.Trim(), string.IsNullOrWhiteSpace(Icon) ? null : Icon.Trim(), sort, actor);
        if (IsProduct)
        {
            if (Category?.Id is null || !CategoryChoices.Contains(Category) || Station is null) { ActionError = "اختر القسم ومحطة التجهيز."; return null; }
            var variants = new List<ProductVariantInput>();
            foreach (var v in Variants)
            {
                if (string.IsNullOrWhiteSpace(v.Name) || !CashInput.TryParse(v.PriceText, out var amount) || amount <= 0) { ActionError = "كل حجم يحتاج اسمًا وسعرًا صحيحًا أكبر من صفر."; return null; }
                variants.Add(new(v.Id, v.Name.Trim(), amount, v.Recipe?.Id));
            }
            return _editing is ProductDetailsDto p
                ? new UpdateProductRequest(p.Id, Category.Id, Name.Trim(), price, SendToGrill, Station.Id, sort, Recipe?.Id, variants.ToArray(), actor, key)
                : new CreateProductRequest(key, Category.Id, Name.Trim(), price, SendToGrill, Station.Id, sort, Recipe?.Id, variants.ToArray(), actor);
        }
        if (IsTax)
        {
            if (!CashInput.TryParse(TaxRateText, out var rate) || rate > int.MaxValue || TaxMode?.Id is null) { ActionError = "أدخل نسبة صحيحة واختر طريقة احتساب الضريبة."; return null; }
            return _editing is TaxRuleDto t ? new UpdateTaxRuleRequest(t.Id, Name.Trim(), (int)rate, TaxMode.Id, Active, actor, key)
                : new CreateTaxRuleRequest(key, Name.Trim(), (int)rate, TaxMode.Id, Active, actor);
        }
        if (!TryTime(StartTimeText, out var start) || !TryTime(EndTimeText, out var end)) { ActionError = "أدخل الوقت بنظام 24 ساعة مثل 18:30، أو اتركه فارغًا."; return null; }
        var ids = PriceProducts.Where(p => p.Selected).Select(p => p.Id).ToArray();
        if (ids.Length == 0) { ActionError = "اختر صنفًا واحدًا على الأقل للعرض."; return null; }
        var days = Days.Where(d => d.Selected).Select(d => d.Value).ToArray();
        return new SavePriceRuleRequest(key, (_editing as PriceRuleDto)?.Id, Name.Trim(), ids, price, days.Length == 0 ? null : days, start, end, Active, actor, null, null);
    }
    [RelayCommand] private async Task SaveAsync()
    {
        if (IsBusy) return;
        ActionError = null;
        var request = _write.Value ?? CreateRequest();
        if (request is not null) await SubmitAsync(request);
    }
    [RelayCommand] private async Task ToggleStatusAsync()
    {
        if (!CanToggleStatus) return;
        var key = $"menu-status-{Guid.NewGuid():N}"; var actor = _session.CurrentUser!.Id;
        object? request = _editing switch { CategoryDetailsDto c => new SetCategoryActiveRequest(c.Id, !c.Active, actor, key), ProductDetailsDto p => new SetProductAvailabilityRequest(p.Id, !p.IsAvailable, actor, key), _ => null };
        if (request is not null) await SubmitAsync(request);
    }
    private async Task SubmitAsync(object request)
    {
        IsBusy = true; ActionError = ActionNote = null;
        try
        {
            var result = await _write.SendAsync(request, SendWriteAsync);
            if (!result.Success) { ActionError = _write.FailureMessage(result.Error); return; }
            HasEditor = false; _editing = null; ActionNote = "تم تأكيد الحفظ. سيستخدم الكاشير البيانات المحدّثة عند تحميل القائمة.";
            await ReloadAsync();
        }
        finally { IsBusy = false; }
    }
    private static ApiClient.ApiResult<bool> Confirm<T>(ApiClient.ApiResult<T> result) =>
        new(result.Success, result.Success, result.Error, result.OutcomeUnknown, result.StatusCode);
    private async Task<ApiClient.ApiResult<bool>> SendWriteAsync(object request) => request switch
    {
        CreateCategoryRequest r => Confirm(await _session.Api.CreateCategoryAsync(r)),
        UpdateCategoryRequest r => await _session.Api.UpdateCategoryAsync(r),
        SetCategoryActiveRequest r => await _session.Api.SetCategoryActiveAsync(r),
        CreateProductRequest r => Confirm(await _session.Api.CreateProductAsync(r)),
        UpdateProductRequest r => await _session.Api.UpdateProductAsync(r),
        SetProductAvailabilityRequest r => await _session.Api.SetProductAvailabilityAsync(r),
        CreateTaxRuleRequest r => Confirm(await _session.Api.CreateTaxRuleAsync(r)),
        UpdateTaxRuleRequest r => await _session.Api.UpdateTaxRuleAsync(r),
        SavePriceRuleRequest r => Confirm(await _session.Api.SavePriceRuleAsync(r)),
        _ => throw new InvalidOperationException("Unsupported menu command.")
    };
}

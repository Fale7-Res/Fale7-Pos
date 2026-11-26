using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Menu;
using Falah.Application.Pricing;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Owner, 2026-11-25: the offers screen sets every offer shape - a fixed price or a percentage on chosen items, an
/// amount or a percentage off whole categories, a gift with an order over an amount (several tiers, the highest one
/// reached is given) - and its conditions: days, hours, dates, order types, stations, and limits per order, per day
/// and overall. Every offer stays optional at the till. The server checks the whole offer again when it is saved.
/// </summary>
public partial class MenuEditorViewModel
{
    public IReadOnlyList<Choice> OfferKinds { get; } = new[]
    {
        new Choice("ItemFixedPrice", "سعر ثابت على أصناف (سعر واحد لكل الأحجام)"),
        new Choice("ItemPercent", "نسبة خصم على أصناف"),
        new Choice("CategoryAmount", "مبلغ خصم على كل أصناف قسم"),
        new Choice("CategoryPercent", "نسبة خصم على كل أصناف قسم"),
        new Choice("OrderGift", "هدية مع طلب فوق مبلغ"),
    };

    public sealed partial class GiftTierRow : ObservableObject
    {
        private readonly Func<string?, IReadOnlyList<Choice>> _variantsOf;
        public GiftTierRow(Func<string?, IReadOnlyList<Choice>> variantsOf) => _variantsOf = variantsOf;
        [ObservableProperty] private string _thresholdText = "";
        [ObservableProperty] private Choice? _product;
        [ObservableProperty] private Choice? _variant;
        [ObservableProperty] private string _quantityText = "1";
        public ObservableCollection<Choice> VariantChoices { get; } = new();
        public bool HasVariants => VariantChoices.Count > 0;
        partial void OnProductChanged(Choice? value)
        {
            VariantChoices.Clear();
            foreach (var v in _variantsOf(value?.Id)) VariantChoices.Add(v);
            Variant = VariantChoices.Count == 1 ? VariantChoices[0] : null;
            OnPropertyChanged(nameof(HasVariants));
        }
    }

    public ObservableCollection<ProductChoice> PriceCategories { get; } = new();
    public ObservableCollection<Choice> GiftProductChoices { get; } = new();
    public ObservableCollection<GiftTierRow> GiftTiers { get; } = new();

    [ObservableProperty] private Choice? _offerKind;
    [ObservableProperty] private string _percentText = "";
    [ObservableProperty] private string _amountOffText = "";
    [ObservableProperty] private string _startDateText = "";
    [ObservableProperty] private string _endDateText = "";
    [ObservableProperty] private bool _offerTakeaway;
    [ObservableProperty] private bool _offerDelivery;
    [ObservableProperty] private bool _offerPickup;
    [ObservableProperty] private bool _offerTill;
    [ObservableProperty] private bool _offerCallCenter;
    [ObservableProperty] private string _maxPerOrderText = "";
    [ObservableProperty] private string _maxPerDayText = "";
    [ObservableProperty] private string _maxTotalText = "";
    [ObservableProperty] private bool _giftIncludesDelivery = true;
    [ObservableProperty] private bool _giftAutoAdd;

    private string OfferKindId => OfferKind?.Id ?? "ItemFixedPrice";
    public bool IsOfferFixedPrice => IsPriceRule && OfferKindId == "ItemFixedPrice";
    public bool IsOfferPercent => IsPriceRule && OfferKindId is "ItemPercent" or "CategoryPercent";
    public bool IsOfferAmount => IsPriceRule && OfferKindId == "CategoryAmount";
    public bool IsOfferOnItems => IsPriceRule && OfferKindId is "ItemFixedPrice" or "ItemPercent";
    public bool IsOfferOnCategories => IsPriceRule && OfferKindId is "CategoryAmount" or "CategoryPercent";
    public bool IsOfferGift => IsPriceRule && OfferKindId == "OrderGift";

    /// <summary>One line under the kind saying what the offer will do, so the owner reads it back before saving.</summary>
    public string OfferKindHint => OfferKindId switch
    {
        "ItemPercent" => "كل صنف مختار يتباع بسعره ناقص النسبة، على أي حجم.",
        "CategoryAmount" => "كل أصناف الأقسام المختارة تتباع بسعرها ناقص المبلغ (ما ينزلش تحت صفر).",
        "CategoryPercent" => "كل أصناف الأقسام المختارة تتباع بسعرها ناقص النسبة.",
        "OrderGift" => "الطلب اللي يوصل لمبلغ ياخد هديته ببلاش. لو فيه أكتر من مبلغ، ياخد هدية أعلى مبلغ وصله بس.",
        _ => "الأصناف المختارة تتباع بالسعر ده، مهما كان الحجم.",
    };

    partial void OnOfferKindChanged(Choice? value) => NotifyOfferKind();

    private void NotifyOfferKind()
    {
        foreach (var n in new[] { nameof(IsOfferFixedPrice), nameof(IsOfferPercent), nameof(IsOfferAmount), nameof(IsOfferOnItems),
                     nameof(IsOfferOnCategories), nameof(IsOfferGift), nameof(OfferKindHint), nameof(HasPrice) })
            OnPropertyChanged(n);
    }

    private IReadOnlyList<Choice> VariantsOf(string? productId) =>
        _products.FirstOrDefault(p => p.Id == productId)?.Variants.Select(v => new Choice(v.Id, $"{v.Name} ({CashInput.Display(v.PricePiastres)} ج)")).ToList()
        ?? (IReadOnlyList<Choice>)Array.Empty<Choice>();

    private void LoadOfferChoices()
    {
        PriceCategories.Clear();
        foreach (var c in _categories.OrderBy(c => c.SortOrder)) PriceCategories.Add(new() { Id = c.Id, Name = c.Name });
        GiftProductChoices.Clear();
        foreach (var p in _products.OrderBy(p => p.Name)) GiftProductChoices.Add(new(p.Id, p.Name));
    }

    private void ResetOffer()
    {
        OfferKind = OfferKinds[0];
        PercentText = AmountOffText = StartDateText = EndDateText = MaxPerOrderText = MaxPerDayText = MaxTotalText = "";
        OfferTakeaway = OfferDelivery = OfferPickup = OfferTill = OfferCallCenter = false;
        GiftIncludesDelivery = true; GiftAutoAdd = false;
        foreach (var c in PriceCategories) c.Selected = false;
        GiftTiers.Clear();
        NotifyOfferKind();
    }

    private void LoadOffer(PriceRuleDto p)
    {
        OfferKind = OfferKinds.FirstOrDefault(k => k.Id == p.Kind) ?? OfferKinds[0];
        PriceText = p.FixedPricePiastres > 0 ? CashInput.Format(p.FixedPricePiastres) : "";
        PercentText = p.PercentBasisPoints > 0 ? (p.PercentBasisPoints / 100m).ToString("0.##", CultureInfo.InvariantCulture) : "";
        AmountOffText = p.AmountOffPiastres > 0 ? CashInput.Format(p.AmountOffPiastres) : "";
        StartDateText = p.StartDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
        EndDateText = p.EndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
        OfferTakeaway = p.OrderTypes?.Contains("takeaway") == true;
        OfferDelivery = p.OrderTypes?.Contains("delivery") == true;
        OfferPickup = p.OrderTypes?.Contains("pickup") == true;
        OfferTill = p.Stations?.Contains("till") == true;
        OfferCallCenter = p.Stations?.Contains("callcenter") == true;
        MaxPerOrderText = p.MaxPerOrder?.ToString(CultureInfo.InvariantCulture) ?? "";
        MaxPerDayText = p.MaxPerDay?.ToString(CultureInfo.InvariantCulture) ?? "";
        MaxTotalText = p.MaxTotal?.ToString(CultureInfo.InvariantCulture) ?? "";
        GiftIncludesDelivery = p.GiftIncludesDelivery; GiftAutoAdd = p.GiftAutoAdd;
        foreach (var c in PriceCategories) c.Selected = p.CategoryIds?.Contains(c.Id) == true;
        GiftTiers.Clear();
        foreach (var t in p.GiftTiers ?? Array.Empty<GiftTierDto>())
        {
            var row = new GiftTierRow(VariantsOf) { ThresholdText = CashInput.Format(t.ThresholdPiastres), QuantityText = t.Quantity.ToString(CultureInfo.InvariantCulture) };
            row.Product = GiftProductChoices.FirstOrDefault(c => c.Id == t.ProductId);
            row.Variant = row.VariantChoices.FirstOrDefault(v => v.Id == t.VariantId) ?? row.Variant;
            GiftTiers.Add(row);
        }
        NotifyOfferKind();
    }

    [RelayCommand] private void AddGiftTier() { if (CanEdit && IsOfferGift) GiftTiers.Add(new GiftTierRow(VariantsOf)); }
    [RelayCommand] private void RemoveGiftTier(GiftTierRow row) { if (CanEdit && IsOfferGift) GiftTiers.Remove(row); }

    private static bool TryDate(string text, out DateOnly? date)
    {
        date = null; if (string.IsNullOrWhiteSpace(text)) return true;
        if (!DateOnly.TryParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) return false;
        date = d; return true;
    }

    private static bool TryLimit(string text, out int? limit)
    {
        limit = null; if (string.IsNullOrWhiteSpace(text)) return true;
        if (!int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n < 1) return false;
        limit = n; return true;
    }

    /// <summary>The save request for the offer on screen, or null with ActionError saying what to fix.</summary>
    private SavePriceRuleRequest? CreateOfferRequest(string key, string actor, TimeOnly? start, TimeOnly? end, int[] days)
    {
        var kind = OfferKindId;
        long fixedPrice = 0, amountOff = 0; var percent = 0;
        if (kind == "ItemFixedPrice" && (!CashInput.TryParse(PriceText, out fixedPrice) || fixedPrice <= 0)) { ActionError = "أدخل سعر العرض أكبر من صفر."; return null; }
        if (kind is "ItemPercent" or "CategoryPercent")
        {
            if (!decimal.TryParse(PercentText.Trim().TrimEnd('%'), NumberStyles.Number, CultureInfo.InvariantCulture, out var pct) || pct <= 0 || pct > 100)
            { ActionError = "أدخل نسبة الخصم من 0.01 لـ 100 (مثال 10 يعني 10%)."; return null; }
            percent = (int)Math.Round(pct * 100m, MidpointRounding.AwayFromZero);
        }
        if (kind == "CategoryAmount" && (!CashInput.TryParse(AmountOffText, out amountOff) || amountOff <= 0)) { ActionError = "أدخل مبلغ الخصم أكبر من صفر."; return null; }

        var products = kind is "ItemFixedPrice" or "ItemPercent" ? PriceProducts.Where(p => p.Selected).Select(p => p.Id).ToArray() : Array.Empty<string>();
        if (kind is "ItemFixedPrice" or "ItemPercent" && products.Length == 0) { ActionError = "اختر صنفًا واحدًا على الأقل للعرض."; return null; }
        var categories = kind is "CategoryAmount" or "CategoryPercent" ? PriceCategories.Where(c => c.Selected).Select(c => c.Id).ToArray() : Array.Empty<string>();
        if (kind is "CategoryAmount" or "CategoryPercent" && categories.Length == 0) { ActionError = "اختار قسم واحد على الأقل للعرض."; return null; }

        var tiers = new List<GiftTierDto>();
        if (kind == "OrderGift")
        {
            if (GiftTiers.Count == 0) { ActionError = "أضف هدية واحدة على الأقل: المبلغ والصنف والعدد."; return null; }
            foreach (var row in GiftTiers)
            {
                if (!CashInput.TryParse(row.ThresholdText, out var threshold) || threshold <= 0) { ActionError = "كل هدية محتاجة مبلغ أكبر من صفر."; return null; }
                if (row.Product?.Id is null) { ActionError = "اختار صنف كل هدية."; return null; }
                if (row.HasVariants && row.Variant?.Id is null) { ActionError = $"{row.Product.Label}: اختار حجم الهدية."; return null; }
                if (!int.TryParse(row.QuantityText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var qty) || qty is < 1 or > 99) { ActionError = "عدد الهدية من 1 لـ 99."; return null; }
                tiers.Add(new GiftTierDto(threshold, row.Product.Id, row.Variant?.Id, qty));
            }
        }

        if (!TryDate(StartDateText, out var startDate) || !TryDate(EndDateText, out var endDate)) { ActionError = "اكتب التاريخ بالشكل 2026-12-31، أو سيبه فاضي."; return null; }
        if (!TryLimit(MaxPerOrderText, out var perOrder) || !TryLimit(MaxPerDayText, out var perDay) || !TryLimit(MaxTotalText, out var total))
        { ActionError = "الحد الأقصى رقم صحيح 1 أو أكتر، أو سيبه فاضي."; return null; }

        var orderTypes = new[] { (OfferTakeaway, "takeaway"), (OfferDelivery, "delivery"), (OfferPickup, "pickup") }.Where(x => x.Item1).Select(x => x.Item2).ToArray();
        var stations = new[] { (OfferTill, "till"), (OfferCallCenter, "callcenter") }.Where(x => x.Item1).Select(x => x.Item2).ToArray();

        return new SavePriceRuleRequest(key, (_editing as PriceRuleDto)?.Id, Name.Trim(), products, fixedPrice, days.Length == 0 ? null : days, start, end, Active,
            actor, null, null, kind, percent, amountOff, categories, startDate, endDate,
            orderTypes.Length == 0 ? null : orderTypes, stations.Length == 0 ? null : stations, perOrder, perDay, total,
            tiers, GiftIncludesDelivery, kind == "OrderGift" && GiftAutoAdd);
    }

    /// <summary>The offers list's second line: what the offer does and whether it is on.</summary>
    private static string OfferSummary(PriceRuleDto p) => (p.Kind switch
    {
        "ItemPercent" => $"خصم {p.PercentBasisPoints / 100m:0.##}% على {p.ProductIds.Count} صنف",
        "CategoryAmount" => $"{CashInput.Display(p.AmountOffPiastres)} ج أقل على {p.CategoryIds?.Count ?? 0} قسم",
        "CategoryPercent" => $"خصم {p.PercentBasisPoints / 100m:0.##}% على {p.CategoryIds?.Count ?? 0} قسم",
        "OrderGift" => $"هدية: {string.Join(" / ", (p.GiftTiers ?? Array.Empty<GiftTierDto>()).Select(t => $"فوق {CashInput.Display(t.ThresholdPiastres)} ج"))}",
        _ => $"{CashInput.Display(p.FixedPricePiastres)} ج",
    }) + " • " + (p.Active ? "نشط" : "متوقف");
}

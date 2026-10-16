using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Reports;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Management reporting for one business date. Every figure is read straight from /api/reports/* -
/// this class never sums, nets or averages anything itself, because the server already derives all
/// of it from the same Orders/Expenses/Treasury/Delivery rows every other screen reads. Where the
/// server distinguishes figures that are easy to conflate (collected vs. pending electronic, offers
/// vs. comps vs. manual discounts), the screen keeps them distinct rather than helpfully merging
/// them into a friendlier-looking single number.
/// </summary>
public partial class ReportsViewModel : ObservableObject
{
    private readonly AppSession _session;

    public ReportsViewModel(AppSession session)
    {
        _session = session;
        // Cairo business date, matching how the server stamps every transaction.
        var cairo = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        _businessDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, cairo).DateTime);
        _selectedPeriod = Periods[0];
    }

    [ObservableProperty] private DateOnly _businessDate;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;

    public sealed record PeriodOption(string Key, string Label);

    /// <summary>The dashboard's own period list, plus the one it does not need: this screen keeps a
    /// date picker for a named day, so "يوم محدد" is what re-enables it. Every /api/reports/*
    /// endpoint already took from/to - only this screen was still asking each of them for a single
    /// day twice over.</summary>
    public IReadOnlyList<PeriodOption> Periods { get; } = new[]
    {
        new PeriodOption("today", "اليوم"),
        new PeriodOption("yesterday", "أمس"),
        new PeriodOption("last7", "آخر ٧ أيام"),
        new PeriodOption("last30", "آخر ٣٠ يومًا"),
        new PeriodOption("month", "هذا الشهر"),
        new PeriodOption("day", "يوم محدد"),
    };

    [ObservableProperty] private PeriodOption? _selectedPeriod;

    /// <summary>The date picker is only meaningful for the one period that names a day.</summary>
    public bool IsSpecificDay => SelectedPeriod?.Key == "day";

    /// <summary>Set while ShowDay switches the period on the user's behalf, so the switch does not
    /// fire its own load on top of the one ShowDay is about to run.</summary>
    private bool _suppressPeriodReload;

    partial void OnSelectedPeriodChanged(PeriodOption? value)
    {
        OnPropertyChanged(nameof(IsSpecificDay));
        OnPropertyChanged(nameof(RangeLabel));
        if (_suppressPeriodReload) return;
        _ = LoadAsync();
    }

    /// <summary>Naming a day IS choosing the specific-day period - so stepping to yesterday or
    /// picking a date off the calendar switches the period across for you rather than setting a
    /// date the current period has no use for. Without this the stepper moved BusinessDate while
    /// ResolveRange went on answering "today", and the screen sat there showing the same figures
    /// for a date it was no longer reporting on.</summary>
    private void ShowDay(DateOnly day)
    {
        var specificDay = Periods.First(p => p.Key == "day");
        if (!ReferenceEquals(SelectedPeriod, specificDay))
        {
            _suppressPeriodReload = true;
            try { SelectedPeriod = specificDay; }
            finally { _suppressPeriodReload = false; }
        }

        BusinessDate = day;
        OnPropertyChanged(nameof(BusinessDateOffset));
        OnPropertyChanged(nameof(RangeLabel));
        _ = LoadAsync();
    }

    /// <summary>Cairo business dates, the same boundary the dashboard and every daily report use,
    /// so a month here contains exactly the days those reports show.</summary>
    private (DateOnly From, DateOnly To) ResolveRange()
    {
        var today = Falah.Domain.Common.BusinessClock.Today();
        return SelectedPeriod?.Key switch
        {
            "yesterday" => (today.AddDays(-1), today.AddDays(-1)),
            "last7" => (today.AddDays(-6), today),
            "last30" => (today.AddDays(-29), today),
            "month" => (new DateOnly(today.Year, today.Month, 1), today),
            "day" => (BusinessDate, BusinessDate),
            _ => (today, today),
        };
    }

    public string RangeLabel
    {
        get
        {
            var (from, to) = ResolveRange();
            return from == to ? from.ToString("yyyy-MM-dd") : $"{from:yyyy-MM-dd} ← {to:yyyy-MM-dd}";
        }
    }

    [ObservableProperty] private SalesReportDto? _sales;
    [ObservableProperty] private long _expensesTotalPiastres;
    [ObservableProperty] private DeliveryOutcomesReportDto? _delivery;

    [ObservableProperty] private TaxReportDto? _tax;
    [ObservableProperty] private FoodCostReportDto? _foodCost;

    public ObservableCollection<TopSellingItemDto> TopItems { get; } = new();

    /// <summary>One product's row in the product x hour table: a quantity per column of
    /// <see cref="ActiveHours"/>, in the same order, plus the hour it sells hardest.</summary>
    public sealed record ProductHourRow(string ProductName, int Total, string PeakLabel, IReadOnlyList<HourCell> Cells);
    /// <summary>Display, not Quantity, because an hour that sold nothing reads better as a dot than
    /// as a 0 - in a grid this dense a column of zeros is noise the eye has to filter past to find
    /// the numbers that matter.</summary>
    public sealed record HourCell(string Label, int Quantity, string Display, bool IsPeak);

    /// <summary>Only the hours that actually sold something, across the whole period - a kitchen
    /// open from noon to two in the morning should not read fourteen empty columns to find them.
    /// Ordered by the hour on a Cairo clock.</summary>
    public ObservableCollection<string> ActiveHours { get; } = new();
    public ObservableCollection<ProductHourRow> ProductHours { get; } = new();
    public bool HasProductHours => ProductHours.Count > 0;

    /// <summary>Rows are capped: the table is for reading a pattern off, and every product the
    /// restaurant has ever sold is a spreadsheet, not a pattern. The full per-product totals are
    /// already in the Top Selling panel above.</summary>
    private const int MaxProductHourRows = 15;
    public ObservableCollection<CategorySalesDto> CategorySales { get; } = new();
    public ObservableCollection<RefundActivityDto> Refunds { get; } = new();
    public ObservableCollection<SignificantActionDto> SignificantActions { get; } = new();

    public bool HasRefunds => Refunds.Count > 0;
    public bool HasSignificantActions => SignificantActions.Count > 0;

    /// <summary>Food cost is only meaningful when every sold line had a known cost. The server
    /// reports which ones it could not price, and that stays visible rather than being averaged
    /// away - an unknown cost is not a zero cost.</summary>
    public bool HasIncompleteFoodCost => FoodCost is { } fc && fc.KnownLineCount < fc.SoldLineCount;

    /// <summary>The limitation, said out loud on the screen that shows the number.
    ///
    /// Usage movements carry no unit cost of their own, so a past day's food cost is worked out
    /// from TODAY's recipes at TODAY's ingredient prices. Re-open last week's report after a price
    /// rise and the figure will have moved. Whether this report should instead freeze the cost at
    /// the moment of sale is a decision about what the restaurant wants the number to mean, and
    /// nobody has made it - so the number is labelled rather than quietly redefined.</summary>
    public string FoodCostBasisNote =>
        FoodCost is null ? "" : "محسوبة بأسعار المكوّنات الحالية ووصفاتها الحالية، مش بأسعار وقت البيع.";

    public bool HasFoodCostBasisNote => FoodCostBasisNote.Length > 0;

    public string FoodCostCoverageLabel => FoodCost is { } fc
        ? $"{fc.KnownLineCount} من {fc.SoldLineCount} سطر مبيعات له تكلفة معروفة"
        : "";

    partial void OnFoodCostChanged(FoodCostReportDto? value)
    {
        OnPropertyChanged(nameof(HasIncompleteFoodCost));
        OnPropertyChanged(nameof(FoodCostCoverageLabel));
        OnPropertyChanged(nameof(FoodCostBasisNote));
        OnPropertyChanged(nameof(HasFoodCostBasisNote));
    }

    public bool HasData => Sales is not null && !IsLoading && LoadError is null;
    private bool _reloadRequested;

    /// <summary>DatePicker binds a DateTimeOffset, not a DateOnly.</summary>
    public DateTimeOffset BusinessDateOffset
    {
        get => new(BusinessDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        set
        {
            var day = DateOnly.FromDateTime(value.Date);
            if (day == BusinessDate) return;
            OnPropertyChanged();
            ShowDay(day);
        }
    }

    public string BusinessDateLabel => BusinessDate.ToString("yyyy-MM-dd");

    partial void OnBusinessDateChanged(DateOnly value) => OnPropertyChanged(nameof(BusinessDateLabel));

    /// <summary>Turns the server's sparse (product, hour) rows into the table the screen draws: one
    /// column per hour that sold anything at all, one row per product, zero where a product sold
    /// nothing in an hour that others did. The server deliberately omits those zeros - filling them
    /// is a presentation decision, and it belongs here.</summary>
    private void BuildProductHours(IReadOnlyList<ProductHourlySalesDto> rows)
    {
        ActiveHours.Clear();
        ProductHours.Clear();

        var hours = rows.Select(r => r.Hour).Distinct().OrderBy(h => h).ToList();
        foreach (var h in hours) ActiveHours.Add($"{h:00}");

        var byProduct = rows
            .GroupBy(r => r.ProductName)
            .Select(g => new
            {
                Name = g.Key,
                Total = g.Sum(r => r.Quantity),
                Peak = g.OrderByDescending(r => r.Quantity).ThenBy(r => r.Hour).First(),
                ByHour = g.ToDictionary(r => r.Hour, r => r.Quantity),
            })
            .OrderByDescending(p => p.Total)
            .ThenBy(p => p.Name)
            .Take(MaxProductHourRows);

        foreach (var p in byProduct)
        {
            var cells = hours
                .Select(h =>
                {
                    var q = p.ByHour.TryGetValue(h, out var found) ? found : 0;
                    return new HourCell($"{h:00}", q, q == 0 ? "·" : q.ToString(), q > 0 && h == p.Peak.Hour);
                })
                .ToList();
            ProductHours.Add(new ProductHourRow(p.Name, p.Total, $"{p.Peak.Hour:00}:00", cells));
        }

        OnPropertyChanged(nameof(HasProductHours));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) { _reloadRequested = true; return; }
        IsLoading = true;
        LoadError = null;
        OnPropertyChanged(nameof(HasData));
        var (from, to) = ResolveRange();
        OnPropertyChanged(nameof(RangeLabel));
        try
        {
            var actor = _session.CurrentUser!.Id;

            var sales = await _session.Api.GetSalesReportAsync(actor, from, to);
            if (!sales.Success || sales.Value is null)
            {
                LoadError = sales.Error ?? "تعذر تحميل التقارير.";
                Sales = null;
                Tax = null;
                FoodCost = null;
                Refunds.Clear();
                SignificantActions.Clear();
                OnPropertyChanged(nameof(HasRefunds));
                OnPropertyChanged(nameof(HasSignificantActions));
                OnPropertyChanged(nameof(HasData));
                return;
            }
            Sales = sales.Value;

            var expenses = await _session.Api.GetExpenseReportAsync(actor, from, to);
            if (!expenses.Success || expenses.Value is null) { LoadError = expenses.Error ?? "تعذر تحميل المصروفات."; return; }
            ExpensesTotalPiastres = expenses.Value.TotalPiastres;

            var top = await _session.Api.GetTopItemsAsync(actor, from, to);
            if (!top.Success || top.Value is null) { LoadError = top.Error ?? "تعذر تحميل الأصناف الأكثر مبيعًا."; return; }
            TopItems.Clear();
            if (top.Success && top.Value is not null)
                foreach (var t in top.Value) TopItems.Add(t);

            var hourly = await _session.Api.GetProductHourlySalesAsync(actor, from, to);
            if (!hourly.Success || hourly.Value is null) { LoadError = hourly.Error ?? "تعذر تحميل مبيعات الأصناف بالساعة."; return; }
            BuildProductHours(hourly.Value);

            var cats = await _session.Api.GetCategorySalesAsync(actor, from, to);
            if (!cats.Success || cats.Value is null) { LoadError = cats.Error ?? "تعذر تحميل مبيعات الأقسام."; return; }
            CategorySales.Clear();
            if (cats.Success && cats.Value is not null)
                foreach (var c in cats.Value) CategorySales.Add(c);

            var delivery = await _session.Api.GetDeliveryOutcomesAsync(actor, from, to);
            if (!delivery.Success || delivery.Value is null) { LoadError = delivery.Error ?? "تعذر تحميل نتائج التوصيل."; return; }
            Delivery = delivery.Value;

            var tax = await _session.Api.GetTaxReportAsync(actor, from, to);
            if (!tax.Success || tax.Value is null) { LoadError = tax.Error ?? "تعذر تحميل تقرير الضريبة."; return; }
            Tax = tax.Value;

            var refunds = await _session.Api.GetRefundActivityAsync(actor, from, to);
            if (!refunds.Success || refunds.Value is null) { LoadError = refunds.Error ?? "تعذر تحميل المرتجعات."; return; }
            Refunds.Clear();
            foreach (var r in refunds.Value) Refunds.Add(r);
            OnPropertyChanged(nameof(HasRefunds));

            var foodCost = await _session.Api.GetFoodCostReportAsync(actor, from, to);
            if (!foodCost.Success || foodCost.Value is null) { LoadError = foodCost.Error ?? "تعذر تحميل تكلفة الطعام."; return; }
            FoodCost = foodCost.Value;

            var actions = await _session.Api.GetSignificantActionsAsync(actor, from, to);
            if (!actions.Success || actions.Value is null) { LoadError = actions.Error ?? "تعذر تحميل الإجراءات المهمة."; return; }
            SignificantActions.Clear();
            foreach (var a in actions.Value) SignificantActions.Add(a);
            OnPropertyChanged(nameof(HasSignificantActions));
        }
        finally
        {
            IsLoading = false;
            if (_reloadRequested)
            {
                _reloadRequested = false;
                await LoadAsync();
            }
            OnPropertyChanged(nameof(HasData));
        }
    }

    [RelayCommand]
    private void PreviousDay() => ShowDay(BusinessDate.AddDays(-1));

    [RelayCommand]
    private void NextDay() => ShowDay(BusinessDate.AddDays(1));
}

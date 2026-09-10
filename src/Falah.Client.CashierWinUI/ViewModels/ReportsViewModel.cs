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
    }

    [ObservableProperty] private DateOnly _businessDate;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;

    [ObservableProperty] private SalesReportDto? _sales;
    [ObservableProperty] private long _expensesTotalPiastres;
    [ObservableProperty] private DeliveryOutcomesReportDto? _delivery;

    [ObservableProperty] private TaxReportDto? _tax;
    [ObservableProperty] private FoodCostReportDto? _foodCost;

    public ObservableCollection<TopSellingItemDto> TopItems { get; } = new();
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
            BusinessDate = day;
            OnPropertyChanged();
            _ = LoadAsync();
        }
    }

    public string BusinessDateLabel => BusinessDate.ToString("yyyy-MM-dd");

    partial void OnBusinessDateChanged(DateOnly value) => OnPropertyChanged(nameof(BusinessDateLabel));

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) { _reloadRequested = true; return; }
        IsLoading = true;
        LoadError = null;
        OnPropertyChanged(nameof(HasData));
        var date = BusinessDate;
        try
        {
            var actor = _session.CurrentUser!.Id;

            var sales = await _session.Api.GetSalesReportAsync(actor, date);
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

            var expenses = await _session.Api.GetExpenseReportAsync(actor, date);
            if (!expenses.Success || expenses.Value is null) { LoadError = expenses.Error ?? "تعذر تحميل المصروفات."; return; }
            ExpensesTotalPiastres = expenses.Value.TotalPiastres;

            var top = await _session.Api.GetTopItemsAsync(actor, date, date);
            if (!top.Success || top.Value is null) { LoadError = top.Error ?? "تعذر تحميل الأصناف الأكثر مبيعًا."; return; }
            TopItems.Clear();
            if (top.Success && top.Value is not null)
                foreach (var t in top.Value) TopItems.Add(t);

            var cats = await _session.Api.GetCategorySalesAsync(actor, date, date);
            if (!cats.Success || cats.Value is null) { LoadError = cats.Error ?? "تعذر تحميل مبيعات الأقسام."; return; }
            CategorySales.Clear();
            if (cats.Success && cats.Value is not null)
                foreach (var c in cats.Value) CategorySales.Add(c);

            var delivery = await _session.Api.GetDeliveryOutcomesAsync(actor, date, date);
            if (!delivery.Success || delivery.Value is null) { LoadError = delivery.Error ?? "تعذر تحميل نتائج التوصيل."; return; }
            Delivery = delivery.Value;

            var tax = await _session.Api.GetTaxReportAsync(actor, date);
            if (!tax.Success || tax.Value is null) { LoadError = tax.Error ?? "تعذر تحميل تقرير الضريبة."; return; }
            Tax = tax.Value;

            var refunds = await _session.Api.GetRefundActivityAsync(actor, date);
            if (!refunds.Success || refunds.Value is null) { LoadError = refunds.Error ?? "تعذر تحميل المرتجعات."; return; }
            Refunds.Clear();
            foreach (var r in refunds.Value) Refunds.Add(r);
            OnPropertyChanged(nameof(HasRefunds));

            var foodCost = await _session.Api.GetFoodCostReportAsync(actor, date, date);
            if (!foodCost.Success || foodCost.Value is null) { LoadError = foodCost.Error ?? "تعذر تحميل تكلفة الطعام."; return; }
            FoodCost = foodCost.Value;

            var actions = await _session.Api.GetSignificantActionsAsync(actor, date, date);
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
    private void PreviousDay() { BusinessDate = BusinessDate.AddDays(-1); OnPropertyChanged(nameof(BusinessDateOffset)); _ = LoadAsync(); }

    [RelayCommand]
    private void NextDay() { BusinessDate = BusinessDate.AddDays(1); OnPropertyChanged(nameof(BusinessDateOffset)); _ = LoadAsync(); }
}

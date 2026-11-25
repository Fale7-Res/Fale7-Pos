using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Delivery;
using Falah.Client.CashierWinUI.Services;
using Falah.Domain.Delivery;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>One statement row as the screen shows it - the server's row with display strings.</summary>
public sealed record DriverStatementRow(DriverStatementOrder Order)
{
    private static string Egp(long p) => CashInput.Display(p) + " ج";
    public string Number => Order.OrderNumber;
    public string Time => ShiftsViewModel.FormatCairo(Order.OrderedAtUtc)[^5..];
    public string Customer => string.IsNullOrWhiteSpace(Order.CustomerName) ? Order.CustomerPhone ?? "" : Order.CustomerName;
    // Phone and address share the caption line, so a long name never pushes the phone onto a line of its own.
    public string Address => string.Join(" • ", new[] { string.IsNullOrWhiteSpace(Order.CustomerName) ? null : Order.CustomerPhone, Order.Address }
        .Where(s => !string.IsNullOrWhiteSpace(s)));
    public string Payment => DriverStatementViewModel.FilterLabel(Order.PaymentMethod);
    public string Food => Egp(Order.FoodSalesPiastres);
    public string Fee => Egp(Order.DeliveryFeePiastres);
    public string Total => Egp(Order.OrderTotalPiastres);
    public string ToCollect => Egp(Order.CashToCollectPiastres);
    public string Collected => Egp(Order.CollectedPiastres) + (Order.CustodySettled ? " (مُسوّى)" : "");
    public string Notes => string.Join(" • ", new[]
    {
        Order.Cancelled ? "ملغى - خارج المجاميع" : null,
        Order.DeliveryStatus is "Failed" ? "فشل التوصيل" : Order.DeliveryStatus is "Returned" ? "مرتجع" : null,
        Order.RefundedPiastres > 0 ? $"مرتجع {Egp(Order.RefundedPiastres)}" : null,
        Order.ReassignedFromName is { } from ? $"من {from}" : null,
    }.Where(s => s is not null));
}

/// <summary>
/// UI-REQ-001: a delivery driver's daily statement - pick the driver, the business day and the
/// payment method; see the orders and the summary; print the summary or the detailed paper. Food sales,
/// delivery charges, totals, prepaid, to-collect, collected and settled stay separate, exactly as the
/// server computes them; the screen adds no figures of its own.
/// </summary>
public partial class DriverStatementViewModel : ObservableObject
{
    private readonly AppSession _session;
    public DriverStatementViewModel(AppSession session) => _session = session;

    public sealed record FilterOption(string Value, string Label);
    public IReadOnlyList<FilterOption> Filters { get; } = DriverStatementRules.PaymentFilters.Select(f => new FilterOption(f, FilterLabel(f))).ToList();

    internal static string FilterLabel(string? f) => f switch
    {
        "cash" => "كاش", "card" => "بطاقة", "instapay" => "إنستاباي", "wallet" => "محفظة", "multi" => "دفع مقسّم", _ => "كل طرق الدفع",
    };

    public ObservableCollection<DriverDto> Drivers { get; } = new();
    public ObservableCollection<DriverStatementRow> Rows { get; } = new();

    [ObservableProperty] private bool _isOpen;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string? _printMessage;
    [ObservableProperty] private DriverDto? _selectedDriver;
    [ObservableProperty] private DateTimeOffset _businessDate = DateTimeOffset.Now;
    [ObservableProperty] private FilterOption? _selectedFilter;
    [ObservableProperty] private DriverStatementTotals? _totals;

    public bool HasRows => Rows.Count > 0;
    public bool IsEmpty => !IsLoading && Totals is not null && Rows.Count == 0;
    private static string Egp(long p) => CashInput.Display(p) + " ج";
    public string InvoiceCount => Totals?.InvoiceCount.ToString() ?? "—";
    public string FoodSales => Totals is null ? "—" : Egp(Totals.FoodSalesPiastres);
    public string DeliveryFees => Totals is null ? "—" : Egp(Totals.DeliveryFeesPiastres);
    public string OrderTotal => Totals is null ? "—" : Egp(Totals.OrderTotalPiastres);
    public string Prepaid => Totals is null ? "—" : Egp(Totals.PrepaidPiastres);
    public string ToCollect => Totals is null ? "—" : Egp(Totals.CashToCollectPiastres);
    public string Collected => Totals is null ? "—" : Egp(Totals.CollectedPiastres);
    public string Unsettled => Totals is null ? "—" : Egp(Totals.CollectedUnsettledPiastres);
    public string Refunded => Totals is null ? "—" : Egp(Totals.RefundedPiastres);
    public string DriverFees => Totals is null ? "—" : Egp(Totals.DriverFeesEarnedPiastres);
    public string NetAfterFeesLabel => Totals is { NetAfterFeesPiastres: < 0 } ? "صافي مستحق للسائق" : "صافي على السائق بعد أجره";
    public string NetAfterFees => Totals is null ? "—" : Egp(Math.Abs(Totals.NetAfterFeesPiastres));
    public string Footnote => Totals is null ? "" :
        $"ملغاة خارج المجاميع: {Totals.CancelledCount} • توصيل فشل/مرتجع: {Totals.FailedOrReturnedCount} • خدمة التوصيل كاملة على كل طلب تم توصيله هي أجر السائق، وتُخصم من النقدية التي يسلّمها. التوصيل الفاشل أو المرتجع بلا أجر.";

    partial void OnTotalsChanged(DriverStatementTotals? value)
    {
        foreach (var p in new[] { nameof(InvoiceCount), nameof(FoodSales), nameof(DeliveryFees), nameof(OrderTotal), nameof(Prepaid),
                     nameof(ToCollect), nameof(Collected), nameof(Unsettled), nameof(Refunded), nameof(DriverFees), nameof(NetAfterFeesLabel), nameof(NetAfterFees), nameof(Footnote), nameof(IsEmpty), nameof(HasRows) })
            OnPropertyChanged(p);
    }

    public async Task OpenAsync(DriverDto? driver = null)
    {
        IsOpen = true;
        Error = null;
        PrintMessage = null;
        SelectedFilter ??= Filters[0];
        if (Drivers.Count == 0)
        {
            var drivers = await _session.Api.GetAllDriversAsync();
            if (drivers.Success && drivers.Value is not null) foreach (var d in drivers.Value) Drivers.Add(d);
            else Error = drivers.Error ?? "تعذر تحميل السائقين.";
        }
        SelectedDriver = driver is null ? SelectedDriver ?? Drivers.FirstOrDefault() : Drivers.FirstOrDefault(d => d.Id == driver.Id) ?? SelectedDriver;
        await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (SelectedDriver is null) { Error = "اختر السائق."; return; }
        IsLoading = true;
        Error = null;
        try
        {
            var result = await _session.Api.GetDriverDailyStatementAsync(SelectedDriver.Id, DateOnly.FromDateTime(BusinessDate.Date), SelectedFilter?.Value);
            Rows.Clear();
            if (!result.Success || result.Value is null)
            {
                Totals = null;
                Error = result.Error ?? "تعذر تحميل كشف السائق.";
                return;
            }
            foreach (var o in result.Value.Orders) Rows.Add(new DriverStatementRow(o));
            Totals = result.Value.Totals;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(HasRows));
        }
    }

    [RelayCommand] private Task PrintSummaryAsync() => PrintAsync(detailed: false);
    [RelayCommand] private Task PrintDetailAsync() => PrintAsync(detailed: true);

    private async Task PrintAsync(bool detailed)
    {
        if (SelectedDriver is null) { Error = "اختر السائق."; return; }
        PrintMessage = null;
        var result = await _session.Api.PrintDriverDailyStatementAsync(SelectedDriver.Id, new PrintDriverDailyStatementRequest(
            $"dsp-{Guid.NewGuid():N}", DateOnly.FromDateTime(BusinessDate.Date), SelectedFilter?.Value, detailed));
        PrintMessage = result.Success
            ? (detailed ? "أُرسل الكشف التفصيلي للطابعة." : "أُرسل ملخص الكشف للطابعة.")
            : result.Error ?? "تعذر إرسال الكشف للطباعة.";
    }

    [RelayCommand] private void Close() => IsOpen = false;
}

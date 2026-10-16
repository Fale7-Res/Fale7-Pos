using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Delivery;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Driver statistics, shown inside the driver-account dialog that already exists rather than behind
/// a new nav entry - it is the same subject (one driver), and a dispatcher settling a driver is
/// exactly the person who wants to see how that driver has been doing. Every figure is read straight
/// from /api/delivery/drivers/{id}/statistics; this class computes no metric of its own, so the
/// panel can never disagree with the report the server derives from the same delivery/custody/
/// settlement rows.
/// </summary>
public partial class DeliveryViewModel
{
    public sealed record StatsPeriodOption(string Key, string Label);

    public IReadOnlyList<StatsPeriodOption> StatsPeriods { get; } = new[]
    {
        new StatsPeriodOption("today", "اليوم"),
        new StatsPeriodOption("last7", "آخر ٧ أيام"),
        new StatsPeriodOption("month", "هذا الشهر"),
        new StatsPeriodOption("custom", "فترة مخصصة"),
    };

    [ObservableProperty] private StatsPeriodOption? _selectedStatsPeriod;
    [ObservableProperty] private DriverStatisticsDto? _driverStatistics;
    [ObservableProperty] private bool _isLoadingStatistics;
    [ObservableProperty] private string? _statisticsError;

    /// <summary>Only meaningful for the custom period; both default to today so the pickers open on
    /// a valid, non-empty range rather than on 0001-01-01.</summary>
    [ObservableProperty] private DateTimeOffset _statsFromDate = DateTimeOffset.Now;
    [ObservableProperty] private DateTimeOffset _statsToDate = DateTimeOffset.Now;

    public bool IsCustomStatsPeriod => SelectedStatsPeriod?.Key == "custom";
    public bool HasStatistics => DriverStatistics is not null;

    partial void OnSelectedStatsPeriodChanged(StatsPeriodOption? value)
    {
        OnPropertyChanged(nameof(IsCustomStatsPeriod));
        if (AccountDriver is { } driver) _ = LoadDriverStatisticsAsync(driver.Id);
    }

    partial void OnDriverStatisticsChanged(DriverStatisticsDto? value)
    {
        OnPropertyChanged(nameof(HasStatistics));
        OnPropertyChanged(nameof(AverageDeliveryMinutesDisplay));
        OnPropertyChanged(nameof(LastOrderDisplay));
        OnPropertyChanged(nameof(LastSettlementDisplay));
    }

    partial void OnStatsFromDateChanged(DateTimeOffset value) { if (IsCustomStatsPeriod) ReloadStatisticsForCurrentDriver(); }
    partial void OnStatsToDateChanged(DateTimeOffset value) { if (IsCustomStatsPeriod) ReloadStatisticsForCurrentDriver(); }

    private void ReloadStatisticsForCurrentDriver()
    {
        if (AccountDriver is { } driver) _ = LoadDriverStatisticsAsync(driver.Id);
    }

    /// <summary>Null (never zero) when no delivery in the period had both timestamps - "not known"
    /// and "instant" are different statements, and the server already distinguishes them.</summary>
    public string AverageDeliveryMinutesDisplay =>
        DriverStatistics?.AverageDeliveryMinutes is { } minutes ? $"{minutes} دقيقة" : "—";

    public string LastOrderDisplay => Format(DriverStatistics?.LastOrderAtUtc);
    public string LastSettlementDisplay => Format(DriverStatistics?.LastSettlementAtUtc);

    private static string Format(DateTimeOffset? instant) => instant is { } value
        ? TimeZoneInfo.ConvertTime(value, Falah.Domain.Common.BusinessClock.CairoTimeZone).ToString("yyyy-MM-dd HH:mm")
        : "—";

    private (DateTimeOffset From, DateTimeOffset To) ResolveStatsRange()
    {
        var today = Falah.Domain.Common.BusinessClock.Today();
        var (from, to) = SelectedStatsPeriod?.Key switch
        {
            "last7" => (today.AddDays(-6), today),
            "month" => (new DateOnly(today.Year, today.Month, 1), today),
            "custom" => (DateOnly.FromDateTime(StatsFromDate.Date), DateOnly.FromDateTime(StatsToDate.Date)),
            _ => (today, today),
        };
        if (to < from) (from, to) = (to, from);
        // Cairo business-day boundaries, the same range every other report in this app uses.
        return (Falah.Domain.Common.BusinessClock.UtcRangeForBusinessDate(from).Start,
                Falah.Domain.Common.BusinessClock.UtcRangeForBusinessDate(to).End);
    }

    private int _statisticsVersion;

    private async Task LoadDriverStatisticsAsync(string driverId)
    {
        var version = ++_statisticsVersion;
        IsLoadingStatistics = true;
        StatisticsError = null;
        try
        {
            var (from, to) = ResolveStatsRange();
            var result = await _session.Api.GetDriverStatisticsAsync(driverId, from, to);
            if (version != _statisticsVersion) return;
            if (!result.Success || result.Value is null)
            {
                StatisticsError = result.Error ?? "تعذر تحميل إحصائيات السائق.";
                DriverStatistics = null;
                return;
            }
            DriverStatistics = result.Value;
        }
        finally { if (version == _statisticsVersion) IsLoadingStatistics = false; }
    }

    [RelayCommand]
    private async Task RefreshDriverStatisticsAsync()
    {
        if (AccountDriver is { } driver) await LoadDriverStatisticsAsync(driver.Id);
    }
}

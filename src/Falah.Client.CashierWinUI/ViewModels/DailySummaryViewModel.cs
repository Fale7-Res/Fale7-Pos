using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Reports;
using Falah.Application.Settings;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Ported from DailySummaryScreen.tsx - a single printable end-of-day sheet for the current (or, if
/// none is open, the most recent) business day: sales by payment method and order type, cash/drawer
/// movements, employee wages/advances, and a signature block naming whoever actually worked that day.
///
/// Business-parity adaptation: the reference splits sales into صالة/تيك أواي/دليفري (dine-in/takeaway/
/// delivery). This system has no dine-in orders (see OrderType's own doc comment), so the sheet shows
/// the two order types that actually exist here - Pickup and Takeaway - plus Delivery, never a
/// resurrected صالة bucket.
///
/// No print pipeline exists yet for management reports (only order receipts/kitchen tickets have one -
/// see PrintRoute) - a real gap, not faked here with a button that would do nothing.
/// </summary>
public partial class DailySummaryViewModel : ObservableObject
{
    private readonly AppSession _session;

    public DailySummaryViewModel(AppSession session)
    {
        _session = session;
    }

    [ObservableProperty] private DailySummaryDto? _summary;
    [ObservableProperty] private SettingsDto? _settings;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;

    public bool HasSummary => Summary is not null && !IsLoading && LoadError is null;
    public bool HasNoBusinessDay => !IsLoading && LoadError is null && Summary is null;
    public bool CanSeeSummary => _session.HasPermission("reports_view");

    public string BusinessDateDisplay => Summary is null ? "" : FormatArabicLongDate(Summary.BusinessDate);

    private static string FormatArabicLongDate(string businessDate)
    {
        if (!DateOnly.TryParseExact(businessDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return businessDate;
        var arabic = CultureInfo.GetCultureInfo("ar-EG");
        return date.ToDateTime(TimeOnly.MinValue).ToString("dddd، d MMMM yyyy", arabic);
    }

    public string CashierSignatureLabel => (Summary?.CashierNames.Count ?? 0) > 1 ? "توقيع الكاشيرية" : "توقيع كاشير الوردية";
    public string CashierNamesDisplay => Summary is null || Summary.CashierNames.Count == 0 ? "" : string.Join("، ", Summary.CashierNames);
    public bool HasCashierNames => (Summary?.CashierNames.Count ?? 0) > 0;

    /// <summary>The reference's single differencePiastres-style card has no direct equivalent for
    /// shift cash - this app reports shortage/overage separately (see ShiftsViewModel's own history
    /// table), so the sheet nets them the same way Control Center's reconciliation card does.</summary>
    public long NetShiftDifferencePiastres => (Summary?.ShiftsOveragePiastres ?? 0) - (Summary?.ShiftsShortagePiastres ?? 0);

    public long AdvancesAndWithdrawalsPiastres =>
        (Summary?.EmployeeLedger.AdvancePiastres ?? 0) + (Summary?.EmployeeLedger.WithdrawalPiastres ?? 0);

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        OnPropertyChanged(nameof(HasSummary));
        OnPropertyChanged(nameof(HasNoBusinessDay));
        try
        {
            var settings = await _session.Api.GetSettingsAsync();
            if (settings.Success) Settings = settings.Value;

            var current = await _session.Api.GetCurrentBusinessDayAsync();
            var businessDayId = current.Success ? current.Value?.Id : null;
            if (businessDayId is null)
            {
                var history = await _session.Api.GetBusinessDaysAsync();
                if (!history.Success || history.Value is null || history.Value.Count == 0)
                {
                    // No business day has ever been opened - a legitimate empty state, not an error.
                    Summary = null;
                    if (!history.Success) LoadError = history.Error ?? "تعذر تحميل يوم العمل.";
                    return;
                }
                businessDayId = history.Value[0].Id;
            }

            var summary = await _session.Api.GetDailySummaryAsync(businessDayId, _session.CurrentUser!.Id);
            if (!summary.Success || summary.Value is null)
            {
                LoadError = summary.Error ?? "تعذر تحميل ملخص اليوم.";
                return;
            }
            Summary = summary.Value;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasSummary));
            OnPropertyChanged(nameof(HasNoBusinessDay));
            OnPropertyChanged(nameof(BusinessDateDisplay));
            OnPropertyChanged(nameof(CashierSignatureLabel));
            OnPropertyChanged(nameof(CashierNamesDisplay));
            OnPropertyChanged(nameof(HasCashierNames));
            OnPropertyChanged(nameof(NetShiftDifferencePiastres));
            OnPropertyChanged(nameof(AdvancesAndWithdrawalsPiastres));
        }
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Employees;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// The employee financial report - the reporting half of the final business rule, now that payment
/// is no longer tied to a payroll period. Opens over the roster it belongs to rather than as its own
/// nav entry: it answers a question about the same people the roster lists.
///
/// Every figure comes from /api/employees/financial-report. Advances stay in their own column,
/// never folded into payments, and RemainingBalance is the all-time Employee Ledger balance (not a
/// range subtotal) because that is the authoritative "what do we still owe them" figure - the same
/// number the roster and the ledger drill-down show.
/// </summary>
public partial class EmployeesViewModel
{
    public sealed record ReportPeriodOption(string Key, string Label);

    public IReadOnlyList<ReportPeriodOption> ReportPeriods { get; } = new[]
    {
        new ReportPeriodOption("today", "اليوم"),
        new ReportPeriodOption("last7", "آخر ٧ أيام"),
        new ReportPeriodOption("last10", "آخر ١٠ أيام"),
        new ReportPeriodOption("month", "هذا الشهر"),
        new ReportPeriodOption("custom", "فترة مخصصة"),
    };

    [ObservableProperty] private bool _isReportOpen;
    [ObservableProperty] private ReportPeriodOption? _selectedReportPeriod;
    [ObservableProperty] private bool _isLoadingReport;
    [ObservableProperty] private string? _reportError;
    [ObservableProperty] private DateTimeOffset _reportFromDate = DateTimeOffset.Now;
    [ObservableProperty] private DateTimeOffset _reportToDate = DateTimeOffset.Now;

    public ObservableCollection<EmployeeFinancialReportLineDto> ReportLines { get; } = new();
    public bool IsCustomReportPeriod => SelectedReportPeriod?.Key == "custom";
    public bool HasNoReportLines => IsReportOpen && !IsLoadingReport && ReportError is null && ReportLines.Count == 0;

    /// <summary>Same gate as every other figure-bearing screen in this app: the report is management
    /// information, so it needs reports_view rather than the ledger-write permission.</summary>
    public bool CanSeeReport => _session.HasPermission("reports_view");

    public string ReportRangeLabel
    {
        get
        {
            var (from, to) = ResolveReportRange();
            return from == to ? from.ToString("yyyy-MM-dd") : $"{from:yyyy-MM-dd} ← {to:yyyy-MM-dd}";
        }
    }

    partial void OnSelectedReportPeriodChanged(ReportPeriodOption? value)
    {
        OnPropertyChanged(nameof(IsCustomReportPeriod));
        OnPropertyChanged(nameof(ReportRangeLabel));
        if (IsReportOpen) _ = LoadReportAsync();
    }

    partial void OnReportFromDateChanged(DateTimeOffset value) { if (IsCustomReportPeriod && IsReportOpen) _ = LoadReportAsync(); }
    partial void OnReportToDateChanged(DateTimeOffset value) { if (IsCustomReportPeriod && IsReportOpen) _ = LoadReportAsync(); }

    private (DateOnly From, DateOnly To) ResolveReportRange()
    {
        var today = Falah.Domain.Common.BusinessClock.Today();
        var (from, to) = SelectedReportPeriod?.Key switch
        {
            "last7" => (today.AddDays(-6), today),
            "last10" => (today.AddDays(-9), today),
            "month" => (new DateOnly(today.Year, today.Month, 1), today),
            "custom" => (DateOnly.FromDateTime(ReportFromDate.Date), DateOnly.FromDateTime(ReportToDate.Date)),
            _ => (today, today),
        };
        return to < from ? (to, from) : (from, to);
    }

    [RelayCommand]
    private async Task OpenReportAsync()
    {
        if (!CanEditAction || !CanSeeReport) return;
        SelectedReportPeriod ??= ReportPeriods[0];
        IsReportOpen = true;
        await LoadReportAsync();
    }

    [RelayCommand]
    private void CloseReport()
    {
        _reportVersion++;
        IsReportOpen = false;
        IsLoadingReport = false;
        ReportLines.Clear();
        ReportError = null;
    }

    private int _reportVersion;

    [RelayCommand]
    private async Task LoadReportAsync()
    {
        var version = ++_reportVersion;
        IsLoadingReport = true;
        ReportError = null;
        OnPropertyChanged(nameof(HasNoReportLines));
        OnPropertyChanged(nameof(ReportRangeLabel));
        try
        {
            var (from, to) = ResolveReportRange();
            var result = await _session.Api.GetEmployeeFinancialReportAsync(_session.CurrentUser!.Id, from, to);
            if (version != _reportVersion) return;
            if (!result.Success || result.Value is null)
            {
                ReportError = result.Error ?? "تعذر تحميل التقرير المالي للموظفين.";
                ReportLines.Clear();
                return;
            }
            ReportLines.Clear();
            foreach (var line in result.Value) ReportLines.Add(line);
        }
        finally
        {
            if (version == _reportVersion)
            {
                IsLoadingReport = false;
                OnPropertyChanged(nameof(HasNoReportLines));
            }
        }
    }
}

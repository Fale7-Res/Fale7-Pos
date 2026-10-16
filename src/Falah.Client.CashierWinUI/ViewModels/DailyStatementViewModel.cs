using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Printing;
using Falah.Application.Reports;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// "كشف اليوم" - the owner's single "what happened today" statement. One GET
/// (/api/reports/daily-statement), one Cairo business date at a time - Today/Yesterday/a specific
/// day picked from the same three-option pattern ReportsViewModel already uses, narrowed to a single
/// day because a daily statement, unlike a report, has no meaningful "last 7 days" version.
///
/// This screen does not replace Daily Summary (still reachable at its own nav key, unchanged) - it
/// is the fuller statement Daily Summary was never meant to be: every section the current system can
/// already answer from real data, laid out so the owner can read start-to-finish once at close
/// instead of opening Reports, Treasury, Shifts, Employees, Inventory, Delivery, Audit, Printing and
/// Backup one at a time.
/// </summary>
public partial class DailyStatementViewModel : ObservableObject
{
    private readonly AppSession _session;

    public DailyStatementViewModel(AppSession session)
    {
        _session = session;
        var cairo = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        _today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, cairo).Date);
        _selectedDate = _today;
    }

    private readonly DateOnly _today;
    [ObservableProperty] private DateOnly _selectedDate;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private DailyStatementDto? _statement;

    public bool IsToday => SelectedDate == _today;
    public bool IsYesterday => SelectedDate == _today.AddDays(-1);
    public string SelectedDateDisplay => SelectedDate.ToString("dddd، d MMMM yyyy", new System.Globalization.CultureInfo("ar-EG"));

    partial void OnSelectedDateChanged(DateOnly value)
    {
        OnPropertyChanged(nameof(IsToday));
        OnPropertyChanged(nameof(IsYesterday));
        OnPropertyChanged(nameof(SelectedDateDisplay));
        _ = LoadAsync();
    }

    [RelayCommand] private void ShowToday() => SelectedDate = _today;
    [RelayCommand] private void ShowYesterday() => SelectedDate = _today.AddDays(-1);
    [RelayCommand] private void PreviousDay() => SelectedDate = SelectedDate.AddDays(-1);
    [RelayCommand] private void NextDay() { if (SelectedDate < _today) SelectedDate = SelectedDate.AddDays(1); }
    public bool CanGoNext => SelectedDate < _today;

    // ---------------- Section rows ----------------

    public ObservableCollection<SoldProductLineDto> SoldProducts { get; } = new();
    public ObservableCollection<string> ProductCategories { get; } = new();
    [ObservableProperty] private string _categoryFilter = "الكل";
    partial void OnCategoryFilterChanged(string value) => ApplyProductFilter();

    private void ApplyProductFilter()
    {
        SoldProducts.Clear();
        if (Statement is null) return;
        var rows = CategoryFilter == "الكل"
            ? Statement.SoldProducts
            : Statement.SoldProducts.Where(p => (p.CategoryName ?? "بدون قسم") == CategoryFilter);
        foreach (var row in rows) SoldProducts.Add(row);
        OnPropertyChanged(nameof(HasSoldProducts));
    }
    public bool HasSoldProducts => SoldProducts.Count > 0;

    /// <summary>Product x hour, laid out the same way ReportsViewModel's own matrix already is: one
    /// row per product, one column per hour that actually sold something.</summary>
    public sealed class ProductHourRow
    {
        public required string ProductName { get; init; }
        public required int TotalQuantity { get; init; }
        public required int PeakHour { get; init; }
        public required IReadOnlyList<(int Hour, int Quantity)> Hours { get; init; }
        public string PeakLabel => $"الساعة {PeakHour}:00";
        public string HoursDisplay => string.Join("  ·  ", Hours.Select(h => $"{h.Hour}:00 = {h.Quantity}"));
    }
    public ObservableCollection<ProductHourRow> ProductHourly { get; } = new();

    public ObservableCollection<DailyExpenseLineDto> ExpenseLines { get; } = new();
    public bool HasExpenseLines => ExpenseLines.Count > 0;

    public ObservableCollection<Falah.Application.Reports.ShiftSummaryDto> Shifts { get; } = new();
    public bool HasShifts => Shifts.Count > 0;

    public ObservableCollection<Falah.Application.Treasury.TreasuryMovementDto> TreasuryMovements { get; } = new();
    public bool HasTreasuryMovements => TreasuryMovements.Count > 0;

    public ObservableCollection<DailyEmployeeRowDto> Employees { get; } = new();
    public bool HasEmployees => Employees.Count > 0;

    public ObservableCollection<RefundActivityDto> Refunds { get; } = new();
    public bool HasRefunds => Refunds.Count > 0;

    public ObservableCollection<DailyAuditEntryDto> AuditTrail { get; } = new();
    public bool HasAuditTrail => AuditTrail.Count > 0;

    private bool _reloadRequested;

    // ---- Reprinting an expense voucher ----

    [ObservableProperty] private string? _reprintMessage;
    [ObservableProperty] private bool _isReprinting;

    /// <summary>Queues this expense's voucher again, marked as a reprint. The expense itself - its
    /// amount, its account and the money it already moved - is untouched.</summary>
    [RelayCommand]
    private async Task ReprintExpenseAsync(DailyExpenseLineDto? line)
    {
        if (line is null || IsReprinting) return;
        IsReprinting = true;
        ReprintMessage = null;
        try
        {
            var result = await _session.Api.ReprintDocumentAsync(new ReprintDocumentRequest(
                $"rp-{Guid.NewGuid():N}", "Expense", line.Id, _session.CurrentUser!.Id, "إعادة طباعة إيصال مصروف"));
            ReprintMessage = result.Success
                ? "تم إرسال إيصال المصروف للطباعة؛ لم يتم تأكيد خروج الورقة."
                : result.Error ?? "تعذر إرسال أمر إعادة الطباعة.";
        }
        finally
        {
            IsReprinting = false;
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) { _reloadRequested = true; return; }
        _reloadRequested = false;
        IsLoading = true;
        LoadError = null;
        try
        {
            var requestedDate = SelectedDate;
            var result = await _session.Api.GetDailyStatementAsync(_session.CurrentUser!.Id, requestedDate);
            // The date changed while this was in flight: this answer describes a day nobody is
            // looking at any more. Never show it under the new date - fetch the selected one instead.
            if (requestedDate != SelectedDate) { _reloadRequested = true; return; }
            if (!result.Success || result.Value is null)
            {
                LoadError = result.Error ?? "تعذر تحميل كشف اليوم.";
                Statement = null;
                return;
            }
            Statement = result.Value;
            ApplyProductFilter();

            ProductCategories.Clear();
            ProductCategories.Add("الكل");
            foreach (var name in Statement.SoldProducts.Select(p => p.CategoryName ?? "بدون قسم").Distinct().OrderBy(n => n))
                ProductCategories.Add(name);

            ProductHourly.Clear();
            foreach (var g in Statement.ProductHourly.GroupBy(p => p.ProductName))
            {
                var hours = g.OrderBy(h => h.Hour).Select(h => (h.Hour, h.Quantity)).ToList();
                var peak = g.OrderByDescending(h => h.Quantity).First().Hour;
                ProductHourly.Add(new ProductHourRow { ProductName = g.Key, TotalQuantity = g.Sum(h => h.Quantity), PeakHour = peak, Hours = hours });
            }

            ExpenseLines.Clear();
            foreach (var e in Statement.ExpenseLines) ExpenseLines.Add(e);

            Shifts.Clear();
            foreach (var s in Statement.Shifts) Shifts.Add(s);

            TreasuryMovements.Clear();
            foreach (var m in Statement.TreasuryMovements) TreasuryMovements.Add(m);

            Employees.Clear();
            foreach (var emp in Statement.Employees) Employees.Add(emp);

            Refunds.Clear();
            foreach (var r in Statement.Refunds) Refunds.Add(r);

            AuditTrail.Clear();
            foreach (var a in Statement.AuditTrail) AuditTrail.Add(a);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasSoldProducts));
            OnPropertyChanged(nameof(HasExpenseLines));
            OnPropertyChanged(nameof(HasShifts));
            OnPropertyChanged(nameof(HasTreasuryMovements));
            OnPropertyChanged(nameof(HasEmployees));
            OnPropertyChanged(nameof(HasRefunds));
            OnPropertyChanged(nameof(HasAuditTrail));
            OnPropertyChanged(nameof(CanGoNext));
            if (_reloadRequested) await LoadAsync();
        }
    }
}

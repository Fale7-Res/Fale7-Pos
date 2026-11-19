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

    // ---- Cancelling a mistaken expense (AUD-04) ----

    /// <summary>Kept on disk until its outcome is known, like every money command (CashierViewModel.Durability).</summary>
    private readonly RetainedRequest<Falah.Application.Expenses.ReverseExpenseRequest> _reversalRequest = new("dailystatement.expense.reversal");

    [ObservableProperty] private DailyExpenseLineDto? _reversalTarget;
    [ObservableProperty] private string _reversalReason = "";
    [ObservableProperty] private string _reversalManagerPin = "";
    [ObservableProperty] private string? _reversalMessage;
    [ObservableProperty] private bool _isReversing;

    /// <summary>What happened to the money - required, never assumed (ExpenseCorrectionRules): "not_paid" (a posting
    /// error, it never left) or "cash_returned" (it left and came back). Money that left and stays gone is not reversed.</summary>
    [ObservableProperty] private string? _reversalDisposition;
    /// <summary>Blank = the whole expense; otherwise only the part that never left / came back (5000 typed, 500 paid: 4500).</summary>
    [ObservableProperty] private string _reversalAmountText = "";

    public bool IsReversalNotPaid
    {
        get => ReversalDisposition == "not_paid";
        set { if (value) ReversalDisposition = "not_paid"; }
    }

    public bool IsReversalCashReturned
    {
        get => ReversalDisposition == "cash_returned";
        set { if (value) ReversalDisposition = "cash_returned"; }
    }

    partial void OnReversalDispositionChanged(string? value)
    {
        OnPropertyChanged(nameof(IsReversalNotPaid));
        OnPropertyChanged(nameof(IsReversalCashReturned));
    }

    public bool IsReversalOpen => ReversalTarget is not null;

    /// <summary>A drawer expense is cancelled by a holder of shifts_manage or with a manager's PIN; a main-treasury
    /// one needs the owner's own permission, with no PIN fallback (the server enforces both).</summary>
    public bool ReversalNeedsManagerPin =>
        ReversalTarget is { PaymentAccountId: "tre-drawer" } && !_session.HasPermission("shifts_manage");

    partial void OnReversalTargetChanged(DailyExpenseLineDto? value)
    {
        OnPropertyChanged(nameof(IsReversalOpen));
        OnPropertyChanged(nameof(ReversalNeedsManagerPin));
    }

    [RelayCommand]
    private void OpenReversal(DailyExpenseLineDto? line)
    {
        if (line is null || IsReversing || _reversalRequest.HasPending) return;
        // A reversal line is not reversed again, and an expense is reversed once (the server refuses both too).
        if (line.AmountPiastres <= 0) return;
        if (ExpenseLines.Any(e => e.Id == $"exp-rev-{line.Id}")) { ReprintMessage = "تم إلغاء هذا المصروف بالفعل؛ قيد الإلغاء ظاهر في القائمة."; return; }
        ReversalReason = "";
        ReversalManagerPin = "";
        ReversalMessage = null;
        ReversalDisposition = null;
        ReversalAmountText = "";
        ReversalTarget = line;
    }

    [RelayCommand]
    private void CancelReversal()
    {
        if (IsReversing || _reversalRequest.HasPending) return;
        ReversalTarget = null;
    }

    [RelayCommand]
    private async Task ConfirmReversalAsync()
    {
        if (IsReversing) return;
        var pending = _reversalRequest.Value;
        if (pending is null)
        {
            if (ReversalTarget is not { } line) return;
            if (string.IsNullOrWhiteSpace(ReversalReason)) { ReversalMessage = "اكتب سبب إلغاء المصروف."; return; }
            if (ReversalDisposition is null) { ReversalMessage = "حدّد ما حدث للمبلغ: لم يُصرف أصلًا، أم صُرف ثم عاد نقدًا. إذا خرج المبلغ ولن يعود فالمصروف صحيح."; return; }
            if (!string.IsNullOrWhiteSpace(ReversalAmountText)
                && (!CashInput.TryParse(ReversalAmountText, out var part) || part <= 0 || part > line.AmountPiastres))
            { ReversalMessage = "المبلغ المُلغى يجب أن يكون أكبر من صفر ولا يتجاوز مبلغ المصروف."; return; }
            if (ReversalNeedsManagerPin && ReversalManagerPin.Length < 4) { ReversalMessage = "أدخل رمز المدير لاعتماد الإلغاء."; return; }
        }

        IsReversing = true;
        ReversalMessage = null;
        try
        {
            var request = pending;
            if (request is null)
            {
                var line = ReversalTarget!;
                string? approvalId = null, approverUserId = null;
                if (ReversalNeedsManagerPin)
                {
                    var approval = await _session.Api.IssueManagerApprovalAsync(
                        new Falah.Application.Approvals.IssueManagerApprovalRequest(ReversalManagerPin, "shifts_manage", ReversalReason));
                    if (!approval.Success || approval.Value is null)
                    {
                        ReversalMessage = approval.Error ?? "رمز المدير غير صحيح أو غير مخوّل.";
                        return;
                    }
                    approvalId = approval.Value.ApprovalId;
                    approverUserId = approval.Value.ApproverUserId;
                }
                long? partial = CashInput.TryParse(ReversalAmountText, out var p) && p > 0 ? p : null;
                request = new Falah.Application.Expenses.ReverseExpenseRequest(
                    $"rev-{Guid.NewGuid():N}", line.Id, ReversalReason.Trim(), _session.CurrentUser!.Id, approvalId, approverUserId,
                    ReversalDisposition, string.IsNullOrWhiteSpace(ReversalAmountText) ? null : partial);
            }

            var result = await _reversalRequest.SendAsync(request, r => _session.Api.ReverseExpenseAsync(r));
            if (!result.Success || result.Value is null)
            {
                ReversalMessage = _reversalRequest.FailureMessage(result.Error);
                return;
            }
            ReversalTarget = null;
            ReprintMessage = result.Value.CashDisposition == "cash_returned"
                ? $"سُجّل عودة {CashInput.Format(result.Value.AmountPiastres)} ج نقدًا إلى حساب المصروف."
                : $"سُجّل تصحيح خطأ التسجيل: {CashInput.Format(result.Value.AmountPiastres)} ج لم تُصرف وتعود لحساب المصروف.";
        }
        finally
        {
            IsReversing = false;
            ReversalManagerPin = "";
        }
        await LoadAsync();
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

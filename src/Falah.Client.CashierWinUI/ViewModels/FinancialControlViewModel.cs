using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Reports;
using Falah.Client.CashierWinUI.Services;
using Falah.Domain.Common;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// "الرقابة المالية" - where the owner checks money against the books: what each account held at the
/// start of a period, what came in and went out by kind, what it holds at the end (the figure a
/// physical count is checked against); every InstaPay / wallet payment still unconfirmed and how old
/// it is; every order voided and what became of its food; every refund, with the date of the sale
/// it reverses.
///
/// Like the dashboard, this class adds no arithmetic: each section is one server report shown as it
/// came (docs/METRIC_DEFINITIONS.md). Cash flow, voids and refunds use Cairo calendar dates; pending
/// payments are the current state, not a period - an unconfirmed transfer from last week is the one
/// that most needs chasing.
/// </summary>
public partial class FinancialControlViewModel : ObservableObject
{
    private readonly AppSession _session;

    public FinancialControlViewModel(AppSession session)
    {
        _session = session;
        var today = BusinessClock.Today();
        _customFrom = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue));
        _customTo = _customFrom;
        SelectedPeriod = Periods[0];
    }

    public sealed record PeriodOption(string Key, string Label);

    public IReadOnlyList<PeriodOption> Periods { get; } = new[]
    {
        new PeriodOption("today", "اليوم"),
        new PeriodOption("yesterday", "أمس"),
        new PeriodOption("month", "هذا الشهر"),
        new PeriodOption("last_month", "الشهر الماضي"),
        new PeriodOption("year", "من أول السنة"),
        new PeriodOption("custom", "فترة محددة"),
    };

    public bool CanView => _session.HasPermission("reports_view");

    [ObservableProperty] private PeriodOption? _selectedPeriod;
    [ObservableProperty] private DateTimeOffset? _customFrom;
    [ObservableProperty] private DateTimeOffset? _customTo;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string _periodDisplay = "";

    public bool IsCustomPeriod => SelectedPeriod?.Key == "custom";
    public bool HasData => !IsLoading && LoadError is null && _loaded;
    private bool _loaded;

    partial void OnSelectedPeriodChanged(PeriodOption? value)
    {
        OnPropertyChanged(nameof(IsCustomPeriod));
        if (value?.Key != "custom") _ = LoadAsync();
    }

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(HasData));
    partial void OnLoadErrorChanged(string? value) => OnPropertyChanged(nameof(HasData));

    /// <summary>Cairo business dates - the same boundary every other report uses.</summary>
    public (DateOnly From, DateOnly To)? ResolveRange()
    {
        var today = BusinessClock.Today();
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        return SelectedPeriod?.Key switch
        {
            "yesterday" => (today.AddDays(-1), today.AddDays(-1)),
            "month" => (monthStart, today),
            "last_month" => (monthStart.AddMonths(-1), monthStart.AddDays(-1)),
            "year" => (new DateOnly(today.Year, 1, 1), today),
            "custom" => CustomFrom is { } f && CustomTo is { } t && DateOnly.FromDateTime(f.Date) <= DateOnly.FromDateTime(t.Date)
                ? (DateOnly.FromDateTime(f.Date), DateOnly.FromDateTime(t.Date))
                : null,
            _ => (today, today),
        };
    }

    // ---- Cash flow per account ----

    public sealed record FlowLine(string Label, long InflowPiastres, long OutflowPiastres, int Count);

    public sealed class AccountFlow
    {
        public required string AccountId { get; init; }
        public required string AccountLabel { get; init; }
        public long OpeningPiastres { get; init; }
        public long InflowPiastres { get; init; }
        public long OutflowPiastres { get; init; }
        public long ClosingPiastres { get; init; }
        public IReadOnlyList<FlowLine> Lines { get; init; } = Array.Empty<FlowLine>();
        public bool HasMovement => Lines.Count > 0;
    }

    public ObservableCollection<AccountFlow> Accounts { get; } = new();

    // ---- Pending electronic payments ----

    public sealed record PendingRow(string OrderNumber, string MethodLabel, long AmountPiastres, string AgeDisplay, string CreatedDisplay);

    public ObservableCollection<PendingRow> Pending { get; } = new();
    [ObservableProperty] private int _pendingCount;
    [ObservableProperty] private long _pendingTotalPiastres;
    [ObservableProperty] private string _oldestPendingDisplay = "";
    public bool HasPending => Pending.Count > 0;

    // ---- Voids ----

    public sealed record VoidRow(string OrderNumber, string KindLabel, long TotalPiastres, string Reason, string By, string AtDisplay, string FoodLabel);

    public ObservableCollection<VoidRow> Voids { get; } = new();
    [ObservableProperty] private long _voidsTotalPiastres;
    public bool HasVoids => Voids.Count > 0;

    // ---- Refunds ----

    public sealed record RefundRow(string OrderNumber, long AmountPiastres, string MethodLabel, string Reason, string By,
        string RefundedAtDisplay, string SoldAtDisplay, bool SaleBeforePeriod);

    public ObservableCollection<RefundRow> Refunds { get; } = new();
    [ObservableProperty] private long _refundsTotalPiastres;
    [ObservableProperty] private long _refundsOfEarlierSalesPiastres;
    public bool HasRefunds => Refunds.Count > 0;

    private bool _reloadRequested;

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!CanView) return;
        if (IsLoading) { _reloadRequested = true; return; }
        IsLoading = true;
        try
        {
            do
            {
                _reloadRequested = false;
                LoadError = null;
                if (ResolveRange() is not { } range)
                {
                    LoadError = "اختر تاريخ بداية لا يقع بعد تاريخ النهاية.";
                    return;
                }
                var actor = _session.CurrentUser!.Id;
                var cashFlow = await _session.Api.GetCashFlowAsync(actor, range.From, range.To);
                var pending = await _session.Api.GetPendingElectronicAsync(actor);
                var voids = await _session.Api.GetVoidsAsync(actor, range.From, range.To);
                var refunds = await _session.Api.GetRefundsAsync(actor, range.From, range.To);
                // The period changed while these were in flight: they answer the wrong dates.
                if (ResolveRange() != range) { _reloadRequested = true; continue; }

                var failed = new[] { cashFlow.Error, pending.Error, voids.Error, refunds.Error }.FirstOrDefault(e => e is not null);
                if (!cashFlow.Success || !pending.Success || !voids.Success || !refunds.Success)
                {
                    LoadError = failed ?? "تعذر تحميل بيانات الرقابة المالية.";
                    return;
                }

                PeriodDisplay = range.From == range.To ? Day(range.From) : $"من {Day(range.From)} إلى {Day(range.To)}";
                Fill(cashFlow.Value!, pending.Value!, voids.Value!, refunds.Value!, range.From);
                // P1/P2/P4/P5: the owner's financial report and its follow-ups. A failure there is shown in its own
                // section and does not hide the cash flow above.
                await LoadOwnerControlsAsync(range.From, range.To);
                _loaded = true;
            }
            while (_reloadRequested);
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasData));
        }
    }

    private void Fill(CashFlowReportDto cashFlow, PendingElectronicReportDto pending, VoidsReportDto voids,
        IReadOnlyList<RefundActivityDto> refunds, DateOnly periodStart)
    {
        Accounts.Clear();
        foreach (var account in cashFlow.Accounts)
        {
            Accounts.Add(new AccountFlow
            {
                AccountId = account.AccountId,
                AccountLabel = DisplayLabels.TreasuryAccount(account.AccountId),
                OpeningPiastres = account.OpeningPiastres,
                InflowPiastres = account.InflowPiastres,
                OutflowPiastres = account.OutflowPiastres,
                ClosingPiastres = account.ClosingPiastres,
                Lines = account.Lines.Select(l => new FlowLine(
                    DisplayLabels.TreasuryMovementType(l.MovementType), l.InflowPiastres, l.OutflowPiastres, l.MovementCount)).ToList(),
            });
        }

        Pending.Clear();
        foreach (var p in pending.Payments.OrderByDescending(p => p.AgeMinutes))
            Pending.Add(new PendingRow(p.OrderNumber, DisplayLabels.PaymentMethod(p.Method) + (p.Provider is { Length: > 0 } provider ? $" ({provider})" : ""),
                p.AmountPiastres, Age(p.AgeMinutes), Moment(p.CreatedAtUtc)));
        PendingCount = pending.Count;
        PendingTotalPiastres = pending.TotalPiastres;
        OldestPendingDisplay = pending.OldestAgeMinutes is { } oldest ? Age(oldest) : "";

        Voids.Clear();
        foreach (var v in voids.Voids)
            Voids.Add(new VoidRow(v.OrderNumber, VoidKind(v.Kind), v.TotalPiastres, v.Reason ?? "", v.CancelledBy ?? "",
                v.CancelledAtUtc is { } at ? Moment(at) : "", Food(v.StockDisposition)));
        VoidsTotalPiastres = voids.TotalPiastres;

        Refunds.Clear();
        long earlier = 0;
        foreach (var r in refunds.OrderBy(r => r.CreatedAtUtc))
        {
            var soldBefore = r.OriginalSaleAtUtc is { } sold && BusinessClock.BusinessDate(sold) < periodStart;
            if (soldBefore) earlier += r.AmountPiastres;
            Refunds.Add(new RefundRow(r.OrderNumber ?? r.OrderId, r.AmountPiastres, DisplayLabels.PaymentMethod(r.PaymentMethod),
                r.Reason, r.CreatedBy, Moment(r.CreatedAtUtc), r.OriginalSaleAtUtc is { } s ? Moment(s) : "غير معروف", soldBefore));
        }
        RefundsTotalPiastres = refunds.Sum(r => r.AmountPiastres);
        RefundsOfEarlierSalesPiastres = earlier;

        OnPropertyChanged(nameof(HasPending));
        OnPropertyChanged(nameof(HasVoids));
        OnPropertyChanged(nameof(HasRefunds));
    }

    private static readonly CultureInfo Arabic = CultureInfo.GetCultureInfo("ar-EG");

    private static string Day(DateOnly date) => date.ToDateTime(TimeOnly.MinValue).ToString("d MMMM yyyy", Arabic);

    private static string Moment(DateTimeOffset utc) =>
        TimeZoneInfo.ConvertTime(utc, BusinessClock.CairoTimeZone).ToString("d MMM yyyy - h:mm tt", Arabic);

    public static string Age(double minutes) => minutes switch
    {
        < 60 => $"{Math.Max(0, (int)minutes)} دقيقة",
        < 60 * 24 => $"{(int)(minutes / 60)} ساعة",
        _ => $"{(int)(minutes / (60 * 24))} يوم",
    };

    public static string VoidKind(string kind) => kind switch
    {
        "electronic_unconfirmed" => "دفع إلكتروني لم يصل",
        "undelivered_cash_on_delivery" => "توصيل لم يتم (دفع عند الاستلام)",
        // "other" (FIN-0006: a void the server cannot attribute to either cause) and any future
        // kind: never the raw word on the owner's screen.
        _ => "إلغاء لسبب آخر",
    };

    public static string Food(string? disposition) => disposition switch
    {
        "prepared" => "حُضّر - سُجّل هدرًا",
        "not_prepared" => "لم يُحضّر - أعيد للمخزون",
        _ => "غير مسجّل",
    };
}

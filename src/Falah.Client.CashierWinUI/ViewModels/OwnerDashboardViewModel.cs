using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Reports;
using Falah.Client.CashierWinUI.Services;
using Falah.Domain.Common;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// The owner's one screen.
///
/// Everything comes from a single server read that composes the same queries the individual
/// management screens use. This class adds no arithmetic - it selects a period, asks, and shapes the
/// answer for display. A figure here that disagreed with the screen it came from would be a defect.
///
/// Two presentation rules run through all of it. A figure the server could not determine reads
/// "غير معروف", never zero - an unpriced menu must never render as a free one. And nothing here
/// passes judgement: the screen reports 34% food cost, it does not decide whether 34% is good.
/// </summary>
public partial class OwnerDashboardViewModel : ObservableObject
{
    private readonly AppSession _session;
    private readonly Action<string>? _navigate;

    /// <param name="navigate">Lets an attention row open the screen that resolves it. An alert that
    /// cannot be acted on is a statement, and a statement is where the trail goes cold.</param>
    public OwnerDashboardViewModel(AppSession session, Action<string>? navigate = null)
    {
        _session = session;
        _navigate = navigate;
        SelectedPeriod = Periods[0];
    }

    public sealed record PeriodOption(string Key, string Label);

    public IReadOnlyList<PeriodOption> Periods { get; } = new[]
    {
        new PeriodOption("today", "اليوم"),
        new PeriodOption("yesterday", "أمس"),
        new PeriodOption("last7", "آخر ٧ أيام"),
        new PeriodOption("last30", "آخر ٣٠ يومًا"),
        new PeriodOption("month", "هذا الشهر"),
    };

    /// <summary>One headline figure with its direction. Kept as a view model rather than a pile of
    /// bindings so the five cards are provably the same object with different content.</summary>
    public sealed class MetricCard
    {
        public required string Label { get; init; }
        public required string ValueDisplay { get; init; }
        public required string Caption { get; init; }
        public string DeltaDisplay { get; init; } = "";
        /// <summary>up · down · flat · none — "none" is a change with no meaningful ratio.</summary>
        public string DeltaState { get; init; } = "none";
        public IReadOnlyList<long> Points { get; init; } = Array.Empty<long>();
    }

    public sealed record AttentionItem(string Severity, string Text, string? NavKey)
    {
        public bool CanOpen => NavKey is not null;
        public string OpenLabel => NavKey is null ? "" : "افتح ←";
    }

    public sealed record SplitRow(string Label, long Piastres, double Fraction, string PercentDisplay);

    public ObservableCollection<MetricCard> Metrics { get; } = new();
    public ObservableCollection<AttentionItem> Attention { get; } = new();
    public ObservableCollection<SplitRow> Split { get; } = new();
    public ObservableCollection<LowStockItemDto> LowStockItems { get; } = new();
    public ObservableCollection<PrintRouteHealthDto> PrintRoutes { get; } = new();

    [ObservableProperty] private PeriodOption? _selectedPeriod;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private OwnerDashboardDto? _data;

    public bool HasData => Data is not null && !IsLoading && LoadError is null;
    public bool CanView => _session.HasPermission("reports_view");

    private bool _reloadRequested;

    partial void OnSelectedPeriodChanged(PeriodOption? value) => _ = LoadAsync();
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(HasData));
    partial void OnLoadErrorChanged(string? value) => OnPropertyChanged(nameof(HasData));

    /// <summary>Cairo business dates, the same boundary every other figure uses, so a month here
    /// contains exactly the days the daily reports show.</summary>
    private BusinessDateRange ResolveRange()
    {
        var today = BusinessClock.Today();
        return SelectedPeriod?.Key switch
        {
            "yesterday" => BusinessDateRange.SingleDay(today.AddDays(-1)),
            "last7" => new BusinessDateRange(today.AddDays(-6), today),
            "last30" => new BusinessDateRange(today.AddDays(-29), today),
            "month" => new BusinessDateRange(new DateOnly(today.Year, today.Month, 1), today),
            _ => BusinessDateRange.SingleDay(today),
        };
    }

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
                var range = ResolveRange();
                var result = await _session.Api.GetOwnerDashboardAsync(_session.CurrentUser!.Id, range.From, range.To);
                // The period changed while the read was in flight, so this answer belongs to the
                // wrong dates - discarded rather than shown under the new heading.
                if (range != ResolveRange()) { _reloadRequested = true; continue; }
                if (!result.Success || result.Value is null)
                {
                    Data = null;
                    LoadError = result.Error ?? "تعذر تحميل مركز التحكم.";
                    return;
                }
                Data = result.Value;
            }
            while (_reloadRequested);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void Open(AttentionItem? item)
    {
        if (item?.NavKey is { } key) _navigate?.Invoke(key);
    }

    partial void OnDataChanged(OwnerDashboardDto? value)
    {
        RebuildMetrics(value);
        RebuildAttention(value);
        RebuildSplit(value);

        LowStockItems.Clear();
        PrintRoutes.Clear();
        if (value is not null)
        {
            foreach (var item in value.Stock.LowStockItems.Take(6)) LowStockItems.Add(item);
            foreach (var route in value.Health.PrintRoutes) PrintRoutes.Add(route);
        }

        foreach (var name in new[]
        {
            nameof(HasData), nameof(PeriodLabel), nameof(ComparisonLabel), nameof(HasUnknownFoodCost),
            nameof(UnknownCostLabel), nameof(HasAttention), nameof(AttentionCount), nameof(HasLowStock),
            nameof(BackupLabel), nameof(BackupIsMissing), nameof(SeriesTitle), nameof(SeriesPoints),
            nameof(HasDriverSettlementNote), nameof(DriverSettlementNote),
        }) OnPropertyChanged(name);
    }

    // ---------------- headline figures ----------------

    private void RebuildMetrics(OwnerDashboardDto? d)
    {
        Metrics.Clear();
        if (d is null) return;

        var series = d.Trend.Series.Select(p => p.NetSalesPiastres).ToList();

        Metrics.Add(Card("إجمالي المبيعات", Money(d.Cash.NetSalesPiastres), d.Trend.NetSales,
            $"{d.Cash.OrderCount} طلب · متوسط {Money(d.Cash.AverageOrderValuePiastres)}", series));

        Metrics.Add(Card("المحصّل فعليًا", Money(d.Cash.CollectedSalesPiastres), d.Trend.Collected,
            "ما دخل خزينة المطعم بالفعل", series));

        Metrics.Add(Card("مع السائقين", Money(d.Cash.CashWithDriversPiastres), d.Trend.CashWithDrivers,
            "ليست في عهدة المطعم بعد", Array.Empty<long>()));

        Metrics.Add(Card("المصروفات", Money(d.Leakage.ExpensesPiastres), d.Trend.Expenses,
            "المسجَّل في النظام فقط", Array.Empty<long>()));

        // Food cost has no comparison of its own: comparing an unknown with an unknown is noise.
        var p = d.Profitability;
        Metrics.Add(new MetricCard
        {
            Label = "نسبة تكلفة الطعام",
            ValueDisplay = p.FoodCostPercent is { } pct ? $"{pct:0.#}%" : "غير معروف",
            DeltaDisplay = $"{p.KnownCostLineCount} من {p.SoldLineCount} سطر مسعّر",
            DeltaState = "none",
            Caption = p.IsFoodCostComplete ? "كل الأصناف المباعة لها تكلفة معروفة" : "أضف وصفة للأصناف الناقصة ليظهر الرقم",
        });
    }

    private static MetricCard Card(string label, string value, ChangeDto change, string caption, IReadOnlyList<long> points) =>
        new()
        {
            Label = label,
            ValueDisplay = value,
            Caption = change.Percent is null && change.Delta != 0 ? "لا توجد فترة مقارنة" : caption,
            DeltaDisplay = DescribeChange(change),
            DeltaState = change.Percent switch { null => "none", > 0 => "up", < 0 => "down", _ => "flat" },
            Points = points,
        };

    /// <summary>A change with no ratio says so. "+100%" from a closed week would be the loudest
    /// number on the screen and would describe nothing.</summary>
    private static string DescribeChange(ChangeDto change) => change.Percent switch
    {
        null => "بلا مقارنة",
        > 0 => $"▲ {change.Percent:0.#}%",
        < 0 => $"▼ {Math.Abs(change.Percent.Value):0.#}%",
        _ => "بلا تغيير",
    };

    private static string Money(long piastres) => CashInput.Format(piastres) + " ج";

    // ---------------- work that needs a person ----------------

    private void RebuildAttention(OwnerDashboardDto? d)
    {
        Attention.Clear();
        if (d is null) return;
        var a = d.Attention;

        if (d.Health.LastCompletedBackupAtUtc is null)
            Attention.Add(new AttentionItem("bad", "لا توجد نسخة احتياطية مكتملة على الإطلاق", "backup"));

        if (a.UnauthorizedShiftDiscrepancyCount > 0)
            Attention.Add(new AttentionItem("bad",
                $"فروقات درج غير معتمدة: {a.UnauthorizedShiftDiscrepancyCount} وردية", "controlcenter"));

        if (a.UnsettledCustodyCount > 0)
            Attention.Add(new AttentionItem("warn",
                $"عهدة سائقين غير مسواة: {a.UnsettledCustodyCount} بقيمة {Money(a.UnsettledCustodyPiastres)}", "delivery"));

        if (a.PendingElectronicOrderCount > 0)
            Attention.Add(new AttentionItem("warn",
                $"مدفوعات إلكترونية لم تُؤكَّد: {a.PendingElectronicOrderCount} طلب", "orders"));

        if (d.Stock.LowStockCount > 0)
            Attention.Add(new AttentionItem("warn",
                $"{d.Stock.LowStockCount} صنف تحت الحد الأدنى", "inventory"));

        if (a.OpenExceptionCount > 0)
            Attention.Add(new AttentionItem("info",
                $"استثناءات مفتوحة في مركز التحكم: {a.OpenExceptionCount}", "controlcenter"));

        if (a.CloseBlockerCount > 0)
            Attention.Add(new AttentionItem("info",
                $"{a.CloseBlockerCount} مانع يمنع إغلاق يوم العمل", "controlcenter"));

        var silent = d.Health.PrintRoutes.Count(r => r.LastSuccessAtUtc is null);
        if (silent > 0)
            Attention.Add(new AttentionItem("info",
                $"{silent} مسار طباعة لم يُثبت أنه يعمل", "printing"));

        // ---- added after the operations audit ----

        // A backup that exists but is a week old is a different problem from no backup at all, and
        // the count of hours is the whole finding - "قديمة" on its own would let it slide.
        if (d.Health.LastBackupAgeHours is { } ageHours && ageHours >= StaleBackupHours)
            Attention.Add(new AttentionItem(ageHours >= 72 ? "bad" : "warn",
                $"آخر نسخة احتياطية مضى عليها {ageHours:0} ساعة", "backup"));

        if (d.Health.FailedBackupCount > 0)
            Attention.Add(new AttentionItem("warn",
                $"نسخ احتياطية فاشلة: {d.Health.FailedBackupCount}", "backup"));

        // Nothing at all is known about what the day's sales cost. This is louder than an incomplete
        // cost because the profit figures cannot be produced at all, not merely understated.
        if (d.Profitability.SoldLineCount > 0 && d.Profitability.KnownCostLineCount == 0)
            Attention.Add(new AttentionItem("bad",
                "لا توجد تكلفة معروفة لأي صنف مباع - لا يمكن حساب الربح", "recipes"));
        else if (!d.Profitability.IsFoodCostComplete && d.Profitability.ItemsWithUnknownCost.Count > 0)
            Attention.Add(new AttentionItem("warn",
                $"{d.Profitability.ItemsWithUnknownCost.Count} صنف مباع بلا تكلفة معروفة - الربح مبالغ فيه", "recipes"));

        if (d.Obligations.OverduePayableCount > 0)
            Attention.Add(new AttentionItem(d.Obligations.OldestOverdueDays > 30 ? "bad" : "warn",
                $"فواتير موردين متأخرة: {d.Obligations.OverduePayableCount} بقيمة {Money(d.Obligations.OverduePayablesPiastres)}"
                + $" (أقدمها متأخرة {d.Obligations.OldestOverdueDays} يوم)", "purchasing"));

        // Not a judgement about whether the amount is too much to owe - only that nobody has agreed
        // when it is due, so no report can ever say whether it is late.
        if (d.Obligations.UnscheduledPayableCount > 0)
            Attention.Add(new AttentionItem("info",
                $"{d.Obligations.UnscheduledPayableCount} فاتورة مورد بدون تاريخ استحقاق ({Money(d.Obligations.UnscheduledPayablesPiastres)})", "purchasing"));

        var attendanceProblems = d.People.AbsentCount + d.People.MissingFingerprintCount;
        if (attendanceProblems > 0)
            Attention.Add(new AttentionItem("warn",
                $"حضور يحتاج مراجعة: {d.People.AbsentCount} غياب و{d.People.MissingFingerprintCount} بصمة ناقصة", "attendance"));

        // A day left open keeps stamping today's work onto an old date. The number of days is the
        // finding - "يوم مفتوح" alone lets a week slide past unnoticed.
        if (a.OpenBusinessDayAgeDays is { } dayAge && dayAge >= 1)
            Attention.Add(new AttentionItem(dayAge >= 2 ? "bad" : "warn",
                $"يوم العمل المفتوح تاريخه متأخر {dayAge} يوم عن اليوم الحالي", "controlcenter"));

        if (a.UnusualCostChangeCount > 0)
            Attention.Add(new AttentionItem("warn",
                $"{a.UnusualCostChangeCount} خامة تغيّرت تكلفتها بعشرة أضعاف أو أكثر - راجع أسعار الفواتير", "purchasing"));

        var backedUpRoutes = d.Health.PrintRoutes
            .Where(r => r.PendingCount > 0 && r.OldestPendingAgeMinutes >= StalePrintMinutes).ToList();
        if (backedUpRoutes.Count > 0)
            Attention.Add(new AttentionItem("bad",
                $"طباعة متوقفة: {backedUpRoutes.Count} مسار به تذاكر معلّقة منذ أكثر من {StalePrintMinutes} دقيقة", "printing"));
    }

    /// <summary>A backup older than a full day means a day of trade could not be recovered. Not a
    /// policy about how often to back up - a statement about how much would be lost.</summary>
    private const double StaleBackupHours = 24;

    /// <summary>A prep ticket that has been queued this long has not reached the kitchen, whatever
    /// the printer claims. Chosen against how long an order takes to cook, not arbitrarily: past
    /// this the ticket arrives after the food should already have left.</summary>
    private const double StalePrintMinutes = 10;

    public int AttentionCount => Attention.Count;
    public bool HasAttention => Attention.Count > 0;

    // ---------------- where the money came from ----------------

    /// <summary>The channel figures (نقدي/بطاقة/إنستاباي/محفظة) deliberately do not sum to the
    /// collected total, because a settled driver's cash arrived through a settlement rather than
    /// through any sale's payment leg. Saying so is the difference between a reader trusting the
    /// numbers and a reader concluding one of them is broken.</summary>
    public bool HasDriverSettlementNote => Data is { Cash.SettledFromDriversPiastres: > 0 };

    public string DriverSettlementNote => Data is null ? ""
        : $"يشمل «المحصَّل» مبلغ {Money(Data.Cash.SettledFromDriversPiastres)} وصل من تسويات السائقين،"
          + " ولذلك لا يساوي مجموع طرق الدفع أعلاه.";

    private void RebuildSplit(OwnerDashboardDto? d)
    {
        Split.Clear();
        if (d is null) return;
        var c = d.Cash;
        var rows = new (string Label, long Value)[]
        {
            ("صالة", c.DineInPiastres),
            ("تيك أواي", c.TakeawayPiastres),
            ("توصيل", c.DeliveryPiastres),
        };
        var total = rows.Sum(r => r.Value);
        foreach (var row in rows)
        {
            // A share of nothing is not zero percent - it does not exist.
            var fraction = total > 0 ? (double)row.Value / total : 0;
            Split.Add(new SplitRow(row.Label, row.Value, fraction,
                total > 0 ? $"{fraction * 100:0}%" : "—"));
        }
    }

    // ---------------- labels ----------------

    public string PeriodLabel => Data is null ? ""
        : Data.DayCount == 1 ? Data.FromBusinessDate
        : $"{Data.FromBusinessDate} — {Data.ToBusinessDate}";

    public string ComparisonLabel => Data is null ? ""
        : Data.Trend.ComparedFromBusinessDate == Data.Trend.ComparedToBusinessDate
            ? $"مقارنة بـ {Data.Trend.ComparedFromBusinessDate}"
            : $"مقارنة بـ {Data.Trend.ComparedFromBusinessDate} — {Data.Trend.ComparedToBusinessDate}";

    public string SeriesTitle => Data?.Trend.SeriesGranularity == "hour"
        ? "المبيعات خلال اليوم" : "المبيعات يومًا بيوم";

    public IReadOnlyList<long> SeriesPoints =>
        Data?.Trend.Series.Select(p => p.NetSalesPiastres).ToList() ?? new List<long>();

    public bool HasUnknownFoodCost => Data is { Profitability.IsFoodCostComplete: false };

    public string UnknownCostLabel
    {
        get
        {
            if (Data is not { } d || d.Profitability.IsFoodCostComplete) return "";
            var p = d.Profitability;
            var head = p.KnownCostLineCount == 0
                ? $"لا توجد تكلفة معروفة لأي صنف مباع ({p.SoldLineCount} سطر)، فالربح لا يمكن حسابه."
                : $"{p.KnownCostLineCount} من {p.SoldLineCount} سطر له تكلفة معروفة، فالرقم أقل من التكلفة الحقيقية.";
            return p.ItemsWithUnknownCost.Count == 0
                ? head
                : head + " الأصناف الناقصة: " + string.Join("، ", p.ItemsWithUnknownCost.Take(6));
        }
    }

    public bool HasLowStock => Data is { Stock.LowStockCount: > 0 };
    public bool BackupIsMissing => Data is { Health.LastCompletedBackupAtUtc: null };

    public string BackupLabel
    {
        get
        {
            if (Data is not { } d) return "";
            if (d.Health.LastCompletedBackupAtUtc is not { } at) return "لا توجد نسخة احتياطية مكتملة على الإطلاق";
            var age = d.Health.LastBackupAgeHours ?? 0;
            var when = at.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
            var ageText = age < 48 ? $"منذ {age:0.#} ساعة" : $"منذ {age / 24:0.#} يومًا";
            var verified = d.Health.LastBackupVerificationStatus is { Length: > 0 } v
                ? $" • التحقق: {v}" : " • لم يتم التحقق منها";
            return $"{when} ({ageText}){verified}";
        }
    }
}

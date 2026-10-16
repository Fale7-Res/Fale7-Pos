using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Printing;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// What each printer is doing, and what needs a person.
///
/// This is a STATUS screen, not a printer driver. The system records print jobs and their attempts;
/// pushing bytes at a printer is the station agent's job (Falah.PrintAgent). That distinction is
/// load-bearing and is stated on the screen rather than implied: a job sitting in "pending" means
/// the system recorded a job nobody has printed, which is exactly the kind of thing an operator
/// needs to see instead of assuming a receipt came out.
///
/// It used to show pending jobs only - so a FAILED job, the one somebody actually has to act on,
/// was invisible, and a job stuck for an hour looked exactly like one queued a second ago.
/// </summary>
public partial class PrintingViewModel : ObservableObject
{
    private readonly AppSession _session;

    public PrintingViewModel(AppSession session) => _session = session;

    /// <summary>One printer's queue. Pending is normal; failed and stuck are not.</summary>
    public sealed class RouteQueue
    {
        public required PrintRouteStatusDto Status { get; init; }
        public required string Label { get; init; }

        public string Route => Status.Route;
        public int PendingCount => Status.PendingCount;
        public int FailedCount => Status.FailedCount;

        public bool NeedsAttention => Status.FailedCount > 0 || Status.StuckCount > 0;

        public string StateLine
        {
            get
            {
                var parts = new List<string>();
                if (Status.PendingCount > 0) parts.Add($"{Status.PendingCount} في الانتظار");
                if (Status.FailedCount > 0) parts.Add($"{Status.FailedCount} فشل");
                if (Status.StuckCount > 0) parts.Add($"{Status.StuckCount} متوقف");
                return parts.Count == 0 ? "لا يوجد شيء في الطابور" : string.Join(" · ", parts);
            }
        }

        /// <summary>How long the oldest waiting ticket has been waiting. A queue that is not moving
        /// is a printer or an agent problem, not a job problem, and it reads differently.</summary>
        public string WaitingLine =>
            Status.OldestPendingAtUtc is not { } oldest ? ""
            : $"أقدم تذكرة منتظرة من {Describe(DateTimeOffset.UtcNow - oldest)}";

        public string LastSuccessLine =>
            Status.LastSucceededAtUtc is { } at ? $"آخر طباعة ناجحة {at.ToLocalTime():MM/dd HH:mm}" : "لم تنجح طباعة بعد";

        internal static string Describe(TimeSpan elapsed)
        {
            var minutes = Math.Max(0, (int)elapsed.TotalMinutes);
            return minutes < 60 ? $"{minutes} دقيقة" : $"{minutes / 60} ساعة و{minutes % 60} دقيقة";
        }
    }

    /// <summary>One job that needs a person, said in words.</summary>
    public sealed class JobRow
    {
        public required PrintJobDto Job { get; init; }

        public string OrderNumber => Job.OrderNumber;
        public bool IsFailed => Job.Status == "Failed";

        // Same wording as PrintRouteLabelConverter. Kept here rather than calling it because view
        // models are also compiled into Falah.Client.Tests, which has no WinUI converters. This map
        // used to miss the two delivery routes and showed "DeliveryOrder"/"DriverDispatch" in English.
        public string RouteLabel => Job.Route switch
        {
            "Receipt" => "إيصال الكاشير",
            "CallCenter" => "إيصال مركز الاتصال",
            "Charcoal" => "محطة الفحم",
            "Grill" => "محطة الشواية",
            "DeliveryOrder" => "أوردر الدليفري",
            "DriverDispatch" => "تذكرة السائق",
            _ => Job.Route,
        };

        public string StatusLabel => Job.Status switch
        {
            "Failed" => "فشل",
            "Succeeded" => "تمت",
            _ => "في الانتظار",
        };

        public string AgeLine => $"من {RouteQueue.Describe(DateTimeOffset.UtcNow - Job.CreatedAtUtc)}";

        public string AttemptsLine => Job.AttemptCount == 0
            ? "لم تُحاول الطابعة بعد"
            : $"{Job.AttemptCount} محاولة" + (Job.LastAttemptAtUtc is { } at ? $" · آخرها {at.ToLocalTime():MM/dd HH:mm}" : "");

        public string ErrorLine => Job.LastError ?? "";
        public bool HasError => !string.IsNullOrWhiteSpace(Job.LastError);
        public bool IsReprint => Job.IsReprint;
    }

    public ObservableCollection<RouteQueue> Queues { get; } = new();
    public ObservableCollection<JobRow> NeedsAttention { get; } = new();
    public ObservableCollection<JobRow> RouteJobs { get; } = new();

    [ObservableProperty] private PrintingOverviewDto? _overview;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string _selectedRoute = "Receipt";

    /// <summary>The four real print routes this system models - see Falah.Domain.Printing.PrintRoute.
    /// They map one-to-one onto the restaurant's four physical printers.</summary>
    private static readonly (string Route, string Label)[] Routes =
    {
        ("Receipt", "إيصال الكاشير"),
        ("CallCenter", "إيصال مركز الاتصال"),
        ("Charcoal", "محطة الفحم"),
        ("Grill", "محطة الشواية"),
    };

    public bool HasNoPending => !IsLoading && LoadError is null && RouteJobs.Count == 0;
    public bool HasAttention => NeedsAttention.Count > 0;

    public string HeadlineState =>
        Overview is not { } o ? ""
        : o.TotalFailedCount > 0 || o.TotalStuckCount > 0
            ? $"{o.TotalFailedCount} فشل · {o.TotalStuckCount} متوقف · {o.TotalPendingCount} في الانتظار"
            : o.TotalPendingCount > 0 ? $"{o.TotalPendingCount} تذكرة في الانتظار" : "كل الطوابير فاضية";

    public bool IsHeadlineBad => Overview is { } o && (o.TotalFailedCount > 0 || o.TotalStuckCount > 0);

    private bool _reloadRequested;
    partial void OnSelectedRouteChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) { _reloadRequested = true; return; }
        IsLoading = true;
        LoadError = null;
        OnPropertyChanged(nameof(HasNoPending));
        try
        {
            var overview = await _session.Api.GetPrintingOverviewAsync();
            if (!overview.Success || overview.Value is null)
            {
                LoadError = overview.Error ?? "تعذر تحميل حالة الطباعة.";
                return;
            }
            Overview = overview.Value;

            Queues.Clear();
            foreach (var (route, label) in Routes)
            {
                var status = overview.Value.Routes.FirstOrDefault(r => r.Route == route)
                             ?? new PrintRouteStatusDto(route, 0, 0, 0, null, null);
                Queues.Add(new RouteQueue { Status = status, Label = label });
            }

            NeedsAttention.Clear();
            foreach (var job in overview.Value.NeedsAttention) NeedsAttention.Add(new JobRow { Job = job });

            await LoadJobsAsync();
        }
        finally
        {
            IsLoading = false;
            if (_reloadRequested) { _reloadRequested = false; await LoadAsync(); }
            OnPropertyChanged(nameof(HasNoPending));
            OnPropertyChanged(nameof(HasAttention));
            OnPropertyChanged(nameof(HeadlineState));
            OnPropertyChanged(nameof(IsHeadlineBad));
        }
    }

    private async Task LoadJobsAsync()
    {
        var route = SelectedRoute;
        var jobs = await _session.Api.GetPendingPrintJobsAsync(route);
        if (route != SelectedRoute) { _reloadRequested = true; return; }
        if (!jobs.Success || jobs.Value is null)
        {
            LoadError = jobs.Error ?? "تعذر تحميل قائمة الطباعة.";
            return;
        }
        RouteJobs.Clear();
        foreach (var j in jobs.Value) RouteJobs.Add(new JobRow { Job = j });
        OnPropertyChanged(nameof(HasNoPending));
    }

    [RelayCommand]
    private void SelectRoute(string route) => SelectedRoute = route;
}

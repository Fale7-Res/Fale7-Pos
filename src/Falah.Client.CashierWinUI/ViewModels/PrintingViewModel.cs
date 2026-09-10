using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Printing;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Print queue status per physical station.
///
/// This is a STATUS screen, not a printer driver. The system tracks print jobs and their attempts;
/// actually sending bytes to a printer is the station agent's job, and no such agent exists yet.
/// That distinction is load-bearing and is stated on the screen rather than implied: a job sitting
/// in "pending" means the system recorded a job nobody has printed, which is exactly the kind of
/// thing an operator needs to see instead of assuming a receipt came out.
/// </summary>
public partial class PrintingViewModel : ObservableObject
{
    private readonly AppSession _session;

    public PrintingViewModel(AppSession session) => _session = session;

    public sealed record RouteQueue(string Route, string Label, int PendingCount);

    public ObservableCollection<RouteQueue> Queues { get; } = new();
    public ObservableCollection<PrintJobDto> PendingJobs { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string _selectedRoute = "Receipt";

    /// <summary>The four real print routes this system models - see Falah.Domain.Printing.PrintRoute.</summary>
    private static readonly (string Route, string Label)[] Routes =
    {
        ("Receipt", "إيصال الكاشير"),
        ("CallCenter", "إيصال مركز الاتصال"),
        ("Charcoal", "محطة الفحم"),
        ("Grill", "محطة الشواية"),
    };

    public bool HasNoPending => !IsLoading && LoadError is null && PendingJobs.Count == 0;

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
            Queues.Clear();
            foreach (var (route, label) in Routes)
            {
                var jobs = await _session.Api.GetPendingPrintJobsAsync(route);
                if (!jobs.Success)
                {
                    LoadError = jobs.Error ?? "تعذر تحميل حالة الطباعة.";
                    return;
                }
                Queues.Add(new RouteQueue(route, label, jobs.Value?.Count ?? 0));
            }
            await LoadJobsAsync();
        }
        finally
        {
            IsLoading = false;
            if (_reloadRequested) { _reloadRequested = false; await LoadAsync(); }
            OnPropertyChanged(nameof(HasNoPending));
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
        PendingJobs.Clear();
        if (jobs.Success && jobs.Value is not null)
            foreach (var j in jobs.Value) PendingJobs.Add(j);
        OnPropertyChanged(nameof(HasNoPending));
    }

    [RelayCommand]
    private void SelectRoute(string route) => SelectedRoute = route;
}

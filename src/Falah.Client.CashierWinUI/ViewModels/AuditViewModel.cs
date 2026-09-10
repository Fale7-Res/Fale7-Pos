using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Audit;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// The audit trail: append-only, read-only. There is deliberately no edit or delete action on this
/// screen because the log itself exposes none - IAuditLog offers only Append, by construction.
/// Filters are sent to the server rather than applied client-side so a filtered view is never a
/// partial page of a larger unfiltered fetch.
/// </summary>
public partial class AuditViewModel : ObservableObject
{
    private readonly AppSession _session;

    public AuditViewModel(AppSession session) => _session = session;

    public sealed record FilterOption(string? Value, string Label);

    public ObservableCollection<AuditEventDto> Events { get; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private FilterOption? _selectedCategory;
    [ObservableProperty] private FilterOption? _selectedSeverity;

    public bool HasNoEvents => !IsLoading && LoadError is null && Events.Count == 0;
    public bool HasEvents => !IsLoading && LoadError is null && Events.Count > 0;
    private bool _reloadRequested;

    /// <summary>The categories the audit log actually writes - see the wiring list in
    /// IMPLEMENTATION_MATRIX row 15; not an invented taxonomy.</summary>
    public IReadOnlyList<FilterOption> Categories { get; } = new[]
    {
        new FilterOption(null, "كل الفئات"),
        new FilterOption("order", "الطلبات"),
        new FilterOption("cash", "النقدية"),
        new FilterOption("shift", "الورديات"),
        new FilterOption("inventory", "المخزون"),
        new FilterOption("menu", "القائمة والتسعير"),
        new FilterOption("employee", "حسابات الموظفين"),
        new FilterOption("attendance", "تصحيح الحضور"),
    };

    public IReadOnlyList<FilterOption> Severities { get; } = new[]
    {
        new FilterOption(null, "كل المستويات"),
        new FilterOption("critical", "حرج"),
        new FilterOption("warning", "تحذير"),
        new FilterOption("normal", "عادي"),
    };

    partial void OnSelectedCategoryChanged(FilterOption? value) => _ = LoadAsync();
    partial void OnSelectedSeverityChanged(FilterOption? value) => _ = LoadAsync();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) { _reloadRequested = true; return; }
        IsLoading = true;
        NotifyReadState();
        try
        {
            do
            {
                _reloadRequested = false;
                LoadError = null;
                Events.Clear();
                var category = SelectedCategory?.Value;
                var severity = SelectedSeverity?.Value;
                var result = await _session.Api.GetAuditEventsAsync(_session.CurrentUser!.Id, category, severity);
                if (category != SelectedCategory?.Value || severity != SelectedSeverity?.Value)
                { _reloadRequested = true; continue; }
                if (!result.Success || result.Value is null)
                    LoadError = result.Error ?? "تعذر تحميل سجل المراجعة.";
                else
                    foreach (var e in result.Value) Events.Add(e);
            }
            while (_reloadRequested);
        }
        finally
        {
            IsLoading = false;
            NotifyReadState();
        }
    }

    private void NotifyReadState()
    {
        OnPropertyChanged(nameof(HasNoEvents));
        OnPropertyChanged(nameof(HasEvents));
    }
}

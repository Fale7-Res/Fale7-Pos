using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.BusinessDay;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// The business day and what stands between it and closing.
///
/// Closing is never offered as a button that "tries" - the server publishes its own blocker list
/// (open shift, unsettled driver custody, incomplete journal, pending electronic payment, incomplete
/// checklist, reconciliation checks), so this screen shows exactly what is blocking and enables the
/// close only when nothing is. Exceptions are recomputed fresh on every read and are never stored;
/// only a human's acknowledge/resolve decision persists. Decisions do not remove server blockers.
/// </summary>
public partial class ControlCenterViewModel : ObservableObject
{
    private readonly AppSession _session;

    public ControlCenterViewModel(AppSession session)
    {
        _session = session;
        CashCount = new CashCountViewModel(session);
        if (_safeDropRequest.Value is { } recovered)
        {
            SafeDropAmountText = CashInput.Format(recovered.AmountPiastres);
            SafeDropReason = recovered.Reason;
            SafeDropError = _safeDropRequest.FailureMessage(null);
            IsSafeDropOpen = true;
            _session.SetManagementOperationPending(this, true);
        }
    }

    /// <summary>The physical denomination count belongs to the business day, and is gated on its own
    /// `cash_count` permission - not on `business_day_close` like the rest of this screen.</summary>
    public CashCountViewModel CashCount { get; }

    public bool CanCountCash => CashCount.CanOpen;

    [RelayCommand]
    private async Task OpenCashCountAsync()
    {
        if (BusinessDay is { } day) await CashCount.OpenAsync(day.Id);
    }

    public ObservableCollection<CloseBlockerDto> Blockers { get; } = new();
    public ObservableCollection<ChecklistItemStatusDto> Checklist { get; } = new();
    public ObservableCollection<ControlExceptionDto> Exceptions { get; } = new();

    [ObservableProperty] private BusinessDayDto? _businessDay;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _loadError;
    [ObservableProperty] private string? _actionNote;
    [ObservableProperty] private string? _actionError;
    [ObservableProperty] private bool _isBusy;

    public bool HasBusinessDay => BusinessDay is not null && !IsLoading && LoadError is null;
    partial void OnIsLoadingChanged(bool value) => RefreshActionState();
    partial void OnLoadErrorChanged(string? value) => RefreshActionState();
    public bool HasNoBusinessDay => !IsLoading && LoadError is null && BusinessDay is null;
    public bool CanControl => _session.HasPermission("business_day_close");

    /// <summary>Closing is legal only when the server reports zero blockers - so the action is
    /// enabled by the server's own verdict, never by a client-side guess at the same rules.</summary>
    public bool CanClose => CanEditAction && HasBusinessDay && IsActiveDay && Blockers.Count == 0 && CanControl;
    public bool HasBlockers => Blockers.Count > 0;

    partial void OnBusinessDayChanged(BusinessDayDto? value)
    {
        OnPropertyChanged(nameof(HasBusinessDay));
        OnPropertyChanged(nameof(HasNoBusinessDay));
        RefreshActionState();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!CanEditAction || IsDayActionOpen || ExceptionTarget is not null) return;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        LoadError = null;
        OnPropertyChanged(nameof(CanClose));
        try
        {
            var history = await _session.Api.GetBusinessDaysAsync();
            if (!history.Success || history.Value is null) { LoadError = history.Error ?? "تعذر تحميل أيام العمل."; return; }
            Days.Clear();
            foreach (var entry in history.Value) Days.Add(entry);
            var selected = Days.FirstOrDefault(d => d.Id == _selectedDayId);
            var day = selected is null ? await _session.Api.GetCurrentBusinessDayAsync()
                : await _session.Api.GetBusinessDayAsync(selected.Id);
            if (!day.Success && day.StatusCode == 404 && selected is null && Days.Count > 0)
                day = await _session.Api.GetBusinessDayAsync(Days[0].Id);
            if (!day.Success || day.Value is null)
            {
                // 404 here is a legitimate state (no day open), not an error to shout about.
                BusinessDay = null;
                Reconciliation = null;
                LatestSnapshot = null;
                if (day.StatusCode != 404 || selected is not null || Days.Count > 0)
                    LoadError = day.Error ?? "تعذر تحميل يوم العمل.";
                return;
            }
            BusinessDay = day.Value;
            _selectedDayId = day.Value.Id;
            SelectedHistoryDay = Days.FirstOrDefault(d => d.Id == day.Value.Id);

            var blockers = await _session.Api.GetCloseBlockersAsync(day.Value.Id);
            if (!blockers.Success || blockers.Value is null) { LoadError = blockers.Error ?? "تعذر تحميل موانع الإغلاق."; return; }
            Blockers.Clear();
            if (blockers.Success && blockers.Value is not null)
                foreach (var b in blockers.Value) Blockers.Add(b);

            var checklist = await _session.Api.GetChecklistAsync(day.Value.Id);
            if (!checklist.Success || checklist.Value is null) { LoadError = checklist.Error ?? "تعذر تحميل قائمة الإغلاق."; return; }
            Checklist.Clear();
            if (checklist.Success && checklist.Value is not null)
                foreach (var c in checklist.Value) Checklist.Add(c);

            var exceptions = await _session.Api.GetExceptionsAsync(day.Value.Id);
            if (!exceptions.Success || exceptions.Value is null) { LoadError = exceptions.Error ?? "تعذر تحميل التنبيهات."; return; }
            Exceptions.Clear();
            if (exceptions.Success && exceptions.Value is not null)
                foreach (var e in exceptions.Value) Exceptions.Add(e);

            await LoadReconciliationAsync(day.Value.Id);
            LatestSnapshot = null;
            if (day.Value.Status.Equals("closed", StringComparison.OrdinalIgnoreCase)) await LoadSnapshotAsync(day.Value.Id);
        }
        finally
        {
            _hasLoaded = LoadError is null;
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoBusinessDay));
            OnPropertyChanged(nameof(CanClose));
            OnPropertyChanged(nameof(HasBlockers));
        }
    }

    [RelayCommand]
    private async Task ToggleChecklistItemAsync(ChecklistItemStatusDto item)
    {
        if (!CanEditAction || !HasBusinessDay || !IsActiveDay || !CanControl || item is null) return;
        var request = new CompleteChecklistItemRequest(BusinessDay!.Id, item.Id, !item.Completed, _session.CurrentUser!.Id);
        SetOperation(request, r => _session.Api.CompleteChecklistItemAsync(r));
        await RunOperationAsync();
    }
}

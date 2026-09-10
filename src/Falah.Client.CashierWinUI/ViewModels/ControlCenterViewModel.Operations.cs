using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.BusinessDay;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

public partial class ControlCenterViewModel
{
    public ObservableCollection<BusinessDayDto> Days { get; } = new();
    private string? _selectedDayId;
    private bool _hasLoaded;
    [ObservableProperty] private BusinessDayDto? _selectedHistoryDay;
    partial void OnSelectedHistoryDayChanged(BusinessDayDto? value)
    {
        if (value is not null && !IsLoading && CanEditAction) _ = SelectDayAsync(value);
    }
    private Func<Task<(bool Success, bool Unknown, string? Error)>>? _pendingOperation;
    private bool _outcomeUnknown;
    public bool HasPendingOperation => _pendingOperation is not null;
    public bool CanEditAction => !IsBusy && !HasPendingOperation;
    public bool IsActiveDay => BusinessDay?.Status.ToLowerInvariant() is "open" or "reopened" or "closing";
    public bool CanOpenDay => _hasLoaded && CanEditAction && !IsLoading && LoadError is null && _session.HasPermission("business_day_open") &&
        !Days.Any(d => d.Status.ToLowerInvariant() is "open" or "reopened" or "closing");
    public bool CanReopenDay => CanEditAction && HasBusinessDay && BusinessDay!.Status.Equals("closed", StringComparison.OrdinalIgnoreCase) &&
        _session.HasPermission("business_day_reopen");
    public bool CanReviewExceptions => CanEditAction && HasBusinessDay && _session.HasPermission("control_center");
    public bool CanChangeChecklist => CanEditAction && HasBusinessDay && IsActiveDay && CanControl;
    partial void OnIsBusyChanged(bool value)
    {
        RefreshActionState();
        _session.SetManagementOperationPending(this, !CanEditAction);
    }

    private void RefreshActionState()
    {
        foreach (var name in new[] { nameof(HasBusinessDay), nameof(HasNoBusinessDay), nameof(CanClose),
            nameof(CanEditAction), nameof(HasPendingOperation), nameof(CanOpenDay), nameof(CanReopenDay),
            nameof(CanReviewExceptions), nameof(CanChangeChecklist), nameof(IsActiveDay) }) OnPropertyChanged(name);
    }

    [RelayCommand]
    private async Task SelectDayAsync(BusinessDayDto day)
    {
        if (!CanEditAction || IsLoading || IsDayActionOpen || ExceptionTarget is not null) return;
        _selectedDayId = day.Id;
        await ReloadAsync();
    }

    [ObservableProperty] private string? _dayAction;
    [ObservableProperty] private string _newBusinessDateText = "";
    [ObservableProperty] private string _actionReason = "";
    public bool IsDayActionOpen => DayAction is not null;
    public bool IsOpeningDay => DayAction == "open";
    public bool IsReopeningDay => DayAction == "reopen";
    public string DayActionTitle => DayAction switch { "open" => "فتح يوم عمل", "close" => "إغلاق يوم العمل", _ => "إعادة فتح يوم العمل" };
    partial void OnDayActionChanged(string? value)
    {
        OnPropertyChanged(nameof(IsDayActionOpen)); OnPropertyChanged(nameof(IsOpeningDay));
        OnPropertyChanged(nameof(IsReopeningDay)); OnPropertyChanged(nameof(DayActionTitle));
    }

    [RelayCommand]
    private void OpenDayAction(string action)
    {
        if (!(action switch { "open" => CanOpenDay, "close" => CanClose, "reopen" => CanReopenDay, _ => false })) return;
        ActionError = null; ActionNote = null; ActionReason = ""; NewBusinessDateText = "";
        DayAction = action;
    }

    [RelayCommand]
    private void CloseDayAction() { if (CanEditAction) DayAction = null; }

    [RelayCommand]
    private async Task SubmitDayActionAsync()
    {
        if (IsBusy) return;
        if (HasPendingOperation) { await RunOperationAsync(); return; }
        var actor = _session.CurrentUser!.Id;
        switch (DayAction)
        {
            case "open" when CanOpenDay:
                DateOnly? date = null;
                if (!string.IsNullOrWhiteSpace(NewBusinessDateText))
                {
                    if (!DateOnly.TryParseExact(NewBusinessDateText.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var parsed)) { ActionError = "أدخل تاريخًا صحيحًا بالصيغة YYYY-MM-DD."; return; }
                    date = parsed;
                }
                SetOperation(new OpenBusinessDayRequest($"day-{Guid.NewGuid():N}", actor, date), async r =>
                {
                    var result = await _session.Api.OpenBusinessDayAsync(r);
                    if (result.Success && result.Value is not null) _selectedDayId = result.Value.BusinessDayId;
                    return result;
                });
                break;
            case "close" when CanClose:
                SetOperation(new CloseBusinessDayRequest($"close-{Guid.NewGuid():N}", BusinessDay!.Id, actor, null, null),
                    r => _session.Api.CloseBusinessDayAsync(r));
                break;
            case "reopen" when CanReopenDay:
                if (string.IsNullOrWhiteSpace(ActionReason)) { ActionError = "سبب إعادة الفتح مطلوب."; return; }
                SetOperation(new ReopenBusinessDayRequest($"reopen-{Guid.NewGuid():N}", BusinessDay!.Id, ActionReason.Trim(), actor, null, null),
                    r => _session.Api.ReopenBusinessDayAsync(r));
                break;
            default: return;
        }
        await RunOperationAsync();
    }

    [ObservableProperty] private ControlExceptionDto? _exceptionTarget;
    [ObservableProperty] private string _exceptionStatus = "acknowledged";
    public sealed record ExceptionStatusOption(string Code, string Label);
    public IReadOnlyList<ExceptionStatusOption> ExceptionStatuses { get; } = new[]
    { new ExceptionStatusOption("acknowledged", "تمت المراجعة"), new ExceptionStatusOption("resolved", "تمت المعالجة") };

    [RelayCommand]
    private void OpenException(ControlExceptionDto exception)
    {
        if (!CanReviewExceptions) return;
        ExceptionTarget = exception; ExceptionStatus = "acknowledged"; ActionReason = ""; ActionError = null;
    }
    [RelayCommand]
    private void CloseException() { if (CanEditAction) ExceptionTarget = null; }
    [RelayCommand]
    private async Task SubmitExceptionAsync()
    {
        if (IsBusy) return;
        if (HasPendingOperation) { await RunOperationAsync(); return; }
        if (!CanReviewExceptions || ExceptionTarget is null) return;
        if (string.IsNullOrWhiteSpace(ActionReason)) { ActionError = "سبب المراجعة مطلوب."; return; }
        SetOperation(new SetExceptionStatusRequest(ExceptionTarget.Id, ExceptionStatus, ActionReason.Trim(), _session.CurrentUser!.Id, null, null),
            r => _session.Api.SetExceptionStatusAsync(r));
        await RunOperationAsync();
    }

    private void SetOperation<TRequest, TResult>(TRequest request, Func<TRequest, Task<ApiClient.ApiResult<TResult>>> send)
    {
        _outcomeUnknown = false;
        _pendingOperation = async () =>
        {
            var result = await send(request);
            return (result.Success && result.Value is not null, result.OutcomeUnknown, result.Error);
        };
    }

    [RelayCommand]
    private async Task RetryOperationAsync()
    {
        if (!IsBusy && HasPendingOperation) await RunOperationAsync();
    }

    private async Task RunOperationAsync()
    {
        if (_pendingOperation is null) return;
        IsBusy = true;
        ActionError = null;
        try
        {
            var result = await _pendingOperation();
            if (!result.Success)
            {
                _outcomeUnknown |= result.Unknown;
                if (!_outcomeUnknown) _pendingOperation = null;
                ActionError = _outcomeUnknown ? "لم تُحسم نتيجة العملية. أعد المحاولة بنفس الطلب قبل المغادرة."
                    : result.Error ?? "تعذر تنفيذ العملية.";
                if (!_outcomeUnknown) await ReloadAsync();
                return;
            }
            _pendingOperation = null; _outcomeUnknown = false;
            DayAction = null; ExceptionTarget = null;
            ActionNote = "تم تأكيد العملية من السيرفر.";
            await ReloadAsync();
        }
        finally { IsBusy = false; }
    }
}

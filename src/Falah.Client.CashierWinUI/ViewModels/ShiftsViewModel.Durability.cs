using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Shifts;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// UI-0001 (shift screens). Opening a shift moves the float out of the main treasury and closing one
/// records the counted cash, so both are money commands: each is kept on disk from before it is sent
/// until its outcome is known, and a retry - in this session or after a restart - re-sends the exact
/// same request, so the server replays it instead of treating it as a new operation.
///
/// A recovered close cannot go back through the close modal (after a committed close there is no
/// open shift to close), so the screen offers the retry directly, with what was submitted.
/// </summary>
public partial class ShiftsViewModel : IManagementRequestOwner
{
    private readonly RetainedRequest<OpenShiftRequest> _openWrite = new("shifts.open");
    private readonly RetainedRequest<CloseShiftRequest> _closeWrite = new("shifts.close");

    [ObservableProperty] private bool _isRetryingRecovered;
    [ObservableProperty] private string? _recoveredShiftError;

    public bool HasRecoveredShiftOperation => _openWrite.HasPending || _closeWrite.HasPending;

    public string RecoveredShiftOperationText =>
        _openWrite.Value is { } open
            ? $"فتح {open.ShiftName} برصيد افتتاحي {CashInput.Format(open.OpeningCashPiastres)} ج لم تُعرف نتيجته. اضغط «تحقّق وأعد الإرسال» لإرسال نفس الطلب؛ لن يُسجَّل مرتين."
            : _closeWrite.Value is { } close
                ? $"إغلاق الوردية بمبلغ معدود {CashInput.Format(close.ActualCashCountedPiastres)} ج لم تُعرف نتيجته. اضغط «تحقّق وأعد الإرسال» لإرسال نفس الطلب؛ لن يُسجَّل مرتين."
                : "";

    partial void OnIsRetryingRecoveredChanged(bool value) => RefreshShiftRecovery();
    partial void OnIsOpeningShiftChanged(bool value) => RefreshShiftRecovery();
    partial void OnIsClosingShiftChanged(bool value) => RefreshShiftRecovery();

    private void RefreshShiftRecovery()
    {
        OnPropertyChanged(nameof(HasRecoveredShiftOperation));
        OnPropertyChanged(nameof(RecoveredShiftOperationText));
        _session.SetManagementOperationPending(this, HasRecoveredShiftOperation || IsRecoveryBusy);
    }

    private async Task<ApiClient.ApiResult<OpenShiftResult>> SendOpenAsync(OpenShiftRequest request)
    {
        var result = await _openWrite.SendAsync(request, r => _session.Api.OpenShiftAsync(r));
        RefreshShiftRecovery();
        return result;
    }

    private async Task<ApiClient.ApiResult<CloseShiftResult>> SendCloseAsync(CloseShiftRequest request)
    {
        var result = await _closeWrite.SendAsync(request, r => _session.Api.CloseShiftAsync(r));
        RefreshShiftRecovery();
        return result;
    }

    [RelayCommand]
    private async Task RetryRecoveredShiftOperationAsync()
    {
        if (IsRecoveryBusy || !HasRecoveredShiftOperation) return;
        IsRetryingRecovered = true;
        RecoveredShiftError = null;
        try
        {
            bool success;
            string? error;
            string? closedShiftId = null;
            if (_openWrite.Value is { } open)
            {
                var r = await SendOpenAsync(open);
                (success, error) = (r.Success, r.Error);
            }
            else
            {
                var pendingClose = _closeWrite.Value!;
                var r = await SendCloseAsync(pendingClose);
                (success, error) = (r.Success, r.Error);
                if (success) { _session.ShiftId = null; _session.ShiftName = null; closedShiftId = pendingClose.ShiftId; }
            }

            if (!success)
            {
                RecoveredShiftError = HasRecoveredShiftOperation
                    ? $"{error ?? "تعذر الاتصال بالسيرفر."} ما زالت النتيجة غير محسومة؛ راجع سجل الورديات أو أعد المحاولة."
                    : error;
                return;
            }
            _header.Refresh();
            await LoadAsync();
            if (closedShiftId is not null) await Statement.ShowAsync(closedShiftId);
        }
        finally
        {
            IsRetryingRecovered = false;
        }
    }

    public string RecoveryLabel => "الورديات";
    public bool IsRecoveryBusy => IsOpeningShift || IsClosingShift || IsRetryingRecovered;
    public IEnumerable<IPendingRequest> RecoveryRequests => new IPendingRequest[] { _openWrite, _closeWrite };
    public void RefreshRecoveryState()
    {
        if (!HasRecoveredShiftOperation) RecoveredShiftError = null;
        RefreshShiftRecovery();
    }
}

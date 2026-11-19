using Falah.Client.CashierWinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Approvals;
using Falah.Application.Shifts;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Blind-close from the WinUI side: the cashier enters what they counted, taps "متابعة" to see
/// what the system expected (server-computed - this view never calculates it locally), then
/// confirms. A discrepancy beyond the server's threshold additionally requires a manager's PIN,
/// resolved through the same login-by-pin endpoint the lock screen uses, then exchanged for a
/// manager approval before the close itself is attempted - mirroring how RefundOrderRequest and
/// CheckoutOrderRequest already carry an optional approval.
/// </summary>
public partial class CashierViewModel
{
    [ObservableProperty] private bool _isCloseShiftOpen;
    [ObservableProperty] private string _closeShiftActualCashText = "0";
    [ObservableProperty] private string? _closeShiftReason;
    [ObservableProperty] private string _closeShiftManagerPin = "";
    [ObservableProperty] private PrepareShiftCloseResult? _closeShiftPreview;
    [ObservableProperty] private string? _closeShiftError;
    [ObservableProperty] private bool _isClosingShift;

    // Physical handover (owner, 2026-11-08): of the counted cash, a float stays in the drawer for the next cashier
    // (offered as the target float, never more than was counted) and the rest is handed to a named manager.
    [ObservableProperty] private string _closeShiftRetainedText = "0";
    [ObservableProperty] private HandoverReceiverDto? _selectedHandoverReceiver;
    public System.Collections.ObjectModel.ObservableCollection<HandoverReceiverDto> HandoverReceivers { get; } = new();
    /// <summary>The target drawer float (settings: default opening cash), read at start-up.</summary>
    internal long TargetFloatPiastres { get; set; }

    public bool CloseShiftNeedsApproval => CloseShiftPreview?.RequiresManagerApproval ?? false;
    public bool IsCloseShiftCounting => CloseShiftPreview is null;
    partial void OnCloseShiftPreviewChanged(PrepareShiftCloseResult? value)
    {
        OnPropertyChanged(nameof(CloseShiftNeedsApproval));
        OnPropertyChanged(nameof(IsCloseShiftCounting));
        if (value is not null)
            CloseShiftRetainedText = CashInput.Format(Math.Min(value.ActualCashPiastres, Math.Max(0, TargetFloatPiastres)));
        OnPropertyChanged(nameof(CloseShiftHandedOverText));
    }
    partial void OnCloseShiftRetainedTextChanged(string value) => OnPropertyChanged(nameof(CloseShiftHandedOverText));

    /// <summary>Counted minus retained: what physically goes to the receiver.</summary>
    public string CloseShiftHandedOverText =>
        CloseShiftPreview is null || !CashInput.TryParse(CloseShiftRetainedText, out var retained) || retained < 0 || retained > CloseShiftPreview.ActualCashPiastres
            ? "—" : CashInput.Format(CloseShiftPreview.ActualCashPiastres - retained) + " ج";

    private async Task LoadHandoverReceiversAsync()
    {
        var receivers = await _session.Api.GetHandoverReceiversAsync();
        if (!receivers.Success || receivers.Value is null) return;
        HandoverReceivers.Clear();
        foreach (var r in receivers.Value) HandoverReceivers.Add(r);
        // Never preselected: the cashier names the person who actually takes the cash.
        SelectedHandoverReceiver = null;
    }

    [RelayCommand]
    private void OpenCloseShift()
    {
        if (!CanCloseShift) return;
        if (!CanEditOperation) return;
        if (Cart.Count > 0) { CheckoutError = "أنهِ الطلب الحالي أو ألغِه قبل إغلاق الوردية."; return; }
        CloseShiftActualCashText = "0";
        CloseShiftReason = null;
        CloseShiftManagerPin = "";
        CloseShiftPreview = null;
        CloseShiftError = null;
        CloseShiftRetainedText = "0";
        SelectedHandoverReceiver = null;
        IsCloseShiftOpen = true;
        _handoverReceivers = LoadHandoverReceiversAsync();
    }
    private Task _handoverReceivers = Task.CompletedTask;

    [RelayCommand]
    private void CancelCloseShift() { if (IsOperationBusy) return; if (_pendingCloseShift is not null) { CloseShiftError = "لم تُحسم نتيجة العملية؛ أعد المحاولة للتحقق قبل الإغلاق."; return; } IsCloseShiftOpen = false; }

    /// <summary>Step 1: ask the server what it expects, given what was just counted. Blind by
    /// construction - the cashier can only see this AFTER committing to a counted figure, never
    /// before, matching the reference app's lock-screen-style blind count.</summary>
    [RelayCommand]
    private async Task PrepareCloseShiftAsync()
    {
        if (IsClosingShift) return;
        if (!CashInput.TryParse(CloseShiftActualCashText, out var actualEgp) || actualEgp < 0)
        {
            CloseShiftError = "أدخل المبلغ المعدود فعليًا بشكل صحيح.";
            return;
        }

        IsClosingShift = true;
        CloseShiftError = null;
        try
        {
            var request = new PrepareShiftCloseRequest(_session.ShiftId!, actualEgp);
            var result = await _session.Api.PrepareShiftCloseAsync(request);
            if (!result.Success || result.Value is null)
            {
                CloseShiftError = result.Error ?? "تعذر حساب الرصيد المتوقع.";
                return;
            }
            try { await _handoverReceivers; } catch (Exception) { /* the cashier can still pick a receiver once it loads */ }
            CloseShiftPreview = result.Value;
        }
        finally
        {
            IsClosingShift = false;
        }
    }

    [RelayCommand]
    private void BackToCloseShiftCount() { if (CanEditOperation) CloseShiftPreview = null; }

    /// <summary>Step 2: commit. When the difference is within the server's threshold this closes
    /// immediately; otherwise a manager PIN is resolved to a real user first (never trusted as
    /// typed - login-by-pin is the only thing that turns a PIN into an identity) and exchanged for
    /// a "shifts_manage" approval before the close is attempted, exactly like a refund or a comp.</summary>
    [RelayCommand]
    private async Task ConfirmCloseShiftAsync()
    {
        if (IsClosingShift) return;
        // A close recovered after a restart is retried exactly as it was sent; its preview did not
        // survive the restart and is not needed. Only a NEW close needs the count's preview.
        if (CloseShiftPreview is null && _pendingCloseShift is null) return;
        IsClosingShift = true;
        CloseShiftError = null;
        try
        {
            if (_pendingCloseShift is not null) { await CompleteCloseShiftAsync(_pendingCloseShift); return; }
            if (CloseShiftPreview is null) return;

            if (!CashInput.TryParse(CloseShiftRetainedText, out var retained) || retained < 0)
            { CloseShiftError = "أدخل العهدة المتروكة في الدرج بشكل صحيح."; return; }
            if (retained > CloseShiftPreview.ActualCashPiastres)
            { CloseShiftError = "العهدة المتروكة لا يمكن أن تكون أكبر من المعدود."; return; }
            if (retained < CloseShiftPreview.ActualCashPiastres && SelectedHandoverReceiver is null)
            { CloseShiftError = "اختر المسؤول الذي استلم النقدية."; return; }

            string? approvalId = null;
            string? approverUserId = null;

            if (CloseShiftPreview.RequiresManagerApproval)
            {
                if (string.IsNullOrWhiteSpace(CloseShiftReason))
                {
                    CloseShiftError = "الفارق يتجاوز الحد المسموح ويتطلب سببًا واضحًا.";
                    return;
                }
                if (CloseShiftManagerPin.Length < 4)
                {
                    CloseShiftError = "أدخل رمز المدير المخوّل باعتماد هذا الفارق.";
                    return;
                }

                // One call, not two: the server checks the PIN and decides who approved.
                var approval = await _session.Api.IssueManagerApprovalAsync(
                    new IssueManagerApprovalRequest(CloseShiftManagerPin, "shifts_manage", CloseShiftReason));
                if (!approval.Success || approval.Value is null)
                {
                    CloseShiftError = approval.Error ?? "رمز المدير غير صحيح أو غير مخوّل لاعتماد إغلاق الوردية.";
                    return;
                }
                approvalId = approval.Value.ApprovalId;
                approverUserId = approval.Value.ApproverUserId;
            }

            var closeRequest = new CloseShiftRequest(
                CloseRequestId: _CloseShiftRequestId, ShiftId: _session.ShiftId!,
                ClosedByUserId: _session.CurrentUser!.Id, ActualCashCountedPiastres: CloseShiftPreview.ActualCashPiastres,
                Reason: CloseShiftReason, ManagerApprovalId: approvalId, ManagerApproverUserId: approverUserId,
                RetainedFloatPiastres: retained, HandoverReceiverUserId: SelectedHandoverReceiver?.UserId);
            await CompleteCloseShiftAsync(closeRequest);
        }
        finally
        {
            IsClosingShift = false;
        }
    }
    private CloseShiftRequest? _pendingCloseShift;
    private string _CloseShiftRequestId = Guid.NewGuid().ToString("N");
    private async Task CompleteCloseShiftAsync(CloseShiftRequest closeRequest)
    {
        var wasUnresolved = _pendingCloseShift is not null;
        _pendingCloseShift = closeRequest;
        Remember(ShiftCloseOperation, closeRequest);
        RefreshOperationState();
        var result = await _session.Api.CloseShiftAsync(closeRequest);
        if (result.Success || (!wasUnresolved && !result.OutcomeUnknown)) { _pendingCloseShift = null; Remember(ShiftCloseOperation, null); }
        NoteRetryOutcome(ShiftCloseOperation, wasUnresolved, result.Success, result.OutcomeUnknown, result.Error);
        if (result.Success) _CloseShiftRequestId = Guid.NewGuid().ToString("N");
        RefreshOperationState();
        if (!result.Success || result.Value is null)
        {
            CloseShiftError = result.Error ?? "تعذر إغلاق الوردية.";
            return;
        }

        _session.ShiftId = null;
        _session.ShiftName = null;
        OnPropertyChanged(nameof(IsShiftOpen));
        _header.Refresh();
        IsCloseShiftOpen = false;
        // UI-REQ-002: what was handed over, and whether its paper printed.
        await ShiftStatement.ShowAsync(result.Value.ShiftId);
    }

    private ShiftStatementViewModel? _shiftStatement;
    public ShiftStatementViewModel ShiftStatement => _shiftStatement ??= new ShiftStatementViewModel(_session);

}

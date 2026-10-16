using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Approvals;
using Falah.Application.Shifts;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Blind-close from the Shifts screen - the same two-step flow (count blind, then reveal the
/// server-computed expected figure and require a manager PIN above the discrepancy threshold)
/// CashierViewModel.ShiftClose.cs already implements for the POS-embedded close button. Nothing
/// about the rule is reimplemented here: this calls the identical PrepareShiftCloseAsync/
/// CloseShiftAsync/IssueManagerApprovalAsync endpoints, because the reference itself has this same
/// action reachable from two places (the POS header AND this dedicated screen) calling one shared
/// closeShiftBlind - two presentations of one operation, not two operations.
/// </summary>
public partial class ShiftsViewModel
{
    [ObservableProperty] private bool _isCloseShiftModalOpen;
    [ObservableProperty] private string _closeShiftActualCashText = "0";
    [ObservableProperty] private string? _closeShiftReason;
    [ObservableProperty] private string _closeShiftManagerPin = "";
    [ObservableProperty] private PrepareShiftCloseResult? _closeShiftPreview;
    [ObservableProperty] private string? _closeShiftError;
    [ObservableProperty] private bool _isClosingShift;

    public bool CloseShiftNeedsApproval => CloseShiftPreview?.RequiresManagerApproval ?? false;
    public bool IsCloseShiftCounting => CloseShiftPreview is null;

    partial void OnCloseShiftPreviewChanged(PrepareShiftCloseResult? value)
    {
        OnPropertyChanged(nameof(CloseShiftNeedsApproval));
        OnPropertyChanged(nameof(IsCloseShiftCounting));
    }

    [RelayCommand]
    private void OpenCloseShiftModal()
    {
        if (!CanCloseShift || ActiveShift is null) return;
        CloseShiftActualCashText = "0";
        CloseShiftReason = null;
        CloseShiftManagerPin = "";
        CloseShiftPreview = null;
        CloseShiftError = null;
        IsCloseShiftModalOpen = true;
    }

    [RelayCommand]
    private void CancelCloseShiftModal() { if (!IsClosingShift) IsCloseShiftModalOpen = false; }

    [RelayCommand]
    private void BackToCloseShiftCount() => CloseShiftPreview = null;

    /// <summary>Step 1: reveal what the system expected, given what was just counted. The cashier
    /// only ever learns this AFTER committing to a counted figure - never before - which is the
    /// whole point of a blind count.</summary>
    [RelayCommand]
    private async Task PrepareCloseShiftAsync()
    {
        if (IsClosingShift || ActiveShift is null) return;
        if (!CashInput.TryParse(CloseShiftActualCashText, out var actualPiastres) || actualPiastres < 0)
        {
            CloseShiftError = "أدخل المبلغ المعدود فعليًا بشكل صحيح.";
            return;
        }

        IsClosingShift = true;
        CloseShiftError = null;
        try
        {
            var result = await _session.Api.PrepareShiftCloseAsync(
                new PrepareShiftCloseRequest(ActiveShift.ShiftId, actualPiastres));
            if (!result.Success || result.Value is null)
            {
                CloseShiftError = result.Error ?? "تعذر حساب الرصيد المتوقع.";
                return;
            }
            CloseShiftPreview = result.Value;
        }
        finally
        {
            IsClosingShift = false;
        }
    }

    /// <summary>Step 2: commit. Within threshold, closes immediately; otherwise a manager PIN is
    /// resolved to a real approval first (never trusted as typed - only login-by-pin turns a PIN
    /// into an identity), exactly like the POS-embedded close and like a refund or a comp.</summary>
    [RelayCommand]
    private async Task ConfirmCloseShiftAsync()
    {
        if (IsClosingShift || ActiveShift is null || CloseShiftPreview is null) return;
        IsClosingShift = true;
        CloseShiftError = null;
        try
        {
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

            var closeResult = await _session.Api.CloseShiftAsync(new CloseShiftRequest(
                CloseRequestId: $"shift-close-{Guid.NewGuid():N}", ShiftId: ActiveShift.ShiftId,
                ClosedByUserId: _session.CurrentUser!.Id, ActualCashCountedPiastres: CloseShiftPreview.ActualCashPiastres,
                Reason: CloseShiftReason, ManagerApprovalId: approvalId, ManagerApproverUserId: approverUserId));

            if (!closeResult.Success || closeResult.Value is null)
            {
                CloseShiftError = closeResult.Error ?? "تعذر إغلاق الوردية.";
                return;
            }

            IsCloseShiftModalOpen = false;
            _session.ShiftId = null;
            _session.ShiftName = null;
            _header.Refresh();
            await LoadAsync();
        }
        finally
        {
            IsClosingShift = false;
        }
    }
}

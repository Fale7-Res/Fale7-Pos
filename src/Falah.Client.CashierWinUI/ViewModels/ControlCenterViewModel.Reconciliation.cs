using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.BusinessDay;
using Falah.Application.Reports;
using Falah.Application.Treasury;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>Ported from DailySettlementScreen.tsx's reconciliation cards, safe-drop action, and
/// read-only past-snapshot display - the three pieces of that reference screen this app's own
/// Control Center did not yet have (open/reopen/close/checklist/blockers/cash-count already existed
/// here before this file). Reconciliation figures are management information, same boundary as
/// Shifts' own sales totals - gated on `reports_view`, never invented when the actor lacks it.</summary>
public partial class ControlCenterViewModel
{
    // ---------------- Reconciliation (management-only) ----------------

    [ObservableProperty] private BusinessDayReconciliationDto? _reconciliation;
    public bool CanSeeReconciliation => _session.HasPermission("reports_view");

    /// <summary>The reference's single `differencePiastres` card has no direct equivalent field
    /// here - this app's reconciliation reports shift shortage/overage separately instead of netting
    /// them into one signed figure, so the net is derived rather than invented.</summary>
    public long ReconciliationDifferencePiastres =>
        (Reconciliation?.ShiftsOveragePiastres ?? 0) - (Reconciliation?.ShiftsShortagePiastres ?? 0);

    private async Task LoadReconciliationAsync(string businessDayId)
    {
        if (!CanSeeReconciliation) { Reconciliation = null; return; }
        var result = await _session.Api.GetBusinessDayReconciliationAsync(businessDayId, _session.CurrentUser!.Id);
        Reconciliation = result.Success ? result.Value : null;
        OnPropertyChanged(nameof(ReconciliationDifferencePiastres));
    }

    // ---------------- Safe drop (drawer -> main treasury) ----------------

    [ObservableProperty] private bool _isSafeDropOpen;
    [ObservableProperty] private string _safeDropAmountText = "";
    [ObservableProperty] private string _safeDropReason = "";
    [ObservableProperty] private string _safeDropManagerPin = "";
    [ObservableProperty] private string? _safeDropError;
    public bool SafeDropNeedsApproval => !_session.HasPermission("safe_drop");

    /// <summary>The drop is minted server-side against a shift, matching Cashier's own safe-drop -
    /// this device must have an open shift even though the screen itself is business-day scoped.</summary>
    public bool HasOpenShiftForDrop => _session.ShiftId is not null;
    public bool CanOpenSafeDrop => HasBusinessDay && IsActiveDay;

    private string _safeDropRequestId = Guid.NewGuid().ToString("N");
    private readonly RetainedRequest<SafeDropRequest> _safeDropRequest = new("controlcenter.safe-drop");

    [RelayCommand]
    private void OpenSafeDrop()
    {
        if (!CanEditAction || !CanOpenSafeDrop) return;
        SafeDropAmountText = ""; SafeDropReason = ""; SafeDropManagerPin = ""; SafeDropError = null;
        IsSafeDropOpen = true;
        OnPropertyChanged(nameof(SafeDropNeedsApproval));
        OnPropertyChanged(nameof(HasOpenShiftForDrop));
    }

    [RelayCommand]
    private void CloseSafeDrop()
    {
        if (!CanEditAction) return;
        if (_safeDropRequest.HasPending) { SafeDropError = "لم تُحسم نتيجة العملية؛ أعد المحاولة للتحقق قبل الإغلاق."; return; }
        IsSafeDropOpen = false;
    }

    [RelayCommand]
    private async Task SubmitSafeDropAsync()
    {
        if (IsBusy) return;
        if (_safeDropRequest.Value is { } pending) { IsBusy = true; try { await CompleteSafeDropAsync(pending); } finally { IsBusy = false; } return; }
        if (!HasOpenShiftForDrop)
        {
            SafeDropError = "لا توجد وردية مفتوحة على هذا الجهاز - الإيداع الآمن يتطلب وردية مفتوحة.";
            return;
        }
        if (!CashInput.TryParse(SafeDropAmountText, out var amount) || amount <= 0)
        {
            SafeDropError = "أدخل مبلغًا صحيحًا أكبر من صفر.";
            return;
        }
        if (string.IsNullOrWhiteSpace(SafeDropReason))
        {
            SafeDropError = "أدخل سبب الإيداع.";
            return;
        }

        IsBusy = true;
        SafeDropError = null;
        try
        {
            var (approvalId, approverId, ok) = await ResolveApprovalAsync(
                SafeDropNeedsApproval, SafeDropManagerPin, "safe_drop", SafeDropReason, e => SafeDropError = e);
            if (!ok) return;

            await CompleteSafeDropAsync(new SafeDropRequest(
                _safeDropRequestId, _session.ShiftId!, amount, SafeDropReason, _session.CurrentUser!.Id, approvalId, approverId));
        }
        finally { IsBusy = false; }
    }

    private async Task CompleteSafeDropAsync(SafeDropRequest request)
    {
        var result = await _safeDropRequest.SendAsync(request, r => _session.Api.SafeDropAsync(r));
        if (result.Success) _safeDropRequestId = Guid.NewGuid().ToString("N");
        if (!result.Success || result.Value is null)
        {
            SafeDropError = _safeDropRequest.FailureMessage(result.Error);
            return;
        }
        ActionNote = $"تم الإيداع الآمن ({CashInput.Format(result.Value.AmountPiastres)} ج) إلى الخزينة الرئيسية.";
        IsSafeDropOpen = false;
        await ReloadAsync();
    }

    // ---------------- Past close snapshot (closed days only, read-only, never recomputed) ----------------

    [ObservableProperty] private BusinessDayCloseSnapshotDto? _latestSnapshot;
    public bool HasSnapshot => LatestSnapshot is not null;

    public BusinessDayReconciliationDto? SnapshotReconciliation => LatestSnapshot is null ? null
        : System.Text.Json.JsonSerializer.Deserialize<BusinessDayReconciliationDto>(LatestSnapshot.ReconciliationJson);

    private async Task LoadSnapshotAsync(string businessDayId)
    {
        var result = await _session.Api.GetLatestCloseSnapshotAsync(businessDayId);
        LatestSnapshot = result.Success ? result.Value : null;
        OnPropertyChanged(nameof(HasSnapshot));
        OnPropertyChanged(nameof(SnapshotReconciliation));
    }

    private async Task<(string? ApprovalId, string? ApproverUserId, bool Ok)> ResolveApprovalAsync(
        bool needsApproval, string managerPin, string permission, string reason, Action<string> onError)
    {
        if (!needsApproval) return (null, null, true);
        if (managerPin.Length < 4)
        {
            onError("أدخل رمز المدير المخوّل باعتماد هذا الإجراء.");
            return (null, null, false);
        }
        var approval = await _session.Api.IssueManagerApprovalAsync(
            new Falah.Application.Approvals.IssueManagerApprovalRequest(managerPin, permission, reason));
        if (!approval.Success || approval.Value is null)
        {
            onError(approval.Error ?? "رمز المدير غير صحيح أو غير مخوّل باعتماد هذا الإجراء.");
            return (null, null, false);
        }
        return (approval.Value.ApprovalId, approval.Value.ApproverUserId, true);
    }
}

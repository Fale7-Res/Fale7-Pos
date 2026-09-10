using Falah.Client.CashierWinUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.CashDrawer;
using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Application.Treasury;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Everything the new "Cashier Operations" menu covers: Safe Drop, Cash In/Out, Cash Drawer command,
/// Cash Count, and Order Lookup (with Reprint). These are deliberately NOT part of the Current Order
/// panel - they are administrative/operational actions a cashier takes between or around orders, not
/// actions on the order itself, so they live in their own menu (see CashierOperationsView.xaml) per
/// the explicit UX requirement to keep the order panel free of unrelated actions. Expense
/// (CashierViewModel.Expense.cs) and Shift Close (CashierViewModel.ShiftClose.cs) already existed and
/// are exposed from the same menu now instead of the cart header.
/// </summary>
public partial class CashierViewModel
{
    // ---------------- Safe Drop (tre-drawer -> safe, gated on "safe_drop") ----------------

    [ObservableProperty] private bool _isSafeDropOpen;
    [ObservableProperty] private string _safeDropAmountText = "0";
    [ObservableProperty] private string _safeDropReason = "";
    [ObservableProperty] private string _safeDropManagerPin = "";
    [ObservableProperty] private string? _safeDropError;
    [ObservableProperty] private bool _isSubmittingSafeDrop;

    public bool SafeDropNeedsApproval => !_session.HasPermission("safe_drop");

    [RelayCommand]
    private void OpenSafeDrop()
    {
        if (!CanEditOperation) return;
        SafeDropAmountText = "0";
        SafeDropReason = "";
        SafeDropManagerPin = "";
        SafeDropError = null;
        IsSafeDropOpen = true;
        OnPropertyChanged(nameof(SafeDropNeedsApproval));
    }

    [RelayCommand]
    private void CancelSafeDrop() { if (IsOperationBusy) return; if (_pendingSafeDrop is not null) { SafeDropError = "لم تُحسم نتيجة العملية؛ أعد المحاولة للتحقق قبل الإغلاق."; return; } IsSafeDropOpen = false; }

    [RelayCommand]
    private async Task SubmitSafeDropAsync()
    {
        if (IsSubmittingSafeDrop) return;
        if (_pendingSafeDrop is not null) { IsSubmittingSafeDrop = true; try { await CompleteSafeDropAsync(_pendingSafeDrop); } finally { IsSubmittingSafeDrop = false; } return; }
        if (!CashInput.TryParse(SafeDropAmountText, out var amountEgp) || amountEgp <= 0)
        {
            SafeDropError = "أدخل مبلغًا صحيحًا أكبر من صفر.";
            return;
        }
        if (string.IsNullOrWhiteSpace(SafeDropReason))
        {
            SafeDropError = "أدخل سبب الإيداع.";
            return;
        }

        IsSubmittingSafeDrop = true;
        SafeDropError = null;
        try
        {
            if (_pendingSafeDrop is not null) { await CompleteSafeDropAsync(_pendingSafeDrop); return; }

            var (approvalId, approverUserId, ok) = await ResolveManagerApprovalIfNeededAsync(
                SafeDropNeedsApproval, SafeDropManagerPin, "safe_drop", SafeDropReason, e => SafeDropError = e);
            if (!ok) return;

            var request = new SafeDropRequest(
                DropRequestId: _SafeDropRequestId, ShiftId: _session.ShiftId!,
                AmountPiastres: amountEgp, Reason: SafeDropReason,
                CreatedByUserId: _session.CurrentUser!.Id,
                ManagerApprovalId: approvalId, ManagerApproverUserId: approverUserId);
            await CompleteSafeDropAsync(request);
        }
        finally
        {
            IsSubmittingSafeDrop = false;
        }
    }

    // ---------------- Cash In / Cash Out (tre-main <-> tre-drawer, gated on "cash_drawer") ----------------

    [ObservableProperty] private bool _isCashMovementOpen;
    [ObservableProperty] private string _cashMovementType = "cash_in";
    [ObservableProperty] private string _cashMovementAmountText = "0";
    [ObservableProperty] private string _cashMovementReason = "";
    [ObservableProperty] private string _cashMovementManagerPin = "";
    [ObservableProperty] private string? _cashMovementError;
    [ObservableProperty] private bool _isSubmittingCashMovement;

    public bool CashMovementNeedsApproval => !_session.HasPermission("cash_drawer");
    public string CashMovementTitle => CashMovementType == "cash_in" ? "إيداع نقدي في الدرج" : "سحب نقدي من الدرج";

    [RelayCommand]
    private void OpenCashMovement(string movementType)
    {
        if (!CanEditOperation) return;
        CashMovementType = movementType;
        CashMovementAmountText = "0";
        CashMovementReason = "";
        CashMovementManagerPin = "";
        CashMovementError = null;
        IsCashMovementOpen = true;
        OnPropertyChanged(nameof(CashMovementNeedsApproval));
        OnPropertyChanged(nameof(CashMovementTitle));
    }

    [RelayCommand]
    private void CancelCashMovement() { if (IsOperationBusy) return; if (_pendingCashMovement is not null) { CashMovementError = "لم تُحسم نتيجة العملية؛ أعد المحاولة للتحقق قبل الإغلاق."; return; } IsCashMovementOpen = false; }

    [RelayCommand]
    private async Task SubmitCashMovementAsync()
    {
        if (IsSubmittingCashMovement) return;
        if (_pendingCashMovement is not null) { IsSubmittingCashMovement = true; try { await CompleteCashMovementAsync(_pendingCashMovement); } finally { IsSubmittingCashMovement = false; } return; }
        if (!CashInput.TryParse(CashMovementAmountText, out var amountEgp) || amountEgp <= 0)
        {
            CashMovementError = "أدخل مبلغًا صحيحًا أكبر من صفر.";
            return;
        }
        if (string.IsNullOrWhiteSpace(CashMovementReason))
        {
            CashMovementError = "أدخل سبب الحركة.";
            return;
        }

        IsSubmittingCashMovement = true;
        CashMovementError = null;
        try
        {
            if (_pendingCashMovement is not null) { await CompleteCashMovementAsync(_pendingCashMovement); return; }

            var (approvalId, approverUserId, ok) = await ResolveManagerApprovalIfNeededAsync(
                CashMovementNeedsApproval, CashMovementManagerPin, "cash_drawer", CashMovementReason, e => CashMovementError = e);
            if (!ok) return;

            var request = new CashierCashMovementRequest(
                RequestId: _CashMovementRequestId, ShiftId: _session.ShiftId!, MovementType: CashMovementType,
                AmountPiastres: amountEgp, Reason: CashMovementReason,
                ActorUserId: _session.CurrentUser!.Id,
                ManagerApprovalId: approvalId, ManagerApproverUserId: approverUserId);
            await CompleteCashMovementAsync(request);
        }
        finally
        {
            IsSubmittingCashMovement = false;
        }
    }

    // ---------------- Cash Drawer command (hardware boundary, gated on "cash_drawer") ----------------

    [ObservableProperty] private bool _isCashDrawerOpen;
    [ObservableProperty] private string _cashDrawerReason = "فتح يدوي";
    [ObservableProperty] private string _cashDrawerManagerPin = "";
    [ObservableProperty] private string? _cashDrawerError;
    [ObservableProperty] private bool _isSubmittingCashDrawer;

    public bool CashDrawerNeedsApproval => !_session.HasPermission("cash_drawer");

    [RelayCommand]
    private void OpenCashDrawerDialog()
    {
        if (!CanEditOperation) return;
        CashDrawerReason = "فتح يدوي";
        CashDrawerManagerPin = "";
        CashDrawerError = null;
        IsCashDrawerOpen = true;
        OnPropertyChanged(nameof(CashDrawerNeedsApproval));
    }

    [RelayCommand]
    private void CancelCashDrawer() { if (IsOperationBusy) return; if (_pendingCashDrawer is not null) { CashDrawerError = "لم تُحسم نتيجة العملية؛ أعد المحاولة للتحقق قبل الإغلاق."; return; } IsCashDrawerOpen = false; }

    /// <summary>Sends the open command to the server. Whether the physical drawer actually opened is
    /// reported later through ReportCashDrawerResultRequest by the Cashier-machine's own hardware
    /// agent (not built in this pass) - this only records that the command was requested and by
    /// whom, exactly like a print job never claims a paper ticket physically came out.</summary>
    [RelayCommand]
    private async Task SubmitCashDrawerAsync()
    {
        if (IsSubmittingCashDrawer) return;
        if (_pendingCashDrawer is not null) { IsSubmittingCashDrawer = true; try { await CompleteCashDrawerAsync(_pendingCashDrawer); } finally { IsSubmittingCashDrawer = false; } return; }
        if (string.IsNullOrWhiteSpace(CashDrawerReason))
        {
            CashDrawerError = "أدخل سبب فتح الدرج.";
            return;
        }

        IsSubmittingCashDrawer = true;
        CashDrawerError = null;
        try
        {
            if (_pendingCashDrawer is not null) { await CompleteCashDrawerAsync(_pendingCashDrawer); return; }

            var (approvalId, approverUserId, ok) = await ResolveManagerApprovalIfNeededAsync(
                CashDrawerNeedsApproval, CashDrawerManagerPin, "cash_drawer", CashDrawerReason, e => CashDrawerError = e);
            if (!ok) return;

            var request = new OpenCashDrawerRequest(
                RequestId: _CashDrawerRequestId, ShiftId: _session.ShiftId!, Reason: CashDrawerReason,
                ActorUserId: _session.CurrentUser!.Id,
                ManagerApprovalId: approvalId, ManagerApproverUserId: approverUserId);
            await CompleteCashDrawerAsync(request);
        }
        finally
        {
            IsSubmittingCashDrawer = false;
        }
    }

    // ---------------- Cash Count (read-only - the real live tre-drawer balance, nothing computed locally) ----------------

    [ObservableProperty] private bool _isCashCountOpen;
    [ObservableProperty] private long? _cashCountBalancePiastres;
    [ObservableProperty] private string? _cashCountError;
    [ObservableProperty] private bool _isLoadingCashCount;

    [RelayCommand]
    private async Task OpenCashCountAsync()
    {
        if (!CanViewDrawerBalance) return;
        IsCashCountOpen = true;
        IsLoadingCashCount = true;
        CashCountError = null;
        CashCountBalancePiastres = null;
        try
        {
            var result = await _session.Api.GetTreasuryBalanceAsync("tre-drawer");
            if (!result.Success || result.Value is null)
            {
                CashCountError = result.Error ?? "تعذر قراءة رصيد الدرج.";
                return;
            }
            CashCountBalancePiastres = result.Value.BalancePiastres;
        }
        finally
        {
            IsLoadingCashCount = false;
        }
    }

    [RelayCommand]
    private void CloseCashCount() => IsCashCountOpen = false;

    // ---------------- Order Lookup + Reprint ----------------

    [ObservableProperty] private bool _isOrderLookupOpen;
    [ObservableProperty] private string _orderLookupNumber = "";
    [ObservableProperty] private OrderDetailsDto? _orderLookupResult;
    [ObservableProperty] private string? _orderLookupError;
    [ObservableProperty] private bool _isSearchingOrder;
    [ObservableProperty] private string? _reprintStatusMessage;
    [ObservableProperty] private bool _isReprinting;

    [RelayCommand]
    private void OpenOrderLookup()
    {
        if (!CanEditOperation) return;
        OrderLookupNumber = "";
        OrderLookupResult = null;
        OrderLookupError = null;
        ReprintStatusMessage = null;
        IsOrderLookupOpen = true;
    }

    [RelayCommand]
    private void CloseOrderLookup() { if (_pendingReprint is null && !IsOperationBusy) IsOrderLookupOpen = false; }

    [RelayCommand]
    private async Task SearchOrderAsync()
    {
        if (_pendingReprint is not null || IsOperationBusy) return;
        if (string.IsNullOrWhiteSpace(OrderLookupNumber))
        {
            OrderLookupError = "أدخل رقم الطلب.";
            return;
        }

        IsSearchingOrder = true;
        OrderLookupError = null;
        OrderLookupResult = null;
        ReprintStatusMessage = null;
        try
        {
            var result = await _session.Api.LookupOrderByNumberAsync(OrderLookupNumber.Trim());
            if (!result.Success || result.Value is null)
            {
                OrderLookupError = result.Error ?? "لم يتم العثور على الطلب.";
                return;
            }
            OrderLookupResult = result.Value;
        }
        finally
        {
            IsSearchingOrder = false;
        }
    }

    [RelayCommand]
    private async Task ReprintOrderAsync()
    {
        // Reprint deliberately mints a fresh RequestId per press (each press is a legitimate,
        // independent reprint - print jobs are never deduplicated against each other the way a
        // payment is, per CreatePrintJobsRequest's own doc comment). This guard only stops a
        // double-fire of the SAME press from queuing two jobs for one intended click.
        if (IsReprinting) return;
        if (OrderLookupResult is null) return;
        IsReprinting = true;
        ReprintStatusMessage = null;
        try
        {
            var request = _pendingReprint ?? new CreatePrintJobsRequest(
                RequestId: $"rp-{Guid.NewGuid():N}", OrderId: OrderLookupResult.Id, CustomerRoute: "Receipt",
                IsReprint: true, ActorUserId: _session.CurrentUser!.Id);
            var wasUnresolved = _pendingReprint is not null;
            _pendingReprint = request; RefreshOperationState();
            var result = await _session.Api.CreatePrintJobsAsync(request.OrderId, request);
            if (result.Success || (!wasUnresolved && !result.OutcomeUnknown)) _pendingReprint = null;
            RefreshOperationState();
            ReprintStatusMessage = result.Success
                ? "تم تسجيل طلب إعادة الطباعة؛ لم يتم تأكيد خروج الورقة."
                : result.Error ?? "تعذر إرسال أمر إعادة الطباعة.";
        }
        finally
        {
            IsReprinting = false;
        }
    }

    private CreatePrintJobsRequest? _pendingReprint;

    // ---------------- Shared manager-approval resolution ----------------

    /// <summary>Same shape used by Expense and Shift Close: a PIN typed here is NEVER trusted as an
    /// identity by itself - it is exchanged through login-by-pin first, and only THAT resolved
    /// user's approval is what gets attached to the actual command.</summary>
    private async Task<(string? ApprovalId, string? ApproverUserId, bool Ok)> ResolveManagerApprovalIfNeededAsync(
        bool needsApproval, string managerPin, string permission, string reason, Action<string> onError)
    {
        if (!needsApproval) return (null, null, true);

        if (managerPin.Length < 4)
        {
            onError("أدخل رمز المدير المخوّل باعتماد هذا الإجراء.");
            return (null, null, false);
        }

        var managerLogin = await _session.Api.LoginByPinAsync(managerPin);
        if (!managerLogin.Success || managerLogin.Value is null)
        {
            onError(managerLogin.Error ?? "تعذر التحقق من رمز المدير.");
            return (null, null, false);
        }

        var approval = await _session.Api.IssueManagerApprovalAsync(
            new Falah.Application.Approvals.IssueManagerApprovalRequest(managerLogin.Value.Id, permission, reason));
        if (!approval.Success || approval.Value is null)
        {
            onError(approval.Error ?? "هذا المستخدم غير مخول باعتماد هذا الإجراء.");
            return (null, null, false);
        }

        return (approval.Value.ApprovalId, managerLogin.Value.Id, true);
    }
    private SafeDropRequest? _pendingSafeDrop;
    private string _SafeDropRequestId = Guid.NewGuid().ToString("N");
    private async Task CompleteSafeDropAsync(SafeDropRequest request)
    {
        var wasUnresolved = _pendingSafeDrop is not null;
        _pendingSafeDrop = request;
        RefreshOperationState();
        var result = await _session.Api.SafeDropAsync(request);
        if (result.Success || (!wasUnresolved && !result.OutcomeUnknown)) _pendingSafeDrop = null;
        if (result.Success) _SafeDropRequestId = Guid.NewGuid().ToString("N");
        RefreshOperationState();
        if (!result.Success || result.Value is null)
        {
            SafeDropError = result.Error ?? "تعذر تسجيل الإيداع.";
            return;
        }

        IsSafeDropOpen = false;
    }

    private CashierCashMovementRequest? _pendingCashMovement;
    private string _CashMovementRequestId = Guid.NewGuid().ToString("N");
    private async Task CompleteCashMovementAsync(CashierCashMovementRequest request)
    {
        var wasUnresolved = _pendingCashMovement is not null;
        _pendingCashMovement = request;
        RefreshOperationState();
        var result = await _session.Api.CashierCashMovementAsync(request);
        if (result.Success || (!wasUnresolved && !result.OutcomeUnknown)) _pendingCashMovement = null;
        if (result.Success) _CashMovementRequestId = Guid.NewGuid().ToString("N");
        RefreshOperationState();
        if (!result.Success || result.Value is null)
        {
            CashMovementError = result.Error ?? "تعذر تسجيل الحركة النقدية.";
            return;
        }

        IsCashMovementOpen = false;
    }

    private OpenCashDrawerRequest? _pendingCashDrawer;
    private string _CashDrawerRequestId = Guid.NewGuid().ToString("N");
    private async Task CompleteCashDrawerAsync(OpenCashDrawerRequest request)
    {
        var wasUnresolved = _pendingCashDrawer is not null;
        _pendingCashDrawer = request;
        RefreshOperationState();
        var result = await _session.Api.OpenCashDrawerAsync(request);
        if (result.Success || (!wasUnresolved && !result.OutcomeUnknown)) _pendingCashDrawer = null;
        if (result.Success) _CashDrawerRequestId = Guid.NewGuid().ToString("N");
        RefreshOperationState();
        if (!result.Success || result.Value is null)
        {
            CashDrawerError = result.Error ?? "تعذر إرسال أمر فتح الدرج.";
            return;
        }

        IsCashDrawerOpen = false;
    }

}

using CommunityToolkit.Mvvm.Input;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// The three commands an operator can least afford to lose - a sale, a refund, a shift close - are
/// written to disk before they are sent and removed only once their outcome is known, the same
/// way RetainedRequest keeps every other money command on this station.
///
/// They were held in memory only. A power cut or a killed app while one was in flight destroyed the
/// only evidence it had been sent, and the obvious next step - ring it up again, refund again, close
/// again - is exactly the duplicate the request's idempotency key exists to prevent. Recovered on the
/// next login, keyed per operator so two people sharing the till never pick up each other's
/// unfinished work, the request reopens its own screen and blocks new work until it is retried with
/// the same key: the server then completes it or reports that it already happened.
/// </summary>
public partial class CashierViewModel
{
    private const string CheckoutOperation = "checkout";
    private const string RefundOperation = "refund";
    private const string ShiftCloseOperation = "shift-close";
    // The drawer's own money commands, held the same way for the same reason.
    private const string ExpenseOperation = "expense";
    private const string SafeDropOperation = "safe-drop";
    private const string CashMovementOperation = "cash-movement";
    private const string CashDrawerOperation = "cash-drawer";
    // Kept by their own RetainedRequest (not Remember), listed here so a lost answer can be let go.
    private const string ElectronicConfirmOperation = "electronic-confirm";
    private const string ElectronicCancelOperation = "electronic-cancel";

    private string DurableKey(string operation) => $"cashier.{operation}.{_session.CurrentUser?.Id}";

    // ---------------- REC3: a kept request the server refuses ----------------

    /// <summary>Requests that were already pending (recovered after a restart, or retried after an
    /// unknown outcome) and that the server has now definitively refused - the shift was closed
    /// meanwhile, the approval expired, the drawer no longer holds the cash. They stay kept: a refusal
    /// of a RETRY can, rarely, follow the original having been applied with its answer lost, so the
    /// till never drops one on its own. But they used to block the till for good, with no way out
    /// short of deleting a file. The operator, having checked, can now let them go.</summary>
    private readonly Dictionary<string, string> _refusedRetries = new();

    public bool HasRefusedRetry => _refusedRetries.Count > 0;
    public string RefusedRetryMessage => string.Join("\n", _refusedRetries.Values);

    private static string OperationLabel(string operation) => operation switch
    {
        CheckoutOperation => "عملية دفع",
        RefundOperation => "استرداد",
        ShiftCloseOperation => "تقفيل الوردية",
        ExpenseOperation => "مصروف",
        SafeDropOperation => "إيداع بالخزنة",
        CashMovementOperation => "حركة نقدية",
        CashDrawerOperation => "فتح الدرج",
        ElectronicConfirmOperation => "تأكيد تحويل إلكتروني",
        ElectronicCancelOperation => "إلغاء طلب بانتظار التحويل",
        CollectOperation => "تحصيل طلب استلام",
        _ => operation,
    };

    /// <summary>Called after every send of a kept request with its outcome.</summary>
    private void NoteRetryOutcome(string operation, bool wasUnresolved, bool success, bool outcomeUnknown, string? error)
    {
        if (success || !wasUnresolved || outcomeUnknown) _refusedRetries.Remove(operation);
        else _refusedRetries[operation] = $"{OperationLabel(operation)}: {error ?? "رفضها الخادم."}";
        OnPropertyChanged(nameof(HasRefusedRetry));
        OnPropertyChanged(nameof(RefusedRetryMessage));
    }

    /// <summary>Lets go of every kept request the server refused on retry - after the operator has
    /// checked (orders list, drawer) that it did not happen. Only those: a request whose outcome is
    /// still unknown is never dropped here.</summary>
    [RelayCommand]
    private void DiscardRefusedRetries()
    {
        foreach (var operation in _refusedRetries.Keys.ToList())
        {
            switch (operation)
            {
                case CheckoutOperation: _pendingCheckout = null; CheckoutError = null; break;
                case RefundOperation: _pendingRefund = null; RefundError = null; break;
                case ShiftCloseOperation: _pendingCloseShift = null; CloseShiftError = null; break;
                case ExpenseOperation: _pendingExpense = null; ExpenseError = null; break;
                case SafeDropOperation: _pendingSafeDrop = null; SafeDropError = null; break;
                case CashMovementOperation: _pendingCashMovement = null; CashMovementError = null; break;
                case CashDrawerOperation: _pendingCashDrawer = null; CashDrawerError = null; break;
                case CollectOperation: _pendingCollect = null; CollectError = null; break;
                case ElectronicConfirmOperation: _pendingElectronicConfirm.DiscardAfterReview(true); PendingPaymentActionError = null; continue;
                case ElectronicCancelOperation: _pendingElectronicCancel.DiscardAfterReview(true); PendingPaymentActionError = null; continue;
            }
            Remember(operation, null);
        }
        _refusedRetries.Clear();
        OnPropertyChanged(nameof(HasRefusedRetry));
        OnPropertyChanged(nameof(RefusedRetryMessage));
        RefreshOperationState();
    }

    /// <summary>Writes the in-flight request, or clears it once its outcome is known.</summary>
    private void Remember(string operation, object? request)
    {
        if (_session.CurrentUser is null || RetainedRequestStorage.Current is not { } store) return;
        if (request is null) store.Clear(DurableKey(operation));
        else store.Save(DurableKey(operation), request);
    }

    private void RecoverUnresolvedOperations()
    {
        if (_session.CurrentUser is null || RetainedRequestStorage.Current is not { } store) return;

        if (store.Load(DurableKey(CheckoutOperation)) is CheckoutOrderRequest checkout)
        {
            _pendingCheckout = checkout;
            CurrentCheckoutRequestId = checkout.CheckoutRequestId;
            CheckoutError = "عملية دفع سابقة لم يصل تأكيدها قبل إغلاق البرنامج. اضغط دفع لإعادة المحاولة بنفس الطلب والتحقق منه قبل تسجيل أي طلب جديد.";
            IsCheckoutOpen = true;
        }

        if (store.Load(DurableKey(RefundOperation)) is RefundOrderRequest refund)
        {
            _pendingRefund = refund;
            _refundRequestId = refund.RefundRequestId;
            RefundError = "عملية استرداد سابقة لم يصل تأكيدها قبل إغلاق البرنامج. أعد المحاولة بنفس الطلب للتحقق منها.";
            IsRefundOpen = true;
        }

        if (store.Load(DurableKey(ShiftCloseOperation)) is CloseShiftRequest close)
        {
            _pendingCloseShift = close;
            _CloseShiftRequestId = close.CloseRequestId;
            CloseShiftError = "تقفيل وردية سابق لم يصل تأكيده قبل إغلاق البرنامج. أعد المحاولة بنفس الطلب للتحقق منه.";
            IsCloseShiftOpen = true;
        }

        const string unresolved = "عملية سابقة لم يصل تأكيدها قبل إغلاق البرنامج. أعد المحاولة بنفس الطلب للتحقق منها.";

        if (store.Load(DurableKey(ExpenseOperation)) is Falah.Application.Expenses.AddExpenseRequest expense)
        {
            _pendingExpense = expense;
            _ExpenseRequestId = expense.RequestId;
            ExpenseError = unresolved;
            IsAddExpenseOpen = true;
        }

        if (store.Load(DurableKey(SafeDropOperation)) is Falah.Application.Treasury.SafeDropRequest drop)
        {
            _pendingSafeDrop = drop;
            _SafeDropRequestId = drop.DropRequestId;
            SafeDropError = unresolved;
            IsSafeDropOpen = true;
        }

        if (store.Load(DurableKey(CashMovementOperation)) is Falah.Application.CashDrawer.CashierCashMovementRequest movement)
        {
            _pendingCashMovement = movement;
            _CashMovementRequestId = movement.RequestId;
            CashMovementError = unresolved;
            IsCashMovementOpen = true;
        }

        if (store.Load(DurableKey(CashDrawerOperation)) is Falah.Application.CashDrawer.OpenCashDrawerRequest drawer)
        {
            _pendingCashDrawer = drawer;
            _CashDrawerRequestId = drawer.RequestId;
            CashDrawerError = unresolved;
            IsCashDrawerOpen = true;
        }

        RecoverUnresolvedCollection();

        // A transfer confirm/cancel kept from before the restart: its buttons need the checkout
        // result the restart wiped, so it is surfaced here to be checked and let go, never resent alone.
        const string checkTransfer = "لم يصل ردها قبل إغلاق البرنامج. راجع حالة التحويل للطلب من شاشة الطلبات بعد تجاهلها، ونفّذها من جديد لو لزم.";
        if (_pendingElectronicConfirm.HasPending)
            _refusedRetries[ElectronicConfirmOperation] = $"{OperationLabel(ElectronicConfirmOperation)}: {checkTransfer}";
        if (_pendingElectronicCancel.HasPending)
            _refusedRetries[ElectronicCancelOperation] = $"{OperationLabel(ElectronicCancelOperation)}: {checkTransfer}";
        OnPropertyChanged(nameof(HasRefusedRetry));
        OnPropertyChanged(nameof(RefusedRetryMessage));

        RefreshOperationState();
    }
}

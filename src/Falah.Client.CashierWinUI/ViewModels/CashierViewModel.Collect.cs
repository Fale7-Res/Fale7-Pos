using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Orders;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// UI-0025 (owner decision 2026-11-22): a pickup can be registered and sent to the kitchen first, and
/// the customer pays when collecting it. Registering is the pickup details' "pay on collection" switch;
/// collecting is done from Order Lookup (search the number, or pick it from the waiting list), and it is
/// a money command, kept on disk until its outcome is known like a sale or a refund. A pickup nobody
/// came for blocks the day close until a manager cancels it - nothing to refund, only the food to account for.
/// </summary>
public partial class CashierViewModel
{
    private const string CollectOperation = "pickup-collect";

    /// <summary>Set in the pickup details: register now, the customer pays on collection.</summary>
    [ObservableProperty] private bool _payOnCollection;
    public bool IsPayOnCollection => IsPickup && PayOnCollection;
    partial void OnPayOnCollectionChanged(bool value)
    {
        OnPropertyChanged(nameof(IsPayOnCollection));
        OnPropertyChanged(nameof(PickupSummary));
    }

    // ---------------- collecting ----------------

    [ObservableProperty] private string _collectMethod = "cash";
    [ObservableProperty] private string _collectAmountText = "";
    [ObservableProperty] private string? _collectError;
    [ObservableProperty] private bool _isCollecting;
    private CollectPickupPaymentRequest? _pendingCollect;
    private string _collectRequestId = Guid.NewGuid().ToString("N");

    public bool CanCollectPickup => OrderLookupResult is { AwaitingCollection: true };
    public ObservableCollection<AwaitingPickupDto> AwaitingPickups { get; } = new();
    public bool HasAwaitingPickups => AwaitingPickups.Count > 0;

    partial void OnIsCollectingChanged(bool value) => RefreshOperationState();
    partial void OnIsCancellingPickupChanged(bool value) => RefreshOperationState();

    /// <summary>Called when a different order is looked up.</summary>
    private void ResetCollectFor(OrderDetailsDto? order)
    {
        OnPropertyChanged(nameof(CanCollectPickup));
        if (_pendingCollect is not null) return;
        CollectMethod = "cash";
        CollectAmountText = order is { AwaitingCollection: true } ? CashInput.Format(order.TotalPiastres) : "";
        CollectError = null;
        CancelPickupReason = CancelPickupManagerPin = "";
        CancelPickupPrepared = true;
        CancelPickupError = null;
    }

    [RelayCommand]
    private async Task LoadAwaitingPickupsAsync()
    {
        var result = await _session.Api.GetAwaitingPickupsAsync();
        AwaitingPickups.Clear();
        if (result.Success && result.Value is not null) foreach (var row in result.Value) AwaitingPickups.Add(row);
        else OrderLookupError = result.Error ?? "تعذر تحميل طلبات الاستلام المستنية.";
        OnPropertyChanged(nameof(HasAwaitingPickups));
    }

    [RelayCommand]
    private async Task OpenAwaitingPickupAsync(AwaitingPickupDto? pickup)
    {
        if (pickup is null) return;
        OrderLookupNumber = pickup.OrderNumber;
        await SearchOrderAsync();
    }

    [RelayCommand]
    private async Task CollectPickupAsync()
    {
        if (IsCollecting || (OrderLookupResult is null && _pendingCollect is null)) return;
        IsCollecting = true;
        CollectError = null;
        try
        {
            if (_pendingCollect is not null) { await CompleteCollectAsync(_pendingCollect, true); return; }
            if (OrderLookupResult is not { AwaitingCollection: true } order) return;
            if (_session.ShiftId is not { } shiftId) { CollectError = "افتح وردية الأول عشان تحصّل."; return; }
            long amount = order.TotalPiastres;
            if (CollectMethod == "cash" && (!CashInput.TryParse(CollectAmountText, out amount) || amount < order.TotalPiastres))
            { CollectError = "المبلغ المستلم أقل من إجمالي الطلب."; return; }
            var request = new CollectPickupPaymentRequest(_collectRequestId, order.Id, _session.CurrentUser!.Id, shiftId,
                CollectMethod, amount);
            await CompleteCollectAsync(request, false);
        }
        finally { IsCollecting = false; }
    }

    private async Task CompleteCollectAsync(CollectPickupPaymentRequest request, bool wasUnresolved)
    {
        _pendingCollect = request; Remember(CollectOperation, request); RefreshOperationState();
        var result = await _session.Api.CollectPickupAsync(request);
        if (result.Success || (!wasUnresolved && !result.OutcomeUnknown)) { _pendingCollect = null; Remember(CollectOperation, null); }
        NoteRetryOutcome(CollectOperation, wasUnresolved, result.Success, result.OutcomeUnknown, result.Error);
        RefreshOperationState();
        if (!result.Success || result.Value is not { } paid) { CollectError = result.Error; return; }
        _collectRequestId = Guid.NewGuid().ToString("N");
        DrawerCashAlert = paid.DrawerCashAlert;
        if (OrderLookupResult is { } looked && looked.Id == paid.OrderId)
            OrderLookupResult = looked with { IsPaid = paid.IsPaid, PaymentMethod = request.PaymentMethod,
                AmountPaidPiastres = paid.AmountPaidPiastres, ChangeDuePiastres = paid.ChangeDuePiastres,
                ElectronicPaymentStatus = paid.ElectronicPaymentStatus, AwaitingCollection = false };
        var change = paid.ChangeDuePiastres > 0 ? $" - الباقي للعميل {CashInput.Display(paid.ChangeDuePiastres)} ج" : "";
        ReprintStatusMessage = paid.ElectronicPaymentStatus == "pending"
            ? $"اتسجل التحويل على الطلب {paid.OrderNumber}؛ أكّده لما يوصل."
            : $"تم تحصيل الطلب {paid.OrderNumber}{change}" + (paid.PrintJobsQueued ? "" : " (الإيصال ما اتبعتش للطباعة - اطبعه من هنا)");
        if (AwaitingPickups.FirstOrDefault(p => p.OrderId == paid.OrderId) is { } row) { AwaitingPickups.Remove(row); OnPropertyChanged(nameof(HasAwaitingPickups)); }
    }

    // ---------------- a manager cancels a pickup nobody came for ----------------

    [ObservableProperty] private string _cancelPickupReason = "";
    /// <summary>True = the food was made and is wasted; false = never made, back to stock.</summary>
    [ObservableProperty] private bool _cancelPickupPrepared = true;
    [ObservableProperty] private string _cancelPickupManagerPin = "";
    [ObservableProperty] private string? _cancelPickupError;
    [ObservableProperty] private bool _isCancellingPickup;
    private string _cancelPickupRequestId = Guid.NewGuid().ToString("N");
    /// <summary>Owner, 2026-11-23: always a manager - a manager at the till, or a manager's PIN.</summary>
    public bool CancelPickupNeedsApproval => !_session.CanApproveOwn("pickup_cancel");

    [RelayCommand]
    private async Task CancelUncollectedPickupAsync()
    {
        if (IsCancellingPickup || OrderLookupResult is not { AwaitingCollection: true } order) return;
        if (string.IsNullOrWhiteSpace(CancelPickupReason)) { CancelPickupError = "اكتب سبب الإلغاء."; return; }
        IsCancellingPickup = true;
        CancelPickupError = null;
        try
        {
            var (approvalId, approver, ok) = await ResolveManagerApprovalIfNeededAsync(CancelPickupNeedsApproval,
                CancelPickupManagerPin, "pickup_cancel", CancelPickupReason, e => CancelPickupError = e);
            if (!ok) return;
            var result = await _session.Api.CancelUncollectedPickupAsync(new CancelUncollectedPickupRequest(
                _cancelPickupRequestId, order.Id, CancelPickupReason.Trim(), CancelPickupPrepared ? "prepared" : "not_prepared",
                _session.CurrentUser!.Id, approvalId, approver));
            if (!result.Success) { CancelPickupError = result.Error; return; }
            _cancelPickupRequestId = Guid.NewGuid().ToString("N");
            OrderLookupResult = order with { Status = "cancelled", AwaitingCollection = false };
            ReprintStatusMessage = $"اتلغى الطلب {order.OrderNumber}" + (CancelPickupPrepared ? " والأكل اتسجل هالك." : " والمكونات رجعت للمخزون.");
            CancelPickupManagerPin = "";
            if (AwaitingPickups.FirstOrDefault(p => p.OrderId == order.Id) is { } row) { AwaitingPickups.Remove(row); OnPropertyChanged(nameof(HasAwaitingPickups)); }
        }
        finally { IsCancellingPickup = false; }
    }

    private void RecoverUnresolvedCollection()
    {
        if (_session.CurrentUser is null || RetainedRequestStorage.Current is not { } store) return;
        if (store.Load(DurableKey(CollectOperation)) is CollectPickupPaymentRequest collect)
        {
            _pendingCollect = collect;
            _collectRequestId = collect.RequestId;
            CollectError = "تحصيل طلب استلام سابق ما وصلش تأكيده قبل إغلاق البرنامج. اضغط تحصيل لإعادة المحاولة بنفس العملية.";
            IsOrderLookupOpen = true;
        }
    }
}

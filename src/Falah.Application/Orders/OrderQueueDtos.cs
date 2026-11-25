namespace Falah.Application.Orders;

/// <summary>
/// Workflow redesign (owner decision 2026-11-24): one delivery or pickup in the live order queue, with where it is now
/// and what happens next - all of it derived on the server from the order's real records (the order row, its delivery
/// row, its print jobs), never a status the station keeps for itself. See OrderQueueQueryService for the mapping.
/// </summary>
/// <param name="OrderType">"delivery" or "pickup".</param>
/// <param name="PaymentState">"cash_on_delivery" (the driver collects), "pay_at_counter" (an unpaid pickup), "pending_transfer"
/// (InstaPay/wallet not confirmed yet), "paid".</param>
/// <param name="Stage">Delivery: new, preparing, ready, out, delivered, failed, returned. Pickup: new, preparing, completed.</param>
/// <param name="NextAction">print_kitchen, mark_ready, confirm_transfer, assign, record_result, settle_driver, collect, hand_over - or null.</param>
/// <param name="CancelNeedsAdmin">The kitchen already has its paper: a manager authorises and the operator says whether the food was prepared.</param>
/// <param name="NeedsRefund">A paid delivery that failed or came back and still holds the customer's money.</param>
public sealed record QueueOrderDto(
    string OrderId, string OrderNumber, string OrderType,
    string? CustomerName, string? CustomerPhone, string? DeliveryArea, string? DeliveryAddress,
    long TotalPiastres, string PaymentMethod, string PaymentState,
    string Stage, string StageLabel, string? NextAction, string? NextActionLabel,
    bool CanCancel, bool CancelNeedsAdmin, bool KitchenPrinted,
    string? DeliveryId, string? DeliveryStatus, string? DriverId, string? DriverName, long AmountToCollectPiastres,
    bool NeedsRefund, string? Attention,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? RequestedPickupAtUtc, DateTimeOffset? HandedOverAtUtc,
    string? ElectronicPaymentStatus, bool AwaitingCollection,
    /// <summary>A second, optional step beside the main one - today only "print_kitchen", offered while the kitchen
    /// paper has not reached the printer. It never holds the order back: the main step stays available.</summary>
    string? SecondaryAction = null, string? SecondaryActionLabel = null);

/// <summary>Sends the kitchen papers of an order whose kitchen invoice never reached the printer (printing was off,
/// failed, or is still waiting): the missing first papers are queued and a failed one is sent again. StationRoute is
/// the customer-copy route of the station asking ("Receipt" at a till, "CallCenter" at the call center).</summary>
public sealed record SendKitchenInvoiceRequest(string RequestId, string OrderId, string ActorUserId, string StationRoute);

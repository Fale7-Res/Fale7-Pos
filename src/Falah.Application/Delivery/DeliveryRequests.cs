namespace Falah.Application.Delivery;

public sealed record CreateDriverRequest(string RequestId, string Name, string CreatedByUserId);
public sealed record DriverDto(string Id, string Name, bool Active);

/// <summary>Ported from setDriverActive in lib/enterprise-store.tsx - a real gap found during the
/// legacy-parity audit: there was a way to create a driver but no way to deactivate one. Legacy also
/// refuses reactivating an employee-linked driver whose employee is no longer eligible
/// (assertDriverAssignable) - not applicable yet since standalone drivers (EmployeeId always null)
/// are this system's only kind until the Employees/HR feature exists (see CreateDriverHandler's own
/// doc comment).</summary>
public sealed record SetDriverActiveRequest(string DriverId, bool Active, string ActorUserId);

public sealed record MarkDeliveryReadyRequest(string DeliveryId, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

/// <summary>Ported from assignDelivery - the modern per-order path: assignment IS physical receipt,
/// atomically ready->out_for_delivery, strictly per-order (no global "current driver"). RequestId is
/// minted once per submission; the assignment event's own id IS the idempotency key, matching the
/// reference app's `delivery-assignment-{requestId}` exactly.</summary>
public sealed record AssignDeliveryRequest(
    string RequestId, string DeliveryId, string DriverId, string ActorUserId,
    string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record AssignDeliveryResult(
    string DeliveryId, string Status, string DriverId, string DriverName, string TripId, bool WasReplayed);

/// <summary>Ported from reassignDelivery. Same driver -> no-op (not an error). Blocked once
/// settled/cancelled. Needs delivery_override instead of delivery_manage once the outcome (delivered/
/// failed/returned) is already recorded - see AuthorizationPolicy... this distinction is resolved
/// server-side from the delivery's current status, never trusted from the client.</summary>
public sealed record ReassignDeliveryRequest(
    string RequestId, string DeliveryId, string NewDriverId, string Reason, string ActorUserId,
    string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record ReassignDeliveryResult(string DeliveryId, string DriverId, string DriverName, bool WasReplayed, bool WasNoOp);

/// <summary>Ported from recordDeliveryResult. Result is one of delivered/failed/returned.
/// CollectedAmountPiastres only applies to 'delivered' (defaults to the expected amount if omitted -
/// matching the reference app's `collectedAmountPiastres ?? expected`). Reason is required for
/// 'returned' always, for 'delivered' only when the collected amount differs from expected, and for
/// 'failed' must be one of a fixed set of codes. RequestId adds a proper idempotency key on top of
/// the reference app's weaker "already in that status" no-op check - see the handler's own doc
/// comment for why.</summary>
public sealed record RecordDeliveryResultRequest(
    string RequestId, string DeliveryId, string Result, long? CollectedAmountPiastres, string? Reason,
    string? Notes, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record RecordDeliveryResultResult(string DeliveryId, string Status, bool WasReplayed);

/// <summary>Ported from settleDriverCustody - driver-centric, settles ALL of a driver's currently
/// unsettled/resolved deliveries in one pass. Refuses (all-or-nothing) if any of the driver's orders
/// is still out_for_delivery. A nonzero difference needs a reason AND delivery_settlement_approve on
/// top of delivery_manage - a second, stacked authorization gate.</summary>
public sealed record SettleDriverCustodyRequest(
    string RequestId, string DriverId, long ActualCashPiastres, string? Reason, string ActorUserId,
    string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record SettleDriverCustodyResult(
    string SettlementId, string DriverId, long ExpectedPiastres, long ActualPiastres, long DifferencePiastres,
    int OrderCount, string? TreasuryMovementId, bool WasReplayed);

/// <summary>One row of the dispatch board.
///
/// The board used to carry the delivery's own state and the money, and nothing about where the food
/// was going: no customer, no area, no address, no phone. A dispatcher assigning a driver had to
/// open each order separately to find out which side of town it was on.
///
/// <paramref name="ElectronicPaymentStatus"/> is "Pending" while a transfer the customer says they
/// sent has not been confirmed. That order's money is not settled and not cash-on-delivery either -
/// the board has to say so before somebody hands it to a driver.</summary>
public sealed record DeliveryDto(
    string DeliveryId, string OrderId, string? OrderNumber, string Status, string? DriverId, string? DriverName,
    long AmountToCollectPiastres, long OrderTotalPiastres, string PaymentMethod, bool PaymentAlreadySettled,
    string? ShiftId, DateTimeOffset CreatedAtUtc,
    string? CustomerName = null, string? CustomerPhone = null, string? DeliveryArea = null,
    string? DeliveryAddress = null, string? ElectronicPaymentStatus = null);

public sealed record DeliveryEventDto(
    string Type, string? Reason, long? AmountPiastres, string Actor, string? Approver, DateTimeOffset CreatedAtUtc);

/// <summary>Ported from driverOperationalStatus in lib/delivery-domain.mjs - a real gap found
/// during the legacy-parity audit, needed once a dispatch/settlement UI reads it. This system's
/// per-order model has no separate "trip" object, so the legacy trip.status checks ('out'/
/// 'returned') collapse onto OutForDelivery/Returned deliveries directly.</summary>
public enum DriverOperationalStatus { Available, Out, Returning, Assigned, SettlementPending }

public sealed record BlockingOrderDto(string DeliveryId, string OrderId, string OrderNumber, string Status);

/// <summary>One driver's figures over a date range - "today / last 7 days / month / custom period"
/// per the final pre-go-live business rule. Every field is derived from the same Deliveries/
/// DriverCustodies/DriverSettlements rows GetDriverAccountAsync and GetDeliveryOutcomesReportAsync
/// already read - no invented metric. CurrentCustodyPiastres and LastSettlementAtUtc are
/// deliberately NOT range-scoped ("current"/"last" mean now, not "within the period"), matching the
/// same all-time convention treasury balances and running employee balances already use elsewhere.</summary>
public sealed record DriverStatisticsDto(
    string DriverId, string DriverName,
    int AssignedCount, int DeliveredCount, int FailedCount, int ReturnedCount,
    long OrderValuePiastres, long CashCollectedPiastres, long CurrentCustodyPiastres,
    int SettlementCount, long SettlementShortagesPiastres, long SettlementOveragesPiastres,
    long? AverageDeliveryMinutes, DateTimeOffset? LastOrderAtUtc, DateTimeOffset? LastSettlementAtUtc);

/// <summary>Ported from driverAccountView in lib/delivery-domain.mjs - the full owner/dispatcher
/// account view for one driver: current in-flight/unsettled position plus all-time settlement
/// totals. A real gap found during the legacy-parity audit: the underlying rows (deliveries,
/// custodies, settlements) all already existed in EF, only this read-model aggregate was missing.
/// Transfer history (transfersIn/transfersOut from reassignments) is deliberately not included here
/// - a lower-value list view than the settlement-blocking figures above it, left for the dispatch
/// UI slice to add if actually needed.</summary>
public sealed record DriverAccountDto(
    string DriverId, DriverOperationalStatus OperationalStatus,
    /// <summary>The same status in words. The enum serialises as a bare integer, so a supervisor
    /// screen binding straight to it showed "0" - a number that means nothing to the person reading
    /// it. The label travels with the value rather than being re-derived in each client.</summary>
    string OperationalStatusLabel,
    int ActiveOrderCount, int CompletedUnsettledCount, int FailedOrReturnedUnsettledCount,
    long GrossWorkValuePiastres, long PrepaidCompletedPiastres, long CashCustodyPiastres, bool CanSettle,
    IReadOnlyList<BlockingOrderDto> BlockingOrders,
    int AllTimeSettledOrderCount, long AllTimeSettledCashPiastres, DateTimeOffset? LastSettlementAtUtc);

public sealed class DeliveryConflictException : Exception
{
    public DeliveryConflictException(string message) : base(message) { }
}

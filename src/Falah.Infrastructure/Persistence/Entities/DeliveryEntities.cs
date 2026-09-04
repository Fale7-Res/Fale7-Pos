namespace Falah.Infrastructure.Persistence.Entities;

/// <summary>Ported from the Driver shape in the reference app. EmployeeId stays null in this slice -
/// there is no Employees/HR feature in this system yet, and the domain's own AssertDriverAssignable
/// already handles that gracefully ("a driver with no linked employee is judged on its own Active
/// flag alone"), so this is not a gap invented here.</summary>
/// <summary>Ported from DeliveryArea in enterprise-types.ts - the management-side catalog behind
/// Order's own frozen DeliveryServiceId/DeliveryServiceName snapshot fields. Never hard-deleted;
/// Active=false retires a service from the picker for new orders only.</summary>
public class DeliveryServiceEntity
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public long? FeePiastres { get; set; }
    public bool Active { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class DriverEntity
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool Active { get; set; }
    public string? EmployeeId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>One row per delivery-type order - created automatically at checkout (see
/// CheckoutOrderHandler), never by a separate "register delivery" call, since every delivery order
/// in this system is created through the same checkout path. Falah.Domain.Delivery.DeliveryFacts
/// (the pure-function read-model) is composed from this row + its Order at query time, exactly as
/// that record's own doc comment instructs - no financial fields are duplicated here.
/// DriverNameAtHandover is a deliberate snapshot, re-frozen on every (re)assignment - a later driver
/// rename must never rewrite what a completed delivery's receipt already showed.</summary>
public class DeliveryEntity
{
    public string Id { get; set; } = default!;
    public string OrderId { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? ShiftId { get; set; }
    public string? DriverId { get; set; }
    public string? DriverNameAtHandover { get; set; }
    public string? TripId { get; set; }
    public string? AssignedBy { get; set; }
    public DateTimeOffset? AssignedAtUtc { get; set; }
    public DateTimeOffset? OutForDeliveryAtUtc { get; set; }
    public DateTimeOffset? DeliveredAtUtc { get; set; }
    public long? CollectedAmountPiastres { get; set; }
    public DateTimeOffset? FailedAtUtc { get; set; }
    public string? FailureReason { get; set; }
    public string? FailureNotes { get; set; }
    public DateTimeOffset? ReturnedAtUtc { get; set; }
    public string? ReturnReason { get; set; }
    public DateTimeOffset? SettledAtUtc { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    /// <summary>Optimistic concurrency token - same pattern as ShiftEntity/OrderEntity: an assign/
    /// reassign/record-result/settle call that mutates an existing row increments this.</summary>
    public uint Version { get; set; }
}

/// <summary>Ported from DriverCustody. One row per delivery that actually has money at stake -
/// CreateCashCustody returns null (no row) for a prepaid/fully-comped delivery, exactly like the
/// domain function this mirrors. Reassignment relabels DriverId on this SAME row - see
/// DeliveryRules.ReassignCustody's own doc comment for why a second row must never be created.</summary>
public class DriverCustodyEntity
{
    public string Id { get; set; } = default!;
    public string TripId { get; set; } = default!;
    public string DriverId { get; set; } = default!;
    public string OrderId { get; set; } = default!;
    public string DeliveryId { get; set; } = default!;
    public long ExpectedPiastres { get; set; }
    public long CollectedPiastres { get; set; }
    public long SettledPiastres { get; set; }
    public long DifferencePiastres { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? SettledAtUtc { get; set; }
}

/// <summary>The transfer-history table - ported from DeliveryReassignment. Id doubles as the
/// idempotency key (delivery-reassignment-{requestId}), matching the reference app's own dedupe key
/// exactly.</summary>
public class DeliveryReassignmentEntity
{
    public string Id { get; set; } = default!;
    public string OrderId { get; set; } = default!;
    public string DeliveryId { get; set; } = default!;
    public string? OldDriverId { get; set; }
    public string? OldDriverName { get; set; }
    public string NewDriverId { get; set; } = default!;
    public string NewDriverName { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public string ChangedBy { get; set; } = default!;
    public DateTimeOffset ChangedAtUtc { get; set; }
}

/// <summary>The append-only per-order timeline ("سجل الطلب") - every assignment, reassignment,
/// outcome, and settlement touch writes one row here, fully attributable (actor/approver/reason/
/// amount), exactly like the reference app's DeliveryEvent.</summary>
public class DeliveryEventEntity
{
    public string Id { get; set; } = default!;
    public string DeliveryId { get; set; } = default!;
    public string OrderId { get; set; } = default!;
    public string? DriverId { get; set; }
    public string? TripId { get; set; }
    public string Type { get; set; } = default!;
    public string? Reason { get; set; }
    public long? AmountPiastres { get; set; }
    public string Actor { get; set; } = default!;
    public string? Approver { get; set; }
    public string? OperationId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>Driver-centric settlement - ported from settleDriverCustody, the modern per-order-dispatch
/// equivalent of the older manually-batched trip settlement (which this system does not implement -
/// there is no legacy in-flight data to stay compatible with). IdempotencyKey is
/// driver-settlement-{requestId}.</summary>
public class DriverSettlementEntity
{
    public string Id { get; set; } = default!;
    public string DriverId { get; set; } = default!;
    public string? DriverName { get; set; }
    public string OrderIdsJson { get; set; } = default!;
    public int OrderCount { get; set; }
    public long GrossOrderPiastres { get; set; }
    public long PrepaidPiastres { get; set; }
    public long ExpectedPiastres { get; set; }
    public long ActualPiastres { get; set; }
    public long DifferencePiastres { get; set; }
    public string? Reason { get; set; }
    public string? ApprovedBy { get; set; }
    public string OperationId { get; set; } = default!;
    public string IdempotencyKey { get; set; } = default!;
    public string? TreasuryMovementId { get; set; }
    public string ReceivedBy { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

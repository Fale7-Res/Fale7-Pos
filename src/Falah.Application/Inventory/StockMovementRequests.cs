namespace Falah.Application.Inventory;

/// <summary>Physical counts send CountedTotalMilliUnits: the server derives the adjustment against
/// the current balance under the stock lock. Legacy delta callers retain Direction/QuantityMilliUnits.
/// The request id makes both forms replay-safe, including a zero-net physical observation.</summary>
public sealed record RecordCountAdjustmentRequest(
    string RequestId, string InventoryItemId, long QuantityMilliUnits, string Direction, string Reason,
    string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId, long? CountedTotalMilliUnits = null);

public sealed record RecordCountAdjustmentResult(string MovementId, string InventoryItemId, bool WasReplayed);

/// <summary>Ported from recordInventoryWaste. LossType is one of Waste/Spoilage/Breakage - three
/// distinct corrective actions (prep error / storage-expiry / handling accident), never merged into
/// one bucket. Loss value is valued at the LAST PURCHASE unit cost and frozen at posting time.</summary>
public sealed record RecordWasteRequest(
    string RequestId, string InventoryItemId, long QuantityMilliUnits, string LossType, string Reason,
    string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record RecordWasteResult(
    string MovementId, string InventoryItemId, long? UnitCostPiastres, long? LossValuePiastres, bool WasReplayed);

public sealed record StockMovementDto(
    string InventoryItemId, string Type, string Direction, long QuantityMilliUnits, long? UnitCostPiastres,
    long? LossValuePiastres, string ReferenceType, string ReferenceId, string Reason, string CreatedBy,
    string? ApprovedBy, DateTimeOffset CreatedAtUtc);

public sealed class InventoryConflictException : Exception
{
    public InventoryConflictException(string message) : base(message) { }
}

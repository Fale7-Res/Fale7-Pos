namespace Falah.Application.Orders;

/// <summary>FIN-0051 (owner decision 2026-11-22): cancel a cash-on-delivery order before it is handed
/// to a driver. Needs delivery_override, or a manager's approval issued for that action; a reason; and
/// what happened to the food (StockDisposition "prepared" = wasted, "not_prepared" = back to stock).
/// RequestId replays.</summary>
public sealed record CancelUndispatchedDeliveryRequest(
    string RequestId, string OrderId, string Reason, string ActorUserId, string? StockDisposition,
    string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record CancelUndispatchedDeliveryResult(string OrderId, string OrderStatus, string DeliveryStatus, bool WasReplayed);

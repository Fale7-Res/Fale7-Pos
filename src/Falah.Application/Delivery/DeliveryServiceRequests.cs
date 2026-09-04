namespace Falah.Application.Delivery;

/// <summary>Ported from addDeliveryArea/updateDeliveryArea in lib/enterprise-store.tsx - a real
/// gap found during the legacy-parity audit: Order already carries DeliveryServiceId/
/// DeliveryServiceName snapshot fields (row 1 of the implementation matrix), but there was no
/// management surface to create/edit/retire the delivery services themselves at all. Editing name
/// or fee never touches a historical order - every order already froze the service name/fee it
/// was actually sold with; Active=false is the only way to retire one (never a hard delete), same
/// soft-archive idiom as Categories/Products/Drivers.</summary>
public sealed record CreateDeliveryServiceRequest(string RequestId, string Name, long? FeePiastres, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record UpdateDeliveryServiceRequest(
    string ServiceId, string? Name, long? FeePiastres, bool? Active, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record DeliveryServiceDto(string Id, string Name, long? FeePiastres, bool Active, string CreatedBy, DateTimeOffset CreatedAtUtc);

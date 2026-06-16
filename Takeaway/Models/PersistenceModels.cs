using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fale7_POS
{
    // ──────────────────────────────────────────────────────────────────────
    // [PHASE 7.5 PURE CLOUD] Removed all offline queue state DTOs:
    //   PosOfflineOrderQueueState, PosOfflineOrderQueueItem,
    //   PendingDeliveryAddressDeleteQueueState, PendingDeliveryAddressDeleteQueueItem,
    //   PosShiftMigrationQueueState
    // Only the Cloud INSERT payload DTOs and Idempotent RPC request DTOs remain.
    // ──────────────────────────────────────────────────────────────────────

    [DataContract]
    public sealed class AppOrderMutationQueueState
    {
        [DataMember(Name = "version", EmitDefaultValue = false)] public int Version { get; set; }
        [DataMember(Name = "items")] public List<AppOrderMutationQueueItem> Items { get; set; }
    }

    [DataContract]
    public sealed class AppOrderMutationQueueItem
    {
        [DataMember(Name = "queue_id", EmitDefaultValue = false)] public string QueueId { get; set; }
        [DataMember(Name = "order_id", EmitDefaultValue = false)] public string OrderId { get; set; }
        [DataMember(Name = "operation_kind", EmitDefaultValue = false)] public string OperationKind { get; set; }
        [DataMember(Name = "created_at_millis", EmitDefaultValue = false)] public long CreatedAtMillis { get; set; }
        [DataMember(Name = "actor_role", EmitDefaultValue = false)] public string ActorRole { get; set; }
        [DataMember(Name = "actor_user_id", EmitDefaultValue = false)] public string ActorUserId { get; set; }
        [DataMember(Name = "to_status", EmitDefaultValue = false)] public string ToStatus { get; set; }
        [DataMember(Name = "mark_kitchen_printed", EmitDefaultValue = false)] public bool? MarkKitchenPrinted { get; set; }
        [DataMember(Name = "mark_driver_receipt_printed", EmitDefaultValue = false)] public bool? MarkDriverReceiptPrinted { get; set; }
        [DataMember(Name = "mark_partner_receipt_printed", EmitDefaultValue = false)] public bool? MarkPartnerReceiptPrinted { get; set; }
        [DataMember(Name = "customer_user_id", EmitDefaultValue = false)] public string CustomerUserId { get; set; }
        [DataMember(Name = "customer_phone", EmitDefaultValue = false)] public string CustomerPhone { get; set; }
        [DataMember(Name = "address_id", EmitDefaultValue = false)] public string AddressId { get; set; }
        [DataMember(Name = "district", EmitDefaultValue = false)] public string District { get; set; }
        [DataMember(Name = "district_id", EmitDefaultValue = false)] public string DistrictId { get; set; }
        [DataMember(Name = "address_text", EmitDefaultValue = false)] public string AddressText { get; set; }
        [DataMember(Name = "delivery_fee", EmitDefaultValue = false)] public int DeliveryFee { get; set; }
        [DataMember(Name = "address_block", EmitDefaultValue = false)] public string AddressBlock { get; set; }
        [DataMember(Name = "address_street", EmitDefaultValue = false)] public string AddressStreet { get; set; }
        [DataMember(Name = "address_building", EmitDefaultValue = false)] public string AddressBuilding { get; set; }
        [DataMember(Name = "address_apartment", EmitDefaultValue = false)] public string AddressApartment { get; set; }
        [DataMember(Name = "address_floor", EmitDefaultValue = false)] public string AddressFloor { get; set; }
        [DataMember(Name = "address_note", EmitDefaultValue = false)] public string AddressNote { get; set; }
        [DataMember(Name = "driver_user_id", EmitDefaultValue = false)] public string DriverUserId { get; set; }
        [DataMember(Name = "driver_name", EmitDefaultValue = false)] public string DriverName { get; set; }
        [DataMember(Name = "driver_phone", EmitDefaultValue = false)] public string DriverPhone { get; set; }
    }

    [DataContract]
    public sealed class PosOfflineSupabaseOrderInsertRow
    {
        [DataMember(Name = "id")] public string Id { get; set; }
        [DataMember(Name = "idempotency_key", EmitDefaultValue = false)] public string IdempotencyKey { get; set; }
        [DataMember(Name = "order_type")] public string OrderType { get; set; }
        [DataMember(Name = "status")] public string Status { get; set; }
        [DataMember(Name = "customer_user_id", EmitDefaultValue = false)] public string CustomerUserId { get; set; }
        [DataMember(Name = "customer_name", EmitDefaultValue = false)] public string CustomerName { get; set; }
        [DataMember(Name = "customer_phone", EmitDefaultValue = false)] public string CustomerPhone { get; set; }
        [DataMember(Name = "district", EmitDefaultValue = false)] public string District { get; set; }
        [DataMember(Name = "address_text", EmitDefaultValue = false)] public string AddressText { get; set; }
        [DataMember(Name = "pos_address_id", EmitDefaultValue = false)] public string PosAddressId { get; set; }
        [DataMember(Name = "mobile_address_id", EmitDefaultValue = false)] public string MobileAddressId { get; set; }
        [DataMember(Name = "driver_user_id", EmitDefaultValue = false)] public string DriverUserId { get; set; }
        [DataMember(Name = "subtotal")] public double Subtotal { get; set; }
        [DataMember(Name = "delivery_fee")] public double DeliveryFee { get; set; }
        [DataMember(Name = "total")] public double Total { get; set; }
        [DataMember(Name = "created_at", EmitDefaultValue = false)] public string CreatedAt { get; set; }
        [DataMember(Name = "updated_at", EmitDefaultValue = false)] public string UpdatedAt { get; set; }
        [DataMember(Name = "pos_shift_id", EmitDefaultValue = false)] public string PosShiftId { get; set; }
        [DataMember(Name = "metadata", EmitDefaultValue = false)] public PosOfflineOrderMetadata Metadata { get; set; }
    }

    [DataContract]
    public sealed class SupabasePosCreateOrderIdempotentRequest
    {
        [DataMember(Name = "p_idempotency_key")] public string IdempotencyKey { get; set; }
        [DataMember(Name = "p_order_payload")] public PosOfflineSupabaseOrderInsertRow Payload { get; set; }
    }

    [DataContract]
    public sealed class SupabasePosUpdateOrderStatusIdempotentRequest
    {
        [DataMember(Name = "p_order_id")] public string OrderId { get; set; }
        [DataMember(Name = "p_to_status")] public string ToStatus { get; set; }
        [DataMember(Name = "p_idempotency_key")] public string IdempotencyKey { get; set; }
        [DataMember(Name = "p_actor_role", EmitDefaultValue = false)] public string ActorRole { get; set; }
        [DataMember(Name = "p_actor_user_id", EmitDefaultValue = false)] public string ActorUserId { get; set; }
    }

    [DataContract]
    public sealed class SupabasePosAssignDriverIdempotentRequest
    {
        [DataMember(Name = "p_order_id")] public string OrderId { get; set; }
        [DataMember(Name = "p_idempotency_key")] public string IdempotencyKey { get; set; }
        [DataMember(Name = "p_driver_user_id", EmitDefaultValue = false)] public string DriverUserId { get; set; }
        [DataMember(Name = "p_driver_name", EmitDefaultValue = false)] public string DriverName { get; set; }
    }

    [DataContract]
    public sealed class SupabaseCreateInventoryOrderRpcResponse
    {
        [DataMember(Name = "ok", EmitDefaultValue = false)] public bool Ok { get; set; }
        [DataMember(Name = "orderId", EmitDefaultValue = false)] public string OrderId { get; set; }
        [DataMember(Name = "code", EmitDefaultValue = false)] public string Code { get; set; }
        [DataMember(Name = "error", EmitDefaultValue = false)] public string Error { get; set; }
    }

    [DataContract]
    public sealed class PosOfflineOrderMetadata
    {
        [DataMember(Name = "source", EmitDefaultValue = false)] public string Source { get; set; }
        [DataMember(Name = "localOrderId", EmitDefaultValue = false)] public string LocalOrderId { get; set; }
        [DataMember(Name = "localOrderNo", EmitDefaultValue = false)] public int LocalOrderNo { get; set; }
        [DataMember(Name = "localCreatedAtMillis", EmitDefaultValue = false)] public long LocalCreatedAtMillis { get; set; }
        [DataMember(Name = "finalizedAtMillis", EmitDefaultValue = false)] public long FinalizedAtMillis { get; set; }
        [DataMember(Name = "paymentMethod", EmitDefaultValue = false)] public string PaymentMethod { get; set; }
        [DataMember(Name = "posOrderType", EmitDefaultValue = false)] public string PosOrderType { get; set; }
        [DataMember(Name = "driverName", EmitDefaultValue = false)] public string DriverName { get; set; }
        [DataMember(Name = "driverUserId", EmitDefaultValue = false)] public string DriverUserId { get; set; }
        [DataMember(Name = "driverPhone", EmitDefaultValue = false)] public string DriverPhone { get; set; }
        [DataMember(Name = "cashierUserId", EmitDefaultValue = false)] public string CashierUserId { get; set; }
        [DataMember(Name = "cashierUsername", EmitDefaultValue = false)] public string CashierUsername { get; set; }
        [DataMember(Name = "cashierRole", EmitDefaultValue = false)] public string CashierRole { get; set; }
        [DataMember(Name = "items", EmitDefaultValue = false)] public List<PosOfflineOrderMetadataItem> Items { get; set; }
    }

    [DataContract]
    public sealed class PosOfflineOrderMetadataItem
    {
        [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }
        [DataMember(Name = "productId", EmitDefaultValue = false)] public string ProductId { get; set; }
        [DataMember(Name = "sourceProductId", EmitDefaultValue = false)] public string SourceProductId { get; set; }
        [DataMember(Name = "qty", EmitDefaultValue = false)] public int Qty { get; set; }
        [DataMember(Name = "price", EmitDefaultValue = false)] public int Price { get; set; }
        [DataMember(Name = "notes", EmitDefaultValue = false)] public string Notes { get; set; }
        [DataMember(Name = "optionIds", EmitDefaultValue = false)] public List<string> OptionIds { get; set; }
    }
}

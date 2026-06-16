using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fale7_POS
{
    [DataContract]
    public sealed class IdempotencyAwareQueueItem
    {
        [DataMember(Name = "idempotency_key", EmitDefaultValue = false)] public string IdempotencyKey { get; set; }
        [DataMember(Name = "queue_id", EmitDefaultValue = false)] public string QueueId { get; set; }
        [DataMember(Name = "order_id", EmitDefaultValue = false)] public string OrderId { get; set; }
        [DataMember(Name = "local_order_id", EmitDefaultValue = false)] public string LocalOrderId { get; set; }
        [DataMember(Name = "operation_kind", EmitDefaultValue = false)] public string OperationKind { get; set; }
        [DataMember(Name = "created_at_millis", EmitDefaultValue = false)] public long CreatedAtMillis { get; set; }
        [DataMember(Name = "retry_count", EmitDefaultValue = false)] public int RetryCount { get; set; }
        [DataMember(Name = "last_error", EmitDefaultValue = false)] public string LastError { get; set; }
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
        [DataMember(Name = "address_text", EmitDefaultValue = false)] public string AddressText { get; set; }
        [DataMember(Name = "order_payload", EmitDefaultValue = false)] public object OrderPayload { get; set; }
        [DataMember(Name = "pos_shift_id", EmitDefaultValue = false)] public string PosShiftId { get; set; }
    }

    [DataContract]
    public sealed class IdempotencyAwareQueueState
    {
        [DataMember(Name = "version")] public int Version { get; set; } = 2;
        [DataMember(Name = "items")] public List<IdempotencyAwareQueueItem> Items { get; set; } = new List<IdempotencyAwareQueueItem>();
    }

    [DataContract]
    public sealed class SupabaseRpcGenericResult
    {
        [DataMember(Name = "ok")] public bool Ok { get; set; }
        [DataMember(Name = "error")] public string Error { get; set; }
        [DataMember(Name = "order_id")] public string OrderId { get; set; }
    }
}

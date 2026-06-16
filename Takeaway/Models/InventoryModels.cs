using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        [DataContract]
        public sealed class PosInventoryAdminSnapshotResponse
        {
            [DataMember(Name = "ok", EmitDefaultValue = false)] public bool? Ok { get; set; }
            [DataMember(Name = "error", EmitDefaultValue = false)] public string Error { get; set; }
            [DataMember(Name = "inventoryItems", EmitDefaultValue = false)] public List<PosInventoryAdminItemRow> InventoryItems { get; set; }
            [DataMember(Name = "inventoryLinks", EmitDefaultValue = false)] public List<PosInventoryAdminLinkRow> InventoryLinks { get; set; }
            [DataMember(Name = "targets", EmitDefaultValue = false)] public List<PosInventoryAdminTargetRow> Targets { get; set; }
        }

        [DataContract]
        public sealed class PosInventoryAdminItemRow
        {
            [DataMember(Name = "id", EmitDefaultValue = false)] public string Id { get; set; }
            [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }
            [DataMember(Name = "measureType", EmitDefaultValue = false)] public string MeasureType { get; set; }
            [DataMember(Name = "baseUnit", EmitDefaultValue = false)] public string BaseUnit { get; set; }
            [DataMember(Name = "displayUnit", EmitDefaultValue = false)] public string DisplayUnit { get; set; }
            [DataMember(Name = "quantityOnHand", EmitDefaultValue = false)] public double QuantityOnHand { get; set; }
            [DataMember(Name = "lowStockThreshold", EmitDefaultValue = false)] public double LowStockThreshold { get; set; }
            [DataMember(Name = "active", EmitDefaultValue = false)] public bool Active { get; set; }
        }

        [DataContract]
        public sealed class PosInventoryAdminLinkRow
        {
            [DataMember(Name = "inventoryItemId", EmitDefaultValue = false)] public string InventoryItemId { get; set; }
            [DataMember(Name = "entityKind", EmitDefaultValue = false)] public string EntityKind { get; set; }
            [DataMember(Name = "entityId", EmitDefaultValue = false)] public string EntityId { get; set; }
            [DataMember(Name = "consumptionQuantity", EmitDefaultValue = false)] public double ConsumptionQuantity { get; set; }
        }

        [DataContract]
        public sealed class PosInventoryAdminTargetRow
        {
            [DataMember(Name = "entityKind", EmitDefaultValue = false)] public string EntityKind { get; set; }
            [DataMember(Name = "entityId", EmitDefaultValue = false)] public string EntityId { get; set; }
            [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }
            [DataMember(Name = "groupName", EmitDefaultValue = false)] public string GroupName { get; set; }
            [DataMember(Name = "catalogScope", EmitDefaultValue = false)] public string CatalogScope { get; set; }
            [DataMember(Name = "inventoryMode", EmitDefaultValue = false)] public string InventoryMode { get; set; }
            [DataMember(Name = "inventoryStatus", EmitDefaultValue = false)] public string InventoryStatus { get; set; }
            [DataMember(Name = "inventoryRemaining", EmitDefaultValue = false)] public double? InventoryRemaining { get; set; }
            [DataMember(Name = "linkedCount", EmitDefaultValue = false)] public int LinkedCount { get; set; }
        }

        [DataContract]
        public sealed class PosInventoryRpcResponse
        {
            [DataMember(Name = "ok", EmitDefaultValue = false)] public bool? Ok { get; set; }
            [DataMember(Name = "error", EmitDefaultValue = false)] public string Error { get; set; }
            [DataMember(Name = "itemId", EmitDefaultValue = false)] public string ItemId { get; set; }
            [DataMember(Name = "updated", EmitDefaultValue = false)] public bool? Updated { get; set; }
            [DataMember(Name = "deleted", EmitDefaultValue = false)] public bool? Deleted { get; set; }
        }

        [DataContract]
        public sealed class PosInventoryUpsertItemRequest
        {
            [DataMember(Name = "p_item", EmitDefaultValue = false)] public PosInventoryUpsertItemPayload Item { get; set; }
        }

        [DataContract]
        public sealed class PosInventoryUpsertItemPayload
        {
            [DataMember(Name = "id", EmitDefaultValue = false)] public string Id { get; set; }
            [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }
            [DataMember(Name = "displayUnit", EmitDefaultValue = false)] public string DisplayUnit { get; set; }
            [DataMember(Name = "quantityOnHand", EmitDefaultValue = false)] public double QuantityOnHand { get; set; }
            [DataMember(Name = "lowStockThreshold", EmitDefaultValue = false)] public double LowStockThreshold { get; set; }
            [DataMember(Name = "active", EmitDefaultValue = false)] public bool Active { get; set; }
        }

        [DataContract]
        public sealed class PosInventoryDeleteItemRequest
        {
            [DataMember(Name = "p_inventory_item_id", EmitDefaultValue = false)] public string InventoryItemId { get; set; }
        }

        [DataContract]
        public sealed class PosInventorySetModeRequest
        {
            [DataMember(Name = "p_entity_kind", EmitDefaultValue = false)] public string EntityKind { get; set; }
            [DataMember(Name = "p_entity_id", EmitDefaultValue = false)] public string EntityId { get; set; }
            [DataMember(Name = "p_inventory_mode", EmitDefaultValue = false)] public string InventoryMode { get; set; }
        }

        [DataContract]
        public sealed class PosInventoryReplaceLinksRequest
        {
            [DataMember(Name = "p_entity_kind", EmitDefaultValue = false)] public string EntityKind { get; set; }
            [DataMember(Name = "p_entity_id", EmitDefaultValue = false)] public string EntityId { get; set; }
            [DataMember(Name = "p_links", EmitDefaultValue = false)] public List<PosInventoryReplaceLinkPayload> Links { get; set; }
        }

        [DataContract]
        public sealed class PosInventoryReplaceLinkPayload
        {
            [DataMember(Name = "inventoryItemId", EmitDefaultValue = false)] public string InventoryItemId { get; set; }
            [DataMember(Name = "consumptionQuantity", EmitDefaultValue = false)] public double ConsumptionQuantity { get; set; }
        }
    }
}

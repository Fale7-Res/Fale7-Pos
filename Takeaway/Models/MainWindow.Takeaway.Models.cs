using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        [DataContract]
        private sealed class PosMenuBackendSnapshotRecord
        {
            [DataMember(Name = "categories")] public List<PosMenuBackendCategoryRecord> Categories { get; set; }
            [DataMember(Name = "products")] public List<PosMenuBackendProductRecord> Products { get; set; }
        }

        [DataContract]
        private sealed class PosMenuBackendCategoryRecord
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "sortOrder", EmitDefaultValue = false)] public int SortOrder { get; set; }
            [DataMember(Name = "enabled", EmitDefaultValue = false)] public bool Enabled { get; set; }
        }

        [DataContract]
        private sealed class PosMenuBackendProductRecord
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "categoryId")] public string CategoryId { get; set; }
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "posDisplayName", EmitDefaultValue = false)] public string PosDisplayName { get; set; }
            [DataMember(Name = "sourceProductId", EmitDefaultValue = false)] public string SourceProductId { get; set; }
            [DataMember(Name = "basePrice", EmitDefaultValue = false)] public double BasePrice { get; set; }
            [DataMember(Name = "enabled", EmitDefaultValue = false)] public bool Enabled { get; set; }
            [DataMember(Name = "sortOrder", EmitDefaultValue = false)] public int SortOrder { get; set; }
            [DataMember(Name = "inventoryStatus", EmitDefaultValue = false)] public string InventoryStatus { get; set; }
            [DataMember(Name = "inventoryRemaining", EmitDefaultValue = false)] public double? InventoryRemaining { get; set; }
        }

        [DataContract]
        private sealed class AppProductsSnapshotRecord
        {
            [DataMember(Name = "source", EmitDefaultValue = false)] public string Source { get; set; }
            [DataMember(Name = "generatedAtUnixMs", EmitDefaultValue = false)] public long? GeneratedAtUnixMs { get; set; }
            [DataMember(Name = "products")] public List<AppProductSnapshotRecord> Products { get; set; }
        }

        [DataContract]
        private sealed class AppProductSnapshotRecord
        {
            [DataMember(Name = "Id")] public string Id { get; set; }
            [DataMember(Name = "SourceProductId", EmitDefaultValue = false)] public string SourceProductId { get; set; }
            [DataMember(Name = "NameAr")] public string NameAr { get; set; }
            [DataMember(Name = "BasePrice")] public int BasePrice { get; set; }
            [DataMember(Name = "InventoryStatus", EmitDefaultValue = false)] public string InventoryStatus { get; set; }
            [DataMember(Name = "InventoryRemaining", EmitDefaultValue = false)] public double? InventoryRemaining { get; set; }
            [DataMember(Name = "AddonGroups")] public List<AppAddonGroupSnapshotRecord> AddonGroups { get; set; }
        }

        [DataContract]
        private sealed class AppAddonGroupSnapshotRecord
        {
            [DataMember(Name = "Id")] public string Id { get; set; }
            [DataMember(Name = "NameAr")] public string NameAr { get; set; }
            [DataMember(Name = "MultiSelect")] public bool MultiSelect { get; set; }
            [DataMember(Name = "Options")] public List<AppAddonOptionSnapshotRecord> Options { get; set; }
        }

        [DataContract]
        private sealed class AppAddonOptionSnapshotRecord
        {
            [DataMember(Name = "Id")] public string Id { get; set; }
            [DataMember(Name = "NameAr")] public string NameAr { get; set; }
            [DataMember(Name = "ExtraPrice")] public int ExtraPrice { get; set; }
            [DataMember(Name = "InventoryStatus", EmitDefaultValue = false)] public string InventoryStatus { get; set; }
            [DataMember(Name = "InventoryRemaining", EmitDefaultValue = false)] public double? InventoryRemaining { get; set; }
        }

        [DataContract]
        private sealed class PosAppProductRpcRecord
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "sourceProductId", EmitDefaultValue = false)] public string SourceProductId { get; set; }
            [DataMember(Name = "basePrice", EmitDefaultValue = false)] public double BasePrice { get; set; }
            [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }
            [DataMember(Name = "inventoryStatus", EmitDefaultValue = false)] public string InventoryStatus { get; set; }
            [DataMember(Name = "inventoryRemaining", EmitDefaultValue = false)] public double? InventoryRemaining { get; set; }
            [DataMember(Name = "addonGroups", EmitDefaultValue = false)] public List<PosAppAddonGroupRpcRecord> AddonGroups { get; set; }
        }

        [DataContract]
        private sealed class PosAppAddonGroupRpcRecord
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "nameAr", EmitDefaultValue = false)] public string NameAr { get; set; }
            [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }
            [DataMember(Name = "multiSelect", EmitDefaultValue = false)] public bool MultiSelect { get; set; }
            [DataMember(Name = "options", EmitDefaultValue = false)] public List<PosAppAddonOptionRpcRecord> Options { get; set; }
        }

        [DataContract]
        private sealed class PosAppAddonOptionRpcRecord
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "nameAr", EmitDefaultValue = false)] public string NameAr { get; set; }
            [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }
            [DataMember(Name = "extraPrice", EmitDefaultValue = false)] public double ExtraPrice { get; set; }
            [DataMember(Name = "price", EmitDefaultValue = false)] public double Price { get; set; }
            [DataMember(Name = "inventoryStatus", EmitDefaultValue = false)] public string InventoryStatus { get; set; }
            [DataMember(Name = "inventoryRemaining", EmitDefaultValue = false)] public double? InventoryRemaining { get; set; }
        }
    }
}

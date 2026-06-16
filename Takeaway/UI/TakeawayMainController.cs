using Fale7_POS.Infrastructure.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Printing;
using System.Runtime.InteropServices;
using Drawing = System.Drawing;
using Drawing2D = System.Drawing.Drawing2D;
using DrawingPrinting = System.Drawing.Printing;
using DrawingText = System.Drawing.Text;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;

        private FrameworkElement RootViewbox { get { return RootScale; } }
        private readonly Dictionary<string, string> _pickupOrderLinks = new();
        private bool _takeawayPickupContextActive;
        private string _takeawayPickupSelectedRowKey;
        private bool _deliveryPickupContextActive;
        private string _deliveryPickupSelectedRowKey;
        private bool _adminAddCategoryTargetDelivery;
        private bool _adminAddItemTargetDelivery;
        private bool _adminReorderPopupTargetDelivery;
        private MenuCategory _adminCategoryReorderTarget;
        private MenuCategory _adminItemReorderCategoryTarget;
        private MenuItemDef _adminItemReorderTarget;
        private MenuCategory _adminCategoryEditTarget;
        private MenuCategory _adminItemEditCategoryTarget;
        private MenuItemDef _adminItemEditTarget;
        private readonly List<AppProductSnapshotRecord> _appProductsCatalog = new();
        private readonly Dictionary<string, ComboBox> _adminAddItemSingleOptionSelectors = new();
        private readonly Dictionary<string, List<CheckBox>> _adminAddItemMultiOptionSelectors = new();
        private string _adminAddItemSelectedProductId;
        private string _adminAddItemLastAutoDisplayName;
        private bool _adminAddItemUiSyncing;

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

        private bool TryLoadAppProductsFromBackend()
        {
            return TryRefreshAppProductsSnapshotFromBackend();
        }

        private bool TryRefreshPosMenuSnapshotFromBackend()
        {
            SupabaseClientConfigRecord cfg = null;
            try { cfg = LoadSupabaseClientConfigResolved(); } catch { }
            if (cfg == null) return false;

            try
            {
                var raw = PostSupabaseRpcGetStringAsync(cfg, "api_pos_menu_snapshot", "{}").GetAwaiter().GetResult();
                if (string.IsNullOrWhiteSpace(raw)) return false;
                var snapshot = UnwrapSupabaseRpcResult<PosMenuBackendSnapshotRecord>(raw);
                if (snapshot == null || snapshot.Categories == null || snapshot.Products == null) return false;
                if (!ApplyPosMenuSnapshot(snapshot)) return false;
                SaveMenuStateToDisk();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool ApplyPosMenuSnapshot(PosMenuBackendSnapshotRecord snapshot)
        {
            if (snapshot == null) return false;
            var categories = snapshot.Categories ?? new List<PosMenuBackendCategoryRecord>();
            var products = snapshot.Products ?? new List<PosMenuBackendProductRecord>();
            var previousItemsByRemoteId = new Dictionary<string, MenuItemDef>(StringComparer.Ordinal);
            if (_menu != null)
            {
                for (int c = 0; c < _menu.Count; c++)
                {
                    var prevCat = _menu[c];
                    if (prevCat == null || prevCat.Items == null) continue;
                    for (int p = 0; p < prevCat.Items.Count; p++)
                    {
                        var prevItem = prevCat.Items[p];
                        if (prevItem == null || string.IsNullOrWhiteSpace(prevItem.RemoteId)) continue;
                        var remoteId = prevItem.RemoteId.Trim();
                        if (!previousItemsByRemoteId.ContainsKey(remoteId)) previousItemsByRemoteId[remoteId] = prevItem;
                    }
                }
            }

            EnsureAppProductsCatalogLoaded();
            var appProductIdBySourceId = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < _appProductsCatalog.Count; i++)
            {
                var appProd = _appProductsCatalog[i];
                if (appProd == null) continue;
                var appId = (appProd.Id ?? string.Empty).Trim();
                var sourceId = (appProd.SourceProductId ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(sourceId)) continue;
                if (!appProductIdBySourceId.ContainsKey(sourceId)) appProductIdBySourceId[sourceId] = appId;
            }

            var enabledCategories = new List<PosMenuBackendCategoryRecord>();
            for (int i = 0; i < categories.Count; i++)
            {
                var c = categories[i];
                if (c == null) continue;
                if (string.IsNullOrWhiteSpace(c.Id) || string.IsNullOrWhiteSpace(c.Name)) continue;
                enabledCategories.Add(c);
            }
            enabledCategories.Sort(delegate (PosMenuBackendCategoryRecord a, PosMenuBackendCategoryRecord b)
            {
                var cmp = a.SortOrder.CompareTo(b.SortOrder);
                if (cmp != 0) return cmp;
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });

            var nextMenu = new List<MenuCategory>();
            for (int i = 0; i < enabledCategories.Count; i++)
            {
                var c = enabledCategories[i];
                var cat = new MenuCategory
                {
                    RemoteId = (c.Id ?? string.Empty).Trim(),
                    Name = c.Name.Trim(),
                    Items = new List<MenuItemDef>()
                };

                var catProducts = new List<PosMenuBackendProductRecord>();
                for (int p = 0; p < products.Count; p++)
                {
                    var prod = products[p];
                    if (prod == null) continue;
                    if (!prod.Enabled) continue;
                    if (!string.Equals((prod.CategoryId ?? string.Empty).Trim(), (c.Id ?? string.Empty).Trim(), StringComparison.Ordinal)) continue;
                    if (string.IsNullOrWhiteSpace(prod.Name)) continue;
                    catProducts.Add(prod);
                }

                catProducts.Sort(delegate (PosMenuBackendProductRecord a, PosMenuBackendProductRecord b)
                {
                    var cmp = a.SortOrder.CompareTo(b.SortOrder);
                    if (cmp != 0) return cmp;
                    return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                });

                for (int p = 0; p < catProducts.Count; p++)
                {
                    var prod = catProducts[p];
                    var remoteId = !string.IsNullOrWhiteSpace(prod.Id) ? prod.Id.Trim() : null;
                    MenuItemDef previous = null;
                    if (!string.IsNullOrWhiteSpace(remoteId)) previousItemsByRemoteId.TryGetValue(remoteId, out previous);

                    var sourceProductId = !string.IsNullOrWhiteSpace(prod.SourceProductId)
                        ? prod.SourceProductId.Trim()
                        : (!string.IsNullOrWhiteSpace(prod.Id) ? prod.Id.Trim() : null);
                    var resolvedProductId = sourceProductId;
                    if (!string.IsNullOrWhiteSpace(sourceProductId)
                        && appProductIdBySourceId.TryGetValue(sourceProductId, out var appProductId)
                        && !string.IsNullOrWhiteSpace(appProductId))
                    {
                        resolvedProductId = appProductId;
                    }
                    if (previous != null && !string.IsNullOrWhiteSpace(previous.ProductId))
                    {
                        resolvedProductId = previous.ProductId.Trim();
                    }

                    var fixedOptionIds = new List<string>();
                    if (previous != null && previous.FixedOptionIds != null)
                    {
                        for (int f = 0; f < previous.FixedOptionIds.Count; f++)
                        {
                            var fixedId = previous.FixedOptionIds[f];
                            if (string.IsNullOrWhiteSpace(fixedId)) continue;
                            var cleanedFixedId = fixedId.Trim();
                            if (!fixedOptionIds.Contains(cleanedFixedId)) fixedOptionIds.Add(cleanedFixedId);
                        }
                    }

                    cat.Items.Add(new MenuItemDef
                    {
                        Name = (prod.Name ?? string.Empty).Trim(),
                        PosDisplayName = !string.IsNullOrWhiteSpace(prod.PosDisplayName) ? prod.PosDisplayName.Trim() : (prod.Name ?? string.Empty).Trim(),
                        Price = (int)Math.Round(prod.BasePrice < 0 ? 0 : prod.BasePrice),
                        ProductId = resolvedProductId,
                        RemoteId = remoteId,
                        FixedOptionIds = fixedOptionIds,
                        SourceProductId = sourceProductId,
                        InventoryStatus = string.IsNullOrWhiteSpace(prod.InventoryStatus) ? "available" : prod.InventoryStatus.Trim(),
                        InventoryRemaining = prod.InventoryRemaining
                    });
                }

                nextMenu.Add(cat);
            }

            _menu.Clear();
            for (int i = 0; i < nextMenu.Count; i++) _menu.Add(nextMenu[i]);
            NormalizeMenuState();
            return _menu.Count > 0;
        }

        private bool TryRefreshAppProductsSnapshotFromBackend()
        {
            SupabaseClientConfigRecord cfg = null;
            try { cfg = LoadSupabaseClientConfigResolved(); } catch { }
            if (cfg == null) return false;

            try
            {
                var raw = PostSupabaseRpcGetStringAsync(cfg, "api_pos_app_products_snapshot", "{}").GetAwaiter().GetResult();
                if (string.IsNullOrWhiteSpace(raw)) return false;
                var list = UnwrapSupabaseRpcResult<List<PosAppProductRpcRecord>>(raw);
                if (list == null || list.Count == 0) return false;
                var products = new List<AppProductSnapshotRecord>();
                foreach (var p in list)
                {
                    if (p == null || string.IsNullOrWhiteSpace(p.Id)) continue;
                    var groups = new List<AppAddonGroupSnapshotRecord>();
                    if (p.AddonGroups != null)
                    {
                        for (int g = 0; g < p.AddonGroups.Count; g++)
                        {
                            var srcGroup = p.AddonGroups[g];
                            if (srcGroup == null || string.IsNullOrWhiteSpace(srcGroup.Id)) continue;
                            var groupName = !string.IsNullOrWhiteSpace(srcGroup.NameAr) ? srcGroup.NameAr : srcGroup.Name;
                            if (string.IsNullOrWhiteSpace(groupName)) continue;

                            var mappedGroup = new AppAddonGroupSnapshotRecord
                            {
                                Id = srcGroup.Id.Trim(),
                                NameAr = groupName.Trim(),
                                MultiSelect = srcGroup.MultiSelect,
                                Options = new List<AppAddonOptionSnapshotRecord>()
                            };

                            if (srcGroup.Options != null)
                            {
                                for (int o = 0; o < srcGroup.Options.Count; o++)
                                {
                                    var srcOpt = srcGroup.Options[o];
                                    if (srcOpt == null || string.IsNullOrWhiteSpace(srcOpt.Id)) continue;
                                    var optionName = !string.IsNullOrWhiteSpace(srcOpt.NameAr) ? srcOpt.NameAr : srcOpt.Name;
                                    if (string.IsNullOrWhiteSpace(optionName)) continue;
                                    var extra = srcOpt.ExtraPrice != 0 ? srcOpt.ExtraPrice : srcOpt.Price;
                                    mappedGroup.Options.Add(new AppAddonOptionSnapshotRecord
                                    {
                                        Id = srcOpt.Id.Trim(),
                                        NameAr = optionName.Trim(),
                                        ExtraPrice = extra < 0 ? 0 : (int)Math.Round(extra),
                                        InventoryStatus = string.IsNullOrWhiteSpace(srcOpt.InventoryStatus) ? "available" : srcOpt.InventoryStatus.Trim(),
                                        InventoryRemaining = srcOpt.InventoryRemaining
                                    });
                                }
                            }

                            groups.Add(mappedGroup);
                        }
                    }

                    products.Add(new AppProductSnapshotRecord
                    {
                        Id = p.Id.Trim(),
                        SourceProductId = !string.IsNullOrWhiteSpace(p.SourceProductId) ? p.SourceProductId.Trim() : p.Id.Trim(),
                        NameAr = (p.Name ?? string.Empty).Trim(),
                        BasePrice = p.BasePrice < 0 ? 0 : (int)Math.Round(p.BasePrice),
                        InventoryStatus = string.IsNullOrWhiteSpace(p.InventoryStatus) ? "available" : p.InventoryStatus.Trim(),
                        InventoryRemaining = p.InventoryRemaining,
                        AddonGroups = groups
                    });
                }
                if (!ApplyAppProductsSnapshot(products)) return false;
                SaveAppProductsSnapshotToDisk();
                Debug.WriteLine("Fale7_POS linked app products snapshot loaded from Supabase api_pos_app_products_snapshot");
                return true;
            }
            catch
            {
                return false;
            }
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

        private bool ApplyAppProductsSnapshot(List<AppProductSnapshotRecord> products)
        {
            if (products == null || products.Count == 0) return false;
            _appProductsCatalog.Clear();

            for (int i = 0; i < products.Count; i++)
            {
                var src = products[i];
                if (src == null || string.IsNullOrWhiteSpace(src.Id) || string.IsNullOrWhiteSpace(src.NameAr)) continue;

                var p = new AppProductSnapshotRecord
                {
                    Id = src.Id.Trim(),
                    SourceProductId = !string.IsNullOrWhiteSpace(src.SourceProductId) ? src.SourceProductId.Trim() : src.Id.Trim(),
                    NameAr = src.NameAr.Trim(),
                    BasePrice = src.BasePrice < 0 ? 0 : src.BasePrice,
                    InventoryStatus = string.IsNullOrWhiteSpace(src.InventoryStatus) ? "available" : src.InventoryStatus.Trim(),
                    InventoryRemaining = src.InventoryRemaining,
                    AddonGroups = new List<AppAddonGroupSnapshotRecord>()
                };

                if (src.AddonGroups != null)
                {
                    for (int gIndex = 0; gIndex < src.AddonGroups.Count; gIndex++)
                    {
                        var srcGroup = src.AddonGroups[gIndex];
                        if (srcGroup == null || string.IsNullOrWhiteSpace(srcGroup.Id) || string.IsNullOrWhiteSpace(srcGroup.NameAr)) continue;

                        var g = new AppAddonGroupSnapshotRecord
                        {
                            Id = srcGroup.Id.Trim(),
                            NameAr = srcGroup.NameAr.Trim(),
                            MultiSelect = srcGroup.MultiSelect,
                            Options = new List<AppAddonOptionSnapshotRecord>()
                        };

                        if (srcGroup.Options != null)
                        {
                            for (int oIndex = 0; oIndex < srcGroup.Options.Count; oIndex++)
                            {
                                var srcOpt = srcGroup.Options[oIndex];
                                if (srcOpt == null || string.IsNullOrWhiteSpace(srcOpt.Id) || string.IsNullOrWhiteSpace(srcOpt.NameAr)) continue;
                                g.Options.Add(new AppAddonOptionSnapshotRecord
                                {
                                    Id = srcOpt.Id.Trim(),
                                    NameAr = srcOpt.NameAr.Trim(),
                                    ExtraPrice = srcOpt.ExtraPrice < 0 ? 0 : srcOpt.ExtraPrice,
                                    InventoryStatus = string.IsNullOrWhiteSpace(srcOpt.InventoryStatus) ? "available" : srcOpt.InventoryStatus.Trim(),
                                    InventoryRemaining = srcOpt.InventoryRemaining
                                });
                            }
                        }

                        p.AddonGroups.Add(g);
                    }
                }

                _appProductsCatalog.Add(p);
            }

            return _appProductsCatalog.Count > 0;
        }

        private void EnsureAppProductsCatalogLoaded()
        {
            if (_appProductsCatalog.Count > 0) return;
            _appProductsCatalog.Clear();
            if (LoadAppProductsSnapshotFromDisk()) return;
            try
            {
                QueueSupabasePosMenuPullFromBackend(false);
            }
            catch
            {
            }
        }

        private AppProductSnapshotRecord FindAppProductSnapshotById(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId)) return null;
            var targetId = productId.Trim();
            EnsureAppProductsCatalogLoaded();
            for (int i = 0; i < _appProductsCatalog.Count; i++)
            {
                var p = _appProductsCatalog[i];
                if (p == null) continue;
                if (string.Equals((p.Id ?? string.Empty).Trim(), targetId, StringComparison.Ordinal)) return p;
                if (string.Equals((p.SourceProductId ?? string.Empty).Trim(), targetId, StringComparison.Ordinal)) return p;
            }
            return null;
        }

        private AppAddonOptionSnapshotRecord FindAppOptionSnapshotById(AppProductSnapshotRecord product, string optionId)
        {
            if (product == null || string.IsNullOrWhiteSpace(optionId) || product.AddonGroups == null) return null;
            for (int i = 0; i < product.AddonGroups.Count; i++)
            {
                var g = product.AddonGroups[i];
                if (g == null || g.Options == null) continue;
                for (int j = 0; j < g.Options.Count; j++)
                {
                    var opt = g.Options[j];
                    if (opt != null && string.Equals(opt.Id, optionId, StringComparison.Ordinal)) return opt;
                }
            }
            return null;
        }

        private string NormalizeInventoryStatus(string raw)
        {
            var value = (raw ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(value)) return "available";
            if (value == "out_of_stock" || value == "limited" || value == "available") return value;
            return "available";
        }

        private bool IsInventoryRemainingOut(double? remaining)
        {
            return remaining.HasValue && remaining.Value <= 0.0001;
        }

        private int? FloorInventoryRemainingCount(double? remaining)
        {
            if (!remaining.HasValue) return null;
            var floored = (int)Math.Floor(remaining.Value + 0.0001);
            return floored <= 0 ? 0 : floored;
        }

        private int? MergeInventoryRemainingCount(int? current, double? candidate)
        {
            var next = FloorInventoryRemainingCount(candidate);
            if (!next.HasValue) return current;
            if (!current.HasValue) return next;
            return next.Value < current.Value ? next : current;
        }

        private int? ApplyLocalReservationsToRemaining(double? remaining, int reservedQty)
        {
            var count = FloorInventoryRemainingCount(remaining);
            if (!count.HasValue) return null;
            var adjusted = count.Value - Math.Max(0, reservedQty);
            return adjusted <= 0 ? 0 : adjusted;
        }

        private string ResolveMenuItemSourceProductId(MenuItemDef item)
        {
            if (item == null) return string.Empty;
            var sourceId = (item.SourceProductId ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(sourceId)) return sourceId;
            return (item.ProductId ?? string.Empty).Trim();
        }

        private string BuildOptionIdsSignature(List<string> optionIds)
        {
            if (optionIds == null || optionIds.Count == 0) return string.Empty;
            var normalized = new List<string>();
            for (int i = 0; i < optionIds.Count; i++)
            {
                var clean = (optionIds[i] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(clean) || normalized.Contains(clean)) continue;
                normalized.Add(clean);
            }
            normalized.Sort(StringComparer.Ordinal);
            return string.Join("|", normalized);
        }

        private void AppendReservedQuantity(Dictionary<string, int> reservations, string key, int qty)
        {
            if (reservations == null) return;
            var cleanKey = (key ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(cleanKey)) return;
            var safeQty = qty <= 0 ? 1 : qty;
            if (reservations.TryGetValue(cleanKey, out var current)) reservations[cleanKey] = current + safeQty;
            else reservations[cleanKey] = safeQty;
        }

        private void AppendTakeawayOrderReservations(
            Dictionary<string, int> productReservations,
            Dictionary<string, int> optionReservations)
        {
            if (_takeawayState == null || _takeawayState.OpenOrders == null) return;
            for (int i = 0; i < _takeawayState.OpenOrders.Count; i++)
            {
                var order = _takeawayState.OpenOrders[i];
                if (order == null || order.Items == null) continue;
                for (int j = 0; j < order.Items.Count; j++)
                {
                    var item = order.Items[j];
                    if (item == null) continue;
                    var qty = item.Qty <= 0 ? 1 : item.Qty;
                    var sourceProductId = (item.SourceProductId ?? item.ProductId ?? string.Empty).Trim();
                    AppendReservedQuantity(productReservations, sourceProductId, qty);

                    var optionIds = item.OptionIds ?? new List<string>();
                    for (int k = 0; k < optionIds.Count; k++)
                    {
                        AppendReservedQuantity(optionReservations, optionIds[k], qty);
                    }
                }
            }
        }

        private void AppendDeliveryOrderReservations(
            Dictionary<string, int> productReservations,
            Dictionary<string, int> optionReservations)
        {
            if (_deliveryOrders == null) return;
            for (int i = 0; i < _deliveryOrders.Count; i++)
            {
                var order = _deliveryOrders[i];
                if (order == null || order.Items == null) continue;
                for (int j = 0; j < order.Items.Count; j++)
                {
                    var item = order.Items[j];
                    if (item == null) continue;
                    var qty = item.Qty <= 0 ? 1 : item.Qty;
                    var sourceProductId = (item.SourceProductId ?? item.ProductId ?? string.Empty).Trim();
                    AppendReservedQuantity(productReservations, sourceProductId, qty);

                    var optionIds = item.OptionIds ?? new List<string>();
                    for (int k = 0; k < optionIds.Count; k++)
                    {
                        AppendReservedQuantity(optionReservations, optionIds[k], qty);
                    }
                }
            }
        }

        private void BuildLocalInventoryReservationCounters(
            out Dictionary<string, int> productReservations,
            out Dictionary<string, int> optionReservations)
        {
            productReservations = new Dictionary<string, int>(StringComparer.Ordinal);
            optionReservations = new Dictionary<string, int>(StringComparer.Ordinal);
            AppendTakeawayOrderReservations(productReservations, optionReservations);
            AppendDeliveryOrderReservations(productReservations, optionReservations);
        }

        private MenuItemDef FindMenuItemDefinitionForLine(string productId, string sourceProductId, List<string> optionIds)
        {
            var targetProductId = (productId ?? string.Empty).Trim();
            var targetSourceProductId = (sourceProductId ?? string.Empty).Trim();
            var targetSignature = BuildOptionIdsSignature(optionIds);
            for (int c = 0; c < _menu.Count; c++)
            {
                var cat = _menu[c];
                if (cat == null || cat.Items == null) continue;
                for (int i = 0; i < cat.Items.Count; i++)
                {
                    var candidate = cat.Items[i];
                    if (candidate == null || candidate.Name == "â€”") continue;

                    var candidateProductId = (candidate.ProductId ?? string.Empty).Trim();
                    var candidateSourceProductId = ResolveMenuItemSourceProductId(candidate);
                    var productMatch =
                        (!string.IsNullOrWhiteSpace(targetProductId)
                            && string.Equals(candidateProductId, targetProductId, StringComparison.Ordinal))
                        || (!string.IsNullOrWhiteSpace(targetSourceProductId)
                            && string.Equals(candidateSourceProductId, targetSourceProductId, StringComparison.Ordinal));
                    if (!productMatch) continue;
                    if (!string.Equals(BuildOptionIdsSignature(candidate.FixedOptionIds), targetSignature, StringComparison.Ordinal)) continue;
                    return candidate;
                }
            }
            return null;
        }

        private void RefreshMenuInventoryTiles()
        {
            if (ItemsGridPanel != null) RenderItems();
            if (DeliveryItemsGridPanel != null) RenderDeliveryItems();
        }

        private string GetMenuItemInventoryStatus(
            MenuItemDef item,
            Dictionary<string, int> productReservations,
            Dictionary<string, int> optionReservations,
            out int? effectiveRemaining)
        {
            effectiveRemaining = null;
            if (item == null) return "available";

            var status = NormalizeInventoryStatus(item.InventoryStatus);
            var sourceProductId = ResolveMenuItemSourceProductId(item);
            productReservations = productReservations ?? new Dictionary<string, int>(StringComparer.Ordinal);
            optionReservations = optionReservations ?? new Dictionary<string, int>(StringComparer.Ordinal);

            productReservations.TryGetValue(sourceProductId, out var reservedProductQty);
            effectiveRemaining = MergeInventoryRemainingCount(
                effectiveRemaining,
                ApplyLocalReservationsToRemaining(item.InventoryRemaining, reservedProductQty));

            var product = FindAppProductSnapshotById(
                !string.IsNullOrWhiteSpace(item.ProductId) ? item.ProductId : sourceProductId);
            if (product != null)
            {
                var productStatus = NormalizeInventoryStatus(product.InventoryStatus);
                effectiveRemaining = MergeInventoryRemainingCount(
                    effectiveRemaining,
                    ApplyLocalReservationsToRemaining(product.InventoryRemaining, reservedProductQty));
                if (productStatus == "out_of_stock") return "out_of_stock";
                if (productStatus == "limited") status = "limited";

                var optionIds = item.FixedOptionIds ?? new List<string>();
                for (int i = 0; i < optionIds.Count; i++)
                {
                    var optionId = (optionIds[i] ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(optionId)) continue;

                    var opt = FindAppOptionSnapshotById(product, optionId);
                    if (opt == null) continue;

                    optionReservations.TryGetValue(optionId, out var reservedOptionQty);
                    effectiveRemaining = MergeInventoryRemainingCount(
                        effectiveRemaining,
                        ApplyLocalReservationsToRemaining(opt.InventoryRemaining, reservedOptionQty));

                    var optionStatus = NormalizeInventoryStatus(opt.InventoryStatus);
                    if (optionStatus == "out_of_stock") return "out_of_stock";
                    if (optionStatus == "limited") status = "limited";
                }
            }

            if (effectiveRemaining.HasValue && effectiveRemaining.Value <= 0) return "out_of_stock";
            if (status == "out_of_stock") return "out_of_stock";
            return status;
        }

        private string GetMenuItemInventoryStatus(MenuItemDef item)
        {
            BuildLocalInventoryReservationCounters(out var productReservations, out var optionReservations);
            return GetMenuItemInventoryStatus(item, productReservations, optionReservations, out _);
        }

        private bool IsMenuItemOutOfStock(MenuItemDef item)
        {
            return string.Equals(GetMenuItemInventoryStatus(item), "out_of_stock", StringComparison.OrdinalIgnoreCase);
        }

        private string GetMenuItemInventoryBadgeText(
            MenuItemDef item,
            Dictionary<string, int> productReservations,
            Dictionary<string, int> optionReservations)
        {
            var status = GetMenuItemInventoryStatus(item, productReservations, optionReservations, out var effectiveRemaining);
            if (string.Equals(status, "out_of_stock", StringComparison.OrdinalIgnoreCase)) return "\u063a\u064a\u0631 \u0645\u062a\u0627\u062d";
            if (string.Equals(status, "limited", StringComparison.OrdinalIgnoreCase))
            {
                if (effectiveRemaining.HasValue) return "\u0645\u062a\u0628\u0642\u064a " + effectiveRemaining.Value.ToString();
                return "\u0643\u0645\u064a\u0629 \u0645\u062d\u062f\u0648\u062f\u0629";
            }
            return string.Empty;
        }

        private string GetMenuItemInventoryBadgeText(MenuItemDef item)
        {
            BuildLocalInventoryReservationCounters(out var productReservations, out var optionReservations);
            return GetMenuItemInventoryBadgeText(item, productReservations, optionReservations);
        }

        private string GetMenuItemInventoryStatusText(MenuItemDef item)
        {
            var status = GetMenuItemInventoryStatus(item);
            if (string.Equals(status, "out_of_stock", StringComparison.OrdinalIgnoreCase)) return "غير متاح";
            if (string.Equals(status, "limited", StringComparison.OrdinalIgnoreCase)) return "كمية محدودة";
            return string.Empty;
        }

        private void ApplyInventoryTileVisualState(
            Button button,
            MenuItemDef item,
            Dictionary<string, int> productReservations,
            Dictionary<string, int> optionReservations)
        {
            if (button == null) return;

            var status = GetMenuItemInventoryStatus(item, productReservations, optionReservations, out _);
            var isOut = string.Equals(status, "out_of_stock", StringComparison.OrdinalIgnoreCase);
            var isLimited = string.Equals(status, "limited", StringComparison.OrdinalIgnoreCase);

            button.IsEnabled = !isOut;
            button.Cursor = isOut ? Cursors.No : Cursors.Hand;
            button.Opacity = isOut ? 0.58 : 1.0;

            if (isOut)
            {
                button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB9B9B9"));
                button.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4A4A4A"));
                button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8C8C8C"));
                return;
            }

            if (isLimited)
            {
                button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE09C22"));
                button.Foreground = Brushes.White;
                button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB36D00"));
                return;
            }

            button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2F6FAB"));
            button.Foreground = Brushes.White;
            button.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1F4F83"));
        }

        private void ApplyInventoryTileVisualState(Button button, MenuItemDef item)
        {
            BuildLocalInventoryReservationCounters(out var productReservations, out var optionReservations);
            ApplyInventoryTileVisualState(button, item, productReservations, optionReservations);
        }

        private List<string> GetAdminAddItemSelectedFixedOptionIds()
        {
            var ids = new List<string>();
            foreach (var kv in _adminAddItemSingleOptionSelectors)
            {
                var combo = kv.Value;
                var cbi = combo != null ? combo.SelectedItem as ComboBoxItem : null;
                var opt = cbi != null ? cbi.Tag as AppAddonOptionSnapshotRecord : null;
                if (opt != null && !string.IsNullOrWhiteSpace(opt.Id)) ids.Add(opt.Id);
            }
            foreach (var kv in _adminAddItemMultiOptionSelectors)
            {
                var list = kv.Value;
                if (list == null) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    var cb = list[i];
                    if (cb == null || cb.IsChecked != true) continue;
                    if (cb.Tag is AppAddonOptionSnapshotRecord opt && !string.IsNullOrWhiteSpace(opt.Id)) ids.Add(opt.Id);
                }
            }
            return ids;
        }

        private int ComputeLinkedPosPrice(AppProductSnapshotRecord product, List<string> fixedOptionIds)
        {
            if (product == null) return 0;
            var total = product.BasePrice < 0 ? 0 : product.BasePrice;
            if (fixedOptionIds != null)
            {
                for (int i = 0; i < fixedOptionIds.Count; i++)
                {
                    var opt = FindAppOptionSnapshotById(product, fixedOptionIds[i]);
                    if (opt != null) total += opt.ExtraPrice;
                }
            }
            return total < 0 ? 0 : total;
        }

        private string BuildLinkedPosDisplayName(AppProductSnapshotRecord product, List<string> fixedOptionIds)
        {
            if (product == null) return string.Empty;
            var name = (product.NameAr ?? string.Empty).Trim();
            if (fixedOptionIds == null || fixedOptionIds.Count == 0) return name;

            var suffix = string.Empty;
            for (int i = 0; i < fixedOptionIds.Count; i++)
            {
                var opt = FindAppOptionSnapshotById(product, fixedOptionIds[i]);
                if (opt == null || string.IsNullOrWhiteSpace(opt.NameAr)) continue;
                var part = opt.NameAr.Trim();
                if (suffix.Length > 0) suffix += " ";
                suffix += part;
            }
            if (string.IsNullOrWhiteSpace(suffix)) return name;
            return (name + " " + suffix).Trim();
        }

        private int ResolvePosTileFinalPrice(MenuItemDef item)
        {
            if (item == null) return 0;
            var product = FindAppProductSnapshotById(item.ProductId);
            if (product == null) return item.Price < 0 ? 0 : item.Price;
            var optionIds = item.FixedOptionIds ?? new List<string>();
            return ComputeLinkedPosPrice(product, optionIds);
        }

        private void InitializeTakeawayMenu()
        {
            _activeCategory = null;
            LoadMenuStateFromDisk();
            if (_menu.Count == 0) _menu.Clear();
            try
            {
                QueueSupabasePosMenuPullFromBackend(false);
            }
            catch
            {
            }
        }

        private long NowUnixMs() { return DateTimeOffset.Now.ToUnixTimeMilliseconds(); }
        private string NewId() { return Guid.NewGuid().ToString("N"); }

        private int NextOrderNo()
        {
            _takeawayState.OrderSequence += 1;
            return _takeawayState.OrderSequence;
        }

        private int NextPickupNo()
        {
            _takeawayState.PickupSequence += 1;
            return _takeawayState.PickupSequence;
        }

        private TakeawayOrderRecord GetCurrentOrder()
        {
            if (_takeawayState == null || _takeawayState.OpenOrders == null) return null;
            for (int i = 0; i < _takeawayState.OpenOrders.Count; i++)
            {
                var o = _takeawayState.OpenOrders[i];
                if (o != null && o.Id == _takeawayState.CurrentOrderId && !IsPickupLinkedOrder(o)) return o;
            }
            return null;
        }

        private bool IsPickupLinkedOrder(TakeawayOrderRecord order)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.Id)) return false;
            if (_pickupOrderLinks.TryGetValue(order.Id, out var linkedId) && !string.IsNullOrWhiteSpace(linkedId)) return true;
            linkedId = TryReadPickupLinkFromOrder(order);
            return !string.IsNullOrWhiteSpace(linkedId);
        }

        private string FindFirstRegularTakeawayOrderId()
        {
            if (_takeawayState == null || _takeawayState.OpenOrders == null) return null;
            for (int i = 0; i < _takeawayState.OpenOrders.Count; i++)
            {
                var o = _takeawayState.OpenOrders[i];
                if (o == null || string.IsNullOrWhiteSpace(o.Id)) continue;
                if (IsPickupLinkedOrder(o)) continue;
                return o.Id;
            }
            return null;
        }

        private void PersistTakeawayState()
        {
            // [PHASE 7.5 PURE CLOUD] Completely bypass all local DB writes.
            // Server is the absolute truth.
            return;
        }

        private TakeawayOrderRecord CreateOrder(bool isPickup = false)
        {
            var prefix = isPickup ? "PICK" : "TAKE";
            var o = new TakeawayOrderRecord { Id = NewId(), IdempotencyKey = LocalIdempotencyEngine.GenerateIdempotencyKey(prefix), No = NextOrderNo(), CreatedAt = NowUnixMs(), Items = new List<TakeawayLineRecord>() };
            _takeawayState.OpenOrders.Insert(0, o);
            _takeawayState.CurrentOrderId = o.Id;
            PersistTakeawayState();
            return o;
        }

        private TakeawayOrderRecord EnsureOrder()
        {
            var activePickupId = _takeawayState?.ActivePickupId;
            if (_takeawayPickupContextActive && !string.IsNullOrWhiteSpace(activePickupId))
            {
                var o = FindOrderLinkedToPickup(activePickupId);
                if (o != null)
                {
                    return o;
                }

                var previousQueueOrderId = _takeawayState.CurrentOrderId;
                o = CreateOrder(true);
                LinkPickupToOrder(o, activePickupId);
                _takeawayState.CurrentOrderId = previousQueueOrderId;
                PersistTakeawayState();
                return o;
            }

            var current = GetCurrentOrder();
            if (current != null) return current;
            return CreateOrder();
        }

        private TakeawayOrderRecord GetActiveTakeawayContextOrder()
        {
            if (_takeawayPickupContextActive)
            {
                var activePickupId = _takeawayState?.ActivePickupId;
                if (string.IsNullOrWhiteSpace(activePickupId)) return null;
                return FindOrderLinkedToPickup(activePickupId);
            }
            return GetCurrentOrder();
        }

        private string GetActiveTakeawayReceiptSelectedRowKey()
        {
            return _takeawayPickupContextActive ? _takeawayPickupSelectedRowKey : _selectedRowKey;
        }

        private void SetActiveTakeawayReceiptSelectedRowKey(string itemKey)
        {
            if (_takeawayPickupContextActive) _takeawayPickupSelectedRowKey = itemKey;
            else _selectedRowKey = itemKey;
        }

        private void ClearActiveTakeawayReceiptSelectedRowKey()
        {
            if (_takeawayPickupContextActive) _takeawayPickupSelectedRowKey = null;
            else _selectedRowKey = null;
        }

        private void SetTakeawayQueueContext(string orderId)
        {
            _takeawayPickupContextActive = false;
            if (!string.IsNullOrWhiteSpace(orderId)) _takeawayState.CurrentOrderId = orderId;
        }

        private void SetTakeawayPickupContext(string pickupId)
        {
            _takeawayPickupContextActive = !string.IsNullOrWhiteSpace(pickupId);
            if (!string.IsNullOrWhiteSpace(pickupId)) _takeawayState.ActivePickupId = pickupId;
        }

        private void SetDeliveryPickupContextActive(bool isActive)
        {
            _deliveryPickupContextActive = isActive;
            if (!isActive) _deliveryPickupSelectedRowKey = null;
        }

        private TakeawayOrderRecord GetActiveDeliveryPickupOrder()
        {
            if (!_deliveryPickupContextActive || _takeawayState == null || string.IsNullOrWhiteSpace(_takeawayState.ActivePickupId)) return null;
            return FindOrderLinkedToPickup(_takeawayState.ActivePickupId);
        }

        private TakeawayOrderRecord EnsureActiveDeliveryPickupOrder()
        {
            if (!_deliveryPickupContextActive || _takeawayState == null || string.IsNullOrWhiteSpace(_takeawayState.ActivePickupId)) return null;
            var o = FindOrderLinkedToPickup(_takeawayState.ActivePickupId);
            if (o != null) return o;
            var previousQueueOrderId = _takeawayState.CurrentOrderId;
            o = CreateOrder(true);
                LinkPickupToOrder(o, _takeawayState.ActivePickupId);
            _takeawayState.CurrentOrderId = previousQueueOrderId;
            PersistTakeawayState();
            return o;
        }

        private string FormatTimeShort(long ts)
        {
            try { return DateTimeOffset.FromUnixTimeMilliseconds(ts).LocalDateTime.ToString("hh:mm tt", _arEg); }
            catch { return "--:--"; }
        }

        private void BtnNewOrder_Click(object sender, RoutedEventArgs e)
        {
            // [PHASE 7.5 PURE CLOUD] No offline sync blocking - proceed directly.
            _takeawayPickupContextActive = false;
            CreateOrder();
            _selectedRowKey = null;
            RenderTakeawayAll();
        }

        private void BtnAddPickup_Click(object sender, RoutedEventArgs e)
        {
            OpenPickupOrderForm();
        }

        private void BtnClearOrder_Click(object sender, RoutedEventArgs e)
        {
            ClearCurrentOrderWithConfirm();
        }

        private void BtnKitchenConfirm_Click(object sender, RoutedEventArgs e)
        {
            PrintKitchenConfirm();
        }

        private void BtnShiftReport_Click(object sender, RoutedEventArgs e)
        {
            OpenTakeawayShiftOverlay();
        }

        private void RenderTakeawayAll()
        {
            RenderQueue();
            RenderPickups();
            RenderCategories();
            RenderItems();
            RenderReceipt();
        }

        private MenuCategory GetActiveCategory()
        {
            for (int i = 0; i < _menu.Count; i++) if (_menu[i].Name == _activeCategory) return _menu[i];
            return _menu.Count > 0 ? _menu[0] : null;
        }

        private Border CreateCard(string line1, string line2, bool active, bool compactPickup)
        {
            var outer = new Border
            {
                Margin = new Thickness(0, 0, 0, 8),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Focusable = true
            };

            var root = new Grid();
            var card = new Border
            {
                Background = active ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3F9FF")) : Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF666666")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                SnapsToDevicePixels = true
            };

            var g = new Grid();
            if (compactPickup)
            {
                g.ColumnDefinitions.Add(new ColumnDefinition());
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var t1 = new TextBlock { Text = line1, FontWeight = FontWeights.Black, FontSize = 12, FlowDirection = FlowDirection.RightToLeft, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.NoWrap };
                var t2 = new TextBlock { Text = line2, FontWeight = FontWeights.Bold, FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF333333")), FlowDirection = FlowDirection.LeftToRight, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.NoWrap };
                Grid.SetColumn(t2, 1); g.Children.Add(t1); g.Children.Add(t2);
            }
            else
            {
                var s = new StackPanel();
                s.Children.Add(new TextBlock { Text = line1, FontWeight = FontWeights.Black, FontSize = 12, FlowDirection = FlowDirection.RightToLeft });
                s.Children.Add(new TextBlock { Text = line2, FontWeight = FontWeights.Bold, FontSize = 11, Margin = new Thickness(0, 4, 0, 0), Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF333333")), FlowDirection = FlowDirection.LeftToRight, TextAlignment = TextAlignment.Left });
                g.Children.Add(s);
            }

            var cardShell = new Grid();
            cardShell.Children.Add(new Border { Margin = new Thickness(1, 1, 0, 0), BorderBrush = Brushes.White, BorderThickness = new Thickness(1, 1, 0, 0), IsHitTestVisible = false });
            cardShell.Children.Add(new Border { Margin = new Thickness(0, 0, 1, 1), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB3B3B3")), BorderThickness = new Thickness(0, 0, 1, 1), IsHitTestVisible = false });
            cardShell.Children.Add(g);

            card.Child = cardShell;
            root.Children.Add(card);
            var focusOverlay = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3A7BD5")),
                BorderThickness = new Thickness(1),
                Background = Brushes.Transparent,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            root.Children.Add(focusOverlay);
            if (active)
            {
                root.Children.Add(new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3A7BD5")),
                    BorderThickness = new Thickness(2),
                    Background = Brushes.Transparent,
                    IsHitTestVisible = false
                });
            }

            outer.GotKeyboardFocus += delegate { focusOverlay.Visibility = Visibility.Visible; };
            outer.LostKeyboardFocus += delegate { focusOverlay.Visibility = Visibility.Collapsed; };
            outer.Child = root;
            return outer;
        }

        private void RenderQueue()
        {
            QueueListPanel.Children.Clear();
            var visibleQueueOrders = new List<TakeawayOrderRecord>();
            for (int i = 0; i < _takeawayState.OpenOrders.Count; i++)
            {
                var queueOrder = _takeawayState.OpenOrders[i];
                if (queueOrder == null || IsPickupLinkedOrder(queueOrder)) continue;
                visibleQueueOrders.Add(queueOrder);
            }
            if (GetCurrentOrder() == null) _takeawayState.CurrentOrderId = FindFirstRegularTakeawayOrderId();
            if (visibleQueueOrders.Count == 0)
            {
                var empty = CreateCard("لا توجد أوردرات", "—", false, false);
                empty.Cursor = Cursors.Arrow;
                QueueListPanel.Children.Add(empty);
                return;
            }
            for (int i = 0; i < visibleQueueOrders.Count; i++)
            {
                var o = visibleQueueOrders[i];
                var card = CreateCard("أوردر رقم " + o.No, FormatTimeShort(o.CreatedAt), (!_takeawayPickupContextActive) && o.Id == _takeawayState.CurrentOrderId, false);
                card.Tag = o.Id;
                var id = o.Id;
                card.MouseLeftButtonUp += delegate { SetTakeawayQueueContext(id); _selectedRowKey = null; PersistTakeawayState(); RenderTakeawayAll(); RestoreFocusToCardByTag(QueueListPanel, id); if (DeliveryView != null && DeliveryView.Visibility == Visibility.Visible) RenderDeliveryReceipt(); };
                AttachCardKeyboardHandlers(card, QueueListPanel, delegate { SetTakeawayQueueContext(id); _selectedRowKey = null; PersistTakeawayState(); RenderTakeawayAll(); RestoreFocusToCardByTag(QueueListPanel, id); if (DeliveryView != null && DeliveryView.Visibility == Visibility.Visible) RenderDeliveryReceipt(); });
                QueueListPanel.Children.Add(card);
            }
        }

        private void RenderPickups()
        {
            PickupsListPanel.Children.Clear();
            if (_takeawayState.Pickups.Count > 0)
            {
                PickBadgeText.Text = _takeawayState.Pickups.Count.ToString();
                PickBadgeBorder.Visibility = Visibility.Visible;
            }
            else PickBadgeBorder.Visibility = Visibility.Collapsed;

            if (_takeawayState.Pickups.Count == 0)
            {
                var empty = CreateCard("لا يوجد استلام", "—", false, false);
                empty.Cursor = Cursors.Arrow;
                PickupsListPanel.Children.Add(empty);
                return;
            }

            for (int i = 0; i < _takeawayState.Pickups.Count; i++)
            {
                var p = _takeawayState.Pickups[i];
                var pickupName = string.IsNullOrWhiteSpace(p.Name) ? "استلام" : p.Name.Trim();
                var pickupPhone = p.Phone ?? string.Empty;
                var line1 = pickupName;
                var line2 = string.IsNullOrWhiteSpace(pickupPhone) ? "—" : pickupPhone.Trim();
                var card = CreateCard(line1, line2, _takeawayPickupContextActive && p.Id == _takeawayState.ActivePickupId, false);
                card.Tag = p.Id;
                var id = p.Id;
                card.MouseLeftButtonUp += delegate
                {
                    SetTakeawayPickupContext(id);
                    _takeawayPickupSelectedRowKey = null;
                    PersistTakeawayState();
                    RenderTakeawayAll();
                    RestoreFocusToCardByTag(PickupsListPanel, id);
                    if (DeliveryView != null && DeliveryView.Visibility == Visibility.Visible) { RenderDeliverySharedPickups(); RenderDeliveryReceipt(); }
                };
                AttachCardKeyboardHandlers(card, PickupsListPanel, delegate
                {
                    SetTakeawayPickupContext(id);
                    _takeawayPickupSelectedRowKey = null;
                    PersistTakeawayState();
                    RenderTakeawayAll();
                    RestoreFocusToCardByTag(PickupsListPanel, id);
                    if (DeliveryView != null && DeliveryView.Visibility == Visibility.Visible) { RenderDeliverySharedPickups(); RenderDeliveryReceipt(); }
                });
                PickupsListPanel.Children.Add(card);
            }
        }

        private void OpenPickupOrderForm()
        {
            if (PickupOrderOverlay == null) return;
            PickupPhoneTextBox.Text = string.Empty;
            PickupCustomerNameTextBox.Text = string.Empty;
            PickupOrderOverlay.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke(new Action(delegate
            {
                PickupPhoneTextBox.Focus();
                PickupPhoneTextBox.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void ClosePickupOrderForm()
        {
            if (PickupOrderOverlay == null) return;
            PickupOrderOverlay.Visibility = Visibility.Collapsed;
            PickupPhoneTextBox.Text = string.Empty;
            PickupCustomerNameTextBox.Text = string.Empty;
        }

        private void ConfirmPickupOrderForm()
        {
            // [PHASE 7.5 PURE CLOUD] No offline sync blocking - proceed directly.
            var phone = (PickupPhoneTextBox.Text ?? string.Empty).Trim();
            var customerName = (PickupCustomerNameTextBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(customerName)) customerName = "استلام";

            var pickup = new TakeawayPickupRecord
            {
                Id = NewId(),
                No = NextPickupNo(),
                CreatedAt = NowUnixMs(),
                Phone = phone,
                Name = customerName
            };

            _takeawayState.Pickups.Insert(0, pickup);
            SetTakeawayPickupContext(pickup.Id);

            var previousQueueOrderId = _takeawayState.CurrentOrderId;
            var order = CreateOrder(true);
            if (order != null)
            {
                LinkPickupToOrder(order, pickup.Id);
                _takeawayState.CurrentOrderId = previousQueueOrderId;
                PersistTakeawayState();
            }

            _takeawayPickupSelectedRowKey = null;
            ClosePickupOrderForm();
            RenderTakeawayAll();
            RefreshSharedPickupPanels();
        }

        private void TryAssignPickupLinkOnOrder(TakeawayOrderRecord order, string pickupId)
        {
            if (order == null || string.IsNullOrEmpty(pickupId)) return;
            try
            {
                var prop = typeof(TakeawayOrderRecord).GetProperty("PickupId");
                if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string))
                {
                    prop.SetValue(order, pickupId, null);
                    return;
                }
                prop = typeof(TakeawayOrderRecord).GetProperty("ActivePickupId");
                if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string))
                {
                    prop.SetValue(order, pickupId, null);
                }
            }
            catch
            {
            }
        }

        private void LinkPickupToOrder(TakeawayOrderRecord order, string pickupId)
        {
            if (order == null || string.IsNullOrWhiteSpace(pickupId)) return;
            _pickupOrderLinks[order.Id] = pickupId;
            TryAssignPickupLinkOnOrder(order, pickupId);
        }

        private string TryReadPickupLinkFromOrder(TakeawayOrderRecord order)
        {
            if (order == null) return null;
            try
            {
                var prop = typeof(TakeawayOrderRecord).GetProperty("PickupId");
                if (prop != null && prop.CanRead && prop.PropertyType == typeof(string))
                {
                    return prop.GetValue(order, null) as string;
                }
                prop = typeof(TakeawayOrderRecord).GetProperty("ActivePickupId");
                if (prop != null && prop.CanRead && prop.PropertyType == typeof(string))
                {
                    return prop.GetValue(order, null) as string;
                }
            }
            catch
            {
            }
            return null;
        }

        private TakeawayOrderRecord FindOrderLinkedToPickup(string pickupId)
        {
            if (string.IsNullOrWhiteSpace(pickupId) || _takeawayState == null || _takeawayState.OpenOrders == null) return null;
            for (int i = 0; i < _takeawayState.OpenOrders.Count; i++)
            {
                var order = _takeawayState.OpenOrders[i];
                if (order == null || string.IsNullOrEmpty(order.Id)) continue;

                if (_pickupOrderLinks.TryGetValue(order.Id, out var linkedId) && string.Equals(linkedId, pickupId, StringComparison.Ordinal))
                    return order;

                linkedId = TryReadPickupLinkFromOrder(order);
                if (!string.IsNullOrWhiteSpace(linkedId))
                {
                    _pickupOrderLinks[order.Id] = linkedId;
                    if (string.Equals(linkedId, pickupId, StringComparison.Ordinal))
                        return order;
                }
            }
            return null;
        }

        private void TrySelectCurrentOrderForPickup(string pickupId)
        {
            var order = FindOrderLinkedToPickup(pickupId);
            if (order == null) return;
        }

        private void PickupFormOkButton_Click(object sender, RoutedEventArgs e)
        {
            ConfirmPickupOrderForm();
        }

        private void PickupFormCancelButton_Click(object sender, RoutedEventArgs e)
        {
            ClosePickupOrderForm();
        }

        private void PickupOrderModalBorder_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (PickupOrderOverlay.Visibility != Visibility.Visible) return;
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                ClosePickupOrderForm();
                return;
            }
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                ConfirmPickupOrderForm();
            }
        }

        private void PickupKeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            OpenOnScreenKeyboard();
        }

        private bool TryActivateKeyboardProcess(string processName)
        {
            Process[] processes;
            try { processes = Process.GetProcessesByName(processName); }
            catch { return false; }

            for (int i = 0; i < processes.Length; i++)
            {
                try
                {
                    processes[i].Refresh();
                    var h = processes[i].MainWindowHandle;
                    if (h == IntPtr.Zero) continue;

                    try { ShowWindow(h, SW_RESTORE); } catch { }
                    try { ShowWindow(h, SW_SHOW); } catch { }
                    try { SetForegroundWindow(h); } catch { }

                    System.Threading.Thread.Sleep(120);
                    processes[i].Refresh();
                    h = processes[i].MainWindowHandle;
                    if (h != IntPtr.Zero) return true;
                }
                catch
                {
                }
            }

            return false;
        }

        private bool TryStartExecutable(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                return true;
            }
            catch
            {
            }

            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = false });
                return true;
            }
            catch
            {
            }

            return false;
        }

        private bool TryStartExistingPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            if (path.IndexOf("\\") >= 0 && !System.IO.File.Exists(path)) return false;
            return TryStartExecutable(path);
        }

        private void TryKillProcessesByName(string processName)
        {
            Process[] processes;
            try { processes = Process.GetProcessesByName(processName); }
            catch { return; }

            for (int i = 0; i < processes.Length; i++)
            {
                try
                {
                    if (processes[i].HasExited) continue;
                    processes[i].Kill();
                }
                catch
                {
                }
            }
        }

        private bool TryLaunchTabTipAndActivate(string tabTipPath)
        {
            if (!TryStartExistingPath(tabTipPath)) return false;

            for (int i = 0; i < 12; i++)
            {
                if (TryActivateKeyboardProcess("TabTip")) return true;
                System.Threading.Thread.Sleep(120);
            }

            return false;
        }

        private bool TryLaunchOskAndActivate(List<string> oskCandidates)
        {
            for (int i = 0; i < oskCandidates.Count; i++)
            {
                if (!TryStartExistingPath(oskCandidates[i])) continue;

                for (int j = 0; j < 12; j++)
                {
                    if (TryActivateKeyboardProcess("osk")) return true;
                    System.Threading.Thread.Sleep(100);
                }

                return true;
            }

            return false;
        }

        private void OpenOnScreenKeyboard()
        {
            Task.Run(new Action(OpenOnScreenKeyboardWorker));
        }

        private void OpenOnScreenKeyboardWorker()
        {
            var tabTipPath = @"C:\Program Files\Common Files\microsoft shared\ink\TabTip.exe";
            if (TryActivateKeyboardProcess("osk")) return;
            if (TryActivateKeyboardProcess("TabTip")) return;

            var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var oskCandidates = new List<string>();
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                oskCandidates.Add(System.IO.Path.Combine(winDir, "Sysnative", "osk.exe"));
            }
            oskCandidates.Add(System.IO.Path.Combine(winDir, "System32", "osk.exe"));
            oskCandidates.Add(System.IO.Path.Combine(winDir, "SysWOW64", "osk.exe"));
            oskCandidates.Add("osk.exe");

            TryKillProcessesByName("TabTip");
            System.Threading.Thread.Sleep(180);

            if (TryLaunchTabTipAndActivate(tabTipPath)) return;

            if (TryActivateKeyboardProcess("osk")) return;
            TryLaunchOskAndActivate(oskCandidates);
        }

        private Button CreateTileButton(string text, bool active, double minHeight, double fontSize)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Width = 92,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                FlowDirection = FlowDirection.RightToLeft,
                FontWeight = FontWeights.Black,
                FontSize = fontSize,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var textScale = new Viewbox
            {
                Stretch = Stretch.Uniform,
                Child = textBlock
            };

            var button = new Button
            {
                Content = textScale,
                Margin = new Thickness(4),
                Background = active ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2F6FAB")),
                Foreground = active ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2F6FAB")) : Brushes.White,
                BorderBrush = active ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2F6FAB")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1F4F83")),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Padding = new Thickness(6, 5, 6, 5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                MinHeight = minHeight
            };

            var template = new ControlTemplate(typeof(Button));

            var rootBorder = new FrameworkElementFactory(typeof(Border));
            rootBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            rootBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
            rootBorder.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
            rootBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
            rootBorder.SetValue(Border.SnapsToDevicePixelsProperty, true);

            var shell = new FrameworkElementFactory(typeof(Grid));

            var shadeBorder = new FrameworkElementFactory(typeof(Border));
            shadeBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
            shadeBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)));
            shadeBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            shadeBorder.SetValue(UIElement.OpacityProperty, 0.35);
            shadeBorder.SetValue(UIElement.IsHitTestVisibleProperty, false);

            var innerBorder = new FrameworkElementFactory(typeof(Border));
            innerBorder.SetValue(Border.MarginProperty, new Thickness(1));
            innerBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));
            innerBorder.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)));
            innerBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            innerBorder.SetValue(UIElement.IsHitTestVisibleProperty, false);

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Stretch);
            presenter.SetValue(ContentPresenter.MarginProperty, new Thickness(4));

            shell.AppendChild(shadeBorder);
            shell.AppendChild(innerBorder);
            shell.AppendChild(presenter);
            rootBorder.AppendChild(shell);
            template.VisualTree = rootBorder;

            button.Template = template;
            return button;
        }

        private void RenderCategories()
        {
            CategoriesGridPanel.Children.Clear();
            CategoriesGridPanel.Columns = 1;
            for (int i = 0; i < _menu.Count; i++)
            {
                var cat = _menu[i];
                var btn = CreateTileButton(cat.Name, cat.Name == _activeCategory, 46, 11);
                btn.Tag = cat;
                var name = cat.Name;
                btn.Click += delegate { _activeCategory = name; RenderCategories(); RenderItems(); };
                AttachAdminCategoryReorderHandler(btn, false, cat);
                CategoriesGridPanel.Children.Add(btn);
            }
        }

        private async void ItemTile_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var def = btn != null ? btn.Tag as MenuItemDef : null;
            if (def != null && IsMenuItemOutOfStock(def))
            {
                MessageBox.Show("هذا المنتج غير متاح الآن بسبب المخزون.", "Fale7 POS", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (def == null || def.Name == "—") return;
            var oldBg = btn.Background; var oldFg = btn.Foreground; var oldBorder = btn.BorderBrush;
            btn.Background = Brushes.White;
            btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2F6FAB"));
            btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2F6FAB"));
            AddItemToCurrentOrder(def);
            await Task.Delay(140);
            if (btn.IsLoaded) { btn.Background = oldBg; btn.Foreground = oldFg; btn.BorderBrush = oldBorder; }
        }

        private void RenderItems()
        {
            ItemsGridPanel.Children.Clear();
            var cat = GetActiveCategory();
            if (cat == null || cat.Items == null) return;
            ItemsGridPanel.Columns = 4;
            ItemsGridPanel.FlowDirection = FlowDirection.RightToLeft;
            var itemCount = 0;
            for (int i = 0; i < cat.Items.Count; i++)
            {
                if (cat.Items[i] != null && cat.Items[i].Name != "—") itemCount++;
            }
            var rows = itemCount <= 48 ? 12 : (int)Math.Ceiling(itemCount / 4.0);
            if (rows < 12) rows = 12;
            ItemsGridPanel.Rows = rows;
            BuildLocalInventoryReservationCounters(out var productReservations, out var optionReservations);
            for (int i = 0; i < cat.Items.Count; i++)
            {
                var it = cat.Items[i];
                if (it == null || it.Name == "—") continue;
                var statusText = GetMenuItemInventoryBadgeText(it, productReservations, optionReservations);
                var btn = CreateTileButton(string.IsNullOrWhiteSpace(statusText) ? it.Name : (it.Name + "\n" + statusText), false, 72, 13);
                btn.Tag = it;
                ApplyInventoryTileVisualState(btn, it, productReservations, optionReservations);
                btn.Click += ItemTile_Click;
                AttachAdminItemReorderHandler(btn, false, cat, it);
                ItemsGridPanel.Children.Add(btn);
            }
        }

        private int UpdateTotals(TakeawayOrderRecord order)
        {
            var subtotal = 0;
            if (order != null && order.Items != null) for (int i = 0; i < order.Items.Count; i++) subtotal += order.Items[i].Qty * order.Items[i].Price;
            SubtotalValText.Text = subtotal.ToString();
            TaxValText.Text = "0";
            GrandValText.Text = subtotal.ToString();
            return subtotal;
        }

        private Border CreateReceiptCell(string text, bool isDesc, bool selected)
        {
            var cell = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8F8F8F")),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Background = selected ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD7EBFF")) : Brushes.White,
                Padding = isDesc ? new Thickness(6, 6, 10, 6) : new Thickness(6)
            };
            cell.Child = new TextBlock
            {
                Text = text ?? string.Empty,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = isDesc ? TextAlignment.Right : TextAlignment.Center,
                FlowDirection = isDesc ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            return cell;
        }

        private void RenderReceipt()
        {
            ReceiptRowsPanel.Children.Clear();
            var o = GetActiveTakeawayContextOrder();
            if (o == null) { UpdateTotals(null); return; }

            for (int i = 0; i < o.Items.Count; i++)
            {
                var it = o.Items[i];
                var selected = it.Key == GetActiveTakeawayReceiptSelectedRowKey();
                var row = new Grid { Tag = it.Key, FlowDirection = FlowDirection.LeftToRight };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(15, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(15, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(15, GridUnitType.Star) });

                var descCell = CreateReceiptCell(it.Desc, true, selected);
                var qtyCell = CreateReceiptCell(it.Qty.ToString(), false, selected);
                var priceCell = CreateReceiptCell(it.Price.ToString(), false, selected);
                var totalCell = CreateReceiptCell((it.Qty * it.Price).ToString(), false, selected);
                Grid.SetColumn(totalCell, 0); Grid.SetColumn(priceCell, 1); Grid.SetColumn(qtyCell, 2); Grid.SetColumn(descCell, 3);
                row.Children.Add(descCell); row.Children.Add(qtyCell); row.Children.Add(priceCell); row.Children.Add(totalCell);

                var itemKey = it.Key;
                row.MouseLeftButtonUp += delegate { SetActiveTakeawayReceiptSelectedRowKey(itemKey); RenderReceipt(); };
                descCell.MouseLeftButtonDown += delegate (object s, MouseButtonEventArgs e)
                {
                    if (e.ClickCount >= 2) { e.Handled = true; RemoveItemFromCurrentOrder(itemKey); }
                };
                qtyCell.Cursor = Cursors.Hand;
                qtyCell.ToolTip = "اضغط لتعديل الكمية";
                qtyCell.MouseLeftButtonUp += delegate { OpenQtyPad(itemKey); };

                ReceiptRowsPanel.Children.Add(row);
            }

            UpdateTotals(o);
        }

        private void AddItemToCurrentOrder(MenuItemDef def)
        {
            if (def == null) return;
            if (IsMenuItemOutOfStock(def)) return;
            var desc = def.Name;
            var price = ResolvePosTileFinalPrice(def);
            var o = EnsureOrder();
            TakeawayLineRecord existing = null;
            for (int i = 0; i < o.Items.Count; i++)
            {
                var existingOptionIds = o.Items[i].OptionIds ?? new List<string>();
                var targetOptionIds = def.FixedOptionIds ?? new List<string>();
                var sameOptions = string.Equals(string.Join("|", existingOptionIds), string.Join("|", targetOptionIds), StringComparison.Ordinal);
                if (o.Items[i].Desc == desc
                    && string.Equals((o.Items[i].ProductId ?? string.Empty), (def.ProductId ?? string.Empty), StringComparison.Ordinal)
                    && sameOptions)
                {
                    existing = o.Items[i];
                    break;
                }
            }
            if (existing != null) existing.Qty += 1;
            else o.Items.Add(new TakeawayLineRecord
            {
                Key = NewId(),
                Desc = desc,
                Qty = 1,
                Price = price,
                ProductId = def.ProductId,
                SourceProductId = def.SourceProductId,
                OptionIds = def.FixedOptionIds == null ? new List<string>() : new List<string>(def.FixedOptionIds)
            });
            PersistTakeawayState();
            RenderReceipt();
            RenderQueue();
            RefreshMenuInventoryTiles();
        }

        private void RemoveItemFromCurrentOrder(string itemKey)
        {
            var o = GetActiveTakeawayContextOrder();
            if (o == null || o.Items == null) return;
            for (int i = o.Items.Count - 1; i >= 0; i--)
            {
                if (o.Items[i].Key == itemKey) o.Items.RemoveAt(i);
            }
            if (GetActiveTakeawayReceiptSelectedRowKey() == itemKey) ClearActiveTakeawayReceiptSelectedRowKey();
            PersistTakeawayState();
            RenderReceipt();
            RefreshMenuInventoryTiles();
        }

        private void ClearCurrentOrderWithConfirm()
        {
            var o = GetActiveTakeawayContextOrder();
            if (o == null) return;
            var result = MessageBox.Show("مسح أصناف الأوردر الحالي؟", "تأكيد المسح", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            o.Items.Clear();
            ClearActiveTakeawayReceiptSelectedRowKey();
            PersistTakeawayState();
            RenderReceipt();
            RefreshMenuInventoryTiles();
        }

        private Grid FindTakeawayShiftOverlay()
        {
            return FindName("TakeawayShiftOverlay") as Grid;
        }

        private StackPanel FindTakeawayShiftRowsPanel()
        {
            return FindName("TakeawayShiftRowsPanel") as StackPanel;
        }

        private void OpenTakeawayShiftOverlay()
        {
            var overlay = FindTakeawayShiftOverlay();
            if (overlay == null) return;
            NormalizeTakeawayState();
            RenderTakeawayShiftRows();
            overlay.Visibility = Visibility.Visible;
        }

        private void CloseTakeawayShiftOverlay()
        {
            var overlay = FindTakeawayShiftOverlay();
            if (overlay != null) overlay.Visibility = Visibility.Collapsed;
        }

        private void TakeawayShiftBackButton_Click(object sender, RoutedEventArgs e)
        {
            CloseTakeawayShiftOverlay();
        }

        private void TakeawayShiftReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdminMode)
            {
                MessageBox.Show("طباعة التقرير متاحة للأدمن فقط.", "نقاط البيع - الوردية", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var list = GetUnifiedShiftRecordsForMigration();
            if (list.Count == 0)
            {
                MessageBox.Show("لا توجد أوردرات محفوظة داخل الشيفت الحالي.", "نقاط البيع - الوردية", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            TryPrintFlowDocumentWithDialog(BuildPosShiftReportDocument(list, "كل الجهاز"));
        }

        private void TakeawayShiftMigrateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdminMode)
            {
                MessageBox.Show("ترحيل الشيفت متاح للأدمن فقط.", "نقاط البيع - الوردية", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            RunPosShiftMigrationFlow();
            RenderTakeawayShiftRows();
        }

        private void RenderTakeawayShiftRows()
        {
            var rowsPanel = FindTakeawayShiftRowsPanel();
            if (rowsPanel == null) return;
            rowsPanel.Children.Clear();
            NormalizeTakeawayState();

            var list = GetUnifiedShiftRecordsForMigration();

            if (list.Count == 0)
            {
                rowsPanel.Children.Add(new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF999999")),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7F7F7")),
                    Padding = new Thickness(10),
                    Child = new TextBlock
                    {
                        Text = "لا توجد أوردرات في الشيفت الحالي.",
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FlowDirection = FlowDirection.RightToLeft
                    }
                });
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var rowData = list[i];
                if (rowData == null) continue;

                var row = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD0D0D0")),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFAFAFA")),
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(8)
                };

                var grid = new Grid { FlowDirection = FlowDirection.RightToLeft };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                if (_isAdminMode) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

                var orderRef = ResolveShiftOrderRef(rowData);
                var channel = (rowData.ChannelKind ?? string.Empty).Trim().ToUpperInvariant();
                var channelLabel = "تيك أواي";
                if (channel == "PICKUP") channelLabel = "استلام/انتظار";
                else if (channel == "DELIVERY") channelLabel = "دليفري";
                var source = (rowData.SourceKind ?? string.Empty).Trim().ToUpperInvariant();
                var sourceLabel = source == "APP" ? "تطبيق" : "يدوي";
                var when = rowData.FinalizedAtMillis > 0 ? rowData.FinalizedAtMillis : rowData.CreatedAtMillis;
                var timeText = when > 0
                    ? DateTimeOffset.FromUnixTimeMilliseconds(when).ToLocalTime().ToString("g", _arEg)
                    : "-";

                grid.Children.Add(CreateTakeawayShiftCell(orderRef, FlowDirection.LeftToRight, TextAlignment.Center, 0));
                grid.Children.Add(CreateTakeawayShiftCell(channelLabel, FlowDirection.RightToLeft, TextAlignment.Center, 1));
                grid.Children.Add(CreateTakeawayShiftCell(sourceLabel, FlowDirection.RightToLeft, TextAlignment.Center, 2));
                grid.Children.Add(CreateTakeawayShiftCell((rowData.CustomerName ?? string.Empty).Trim().Length == 0 ? "-" : rowData.CustomerName.Trim(), FlowDirection.RightToLeft, TextAlignment.Center, 3));
                grid.Children.Add(CreateTakeawayShiftCell((rowData.Total < 0 ? 0 : rowData.Total).ToString(), FlowDirection.LeftToRight, TextAlignment.Center, 4));
                grid.Children.Add(CreateTakeawayShiftCell(timeText, FlowDirection.RightToLeft, TextAlignment.Center, 5));

                if (_isAdminMode)
                {
                    var reprintButton = new Button
                    {
                        Content = "طباعة نسخة",
                        Height = 30,
                        Margin = new Thickness(4),
                        FontWeight = FontWeights.Black,
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF7F7F7F"))
                    };
                    var captured = rowData;
                    reprintButton.Click += delegate
                    {
                        TryPrintFlowDocument(BuildPosShiftOrderCopyDocument(captured));
                    };
                    Grid.SetColumn(reprintButton, 6);
                    grid.Children.Add(reprintButton);
                }

                row.Child = grid;
                rowsPanel.Children.Add(row);
            }
        }

        private Border CreateTakeawayShiftCell(string text, FlowDirection direction, TextAlignment align, int col)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDDDDDD")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6)
            };
            border.Child = new TextBlock
            {
                Text = text ?? string.Empty,
                FontWeight = FontWeights.Bold,
                TextAlignment = align,
                FlowDirection = direction,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(border, col);
            return border;
        }

        private FlowDocument BuildPosShiftOrderCopyDocument(PosShiftOrderRecord record)
        {
            var doc = new FlowDocument { FontFamily = new FontFamily("Tahoma"), FontSize = 12, FlowDirection = FlowDirection.RightToLeft };
            var channel = (record == null ? string.Empty : (record.ChannelKind ?? string.Empty).Trim().ToUpperInvariant());
            var channelLabel = "تيك أواي";
            if (channel == "PICKUP") channelLabel = "استلام/انتظار";
            else if (channel == "DELIVERY") channelLabel = "دليفري";
            var source = (record == null ? string.Empty : (record.SourceKind ?? string.Empty).Trim().ToUpperInvariant());
            var sourceLabel = source == "APP" ? "تطبيق" : "يدوي";
            var orderRef = ResolveShiftOrderRef(record);

            doc.Blocks.Add(new Paragraph(new Run("نسخة فاتورة الشيفت")) { FontSize = 18, FontWeight = FontWeights.Black, Margin = new Thickness(0, 0, 0, 8) });
            doc.Blocks.Add(new Paragraph(new Run("رقم الطلب: " + orderRef + " — النوع: " + channelLabel + " — المصدر: " + sourceLabel)) { Margin = new Thickness(0, 0, 0, 4) });
            doc.Blocks.Add(new Paragraph(new Run("العميل: " + (record == null || string.IsNullOrWhiteSpace(record.CustomerName) ? "-" : record.CustomerName))) { Margin = new Thickness(0, 0, 0, 4) });
            doc.Blocks.Add(new Paragraph(new Run("الهاتف: " + (record == null || string.IsNullOrWhiteSpace(record.CustomerPhone) ? "-" : record.CustomerPhone))) { Margin = new Thickness(0, 0, 0, 6) });
            doc.Blocks.Add(new Paragraph(new Run("الإجمالي: " + (record == null ? 0 : (record.Total < 0 ? 0 : record.Total)))) { FontWeight = FontWeights.Black, Margin = new Thickness(0, 6, 0, 0) });
            return doc;
        }

        private string ResolveShiftOrderRef(PosShiftOrderRecord record)
        {
            if (record == null) return "-";
            if (record.OrderNo > 0) return record.OrderNo.ToString();
            if (!string.IsNullOrWhiteSpace(record.DisplayRef))
            {
                var trimmed = record.DisplayRef.Trim();
                int parsedRef;
                if (int.TryParse(trimmed, out parsedRef) && parsedRef > 0) return parsedRef.ToString();
            }

            var fallback = ExtractShiftTrailingDigits(record.DisplayRef, 6);
            if (fallback <= 0) fallback = ExtractShiftTrailingDigits(record.BackendOrderId, 6);
            if (fallback <= 0) fallback = ExtractShiftTrailingDigits(record.CustomerPhone, 6);
            return fallback > 0 ? fallback.ToString() : "---";
        }

        private int ExtractShiftTrailingDigits(string raw, int maxDigits)
        {
            if (string.IsNullOrWhiteSpace(raw) || maxDigits <= 0) return 0;
            var digits = new List<char>();
            for (int i = 0; i < raw.Length; i++)
            {
                var ch = raw[i];
                if (char.IsDigit(ch)) digits.Add(ch);
            }
            if (digits.Count == 0) return 0;
            var take = Math.Min(maxDigits, digits.Count);
            var start = digits.Count - take;
            var text = new string(digits.GetRange(start, take).ToArray());
            int parsed;
            return int.TryParse(text, out parsed) ? parsed : 0;
        }

        private void NormalizePosShiftOrderRecord(PosShiftOrderRecord record)
        {
            if (record == null) return;
            if (string.IsNullOrWhiteSpace(record.BackendOrderId)) record.BackendOrderId = "pos_" + NewId();
            if (string.IsNullOrWhiteSpace(record.SourceKind)) record.SourceKind = "MANUAL";
            if (string.IsNullOrWhiteSpace(record.ChannelKind)) record.ChannelKind = "TAKEAWAY";
            if (record.Subtotal < 0) record.Subtotal = 0;
            if (record.DeliveryFee < 0) record.DeliveryFee = 0;
            if (record.Total < 0) record.Total = 0;
            if (record.Total <= 0) record.Total = record.Subtotal + record.DeliveryFee;

            if (record.CreatedAtMillis <= 0) record.CreatedAtMillis = NowUnixMs();
            if (record.FinalizedAtMillis <= 0) record.FinalizedAtMillis = record.CreatedAtMillis;
            var sourceKind = (record.SourceKind ?? string.Empty).Trim().ToUpperInvariant();
            if (sourceKind != "APP")
            {
                if (string.IsNullOrWhiteSpace(record.CashierUserId)) record.CashierUserId = GetCurrentSessionUserId();
                if (string.IsNullOrWhiteSpace(record.CashierUsername)) record.CashierUsername = GetCurrentSessionDisplayName();
                if (string.IsNullOrWhiteSpace(record.CashierRole)) record.CashierRole = GetCurrentBackendActorRole();
            }
            record.CashierUserId = (record.CashierUserId ?? string.Empty).Trim();
            record.CashierUsername = (record.CashierUsername ?? string.Empty).Trim();
            record.CashierRole = (record.CashierRole ?? string.Empty).Trim().ToUpperInvariant();

            var orderNo = record.OrderNo;
            if (orderNo <= 0 && !string.IsNullOrWhiteSpace(record.DisplayRef))
            {
                var trimmedRef = record.DisplayRef.Trim();
                int parsedOrderNo;
                if (int.TryParse(trimmedRef, out parsedOrderNo) && parsedOrderNo > 0) orderNo = parsedOrderNo;
            }
            if (orderNo <= 0) orderNo = ExtractShiftTrailingDigits(record.DisplayRef, 6);
            if (orderNo <= 0) orderNo = ExtractShiftTrailingDigits(record.BackendOrderId, 6);
            if (orderNo <= 0) orderNo = ExtractShiftTrailingDigits(record.CustomerPhone, 6);
            if (orderNo > 0) record.OrderNo = orderNo;

            var trimmedDisplay = (record.DisplayRef ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedDisplay) || trimmedDisplay == "0" || trimmedDisplay == "---")
            {
                record.DisplayRef = record.OrderNo > 0 ? record.OrderNo.ToString() : record.BackendOrderId.Trim();
            }
        }

        private void TrackCompletedTakeawayShiftOrder(TakeawayOrderRecord order, bool isPickup, TakeawayPickupRecord pickup)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.Id)) return;

            var subtotal = 0;
            if (order.Items != null)
            {
                for (int i = 0; i < order.Items.Count; i++)
                {
                    var it = order.Items[i];
                    if (it == null) continue;
                    var qty = it.Qty < 0 ? 0 : it.Qty;
                    var price = it.Price < 0 ? 0 : it.Price;
                    subtotal += qty * price;
                }
            }

            var record = new PosShiftOrderRecord
            {
                BackendOrderId = "pos_" + order.Id.Trim(),
                SourceKind = "MANUAL",
                ChannelKind = isPickup ? "PICKUP" : "TAKEAWAY",
                OrderNo = order.No,
                DisplayRef = order.No > 0 ? order.No.ToString() : order.Id,
                CustomerName = isPickup ? (pickup == null ? "استلام" : (pickup.Name ?? string.Empty)) : "عميل كاشير",
                CustomerPhone = isPickup ? (pickup == null ? string.Empty : (pickup.Phone ?? string.Empty)) : string.Empty,
                Subtotal = subtotal < 0 ? 0 : subtotal,
                DeliveryFee = 0,
                Total = subtotal < 0 ? 0 : subtotal,
                CreatedAtMillis = order.CreatedAt > 0 ? order.CreatedAt : NowUnixMs(),
                FinalizedAtMillis = NowUnixMs(),
                CashierUserId = GetCurrentSessionUserId(),
                CashierUsername = GetCurrentSessionDisplayName(),
                CashierRole = GetCurrentBackendActorRole()
            };

            AppendPosShiftOrderRecord(record);
        }

        private void AppendPosShiftOrderRecord(PosShiftOrderRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.BackendOrderId)) return;
            NormalizeTakeawayState();
            NormalizePosShiftOrderRecord(record);
            var list = _takeawayState == null ? null : _takeawayState.ShiftRecords;
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                var existing = list[i];
                if (existing == null) continue;
                if (!string.Equals(existing.BackendOrderId, record.BackendOrderId, StringComparison.OrdinalIgnoreCase)) continue;

                existing.SourceKind = record.SourceKind;
                existing.ChannelKind = record.ChannelKind;
                if (record.OrderNo > 0 || existing.OrderNo <= 0) existing.OrderNo = record.OrderNo;
                if (!string.IsNullOrWhiteSpace(record.DisplayRef)) existing.DisplayRef = record.DisplayRef;
                existing.CustomerName = record.CustomerName;
                existing.CustomerPhone = record.CustomerPhone;
                existing.Subtotal = record.Subtotal;
                existing.DeliveryFee = record.DeliveryFee;
                existing.Total = record.Total;
                existing.CreatedAtMillis = record.CreatedAtMillis;
                existing.FinalizedAtMillis = record.FinalizedAtMillis;
                existing.CashierUserId = record.CashierUserId;
                existing.CashierUsername = record.CashierUsername;
                existing.CashierRole = record.CashierRole;
                NormalizePosShiftOrderRecord(existing);
                PersistTakeawayState();
                var overlay = FindTakeawayShiftOverlay();
                if (overlay != null && overlay.Visibility == Visibility.Visible)
                {
                    RenderTakeawayShiftRows();
                }
                return;
            }

            list.Insert(0, record);
            PersistTakeawayState();
            var refreshOverlay = FindTakeawayShiftOverlay();
            if (refreshOverlay != null && refreshOverlay.Visibility == Visibility.Visible)
            {
                RenderTakeawayShiftRows();
            }
        }

        private List<PosShiftOrderRecord> GetScopedTakeawayShiftRecordsForAdmin(out string scopeCashierUserId, out string scopeCashierUsername)
        {
            scopeCashierUserId = string.Empty;
            scopeCashierUsername = string.Empty;
            NormalizeTakeawayState();

            var all = _takeawayState == null || _takeawayState.ShiftRecords == null
                ? new List<PosShiftOrderRecord>()
                : _takeawayState.ShiftRecords;
            if (!_isAdminMode || all.Count == 0)
            {
                return new List<PosShiftOrderRecord>(all);
            }

            PosShiftOrderRecord anchor = null;
            var anchorWhen = long.MinValue;
            for (int i = 0; i < all.Count; i++)
            {
                var row = all[i];
                if (row == null) continue;
                var rowCashierUserId = (row.CashierUserId ?? string.Empty).Trim();
                var rowCashierUsername = (row.CashierUsername ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(rowCashierUserId) && string.IsNullOrWhiteSpace(rowCashierUsername)) continue;
                var when = row.FinalizedAtMillis > 0 ? row.FinalizedAtMillis : row.CreatedAtMillis;
                if (anchor == null || when > anchorWhen)
                {
                    anchor = row;
                    anchorWhen = when;
                }
            }

            if (anchor == null)
            {
                return new List<PosShiftOrderRecord>(all);
            }

            scopeCashierUserId = (anchor.CashierUserId ?? string.Empty).Trim();
            scopeCashierUsername = (anchor.CashierUsername ?? string.Empty).Trim();

            var scoped = new List<PosShiftOrderRecord>();
            for (int i = 0; i < all.Count; i++)
            {
                var row = all[i];
                if (!PosShiftRecordMatchesCashierScope(row, scopeCashierUserId, scopeCashierUsername)) continue;
                scoped.Add(row);
            }

            return scoped.Count > 0 ? scoped : new List<PosShiftOrderRecord>(all);
        }

        private bool PosShiftRecordMatchesCashierScope(PosShiftOrderRecord row, string scopeCashierUserId, string scopeCashierUsername)
        {
            if (row == null) return false;

            var rowCashierUserId = (row.CashierUserId ?? string.Empty).Trim();
            var rowCashierUsername = (row.CashierUsername ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(scopeCashierUserId) && !string.IsNullOrWhiteSpace(rowCashierUserId))
            {
                return string.Equals(rowCashierUserId, scopeCashierUserId, StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrWhiteSpace(scopeCashierUsername))
            {
                return string.Equals(rowCashierUsername, scopeCashierUsername, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        private string BuildTakeawayShiftScopeLabel(string scopeCashierUserId, string scopeCashierUsername)
        {
            if (!string.IsNullOrWhiteSpace(scopeCashierUsername)) return scopeCashierUsername.Trim();
            if (!string.IsNullOrWhiteSpace(scopeCashierUserId)) return scopeCashierUserId.Trim();
            return string.Empty;
        }

        private List<PosShiftOrderRecord> GetUnifiedShiftRecordsForMigration()
        {
            NormalizeTakeawayState();
            EnsureDeliveryStateLoaded();

            var list = new List<PosShiftOrderRecord>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var takeawayShift = _takeawayState == null ? null : _takeawayState.ShiftRecords;
            if (takeawayShift != null)
            {
                for (int i = 0; i < takeawayShift.Count; i++)
                {
                    var src = takeawayShift[i];
                    if (src == null) continue;
                    var copy = ClonePosShiftOrderRecord(src);
                    NormalizePosShiftOrderRecord(copy);
                    var backendId = (copy.BackendOrderId ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(backendId)) continue;
                    if (!seen.Add(backendId)) continue;
                    list.Add(copy);
                }
            }

            for (int i = 0; i < _deliveryShiftOrders.Count; i++)
            {
                var mapped = ConvertDeliveryShiftOrderToPosShiftOrder(_deliveryShiftOrders[i]);
                if (mapped == null) continue;
                var backendId = (mapped.BackendOrderId ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(backendId)) continue;
                if (!seen.Add(backendId)) continue;
                list.Add(mapped);
            }

            list.Sort(delegate (PosShiftOrderRecord a, PosShiftOrderRecord b)
            {
                var aWhen = a == null ? 0 : (a.FinalizedAtMillis > 0 ? a.FinalizedAtMillis : a.CreatedAtMillis);
                var bWhen = b == null ? 0 : (b.FinalizedAtMillis > 0 ? b.FinalizedAtMillis : b.CreatedAtMillis);
                if (aWhen == bWhen) return string.Compare(
                    a == null ? string.Empty : (a.BackendOrderId ?? string.Empty),
                    b == null ? string.Empty : (b.BackendOrderId ?? string.Empty),
                    StringComparison.OrdinalIgnoreCase);
                return bWhen.CompareTo(aWhen);
            });

            return list;
        }

        private PosShiftOrderRecord ClonePosShiftOrderRecord(PosShiftOrderRecord src)
        {
            if (src == null) return null;
            return new PosShiftOrderRecord
            {
                BackendOrderId = src.BackendOrderId,
                SourceKind = src.SourceKind,
                ChannelKind = src.ChannelKind,
                OrderNo = src.OrderNo,
                DisplayRef = src.DisplayRef,
                CustomerName = src.CustomerName,
                CustomerPhone = src.CustomerPhone,
                Subtotal = src.Subtotal,
                DeliveryFee = src.DeliveryFee,
                Total = src.Total,
                CreatedAtMillis = src.CreatedAtMillis,
                FinalizedAtMillis = src.FinalizedAtMillis,
                CashierUserId = src.CashierUserId,
                CashierUsername = src.CashierUsername,
                CashierRole = src.CashierRole
            };
        }

        private PosShiftOrderRecord ConvertDeliveryShiftOrderToPosShiftOrder(DeliveryOrderRecord order)
        {
            if (order == null) return null;
            var localOrderId = (order.Id ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(localOrderId)) return null;

            var backendOrderId = localOrderId.StartsWith("pos_", StringComparison.OrdinalIgnoreCase)
                ? localOrderId
                : BuildPosOfflineBackendOrderId(localOrderId);

            var subtotal = order.Subtotal < 0 ? 0 : order.Subtotal;
            var deliveryFee = order.DeliveryFee < 0 ? 0 : order.DeliveryFee;
            var total = order.Total > 0 ? order.Total : (subtotal + deliveryFee);
            if (total < 0) total = 0;

            var record = new PosShiftOrderRecord
            {
                BackendOrderId = backendOrderId,
                SourceKind = "MANUAL",
                ChannelKind = "DELIVERY",
                OrderNo = order.No,
                DisplayRef = order.No > 0 ? order.No.ToString() : localOrderId,
                CustomerName = order.CustomerName ?? string.Empty,
                CustomerPhone = order.CustomerPhone ?? string.Empty,
                Subtotal = subtotal,
                DeliveryFee = deliveryFee,
                Total = total,
                CreatedAtMillis = order.CreatedAt > 0 ? order.CreatedAt : NowUnixMs(),
                FinalizedAtMillis = order.CreatedAt > 0 ? order.CreatedAt : NowUnixMs(),
                CashierUserId = order.CashierUserId,
                CashierUsername = order.CashierUsername,
                CashierRole = order.CashierRole
            };
            NormalizePosShiftOrderRecord(record);
            return record;
        }

        private List<string> CollectMigratedBackendOrderIds(List<PosShiftOrderRecord> records)
        {
            var list = new List<string>();
            if (records == null || records.Count == 0) return list;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < records.Count; i++)
            {
                var row = records[i];
                if (row == null) continue;
                var id = (row.BackendOrderId ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!seen.Add(id)) continue;
                list.Add(id);
            }
            return list;
        }

        private List<string> CollectLocalAppBackendOrderIdsForMigration()
        {
            var list = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _deliveryAppDeliveryOrders.Count; i++)
            {
                var order = _deliveryAppDeliveryOrders[i];
                var id = (order == null ? string.Empty : order.Id) ?? string.Empty;
                id = id.Trim();
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!seen.Add(id)) continue;
                list.Add(id);
            }

            for (int i = 0; i < _deliveryAppPickupOrders.Count; i++)
            {
                var order = _deliveryAppPickupOrders[i];
                var id = (order == null ? string.Empty : order.Id) ?? string.Empty;
                id = id.Trim();
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!seen.Add(id)) continue;
                list.Add(id);
            }

            return list;
        }

        private void AppendMissingBackendOrderIds(List<string> target, List<string> source)
        {
            if (target == null || source == null || source.Count == 0) return;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < target.Count; i++)
            {
                var id = (target[i] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(id)) continue;
                seen.Add(id);
            }

            for (int i = 0; i < source.Count; i++)
            {
                var id = (source[i] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (!seen.Add(id)) continue;
                target.Add(id);
            }
        }

        private void ResetLocalShiftAfterMigration()
        {
            NormalizeTakeawayState();

            if (_takeawayState == null) _takeawayState = new TakeawayStateRecord();
            if (_takeawayState.OpenOrders == null) _takeawayState.OpenOrders = new List<TakeawayOrderRecord>();
            if (_takeawayState.Pickups == null) _takeawayState.Pickups = new List<TakeawayPickupRecord>();
            if (_takeawayState.ShiftRecords == null) _takeawayState.ShiftRecords = new List<PosShiftOrderRecord>();
            _takeawayState.ShiftRecords.Clear();
            _takeawayState.OpenOrders.Clear();
            _takeawayState.Pickups.Clear();
            _takeawayState.CurrentOrderId = null;
            _takeawayState.ActivePickupId = null;
            _takeawayPickupContextActive = false;
            _deliveryPickupContextActive = false;
            _takeawayState.OrderSequence = 0;
            _takeawayState.PickupSequence = 0;

            _takeawayPickupSelectedRowKey = null;
            _selectedRowKey = null;
            ClearActiveTakeawayReceiptSelectedRowKey();
            PersistTakeawayState();

            EnsureDeliveryStateLoaded();
            _deliveryShiftOrders.Clear();
            _deliveryOrders.Clear();
            _deliveryCurrentOrderId = null;
            _deliveryOrderSequence = 0;
            _deliveryAppDriverAssignTargetId = null;

            _deliverySelectedRowKey = null;
            PersistDeliveryState();
        }

        private int GetMaxTakeawayOpenOrderNo(List<TakeawayOrderRecord> list)
        {
            var max = 0;
            if (list == null) return 0;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null || row.No <= 0) continue;
                if (row.No > max) max = row.No;
            }
            return max;
        }

        private int GetMaxTakeawayPickupNo(List<TakeawayPickupRecord> list)
        {
            var max = 0;
            if (list == null) return 0;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null || row.No <= 0) continue;
                if (row.No > max) max = row.No;
            }
            return max;
        }

        private int GetMaxDeliveryOpenOrderNo(List<DeliveryOrderRecord> list)
        {
            var max = 0;
            if (list == null) return 0;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null || row.No <= 0) continue;
                if (row.No > max) max = row.No;
            }
            return max;
        }

        private int GetMaxAppDeliveryOrderNo(List<AppDeliveryOrderRecord> list)
        {
            var max = 0;
            if (list == null) return 0;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null || row.No <= 0) continue;
                if (row.No > max) max = row.No;
            }
            return max;
        }

        private int GetMaxAppPickupOrderNo(List<AppPickupOrderRecord> list)
        {
            var max = 0;
            if (list == null) return 0;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null || row.No <= 0) continue;
                if (row.No > max) max = row.No;
            }
            return max;
        }

        private void RemoveFinalizedLocalAppOrdersByMigrationIds(List<string> migratedOrderIds)
        {
            if (migratedOrderIds == null || migratedOrderIds.Count == 0) return;

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < migratedOrderIds.Count; i++)
            {
                var id = (migratedOrderIds[i] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(id)) continue;
                ids.Add(id);
            }
            if (ids.Count == 0) return;

            for (int i = _deliveryAppDeliveryOrders.Count - 1; i >= 0; i--)
            {
                var order = _deliveryAppDeliveryOrders[i];
                if (order == null)
                {
                    _deliveryAppDeliveryOrders.RemoveAt(i);
                    continue;
                }
                if (order.Status != AppDeliveryOrderStatus.Done) continue;
                if (!IsMigratedBackendOrderId(ids, order.Id)) continue;
                _deliveryAppDeliveryOrders.RemoveAt(i);
            }

            for (int i = _deliveryAppPickupOrders.Count - 1; i >= 0; i--)
            {
                var order = _deliveryAppPickupOrders[i];
                if (order == null)
                {
                    _deliveryAppPickupOrders.RemoveAt(i);
                    continue;
                }
                if (order.Status != AppPickupOrderStatus.Delivered) continue;
                if (!IsMigratedBackendOrderId(ids, order.Id)) continue;
                _deliveryAppPickupOrders.RemoveAt(i);
            }
        }

        private bool IsMigratedBackendOrderId(HashSet<string> ids, string orderId)
        {
            if (ids == null || ids.Count == 0) return false;
            var id = (orderId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id)) return false;
            if (ids.Contains(id)) return true;

            var backendId = id.StartsWith("pos_", StringComparison.OrdinalIgnoreCase)
                ? id
                : BuildPosOfflineBackendOrderId(id);
            return ids.Contains(backendId);
        }

        private void RemoveTakeawayShiftRecordsByBackendIds(List<string> backendOrderIds)
        {
            if (backendOrderIds == null || backendOrderIds.Count == 0) return;

            NormalizeTakeawayState();
            var list = _takeawayState == null ? null : _takeawayState.ShiftRecords;
            if (list == null || list.Count == 0) return;

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < backendOrderIds.Count; i++)
            {
                var id = (backendOrderIds[i] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(id)) continue;
                ids.Add(id);
            }
            if (ids.Count == 0) return;

            var changed = false;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var row = list[i];
                if (row == null)
                {
                    list.RemoveAt(i);
                    changed = true;
                    continue;
                }

                var rowId = (row.BackendOrderId ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(rowId)) continue;
                if (!ids.Contains(rowId)) continue;

                list.RemoveAt(i);
                changed = true;
            }

            if (changed) PersistTakeawayState();
        }

        private void RunPosShiftMigrationFlow()
        {
            if (!_isAdminMode)
            {
                MessageBox.Show("ترحيل الشيفت متاح للأدمن فقط.", "نقاط البيع - الوردية", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var scoped = GetUnifiedShiftRecordsForMigration();

            if (scoped.Count == 0)
            {
                MessageBox.Show("لا توجد أوردرات محفوظة داخل الشيفت الحالي.", "نقاط البيع - الوردية", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                "سيتم طباعة تقرير الشيفت ثم تصفير الأرشيف المحلي (الأوردرات المرحّلة/الحسابات/فواتير الشيفت) على هذا الجهاز." +
                "\nسيتم أيضًا إرسال حالة MIGRATED للباك اند لضمان المزامنة مع باقي الأجهزة." +
                "\nهل تريد الاستمرار؟",
                "ترحيل الشيفت",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            if (!TryPrintFlowDocumentWithDialog(BuildPosShiftReportDocument(scoped, "كل الجهاز")))
            {
                MessageBox.Show("تم إلغاء الطباعة. لم يتم ترحيل الشيفت.", "نقاط البيع - الوردية", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var migratedOrderIds = CollectMigratedBackendOrderIds(scoped);
            var localAppOrderIds = CollectLocalAppBackendOrderIdsForMigration();
            AppendMissingBackendOrderIds(migratedOrderIds, localAppOrderIds);

            ResetLocalShiftAfterMigration();
            ClearPosIdempotencyQueueAfterMigration();
            EnqueuePosShiftMigrationByOrderIds(migratedOrderIds);
            QueuePosOfflineSync();
            System.Threading.Tasks.Task.Run(async delegate
            {
                var cfg = LoadSupabaseClientConfigResolved();
                if (cfg != null)
                {
                    await ResetPosDeviceCountersGlobalAsync(cfg).ConfigureAwait(false);
                }
            });


            var overlay = FindTakeawayShiftOverlay();
            if (overlay != null && overlay.Visibility == Visibility.Visible)
            {
                RenderTakeawayShiftRows();
            }
            if (DeliveryShiftOverlay != null && DeliveryShiftOverlay.Visibility == Visibility.Visible)
            {
                RenderDeliveryShiftRows();
            }

            RenderTakeawayAll();
            RenderDeliveryAll();
            MessageBox.Show("تم ترحيل الشيفت وتصفير الأرشيف المحلي بنجاح.", "نقاط البيع - الوردية", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private FlowDocument BuildPosShiftReportDocument(List<PosShiftOrderRecord> records, string scopeLabel = null)
        {
            var list = records ?? new List<PosShiftOrderRecord>();
            var takeawayManualCount = 0;
            var takeawayManualTotal = 0;
            var takeawayAppCount = 0;
            var takeawayAppTotal = 0;
            var pickupManualCount = 0;
            var pickupManualTotal = 0;
            var pickupAppCount = 0;
            var pickupAppTotal = 0;
            var deliveryManualCount = 0;
            var deliveryManualTotal = 0;
            var deliveryAppCount = 0;
            var deliveryAppTotal = 0;

            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null) continue;
                var channel = (row.ChannelKind ?? string.Empty).Trim().ToUpperInvariant();
                var source = (row.SourceKind ?? string.Empty).Trim().ToUpperInvariant();
                var total = row.Total < 0 ? 0 : row.Total;

                if (channel == "PICKUP")
                {
                    if (source == "APP")
                    {
                        pickupAppCount++;
                        pickupAppTotal += total;
                    }
                    else
                    {
                        pickupManualCount++;
                        pickupManualTotal += total;
                    }
                }
                else if (channel == "DELIVERY")
                {
                    if (source == "APP")
                    {
                        deliveryAppCount++;
                        deliveryAppTotal += total;
                    }
                    else
                    {
                        deliveryManualCount++;
                        deliveryManualTotal += total;
                    }
                }
                else
                {
                    if (source == "APP")
                    {
                        takeawayAppCount++;
                        takeawayAppTotal += total;
                    }
                    else
                    {
                        takeawayManualCount++;
                        takeawayManualTotal += total;
                    }
                }
            }

            var takeawayCount = takeawayManualCount + takeawayAppCount;
            var takeawayTotal = takeawayManualTotal + takeawayAppTotal;
            var pickupCount = pickupManualCount + pickupAppCount;
            var pickupTotal = pickupManualTotal + pickupAppTotal;
            var deliveryCount = deliveryManualCount + deliveryAppCount;
            var deliveryTotal = deliveryManualTotal + deliveryAppTotal;
            var grandCount = takeawayCount + pickupCount + deliveryCount;
            var grandTotal = takeawayTotal + pickupTotal + deliveryTotal;

            var doc = new FlowDocument { FontFamily = new FontFamily("Tahoma"), FontSize = 12, FlowDirection = FlowDirection.RightToLeft };
            doc.Blocks.Add(new Paragraph(new Run("تقرير ترحيل الشيفت - POS")) { FontSize = 18, FontWeight = FontWeights.Black, Margin = new Thickness(0, 0, 0, 8) });
            doc.Blocks.Add(new Paragraph(new Run("وقت التقرير: " + DateTime.Now.ToString("g", _arEg))) { Margin = new Thickness(0, 0, 0, 8) });
            if (!string.IsNullOrWhiteSpace(scopeLabel))
            {
                doc.Blocks.Add(new Paragraph(new Run("نطاق الترحيل: " + scopeLabel.Trim())) { Margin = new Thickness(0, 0, 0, 8), FontWeight = FontWeights.Bold });
            }

            var table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(350) });
            table.Columns.Add(new TableColumn { Width = new GridLength(120) });
            table.Columns.Add(new TableColumn { Width = new GridLength(120) });
            var group = new TableRowGroup();
            table.RowGroups.Add(group);

            AddPosShiftSummaryRow(group, "تيك أواي (يدوي)", takeawayManualCount, takeawayManualTotal, false);
            AddPosShiftSummaryRow(group, "تيك أواي (تطبيق)", takeawayAppCount, takeawayAppTotal, false);
            AddPosShiftSummaryRow(group, "إجمالي مبيعات تيك أواي", takeawayCount, takeawayTotal, true);
            AddPosShiftSummaryRow(group, "استلام/انتظار (يدوي)", pickupManualCount, pickupManualTotal, false);
            AddPosShiftSummaryRow(group, "استلام/انتظار (تطبيق)", pickupAppCount, pickupAppTotal, false);
            AddPosShiftSummaryRow(group, "إجمالي مبيعات الاستلام/الانتظار", pickupCount, pickupTotal, true);
            AddPosShiftSummaryRow(group, "دليفري (يدوي)", deliveryManualCount, deliveryManualTotal, false);
            AddPosShiftSummaryRow(group, "دليفري (تطبيق)", deliveryAppCount, deliveryAppTotal, false);
            AddPosShiftSummaryRow(group, "إجمالي مبيعات الدليفري", deliveryCount, deliveryTotal, true);
            AddPosShiftSummaryRow(group, "الإجمالي الكلي", grandCount, grandTotal, true);

            doc.Blocks.Add(table);
            return doc;
        }

        private void AddPosShiftSummaryRow(TableRowGroup group, string label, int count, int total, bool bold)
        {
            var row = new TableRow();
            var pLabel = new Paragraph(new Run(label)) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FlowDirection = FlowDirection.RightToLeft };
            var pCount = new Paragraph(new Run(count.ToString())) { Margin = new Thickness(0), TextAlignment = TextAlignment.Center, FlowDirection = FlowDirection.LeftToRight };
            var pTotal = new Paragraph(new Run(total.ToString())) { Margin = new Thickness(0), TextAlignment = TextAlignment.Center, FlowDirection = FlowDirection.LeftToRight };
            if (bold)
            {
                pLabel.FontWeight = FontWeights.Black;
                pCount.FontWeight = FontWeights.Black;
                pTotal.FontWeight = FontWeights.Black;
            }

            row.Cells.Add(new TableCell(pLabel) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(6) });
            row.Cells.Add(new TableCell(pCount) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(6) });
            row.Cells.Add(new TableCell(pTotal) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(6) });
            group.Rows.Add(row);
        }

        private void AttachCardKeyboardHandlers(Border card, Panel hostPanel, Action activateAction)
        {
            if (card == null || hostPanel == null) return;
            card.Focusable = true;
            card.PreviewMouseLeftButtonDown += delegate
            {
                if (!card.IsKeyboardFocusWithin) card.Focus();
            };
            card.PreviewMouseRightButtonDown += delegate
            {
                if (!card.IsKeyboardFocusWithin) card.Focus();
            };
            card.KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Enter || e.Key == Key.Space)
                {
                    e.Handled = true;
                    activateAction?.Invoke();
                    return;
                }
                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    Keyboard.ClearFocus();
                    return;
                }
                if (e.Key != Key.Up && e.Key != Key.Down) return;

                e.Handled = true;
                var dir = e.Key == Key.Up ? -1 : 1;
                var currentIndex = -1;
                for (int i = 0; i < hostPanel.Children.Count; i++)
                {
                    if (ReferenceEquals(hostPanel.Children[i], card))
                    {
                        currentIndex = i;
                        break;
                    }
                }
                if (currentIndex < 0) return;

                for (int i = currentIndex + dir; i >= 0 && i < hostPanel.Children.Count; i += dir)
                {
                    if (hostPanel.Children[i] is FrameworkElement fe && fe.Focusable)
                    {
                        fe.Focus();
                        break;
                    }
                }
            };
        }

        private void RestoreFocusToCardByTag(Panel hostPanel, string tag)
        {
            if (hostPanel == null || string.IsNullOrEmpty(tag)) return;
            Dispatcher.BeginInvoke(new Action(delegate
            {
                for (int i = 0; i < hostPanel.Children.Count; i++)
                {
                    if (!(hostPanel.Children[i] is FrameworkElement fe) || !fe.Focusable) continue;
                    var t = fe.Tag as string;
                    if (!string.Equals(t, tag, StringComparison.Ordinal)) continue;
                    fe.Focus();
                    break;
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private string FindFirstPickupId()
        {
            if (_takeawayState == null || _takeawayState.Pickups == null) return null;
            for (int i = 0; i < _takeawayState.Pickups.Count; i++)
            {
                var p = _takeawayState.Pickups[i];
                if (p != null && !string.IsNullOrWhiteSpace(p.Id)) return p.Id;
            }
            return null;
        }

        private string FindFirstDeliveryQueueOrderId()
        {
            for (int i = 0; i < _deliveryOrders.Count; i++)
            {
                var o = _deliveryOrders[i];
                if (o != null && !string.IsNullOrWhiteSpace(o.Id)) return o.Id;
            }
            return null;
        }

        private bool IsAnyTakeawayOverlayOpen()
        {
            return (QtyOverlay != null && QtyOverlay.Visibility == Visibility.Visible) ||
                   (PickupOrderOverlay != null && PickupOrderOverlay.Visibility == Visibility.Visible) ||
                   (AdminAddCategoryOverlay != null && AdminAddCategoryOverlay.Visibility == Visibility.Visible) ||
                   (AdminAddItemOverlay != null && AdminAddItemOverlay.Visibility == Visibility.Visible);
        }

        private bool IsAnyDeliveryOverlayOpen()
        {
            return (DeliveryPickupOrderOverlay != null && DeliveryPickupOrderOverlay.Visibility == Visibility.Visible) ||
                   (DeliveryFormOverlay != null && DeliveryFormOverlay.Visibility == Visibility.Visible) ||
                   (DeliveryDriversOverlay != null && DeliveryDriversOverlay.Visibility == Visibility.Visible) ||
                   (DeliveryShiftOverlay != null && DeliveryShiftOverlay.Visibility == Visibility.Visible) ||
                   (DeliveryAppOrdersBoardView != null && DeliveryAppOrdersBoardView.Visibility == Visibility.Visible);
        }

        private void ToggleTakeawayQueuePickupByMiddleClick()
        {
            NormalizeTakeawayState();
            if (_takeawayPickupContextActive)
            {
                var queueId = GetCurrentOrder() != null ? _takeawayState.CurrentOrderId : FindFirstRegularTakeawayOrderId();
                if (string.IsNullOrWhiteSpace(queueId)) return;
                SetTakeawayQueueContext(queueId);
                _selectedRowKey = null;
                PersistTakeawayState();
                RenderTakeawayAll();
                RestoreFocusToCardByTag(QueueListPanel, queueId);
                if (DeliveryView != null && DeliveryView.Visibility == Visibility.Visible) RenderDeliveryReceipt();
                return;
            }

            var pickupId = !string.IsNullOrWhiteSpace(_takeawayState.ActivePickupId) ? _takeawayState.ActivePickupId : FindFirstPickupId();
            if (string.IsNullOrWhiteSpace(pickupId)) return;
            SetTakeawayPickupContext(pickupId);
            _takeawayPickupSelectedRowKey = null;
            PersistTakeawayState();
            RenderTakeawayAll();
            RestoreFocusToCardByTag(PickupsListPanel, pickupId);
            if (DeliveryView != null && DeliveryView.Visibility == Visibility.Visible) RenderDeliveryReceipt();
        }

        private void ToggleDeliveryQueuePickupByMiddleClick()
        {
            NormalizeTakeawayState();
            if (_deliveryPickupContextActive)
            {
                var queueId = !string.IsNullOrWhiteSpace(_deliveryCurrentOrderId) ? _deliveryCurrentOrderId : FindFirstDeliveryQueueOrderId();
                if (string.IsNullOrWhiteSpace(queueId)) return;
                SetDeliveryPickupContextActive(false);
                _deliveryCurrentOrderId = queueId;
                _deliverySelectedRowKey = null;
                RenderDeliverySharedPickups();
                RenderDeliveryQueue();
                RenderDeliveryReceipt();
                RestoreFocusToCardByTag(DeliveryQueueRowsPanel, queueId);
                if (TakeawayView != null && TakeawayView.Visibility == Visibility.Visible) { RenderPickups(); RenderReceipt(); }
                return;
            }

            var pickupId = (_takeawayState != null && !string.IsNullOrWhiteSpace(_takeawayState.ActivePickupId)) ? _takeawayState.ActivePickupId : FindFirstPickupId();
            if (string.IsNullOrWhiteSpace(pickupId) || _takeawayState == null) return;
            _takeawayState.ActivePickupId = pickupId;
            SetDeliveryPickupContextActive(true);
            _deliveryPickupSelectedRowKey = null;
            PersistTakeawayState();
            RenderDeliverySharedPickups();
            RenderDeliveryQueue();
            RenderDeliveryReceipt();
            RestoreFocusToCardByTag(DeliveryPickupsListPanel, pickupId);
            if (TakeawayView != null && TakeawayView.Visibility == Visibility.Visible) { RenderPickups(); RenderReceipt(); }
        }

        private void TakeawayView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                CloseAdminReorderPopups();
            }
            if (e.ChangedButton != MouseButton.Middle) return;
            if (IsAnyTakeawayOverlayOpen()) return;
            e.Handled = true;
            ToggleTakeawayQueuePickupByMiddleClick();
        }

        private void DeliveryView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                CloseAdminReorderPopups();
            }
            if (e.ChangedButton != MouseButton.Middle) return;
            if (IsAnyDeliveryOverlayOpen()) return;
            e.Handled = true;
            ToggleDeliveryQueuePickupByMiddleClick();
        }

        private bool HandleAdminOverlayKey(KeyEventArgs e)
        {
            if (e == null || e.Key != Key.Escape) return false;

            if ((AdminCategoryReorderPopup != null && AdminCategoryReorderPopup.IsOpen) ||
                (AdminItemReorderPopup != null && AdminItemReorderPopup.IsOpen))
            {
                CloseAdminReorderPopups();
                return true;
            }
            if (AdminAddCategoryOverlay != null && AdminAddCategoryOverlay.Visibility == Visibility.Visible)
            {
                CloseAdminAddCategoryForm();
                return true;
            }
            if (AdminAddItemOverlay != null && AdminAddItemOverlay.Visibility == Visibility.Visible)
            {
                CloseAdminAddItemForm();
                return true;
            }
            return false;
        }

        private void CloseAdminReorderPopups()
        {
            if (AdminCategoryReorderPopup != null) AdminCategoryReorderPopup.IsOpen = false;
            if (AdminItemReorderPopup != null) AdminItemReorderPopup.IsOpen = false;
        }

        private void ReceiptMiddleArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CloseAdminReorderPopups();
        }

        private void DeliveryReceiptMiddleArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CloseAdminReorderPopups();
        }

        private void TakeawayAddCategoryButton_Click(object sender, RoutedEventArgs e) { OpenAdminAddCategoryForm(false); }
        private void DeliveryAddCategoryButton_Click(object sender, RoutedEventArgs e) { OpenAdminAddCategoryForm(true); }
        private void TakeawayAddItemButton_Click(object sender, RoutedEventArgs e) { OpenAdminAddItemForm(false); }
        private void DeliveryAddItemButton_Click(object sender, RoutedEventArgs e) { OpenAdminAddItemForm(true); }

        private void OpenAdminAddCategoryForm(bool targetDelivery)
        {
            if (!_isAdminMode || AdminAddCategoryOverlay == null || AdminAddCategoryNameTextBox == null) return;
            _adminAddCategoryTargetDelivery = targetDelivery;
            _adminCategoryEditTarget = null;
            AdminAddCategoryNameTextBox.Text = string.Empty;
            AdminAddCategoryOverlay.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke(new Action(delegate
            {
                AdminAddCategoryNameTextBox.Focus();
                AdminAddCategoryNameTextBox.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void CloseAdminAddCategoryForm()
        {
            if (AdminAddCategoryOverlay != null) AdminAddCategoryOverlay.Visibility = Visibility.Collapsed;
            if (AdminAddCategoryNameTextBox != null) AdminAddCategoryNameTextBox.Text = string.Empty;
            _adminCategoryEditTarget = null;
        }

        private void SaveAdminAddCategoryForm()
        {
            if (!_isAdminMode) return;
            var name = (AdminAddCategoryNameTextBox != null ? AdminAddCategoryNameTextBox.Text : string.Empty) ?? string.Empty;
            name = name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("اسم القائمة مطلوب.", "ADMIN", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            for (int i = 0; i < _menu.Count; i++)
            {
                if (_menu[i] != null && !ReferenceEquals(_menu[i], _adminCategoryEditTarget) && string.Equals(_menu[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("القائمة موجودة بالفعل.", "ADMIN", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            if (_adminCategoryEditTarget != null)
            {
                var oldName = _adminCategoryEditTarget.Name;
                _adminCategoryEditTarget.Name = name;
                if (string.Equals(_activeCategory, oldName, StringComparison.Ordinal)) _activeCategory = name;
                if (string.Equals(_deliveryActiveCategory, oldName, StringComparison.Ordinal)) _deliveryActiveCategory = name;
            }
            else
            {
                var preserveTakeawayCategory = !string.IsNullOrWhiteSpace(_activeCategory) && GetMenuCategoryByName(_activeCategory) != null;
                var preserveDeliveryCategory = !string.IsNullOrWhiteSpace(_deliveryActiveCategory) && GetMenuCategoryByName(_deliveryActiveCategory) != null;
                _menu.Add(new MenuCategory { Name = name, Items = new List<MenuItemDef>() });
                if (!preserveTakeawayCategory) _activeCategory = name;
                if (!preserveDeliveryCategory) _deliveryActiveCategory = name;
            }

            PersistMenuState();
            CloseAdminAddCategoryForm();
            RefreshMenuPanelsAcrossRoles();
        }

        private void AdminAddCategorySaveButton_Click(object sender, RoutedEventArgs e) { SaveAdminAddCategoryForm(); }
        private void AdminAddCategoryCancelButton_Click(object sender, RoutedEventArgs e) { CloseAdminAddCategoryForm(); }

        private void AdminAddCategoryModalBorder_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (AdminAddCategoryOverlay == null || AdminAddCategoryOverlay.Visibility != Visibility.Visible) return;
            if (e.Key == Key.Escape) { e.Handled = true; CloseAdminAddCategoryForm(); return; }
            if (e.Key == Key.Enter) { e.Handled = true; SaveAdminAddCategoryForm(); }
        }

        private MenuCategory GetMenuCategoryByName(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName)) return null;
            for (int i = 0; i < _menu.Count; i++)
            {
                if (_menu[i] != null && string.Equals(_menu[i].Name, categoryName, StringComparison.Ordinal))
                    return _menu[i];
            }
            return null;
        }

        private void RebuildAdminAddItemProductsList()
        {
            if (AdminAddItemProductsListBox == null) return;
            EnsureAppProductsCatalogLoaded();

            var search = (AdminAddItemProductSearchTextBox != null ? AdminAddItemProductSearchTextBox.Text : string.Empty) ?? string.Empty;
            search = search.Trim();

            var keepSelected = _adminAddItemSelectedProductId;
            AdminAddItemProductsListBox.Items.Clear();

            for (int i = 0; i < _appProductsCatalog.Count; i++)
            {
                var product = _appProductsCatalog[i];
                if (product == null) continue;
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var name = product.NameAr ?? string.Empty;
                    if (name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                }

                var item = new ListBoxItem
                {
                    Tag = product,
                    Content = product.NameAr ?? string.Empty,
                    FontWeight = FontWeights.Black,
                    Padding = new Thickness(6, 4, 6, 4),
                    FlowDirection = FlowDirection.RightToLeft
                };
                AdminAddItemProductsListBox.Items.Add(item);
            }

            if (string.IsNullOrWhiteSpace(keepSelected))
            {
                RefreshAdminAddItemOptionsPanel();
                RefreshAdminAddItemPricePreviewAndDisplayName(false, false);
                return;
            }

            for (int i = 0; i < AdminAddItemProductsListBox.Items.Count; i++)
            {
                var lbi = AdminAddItemProductsListBox.Items[i] as ListBoxItem;
                var p = lbi != null ? lbi.Tag as AppProductSnapshotRecord : null;
                if (p == null || !string.Equals(p.Id, keepSelected, StringComparison.Ordinal)) continue;
                _adminAddItemUiSyncing = true;
                AdminAddItemProductsListBox.SelectedItem = lbi;
                _adminAddItemUiSyncing = false;
                break;
            }
        }

        private AppProductSnapshotRecord GetSelectedAdminAddItemProduct()
        {
            if (AdminAddItemProductsListBox == null) return FindAppProductSnapshotById(_adminAddItemSelectedProductId);
            var product = (AdminAddItemProductsListBox.SelectedItem as ListBoxItem)?.Tag as AppProductSnapshotRecord;
            if (product != null)
            {
                _adminAddItemSelectedProductId = product.Id;
                return product;
            }
            return FindAppProductSnapshotById(_adminAddItemSelectedProductId);
        }

        private void RefreshAdminAddItemOptionsPanel()
        {
            if (AdminAddItemOptionsPanel == null) return;
            AdminAddItemOptionsPanel.Children.Clear();
            _adminAddItemSingleOptionSelectors.Clear();
            _adminAddItemMultiOptionSelectors.Clear();

            var product = GetSelectedAdminAddItemProduct();
            if (product == null || product.AddonGroups == null || product.AddonGroups.Count == 0)
            {
                AdminAddItemOptionsPanel.Children.Add(new TextBlock
                {
                    Text = "لا توجد خيارات ثابتة لهذا المنتج.",
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF555555")),
                    FlowDirection = FlowDirection.RightToLeft
                });
                return;
            }

            for (int i = 0; i < product.AddonGroups.Count; i++)
            {
                var group = product.AddonGroups[i];
                if (group == null) continue;

                var groupWrap = new StackPanel { Margin = new Thickness(0, 0, 0, 8), FlowDirection = FlowDirection.RightToLeft };
                groupWrap.Children.Add(new TextBlock
                {
                    Text = group.NameAr ?? "خيارات",
                    FontWeight = FontWeights.Black,
                    Margin = new Thickness(0, 0, 0, 4),
                    FlowDirection = FlowDirection.RightToLeft
                });

                if (group.MultiSelect)
                {
                    var list = new List<CheckBox>();
                    if (group.Options != null)
                    {
                        for (int j = 0; j < group.Options.Count; j++)
                        {
                            var opt = group.Options[j];
                            if (opt == null) continue;
                            var cb = new CheckBox
                            {
                                Content = (opt.NameAr ?? string.Empty) + (opt.ExtraPrice != 0 ? (" (+" + opt.ExtraPrice + ")") : string.Empty),
                                Tag = opt,
                                Margin = new Thickness(0, 0, 0, 2),
                                FontWeight = FontWeights.Bold,
                                FlowDirection = FlowDirection.RightToLeft
                            };
                            cb.Checked += AdminAddItemOptionCheckBox_Changed;
                            cb.Unchecked += AdminAddItemOptionCheckBox_Changed;
                            list.Add(cb);
                            groupWrap.Children.Add(cb);
                        }
                    }
                    _adminAddItemMultiOptionSelectors[group.Id ?? ("grp_" + i)] = list;
                }
                else
                {
                    var combo = new ComboBox
                    {
                        Height = 30,
                        FontWeight = FontWeights.Bold,
                        FlowDirection = FlowDirection.RightToLeft
                    };
                    combo.SelectionChanged += AdminAddItemOptionComboBox_SelectionChanged;
                    combo.Items.Add(new ComboBoxItem { Content = "بدون", Tag = null, FontWeight = FontWeights.Bold, FlowDirection = FlowDirection.RightToLeft });
                    if (group.Options != null)
                    {
                        for (int j = 0; j < group.Options.Count; j++)
                        {
                            var opt = group.Options[j];
                            if (opt == null) continue;
                            combo.Items.Add(new ComboBoxItem
                            {
                                Content = (opt.NameAr ?? string.Empty) + (opt.ExtraPrice != 0 ? (" (+" + opt.ExtraPrice + ")") : string.Empty),
                                Tag = opt,
                                FontWeight = FontWeights.Bold,
                                FlowDirection = FlowDirection.RightToLeft
                            });
                        }
                    }
                    combo.SelectedIndex = 0;
                    _adminAddItemSingleOptionSelectors[group.Id ?? ("grp_" + i)] = combo;
                    groupWrap.Children.Add(combo);
                }

                AdminAddItemOptionsPanel.Children.Add(groupWrap);
            }
        }

        private void ApplyAdminAddItemSelectedOptions(List<string> optionIds)
        {
            optionIds ??= new List<string>();
            foreach (var kv in _adminAddItemSingleOptionSelectors)
            {
                var combo = kv.Value;
                if (combo == null) continue;
                var selectedIndex = 0;
                for (int i = 0; i < combo.Items.Count; i++)
                {
                    var opt = (combo.Items[i] as ComboBoxItem)?.Tag as AppAddonOptionSnapshotRecord;
                    if (opt != null && optionIds.Contains(opt.Id))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
                combo.SelectedIndex = selectedIndex;
            }
            foreach (var kv in _adminAddItemMultiOptionSelectors)
            {
                var list = kv.Value;
                if (list == null) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    var cb = list[i];
                    var opt = cb != null ? cb.Tag as AppAddonOptionSnapshotRecord : null;
                    if (cb == null || opt == null) continue;
                    cb.IsChecked = optionIds.Contains(opt.Id);
                }
            }
        }

        private void RefreshAdminAddItemPricePreviewAndDisplayName(bool forceDisplayName, bool preserveUserText)
        {
            if (AdminAddItemPricePreviewText == null || AdminAddItemNameTextBox == null) return;
            var product = GetSelectedAdminAddItemProduct();
            var optionIds = GetAdminAddItemSelectedFixedOptionIds();
            var finalPrice = ComputeLinkedPosPrice(product, optionIds);
            AdminAddItemPricePreviewText.Text = finalPrice.ToString();

            var autoName = BuildLinkedPosDisplayName(product, optionIds);
            var currentText = (AdminAddItemNameTextBox.Text ?? string.Empty);
            var canReplace = forceDisplayName || !preserveUserText || string.IsNullOrWhiteSpace(currentText) || string.Equals(currentText, _adminAddItemLastAutoDisplayName ?? string.Empty, StringComparison.Ordinal);
            if (canReplace) AdminAddItemNameTextBox.Text = autoName;
            _adminAddItemLastAutoDisplayName = autoName;
        }

        private void PopulateAdminAddItemFormFromSelection(MenuItemDef existing)
        {
            EnsureAppProductsCatalogLoaded();
            _adminAddItemUiSyncing = true;
            _adminAddItemSelectedProductId = existing?.ProductId;
            if (AdminAddItemProductSearchTextBox != null) AdminAddItemProductSearchTextBox.Text = string.Empty;
            RebuildAdminAddItemProductsList();
            if (AdminAddItemProductsListBox != null && !string.IsNullOrWhiteSpace(_adminAddItemSelectedProductId))
            {
                for (int i = 0; i < AdminAddItemProductsListBox.Items.Count; i++)
                {
                    var lbi = AdminAddItemProductsListBox.Items[i] as ListBoxItem;
                    var p = lbi != null ? lbi.Tag as AppProductSnapshotRecord : null;
                    if (p != null && string.Equals(p.Id, _adminAddItemSelectedProductId, StringComparison.Ordinal))
                    {
                        AdminAddItemProductsListBox.SelectedItem = lbi;
                        break;
                    }
                }
            }
            _adminAddItemUiSyncing = false;

            RefreshAdminAddItemOptionsPanel();
            ApplyAdminAddItemSelectedOptions(existing?.FixedOptionIds);

            if (AdminAddItemNameTextBox != null) AdminAddItemNameTextBox.Text = existing != null ? (existing.Name ?? string.Empty) : string.Empty;
            _adminAddItemLastAutoDisplayName = null;
            RefreshAdminAddItemPricePreviewAndDisplayName(existing == null, true);

            if (existing != null && string.IsNullOrWhiteSpace(existing.ProductId) && AdminAddItemPricePreviewText != null)
            {
                AdminAddItemPricePreviewText.Text = existing.Price.ToString();
            }
        }

        private void AdminAddItemProductSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_adminAddItemUiSyncing) return;
            RebuildAdminAddItemProductsList();
        }

        private void AdminAddItemProductsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_adminAddItemUiSyncing) return;
            var product = GetSelectedAdminAddItemProduct();
            _adminAddItemSelectedProductId = product?.Id;
            RefreshAdminAddItemOptionsPanel();
            RefreshAdminAddItemPricePreviewAndDisplayName(false, true);
        }

        private void AdminAddItemOptionCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_adminAddItemUiSyncing) return;
            RefreshAdminAddItemPricePreviewAndDisplayName(false, true);
        }

        private void AdminAddItemOptionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_adminAddItemUiSyncing) return;
            RefreshAdminAddItemPricePreviewAndDisplayName(false, true);
        }

        private void OpenAdminAddItemForm(bool targetDelivery)
        {
            if (!_isAdminMode || AdminAddItemOverlay == null || AdminAddItemNameTextBox == null) return;
            _adminAddItemTargetDelivery = targetDelivery;
            _adminItemEditCategoryTarget = null;
            _adminItemEditTarget = null;
            var categoryName = targetDelivery ? _deliveryActiveCategory : _activeCategory;
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                MessageBox.Show("اختر قائمة أولاً.", "ADMIN", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (AdminAddItemCategoryNameText != null) AdminAddItemCategoryNameText.Text = categoryName;
            PopulateAdminAddItemFormFromSelection(null);
            AdminAddItemOverlay.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (AdminAddItemProductSearchTextBox != null)
                {
                    AdminAddItemProductSearchTextBox.Focus();
                    AdminAddItemProductSearchTextBox.SelectAll();
                }
                else
                {
                    AdminAddItemNameTextBox.Focus();
                    AdminAddItemNameTextBox.SelectAll();
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void CloseAdminAddItemForm()
        {
            if (AdminAddItemOverlay != null) AdminAddItemOverlay.Visibility = Visibility.Collapsed;
            _adminAddItemUiSyncing = true;
            if (AdminAddItemProductSearchTextBox != null) AdminAddItemProductSearchTextBox.Text = string.Empty;
            if (AdminAddItemProductsListBox != null) AdminAddItemProductsListBox.Items.Clear();
            if (AdminAddItemOptionsPanel != null) AdminAddItemOptionsPanel.Children.Clear();
            _adminAddItemUiSyncing = false;
            if (AdminAddItemNameTextBox != null) AdminAddItemNameTextBox.Text = string.Empty;
            if (AdminAddItemPricePreviewText != null) AdminAddItemPricePreviewText.Text = "0";
            _adminAddItemSelectedProductId = null;
            _adminAddItemLastAutoDisplayName = null;
            _adminAddItemSingleOptionSelectors.Clear();
            _adminAddItemMultiOptionSelectors.Clear();
            _adminItemEditCategoryTarget = null;
            _adminItemEditTarget = null;
        }

        private void SaveAdminAddItemForm()
        {
            if (!_isAdminMode) return;
            var categoryName = _adminAddItemTargetDelivery ? _deliveryActiveCategory : _activeCategory;
            var cat = _adminItemEditCategoryTarget ?? GetMenuCategoryByName(categoryName);
            if (cat == null)
            {
                MessageBox.Show("تعذر تحديد القائمة الحالية للصنف.", "ADMIN", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var itemName = (AdminAddItemNameTextBox != null ? AdminAddItemNameTextBox.Text : string.Empty) ?? string.Empty;
            itemName = itemName.Trim();
            if (string.IsNullOrWhiteSpace(itemName))
            {
                MessageBox.Show("اسم الصنف مطلوب.", "ADMIN", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var selectedProduct = GetSelectedAdminAddItemProduct();
            if (selectedProduct == null || string.IsNullOrWhiteSpace(selectedProduct.Id))
            {
                MessageBox.Show("اختر منتج التطبيق المرتبط أولاً.", "ADMIN", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var fixedOptionIds = GetAdminAddItemSelectedFixedOptionIds();
            var finalPrice = ComputeLinkedPosPrice(selectedProduct, fixedOptionIds);

            if (_adminItemEditTarget != null)
            {
                _adminItemEditTarget.Name = itemName;
                _adminItemEditTarget.PosDisplayName = itemName;
                _adminItemEditTarget.Price = finalPrice;
                _adminItemEditTarget.ProductId = selectedProduct.Id;
                _adminItemEditTarget.SourceProductId = selectedProduct.SourceProductId;
                _adminItemEditTarget.FixedOptionIds = fixedOptionIds;
            }
            else
            {
                cat.Items.Add(new MenuItemDef
                {
                    Name = itemName,
                    PosDisplayName = itemName,
                    Price = finalPrice,
                    ProductId = selectedProduct.Id,
                    SourceProductId = selectedProduct.SourceProductId,
                    FixedOptionIds = fixedOptionIds,
                    PosPosition = cat.Items != null ? cat.Items.Count : 0
                });
            }

            PersistMenuState();
            CloseAdminAddItemForm();
            RefreshMenuPanelsAcrossRoles();
        }

        private void AdminAddItemSaveButton_Click(object sender, RoutedEventArgs e) { SaveAdminAddItemForm(); }
        private void AdminAddItemCancelButton_Click(object sender, RoutedEventArgs e) { CloseAdminAddItemForm(); }

        private void AdminAddItemModalBorder_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (AdminAddItemOverlay == null || AdminAddItemOverlay.Visibility != Visibility.Visible) return;
            if (e.Key == Key.Escape) { e.Handled = true; CloseAdminAddItemForm(); return; }
            if (e.Key == Key.Enter) { e.Handled = true; SaveAdminAddItemForm(); }
        }

        private void OpenAdminEditCategoryForm(MenuCategory category, bool targetDelivery)
        {
            if (!_isAdminMode || category == null || AdminAddCategoryOverlay == null || AdminAddCategoryNameTextBox == null) return;
            _adminAddCategoryTargetDelivery = targetDelivery;
            _adminCategoryEditTarget = category;
            AdminAddCategoryNameTextBox.Text = category.Name ?? string.Empty;
            AdminAddCategoryOverlay.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke(new Action(delegate
            {
                AdminAddCategoryNameTextBox.Focus();
                AdminAddCategoryNameTextBox.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void OpenAdminEditItemForm(MenuCategory category, MenuItemDef item, bool targetDelivery)
        {
            if (!_isAdminMode || category == null || item == null || AdminAddItemOverlay == null || AdminAddItemNameTextBox == null) return;
            _adminAddItemTargetDelivery = targetDelivery;
            _adminItemEditCategoryTarget = category;
            _adminItemEditTarget = item;
            if (AdminAddItemCategoryNameText != null) AdminAddItemCategoryNameText.Text = category.Name ?? string.Empty;
            PopulateAdminAddItemFormFromSelection(item);
            AdminAddItemOverlay.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (AdminAddItemNameTextBox != null)
                {
                    AdminAddItemNameTextBox.Focus();
                    AdminAddItemNameTextBox.SelectAll();
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void DeleteAdminCategory(MenuCategory category)
        {
            if (!_isAdminMode || category == null) return;
            if (_menu.Count <= 1)
            {
                MessageBox.Show("لا يمكن حذف آخر قائمة.", "ADMIN", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("حذف القائمة والأصناف التابعة لها؟", "ADMIN", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var removedName = category.Name;
            for (int i = _menu.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_menu[i], category)) _menu.RemoveAt(i);
            }
            if (_menu.Count > 0)
            {
                if (string.Equals(_activeCategory, removedName, StringComparison.Ordinal)) _activeCategory = _menu[0].Name;
                if (string.Equals(_deliveryActiveCategory, removedName, StringComparison.Ordinal)) _deliveryActiveCategory = _menu[0].Name;
            }

            CloseAdminReorderPopups();
            PersistMenuState();
            RefreshMenuPanelsAcrossRoles();
        }

        private void DeleteAdminItem(MenuCategory category, MenuItemDef item)
        {
            if (!_isAdminMode || category == null || item == null || category.Items == null) return;
            if (MessageBox.Show("حذف الصنف المحدد؟", "ADMIN", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            for (int i = category.Items.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(category.Items[i], item)) category.Items.RemoveAt(i);
            }

            CloseAdminReorderPopups();
            PersistMenuState();
            RefreshMenuPanelsAcrossRoles();
        }

        private void AdminCategoryEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdminMode || _adminCategoryReorderTarget == null) return;
            var targetDelivery = _adminReorderPopupTargetDelivery;
            var category = _adminCategoryReorderTarget;
            CloseAdminReorderPopups();
            OpenAdminEditCategoryForm(category, targetDelivery);
        }

        private void AdminCategoryDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdminMode || _adminCategoryReorderTarget == null) return;
            DeleteAdminCategory(_adminCategoryReorderTarget);
        }

        private void AdminItemEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdminMode || _adminItemReorderCategoryTarget == null || _adminItemReorderTarget == null) return;
            var targetDelivery = _adminReorderPopupTargetDelivery;
            var category = _adminItemReorderCategoryTarget;
            var item = _adminItemReorderTarget;
            CloseAdminReorderPopups();
            OpenAdminEditItemForm(category, item, targetDelivery);
        }

        private void AdminItemDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdminMode || _adminItemReorderCategoryTarget == null || _adminItemReorderTarget == null) return;
            DeleteAdminItem(_adminItemReorderCategoryTarget, _adminItemReorderTarget);
        }

        private void AttachAdminCategoryReorderHandler(Button button, bool targetDelivery, MenuCategory category)
        {
            if (!_isAdminMode || button == null || category == null) return;
            button.PreviewMouseRightButtonUp += delegate (object s, MouseButtonEventArgs e)
            {
                if (!_isAdminMode) return;
                e.Handled = true;
                OpenAdminCategoryReorderPopup(button, targetDelivery, category);
            };
        }

        private void AttachAdminItemReorderHandler(Button button, bool targetDelivery, MenuCategory category, MenuItemDef item)
        {
            if (!_isAdminMode || button == null || category == null || item == null) return;
            button.PreviewMouseRightButtonUp += delegate (object s, MouseButtonEventArgs e)
            {
                if (!_isAdminMode) return;
                e.Handled = true;
                OpenAdminItemReorderPopup(button, targetDelivery, category, item);
            };
        }

        private void OpenAdminCategoryReorderPopup(Button anchor, bool targetDelivery, MenuCategory category)
        {
            if (!_isAdminMode || AdminCategoryReorderPopup == null || anchor == null || category == null) return;
            if (AdminCategoryReorderPopup.IsOpen &&
                ReferenceEquals(_adminCategoryReorderTarget, category) &&
                ReferenceEquals(AdminCategoryReorderPopup.PlacementTarget, anchor))
            {
                AdminCategoryReorderPopup.IsOpen = false;
                return;
            }
            if (AdminItemReorderPopup != null) AdminItemReorderPopup.IsOpen = false;
            _adminReorderPopupTargetDelivery = targetDelivery;
            _adminCategoryReorderTarget = category;
            _adminItemReorderCategoryTarget = null;
            _adminItemReorderTarget = null;
            AdminCategoryReorderPopup.PlacementTarget = anchor;
            AdminCategoryReorderPopup.IsOpen = true;
        }

        private void OpenAdminItemReorderPopup(Button anchor, bool targetDelivery, MenuCategory category, MenuItemDef item)
        {
            if (!_isAdminMode || AdminItemReorderPopup == null || anchor == null || category == null || item == null) return;
            if (AdminItemReorderPopup.IsOpen &&
                ReferenceEquals(_adminItemReorderTarget, item) &&
                ReferenceEquals(AdminItemReorderPopup.PlacementTarget, anchor))
            {
                AdminItemReorderPopup.IsOpen = false;
                return;
            }
            if (AdminCategoryReorderPopup != null) AdminCategoryReorderPopup.IsOpen = false;
            _adminReorderPopupTargetDelivery = targetDelivery;
            _adminCategoryReorderTarget = null;
            _adminItemReorderCategoryTarget = category;
            _adminItemReorderTarget = item;
            AdminItemReorderPopup.PlacementTarget = anchor;
            AdminItemReorderPopup.IsOpen = true;
        }

        private void AdminCategoryReorderPopup_Closed(object sender, EventArgs e)
        {
            _adminCategoryReorderTarget = null;
        }

        private void AdminItemReorderPopup_Closed(object sender, EventArgs e)
        {
            _adminItemReorderCategoryTarget = null;
            _adminItemReorderTarget = null;
        }

        private Button FindCategoryButtonByTarget(bool targetDelivery, MenuCategory category)
        {
            var panel = targetDelivery ? (Panel)DeliveryCategoriesGridPanel : CategoriesGridPanel;
            if (panel == null || category == null) return null;
            for (int i = 0; i < panel.Children.Count; i++)
            {
                if (panel.Children[i] is Button b && ReferenceEquals(b.Tag, category)) return b;
            }
            return null;
        }

        private Button FindItemButtonByTarget(bool targetDelivery, MenuItemDef item)
        {
            var panel = targetDelivery ? (Panel)DeliveryItemsGridPanel : ItemsGridPanel;
            if (panel == null || item == null) return null;
            for (int i = 0; i < panel.Children.Count; i++)
            {
                if (panel.Children[i] is Button b && ReferenceEquals(b.Tag, item)) return b;
            }
            return null;
        }

        private void MoveAdminCategoryBy(int delta)
        {
            if (!_isAdminMode) return;
            if (_adminCategoryReorderTarget == null) return;
            var current = -1;
            for (int i = 0; i < _menu.Count; i++)
            {
                if (ReferenceEquals(_menu[i], _adminCategoryReorderTarget)) { current = i; break; }
            }
            if (current < 0) return;
            var target = current + delta;
            if (target < 0 || target >= _menu.Count) return;

            var temp = _menu[current];
            _menu[current] = _menu[target];
            _menu[target] = temp;
            PersistMenuState();
            var popupTargetDelivery = _adminReorderPopupTargetDelivery;
            var popupCategory = _adminCategoryReorderTarget;
            RefreshMenuPanelsAcrossRoles();

            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (!_isAdminMode || popupCategory == null) return;
                var btn = FindCategoryButtonByTarget(popupTargetDelivery, popupCategory);
                if (btn != null && AdminCategoryReorderPopup != null)
                {
                    _adminReorderPopupTargetDelivery = popupTargetDelivery;
                    AdminCategoryReorderPopup.IsOpen = false;
                    _adminCategoryReorderTarget = popupCategory;
                    AdminCategoryReorderPopup.PlacementTarget = btn;
                    AdminCategoryReorderPopup.IsOpen = true;
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private List<int> GetVisibleItemIndexes(MenuCategory category)
        {
            var result = new List<int>();
            if (category == null || category.Items == null) return result;
            for (int i = 0; i < category.Items.Count; i++)
            {
                var it = category.Items[i];
                if (it == null || it.Name == "—") continue;
                result.Add(i);
            }
            return result;
        }

        private void MoveAdminItemToVisibleIndex(int targetVisibleIndex)
        {
            if (!_isAdminMode) return;
            if (_adminItemReorderCategoryTarget == null || _adminItemReorderTarget == null) return;
            var visible = GetVisibleItemIndexes(_adminItemReorderCategoryTarget);
            var currentVisible = -1;
            for (int i = 0; i < visible.Count; i++)
            {
                if (ReferenceEquals(_adminItemReorderCategoryTarget.Items[visible[i]], _adminItemReorderTarget))
                {
                    currentVisible = i;
                    break;
                }
            }
            if (currentVisible < 0 || targetVisibleIndex < 0 || targetVisibleIndex >= visible.Count || targetVisibleIndex == currentVisible) return;

            var a = visible[currentVisible];
            var b = visible[targetVisibleIndex];
            var temp = _adminItemReorderCategoryTarget.Items[a];
            _adminItemReorderCategoryTarget.Items[a] = _adminItemReorderCategoryTarget.Items[b];
            _adminItemReorderCategoryTarget.Items[b] = temp;

            PersistMenuState();
            var popupTargetDelivery = _adminReorderPopupTargetDelivery;
            var popupCategory = _adminItemReorderCategoryTarget;
            var popupItem = _adminItemReorderTarget;
            RefreshMenuPanelsAcrossRoles();

            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (!_isAdminMode || popupCategory == null || popupItem == null) return;
                var btn = FindItemButtonByTarget(popupTargetDelivery, popupItem);
                if (btn != null && AdminItemReorderPopup != null)
                {
                    _adminReorderPopupTargetDelivery = popupTargetDelivery;
                    AdminItemReorderPopup.IsOpen = false;
                    _adminItemReorderCategoryTarget = popupCategory;
                    _adminItemReorderTarget = popupItem;
                    AdminItemReorderPopup.PlacementTarget = btn;
                    AdminItemReorderPopup.IsOpen = true;
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void MoveAdminItemByVisualStep(int dx, int dy)
        {
            if (!_isAdminMode) return;
            if (_adminItemReorderCategoryTarget == null || _adminItemReorderTarget == null) return;
            var visible = GetVisibleItemIndexes(_adminItemReorderCategoryTarget);
            var currentVisible = -1;
            for (int i = 0; i < visible.Count; i++)
            {
                if (ReferenceEquals(_adminItemReorderCategoryTarget.Items[visible[i]], _adminItemReorderTarget))
                {
                    currentVisible = i;
                    break;
                }
            }
            if (currentVisible < 0) return;

            var step = 0;
            if (dy < 0) step = -1;          // Up
            else if (dy > 0) step = 1;      // Down
            else if (dx < 0) step = -1;     // Right in RTL popup
            else if (dx > 0) step = 1;      // Left in RTL popup
            if (step == 0) return;

            var nextVisible = currentVisible + step;
            if (nextVisible < 0 || nextVisible >= visible.Count) return;
            MoveAdminItemToVisibleIndex(nextVisible);
        }

        private void AdminCategoryReorderUpButton_Click(object sender, RoutedEventArgs e) { MoveAdminCategoryBy(-1); }
        private void AdminCategoryReorderDownButton_Click(object sender, RoutedEventArgs e) { MoveAdminCategoryBy(1); }
        private void AdminItemReorderLeftButton_Click(object sender, RoutedEventArgs e) { MoveAdminItemByVisualStep(1, 0); }
        private void AdminItemReorderRightButton_Click(object sender, RoutedEventArgs e) { MoveAdminItemByVisualStep(-1, 0); }
        private void AdminItemReorderUpButton_Click(object sender, RoutedEventArgs e) { MoveAdminItemByVisualStep(0, -1); }
        private void AdminItemReorderDownButton_Click(object sender, RoutedEventArgs e) { MoveAdminItemByVisualStep(0, 1); }
    }
}

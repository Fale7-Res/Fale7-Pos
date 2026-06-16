using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Fale7_POS
{
    public partial class MainWindow
    {
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
    }
}

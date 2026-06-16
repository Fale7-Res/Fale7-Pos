#pragma warning disable 4014
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        private async void RefreshInventoryOverviewAsync(bool showErrors)
        {
            if (_posInventorySnapshotLoading) return;
            _posInventorySnapshotLoading = true;
            _posInventorySnapshotError = string.Empty;
            RenderInventoryOverviewRows();

            try
            {
                var cfg = LoadSupabaseClientConfigResolved();
                if (cfg == null)
                {
                    _posInventorySnapshotError = "ربط السيرفر غير مفعل.";
                    return;
                }

                var raw = await PostSupabaseRpcGetStringAsync(
                    cfg,
                    "api_pos_inventory_admin_snapshot",
                    "{}",
                    null,
                    null,
                    true).ConfigureAwait(false);

                var parsed = DeserializeJson<PosInventoryAdminSnapshotResponse>(raw);
                if (parsed == null)
                {
                    _posInventorySnapshotError = "تعذر تحميل شاشة المخزون من السيرفر.";
                    return;
                }

                if (parsed.Ok == false)
                {
                    _posInventorySnapshotError = string.IsNullOrWhiteSpace(parsed.Error)
                        ? "تعذر تحميل شاشة المخزون."
                        : parsed.Error.Trim();
                    return;
                }

                lock (_posInventoryItems)
                {
                    _posInventoryItems.Clear();
                    if (parsed.InventoryItems != null)
                    {
                        for (int i = 0; i < parsed.InventoryItems.Count; i++)
                        {
                            var row = parsed.InventoryItems[i];
                            if (row == null || string.IsNullOrWhiteSpace(row.Id)) continue;
                            _posInventoryItems.Add(row);
                        }
                    }
                }

                lock (_posInventoryLinks)
                {
                    _posInventoryLinks.Clear();
                    if (parsed.InventoryLinks != null)
                    {
                        for (int i = 0; i < parsed.InventoryLinks.Count; i++)
                        {
                            var row = parsed.InventoryLinks[i];
                            if (row == null || string.IsNullOrWhiteSpace(row.InventoryItemId)) continue;
                            _posInventoryLinks.Add(row);
                        }
                    }
                }

                lock (_posInventoryTargets)
                {
                    _posInventoryTargets.Clear();
                    if (parsed.Targets != null)
                    {
                        for (int i = 0; i < parsed.Targets.Count; i++)
                        {
                            var row = parsed.Targets[i];
                            if (row == null || string.IsNullOrWhiteSpace(row.EntityId)) continue;
                            _posInventoryTargets.Add(row);
                        }
                    }
                }

                PruneInventoryDrafts();
            }
            catch (Exception ex)
            {
                _posInventorySnapshotError = ex == null
                    ? "تعذر تحميل شاشة المخزون."
                    : (ex.Message ?? "تعذر تحميل شاشة المخزون.");
                if (showErrors)
                {
                    try { ShowSupabaseServerError("Failed to load inventory admin snapshot", _posInventorySnapshotError); } catch { }
                }
            }
            finally
            {
                _posInventorySnapshotLoading = false;
                try
                {
                    Dispatcher.BeginInvoke(new Action(RenderInventoryOverviewRows));
                }
                catch
                {
                }
            }
        }

        private async Task SaveInventoryItemAsync()
        {
            if (_posInventoryMutationBusy) return;

            var name = (_inventoryItemDraftName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                try { ShowSupabaseServerError("Inventory validation", "اكتب اسم عنصر المخزون أولاً."); } catch { }
                return;
            }

            double quantity;
            if (!double.TryParse((_inventoryItemDraftQuantity ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out quantity))
            {
                double.TryParse((_inventoryItemDraftQuantity ?? string.Empty).Trim(), NumberStyles.Float, _arEg, out quantity);
            }

            double lowStock;
            if (!double.TryParse((_inventoryItemDraftLowStock ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out lowStock))
            {
                double.TryParse((_inventoryItemDraftLowStock ?? string.Empty).Trim(), NumberStyles.Float, _arEg, out lowStock);
            }

            quantity = Math.Max(0, quantity);
            lowStock = Math.Max(0, lowStock);

            var cfg = LoadSupabaseClientConfigResolved();
            if (cfg == null)
            {
                try { ShowSupabaseServerError("Inventory save", "ربط السيرفر غير مفعل."); } catch { }
                return;
            }

            _posInventoryMutationBusy = true;
            try
            {
                var request = new PosInventoryUpsertItemRequest
                {
                    Item = new PosInventoryUpsertItemPayload
                    {
                        Id = string.IsNullOrWhiteSpace(_inventoryEditingItemId) ? null : _inventoryEditingItemId,
                        Name = name,
                        DisplayUnit = string.IsNullOrWhiteSpace(_inventoryItemDraftDisplayUnit) ? "piece" : _inventoryItemDraftDisplayUnit,
                        QuantityOnHand = ToBaseInventoryQuantity(quantity, _inventoryItemDraftDisplayUnit),
                        LowStockThreshold = ToBaseInventoryQuantity(lowStock, _inventoryItemDraftDisplayUnit),
                        Active = _inventoryItemDraftActive
                    }
                };

                var raw = await PostSupabaseRpcGetStringAsync(
                    cfg,
                    "api_inventory_upsert_item",
                    SerializeJson(request),
                    null,
                    null,
                    true).ConfigureAwait(false);

                var parsed = DeserializeJson<PosInventoryRpcResponse>(raw);
                if (parsed == null || parsed.Ok == false)
                {
                    throw new InvalidOperationException(parsed != null && !string.IsNullOrWhiteSpace(parsed.Error)
                        ? parsed.Error.Trim()
                        : "تعذر حفظ عنصر المخزون.");
                }

                ResetInventoryItemDraft();
                RefreshInventoryOverviewAsync(false);
            }
            catch (Exception ex)
            {
                try { ShowSupabaseServerError("Inventory save", ex == null ? "تعذر حفظ عنصر المخزون." : ex.Message); } catch { }
            }
            finally
            {
                _posInventoryMutationBusy = false;
            }
        }

        private async Task DeleteInventoryItemAsync(PosInventoryAdminItemRow item)
        {
            if (_posInventoryMutationBusy || item == null || string.IsNullOrWhiteSpace(item.Id)) return;

            var cfg = LoadSupabaseClientConfigResolved();
            if (cfg == null)
            {
                try { ShowSupabaseServerError("Inventory delete", "ربط السيرفر غير مفعل."); } catch { }
                return;
            }

            _posInventoryMutationBusy = true;
            try
            {
                var raw = await PostSupabaseRpcGetStringAsync(
                    cfg,
                    "api_inventory_delete_item",
                    SerializeJson(new PosInventoryDeleteItemRequest { InventoryItemId = item.Id.Trim() }),
                    null,
                    null,
                    true).ConfigureAwait(false);

                var parsed = DeserializeJson<PosInventoryRpcResponse>(raw);
                if (parsed == null || parsed.Ok == false)
                {
                    throw new InvalidOperationException(parsed != null && !string.IsNullOrWhiteSpace(parsed.Error)
                        ? parsed.Error.Trim()
                        : "تعذر حذف عنصر المخزون.");
                }

                if (string.Equals(_inventoryEditingItemId, item.Id, StringComparison.OrdinalIgnoreCase))
                {
                    ResetInventoryItemDraft();
                }
                RefreshInventoryOverviewAsync(false);
            }
            catch (Exception ex)
            {
                try { ShowSupabaseServerError("Inventory delete", ex == null ? "تعذر حذف عنصر المخزون." : ex.Message); } catch { }
            }
            finally
            {
                _posInventoryMutationBusy = false;
            }
        }

        private async Task SaveInventoryTargetModeAsync(PosInventoryAdminTargetRow target, ComboBox modeCombo)
        {
            if (_posInventoryMutationBusy || target == null) return;

            var cfg = LoadSupabaseClientConfigResolved();
            if (cfg == null)
            {
                try { ShowSupabaseServerError("Inventory mode", "ربط السيرفر غير مفعل."); } catch { }
                return;
            }

            var mode = GetSelectedComboValue(modeCombo, "available");
            var key = BuildInventoryTargetKey(target.EntityKind, target.EntityId);

            _posInventoryMutationBusy = true;
            try
            {
                var raw = await PostSupabaseRpcGetStringAsync(
                    cfg,
                    "api_inventory_set_entity_mode",
                    SerializeJson(new PosInventorySetModeRequest
                    {
                        EntityKind = (target.EntityKind ?? string.Empty).Trim().ToUpperInvariant(),
                        EntityId = (target.EntityId ?? string.Empty).Trim(),
                        InventoryMode = mode
                    }),
                    null,
                    null,
                    true).ConfigureAwait(false);

                var parsed = DeserializeJson<PosInventoryRpcResponse>(raw);
                if (parsed == null || parsed.Ok == false)
                {
                    throw new InvalidOperationException(parsed != null && !string.IsNullOrWhiteSpace(parsed.Error)
                        ? parsed.Error.Trim()
                        : "تعذر حفظ حالة الصنف.");
                }

                _posInventoryModeDrafts.Remove(key);
                RefreshInventoryOverviewAsync(false);
            }
            catch (Exception ex)
            {
                try { ShowSupabaseServerError("Inventory mode", ex == null ? "تعذر حفظ حالة الصنف." : ex.Message); } catch { }
            }
            finally
            {
                _posInventoryMutationBusy = false;
            }
        }

        private async Task SaveInventoryTargetLinksAsync(PosInventoryAdminTargetRow target)
        {
            if (_posInventoryMutationBusy || target == null) return;

            var cfg = LoadSupabaseClientConfigResolved();
            if (cfg == null)
            {
                try { ShowSupabaseServerError("Inventory links", "ربط السيرفر غير مفعل."); } catch { }
                return;
            }

            var draftLinks = GetDraftInventoryLinks(target, CopyInventoryLinks());
            var payload = new List<PosInventoryReplaceLinkPayload>();
            for (int i = 0; i < draftLinks.Count; i++)
            {
                var link = draftLinks[i];
                if (link == null || string.IsNullOrWhiteSpace(link.InventoryItemId) || link.ConsumptionQuantity <= 0) continue;
                payload.Add(new PosInventoryReplaceLinkPayload
                {
                    InventoryItemId = link.InventoryItemId.Trim(),
                    ConsumptionQuantity = link.ConsumptionQuantity
                });
            }

            _posInventoryMutationBusy = true;
            try
            {
                var raw = await PostSupabaseRpcGetStringAsync(
                    cfg,
                    "api_inventory_replace_entity_links",
                    SerializeJson(new PosInventoryReplaceLinksRequest
                    {
                        EntityKind = (target.EntityKind ?? string.Empty).Trim().ToUpperInvariant(),
                        EntityId = (target.EntityId ?? string.Empty).Trim(),
                        Links = payload
                    }),
                    null,
                    null,
                    true).ConfigureAwait(false);

                var parsed = DeserializeJson<PosInventoryRpcResponse>(raw);
                if (parsed == null || parsed.Ok == false)
                {
                    throw new InvalidOperationException(parsed != null && !string.IsNullOrWhiteSpace(parsed.Error)
                        ? parsed.Error.Trim()
                        : "تعذر حفظ روابط الاستهلاك.");
                }

                _posInventoryLinkDrafts.Remove(BuildInventoryTargetKey(target.EntityKind, target.EntityId));
                RefreshInventoryOverviewAsync(false);
            }
            catch (Exception ex)
            {
                try { ShowSupabaseServerError("Inventory links", ex == null ? "تعذر حفظ روابط الاستهلاك." : ex.Message); } catch { }
            }
            finally
            {
                _posInventoryMutationBusy = false;
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        private readonly List<PosInventoryAdminItemRow> _posInventoryItems = new List<PosInventoryAdminItemRow>();
        private readonly List<PosInventoryAdminLinkRow> _posInventoryLinks = new List<PosInventoryAdminLinkRow>();
        private readonly List<PosInventoryAdminTargetRow> _posInventoryTargets = new List<PosInventoryAdminTargetRow>();
        private readonly Dictionary<string, string> _posInventoryModeDrafts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PosInventoryAdminLinkRow>> _posInventoryLinkDrafts = new Dictionary<string, List<PosInventoryAdminLinkRow>>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _posInventoryOpenTargetEditors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private bool _posInventorySnapshotLoading;
        private bool _posInventoryMutationBusy;
        private string _posInventorySnapshotError = string.Empty;

        private string _inventoryEditingItemId = string.Empty;
        private string _inventoryItemDraftName = string.Empty;
        private string _inventoryItemDraftDisplayUnit = "piece";
        private string _inventoryItemDraftQuantity = string.Empty;
        private string _inventoryItemDraftLowStock = string.Empty;
        private bool _inventoryItemDraftActive = true;
        private string _inventoryOverviewSection = "targets";

        private List<PosInventoryAdminItemRow> CopyInventoryItems()
        {
            lock (_posInventoryItems)
            {
                return new List<PosInventoryAdminItemRow>(_posInventoryItems);
            }
        }

        private List<PosInventoryAdminLinkRow> CopyInventoryLinks()
        {
            lock (_posInventoryLinks)
            {
                return new List<PosInventoryAdminLinkRow>(_posInventoryLinks);
            }
        }

        private List<PosInventoryAdminTargetRow> CopyInventoryTargets()
        {
            lock (_posInventoryTargets)
            {
                return new List<PosInventoryAdminTargetRow>(_posInventoryTargets);
            }
        }

        private string NormalizeInventoryOverviewSection(string section)
        {
            return string.Equals((section ?? string.Empty).Trim(), "items", StringComparison.OrdinalIgnoreCase)
                ? "items"
                : "targets";
        }

        private string BuildInventoryTargetKey(string entityKind, string entityId)
        {
            return ((entityKind ?? string.Empty).Trim().ToUpperInvariant()) + "|" + ((entityId ?? string.Empty).Trim());
        }

        private string GetDraftInventoryMode(PosInventoryAdminTargetRow target)
        {
            var key = BuildInventoryTargetKey(target.EntityKind, target.EntityId);
            string mode;
            if (_posInventoryModeDrafts.TryGetValue(key, out mode))
            {
                return NormalizeInventoryMode(mode);
            }

            mode = NormalizeInventoryMode(target.InventoryMode);
            _posInventoryModeDrafts[key] = mode;
            return mode;
        }

        private List<PosInventoryAdminLinkRow> GetDraftInventoryLinks(PosInventoryAdminTargetRow target, List<PosInventoryAdminLinkRow> snapshotLinks)
        {
            var key = BuildInventoryTargetKey(target.EntityKind, target.EntityId);
            List<PosInventoryAdminLinkRow> existing;
            if (_posInventoryLinkDrafts.TryGetValue(key, out existing))
            {
                return CloneInventoryLinks(existing);
            }

            var seeded = new List<PosInventoryAdminLinkRow>();
            for (int i = 0; i < snapshotLinks.Count; i++)
            {
                var link = snapshotLinks[i];
                if (link == null) continue;
                if (!string.Equals((link.EntityKind ?? string.Empty).Trim(), (target.EntityKind ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals((link.EntityId ?? string.Empty).Trim(), (target.EntityId ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                seeded.Add(CloneInventoryLink(link));
            }

            _posInventoryLinkDrafts[key] = CloneInventoryLinks(seeded);
            return seeded;
        }

        private List<PosInventoryAdminLinkRow> CloneInventoryLinks(List<PosInventoryAdminLinkRow> source)
        {
            var clone = new List<PosInventoryAdminLinkRow>();
            if (source == null) return clone;
            for (int i = 0; i < source.Count; i++)
            {
                var row = source[i];
                if (row == null) continue;
                clone.Add(CloneInventoryLink(row));
            }
            return clone;
        }

        private PosInventoryAdminLinkRow CloneInventoryLink(PosInventoryAdminLinkRow row)
        {
            if (row == null) return null;
            return new PosInventoryAdminLinkRow
            {
                EntityKind = row.EntityKind,
                EntityId = row.EntityId,
                InventoryItemId = row.InventoryItemId,
                ConsumptionQuantity = row.ConsumptionQuantity
            };
        }

        private string NormalizeInventoryMode(string raw)
        {
            var value = (raw ?? string.Empty).Trim().ToLowerInvariant();
            if (value == "linked") return "linked";
            if (value == "unavailable") return "unavailable";
            return "available";
        }

        private double ToBaseInventoryQuantity(double quantity, string displayUnit)
        {
            var unit = (displayUnit ?? string.Empty).Trim().ToLowerInvariant();
            if (unit == "kilogram" || unit == "liter") return quantity * 1000d;
            return quantity;
        }

        private double FromBaseInventoryQuantity(double quantity, string displayUnit)
        {
            var unit = (displayUnit ?? string.Empty).Trim().ToLowerInvariant();
            if (unit == "kilogram" || unit == "liter") return quantity / 1000d;
            return quantity;
        }

        private PosInventoryAdminItemRow FindInventoryItemById(string inventoryItemId)
        {
            return FindInventoryItemById(inventoryItemId, CopyInventoryItems());
        }

        private PosInventoryAdminItemRow FindInventoryItemById(string inventoryItemId, List<PosInventoryAdminItemRow> items)
        {
            inventoryItemId = (inventoryItemId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(inventoryItemId) || items == null) return null;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.Id)) continue;
                if (string.Equals(item.Id.Trim(), inventoryItemId, StringComparison.OrdinalIgnoreCase)) return item;
            }
            return null;
        }

        private void SeedInventoryItemDraft(PosInventoryAdminItemRow item)
        {
            if (item == null)
            {
                ResetInventoryItemDraft();
                return;
            }

            _inventoryEditingItemId = item.Id ?? string.Empty;
            _inventoryItemDraftName = item.Name ?? string.Empty;
            _inventoryItemDraftDisplayUnit = string.IsNullOrWhiteSpace(item.DisplayUnit) ? "piece" : item.DisplayUnit.Trim().ToLowerInvariant();
            _inventoryItemDraftQuantity = FormatInventoryNumber(FromBaseInventoryQuantity(item.QuantityOnHand, _inventoryItemDraftDisplayUnit));
            _inventoryItemDraftLowStock = FormatInventoryNumber(FromBaseInventoryQuantity(item.LowStockThreshold, _inventoryItemDraftDisplayUnit));
            _inventoryItemDraftActive = item.Active;
        }

        private void ResetInventoryItemDraft()
        {
            _inventoryEditingItemId = string.Empty;
            _inventoryItemDraftName = string.Empty;
            _inventoryItemDraftDisplayUnit = "piece";
            _inventoryItemDraftQuantity = string.Empty;
            _inventoryItemDraftLowStock = string.Empty;
            _inventoryItemDraftActive = true;
        }

        private void EnsureInventoryItemDraftDefaults()
        {
            if (string.IsNullOrWhiteSpace(_inventoryItemDraftDisplayUnit))
            {
                _inventoryItemDraftDisplayUnit = "piece";
            }
        }

        private string TryAddInventoryDraftLink(
            PosInventoryAdminTargetRow target,
            string inventoryItemId,
            string quantityText,
            List<PosInventoryAdminItemRow> items)
        {
            if (target == null) return "تعذر تحديد الصنف المطلوب.";
            inventoryItemId = (inventoryItemId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(inventoryItemId)) return "اختر عنصر مخزون أولاً.";

            var item = FindInventoryItemById(inventoryItemId, items);
            if (item == null) return "عنصر المخزون المحدد غير موجود.";

            double quantity;
            if (!double.TryParse((quantityText ?? string.Empty).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out quantity))
            {
                if (!double.TryParse((quantityText ?? string.Empty).Trim(), System.Globalization.NumberStyles.Float, _arEg, out quantity))
                {
                    return "اكتب كمية استهلاك صحيحة.";
                }
            }

            if (quantity <= 0) return "كمية الاستهلاك يجب أن تكون أكبر من صفر.";

            var key = BuildInventoryTargetKey(target.EntityKind, target.EntityId);
            var draftLinks = GetDraftInventoryLinks(target, CopyInventoryLinks());
            var next = new List<PosInventoryAdminLinkRow>();

            for (int i = 0; i < draftLinks.Count; i++)
            {
                var link = draftLinks[i];
                if (link == null) continue;
                if (string.Equals(link.InventoryItemId, inventoryItemId, StringComparison.OrdinalIgnoreCase)) continue;
                next.Add(CloneInventoryLink(link));
            }

            next.Add(new PosInventoryAdminLinkRow
            {
                EntityKind = (target.EntityKind ?? string.Empty).Trim().ToUpperInvariant(),
                EntityId = (target.EntityId ?? string.Empty).Trim(),
                InventoryItemId = inventoryItemId,
                ConsumptionQuantity = ToBaseInventoryQuantity(quantity, item.DisplayUnit)
            });

            _posInventoryLinkDrafts[key] = next;
            _posInventoryOpenTargetEditors.Add(key);
            return null;
        }

        private void RemoveDraftInventoryLink(PosInventoryAdminTargetRow target, PosInventoryAdminLinkRow link)
        {
            if (target == null || link == null) return;
            var key = BuildInventoryTargetKey(target.EntityKind, target.EntityId);
            var draftLinks = GetDraftInventoryLinks(target, CopyInventoryLinks());
            var next = new List<PosInventoryAdminLinkRow>();
            for (int i = 0; i < draftLinks.Count; i++)
            {
                var row = draftLinks[i];
                if (row == null) continue;
                if (string.Equals(row.InventoryItemId, link.InventoryItemId, StringComparison.OrdinalIgnoreCase)) continue;
                next.Add(CloneInventoryLink(row));
            }
            _posInventoryLinkDrafts[key] = next;
            _posInventoryOpenTargetEditors.Add(key);
        }

        private void PruneInventoryDrafts()
        {
            var validTargetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            lock (_posInventoryTargets)
            {
                for (int i = 0; i < _posInventoryTargets.Count; i++)
                {
                    var target = _posInventoryTargets[i];
                    if (target == null) continue;
                    validTargetKeys.Add(BuildInventoryTargetKey(target.EntityKind, target.EntityId));
                }
            }

            var modeKeys = new List<string>(_posInventoryModeDrafts.Keys);
            for (int i = 0; i < modeKeys.Count; i++)
            {
                if (!validTargetKeys.Contains(modeKeys[i])) _posInventoryModeDrafts.Remove(modeKeys[i]);
            }

            var linkKeys = new List<string>(_posInventoryLinkDrafts.Keys);
            for (int i = 0; i < linkKeys.Count; i++)
            {
                if (!validTargetKeys.Contains(linkKeys[i])) _posInventoryLinkDrafts.Remove(linkKeys[i]);
            }

            var openKeys = new List<string>(_posInventoryOpenTargetEditors);
            for (int i = 0; i < openKeys.Count; i++)
            {
                if (!validTargetKeys.Contains(openKeys[i])) _posInventoryOpenTargetEditors.Remove(openKeys[i]);
            }

            if (!string.IsNullOrWhiteSpace(_inventoryEditingItemId))
            {
                var found = FindInventoryItemById(_inventoryEditingItemId);
                if (found == null) ResetInventoryItemDraft();
            }
        }
    }
}

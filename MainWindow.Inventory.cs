using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

        [DataContract]
        private sealed class PosInventoryAdminSnapshotResponse
        {
            [DataMember(Name = "ok", EmitDefaultValue = false)] public bool? Ok { get; set; }
            [DataMember(Name = "error", EmitDefaultValue = false)] public string Error { get; set; }
            [DataMember(Name = "inventoryItems", EmitDefaultValue = false)] public List<PosInventoryAdminItemRow> InventoryItems { get; set; }
            [DataMember(Name = "inventoryLinks", EmitDefaultValue = false)] public List<PosInventoryAdminLinkRow> InventoryLinks { get; set; }
            [DataMember(Name = "targets", EmitDefaultValue = false)] public List<PosInventoryAdminTargetRow> Targets { get; set; }
        }

        [DataContract]
        private sealed class PosInventoryAdminItemRow
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
        private sealed class PosInventoryAdminLinkRow
        {
            [DataMember(Name = "inventoryItemId", EmitDefaultValue = false)] public string InventoryItemId { get; set; }
            [DataMember(Name = "entityKind", EmitDefaultValue = false)] public string EntityKind { get; set; }
            [DataMember(Name = "entityId", EmitDefaultValue = false)] public string EntityId { get; set; }
            [DataMember(Name = "consumptionQuantity", EmitDefaultValue = false)] public double ConsumptionQuantity { get; set; }
        }

        [DataContract]
        private sealed class PosInventoryAdminTargetRow
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
        private sealed class PosInventoryRpcResponse
        {
            [DataMember(Name = "ok", EmitDefaultValue = false)] public bool? Ok { get; set; }
            [DataMember(Name = "error", EmitDefaultValue = false)] public string Error { get; set; }
            [DataMember(Name = "itemId", EmitDefaultValue = false)] public string ItemId { get; set; }
            [DataMember(Name = "updated", EmitDefaultValue = false)] public bool? Updated { get; set; }
            [DataMember(Name = "deleted", EmitDefaultValue = false)] public bool? Deleted { get; set; }
        }

        [DataContract]
        private sealed class PosInventoryUpsertItemRequest
        {
            [DataMember(Name = "p_item", EmitDefaultValue = false)] public PosInventoryUpsertItemPayload Item { get; set; }
        }

        [DataContract]
        private sealed class PosInventoryUpsertItemPayload
        {
            [DataMember(Name = "id", EmitDefaultValue = false)] public string Id { get; set; }
            [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }
            [DataMember(Name = "displayUnit", EmitDefaultValue = false)] public string DisplayUnit { get; set; }
            [DataMember(Name = "quantityOnHand", EmitDefaultValue = false)] public double QuantityOnHand { get; set; }
            [DataMember(Name = "lowStockThreshold", EmitDefaultValue = false)] public double LowStockThreshold { get; set; }
            [DataMember(Name = "active", EmitDefaultValue = false)] public bool Active { get; set; }
        }

        [DataContract]
        private sealed class PosInventoryDeleteItemRequest
        {
            [DataMember(Name = "p_inventory_item_id", EmitDefaultValue = false)] public string InventoryItemId { get; set; }
        }

        [DataContract]
        private sealed class PosInventorySetModeRequest
        {
            [DataMember(Name = "p_entity_kind", EmitDefaultValue = false)] public string EntityKind { get; set; }
            [DataMember(Name = "p_entity_id", EmitDefaultValue = false)] public string EntityId { get; set; }
            [DataMember(Name = "p_inventory_mode", EmitDefaultValue = false)] public string InventoryMode { get; set; }
        }

        [DataContract]
        private sealed class PosInventoryReplaceLinksRequest
        {
            [DataMember(Name = "p_entity_kind", EmitDefaultValue = false)] public string EntityKind { get; set; }
            [DataMember(Name = "p_entity_id", EmitDefaultValue = false)] public string EntityId { get; set; }
            [DataMember(Name = "p_links", EmitDefaultValue = false)] public List<PosInventoryReplaceLinkPayload> Links { get; set; }
        }

        [DataContract]
        private sealed class PosInventoryReplaceLinkPayload
        {
            [DataMember(Name = "inventoryItemId", EmitDefaultValue = false)] public string InventoryItemId { get; set; }
            [DataMember(Name = "consumptionQuantity", EmitDefaultValue = false)] public double ConsumptionQuantity { get; set; }
        }

        private void TakeawayInventoryButton_Click(object sender, RoutedEventArgs e)
        {
            OpenInventoryOverviewOverlay();
        }

        private void DeliveryInventoryButton_Click(object sender, RoutedEventArgs e)
        {
            OpenInventoryOverviewOverlay();
        }

        private void InventoryOverviewRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshInventoryOverviewAsync(true);
        }

        private void InventoryOverviewCloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseInventoryOverviewOverlay();
        }

        private void InventoryOverviewOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ReferenceEquals(e.OriginalSource, InventoryOverviewOverlay)) CloseInventoryOverviewOverlay();
        }

        private void InventoryOverviewModalBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Prevent accidental close when clicking inside the modal.
        }

        private void OpenInventoryOverviewOverlay()
        {
            if (InventoryOverviewOverlay == null) return;
            InventoryOverviewOverlay.Visibility = Visibility.Visible;
            RenderInventoryOverviewRows();
            RefreshInventoryOverviewAsync(true);
        }

        private void CloseInventoryOverviewOverlay()
        {
            if (InventoryOverviewOverlay != null) InventoryOverviewOverlay.Visibility = Visibility.Collapsed;
        }

        private void QueueInventoryOverviewRefreshIfVisible()
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (InventoryOverviewOverlay == null) return;
                    if (InventoryOverviewOverlay.Visibility != Visibility.Visible) return;
                    RefreshInventoryOverviewAsync(false);
                }));
            }
            catch
            {
            }
        }

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

        private void RenderInventoryOverviewRows()
        {
            if (InventoryOverviewRowsPanel == null) return;
            InventoryOverviewRowsPanel.Children.Clear();

            if (InventoryOverviewTitleText != null)
            {
                InventoryOverviewTitleText.Text = string.Equals(NormalizeInventoryOverviewSection(_inventoryOverviewSection), "items", StringComparison.OrdinalIgnoreCase)
                    ? "عناصر المخزون"
                    : "الأصناف والربط";
            }

            if (_posInventorySnapshotLoading && _posInventoryItems.Count == 0 && _posInventoryTargets.Count == 0)
            {
                InventoryOverviewRowsPanel.Children.Add(BuildInventoryOverviewMessageCard("جارٍ تحميل المخزون والمنتجات من السيرفر..."));
                return;
            }

            if (!string.IsNullOrWhiteSpace(_posInventorySnapshotError))
            {
                InventoryOverviewRowsPanel.Children.Add(BuildInventoryOverviewMessageCard(_posInventorySnapshotError));
            }

            var snapshotItems = CopyInventoryItems();
            var snapshotLinks = CopyInventoryLinks();
            var snapshotTargets = CopyInventoryTargets();

            InventoryOverviewRowsPanel.Children.Add(BuildInventoryTabStrip());
            InventoryOverviewRowsPanel.Children.Add(BuildInventorySummaryCard(snapshotItems, snapshotTargets));
            if (string.Equals(NormalizeInventoryOverviewSection(_inventoryOverviewSection), "items", StringComparison.OrdinalIgnoreCase))
            {
                InventoryOverviewRowsPanel.Children.Add(BuildInventoryItemsSection(snapshotItems, snapshotLinks));
            }
            else
            {
                InventoryOverviewRowsPanel.Children.Add(BuildInventoryTargetsSection(snapshotItems, snapshotLinks, snapshotTargets));
            }
        }

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

        private Border BuildInventoryTabStrip()
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFDFDFD")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD4D4D4")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var row = new WrapPanel
            {
                FlowDirection = FlowDirection.RightToLeft
            };
            row.Children.Add(BuildInventoryTabButton("targets", "الأصناف"));
            row.Children.Add(BuildInventoryTabButton("items", "عناصر المخزون"));

            border.Child = row;
            return border;
        }

        private Button BuildInventoryTabButton(string section, string text)
        {
            var active = string.Equals(NormalizeInventoryOverviewSection(_inventoryOverviewSection), NormalizeInventoryOverviewSection(section), StringComparison.OrdinalIgnoreCase);
            var background = active ? "#FF1C5D99" : "#FFF2F5F9";
            var foreground = active ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1E1E1E"));
            var border = active ? "#FF1C5D99" : "#FFCCD5DE";

            var button = new Button
            {
                Content = text ?? string.Empty,
                Height = 36,
                MinWidth = 150,
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(16, 4, 16, 4),
                FontSize = 14,
                FontWeight = FontWeights.Black,
                Foreground = foreground,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(background)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(border))
            };
            button.Click += delegate
            {
                var next = NormalizeInventoryOverviewSection(section);
                if (string.Equals(next, NormalizeInventoryOverviewSection(_inventoryOverviewSection), StringComparison.OrdinalIgnoreCase)) return;
                _inventoryOverviewSection = next;
                RenderInventoryOverviewRows();
            };
            return button;
        }

        private Border BuildInventorySummaryCard(List<PosInventoryAdminItemRow> items, List<PosInventoryAdminTargetRow> targets)
        {
            int activeItems = 0;
            int outOfStockTargets = 0;
            int linkedTargets = 0;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].Active) activeItems++;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null) continue;
                if (NormalizeInventoryMode(target.InventoryMode) == "linked") linkedTargets++;
                if (string.Equals(NormalizeInventoryStatus(target.InventoryStatus), "out_of_stock", StringComparison.OrdinalIgnoreCase)) outOfStockTargets++;
            }

            var stack = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            stack.Children.Add(new TextBlock
            {
                Text = "اختر تبويب الأصناف لإدارة الربط، أو تبويب عناصر المخزون لإدارة الكميات والوحدات.",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1E1E1E")),
                TextWrapping = TextWrapping.Wrap
            });
            stack.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                Text = "الصنف هنا موحّد، وسترى شارة توضّح هل هو ظاهر في التطبيق أو الكاشير أو الاثنين. إذا اخترت \"مرتبط بالمخزون\" ولم تضف روابط استهلاك، فسيصبح الصنف غير متاح تلقائيًا.",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF555555")),
                TextWrapping = TextWrapping.Wrap
            });

            var badges = new WrapPanel
            {
                Margin = new Thickness(0, 12, 0, 0),
                FlowDirection = FlowDirection.RightToLeft
            };
            badges.Children.Add(BuildInventoryBadge("عناصر المخزون: " + items.Count.ToString(CultureInfo.InvariantCulture), "#FF1C5D99"));
            badges.Children.Add(BuildInventoryBadge("العناصر النشطة: " + activeItems.ToString(CultureInfo.InvariantCulture), "#FF247A3D"));
            badges.Children.Add(BuildInventoryBadge("أصناف مرتبطة: " + linkedTargets.ToString(CultureInfo.InvariantCulture), "#FF7A4D11"));
            badges.Children.Add(BuildInventoryBadge("غير متاح الآن: " + outOfStockTargets.ToString(CultureInfo.InvariantCulture), "#FF9E3B3B"));
            stack.Children.Add(badges);

            return WrapInventorySection("نظرة سريعة", stack);
        }

        private Border BuildInventoryTargetsSection(
            List<PosInventoryAdminItemRow> items,
            List<PosInventoryAdminLinkRow> links,
            List<PosInventoryAdminTargetRow> targets)
        {
            var root = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            root.Children.Add(new TextBlock
            {
                Text = "الأصناف القابلة للربط",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = Brushes.Black
            });
            root.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 4, 0, 12),
                Text = "سترى هنا الصنف الموحّد مع شارة توضّح هل هو للتطبيق أو للكاشير أو للاثنين. غيّر الحالة مباشرة، أو اربطه بعنصر مخزون وحدد كمية الاستهلاك.",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF555555")),
                TextWrapping = TextWrapping.Wrap
            });

            var products = new List<PosInventoryAdminTargetRow>();
            var options = new List<PosInventoryAdminTargetRow>();
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null) continue;
                if (string.Equals(target.EntityKind, "SOURCE_PRODUCT", StringComparison.OrdinalIgnoreCase))
                {
                    products.Add(target);
                }
                else
                {
                    options.Add(target);
                }
            }

            products.Sort(delegate (PosInventoryAdminTargetRow a, PosInventoryAdminTargetRow b)
            {
                return string.Compare(BuildInventoryTargetDisplayName(a), BuildInventoryTargetDisplayName(b), StringComparison.CurrentCultureIgnoreCase);
            });
            options.Sort(delegate (PosInventoryAdminTargetRow a, PosInventoryAdminTargetRow b)
            {
                return string.Compare(BuildInventoryTargetDisplayName(a), BuildInventoryTargetDisplayName(b), StringComparison.CurrentCultureIgnoreCase);
            });

            if (products.Count == 0 && options.Count == 0)
            {
                root.Children.Add(BuildInventoryOverviewMessageCard("لا توجد منتجات أو إضافات يمكن إدارتها الآن."));
                return WrapInventorySection("المنتجات والإضافات", root);
            }

            if (products.Count > 0)
            {
                root.Children.Add(BuildInventorySubsectionLabel("المنتجات"));
                for (int i = 0; i < products.Count; i++)
                {
                    root.Children.Add(BuildInventoryTargetCard(products[i], items, links));
                }
            }

            if (options.Count > 0)
            {
                root.Children.Add(BuildInventorySubsectionLabel("الإضافات والخبز"));
                for (int i = 0; i < options.Count; i++)
                {
                    root.Children.Add(BuildInventoryTargetCard(options[i], items, links));
                }
            }

            return WrapInventorySection("المنتجات والإضافات", root);
        }

        private Border BuildInventoryItemsSection(List<PosInventoryAdminItemRow> items, List<PosInventoryAdminLinkRow> links)
        {
            EnsureInventoryItemDraftDefaults();

            var root = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            root.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(_inventoryEditingItemId) ? "إضافة عنصر مخزون" : "تعديل عنصر مخزون",
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = Brushes.Black
            });
            root.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 4, 0, 12),
                Text = "أضف عنصر المخزون باسمه ووحدته وكميته الحالية، ثم استخدمه في ربط المنتجات.",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF555555")),
                TextWrapping = TextWrapping.Wrap
            });

            var formCard = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF9FBFF")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC2D7EB")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var formStack = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            var fields = new WrapPanel { FlowDirection = FlowDirection.RightToLeft, ItemWidth = 220 };

            var nameBox = new TextBox
            {
                Height = 34,
                Text = _inventoryItemDraftName ?? string.Empty,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalContentAlignment = HorizontalAlignment.Right
            };
            nameBox.TextChanged += delegate
            {
                _inventoryItemDraftName = nameBox.Text ?? string.Empty;
            };
            fields.Children.Add(BuildInventoryField("اسم عنصر المخزون", nameBox, 250));

            var unitCombo = BuildInventoryModeComboBox();
            unitCombo.Items.Clear();
            unitCombo.Items.Add(BuildComboOption("piece", "قطعة"));
            unitCombo.Items.Add(BuildComboOption("gram", "جرام"));
            unitCombo.Items.Add(BuildComboOption("kilogram", "كيلوجرام"));
            unitCombo.Items.Add(BuildComboOption("milliliter", "مليلتر"));
            unitCombo.Items.Add(BuildComboOption("liter", "لتر"));
            SelectComboOption(unitCombo, string.IsNullOrWhiteSpace(_inventoryItemDraftDisplayUnit) ? "piece" : _inventoryItemDraftDisplayUnit);
            unitCombo.SelectionChanged += delegate
            {
                _inventoryItemDraftDisplayUnit = GetSelectedComboValue(unitCombo, "piece");
            };
            fields.Children.Add(BuildInventoryField("وحدة العرض", unitCombo, 180));

            var quantityBox = new TextBox
            {
                Height = 34,
                Text = _inventoryItemDraftQuantity ?? string.Empty,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalContentAlignment = HorizontalAlignment.Right
            };
            quantityBox.TextChanged += delegate
            {
                _inventoryItemDraftQuantity = quantityBox.Text ?? string.Empty;
            };
            fields.Children.Add(BuildInventoryField("الكمية الحالية", quantityBox, 160));

            var lowStockBox = new TextBox
            {
                Height = 34,
                Text = _inventoryItemDraftLowStock ?? string.Empty,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalContentAlignment = HorizontalAlignment.Right
            };
            lowStockBox.TextChanged += delegate
            {
                _inventoryItemDraftLowStock = lowStockBox.Text ?? string.Empty;
            };
            fields.Children.Add(BuildInventoryField("حد الكمية المحدودة", lowStockBox, 170));

            var activeCombo = BuildInventoryModeComboBox();
            activeCombo.Items.Clear();
            activeCombo.Items.Add(BuildComboOption("true", "نشط"));
            activeCombo.Items.Add(BuildComboOption("false", "موقوف"));
            SelectComboOption(activeCombo, _inventoryItemDraftActive ? "true" : "false");
            activeCombo.SelectionChanged += delegate
            {
                _inventoryItemDraftActive = string.Equals(GetSelectedComboValue(activeCombo, "true"), "true", StringComparison.OrdinalIgnoreCase);
            };
            fields.Children.Add(BuildInventoryField("الحالة", activeCombo, 150));

            formStack.Children.Add(fields);

            var buttons = new WrapPanel
            {
                Margin = new Thickness(0, 10, 0, 0),
                FlowDirection = FlowDirection.RightToLeft
            };

            var saveButton = BuildInventoryActionButton(
                string.IsNullOrWhiteSpace(_inventoryEditingItemId) ? "حفظ عنصر المخزون" : "حفظ التعديل",
                "#FF1C5D99");
            saveButton.Click += async delegate
            {
                await SaveInventoryItemAsync().ConfigureAwait(false);
            };
            buttons.Children.Add(saveButton);

            var resetButton = BuildInventoryActionButton("عنصر جديد", "#FF666666");
            resetButton.Click += delegate
            {
                ResetInventoryItemDraft();
                RenderInventoryOverviewRows();
            };
            buttons.Children.Add(resetButton);

            formStack.Children.Add(buttons);
            formCard.Child = formStack;
            root.Children.Add(formCard);

            root.Children.Add(new TextBlock
            {
                Text = "عناصر المخزون الحالية",
                FontSize = 17,
                FontWeight = FontWeights.Black,
                Foreground = Brushes.Black
            });

            if (items.Count == 0)
            {
                root.Children.Add(BuildInventoryOverviewMessageCard("لا توجد عناصر مخزون حتى الآن."));
                return WrapInventorySection("عناصر المخزون", root);
            }

            items.Sort(delegate (PosInventoryAdminItemRow a, PosInventoryAdminItemRow b)
            {
                return string.Compare(BuildInventoryItemDisplayName(a), BuildInventoryItemDisplayName(b), StringComparison.CurrentCultureIgnoreCase);
            });

            for (int i = 0; i < items.Count; i++)
            {
                root.Children.Add(BuildInventoryItemCard(items[i], links));
            }

            return WrapInventorySection("عناصر المخزون", root);
        }

        private Border BuildInventoryTargetCard(
            PosInventoryAdminTargetRow target,
            List<PosInventoryAdminItemRow> items,
            List<PosInventoryAdminLinkRow> links)
        {
            var key = BuildInventoryTargetKey(target.EntityKind, target.EntityId);
            var mode = GetDraftInventoryMode(target);
            var draftLinks = GetDraftInventoryLinks(target, links);
            var isOpen = _posInventoryOpenTargetEditors.Contains(key);

            var card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD6D6D6")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var root = new StackPanel { FlowDirection = FlowDirection.RightToLeft };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition());
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            titleStack.Children.Add(new TextBlock
            {
                Text = BuildInventoryTargetDisplayName(target),
                FontSize = 18,
                FontWeight = FontWeights.Black,
                Foreground = Brushes.Black,
                TextWrapping = TextWrapping.Wrap
            });
            titleStack.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 4, 0, 0),
                Text = BuildInventoryTargetSubtitle(target),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5E5E5E")),
                TextWrapping = TextWrapping.Wrap
            });
            Grid.SetColumn(titleStack, 0);
            headerGrid.Children.Add(titleStack);

            var toggleButton = BuildInventoryActionButton(isOpen ? "إخفاء الربط" : "فتح الربط", "#FF666666");
            toggleButton.Content = isOpen ? "إخفاء الوصفة" : "فتح الوصفة";
            toggleButton.Click += delegate
            {
                if (_posInventoryOpenTargetEditors.Contains(key)) _posInventoryOpenTargetEditors.Remove(key);
                else _posInventoryOpenTargetEditors.Add(key);
                RenderInventoryOverviewRows();
            };
            Grid.SetColumn(toggleButton, 1);
            headerGrid.Children.Add(toggleButton);
            root.Children.Add(headerGrid);

            var badges = new WrapPanel
            {
                Margin = new Thickness(0, 10, 0, 0),
                FlowDirection = FlowDirection.RightToLeft
            };
            badges.Children.Add(BuildInventoryBadge("الحالة الآن: " + BuildInventoryStatusText(target.InventoryStatus), BuildInventoryStatusColor(target.InventoryStatus)));
            badges.Children.Add(BuildInventoryBadge(BuildInventoryCatalogScopeText(target), BuildInventoryCatalogScopeColor(target)));
            badges.Children.Add(BuildInventoryBadge("طريقة التوفر: " + BuildInventoryModeText(mode), BuildInventoryModeColor(mode)));
            if (target.InventoryRemaining.HasValue)
            {
                badges.Children.Add(BuildInventoryBadge("المتبقي: " + FormatInventoryNumber(target.InventoryRemaining.Value), "#FF7A4D11"));
            }
            badges.Children.Add(BuildInventoryBadge("الروابط: " + draftLinks.Count.ToString(CultureInfo.InvariantCulture), "#FF5E5E5E"));
            root.Children.Add(badges);

            root.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 12, 0, 0),
                Text = "1. حدّد طريقة التعامل مع الصنف",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = Brushes.Black
            });
            root.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 4, 0, 0),
                Text = "اختر أولًا هل هذا الصنف مرتبط بالمخزون، أم متاح يدويًا، أم غير متاح بالكامل.",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5E5E5E")),
                TextWrapping = TextWrapping.Wrap
            });

            var modeRow = new WrapPanel
            {
                Margin = new Thickness(0, 12, 0, 0),
                FlowDirection = FlowDirection.RightToLeft
            };
            var modeCombo = BuildInventoryModeComboBox();
            modeCombo.Items.Add(BuildComboOption("linked", "مرتبط بالمخزون"));
            modeCombo.Items.Add(BuildComboOption("available", "متاح يدويًا"));
            modeCombo.Items.Add(BuildComboOption("unavailable", "غير متاح"));
            SelectComboOption(modeCombo, mode);
            modeCombo.SelectionChanged += delegate
            {
                _posInventoryModeDrafts[key] = GetSelectedComboValue(modeCombo, "available");
            };
            modeRow.Children.Add(BuildInventoryField("حالة الصنف", modeCombo, 240));

            var saveModeButton = BuildInventoryActionButton("حفظ الخطوة 1", "#FF1C5D99");
            saveModeButton.Click += async delegate
            {
                await SaveInventoryTargetModeAsync(target, modeCombo).ConfigureAwait(false);
            };
            modeRow.Children.Add(saveModeButton);
            root.Children.Add(modeRow);

            if (isOpen || string.Equals(mode, "linked", StringComparison.OrdinalIgnoreCase))
            {
                root.Children.Add(BuildInventoryLinkEditor(target, items, draftLinks));
            }

            card.Child = root;
            return card;
        }

        private FrameworkElement BuildInventoryLinkEditor(
            PosInventoryAdminTargetRow target,
            List<PosInventoryAdminItemRow> items,
            List<PosInventoryAdminLinkRow> draftLinks)
        {
            var key = BuildInventoryTargetKey(target.EntityKind, target.EntityId);
            var container = new Border
            {
                Margin = new Thickness(0, 12, 0, 0),
                Padding = new Thickness(12),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF8FBFF")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC2D7EB")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12)
            };

            var root = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            root.Children.Add(new TextBlock
            {
                Text = "2. وصفة الصنف من المخزون",
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = Brushes.Black
            });
            root.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 4, 0, 10),
                Text = "أضف كل عناصر المخزون التي يحتاجها هذا الصنف. إذا كان المنتج يستهلك أكثر من شيء، أضف كل شيء كسطر مستقل داخل نفس الوصفة.",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5E5E5E")),
                TextWrapping = TextWrapping.Wrap
            });
            root.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 10),
                Text = BuildInventoryRecipeGuidance(target, draftLinks),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B4E16")),
                TextWrapping = TextWrapping.Wrap
            });

            if (draftLinks.Count == 0)
            {
                root.Children.Add(BuildInventoryOverviewMessageCard("لا توجد مكوّنات محفوظة في الوصفة حتى الآن."));
            }
            else
            {
                for (int i = 0; i < draftLinks.Count; i++)
                {
                    root.Children.Add(BuildInventoryLinkCard(target, draftLinks[i]));
                }
            }

            var addRow = new WrapPanel
            {
                Margin = new Thickness(0, 10, 0, 0),
                FlowDirection = FlowDirection.RightToLeft
            };

            var itemCombo = BuildInventoryModeComboBox();
            itemCombo.Width = 280;
            itemCombo.Items.Add(BuildComboOption(string.Empty, "اختر عنصر مخزون"));
            items.Sort(delegate (PosInventoryAdminItemRow a, PosInventoryAdminItemRow b)
            {
                return string.Compare(BuildInventoryItemDisplayName(a), BuildInventoryItemDisplayName(b), StringComparison.CurrentCultureIgnoreCase);
            });
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.Id)) continue;
                var label = BuildInventoryItemDisplayName(item) + " (" + InventoryDisplayUnitLabel(item.DisplayUnit) + ")";
                if (!item.Active) label += " - موقوف";
                itemCombo.Items.Add(BuildComboOption(item.Id.Trim(), label));
            }
            SelectComboOption(itemCombo, string.Empty);
            addRow.Children.Add(BuildInventoryField("عنصر المخزون", itemCombo, 300));

            var consumptionBox = new TextBox
            {
                Height = 34,
                Width = 140,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalContentAlignment = HorizontalAlignment.Right
            };
            addRow.Children.Add(BuildInventoryField("كمية الاستهلاك", consumptionBox, 160));

            var addButton = BuildInventoryActionButton("إضافة عنصر إلى الوصفة", "#FF247A3D");
            addButton.Click += delegate
            {
                var inventoryItemId = GetSelectedComboValue(itemCombo, string.Empty);
                var failure = TryAddInventoryDraftLink(target, inventoryItemId, consumptionBox.Text, items);
                if (!string.IsNullOrWhiteSpace(failure))
                {
                    try { ShowSupabaseServerError("Inventory link validation", failure); } catch { }
                    return;
                }
                RenderInventoryOverviewRows();
            };
            addRow.Children.Add(addButton);

            var saveLinksButton = BuildInventoryActionButton("حفظ الخطوة 2", "#FF1C5D99");
            saveLinksButton.Click += async delegate
            {
                await SaveInventoryTargetLinksAsync(target).ConfigureAwait(false);
            };
            addRow.Children.Add(saveLinksButton);

            root.Children.Add(addRow);
            container.Child = root;
            return container;
        }

        private Border BuildInventoryItemCard(PosInventoryAdminItemRow item, List<PosInventoryAdminLinkRow> links)
        {
            var linkCount = 0;
            for (int i = 0; i < links.Count; i++)
            {
                var link = links[i];
                if (link == null) continue;
                if (string.Equals(link.InventoryItemId, item.Id, StringComparison.OrdinalIgnoreCase)) linkCount++;
            }

            var isLow = item.Active && item.QuantityOnHand <= item.LowStockThreshold;
            var badgeColor = !item.Active ? "#FF8C8C8C" : (isLow ? "#FFE09C22" : "#FF247A3D");
            var badgeText = !item.Active ? "موقوف" : (isLow ? "كمية محدودة" : "متاح");

            var card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD6D6D6")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 8, 0, 0)
            };

            var root = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition());
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            titleStack.Children.Add(new TextBlock
            {
                Text = BuildInventoryItemDisplayName(item),
                FontSize = 17,
                FontWeight = FontWeights.Black,
                Foreground = Brushes.Black
            });
            titleStack.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 4, 0, 0),
                Text = "الوحدة: " + InventoryDisplayUnitLabel(item.DisplayUnit),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5E5E5E"))
            });
            Grid.SetColumn(titleStack, 0);
            header.Children.Add(titleStack);

            var badge = BuildInventoryBadge(badgeText, badgeColor);
            Grid.SetColumn(badge, 1);
            header.Children.Add(badge);
            root.Children.Add(header);

            root.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0),
                Text = "الكمية الحالية: " + FormatInventoryNumber(FromBaseInventoryQuantity(item.QuantityOnHand, item.DisplayUnit)) + " " + InventoryDisplayUnitLabel(item.DisplayUnit),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2A2A2A"))
            });
            root.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 4, 0, 0),
                Text = "حد الكمية المحدودة: " + FormatInventoryNumber(FromBaseInventoryQuantity(item.LowStockThreshold, item.DisplayUnit)) + " " + InventoryDisplayUnitLabel(item.DisplayUnit),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5E5E5E"))
            });
            root.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 4, 0, 0),
                Text = "عدد الروابط: " + linkCount.ToString(CultureInfo.InvariantCulture),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5E5E5E"))
            });

            var buttons = new WrapPanel
            {
                Margin = new Thickness(0, 10, 0, 0),
                FlowDirection = FlowDirection.RightToLeft
            };
            var editButton = BuildInventoryActionButton("تعديل", "#FF1C5D99");
            editButton.Click += delegate
            {
                SeedInventoryItemDraft(item);
                RenderInventoryOverviewRows();
            };
            buttons.Children.Add(editButton);

            var deleteButton = BuildInventoryActionButton("حذف", "#FF9E3B3B");
            deleteButton.Click += async delegate
            {
                var answer = MessageBox.Show(
                    "سيتم حذف عنصر المخزون \"" + BuildInventoryItemDisplayName(item) + "\" وروابطه. هل تريد المتابعة؟",
                    "حذف عنصر المخزون",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (answer != MessageBoxResult.Yes) return;
                await DeleteInventoryItemAsync(item).ConfigureAwait(false);
            };
            buttons.Children.Add(deleteButton);

            root.Children.Add(buttons);
            card.Child = root;
            return card;
        }

        private Border BuildInventoryLinkCard(PosInventoryAdminTargetRow target, PosInventoryAdminLinkRow link)
        {
            var item = FindInventoryItemById(link.InventoryItemId);
            var name = item == null
                ? "عنصر مخزون محذوف"
                : BuildInventoryItemDisplayName(item);
            var unit = item == null ? "قطعة" : InventoryDisplayUnitLabel(item.DisplayUnit);
            var qty = item == null
                ? FormatInventoryNumber(link.ConsumptionQuantity)
                : FormatInventoryNumber(FromBaseInventoryQuantity(link.ConsumptionQuantity, item.DisplayUnit));

            var card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD6D6D6")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var text = new TextBlock
            {
                Text = name + " - يستهلك " + qty + " " + unit,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                TextWrapping = TextWrapping.Wrap,
                FlowDirection = FlowDirection.RightToLeft
            };
            Grid.SetColumn(text, 0);
            row.Children.Add(text);

            var removeButton = BuildInventoryActionButton("إزالة", "#FF9E3B3B");
            removeButton.Click += delegate
            {
                RemoveDraftInventoryLink(target, link);
                RenderInventoryOverviewRows();
            };
            Grid.SetColumn(removeButton, 1);
            row.Children.Add(removeButton);

            card.Child = row;
            return card;
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
            if (!double.TryParse((quantityText ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out quantity))
            {
                if (!double.TryParse((quantityText ?? string.Empty).Trim(), NumberStyles.Float, _arEg, out quantity))
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

        private string BuildInventoryStatusText(string status)
        {
            var normalized = NormalizeInventoryStatus(status);
            if (string.Equals(normalized, "out_of_stock", StringComparison.OrdinalIgnoreCase)) return "غير متاح";
            if (string.Equals(normalized, "limited", StringComparison.OrdinalIgnoreCase)) return "كمية محدودة";
            return "متاح";
        }

        private string BuildInventoryStatusColor(string status)
        {
            var normalized = NormalizeInventoryStatus(status);
            if (string.Equals(normalized, "out_of_stock", StringComparison.OrdinalIgnoreCase)) return "#FF9E3B3B";
            if (string.Equals(normalized, "limited", StringComparison.OrdinalIgnoreCase)) return "#FFE09C22";
            return "#FF247A3D";
        }

        private string BuildInventoryModeText(string mode)
        {
            mode = NormalizeInventoryMode(mode);
            if (mode == "linked") return "مرتبط بالمخزون";
            if (mode == "unavailable") return "غير متاح";
            return "متاح يدويًا";
        }

        private string BuildInventoryModeColor(string mode)
        {
            mode = NormalizeInventoryMode(mode);
            if (mode == "linked") return "#FF7A4D11";
            if (mode == "unavailable") return "#FF9E3B3B";
            return "#FF1C5D99";
        }

        private string BuildInventoryTargetDisplayName(PosInventoryAdminTargetRow target)
        {
            var name = (target == null ? string.Empty : target.Name) ?? string.Empty;
            name = name.Trim();
            if (!string.IsNullOrWhiteSpace(name)) return name;
            return target != null && string.Equals(target.EntityKind, "ADDON_OPTION", StringComparison.OrdinalIgnoreCase)
                ? "إضافة بدون اسم"
                : "منتج بدون اسم";
        }

        private string BuildInventoryItemDisplayName(PosInventoryAdminItemRow item)
        {
            var name = (item == null ? string.Empty : item.Name) ?? string.Empty;
            name = name.Trim();
            if (!string.IsNullOrWhiteSpace(name)) return name;
            return "عنصر مخزون بدون اسم";
        }

        private string BuildInventoryCatalogScopeText(PosInventoryAdminTargetRow target)
        {
            if (target != null && string.Equals(target.EntityKind, "ADDON_OPTION", StringComparison.OrdinalIgnoreCase))
            {
                return "إضافة للتطبيق";
            }

            var scope = ((target == null ? string.Empty : target.CatalogScope) ?? string.Empty).Trim().ToLowerInvariant();
            if (scope == "both") return "التطبيق + الكاشير";
            if (scope == "app") return "التطبيق فقط";
            if (scope == "pos") return "الكاشير فقط";
            return "غير مربوط بكتالوج واضح";
        }

        private string BuildInventoryCatalogScopeColor(PosInventoryAdminTargetRow target)
        {
            if (target != null && string.Equals(target.EntityKind, "ADDON_OPTION", StringComparison.OrdinalIgnoreCase))
            {
                return "#FF5E5E5E";
            }

            var scope = ((target == null ? string.Empty : target.CatalogScope) ?? string.Empty).Trim().ToLowerInvariant();
            if (scope == "both") return "#FF7A4D11";
            if (scope == "app") return "#FF1C5D99";
            if (scope == "pos") return "#FF247A3D";
            return "#FF9E3B3B";
        }

        private string BuildInventoryRecipeGuidance(PosInventoryAdminTargetRow target, List<PosInventoryAdminLinkRow> draftLinks)
        {
            var mode = target == null ? "available" : GetDraftInventoryMode(target);
            if (draftLinks == null || draftLinks.Count == 0)
            {
                if (string.Equals(mode, "linked", StringComparison.OrdinalIgnoreCase))
                {
                    return "هذا الصنف مرتبط بالمخزون لكن بدون وصفة محفوظة، لذلك سيبقى غير متاح حتى تضيف كل مكوّناته.";
                }
                return "لا توجد وصفة مخزون محفوظة لهذا الصنف حاليًا.";
            }

            if (draftLinks.Count == 1)
            {
                return "وصفة الصنف بسيطة: عنصر واحد فقط يُخصم من المخزون لكل طلب.";
            }

            if (target != null && target.InventoryRemaining.HasValue)
            {
                return "وصفة هذا الصنف مركّبة: يحتاج " + draftLinks.Count.ToString(CultureInfo.InvariantCulture) + " عناصر معًا لكل طلب واحد. إذا نقص عنصر واحد فقط يصبح الصنف غير متاح، والمتبقي الحالي (" + FormatInventoryNumber(target.InventoryRemaining.Value) + ") محسوب على أقل عنصر متاح في الوصفة.";
            }

            return "وصفة هذا الصنف مركّبة: يحتاج " + draftLinks.Count.ToString(CultureInfo.InvariantCulture) + " عناصر معًا لكل طلب واحد. إذا نقص عنصر واحد فقط يصبح الصنف غير متاح.";
        }

        private string BuildInventoryTargetSubtitle(PosInventoryAdminTargetRow target)
        {
            var kind = string.Equals(target.EntityKind, "SOURCE_PRODUCT", StringComparison.OrdinalIgnoreCase)
                ? "منتج"
                : "إضافة أو خبز";
            var groupName = (target.GroupName ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(groupName))
            {
                return kind + " - " + groupName;
            }

            if (string.Equals(target.EntityKind, "ADDON_OPTION", StringComparison.OrdinalIgnoreCase))
            {
                return "إضافة أو خبز يمكن إيقافه أو ربطه بالمخزون";
            }

            var scope = (target.CatalogScope ?? string.Empty).Trim().ToLowerInvariant();
            if (scope == "both") return "صنف موحّد يظهر مرة واحدة ويتحكم في التطبيق والكاشير معًا";
            if (scope == "app") return "صنف ظاهر في التطبيق فقط، لكن مخزونه يُدار من هنا";
            if (scope == "pos") return "صنف ظاهر في الكاشير فقط، لكن مخزونه يُدار من هنا";
            return "صنف موجود في طبقة الربط المشتركة ويحتاج تسمية أو ربطًا أوضح في الكتالوج";
        }

        private Border WrapInventorySection(string title, UIElement child)
        {
            var wrapper = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFDFDFD")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD4D4D4")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 12)
            };
            wrapper.Child = child;
            return wrapper;
        }

        private TextBlock BuildInventorySubsectionLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                Margin = new Thickness(0, 8, 0, 8),
                FontSize = 16,
                FontWeight = FontWeights.Black,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1C5D99"))
            };
        }

        private Border BuildInventoryOverviewMessageCard(string text)
        {
            return new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD0D0D0")),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF9F9F9")),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 8),
                Child = new TextBlock
                {
                    Text = text ?? string.Empty,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF444444")),
                    FlowDirection = FlowDirection.RightToLeft,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }

        private Border BuildInventoryBadge(string text, string backgroundHex)
        {
            return new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundHex)),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(6, 0, 0, 6),
                Padding = new Thickness(10, 4, 10, 4),
                Child = new TextBlock
                {
                    Text = text ?? string.Empty,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Black,
                    FontSize = 12
                }
            };
        }

        private StackPanel BuildInventoryField(string label, FrameworkElement input, double width)
        {
            var panel = new StackPanel
            {
                Width = width,
                Margin = new Thickness(0, 0, 10, 10),
                FlowDirection = FlowDirection.RightToLeft
            };
            panel.Children.Add(new TextBlock
            {
                Text = label ?? string.Empty,
                Margin = new Thickness(0, 0, 0, 6),
                FontSize = 13,
                FontWeight = FontWeights.Black,
                Foreground = Brushes.Black
            });
            panel.Children.Add(input);
            return panel;
        }

        private Button BuildInventoryActionButton(string text, string backgroundHex)
        {
            return new Button
            {
                Content = text ?? string.Empty,
                Height = 34,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(12, 4, 12, 4),
                FontSize = 14,
                FontWeight = FontWeights.Black,
                Foreground = Brushes.White,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundHex)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundHex))
            };
        }

        private ComboBox BuildInventoryModeComboBox()
        {
            return new ComboBox
            {
                Height = 34,
                FontSize = 14,
                FontWeight = FontWeights.Black,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                FlowDirection = FlowDirection.RightToLeft
            };
        }

        private ComboBoxItem BuildComboOption(string value, string text)
        {
            return new ComboBoxItem
            {
                Tag = value,
                Content = text,
                FlowDirection = FlowDirection.RightToLeft,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                FontWeight = FontWeights.Bold
            };
        }

        private void SelectComboOption(ComboBox combo, string value)
        {
            if (combo == null) return;
            value = value ?? string.Empty;
            for (int i = 0; i < combo.Items.Count; i++)
            {
                var item = combo.Items[i] as ComboBoxItem;
                if (item == null) continue;
                var tag = (item.Tag ?? string.Empty).ToString();
                if (string.Equals(tag, value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        }

        private string GetSelectedComboValue(ComboBox combo, string fallback)
        {
            if (combo != null)
            {
                var item = combo.SelectedItem as ComboBoxItem;
                if (item != null)
                {
                    var tag = (item.Tag ?? string.Empty).ToString();
                    if (!string.IsNullOrWhiteSpace(tag)) return tag.Trim();
                }
            }
            return fallback ?? string.Empty;
        }

        private string InventoryDisplayUnitLabel(string displayUnit)
        {
            var value = (displayUnit ?? string.Empty).Trim().ToLowerInvariant();
            if (value == "kilogram") return "كجم";
            if (value == "gram") return "جم";
            if (value == "liter") return "لتر";
            if (value == "milliliter") return "مل";
            return "قطعة";
        }

        private string FormatInventoryNumber(double value)
        {
            if (Math.Abs(value % 1) < 0.0001) return ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture);
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}

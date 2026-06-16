#pragma warning disable 4014
using Fale7_POS.Infrastructure.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        private readonly List<DeliveryOrderRecord> _deliveryOrders = new List<DeliveryOrderRecord>();
        private readonly List<DeliveryOrderRecord> _deliveryShiftOrders = new List<DeliveryOrderRecord>();
        private readonly List<string> _deliveryNotesHistory = new List<string>();
        private string _deliveryCurrentOrderId;
        private string _deliverySelectedRowKey;
        private string _deliveryActiveCategory;
        private string _deliveryEditingNotesItemKey;
        private bool _deliveryInitialized;
        private int _deliveryOrderSequence;
        private bool _deliveryAppOrdersInitialized;
        private int _deliveryAppDeliveryOrderSequence;
        private int _deliveryAppPickupOrderSequence;
        private readonly List<AppDeliveryOrderRecord> _deliveryAppDeliveryOrders = new List<AppDeliveryOrderRecord>();
        private readonly List<AppPickupOrderRecord> _deliveryAppPickupOrders = new List<AppPickupOrderRecord>();
        private readonly HashSet<string> _deliveryAppDeliverySelectedIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _deliveryAppPickupSelectedIds = new HashSet<string>(StringComparer.Ordinal);
        private string _deliveryAppDriverAssignTargetId;
        private bool _deliveryAppDriverAssignInProgress;
        private readonly HashSet<string> _deliveryAppBusyOrderIds = new HashSet<string>(StringComparer.Ordinal);

        private readonly List<DeliveryDriverRecord> _deliveryDrivers = new List<DeliveryDriverRecord>();

        private void EnsureDeliveryInitialized()
        {
            if (_deliveryInitialized) return;
            EnsureDeliveryStateLoaded();
            EnsureDeliveryFormUiInitialized();
            EnsureDeliveryAppOrdersInitialized();
            QueueSupabaseDriversPullFromBackend(false);
            RebuildDeliveryShiftFilterCombo();
            if (string.IsNullOrEmpty(_deliveryActiveCategory) && _menu.Count > 0) _deliveryActiveCategory = _menu[0].Name;
            _deliveryInitialized = true;
        }

        private void ShowDeliveryView(string username)
        {
            LoginView.Visibility = Visibility.Collapsed;
            PlaceholderView.Visibility = Visibility.Collapsed;
            TakeawayView.Visibility = Visibility.Collapsed;
            if (DeliveryView != null) DeliveryView.Visibility = Visibility.Visible;

            if (DeliveryHeaderUserRun != null)
            {
                DeliveryHeaderUserRun.Text = string.IsNullOrWhiteSpace(username) ? "فاتح" : username;
            }

            EnsureDeliveryInitialized();
            NormalizeTakeawayState();
            UpdateRoleSwitchButtonsAndAdminUi();
            RenderDeliveryAll();
        }

        // ── PATCHED: رقم الأوردر يُسحب من السيرفر (Unified Branch Sequence) ──
        // الدالة القديمة أُبقي عليها كـ fallback للـ Offline فقط
        private int DeliveryNextOrderNoLocal()
        {
            _deliveryOrderSequence += 1;
            return _deliveryOrderSequence;
        }

        private int DeliveryNextOrderNo()
        {
            // نُسجِّل رقماً مؤقتاً سالباً إذا كنا Offline؛
            // يُستبدل بالرقم الحقيقي عند Sync
            return DeliveryNextOrderNoLocal();
        }

        // تُستخدم عند بناء الأوردر بالكامل (async context)
        private async Task<int> DeliveryNextOrderNoAsync()
        {
            var no = await GetNextBranchOrderNumberAsync().ConfigureAwait(false);
            if (no > 0)
            {
                // حدِّث العداد المحلي ليتماشى مع السيرفر
                if (no > _deliveryOrderSequence)
                    _deliveryOrderSequence = (int)no;
                return (int)no;
            }
            return DeliveryNextOrderNoLocal();
        }

        private DeliveryOrderRecord GetCurrentDeliveryOrder()
        {
            for (int i = 0; i < _deliveryOrders.Count; i++)
            {
                var o = _deliveryOrders[i];
                if (o != null && o.Id == _deliveryCurrentOrderId) return o;
            }
            return null;
        }

        private AppDeliveryOrderRecord FindAppDeliveryOrderById(string orderId)
        {
            var id = (orderId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < _deliveryAppDeliveryOrders.Count; i++)
            {
                var order = _deliveryAppDeliveryOrders[i];
                if (order == null || string.IsNullOrWhiteSpace(order.Id)) continue;
                if (string.Equals(order.Id.Trim(), id, StringComparison.OrdinalIgnoreCase))
                {
                    return order;
                }
            }
            return null;
        }

        private DeliveryOrderRecord CreateDeliveryOrder(string phone, string customerName, string district, int deliveryFee)
        {
            var order = new DeliveryOrderRecord
            {
                Id = NewId(),
                IdempotencyKey = LocalIdempotencyEngine.GenerateIdempotencyKey("DELV"),
                No = DeliveryNextOrderNo(),   // رقم مؤقت - يُحدَّث أسينكرون
                CreatedAt = NowUnixMs(),
                CustomerPhone = phone ?? string.Empty,
                CustomerName = string.IsNullOrWhiteSpace(customerName) ? "عميل" : customerName.Trim(),
                District = string.IsNullOrWhiteSpace(district) ? "-" : district.Trim(),
                Address = new DeliveryAddressRecord { District = string.IsNullOrWhiteSpace(district) ? "-" : district.Trim() },
                Status = "معلق",
                DeliveryFee = deliveryFee < 0 ? 0 : deliveryFee,
                DeliveryName = "خدمة التوصيل",
                CashierUserId = GetCurrentSessionUserId(),
                CashierUsername = GetCurrentSessionDisplayName(),
                CashierRole = GetCurrentBackendActorRole(),
                Items = new List<DeliveryLineRecord>()
            };

            _deliveryOrders.Insert(0, order);
            _deliveryCurrentOrderId = order.Id;
            PersistDeliveryState();

            // ── PATCH: سحب الرقم الحقيقي من السيرفر في الخلفية ──
            var capturedId = order.Id;
            _ = Task.Run(async () =>
            {
                var realNo = await DeliveryNextOrderNoAsync().ConfigureAwait(false);
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    var found = _deliveryOrders.Find(o => o != null && o.Id == capturedId);
                    if (found != null && found.No != realNo)
                    {
                        found.No = realNo;
                        PersistDeliveryState();
                        RenderDeliveryQueue();
                        RenderDeliveryReceipt();
                    }
                }));
            });

            // [PHASE 7.5] IMMEDIATE CLOUD INSERT — fire the order to Supabase the moment
            // it is created, with status RECEIVED so Terminal B sees it on its live board
            // instantly via the Realtime WebSocket INSERT event.
            var capturedOrder = order;
            Task.Run(async delegate
            {
                await PushNewDeliveryOrderToCloudAsync(capturedOrder).ConfigureAwait(false);
            });

            return order;
        }

        private DeliveryOrderRecord EnsureDeliveryOrder()
        {
            var order = GetCurrentDeliveryOrder();
            if (order == null)
            {
                order = CreateDeliveryOrder(string.Empty, string.Empty, "-", 0);
            }
            return order;
        }

        private int DeliveryMinutesSince(long createdAt)
        {
            var diff = DateTimeOffset.Now.ToUnixTimeMilliseconds() - createdAt;
            if (diff < 0) return 0;
            return (int)(diff / 60000L);
        }

        private void DeliveryBtnNewOrder_Click(object sender, RoutedEventArgs e)
        {
            SetDeliveryPickupContextActive(false);
            OpenDeliveryForm(false);
        }

        private void DeliveryBtnAddPickup_Click(object sender, RoutedEventArgs e)
        {
            OpenDeliveryPickupOrderForm();
        }

        private void DeliveryBtnDrivers_Click(object sender, RoutedEventArgs e)
        {
            var order = GetCurrentDeliveryOrder();
            if (order == null)
            {
                MessageBox.Show("حدد أوردر من قائمة الانتظار أولاً.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!order.KitchenPrinted)
            {
                MessageBox.Show("لا يمكن طباعة/تعيين الدليفري قبل طباعة فاتورة المطبخ لهذا الأوردر.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            OpenDeliveryDriversOverlay();
        }

        private void DeliveryBtnKitchenConfirm_Click(object sender, RoutedEventArgs e)
        {
            DeliveryKitchenPrintAndMove();
        }

        private void DeliveryBtnShift_Click(object sender, RoutedEventArgs e)
        {
            OpenDeliveryShiftOverlay();
        }

        private void DeliveryAppOrdersBoardButton_Click(object sender, RoutedEventArgs e)
        {
            OpenDeliveryAppOrdersBoardView();
        }

        private void TakeawayAppOrdersBoardButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSession == null) return;
            ShowDeliveryView(_currentSession.Username);
            OpenDeliveryAppOrdersBoardView();
        }

        private void RenderDeliveryAll()
        {
            RenderDeliveryAppOrderCounters();
            RenderDeliveryQueue();
            RenderDeliverySharedPickups();
            RenderDeliveryCategories();
            RenderDeliveryItems();
            RenderDeliveryReceipt();
            RenderDeliveryAppOrdersBoard();
        }

        private void RenderDeliverySharedPickups()
        {
            if (DeliveryPickupsListPanel == null) return;
            NormalizeTakeawayState();
            DeliveryPickupsListPanel.Children.Clear();

            if (_takeawayState != null && _takeawayState.Pickups != null && _takeawayState.Pickups.Count > 0)
            {
                DeliveryPickBadgeText.Text = _takeawayState.Pickups.Count.ToString();
                DeliveryPickBadgeBorder.Visibility = Visibility.Visible;
            }
            else
            {
                DeliveryPickBadgeBorder.Visibility = Visibility.Collapsed;
            }

            if (_takeawayState == null || _takeawayState.Pickups == null || _takeawayState.Pickups.Count == 0)
            {
                var empty = CreateCard("لا يوجد استلام", "—", false, false);
                empty.Cursor = Cursors.Arrow;
                DeliveryPickupsListPanel.Children.Add(empty);
                return;
            }

            for (int i = 0; i < _takeawayState.Pickups.Count; i++)
            {
                var p = _takeawayState.Pickups[i];
                if (p == null) continue;
                var pickupName = string.IsNullOrWhiteSpace(p.Name) ? "استلام" : p.Name.Trim();
                var pickupPhone = string.IsNullOrWhiteSpace(p.Phone) ? "—" : p.Phone.Trim();
                var card = CreateCard(pickupName, pickupPhone, _deliveryPickupContextActive && p.Id == _takeawayState.ActivePickupId, false);
                card.Tag = p.Id;
                var id = p.Id;
                card.MouseLeftButtonUp += delegate
                {
                    _takeawayState.ActivePickupId = id;
                    SetDeliveryPickupContextActive(true);
                    _deliveryPickupSelectedRowKey = null;
                    PersistTakeawayState();
                    RenderDeliverySharedPickups();
                    RenderDeliveryReceipt();
                    RestoreFocusToCardByTag(DeliveryPickupsListPanel, id);
                    if (TakeawayView != null && TakeawayView.Visibility == Visibility.Visible) { RenderPickups(); RenderReceipt(); }
                };
                AttachCardKeyboardHandlers(card, DeliveryPickupsListPanel, delegate
                {
                    _takeawayState.ActivePickupId = id;
                    SetDeliveryPickupContextActive(true);
                    _deliveryPickupSelectedRowKey = null;
                    PersistTakeawayState();
                    RenderDeliverySharedPickups();
                    RenderDeliveryReceipt();
                    RestoreFocusToCardByTag(DeliveryPickupsListPanel, id);
                    if (TakeawayView != null && TakeawayView.Visibility == Visibility.Visible) { RenderPickups(); RenderReceipt(); }
                });
                DeliveryPickupsListPanel.Children.Add(card);
            }
        }

        private void OpenDeliveryPickupOrderForm()
        {
            if (DeliveryPickupOrderOverlay == null) return;
            DeliveryPickupPhoneTextBox.Text = string.Empty;
            DeliveryPickupCustomerNameTextBox.Text = string.Empty;
            DeliveryPickupOrderOverlay.Visibility = Visibility.Visible;
            Dispatcher.BeginInvoke(new Action(delegate
            {
                DeliveryPickupPhoneTextBox.Focus();
                DeliveryPickupPhoneTextBox.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void CloseDeliveryPickupOrderForm()
        {
            if (DeliveryPickupOrderOverlay == null) return;
            DeliveryPickupOrderOverlay.Visibility = Visibility.Collapsed;
            DeliveryPickupPhoneTextBox.Text = string.Empty;
            DeliveryPickupCustomerNameTextBox.Text = string.Empty;
        }

        private void ConfirmDeliveryPickupOrderForm()
        {
            NormalizeTakeawayState();

            var phone = (DeliveryPickupPhoneTextBox.Text ?? string.Empty).Trim();
            var customerName = (DeliveryPickupCustomerNameTextBox.Text ?? string.Empty).Trim();
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
            _takeawayState.ActivePickupId = pickup.Id;
            SetDeliveryPickupContextActive(true);
            _deliveryPickupSelectedRowKey = null;

            var previousQueueOrderId = _takeawayState != null ? _takeawayState.CurrentOrderId : null;
            var takeawayOrder = CreateOrder();
            if (takeawayOrder != null)
            {
                LinkPickupToOrder(takeawayOrder, pickup.Id);
                if (_takeawayState != null) _takeawayState.CurrentOrderId = previousQueueOrderId;
            }

            PersistTakeawayState();

            CloseDeliveryPickupOrderForm();
            RefreshSharedPickupPanels();
            RenderDeliveryReceipt();
        }

        private void DeliveryPickupFormOkButton_Click(object sender, RoutedEventArgs e)
        {
            ConfirmDeliveryPickupOrderForm();
        }

        private void DeliveryPickupFormCancelButton_Click(object sender, RoutedEventArgs e)
        {
            CloseDeliveryPickupOrderForm();
        }

        private void DeliveryPickupOrderModalBorder_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DeliveryPickupOrderOverlay == null || DeliveryPickupOrderOverlay.Visibility != Visibility.Visible) return;
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CloseDeliveryPickupOrderForm();
                return;
            }
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                ConfirmDeliveryPickupOrderForm();
            }
        }

        private void DeliveryPickupKeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            OpenOnScreenKeyboard();
        }

        private void RenderDeliveryQueue()
        {
            if (DeliveryQueueRowsPanel == null) return;
            DeliveryQueueRowsPanel.Children.Clear();

            if (_deliveryOrders.Count == 0)
            {
                var empty = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDDDDDD")),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(4, 8, 4, 8)
                };
                var grid = new Grid { FlowDirection = FlowDirection.LeftToRight };
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.Children.Add(new TextBlock { Text = "—", FontSize = 11, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
                var t2 = new TextBlock { Text = "—", FontSize = 11, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center };
                var t3 = new TextBlock { Text = "-", FontSize = 11, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, FlowDirection = FlowDirection.RightToLeft };
                Grid.SetColumn(t2, 1);
                Grid.SetColumn(t3, 2);
                grid.Children.Add(t2);
                grid.Children.Add(t3);
                empty.Child = grid;
                DeliveryQueueRowsPanel.Children.Add(empty);
                return;
            }

            for (int i = 0; i < _deliveryOrders.Count; i++)
            {
                var order = _deliveryOrders[i];
                if (order == null) continue;

                var rowBorder = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDDDDDD")),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Cursor = Cursors.Hand,
                    Padding = new Thickness(4, 7, 4, 7),
                    Margin = new Thickness(0)
                };

                var rowGrid = new Grid { FlowDirection = FlowDirection.LeftToRight };
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });

                var noText = new TextBlock
                {
                    Text = order.No.ToString(),
                    FontSize = 11,
                    FontWeight = FontWeights.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FlowDirection = FlowDirection.LeftToRight
                };

                var sinceText = new TextBlock
                {
                    Text = DeliveryMinutesSince(order.CreatedAt).ToString() + " د",
                    FontSize = 11,
                    FontWeight = FontWeights.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FlowDirection = FlowDirection.LeftToRight
                };

                var districtText = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(order.District) ? "-" : order.District,
                    FontSize = 11,
                    FontWeight = FontWeights.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FlowDirection = FlowDirection.RightToLeft,
                    TextWrapping = TextWrapping.Wrap
                };

                Grid.SetColumn(sinceText, 1);
                Grid.SetColumn(districtText, 2);
                rowGrid.Children.Add(noText);
                rowGrid.Children.Add(sinceText);
                rowGrid.Children.Add(districtText);
                rowBorder.Child = rowGrid;
                rowBorder.Background = GetDeliveryQueueDelayBackground(order);

                if (!_deliveryPickupContextActive && order.Id == _deliveryCurrentOrderId)
                {
                    rowBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#99000000"));
                    rowBorder.BorderThickness = new Thickness(2);
                    rowBorder.Margin = new Thickness(0, 0, 0, 1);
                }

                var selectedId = order.Id;
                rowBorder.Tag = selectedId;
                rowBorder.GotKeyboardFocus += delegate
                {
                    if (!_deliveryPickupContextActive && selectedId == _deliveryCurrentOrderId) return;
                    rowBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3A7BD5"));
                    rowBorder.BorderThickness = new Thickness(1);
                };
                rowBorder.LostKeyboardFocus += delegate
                {
                    if (!_deliveryPickupContextActive && selectedId == _deliveryCurrentOrderId) return;
                    rowBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDDDDDD"));
                    rowBorder.BorderThickness = new Thickness(0, 0, 0, 1);
                };
                rowBorder.MouseLeftButtonUp += delegate
                {
                    SetDeliveryPickupContextActive(false);
                    _deliveryCurrentOrderId = selectedId;
                    _deliverySelectedRowKey = null;
                    RenderDeliveryQueue();
                    RenderDeliveryReceipt();
                    RestoreFocusToCardByTag(DeliveryQueueRowsPanel, selectedId);
                };
                rowBorder.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    if (e.ClickCount >= 2)
                    {
                        e.Handled = true;
                        SetDeliveryPickupContextActive(false);
                        _deliveryCurrentOrderId = selectedId;
                        _deliverySelectedRowKey = null;
                        OpenDeliveryForm(true);
                    }
                };
                AttachCardKeyboardHandlers(rowBorder, DeliveryQueueRowsPanel, delegate
                {
                    SetDeliveryPickupContextActive(false);
                    _deliveryCurrentOrderId = selectedId;
                    _deliverySelectedRowKey = null;
                    RenderDeliveryQueue();
                    RenderDeliveryReceipt();
                    RestoreFocusToCardByTag(DeliveryQueueRowsPanel, selectedId);
                });

                DeliveryQueueRowsPanel.Children.Add(rowBorder);
            }
        }

        private void RenderDeliveryCategories()
        {
            if (DeliveryCategoriesGridPanel == null) return;
            DeliveryCategoriesGridPanel.Children.Clear();

            if (string.IsNullOrEmpty(_deliveryActiveCategory) && _menu.Count > 0) _deliveryActiveCategory = _menu[0].Name;

            for (int i = 0; i < _menu.Count; i++)
            {
                var cat = _menu[i];
                if (cat == null) continue;
                var btn = CreateTileButton(cat.Name, cat.Name == _deliveryActiveCategory, 46, 11);
                btn.Tag = cat;
                var name = cat.Name;
                btn.Click += delegate
                {
                    _deliveryActiveCategory = name;
                    PersistDeliveryState();
                    RenderDeliveryCategories();
                    RenderDeliveryItems();
                };
                AttachAdminCategoryReorderHandler(btn, true, cat);
                DeliveryCategoriesGridPanel.Children.Add(btn);
            }
        }

        private async void DeliveryItemTile_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var def = btn != null ? btn.Tag as MenuItemDef : null;
            if (def != null && IsMenuItemOutOfStock(def))
            {
                MessageBox.Show("هذا المنتج غير متاح الآن بسبب المخزون.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (def == null || def.Name == "—") return;
            if (!_deliveryPickupContextActive && !EnsureDeliveryOrderSelectedOrOpenForm()) return;

            var oldBg = btn.Background;
            var oldFg = btn.Foreground;
            var oldBorder = btn.BorderBrush;
            btn.Background = Brushes.White;
            btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2F6FAB"));
            btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2F6FAB"));

            DeliveryAddItemToCurrentOrder(def);
            await Task.Delay(140);

            if (btn.IsLoaded)
            {
                btn.Background = oldBg;
                btn.Foreground = oldFg;
                btn.BorderBrush = oldBorder;
            }
        }

        private void RenderDeliveryItems()
        {
            if (DeliveryItemsGridPanel == null) return;
            DeliveryItemsGridPanel.Children.Clear();
            DeliveryItemsGridPanel.FlowDirection = FlowDirection.RightToLeft;
            DeliveryItemsGridPanel.Columns = 4;

            MenuCategory cat = null;
            for (int i = 0; i < _menu.Count; i++)
            {
                if (_menu[i] != null && _menu[i].Name == _deliveryActiveCategory)
                {
                    cat = _menu[i];
                    break;
                }
            }
            if (cat == null && _menu.Count > 0) cat = _menu[0];
            if (cat == null || cat.Items == null) return;
            BuildLocalInventoryReservationCounters(out var productReservations, out var optionReservations);

            for (int i = 0; i < cat.Items.Count; i++)
            {
                var it = cat.Items[i];
                if (it == null || it.Name == "—") continue;
                var statusText = GetMenuItemInventoryBadgeText(it, productReservations, optionReservations);
                var btn = CreateTileButton(string.IsNullOrWhiteSpace(statusText) ? it.Name : (it.Name + "\n" + statusText), false, 58, 12);
                btn.Tag = it;
                ApplyInventoryTileVisualState(btn, it, productReservations, optionReservations);
                btn.Click += DeliveryItemTile_Click;
                AttachAdminItemReorderHandler(btn, true, cat, it);
                DeliveryItemsGridPanel.Children.Add(btn);
            }
        }

        private void DeliveryAddItemToCurrentOrder(MenuItemDef def)
        {
            if (def == null) return;
            if (IsMenuItemOutOfStock(def)) return;
            var desc = def.Name;
            var price = ResolvePosTileFinalPrice(def);
            if (_deliveryPickupContextActive)
            {
                var pickupOrder = EnsureActiveDeliveryPickupOrder();
                if (pickupOrder == null) return;

                TakeawayLineRecord existingPickup = null;
                for (int i = 0; i < pickupOrder.Items.Count; i++)
                {
                    var existingOptionIds = pickupOrder.Items[i].OptionIds ?? new List<string>();
                    var targetOptionIds = def.FixedOptionIds ?? new List<string>();
                    var sameOptions = string.Equals(string.Join("|", existingOptionIds), string.Join("|", targetOptionIds), StringComparison.Ordinal);
                    if (pickupOrder.Items[i].Desc == desc
                        && string.Equals((pickupOrder.Items[i].ProductId ?? string.Empty), (def.ProductId ?? string.Empty), StringComparison.Ordinal)
                        && sameOptions)
                    {
                        existingPickup = pickupOrder.Items[i];
                        break;
                    }
                }

                if (existingPickup != null) existingPickup.Qty += 1;
                else pickupOrder.Items.Add(new TakeawayLineRecord
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
                RenderDeliveryReceipt();
                if (TakeawayView != null && TakeawayView.Visibility == Visibility.Visible) RenderReceipt();
                RefreshMenuInventoryTiles();
                return;
            }

            var order = GetCurrentDeliveryOrder();
            if (order == null) return;
            DeliveryLineRecord existing = null;
            for (int i = 0; i < order.Items.Count; i++)
            {
                var existingOptionIds = order.Items[i].OptionIds ?? new List<string>();
                var targetOptionIds = def.FixedOptionIds ?? new List<string>();
                var sameOptions = string.Equals(string.Join("|", existingOptionIds), string.Join("|", targetOptionIds), StringComparison.Ordinal);
                if (order.Items[i].Desc == desc
                    && string.Equals((order.Items[i].ProductId ?? string.Empty), (def.ProductId ?? string.Empty), StringComparison.Ordinal)
                    && sameOptions)
                {
                    existing = order.Items[i];
                    break;
                }
            }

            if (existing != null) existing.Qty += 1;
            else order.Items.Add(new DeliveryLineRecord
            {
                Key = NewId(),
                Desc = desc,
                Qty = 1,
                Price = price,
                Notes = string.Empty,
                ProductId = def.ProductId,
                SourceProductId = def.SourceProductId,
                OptionIds = def.FixedOptionIds == null ? new List<string>() : new List<string>(def.FixedOptionIds)
            });

            DeliveryRecalcOrder(order);
            PersistDeliveryState();
            RenderDeliveryReceipt();
            RenderDeliveryQueue();
            RefreshMenuInventoryTiles();
        }

        private void DeliveryRemoveItemFromCurrentOrder(string itemKey)
        {
            if (_deliveryPickupContextActive)
            {
                var pickupOrder = GetActiveDeliveryPickupOrder();
                if (pickupOrder == null) return;

                for (int i = pickupOrder.Items.Count - 1; i >= 0; i--)
                {
                    if (pickupOrder.Items[i].Key == itemKey) pickupOrder.Items.RemoveAt(i);
                }

                if (_deliveryPickupSelectedRowKey == itemKey) _deliveryPickupSelectedRowKey = null;
                PersistTakeawayState();
                RenderDeliveryReceipt();
                if (TakeawayView != null && TakeawayView.Visibility == Visibility.Visible) RenderReceipt();
                RefreshMenuInventoryTiles();
                return;
            }

            var order = GetCurrentDeliveryOrder();
            if (order == null) return;

            for (int i = order.Items.Count - 1; i >= 0; i--)
            {
                if (order.Items[i].Key == itemKey) order.Items.RemoveAt(i);
            }

            if (_deliverySelectedRowKey == itemKey) _deliverySelectedRowKey = null;
            if (_deliveryEditingNotesItemKey == itemKey) _deliveryEditingNotesItemKey = null;
            DeliveryRecalcOrder(order);
            PersistDeliveryState();
            RenderDeliveryReceipt();
            RefreshMenuInventoryTiles();
        }

        private void DeliveryRecalcOrder(DeliveryOrderRecord order)
        {
            if (order == null) return;
            var subtotal = 0;
            for (int i = 0; i < order.Items.Count; i++)
            {
                var it = order.Items[i];
                subtotal += (it.Qty * it.Price);
            }
            order.Subtotal = subtotal;
            order.Total = subtotal + (order.DeliveryFee < 0 ? 0 : order.DeliveryFee);
        }

        private Border CreateDeliveryReceiptCell(string text, bool isDesc, bool isNotes, bool selected)
        {
            var bg = selected ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD7EBFF")) : Brushes.White;
            var cell = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8F8F8F")),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Background = bg,
                Padding = (isDesc || isNotes) ? new Thickness(6, 6, 8, 6) : new Thickness(6)
            };

            cell.Child = new TextBlock
            {
                Text = text ?? string.Empty,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = (isDesc || isNotes) ? TextAlignment.Right : TextAlignment.Center,
                FlowDirection = (isDesc || isNotes) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                VerticalAlignment = VerticalAlignment.Top
            };

            return cell;
        }

        private void RenderDeliveryPickupReceipt()
        {
            if (DeliveryReceiptRowsPanel == null) return;
            DeliveryReceiptRowsPanel.Children.Clear();

            var order = GetActiveDeliveryPickupOrder();
            if (order == null)
            {
                DeliverySubtotalValText.Text = "0";
                DeliveryFeeValText.Text = "0";
                DeliveryGrandValText.Text = "0";
                DeliveryFeeLabelText.Text = "استلام من الفرع";
                return;
            }

            var subtotal = 0;
            for (int i = 0; i < order.Items.Count; i++) subtotal += order.Items[i].Qty * order.Items[i].Price;
            DeliveryFeeLabelText.Text = "استلام من الفرع";
            DeliverySubtotalValText.Text = subtotal.ToString();
            DeliveryFeeValText.Text = "0";
            DeliveryGrandValText.Text = subtotal.ToString();

            for (int i = 0; i < order.Items.Count; i++)
            {
                var it = order.Items[i];
                var selected = it.Key == _deliveryPickupSelectedRowKey;
                var row = new Grid { Tag = it.Key, FlowDirection = FlowDirection.LeftToRight };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Star) });

                var descCell = CreateDeliveryReceiptCell(it.Desc, true, false, selected);
                var notesCell = CreateDeliveryReceiptCell(string.Empty, false, true, selected);
                var qtyCell = CreateDeliveryReceiptCell(it.Qty.ToString(), false, false, selected);
                var priceCell = CreateDeliveryReceiptCell(it.Price.ToString(), false, false, selected);
                var totalCell = CreateDeliveryReceiptCell((it.Qty * it.Price).ToString(), false, false, selected);

                Grid.SetColumn(totalCell, 0);
                Grid.SetColumn(priceCell, 1);
                Grid.SetColumn(qtyCell, 2);
                Grid.SetColumn(notesCell, 3);
                Grid.SetColumn(descCell, 4);

                row.Children.Add(descCell);
                row.Children.Add(notesCell);
                row.Children.Add(qtyCell);
                row.Children.Add(priceCell);
                row.Children.Add(totalCell);

                var itemKey = it.Key;
                row.MouseLeftButtonUp += delegate
                {
                    _deliveryPickupSelectedRowKey = itemKey;
                    RenderDeliveryReceipt();
                };

                descCell.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    if (e.ClickCount >= 2)
                    {
                        e.Handled = true;
                        DeliveryRemoveItemFromCurrentOrder(itemKey);
                    }
                };

                DeliveryReceiptRowsPanel.Children.Add(row);
            }
        }

        private void RenderDeliveryReceipt()
        {
            if (DeliveryReceiptRowsPanel == null) return;
            DeliveryReceiptRowsPanel.Children.Clear();

            if (_deliveryPickupContextActive)
            {
                RenderDeliveryPickupReceipt();
                return;
            }

            var order = GetCurrentDeliveryOrder();
            if (order == null)
            {
                DeliverySubtotalValText.Text = "0";
                DeliveryFeeValText.Text = "0";
                DeliveryGrandValText.Text = "0";
                DeliveryFeeLabelText.Text = "خدمة التوصيل";
                return;
            }

            DeliveryRecalcOrder(order);
            DeliveryFeeLabelText.Text = string.IsNullOrWhiteSpace(order.DeliveryName) ? "خدمة التوصيل" : order.DeliveryName;
            DeliverySubtotalValText.Text = order.Subtotal.ToString();
            DeliveryFeeValText.Text = order.DeliveryFee.ToString();
            DeliveryGrandValText.Text = order.Total.ToString();

            for (int i = 0; i < order.Items.Count; i++)
            {
                var it = order.Items[i];
                var selected = it.Key == _deliverySelectedRowKey;
                var row = new Grid { Tag = it.Key, FlowDirection = FlowDirection.LeftToRight };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Star) });

                var descCell = CreateDeliveryReceiptCell(it.Desc, true, false, selected);
                var notesCell = CreateDeliveryReceiptCell(it.Notes, false, true, selected);
                var qtyCell = CreateDeliveryReceiptCell(it.Qty.ToString(), false, false, selected);
                var priceCell = CreateDeliveryReceiptCell(it.Price.ToString(), false, false, selected);
                var totalCell = CreateDeliveryReceiptCell((it.Qty * it.Price).ToString(), false, false, selected);

                Grid.SetColumn(totalCell, 0);
                Grid.SetColumn(priceCell, 1);
                Grid.SetColumn(qtyCell, 2);
                Grid.SetColumn(notesCell, 3);
                Grid.SetColumn(descCell, 4);

                row.Children.Add(descCell);
                row.Children.Add(notesCell);
                row.Children.Add(qtyCell);
                row.Children.Add(priceCell);
                row.Children.Add(totalCell);

                var itemKey = it.Key;
                row.MouseLeftButtonUp += delegate
                {
                    _deliverySelectedRowKey = itemKey;
                    RenderDeliveryReceipt();
                };

                descCell.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    if (e.ClickCount >= 2)
                    {
                        e.Handled = true;
                        DeliveryRemoveItemFromCurrentOrder(itemKey);
                    }
                };

                notesCell.Cursor = Cursors.Hand;
                notesCell.ToolTip = "اضغط لكتابة ملاحظات";
                notesCell.MouseLeftButtonUp += delegate
                {
                    OpenDeliveryNotesOverlay(itemKey);
                };

                DeliveryReceiptRowsPanel.Children.Add(row);
            }
        }

        private DeliveryLineRecord GetDeliveryLineByKey(string itemKey)
        {
            var order = GetCurrentDeliveryOrder();
            if (order == null || string.IsNullOrEmpty(itemKey)) return null;
            for (int i = 0; i < order.Items.Count; i++)
            {
                if (order.Items[i].Key == itemKey) return order.Items[i];
            }
            return null;
        }

        private void OpenDeliveryNotesOverlay(string itemKey)
        {
            var line = GetDeliveryLineByKey(itemKey);
            if (line == null || DeliveryNotesOverlay == null) return;

            _deliveryEditingNotesItemKey = itemKey;
            _deliveryNotesSelIndex = -1;
            _deliveryNotesFiltered.Clear();
            DeliveryNotesInputTextBox.Text = line.Notes ?? string.Empty;
            DeliveryNotesOverlay.Visibility = Visibility.Visible;
            RefreshDeliveryNotesSuggestions();

            Dispatcher.BeginInvoke(new Action(delegate
            {
                DeliveryNotesInputTextBox.Focus();
                DeliveryNotesInputTextBox.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void CloseDeliveryNotesOverlay()
        {
            if (DeliveryNotesOverlay == null) return;
            DeliveryNotesOverlay.Visibility = Visibility.Collapsed;
            _deliveryEditingNotesItemKey = null;
            if (DeliveryNotesInputTextBox != null) DeliveryNotesInputTextBox.Text = string.Empty;
            if (DeliveryNotesSuggestPanel != null) DeliveryNotesSuggestPanel.Children.Clear();
        }

        private void RefreshDeliveryNotesSuggestions()
        {
            if (DeliveryNotesSuggestPanel == null || DeliveryNotesInputTextBox == null) return;

            var q = (DeliveryNotesInputTextBox.Text ?? string.Empty).Trim();
            DeliveryNotesSuggestPanel.Children.Clear();
            _deliveryNotesFiltered.Clear();

            for (int i = 0; i < _deliveryNotesHistory.Count; i++)
            {
                var text = _deliveryNotesHistory[i];
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (!string.IsNullOrEmpty(q) && text.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                var exists = false;
                for (int j = 0; j < _deliveryNotesFiltered.Count; j++)
                {
                    if (string.Equals(_deliveryNotesFiltered[j], text, StringComparison.Ordinal))
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists) continue;
                _deliveryNotesFiltered.Add(text);
                if (_deliveryNotesFiltered.Count >= 20) break;
            }

            if (_deliveryNotesFiltered.Count == 0)
            {
                _deliveryNotesSelIndex = -1;
                DeliveryNotesSuggestPanel.Children.Add(new Border
                {
                    Background = Brushes.White,
                    Padding = new Thickness(8),
                    Child = new TextBlock
                    {
                        Text = "لا توجد اقتراحات",
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF555555")),
                        FlowDirection = FlowDirection.RightToLeft
                    }
                });
                return;
            }

            if (_deliveryNotesSelIndex < 0) _deliveryNotesSelIndex = 0;
            if (_deliveryNotesSelIndex >= _deliveryNotesFiltered.Count) _deliveryNotesSelIndex = _deliveryNotesFiltered.Count - 1;

            for (int i = 0; i < _deliveryNotesFiltered.Count; i++)
            {
                var text = _deliveryNotesFiltered[i];
                var itemBorder = new Border
                {
                    Background = i == _deliveryNotesSelIndex
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD7EBFF"))
                        : Brushes.Transparent,
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDDDDDD")),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(8),
                    Cursor = Cursors.Hand
                };
                if (i == _deliveryNotesSelIndex)
                {
                    itemBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3A7BD5"));
                    itemBorder.BorderThickness = new Thickness(1);
                }
                itemBorder.Child = new TextBlock
                {
                    Text = text,
                    FontWeight = FontWeights.Bold,
                    TextWrapping = TextWrapping.Wrap,
                    FlowDirection = FlowDirection.RightToLeft
                };
                var captured = text;
                var capturedIndex = i;
                itemBorder.MouseLeftButtonUp += delegate
                {
                    _deliveryNotesSelIndex = capturedIndex;
                    DeliveryNotesInputTextBox.Text = captured;
                    SaveDeliveryNotesOverlay();
                };
                DeliveryNotesSuggestPanel.Children.Add(itemBorder);
            }
        }

        private void SaveDeliveryNotesOverlay()
        {
            var line = GetDeliveryLineByKey(_deliveryEditingNotesItemKey);
            if (line == null)
            {
                CloseDeliveryNotesOverlay();
                return;
            }

            var text = (DeliveryNotesInputTextBox.Text ?? string.Empty).Trim();
            if (_deliveryNotesSelIndex >= 0 && _deliveryNotesSelIndex < _deliveryNotesFiltered.Count)
            {
                var selectedSuggestion = _deliveryNotesFiltered[_deliveryNotesSelIndex];
                if (!string.IsNullOrWhiteSpace(selectedSuggestion) && (string.IsNullOrWhiteSpace(text) || string.Equals(text, selectedSuggestion, StringComparison.Ordinal)))
                {
                    text = selectedSuggestion;
                }
            }
            line.Notes = text;

            if (!string.IsNullOrWhiteSpace(text))
            {
                for (int i = _deliveryNotesHistory.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(_deliveryNotesHistory[i], text, StringComparison.Ordinal))
                    {
                        _deliveryNotesHistory.RemoveAt(i);
                    }
                }
                _deliveryNotesHistory.Insert(0, text);
                while (_deliveryNotesHistory.Count > 60) _deliveryNotesHistory.RemoveAt(_deliveryNotesHistory.Count - 1);
            }

            CloseDeliveryNotesOverlay();
            PersistDeliveryState();
            RenderDeliveryReceipt();
        }

        private void DeliveryNotesOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ReferenceEquals(e.OriginalSource, DeliveryNotesOverlay)) CloseDeliveryNotesOverlay();
        }

        private void DeliveryNotesModalBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Prevent overlay click-close when clicking inside the modal.
        }

        private void DeliveryNotesCloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseDeliveryNotesOverlay();
        }

        private void DeliveryNotesCancelButton_Click(object sender, RoutedEventArgs e)
        {
            CloseDeliveryNotesOverlay();
        }

        private void DeliveryNotesSaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveDeliveryNotesOverlay();
        }

        private void DeliveryNotesInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CloseDeliveryNotesOverlay();
                return;
            }

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                SaveDeliveryNotesOverlay();
                return;
            }

            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                e.Handled = true;
                if (_deliveryNotesFiltered.Count == 0) return;
                if (e.Key == Key.Down) _deliveryNotesSelIndex = Math.Min(_deliveryNotesSelIndex + 1, _deliveryNotesFiltered.Count - 1);
                else _deliveryNotesSelIndex = Math.Max(_deliveryNotesSelIndex - 1, 0);
                RefreshDeliveryNotesSuggestions();
            }
        }

        private void DeliveryNotesInputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DeliveryNotesOverlay != null && DeliveryNotesOverlay.Visibility == Visibility.Visible)
            {
                RefreshDeliveryNotesSuggestions();
            }
        }

        private bool HandleDeliveryNotesOverlayKey(KeyEventArgs e)
        {
            if (DeliveryNotesOverlay == null || DeliveryNotesOverlay.Visibility != Visibility.Visible) return false;
            if (e.Key == Key.Escape)
            {
                CloseDeliveryNotesOverlay();
                return true;
            }
            if (e.Key == Key.Enter)
            {
                SaveDeliveryNotesOverlay();
                return true;
            }
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                if (_deliveryNotesFiltered.Count == 0) return true;
                if (e.Key == Key.Down) _deliveryNotesSelIndex = Math.Min(_deliveryNotesSelIndex + 1, _deliveryNotesFiltered.Count - 1);
                else _deliveryNotesSelIndex = Math.Max(_deliveryNotesSelIndex - 1, 0);
                RefreshDeliveryNotesSuggestions();
                return true;
            }
            return false;
        }

        private void DeliveryKitchenPrintAndMove()
        {
            if (_deliveryPickupContextActive)
            {
                PrintAndCompleteActivePickupOrder();
                return;
            }
            var order = GetCurrentDeliveryOrder();
            if (order == null)
            {
                MessageBox.Show("حدد أوردر من قائمة الانتظار أولاً.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            // ── PATCH: التحقق من السيرفر أولاً (Atomic kitchen_print_count) ──
            _ = DeliveryKitchenPrintAndMoveAsync(order);
        }

        private async Task DeliveryKitchenPrintAndMoveAsync(DeliveryOrderRecord order)
        {
            if (order == null) return;

            // بناء بيانات الطباعة
            var printData = BuildDeliveryOrderPrintData(order);
            printData.BackendOrderId = order.Id;

            // استدعاء RPC الذري - يمنع التكرار بين جهازين
            var printed = await PrintKitchenTicketWithServerCheckAsync(
                printData,
                adminAllowsReprint: _isAdminMode
            ).ConfigureAwait(false);

            if (!printed) return;

            Dispatcher.Invoke(delegate
            {
                order.KitchenPrinted   = true;
                order.KitchenPrintedAt = NowUnixMs();
                order.Status           = "قيد التجهيز";
                PersistDeliveryState();
                RenderDeliveryReceipt();
                RenderDeliveryQueue();
            });
        }

        private void OpenDeliveryDriversOverlay()
        {
            if (DeliveryDriversOverlay == null || DeliveryDriversListPanel == null) return;
            DeliveryDriversOverlay.Visibility = Visibility.Visible;
            RenderDeliveryDriversOverlayList();
            // Refresh async in background without blocking UI.
            QueueSupabaseDriversPullFromBackend(false);
        }

        private void RenderDeliveryDriversOverlayList()
        {
            if (DeliveryDriversListPanel == null) return;
            DeliveryDriversListPanel.Children.Clear();

            if (_deliveryDrivers.Count == 0)
            {
                var fallback = BuildFallbackDeliveryDriversFromLocalOrders();
                if (fallback.Count > 0)
                {
                    _deliveryDrivers.Clear();
                    for (int i = 0; i < fallback.Count; i++) _deliveryDrivers.Add(fallback[i]);
                    RebuildDeliveryShiftFilterCombo();
                    PersistDeliveryState();
                }
            }

            if (_deliveryDrivers.Count == 0)
            {
                DeliveryDriversListPanel.Children.Add(new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF999999")),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7F7F7")),
                    Padding = new Thickness(10),
                    Child = new TextBlock
                    {
                        Text = "لا يوجد سائقون مفعّلون على السيرفر.",
                        FontWeight = FontWeights.Bold,
                        TextWrapping = TextWrapping.Wrap
                    }
                });
                return;
            }

            for (int i = 0; i < _deliveryDrivers.Count; i++)
            {
                var d = _deliveryDrivers[i];
                var card = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF999999")),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7F7F7")),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 10),
                    Cursor = Cursors.Hand
                };
                card.Child = new TextBlock
                {
                    Text = d.Name + " — " + d.Phone,
                    FontWeight = FontWeights.Bold,
                    FlowDirection = FlowDirection.RightToLeft,
                    TextWrapping = TextWrapping.Wrap
                };

                var captured = d;
                card.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
                {
                    if (e.ClickCount >= 2)
                    {
                        e.Handled = true;
                        HandleDeliveryDriverOverlaySelection(captured);
                    }
                };

                DeliveryDriversListPanel.Children.Add(card);
            }
        }

        private void OpenDeliveryDriversOverlayForAppOrder(AppDeliveryOrderRecord order)
        {
            if (order == null) return;
            _deliveryAppDriverAssignTargetId = (order.Id ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(_deliveryAppDriverAssignTargetId)) return;
            OpenDeliveryDriversOverlay();
        }

        private void HandleDeliveryDriverOverlaySelection(DeliveryDriverRecord driver)
        {
            if (driver == null) return;
            if (_deliveryAppDriverAssignInProgress) return;

            if (!string.IsNullOrWhiteSpace(_deliveryAppDriverAssignTargetId))
            {
                var target = FindAppDeliveryOrderById(_deliveryAppDriverAssignTargetId);
                if (target == null)
                {
                    CloseDeliveryDriversOverlay();
                    return;
                }
                DeliveryAssignDriverAndPrintForAppOrder(target, driver);
                return;
            }
            DeliveryAssignDriverAndPrint(driver);
        }

        private void CloseDeliveryDriversOverlay()
        {
            if (DeliveryDriversOverlay != null) DeliveryDriversOverlay.Visibility = Visibility.Collapsed;
            _deliveryAppDriverAssignTargetId = null;
        }

        private void DeliveryAssignDriverAndPrint(DeliveryDriverRecord driver)
        {
            var order = GetCurrentDeliveryOrder();
            if (order == null || driver == null) return;

            if (!order.KitchenPrinted)
            {
                MessageBox.Show("لا يمكن طباعة فاتورة الدليفري قبل طباعة فاتورة المطبخ.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var pendingOrder = CloneDeliveryOrder(order);
            pendingOrder.Driver = driver;
            pendingOrder.Status = "Ù…Ø¹ Ø§Ù„Ø³Ø§Ø¦Ù‚";
            pendingOrder.ShiftTimeText = DateTime.Now.ToString("g", _arEg);
            pendingOrder.CashierUserId = GetCurrentSessionUserId();
            pendingOrder.CashierUsername = GetCurrentSessionDisplayName();
            pendingOrder.CashierRole = GetCurrentBackendActorRole();

            var backendRow = BuildPosOfflineDeliveryOrderInsertRow(pendingOrder);
            // ── PATCH: ربط الأوردر بالوردية المشتركة ──
            AttachShiftIdToOrderPayload(backendRow);
            bool pushedToServer;
            string syncMessage;
            if (!TryCreatePosOrderOnServerOrFallback(backendRow, out pushedToServer, out syncMessage))
            {
                MessageBox.Show(string.IsNullOrWhiteSpace(syncMessage) ? "تعذر اعتماد الطلب الآن." : syncMessage, "الدليفري", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryPrintFlowDocument(BuildDeliveryDriverDocument(order, driver)))
            {
                if (pushedToServer && backendRow != null) TryReleaseServerReservedPosOrder(backendRow.Id);
                return;
            }

            order.Driver = driver;
            order.Status = "مع السائق";
            order.ShiftTimeText = DateTime.Now.ToString("g", _arEg);
            order.CashierUserId = GetCurrentSessionUserId();
            order.CashierUsername = GetCurrentSessionDisplayName();
            order.CashierRole = GetCurrentBackendActorRole();
            // [PURE CLOUD] Push DONE order directly — no offline queue fallback
            if (!pushedToServer)
            {
                try
                {
                    PushCompletedDeliveryOrderToCloudAsync(order).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(BuildPosOrderServerFailureUserMessage(ex.Message), "الدليفري", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // Order remains in memory queue; cashier can retry via admin
                }
            }
            TrackCompletedManualDeliveryShiftOrder(order);

            _deliveryShiftOrders.Insert(0, CloneDeliveryOrder(order));
            for (int i = _deliveryOrders.Count - 1; i >= 0; i--)
            {
                if (_deliveryOrders[i].Id == order.Id) _deliveryOrders.RemoveAt(i);
            }
            _deliveryCurrentOrderId = _deliveryOrders.Count > 0 ? _deliveryOrders[0].Id : null;
            _deliverySelectedRowKey = null;

            PersistDeliveryState();
            CloseDeliveryDriversOverlay();
            RenderDeliveryQueue();
            RenderDeliveryReceipt();
            RenderDeliveryShiftRows();
        }

        private void DeliveryAssignDriverAndPrintForAppOrder(AppDeliveryOrderRecord order, DeliveryDriverRecord driver)
        {
            if (order == null || driver == null) return;
            var currentOrder = FindAppDeliveryOrderById(order.Id) ?? order;
            if (!HasSupabaseAppOrdersBackendConfigured())
            {
                MessageBox.Show("ربط طلبات التطبيق مع الباك اند مطلوب.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var kitchenWasPrintedOrOrderMoved =
                currentOrder.KitchenPrinted ||
                currentOrder.Status == AppDeliveryOrderStatus.Preparing ||
                currentOrder.Status == AppDeliveryOrderStatus.DriverAssigned ||
                currentOrder.Status == AppDeliveryOrderStatus.Done;
            if (!kitchenWasPrintedOrOrderMoved)
            {
                MessageBox.Show("لا يمكن تعيين سائق قبل طباعة فاتورة المطبخ لطلب التطبيق.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (currentOrder.DriverReceiptPrinted)
            {
                MessageBox.Show("تمت طباعة فاتورة السائق لهذا الطلب من قبل.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (_deliveryAppDriverAssignInProgress) return;
            if (!TryBeginAppOrderBusyState(currentOrder.Id)) return;

            _deliveryAppDriverAssignInProgress = true;
            try
            {
                var previousDriver = currentOrder.Driver == null
                    ? null
                    : new DeliveryDriverRecord
                    {
                        Id = currentOrder.Driver.Id,
                        Name = currentOrder.Driver.Name,
                        Phone = currentOrder.Driver.Phone,
                        AppUserId = currentOrder.Driver.AppUserId
                    };
                var previousDriverReceiptPrinted = currentOrder.DriverReceiptPrinted;
                var previousStatus = currentOrder.Status;

                if (!TryPrintFlowDocument(BuildAppDeliveryDriverReceiptDocument(currentOrder, driver)))
                {
                    EndAppOrderBusyState(currentOrder.Id);
                    return;
                }

                currentOrder.Driver = new DeliveryDriverRecord { Id = driver.Id, Name = driver.Name, Phone = driver.Phone, AppUserId = driver.AppUserId };
                currentOrder.DriverReceiptPrinted = true;
                currentOrder.Status = AppDeliveryOrderStatus.DriverAssigned;
                QueueSupabaseAppOrdersUiRefresh();
                CloseDeliveryDriversOverlay();
                PersistDeliveryState();

                if (!QueueSupabaseBackendAssignDriver(currentOrder, currentOrder.Driver))
                {
                    currentOrder.Driver = previousDriver;
                    currentOrder.DriverReceiptPrinted = previousDriverReceiptPrinted;
                    currentOrder.Status = previousStatus;
                    QueueSupabaseAppOrdersUiRefresh();
                    PersistDeliveryState();
                    EndAppOrderBusyState(currentOrder.Id);
                    MessageBox.Show(GetAppOrderBackendFailureMessage("تعذر تعيين السائق على الباك اند. تأكد أن السائق موجود في جدول users وله role=DRIVER."), "الدليفري", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            finally
            {
                _deliveryAppDriverAssignInProgress = false;
            }
        }

        private DeliveryOrderRecord CloneDeliveryOrder(DeliveryOrderRecord source)
        {
            var copy = new DeliveryOrderRecord
            {
                Id = source.Id,
                No = source.No,
                CreatedAt = source.CreatedAt,
                CustomerPhone = source.CustomerPhone,
                CustomerName = source.CustomerName,
                District = source.District,
                DeliveryName = source.DeliveryName,
                DeliveryFee = source.DeliveryFee,
                Subtotal = source.Subtotal,
                Total = source.Total,
                KitchenPrinted = source.KitchenPrinted,
                KitchenPrintedAt = source.KitchenPrintedAt,
                Status = source.Status,
                ShiftTimeText = source.ShiftTimeText,
                CashierUserId = source.CashierUserId,
                CashierUsername = source.CashierUsername,
                CashierRole = source.CashierRole,
                Driver = source.Driver == null ? null : new DeliveryDriverRecord { Id = source.Driver.Id, Name = source.Driver.Name, Phone = source.Driver.Phone, AppUserId = source.Driver.AppUserId },
                Address = source.Address == null ? null : new DeliveryAddressRecord
                {
                    Id = source.Address.Id,
                    District = source.Address.District,
                    Block = source.Address.Block,
                    Street = source.Address.Street,
                    Building = source.Address.Building,
                    Apartment = source.Address.Apartment,
                    Floor = source.Address.Floor,
                    Note = source.Address.Note
                },
                Items = new List<DeliveryLineRecord>()
            };

            if (source.Items != null)
            {
                for (int i = 0; i < source.Items.Count; i++)
                {
                    var it = source.Items[i];
                    copy.Items.Add(new DeliveryLineRecord
                    {
                        Key = it.Key,
                        Desc = it.Desc,
                        Notes = it.Notes,
                        Qty = it.Qty,
                        Price = it.Price,
                        ProductId = it.ProductId,
                        SourceProductId = it.SourceProductId,
                        OptionIds = it.OptionIds == null ? new List<string>() : new List<string>(it.OptionIds)
                    });
                }
            }

            return copy;
        }

        private void EnsureDeliveryAppOrdersInitialized()
        {
            if (!_deliveryAppOrdersInitialized) _deliveryAppOrdersInitialized = true;
            QueueSupabaseAppOrdersPullFromBackend();
        }

        private void RenderDeliveryAppOrderCounters()
        {
            EnsureDeliveryAppOrdersInitialized();

            var dPending = 0; var dPreparing = 0; var dDriverAssigned = 0; var dDone = 0;
            for (int i = 0; i < _deliveryAppDeliveryOrders.Count; i++)
            {
                var o = _deliveryAppDeliveryOrders[i];
                if (o == null) continue;
                if (o.Status == AppDeliveryOrderStatus.Pending) dPending++;
                else if (o.Status == AppDeliveryOrderStatus.Preparing) dPreparing++;
                else if (o.Status == AppDeliveryOrderStatus.DriverAssigned) dDriverAssigned++;
                else if (o.Status == AppDeliveryOrderStatus.Done) dDone++;
            }

            var pPending = 0; var pPreparing = 0; var pReady = 0; var pDelivered = 0;
            for (int i = 0; i < _deliveryAppPickupOrders.Count; i++)
            {
                var o = _deliveryAppPickupOrders[i];
                if (o == null) continue;
                if (o.Status == AppPickupOrderStatus.Pending) pPending++;
                else if (o.Status == AppPickupOrderStatus.Accepted || o.Status == AppPickupOrderStatus.Preparing) pPreparing++;
                else if (o.Status == AppPickupOrderStatus.Ready) pReady++;
                else if (o.Status == AppPickupOrderStatus.Delivered) pDelivered++;
            }

            var deliveryCountersText = string.Format("طلبات التطبيق دليفري | جديد:{0} | تجهيز:{1} | مع سائق:{2} | تم:{3}", dPending, dPreparing, dDriverAssigned, dDone);
            var pickupCountersText = string.Format("طلبات التطبيق استلام | جديد:{0} | تجهيز:{1} | جاهز:{2} | تم:{3}", pPending, pPreparing, pReady, pDelivered);

            if (DeliveryAppDeliveryCountersText != null) DeliveryAppDeliveryCountersText.Text = deliveryCountersText;
            if (DeliveryAppPickupCountersText != null) DeliveryAppPickupCountersText.Text = pickupCountersText;
            if (TakeawayAppDeliveryCountersText != null) TakeawayAppDeliveryCountersText.Text = deliveryCountersText;
            if (TakeawayAppPickupCountersText != null) TakeawayAppPickupCountersText.Text = pickupCountersText;
        }

        private void OpenDeliveryAppOrdersBoardView()
        {
            if (DeliveryAppOrdersBoardView == null) return;
            EnsureDeliveryAppOrdersInitialized();
            RefreshDeliveryAppOrdersUi();
            DeliveryAppOrdersBoardView.Visibility = Visibility.Visible;
        }

        private void CloseDeliveryAppOrdersBoardView()
        {
            if (DeliveryAppOrdersBoardView != null) DeliveryAppOrdersBoardView.Visibility = Visibility.Collapsed;
        }

        private void DeliveryAppOrdersBoardBackButton_Click(object sender, RoutedEventArgs e)
        {
            CloseDeliveryAppOrdersBoardView();
        }

        private void RefreshDeliveryAppOrdersUi()
        {
            RenderDeliveryAppOrderCounters();
            RenderDeliveryAppOrdersBoard();
            UpdateDeliveryAppBulkActionButtons();
        }

        private bool TryBeginAppOrderBusyState(string orderId)
        {
            var cleanedId = (orderId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(cleanedId)) return false;
            lock (_deliveryAppBusyOrderIds)
            {
                if (_deliveryAppBusyOrderIds.Contains(cleanedId)) return false;
                _deliveryAppBusyOrderIds.Add(cleanedId);
            }
            QueueSupabaseAppOrdersUiRefresh();
            return true;
        }

        private void EndAppOrderBusyState(string orderId)
        {
            var cleanedId = (orderId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(cleanedId)) return;
            lock (_deliveryAppBusyOrderIds) _deliveryAppBusyOrderIds.Remove(cleanedId);
            QueueSupabaseAppOrdersUiRefresh();
        }

        private bool IsAppOrderBusyState(string orderId)
        {
            var cleanedId = (orderId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(cleanedId)) return false;
            lock (_deliveryAppBusyOrderIds) return _deliveryAppBusyOrderIds.Contains(cleanedId);
        }

        private static bool HasAppOrderAddress(string district, string addressText)
        {
            return !string.IsNullOrWhiteSpace(district) || !string.IsNullOrWhiteSpace(addressText);
        }

        private string BuildAppOrderAddressLine(string district, string addressText)
        {
            var parts = new List<string>();
            var cleanedDistrict = (district ?? string.Empty).Trim();
            var cleanedAddress = (addressText ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(cleanedDistrict)) parts.Add("المنطقة: " + cleanedDistrict);
            if (!string.IsNullOrWhiteSpace(cleanedAddress)) parts.Add("العنوان: " + cleanedAddress);
            return parts.Count == 0 ? "العنوان: غير محدد" : string.Join(" | ", parts);
        }

        private DeliveryAddressRecord CloneDeliveryAddress(DeliveryAddressRecord source)
        {
            if (source == null) return new DeliveryAddressRecord();
            return new DeliveryAddressRecord
            {
                Id = source.Id,
                District = source.District,
                Block = source.Block,
                Street = source.Street,
                Building = source.Building,
                Apartment = source.Apartment,
                Floor = source.Floor,
                Note = source.Note
            };
        }

        private DeliveryAddressRecord ParseAppDeliveryAddressText(string district, string addressText)
        {
            var parsed = new DeliveryAddressRecord
            {
                District = (district ?? string.Empty).Trim()
            };

            var raw = (addressText ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw)) return parsed;

            var normalized = raw
                .Replace("—", "|")
                .Replace(" - ", "|")
                .Replace(" | ", "|");
            var segments = normalized.Split(new[] { '|', '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var fallbackParts = new List<string>();
            for (int i = 0; i < segments.Length; i++)
            {
                var segment = (segments[i] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(segment)) continue;

                if (TryExtractLabeledAddressPart(segment, new[] { "المجاورة", "بلوك", "block" }, out var block))
                {
                    parsed.Block = block;
                    continue;
                }
                if (TryExtractLabeledAddressPart(segment, new[] { "الشارع", "street" }, out var street))
                {
                    parsed.Street = street;
                    continue;
                }
                if (TryExtractLabeledAddressPart(segment, new[] { "عمارة", "مبنى", "building" }, out var building))
                {
                    parsed.Building = building;
                    continue;
                }
                if (TryExtractLabeledAddressPart(segment, new[] { "شقة", "apt", "apartment" }, out var apartment))
                {
                    parsed.Apartment = apartment;
                    continue;
                }
                if (TryExtractLabeledAddressPart(segment, new[] { "دور", "floor" }, out var floor))
                {
                    parsed.Floor = floor;
                    continue;
                }
                if (TryExtractLabeledAddressPart(segment, new[] { "ملاحظة", "note" }, out var note))
                {
                    parsed.Note = note;
                    continue;
                }

                fallbackParts.Add(segment);
            }

            if (string.IsNullOrWhiteSpace(parsed.Street) && fallbackParts.Count > 0)
            {
                parsed.Street = fallbackParts[0];
                fallbackParts.RemoveAt(0);
            }
            if (string.IsNullOrWhiteSpace(parsed.Note) && fallbackParts.Count > 0)
            {
                parsed.Note = string.Join(" - ", fallbackParts.ToArray());
            }

            return parsed;
        }

        private bool TryExtractLabeledAddressPart(string segment, string[] labels, out string value)
        {
            value = string.Empty;
            var raw = (segment ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw) || labels == null || labels.Length == 0) return false;

            for (int i = 0; i < labels.Length; i++)
            {
                var label = (labels[i] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(label)) continue;
                if (!raw.StartsWith(label, StringComparison.OrdinalIgnoreCase)) continue;

                var remainder = raw.Substring(label.Length).TrimStart();
                if (remainder.StartsWith(":", StringComparison.Ordinal) || remainder.StartsWith("：", StringComparison.Ordinal))
                {
                    remainder = remainder.Substring(1).TrimStart();
                }
                value = remainder;
                return true;
            }

            return false;
        }

        private string BuildAppDeliveryAddressText(DeliveryAddressRecord address)
        {
            if (address == null) return string.Empty;
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(address.Block)) parts.Add("المجاورة " + address.Block.Trim());
            if (!string.IsNullOrWhiteSpace(address.Street)) parts.Add("الشارع " + address.Street.Trim());
            if (!string.IsNullOrWhiteSpace(address.Building)) parts.Add("مبنى " + address.Building.Trim());
            if (!string.IsNullOrWhiteSpace(address.Apartment)) parts.Add("شقة " + address.Apartment.Trim());
            if (!string.IsNullOrWhiteSpace(address.Floor)) parts.Add("دور " + address.Floor.Trim());
            if (!string.IsNullOrWhiteSpace(address.Note)) parts.Add("ملاحظة " + address.Note.Trim());
            return string.Join(" - ", parts.ToArray());
        }

        private DeliveryAddressRecord GetEditableAddressForAppDeliveryOrder(AppDeliveryOrderRecord order)
        {
            if (order == null) return new DeliveryAddressRecord();
            var address = order.Address == null ? ParseAppDeliveryAddressText(order.District, order.AddressText) : CloneDeliveryAddress(order.Address);
            if (string.IsNullOrWhiteSpace(address.District)) address.District = (order.District ?? string.Empty).Trim();
            return address;
        }

        private int GetStoredAppDeliveryOrderSubtotal(AppDeliveryOrderRecord order)
        {
            if (order == null) return 0;
            if (order.Subtotal > 0) return order.Subtotal;
            return GetAppOrderItemsTotal(order.Items);
        }

        private int GetStoredAppDeliveryOrderDeliveryFee(AppDeliveryOrderRecord order)
        {
            if (order == null) return 0;
            return order.DeliveryFee < 0 ? 0 : order.DeliveryFee;
        }

        private int GetStoredAppDeliveryOrderTotal(AppDeliveryOrderRecord order)
        {
            if (order == null) return 0;
            var subtotal = GetStoredAppDeliveryOrderSubtotal(order);
            var deliveryFee = GetStoredAppDeliveryOrderDeliveryFee(order);
            var baseTotal = subtotal + deliveryFee;
            return order.Total > 0 ? order.Total : baseTotal;
        }

        private int GetStoredAppDeliveryOrderDiscount(AppDeliveryOrderRecord order)
        {
            var subtotal = GetStoredAppDeliveryOrderSubtotal(order);
            var deliveryFee = GetStoredAppDeliveryOrderDeliveryFee(order);
            var total = GetStoredAppDeliveryOrderTotal(order);
            var discount = (subtotal + deliveryFee) - total;
            return discount > 0 ? discount : 0;
        }

        private bool AppDeliveryOrderMatchesCustomer(AppDeliveryOrderRecord order, string customerUserId, string phone)
        {
            if (order == null) return false;
            var normalizedUserId = (customerUserId ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(normalizedUserId)
                && string.Equals((order.CustomerUserId ?? string.Empty).Trim(), normalizedUserId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var normalizedPhone = (phone ?? string.Empty).Trim();
            return !string.IsNullOrWhiteSpace(normalizedPhone)
                && string.Equals((order.Phone ?? string.Empty).Trim(), normalizedPhone, StringComparison.Ordinal);
        }

        private void UpdateExistingAppDeliveryShiftRecords(List<AppDeliveryOrderRecord> orders)
        {
            if (orders == null || orders.Count == 0) return;

            NormalizeTakeawayState();
            var list = _takeawayState == null ? null : _takeawayState.ShiftRecords;
            if (list == null || list.Count == 0) return;

            var changed = false;
            for (int i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                if (order == null || string.IsNullOrWhiteSpace(order.Id)) continue;
                for (int j = 0; j < list.Count; j++)
                {
                    var existing = list[j];
                    if (existing == null) continue;
                    if (!string.Equals(existing.BackendOrderId, order.Id, StringComparison.OrdinalIgnoreCase)) continue;

                    existing.SourceKind = "APP";
                    existing.ChannelKind = "DELIVERY";
                    existing.OrderNo = order.No;
                    existing.DisplayRef = order.No > 0 ? order.No.ToString() : order.Id;
                    existing.CustomerName = order.Name ?? string.Empty;
                    existing.CustomerPhone = order.Phone ?? string.Empty;
                    existing.Subtotal = GetStoredAppDeliveryOrderSubtotal(order);
                    existing.DeliveryFee = GetStoredAppDeliveryOrderDeliveryFee(order);
                    existing.Total = GetStoredAppDeliveryOrderTotal(order);
                    existing.CreatedAtMillis = order.CreatedAt > 0 ? order.CreatedAt : existing.CreatedAtMillis;
                    NormalizePosShiftOrderRecord(existing);
                    changed = true;
                }
            }

            if (!changed) return;
            PersistTakeawayState();
            var overlay = FindTakeawayShiftOverlay();
            if (overlay != null && overlay.Visibility == Visibility.Visible)
            {
                RenderTakeawayShiftRows();
            }
        }

        private void ApplyAppDeliveryCustomerAddressChangeLocally(AppDeliveryOrderRecord sourceOrder, DeliveryAddressRecord updatedAddress, int deliveryFee, string addressId)
        {
            if (sourceOrder == null) return;

            var normalizedAddress = CloneDeliveryAddress(updatedAddress);
            normalizedAddress.District = (sourceOrder == null ? string.Empty : (updatedAddress == null ? sourceOrder.District : updatedAddress.District)) ?? string.Empty;
            var normalizedAddressId = (addressId ?? string.Empty).Trim();
            var addressText = BuildAppDeliveryAddressText(normalizedAddress);
            var customerUserId = sourceOrder.CustomerUserId;
            var customerPhone = sourceOrder.Phone;
            var changedOrders = new List<AppDeliveryOrderRecord>();

            for (int i = 0; i < _deliveryAppDeliveryOrders.Count; i++)
            {
                var current = _deliveryAppDeliveryOrders[i];
                if (!AppDeliveryOrderMatchesCustomer(current, customerUserId, customerPhone)) continue;

                var discount = GetStoredAppDeliveryOrderDiscount(current);
                current.Address = CloneDeliveryAddress(normalizedAddress);
                current.AddressId = normalizedAddressId;
                current.District = normalizedAddress.District ?? string.Empty;
                current.AddressText = addressText;
                current.Subtotal = GetStoredAppDeliveryOrderSubtotal(current);
                current.DeliveryFee = deliveryFee < 0 ? 0 : deliveryFee;
                current.Total = Math.Max(0, current.Subtotal + current.DeliveryFee - discount);
                changedOrders.Add(current);
            }

            if (changedOrders.Count == 0)
            {
                var discount = GetStoredAppDeliveryOrderDiscount(sourceOrder);
                sourceOrder.Address = CloneDeliveryAddress(normalizedAddress);
                sourceOrder.AddressId = normalizedAddressId;
                sourceOrder.District = normalizedAddress.District ?? string.Empty;
                sourceOrder.AddressText = addressText;
                sourceOrder.Subtotal = GetStoredAppDeliveryOrderSubtotal(sourceOrder);
                sourceOrder.DeliveryFee = deliveryFee < 0 ? 0 : deliveryFee;
                sourceOrder.Total = Math.Max(0, sourceOrder.Subtotal + sourceOrder.DeliveryFee - discount);
                changedOrders.Add(sourceOrder);
            }

            UpdateExistingAppDeliveryShiftRecords(changedOrders);
            PersistDeliveryState();
            QueueSupabaseAppOrdersUiRefresh();
        }

        private bool ShowAppOrderAddressEditor(string currentDistrict, string currentAddressText, out string updatedDistrict, out string updatedAddressText)
        {
            var dialog = new Window
            {
                Title = "تعديل عنوان الطلب",
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                SizeToContent = SizeToContent.WidthAndHeight,
                Background = Brushes.White,
                FlowDirection = FlowDirection.RightToLeft,
                ShowInTaskbar = false
            };

            var root = new StackPanel
            {
                Width = 440,
                Margin = new Thickness(16)
            };

            root.Children.Add(new TextBlock
            {
                Text = "عدّل عنوان طلب التطبيق ثم احفظ التغيير.",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 12),
                TextWrapping = TextWrapping.Wrap
            });

            root.Children.Add(new TextBlock
            {
                Text = "المنطقة",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var districtBox = new TextBox
            {
                Text = (currentDistrict ?? string.Empty).Trim(),
                Height = 32,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            root.Children.Add(districtBox);

            root.Children.Add(new TextBlock
            {
                Text = "العنوان",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var addressBox = new TextBox
            {
                Text = (currentAddressText ?? string.Empty).Trim(),
                MinHeight = 120,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontWeight = FontWeights.Bold
            };
            root.Children.Add(addressBox);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                FlowDirection = FlowDirection.LeftToRight,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 14, 0, 0)
            };

            var saveButton = new Button
            {
                Content = "حفظ",
                Width = 110,
                Height = 34,
                Margin = new Thickness(0, 0, 8, 0),
                FontWeight = FontWeights.Black,
                IsDefault = true
            };
            var cancelButton = new Button
            {
                Content = "إلغاء",
                Width = 110,
                Height = 34,
                FontWeight = FontWeights.Black,
                IsCancel = true
            };

            saveButton.Click += delegate
            {
                dialog.DialogResult = true;
                dialog.Close();
            };
            cancelButton.Click += delegate
            {
                dialog.DialogResult = false;
                dialog.Close();
            };

            buttons.Children.Add(saveButton);
            buttons.Children.Add(cancelButton);
            root.Children.Add(buttons);
            dialog.Content = root;

            var result = dialog.ShowDialog() == true;
            updatedDistrict = (districtBox.Text ?? string.Empty).Trim();
            updatedAddressText = (addressBox.Text ?? string.Empty).Trim();
            return result;
        }

        private void EditAppDeliveryOrderAddress(AppDeliveryOrderRecord order)
        {
            var currentOrder = FindAppDeliveryOrderById(order != null ? order.Id : null) ?? order;
            if (currentOrder == null || string.IsNullOrWhiteSpace(currentOrder.Id)) return;
            if (!HasSupabaseAppOrdersBackendConfigured())
            {
                MessageBox.Show("ربط طلبات التطبيق مع الباك اند مطلوب.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (IsAppOrderBusyState(currentOrder.Id))
            {
                MessageBox.Show("الطلب جاري تحديثه الآن. انتظر لحظة ثم حاول مرة أخرى.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            OpenAppDeliveryOrderAddressForm(currentOrder);
        }

        private void UpdateDeliveryAppBulkActionButtons()
        {
            if (DeliveryAppDeliveryApplyStatusButton != null)
            {
                DeliveryAppDeliveryApplyStatusButton.Visibility = _deliveryAppDeliverySelectedIds.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                DeliveryAppDeliveryApplyStatusButton.IsEnabled = _deliveryAppDeliverySelectedIds.Count > 0;
            }
            if (DeliveryAppPickupApplyStatusButton != null)
            {
                DeliveryAppPickupApplyStatusButton.Visibility = _deliveryAppPickupSelectedIds.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                DeliveryAppPickupApplyStatusButton.IsEnabled = _deliveryAppPickupSelectedIds.Count > 0;
            }
        }

        private void DeliveryAppDeliverySelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            var visibleIds = new List<string>();
            for (int i = 0; i < _deliveryAppDeliveryOrders.Count; i++)
            {
                var o = _deliveryAppDeliveryOrders[i];
                if (o == null) continue;
                if (o.Status == AppDeliveryOrderStatus.Done) continue;
                visibleIds.Add(o.Id);
            }
            var allSelected = visibleIds.Count > 0;
            for (int i = 0; i < visibleIds.Count; i++) if (!_deliveryAppDeliverySelectedIds.Contains(visibleIds[i])) { allSelected = false; break; }
            _deliveryAppDeliverySelectedIds.Clear();
            if (!allSelected) for (int i = 0; i < visibleIds.Count; i++) _deliveryAppDeliverySelectedIds.Add(visibleIds[i]);
            QueueSupabaseAppOrdersUiRefresh();
        }

        private void DeliveryAppDeliveryApplyStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (_deliveryAppDeliverySelectedIds.Count == 0) return;
            var list = new List<AppDeliveryOrderRecord>();
            for (int i = 0; i < _deliveryAppDeliveryOrders.Count; i++)
            {
                var o = _deliveryAppDeliveryOrders[i];
                if (o != null && _deliveryAppDeliverySelectedIds.Contains(o.Id)) list.Add(o);
            }
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i];
                if (o == null) continue;
                if (o.Status == AppDeliveryOrderStatus.DriverAssigned || o.Status == AppDeliveryOrderStatus.Done) continue;
                AdvanceAppDeliveryOrderStatus(o);
            }
        }

        private void DeliveryAppPickupSelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            var visibleIds = new List<string>();
            for (int i = 0; i < _deliveryAppPickupOrders.Count; i++)
            {
                var o = _deliveryAppPickupOrders[i];
                if (o == null) continue;
                visibleIds.Add(o.Id);
            }
            var allSelected = visibleIds.Count > 0;
            for (int i = 0; i < visibleIds.Count; i++) if (!_deliveryAppPickupSelectedIds.Contains(visibleIds[i])) { allSelected = false; break; }
            _deliveryAppPickupSelectedIds.Clear();
            if (!allSelected) for (int i = 0; i < visibleIds.Count; i++) _deliveryAppPickupSelectedIds.Add(visibleIds[i]);
            QueueSupabaseAppOrdersUiRefresh();
        }

        private void DeliveryAppPickupApplyStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (_deliveryAppPickupSelectedIds.Count == 0) return;
            var list = new List<AppPickupOrderRecord>();
            for (int i = 0; i < _deliveryAppPickupOrders.Count; i++)
            {
                var o = _deliveryAppPickupOrders[i];
                if (o != null && _deliveryAppPickupSelectedIds.Contains(o.Id)) list.Add(o);
            }
            for (int i = 0; i < list.Count; i++) AdvanceAppPickupOrderStatus(list[i]);
        }

        private void RenderDeliveryAppOrdersBoard()
        {
            if (DeliveryAppDeliveryPendingPanel == null) return;
            RenderDeliveryAppDeliveryStatusPanel(DeliveryAppDeliveryPendingPanel, AppDeliveryOrderStatus.Pending);
            RenderDeliveryAppDeliveryStatusPanel(DeliveryAppDeliveryPreparingPanel, AppDeliveryOrderStatus.Preparing);
            RenderDeliveryAppDeliveryStatusPanel(DeliveryAppDeliveryWithDriverPanel, AppDeliveryOrderStatus.DriverAssigned);
            RenderDeliveryAppDeliveryStatusPanel(DeliveryAppDeliveryDonePanel, AppDeliveryOrderStatus.Done);

            RenderDeliveryAppPickupStatusPanel(DeliveryAppPickupPendingPanel, AppPickupOrderStatus.Pending);
            if (DeliveryAppPickupAcceptedPanel != null) DeliveryAppPickupAcceptedPanel.Children.Clear();
            RenderDeliveryAppPickupStatusPanel(DeliveryAppPickupPreparingPanel, AppPickupOrderStatus.Preparing);
            RenderDeliveryAppPickupStatusPanel(DeliveryAppPickupReadyPanel, AppPickupOrderStatus.Ready);
            RenderDeliveryAppPickupStatusPanel(DeliveryAppPickupDeliveredPanel, AppPickupOrderStatus.Delivered);
        }

        private void RenderDeliveryAppDeliveryStatusPanel(StackPanel panel, AppDeliveryOrderStatus status)
        {
            if (panel == null) return;
            // [PHASE 7.1 PURE CLOUD] Anti-flicker UI mutation
            var existingCards = new System.Collections.Generic.Dictionary<string, Border>();
            var toRemove = new System.Collections.Generic.List<UIElement>();
            foreach (UIElement child in panel.Children)
            {
                if (child is Border b && b.Tag is string id) existingCards[id] = b;
                else toRemove.Add(child);
            }

            var activeIds = new System.Collections.Generic.HashSet<string>();
            var count = 0;
            for (int i = 0; i < _deliveryAppDeliveryOrders.Count; i++)
            {
                var order = _deliveryAppDeliveryOrders[i];
                if (order == null || order.Status != status) continue;
                var id = order.Id;
                activeIds.Add(id);
                if (!existingCards.ContainsKey(id))
                {
                    panel.Children.Add(CreateDeliveryAppDeliveryCard(order));
                }
                else
                {
                    var oldCard = existingCards[id];
                    var newCard = CreateDeliveryAppDeliveryCard(order);
                    var idx = panel.Children.IndexOf(oldCard);
                    if (idx >= 0)
                    {
                        panel.Children.RemoveAt(idx);
                        panel.Children.Insert(idx, newCard);
                    }
                }
                count++;
            }

            for (int i = panel.Children.Count - 1; i >= 0; i--)
            {
                var child = panel.Children[i];
                if (child is Border b && b.Tag is string id && !activeIds.Contains(id))
                {
                    panel.Children.RemoveAt(i);
                }
                else if (toRemove.Contains(child))
                {
                    panel.Children.RemoveAt(i);
                }
            }
            if (count == 0) panel.Children.Add(CreateAppBoardEmptyHint());
        }

        private void RenderDeliveryAppPickupStatusPanel(StackPanel panel, AppPickupOrderStatus status)
        {
            if (panel == null) return;
            // [PHASE 7.1 PURE CLOUD] Anti-flicker UI mutation
            var existingCards = new System.Collections.Generic.Dictionary<string, Border>();
            var toRemove = new System.Collections.Generic.List<UIElement>();
            foreach (UIElement child in panel.Children)
            {
                if (child is Border b && b.Tag is string id) existingCards[id] = b;
                else toRemove.Add(child);
            }

            var activeIds = new System.Collections.Generic.HashSet<string>();
            var count = 0;
            for (int i = 0; i < _deliveryAppPickupOrders.Count; i++)
            {
                var order = _deliveryAppPickupOrders[i];
                if (order == null || order.Status != status) continue;
                var id = order.Id;
                activeIds.Add(id);
                if (!existingCards.ContainsKey(id))
                {
                    panel.Children.Add(CreateDeliveryAppPickupCard(order));
                }
                else
                {
                    var oldCard = existingCards[id];
                    var newCard = CreateDeliveryAppPickupCard(order);
                    var idx = panel.Children.IndexOf(oldCard);
                    if (idx >= 0)
                    {
                        panel.Children.RemoveAt(idx);
                        panel.Children.Insert(idx, newCard);
                    }
                }
                count++;
            }

            for (int i = panel.Children.Count - 1; i >= 0; i--)
            {
                var child = panel.Children[i];
                if (child is Border b && b.Tag is string id && !activeIds.Contains(id))
                {
                    panel.Children.RemoveAt(i);
                }
                else if (toRemove.Contains(child))
                {
                    panel.Children.RemoveAt(i);
                }
            }
            if (count == 0) panel.Children.Add(CreateAppBoardEmptyHint());
        }

        private UIElement CreateAppBoardEmptyHint()
        {
            return new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD0D0D0")),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7F7F7")),
                Padding = new Thickness(10),
                Child = new TextBlock { Text = "لا توجد طلبات الآن", FontWeight = FontWeights.Bold, FontSize = 13, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF666666")), FlowDirection = FlowDirection.RightToLeft }
            };
        }

        private Border CreateAppOrderInfoSection(string title, string value)
        {
            var stack = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF555555")),
                Margin = new Thickness(0, 0, 0, 4)
            });
            stack.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(value) ? "-" : value.Trim(),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111111"))
            });

            return new Border
            {
                Margin = new Thickness(0, 6, 0, 0),
                Padding = new Thickness(10, 8, 10, 8),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7F7F7")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE1E1E1")),
                BorderThickness = new Thickness(1),
                Child = stack
            };
        }

        private Border CreateAppOrderItemsSection(List<AppOrderItemRecord> items)
        {
            var stack = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            stack.Children.Add(new TextBlock
            {
                Text = "تفاصيل الطلب",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF555555")),
                Margin = new Thickness(0, 0, 0, 4)
            });

            if (items == null || items.Count == 0)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "لا توجد أصناف مسجلة",
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF777777"))
                });
            }
            else
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item == null) continue;

                    var row = new Grid { Margin = new Thickness(0, i == 0 ? 0 : 4, 0, 0), FlowDirection = FlowDirection.LeftToRight };
                    row.ColumnDefinitions.Add(new ColumnDefinition());
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var name = new TextBlock
                    {
                        Text = string.Format("{0} × {1}", item.Qty < 1 ? 1 : item.Qty, string.IsNullOrWhiteSpace(item.Name) ? "صنف غير مسمى" : item.Name.Trim()),
                        FontSize = 13,
                        FontWeight = FontWeights.SemiBold,
                        TextWrapping = TextWrapping.Wrap,
                        FlowDirection = FlowDirection.RightToLeft
                    };
                    Grid.SetColumn(name, 0);
                    row.Children.Add(name);

                    var price = new TextBlock
                    {
                        Text = string.Format("{0} ج.م", item.Price < 0 ? 0 : item.Price),
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(10, 0, 0, 0),
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF444444")),
                        FlowDirection = FlowDirection.RightToLeft
                    };
                    Grid.SetColumn(price, 1);
                    row.Children.Add(price);

                    stack.Children.Add(row);
                }
            }

            return new Border
            {
                Margin = new Thickness(0, 6, 0, 0),
                Padding = new Thickness(10, 8, 10, 8),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFBF6EA")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE3D7B6")),
                BorderThickness = new Thickness(1),
                Child = stack
            };
        }

        private Border CreateDeliveryAppDeliveryCard(AppDeliveryOrderRecord order)
        {
            var isBusy = IsAppOrderBusyState(order != null ? order.Id : null);
            var card = new Border
            {
                Tag = order != null ? order.Id : null,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9A9A9A")),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(10)
            };

            if (order != null && order.Status == AppDeliveryOrderStatus.Preparing && !isBusy)
            {
                var dbl = order;
                card.MouseLeftButtonDown += delegate(object s2, MouseButtonEventArgs e)
                {
                    if (e.ClickCount < 2) return;
                    e.Handled = true;
                    OpenDeliveryDriversOverlayForAppOrder(dbl);
                };
            }

            var captured = order;
            var stack = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            var topRow = new Grid { FlowDirection = FlowDirection.LeftToRight };
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topRow.ColumnDefinitions.Add(new ColumnDefinition());
            var cb = new CheckBox { Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, IsChecked = _deliveryAppDeliverySelectedIds.Contains(order.Id), IsEnabled = !isBusy };
            cb.Checked += delegate { _deliveryAppDeliverySelectedIds.Add(captured.Id); UpdateDeliveryAppBulkActionButtons(); };
            cb.Unchecked += delegate { _deliveryAppDeliverySelectedIds.Remove(captured.Id); UpdateDeliveryAppBulkActionButtons(); };
            Grid.SetColumn(cb, 0);
            topRow.Children.Add(cb);
            var title = new TextBlock { Text = "طلب التطبيق رقم " + order.No, FontWeight = FontWeights.Black, FontSize = 15, FlowDirection = FlowDirection.RightToLeft };
            Grid.SetColumn(title, 1);
            topRow.Children.Add(title);
            stack.Children.Add(topRow);

            stack.Children.Add(new TextBlock { Text = (order.Name ?? "-") + " | " + (order.Phone ?? "-"), FontWeight = FontWeights.Bold, FontSize = 13, Margin = new Thickness(0, 4, 0, 0), FlowDirection = FlowDirection.RightToLeft });
            stack.Children.Add(new TextBlock { Text = "الوقت: " + FormatTimeShort(order.CreatedAt), FontSize = 12, Margin = new Thickness(0, 4, 0, 0), FlowDirection = FlowDirection.RightToLeft });
            stack.Children.Add(new TextBlock { Text = "الحالة: " + GetAppDeliveryStatusLabel(order.Status), FontSize = 12, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 3, 0, 0), FlowDirection = FlowDirection.RightToLeft });
            stack.Children.Add(new TextBlock { Text = "المصدر: طلب تطبيق دليفري", FontSize = 12, Margin = new Thickness(0, 3, 0, 0), FlowDirection = FlowDirection.RightToLeft });
            stack.Children.Add(CreateAppOrderInfoSection("ملخص الحساب", string.Format("قيمة الطلب: {0} ج.م\nخدمة التوصيل: {1} ج.م\nالإجمالي: {2} ج.م", GetStoredAppDeliveryOrderSubtotal(order), GetStoredAppDeliveryOrderDeliveryFee(order), GetStoredAppDeliveryOrderTotal(order))));
            if (order.Driver != null) stack.Children.Add(CreateAppOrderInfoSection("السائق", order.Driver.Name));
            if (HasAppOrderAddress(order.District, order.AddressText))
            {
                stack.Children.Add(CreateAppOrderInfoSection("العنوان", BuildAppOrderAddressLine(order.District, order.AddressText)));
            }
            stack.Children.Add(CreateAppOrderItemsSection(order.Items));

            var actions = new WrapPanel { Margin = new Thickness(0, 8, 0, 0), FlowDirection = FlowDirection.RightToLeft };
            var actionText = isBusy ? "جارٍ التحديث..." : GetAppDeliveryNextActionLabel(order.Status);
            if (!string.IsNullOrWhiteSpace(actionText))
            {
                var btn = new Button { Content = actionText, Height = 30, Margin = new Thickness(0, 0, 6, 0), FontWeight = FontWeights.Black, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF7F7F7F")), IsEnabled = !isBusy };
                btn.Click += delegate { AdvanceAppDeliveryOrderStatus(captured); };
                actions.Children.Add(btn);
            }
            var editAddressButton = new Button
            {
                Content = "تعديل العنوان",
                Height = 30,
                Margin = new Thickness(0, 0, 6, 0),
                FontWeight = FontWeights.Black,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF666666")),
                ToolTip = isBusy ? "الطلب جاري تحديثه الآن، لكن الضغط سيعرض السبب." : "فتح نفس فورم عنوان الدليفري لتعديل العنوان"
            };
            editAddressButton.Click += delegate { EditAppDeliveryOrderAddress(captured); };
            actions.Children.Add(editAddressButton);
            if (_isAdminMode && order.KitchenPrinted)
            {
                var btn = new Button { Content = "إعادة طباعة المطبخ", Height = 30, Margin = new Thickness(0, 0, 6, 0), FontWeight = FontWeights.Black, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF666666")), IsEnabled = !isBusy };
                btn.Click += delegate { TryPrintAppDeliveryKitchenReceipt(captured, true); };
                actions.Children.Add(btn);
            }
            if (_isAdminMode && order.Driver != null && order.DriverReceiptPrinted)
            {
                var btn = new Button { Content = "إعادة طباعة السائق", Height = 30, Margin = new Thickness(0, 0, 6, 0), FontWeight = FontWeights.Black, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF666666")), IsEnabled = !isBusy };
                btn.Click += delegate { TryPrintFlowDocument(BuildAppDeliveryDriverReceiptDocument(captured, captured.Driver)); };
                actions.Children.Add(btn);
            }
            if (actions.Children.Count > 0) stack.Children.Add(actions);

            card.Child = stack;
            return card;
        }

        private Border CreateDeliveryAppPickupCard(AppPickupOrderRecord order)
        {
            var isBusy = IsAppOrderBusyState(order != null ? order.Id : null);
            var card = new Border
            {
                Tag = order != null ? order.Id : null,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9A9A9A")),
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(10)
            };

            var captured = order;
            var stack = new StackPanel { FlowDirection = FlowDirection.RightToLeft };
            var topRow = new Grid { FlowDirection = FlowDirection.LeftToRight };
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topRow.ColumnDefinitions.Add(new ColumnDefinition());
            var cb = new CheckBox { Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, IsChecked = _deliveryAppPickupSelectedIds.Contains(order.Id), IsEnabled = !isBusy };
            cb.Checked += delegate { _deliveryAppPickupSelectedIds.Add(captured.Id); UpdateDeliveryAppBulkActionButtons(); };
            cb.Unchecked += delegate { _deliveryAppPickupSelectedIds.Remove(captured.Id); UpdateDeliveryAppBulkActionButtons(); };
            Grid.SetColumn(cb, 0);
            topRow.Children.Add(cb);
            var title = new TextBlock { Text = "طلب التطبيق رقم " + order.No, FontWeight = FontWeights.Black, FontSize = 15, FlowDirection = FlowDirection.RightToLeft };
            Grid.SetColumn(title, 1);
            topRow.Children.Add(title);
            stack.Children.Add(topRow);

            stack.Children.Add(new TextBlock { Text = (order.Name ?? "-") + " | " + (order.Phone ?? "-"), FontWeight = FontWeights.Bold, FontSize = 13, Margin = new Thickness(0, 4, 0, 0), FlowDirection = FlowDirection.RightToLeft });
            stack.Children.Add(new TextBlock { Text = "الوقت: " + FormatTimeShort(order.CreatedAt), FontSize = 12, Margin = new Thickness(0, 4, 0, 0), FlowDirection = FlowDirection.RightToLeft });
            stack.Children.Add(new TextBlock { Text = "الحالة: " + GetAppPickupStatusLabel(order.Status), FontSize = 12, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 3, 0, 0), FlowDirection = FlowDirection.RightToLeft });
            stack.Children.Add(new TextBlock { Text = "المصدر: طلب تطبيق استلام", FontSize = 12, Margin = new Thickness(0, 3, 0, 0), FlowDirection = FlowDirection.RightToLeft });
            if (HasAppOrderAddress(order.District, order.AddressText))
            {
                stack.Children.Add(CreateAppOrderInfoSection("العنوان", BuildAppOrderAddressLine(order.District, order.AddressText)));
            }
            stack.Children.Add(CreateAppOrderItemsSection(order.Items));

            var actions = new WrapPanel { Margin = new Thickness(0, 8, 0, 0), FlowDirection = FlowDirection.RightToLeft };
            var actionText = isBusy ? "جارٍ التحديث..." : GetAppPickupNextActionLabel(order.Status);
            if (!string.IsNullOrWhiteSpace(actionText))
            {
                var btn = new Button { Content = actionText, Height = 30, Margin = new Thickness(0, 0, 6, 0), FontWeight = FontWeights.Black, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF7F7F7F")), IsEnabled = !isBusy };
                btn.Click += delegate { AdvanceAppPickupOrderStatus(captured); };
                actions.Children.Add(btn);
            }
            if (_isAdminMode && order.PartnerReceiptPrinted)
            {
                var btn = new Button { Content = "إعادة طباعة", Height = 30, Margin = new Thickness(0, 0, 6, 0), FontWeight = FontWeights.Black, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF666666")), IsEnabled = !isBusy };
                btn.Click += delegate { TryPrintAppPickupPartnerReceipt(captured, true); };
                actions.Children.Add(btn);
            }
            if (actions.Children.Count > 0) stack.Children.Add(actions);

            card.Child = stack;
            return card;
        }

        private string GetAppDeliveryStatusLabel(AppDeliveryOrderStatus status)
        {
            if (status == AppDeliveryOrderStatus.Pending) return "جديد";
            if (status == AppDeliveryOrderStatus.Preparing) return "قيد التجهيز";
            if (status == AppDeliveryOrderStatus.DriverAssigned) return "تم تعيين سائق";
            return "تم التسليم";
        }

        private string GetAppPickupStatusLabel(AppPickupOrderStatus status)
        {
            if (status == AppPickupOrderStatus.Pending) return "جديد";
            if (status == AppPickupOrderStatus.Accepted) return "تم استلام الطلب";
            if (status == AppPickupOrderStatus.Preparing) return "قيد التجهيز";
            if (status == AppPickupOrderStatus.Ready) return "جاهز للاستلام";
            return "تم التسليم";
        }

        private string GetAppDeliveryNextActionLabel(AppDeliveryOrderStatus status)
        {
            if (status == AppDeliveryOrderStatus.Pending) return "ابدأ تجهيز";
            if (status == AppDeliveryOrderStatus.Preparing) return "تعيين سائق";
            return null;
        }

        private string GetAppPickupNextActionLabel(AppPickupOrderStatus status)
        {
            if (status == AppPickupOrderStatus.Pending) return "ابدأ تجهيز";
            if (status == AppPickupOrderStatus.Accepted) return "ابدأ تجهيز";
            if (status == AppPickupOrderStatus.Preparing) return "جاهز للاستلام";
            if (status == AppPickupOrderStatus.Ready) return "تم التسليم";
            return null;
        }

        private void AdvanceAppDeliveryOrderStatus(AppDeliveryOrderRecord order)
        {
            if (order == null) return;
            var currentOrder = FindAppDeliveryOrderById(order.Id) ?? order;
            if (!HasSupabaseAppOrdersBackendConfigured())
            {
                MessageBox.Show("ربط طلبات التطبيق مع الباك اند مطلوب.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (currentOrder.Status == AppDeliveryOrderStatus.Pending)
            {
                if (!TryBeginAppOrderBusyState(currentOrder.Id)) return;
                var previousStatus = currentOrder.Status;
                var previousKitchenPrinted = currentOrder.KitchenPrinted;
                if (!TryPrintAppDeliveryKitchenReceipt(currentOrder, false))
                {
                    EndAppOrderBusyState(currentOrder.Id);
                    return;
                }
                currentOrder.Status = AppDeliveryOrderStatus.Preparing;
                QueueSupabaseAppOrdersUiRefresh();
                PersistDeliveryState();
                if (!QueueSupabaseBackendAppDeliveryStatusUpdate(currentOrder, "PREPARING", true, null))
                {
                    currentOrder.Status = previousStatus;
                    currentOrder.KitchenPrinted = previousKitchenPrinted;
                    QueueSupabaseAppOrdersUiRefresh();
                    PersistDeliveryState();
                    EndAppOrderBusyState(currentOrder.Id);
                    MessageBox.Show(GetAppOrderBackendFailureMessage("تعذر إرسال الحالة إلى الباك اند."), "الدليفري", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                return;
            }
            if (currentOrder.Status == AppDeliveryOrderStatus.Preparing)
            {
                if (_deliveryAppDriverAssignInProgress) return;
                OpenDeliveryDriversOverlayForAppOrder(currentOrder);
                return;
            }
        }

        private void AdvanceAppPickupOrderStatus(AppPickupOrderRecord order)
        {
            if (order == null) return;
            if (!HasSupabaseAppOrdersBackendConfigured())
            {
                MessageBox.Show("ربط طلبات التطبيق مع الباك اند مطلوب.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (order.Status == AppPickupOrderStatus.Pending || order.Status == AppPickupOrderStatus.Accepted)
            {
                if (!TryBeginAppOrderBusyState(order.Id)) return;
                var previousStatus = order.Status;
                order.Status = AppPickupOrderStatus.Preparing;
                QueueSupabaseAppOrdersUiRefresh();
                PersistDeliveryState();
                if (!QueueSupabaseBackendAppPickupStatusUpdate(order, "PREPARING", null))
                {
                    order.Status = previousStatus;
                    QueueSupabaseAppOrdersUiRefresh();
                    PersistDeliveryState();
                    EndAppOrderBusyState(order.Id);
                    MessageBox.Show(GetAppOrderBackendFailureMessage("تعذر إرسال الحالة إلى الباك اند."), "الدليفري", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                return;
            }
            if (order.Status == AppPickupOrderStatus.Preparing)
            {
                if (!TryBeginAppOrderBusyState(order.Id)) return;
                var previousStatus = order.Status;
                order.Status = AppPickupOrderStatus.Ready;
                QueueSupabaseAppOrdersUiRefresh();
                PersistDeliveryState();
                if (!QueueSupabaseBackendAppPickupStatusUpdate(order, "READY", null))
                {
                    order.Status = previousStatus;
                    QueueSupabaseAppOrdersUiRefresh();
                    PersistDeliveryState();
                    EndAppOrderBusyState(order.Id);
                    MessageBox.Show(GetAppOrderBackendFailureMessage("تعذر إرسال الحالة إلى الباك اند."), "الدليفري", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                return;
            }
            if (order.Status == AppPickupOrderStatus.Ready)
            {
                if (!TryBeginAppOrderBusyState(order.Id)) return;
                var previousStatus = order.Status;
                var previousPartnerReceiptPrinted = order.PartnerReceiptPrinted;
                if (!TryPrintAppPickupPartnerReceipt(order, false))
                {
                    EndAppOrderBusyState(order.Id);
                    return;
                }
                order.Status = AppPickupOrderStatus.Delivered;
                QueueSupabaseAppOrdersUiRefresh();
                PersistDeliveryState();
                if (!QueueSupabaseBackendAppPickupStatusUpdate(order, "DELIVERED", true))
                {
                    order.Status = previousStatus;
                    order.PartnerReceiptPrinted = previousPartnerReceiptPrinted;
                    QueueSupabaseAppOrdersUiRefresh();
                    PersistDeliveryState();
                    EndAppOrderBusyState(order.Id);
                    MessageBox.Show(GetAppOrderBackendFailureMessage("تعذر إرسال الحالة إلى الباك اند."), "الدليفري", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                TrackCompletedAppPickupShiftOrder(order);
            }
        }

        private int GetAppOrderItemsTotal(List<AppOrderItemRecord> items)
        {
            var total = 0;
            if (items == null) return 0;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it == null) continue;
                total += (it.Qty < 0 ? 0 : it.Qty) * (it.Price < 0 ? 0 : it.Price);
            }
            return total;
        }

        private bool TryPrintAppDeliveryKitchenReceipt(AppDeliveryOrderRecord order, bool allowReprint)
        {
            if (order == null) return false;
            if (order.KitchenPrinted && !allowReprint)
            {
                MessageBox.Show("لا يمكن طباعة المطبخ أكثر من مرة لطلب التطبيق.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            if (!TryPrintFlowDocument(BuildAppDeliveryKitchenReceiptDocument(order))) return false;
            order.KitchenPrinted = true;
            PersistDeliveryState();
            return true;
        }

        private bool TryPrintAppPickupPartnerReceipt(AppPickupOrderRecord order, bool allowReprint)
        {
            if (order == null) return false;
            if (order.PartnerReceiptPrinted && !allowReprint)
            {
                MessageBox.Show("تمت طباعة فاتورة الاستلام لهذا الطلب من قبل.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            if (!TryPrintFlowDocument(BuildAppPickupPartnerReceiptDocument(order))) return false;
            order.PartnerReceiptPrinted = true;
            PersistDeliveryState();
            return true;
        }

        private void TrackCompletedManualDeliveryShiftOrder(DeliveryOrderRecord order)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.Id)) return;

            var localId = order.Id.Trim();
            var backendOrderId = localId.StartsWith("pos_", StringComparison.OrdinalIgnoreCase)
                ? localId
                : BuildPosOfflineBackendOrderId(localId);

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
                DisplayRef = order.No > 0 ? order.No.ToString() : localId,
                CustomerName = order.CustomerName ?? string.Empty,
                CustomerPhone = order.CustomerPhone ?? string.Empty,
                Subtotal = subtotal,
                DeliveryFee = deliveryFee,
                Total = total,
                CreatedAtMillis = order.CreatedAt > 0 ? order.CreatedAt : NowUnixMs(),
                FinalizedAtMillis = NowUnixMs(),
                CashierUserId = order.CashierUserId,
                CashierUsername = order.CashierUsername,
                CashierRole = order.CashierRole
            };
            AppendPosShiftOrderRecord(record);
        }

        private void TrackCompletedAppDeliveryShiftOrder(AppDeliveryOrderRecord order)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.Id)) return;
            var subtotal = GetStoredAppDeliveryOrderSubtotal(order);
            var deliveryFee = GetStoredAppDeliveryOrderDeliveryFee(order);
            var total = GetStoredAppDeliveryOrderTotal(order);
            var record = new PosShiftOrderRecord
            {
                BackendOrderId = order.Id.Trim(),
                SourceKind = "APP",
                ChannelKind = "DELIVERY",
                OrderNo = order.No,
                DisplayRef = order.No > 0 ? order.No.ToString() : order.Id,
                CustomerName = order.Name ?? string.Empty,
                CustomerPhone = order.Phone ?? string.Empty,
                Subtotal = subtotal < 0 ? 0 : subtotal,
                DeliveryFee = deliveryFee < 0 ? 0 : deliveryFee,
                Total = total < 0 ? 0 : total,
                CreatedAtMillis = order.CreatedAt > 0 ? order.CreatedAt : NowUnixMs(),
                FinalizedAtMillis = NowUnixMs(),
                CashierUserId = GetCurrentSessionUserId(),
                CashierUsername = GetCurrentSessionDisplayName(),
                CashierRole = GetCurrentBackendActorRole()
            };
            AppendPosShiftOrderRecord(record);
        }

        private void TrackCompletedAppPickupShiftOrder(AppPickupOrderRecord order)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.Id)) return;
            var total = GetAppOrderItemsTotal(order.Items);
            var record = new PosShiftOrderRecord
            {
                BackendOrderId = order.Id.Trim(),
                SourceKind = "APP",
                ChannelKind = "PICKUP",
                OrderNo = order.No,
                DisplayRef = order.No > 0 ? order.No.ToString() : order.Id,
                CustomerName = order.Name ?? string.Empty,
                CustomerPhone = order.Phone ?? string.Empty,
                Subtotal = total < 0 ? 0 : total,
                DeliveryFee = 0,
                Total = total < 0 ? 0 : total,
                CreatedAtMillis = order.CreatedAt > 0 ? order.CreatedAt : NowUnixMs(),
                FinalizedAtMillis = NowUnixMs(),
                CashierUserId = GetCurrentSessionUserId(),
                CashierUsername = GetCurrentSessionDisplayName(),
                CashierRole = GetCurrentBackendActorRole()
            };
            AppendPosShiftOrderRecord(record);
        }

        private FlowDocument BuildAppDeliveryKitchenReceiptDocument(AppDeliveryOrderRecord order)
        {
            var doc = new FlowDocument { FontFamily = new FontFamily("Tahoma"), FontSize = 12, FlowDirection = FlowDirection.RightToLeft };
            doc.Blocks.Add(new Paragraph(new Run("فاتورة المطبخ (App Delivery)")) { FontSize = 18, FontWeight = FontWeights.Black, Margin = new Thickness(0, 0, 0, 6) });
            doc.Blocks.Add(new Paragraph(new Run("رقم الطلب: " + (order == null ? 0 : order.No) + " — الوقت: " + DateTime.Now.ToString("g", _arEg))) { Margin = new Thickness(0, 0, 0, 4) });
            doc.Blocks.Add(new Paragraph(new Run("الاسم: " + (order == null || string.IsNullOrWhiteSpace(order.Name) ? "-" : order.Name) + " — الهاتف: " + (order == null || string.IsNullOrWhiteSpace(order.Phone) ? "-" : order.Phone))) { Margin = new Thickness(0, 0, 0, 8) });

            var table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(380) });
            table.Columns.Add(new TableColumn { Width = new GridLength(90) });
            var g = new TableRowGroup();
            table.RowGroups.Add(g);
            var hr = new TableRow();
            hr.Cells.Add(MakeDeliveryPrintCell("الصنف", true, false, false));
            hr.Cells.Add(MakeDeliveryPrintCell("الكمية", true, true, true));
            g.Rows.Add(hr);

            if (order != null && order.Items != null)
            {
                for (int i = 0; i < order.Items.Count; i++)
                {
                    var it = order.Items[i];
                    if (it == null) continue;
                    var r = new TableRow();
                    r.Cells.Add(MakeDeliveryPrintCell(it.Name, false, false, false));
                    r.Cells.Add(MakeDeliveryPrintCell(it.Qty.ToString(), false, true, true));
                    g.Rows.Add(r);
                }
            }

            doc.Blocks.Add(table);
            var subtotal = GetStoredAppDeliveryOrderSubtotal(order);
            var deliveryFee = GetStoredAppDeliveryOrderDeliveryFee(order);
            var total = GetStoredAppDeliveryOrderTotal(order);
            doc.Blocks.Add(new Paragraph(new Run("الإجمالي قبل التوصيل: " + subtotal)) { FontWeight = FontWeights.Black, Margin = new Thickness(0, 10, 0, 0) });
            doc.Blocks.Add(new Paragraph(new Run("خدمة التوصيل: " + deliveryFee)) { FontWeight = FontWeights.Black, Margin = new Thickness(0, 2, 0, 0) });
            doc.Blocks.Add(new Paragraph(new Run("الإجمالي الكلي: " + total)) { FontWeight = FontWeights.Black, Margin = new Thickness(0, 2, 0, 0) });
            return doc;
        }

        private FlowDocument BuildAppDeliveryDriverReceiptDocument(AppDeliveryOrderRecord order, DeliveryDriverRecord driver)
        {
            var doc = new FlowDocument { FontFamily = new FontFamily("Tahoma"), FontSize = 12, FlowDirection = FlowDirection.RightToLeft };
            doc.Blocks.Add(new Paragraph(new Run("فاتورة السائق (App Delivery)")) { FontSize = 18, FontWeight = FontWeights.Black, Margin = new Thickness(0, 0, 0, 6) });
            doc.Blocks.Add(new Paragraph(new Run("السائق: " + (driver == null ? "-" : driver.Name) + " — هاتف السائق: " + (driver == null ? "-" : driver.Phone))) { Margin = new Thickness(0, 0, 0, 4) });
            doc.Blocks.Add(new Paragraph(new Run("رقم الطلب: " + (order == null ? 0 : order.No) + " — الوقت: " + DateTime.Now.ToString("g", _arEg))) { Margin = new Thickness(0, 0, 0, 4) });
            doc.Blocks.Add(new Paragraph(new Run("هاتف العميل: " + (order == null || string.IsNullOrWhiteSpace(order.Phone) ? "-" : order.Phone))) { Margin = new Thickness(0, 0, 0, 8) });

            var table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(380) });
            table.Columns.Add(new TableColumn { Width = new GridLength(90) });
            var g = new TableRowGroup();
            table.RowGroups.Add(g);
            var hr = new TableRow();
            hr.Cells.Add(MakeDeliveryPrintCell("الصنف", true, false, false));
            hr.Cells.Add(MakeDeliveryPrintCell("الكمية", true, true, true));
            g.Rows.Add(hr);

            if (order != null && order.Items != null)
            {
                for (int i = 0; i < order.Items.Count; i++)
                {
                    var it = order.Items[i];
                    if (it == null) continue;
                    var r = new TableRow();
                    r.Cells.Add(MakeDeliveryPrintCell(it.Name, false, false, false));
                    r.Cells.Add(MakeDeliveryPrintCell(it.Qty.ToString(), false, true, true));
                    g.Rows.Add(r);
                }
            }

            doc.Blocks.Add(table);
            var subtotal = GetStoredAppDeliveryOrderSubtotal(order);
            var deliveryFee = GetStoredAppDeliveryOrderDeliveryFee(order);
            var total = GetStoredAppDeliveryOrderTotal(order);
            doc.Blocks.Add(new Paragraph(new Run("الإجمالي قبل التوصيل: " + subtotal)) { FontWeight = FontWeights.Black, Margin = new Thickness(0, 10, 0, 0) });
            doc.Blocks.Add(new Paragraph(new Run("خدمة التوصيل: " + deliveryFee)) { FontWeight = FontWeights.Black, Margin = new Thickness(0, 2, 0, 0) });
            doc.Blocks.Add(new Paragraph(new Run("الإجمالي الكلي: " + total)) { FontWeight = FontWeights.Black, Margin = new Thickness(0, 2, 0, 0) });
            return doc;
        }

        private FlowDocument BuildAppPickupPartnerReceiptDocument(AppPickupOrderRecord order)
        {
            var doc = new FlowDocument { FontFamily = new FontFamily("Tahoma"), FontSize = 12, FlowDirection = FlowDirection.RightToLeft };
            doc.Blocks.Add(new Paragraph(new Run("فاتورة استلام من الفرع (App Pickup)")) { FontSize = 18, FontWeight = FontWeights.Black, Margin = new Thickness(0, 0, 0, 6) });
            doc.Blocks.Add(new Paragraph(new Run("رقم الطلب: " + order.No + " — الوقت: " + DateTime.Now.ToString("g", _arEg))) { Margin = new Thickness(0, 0, 0, 4) });
            doc.Blocks.Add(new Paragraph(new Run("الاسم: " + (string.IsNullOrWhiteSpace(order.Name) ? "-" : order.Name) + " — الهاتف: " + (string.IsNullOrWhiteSpace(order.Phone) ? "-" : order.Phone))) { Margin = new Thickness(0, 0, 0, 8) });

            var table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(320) });
            table.Columns.Add(new TableColumn { Width = new GridLength(70) });
            table.Columns.Add(new TableColumn { Width = new GridLength(90) });
            table.Columns.Add(new TableColumn { Width = new GridLength(100) });
            var g = new TableRowGroup();
            table.RowGroups.Add(g);

            var hr = new TableRow();
            hr.Cells.Add(MakeDeliveryPrintCell("الصنف", true, false, false));
            hr.Cells.Add(MakeDeliveryPrintCell("الكمية", true, true, true));
            hr.Cells.Add(MakeDeliveryPrintCell("السعر", true, true, true));
            hr.Cells.Add(MakeDeliveryPrintCell("الإجمالي", true, true, true));
            g.Rows.Add(hr);

            if (order.Items != null)
            {
                for (int i = 0; i < order.Items.Count; i++)
                {
                    var it = order.Items[i];
                    if (it == null) continue;
                    var r = new TableRow();
                    r.Cells.Add(MakeDeliveryPrintCell(it.Name, false, false, false));
                    r.Cells.Add(MakeDeliveryPrintCell(it.Qty.ToString(), false, true, true));
                    r.Cells.Add(MakeDeliveryPrintCell(it.Price.ToString(), false, true, true));
                    r.Cells.Add(MakeDeliveryPrintCell((it.Qty * it.Price).ToString(), false, true, true));
                    g.Rows.Add(r);
                }
            }

            doc.Blocks.Add(table);
            doc.Blocks.Add(new Paragraph(new Run("الإجمالي: " + GetAppOrderItemsTotal(order.Items))) { FontWeight = FontWeights.Black, Margin = new Thickness(0, 10, 0, 0) });
            return doc;
        }

        private void OpenDeliveryShiftOverlay()
        {
            if (DeliveryShiftOverlay == null) return;
            RebuildDeliveryShiftFilterCombo();
            RenderDeliveryShiftRows();
            DeliveryShiftOverlay.Visibility = Visibility.Visible;
        }

        private void CloseDeliveryShiftOverlay()
        {
            if (DeliveryShiftOverlay != null) DeliveryShiftOverlay.Visibility = Visibility.Collapsed;
        }

        private void RenderDeliveryShiftRows()
        {
            if (DeliveryShiftRowsPanel == null) return;
            DeliveryShiftRowsPanel.Children.Clear();

            var filteredShiftOrders = GetFilteredDeliveryShiftOrders();

            if (filteredShiftOrders.Count == 0)
            {
                DeliveryShiftRowsPanel.Children.Add(new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDDDDDD")),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(10),
                    Child = new TextBlock
                    {
                        Text = "لا توجد أوردرات.",
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FlowDirection = FlowDirection.RightToLeft
                    }
                });
                return;
            }

            for (int i = 0; i < filteredShiftOrders.Count; i++)
            {
                var o = filteredShiftOrders[i];
                var row = new Grid
                {
                    Background = Brushes.White
                };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                row.Children.Add(CreateDeliveryShiftCell(o.No.ToString(), FlowDirection.LeftToRight, TextAlignment.Center, 0));
                row.Children.Add(CreateDeliveryShiftCell(string.IsNullOrWhiteSpace(o.ShiftTimeText) ? "-" : o.ShiftTimeText, FlowDirection.RightToLeft, TextAlignment.Center, 1));
                row.Children.Add(CreateDeliveryShiftCell(string.IsNullOrWhiteSpace(o.CustomerPhone) ? "-" : o.CustomerPhone, FlowDirection.LeftToRight, TextAlignment.Center, 2));
                row.Children.Add(CreateDeliveryShiftCell(o.Driver != null ? o.Driver.Name : "-", FlowDirection.RightToLeft, TextAlignment.Center, 3));
                row.Children.Add(CreateDeliveryShiftCell(o.Total.ToString(), FlowDirection.LeftToRight, TextAlignment.Center, 4));

                var actions = new StackPanel { Orientation = Orientation.Horizontal, FlowDirection = FlowDirection.RightToLeft, Margin = new Thickness(4), HorizontalAlignment = HorizontalAlignment.Right };
                if (_isAdminMode)
                {
                var btnDriver = new Button { Content = "فاتورة السائق", Margin = new Thickness(2), Padding = new Thickness(8, 4, 8, 4), FontWeight = FontWeights.Black, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF666666")) };
                var btnKitchen = new Button { Content = "فاتورة المطبخ", Margin = new Thickness(2), Padding = new Thickness(8, 4, 8, 4), FontWeight = FontWeights.Black, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF3A7BD5")), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDBEAFE")) };
                var btnBoth = new Button { Content = "طباعة الاثنين", Margin = new Thickness(2), Padding = new Thickness(8, 4, 8, 4), FontWeight = FontWeights.Black, BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF666666")) };
                var captured = o;
                btnDriver.Click += delegate { if (captured.Driver != null) TryPrintFlowDocument(BuildDeliveryDriverDocument(captured, captured.Driver)); };
                btnKitchen.Click += delegate { TryPrintFlowDocument(BuildDeliveryKitchenDocument(captured)); };
                btnBoth.Click += delegate { TryPrintFlowDocument(BuildDeliveryKitchenDocument(captured)); if (captured.Driver != null) TryPrintFlowDocument(BuildDeliveryDriverDocument(captured, captured.Driver)); };
                actions.Children.Add(btnDriver);
                actions.Children.Add(btnKitchen);
                actions.Children.Add(btnBoth);
                }
                var actionsBorder = new Border { BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDDDDDD")), BorderThickness = new Thickness(0, 0, 0, 1), Child = actions };
                Grid.SetColumn(actionsBorder, 5);
                row.Children.Add(actionsBorder);

                DeliveryShiftRowsPanel.Children.Add(row);
            }
        }

        private Border CreateDeliveryShiftCell(string text, FlowDirection direction, TextAlignment align, int col)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDDDDDD")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(8)
            };
            border.Child = new TextBlock
            {
                Text = text ?? string.Empty,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                FlowDirection = direction,
                TextAlignment = align,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(border, col);
            return border;
        }

        private void DeliveryShiftBackButton_Click(object sender, RoutedEventArgs e)
        {
            CloseDeliveryShiftOverlay();
        }

        private void DeliveryDriversOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ReferenceEquals(e.OriginalSource, DeliveryDriversOverlay)) CloseDeliveryDriversOverlay();
        }

        private void DeliveryDriversModalBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // keep overlay open when clicking inside modal
        }

        private void DeliveryDriversCloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseDeliveryDriversOverlay();
        }

        private OrderPrintData BuildDeliveryOrderPrintData(DeliveryOrderRecord order)
        {
            if (order == null) return new OrderPrintData();

            var addr = order.Address;
            return new OrderPrintData
            {
                OrderId = order.Id,
                BackendOrderId = order.Id,
                OrderNo = order.No,
                OrderType = "DELIVERY",
                DateText = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                CashierName = order.CashierUsername ?? string.Empty,
                CustomerName = order.CustomerName ?? string.Empty,
                CustomerPhone = order.CustomerPhone ?? string.Empty,
                District = addr != null ? (addr.District ?? order.District ?? string.Empty) : (order.District ?? string.Empty),
                AddressBlock = addr != null ? addr.Block ?? string.Empty : string.Empty,
                AddressStreet = addr != null ? addr.Street ?? string.Empty : string.Empty,
                AddressBuilding = addr != null ? addr.Building ?? string.Empty : string.Empty,
                AddressApartment = addr != null ? addr.Apartment ?? string.Empty : string.Empty,
                AddressFloor = addr != null ? addr.Floor ?? string.Empty : string.Empty,
                AddressNote = addr != null ? addr.Note ?? string.Empty : string.Empty,
                Subtotal = order.Subtotal,
                DeliveryFee = order.DeliveryFee,
                Discount = 0,
                Total = order.Total > 0 ? order.Total : (order.Subtotal + order.DeliveryFee),
                Items = BuildPrintItems(order.Items)
            };
        }

        private FlowDocument BuildDeliveryKitchenDocument(DeliveryOrderRecord order)
        {
            var doc = new FlowDocument { FontFamily = new FontFamily("Tahoma"), FontSize = 12, FlowDirection = FlowDirection.RightToLeft };
            doc.Blocks.Add(new Paragraph(new Run("فاتورة المطبخ")) { FontSize = 18, FontWeight = FontWeights.Black, Margin = new Thickness(0, 0, 0, 6) });
            doc.Blocks.Add(new Paragraph(new Run("رقم الفاتورة: " + order.No + " — الوقت: " + DateTime.Now.ToString("g", _arEg))) { Margin = new Thickness(0, 0, 0, 4) });
            doc.Blocks.Add(new Paragraph(new Run("هاتف: " + (order.CustomerPhone ?? "-") + " — الاسم: " + (string.IsNullOrWhiteSpace(order.CustomerName) ? "-" : order.CustomerName))) { Margin = new Thickness(0, 0, 0, 4) });
            var kitchenAddress = "العنوان: " + (order.Address == null ? (string.IsNullOrWhiteSpace(order.District) ? "-" : order.District) :
                string.Format("{0} / {1} / {2} / عمارة {3} / شقة {4}",
                    string.IsNullOrWhiteSpace(order.Address.District) ? "-" : order.Address.District,
                    string.IsNullOrWhiteSpace(order.Address.Block) ? "-" : order.Address.Block,
                    string.IsNullOrWhiteSpace(order.Address.Street) ? "-" : order.Address.Street,
                    string.IsNullOrWhiteSpace(order.Address.Building) ? "-" : order.Address.Building,
                    string.IsNullOrWhiteSpace(order.Address.Apartment) ? "-" : order.Address.Apartment));
            doc.Blocks.Add(new Paragraph(new Run(kitchenAddress)) { Margin = new Thickness(0, 0, 0, 8) });

            var table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(220) });
            table.Columns.Add(new TableColumn { Width = new GridLength(220) });
            table.Columns.Add(new TableColumn { Width = new GridLength(70) });
            var g = new TableRowGroup();
            table.RowGroups.Add(g);
            var hr = new TableRow();
            hr.Cells.Add(MakeDeliveryPrintCell("الصنف", true, false, false));
            hr.Cells.Add(MakeDeliveryPrintCell("ملاحظات", true, false, false));
            hr.Cells.Add(MakeDeliveryPrintCell("الكمية", true, true, true));
            g.Rows.Add(hr);

            if (order.Items != null)
            {
                for (int i = 0; i < order.Items.Count; i++)
                {
                    var it = order.Items[i];
                    var r = new TableRow();
                    r.Cells.Add(MakeDeliveryPrintCell(it.Desc, false, false, false));
                    r.Cells.Add(MakeDeliveryPrintCell(it.Notes, false, false, false));
                    r.Cells.Add(MakeDeliveryPrintCell(it.Qty.ToString(), false, true, true));
                    g.Rows.Add(r);
                }
            }

            doc.Blocks.Add(table);
            return doc;
        }

        private FlowDocument BuildDeliveryDriverDocument(DeliveryOrderRecord order, DeliveryDriverRecord driver)
        {
            var doc = new FlowDocument { FontFamily = new FontFamily("Tahoma"), FontSize = 12, FlowDirection = FlowDirection.RightToLeft };
            doc.Blocks.Add(new Paragraph(new Run("فاتورة السائق")) { FontSize = 18, FontWeight = FontWeights.Black, Margin = new Thickness(0, 0, 0, 6) });
            doc.Blocks.Add(new Paragraph(new Run("السائق: " + driver.Name + " — هاتف السائق: " + driver.Phone)) { Margin = new Thickness(0, 0, 0, 4) });
            doc.Blocks.Add(new Paragraph(new Run("رقم الفاتورة: " + order.No + " — الوقت: " + DateTime.Now.ToString("g", _arEg))) { Margin = new Thickness(0, 0, 0, 4) });
            doc.Blocks.Add(new Paragraph(new Run("هاتف العميل: " + (order.CustomerPhone ?? "-"))) { Margin = new Thickness(0, 0, 0, 4) });
            var driverAddress = "العنوان: " + (order.Address == null ? (string.IsNullOrWhiteSpace(order.District) ? "-" : order.District) :
                string.Format("{0} / {1} / {2} / عمارة {3} / شقة {4}",
                    string.IsNullOrWhiteSpace(order.Address.District) ? "-" : order.Address.District,
                    string.IsNullOrWhiteSpace(order.Address.Block) ? "-" : order.Address.Block,
                    string.IsNullOrWhiteSpace(order.Address.Street) ? "-" : order.Address.Street,
                    string.IsNullOrWhiteSpace(order.Address.Building) ? "-" : order.Address.Building,
                    string.IsNullOrWhiteSpace(order.Address.Apartment) ? "-" : order.Address.Apartment));
            doc.Blocks.Add(new Paragraph(new Run(driverAddress)) { Margin = new Thickness(0, 0, 0, 8) });

            var table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(400) });
            table.Columns.Add(new TableColumn { Width = new GridLength(90) });
            var g = new TableRowGroup();
            table.RowGroups.Add(g);
            var hr = new TableRow();
            hr.Cells.Add(MakeDeliveryPrintCell("الصنف", true, false, false));
            hr.Cells.Add(MakeDeliveryPrintCell("الكمية", true, true, true));
            g.Rows.Add(hr);

            if (order.Items != null)
            {
                for (int i = 0; i < order.Items.Count; i++)
                {
                    var it = order.Items[i];
                    var r = new TableRow();
                    r.Cells.Add(MakeDeliveryPrintCell(it.Desc, false, false, false));
                    r.Cells.Add(MakeDeliveryPrintCell(it.Qty.ToString(), false, true, true));
                    g.Rows.Add(r);
                }
            }

            doc.Blocks.Add(table);
            doc.Blocks.Add(new Paragraph(new Run("الإجمالي: " + order.Total)) { FontWeight = FontWeights.Black, Margin = new Thickness(0, 10, 0, 0) });
            return doc;
        }

        private TableCell MakeDeliveryPrintCell(string text, bool header, bool center, bool ltr)
        {
            var p = new Paragraph(new Run(text ?? string.Empty))
            {
                Margin = new Thickness(0),
                TextAlignment = center ? TextAlignment.Center : TextAlignment.Right,
                FlowDirection = ltr ? FlowDirection.LeftToRight : FlowDirection.RightToLeft
            };
            if (header) p.FontWeight = FontWeights.Black;
            var c = new TableCell(p) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(6) };
            if (header) c.Background = Brushes.Gainsboro;
            return c;
        }

        private enum AppDeliveryOrderStatus
        {
            Pending,
            Preparing,
            DriverAssigned,
            Done
        }

        private enum AppPickupOrderStatus
        {
            Pending,
            Accepted,
            Preparing,
            Ready,
            Delivered
        }

        private sealed class AppOrderItemRecord
        {
            public string Name { get; set; }
            public int Qty { get; set; }
            public int Price { get; set; }
        }

        private sealed class AppDeliveryOrderRecord
        {
            public string Id { get; set; }
            public string CustomerUserId { get; set; }
            public string AddressId { get; set; }
            public int No { get; set; }
            public string Name { get; set; }
            public string Phone { get; set; }
            public string District { get; set; }
            public string AddressText { get; set; }
            public DeliveryAddressRecord Address { get; set; }
            public long CreatedAt { get; set; }
            public int Subtotal { get; set; }
            public int DeliveryFee { get; set; }
            public int Total { get; set; }
            public AppDeliveryOrderStatus Status { get; set; }
            public bool KitchenPrinted { get; set; }
            public bool DriverReceiptPrinted { get; set; }
            public DeliveryDriverRecord Driver { get; set; }
            public List<AppOrderItemRecord> Items { get; set; }
        }

        private sealed class AppPickupOrderRecord
        {
            public string Id { get; set; }
            public string CustomerUserId { get; set; }
            public int No { get; set; }
            public string Name { get; set; }
            public string Phone { get; set; }
            public string District { get; set; }
            public string AddressText { get; set; }
            public long CreatedAt { get; set; }
            public AppPickupOrderStatus Status { get; set; }
            public bool PartnerReceiptPrinted { get; set; }
            public List<AppOrderItemRecord> Items { get; set; }
        }

        [DataContract]
        private class DeliveryOrderRecord
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "idempotency_key", EmitDefaultValue = false)] public string IdempotencyKey { get; set; }
            [DataMember(Name = "no")] public int No { get; set; }
            [DataMember(Name = "createdAt")] public long CreatedAt { get; set; }
            [DataMember(Name = "customerPhone")] public string CustomerPhone { get; set; }
            [DataMember(Name = "customerName")] public string CustomerName { get; set; }
            [DataMember(Name = "district")] public string District { get; set; }
            [DataMember(Name = "address")] public DeliveryAddressRecord Address { get; set; }
            [DataMember(Name = "deliveryName")] public string DeliveryName { get; set; }
            [DataMember(Name = "deliveryFee")] public int DeliveryFee { get; set; }
            [DataMember(Name = "subtotal")] public int Subtotal { get; set; }
            [DataMember(Name = "total")] public int Total { get; set; }
            [DataMember(Name = "kitchenPrinted")] public bool KitchenPrinted { get; set; }
            [DataMember(Name = "kitchenPrintedAt")] public long? KitchenPrintedAt { get; set; }
            [DataMember(Name = "status")] public string Status { get; set; }
            [DataMember(Name = "driver")] public DeliveryDriverRecord Driver { get; set; }
            [DataMember(Name = "shiftTime")] public string ShiftTimeText { get; set; }
            [DataMember(Name = "cashier_user_id", EmitDefaultValue = false)] public string CashierUserId { get; set; }
            [DataMember(Name = "cashier_username", EmitDefaultValue = false)] public string CashierUsername { get; set; }
            [DataMember(Name = "cashier_role", EmitDefaultValue = false)] public string CashierRole { get; set; }
            // [PHASE 7.5] Server-side backend ID assigned at immediate cloud INSERT
            [DataMember(Name = "backend_id", EmitDefaultValue = false)] public string BackendId { get; set; }
            [DataMember(Name = "items")] public List<DeliveryLineRecord> Items { get; set; }
        }

        [DataContract]
        private class DeliveryLineRecord
        {
            [DataMember(Name = "key")] public string Key { get; set; }
            [DataMember(Name = "desc")] public string Desc { get; set; }
            [DataMember(Name = "notes")] public string Notes { get; set; }
            [DataMember(Name = "qty")] public int Qty { get; set; }
            [DataMember(Name = "price")] public int Price { get; set; }
            [DataMember(Name = "product_id", EmitDefaultValue = false)] public string ProductId { get; set; }
            [DataMember(Name = "source_product_id", EmitDefaultValue = false)] public string SourceProductId { get; set; }
            [DataMember(Name = "option_ids", EmitDefaultValue = false)] public List<string> OptionIds { get; set; }
        }

        [DataContract]
        private class DeliveryDriverRecord
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "phone")] public string Phone { get; set; }
            [DataMember(Name = "appUserId", EmitDefaultValue = false)] public string AppUserId { get; set; }
        }
    }
}

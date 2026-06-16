using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Printing;
using Drawing = System.Drawing;
using DrawingPrinting = System.Drawing.Printing;
namespace Fale7_POS
{
    public partial class MainWindow
    {
        // =====================================================================
        // دوال الطباعة المتسلسلة للاستلام (جديدة)
        // =====================================================================
        private string BuildPickupReceiptText(TakeawayPickupRecord pickup, TakeawayOrderRecord order)
        {
            if (pickup == null && order == null) return string.Empty;

            var printData = new OrderPrintData
            {
                OrderId = pickup?.Id ?? order?.Id,
                BackendOrderId = pickup?.Id ?? order?.Id,
                OrderNo = pickup?.No ?? order?.No ?? 0,
                OrderType = "PICKUP",
                DateText = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                CashierName = GetCurrentSessionDisplayName(),
                CustomerName = pickup?.Name ?? "استلام",
                CustomerPhone = pickup?.Phone ?? string.Empty,
                Subtotal = 0,
                DeliveryFee = 0,
                Total = 0,
                Items = new List<PrintLineItem>()
            };

            if (order?.Items != null)
            {
                foreach (var item in order.Items)
                {
                    printData.Subtotal += item.Qty * item.Price;
                    printData.Items.Add(new PrintLineItem
                    {
                        Name = item.Desc,
                        Qty = item.Qty,
                        UnitPrice = item.Price,
                        Notes = string.Empty
                    });
                }
                printData.Total = printData.Subtotal;
            }

            return BuildThermalReceiptText(printData, PrintType.Pickup);
        }

        private bool PrintPickupKitchenAndReceipt(TakeawayOrderRecord order, TakeawayPickupRecord pickup)
        {
            if (order == null) return false;

            // 1. Ø¨Ù†Ø§Ø¡ ÙˆØ·Ø¨Ø§Ø¹Ø© ÙØ§ØªÙˆØ±Ø© Ø§Ù„Ù…Ø·Ø¨Ø®
            var pickupOrderKitchenData = BuildKitchenOrderPrintData(order);
            string kitchenText = BuildThermalReceiptText(pickupOrderKitchenData, PrintType.Kitchen);
            PrintThermalReceiptSilent(kitchenText);

            // 2. طباعة فاتورة الاستلام بالشكل الرسومي الجديد
            if (!PrintGraphicalTakeawayReceipt(BuildPickupOrderPrintData(pickup)))
            {
                return false;
            }

            // 3. Ø¥Ø²Ø§Ù„Ø© Ø§Ù„Ø£ÙˆØ±Ø¯Ø± Ù…Ù† Ø§Ù„Ù‚ÙˆØ§Ø¦Ù… (ÙŠØªÙ… Ø§Ø³ØªØ¯Ø¹Ø§Ø¤Ù‡Ø§ Ù…Ù† Ø§Ù„Ø®Ø§Ø±Ø¬ Ø¨Ø¹Ø¯ Ù‡Ø°Ù‡ Ø§Ù„Ø¯Ø§Ù„Ø©)
            return true;
        }

        public class ReceiptItem
        {
            public int Quantity { get; set; }
            public string ItemName { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice => Quantity * UnitPrice;
        }

        private void PrintKitchenConfirm()
        {
            if (_takeawayPickupContextActive)
            {
                // تسلسل صحيح: مطبخ ثم استلام ثم حذف
                var pickupId = _takeawayState?.ActivePickupId;
                if (string.IsNullOrWhiteSpace(pickupId))
                {
                    MessageBox.Show("لا يوجد أوردر استلام نشط.", "Fale7 POS", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var pickupOrder = FindOrderLinkedToPickup(pickupId);
                TakeawayPickupRecord pickupRecord = null;
                for (int i = 0; i < _takeawayState.Pickups.Count; i++)
                {
                    if (_takeawayState.Pickups[i]?.Id == pickupId)
                    {
                        pickupRecord = _takeawayState.Pickups[i];
                        break;
                    }
                }
                if (pickupOrder == null)
                {
                    MessageBox.Show("لا توجد أوردرات مرتبطة بهذا الاستلام.", "Fale7 POS", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // استخدام نظام الطباعة الجديد المتسلسل
            if (!pickupRecord.KitchenPrinted)
            {
                var pickupKitchenData = BuildKitchenOrderPrintData(pickupOrder);
                if (!PrintKitchenTicketWithServerCheckAsync(pickupKitchenData, _isAdminMode).GetAwaiter().GetResult())
                    return;

                pickupRecord.KitchenPrinted = true;
                pickupRecord.KitchenPrintedAt = NowUnixMs();
                PersistTakeawayState();
                RenderTakeawayAll();
                if (DeliveryView != null && DeliveryView.Visibility == Visibility.Visible)
                {
                    RenderDeliverySharedPickups();
                    RenderDeliveryReceipt();
                }
                return;
            }

            if (!PrintAndCompleteActivePickupOrder())
                return;

                // بعد الطباعة، نقوم بحذف الأوردر والاستلام من القوائم
                for (int i = _takeawayState.OpenOrders.Count - 1; i >= 0; i--)
                {
                    if (_takeawayState.OpenOrders[i]?.Id == pickupOrder.Id)
                        _takeawayState.OpenOrders.RemoveAt(i);
                }
                _pickupOrderLinks.Remove(pickupOrder.Id);

                for (int i = _takeawayState.Pickups.Count - 1; i >= 0; i--)
                {
                    if (_takeawayState.Pickups[i]?.Id == pickupId)
                        _takeawayState.Pickups.RemoveAt(i);
                }

                _takeawayPickupSelectedRowKey = null;
                _deliveryPickupSelectedRowKey = null;

                if (_takeawayState.Pickups.Count > 0)
                    _takeawayState.ActivePickupId = _takeawayState.Pickups[0].Id;
                else
                {
                    _takeawayState.ActivePickupId = null;
                    _takeawayPickupContextActive = false;
                    _deliveryPickupContextActive = false;
                }

                if (GetCurrentOrder() == null)
                    _takeawayState.CurrentOrderId = FindFirstRegularTakeawayOrderId();

                PersistTakeawayState();
                RenderTakeawayAll();
                if (DeliveryView != null && DeliveryView.Visibility == Visibility.Visible)
                {
                    RenderDeliverySharedPickups();
                    RenderDeliveryReceipt();
                }
                return;
            }

            // باقي الحالة (تيك أواي عادي)
            var o = GetActiveTakeawayContextOrder();
            if (o == null)
            {
                TryPrintFlowDocument(BuildKitchenDocument(null));
                return;
            }

            var backendRow = BuildPosOfflineTakeawayOrderInsertRow(o, null);
            bool pushedToServer;
            string syncMessage;
            if (!TryCreatePosOrderOnServerOrFallback(backendRow, out pushedToServer, out syncMessage))
            {
                MessageBox.Show(string.IsNullOrWhiteSpace(syncMessage) ? "تعذر اعتماد الطلب الآن." : syncMessage, "Fale7 POS", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // طباعة فاتورة المطبخ باستخدام النظام الجديد
            var orderKitchenData = BuildKitchenOrderPrintData(o);
            string kitchenText = BuildThermalReceiptText(orderKitchenData, PrintType.Kitchen);
            PrintThermalReceiptSilent(kitchenText);

            if (!pushedToServer)
            {
                try
                {
                    PushNewTakeawayOrderToCloudAsync(o, null).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    AppendPosRuntimeLog("TakeawayCloudPush", ex);
                }
            }
            TrackCompletedTakeawayShiftOrder(o, false, null);

            for (int i = _takeawayState.OpenOrders.Count - 1; i >= 0; i--)
                if (_takeawayState.OpenOrders[i].Id == o.Id)
                    _takeawayState.OpenOrders.RemoveAt(i);

            if (_takeawayPickupContextActive)
                _takeawayPickupSelectedRowKey = null;
            else
                _selectedRowKey = null;

            if (!_takeawayPickupContextActive)
            {
                if (string.Equals(_takeawayState.CurrentOrderId, o.Id, StringComparison.Ordinal) || GetCurrentOrder() == null)
                    _takeawayState.CurrentOrderId = FindFirstRegularTakeawayOrderId();
            }

            PersistTakeawayState();
            RenderTakeawayAll();
        }

        private bool PrintAndCompleteActivePickupOrder()
        {
            NormalizeTakeawayState();
            if (_takeawayState == null || _takeawayState.Pickups == null || string.IsNullOrWhiteSpace(_takeawayState.ActivePickupId))
            {
                MessageBox.Show("حدد أوردر استلام من الفرع أولاً.", "Fale7 POS", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            var pickupId = _takeawayState.ActivePickupId;
            TakeawayPickupRecord pickupRecord = null;
            for (int i = 0; i < _takeawayState.Pickups.Count; i++)
            {
                if (_takeawayState.Pickups[i] != null && string.Equals(_takeawayState.Pickups[i].Id, pickupId, StringComparison.Ordinal))
                {
                    pickupRecord = _takeawayState.Pickups[i];
                    break;
                }
            }

            var pickupOrder = FindOrderLinkedToPickup(pickupId);
            if (pickupRecord == null || pickupOrder == null)
            {
                MessageBox.Show("لا توجد أوردرات مرتبطة بهذا الاستلام.", "Fale7 POS", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            if (!pickupRecord.KitchenPrinted)
            {
                var kitchenData = BuildKitchenOrderPrintData(pickupOrder);
                if (!PrintKitchenTicketWithServerCheckAsync(kitchenData, _isAdminMode).GetAwaiter().GetResult())
                {
                    return false;
                }

                pickupRecord.KitchenPrinted = true;
                pickupRecord.KitchenPrintedAt = NowUnixMs();
                PersistTakeawayState();
                if (TakeawayView != null && TakeawayView.Visibility == Visibility.Visible)
                    RenderTakeawayAll();
                if (DeliveryView != null && DeliveryView.Visibility == Visibility.Visible)
                {
                    RenderDeliverySharedPickups();
                    RenderDeliveryReceipt();
                }
                return true;
            }

            if (!pickupRecord.ReceiptPrinted)
            {
                var backendRow = BuildPosOfflineTakeawayOrderInsertRow(pickupOrder, pickupRecord);
                bool pushedToServer;
                string syncMessage;
                if (!TryCreatePosOrderOnServerOrFallback(backendRow, out pushedToServer, out syncMessage))
                {
                    MessageBox.Show(string.IsNullOrWhiteSpace(syncMessage) ? "تعذر اعتماد طلب الاستلام الآن." : syncMessage, "Fale7 POS", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                if (!PrintGraphicalTakeawayReceipt(BuildPickupOrderPrintData(pickupRecord)))
                {
                    return false;
                }

                if (!pushedToServer)
                {
                    try
                    {
                        PushNewTakeawayOrderToCloudAsync(pickupOrder, pickupRecord).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        AppendPosRuntimeLog("TakeawayCloudPush", ex);
                    }
                }
                TrackCompletedTakeawayShiftOrder(pickupOrder, false, pickupRecord);

                pickupRecord.ReceiptPrinted = true;
                pickupRecord.ReceiptPrintedAt = NowUnixMs();

                for (int i = _takeawayState.OpenOrders.Count - 1; i >= 0; i--)
                {
                    if (_takeawayState.OpenOrders[i] != null && string.Equals(_takeawayState.OpenOrders[i].Id, pickupOrder.Id, StringComparison.Ordinal))
                        _takeawayState.OpenOrders.RemoveAt(i);
                }

                _pickupOrderLinks.Remove(pickupOrder.Id);

                for (int i = _takeawayState.Pickups.Count - 1; i >= 0; i--)
                {
                    if (_takeawayState.Pickups[i] != null && string.Equals(_takeawayState.Pickups[i].Id, pickupId, StringComparison.Ordinal))
                        _takeawayState.Pickups.RemoveAt(i);
                }

                _takeawayPickupSelectedRowKey = null;
                _deliveryPickupSelectedRowKey = null;

                if (_takeawayState.Pickups.Count > 0)
                    _takeawayState.ActivePickupId = _takeawayState.Pickups[0].Id;
                else
                {
                    _takeawayState.ActivePickupId = null;
                    _takeawayPickupContextActive = false;
                    _deliveryPickupContextActive = false;
                }

                if (GetCurrentOrder() == null)
                    _takeawayState.CurrentOrderId = FindFirstRegularTakeawayOrderId();

                PersistTakeawayState();
                if (TakeawayView != null && TakeawayView.Visibility == Visibility.Visible)
                    RenderTakeawayAll();
                if (DeliveryView != null && DeliveryView.Visibility == Visibility.Visible)
                {
                    RenderDeliverySharedPickups();
                    RenderDeliveryReceipt();
                }
                return true;
            }

            return true;
        }

        private const int THERMAL_WIDTH_58MM = 32;
        private const int THERMAL_WIDTH_80MM = 48;
        private const string BrandName = "ÙØ§Ù„Ø­ Ø§Ø¨Ùˆ Ø§Ù„Ø¹Ù†Ø¨Ù‡";

        private string _cachedDeviceId;

        private int ThermalWidth => THERMAL_WIDTH_80MM;

        private string BuildThermalReceiptText(OrderPrintData order, PrintType printType)
        {
            if (order == null) return string.Empty;

            var sb = new StringBuilder();
            var w = ThermalWidth;
            var sep = new string('-', w);
            var dSep = new string('=', w);

            sb.Append(EscPosDoubleHeight(true));
            sb.Append(EscPosCenter());
            sb.AppendLine(BrandName);
            sb.Append(EscPosDoubleHeight(false));
            sb.Append(EscPosCenter());
            sb.AppendLine(sep);

            var invoiceTitle = printType == PrintType.Kitchen
                ? (order.KitchenPrintCount > 1 ? "*** ÙØ§ØªÙˆØ±Ø© Ù…Ø·Ø¨Ø® - Ø¨Ø¯ÙŠÙ„Ø© ***" : "ÙØ§ØªÙˆØ±Ø© Ø§Ù„Ù…Ø·Ø¨Ø®")
                : printType == PrintType.Pickup ? "ÙØ§ØªÙˆØ±Ø© ØªÙŠÙƒ Ø£ÙˆØ§ÙŠ" : "ÙØ§ØªÙˆØ±Ø© Ø§Ù„Ø¹Ù…ÙŠÙ„";

            if (printType == PrintType.Kitchen && order.KitchenPrintCount > 1)
            {
                sb.Append(EscPosBold(true));
                sb.Append(EscPosCenter());
                sb.AppendLine(CenterText("[Ù†Ø³Ø®Ø© Ø¨Ø¯ÙŠÙ„Ø© - Ø£ÙˆØ±Ø¯Ø± #" + order.OrderNo.ToString() + "]", w));
                sb.Append(EscPosBold(false));
            }

            sb.Append(EscPosBold(true));
            sb.AppendLine(CenterText(invoiceTitle, w));
            sb.Append(EscPosBold(false));
            sb.AppendLine(sep);

            sb.AppendLine(PairLine("Ø±Ù‚Ù… Ø§Ù„Ø£ÙˆØ±Ø¯Ø±:", "#" + order.OrderNo.ToString(), w));
            sb.AppendLine(PairLine("Ø§Ù„Ù†ÙˆØ¹:", OrderTypeAr(order.OrderType), w));
            sb.AppendLine(PairLine("Ø§Ù„ØªØ§Ø±ÙŠØ®:", order.DateText ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm"), w));
            if (!string.IsNullOrWhiteSpace(order.CashierName)) sb.AppendLine(PairLine("Ø§Ù„ÙƒØ§Ø´ÙŠØ±:", TruncateText(order.CashierName, w / 2), w));
            if (!string.IsNullOrWhiteSpace(order.CustomerName)) sb.AppendLine(PairLine("Ø§Ù„Ø¹Ù…ÙŠÙ„:", TruncateText(order.CustomerName, w - 10), w));
            if (!string.IsNullOrWhiteSpace(order.CustomerPhone)) sb.AppendLine(PairLine("Ø§Ù„Ù‡Ø§ØªÙ:", order.CustomerPhone, w));

            if (string.Equals(order.OrderType, "DELIVERY", StringComparison.OrdinalIgnoreCase) || string.Equals(order.OrderType, "POS_DELIVERY", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine(sep);
                if (!string.IsNullOrWhiteSpace(order.District)) sb.AppendLine(PairLine("Ø§Ù„Ù…Ù†Ø·Ù‚Ø©:", TruncateText(order.District, w - 10), w));
                if (!string.IsNullOrWhiteSpace(order.AddressBlock)) sb.AppendLine(PairLine("Ù…Ø¬Ø§ÙˆØ±Ø©:", order.AddressBlock, w));
                if (!string.IsNullOrWhiteSpace(order.AddressStreet)) sb.AppendLine(PairLine("Ø´Ø§Ø±Ø¹:", TruncateText(order.AddressStreet, w - 8), w));
                if (!string.IsNullOrWhiteSpace(order.AddressBuilding)) sb.AppendLine(PairLine("Ø¹Ù…Ø§Ø±Ø©:", order.AddressBuilding, w));
                if (!string.IsNullOrWhiteSpace(order.AddressApartment)) sb.AppendLine(PairLine("Ø´Ù‚Ø©:", order.AddressApartment, w));
                if (!string.IsNullOrWhiteSpace(order.AddressFloor)) sb.AppendLine(PairLine("Ø¯ÙˆØ±:", order.AddressFloor, w));
                if (!string.IsNullOrWhiteSpace(order.AddressNote)) sb.AppendLine(PairLine("Ù…Ù„Ø§Ø­Ø¸Ø©:", TruncateText(order.AddressNote, w - 10), w));
            }

            sb.AppendLine(sep);
            sb.AppendLine(ThermalItemHeader(w));
            sb.AppendLine(sep);

            if (order.Items != null)
            {
                foreach (var item in order.Items)
                {
                    if (item == null) continue;
                    var name = TruncateText(item.Name ?? string.Empty, w - 14);
                    sb.AppendLine(ThermalItemLine(name, item.Qty, item.UnitPrice, w));
                    if (!string.IsNullOrWhiteSpace(item.Notes))
                    {
                        sb.AppendLine("  > " + TruncateText(item.Notes, w - 5));
                    }
                }
            }

            if (printType == PrintType.Customer || printType == PrintType.Pickup)
            {
                sb.AppendLine(sep);
                sb.AppendLine(PairLine("Ø§Ù„Ù…Ø¬Ù…ÙˆØ¹:", FormatPrice(order.Subtotal), w));
                if (order.DeliveryFee > 0) sb.AppendLine(PairLine("Ø±Ø³ÙˆÙ… Ø§Ù„ØªÙˆØµÙŠÙ„:", FormatPrice(order.DeliveryFee), w));
                if (order.Discount > 0) sb.AppendLine(PairLine("Ø§Ù„Ø®ØµÙ…:", "-" + FormatPrice(order.Discount), w));
                sb.AppendLine(dSep);
                sb.Append(EscPosBold(true));
                sb.AppendLine(PairLine("Ø§Ù„Ø¥Ø¬Ù…Ø§Ù„ÙŠ:", FormatPrice(order.Total), w));
                sb.Append(EscPosBold(false));
                sb.AppendLine(dSep);
            }

            sb.AppendLine(CenterText("Ø´ÙƒØ±Ø§Ù‹ Ù„Ø²ÙŠØ§Ø±ØªÙƒÙ…", w));
            sb.AppendLine(CenterText(BrandName, w));
            sb.AppendLine();
            sb.AppendLine();
            sb.Append(EscPosCut());
            return sb.ToString();
        }

        private void PrintThermalReceiptSilent(string text)
        {
            PrintThermalReceipt(text);
        }

        private void PrintThermalReceipt(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            try
            {
                var doc = BuildPlainTextReceiptDocument(text);
                TryPrintFlowDocument(doc);
            }
            catch (Exception ex)
            {
                AppendPosRuntimeLog("PrintThermalReceipt", ex);
            }
        }

        private OrderPrintData BuildKitchenOrderPrintData(TakeawayOrderRecord order)
        {
            if (order == null) return new OrderPrintData();

            var subtotal = GetOrderSubtotal(order);
            return new OrderPrintData
            {
                OrderId = order.Id,
                BackendOrderId = order.Id,
                OrderNo = order.No,
                OrderType = "TAKEAWAY",
                DateText = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                CashierName = GetCurrentSessionDisplayName(),
                CustomerName = string.Empty,
                CustomerPhone = string.Empty,
                Subtotal = subtotal,
                DeliveryFee = 0,
                Discount = 0,
                Total = subtotal,
                Items = BuildPrintItems(order.Items)
            };
        }

        private int GetOrderSubtotal(TakeawayOrderRecord order)
        {
            if (order == null || order.Items == null) return 0;
            var subtotal = 0;
            for (int i = 0; i < order.Items.Count; i++)
            {
                var item = order.Items[i];
                if (item == null) continue;
                subtotal += item.Qty * item.Price;
            }
            return subtotal;
        }

        private OrderPrintData BuildPickupOrderPrintData(TakeawayPickupRecord pickup)
        {
            if (pickup == null) return new OrderPrintData();

            var linkedOrder = FindOrderLinkedToPickup(pickup.Id);
            var subtotal = GetOrderSubtotal(linkedOrder);
            return new OrderPrintData
            {
                OrderId = pickup.Id,
                BackendOrderId = pickup.Id,
                OrderNo = pickup.No,
                OrderType = "PICKUP",
                DateText = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                CashierName = GetCurrentSessionDisplayName(),
                CustomerName = !string.IsNullOrWhiteSpace(pickup.Name) ? pickup.Name : "Ø§Ø³ØªÙ„Ø§Ù…",
                CustomerPhone = pickup.Phone ?? string.Empty,
                Subtotal = subtotal,
                DeliveryFee = 0,
                Discount = 0,
                Total = subtotal,
                Items = BuildPrintItems(linkedOrder != null ? linkedOrder.Items : null)
            };
        }

        private List<PrintLineItem> BuildPrintItems<T>(List<T> source)
        {
            var result = new List<PrintLineItem>();
            if (source == null) return result;

            foreach (var item in source)
            {
                if (item == null) continue;
                var type = item.GetType();
                var name = GetStringProperty(type, item, "Desc", "Name");
                var notes = GetStringProperty(type, item, "Notes");
                var qty = GetIntProperty(type, item, "Qty");
                var price = GetDecimalProperty(type, item, "Price", "UnitPrice");

                result.Add(new PrintLineItem
                {
                    Name = name,
                    Qty = qty <= 0 ? 1 : qty,
                    UnitPrice = price,
                    Notes = notes
                });
            }

            return result;
        }

        private static string StripPrinterControlChars(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch == '\r' || ch == '\n' || ch == '\t' || ch >= ' ') sb.Append(ch);
            }
            return sb.ToString();
        }

        private string GetStringProperty(Type type, object instance, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var prop = type.GetProperty(names[i]);
                if (prop == null) continue;
                var value = prop.GetValue(instance, null) as string;
                if (!string.IsNullOrWhiteSpace(value)) return value;
            }
            return string.Empty;
        }

        private int GetIntProperty(Type type, object instance, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var prop = type.GetProperty(names[i]);
                if (prop == null) continue;
                var value = prop.GetValue(instance, null);
                if (value == null) continue;
                try { return Convert.ToInt32(value); } catch { }
            }
            return 1;
        }

        private decimal GetDecimalProperty(Type type, object instance, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var prop = type.GetProperty(names[i]);
                if (prop == null) continue;
                var value = prop.GetValue(instance, null);
                if (value == null) continue;
                try { return Convert.ToDecimal(value); } catch { }
            }
            return 0m;
        }

        private string CenterText(string text, int width)
        {
if (string.IsNullOrEmpty(text)) return string.Empty;
if (text.Length >= width) return text;
var pad = (width - text.Length) / 2;
return new string(' ', pad) + text;
}
        private int GetReceiptWidth()
        {
            try
            {
                return Properties.Settings.Default["PrinterWidth80mm"] is bool width80 && width80 ? 42 : 32;
            }
            catch
            {
                return 32;
            }
        }

        private string BuildReceiptSeparator()
        {
            return new string('-', GetReceiptWidth());
        }

        private string SanitizeReceiptText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return (text ?? string.Empty)
                .Replace("\t", " ")
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Trim();
        }

        private string TrimReceiptText(string text, int maxLength)
        {
            var value = SanitizeReceiptText(text);
            if (maxLength <= 0) return string.Empty;
            if (value.Length <= maxLength) return value;
            if (maxLength <= 2) return value.Substring(0, maxLength);
            return value.Substring(0, maxLength - 2) + "..";
        }

        private string CenterReceiptText(string text)
        {
            var value = SanitizeReceiptText(text);
            var w = GetReceiptWidth();
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            if (value.Length >= w) return value;
            var spaces = (w - value.Length) / 2;
            return new string(' ', Math.Max(0, spaces)) + value;
        }

        private string BuildReceiptLeftRightRow(string rightPart, string leftPart)
        {
            var w = GetReceiptWidth();
            var rightValue = SanitizeReceiptText(rightPart);
            var leftValue = SanitizeReceiptText(leftPart);

            if (string.IsNullOrWhiteSpace(rightValue) && string.IsNullOrWhiteSpace(leftValue))
            {
                return string.Empty;
            }

            if (rightValue.Length + leftValue.Length >= w)
            {
                var maxRight = Math.Max(5, w - leftValue.Length - 2);
                if (rightValue.Length > maxRight)
                {
                    rightValue = TrimReceiptText(rightValue, maxRight);
                }

                if (rightValue.Length + leftValue.Length >= w)
                {
                    var maxLeft = Math.Max(5, w - rightValue.Length - 2);
                    if (leftValue.Length > maxLeft)
                    {
                        leftValue = TrimReceiptText(leftValue, maxLeft);
                    }
                }
            }

            var spaceCount = w - (rightValue.Length + leftValue.Length);
            if (spaceCount < 1) spaceCount = 1;
            return rightValue + new string(' ', spaceCount) + leftValue;
        }

        private string FormatReceiptInt(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private string FormatReceiptLong(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private string FormatReceiptDecimal(decimal value, string format = "0.###")
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private string FormatReceiptDouble(double value, string format = "0.###")
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private string FormatReceiptDate(DateTime value, string format = "dd/MM/yyyy HH:mm")
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private string EnsureEnglishDigits(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (ch >= '٠' && ch <= '٩')
                {
                    sb.Append((char)('0' + (ch - '٠')));
                    continue;
                }

                if (ch >= '۰' && ch <= '۹')
                {
                    sb.Append((char)('0' + (ch - '۰')));
                    continue;
                }

                sb.Append(ch);
            }
            return sb.ToString();
        }

        private string BuildReceiptTitleBlock(string title)
        {
            return CenterReceiptText(title);
        }

        private string BuildReceiptSubtitleBlock(string subtitle)
        {
            return CenterReceiptText(subtitle);
        }

        private string BuildReceiptNumericLine(string label, int value)
        {
            return BuildReceiptLeftRightRow(label, FormatReceiptInt(value));
        }

        private string BuildReceiptNumericLine(string label, decimal value)
        {
            return BuildReceiptLeftRightRow(label, FormatReceiptDecimal(value));
        }

        private string BuildReceiptItemRow(string itemName, int qty, int unitPrice)
        {
            var leftPart = FormatReceiptInt(qty) + " x " + FormatReceiptInt(unitPrice);
            var rightPart = SanitizeReceiptText(itemName);
            return BuildReceiptLeftRightRow(rightPart, leftPart);
        }

        private string BuildReceiptItemRow(string itemName, int qty, decimal unitPrice)
        {
            var leftPart = FormatReceiptInt(qty) + " x " + FormatReceiptDecimal(unitPrice);
            var rightPart = SanitizeReceiptText(itemName);
            return BuildReceiptLeftRightRow(rightPart, leftPart);
        }

        private static ReceiptPrintItemData CreateReceiptPrintItem(
            string name,
            int qty,
            decimal unitPrice,
            string notes = null,
            bool isDivider = false)
        {
            var safeQty = qty <= 0 ? 1 : qty;
            var safePrice = unitPrice < 0 ? 0 : unitPrice;
            return new ReceiptPrintItemData
            {
                Name = name ?? string.Empty,
                Qty = safeQty,
                UnitPrice = safePrice,
                LineTotal = safeQty * safePrice,
                Notes = notes ?? string.Empty,
                IsDivider = isDivider
            };
        }

        private static ReceiptPrintData CreateEmptyReceiptPrintData()
        {
            return new ReceiptPrintData
            {
                Items = new List<ReceiptPrintItemData>(),
                FooterLines = new List<string>(),
                Metadata = new ReceiptPrintMetadataData()
            };
        }
private string PairLine(string label, string value, int width)
{
if (string.IsNullOrEmpty(label)) return value ?? string.Empty;
value = value ?? string.Empty;
var available = width - label.Length - 1;
if (available < 1) return label;
if (value.Length > available) value = value.Substring(0, available);
var spaces = width - label.Length - value.Length;
return label + new string(' ', Math.Max(1, spaces)) + value;
}
private string TruncateText(string text, int maxLen)
{
if (string.IsNullOrEmpty(text) || maxLen <= 0) return string.Empty;
if (text.Length <= maxLen) return text;
return maxLen > 2 ? text.Substring(0, maxLen - 2) + ".." : text.Substring(0, maxLen);
}
private string ThermalItemHeader(int width)
{
var nameH = "O\u0015U,O�U+U?";
var qtyH = "O\u0015U,U�U.USOc";
var unitH = "O\u0015U,O3O1O�";
var totalH = "O\u0015U,O�O�U.O\u0015U,US";
var spaces = width - nameH.Length - qtyH.Length - unitH.Length - totalH.Length - 3;
return nameH + new string(' ', Math.Max(1, spaces)) + qtyH + " " + unitH + " " + totalH;
}
private string ThermalItemLine(string name, int qty, decimal price, int width)
{
var unit = FormatPrice(price);
var total = FormatPrice(qty * price);
var qtyStr = qty.ToString(CultureInfo.InvariantCulture);
var safeName = name ?? string.Empty;
var maxName = width - qtyStr.Length - unit.Length - total.Length - 4;
var shortName = safeName.Length > maxName && maxName > 0 ? safeName.Substring(0, maxName) : safeName;
var spaces = width - shortName.Length - qtyStr.Length - unit.Length - total.Length - 3;
return shortName + new string(' ', Math.Max(1, spaces)) + qtyStr + " " + unit + " " + total;
}
private string FormatPrice(decimal amount)
=> amount.ToString("0.000", CultureInfo.InvariantCulture) + " O_.U�";
private string OrderTypeAr(string type)
{
switch ((type ?? string.Empty).ToUpperInvariant())
{
case "DELIVERY":
case "POS_DELIVERY":
return "O�U^O�USU,";
case "PICKUP":
case "POS_PICKUP":
return "O\u0015O3O�U,O\u0015U. U.U+ O\u0015U,U?O�O1";
case "TAKEAWAY":
case "POS_TAKEAWAY":
return "O�USU� O�U^O\u0015US";
default:
return type ?? string.Empty;
}
}
private string EscPosDoubleHeight(bool on) => on ? "\x1B\x21\x10" : "\x1B\x21\x00";
private string EscPosBold(bool on) => on ? "\x1B\x45\x01" : "\x1B\x45\x00";
private string EscPosCenter() => "\x1B\x61\x01";
private string EscPosCut() => "\x1D\x56\x41\x00";
    }
}

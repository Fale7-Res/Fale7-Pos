// =============================================================================
// PosShiftManager.cs
// مسار الملف: POS-Windows/PosShiftManager.cs
// يُضاف كـ partial class داخل MainWindow
// يعالج: الوردية المشتركة بين الجهازين + Realtime Sync + Order Edit Lock
// =============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Fale7_POS
{
    // =========================================================================
    // Data Contracts
    // =========================================================================

    [DataContract]
    public sealed class PosShiftRecord
    {
        [DataMember(Name = "id")]                  public string Id                { get; set; }
        [DataMember(Name = "branch_id")]           public string BranchId          { get; set; }
        [DataMember(Name = "status")]              public string Status            { get; set; }
        [DataMember(Name = "opened_at")]           public string OpenedAt          { get; set; }
        [DataMember(Name = "closed_at")]           public string ClosedAt          { get; set; }
        [DataMember(Name = "opened_by_user_id")]   public string OpenedByUserId    { get; set; }
        [DataMember(Name = "opening_cash")]        public decimal OpeningCash      { get; set; }
        [DataMember(Name = "closing_cash")]        public decimal? ClosingCash     { get; set; }
        [DataMember(Name = "total_cash_orders")]   public decimal TotalCashOrders  { get; set; }
        [DataMember(Name = "total_card_orders")]   public decimal TotalCardOrders  { get; set; }
        [DataMember(Name = "note")]                public string Note             { get; set; }
    }

    [DataContract]
    public sealed class ActiveShiftRpcResult
    {
        [DataMember(Name = "ok")]           public bool          Ok           { get; set; }
        [DataMember(Name = "error")]        public string        Error        { get; set; }
        [DataMember(Name = "shift")]        public PosShiftRecord Shift       { get; set; }
        [DataMember(Name = "orderCount")]   public int           OrderCount   { get; set; }
        [DataMember(Name = "totalRevenue")] public decimal       TotalRevenue { get; set; }
    }

    [DataContract]
    public sealed class OpenShiftRpcResult
    {
        [DataMember(Name = "ok")]           public bool   Ok          { get; set; }
        [DataMember(Name = "error")]        public string Error       { get; set; }
        [DataMember(Name = "shiftId")]      public string ShiftId     { get; set; }
        [DataMember(Name = "alreadyOpen")]  public bool   AlreadyOpen { get; set; }
    }

    [DataContract]
    public sealed class CloseShiftRpcResult
    {
        [DataMember(Name = "ok")]           public bool    Ok          { get; set; }
        [DataMember(Name = "error")]        public string  Error       { get; set; }
        [DataMember(Name = "totalCash")]    public decimal TotalCash   { get; set; }
        [DataMember(Name = "totalCard")]    public decimal TotalCard   { get; set; }
        [DataMember(Name = "closingCash")]  public decimal ClosingCash { get; set; }
        [DataMember(Name = "difference")]   public decimal Difference  { get; set; }
    }

    // =========================================================================
    // Shared printing contracts
    // =========================================================================

    public enum PrintType
    {
        Kitchen,
        Customer,
        Pickup,
        ShiftReport
    }

    public sealed class OrderPrintData
    {
        public string OrderId { get; set; }
        public string BackendOrderId { get; set; }
        public int OrderNo { get; set; }
        public string OrderType { get; set; }
        public string DateText { get; set; }
        public string CashierName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string District { get; set; }
        public string AddressBlock { get; set; }
        public string AddressStreet { get; set; }
        public string AddressBuilding { get; set; }
        public string AddressApartment { get; set; }
        public string AddressFloor { get; set; }
        public string AddressNote { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public int KitchenPrintCount { get; set; }
        public List<PrintLineItem> Items { get; set; } = new List<PrintLineItem>();
    }

    public sealed class PrintLineItem
    {
        public string Name { get; set; }
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public string Notes { get; set; }
    }

    [DataContract]
    public sealed class KitchenPrintRpcResult
    {
        [DataMember(Name = "ok")] public bool Ok { get; set; }
        [DataMember(Name = "error")] public string Error { get; set; }
        [DataMember(Name = "printCount")] public int PrintCount { get; set; }
        [DataMember(Name = "isReprint")] public bool IsReprint { get; set; }
    }

    [DataContract]
    public sealed class OrderNumberRpcResult
    {
        [DataMember(Name = "ok")] public bool Ok { get; set; }
        [DataMember(Name = "error")] public string Error { get; set; }
        [DataMember(Name = "orderNumber")] public long OrderNumber { get; set; }
    }

    [DataContract]
    public sealed class OrderLockRpcResult
    {
        [DataMember(Name = "ok")] public bool Ok { get; set; }
        [DataMember(Name = "locked")] public bool Locked { get; set; }
        [DataMember(Name = "renewed")] public bool Renewed { get; set; }
        [DataMember(Name = "error")] public string Error { get; set; }
        [DataMember(Name = "message")] public string Message { get; set; }
    }

    // =========================================================================
    // Partial class
    // =========================================================================
    public partial class MainWindow
    {
        // الوردية النشطة حالياً
        private string         _activeShiftId;
        private PosShiftRecord _activeShift;
        private readonly SemaphoreSlim _shiftOpSem = new SemaphoreSlim(1, 1);

        private string ShiftCacheFilePath
            => System.IO.Path.Combine(AppDataLogsDirPath, "pos_active_shift.json");

        // ======================================================================
        // 1. تحميل / مزامنة الوردية عند بدء التشغيل
        // ======================================================================
        private async Task LoadAndSyncActiveShiftAsync()
        {
            // أولاً: حمِّل الكاش المحلي (للعمل Offline)
            try
            {
                var cached = ReadJson<PosShiftRecord>(ShiftCacheFilePath);
                if (cached != null && string.Equals(cached.Status, "open",
                    StringComparison.OrdinalIgnoreCase))
                {
                    _activeShiftId = cached.Id;
                    _activeShift   = cached;
                }
            }
            catch { }

            // ثانياً: تزامن مع السيرفر
            await RefreshActiveShiftFromServerAsync().ConfigureAwait(false);
        }

        private async Task RefreshActiveShiftFromServerAsync()
        {
            var cfg = LoadSupabaseClientConfigResolved();
            if (cfg == null) return;

            try
            {
                var raw = await PostSupabaseRpcGetStringAsync(
                    cfg,
                    "api_pos_get_active_shift",
                    "{}",
                    actorRoleOverride: GetCurrentBackendActorRole()
                ).ConfigureAwait(false);

                var result = DeserializeJson<ActiveShiftRpcResult>(raw);
                if (result == null || !result.Ok) return;

                if (result.Shift != null)
                {
                    _activeShiftId = result.Shift.Id;
                    _activeShift   = result.Shift;
                    PersistActiveShiftCache();
                }
                else
                {
                    // لا توجد وردية مفتوحة
                    _activeShiftId = null;
                    _activeShift   = null;
                    DeleteShiftCache();
                }

                Dispatcher.Invoke(UpdateShiftStatusUi);
            }
            catch (Exception ex)
            {
                AppendPosRuntimeLog("SHIFT_REFRESH", ex);
            }
        }

        // ======================================================================
        // 2. فتح وردية جديدة
        // ======================================================================
        private async Task<bool> OpenShiftAsync(decimal openingCash = 0m, string note = "")
        {
            if (!await _shiftOpSem.WaitAsync(5000).ConfigureAwait(false)) return false;
            try
            {
                var cfg = LoadSupabaseClientConfigResolved();
                if (cfg == null)
                {
                    // Offline: إنشاء وردية محلية مؤقتة
                    _activeShiftId = "local_shift_" + Guid.NewGuid().ToString("N").Substring(0,12);
                    _activeShift   = new PosShiftRecord
                    {
                        Id          = _activeShiftId,
                        Status      = "open",
                        OpenedAt    = DateTime.Now.ToString("o"),
                        OpeningCash = openingCash,
                        Note        = note ?? string.Empty
                    };
                    PersistActiveShiftCache();
                    Dispatcher.Invoke(UpdateShiftStatusUi);
                    // سيُرفع للسيرفر عند عودة النت عبر Queue
                    EnqueueIdempotentMutation(new IdempotencyAwareQueueItem
                    {
                        OperationKind   = "open_shift",
                        QueueId         = _activeShiftId,
                        LocalOrderId    = _activeShiftId,
                        CreatedAtMillis = NowUnixMs()
                    });
                    return true;
                }

                var raw = await PostSupabaseRpcGetStringAsync(
                    cfg,
                    "api_pos_open_shift",
                    SerializeJson(new { p_opening_cash = openingCash, p_note = note ?? string.Empty }),
                    actorRoleOverride: GetCurrentBackendActorRole()
                ).ConfigureAwait(false);

                var result = DeserializeJson<OpenShiftRpcResult>(raw);
                if (result == null || !result.Ok)
                {
                    Dispatcher.Invoke(() =>
                        MessageBox.Show($"فشل فتح الوردية: {result?.Error}", "خطأ",
                            MessageBoxButton.OK, MessageBoxImage.Error));
                    return false;
                }

                _activeShiftId = result.ShiftId;
                await RefreshActiveShiftFromServerAsync().ConfigureAwait(false);

                var msg = result.AlreadyOpen
                    ? "الوردية مفتوحة بالفعل - تم الاتصال بها."
                    : "تم فتح الوردية بنجاح.";
                Dispatcher.Invoke(() =>
                    MessageBox.Show(msg, "الوردية", MessageBoxButton.OK, MessageBoxImage.Information));

                return true;
            }
            catch (Exception ex)
            {
                AppendPosRuntimeLog("SHIFT_OPEN", ex);
                return false;
            }
            finally
            {
                _shiftOpSem.Release();
            }
        }

        // ======================================================================
        // 3. إغلاق الوردية
        // ======================================================================
        private async Task<CloseShiftRpcResult> CloseShiftAsync(decimal closingCash = 0m)
        {
            if (string.IsNullOrWhiteSpace(_activeShiftId))
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show("لا توجد وردية مفتوحة.", "تحذير",
                        MessageBoxButton.OK, MessageBoxImage.Warning));
                return null;
            }

            if (!await _shiftOpSem.WaitAsync(5000).ConfigureAwait(false)) return null;
            try
            {
                // طباعة تقرير الوردية أولاً
                await PrintShiftReportAsync(_activeShiftId, closingCash).ConfigureAwait(false);

                var cfg = LoadSupabaseClientConfigResolved();
                if (cfg == null)
                {
                    EnqueueIdempotentMutation(new IdempotencyAwareQueueItem
                    {
                        OperationKind   = "close_shift",
                        QueueId         = "cs_" + _activeShiftId,
                        LocalOrderId    = _activeShiftId,
                        CreatedAtMillis = NowUnixMs()
                    });
                    _activeShiftId = null;
                    _activeShift   = null;
                    DeleteShiftCache();
                    Dispatcher.Invoke(UpdateShiftStatusUi);
                    return new CloseShiftRpcResult { Ok = true };
                }

                var raw = await PostSupabaseRpcGetStringAsync(
                    cfg,
                    "api_pos_close_shift",
                    SerializeJson(new
                    {
                        p_shift_id     = _activeShiftId,
                        p_closing_cash = closingCash
                    }),
                    actorRoleOverride: GetCurrentBackendActorRole()
                ).ConfigureAwait(false);

                var result = DeserializeJson<CloseShiftRpcResult>(raw);
                if (result == null || !result.Ok)
                {
                    Dispatcher.Invoke(() =>
                        MessageBox.Show($"فشل إغلاق الوردية: {result?.Error}", "خطأ",
                            MessageBoxButton.OK, MessageBoxImage.Error));
                    return result;
                }

                _activeShiftId = null;
                _activeShift   = null;
                DeleteShiftCache();
                Dispatcher.Invoke(UpdateShiftStatusUi);

                Dispatcher.Invoke(() =>
                {
                    var diff    = result.Difference;
                    var diffStr = diff >= 0 ? $"+{diff:0.000}" : $"{diff:0.000}";
                    MessageBox.Show(
                        $"تم إغلاق الوردية بنجاح.\n\n" +
                        $"إجمالي نقدي: {result.TotalCash:0.000} د.ك\n" +
                        $"إجمالي شبكة: {result.TotalCard:0.000} د.ك\n" +
                        $"المبلغ المسلَّم: {result.ClosingCash:0.000} د.ك\n" +
                        $"الفرق: {diffStr} د.ك",
                        "تقرير الوردية",
                        MessageBoxButton.OK,
                        diff == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
                });

                return result;
            }
            catch (Exception ex)
            {
                AppendPosRuntimeLog("SHIFT_CLOSE", ex);
                return null;
            }
            finally
            {
                _shiftOpSem.Release();
            }
        }

        // ======================================================================
        // 4. طباعة تقرير الوردية
        // ======================================================================
        private async Task PrintShiftReportAsync(string shiftId, decimal closingCash)
        {
            if (string.IsNullOrWhiteSpace(shiftId)) return;

            var cfg = LoadSupabaseClientConfigResolved();
            if (cfg == null) return;

            try
            {
                var raw = await PostSupabaseRpcGetStringAsync(
                    cfg,
                    "api_pos_get_active_shift",
                    "{}",
                    actorRoleOverride: GetCurrentBackendActorRole()
                ).ConfigureAwait(false);

                var result = DeserializeJson<ActiveShiftRpcResult>(raw);
                if (result?.Shift == null) return;

                var w    = ThermalWidth;
                var sep  = new string('-', w);
                var dSep = new string('=', w);
                var sb   = new System.Text.StringBuilder();

                // --- Header ---
                sb.Append(EscPosDoubleHeight(true));
                sb.Append(EscPosCenter());
                sb.AppendLine(BrandName);
                sb.Append(EscPosDoubleHeight(false));
                sb.Append(EscPosCenter());
                sb.AppendLine(sep);
                sb.Append(EscPosBold(true));
                sb.AppendLine(CenterText("تقرير إغلاق الوردية", w));
                sb.Append(EscPosBold(false));
                sb.AppendLine(sep);

                // --- Shift Info ---
                var shift = result.Shift;
                sb.AppendLine(PairLine("رقم الوردية:", shift.Id?.Substring(0,8) ?? "-", w));

                if (DateTime.TryParse(shift.OpenedAt, out var openDt))
                    sb.AppendLine(PairLine("بدأت:", openDt.ToString("dd/MM/yyyy HH:mm"), w));

                sb.AppendLine(PairLine("تنتهي:", DateTime.Now.ToString("dd/MM/yyyy HH:mm"), w));
                sb.AppendLine(sep);

                // --- Totals ---
                var totalCash  = result.Shift.TotalCashOrders;
                var totalCard  = result.Shift.TotalCardOrders;
                var totalAll   = totalCash + totalCard;
                var diff       = closingCash - totalCash;

                sb.AppendLine(PairLine("عدد الأوردرات:", result.OrderCount.ToString(), w));
                sb.AppendLine(PairLine("إجمالي الإيراد:", FormatPrice(totalAll), w));
                sb.AppendLine(sep);
                sb.AppendLine(PairLine("نقدي:", FormatPrice(totalCash), w));
                sb.AppendLine(PairLine("شبكة:", FormatPrice(totalCard), w));
                sb.AppendLine(dSep);
                sb.Append(EscPosBold(true));
                sb.AppendLine(PairLine("المبلغ المسلَّم:", FormatPrice(closingCash), w));
                sb.AppendLine(PairLine("الفرق:",
                    (diff >= 0 ? "+" : "") + FormatPrice(diff), w));
                sb.Append(EscPosBold(false));
                sb.AppendLine(dSep);

                sb.AppendLine(CenterText("شكراً لجهودكم", w));
                sb.AppendLine(CenterText(BrandName, w));
                sb.AppendLine();
                sb.AppendLine();
                sb.Append(EscPosCut());

                PrintThermalReceipt(sb.ToString());
            }
            catch (Exception ex)
            {
                AppendPosRuntimeLog("SHIFT_PRINT", ex);
            }
        }

        // ======================================================================
        // 5. إضافة shift_id لكل أوردر جديد
        // ======================================================================
        private void AttachShiftIdToOrderPayload(PosOfflineSupabaseOrderInsertRow row)
        {
            if (row == null) return;
            if (!string.IsNullOrWhiteSpace(_activeShiftId))
                row.PosShiftId = _activeShiftId;
        }

        // ======================================================================
        // 6. Persistence مساعدات
        // ======================================================================
        private void PersistActiveShiftCache()
        {
            try { WriteJson(ShiftCacheFilePath, _activeShift); }
            catch { }
        }

        private void DeleteShiftCache()
        {
            try
            {
                if (System.IO.File.Exists(ShiftCacheFilePath))
                    System.IO.File.Delete(ShiftCacheFilePath);
            }
            catch { }
        }

        private const int THERMAL_WIDTH_58MM = 32;
        private const int THERMAL_WIDTH_80MM = 48;
        private const string BrandName = "فالح ابو العنبه";

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
                ? (order.KitchenPrintCount > 1 ? "*** فاتورة مطبخ - بديلة ***" : "فاتورة المطبخ")
                : printType == PrintType.Pickup ? "فاتورة تيك أواي" : "فاتورة العميل";

            if (printType == PrintType.Kitchen && order.KitchenPrintCount > 1)
            {
                sb.Append(EscPosBold(true));
                sb.Append(EscPosCenter());
                sb.AppendLine(CenterText("[نسخة بديلة - أوردر #" + order.OrderNo.ToString() + "]", w));
                sb.Append(EscPosBold(false));
            }

            sb.Append(EscPosBold(true));
            sb.AppendLine(CenterText(invoiceTitle, w));
            sb.Append(EscPosBold(false));
            sb.AppendLine(sep);

            sb.AppendLine(PairLine("رقم الأوردر:", "#" + order.OrderNo.ToString(), w));
            sb.AppendLine(PairLine("النوع:", OrderTypeAr(order.OrderType), w));
            sb.AppendLine(PairLine("التاريخ:", order.DateText ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm"), w));
            if (!string.IsNullOrWhiteSpace(order.CashierName)) sb.AppendLine(PairLine("الكاشير:", TruncateText(order.CashierName, w / 2), w));
            if (!string.IsNullOrWhiteSpace(order.CustomerName)) sb.AppendLine(PairLine("العميل:", TruncateText(order.CustomerName, w - 10), w));
            if (!string.IsNullOrWhiteSpace(order.CustomerPhone)) sb.AppendLine(PairLine("الهاتف:", order.CustomerPhone, w));

            if (string.Equals(order.OrderType, "DELIVERY", StringComparison.OrdinalIgnoreCase) || string.Equals(order.OrderType, "POS_DELIVERY", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine(sep);
                if (!string.IsNullOrWhiteSpace(order.District)) sb.AppendLine(PairLine("المنطقة:", TruncateText(order.District, w - 10), w));
                if (!string.IsNullOrWhiteSpace(order.AddressBlock)) sb.AppendLine(PairLine("مجاورة:", order.AddressBlock, w));
                if (!string.IsNullOrWhiteSpace(order.AddressStreet)) sb.AppendLine(PairLine("شارع:", TruncateText(order.AddressStreet, w - 8), w));
                if (!string.IsNullOrWhiteSpace(order.AddressBuilding)) sb.AppendLine(PairLine("عمارة:", order.AddressBuilding, w));
                if (!string.IsNullOrWhiteSpace(order.AddressApartment)) sb.AppendLine(PairLine("شقة:", order.AddressApartment, w));
                if (!string.IsNullOrWhiteSpace(order.AddressFloor)) sb.AppendLine(PairLine("دور:", order.AddressFloor, w));
                if (!string.IsNullOrWhiteSpace(order.AddressNote)) sb.AppendLine(PairLine("ملاحظة:", TruncateText(order.AddressNote, w - 10), w));
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
                sb.AppendLine(PairLine("المجموع:", FormatPrice(order.Subtotal), w));
                if (order.DeliveryFee > 0) sb.AppendLine(PairLine("رسوم التوصيل:", FormatPrice(order.DeliveryFee), w));
                if (order.Discount > 0) sb.AppendLine(PairLine("الخصم:", "-" + FormatPrice(order.Discount), w));
                sb.AppendLine(dSep);
                sb.Append(EscPosBold(true));
                sb.AppendLine(PairLine("الإجمالي:", FormatPrice(order.Total), w));
                sb.Append(EscPosBold(false));
                sb.AppendLine(dSep);
            }

            sb.AppendLine(CenterText("شكراً لزيارتكم", w));
            sb.AppendLine(CenterText(BrandName, w));
            sb.AppendLine();
            sb.AppendLine();
            sb.Append(EscPosCut());
            return sb.ToString();
        }

        private async Task<bool> PrintKitchenTicketWithServerCheckAsync(OrderPrintData order, bool adminAllowsReprint = false)
        {
            if (order == null) return false;

            var cfg = LoadSupabaseClientConfigResolved();
            if (cfg == null)
            {
                PrintThermalReceiptSilent(BuildThermalReceiptText(order, PrintType.Kitchen));
                return true;
            }

            try
            {
                var raw = await PostSupabaseRpcGetStringAsync(
                    cfg,
                    "api_pos_increment_kitchen_print",
                    SerializeJson(new { p_order_id = order.BackendOrderId ?? order.OrderId, p_allow_reprint = adminAllowsReprint }),
                    actorRoleOverride: GetCurrentBackendActorRole()).ConfigureAwait(false);

                var result = DeserializeJson<KitchenPrintRpcResult>(raw) ?? new KitchenPrintRpcResult { Ok = true };
                if (!result.Ok)
                {
                    if (string.Equals(result.Error, "kitchen_already_printed", StringComparison.OrdinalIgnoreCase))
                    {
                        var choice = MessageBox.Show(
                            "تم طباعة فاتورة المطبخ بالفعل (" + result.PrintCount.ToString() + " مرة).\n\nهل تريد طباعة نسخة بديلة؟",
                            "تحذير",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (choice != MessageBoxResult.Yes) return false;
                        return await PrintKitchenTicketWithServerCheckAsync(order, true).ConfigureAwait(false);
                    }
                    return false;
                }

                order.KitchenPrintCount = result.PrintCount;
            }
            catch
            {
            }

            PrintThermalReceiptSilent(BuildThermalReceiptText(order, PrintType.Kitchen));
            return true;
        }

        private async Task<int> GetNextBranchOrderNumberAsync()
        {
            var cfg = LoadSupabaseClientConfigResolved();
            if (cfg == null)
            {
                return -(int)(NowUnixMs() % 9000 + 1000);
            }

            try
            {
                var raw = await PostSupabaseRpcGetStringAsync(
                    cfg,
                    "api_pos_next_order_number",
                    "{}",
                    actorRoleOverride: GetCurrentBackendActorRole()).ConfigureAwait(false);

                var result = DeserializeJson<OrderNumberRpcResult>(raw);
                if (result != null && result.Ok && result.OrderNumber > 0) return (int)result.OrderNumber;
            }
            catch
            {
            }

            return -(int)(NowUnixMs() % 9000 + 1000);
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
                CustomerName = !string.IsNullOrWhiteSpace(pickup.Name) ? pickup.Name : "استلام",
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

        private FlowDocument BuildPlainTextReceiptDocument(string text)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                FlowDirection = FlowDirection.RightToLeft
            };
            doc.Blocks.Add(new Paragraph(new Run(StripPrinterControlChars(text))) { Margin = new Thickness(0) });
            return doc;
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
            var nameH = "الصنف";
            var qtyH = "الكمية";
            var unitH = "السعر";
            var totalH = "الإجمالي";
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
            => amount.ToString("0.000", CultureInfo.InvariantCulture) + " د.ك";

        private string OrderTypeAr(string type)
        {
            switch ((type ?? string.Empty).ToUpperInvariant())
            {
                case "DELIVERY":
                case "POS_DELIVERY":
                    return "توصيل";
                case "PICKUP":
                case "POS_PICKUP":
                    return "استلام من الفرع";
                case "TAKEAWAY":
                case "POS_TAKEAWAY":
                    return "تيك أواي";
                default:
                    return type ?? string.Empty;
            }
        }

        private string EscPosDoubleHeight(bool on) => on ? "\x1B\x21\x10" : "\x1B\x21\x00";
        private string EscPosBold(bool on) => on ? "\x1B\x45\x01" : "\x1B\x45\x00";
        private string EscPosCenter() => "\x1B\x61\x01";
        private string EscPosCut() => "\x1D\x56\x41\x00";

        private string GetDeviceId()
        {
            if (!string.IsNullOrWhiteSpace(_cachedDeviceId)) return _cachedDeviceId;

            try
            {
                var path = System.IO.Path.Combine(AppDataLogsDirPath, "device_id.txt");
                if (System.IO.File.Exists(path))
                {
                    var stored = System.IO.File.ReadAllText(path, Encoding.UTF8).Trim();
                    if (!string.IsNullOrWhiteSpace(stored))
                    {
                        _cachedDeviceId = stored;
                        return _cachedDeviceId;
                    }
                }

                _cachedDeviceId = "pos_" + Guid.NewGuid().ToString("N").Substring(0, 12);
                System.IO.File.WriteAllText(path, _cachedDeviceId, Encoding.UTF8);
            }
            catch
            {
                _cachedDeviceId = "pos_" + Environment.MachineName.GetHashCode().ToString("X8");
            }

            return _cachedDeviceId;
        }


        // UpdateShiftStatusUi() مُعرَّفة بالفعل داخل MainWindow.xaml.cs
    }
}

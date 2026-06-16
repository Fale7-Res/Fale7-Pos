// =============================================================================
// PosShiftManager.cs
// Ù…Ø³Ø§Ø± Ø§Ù„Ù…Ù„Ù: POS-Windows/PosShiftManager.cs
// ÙŠÙØ¶Ø§Ù ÙƒÙ€ partial class Ø¯Ø§Ø®Ù„ MainWindow
// ÙŠØ¹Ø§Ù„Ø¬: Ø§Ù„ÙˆØ±Ø¯ÙŠØ© Ø§Ù„Ù…Ø´ØªØ±ÙƒØ© Ø¨ÙŠÙ† Ø§Ù„Ø¬Ù‡Ø§Ø²ÙŠÙ† + Realtime Sync + Order Edit Lock
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
    // Partial class
    // =========================================================================
    public partial class MainWindow
    {
        // Ø§Ù„ÙˆØ±Ø¯ÙŠØ© Ø§Ù„Ù†Ø´Ø·Ø© Ø­Ø§Ù„ÙŠØ§Ù‹
        private string         _activeShiftId;
        private PosShiftRecord _activeShift;
        private readonly SemaphoreSlim _shiftOpSem = new SemaphoreSlim(1, 1);

        private string ShiftCacheFilePath
            => System.IO.Path.Combine(AppDataLogsDirPath, "pos_active_shift.json");

        // ======================================================================
        // 1. ØªØ­Ù…ÙŠÙ„ / Ù…Ø²Ø§Ù…Ù†Ø© Ø§Ù„ÙˆØ±Ø¯ÙŠØ© Ø¹Ù†Ø¯ Ø¨Ø¯Ø¡ Ø§Ù„ØªØ´ØºÙŠÙ„
        // ======================================================================
        private async Task LoadAndSyncActiveShiftAsync()
        {
            // Ø£ÙˆÙ„Ø§Ù‹: Ø­Ù…ÙÙ‘Ù„ Ø§Ù„ÙƒØ§Ø´ Ø§Ù„Ù…Ø­Ù„ÙŠ (Ù„Ù„Ø¹Ù…Ù„ Offline)
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

            // Ø«Ø§Ù†ÙŠØ§Ù‹: ØªØ²Ø§Ù…Ù† Ù…Ø¹ Ø§Ù„Ø³ÙŠØ±ÙØ±
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
                    // Ù„Ø§ ØªÙˆØ¬Ø¯ ÙˆØ±Ø¯ÙŠØ© Ù…ÙØªÙˆØ­Ø©
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
        // 2. ÙØªØ­ ÙˆØ±Ø¯ÙŠØ© Ø¬Ø¯ÙŠØ¯Ø©
        // ======================================================================
        private async Task<bool> OpenShiftAsync(decimal openingCash = 0m, string note = "")
        {
            if (!await _shiftOpSem.WaitAsync(5000).ConfigureAwait(false)) return false;
            try
            {
                var cfg = LoadSupabaseClientConfigResolved();
                if (cfg == null)
                {
                    // Offline: Ø¥Ù†Ø´Ø§Ø¡ ÙˆØ±Ø¯ÙŠØ© Ù…Ø­Ù„ÙŠØ© Ù…Ø¤Ù‚ØªØ©
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
                    // Ø³ÙŠÙØ±ÙØ¹ Ù„Ù„Ø³ÙŠØ±ÙØ± Ø¹Ù†Ø¯ Ø¹ÙˆØ¯Ø© Ø§Ù„Ù†Øª Ø¹Ø¨Ø± Queue
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
                        MessageBox.Show($"ÙØ´Ù„ ÙØªØ­ Ø§Ù„ÙˆØ±Ø¯ÙŠØ©: {result?.Error}", "Ø®Ø·Ø£",
                            MessageBoxButton.OK, MessageBoxImage.Error));
                    return false;
                }

                _activeShiftId = result.ShiftId;
                await RefreshActiveShiftFromServerAsync().ConfigureAwait(false);

                var msg = result.AlreadyOpen
                    ? "Ø§Ù„ÙˆØ±Ø¯ÙŠØ© Ù…ÙØªÙˆØ­Ø© Ø¨Ø§Ù„ÙØ¹Ù„ - ØªÙ… Ø§Ù„Ø§ØªØµØ§Ù„ Ø¨Ù‡Ø§."
                    : "ØªÙ… ÙØªØ­ Ø§Ù„ÙˆØ±Ø¯ÙŠØ© Ø¨Ù†Ø¬Ø§Ø­.";
                Dispatcher.Invoke(() =>
                    MessageBox.Show(msg, "Ø§Ù„ÙˆØ±Ø¯ÙŠØ©", MessageBoxButton.OK, MessageBoxImage.Information));

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
        // 3. Ø¥ØºÙ„Ø§Ù‚ Ø§Ù„ÙˆØ±Ø¯ÙŠØ©
        // ======================================================================
        private async Task<CloseShiftRpcResult> CloseShiftAsync(decimal closingCash = 0m)
        {
            if (string.IsNullOrWhiteSpace(_activeShiftId))
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show("Ù„Ø§ ØªÙˆØ¬Ø¯ ÙˆØ±Ø¯ÙŠØ© Ù…ÙØªÙˆØ­Ø©.", "ØªØ­Ø°ÙŠØ±",
                        MessageBoxButton.OK, MessageBoxImage.Warning));
                return null;
            }

            if (!await _shiftOpSem.WaitAsync(5000).ConfigureAwait(false)) return null;
            try
            {
                // Ø·Ø¨Ø§Ø¹Ø© ØªÙ‚Ø±ÙŠØ± Ø§Ù„ÙˆØ±Ø¯ÙŠØ© Ø£ÙˆÙ„Ø§Ù‹
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
                        MessageBox.Show($"ÙØ´Ù„ Ø¥ØºÙ„Ø§Ù‚ Ø§Ù„ÙˆØ±Ø¯ÙŠØ©: {result?.Error}", "Ø®Ø·Ø£",
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
                        $"ØªÙ… Ø¥ØºÙ„Ø§Ù‚ Ø§Ù„ÙˆØ±Ø¯ÙŠØ© Ø¨Ù†Ø¬Ø§Ø­.\n\n" +
                        $"Ø¥Ø¬Ù…Ø§Ù„ÙŠ Ù†Ù‚Ø¯ÙŠ: {result.TotalCash:0.000} Ø¯.Ùƒ\n" +
                        $"Ø¥Ø¬Ù…Ø§Ù„ÙŠ Ø´Ø¨ÙƒØ©: {result.TotalCard:0.000} Ø¯.Ùƒ\n" +
                        $"Ø§Ù„Ù…Ø¨Ù„Øº Ø§Ù„Ù…Ø³Ù„ÙŽÙ‘Ù…: {result.ClosingCash:0.000} Ø¯.Ùƒ\n" +
                        $"Ø§Ù„ÙØ±Ù‚: {diffStr} Ø¯.Ùƒ",
                        "ØªÙ‚Ø±ÙŠØ± Ø§Ù„ÙˆØ±Ø¯ÙŠØ©",
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
        // 4. Ø·Ø¨Ø§Ø¹Ø© ØªÙ‚Ø±ÙŠØ± Ø§Ù„ÙˆØ±Ø¯ÙŠØ©
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
                sb.AppendLine(CenterText("ØªÙ‚Ø±ÙŠØ± Ø¥ØºÙ„Ø§Ù‚ Ø§Ù„ÙˆØ±Ø¯ÙŠØ©", w));
                sb.Append(EscPosBold(false));
                sb.AppendLine(sep);

                // --- Shift Info ---
                var shift = result.Shift;
                sb.AppendLine(PairLine("Ø±Ù‚Ù… Ø§Ù„ÙˆØ±Ø¯ÙŠØ©:", shift.Id?.Substring(0,8) ?? "-", w));

                if (DateTime.TryParse(shift.OpenedAt, out var openDt))
                    sb.AppendLine(PairLine("Ø¨Ø¯Ø£Øª:", openDt.ToString("dd/MM/yyyy HH:mm"), w));

                sb.AppendLine(PairLine("ØªÙ†ØªÙ‡ÙŠ:", DateTime.Now.ToString("dd/MM/yyyy HH:mm"), w));
                sb.AppendLine(sep);

                // --- Totals ---
                var totalCash  = result.Shift.TotalCashOrders;
                var totalCard  = result.Shift.TotalCardOrders;
                var totalAll   = totalCash + totalCard;
                var diff       = closingCash - totalCash;

                sb.AppendLine(PairLine("Ø¹Ø¯Ø¯ Ø§Ù„Ø£ÙˆØ±Ø¯Ø±Ø§Øª:", result.OrderCount.ToString(), w));
                sb.AppendLine(PairLine("Ø¥Ø¬Ù…Ø§Ù„ÙŠ Ø§Ù„Ø¥ÙŠØ±Ø§Ø¯:", FormatPrice(totalAll), w));
                sb.AppendLine(sep);
                sb.AppendLine(PairLine("Ù†Ù‚Ø¯ÙŠ:", FormatPrice(totalCash), w));
                sb.AppendLine(PairLine("Ø´Ø¨ÙƒØ©:", FormatPrice(totalCard), w));
                sb.AppendLine(dSep);
                sb.Append(EscPosBold(true));
                sb.AppendLine(PairLine("Ø§Ù„Ù…Ø¨Ù„Øº Ø§Ù„Ù…Ø³Ù„ÙŽÙ‘Ù…:", FormatPrice(closingCash), w));
                sb.AppendLine(PairLine("Ø§Ù„ÙØ±Ù‚:",
                    (diff >= 0 ? "+" : "") + FormatPrice(diff), w));
                sb.Append(EscPosBold(false));
                sb.AppendLine(dSep);

                sb.AppendLine(CenterText("Ø´ÙƒØ±Ø§Ù‹ Ù„Ø¬Ù‡ÙˆØ¯ÙƒÙ…", w));
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
        // 5. Ø¥Ø¶Ø§ÙØ© shift_id Ù„ÙƒÙ„ Ø£ÙˆØ±Ø¯Ø± Ø¬Ø¯ÙŠØ¯
        // ======================================================================
        private void AttachShiftIdToOrderPayload(PosOfflineSupabaseOrderInsertRow row)
        {
            if (row == null) return;
            if (!string.IsNullOrWhiteSpace(_activeShiftId))
                row.PosShiftId = _activeShiftId;
        }

        // ======================================================================
        // 6. Persistence Ù…Ø³Ø§Ø¹Ø¯Ø§Øª
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
                            "ØªÙ… Ø·Ø¨Ø§Ø¹Ø© ÙØ§ØªÙˆØ±Ø© Ø§Ù„Ù…Ø·Ø¨Ø® Ø¨Ø§Ù„ÙØ¹Ù„ (" + result.PrintCount.ToString() + " Ù…Ø±Ø©).\n\nÙ‡Ù„ ØªØ±ÙŠØ¯ Ø·Ø¨Ø§Ø¹Ø© Ù†Ø³Ø®Ø© Ø¨Ø¯ÙŠÙ„Ø©ØŸ",
                            "ØªØ­Ø°ÙŠØ±",
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


        // UpdateShiftStatusUi() Ù…ÙØ¹Ø±ÙŽÙ‘ÙØ© Ø¨Ø§Ù„ÙØ¹Ù„ Ø¯Ø§Ø®Ù„ MainWindow.xaml.cs
    }
}

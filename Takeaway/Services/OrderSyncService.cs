using Fale7_POS.Infrastructure.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        // ──────────────────────────────────────────────────────────────────────
        // PHASE 7.5 — PURE CLOUD
        // NO local queue files, NO offline JSON persistence.
        // The ONLY disk-persisted queue is the idempotency queue
        // (pos_idq_v2.json) managed by IdempotencyQueueManager.cs.
        // ──────────────────────────────────────────────────────────────────────

        // 5-second timer now ONLY drives the idempotency queue.
        // All offline-queue fields (posOfflineQueue, pendingAddressDeletes,
        // pendingAppOrderMutations, pendingShiftMigrations) have been removed.
        private DispatcherTimer _posOfflineSyncTimer;

        private void InitializePosOfflineSensing()
        {
            if (_posOfflineSyncTimer != null) return;

            EnsureIdqLoaded(); // load idempotency queue from disk (the only persisted queue)

            _posOfflineSyncTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _posOfflineSyncTimer.Tick += PosOfflineSyncTimer_Tick;
            _posOfflineSyncTimer.Start();
        }

        private void StopPosOfflineSensing()
        {
            try
            {
                if (_posOfflineSyncTimer != null)
                {
                    _posOfflineSyncTimer.Tick -= PosOfflineSyncTimer_Tick;
                    _posOfflineSyncTimer.Stop();
                }
            }
            catch
            {
            }
            _posOfflineSyncTimer = null;
        }

        private void PosOfflineSyncTimer_Tick(object sender, EventArgs e)
        {
            // [PHASE 7.5] The 5-second timer only processes the idempotency queue.
            // All offline-order queues have been removed.
            _ = ProcessIdempotencyQueueAsync();
        }

        /// <summary>
        /// Triggered by IdempotencyQueueManager (via EnqueueIdempotentMutation).
        /// Now immediately processes the idempotency queue — no old offline sync.
        /// </summary>
        private void QueuePosOfflineSync()
        {
            Task.Run(async () =>
            {
                try
                {
                    await ProcessIdempotencyQueueAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Idempotency queue process failed: " + ex.Message);
                }
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // ORDER BUILDERS — used by DeliveryMainView to create cloud payloads
        // ──────────────────────────────────────────────────────────────────────

        private PosOfflineSupabaseOrderInsertRow BuildPosOfflineDeliveryOrderInsertRow(DeliveryOrderRecord order)
        {
            if (order == null) return null;

            var createdAtMs = order.CreatedAt > 0 ? order.CreatedAt : NowUnixMs();
            var finalizedAtMs = NowUnixMs();
            var subtotal = order.Subtotal < 0 ? 0 : order.Subtotal;
            var deliveryFee = order.DeliveryFee < 0 ? 0 : order.DeliveryFee;
            var total = order.Total > 0 ? order.Total : (subtotal + deliveryFee);
            if (total < 0) total = 0;

            var metaItems = new List<PosOfflineOrderMetadataItem>();
            if (order.Items != null)
            {
                for (int i = 0; i < order.Items.Count; i++)
                {
                    var item = order.Items[i];
                    if (item == null) continue;
                    var qty = item.Qty < 0 ? 0 : item.Qty;
                    var price = item.Price < 0 ? 0 : item.Price;
                    subtotal += qty * price;
                    metaItems.Add(new PosOfflineOrderMetadataItem
                    {
                        Name = item.Desc ?? string.Empty,
                        ProductId = item.ProductId,
                        SourceProductId = item.SourceProductId,
                        Qty = qty,
                        Price = price,
                        Notes = item.Notes ?? string.Empty,
                        OptionIds = item.OptionIds == null ? new List<string>() : new List<string>(item.OptionIds)
                    });
                }
            }

            var rowId = BuildPosOfflineBackendOrderId(order.Id);
            var orderType = "POS_DELIVERY";
            var driverUserId = order.Driver == null
                ? null
                : (!string.IsNullOrWhiteSpace(order.Driver.AppUserId)
                    ? order.Driver.AppUserId.Trim()
                    : (string.IsNullOrWhiteSpace(order.Driver.Id) ? null : order.Driver.Id.Trim()));

            return new PosOfflineSupabaseOrderInsertRow
            {
                Id = rowId,
                IdempotencyKey = order.IdempotencyKey,
                OrderType = orderType,
                Status = "DONE",
                CustomerUserId = null,
                CustomerName = string.IsNullOrWhiteSpace(order.CustomerName) ? "Cashier Customer" : order.CustomerName.Trim(),
                CustomerPhone = (order.CustomerPhone ?? string.Empty).Trim(),
                District = (order.District ?? string.Empty).Trim(),
                AddressText = BuildDeliveryAddressText(order),
                PosAddressId = order.Address?.Id,
                DriverUserId = driverUserId,
                Subtotal = subtotal,
                DeliveryFee = deliveryFee,
                Total = total,
                CreatedAt = UnixMsToIsoUtc(createdAtMs),
                UpdatedAt = UnixMsToIsoUtc(finalizedAtMs),
                Metadata = BuildPosOfflineMetadata(
                    localOrderId: order.Id,
                    localOrderNo: order.No,
                    localCreatedAtMs: createdAtMs,
                    finalizedAtMs: finalizedAtMs,
                    orderType: orderType,
                    paymentMethod: "CASH",
                    items: metaItems,
                    driverName: order.Driver == null ? null : order.Driver.Name,
                    driverPhone: order.Driver == null ? null : order.Driver.Phone,
                    cashierUserId: !string.IsNullOrWhiteSpace(order.CashierUserId) ? order.CashierUserId : GetCurrentSessionUserId(),
                    cashierUsername: !string.IsNullOrWhiteSpace(order.CashierUsername) ? order.CashierUsername : GetCurrentSessionDisplayName(),
                    cashierRole: !string.IsNullOrWhiteSpace(order.CashierRole) ? order.CashierRole : GetCurrentBackendActorRole(),
                    driverUserId: driverUserId)
            };
        }

        private PosOfflineSupabaseOrderInsertRow BuildPosOfflineTakeawayOrderInsertRow(TakeawayOrderRecord order, TakeawayPickupRecord pickup)
        {
            if (order == null) return null;

            var createdAtMs = order.CreatedAt > 0 ? order.CreatedAt : NowUnixMs();
            var finalizedAtMs = NowUnixMs();
            var subtotal = 0d;
            var metaItems = new List<PosOfflineOrderMetadataItem>();
            if (order.Items != null)
            {
                for (int i = 0; i < order.Items.Count; i++)
                {
                    var item = order.Items[i];
                    if (item == null) continue;
                    var qty = item.Qty < 0 ? 0 : item.Qty;
                    var price = item.Price < 0 ? 0 : item.Price;
                    subtotal += qty * price;
                    metaItems.Add(new PosOfflineOrderMetadataItem
                    {
                        Name = item.Desc ?? string.Empty,
                        ProductId = item.ProductId,
                        SourceProductId = item.SourceProductId,
                        Qty = qty,
                        Price = price,
                        Notes = string.Empty,
                        OptionIds = item.OptionIds == null ? new List<string>() : new List<string>(item.OptionIds)
                    });
                }
            }

            var rowId = BuildPosOfflineBackendOrderId(order.Id);
            var customerName = pickup != null && !string.IsNullOrWhiteSpace(pickup.Name) ? pickup.Name.Trim() : "Cashier Customer";
            var customerPhone = pickup != null && !string.IsNullOrWhiteSpace(pickup.Phone) ? pickup.Phone.Trim() : string.Empty;
            var orderType = pickup == null ? "POS_TAKEAWAY" : "POS_PICKUP";

            return new PosOfflineSupabaseOrderInsertRow
            {
                Id = rowId,
                IdempotencyKey = order.IdempotencyKey,
                OrderType = orderType,
                Status = "DONE",
                CustomerUserId = null,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                District = null,
                AddressText = "Pickup from branch",
                DriverUserId = null,
                Subtotal = subtotal < 0 ? 0 : subtotal,
                DeliveryFee = 0,
                Total = subtotal < 0 ? 0 : subtotal,
                CreatedAt = UnixMsToIsoUtc(createdAtMs),
                UpdatedAt = UnixMsToIsoUtc(finalizedAtMs),
                Metadata = BuildPosOfflineMetadata(
                    localOrderId: order.Id,
                    localOrderNo: order.No,
                    localCreatedAtMs: createdAtMs,
                    finalizedAtMs: finalizedAtMs,
                    orderType: orderType,
                    paymentMethod: "CASH",
                    items: metaItems,
                    driverName: null,
                    driverPhone: null,
                    cashierUserId: GetCurrentSessionUserId(),
                    cashierUsername: GetCurrentSessionDisplayName(),
                    cashierRole: GetCurrentBackendActorRole())
            };
        }

        private PosOfflineOrderMetadata BuildPosOfflineMetadata(
            string localOrderId,
            int localOrderNo,
            long localCreatedAtMs,
            long finalizedAtMs,
            string orderType,
            string paymentMethod,
            List<PosOfflineOrderMetadataItem> items,
            string driverName,
            string driverPhone,
            string cashierUserId,
            string cashierUsername,
            string cashierRole,
            string driverUserId = null)
        {
            return new PosOfflineOrderMetadata
            {
                Source = "POS_OFFLINE_SENSING",
                LocalOrderId = localOrderId ?? string.Empty,
                LocalOrderNo = localOrderNo,
                LocalCreatedAtMillis = localCreatedAtMs,
                FinalizedAtMillis = finalizedAtMs,
                PaymentMethod = paymentMethod ?? "CASH",
                PosOrderType = orderType ?? string.Empty,
                DriverName = string.IsNullOrWhiteSpace(driverName) ? null : driverName.Trim(),
                DriverPhone = string.IsNullOrWhiteSpace(driverPhone) ? null : driverPhone.Trim(),
                CashierUserId = string.IsNullOrWhiteSpace(cashierUserId) ? null : cashierUserId.Trim(),
                CashierUsername = string.IsNullOrWhiteSpace(cashierUsername) ? null : cashierUsername.Trim(),
                CashierRole = string.IsNullOrWhiteSpace(cashierRole) ? null : cashierRole.Trim().ToUpperInvariant(),
                Items = items ?? new List<PosOfflineOrderMetadataItem>(),
                DriverUserId = string.IsNullOrWhiteSpace(driverUserId) ? null : driverUserId.Trim()
            };
        }

        private string BuildPosOfflineBackendOrderId(string localOrderId)
        {
            var safe = string.IsNullOrWhiteSpace(localOrderId) ? NewId() : localOrderId.Trim();
            return "pos_" + safe;
        }

        private string BuildDeliveryAddressText(DeliveryOrderRecord order)
        {
            if (order == null) return string.Empty;
            var parts = new List<string>();
            var district = order.Address != null && !string.IsNullOrWhiteSpace(order.Address.District) ? order.Address.District.Trim() : (order.District ?? string.Empty).Trim();
            var block = order.Address == null ? string.Empty : (order.Address.Block ?? string.Empty).Trim();
            var street = order.Address == null ? string.Empty : (order.Address.Street ?? string.Empty).Trim();
            var building = order.Address == null ? string.Empty : (order.Address.Building ?? string.Empty).Trim();
            var apartment = order.Address == null ? string.Empty : (order.Address.Apartment ?? string.Empty).Trim();
            var floor = order.Address == null ? string.Empty : (order.Address.Floor ?? string.Empty).Trim();
            var note = order.Address == null ? string.Empty : (order.Address.Note ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(district)) parts.Add("District: " + district);
            if (!string.IsNullOrWhiteSpace(block)) parts.Add("Block: " + block);
            if (!string.IsNullOrWhiteSpace(street)) parts.Add("Street: " + street);
            if (!string.IsNullOrWhiteSpace(building)) parts.Add("Building: " + building);
            if (!string.IsNullOrWhiteSpace(apartment)) parts.Add("Apartment: " + apartment);
            if (!string.IsNullOrWhiteSpace(floor)) parts.Add("Floor: " + floor);
            if (!string.IsNullOrWhiteSpace(note)) parts.Add("Note: " + note);
            return parts.Count == 0 ? string.Empty : string.Join(" - ", parts.ToArray());
        }

        private string UnixMsToIsoUtc(long unixMs)
        {
            try
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(unixMs).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            }
            catch
            {
                return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // CLOUD PUSH — direct to Supabase RPC with idempotency key
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Pushes a skeleton RECEIVED-status order to Supabase immediately when the
        /// cashier confirms the delivery form. Terminal B sees it via Realtime INSERT.
        /// </summary>
        private async Task PushNewDeliveryOrderToCloudAsync(DeliveryOrderRecord order)
        {
            if (order == null) return;

            SupabaseClientConfigRecord cfg = null;
            try { cfg = LoadSupabaseClientConfigResolved(); } catch { cfg = null; }
            if (cfg == null) return;

            var rowId = BuildPosOfflineBackendOrderId(order.Id);
            var ts = DateTime.UtcNow.ToString("o");

            var row = new PosOfflineSupabaseOrderInsertRow
            {
                Id = rowId,
                IdempotencyKey = string.IsNullOrWhiteSpace(order.IdempotencyKey)
                    ? LocalIdempotencyEngine.GenerateIdempotencyKey("DELV")
                    : order.IdempotencyKey,
                OrderType = "POS_DELIVERY",
                Status = "RECEIVED",
                CustomerUserId = null,
                CustomerName = string.IsNullOrWhiteSpace(order.CustomerName) ? "Cashier Customer" : order.CustomerName.Trim(),
                CustomerPhone = (order.CustomerPhone ?? string.Empty).Trim(),
                District = (order.District ?? string.Empty).Trim(),
                AddressText = BuildDeliveryAddressText(order),
                PosAddressId = order.Address?.Id,
                DriverUserId = null,
                Subtotal = 0,
                DeliveryFee = order.DeliveryFee < 0 ? 0 : order.DeliveryFee,
                Total = order.DeliveryFee < 0 ? 0 : order.DeliveryFee,
                CreatedAt = UnixMsToIsoUtc(order.CreatedAt > 0 ? order.CreatedAt : NowUnixMs()),
                UpdatedAt = ts,
                Metadata = BuildPosOfflineMetadata(
                    localOrderId: order.Id,
                    localOrderNo: order.No,
                    localCreatedAtMs: order.CreatedAt > 0 ? order.CreatedAt : NowUnixMs(),
                    finalizedAtMs: NowUnixMs(),
                    orderType: "POS_DELIVERY",
                    paymentMethod: "CASH",
                    items: new List<PosOfflineOrderMetadataItem>(),
                    driverName: null,
                    driverPhone: null,
                    cashierUserId: order.CashierUserId ?? GetCurrentSessionUserId(),
                    cashierUsername: order.CashierUsername ?? GetCurrentSessionDisplayName(),
                    cashierRole: order.CashierRole ?? GetCurrentBackendActorRole())
            };

            order.BackendId = rowId;

            await PushPosOfflineOrderToSupabaseAsync(cfg, row).ConfigureAwait(false);
            Debug.WriteLine("[PURE CLOUD] Delivery order " + order.Id + " pushed to cloud at creation. backendId=" + rowId);
        }

        /// <summary>
        /// Direct cloud push of a takeaway/pickup order with DONE status.
        /// Called from TakeawayMainController when the cashier finalises an order.
        /// </summary>
        private async Task PushNewTakeawayOrderToCloudAsync(TakeawayOrderRecord order, TakeawayPickupRecord pickup)
        {
            if (order == null) return;

            SupabaseClientConfigRecord cfg = null;
            try { cfg = LoadSupabaseClientConfigResolved(); } catch { cfg = null; }
            if (cfg == null) return;

            var row = BuildPosOfflineTakeawayOrderInsertRow(order, pickup);
            if (row == null) return;

            await PushPosOfflineOrderToSupabaseAsync(cfg, row).ConfigureAwait(false);
            Debug.WriteLine("[PURE CLOUD] Takeaway/pickup order " + order.Id + " pushed to cloud. type=" + (row.OrderType ?? string.Empty));
        }

        /// <summary>
        /// Direct cloud push of a delivery order with DONE status (driver assigned).
        /// Called from DeliveryMainView when the cashier assigns a driver / completes.
        /// </summary>
        private async Task PushCompletedDeliveryOrderToCloudAsync(DeliveryOrderRecord order)
        {
            if (order == null) return;

            SupabaseClientConfigRecord cfg = null;
            try { cfg = LoadSupabaseClientConfigResolved(); } catch { cfg = null; }
            if (cfg == null) return;

            var row = BuildPosOfflineDeliveryOrderInsertRow(order);
            if (row == null) return;

            await PushPosOfflineOrderToSupabaseAsync(cfg, row).ConfigureAwait(false);
            Debug.WriteLine("[PURE CLOUD] Delivery order " + order.Id + " pushed to cloud at completion.");
        }

        /// <summary>
        /// Core RPC call to create/upsert an order in Supabase via
        /// api_pos_create_order_idempotent. Uses the idempotency key for safety.
        /// </summary>
        private async Task PushPosOfflineOrderToSupabaseAsync(SupabaseClientConfigRecord cfg, PosOfflineSupabaseOrderInsertRow row)
        {
            if (cfg == null) throw new ArgumentNullException("cfg");
            if (row == null) throw new ArgumentNullException("row");

            if (string.IsNullOrWhiteSpace(row.IdempotencyKey))
            {
                var prefix = "ORD_";
                var t = (row.OrderType ?? string.Empty).Trim().ToUpperInvariant();
                if (t == "POS_DELIVERY" || t == "DELIVERY") prefix = "DELV_";
                else if (t == "POS_TAKEAWAY" || t == "TAKEAWAY") prefix = "TAKE_";
                else if (t == "POS_PICKUP" || t == "PICKUP") prefix = "PICK_";
                row.IdempotencyKey = prefix + Guid.NewGuid().ToString("N");
            }

            var payload = new SupabasePosCreateOrderIdempotentRequest { IdempotencyKey = row.IdempotencyKey, Payload = row };

            var actorRole = row.Metadata != null ? row.Metadata.CashierRole : null;
            var actorUserId = row.Metadata != null ? row.Metadata.CashierUserId : null;
            if (string.IsNullOrWhiteSpace(actorRole)) actorRole = GetCurrentBackendActorRole();
            if (string.IsNullOrWhiteSpace(actorUserId)) actorUserId = GetCurrentSessionUserId();

            var raw = await PostSupabaseRpcGetStringAsync(
                cfg,
                "api_pos_create_order_idempotent",
                SerializeJson(payload),
                actorRole,
                actorUserId,
                true).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(raw))
            {
                if (await DoesSupabaseAppOrderExistAsync(cfg, row.Id).ConfigureAwait(false)) return;
                throw new InvalidOperationException("inventory_order_rpc_empty");
            }

            var response = UnwrapSupabaseRpcResult<SupabaseCreateInventoryOrderRpcResponse>(raw);
            if (response == null)
            {
                if (await DoesSupabaseAppOrderExistAsync(cfg, row.Id).ConfigureAwait(false)) return;
                throw new InvalidOperationException("inventory_order_rpc_parse_failed");
            }
            if (response.Ok)
            {
                if (row.Metadata != null && !string.IsNullOrWhiteSpace(row.Metadata.DriverUserId))
                {
                    try
                    {
                        var driverPayload = new SupabasePosAssignDriverIdempotentRequest
                        {
                            OrderId = row.Id,
                            IdempotencyKey = LocalIdempotencyEngine.GenerateIdempotencyKey("DRV"),
                            DriverUserId = row.Metadata.DriverUserId,
                            DriverName = row.Metadata.DriverName
                        };
                        await PostSupabaseRpcExpectBoolAsync(cfg, "api_pos_assign_driver", driverPayload).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Deferred driver assignment failed on completion push: " + ex.Message);
                    }
                }
                return;
            }

            var message = !string.IsNullOrWhiteSpace(response.Error)
                ? response.Error
                : (!string.IsNullOrWhiteSpace(response.Code) ? response.Code : "inventory_order_rpc_failed");
            throw new InvalidOperationException(message);
        }

        /// <summary>
        /// Legacy sync wrapper that now simply does a direct cloud push.
        /// Returns true if the order was pushed (or already exists as a duplicate).
        /// </summary>
        internal bool TryCreatePosOrderOnServerOrFallback(PosOfflineSupabaseOrderInsertRow row, out bool pushedToServer, out string userMessage)
        {
            pushedToServer = false;
            userMessage = null;
            if (row == null)
            {
                userMessage = "تعذر تجهيز الطلب للإرسال.";
                return false;
            }

            SupabaseClientConfigRecord cfg = null;
            try { cfg = LoadSupabaseClientConfigResolved(); } catch { cfg = null; }
            if (cfg == null) return true;

            try
            {
                PushPosOfflineOrderToSupabaseAsync(cfg, row).GetAwaiter().GetResult();
                pushedToServer = true;
                try { QueueSupabasePosMenuPullFromBackend(false); } catch { }
                return true;
            }
            catch (Exception ex)
            {
                var details = ex == null
                    ? string.Empty
                    : (!string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : ex.ToString());

                if (LooksLikeInventoryReservationError(details))
                {
                    userMessage = BuildInventoryOutOfStockUserMessage();
                    return false;
                }

                if (LooksLikeDuplicateOrderInsertError(details))
                {
                    pushedToServer = true;
                    return true;
                }

                userMessage = BuildPosOrderServerFailureUserMessage(details);
                return false;
            }
        }

        internal void TryReleaseServerReservedPosOrder(string backendOrderId)
        {
            var orderId = (backendOrderId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(orderId)) return;

            try
            {
                var cfg = LoadSupabaseClientConfigResolved();
                if (cfg == null) return;

                PostSupabaseRpcExpectBoolAsync(
                    cfg,
                    "api_pos_update_order_status_idempotent",
                    new SupabasePosUpdateOrderStatusIdempotentRequest
                    {
                        OrderId = orderId,
                        ToStatus = "CANCELLED",
                        IdempotencyKey = LocalIdempotencyEngine.GenerateIdempotencyKey("STAT"),
                        ActorRole = GetCurrentBackendActorRole(),
                        ActorUserId = GetCurrentSessionUserId()
                    }).GetAwaiter().GetResult();
            }
            catch
            {
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // ERROR DETECTION HELPERS
        // ──────────────────────────────────────────────────────────────────────

        private string BuildPosOrderServerFailureMessage(string error)
        {
            var trimmed = (error ?? string.Empty).Trim();
            if (trimmed.Length > 220) trimmed = trimmed.Substring(0, 220) + " ...";
            return string.IsNullOrWhiteSpace(trimmed)
                ? "تعذر اعتماد الطلب من السيرفر الآن."
                : "تعذر اعتماد الطلب من السيرفر الآن: " + trimmed;
        }

        private string BuildPosOrderServerFailureUserMessage(string error)
        {
            var trimmed = (error ?? string.Empty).Trim();
            if (trimmed.IndexOf("inventory_order_rpc_empty", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "السيرفر لم يرجع تأكيد واضح للطلب. الرجاء أعد المحاولة إن لم يظهر الطلب.";
            }
            if (trimmed.IndexOf("inventory_order_rpc_parse_failed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "تعذر فهم رد السيرفر عند اعتماد الطلب.";
            }
            if (trimmed.Length > 220) trimmed = trimmed.Substring(0, 220) + " ...";
            return string.IsNullOrWhiteSpace(trimmed)
                ? "تعذر اعتماد الطلب من السيرفر الآن."
                : "تعذر اعتماد الطلب من السيرفر الآن: " + trimmed;
        }

        private string BuildInventoryOutOfStockUserMessage()
        {
            return "لا يمكن إتمام الطلب الآن لأن المخزون لا يكفي لبعض الأصناف أو الإضافات.";
        }

        private bool LooksLikeInventoryReservationError(string error)
        {
            if (string.IsNullOrWhiteSpace(error)) return false;
            return error.IndexOf("inventory_out_of_stock", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("out_of_stock", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("stock_shortage", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool LooksLikeTransientSupabaseOrderCreateError(string error)
        {
            if (string.IsNullOrWhiteSpace(error)) return false;
            return error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("task was canceled", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("operation was canceled", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("operation canceled", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("no such host", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("name or service not known", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("connection refused", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("actively refused", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("unable to connect", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("forcibly closed", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("socket", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("network", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("dns", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("HTTP 500", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("HTTP 502", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("HTTP 503", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("HTTP 504", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("sending the request", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("an error occurred while", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool LooksLikeDuplicateOrderInsertError(string error)
        {
            if (string.IsNullOrWhiteSpace(error)) return false;
            return error.IndexOf("23505", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("duplicate key", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("already exists", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("HTTP 409", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ──────────────────────────────────────────────────────────────────────
        // ORDER EXISTENCE CHECKS — used by PushPosOfflineOrderToSupabaseAsync
        // ──────────────────────────────────────────────────────────────────────

        private async Task<bool> DoesSupabaseAppOrderExistAsync(SupabaseClientConfigRecord cfg, string orderId)
        {
            var id = (orderId ?? string.Empty).Trim();
            if (cfg == null || string.IsNullOrWhiteSpace(id)) return false;

            var tableName = ResolveSupabaseOrdersTable(cfg);
            if (string.IsNullOrWhiteSpace(tableName)) tableName = "app_orders";

            await EnsureSupabaseBackendSessionAsync(cfg, false).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(GetCurrentSessionToken()))
            {
                await EnsureSupabaseBackendSessionAsync(cfg, true).ConfigureAwait(false);
            }

            Func<Task<bool?>> fetch = async delegate
            {
                var url = cfg.Url
                    + "/rest/v1/"
                    + tableName.Trim()
                    + "?select=id&id=eq."
                    + Uri.EscapeDataString(id)
                    + "&limit=1";

                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    ApplySupabaseRestHeaders(req, cfg, true);
                    using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                    using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                    {
                        if (!resp.IsSuccessStatusCode)
                        {
                            if ((int)resp.StatusCode == 401 || (int)resp.StatusCode == 403)
                            {
                                return null;
                            }
                            return false;
                        }

                        var raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var rows = DeserializeJson<List<SupabaseIdOnlyRow>>(raw);
                        return rows != null && rows.Count > 0 && !string.IsNullOrWhiteSpace(rows[0].Id);
                    }
                }
            };

            var exists = await fetch().ConfigureAwait(false);
            if (exists.HasValue) return exists.Value;

            await EnsureSupabaseBackendSessionAsync(cfg, true).ConfigureAwait(false);
            exists = await fetch().ConfigureAwait(false);
            return exists ?? false;
        }

        private async Task<SupabaseAppOrderRow> FetchSupabaseAppOrderByIdAsync(SupabaseClientConfigRecord cfg, string orderId)
        {
            var id = (orderId ?? string.Empty).Trim();
            if (cfg == null || string.IsNullOrWhiteSpace(id)) return null;

            var tableName = ResolveSupabaseOrdersTable(cfg);
            if (string.IsNullOrWhiteSpace(tableName)) tableName = "app_orders";

            await EnsureSupabaseBackendSessionAsync(cfg, false).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(GetCurrentSessionToken()))
            {
                await EnsureSupabaseBackendSessionAsync(cfg, true).ConfigureAwait(false);
            }

            Func<Task<SupabaseAppOrderRow>> fetch = async delegate
            {
                var url = cfg.Url
                    + "/rest/v1/"
                    + tableName.Trim()
                    + "?select="
                    + Uri.EscapeDataString("id,order_type,status,driver_user_id")
                    + "&id=eq."
                    + Uri.EscapeDataString(id)
                    + "&limit=1";

                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    ApplySupabaseRestHeaders(req, cfg, true);
                    using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                    using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                    {
                        if (!resp.IsSuccessStatusCode)
                        {
                            if ((int)resp.StatusCode == 401 || (int)resp.StatusCode == 403)
                            {
                                return null;
                            }
                            return null;
                        }

                        var raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var rows = DeserializeJson<List<SupabaseAppOrderRow>>(raw);
                        return rows != null && rows.Count > 0 ? rows[0] : null;
                    }
                }
            };

            var order = await fetch().ConfigureAwait(false);
            if (order != null) return order;

            await EnsureSupabaseBackendSessionAsync(cfg, true).ConfigureAwait(false);
            return await fetch().ConfigureAwait(false);
        }

        // ──────────────────────────────────────────────────────────────────────
        // ORDER STATUS STAGE HELPERS — used by AppOrderSyncService.cs
        // ──────────────────────────────────────────────────────────────────────

        internal string NormalizeQueuedAppOrderType(string orderType)
        {
            var raw = (orderType ?? string.Empty).Trim().ToUpperInvariant();
            if (raw == "DELIVERY" || raw == "POS_DELIVERY") return "DELIVERY";
            if (raw == "PICKUP" || raw == "POS_PICKUP") return "PICKUP";
            if (raw == "TAKEAWAY" || raw == "POS_TAKEAWAY") return "TAKEAWAY";
            return raw;
        }

        internal int GetQueuedAppOrderStatusStage(string orderType, string status)
        {
            var normalizedType = NormalizeQueuedAppOrderType(orderType);
            var normalizedStatus = (status ?? string.Empty).Trim().ToUpperInvariant();

            if (normalizedType == "DELIVERY")
            {
                if (normalizedStatus == string.Empty || normalizedStatus == "RECEIVED" || normalizedStatus == "PENDING") return 1;
                if (normalizedStatus == "PREPARING" || normalizedStatus == "NO_DRIVER") return 2;
                if (normalizedStatus == "DRIVER_ASSIGNED") return 3;
                if (normalizedStatus == "DRIVER_PICKED") return 4;
                if (normalizedStatus == "ON_ROAD") return 5;
                if (normalizedStatus == "DONE" || normalizedStatus == "DELIVERED") return 6;
                if (normalizedStatus == "CANCELLED" || normalizedStatus == "CANCELED" || normalizedStatus == "REJECTED") return 98;
                if (normalizedStatus == "MIGRATED") return 99;
                return 0;
            }

            if (normalizedType == "PICKUP")
            {
                if (normalizedStatus == string.Empty || normalizedStatus == "RECEIVED" || normalizedStatus == "PENDING" || normalizedStatus == "ACCEPTED") return 1;
                if (normalizedStatus == "PREPARING") return 2;
                if (normalizedStatus == "READY") return 3;
                if (normalizedStatus == "DONE" || normalizedStatus == "DELIVERED") return 4;
                if (normalizedStatus == "CANCELLED" || normalizedStatus == "CANCELED" || normalizedStatus == "REJECTED") return 98;
                if (normalizedStatus == "MIGRATED") return 99;
                return 0;
            }

            return 0;
        }

        internal bool IsQueuedAppOrderTerminalStatus(string status)
        {
            var normalizedStatus = (status ?? string.Empty).Trim().ToUpperInvariant();
            return normalizedStatus == "DONE"
                || normalizedStatus == "DELIVERED"
                || normalizedStatus == "CANCELLED"
                || normalizedStatus == "CANCELED"
                || normalizedStatus == "REJECTED"
                || normalizedStatus == "MIGRATED";
        }

        // ──────────────────────────────────────────────────────────────────────
        // RETAINED SHIFT-MIGRATION QUEUE (delegates to idempotency engine)
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Queues backend order IDs for status MIGRATED push.
        /// Uses the idempotency engine directly instead of the old JSON queue.
        /// </summary>
        internal void EnqueuePosShiftMigrationByOrderIds(List<string> backendOrderIds)
        {
            if (backendOrderIds == null || backendOrderIds.Count == 0) return;
            for (int i = 0; i < backendOrderIds.Count; i++)
            {
                var id = (backendOrderIds[i] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(id)) continue;
                EnqueueIdempotentMutation(new IdempotencyAwareQueueItem
                {
                    OrderId = id,
                    OperationKind = "status",
                    ToStatus = "MIGRATED",
                    CreatedAtMillis = NowUnixMs(),
                    ActorRole = GetCurrentBackendActorRole(),
                    ActorUserId = GetCurrentSessionUserId(),
                    PosShiftId = _activeShiftId
                });
            }
        }
    }
}

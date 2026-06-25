using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        private readonly object _posOfflineQueueSync = new object();
        private readonly List<PosOfflineOrderQueueItem> _posOfflineQueue = new List<PosOfflineOrderQueueItem>();
        private readonly List<PendingDeliveryAddressDeleteQueueItem> _pendingDeliveryAddressDeletes = new List<PendingDeliveryAddressDeleteQueueItem>();
        private readonly List<AppOrderMutationQueueItem> _pendingAppOrderMutations = new List<AppOrderMutationQueueItem>();
        private readonly object _posShiftMigrationSync = new object();
        private readonly List<string> _pendingPosShiftMigrationOrderIds = new List<string>();
        private DispatcherTimer _posOfflineSyncTimer;
        private bool _posOfflineQueueLoaded;
        private bool _pendingDeliveryAddressDeletesLoaded;
        private bool _pendingAppOrderMutationsLoaded;
        private bool _pendingPosShiftMigrationLoaded;
        private bool _posOfflineSyncInFlight;
        private bool _posOfflineSyncPending;
        private int _posOfflineConnectivityProbeSuccessStreak;
        private DateTime _posOfflineLastBlockedNoticeAtUtc = DateTime.MinValue;
        private DateTime _posOfflineLastAddressSyncAtUtc = DateTime.MinValue;
        private DateTime _posOfflineLastCashierSyncAtUtc = DateTime.MinValue;
        private DateTime _posOfflineLastShiftMigrationPushAtUtc = DateTime.MinValue;

        private string PosOfflineOrdersQueueFilePath
        {
            get { return Path.Combine(AppDataQueueDirPath, "pos_offline_orders_queue.json"); }
        }

        private string PosShiftMigrationQueueFilePath
        {
            get { return Path.Combine(AppDataQueueDirPath, "pos_shift_migration_queue.json"); }
        }

        private string DeliveryAddressDeleteQueueFilePath
        {
            get { return Path.Combine(AppDataQueueDirPath, "delivery_address_delete_queue.json"); }
        }

        private string AppOrderMutationsQueueFilePath
        {
            get { return Path.Combine(AppDataQueueDirPath, "app_order_mutations_queue.json"); }
        }

        private void InitializePosOfflineSensing()
        {
            EnsurePosOfflineQueueLoaded();
            EnsurePendingDeliveryAddressDeletesLoaded();
            EnsurePendingAppOrderMutationsLoaded();
            EnsurePosShiftMigrationQueueLoaded();
            EnsureIdqLoaded(); // PATCH: تحميل Queue الـ Idempotency الجديدة
            if (_posOfflineSyncTimer != null) return;

            _posOfflineSyncTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _posOfflineSyncTimer.Tick += PosOfflineSyncTimer_Tick;
            _posOfflineSyncTimer.Start();

            QueuePosOfflineSync();
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
            QueuePosOfflineSync();
            _ = ProcessIdempotencyQueueAsync(); // PATCH: معالجة Queue الجديدة
        }

        private bool TryBlockNewOrderWhileSyncing()
        {
            var shouldNotify = false;
            lock (_posOfflineQueueSync)
            {
                if (!(_posOfflineSyncInFlight && _posOfflineQueue.Count > 0)) return false;
                var nowUtc = DateTime.UtcNow;
                if ((nowUtc - _posOfflineLastBlockedNoticeAtUtc).TotalSeconds >= 4)
                {
                    _posOfflineLastBlockedNoticeAtUtc = nowUtc;
                    shouldNotify = true;
                }
            }

            if (!shouldNotify) return true;
            Dispatcher.BeginInvoke(new Action(delegate
            {
                ShowSupabaseToast("POS Offline Sync", "Offline orders are syncing. Try again in a few seconds.", false);
            }), DispatcherPriority.Background);
            return true;
        }

        private bool HasLocalDeliveryAddressesCached()
        {
            try
            {
                if (Dispatcher == null) return false;
                if (Dispatcher.CheckAccess()) return _deliveryAddressesDb.Count > 0;
                return Dispatcher.Invoke(new Func<bool>(delegate { return _deliveryAddressesDb.Count > 0; }));
            }
            catch
            {
                return false;
            }
        }

        private Dictionary<string, List<DeliverySavedAddressRecord>> CaptureLocalDeliveryAddressesSnapshot()
        {
            try
            {
                if (Dispatcher == null) return new Dictionary<string, List<DeliverySavedAddressRecord>>(StringComparer.Ordinal);
                if (!Dispatcher.CheckAccess())
                {
                    return Dispatcher.Invoke(new Func<Dictionary<string, List<DeliverySavedAddressRecord>>>(CaptureLocalDeliveryAddressesSnapshotCore));
                }
                return CaptureLocalDeliveryAddressesSnapshotCore();
            }
            catch
            {
                return new Dictionary<string, List<DeliverySavedAddressRecord>>(StringComparer.Ordinal);
            }
        }

        private Dictionary<string, List<DeliverySavedAddressRecord>> CaptureLocalDeliveryAddressesSnapshotCore()
        {
            var snapshot = new Dictionary<string, List<DeliverySavedAddressRecord>>(StringComparer.Ordinal);
            foreach (var kv in _deliveryAddressesDb)
            {
                var phone = (kv.Key ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(phone)) continue;
                var src = kv.Value ?? new List<DeliverySavedAddressRecord>();
                var copy = new List<DeliverySavedAddressRecord>();
                for (int i = 0; i < src.Count; i++)
                {
                    var addr = src[i];
                    if (addr == null) continue;
                    copy.Add(CloneDeliverySavedAddress(addr));
                }
                snapshot[phone] = copy;
            }
            return snapshot;
        }

        private void ApplySyncedDeliveryAddresses(Dictionary<string, List<DeliverySavedAddressRecord>> synced)
        {
            if (synced == null || synced.Count == 0) return;

            Action apply = delegate
            {
                var anyChanged = false;
                foreach (var kv in synced)
                {
                    var phone = (kv.Key ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(phone)) continue;

                    var next = kv.Value ?? new List<DeliverySavedAddressRecord>();
                    List<DeliverySavedAddressRecord> current;
                    _deliveryAddressesDb.TryGetValue(phone, out current);
                    if (!AreDeliveryAddressListsEquivalent(current, next))
                    {
                        var normalized = new List<DeliverySavedAddressRecord>();
                        for (int i = 0; i < next.Count; i++)
                        {
                            var row = next[i];
                            if (row == null) continue;
                            normalized.Add(CloneDeliverySavedAddress(row));
                        }
                        _deliveryAddressesDb[phone] = normalized;
                        anyChanged = true;
                    }
                }

                if (!anyChanged) return;
                PersistDeliveryState();

                if (DeliveryFormOverlay != null && DeliveryFormOverlay.Visibility == System.Windows.Visibility.Visible)
                {
                    RenderDeliveryFormAddressList();
                }
            };

            try
            {
                if (Dispatcher == null || Dispatcher.CheckAccess())
                {
                    apply();
                }
                else
                {
                    Dispatcher.Invoke(apply);
                }
            }
            catch
            {
            }
        }

        private List<DeliverySavedAddressRecord> FilterPendingDeliveryAddressDeletes(string phone, List<DeliverySavedAddressRecord> rows)
        {
            rows = rows ?? new List<DeliverySavedAddressRecord>();
            if (rows.Count == 0) return rows;

            var pendingDeletes = GetPendingDeliveryAddressDeleteSnapshot();
            if (pendingDeletes == null || pendingDeletes.Count == 0) return rows;

            var filtered = new List<DeliverySavedAddressRecord>(rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null) continue;
                if (IsPendingDeliveryAddressDelete(phone, row.Id, pendingDeletes)) continue;
                filtered.Add(row);
            }
            return filtered;
        }

        private async Task RunPosOfflineAddressSyncPassAsync(SupabaseClientConfigRecord cfg)
        {
            if (cfg == null) return;

            var nowUtc = DateTime.UtcNow;
            lock (_posOfflineQueueSync)
            {
                if ((nowUtc - _posOfflineLastAddressSyncAtUtc).TotalSeconds < 4) return;
                _posOfflineLastAddressSyncAtUtc = nowUtc;
            }

            var snapshot = CaptureLocalDeliveryAddressesSnapshot();
            if (snapshot == null || snapshot.Count == 0) return;

            var syncedByPhone = new Dictionary<string, List<DeliverySavedAddressRecord>>(StringComparer.Ordinal);
            foreach (var kv in snapshot)
            {
                var phone = (kv.Key ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(phone)) continue;

                var localList = kv.Value ?? new List<DeliverySavedAddressRecord>();
                var merged = new List<DeliverySavedAddressRecord>();

                try
                {
                    var rows = await FetchPosRegisteredAddressesByPhoneAsync(cfg, phone).ConfigureAwait(false);
                    for (int i = 0; i < rows.Count; i++)
                    {
                        var mapped = MapSupabasePosRegisteredAddress(rows[i]);
                        if (mapped != null) merged.Add(mapped);
                    }
                }
                catch
                {
                    continue;
                }

                merged = FilterPendingDeliveryAddressDeletes(phone, merged);

                // Merge local cache with backend list, and push pending local changes.
                MergeAndSyncLocalAddressesForPhone(phone, merged, localList, true);
                syncedByPhone[phone] = merged;
            }

            ApplySyncedDeliveryAddresses(syncedByPhone);
        }

        private async Task RunPendingDeliveryAddressDeletesPushPassAsync(SupabaseClientConfigRecord cfg)
        {
            if (cfg == null) return;

            var pending = GetPendingDeliveryAddressDeleteSnapshot();
            if (pending == null || pending.Count == 0) return;

            for (int i = 0; i < pending.Count; i++)
            {
                var item = pending[i];
                int parsedId;
                if (item == null || !int.TryParse((item.AddressId ?? string.Empty).Trim(), out parsedId) || parsedId <= 0)
                {
                    lock (_posOfflineQueueSync)
                    {
                        if (item != null) RemovePendingDeliveryAddressDeleteByQueueIdNoLock(item.QueueId);
                        PersistPendingDeliveryAddressDeletesNoLock();
                    }
                    continue;
                }

                try
                {
                    await DeletePosRegisteredAddressByIdAsync(cfg, parsedId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (!LooksLikeObsoleteDeliveryAddressDeleteError(ex.Message)) break;
                }

                lock (_posOfflineQueueSync)
                {
                    RemovePendingDeliveryAddressDeleteByQueueIdNoLock(item.QueueId);
                    PersistPendingDeliveryAddressDeletesNoLock();
                }
            }
        }

        private bool LooksLikeObsoleteDeliveryAddressDeleteError(string error)
        {
            var message = (error ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(message)) return false;
            return message.IndexOf("HTTP 404", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async Task RunPosOfflineCashierCredentialsSyncPassAsync(SupabaseClientConfigRecord cfg)
        {
            if (cfg == null) return;

            var nowUtc = DateTime.UtcNow;
            lock (_posOfflineQueueSync)
            {
                if ((nowUtc - _posOfflineLastCashierSyncAtUtc).TotalSeconds < 10) return;
                _posOfflineLastCashierSyncAtUtc = nowUtc;
            }

            List<SupabaseCashierCredentialRow> rows;
            try
            {
                rows = await FetchSupabaseCashierCredentialsSnapshotAsync(cfg).ConfigureAwait(false);
            }
            catch
            {
                return;
            }

            if (rows == null || rows.Count == 0) return;
            MergeLocalCashierCredentialsFromSupabase(rows);
        }

        private async Task RunPosOfflineCashierCredentialsPushPassAsync(SupabaseClientConfigRecord cfg)
        {
            if (cfg == null) return;

            var dirty = GetDirtyLocalCashierCredentialsSnapshot();
            if (dirty == null || dirty.Count == 0) return;

            var synced = new List<string>();
            for (var i = 0; i < dirty.Count; i++)
            {
                var row = dirty[i];
                if (row == null) continue;
                try
                {
                    var ok = await PushLocalCashierCredentialToSupabaseAsync(cfg, row).ConfigureAwait(false);
                    if (ok)
                    {
                        var id = (row.UserId ?? string.Empty).Trim();
                        if (!string.IsNullOrWhiteSpace(id)) synced.Add(id);
                    }
                }
                catch
                {
                }
            }

            if (synced.Count > 0) MarkLocalCashierCredentialsAsSynced(synced);
        }

        private void EnqueuePosShiftMigrationByOrderIds(List<string> backendOrderIds)
        {
            if (backendOrderIds == null || backendOrderIds.Count == 0) return;
            EnsurePosShiftMigrationQueueLoaded();

            var changed = false;
            lock (_posShiftMigrationSync)
            {
                for (int i = 0; i < backendOrderIds.Count; i++)
                {
                    var id = (backendOrderIds[i] ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    if (ContainsPendingPosShiftMigrationOrderIdNoLock(id)) continue;
                    _pendingPosShiftMigrationOrderIds.Add(id);
                    changed = true;
                }

                if (changed) PersistPosShiftMigrationQueueNoLock();
            }

            if (changed) QueuePosOfflineSync();
        }

        private bool HasPendingPosShiftMigrationWork()
        {
            EnsurePosShiftMigrationQueueLoaded();
            lock (_posShiftMigrationSync)
            {
                return _pendingPosShiftMigrationOrderIds.Count > 0;
            }
        }

        private void EnsurePosShiftMigrationQueueLoaded()
        {
            lock (_posShiftMigrationSync)
            {
                if (_pendingPosShiftMigrationLoaded) return;
                _pendingPosShiftMigrationLoaded = true;
                _pendingPosShiftMigrationOrderIds.Clear();

                var state = ReadJson<PosShiftMigrationQueueState>(PosShiftMigrationQueueFilePath);
                var ids = state == null ? null : state.OrderIds;
                if (ids == null) return;

                for (int i = 0; i < ids.Count; i++)
                {
                    var id = (ids[i] ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    if (ContainsPendingPosShiftMigrationOrderIdNoLock(id)) continue;
                    _pendingPosShiftMigrationOrderIds.Add(id);
                }
            }
        }

        private void PersistPosShiftMigrationQueueNoLock()
        {
            var state = new PosShiftMigrationQueueState
            {
                Version = 1,
                OrderIds = new List<string>(_pendingPosShiftMigrationOrderIds)
            };
            WriteJson(PosShiftMigrationQueueFilePath, state);
        }

        private bool ContainsPendingPosShiftMigrationOrderIdNoLock(string backendOrderId)
        {
            var id = (backendOrderId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id)) return false;
            for (int i = 0; i < _pendingPosShiftMigrationOrderIds.Count; i++)
            {
                if (string.Equals(_pendingPosShiftMigrationOrderIds[i], id, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private List<string> GetPendingPosShiftMigrationSnapshot()
        {
            EnsurePosShiftMigrationQueueLoaded();
            lock (_posShiftMigrationSync)
            {
                return new List<string>(_pendingPosShiftMigrationOrderIds);
            }
        }

        private void MarkPosShiftMigrationAsSynced(List<string> backendOrderIds)
        {
            if (backendOrderIds == null || backendOrderIds.Count == 0) return;
            EnsurePosShiftMigrationQueueLoaded();

            lock (_posShiftMigrationSync)
            {
                var changed = false;
                for (int i = 0; i < backendOrderIds.Count; i++)
                {
                    var id = (backendOrderIds[i] ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    for (int j = _pendingPosShiftMigrationOrderIds.Count - 1; j >= 0; j--)
                    {
                        if (!string.Equals(_pendingPosShiftMigrationOrderIds[j], id, StringComparison.OrdinalIgnoreCase)) continue;
                        _pendingPosShiftMigrationOrderIds.RemoveAt(j);
                        changed = true;
                    }
                }

                if (changed) PersistPosShiftMigrationQueueNoLock();
            }
        }

        private async Task<bool> TryMarkSupabaseOrderAsMigratedAsync(SupabaseClientConfigRecord cfg, string backendOrderId)
        {
            var orderId = (backendOrderId ?? string.Empty).Trim();
            if (cfg == null || string.IsNullOrWhiteSpace(orderId)) return false;

            var payload = new SupabaseRpcUpdateAppOrderStatusRequest
            {
                OrderId = orderId,
                ToStatus = "MIGRATED",
                ActorRole = GetCurrentBackendActorRole(),
                ActorUserId = GetCurrentSessionUserId(),
                MarkKitchenPrinted = null,
                MarkDriverReceiptPrinted = null,
                MarkPartnerReceiptPrinted = null
            };

            var raw = await PostSupabaseRpcGetStringAsync(
                cfg,
                "api_update_app_order_status",
                SerializeJson(payload),
                actorRoleOverride: GetCurrentBackendActorRole(),
                actorUserIdOverride: GetCurrentSessionUserId()).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var normalized = raw.Trim();
            if (string.Equals(normalized, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(normalized, "false", StringComparison.OrdinalIgnoreCase)) return false;
            if (normalized.IndexOf("\"ok\":true", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (normalized.IndexOf("true", StringComparison.OrdinalIgnoreCase) >= 0
                && normalized.IndexOf("false", StringComparison.OrdinalIgnoreCase) < 0) return true;
            return false;
        }

        private async Task RunPosShiftMigrationPushPassAsync(SupabaseClientConfigRecord cfg)
        {
            if (cfg == null) return;

            var nowUtc = DateTime.UtcNow;
            lock (_posOfflineQueueSync)
            {
                if ((nowUtc - _posOfflineLastShiftMigrationPushAtUtc).TotalSeconds < 3) return;
                _posOfflineLastShiftMigrationPushAtUtc = nowUtc;
            }

            var pending = GetPendingPosShiftMigrationSnapshot();
            if (pending == null || pending.Count == 0) return;

            var synced = new List<string>();
            for (int i = 0; i < pending.Count; i++)
            {
                var id = pending[i];
                if (string.IsNullOrWhiteSpace(id)) continue;

                bool ok;
                try
                {
                    ok = await TryMarkSupabaseOrderAsMigratedAsync(cfg, id).ConfigureAwait(false);
                }
                catch
                {
                    ok = false;
                }

                if (!ok) continue;
                synced.Add(id);
            }

            if (synced.Count > 0) MarkPosShiftMigrationAsSynced(synced);
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

        private void QueuePosOfflineTakeawayOrderCompletion(TakeawayOrderRecord order, TakeawayPickupRecord pickup)
        {
            var row = BuildPosOfflineTakeawayOrderInsertRow(order, pickup);
            if (row == null) return;

            var createdAtMs = order.CreatedAt > 0 ? order.CreatedAt : NowUnixMs();
            var finalizedAtMs = NowUnixMs();
            var orderType = pickup == null ? "POS_TAKEAWAY" : "POS_PICKUP";

            EnqueuePosOfflineOrder(new PosOfflineOrderQueueItem
            {
                QueueId = "q_" + row.Id,
                LocalOrderId = order.Id ?? string.Empty,
                OrderType = orderType,
                CreatedAtMillis = createdAtMs,
                FinalizedAtMillis = finalizedAtMs,
                Row = row
            });
        }

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
                OrderType = orderType,
                Status = "DONE",
                CustomerUserId = null,
                CustomerName = string.IsNullOrWhiteSpace(order.CustomerName) ? "Cashier Customer" : order.CustomerName.Trim(),
                CustomerPhone = (order.CustomerPhone ?? string.Empty).Trim(),
                District = (order.District ?? string.Empty).Trim(),
                AddressText = BuildDeliveryAddressText(order),
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
                    cashierRole: !string.IsNullOrWhiteSpace(order.CashierRole) ? order.CashierRole : GetCurrentBackendActorRole())
            };
        }

        private void QueuePosOfflineDeliveryOrderCompletion(DeliveryOrderRecord order)
        {
            var row = BuildPosOfflineDeliveryOrderInsertRow(order);
            if (row == null) return;

            var createdAtMs = order.CreatedAt > 0 ? order.CreatedAt : NowUnixMs();
            var finalizedAtMs = NowUnixMs();

            EnqueuePosOfflineOrder(new PosOfflineOrderQueueItem
            {
                QueueId = "q_" + row.Id,
                LocalOrderId = order.Id ?? string.Empty,
                OrderType = "POS_DELIVERY",
                CreatedAtMillis = createdAtMs,
                FinalizedAtMillis = finalizedAtMs,
                Row = row
            });
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
            string cashierRole)
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
                Items = items ?? new List<PosOfflineOrderMetadataItem>()
            };
        }

        private string BuildPosOfflineBackendOrderId(string localOrderId)
        {
            var safe = string.IsNullOrWhiteSpace(localOrderId) ? NewId() : localOrderId.Trim();
            return "pos_" + safe;
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
                || error.IndexOf("HTTP 504", StringComparison.OrdinalIgnoreCase) >= 0;
        }

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
                return "\u0627\u0644\u0633\u064a\u0631\u0641\u0631 \u0644\u0645 \u064a\u0631\u062c\u0639 \u062a\u0623\u0643\u064a\u062f \u0648\u0627\u0636\u062d \u0644\u0644\u0637\u0644\u0628. \u0627\u0644\u0631\u062c\u0627\u0621 \u0623\u0639\u062f \u0627\u0644\u0645\u062d\u0627\u0648\u0644\u0629 \u0625\u0646 \u0644\u0645 \u064a\u0638\u0647\u0631 \u0627\u0644\u0637\u0644\u0628.";
            }
            if (trimmed.IndexOf("inventory_order_rpc_parse_failed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "\u062a\u0639\u0630\u0631 \u0641\u0647\u0645 \u0631\u062f \u0627\u0644\u0633\u064a\u0631\u0641\u0631 \u0639\u0646\u062f \u0627\u0639\u062a\u0645\u0627\u062f \u0627\u0644\u0637\u0644\u0628.";
            }
            if (trimmed.Length > 220) trimmed = trimmed.Substring(0, 220) + " ...";
            return string.IsNullOrWhiteSpace(trimmed)
                ? "\u062a\u0639\u0630\u0631 \u0627\u0639\u062a\u0645\u0627\u062f \u0627\u0644\u0637\u0644\u0628 \u0645\u0646 \u0627\u0644\u0633\u064a\u0631\u0641\u0631 \u0627\u0644\u0622\u0646."
                : "\u062a\u0639\u0630\u0631 \u0627\u0639\u062a\u0645\u0627\u062f \u0627\u0644\u0637\u0644\u0628 \u0645\u0646 \u0627\u0644\u0633\u064a\u0631\u0641\u0631 \u0627\u0644\u0622\u0646: " + trimmed;
        }

        private string BuildInventoryOutOfStockUserMessage()
        {
            return "\u0644\u0627 \u064a\u0645\u0643\u0646 \u0625\u062a\u0645\u0627\u0645 \u0627\u0644\u0637\u0644\u0628 \u0627\u0644\u0622\u0646 \u0644\u0623\u0646 \u0627\u0644\u0645\u062e\u0632\u0648\u0646 \u0644\u0627 \u064a\u0643\u0641\u064a \u0644\u0628\u0639\u0636 \u0627\u0644\u0623\u0635\u0646\u0627\u0641 \u0623\u0648 \u0627\u0644\u0625\u0636\u0627\u0641\u0627\u062a.";
        }

        private bool TryCreatePosOrderOnServerOrFallback(PosOfflineSupabaseOrderInsertRow row, out bool pushedToServer, out string userMessage)
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
                if (LooksLikeInventoryReservationError(details))
                {
                    userMessage = "لا يمكن إتمام الطلب الآن لأن المخزون لا يكفي لبعض الأصناف أو الإضافات.";
                    return false;
                }
                if (LooksLikeTransientSupabaseOrderCreateError(details))
                {
                    return true;
                }

                userMessage = BuildPosOrderServerFailureUserMessage(details);
                return false;
            }
        }

        private void TryReleaseServerReservedPosOrder(string backendOrderId)
        {
            var orderId = (backendOrderId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(orderId)) return;

            try
            {
                var cfg = LoadSupabaseClientConfigResolved();
                if (cfg == null) return;

                PostSupabaseRpcExpectBoolAsync(
                    cfg,
                    "api_update_app_order_status",
                    new SupabaseRpcUpdateAppOrderStatusRequest
                    {
                        OrderId = orderId,
                        ToStatus = "CANCELLED",
                        ActorRole = GetCurrentBackendActorRole(),
                        ActorUserId = GetCurrentSessionUserId()
                    }).GetAwaiter().GetResult();
            }
            catch
            {
            }
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

        private void EnsurePosOfflineQueueLoaded()
        {
            lock (_posOfflineQueueSync)
            {
                if (_posOfflineQueueLoaded) return;
                _posOfflineQueueLoaded = true;
                _posOfflineQueue.Clear();

                var persisted = ReadJson<PosOfflineOrderQueueState>(PosOfflineOrdersQueueFilePath);
                if (persisted == null || persisted.Items == null) return;

                for (int i = 0; i < persisted.Items.Count; i++)
                {
                    var item = persisted.Items[i];
                    if (item == null || item.Row == null || string.IsNullOrWhiteSpace(item.Row.Id)) continue;
                    if (item.CreatedAtMillis <= 0) item.CreatedAtMillis = NowUnixMs();
                    if (item.FinalizedAtMillis <= 0) item.FinalizedAtMillis = item.CreatedAtMillis;
                    if (string.IsNullOrWhiteSpace(item.QueueId)) item.QueueId = "q_" + item.Row.Id;
                    _posOfflineQueue.Add(item);
                }

                SortPosOfflineQueueNoLock();
            }
        }

        private void EnqueuePosOfflineOrder(PosOfflineOrderQueueItem item)
        {
            if (item == null || item.Row == null || string.IsNullOrWhiteSpace(item.Row.Id)) return;
            EnsurePosOfflineQueueLoaded();

            lock (_posOfflineQueueSync)
            {
                for (int i = 0; i < _posOfflineQueue.Count; i++)
                {
                    var existing = _posOfflineQueue[i];
                    if (existing == null || existing.Row == null) continue;
                    if (string.Equals(existing.Row.Id, item.Row.Id, StringComparison.OrdinalIgnoreCase)) return;
                }

                if (item.CreatedAtMillis <= 0) item.CreatedAtMillis = NowUnixMs();
                if (item.FinalizedAtMillis <= 0) item.FinalizedAtMillis = item.CreatedAtMillis;
                if (string.IsNullOrWhiteSpace(item.QueueId)) item.QueueId = "q_" + item.Row.Id;
                _posOfflineQueue.Add(item);
                SortPosOfflineQueueNoLock();
                PersistPosOfflineQueueNoLock();
            }

            QueuePosOfflineSync();
        }

        private void QueuePosOfflineSync()
        {
            EnsurePosOfflineQueueLoaded();
            EnsurePendingDeliveryAddressDeletesLoaded();
            EnsurePendingAppOrderMutationsLoaded();
            EnsurePosShiftMigrationQueueLoaded();
            var hasAddressSyncWork = HasLocalDeliveryAddressesCached();
            var hasAddressDeleteWork = HasPendingDeliveryAddressDeleteWork();
            // Keep staff credential cache fresh even on login screen.
            var hasCashierSyncWork = true;
            var hasAppOrderMutationWork = HasPendingAppOrderMutationWork();
            var hasShiftMigrationWork = HasPendingPosShiftMigrationWork();
            lock (_posOfflineQueueSync)
            {
                if (_posOfflineQueue.Count == 0 && !hasAddressSyncWork && !hasAddressDeleteWork && !hasCashierSyncWork && !hasAppOrderMutationWork && !hasShiftMigrationWork) return;
                if (_posOfflineSyncInFlight)
                {
                    _posOfflineSyncPending = true;
                    return;
                }

                _posOfflineSyncInFlight = true;
                _posOfflineSyncPending = false;
            }

            Task.Run(async delegate
            {
                while (true)
                {
                    try
                    {
                        await RunPosOfflineSyncPassAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("POS offline sync pass failed: " + ex.Message);
                    }

                    lock (_posOfflineQueueSync)
                    {
                        if (_posOfflineSyncPending)
                        {
                            _posOfflineSyncPending = false;
                            continue;
                        }

                        _posOfflineSyncInFlight = false;
                        break;
                    }
                }
            });
        }

        private async Task RunPosOfflineAppOrderMutationsPushPassAsync(SupabaseClientConfigRecord cfg)
        {
            if (cfg == null) return;

            await EnsureSupabaseBackendSessionAsync(cfg, false).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(GetCurrentSessionToken()))
            {
                await EnsureSupabaseBackendSessionAsync(cfg, true).ConfigureAwait(false);
            }
            if (string.IsNullOrWhiteSpace(GetCurrentSessionToken()))
            {
                return;
            }

            var pending = GetPendingAppOrderMutationsSnapshot();
            if (pending == null || pending.Count == 0) return;

            var shouldPullFreshOrders = false;
            var blockedOrderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < pending.Count; i++)
            {
                var item = pending[i];
                if (item == null || string.IsNullOrWhiteSpace(item.OrderId) || string.IsNullOrWhiteSpace(item.OperationKind))
                {
                    lock (_posOfflineQueueSync)
                    {
                        if (item != null) RemovePendingAppOrderMutationByQueueIdNoLock(item.QueueId);
                        PersistPendingAppOrderMutationsNoLock();
                    }
                    continue;
                }

                if (blockedOrderIds.Contains(item.OrderId.Trim()))
                {
                    continue;
                }

                var applied = false;
                try
                {
                    var op = item.OperationKind.Trim().ToLowerInvariant();
                    if (op == "status")
                    {
                        applied = await UpdateAppOrderStatusWithFallbackAsync(cfg, new SupabaseRpcUpdateAppOrderStatusRequest
                        {
                            OrderId = item.OrderId,
                            ToStatus = item.ToStatus,
                            ActorRole = item.ActorRole,
                            ActorUserId = item.ActorUserId,
                            MarkKitchenPrinted = item.MarkKitchenPrinted,
                            MarkDriverReceiptPrinted = item.MarkDriverReceiptPrinted,
                            MarkPartnerReceiptPrinted = item.MarkPartnerReceiptPrinted
                        }).ConfigureAwait(false);
                    }
                    else if (op == "delivery_address")
                    {
                        await UpdateAppOrderAddressWithFallbackAsync(cfg, item.OrderId, item.District, item.AddressText).ConfigureAwait(false);
                        applied = true;
                    }
                    else if (op == "customer_delivery_address")
                    {
                        await UpdateCustomerDeliveryAddressAndOrdersWithFallbackAsync(cfg, item).ConfigureAwait(false);
                        applied = true;
                    }
                    else if (op == "assign_driver")
                    {
                        applied = await AssignDriverWithFallbackAsync(cfg, new SupabaseRpcAssignDriverRequest
                        {
                            OrderId = item.OrderId,
                            DriverUserId = item.DriverUserId,
                            DriverName = item.DriverName,
                            DriverPhone = item.DriverPhone,
                            ActorRole = item.ActorRole,
                            MarkDriverReceiptPrinted = item.MarkDriverReceiptPrinted
                        }).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    if (!LooksLikeObsoleteAppOrderMutationError(ex.Message)) break;

                    lock (_posOfflineQueueSync)
                    {
                        RemovePendingAppOrderMutationByQueueIdNoLock(item.QueueId);
                        PersistPendingAppOrderMutationsNoLock();
                    }
                    shouldPullFreshOrders = true;
                    continue;
                }

                if (!applied)
                {
                    if (await ShouldDiscardPendingAppOrderMutationAsync(cfg, item).ConfigureAwait(false))
                    {
                        AppendPosRuntimeLog("AppOrderMutationSync", "discarded stale mutation. op=" + item.OperationKind + ", order=" + item.OrderId);
                        lock (_posOfflineQueueSync)
                        {
                            RemovePendingAppOrderMutationByQueueIdNoLock(item.QueueId);
                            PersistPendingAppOrderMutationsNoLock();
                        }
                        shouldPullFreshOrders = true;
                        continue;
                    }

                    AppendPosRuntimeLog("AppOrderMutationSync", "mutation returned false and remains queued. op=" + item.OperationKind + ", order=" + item.OrderId);
                    Debug.WriteLine("App order mutation push returned false; keeping item queued. op=" + item.OperationKind + ", order=" + item.OrderId);
                    blockedOrderIds.Add(item.OrderId.Trim());
                    shouldPullFreshOrders = true;
                    continue;
                }

                lock (_posOfflineQueueSync)
                {
                    RemovePendingAppOrderMutationByQueueIdNoLock(item.QueueId);
                    PersistPendingAppOrderMutationsNoLock();
                }

                shouldPullFreshOrders = true;
            }

            if (shouldPullFreshOrders)
            {
                QueueSupabaseAppOrdersPullFromBackend();
            }
        }

        private bool LooksLikeObsoleteAppOrderMutationError(string error)
        {
            var message = (error ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(message)) return false;
            return message.IndexOf("HTTP 404", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("HTTP 409", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("invalid_app_order_address_patch", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async Task RunPosOfflineSyncPassAsync()
        {
            EnsurePosOfflineQueueLoaded();
            EnsurePendingDeliveryAddressDeletesLoaded();
            EnsurePendingAppOrderMutationsLoaded();

            SupabaseClientConfigRecord cfg = null;
            try { cfg = LoadSupabaseClientConfigResolved(); } catch { cfg = null; }
            if (cfg == null)
            {
                lock (_posOfflineQueueSync) _posOfflineConnectivityProbeSuccessStreak = 0;
                return;
            }

            var probeOk = await TrySupabaseConnectivityProbeAsync(cfg).ConfigureAwait(false);
            lock (_posOfflineQueueSync)
            {
                if (!probeOk)
                {
                    _posOfflineConnectivityProbeSuccessStreak = 0;
                    return;
                }

                _posOfflineConnectivityProbeSuccessStreak += 1;
                if (_posOfflineConnectivityProbeSuccessStreak < 2) return;
            }

            await RunPosOfflineCashierCredentialsPushPassAsync(cfg).ConfigureAwait(false);
            await RunPosOfflineCashierCredentialsSyncPassAsync(cfg).ConfigureAwait(false);
            await RunPendingDeliveryAddressDeletesPushPassAsync(cfg).ConfigureAwait(false);
            await RunPosOfflineAddressSyncPassAsync(cfg).ConfigureAwait(false);
            await RunPosOfflineAppOrderMutationsPushPassAsync(cfg).ConfigureAwait(false);
            await RunPosShiftMigrationPushPassAsync(cfg).ConfigureAwait(false);
            QueueSupabaseBackendSnapshotExport();

            while (true)
            {
                PosOfflineOrderQueueItem next = null;
                lock (_posOfflineQueueSync)
                {
                    if (_posOfflineQueue.Count == 0) return;
                    SortPosOfflineQueueNoLock();
                    next = _posOfflineQueue[0];
                }

                if (next == null || next.Row == null || string.IsNullOrWhiteSpace(next.Row.Id))
                {
                    lock (_posOfflineQueueSync)
                    {
                        if (_posOfflineQueue.Count > 0) _posOfflineQueue.RemoveAt(0);
                        PersistPosOfflineQueueNoLock();
                    }
                    continue;
                }

                try
                {
                    await PushPosOfflineOrderToSupabaseAsync(cfg, next.Row).ConfigureAwait(false);

                    lock (_posOfflineQueueSync)
                    {
                        RemoveQueuedItemByIdNoLock(next.Row.Id);
                        PersistPosOfflineQueueNoLock();
                    }
                }
                catch (Exception ex)
                {
                    var message = ex.Message ?? string.Empty;
                    if (LooksLikeDuplicateOrderInsertError(message))
                    {
                        lock (_posOfflineQueueSync)
                        {
                            RemoveQueuedItemByIdNoLock(next.Row.Id);
                            PersistPosOfflineQueueNoLock();
                        }
                        continue;
                    }

                    Debug.WriteLine("POS offline sync failed for order " + next.Row.Id + ": " + message);
                    break;
                }
            }
        }

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

        private string NormalizeQueuedAppOrderType(string orderType)
        {
            var raw = (orderType ?? string.Empty).Trim().ToUpperInvariant();
            if (raw == "DELIVERY" || raw == "POS_DELIVERY") return "DELIVERY";
            if (raw == "PICKUP" || raw == "POS_PICKUP") return "PICKUP";
            if (raw == "TAKEAWAY" || raw == "POS_TAKEAWAY") return "TAKEAWAY";
            return raw;
        }

        private int GetQueuedAppOrderStatusStage(string orderType, string status)
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

        private bool IsQueuedAppOrderTerminalStatus(string status)
        {
            var normalizedStatus = (status ?? string.Empty).Trim().ToUpperInvariant();
            return normalizedStatus == "DONE"
                || normalizedStatus == "DELIVERED"
                || normalizedStatus == "CANCELLED"
                || normalizedStatus == "CANCELED"
                || normalizedStatus == "REJECTED"
                || normalizedStatus == "MIGRATED";
        }

        private async Task<bool> ShouldDiscardPendingAppOrderMutationAsync(SupabaseClientConfigRecord cfg, AppOrderMutationQueueItem item)
        {
            if (cfg == null || item == null || string.IsNullOrWhiteSpace(item.OrderId)) return false;

            var order = await FetchSupabaseAppOrderByIdAsync(cfg, item.OrderId).ConfigureAwait(false);
            if (order == null || string.IsNullOrWhiteSpace(order.Id))
            {
                return false;
            }

            var operation = (item.OperationKind ?? string.Empty).Trim().ToLowerInvariant();
            var currentStatus = (order.Status ?? string.Empty).Trim().ToUpperInvariant();
            var currentOrderType = NormalizeQueuedAppOrderType(order.OrderType);

            if (operation == "assign_driver")
            {
                if (currentOrderType != "DELIVERY") return true;
                if (IsQueuedAppOrderTerminalStatus(currentStatus)) return true;

                var queuedDriverUserId = (item.DriverUserId ?? string.Empty).Trim();
                var currentDriverUserId = (order.DriverUserId ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(queuedDriverUserId)
                    && !string.IsNullOrWhiteSpace(currentDriverUserId)
                    && string.Equals(queuedDriverUserId, currentDriverUserId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return GetQueuedAppOrderStatusStage(currentOrderType, currentStatus) >= 3;
            }

            if (operation == "status")
            {
                var targetStatus = (item.ToStatus ?? string.Empty).Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(targetStatus)) return true;
                if (string.Equals(currentStatus, targetStatus, StringComparison.OrdinalIgnoreCase)) return true;
                if (IsQueuedAppOrderTerminalStatus(currentStatus)) return true;

                var currentStage = GetQueuedAppOrderStatusStage(currentOrderType, currentStatus);
                var targetStage = GetQueuedAppOrderStatusStage(currentOrderType, targetStatus);
                if (currentStage > 0 && targetStage > 0 && currentStage >= targetStage)
                {
                    return true;
                }

                return false;
            }

            if (operation == "delivery_address" || operation == "customer_delivery_address")
            {
                return IsQueuedAppOrderTerminalStatus(currentStatus);
            }

            return false;
        }

        private async Task PushPosOfflineOrderToSupabaseAsync(SupabaseClientConfigRecord cfg, PosOfflineSupabaseOrderInsertRow row)
        {
            if (cfg == null) throw new ArgumentNullException("cfg");
            if (row == null) throw new ArgumentNullException("row");

            var payload = new SupabaseCreateInventoryOrderRpcRequest
            {
                Payload = row
            };

            var actorRole = row.Metadata != null ? row.Metadata.CashierRole : null;
            var actorUserId = row.Metadata != null ? row.Metadata.CashierUserId : null;
            if (string.IsNullOrWhiteSpace(actorRole)) actorRole = GetCurrentBackendActorRole();
            if (string.IsNullOrWhiteSpace(actorUserId)) actorUserId = GetCurrentSessionUserId();

            var raw = await PostSupabaseRpcGetStringAsync(
                cfg,
                "api_create_app_order_with_inventory",
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
            if (response.Ok) return;

            var message = !string.IsNullOrWhiteSpace(response.Error)
                ? response.Error
                : (!string.IsNullOrWhiteSpace(response.Code) ? response.Code : "inventory_order_rpc_failed");
            throw new InvalidOperationException(message);
        }

        private bool LooksLikeDuplicateOrderInsertError(string error)
        {
            if (string.IsNullOrWhiteSpace(error)) return false;
            return error.IndexOf("23505", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("duplicate key", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("already exists", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("HTTP 409", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void RemoveQueuedItemByIdNoLock(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return;
            for (int i = _posOfflineQueue.Count - 1; i >= 0; i--)
            {
                var item = _posOfflineQueue[i];
                if (item == null || item.Row == null) continue;
                if (string.Equals(item.Row.Id, orderId, StringComparison.OrdinalIgnoreCase))
                {
                    _posOfflineQueue.RemoveAt(i);
                }
            }
        }

        private void SortPosOfflineQueueNoLock()
        {
            _posOfflineQueue.Sort(delegate(PosOfflineOrderQueueItem a, PosOfflineOrderQueueItem b)
            {
                var aCreated = a == null ? long.MaxValue : a.CreatedAtMillis;
                var bCreated = b == null ? long.MaxValue : b.CreatedAtMillis;
                if (aCreated != bCreated) return aCreated < bCreated ? -1 : 1;

                var aFinalized = a == null ? long.MaxValue : a.FinalizedAtMillis;
                var bFinalized = b == null ? long.MaxValue : b.FinalizedAtMillis;
                if (aFinalized != bFinalized) return aFinalized < bFinalized ? -1 : 1;

                var aId = a == null ? string.Empty : (a.QueueId ?? string.Empty);
                var bId = b == null ? string.Empty : (b.QueueId ?? string.Empty);
                return string.Compare(aId, bId, StringComparison.Ordinal);
            });
        }

        private void PersistPosOfflineQueueNoLock()
        {
            var state = new PosOfflineOrderQueueState
            {
                Version = 1,
                Items = new List<PosOfflineOrderQueueItem>(_posOfflineQueue)
            };
            WriteJson(PosOfflineOrdersQueueFilePath, state);
        }

        private void EnsurePendingDeliveryAddressDeletesLoaded()
        {
            lock (_posOfflineQueueSync)
            {
                if (_pendingDeliveryAddressDeletesLoaded) return;
                _pendingDeliveryAddressDeletesLoaded = true;
                _pendingDeliveryAddressDeletes.Clear();

                var state = ReadJson<PendingDeliveryAddressDeleteQueueState>(DeliveryAddressDeleteQueueFilePath);
                var items = state == null ? null : state.Items;
                if (items == null) return;

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    var addressId = item == null ? string.Empty : (item.AddressId ?? string.Empty).Trim();
                    if (!IsSupabaseAddressId(addressId)) continue;
                    if (item.CreatedAtMillis <= 0) item.CreatedAtMillis = NowUnixMs();
                    if (string.IsNullOrWhiteSpace(item.QueueId)) item.QueueId = "dq_" + item.CreatedAtMillis.ToString(CultureInfo.InvariantCulture) + "_" + NewId();
                    if (ContainsPendingDeliveryAddressDeleteNoLock(addressId)) continue;
                    item.Phone = (item.Phone ?? string.Empty).Trim();
                    item.AddressId = addressId;
                    _pendingDeliveryAddressDeletes.Add(item);
                }

                SortPendingDeliveryAddressDeletesNoLock();
            }
        }

        private bool HasPendingDeliveryAddressDeleteWork()
        {
            EnsurePendingDeliveryAddressDeletesLoaded();
            lock (_posOfflineQueueSync)
            {
                return _pendingDeliveryAddressDeletes.Count > 0;
            }
        }

        private void EnqueuePendingDeliveryAddressDelete(string phone, string addressId)
        {
            phone = (phone ?? string.Empty).Trim();
            addressId = (addressId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(phone) || !IsSupabaseAddressId(addressId)) return;
            EnsurePendingDeliveryAddressDeletesLoaded();

            lock (_posOfflineQueueSync)
            {
                if (ContainsPendingDeliveryAddressDeleteNoLock(addressId)) return;
                var createdAtMillis = NowUnixMs();
                _pendingDeliveryAddressDeletes.Add(new PendingDeliveryAddressDeleteQueueItem
                {
                    QueueId = "dq_" + createdAtMillis.ToString(CultureInfo.InvariantCulture) + "_" + NewId(),
                    Phone = phone,
                    AddressId = addressId,
                    CreatedAtMillis = createdAtMillis
                });
                SortPendingDeliveryAddressDeletesNoLock();
                PersistPendingDeliveryAddressDeletesNoLock();
            }

            QueuePosOfflineSync();
        }

        private List<PendingDeliveryAddressDeleteQueueItem> GetPendingDeliveryAddressDeleteSnapshot()
        {
            EnsurePendingDeliveryAddressDeletesLoaded();
            lock (_posOfflineQueueSync)
            {
                return new List<PendingDeliveryAddressDeleteQueueItem>(_pendingDeliveryAddressDeletes);
            }
        }

        private bool IsPendingDeliveryAddressDelete(string phone, string addressId, List<PendingDeliveryAddressDeleteQueueItem> pendingDeletes = null)
        {
            var normalizedAddressId = (addressId ?? string.Empty).Trim();
            if (!IsSupabaseAddressId(normalizedAddressId)) return false;

            var normalizedPhone = (phone ?? string.Empty).Trim();
            var pending = pendingDeletes ?? GetPendingDeliveryAddressDeleteSnapshot();
            if (pending == null || pending.Count == 0) return false;

            for (int i = 0; i < pending.Count; i++)
            {
                var item = pending[i];
                if (item == null) continue;
                if (!string.Equals((item.AddressId ?? string.Empty).Trim(), normalizedAddressId, StringComparison.OrdinalIgnoreCase)) continue;
                var itemPhone = (item.Phone ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(itemPhone) || string.IsNullOrWhiteSpace(normalizedPhone) || string.Equals(itemPhone, normalizedPhone, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void MarkPendingDeliveryAddressDeleteAsSynced(string addressId)
        {
            addressId = (addressId ?? string.Empty).Trim();
            if (!IsSupabaseAddressId(addressId)) return;
            EnsurePendingDeliveryAddressDeletesLoaded();

            lock (_posOfflineQueueSync)
            {
                if (!RemovePendingDeliveryAddressDeleteByAddressIdNoLock(addressId)) return;
                PersistPendingDeliveryAddressDeletesNoLock();
            }
        }

        private bool ContainsPendingDeliveryAddressDeleteNoLock(string addressId)
        {
            var id = (addressId ?? string.Empty).Trim();
            if (!IsSupabaseAddressId(id)) return false;
            for (int i = 0; i < _pendingDeliveryAddressDeletes.Count; i++)
            {
                var item = _pendingDeliveryAddressDeletes[i];
                if (item == null) continue;
                if (string.Equals((item.AddressId ?? string.Empty).Trim(), id, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private void RemovePendingDeliveryAddressDeleteByQueueIdNoLock(string queueId)
        {
            var id = (queueId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id)) return;
            for (int i = _pendingDeliveryAddressDeletes.Count - 1; i >= 0; i--)
            {
                var item = _pendingDeliveryAddressDeletes[i];
                if (item == null) continue;
                if (!string.Equals((item.QueueId ?? string.Empty).Trim(), id, StringComparison.OrdinalIgnoreCase)) continue;
                _pendingDeliveryAddressDeletes.RemoveAt(i);
            }
        }

        private bool RemovePendingDeliveryAddressDeleteByAddressIdNoLock(string addressId)
        {
            var id = (addressId ?? string.Empty).Trim();
            if (!IsSupabaseAddressId(id)) return false;

            var changed = false;
            for (int i = _pendingDeliveryAddressDeletes.Count - 1; i >= 0; i--)
            {
                var item = _pendingDeliveryAddressDeletes[i];
                if (item == null) continue;
                if (!string.Equals((item.AddressId ?? string.Empty).Trim(), id, StringComparison.OrdinalIgnoreCase)) continue;
                _pendingDeliveryAddressDeletes.RemoveAt(i);
                changed = true;
            }
            return changed;
        }

        private void SortPendingDeliveryAddressDeletesNoLock()
        {
            _pendingDeliveryAddressDeletes.Sort(delegate(PendingDeliveryAddressDeleteQueueItem a, PendingDeliveryAddressDeleteQueueItem b)
            {
                var aCreated = a == null ? long.MaxValue : a.CreatedAtMillis;
                var bCreated = b == null ? long.MaxValue : b.CreatedAtMillis;
                if (aCreated != bCreated) return aCreated < bCreated ? -1 : 1;

                var aId = a == null ? string.Empty : (a.QueueId ?? string.Empty);
                var bId = b == null ? string.Empty : (b.QueueId ?? string.Empty);
                return string.Compare(aId, bId, StringComparison.Ordinal);
            });
        }

        private void PersistPendingDeliveryAddressDeletesNoLock()
        {
            var state = new PendingDeliveryAddressDeleteQueueState
            {
                Version = 1,
                Items = new List<PendingDeliveryAddressDeleteQueueItem>(_pendingDeliveryAddressDeletes)
            };
            WriteJson(DeliveryAddressDeleteQueueFilePath, state);
        }

        private void EnsurePendingAppOrderMutationsLoaded()
        {
            lock (_posOfflineQueueSync)
            {
                if (_pendingAppOrderMutationsLoaded) return;
                _pendingAppOrderMutationsLoaded = true;
                _pendingAppOrderMutations.Clear();

                var state = ReadJson<AppOrderMutationQueueState>(AppOrderMutationsQueueFilePath);
                var items = state == null ? null : state.Items;
                if (items == null) return;

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item == null || string.IsNullOrWhiteSpace(item.OrderId) || string.IsNullOrWhiteSpace(item.OperationKind)) continue;
                    if (item.CreatedAtMillis <= 0) item.CreatedAtMillis = NowUnixMs();
                    if (string.IsNullOrWhiteSpace(item.QueueId)) item.QueueId = "aq_" + item.CreatedAtMillis.ToString(CultureInfo.InvariantCulture) + "_" + NewId();
                    _pendingAppOrderMutations.Add(item);
                }

                SortPendingAppOrderMutationsNoLock();
            }
        }

        private bool HasPendingAppOrderMutationWork()
        {
            EnsurePendingAppOrderMutationsLoaded();
            lock (_posOfflineQueueSync)
            {
                return _pendingAppOrderMutations.Count > 0;
            }
        }

        private void EnqueuePendingAppOrderMutation(AppOrderMutationQueueItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.OrderId) || string.IsNullOrWhiteSpace(item.OperationKind)) return;
            EnsurePendingAppOrderMutationsLoaded();

            lock (_posOfflineQueueSync)
            {
                item.OrderId = item.OrderId.Trim();
                item.OperationKind = item.OperationKind.Trim();
                if (item.CreatedAtMillis <= 0) item.CreatedAtMillis = NowUnixMs();
                if (string.IsNullOrWhiteSpace(item.QueueId)) item.QueueId = "aq_" + item.CreatedAtMillis.ToString(CultureInfo.InvariantCulture) + "_" + NewId();

                if (string.Equals(item.OperationKind, "delivery_address", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.OperationKind, "assign_driver", StringComparison.OrdinalIgnoreCase))
                {
                    for (int i = _pendingAppOrderMutations.Count - 1; i >= 0; i--)
                    {
                        var existing = _pendingAppOrderMutations[i];
                        if (existing == null) continue;
                        if (!string.Equals(existing.OperationKind, item.OperationKind, StringComparison.OrdinalIgnoreCase)) continue;
                        if (!string.Equals(existing.OrderId, item.OrderId, StringComparison.OrdinalIgnoreCase)) continue;
                        _pendingAppOrderMutations.RemoveAt(i);
                    }
                }
                else if (string.Equals(item.OperationKind, "status", StringComparison.OrdinalIgnoreCase))
                {
                    for (int i = _pendingAppOrderMutations.Count - 1; i >= 0; i--)
                    {
                        var existing = _pendingAppOrderMutations[i];
                        if (existing == null) continue;
                        if (!string.Equals(existing.OperationKind, item.OperationKind, StringComparison.OrdinalIgnoreCase)) continue;
                        if (!string.Equals(existing.OrderId, item.OrderId, StringComparison.OrdinalIgnoreCase)) continue;
                        if (!string.Equals((existing.ToStatus ?? string.Empty).Trim(), (item.ToStatus ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                        if (existing.MarkKitchenPrinted != item.MarkKitchenPrinted) continue;
                        if (existing.MarkDriverReceiptPrinted != item.MarkDriverReceiptPrinted) continue;
                        if (existing.MarkPartnerReceiptPrinted != item.MarkPartnerReceiptPrinted) continue;
                        _pendingAppOrderMutations.RemoveAt(i);
                    }
                }
                else if (string.Equals(item.OperationKind, "customer_delivery_address", StringComparison.OrdinalIgnoreCase))
                {
                    var normalizedUserId = (item.CustomerUserId ?? string.Empty).Trim();
                    var normalizedPhone = (item.CustomerPhone ?? string.Empty).Trim();
                    for (int i = _pendingAppOrderMutations.Count - 1; i >= 0; i--)
                    {
                        var existing = _pendingAppOrderMutations[i];
                        if (existing == null) continue;
                        if (!string.Equals(existing.OperationKind, item.OperationKind, StringComparison.OrdinalIgnoreCase)) continue;

                        var sameUser = !string.IsNullOrWhiteSpace(normalizedUserId)
                            && string.Equals((existing.CustomerUserId ?? string.Empty).Trim(), normalizedUserId, StringComparison.OrdinalIgnoreCase);
                        var samePhone = !string.IsNullOrWhiteSpace(normalizedPhone)
                            && string.Equals((existing.CustomerPhone ?? string.Empty).Trim(), normalizedPhone, StringComparison.Ordinal);
                        var sameOrder = string.Equals(existing.OrderId, item.OrderId, StringComparison.OrdinalIgnoreCase);
                        if (!sameUser && !samePhone && !sameOrder) continue;
                        _pendingAppOrderMutations.RemoveAt(i);
                    }
                }

                _pendingAppOrderMutations.Add(item);
                SortPendingAppOrderMutationsNoLock();
                PersistPendingAppOrderMutationsNoLock();
            }

            // PATCH: أيضاً أرسل للقائمة الجديدة ذات الـ Idempotency Key
            EnqueueIdempotentMutation(new IdempotencyAwareQueueItem
            {
                QueueId = item.QueueId,
                OrderId = item.OrderId,
                LocalOrderId = item.OrderId,
                OperationKind = item.OperationKind,
                CreatedAtMillis = item.CreatedAtMillis,
                ActorRole = item.ActorRole,
                ActorUserId = item.ActorUserId,
                ToStatus = item.ToStatus,
                MarkKitchenPrinted = item.MarkKitchenPrinted,
                MarkDriverReceiptPrinted = item.MarkDriverReceiptPrinted,
                MarkPartnerReceiptPrinted = item.MarkPartnerReceiptPrinted,
                CustomerUserId = item.CustomerUserId,
                CustomerPhone = item.CustomerPhone,
                AddressId = item.AddressId,
                District = item.District,
                DistrictId = item.DistrictId,
                DeliveryFee = item.DeliveryFee,
                AddressBlock = item.AddressBlock,
                AddressStreet = item.AddressStreet,
                AddressBuilding = item.AddressBuilding,
                AddressApartment = item.AddressApartment,
                AddressFloor = item.AddressFloor,
                AddressNote = item.AddressNote,
                DriverUserId = item.DriverUserId,
                DriverName = item.DriverName,
                PosShiftId = _activeShiftId
            });

            QueuePosOfflineSync();
        }

        private List<AppOrderMutationQueueItem> GetPendingAppOrderMutationsSnapshot()
        {
            EnsurePendingAppOrderMutationsLoaded();
            lock (_posOfflineQueueSync)
            {
                return new List<AppOrderMutationQueueItem>(_pendingAppOrderMutations);
            }
        }

        private void RemovePendingAppOrderMutationByQueueIdNoLock(string queueId)
        {
            var id = (queueId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id)) return;
            for (int i = _pendingAppOrderMutations.Count - 1; i >= 0; i--)
            {
                var item = _pendingAppOrderMutations[i];
                if (item == null) continue;
                if (!string.Equals(item.QueueId, id, StringComparison.OrdinalIgnoreCase)) continue;
                _pendingAppOrderMutations.RemoveAt(i);
            }
        }

        private void SortPendingAppOrderMutationsNoLock()
        {
            _pendingAppOrderMutations.Sort(delegate(AppOrderMutationQueueItem a, AppOrderMutationQueueItem b)
            {
                var aCreated = a == null ? long.MaxValue : a.CreatedAtMillis;
                var bCreated = b == null ? long.MaxValue : b.CreatedAtMillis;
                if (aCreated != bCreated) return aCreated < bCreated ? -1 : 1;

                var aId = a == null ? string.Empty : (a.QueueId ?? string.Empty);
                var bId = b == null ? string.Empty : (b.QueueId ?? string.Empty);
                return string.Compare(aId, bId, StringComparison.Ordinal);
            });
        }

        private void PersistPendingAppOrderMutationsNoLock()
        {
            var state = new AppOrderMutationQueueState
            {
                Version = 1,
                Items = new List<AppOrderMutationQueueItem>(_pendingAppOrderMutations)
            };
            WriteJson(AppOrderMutationsQueueFilePath, state);
        }

        [DataContract]
        private sealed class PosOfflineOrderQueueState
        {
            [DataMember(Name = "version", EmitDefaultValue = false)] public int Version { get; set; }
            [DataMember(Name = "items")] public List<PosOfflineOrderQueueItem> Items { get; set; }
        }

        [DataContract]
        private sealed class PendingDeliveryAddressDeleteQueueState
        {
            [DataMember(Name = "version", EmitDefaultValue = false)] public int Version { get; set; }
            [DataMember(Name = "items")] public List<PendingDeliveryAddressDeleteQueueItem> Items { get; set; }
        }

        [DataContract]
        private sealed class AppOrderMutationQueueState
        {
            [DataMember(Name = "version", EmitDefaultValue = false)] public int Version { get; set; }
            [DataMember(Name = "items")] public List<AppOrderMutationQueueItem> Items { get; set; }
        }

        [DataContract]
        private sealed class PosShiftMigrationQueueState
        {
            [DataMember(Name = "version", EmitDefaultValue = false)] public int Version { get; set; }
            [DataMember(Name = "order_ids")] public List<string> OrderIds { get; set; }
        }

        [DataContract]
        private sealed class PosOfflineOrderQueueItem
        {
            [DataMember(Name = "queue_id", EmitDefaultValue = false)] public string QueueId { get; set; }
            [DataMember(Name = "local_order_id", EmitDefaultValue = false)] public string LocalOrderId { get; set; }
            [DataMember(Name = "order_type", EmitDefaultValue = false)] public string OrderType { get; set; }
            [DataMember(Name = "created_at_millis", EmitDefaultValue = false)] public long CreatedAtMillis { get; set; }
            [DataMember(Name = "finalized_at_millis", EmitDefaultValue = false)] public long FinalizedAtMillis { get; set; }
            [DataMember(Name = "row")] public PosOfflineSupabaseOrderInsertRow Row { get; set; }
        }

        [DataContract]
        private sealed class PendingDeliveryAddressDeleteQueueItem
        {
            [DataMember(Name = "queue_id", EmitDefaultValue = false)] public string QueueId { get; set; }
            [DataMember(Name = "phone", EmitDefaultValue = false)] public string Phone { get; set; }
            [DataMember(Name = "address_id", EmitDefaultValue = false)] public string AddressId { get; set; }
            [DataMember(Name = "created_at_millis", EmitDefaultValue = false)] public long CreatedAtMillis { get; set; }
        }

        [DataContract]
        private sealed class AppOrderMutationQueueItem
        {
            [DataMember(Name = "queue_id", EmitDefaultValue = false)] public string QueueId { get; set; }
            [DataMember(Name = "order_id", EmitDefaultValue = false)] public string OrderId { get; set; }
            [DataMember(Name = "operation_kind", EmitDefaultValue = false)] public string OperationKind { get; set; }
            [DataMember(Name = "created_at_millis", EmitDefaultValue = false)] public long CreatedAtMillis { get; set; }
            [DataMember(Name = "actor_role", EmitDefaultValue = false)] public string ActorRole { get; set; }
            [DataMember(Name = "actor_user_id", EmitDefaultValue = false)] public string ActorUserId { get; set; }
            [DataMember(Name = "to_status", EmitDefaultValue = false)] public string ToStatus { get; set; }
            [DataMember(Name = "mark_kitchen_printed", EmitDefaultValue = false)] public bool? MarkKitchenPrinted { get; set; }
            [DataMember(Name = "mark_driver_receipt_printed", EmitDefaultValue = false)] public bool? MarkDriverReceiptPrinted { get; set; }
            [DataMember(Name = "mark_partner_receipt_printed", EmitDefaultValue = false)] public bool? MarkPartnerReceiptPrinted { get; set; }
            [DataMember(Name = "customer_user_id", EmitDefaultValue = false)] public string CustomerUserId { get; set; }
            [DataMember(Name = "customer_phone", EmitDefaultValue = false)] public string CustomerPhone { get; set; }
            [DataMember(Name = "address_id", EmitDefaultValue = false)] public string AddressId { get; set; }
            [DataMember(Name = "district", EmitDefaultValue = false)] public string District { get; set; }
            [DataMember(Name = "district_id", EmitDefaultValue = false)] public string DistrictId { get; set; }
            [DataMember(Name = "address_text", EmitDefaultValue = false)] public string AddressText { get; set; }
            [DataMember(Name = "delivery_fee", EmitDefaultValue = false)] public int DeliveryFee { get; set; }
            [DataMember(Name = "address_block", EmitDefaultValue = false)] public string AddressBlock { get; set; }
            [DataMember(Name = "address_street", EmitDefaultValue = false)] public string AddressStreet { get; set; }
            [DataMember(Name = "address_building", EmitDefaultValue = false)] public string AddressBuilding { get; set; }
            [DataMember(Name = "address_apartment", EmitDefaultValue = false)] public string AddressApartment { get; set; }
            [DataMember(Name = "address_floor", EmitDefaultValue = false)] public string AddressFloor { get; set; }
            [DataMember(Name = "address_note", EmitDefaultValue = false)] public string AddressNote { get; set; }
            [DataMember(Name = "driver_user_id", EmitDefaultValue = false)] public string DriverUserId { get; set; }
            [DataMember(Name = "driver_name", EmitDefaultValue = false)] public string DriverName { get; set; }
            [DataMember(Name = "driver_phone", EmitDefaultValue = false)] public string DriverPhone { get; set; }
        }

        [DataContract]
        private sealed class PosOfflineSupabaseOrderInsertRow
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "order_type")] public string OrderType { get; set; }
            [DataMember(Name = "status")] public string Status { get; set; }
            [DataMember(Name = "customer_user_id", EmitDefaultValue = false)] public string CustomerUserId { get; set; }
            [DataMember(Name = "customer_name", EmitDefaultValue = false)] public string CustomerName { get; set; }
            [DataMember(Name = "customer_phone", EmitDefaultValue = false)] public string CustomerPhone { get; set; }
            [DataMember(Name = "district", EmitDefaultValue = false)] public string District { get; set; }
            [DataMember(Name = "address_text", EmitDefaultValue = false)] public string AddressText { get; set; }
            [DataMember(Name = "driver_user_id", EmitDefaultValue = false)] public string DriverUserId { get; set; }
            [DataMember(Name = "subtotal")] public double Subtotal { get; set; }
            [DataMember(Name = "delivery_fee")] public double DeliveryFee { get; set; }
            [DataMember(Name = "total")] public double Total { get; set; }
            [DataMember(Name = "created_at", EmitDefaultValue = false)] public string CreatedAt { get; set; }
            [DataMember(Name = "updated_at", EmitDefaultValue = false)] public string UpdatedAt { get; set; }
            [DataMember(Name = "pos_shift_id", EmitDefaultValue = false)] public string PosShiftId { get; set; }
            [DataMember(Name = "metadata", EmitDefaultValue = false)] public PosOfflineOrderMetadata Metadata { get; set; }
        }

        [DataContract]
        private sealed class SupabaseCreateInventoryOrderRpcRequest
        {
            [DataMember(Name = "p_payload")] public PosOfflineSupabaseOrderInsertRow Payload { get; set; }
        }

        [DataContract]
        private sealed class SupabaseCreateInventoryOrderRpcResponse
        {
            [DataMember(Name = "ok", EmitDefaultValue = false)] public bool Ok { get; set; }
            [DataMember(Name = "orderId", EmitDefaultValue = false)] public string OrderId { get; set; }
            [DataMember(Name = "code", EmitDefaultValue = false)] public string Code { get; set; }
            [DataMember(Name = "error", EmitDefaultValue = false)] public string Error { get; set; }
        }

        [DataContract]
        private sealed class PosOfflineOrderMetadata
        {
            [DataMember(Name = "source", EmitDefaultValue = false)] public string Source { get; set; }
            [DataMember(Name = "localOrderId", EmitDefaultValue = false)] public string LocalOrderId { get; set; }
            [DataMember(Name = "localOrderNo", EmitDefaultValue = false)] public int LocalOrderNo { get; set; }
            [DataMember(Name = "localCreatedAtMillis", EmitDefaultValue = false)] public long LocalCreatedAtMillis { get; set; }
            [DataMember(Name = "finalizedAtMillis", EmitDefaultValue = false)] public long FinalizedAtMillis { get; set; }
            [DataMember(Name = "paymentMethod", EmitDefaultValue = false)] public string PaymentMethod { get; set; }
            [DataMember(Name = "posOrderType", EmitDefaultValue = false)] public string PosOrderType { get; set; }
            [DataMember(Name = "driverName", EmitDefaultValue = false)] public string DriverName { get; set; }
            [DataMember(Name = "driverPhone", EmitDefaultValue = false)] public string DriverPhone { get; set; }
            [DataMember(Name = "cashierUserId", EmitDefaultValue = false)] public string CashierUserId { get; set; }
            [DataMember(Name = "cashierUsername", EmitDefaultValue = false)] public string CashierUsername { get; set; }
            [DataMember(Name = "cashierRole", EmitDefaultValue = false)] public string CashierRole { get; set; }
            [DataMember(Name = "items", EmitDefaultValue = false)] public List<PosOfflineOrderMetadataItem> Items { get; set; }
        }

        [DataContract]
        private sealed class PosOfflineOrderMetadataItem
        {
            [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }
            [DataMember(Name = "productId", EmitDefaultValue = false)] public string ProductId { get; set; }
            [DataMember(Name = "sourceProductId", EmitDefaultValue = false)] public string SourceProductId { get; set; }
            [DataMember(Name = "qty", EmitDefaultValue = false)] public int Qty { get; set; }
            [DataMember(Name = "price", EmitDefaultValue = false)] public int Price { get; set; }
            [DataMember(Name = "notes", EmitDefaultValue = false)] public string Notes { get; set; }
            [DataMember(Name = "optionIds", EmitDefaultValue = false)] public List<string> OptionIds { get; set; }
        }
    }
}

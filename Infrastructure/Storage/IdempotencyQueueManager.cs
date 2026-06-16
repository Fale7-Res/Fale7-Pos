// =============================================================================
// PosIdempotencyQueue.cs
// Ã™â€¦Ã˜Â³Ã˜Â§Ã˜Â± Ã˜Â§Ã™â€žÃ™â€¦Ã™â€žÃ™Â: POS-Windows/PosIdempotencyQueue.cs
// Ã™Å Ã™ÂÃ˜Â¶Ã˜Â§Ã™Â Ã™Æ’Ã™â‚¬ partial class Ã˜Â¯Ã˜Â§Ã˜Â®Ã™â€ž MainWindow
// Ã™Å Ã˜Â¹Ã˜Â§Ã™â€žÃ˜Â¬: Idempotency Keys + Offline Queue Ã˜Â§Ã™â€žÃ™Æ’Ã˜Â§Ã™â€¦Ã™â€žÃ˜Â© Ã˜Â§Ã™â€žÃ™â€¦Ã™ÂÃ˜ÂµÃ™â€žÃ™Å½Ã˜Â­Ã˜Â©
// =============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fale7_POS
{
    // =========================================================================
    // Partial class: Ã™â€¦Ã™â€ Ã˜Â·Ã™â€š Ã˜Â§Ã™â€žÃ™â€šÃ˜Â§Ã˜Â¦Ã™â€¦Ã˜Â© Ã˜Â§Ã™â€žÃ™â€¦Ã™ÂÃ˜ÂµÃ™â€žÃ™Å½Ã˜Â­
    // =========================================================================
    public partial class MainWindow
    {
        // ----------------------------------------------------------------------
        // State
        // ----------------------------------------------------------------------
        private readonly object _idqSync = new object();
        private List<IdempotencyAwareQueueItem> _idqItems;
        private bool _idqLoaded;
        private const int MAX_RETRY_COUNT = 7;

        private string IdempotencyQueueFilePath
            => System.IO.Path.Combine(AppDataLogsDirPath, "pos_idq_v2.json");

        // ----------------------------------------------------------------------
        // 1. Ã˜Â¨Ã™â€ Ã˜Â§Ã˜Â¡ Idempotency Key
        //    Ã˜Â§Ã™â€žÃ˜ÂµÃ™Å Ã˜ÂºÃ˜Â©: {OperationType}_{ItemKey}_{TimestampMs}_{DeviceId}
        // ----------------------------------------------------------------------
        private string BuildIdempotencyKey(string operationType, string itemKey)
        {
            var ts        = NowUnixMs();
            var deviceId  = GetDeviceId();
            var safeOp    = Sanitize(operationType, 24);
            var safeItem  = Sanitize(itemKey,        32);
            return $"{safeOp}_{safeItem}_{ts}_{deviceId}";
        }

        private static string Sanitize(string value, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(value)) return "x";
            var clean = value.Replace(' ', '_')
                             .Replace('/', '_')
                             .Replace('\\','_');
            return clean.Length > maxLen ? clean.Substring(0, maxLen) : clean;
        }

        // ----------------------------------------------------------------------
        // 2. Ã˜Â¥Ã˜Â¶Ã˜Â§Ã™ÂÃ˜Â© Ã˜Â¹Ã™â€ Ã˜ÂµÃ˜Â± Ã™â€žÃ™â€žÃ™â€šÃ˜Â§Ã˜Â¦Ã™â€¦Ã˜Â© Ã™â€¦Ã˜Â¹ Idempotency Key
        // ----------------------------------------------------------------------
        private void EnqueueIdempotentMutation(IdempotencyAwareQueueItem item)
        {
            if (item == null) return;

            EnsureIdqLoaded();

            var itemKey = item.OrderId ?? item.LocalOrderId ?? Guid.NewGuid().ToString("N");

            // Ã˜ÂªÃ™Ë†Ã™â€žÃ™Å Ã˜Â¯ QueueId Ã™Ë† IdempotencyKey Ã˜Â¥Ã˜Â°Ã˜Â§ Ã™â€žÃ™â€¦ Ã™Å Ã™ÂÃ˜Â­Ã˜Â¯Ã˜Â¯Ã˜Â§
            if (string.IsNullOrWhiteSpace(item.QueueId))
                item.QueueId = "iq_" + Guid.NewGuid().ToString("N").Substring(0, 16);

            if (string.IsNullOrWhiteSpace(item.IdempotencyKey))
                item.IdempotencyKey = BuildIdempotencyKey(item.OperationKind ?? "op", itemKey);

            if (item.CreatedAtMillis == 0)
                item.CreatedAtMillis = NowUnixMs();

            lock (_idqSync)
            {
                // Ã˜Â¥Ã˜Â²Ã˜Â§Ã™â€žÃ˜Â© Ã˜Â§Ã™â€žÃ˜Â¹Ã™â€¦Ã™â€žÃ™Å Ã˜Â§Ã˜Âª Ã˜Â§Ã™â€žÃ™â€¦Ã™Æ’Ã˜Â±Ã˜Â±Ã˜Â© (Ã™â€ Ã™ÂÃ˜Â³ Ã˜Â§Ã™â€žÃ™â€ Ã™Ë†Ã˜Â¹ Ã˜Â¹Ã™â€žÃ™â€° Ã™â€ Ã™ÂÃ˜Â³ Ã˜Â§Ã™â€žÃ˜Â£Ã™Ë†Ã˜Â±Ã˜Â¯Ã˜Â±)
                DeduplicateBeforeAdd(item);

                _idqItems.Add(item);
                _idqItems.Sort((a, b) =>
                {
                    var diff = (a?.CreatedAtMillis ?? 0).CompareTo(b?.CreatedAtMillis ?? 0);
                    if (diff != 0) return diff;
                    return string.Compare(a?.QueueId, b?.QueueId, StringComparison.Ordinal);
                });

                PersistIdqNoLock();
            }

            QueuePosOfflineSync();
        }

        public void ClearPosIdempotencyQueueAfterMigration()
        {
            lock (_idqSync)
            {
                EnsureIdqLoaded();
                _idqItems.Clear();
                PersistIdqNoLock();
            }
        }

        // Ã˜Â«Ã˜ÂºÃ˜Â±Ã˜Â© Ã˜Â§Ã™â€žÃ˜ÂªÃ™Æ’Ã˜Â±Ã˜Â§Ã˜Â±: Ã˜Â¥Ã˜Â²Ã˜Â§Ã™â€žÃ˜Â© Ã˜Â¹Ã™â€ Ã˜ÂµÃ˜Â± Ã˜Â³Ã˜Â§Ã˜Â¨Ã™â€š Ã™â€žÃ™â€ Ã™ÂÃ˜Â³ Ã˜Â§Ã™â€žÃ˜Â¹Ã™â€¦Ã™â€žÃ™Å Ã˜Â© Ã˜Â¹Ã™â€žÃ™â€° Ã™â€ Ã™ÂÃ˜Â³ Ã˜Â§Ã™â€žÃ˜Â£Ã™Ë†Ã˜Â±Ã˜Â¯Ã˜Â±
                private void DeduplicateBeforeAdd(IdempotencyAwareQueueItem incoming)
        {
            // Ø¹Ù…Ù„ÙŠØ§Øª Ø§Ù„Ø­Ø§Ù„Ø©: Ø¢Ø®Ø± ÙˆØ§Ø­Ø¯Ø© ØªÙÙˆØ²
            if (string.Equals(incoming.OperationKind, "status",
                               StringComparison.OrdinalIgnoreCase))
            {
                _idqItems.RemoveAll(x =>
                    x != null
                    && string.Equals(x.OperationKind, "status", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.OrderId, incoming.OrderId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.ToStatus, incoming.ToStatus, StringComparison.OrdinalIgnoreCase));
                return;
            }

            // Ø¹Ù…Ù„ÙŠØ§Øª Ø¹Ù†ÙˆØ§Ù† Ø§Ù„ØªÙˆØµÙŠÙ„: Ø¢Ø®Ø± ÙˆØ§Ø­Ø¯Ø© ØªÙÙˆØ²
            if (string.Equals(incoming.OperationKind, "customer_delivery_address",
                               StringComparison.OrdinalIgnoreCase))
            {
                _idqItems.RemoveAll(x =>
                    x != null
                    && string.Equals(x.OperationKind, incoming.OperationKind,
                                     StringComparison.OrdinalIgnoreCase)
                    && (string.Equals(x.OrderId, incoming.OrderId, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(x.CustomerPhone, incoming.CustomerPhone,
                                         StringComparison.OrdinalIgnoreCase)));
                return;
            }

            // Ø¹Ù…Ù„ÙŠØ§Øª Ø·Ø¨Ø§Ø¹Ø©: Ø¢Ø®Ø± ÙˆØ§Ø­Ø¯Ø© ØªÙÙˆØ²
            if (string.Equals(incoming.OperationKind, "mark_prints",
                               StringComparison.OrdinalIgnoreCase))
            {
                _idqItems.RemoveAll(x =>
                    x != null
                    && string.Equals(x.OperationKind, incoming.OperationKind,
                                     StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.OrderId, incoming.OrderId,
                                     StringComparison.OrdinalIgnoreCase));
            }
        }

        // ----------------------------------------------------------------------
        // 4. Ù…Ø¹Ø§Ù„Ø¬Ø© Ø§Ù„Ù‚Ø§Ø¦Ù…Ø© Ø¹Ù†Ø¯ Ø¹ÙˆØ¯Ø© Ø§Ù„Ø¥Ù†ØªØ±Ù†Øª
        // ----------------------------------------------------------------------
        private SemaphoreSlim _idqProcessSem = new SemaphoreSlim(1, 1);

        private async Task ProcessIdempotencyQueueAsync()
        {
            if (!await _idqProcessSem.WaitAsync(0).ConfigureAwait(false))
                return; // ÙŠØ¹Ù…Ù„ Ø¨Ø§Ù„ÙØ¹Ù„

            try
            {
                EnsureIdqLoaded();

                while (true)
                {
                    IdempotencyAwareQueueItem item;
                    lock (_idqSync)
                    {
                        item = _idqItems.FirstOrDefault(x =>
                            x != null && x.RetryCount < MAX_RETRY_COUNT);
                    }

                    if (item == null) break;

                    var success = await ProcessSingleIdqItemAsync(item).ConfigureAwait(false);

                    lock (_idqSync)
                    {
                        if (success)
                        {
                            _idqItems.RemoveAll(x =>
                                x != null && string.Equals(x.QueueId, item.QueueId,
                                    StringComparison.OrdinalIgnoreCase));
                            AppendPosRuntimeLog("IDQ", $"ØªÙ…Øª Ø§Ù„Ù…Ø²Ø§Ù…Ù†Ø© Ø¨Ù†Ø¬Ø§Ø­: {item.QueueId} op={item.OperationKind}");
                        }
                        else
                        {
                            item.RetryCount++;
                            // ØªØ£Ø®ÙŠØ± ØªØ¯Ø±ÙŠØ¬ÙŠ Ø¹Ù†Ø¯ Ø§Ù„ÙØ´Ù„
                            if (item.RetryCount >= MAX_RETRY_COUNT)
                            {
                                AppendPosRuntimeLog("IDQ",
                                    $"Ø®Ø·Ø£ Ù†Ù‡Ø§Ø¦ÙŠ ÙÙŠ Ø§Ù„Ù…Ø²Ø§Ù…Ù†Ø© (DEAD_LETTER): {item.QueueId} Ø§Ù„Ø¹Ù…Ù„ÙŠØ©={item.OperationKind} " +
                                    $"Ø§Ù„Ø·Ù„Ø¨={item.OrderId} Ø§Ù„Ø®Ø·Ø£={item.LastError}");
                                _idqItems.RemoveAll(x =>
                                    x != null && string.Equals(x.QueueId, item.QueueId,
                                        StringComparison.OrdinalIgnoreCase));
                            }
                            else
                            {
                                AppendPosRuntimeLog("IDQ", $"ÙØ´Ù„ Ù…Ø¤Ù‚Øª ÙÙŠ Ø§Ù„Ù…Ø²Ø§Ù…Ù†Ø©ØŒ Ø¬Ø§Ø±ÙŠ Ø¥Ø¹Ø§Ø¯Ø© Ø§Ù„Ù…Ø­Ø§ÙˆÙ„Ø©: {item.QueueId}");
                            }
                        }
                        PersistIdqNoLock();
                    }

                    if (!success) break; // Ù„Ø§ ØªÙƒÙ…Ù„ Ø¹Ù†Ø¯ ÙØ´Ù„ -> Ø§Ù†ØªØ¸Ø± Ø§Ù„Ù…Ø¤Ù‚Øª Ø§Ù„ØªØ§Ù„ÙŠ

                    // Throttled Micro-Batching: ØªØ£Ø®ÙŠØ± ØªØ¯Ø±ÙŠØ¬ÙŠ Ù„Ù…Ù†Ø¹ Ø§Ø®ØªÙ†Ø§Ù‚ Ø§Ù„Ø³ÙŠØ±ÙØ±
                    await Task.Delay(200).ConfigureAwait(false);
                }
            }
            finally
            {
                _idqProcessSem.Release();
            }
        }

private async Task<bool> ProcessSingleIdqItemAsync(IdempotencyAwareQueueItem item)
        {
            var cfg = LoadSupabaseClientConfigResolved();
            if (cfg == null) return false;

            try
            {
                switch ((item.OperationKind ?? string.Empty).ToLowerInvariant())
                {
                    case "create_order":
                    case "create_offline_order":
                        return await SyncOfflineOrderCreateAsync(item, cfg).ConfigureAwait(false);

                    case "status":
                        return await SyncOrderStatusAsync(item, cfg).ConfigureAwait(false);

                    case "customer_delivery_address":
                        return await SyncDeliveryAddressAsync(item, cfg).ConfigureAwait(false);

                    case "mark_prints":
                        return await SyncMarkPrintsAsync(item, cfg).ConfigureAwait(false);

                    case "assign_driver":
                        return await SyncAssignDriverAsync(item, cfg).ConfigureAwait(false);

                    default:
                        AppendPosRuntimeLog("IDQ", $"UNKNOWN_OP: {item.OperationKind}");
                        return true; // Ã˜ÂªÃ˜Â¬Ã˜Â§Ã™â€¡Ã™â€ž Ã˜Â§Ã™â€žÃ˜Â¹Ã™â€¦Ã™â€žÃ™Å Ã˜Â§Ã˜Âª Ã˜Â§Ã™â€žÃ™â€¦Ã˜Â¬Ã™â€¡Ã™Ë†Ã™â€žÃ˜Â©
                }
            }
                        catch (Exception ex)
            {
                item.LastError = ex.Message;
                AppendPosRuntimeLog("IDQ", ex);
                return false;
            }
        }

        // ----------------------------------------------------------------------
        // 5. التنفيذ (Idempotency Key)
        // ----------------------------------------------------------------------
        private async Task<bool> SyncOfflineOrderCreateAsync(
            IdempotencyAwareQueueItem item,
            SupabaseClientConfigRecord cfg)
        {
            if (item.OrderPayload == null) return true;

            var payloadDict = item.OrderPayload;

            var body = SerializeJson(new
            {
                p_idempotency_key = item.IdempotencyKey,
                p_order_payload   = new
                {
                    operationType = item.OperationKind,
                    itemKey       = item.LocalOrderId ?? item.OrderId,
                    deviceId      = GetDeviceId(),
                    posShiftId    = item.PosShiftId,
                    payload       = payloadDict
                }
            });

            var raw = await PostSupabaseRpcGetStringAsync(
                cfg,
                "api_pos_create_order_idempotent",
                body,
                actorRoleOverride: GetCurrentBackendActorRole()
            ).ConfigureAwait(false);

            var result = DeserializeJson<SupabaseRpcGenericResult>(raw);

            if (result == null) return false;
            if (!result.Ok)
            {
                if (string.Equals(result.Error, "idempotent_duplicate", StringComparison.OrdinalIgnoreCase)) return true;
                item.LastError = result.Error;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(item.LocalOrderId) && !string.IsNullOrWhiteSpace(result.OrderId))
            {
                RegisterLocalToBackendIdMapping(item.LocalOrderId, result.OrderId);
            }

            return true;
        }

        private async Task<bool> SyncOrderStatusAsync(
            IdempotencyAwareQueueItem item,
            SupabaseClientConfigRecord cfg)
        {
            var orderId = ResolveBackendOrderId(item.OrderId);
            if (string.IsNullOrWhiteSpace(orderId)) return false;

            var body = SerializeJson(new
            {
                p_order_id        = orderId,
                p_to_status       = item.ToStatus,
                p_idempotency_key = item.IdempotencyKey,
                p_actor_role      = item.ActorRole ?? GetCurrentBackendActorRole(),
                p_actor_user_id   = item.ActorUserId
            });

            var raw = await PostSupabaseRpcGetStringAsync(
                cfg,
                "api_pos_update_order_status_idempotent",
                body,
                actorRoleOverride: GetCurrentBackendActorRole()
            ).ConfigureAwait(false);

            var result = DeserializeJson<SupabaseRpcGenericResult>(raw);
            if (result == null) return false;
            if (!result.Ok)
            {
                if (IsIdempotentDuplicate(result.Error)) return true;
                item.LastError = result.Error;
            }
            return result.Ok;
        }

        private async Task<bool> SyncDeliveryAddressAsync(
            IdempotencyAwareQueueItem item,
            SupabaseClientConfigRecord cfg)
        {
            var orderId = ResolveBackendOrderId(item.OrderId);
            if (string.IsNullOrWhiteSpace(orderId)) return false;

            var body = SerializeJson(new
            {
                p_order_id        = orderId,
                p_idempotency_key = item.IdempotencyKey,
                p_address_id      = item.AddressId,
                p_district        = item.District,
                p_district_id     = item.DistrictId,
                p_delivery_fee    = item.DeliveryFee,
                p_address_block   = item.AddressBlock,
                p_address_street  = item.AddressStreet,
                p_address_building= item.AddressBuilding,
                p_address_apartment=item.AddressApartment,
                p_address_floor   = item.AddressFloor,
                p_address_note    = item.AddressNote
            });

            var raw = await PostSupabaseRpcGetStringAsync(
                cfg,
                "api_pos_update_delivery_address",
                body,
                actorRoleOverride: GetCurrentBackendActorRole()
            ).ConfigureAwait(false);

            var result = DeserializeJson<SupabaseRpcGenericResult>(raw);
            if (result == null) return false;
            if (!result.Ok && IsIdempotentDuplicate(result.Error)) return true;
            if (!result.Ok) item.LastError = result.Error;
            return result.Ok;
        }

        private async Task<bool> SyncMarkPrintsAsync(
            IdempotencyAwareQueueItem item,
            SupabaseClientConfigRecord cfg)
        {
            var orderId = ResolveBackendOrderId(item.OrderId);
            if (string.IsNullOrWhiteSpace(orderId)) return false;

            var body = SerializeJson(new
            {
                p_order_id                    = orderId,
                p_idempotency_key             = item.IdempotencyKey,
                p_mark_kitchen_printed        = item.MarkKitchenPrinted,
                p_mark_driver_receipt_printed = item.MarkDriverReceiptPrinted,
                p_mark_partner_receipt_printed= item.MarkPartnerReceiptPrinted
            });

            var raw = await PostSupabaseRpcGetStringAsync(
                cfg,
                "api_pos_mark_order_prints",
                body,
                actorRoleOverride: GetCurrentBackendActorRole()
            ).ConfigureAwait(false);

            var result = DeserializeJson<SupabaseRpcGenericResult>(raw);
            if (result == null) return false;
            if (!result.Ok && IsIdempotentDuplicate(result.Error)) return true;
            if (!result.Ok) item.LastError = result.Error;
            return result.Ok;
        }

        private async Task<bool> SyncAssignDriverAsync(
            IdempotencyAwareQueueItem item,
            SupabaseClientConfigRecord cfg)
        {
            var orderId = ResolveBackendOrderId(item.OrderId);
            if (string.IsNullOrWhiteSpace(orderId)) return false;

            var body = SerializeJson(new
            {
                p_order_id        = orderId,
                p_idempotency_key = item.IdempotencyKey,
                p_driver_user_id  = item.DriverUserId,
                p_driver_name     = item.DriverName
            });

            var raw = await PostSupabaseRpcGetStringAsync(
                cfg,
                "api_pos_assign_driver",
                body,
                actorRoleOverride: GetCurrentBackendActorRole()
            ).ConfigureAwait(false);

            var result = DeserializeJson<SupabaseRpcGenericResult>(raw);
            if (result == null) return false;
            if (!result.Ok && IsIdempotentDuplicate(result.Error)) return true;
            if (!result.Ok) item.LastError = result.Error;
            return result.Ok;
        }

        // ----------------------------------------------------------------------
        // 6. Persistence
        // ----------------------------------------------------------------------
        private void EnsureIdqLoaded()
        {
            lock (_idqSync)
            {
                if (_idqLoaded) return;
                try
                {
                    var state = ReadJson<IdempotencyAwareQueueState>(IdempotencyQueueFilePath);
                    _idqItems = state?.Items ?? new List<IdempotencyAwareQueueItem>();
                }
                catch
                {
                    _idqItems = new List<IdempotencyAwareQueueItem>();
                }

                _idqLoaded = true;
            }
        }

        private void PersistIdqNoLock()
        {
            try
            {
                var state = new IdempotencyAwareQueueState { Items = new List<IdempotencyAwareQueueItem>(_idqItems) };
                WriteJson(IdempotencyQueueFilePath, state);
            }
            catch (Exception ex)
            {
                AppendPosRuntimeLog("IDQ_PERSIST", ex);
            }
        }


        // ----------------------------------------------------------------------
        // 7. Ã˜Â®Ã˜Â±Ã™Å Ã˜Â·Ã˜Â© LocalId Ã¢â€ â€™ BackendId
        // ----------------------------------------------------------------------
        private readonly Dictionary<string, string> _localToBackendIdMap
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private string LocalToBackendIdMapFilePath
            => System.IO.Path.Combine(AppDataLogsDirPath, "pos_local_backend_id_map.json");

        private void RegisterLocalToBackendIdMapping(string localId, string backendId)
        {
            if (string.IsNullOrWhiteSpace(localId) || string.IsNullOrWhiteSpace(backendId)) return;
            lock (_localToBackendIdMap)
            {
                _localToBackendIdMap[localId] = backendId;
                PersistLocalToBackendIdMap();
            }
        }

        private string ResolveBackendOrderId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return id;
            lock (_localToBackendIdMap)
            {
                return _localToBackendIdMap.TryGetValue(id, out var backend)
                    ? backend
                    : id;
            }
        }

        private void PersistLocalToBackendIdMap()
        {
            try
            {
                WriteJson(LocalToBackendIdMapFilePath, _localToBackendIdMap);
            }
            catch { }
        }

        private void LoadLocalToBackendIdMap()
        {
            try
            {
                var map = ReadJson<Dictionary<string,string>>(LocalToBackendIdMapFilePath);
                if (map == null) return;
                lock (_localToBackendIdMap)
                {
                    foreach (var kv in map)
                        _localToBackendIdMap[kv.Key] = kv.Value;
                }
            }
            catch { }
        }

        // ----------------------------------------------------------------------
        // 8. Ã™â€¦Ã˜Â³Ã˜Â§Ã˜Â¹Ã˜Â¯Ã˜Â§Ã˜Âª
        // ----------------------------------------------------------------------
        private static bool IsIdempotentDuplicate(string error)
            => string.Equals(error, "idempotent_duplicate", StringComparison.OrdinalIgnoreCase)
            || string.Equals(error, "pending",              StringComparison.OrdinalIgnoreCase);

        // DataContract Ã™â€¦Ã˜Â³Ã˜Â§Ã˜Â¹Ã˜Â¯
    }
}


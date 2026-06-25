// =============================================================================
// PosIdempotencyQueue.cs
// مسار الملف: POS-Windows/PosIdempotencyQueue.cs
// يُضاف كـ partial class داخل MainWindow
// يعالج: Idempotency Keys + Offline Queue الكاملة المُصلَحة
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
    // Data Contracts الجديدة
    // =========================================================================

    [DataContract]
    public sealed class IdempotencyAwareQueueItem
    {
        // المفتاح الفريد: OperationType_ItemKey_TimestampMs_DeviceId
        [DataMember(Name = "idempotency_key",   EmitDefaultValue = false)]
        public string IdempotencyKey { get; set; }

        // حقول القائمة الأصلية
        [DataMember(Name = "queue_id",          EmitDefaultValue = false)]
        public string QueueId { get; set; }

        [DataMember(Name = "order_id",          EmitDefaultValue = false)]
        public string OrderId { get; set; }

        [DataMember(Name = "local_order_id",    EmitDefaultValue = false)]
        public string LocalOrderId { get; set; }

        [DataMember(Name = "operation_kind",    EmitDefaultValue = false)]
        public string OperationKind { get; set; }

        [DataMember(Name = "created_at_millis", EmitDefaultValue = false)]
        public long CreatedAtMillis { get; set; }

        [DataMember(Name = "retry_count",       EmitDefaultValue = false)]
        public int RetryCount { get; set; }

        [DataMember(Name = "last_error",        EmitDefaultValue = false)]
        public string LastError { get; set; }

        // نسخة من AppOrderMutationQueueItem الحالي
        [DataMember(Name = "actor_role",        EmitDefaultValue = false)]
        public string ActorRole { get; set; }

        [DataMember(Name = "actor_user_id",     EmitDefaultValue = false)]
        public string ActorUserId { get; set; }

        [DataMember(Name = "to_status",         EmitDefaultValue = false)]
        public string ToStatus { get; set; }

        [DataMember(Name = "mark_kitchen_printed",         EmitDefaultValue = false)]
        public bool? MarkKitchenPrinted { get; set; }

        [DataMember(Name = "mark_driver_receipt_printed",  EmitDefaultValue = false)]
        public bool? MarkDriverReceiptPrinted { get; set; }

        [DataMember(Name = "mark_partner_receipt_printed", EmitDefaultValue = false)]
        public bool? MarkPartnerReceiptPrinted { get; set; }

        [DataMember(Name = "customer_user_id",  EmitDefaultValue = false)]
        public string CustomerUserId { get; set; }

        [DataMember(Name = "customer_phone",    EmitDefaultValue = false)]
        public string CustomerPhone { get; set; }

        [DataMember(Name = "address_id",        EmitDefaultValue = false)]
        public string AddressId { get; set; }

        [DataMember(Name = "district",          EmitDefaultValue = false)]
        public string District { get; set; }

        [DataMember(Name = "district_id",       EmitDefaultValue = false)]
        public string DistrictId { get; set; }

        [DataMember(Name = "delivery_fee",      EmitDefaultValue = false)]
        public int DeliveryFee { get; set; }

        [DataMember(Name = "address_block",     EmitDefaultValue = false)]
        public string AddressBlock { get; set; }

        [DataMember(Name = "address_street",    EmitDefaultValue = false)]
        public string AddressStreet { get; set; }

        [DataMember(Name = "address_building",  EmitDefaultValue = false)]
        public string AddressBuilding { get; set; }

        [DataMember(Name = "address_apartment", EmitDefaultValue = false)]
        public string AddressApartment { get; set; }

        [DataMember(Name = "address_floor",     EmitDefaultValue = false)]
        public string AddressFloor { get; set; }

        [DataMember(Name = "address_note",      EmitDefaultValue = false)]
        public string AddressNote { get; set; }

        [DataMember(Name = "driver_user_id",    EmitDefaultValue = false)]
        public string DriverUserId { get; set; }

        [DataMember(Name = "driver_name",       EmitDefaultValue = false)]
        public string DriverName { get; set; }

        // بيانات الأوردر الكاملة (للإنشاء offline)
        [DataMember(Name = "order_payload",     EmitDefaultValue = false)]
        public object OrderPayload { get; set; }

        // shift_id المرتبط
        [DataMember(Name = "pos_shift_id",      EmitDefaultValue = false)]
        public string PosShiftId { get; set; }
    }

    [DataContract]
    public sealed class IdempotencyAwareQueueState
    {
        [DataMember(Name = "version")]
        public int Version { get; set; } = 2;

        [DataMember(Name = "items")]
        public List<IdempotencyAwareQueueItem> Items { get; set; }
            = new List<IdempotencyAwareQueueItem>();
    }

    // =========================================================================
    // Partial class: منطق القائمة المُصلَح
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
        // 1. بناء Idempotency Key
        //    الصيغة: {OperationType}_{ItemKey}_{TimestampMs}_{DeviceId}
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
        // 2. إضافة عنصر للقائمة مع Idempotency Key
        // ----------------------------------------------------------------------
        private void EnqueueIdempotentMutation(IdempotencyAwareQueueItem item)
        {
            if (item == null) return;

            EnsureIdqLoaded();

            var itemKey = item.OrderId ?? item.LocalOrderId ?? Guid.NewGuid().ToString("N");

            // توليد QueueId و IdempotencyKey إذا لم يُحددا
            if (string.IsNullOrWhiteSpace(item.QueueId))
                item.QueueId = "iq_" + Guid.NewGuid().ToString("N").Substring(0, 16);

            if (string.IsNullOrWhiteSpace(item.IdempotencyKey))
                item.IdempotencyKey = BuildIdempotencyKey(item.OperationKind ?? "op", itemKey);

            if (item.CreatedAtMillis == 0)
                item.CreatedAtMillis = NowUnixMs();

            lock (_idqSync)
            {
                // إزالة العمليات المكررة (نفس النوع على نفس الأوردر)
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

        // ثغرة التكرار: إزالة عنصر سابق لنفس العملية على نفس الأوردر
        private void DeduplicateBeforeAdd(IdempotencyAwareQueueItem incoming)
        {
            // عمليات الحالة: آخر واحدة تفوز
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

            // عمليات عنوان التوصيل: آخر واحدة تفوز
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

            // عمليات طباعة: آخر واحدة تفوز
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
        // 4. معالجة القائمة عند عودة الإنترنت
        // ----------------------------------------------------------------------
        private SemaphoreSlim _idqProcessSem = new SemaphoreSlim(1, 1);

        private async Task ProcessIdempotencyQueueAsync()
        {
            if (!await _idqProcessSem.WaitAsync(0).ConfigureAwait(false))
                return; // يعمل بالفعل

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
                        }
                        else
                        {
                            item.RetryCount++;
                            // تأخير تدريجي: 2^retryCount ثانية بحد أقصى 5 دقائق
                            if (item.RetryCount >= MAX_RETRY_COUNT)
                            {
                                AppendPosRuntimeLog("IDQ",
                                    $"DEAD_LETTER: {item.QueueId} op={item.OperationKind} " +
                                    $"orderId={item.OrderId} err={item.LastError}");
                                _idqItems.RemoveAll(x =>
                                    x != null && string.Equals(x.QueueId, item.QueueId,
                                        StringComparison.OrdinalIgnoreCase));
                            }
                        }
                        PersistIdqNoLock();
                    }

                    if (!success) break; // لا تكمل عند فشل → انتظر Timer التالي
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
                        return true; // تجاهل العمليات المجهولة
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
        // 5. تنفيذ العمليات المختلفة مع Idempotency Key
        // ----------------------------------------------------------------------
        private async Task<bool> SyncOfflineOrderCreateAsync(
            IdempotencyAwareQueueItem item,
            SupabaseClientConfigRecord cfg)
        {
            if (item.OrderPayload == null) return true; // لا يوجد بيانات → تجاهل

            // إضافة بيانات الـ shift و idempotency key في الـ payload
            var payloadDict = item.OrderPayload;

            var body = SerializeJson(new
            {
                p_idempotency_key = item.IdempotencyKey,
                p_order_payload   = SerializeJson(new
                {
                    operationType = item.OperationKind,
                    itemKey       = item.LocalOrderId ?? item.OrderId,
                    deviceId      = GetDeviceId(),
                    posShiftId    = item.PosShiftId,
                    payload       = payloadDict
                })
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
                // إذا كان idempotent duplicate → اعتبره نجاحاً
                if (string.Equals(result.Error, "idempotent_duplicate",
                    StringComparison.OrdinalIgnoreCase)) return true;
                item.LastError = result.Error;
                return false;
            }

            // ربط LocalOrderId بـ BackendOrderId
            if (!string.IsNullOrWhiteSpace(item.LocalOrderId)
                && !string.IsNullOrWhiteSpace(result.OrderId))
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
        // 7. خريطة LocalId → BackendId
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
        // 8. مساعدات
        // ----------------------------------------------------------------------
        private static bool IsIdempotentDuplicate(string error)
            => string.Equals(error, "idempotent_duplicate", StringComparison.OrdinalIgnoreCase)
            || string.Equals(error, "pending",              StringComparison.OrdinalIgnoreCase);

        // DataContract مساعد
        [DataContract]
        private sealed class SupabaseRpcGenericResult
        {
            [DataMember(Name = "ok")]       public bool   Ok      { get; set; }
            [DataMember(Name = "error")]    public string Error   { get; set; }
            [DataMember(Name = "order_id")] public string OrderId { get; set; }
        }
    }
}

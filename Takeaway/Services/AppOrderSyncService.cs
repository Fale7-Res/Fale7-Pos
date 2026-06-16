using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        // ──────────────────────────────────────────────────────────────────────
        // FIELDS
        // ──────────────────────────────────────────────────────────────────────
        private bool _supabaseAppOrdersPullInFlight;
        private bool _supabaseAppOrdersPullPending;
        private readonly HashSet<string> _supabaseSeenAppOrderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _supabaseAppOrdersSeenPrimed;
        private readonly object _supabaseAppOrdersUiRefreshSync = new object();
        private bool _supabaseAppOrdersUiRefreshQueued;
        private bool _supabaseAppOrdersUiRefreshPending;
        private string _supabaseAppOrdersUiSnapshotKey = string.Empty;
        private readonly object _supabaseAppOrderMutationErrorSync = new object();
        private string _lastAppOrderMutationBackendError = string.Empty;

        // ──────────────────────────────────────────────────────────────────────
        // CONFIG CHECK
        // ──────────────────────────────────────────────────────────────────────
        private bool HasSupabaseAppOrdersBackendConfigured()
        {
            try
            {
                return LoadSupabaseClientConfigResolved() != null;
            }
            catch
            {
                return false;
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // UI REFRESH
        // ──────────────────────────────────────────────────────────────────────
        private void QueueSupabaseAppOrdersUiRefresh()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(QueueSupabaseAppOrdersUiRefresh), DispatcherPriority.Background);
                return;
            }

            lock (_supabaseAppOrdersUiRefreshSync)
            {
                if (_supabaseAppOrdersUiRefreshQueued)
                {
                    _supabaseAppOrdersUiRefreshPending = true;
                    return;
                }
                _supabaseAppOrdersUiRefreshQueued = true;
                _supabaseAppOrdersUiRefreshPending = false;
            }

            Dispatcher.BeginInvoke(new Action(ProcessSupabaseAppOrdersUiRefreshQueue), DispatcherPriority.Background);
        }

        private void ProcessSupabaseAppOrdersUiRefreshQueue()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(ProcessSupabaseAppOrdersUiRefreshQueue), DispatcherPriority.Background);
                return;
            }

            while (true)
            {
                try { RefreshDeliveryAppOrdersUi(); } catch { }
                try { RenderDeliverySharedPickups(); } catch { }

                var repeat = false;
                lock (_supabaseAppOrdersUiRefreshSync)
                {
                    if (_supabaseAppOrdersUiRefreshPending)
                    {
                        _supabaseAppOrdersUiRefreshPending = false;
                        repeat = true;
                    }
                    else
                    {
                        _supabaseAppOrdersUiRefreshQueued = false;
                    }
                }
                if (!repeat) break;
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // DATA TYPES
        // ──────────────────────────────────────────────────────────────────────
        [DataContract]
        private sealed class SupabaseAppOrderItemRow
        {
            [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }
            [DataMember(Name = "qty", EmitDefaultValue = false)] public int Qty { get; set; }
            [DataMember(Name = "price", EmitDefaultValue = false)] public double Price { get; set; }
        }

        [DataContract]
        private sealed class SupabaseAppOrderMetadataRow
        {
            [DataMember(Name = "localOrderId", EmitDefaultValue = false)] public string LocalOrderId { get; set; }
            [DataMember(Name = "localOrderNo", EmitDefaultValue = false)] public int LocalOrderNo { get; set; }
            [DataMember(Name = "finalizedAtMillis", EmitDefaultValue = false)] public long FinalizedAtMillis { get; set; }
            [DataMember(Name = "source", EmitDefaultValue = false)] public string Source { get; set; }
            [DataMember(Name = "posOrderType", EmitDefaultValue = false)] public string PosOrderType { get; set; }
            [DataMember(Name = "cashierUserId", EmitDefaultValue = false)] public string CashierUserId { get; set; }
            [DataMember(Name = "cashierUsername", EmitDefaultValue = false)] public string CashierUsername { get; set; }
            [DataMember(Name = "cashierRole", EmitDefaultValue = false)] public string CashierRole { get; set; }
            [DataMember(Name = "items", EmitDefaultValue = false)] public List<SupabaseAppOrderItemRow> Items { get; set; }
            [DataMember(Name = "driver_name", EmitDefaultValue = false)] public string DriverName { get; set; }
            [DataMember(Name = "driver_phone", EmitDefaultValue = false)] public string DriverPhone { get; set; }
            [DataMember(Name = "driverName", EmitDefaultValue = false)] public string DriverNameCamel { get; set; }
            [DataMember(Name = "driverPhone", EmitDefaultValue = false)] public string DriverPhoneCamel { get; set; }
            [DataMember(Name = "addressId", EmitDefaultValue = false)] public string AddressId { get; set; }
            [DataMember(Name = "districtId", EmitDefaultValue = false)] public string DistrictId { get; set; }
            [DataMember(Name = "districtName", EmitDefaultValue = false)] public string DistrictName { get; set; }
            [DataMember(Name = "addressLabel", EmitDefaultValue = false)] public string AddressLabel { get; set; }
            [DataMember(Name = "addressBlock", EmitDefaultValue = false)] public string AddressBlock { get; set; }
            [DataMember(Name = "addressStreet", EmitDefaultValue = false)] public string AddressStreet { get; set; }
            [DataMember(Name = "addressBuilding", EmitDefaultValue = false)] public string AddressBuilding { get; set; }
            [DataMember(Name = "addressApartment", EmitDefaultValue = false)] public string AddressApartment { get; set; }
            [DataMember(Name = "addressNote", EmitDefaultValue = false)] public string AddressNote { get; set; }
            [DataMember(Name = "addressTextFull", EmitDefaultValue = false)] public string AddressTextFull { get; set; }
        }

        [DataContract]
        private sealed class SupabaseAppOrderRow
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "order_no", EmitDefaultValue = false)] public long OrderNo { get; set; }
            [DataMember(Name = "order_type", EmitDefaultValue = false)] public string OrderType { get; set; }
            [DataMember(Name = "status", EmitDefaultValue = false)] public string Status { get; set; }
            [DataMember(Name = "customer_user_id", EmitDefaultValue = false)] public string CustomerUserId { get; set; }
            [DataMember(Name = "customer_name", EmitDefaultValue = false)] public string CustomerName { get; set; }
            [DataMember(Name = "customer_phone", EmitDefaultValue = false)] public string CustomerPhone { get; set; }
            [DataMember(Name = "district", EmitDefaultValue = false)] public string District { get; set; }
            [DataMember(Name = "address_text", EmitDefaultValue = false)] public string AddressText { get; set; }
            [DataMember(Name = "pos_address_id", EmitDefaultValue = false)] public string PosAddressId { get; set; }
            [DataMember(Name = "mobile_address_id", EmitDefaultValue = false)] public string MobileAddressId { get; set; }
            [DataMember(Name = "items", EmitDefaultValue = false)] public List<SupabaseAppOrderItemRow> Items { get; set; }
            [DataMember(Name = "subtotal", EmitDefaultValue = false)] public double Subtotal { get; set; }
            [DataMember(Name = "delivery_fee", EmitDefaultValue = false)] public double DeliveryFee { get; set; }
            [DataMember(Name = "total", EmitDefaultValue = false)] public double Total { get; set; }
            [DataMember(Name = "kitchen_printed", EmitDefaultValue = false)] public bool KitchenPrinted { get; set; }
            [DataMember(Name = "driver_receipt_printed", EmitDefaultValue = false)] public bool DriverReceiptPrinted { get; set; }
            [DataMember(Name = "partner_receipt_printed", EmitDefaultValue = false)] public bool PartnerReceiptPrinted { get; set; }
            [DataMember(Name = "driver_user_id", EmitDefaultValue = false)] public string DriverUserId { get; set; }
            [DataMember(Name = "driver_name", EmitDefaultValue = false)] public string DriverName { get; set; }
            [DataMember(Name = "driver_phone", EmitDefaultValue = false)] public string DriverPhone { get; set; }
            [DataMember(Name = "created_at", EmitDefaultValue = false)] public string CreatedAt { get; set; }
            [DataMember(Name = "updated_at", EmitDefaultValue = false)] public string UpdatedAt { get; set; }
            [DataMember(Name = "metadata", EmitDefaultValue = false)] public SupabaseAppOrderMetadataRow Metadata { get; set; }
        }

        [DataContract]
        private sealed class SupabaseRpcUpdateAppOrderStatusRequest
        {
            [DataMember(Name = "p_order_id")] public string OrderId { get; set; }
            [DataMember(Name = "p_to_status")] public string ToStatus { get; set; }
            [DataMember(Name = "p_actor_role", EmitDefaultValue = false)] public string ActorRole { get; set; }
            [DataMember(Name = "p_actor_user_id", EmitDefaultValue = false)] public string ActorUserId { get; set; }
            [DataMember(Name = "p_mark_kitchen_printed", EmitDefaultValue = false)] public bool? MarkKitchenPrinted { get; set; }
            [DataMember(Name = "p_mark_driver_receipt_printed", EmitDefaultValue = false)] public bool? MarkDriverReceiptPrinted { get; set; }
            [DataMember(Name = "p_mark_partner_receipt_printed", EmitDefaultValue = false)] public bool? MarkPartnerReceiptPrinted { get; set; }
        }

        [DataContract]
        private sealed class SupabaseAppOrderAddressPatchRow
        {
            [DataMember(Name = "district", EmitDefaultValue = false)] public string District { get; set; }
            [DataMember(Name = "address_text", EmitDefaultValue = false)] public string AddressText { get; set; }
            [DataMember(Name = "delivery_fee", EmitDefaultValue = false)] public double? DeliveryFee { get; set; }
            [DataMember(Name = "total", EmitDefaultValue = false)] public double? Total { get; set; }
            [DataMember(Name = "metadata", EmitDefaultValue = false)] public SupabaseAppOrderMetadataRow Metadata { get; set; }
        }

        [DataContract]
        private sealed class SupabaseCustomerAddressRow
        {
            [DataMember(Name = "id")] public int Id { get; set; }
            [DataMember(Name = "customer_user_id", EmitDefaultValue = false)] public string CustomerUserId { get; set; }
            [DataMember(Name = "customer_phone", EmitDefaultValue = false)] public string CustomerPhone { get; set; }
            [DataMember(Name = "label", EmitDefaultValue = false)] public string Label { get; set; }
            [DataMember(Name = "district_id", EmitDefaultValue = false)] public string DistrictId { get; set; }
            [DataMember(Name = "block", EmitDefaultValue = false)] public string Block { get; set; }
            [DataMember(Name = "street", EmitDefaultValue = false)] public string Street { get; set; }
            [DataMember(Name = "building", EmitDefaultValue = false)] public string Building { get; set; }
            [DataMember(Name = "apt", EmitDefaultValue = false)] public string Apartment { get; set; }
            [DataMember(Name = "note", EmitDefaultValue = false)] public string Note { get; set; }
            [DataMember(Name = "is_default", EmitDefaultValue = false)] public bool IsDefault { get; set; }
        }

        [DataContract]
        private sealed class SupabaseCustomerAddressPatchRow
        {
            [DataMember(Name = "customer_user_id", EmitDefaultValue = false)] public string CustomerUserId { get; set; }
            [DataMember(Name = "customer_phone", EmitDefaultValue = false)] public string CustomerPhone { get; set; }
            [DataMember(Name = "label", EmitDefaultValue = false)] public string Label { get; set; }
            [DataMember(Name = "district_id", EmitDefaultValue = false)] public string DistrictId { get; set; }
            [DataMember(Name = "block", EmitDefaultValue = false)] public string Block { get; set; }
            [DataMember(Name = "street", EmitDefaultValue = false)] public string Street { get; set; }
            [DataMember(Name = "building", EmitDefaultValue = false)] public string Building { get; set; }
            [DataMember(Name = "apt", EmitDefaultValue = false)] public string Apartment { get; set; }
            [DataMember(Name = "note", EmitDefaultValue = false)] public string Note { get; set; }
            [DataMember(Name = "is_default", EmitDefaultValue = false)] public bool IsDefault { get; set; }
        }

        // ──────────────────────────────────────────────────────────────────────
        // PULL FROM BACKEND
        // ──────────────────────────────────────────────────────────────────────
        private void QueueSupabaseAppOrdersPullFromBackend()
        {
            Task.Run(async delegate
            {
                SupabaseClientConfigRecord cfg = null;
                try { cfg = LoadSupabaseClientConfigResolved(); } catch { cfg = null; }
                if (cfg == null) return;

                lock (_supabaseRealtimeSync)
                {
                    if (_supabaseAppOrdersPullInFlight) { _supabaseAppOrdersPullPending = true; return; }
                    _supabaseAppOrdersPullInFlight = true;
                    _supabaseAppOrdersPullPending = false;
                }

                while (true)
                {
                    try
                    {
                        var rows = await FetchSupabaseAppOrdersAsync(cfg).ConfigureAwait(false);
                        ApplySupabaseAppOrdersToLocalUi(rows);
                    }
                    catch (Exception ex)
                    {
                        AppendPosRuntimeLog("SYNC_PULL", $"فشل استرداد بيانات app_orders من السحابة: {ex.Message}");
                    }

                    lock (_supabaseRealtimeSync)
                    {
                        if (_supabaseAppOrdersPullPending) { _supabaseAppOrdersPullPending = false; continue; }
                        _supabaseAppOrdersPullInFlight = false; break;
                    }
                }
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // STATUS UPDATE (delivery)
        // ──────────────────────────────────────────────────────────────────────
        private bool QueueSupabaseBackendAppDeliveryStatusUpdate(AppDeliveryOrderRecord order, string backendStatus, bool? markKitchenPrinted, bool? markDriverReceiptPrinted)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.Id) || string.IsNullOrWhiteSpace(backendStatus)) return false;
            SupabaseClientConfigRecord cfg = null;
            try { cfg = LoadSupabaseClientConfigResolved(); } catch { cfg = null; }
            if (cfg == null) return false;
            return TryPushAppOrderStatusNowOrQueue(cfg, new AppOrderMutationQueueItem
            {
                OrderId = order.Id, OperationKind = "status", CreatedAtMillis = NowUnixMs(),
                ActorRole = GetCurrentBackendActorRole(), ActorUserId = GetCurrentSessionUserId(),
                ToStatus = (backendStatus ?? string.Empty).Trim().ToUpperInvariant(),
                MarkKitchenPrinted = markKitchenPrinted, MarkDriverReceiptPrinted = markDriverReceiptPrinted, MarkPartnerReceiptPrinted = null
            });
        }

        private bool QueueSupabaseBackendAppPickupStatusUpdate(AppPickupOrderRecord order, string backendStatus, bool? markPartnerReceiptPrinted)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.Id) || string.IsNullOrWhiteSpace(backendStatus)) return false;
            SupabaseClientConfigRecord cfg = null;
            try { cfg = LoadSupabaseClientConfigResolved(); } catch { cfg = null; }
            if (cfg == null) return false;
            return TryPushAppOrderStatusNowOrQueue(cfg, new AppOrderMutationQueueItem
            {
                OrderId = order.Id, OperationKind = "status", CreatedAtMillis = NowUnixMs(),
                ActorRole = GetCurrentBackendActorRole(), ActorUserId = GetCurrentSessionUserId(),
                ToStatus = (backendStatus ?? string.Empty).Trim().ToUpperInvariant(),
                MarkKitchenPrinted = null, MarkDriverReceiptPrinted = null, MarkPartnerReceiptPrinted = markPartnerReceiptPrinted
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // ADDRESS UPDATE (simple) — now uses IdempotencyAwareQueueItem
        // ──────────────────────────────────────────────────────────────────────
        private bool QueueSupabaseBackendAppDeliveryAddressUpdate(AppDeliveryOrderRecord order, string district, string addressText)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.Id)) return false;
            SupabaseClientConfigRecord cfg = null;
            try { cfg = LoadSupabaseClientConfigResolved(); } catch { cfg = null; }
            if (cfg == null) return false;
            EnqueueIdempotentMutation(new IdempotencyAwareQueueItem
            {
                OrderId = order.Id, OperationKind = "delivery_address", CreatedAtMillis = NowUnixMs(),
                District = district, AddressText = addressText
            });
            EndAppOrderBusyState(order.Id);
            return true;
        }

        // ──────────────────────────────────────────────────────────────────────
        // ADDRESS UPDATE (customer) — now uses IdempotencyAwareQueueItem
        // ──────────────────────────────────────────────────────────────────────
        private bool QueueSupabaseBackendAppCustomerDeliveryAddressUpdate(AppDeliveryOrderRecord order, string districtId, DeliveryAddressRecord address, int deliveryFee, string addressId)
        {
            if (order == null || string.IsNullOrWhiteSpace(order.Id)) return false;
            SupabaseClientConfigRecord cfg = null;
            try { cfg = LoadSupabaseClientConfigResolved(); } catch { cfg = null; }
            if (cfg == null) return false;

            var clonedAddress = CloneDeliveryAddress(address);
            var district = (clonedAddress == null ? order.District : clonedAddress.District) ?? order.District;
            if (clonedAddress != null) clonedAddress.District = district;

            EnqueueIdempotentMutation(new IdempotencyAwareQueueItem
            {
                OrderId = order.Id, OperationKind = "customer_delivery_address", CreatedAtMillis = NowUnixMs(),
                CustomerUserId = (order.CustomerUserId ?? string.Empty).Trim(), CustomerPhone = (order.Phone ?? string.Empty).Trim(),
                AddressId = (addressId ?? string.Empty).Trim(), District = district, DistrictId = (districtId ?? string.Empty).Trim(),
                AddressText = BuildAppDeliveryAddressText(clonedAddress), DeliveryFee = deliveryFee < 0 ? 0 : deliveryFee,
                AddressBlock = clonedAddress == null ? string.Empty : clonedAddress.Block,
                AddressStreet = clonedAddress == null ? string.Empty : clonedAddress.Street,
                AddressBuilding = clonedAddress == null ? string.Empty : clonedAddress.Building,
                AddressApartment = clonedAddress == null ? string.Empty : clonedAddress.Apartment,
                AddressFloor = clonedAddress == null ? string.Empty : clonedAddress.Floor,
                AddressNote = clonedAddress == null ? string.Empty : clonedAddress.Note
            });
            EndAppOrderBusyState(order.Id);
            return true;
        }

        // ──────────────────────────────────────────────────────────────────────
        // DRIVER ASSIGNMENT (now pushes to cloud, enqueues on failure via
        // the idempotency-aware mechanism)
        // ──────────────────────────────────────────────────────────────────────
        private bool QueueSupabaseBackendAssignDriver(AppDeliveryOrderRecord order, DeliveryDriverRecord driver)
        {
            if (order == null || driver == null) return false;
            var driverUserId = ResolveBackendDriverUserId(driver);
            if (string.IsNullOrWhiteSpace(driverUserId)) return false;
            if (string.IsNullOrWhiteSpace(order.Id)) return false;
            SupabaseClientConfigRecord cfg = null;
            try { cfg = LoadSupabaseClientConfigResolved(); } catch { cfg = null; }
            if (cfg == null) return false;
            return TryPushAppDriverAssignmentNowOrQueue(cfg, new AppOrderMutationQueueItem
            {
                OrderId = order.Id, OperationKind = "assign_driver", CreatedAtMillis = NowUnixMs(),
                ActorRole = GetCurrentBackendActorRole(), ActorUserId = GetCurrentSessionUserId(),
                DriverUserId = driverUserId, DriverName = driver.Name, DriverPhone = driver.Phone, MarkDriverReceiptPrinted = order.DriverReceiptPrinted
            });
        }

        // ──────────────────────────────────────────────────────────────────────
        // ERROR TRACKING
        // ──────────────────────────────────────────────────────────────────────
        private void ClearLastAppOrderMutationBackendError()
        {
            lock (_supabaseAppOrderMutationErrorSync) { _lastAppOrderMutationBackendError = string.Empty; }
        }

        private void RememberAppOrderMutationBackendError(string scope, string details)
        {
            var message = (details ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(message)) return;
            if (message.Length > 600) message = message.Substring(0, 600) + " ...";
            lock (_supabaseAppOrderMutationErrorSync) { _lastAppOrderMutationBackendError = message; }
            AppendPosRuntimeLog(scope, message);
            try { ShowSupabaseServerError(scope, message); } catch { }
        }

        private string GetAppOrderBackendFailureMessage(string defaultMessage)
        {
            var summary = (defaultMessage ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(summary)) summary = "تعذر تنفيذ العملية على الباك اند.";
            string details;
            lock (_supabaseAppOrderMutationErrorSync) { details = (_lastAppOrderMutationBackendError ?? string.Empty).Trim(); }
            if (string.IsNullOrWhiteSpace(details)) return summary;
            return summary + Environment.NewLine + details;
        }

        // ──────────────────────────────────────────────────────────────────────
        // STATUS PUSH WITH FALLBACK (IDEMPOTENCY QUEUE)
        // ──────────────────────────────────────────────────────────────────────
        private bool TryPushAppOrderStatusNowOrQueue(SupabaseClientConfigRecord cfg, AppOrderMutationQueueItem item)
        {
            if (cfg == null || item == null || string.IsNullOrWhiteSpace(item.OrderId)) return false;
            var pushedNow = false;
            var pushCompletedWithoutTransportError = false;
            ClearLastAppOrderMutationBackendError();
            try
            {
                pushedNow = UpdateAppOrderStatusWithFallbackAsync(cfg, new SupabasePosUpdateOrderStatusIdempotentRequest
                {
                    OrderId = item.OrderId, ToStatus = item.ToStatus, ActorRole = item.ActorRole, ActorUserId = item.ActorUserId,
                    IdempotencyKey = Fale7_POS.Infrastructure.Storage.LocalIdempotencyEngine.GenerateIdempotencyKey("STAT")
                }).GetAwaiter().GetResult();
                pushCompletedWithoutTransportError = true;
            }
            catch (Exception ex)
            {
                AppendPosRuntimeLog("AppOrderStatusPush", ex);
                RememberAppOrderMutationBackendError("App order status sync", "order=" + item.OrderId + ", target_status=" + (item.ToStatus ?? string.Empty) + ", error=" + ex.Message);
                Debug.WriteLine("Immediate app order status push failed for order " + item.OrderId + ": " + ex.Message);
            }
            if (pushedNow || WasPendingAppOrderMutationAlreadyApplied(cfg, item))
            {
                ClearLastAppOrderMutationBackendError(); QueueSupabaseAppOrdersPullFromBackend(); EndAppOrderBusyState(item.OrderId); return true;
            }
            if (pushCompletedWithoutTransportError)
            {
                RememberAppOrderMutationBackendError("App order status sync", "order=" + item.OrderId + ", target_status=" + (item.ToStatus ?? string.Empty) + ", error=backend_returned_false");
                QueueSupabaseAppOrdersPullFromBackend(); EndAppOrderBusyState(item.OrderId); return false;
            }
            // Fallback to idempotency queue
            EnqueueIdempotentMutation(new IdempotencyAwareQueueItem
            {
                OrderId = item.OrderId, OperationKind = "status", CreatedAtMillis = item.CreatedAtMillis,
                ActorRole = item.ActorRole, ActorUserId = item.ActorUserId, ToStatus = item.ToStatus,
                MarkKitchenPrinted = item.MarkKitchenPrinted, MarkDriverReceiptPrinted = item.MarkDriverReceiptPrinted,
                MarkPartnerReceiptPrinted = item.MarkPartnerReceiptPrinted
            });
            QueueSupabaseAppOrdersPullFromBackend(); EndAppOrderBusyState(item.OrderId); return false;
        }

        private bool TryPushAppDriverAssignmentNowOrQueue(SupabaseClientConfigRecord cfg, AppOrderMutationQueueItem item)
        {
            if (cfg == null || item == null || string.IsNullOrWhiteSpace(item.OrderId)) return false;
            var pushedNow = false;
            var pushCompletedWithoutTransportError = false;
            ClearLastAppOrderMutationBackendError();
            try
            {
                pushedNow = AssignDriverWithFallbackAsync(cfg, new SupabasePosAssignDriverIdempotentRequest
                {
                    OrderId = item.OrderId, IdempotencyKey = Fale7_POS.Infrastructure.Storage.LocalIdempotencyEngine.GenerateIdempotencyKey("DRV"),
                    DriverUserId = item.DriverUserId, DriverName = item.DriverName
                }).GetAwaiter().GetResult();
                pushCompletedWithoutTransportError = true;
            }
            catch (Exception ex)
            {
                AppendPosRuntimeLog("AppOrderAssignDriver", ex);
                RememberAppOrderMutationBackendError("App order driver sync", "order=" + item.OrderId + ", driver_user_id=" + (item.DriverUserId ?? string.Empty) + ", error=" + ex.Message);
                Debug.WriteLine("Immediate app driver assignment failed for order " + item.OrderId + ": " + ex.Message);
            }
            if (pushedNow || WasPendingAppOrderMutationAlreadyApplied(cfg, item))
            {
                ClearLastAppOrderMutationBackendError(); QueueSupabaseAppOrdersPullFromBackend(); EndAppOrderBusyState(item.OrderId); return true;
            }
            if (pushCompletedWithoutTransportError)
            {
                RememberAppOrderMutationBackendError("App order driver sync", "order=" + item.OrderId + ", driver_user_id=" + (item.DriverUserId ?? string.Empty) + ", error=backend_returned_false");
                QueueSupabaseAppOrdersPullFromBackend(); EndAppOrderBusyState(item.OrderId); return false;
            }
            EnqueueIdempotentMutation(new IdempotencyAwareQueueItem
            {
                OrderId = item.OrderId, OperationKind = "assign_driver", CreatedAtMillis = item.CreatedAtMillis,
                ActorRole = item.ActorRole, ActorUserId = item.ActorUserId, DriverUserId = item.DriverUserId,
                DriverName = item.DriverName, DriverPhone = item.DriverPhone, MarkDriverReceiptPrinted = item.MarkDriverReceiptPrinted
            });
            QueueSupabaseAppOrdersPullFromBackend(); EndAppOrderBusyState(item.OrderId); return false;
        }

        // ──────────────────────────────────────────────────────────────────────
        // DUPLICATE DETECTION
        // ──────────────────────────────────────────────────────────────────────
        private bool WasPendingAppOrderMutationAlreadyApplied(SupabaseClientConfigRecord cfg, AppOrderMutationQueueItem item)
        {
            try { return WasPendingAppOrderMutationAlreadyAppliedAsync(cfg, item).GetAwaiter().GetResult(); }
            catch { return false; }
        }

        private async Task<bool> WasPendingAppOrderMutationAlreadyAppliedAsync(SupabaseClientConfigRecord cfg, AppOrderMutationQueueItem item)
        {
            if (cfg == null || item == null || string.IsNullOrWhiteSpace(item.OrderId)) return false;
            var order = await FetchSupabaseAppOrderByIdAsync(cfg, item.OrderId).ConfigureAwait(false);
            if (order == null || string.IsNullOrWhiteSpace(order.Id)) return false;
            var operation = (item.OperationKind ?? string.Empty).Trim().ToLowerInvariant();
            var currentStatus = (order.Status ?? string.Empty).Trim().ToUpperInvariant();
            var currentOrderType = NormalizeQueuedAppOrderType(order.OrderType);
            if (operation == "status")
            {
                var targetStatus = (item.ToStatus ?? string.Empty).Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(targetStatus)) return false;
                if (string.Equals(currentStatus, targetStatus, StringComparison.OrdinalIgnoreCase)) return true;
                var currentStage = GetQueuedAppOrderStatusStage(currentOrderType, currentStatus);
                var targetStage = GetQueuedAppOrderStatusStage(currentOrderType, targetStatus);
                return currentStage > 0 && targetStage > 0 && currentStage >= targetStage;
            }
            if (operation == "assign_driver")
            {
                var queuedDriverUserId = (item.DriverUserId ?? string.Empty).Trim();
                var currentDriverUserId = (order.DriverUserId ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(queuedDriverUserId) && !string.IsNullOrWhiteSpace(currentDriverUserId)
                    && string.Equals(queuedDriverUserId, currentDriverUserId, StringComparison.OrdinalIgnoreCase)) return true;
                var currentStage = GetQueuedAppOrderStatusStage(currentOrderType, currentStatus);
                return currentStage > 3;
            }
            return false;
        }

        // ──────────────────────────────────────────────────────────────────────
        // DRIVER RESOLUTION
        // ──────────────────────────────────────────────────────────────────────
        private string ResolveBackendDriverUserId(DeliveryDriverRecord driver)
        {
            if (driver == null) return null;
            var directId = (driver.AppUserId ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(directId)) return directId;
            var phone = (driver.Phone ?? string.Empty).Trim();
            var name = (driver.Name ?? string.Empty).Trim();
            for (int i = 0; i < _deliveryDrivers.Count; i++)
            {
                var d = _deliveryDrivers[i];
                if (d == null || string.IsNullOrWhiteSpace(d.AppUserId)) continue;
                var dPhone = (d.Phone ?? string.Empty).Trim();
                var dName = (d.Name ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(phone) && string.Equals(phone, dPhone, StringComparison.OrdinalIgnoreCase)) return d.AppUserId.Trim();
                if (!string.IsNullOrWhiteSpace(name) && string.Equals(name, dName, StringComparison.OrdinalIgnoreCase)) return d.AppUserId.Trim();
            }
            return null;
        }

        // ──────────────────────────────────────────────────────────────────────
        // RPC WRAPPERS
        // ──────────────────────────────────────────────────────────────────────
        private async Task<bool> UpdateAppOrderStatusWithFallbackAsync(SupabaseClientConfigRecord cfg, SupabasePosUpdateOrderStatusIdempotentRequest payload)
        {
            return await PostSupabaseRpcExpectBoolAsync(cfg, "api_pos_update_order_status_idempotent", payload).ConfigureAwait(false);
        }

        private async Task<bool> AssignDriverWithFallbackAsync(SupabaseClientConfigRecord cfg, SupabasePosAssignDriverIdempotentRequest payload)
        {
            return await PostSupabaseRpcExpectBoolAsync(cfg, "api_pos_assign_driver", payload).ConfigureAwait(false);
        }

        private async Task UpdateAppOrderAddressWithFallbackAsync(SupabaseClientConfigRecord cfg, string orderId, string district, string addressText)
        {
            var configuredTable = string.IsNullOrWhiteSpace(cfg.OrdersTable) ? "app_orders" : cfg.OrdersTable.Trim();
            try { await PatchAppOrderAddressAsync(cfg, configuredTable, orderId, district, addressText).ConfigureAwait(false); }
            catch
            {
                if (string.Equals(configuredTable, "app_orders", StringComparison.OrdinalIgnoreCase)) throw;
                await PatchAppOrderAddressAsync(cfg, "app_orders", orderId, district, addressText).ConfigureAwait(false);
            }
        }

        private async Task UpdateCustomerDeliveryAddressAndOrdersWithFallbackAsync(SupabaseClientConfigRecord cfg, AppOrderMutationQueueItem item)
        {
            if (cfg == null || item == null) return;
            var configuredTable = string.IsNullOrWhiteSpace(cfg.OrdersTable) ? "app_orders" : cfg.OrdersTable.Trim();
            try { await UpdateCustomerDeliveryAddressAndOrdersAsync(cfg, configuredTable, item).ConfigureAwait(false); }
            catch
            {
                if (string.Equals(configuredTable, "app_orders", StringComparison.OrdinalIgnoreCase)) throw;
                await UpdateCustomerDeliveryAddressAndOrdersAsync(cfg, "app_orders", item).ConfigureAwait(false);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // CUSTOMER DELIVERY ADDRESS SYNC
        // ──────────────────────────────────────────────────────────────────────
        private async Task UpdateCustomerDeliveryAddressAndOrdersAsync(SupabaseClientConfigRecord cfg, string ordersTable, AppOrderMutationQueueItem item)
        {
            var rows = await FetchCustomerDeliveryOrdersForAddressUpdateAsync(cfg, ordersTable, item).ConfigureAwait(false);
            if ((rows == null || rows.Count == 0) && !string.IsNullOrWhiteSpace((item.CustomerUserId ?? string.Empty).Trim())
                && !string.IsNullOrWhiteSpace((item.CustomerPhone ?? string.Empty).Trim()))
            {
                rows = await FetchCustomerDeliveryOrdersForAddressUpdateAsync(cfg, ordersTable, new AppOrderMutationQueueItem
                { OrderId = item.OrderId, CustomerUserId = string.Empty, CustomerPhone = item.CustomerPhone }).ConfigureAwait(false);
            }
            if ((rows == null || rows.Count == 0) && !string.IsNullOrWhiteSpace(item.OrderId))
            {
                rows = await FetchCustomerDeliveryOrdersForAddressUpdateAsync(cfg, ordersTable, new AppOrderMutationQueueItem
                { OrderId = item.OrderId, CustomerUserId = string.Empty, CustomerPhone = string.Empty }).ConfigureAwait(false);
            }
            rows = rows ?? new List<SupabaseAppOrderRow>();
            var desiredFee = item.DeliveryFee < 0 ? 0 : item.DeliveryFee;
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null || string.IsNullOrWhiteSpace(row.Id)) continue;
                var subtotal = row.Subtotal < 0 ? 0 : row.Subtotal;
                var oldFee = row.DeliveryFee < 0 ? 0 : row.DeliveryFee;
                var oldTotal = row.Total > 0 ? row.Total : (subtotal + oldFee);
                var discount = (subtotal + oldFee) - oldTotal;
                if (discount < 0) discount = 0;
                var nextTotal = subtotal + desiredFee - discount;
                if (nextTotal < 0) nextTotal = 0;
                await PatchAppOrderAddressAsync(cfg, ordersTable, row.Id, item.District, item.AddressText, desiredFee, nextTotal, BuildUpdatedAppOrderAddressMetadata(row, item)).ConfigureAwait(false);
            }
            await SyncCustomerAddressAfterCashierEditAsync(cfg, item).ConfigureAwait(false);
        }

        private async Task<List<SupabaseAppOrderRow>> FetchCustomerDeliveryOrdersForAddressUpdateAsync(SupabaseClientConfigRecord cfg, string tableName, AppOrderMutationQueueItem item)
        {
            var select = "id,customer_user_id,customer_phone,subtotal,delivery_fee,total,metadata";
            var filters = new List<string>();
            filters.Add("select=" + Uri.EscapeDataString(select));
            filters.Add("order_type=in.(DELIVERY,POS_DELIVERY)");
            var customerUserId = item == null ? string.Empty : (item.CustomerUserId ?? string.Empty).Trim();
            var customerPhone = item == null ? string.Empty : (item.CustomerPhone ?? string.Empty).Trim();
            var orderId = item == null ? string.Empty : (item.OrderId ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(customerUserId)) filters.Add("customer_user_id=eq." + Uri.EscapeDataString(customerUserId));
            else if (!string.IsNullOrWhiteSpace(customerPhone)) filters.Add("customer_phone=eq." + Uri.EscapeDataString(customerPhone));
            else if (!string.IsNullOrWhiteSpace(orderId)) filters.Add("id=eq." + Uri.EscapeDataString(orderId));
            else return new List<SupabaseAppOrderRow>();
            filters.Add("order=created_at.desc");
            filters.Add("limit=300");
            var url = cfg.Url + "/rest/v1/" + tableName + "?" + string.Join("&", filters.ToArray());
            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                ApplySupabaseRestHeaders(req, cfg, true);
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    var raw = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode) throw new InvalidOperationException("HTTP " + (int)resp.StatusCode + " " + raw);
                    return DeserializeJson<List<SupabaseAppOrderRow>>(raw) ?? new List<SupabaseAppOrderRow>();
                }
            }
        }

        private async Task SyncCustomerAddressAfterCashierEditAsync(SupabaseClientConfigRecord cfg, AppOrderMutationQueueItem item)
        {
            if (cfg == null || item == null) return;
            var customerUserId = (item.CustomerUserId ?? string.Empty).Trim();
            var customerPhone = (item.CustomerPhone ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(customerPhone)) customerPhone = customerUserId;
            if (string.IsNullOrWhiteSpace(customerPhone) || string.IsNullOrWhiteSpace((item.DistrictId ?? string.Empty).Trim())) return;
            var existing = await FetchCustomerAddressesForOwnerAsync(cfg, customerUserId, customerPhone).ConfigureAwait(false);
            if (existing.Count == 0 && !string.IsNullOrWhiteSpace(customerUserId) && !string.IsNullOrWhiteSpace(customerPhone))
                existing = await FetchCustomerAddressesForOwnerAsync(cfg, string.Empty, customerPhone).ConfigureAwait(false);
            SupabaseCustomerAddressRow target = null;
            var queuedAddressId = (item.AddressId ?? string.Empty).Trim();
            if (IsSupabaseAddressId(queuedAddressId))
            {
                for (int i = 0; i < existing.Count; i++) { var row = existing[i]; if (row != null && string.Equals(row.Id.ToString(), queuedAddressId, StringComparison.Ordinal)) { target = row; break; } }
                if (target == null) target = await FetchCustomerAddressByIdAsync(cfg, queuedAddressId).ConfigureAwait(false);
            }
            if (target == null)
            {
                for (int i = 0; i < existing.Count; i++) { var row = existing[i]; if (row == null) continue; if (!string.Equals((row.DistrictId ?? string.Empty).Trim(), (item.DistrictId ?? string.Empty).Trim(), StringComparison.Ordinal)) continue; if (!string.Equals((row.Block ?? string.Empty).Trim(), (item.AddressBlock ?? string.Empty).Trim(), StringComparison.Ordinal)) continue; if (!string.Equals((row.Street ?? string.Empty).Trim(), (item.AddressStreet ?? string.Empty).Trim(), StringComparison.Ordinal)) continue; if (!string.Equals((row.Building ?? string.Empty).Trim(), (item.AddressBuilding ?? string.Empty).Trim(), StringComparison.Ordinal)) continue; if (!string.Equals((row.Apartment ?? string.Empty).Trim(), (item.AddressApartment ?? string.Empty).Trim(), StringComparison.Ordinal)) continue; target = row; break; }
            }
            if (target == null) { for (int i = 0; i < existing.Count; i++) { var row = existing[i]; if (row != null && row.IsDefault) { target = row; break; } } }
            if (target == null && existing.Count > 0) target = existing[0];
            var targetCustomerUserId = target == null ? string.Empty : (target.CustomerUserId ?? string.Empty).Trim();
            var targetCustomerPhone = target == null ? string.Empty : (target.CustomerPhone ?? string.Empty).Trim();
            var effectiveCustomerUserId = !string.IsNullOrWhiteSpace(targetCustomerUserId) ? targetCustomerUserId : customerUserId;
            if (!string.IsNullOrWhiteSpace(customerUserId)) effectiveCustomerUserId = customerUserId;
            var effectiveCustomerPhone = !string.IsNullOrWhiteSpace(targetCustomerPhone) ? targetCustomerPhone : customerPhone;
            if (!string.IsNullOrWhiteSpace(customerPhone)) effectiveCustomerPhone = customerPhone;
            if (string.IsNullOrWhiteSpace(effectiveCustomerPhone)) effectiveCustomerPhone = effectiveCustomerUserId;
            var shouldBeDefault = target == null || target.IsDefault;
            if (target != null && shouldBeDefault) await ClearDefaultCustomerAddressesAsync(cfg, effectiveCustomerUserId, effectiveCustomerPhone, target.Id).ConfigureAwait(false);
            var payload = new SupabaseCustomerAddressPatchRow
            {
                CustomerUserId = string.IsNullOrWhiteSpace(effectiveCustomerUserId) ? null : effectiveCustomerUserId,
                CustomerPhone = effectiveCustomerPhone,
                Label = target != null && !string.IsNullOrWhiteSpace(target.Label) ? target.Label.Trim() : (string.IsNullOrWhiteSpace(item.District) ? "عنوان" : item.District.Trim()),
                DistrictId = (item.DistrictId ?? string.Empty).Trim(), Block = item.AddressBlock ?? string.Empty, Street = item.AddressStreet ?? string.Empty,
                Building = item.AddressBuilding ?? string.Empty, Apartment = item.AddressApartment ?? string.Empty, Note = item.AddressNote ?? string.Empty, IsDefault = shouldBeDefault
            };
            if (target == null) { await InsertCustomerAddressAsync(cfg, payload).ConfigureAwait(false); return; }
            await PatchCustomerAddressByIdAsync(cfg, target.Id, payload).ConfigureAwait(false);
        }

        private async Task<SupabaseCustomerAddressRow> FetchCustomerAddressByIdAsync(SupabaseClientConfigRecord cfg, string addressId)
        {
            addressId = (addressId ?? string.Empty).Trim();
            if (!IsSupabaseAddressId(addressId)) return null;
            var select = Uri.EscapeDataString("id,customer_user_id,customer_phone,label,district_id,block,street,building,apt,note,is_default");
            var url = cfg.Url + "/rest/v1/customer_addresses?select=" + select + "&id=eq." + Uri.EscapeDataString(addressId) + "&limit=1";
            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                ApplySupabaseRestHeaders(req, cfg, true);
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    var raw = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode) throw new InvalidOperationException("HTTP " + (int)resp.StatusCode + " " + raw);
                    var rows = DeserializeJson<List<SupabaseCustomerAddressRow>>(raw) ?? new List<SupabaseCustomerAddressRow>();
                    return rows.Count > 0 ? rows[0] : null;
                }
            }
        }

        private async Task<List<SupabaseCustomerAddressRow>> FetchCustomerAddressesForOwnerAsync(SupabaseClientConfigRecord cfg, string customerUserId, string customerPhone)
        {
            var filters = new List<string>();
            filters.Add("select=" + Uri.EscapeDataString("id,customer_user_id,customer_phone,label,district_id,block,street,building,apt,note,is_default"));
            customerUserId = (customerUserId ?? string.Empty).Trim(); customerPhone = (customerPhone ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(customerUserId)) filters.Add("customer_user_id=eq." + Uri.EscapeDataString(customerUserId));
            else if (!string.IsNullOrWhiteSpace(customerPhone)) filters.Add("customer_phone=eq." + Uri.EscapeDataString(customerPhone));
            else return new List<SupabaseCustomerAddressRow>();
            filters.Add("order=is_default.desc,id.asc");
            var url = cfg.Url + "/rest/v1/customer_addresses?" + string.Join("&", filters.ToArray());
            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                ApplySupabaseRestHeaders(req, cfg, true);
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    var raw = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode) throw new InvalidOperationException("HTTP " + (int)resp.StatusCode + " " + raw);
                    return DeserializeJson<List<SupabaseCustomerAddressRow>>(raw) ?? new List<SupabaseCustomerAddressRow>();
                }
            }
        }

        private async Task ClearDefaultCustomerAddressesAsync(SupabaseClientConfigRecord cfg, string customerUserId, string customerPhone, int exceptId)
        {
            var filters = new List<string>();
            if (!string.IsNullOrWhiteSpace(customerUserId)) filters.Add("customer_user_id=eq." + Uri.EscapeDataString(customerUserId));
            else filters.Add("customer_phone=eq." + Uri.EscapeDataString(customerPhone));
            if (exceptId > 0) filters.Add("id=neq." + exceptId.ToString());
            var url = cfg.Url + "/rest/v1/customer_addresses?" + string.Join("&", filters.ToArray());
            using (var req = new HttpRequestMessage(new HttpMethod("PATCH"), url))
            {
                ApplySupabaseRestHeaders(req, cfg, false);
                req.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
                req.Content = new StringContent("{\"is_default\":false}", Encoding.UTF8, "application/json");
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    var raw = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode) throw new InvalidOperationException("HTTP " + (int)resp.StatusCode + " " + raw);
                }
            }
        }

        private async Task PatchCustomerAddressByIdAsync(SupabaseClientConfigRecord cfg, int addressId, SupabaseCustomerAddressPatchRow payload)
        {
            var rawPayload = SerializeJson(payload);
            var url = cfg.Url + "/rest/v1/customer_addresses?id=eq." + addressId.ToString();
            using (var req = new HttpRequestMessage(new HttpMethod("PATCH"), url))
            {
                ApplySupabaseRestHeaders(req, cfg, false);
                req.Headers.TryAddWithoutValidation("Prefer", "return=representation");
                req.Content = new StringContent(rawPayload, Encoding.UTF8, "application/json");
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    var raw = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode) throw new InvalidOperationException("HTTP " + (int)resp.StatusCode + " " + raw);
                }
            }
        }

        private async Task InsertCustomerAddressAsync(SupabaseClientConfigRecord cfg, SupabaseCustomerAddressPatchRow payload)
        {
            var rawPayload = SerializeJson(payload);
            var url = cfg.Url + "/rest/v1/customer_addresses";
            using (var req = new HttpRequestMessage(HttpMethod.Post, url))
            {
                ApplySupabaseRestHeaders(req, cfg, false);
                req.Headers.TryAddWithoutValidation("Prefer", "return=representation");
                req.Content = new StringContent(rawPayload, Encoding.UTF8, "application/json");
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    var raw = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode) throw new InvalidOperationException("HTTP " + (int)resp.StatusCode + " " + raw);
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // FETCH APP ORDERS
        // ──────────────────────────────────────────────────────────────────────
        private async Task<List<SupabaseAppOrderRow>> FetchSupabaseAppOrdersAsync(SupabaseClientConfigRecord cfg)
        {
            await EnsureSupabaseBackendSessionAsync(cfg, false).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(GetCurrentSessionToken()))
                await EnsureSupabaseBackendSessionAsync(cfg, true).ConfigureAwait(false);
            var select = "id,order_type,status,customer_user_id,customer_name,customer_phone,district,address_text,driver_user_id,subtotal,delivery_fee,total,created_at,updated_at,metadata";
            var configuredTable = string.IsNullOrWhiteSpace(cfg.OrdersTable) ? "app_orders" : cfg.OrdersTable.Trim();
            Func<string, Task<List<SupabaseAppOrderRow>>> fetchFromTable = async delegate (string tableName)
            {
                var url = cfg.Url + "/rest/v1/" + tableName + "?select=" + Uri.EscapeDataString(select) + "&order=created_at.desc&limit=300";
                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    ApplySupabaseRestHeaders(req, cfg, true);
                    using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                    using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                    {
                        var raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!resp.IsSuccessStatusCode) throw new InvalidOperationException("HTTP " + (int)resp.StatusCode + " " + raw);
                        return DeserializeJson<List<SupabaseAppOrderRow>>(raw) ?? new List<SupabaseAppOrderRow>();
                    }
                }
            };
            Func<Task<List<SupabaseAppOrderRow>>> fetchFromRpc = async delegate
            {
                var raw = await PostSupabaseRpcGetStringAsync(cfg, "api_pos_app_orders_snapshot", "{\"p_limit\":300}",
                    actorRoleOverride: GetCurrentBackendActorRole(), actorUserIdOverride: GetCurrentSessionUserId()).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(raw)) return null;
                return DeserializeJson<List<SupabaseAppOrderRow>>(raw) ?? new List<SupabaseAppOrderRow>();
            };
            try
            {
                var rpcRows = await fetchFromRpc().ConfigureAwait(false);
                if (rpcRows != null) return rpcRows;
                return await fetchFromTable(configuredTable).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var message = ex.Message ?? string.Empty;
                if (message.IndexOf("HTTP 401", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("HTTP 403", StringComparison.OrdinalIgnoreCase) >= 0
                    || message.IndexOf("forbidden", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("permission denied", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    await EnsureSupabaseBackendSessionAsync(cfg, true).ConfigureAwait(false);
                    try { var retriedRpcRows = await fetchFromRpc().ConfigureAwait(false); if (retriedRpcRows != null) return retriedRpcRows; return await fetchFromTable(configuredTable).ConfigureAwait(false); } catch { }
                }
                if (string.Equals(configuredTable, "app_orders", StringComparison.OrdinalIgnoreCase)) throw;
                return await fetchFromTable("app_orders").ConfigureAwait(false);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // METADATA HELPERS
        // ──────────────────────────────────────────────────────────────────────
        private SupabaseAppOrderMetadataRow CloneSupabaseAppOrderMetadata(SupabaseAppOrderMetadataRow source)
        {
            if (source == null) return new SupabaseAppOrderMetadataRow();
            var clone = new SupabaseAppOrderMetadataRow
            {
                LocalOrderId = source.LocalOrderId, LocalOrderNo = source.LocalOrderNo, FinalizedAtMillis = source.FinalizedAtMillis,
                Source = source.Source, PosOrderType = source.PosOrderType, CashierUserId = source.CashierUserId, CashierUsername = source.CashierUsername, CashierRole = source.CashierRole,
                DriverName = source.DriverName, DriverPhone = source.DriverPhone, DriverNameCamel = source.DriverNameCamel, DriverPhoneCamel = source.DriverPhoneCamel,
                AddressId = source.AddressId, DistrictId = source.DistrictId, DistrictName = source.DistrictName, AddressLabel = source.AddressLabel,
                AddressBlock = source.AddressBlock, AddressStreet = source.AddressStreet, AddressBuilding = source.AddressBuilding,
                AddressApartment = source.AddressApartment, AddressNote = source.AddressNote, AddressTextFull = source.AddressTextFull
            };
            if (source.Items != null)
            {
                clone.Items = new List<SupabaseAppOrderItemRow>(source.Items.Count);
                for (int i = 0; i < source.Items.Count; i++) { var item = source.Items[i]; if (item == null) continue; clone.Items.Add(new SupabaseAppOrderItemRow { Name = item.Name, Qty = item.Qty, Price = item.Price }); }
            }
            return clone;
        }

        private SupabaseAppOrderMetadataRow BuildUpdatedAppOrderAddressMetadata(SupabaseAppOrderRow row, AppOrderMutationQueueItem item)
        {
            var metadata = CloneSupabaseAppOrderMetadata(row == null ? null : row.Metadata);
            if (item == null) return metadata;
            var addressId = (item.AddressId ?? string.Empty).Trim();
            metadata.AddressId = IsSupabaseAddressId(addressId) ? addressId : null;
            metadata.DistrictId = (item.DistrictId ?? string.Empty).Trim();
            metadata.DistrictName = string.IsNullOrWhiteSpace(item.District) ? string.Empty : item.District.Trim();
            metadata.AddressLabel = string.IsNullOrWhiteSpace(metadata.AddressLabel) ? (string.IsNullOrWhiteSpace(item.District) ? "عنوان العميل" : item.District.Trim()) : metadata.AddressLabel.Trim();
            metadata.AddressBlock = item.AddressBlock ?? string.Empty; metadata.AddressStreet = item.AddressStreet ?? string.Empty;
            metadata.AddressBuilding = item.AddressBuilding ?? string.Empty; metadata.AddressApartment = item.AddressApartment ?? string.Empty;
            metadata.AddressNote = item.AddressNote ?? string.Empty;
            metadata.AddressTextFull = string.IsNullOrWhiteSpace(item.AddressText) ? null : item.AddressText.Trim();
            return metadata;
        }

        // ──────────────────────────────────────────────────────────────────────
        // PATCH APP ORDER ADDRESS
        // ──────────────────────────────────────────────────────────────────────
        private async Task PatchAppOrderAddressAsync(SupabaseClientConfigRecord cfg, string tableName, string orderId, string district, string addressText, double? deliveryFee = null, double? total = null, SupabaseAppOrderMetadataRow metadata = null)
        {
            if (cfg == null || string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(orderId)) throw new InvalidOperationException("invalid_app_order_address_patch");
            var payload = new SupabaseAppOrderAddressPatchRow
            {
                District = string.IsNullOrWhiteSpace(district) ? null : district.Trim(),
                AddressText = string.IsNullOrWhiteSpace(addressText) ? null : addressText.Trim(),
                DeliveryFee = deliveryFee, Total = total, Metadata = metadata
            };
            var url = cfg.Url + "/rest/v1/" + tableName.Trim() + "?id=eq." + Uri.EscapeDataString(orderId.Trim());
            var rawPayload = SerializeJson(payload);
            using (var req = new HttpRequestMessage(new HttpMethod("PATCH"), url))
            {
                ApplySupabaseRestHeaders(req, cfg, true);
                req.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
                req.Content = new StringContent(rawPayload, Encoding.UTF8, "application/json");
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    if (resp.IsSuccessStatusCode) return;
                    var err = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new InvalidOperationException("PATCH app order address failed: HTTP " + (int)resp.StatusCode + " " + err);
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // SESSION / ERROR HELPERS
        // ──────────────────────────────────────────────────────────────────────
        private bool IsSupabaseSessionRetryableError(string error)
        {
            var message = (error ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(message)) return false;
            return message.IndexOf("HTTP 401", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("HTTP 403", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("forbidden", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("permission denied", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TryParseSupabaseRpcBool(string raw, out bool value)
        {
            value = false;
            var trimmed = (raw ?? string.Empty).Trim();
            if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase)) { value = true; return true; }
            if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase)) { value = false; return true; }
            bool parsed;
            if (bool.TryParse(trimmed, out parsed)) { value = parsed; return true; }
            return false;
        }

        private async Task<bool> PostSupabaseRpcExpectBoolAsync<TReq>(SupabaseClientConfigRecord cfg, string rpcName, TReq payload)
        {
            if (cfg == null) throw new ArgumentNullException("cfg");
            if (string.IsNullOrWhiteSpace(rpcName)) throw new ArgumentNullException("rpcName");
            var url = cfg.Url + "/rest/v1/rpc/" + rpcName.Trim();
            var body = SerializeJson(payload);
            string raw;
            await EnsureSupabaseBackendSessionAsync(cfg, false).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(GetCurrentSessionToken())) await EnsureSupabaseBackendSessionAsync(cfg, true).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(GetCurrentSessionToken())) throw new InvalidOperationException("RPC " + rpcName + " requires an active backend session.");
            Func<Task<string>> post = async delegate
            {
                using (var req = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    ApplySupabaseRestHeaders(req, cfg, true);
                    req.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                    using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                    {
                        var responseRaw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!resp.IsSuccessStatusCode) throw new InvalidOperationException("RPC " + rpcName + " failed: HTTP " + (int)resp.StatusCode + " " + responseRaw);
                        return responseRaw;
                    }
                }
            };
            try { raw = await post().ConfigureAwait(false); }
            catch (Exception ex) { if (!IsSupabaseSessionRetryableError(ex.Message)) throw; await EnsureSupabaseBackendSessionAsync(cfg, true).ConfigureAwait(false); raw = await post().ConfigureAwait(false); }
            bool parsedValue;
            if (!TryParseSupabaseRpcBool(raw, out parsedValue)) throw new InvalidOperationException("RPC " + rpcName + " returned unexpected payload: " + raw.Trim());
            if (parsedValue) return true;
            await EnsureSupabaseBackendSessionAsync(cfg, true).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(GetCurrentSessionToken())) throw new InvalidOperationException("RPC " + rpcName + " backend session refresh failed.");
            raw = await post().ConfigureAwait(false);
            if (!TryParseSupabaseRpcBool(raw, out parsedValue)) throw new InvalidOperationException("RPC " + rpcName + " returned unexpected payload after refresh: " + raw.Trim());
            return parsedValue;
        }

        private async Task<string> PostSupabaseRpcGetStringAsync(SupabaseClientConfigRecord cfg, string rpcName, string requestBody, string actorRoleOverride = null, string actorUserIdOverride = null, bool throwOnHttpError = false)
        {
            if (cfg == null || string.IsNullOrWhiteSpace(rpcName)) return null;
            var url = cfg.Url + "/rest/v1/rpc/" + rpcName.Trim();
            using (var req = new HttpRequestMessage(HttpMethod.Post, url))
            {
                ApplySupabaseRestHeaders(req, cfg, true);
                var overrideRole = (actorRoleOverride ?? string.Empty).Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(overrideRole)) { req.Headers.Remove("x-fale7-role"); req.Headers.Remove("x_fale7_role"); req.Headers.TryAddWithoutValidation("x-fale7-role", overrideRole); req.Headers.TryAddWithoutValidation("x_fale7_role", overrideRole); }
                var overrideUserId = (actorUserIdOverride ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(overrideUserId)) { req.Headers.Remove("x-fale7-user-id"); req.Headers.Remove("x_fale7_user_id"); req.Headers.TryAddWithoutValidation("x-fale7-user-id", overrideUserId); req.Headers.TryAddWithoutValidation("x_fale7_user_id", overrideUserId); }
                req.Content = new StringContent(requestBody ?? "{}", Encoding.UTF8, "application/json");
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    var raw = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode) { if (throwOnHttpError) throw new InvalidOperationException("RPC " + rpcName + " failed: HTTP " + (int)resp.StatusCode + " " + (raw ?? string.Empty).Trim()); return null; }
                    return raw;
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // SHIFT ARCHIVE
        // ──────────────────────────────────────────────────────────────────────
        private void ApplySupabaseShiftArchiveRows(List<SupabaseAppOrderRow> rows)
        {
            if (rows == null || rows.Count == 0) return;
            for (int i = 0; i < rows.Count; i++) ApplySupabaseShiftArchiveRow(rows[i]);
        }

        private void ApplySupabaseShiftArchiveRow(SupabaseAppOrderRow row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.Id)) return;
            var backendOrderId = row.Id.Trim();
            if (string.IsNullOrWhiteSpace(backendOrderId)) return;
            var status = (row.Status ?? string.Empty).Trim().ToUpperInvariant();
            if (status == "MIGRATED") { RemoveTakeawayShiftRecordsByBackendIds(new List<string> { backendOrderId }); return; }
            if (status != "DONE" && status != "DELIVERED") return;
            var orderType = (row.OrderType ?? string.Empty).Trim().ToUpperInvariant();
            var metadataType = row.Metadata == null ? string.Empty : (row.Metadata.PosOrderType ?? string.Empty).Trim().ToUpperInvariant();
            var effectiveType = !string.IsNullOrWhiteSpace(orderType) ? orderType : metadataType;
            if (string.IsNullOrWhiteSpace(effectiveType)) return;
            var channelKind = string.Empty;
            if (effectiveType == "PICKUP" || effectiveType == "POS_PICKUP") channelKind = "PICKUP";
            else if (effectiveType == "DELIVERY" || effectiveType == "POS_DELIVERY") channelKind = "DELIVERY";
            else if (effectiveType == "TAKEAWAY" || effectiveType == "POS_TAKEAWAY") channelKind = "TAKEAWAY";
            if (string.IsNullOrWhiteSpace(channelKind)) return;
            var sourceKind = "APP";
            var metadataSource = row.Metadata == null ? string.Empty : (row.Metadata.Source ?? string.Empty).Trim().ToUpperInvariant();
            if (effectiveType.StartsWith("POS_", StringComparison.OrdinalIgnoreCase) || metadataSource == "POS" || metadataSource == "MANUAL") sourceKind = "MANUAL";
            var subtotal = (int)Math.Round(row.Subtotal < 0 ? 0 : row.Subtotal, MidpointRounding.AwayFromZero);
            var deliveryFee = (int)Math.Round(row.DeliveryFee < 0 ? 0 : row.DeliveryFee, MidpointRounding.AwayFromZero);
            var totalRaw = row.Total > 0 ? row.Total : (row.Subtotal + row.DeliveryFee);
            var total = (int)Math.Round(totalRaw < 0 ? 0 : totalRaw, MidpointRounding.AwayFromZero);
            var orderNo = DeriveSupabaseOrderNo(row);
            if (orderNo <= 0 && row.Metadata != null && row.Metadata.LocalOrderNo > 0) orderNo = row.Metadata.LocalOrderNo;
            var createdAtMillis = ParseIsoDateToUnixMs(row.CreatedAt);
            var finalizedAtMillis = row.Metadata != null && row.Metadata.FinalizedAtMillis > 0 ? row.Metadata.FinalizedAtMillis : ParseIsoDateToUnixMs(row.UpdatedAt);
            var record = new PosShiftOrderRecord
            {
                BackendOrderId = backendOrderId, SourceKind = sourceKind, ChannelKind = channelKind, OrderNo = orderNo,
                DisplayRef = orderNo > 0 ? orderNo.ToString() : (!string.IsNullOrWhiteSpace(row.Metadata == null ? null : row.Metadata.LocalOrderId) ? row.Metadata.LocalOrderId : backendOrderId),
                CustomerName = row.CustomerName ?? string.Empty, CustomerPhone = row.CustomerPhone ?? string.Empty,
                Subtotal = subtotal, DeliveryFee = deliveryFee, Total = total, CreatedAtMillis = createdAtMillis, FinalizedAtMillis = finalizedAtMillis,
                CashierUserId = row.Metadata == null ? string.Empty : (row.Metadata.CashierUserId ?? string.Empty),
                CashierUsername = row.Metadata == null ? string.Empty : (row.Metadata.CashierUsername ?? string.Empty),
                CashierRole = row.Metadata == null ? string.Empty : (row.Metadata.CashierRole ?? string.Empty)
            };
            AppendPosShiftOrderRecord(record);
        }

        private HashSet<string> BuildPendingPosShiftMigrationOrderIdsSet()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private bool IsHiddenByPendingMigration(HashSet<string> pendingIds, string orderId)
        {
            if (pendingIds == null || pendingIds.Count == 0) return false;
            var id = (orderId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id)) return false;
            if (pendingIds.Contains(id)) return true;
            var canonical = id.StartsWith("pos_", StringComparison.OrdinalIgnoreCase) ? id : BuildPosOfflineBackendOrderId(id);
            return !string.IsNullOrWhiteSpace(canonical) && pendingIds.Contains(canonical.Trim());
        }

        private bool RemoveLocalAppOrderById(string orderId)
        {
            var id = (orderId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id)) return false;
            var removed = false;
            for (int i = _deliveryAppDeliveryOrders.Count - 1; i >= 0; i--) { var existing = _deliveryAppDeliveryOrders[i]; if (existing == null || !string.Equals(existing.Id, id, StringComparison.OrdinalIgnoreCase)) continue; _deliveryAppDeliveryOrders.RemoveAt(i); removed = true; }
            for (int i = _deliveryAppPickupOrders.Count - 1; i >= 0; i--) { var existing = _deliveryAppPickupOrders[i]; if (existing == null || !string.Equals(existing.Id, id, StringComparison.OrdinalIgnoreCase)) continue; _deliveryAppPickupOrders.RemoveAt(i); removed = true; }
            if (_deliveryAppDeliverySelectedIds != null) { var removedSelection = _deliveryAppDeliverySelectedIds.RemoveWhere(delegate (string selectedId) { return string.Equals(selectedId, id, StringComparison.OrdinalIgnoreCase); }); if (removedSelection > 0) removed = true; }
            if (_deliveryAppPickupSelectedIds != null) { var removedSelection = _deliveryAppPickupSelectedIds.RemoveWhere(delegate (string selectedId) { return string.Equals(selectedId, id, StringComparison.OrdinalIgnoreCase); }); if (removedSelection > 0) removed = true; }
            _supabaseSeenAppOrderIds.Remove(id);
            return removed;
        }

        // ──────────────────────────────────────────────────────────────────────
        // APPLY CLOUD ORDERS TO LOCAL UI
        // ──────────────────────────────────────────────────────────────────────
        private void ApplySupabaseAppOrdersToLocalUi(List<SupabaseAppOrderRow> rows)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(delegate { try { ApplySupabaseAppOrdersToLocalUi(rows); } catch (Exception ex) { AppendPosRuntimeLog("UI_APPLY", $"فشل تحديث الواجهة ببيانات السحابة: {ex.Message}"); } }), DispatcherPriority.Background);
                return;
            }
            rows = rows ?? new List<SupabaseAppOrderRow>();
            var pendingMigrationIds = BuildPendingPosShiftMigrationOrderIdsSet();
            var visibleRows = new List<SupabaseAppOrderRow>();
            for (int i = 0; i < rows.Count; i++) { var row = rows[i]; if (row == null) continue; if (IsHiddenByPendingMigration(pendingMigrationIds, row.Id)) continue; visibleRows.Add(row); }
            ApplySupabaseShiftArchiveRows(visibleRows);
            var incomingOrderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var newDeliveryOrderRefs = new List<string>();
            var newPickupOrderRefs = new List<string>();
            var delivery = new List<AppDeliveryOrderRecord>();
            var pickup = new List<AppPickupOrderRecord>();
            var maxDeliveryNo = _deliveryAppDeliveryOrderSequence;
            var maxPickupNo = _deliveryAppPickupOrderSequence;
            for (int i = 0; i < visibleRows.Count; i++)
            {
                var row = visibleRows[i];
                if (row == null) continue;
                if (!string.IsNullOrWhiteSpace(row.Id)) { var rowId = row.Id.Trim(); incomingOrderIds.Add(rowId); if (_supabaseAppOrdersSeenPrimed && !_supabaseSeenAppOrderIds.Contains(rowId)) { var normalizedType = (row.OrderType ?? string.Empty).Trim().ToUpperInvariant(); var normalizedStatus = (row.Status ?? string.Empty).Trim().ToUpperInvariant(); if (normalizedStatus == "RECEIVED" || normalizedStatus == "PENDING") { var orderRef = DeriveSupabaseOrderNo(row).ToString(); if (normalizedType == "DELIVERY") newDeliveryOrderRefs.Add(orderRef); else if (normalizedType == "PICKUP") newPickupOrderRefs.Add(orderRef); } } }
                var orderType = (row.OrderType ?? string.Empty).Trim().ToUpperInvariant();
                if (orderType == "DELIVERY" || orderType == "POS_DELIVERY") { var mapped = MapSupabaseDeliveryOrderRow(row); if (mapped == null) continue; delivery.Add(mapped); if (mapped.No > maxDeliveryNo) maxDeliveryNo = mapped.No; continue; }
                if (orderType == "PICKUP") { var mapped = MapSupabasePickupOrderRow(row); if (mapped == null) continue; pickup.Add(mapped); if (mapped.No > maxPickupNo) maxPickupNo = mapped.No; continue; }
                if (orderType == "POS_TAKEAWAY" || orderType == "POS_DINE_IN") { var mapped = MapSupabaseDeliveryOrderRow(row); if (mapped == null) continue; delivery.Add(mapped); if (mapped.No > maxDeliveryNo) maxDeliveryNo = mapped.No; continue; }
                if (orderType == "POS_PICKUP") { var mapped = MapSupabasePickupOrderRow(row); if (mapped == null) continue; pickup.Add(mapped); if (mapped.No > maxPickupNo) maxPickupNo = mapped.No; continue; }
            }
            var nextSnapshotKey = BuildAppOrdersUiSnapshotKey(delivery, pickup);
            var uiChanged = !string.Equals(_supabaseAppOrdersUiSnapshotKey, nextSnapshotKey, StringComparison.Ordinal);
            if (uiChanged) { _deliveryAppDeliveryOrders.Clear(); for (int i = 0; i < delivery.Count; i++) _deliveryAppDeliveryOrders.Add(delivery[i]); _deliveryAppPickupOrders.Clear(); for (int i = 0; i < pickup.Count; i++) _deliveryAppPickupOrders.Add(pickup[i]); _deliveryAppDeliveryOrderSequence = maxDeliveryNo; _deliveryAppPickupOrderSequence = maxPickupNo; _deliveryAppOrdersInitialized = true; _supabaseAppOrdersUiSnapshotKey = nextSnapshotKey; PersistDeliveryState(); if (_deliveryAppDeliverySelectedIds != null) _deliveryAppDeliverySelectedIds.RemoveWhere(delegate (string id) { return !ContainsAppDeliveryOrderId(id); }); if (_deliveryAppPickupSelectedIds != null) _deliveryAppPickupSelectedIds.RemoveWhere(delegate (string id) { return !ContainsAppPickupOrderId(id); }); QueueSupabaseAppOrdersUiRefresh(); }
            else { _deliveryAppOrdersInitialized = true; }
            _supabaseSeenAppOrderIds.Clear();
            foreach (var id in incomingOrderIds) _supabaseSeenAppOrderIds.Add(id);
            if (!_supabaseAppOrdersSeenPrimed) { _supabaseAppOrdersSeenPrimed = true; return; }
            if (newDeliveryOrderRefs.Count > 0 || newPickupOrderRefs.Count > 0)
            {
                var parts = new List<string>();
                if (newDeliveryOrderRefs.Count > 0) parts.Add("دليفري: " + string.Join(", ", newDeliveryOrderRefs));
                if (newPickupOrderRefs.Count > 0) parts.Add("استلام: " + string.Join(", ", newPickupOrderRefs));
                ShowSupabaseToast("طلب تطبيق جديد", string.Join(" | ", parts), true);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // UI SNAPSHOT KEY
        // ──────────────────────────────────────────────────────────────────────
        private string BuildAppOrdersUiSnapshotKey(List<AppDeliveryOrderRecord> delivery, List<AppPickupOrderRecord> pickup)
        {
            var sb = new StringBuilder();
            delivery = delivery ?? new List<AppDeliveryOrderRecord>(); pickup = pickup ?? new List<AppPickupOrderRecord>();
            sb.Append("D:").Append(delivery.Count);
            for (int i = 0; i < delivery.Count; i++) { var order = delivery[i]; if (order == null) { sb.Append("|null"); continue; } sb.Append('|').Append(order.Id ?? string.Empty).Append('|').Append(order.No).Append('|').Append(order.AddressId ?? string.Empty).Append('|').Append(order.Status).Append('|').Append(order.District ?? string.Empty).Append('|').Append(order.AddressText ?? string.Empty).Append('|').Append(order.DeliveryFee).Append('|').Append(order.Total).Append('|').Append(order.KitchenPrinted ? "1" : "0").Append('|').Append(order.DriverReceiptPrinted ? "1" : "0").Append('|').Append(order.Driver == null ? string.Empty : (order.Driver.Id ?? string.Empty)); }
            sb.Append("#P:").Append(pickup.Count);
            for (int i = 0; i < pickup.Count; i++) { var order = pickup[i]; if (order == null) { sb.Append("|null"); continue; } sb.Append('|').Append(order.Id ?? string.Empty).Append('|').Append(order.No).Append('|').Append(order.Status).Append('|').Append(order.District ?? string.Empty).Append('|').Append(order.AddressText ?? string.Empty).Append('|').Append(order.PartnerReceiptPrinted ? "1" : "0"); }
            return sb.ToString();
        }

        // ──────────────────────────────────────────────────────────────────────
        // ITEM EQUIVALENCE
        // ──────────────────────────────────────────────────────────────────────
        private bool AreAppOrderItemsEquivalent(List<AppOrderItemRecord> left, List<AppOrderItemRecord> right)
        {
            if (ReferenceEquals(left, right)) return true;
            left = left ?? new List<AppOrderItemRecord>(); right = right ?? new List<AppOrderItemRecord>();
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++) { var a = left[i]; var b = right[i]; if (a == null || b == null) { if (!ReferenceEquals(a, b)) return false; continue; } if (!string.Equals(a.Name ?? string.Empty, b.Name ?? string.Empty, StringComparison.Ordinal)) return false; if (a.Qty != b.Qty || a.Price != b.Price) return false; }
            return true;
        }

        private bool AreAppDeliveryOrdersEquivalentForRealtime(AppDeliveryOrderRecord left, AppDeliveryOrderRecord right)
        {
            if (ReferenceEquals(left, right)) return true; if (left == null || right == null) return false;
            if (!string.Equals(left.Id ?? string.Empty, right.Id ?? string.Empty, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(left.CustomerUserId ?? string.Empty, right.CustomerUserId ?? string.Empty, StringComparison.Ordinal)) return false;
            if (!string.Equals(left.AddressId ?? string.Empty, right.AddressId ?? string.Empty, StringComparison.Ordinal)) return false;
            if (left.No != right.No || left.CreatedAt != right.CreatedAt) return false;
            if (!string.Equals(left.Name ?? string.Empty, right.Name ?? string.Empty, StringComparison.Ordinal)) return false;
            if (!string.Equals(left.Phone ?? string.Empty, right.Phone ?? string.Empty, StringComparison.Ordinal)) return false;
            if (!string.Equals(left.District ?? string.Empty, right.District ?? string.Empty, StringComparison.Ordinal)) return false;
            if (!string.Equals(left.AddressText ?? string.Empty, right.AddressText ?? string.Empty, StringComparison.Ordinal)) return false;
            if (!string.Equals(DeliveryAddressSignature(BuildDeliverySavedAddressFromAddress(left.Address)), DeliveryAddressSignature(BuildDeliverySavedAddressFromAddress(right.Address)), StringComparison.Ordinal)) return false;
            if (left.Subtotal != right.Subtotal || left.DeliveryFee != right.DeliveryFee || left.Total != right.Total) return false;
            if (left.Status != right.Status || left.KitchenPrinted != right.KitchenPrinted || left.DriverReceiptPrinted != right.DriverReceiptPrinted) return false;
            var leftDriver = left.Driver; var rightDriver = right.Driver;
            if ((leftDriver == null) != (rightDriver == null)) return false;
            if (leftDriver != null) { if (!string.Equals(leftDriver.Id ?? string.Empty, rightDriver.Id ?? string.Empty, StringComparison.Ordinal)) return false; if (!string.Equals(leftDriver.Name ?? string.Empty, rightDriver.Name ?? string.Empty, StringComparison.Ordinal)) return false; if (!string.Equals(leftDriver.Phone ?? string.Empty, rightDriver.Phone ?? string.Empty, StringComparison.Ordinal)) return false; if (!string.Equals(leftDriver.AppUserId ?? string.Empty, rightDriver.AppUserId ?? string.Empty, StringComparison.Ordinal)) return false; }
            return AreAppOrderItemsEquivalent(left.Items, right.Items);
        }

        private bool AreAppPickupOrdersEquivalentForRealtime(AppPickupOrderRecord left, AppPickupOrderRecord right)
        {
            if (ReferenceEquals(left, right)) return true; if (left == null || right == null) return false;
            if (!string.Equals(left.Id ?? string.Empty, right.Id ?? string.Empty, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(left.CustomerUserId ?? string.Empty, right.CustomerUserId ?? string.Empty, StringComparison.Ordinal)) return false;
            if (left.No != right.No || left.CreatedAt != right.CreatedAt) return false;
            if (!string.Equals(left.Name ?? string.Empty, right.Name ?? string.Empty, StringComparison.Ordinal)) return false;
            if (!string.Equals(left.Phone ?? string.Empty, right.Phone ?? string.Empty, StringComparison.Ordinal)) return false;
            if (!string.Equals(left.District ?? string.Empty, right.District ?? string.Empty, StringComparison.Ordinal)) return false;
            if (!string.Equals(left.AddressText ?? string.Empty, right.AddressText ?? string.Empty, StringComparison.Ordinal)) return false;
            if (left.Status != right.Status || left.PartnerReceiptPrinted != right.PartnerReceiptPrinted) return false;
            return AreAppOrderItemsEquivalent(left.Items, right.Items);
        }

        // ──────────────────────────────────────────────────────────────────────
        // REALTIME HANDLER
        // ──────────────────────────────────────────────────────────────────────
        private void ApplySupabaseRealtimeAppOrderChange(string eventType, SupabaseAppOrderRow record, SupabaseAppOrderRow oldRecord)
        {
            if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(new Action(delegate { try { ApplySupabaseRealtimeAppOrderChange(eventType, record, oldRecord); } catch (Exception ex) { Debug.WriteLine("Supabase realtime app_order apply failed: " + ex.Message); } }), DispatcherPriority.Background); return; }
            var normalizedEvent = (eventType ?? string.Empty).Trim().ToUpperInvariant();
            var source = string.Equals(normalizedEvent, "DELETE", StringComparison.Ordinal) ? (oldRecord ?? record) : (record ?? oldRecord);
            if (source == null || string.IsNullOrWhiteSpace(source.Id)) return;
            var rowId = source.Id.Trim();
            var orderType = (source.OrderType ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(orderType)) { if (ContainsAppDeliveryOrderId(rowId)) orderType = "DELIVERY"; else if (ContainsAppPickupOrderId(rowId)) orderType = "PICKUP"; }
            if (string.IsNullOrWhiteSpace(orderType)) return;
            var pendingMigrationIds = BuildPendingPosShiftMigrationOrderIdsSet();
            if (IsHiddenByPendingMigration(pendingMigrationIds, rowId)) { var removed = RemoveLocalAppOrderById(rowId); _supabaseAppOrdersSeenPrimed = true; if (removed) { PersistDeliveryState(); QueueSupabaseAppOrdersUiRefresh(); } return; }
            var seenPrimedAtStart = _supabaseAppOrdersSeenPrimed;
            var isDelete = string.Equals(normalizedEvent, "DELETE", StringComparison.Ordinal);
            var isNewPending = false; var newOrderRef = string.Empty; var changed = false;
            if (string.Equals(orderType, "POS_TAKEAWAY", StringComparison.Ordinal) || string.Equals(orderType, "POS_DINE_IN", StringComparison.Ordinal))
            { if (isDelete) { if (RemoveLocalAppOrderById(rowId)) changed = true; } else { var mapped = MapSupabasePosOrderRow(source); if (mapped != null) { var existingLocal = GetDeliveryOrderRecordById(mapped.Id); if (existingLocal == null) AddDeliveryOrderToMemorySafely(mapped); else UpdateDeliveryOrderInMemorySafely(mapped); } changed = true; } }
            else if (string.Equals(orderType, "POS_DELIVERY", StringComparison.Ordinal)) { if (isDelete) { for (int i = _deliveryAppDeliveryOrders.Count - 1; i >= 0; i--) { var existing = _deliveryAppDeliveryOrders[i]; if (existing != null && string.Equals(existing.Id, rowId, StringComparison.OrdinalIgnoreCase)) { _deliveryAppDeliveryOrders.RemoveAt(i); changed = true; } } } else { var mapped = MapSupabaseDeliveryOrderRow(source); if (mapped != null) { var replaced = false; for (int i = 0; i < _deliveryAppDeliveryOrders.Count; i++) { var existing = _deliveryAppDeliveryOrders[i]; if (existing == null || !string.Equals(existing.Id, mapped.Id, StringComparison.OrdinalIgnoreCase)) continue; if (!AreAppDeliveryOrdersEquivalentForRealtime(existing, mapped)) { _deliveryAppDeliveryOrders[i] = mapped; changed = true; } replaced = true; break; } if (!replaced) { _deliveryAppDeliveryOrders.Add(mapped); changed = true; } _deliveryAppDeliveryOrderSequence = Math.Max(_deliveryAppDeliveryOrderSequence, mapped.No); var status = (source.Status ?? string.Empty).Trim().ToUpperInvariant(); if (seenPrimedAtStart && !_supabaseSeenAppOrderIds.Contains(rowId) && (string.Equals(status, "RECEIVED", StringComparison.Ordinal) || string.Equals(status, "PENDING", StringComparison.Ordinal))) { isNewPending = true; newOrderRef = mapped.No.ToString(); } } } }
            else if (string.Equals(orderType, "POS_PICKUP", StringComparison.Ordinal)) { if (isDelete) { for (int i = _deliveryAppPickupOrders.Count - 1; i >= 0; i--) { var existing = _deliveryAppPickupOrders[i]; if (existing != null && string.Equals(existing.Id, rowId, StringComparison.OrdinalIgnoreCase)) { _deliveryAppPickupOrders.RemoveAt(i); changed = true; } } } else { var mapped = MapSupabasePickupOrderRow(source); if (mapped != null) { var replaced = false; for (int i = 0; i < _deliveryAppPickupOrders.Count; i++) { var existing = _deliveryAppPickupOrders[i]; if (existing == null || !string.Equals(existing.Id, mapped.Id, StringComparison.OrdinalIgnoreCase)) continue; if (!AreAppPickupOrdersEquivalentForRealtime(existing, mapped)) { _deliveryAppPickupOrders[i] = mapped; changed = true; } replaced = true; break; } if (!replaced) { _deliveryAppPickupOrders.Add(mapped); changed = true; } _deliveryAppPickupOrderSequence = Math.Max(_deliveryAppPickupOrderSequence, mapped.No); var status = (source.Status ?? string.Empty).Trim().ToUpperInvariant(); if (seenPrimedAtStart && !_supabaseSeenAppOrderIds.Contains(rowId) && (string.Equals(status, "RECEIVED", StringComparison.Ordinal) || string.Equals(status, "PENDING", StringComparison.Ordinal))) { isNewPending = true; newOrderRef = mapped.No.ToString(); } } } }
            else if (string.Equals(orderType, "DELIVERY", StringComparison.Ordinal)) { if (isDelete) { for (int i = _deliveryAppDeliveryOrders.Count - 1; i >= 0; i--) { var existing = _deliveryAppDeliveryOrders[i]; if (existing != null && string.Equals(existing.Id, rowId, StringComparison.OrdinalIgnoreCase)) { _deliveryAppDeliveryOrders.RemoveAt(i); changed = true; } } } else { var mapped = MapSupabaseDeliveryOrderRow(source); if (mapped != null) { var replaced = false; for (int i = 0; i < _deliveryAppDeliveryOrders.Count; i++) { var existing = _deliveryAppDeliveryOrders[i]; if (existing == null || !string.Equals(existing.Id, mapped.Id, StringComparison.OrdinalIgnoreCase)) continue; if (!AreAppDeliveryOrdersEquivalentForRealtime(existing, mapped)) { _deliveryAppDeliveryOrders[i] = mapped; changed = true; } replaced = true; break; } if (!replaced) { _deliveryAppDeliveryOrders.Add(mapped); changed = true; } _deliveryAppDeliveryOrderSequence = Math.Max(_deliveryAppDeliveryOrderSequence, mapped.No); var status = (source.Status ?? string.Empty).Trim().ToUpperInvariant(); if (seenPrimedAtStart && !_supabaseSeenAppOrderIds.Contains(rowId) && (string.Equals(status, "RECEIVED", StringComparison.Ordinal) || string.Equals(status, "PENDING", StringComparison.Ordinal))) { isNewPending = true; newOrderRef = mapped.No.ToString(); } } } }
            else if (string.Equals(orderType, "PICKUP", StringComparison.Ordinal)) { if (isDelete) { for (int i = _deliveryAppPickupOrders.Count - 1; i >= 0; i--) { var existing = _deliveryAppPickupOrders[i]; if (existing != null && string.Equals(existing.Id, rowId, StringComparison.OrdinalIgnoreCase)) { _deliveryAppPickupOrders.RemoveAt(i); changed = true; } } } else { var mapped = MapSupabasePickupOrderRow(source); if (mapped != null) { var replaced = false; for (int i = 0; i < _deliveryAppPickupOrders.Count; i++) { var existing = _deliveryAppPickupOrders[i]; if (existing == null || !string.Equals(existing.Id, mapped.Id, StringComparison.OrdinalIgnoreCase)) continue; if (!AreAppPickupOrdersEquivalentForRealtime(existing, mapped)) { _deliveryAppPickupOrders[i] = mapped; changed = true; } replaced = true; break; } if (!replaced) { _deliveryAppPickupOrders.Add(mapped); changed = true; } _deliveryAppPickupOrderSequence = Math.Max(_deliveryAppPickupOrderSequence, mapped.No); var status = (source.Status ?? string.Empty).Trim().ToUpperInvariant(); if (seenPrimedAtStart && !_supabaseSeenAppOrderIds.Contains(rowId) && (string.Equals(status, "RECEIVED", StringComparison.Ordinal) || string.Equals(status, "PENDING", StringComparison.Ordinal))) { isNewPending = true; newOrderRef = mapped.No.ToString(); } } } }
            if (!changed) return;
            if (isDelete) _supabaseSeenAppOrderIds.Remove(rowId); else _supabaseSeenAppOrderIds.Add(rowId);
            _deliveryAppOrdersInitialized = true; _supabaseAppOrdersSeenPrimed = true;
            var nextSnapshotKey = BuildAppOrdersUiSnapshotKey(_deliveryAppDeliveryOrders, _deliveryAppPickupOrders);
            var uiChanged = !string.Equals(_supabaseAppOrdersUiSnapshotKey, nextSnapshotKey, StringComparison.Ordinal);
            _supabaseAppOrdersUiSnapshotKey = nextSnapshotKey; PersistDeliveryState();
            if (_deliveryAppDeliverySelectedIds != null) _deliveryAppDeliverySelectedIds.RemoveWhere(delegate (string id) { return !ContainsAppDeliveryOrderId(id); });
            if (_deliveryAppPickupSelectedIds != null) _deliveryAppPickupSelectedIds.RemoveWhere(delegate (string id) { return !ContainsAppPickupOrderId(id); });
            _deliveryAppDeliveryOrders.Sort(delegate (AppDeliveryOrderRecord a, AppDeliveryOrderRecord b) { var aCreated = a == null ? 0L : a.CreatedAt; var bCreated = b == null ? 0L : b.CreatedAt; return aCreated.CompareTo(bCreated); });
            _deliveryAppPickupOrders.Sort(delegate (AppPickupOrderRecord a, AppPickupOrderRecord b) { var aCreated = a == null ? 0L : a.CreatedAt; var bCreated = b == null ? 0L : b.CreatedAt; return aCreated.CompareTo(bCreated); });
            if (uiChanged) QueueSupabaseAppOrdersUiRefresh();
            if (isNewPending && !string.IsNullOrWhiteSpace(newOrderRef)) { if (string.Equals(orderType, "DELIVERY", StringComparison.Ordinal)) ShowSupabaseToast("طلب تطبيق جديد", "دليفري: " + newOrderRef, true); else if (string.Equals(orderType, "PICKUP", StringComparison.Ordinal)) ShowSupabaseToast("طلب تطبيق جديد", "استلام: " + newOrderRef, true); }
        }

        private bool ContainsAppDeliveryOrderId(string id) { if (string.IsNullOrWhiteSpace(id)) return false; for (int i = 0; i < _deliveryAppDeliveryOrders.Count; i++) { var o = _deliveryAppDeliveryOrders[i]; if (o != null && string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase)) return true; } return false; }
        private bool ContainsAppPickupOrderId(string id) { if (string.IsNullOrWhiteSpace(id)) return false; for (int i = 0; i < _deliveryAppPickupOrders.Count; i++) { var o = _deliveryAppPickupOrders[i]; if (o != null && string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase)) return true; } return false; }

        // ──────────────────────────────────────────────────────────────────────
        // ADDRESS BUILDERS
        // ──────────────────────────────────────────────────────────────────────
        private DeliveryAddressRecord BuildAppOrderAddressFromMetadata(string district, string addressText, SupabaseAppOrderMetadataRow metadata)
        {
            var merged = ParseAppDeliveryAddressText(district, addressText);
            if (merged == null) merged = new DeliveryAddressRecord();
            if (string.IsNullOrWhiteSpace(merged.District)) merged.District = (district ?? string.Empty).Trim();
            if (metadata == null) return merged;
            if (!string.IsNullOrWhiteSpace(metadata.AddressTextFull)) { var parsedFromFullText = ParseAppDeliveryAddressText(district, metadata.AddressTextFull); if (parsedFromFullText != null) { if (string.IsNullOrWhiteSpace(merged.Block) && !string.IsNullOrWhiteSpace(parsedFromFullText.Block)) merged.Block = parsedFromFullText.Block; if (string.IsNullOrWhiteSpace(merged.Street) && !string.IsNullOrWhiteSpace(parsedFromFullText.Street)) merged.Street = parsedFromFullText.Street; if (string.IsNullOrWhiteSpace(merged.Building) && !string.IsNullOrWhiteSpace(parsedFromFullText.Building)) merged.Building = parsedFromFullText.Building; if (string.IsNullOrWhiteSpace(merged.Apartment) && !string.IsNullOrWhiteSpace(parsedFromFullText.Apartment)) merged.Apartment = parsedFromFullText.Apartment; if (string.IsNullOrWhiteSpace(merged.Floor) && !string.IsNullOrWhiteSpace(parsedFromFullText.Floor)) merged.Floor = parsedFromFullText.Floor; if (string.IsNullOrWhiteSpace(merged.Note) && !string.IsNullOrWhiteSpace(parsedFromFullText.Note)) merged.Note = parsedFromFullText.Note; } }
            if (!string.IsNullOrWhiteSpace(metadata.AddressBlock)) merged.Block = metadata.AddressBlock.Trim();
            if (!string.IsNullOrWhiteSpace(metadata.AddressStreet)) merged.Street = metadata.AddressStreet.Trim();
            if (!string.IsNullOrWhiteSpace(metadata.AddressBuilding)) merged.Building = metadata.AddressBuilding.Trim();
            if (!string.IsNullOrWhiteSpace(metadata.AddressApartment)) merged.Apartment = metadata.AddressApartment.Trim();
            if (!string.IsNullOrWhiteSpace(metadata.AddressNote)) merged.Note = metadata.AddressNote.Trim();
            if (string.IsNullOrWhiteSpace(merged.District) && !string.IsNullOrWhiteSpace(metadata.DistrictName)) merged.District = metadata.DistrictName.Trim();
            return merged;
        }

        private string BuildPreferredAppOrderAddressText(string addressText, DeliveryAddressRecord address, SupabaseAppOrderMetadataRow metadata)
        {
            if (metadata != null && !string.IsNullOrWhiteSpace(metadata.AddressTextFull)) return metadata.AddressTextFull.Trim();
            var rebuilt = BuildAppDeliveryAddressText(address);
            if (!string.IsNullOrWhiteSpace(rebuilt)) return rebuilt;
            return addressText ?? string.Empty;
        }

        private DeliveryOrderRecord GetDeliveryOrderRecordById(string id) { if (string.IsNullOrWhiteSpace(id)) return null; for (int i = 0; i < _deliveryOrders.Count; i++) { if (string.Equals(_deliveryOrders[i]?.Id, id, StringComparison.OrdinalIgnoreCase)) return _deliveryOrders[i]; } for (int i = 0; i < _deliveryShiftOrders.Count; i++) { if (string.Equals(_deliveryShiftOrders[i]?.Id, id, StringComparison.OrdinalIgnoreCase)) return _deliveryShiftOrders[i]; } return null; }
        private void AddDeliveryOrderToMemorySafely(DeliveryOrderRecord mapped) { if (mapped == null) return; _deliveryOrders.Add(mapped); }
        private void UpdateDeliveryOrderInMemorySafely(DeliveryOrderRecord mapped) { if (mapped == null) return; for (int i = 0; i < _deliveryOrders.Count; i++) { if (string.Equals(_deliveryOrders[i]?.Id, mapped.Id, StringComparison.OrdinalIgnoreCase)) { _deliveryOrders[i] = mapped; return; } } for (int i = 0; i < _deliveryShiftOrders.Count; i++) { if (string.Equals(_deliveryShiftOrders[i]?.Id, mapped.Id, StringComparison.OrdinalIgnoreCase)) { _deliveryShiftOrders[i] = mapped; return; } } }

        private DeliveryOrderRecord MapSupabasePosOrderRow(SupabaseAppOrderRow row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.Id)) return null;
            var items = new List<DeliveryLineRecord>();
            if (row.Metadata?.Items != null) { foreach (var i in row.Metadata.Items) items.Add(new DeliveryLineRecord { Key = Guid.NewGuid().ToString("N"), Desc = i.Name, Qty = i.Qty, Price = (int)i.Price, Notes = "" }); }
            else if (row.Items != null) { foreach (var i in row.Items) items.Add(new DeliveryLineRecord { Key = Guid.NewGuid().ToString("N"), Desc = i.Name, Qty = i.Qty, Price = (int)i.Price, Notes = "" }); }
            return new DeliveryOrderRecord { Id = row.Id, IdempotencyKey = string.Empty, No = row.Metadata?.LocalOrderNo ?? 0, CreatedAt = row.Metadata?.FinalizedAtMillis ?? 0, CustomerPhone = row.CustomerPhone ?? string.Empty, CustomerName = row.CustomerName ?? string.Empty, District = row.District ?? string.Empty, DeliveryName = "", DeliveryFee = (int)row.DeliveryFee, Subtotal = (int)row.Subtotal, Total = (int)row.Total, KitchenPrinted = row.KitchenPrinted, KitchenPrintedAt = null, Status = row.Status ?? "DONE", Driver = null, ShiftTimeText = "", CashierUserId = row.Metadata?.CashierUserId ?? string.Empty, CashierUsername = row.Metadata?.CashierUsername ?? string.Empty, CashierRole = row.Metadata?.CashierRole ?? string.Empty, Items = items };
        }

        private AppDeliveryOrderRecord MapSupabaseDeliveryOrderRow(SupabaseAppOrderRow row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.Id)) return null;
            var statusRaw = (row.Status ?? string.Empty).Trim().ToUpperInvariant();
            if (statusRaw == "MIGRATED") return null;
            AppDeliveryOrderStatus status;
            if (statusRaw == "RECEIVED" || statusRaw == "PENDING") status = AppDeliveryOrderStatus.Pending;
            else if (statusRaw == "PREPARING" || statusRaw == "NO_DRIVER") status = AppDeliveryOrderStatus.Preparing;
            else if (statusRaw == "DRIVER_ASSIGNED" || statusRaw == "DRIVER_PICKED" || statusRaw == "ON_ROAD") status = AppDeliveryOrderStatus.DriverAssigned;
            else if (statusRaw == "DONE" || statusRaw == "DELIVERED") status = AppDeliveryOrderStatus.Done;
            else status = AppDeliveryOrderStatus.Pending;
            var metaDriverName = row.Metadata == null ? string.Empty : (!string.IsNullOrWhiteSpace(row.Metadata.DriverName) ? row.Metadata.DriverName : row.Metadata.DriverNameCamel);
            var metaDriverPhone = row.Metadata == null ? string.Empty : (!string.IsNullOrWhiteSpace(row.Metadata.DriverPhone) ? row.Metadata.DriverPhone : row.Metadata.DriverPhoneCamel);
            var driverName = !string.IsNullOrWhiteSpace(row.DriverName) ? row.DriverName : metaDriverName;
            var driverPhone = !string.IsNullOrWhiteSpace(row.DriverPhone) ? row.DriverPhone : metaDriverPhone;
            var driverId = !string.IsNullOrWhiteSpace(row.DriverUserId) ? row.DriverUserId : (!string.IsNullOrWhiteSpace(driverPhone) ? ("phone:" + driverPhone.Trim()) : "APP_DRIVER");
            var subtotal = (int)Math.Round(row.Subtotal < 0 ? 0 : row.Subtotal, MidpointRounding.AwayFromZero);
            var deliveryFee = (int)Math.Round(row.DeliveryFee < 0 ? 0 : row.DeliveryFee, MidpointRounding.AwayFromZero);
            var totalRaw = row.Total > 0 ? row.Total : (row.Subtotal + row.DeliveryFee);
            var total = (int)Math.Round(totalRaw < 0 ? 0 : totalRaw, MidpointRounding.AwayFromZero);
            var driver = string.IsNullOrWhiteSpace(row.DriverUserId) && string.IsNullOrWhiteSpace(driverName) && string.IsNullOrWhiteSpace(driverPhone) ? null : new DeliveryDriverRecord { Id = driverId, Name = ResolveFriendlyDriverName(driverId, driverName, driverPhone), Phone = driverPhone ?? string.Empty, AppUserId = row.DriverUserId };
            var address = BuildAppOrderAddressFromMetadata(row.District, row.AddressText, row.Metadata);
            var preferredAddressText = BuildPreferredAppOrderAddressText(row.AddressText, address, row.Metadata);
            return new AppDeliveryOrderRecord { Id = row.Id, CustomerUserId = row.CustomerUserId, AddressId = row.Metadata == null ? string.Empty : (row.Metadata.AddressId ?? string.Empty), No = DeriveSupabaseOrderNo(row), Name = row.CustomerName ?? string.Empty, Phone = row.CustomerPhone ?? string.Empty, District = string.IsNullOrWhiteSpace(address.District) ? (row.District ?? string.Empty) : address.District, AddressText = preferredAddressText, Address = address, CreatedAt = ParseIsoDateToUnixMs(row.CreatedAt), Subtotal = subtotal, DeliveryFee = deliveryFee, Total = total, Status = status, KitchenPrinted = row.KitchenPrinted, DriverReceiptPrinted = row.DriverReceiptPrinted, Driver = driver, Items = MapSupabaseItems(row.Items, row.Metadata == null ? null : row.Metadata.Items) };
        }

        private AppPickupOrderRecord MapSupabasePickupOrderRow(SupabaseAppOrderRow row) { if (row == null || string.IsNullOrWhiteSpace(row.Id)) return null; var statusRaw = (row.Status ?? string.Empty).Trim().ToUpperInvariant(); if (statusRaw == "MIGRATED") return null; AppPickupOrderStatus status; if (statusRaw == "RECEIVED" || statusRaw == "PENDING") status = AppPickupOrderStatus.Pending; else if (statusRaw == "ACCEPTED") status = AppPickupOrderStatus.Accepted; else if (statusRaw == "PREPARING") status = AppPickupOrderStatus.Preparing; else if (statusRaw == "READY") status = AppPickupOrderStatus.Ready; else if (statusRaw == "DONE" || statusRaw == "DELIVERED") status = AppPickupOrderStatus.Delivered; else status = AppPickupOrderStatus.Pending; return new AppPickupOrderRecord { Id = row.Id, CustomerUserId = row.CustomerUserId, No = DeriveSupabaseOrderNo(row), Name = row.CustomerName ?? string.Empty, Phone = row.CustomerPhone ?? string.Empty, District = row.District ?? string.Empty, AddressText = row.AddressText ?? string.Empty, CreatedAt = ParseIsoDateToUnixMs(row.CreatedAt), Status = status, PartnerReceiptPrinted = row.PartnerReceiptPrinted, Items = MapSupabaseItems(row.Items, row.Metadata == null ? null : row.Metadata.Items) }; }

        private List<AppOrderItemRecord> MapSupabaseItems(List<SupabaseAppOrderItemRow> items, List<SupabaseAppOrderItemRow> metadataItems)
        {
            var list = new List<AppOrderItemRecord>();
            if ((items == null || items.Count == 0) && metadataItems != null && metadataItems.Count > 0) items = metadataItems;
            if (items == null) return list;
            for (int i = 0; i < items.Count; i++) { var it = items[i]; if (it == null) continue; list.Add(new AppOrderItemRecord { Name = it.Name ?? string.Empty, Qty = it.Qty <= 0 ? 1 : it.Qty, Price = (int)Math.Round(it.Price, MidpointRounding.AwayFromZero) }); }
            return list;
        }

        private long ParseIsoDateToUnixMs(string raw) { if (string.IsNullOrWhiteSpace(raw)) return NowUnixMs(); DateTimeOffset dto; if (DateTimeOffset.TryParse(raw, out dto)) return dto.ToUnixTimeMilliseconds(); return NowUnixMs(); }
        private int SafeIntFromLong(long value) { if (value > int.MaxValue) return int.MaxValue; if (value < int.MinValue) return int.MinValue; return (int)value; }

        private int DeriveSupabaseOrderNo(SupabaseAppOrderRow row)
        {
            if (row != null && row.OrderNo > 0) return SafeIntFromLong(row.OrderNo);
            var localOrderId = row != null && row.Metadata != null ? row.Metadata.LocalOrderId : null;
            var fromLocal = ExtractLastDigits(localOrderId, 5);
            if (fromLocal > 0) return fromLocal;
            var id = row == null ? string.Empty : row.Id;
            var fromId = ExtractLastDigits(id, 5);
            if (fromId > 0) return fromId;
            var hash = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().GetHashCode() : id.GetHashCode();
            return Math.Abs(hash % 90000) + 10000;
        }

        private int ExtractLastDigits(string value, int maxDigits)
        {
            if (string.IsNullOrWhiteSpace(value) || maxDigits <= 0) return 0;
            var digits = new StringBuilder();
            for (int i = 0; i < value.Length; i++) { var ch = value[i]; if (char.IsDigit(ch)) digits.Append(ch); }
            if (digits.Length == 0) return 0;
            var sliceLen = Math.Min(maxDigits, digits.Length);
            var slice = digits.ToString(digits.Length - sliceLen, sliceLen);
            int parsed;
            return int.TryParse(slice, out parsed) ? parsed : 0;
        }
    }
}

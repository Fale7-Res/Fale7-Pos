#pragma warning disable 0649
// =============================================================================
// PosRealtimeSync.cs
// مسار الملف: POS/PosRealtimeSync.cs
// يعمل مع الـ WebSocket الحالي في المشروع (System.Net.WebSockets)
// لا يستخدم Supabase C# SDK
// =============================================================================
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        // =====================================================================
        // الحالة - مشتركة مع MainWindow.Supabase.cs
        // =====================================================================
        private string _currentEditingOrderId;

        // =====================================================================
        // 1. تهيئة Realtime
        //    تستدعي الآلية الموجودة أصلاً في MainWindow.Supabase.cs
        // =====================================================================
        private Task InitRealtimeAsync()
        {
            var cfg = LoadSupabaseClientConfigResolved();
            if (cfg != null && cfg.RealtimeEnabled == true)
            {
                StartSupabaseRealtimeSubscriptions(cfg);
            }
            return Task.CompletedTask;
        }

        // =====================================================================
        // 2. قطع الاتصال
        // =====================================================================
        private Task DisconnectRealtimeAsync()
        {
            StopSupabaseRealtimeSubscriptions();
            return Task.CompletedTask;
        }

        /// <summary>يُطلق جميع أقفال هذا الجهاز (عند تسجيل الخروج)</summary>
        private async Task ReleaseAllLocksForThisDeviceAsync()
        {
            var cfg = LoadSupabaseClientConfigResolved();
            if (cfg == null) return;

            try
            {
                await PostSupabaseRpcGetStringAsync(
                    cfg,
                    "api_pos_release_all_locks_for_device",
                    SerializeJson(new { p_device_id = GetDeviceId() }),
                    actorRoleOverride: GetCurrentBackendActorRole()
                ).ConfigureAwait(false);
            }
            catch
            {
            }
        }

        // =====================================================================
        // 3. عند عودة الإنترنت
        // =====================================================================
        private async Task OnInternetRestoredAsync()
        {
            AppendPosRuntimeLog("CONNECTIVITY", "Internet restored - syncing...");

            // 1. مزامنة Queue الـ Idempotency المعلقة
            await ProcessIdempotencyQueueAsync().ConfigureAwait(false);

            // 2. تحديث الوردية من السيرفر
            await RefreshActiveShiftFromServerAsync().ConfigureAwait(false);

            // 3. تحرير أقفال هذا الجهاز
            await ReleaseAllLocksForThisDeviceAsync().ConfigureAwait(false);

            // 4. إعادة تشغيل Realtime إذا توقف
            await InitRealtimeAsync().ConfigureAwait(false);

            // 5. تحديث الأوردرات
            Dispatcher.Invoke(QueueSupabaseAppOrdersPullFromBackend);
        }

        // =====================================================================
        // 4. أحداث قفل الأوردر (تُستدعى من HandleSupabaseRealtimeMessageAsync)
        // =====================================================================
        private void NotifyOrderLockedByOtherDevice(string orderId, string lockType)
        {
            if (string.Equals(_currentEditingOrderId, orderId, StringComparison.OrdinalIgnoreCase))
            {
                Dispatcher.Invoke(delegate
                {
                    MessageBox.Show(
                        "جاري تعديل بيانات التوصيل من جهاز آخر.\nتم إغلاق شاشة التعديل.",
                        "الأوردر مقفل",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                });
            }
        }

        private void NotifyOrderLockReleased(string orderId)
        {
            Dispatcher.Invoke(QueueSupabaseAppOrdersUiRefresh);
        }

        // =====================================================================
        // 5. Regular methods (كانت partial - أُزيلت التصريحات المكررة)
        // =====================================================================
        private void RemoveOrderFromAllLocalLists_Realtime(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId)) return;
            for (int i = _deliveryOrders.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_deliveryOrders[i]?.Id, orderId, StringComparison.OrdinalIgnoreCase))
                    _deliveryOrders.RemoveAt(i);
            }
            for (int i = _deliveryShiftOrders.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_deliveryShiftOrders[i]?.Id, orderId, StringComparison.OrdinalIgnoreCase))
                    _deliveryShiftOrders.RemoveAt(i);
            }
            QueueSupabaseAppOrdersUiRefresh();
        }
    }
}

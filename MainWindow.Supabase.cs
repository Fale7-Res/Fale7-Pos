using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        private const string SupabaseEnvUrl = "FALE7_SUPABASE_URL";
        private const string SupabaseEnvAnonKey = "FALE7_SUPABASE_ANON_KEY";
        private const string SupabaseDefaultUrl = "";
        private const string SupabaseDefaultAnonKey = "";
        private const int SupabasePosMenuRealtimeDebounceMs = 900;
        private const int SupabaseDefaultRestTimeoutMs = 15000;
        private const int SupabaseSessionIssueTimeoutMs = 30000;

        private static readonly Regex SupabaseRealtimeEventRegex = new Regex("\"event\"\\s*:\\s*\"(?<event>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SupabaseRealtimeTableRegex = new Regex("\"table\"\\s*:\\s*\"(?<table>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SupabaseRealtimeEventTypeRegex = new Regex("\"eventType\"\\s*:\\s*\"(?<eventType>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SupabaseRealtimePayloadTypeRegex = new Regex("\"type\"\\s*:\\s*\"(?<type>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly HttpClient SupabaseHttpClient = new HttpClient();
        private static readonly Encoding Windows1252Encoding = Encoding.GetEncoding(1252);

        private readonly object _supabaseRealtimeSync = new object();
        private readonly object _supabaseNotificationSync = new object();
        private readonly object _supabaseSessionRefreshSync = new object();
        private readonly object _supabasePosMenuSync = new object();
        private readonly object _supabaseBackendExportSync = new object();
        private bool _supabaseBootstrapStarted;
        private bool _supabaseNotificationsPullInFlight;
        private bool _supabaseNotificationsPullPending;
        private bool _supabasePosMenuPushInFlight;
        private bool _supabasePosMenuPushPending;
        private bool _supabasePosMenuPullInFlight;
        private bool _supabasePosMenuPullPending;
        private bool _supabaseDistrictsPullInFlight;
        private bool _supabaseDistrictsPullPending;
        private bool _supabaseDriversPullInFlight;
        private bool _supabaseDriversPullPending;
        private bool _supabaseDriversPullShowErrors;
        private bool _supabaseBackendExportInFlight;
        private DateTime _supabasePosMenuLocalWriteHoldUntilUtc = DateTime.MinValue;
        private DateTime _supabasePosMenuPullNotBeforeUtc = DateTime.MinValue;
        private DateTime _supabaseLastBackendExportAtUtc = DateTime.MinValue;
        private CancellationTokenSource _supabaseRealtimeCts;
        private Task _supabaseRealtimeTask;
        private readonly HashSet<string> _supabaseSeenNotificationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private int _supabaseUnreadNotificationsCount;
        private bool _supabaseNotificationsSeenPrimed;
        private bool _supabaseSessionRefreshInFlight;
        private DateTime _supabaseLastSessionRefreshAttemptUtc = DateTime.MinValue;
        private string _supabaseLastErrorSignature = string.Empty;
        private DateTime _supabaseLastErrorAtUtc = DateTime.MinValue;

        private Popup _supabaseToastPopup;
        private Border _supabaseToastBorder;
        private TextBlock _supabaseToastTitleText;
        private TextBlock _supabaseToastMessageText;
        private Button _supabaseToastCloseButton;
        private bool _supabaseToastNavigateToAppOrders;

        private string SupabaseClientConfigFilePath { get { return Path.Combine(AppDataDirPath, "supabase_client.json"); } }

        [DataContract]
        private sealed class SupabaseClientConfigRecord
        {
            [DataMember(Name = "url", EmitDefaultValue = false)] public string Url { get; set; }
            [DataMember(Name = "anon_key", EmitDefaultValue = false)] public string AnonKey { get; set; }
            [DataMember(Name = "schema", EmitDefaultValue = false)] public string Schema { get; set; }
            [DataMember(Name = "orders_table", EmitDefaultValue = false)] public string OrdersTable { get; set; }
            [DataMember(Name = "notifications_table", EmitDefaultValue = false)] public string NotificationsTable { get; set; }
            [DataMember(Name = "realtime_enabled", EmitDefaultValue = false)] public bool? RealtimeEnabled { get; set; }
            [DataMember(Name = "rest_timeout_ms", EmitDefaultValue = false)] public int RestTimeoutMs { get; set; }
        }

        [DataContract]
        private sealed class SupabaseNotificationInsertRecord
        {
            [DataMember(Name = "user_id", EmitDefaultValue = false)] public string UserId { get; set; }
            [DataMember(Name = "role_target")] public string RoleTarget { get; set; }
            [DataMember(Name = "order_id", EmitDefaultValue = false)] public string OrderId { get; set; }
            [DataMember(Name = "order_type", EmitDefaultValue = false)] public string OrderType { get; set; }
            [DataMember(Name = "title")] public string Title { get; set; }
            [DataMember(Name = "message")] public string Message { get; set; }
            [DataMember(Name = "read")] public bool Read { get; set; }
        }

        [DataContract]
        private sealed class SupabaseEmitOrderNotificationRequest
        {
            [DataMember(Name = "p_user_id")] public string UserId { get; set; }
            [DataMember(Name = "p_role_target")] public string RoleTarget { get; set; }
            [DataMember(Name = "p_order_id")] public string OrderId { get; set; }
            [DataMember(Name = "p_order_type")] public string OrderType { get; set; }
            [DataMember(Name = "p_title")] public string Title { get; set; }
            [DataMember(Name = "p_message")] public string Message { get; set; }
            [DataMember(Name = "p_status")] public string Status { get; set; }
        }

        [DataContract]
        private sealed class SupabaseNotificationRow
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "user_id", EmitDefaultValue = false)] public string UserId { get; set; }
            [DataMember(Name = "role_target", EmitDefaultValue = false)] public string RoleTarget { get; set; }
            [DataMember(Name = "order_id", EmitDefaultValue = false)] public string OrderId { get; set; }
            [DataMember(Name = "order_type", EmitDefaultValue = false)] public string OrderType { get; set; }
            [DataMember(Name = "title", EmitDefaultValue = false)] public string Title { get; set; }
            [DataMember(Name = "message", EmitDefaultValue = false)] public string Message { get; set; }
            [DataMember(Name = "read", EmitDefaultValue = false)] public bool Read { get; set; }
            [DataMember(Name = "created_at", EmitDefaultValue = false)] public string CreatedAt { get; set; }
        }

        [DataContract]
        private sealed class SupabaseDriverRow
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "username", EmitDefaultValue = false)] public string Username { get; set; }
            [DataMember(Name = "phone", EmitDefaultValue = false)] public string Phone { get; set; }
            [DataMember(Name = "display_name", EmitDefaultValue = false)] public string DisplayName { get; set; }
            [DataMember(Name = "displayName", EmitDefaultValue = false)] public string DisplayNameCamel { get; set; }
        }

        [DataContract]
        private sealed class SupabaseRealtimeChangeData
        {
            [DataMember(Name = "schema", EmitDefaultValue = false)] public string Schema { get; set; }
            [DataMember(Name = "table", EmitDefaultValue = false)] public string Table { get; set; }
            [DataMember(Name = "eventType", EmitDefaultValue = false)] public string EventType { get; set; }
        }

        [DataContract]
        private sealed class SupabaseRealtimePayload
        {
            [DataMember(Name = "schema", EmitDefaultValue = false)] public string Schema { get; set; }
            [DataMember(Name = "table", EmitDefaultValue = false)] public string Table { get; set; }
            [DataMember(Name = "type", EmitDefaultValue = false)] public string Type { get; set; }
            [DataMember(Name = "eventType", EmitDefaultValue = false)] public string EventType { get; set; }
            [DataMember(Name = "data", EmitDefaultValue = false)] public SupabaseRealtimeChangeData Data { get; set; }
        }

        [DataContract]
        private sealed class SupabaseRealtimeMessage
        {
            [DataMember(Name = "event", EmitDefaultValue = false)] public string Event { get; set; }
            [DataMember(Name = "topic", EmitDefaultValue = false)] public string Topic { get; set; }
            [DataMember(Name = "payload", EmitDefaultValue = false)] public SupabaseRealtimePayload Payload { get; set; }
        }

        [DataContract]
        private sealed class SupabaseUsersSearchRequest
        {
            [DataMember(Name = "p_identifier", EmitDefaultValue = false)] public string Identifier { get; set; }
            [DataMember(Name = "p_id", EmitDefaultValue = false)] public string Id { get; set; }
            [DataMember(Name = "p_role", EmitDefaultValue = false)] public string Role { get; set; }
            [DataMember(Name = "p_limit", EmitDefaultValue = false)] public int Limit { get; set; }
        }

        [DataContract]
        private sealed class SupabaseCashierCredentialsSnapshotRequest
        {
            [DataMember(Name = "p_include_inactive", EmitDefaultValue = false)] public bool IncludeInactive { get; set; }
            [DataMember(Name = "p_limit", EmitDefaultValue = false)] public int Limit { get; set; }
        }

        [DataContract]
        private sealed class SupabaseCashierCredentialRow
        {
            [DataMember(Name = "id", EmitDefaultValue = false)] public string Id { get; set; }
            [DataMember(Name = "username", EmitDefaultValue = false)] public string Username { get; set; }
            [DataMember(Name = "display_name", EmitDefaultValue = false)] public string DisplayName { get; set; }
            [DataMember(Name = "displayName", EmitDefaultValue = false)] public string DisplayNameCamel { get; set; }
            [DataMember(Name = "phone", EmitDefaultValue = false)] public string Phone { get; set; }
            [DataMember(Name = "email", EmitDefaultValue = false)] public string Email { get; set; }
            [DataMember(Name = "role", EmitDefaultValue = false)] public string Role { get; set; }
            [DataMember(Name = "isActive", EmitDefaultValue = false)] public bool IsActive { get; set; }
            [DataMember(Name = "is_active", EmitDefaultValue = false)] public bool IsActiveSnake { get; set; }
            [DataMember(Name = "updatedAt", EmitDefaultValue = false)] public string UpdatedAt { get; set; }
            [DataMember(Name = "updated_at", EmitDefaultValue = false)] public string UpdatedAtSnake { get; set; }
            [DataMember(Name = "secretUpdatedAt", EmitDefaultValue = false)] public string SecretUpdatedAt { get; set; }
            [DataMember(Name = "secret_updated_at", EmitDefaultValue = false)] public string SecretUpdatedAtSnake { get; set; }
        }

        [DataContract]
        private sealed class SupabaseUpsertStaffCredentialRequest
        {
            [DataMember(Name = "p_user_id", EmitDefaultValue = false)] public string UserId { get; set; }
            [DataMember(Name = "p_username", EmitDefaultValue = false)] public string Username { get; set; }
            [DataMember(Name = "p_display_name", EmitDefaultValue = false)] public string DisplayName { get; set; }
            [DataMember(Name = "p_phone", EmitDefaultValue = false)] public string Phone { get; set; }
            [DataMember(Name = "p_email", EmitDefaultValue = false)] public string Email { get; set; }
            [DataMember(Name = "p_role", EmitDefaultValue = false)] public string Role { get; set; }
            [DataMember(Name = "p_is_active", EmitDefaultValue = false)] public bool IsActive { get; set; }
            [DataMember(Name = "p_password", EmitDefaultValue = false)] public string Password { get; set; }
            [DataMember(Name = "p_pin", EmitDefaultValue = false)] public string Pin { get; set; }
        }

        [DataContract]
        private sealed class SupabaseUpsertStaffCredentialResponse
        {
            [DataMember(Name = "ok", EmitDefaultValue = false)] public bool Ok { get; set; }
            [DataMember(Name = "error", EmitDefaultValue = false)] public string Error { get; set; }
        }

        [DataContract]
        private sealed class SupabaseIdOnlyRow
        {
            [DataMember(Name = "id", EmitDefaultValue = false)] public string Id { get; set; }
        }

        private sealed class SupabaseTableExportDescriptor
        {
            public string TableName { get; set; }
            public string OrderBy { get; set; }
            public string ConflictPolicy { get; set; }
        }

        [DataContract]
        private sealed class SupabaseBackendExportManifestRecord
        {
            [DataMember(Name = "version", EmitDefaultValue = false)] public int Version { get; set; }
            [DataMember(Name = "generated_at", EmitDefaultValue = false)] public string GeneratedAt { get; set; }
            [DataMember(Name = "actor_role", EmitDefaultValue = false)] public string ActorRole { get; set; }
            [DataMember(Name = "actor_user_id", EmitDefaultValue = false)] public string ActorUserId { get; set; }
            [DataMember(Name = "schema_hint", EmitDefaultValue = false)] public string SchemaHint { get; set; }
            [DataMember(Name = "combined_sql_file", EmitDefaultValue = false)] public string CombinedSqlFile { get; set; }
            [DataMember(Name = "tables", EmitDefaultValue = false)] public List<SupabaseBackendExportManifestTableRecord> Tables { get; set; }
            [DataMember(Name = "skipped", EmitDefaultValue = false)] public List<SupabaseBackendExportSkippedRecord> Skipped { get; set; }
        }

        [DataContract]
        private sealed class SupabaseBackendExportManifestTableRecord
        {
            [DataMember(Name = "table", EmitDefaultValue = false)] public string Table { get; set; }
            [DataMember(Name = "row_count", EmitDefaultValue = false)] public int RowCount { get; set; }
            [DataMember(Name = "json_file", EmitDefaultValue = false)] public string JsonFile { get; set; }
            [DataMember(Name = "sql_file", EmitDefaultValue = false)] public string SqlFile { get; set; }
            [DataMember(Name = "conflict_policy", EmitDefaultValue = false)] public string ConflictPolicy { get; set; }
        }

        [DataContract]
        private sealed class SupabaseBackendExportSkippedRecord
        {
            [DataMember(Name = "table", EmitDefaultValue = false)] public string Table { get; set; }
            [DataMember(Name = "reason", EmitDefaultValue = false)] public string Reason { get; set; }
        }

        [DataContract]
        private sealed class SupabaseSourceProductRow
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "price")] public double Price { get; set; }
        }

        [DataContract]
        private sealed class SupabasePosCategoryRow
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "sort_order")] public int SortOrder { get; set; }
        }

        [DataContract]
        private sealed class SupabasePosProductRow
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "pos_category_id")] public string PosCategoryId { get; set; }
            [DataMember(Name = "source_product_id")] public string SourceProductId { get; set; }
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "pos_display_name")] public string PosDisplayName { get; set; }
            [DataMember(Name = "sort_order")] public int SortOrder { get; set; }
            [DataMember(Name = "enabled")] public int Enabled { get; set; }
        }

        [DataContract]
        private sealed class SupabasePosRegisteredAddressRow
        {
            [DataMember(Name = "id")] public int Id { get; set; }
            [DataMember(Name = "phone")] public string Phone { get; set; }
            [DataMember(Name = "label", EmitDefaultValue = false)] public string Label { get; set; }
            [DataMember(Name = "address_line", EmitDefaultValue = false)] public string AddressLine { get; set; }
            [DataMember(Name = "registered_at", EmitDefaultValue = false)] public string RegisteredAt { get; set; }
        }

        [DataContract]
        private sealed class SupabasePosRegisteredAddressInsertRow
        {
            [DataMember(Name = "phone")] public string Phone { get; set; }
            [DataMember(Name = "label")] public string Label { get; set; }
            [DataMember(Name = "address_line")] public string AddressLine { get; set; }
        }

        [DataContract]
        private sealed class SupabasePosAddressPayload
        {
            [DataMember(Name = "district", EmitDefaultValue = false)] public string District { get; set; }
            [DataMember(Name = "block", EmitDefaultValue = false)] public string Block { get; set; }
            [DataMember(Name = "street", EmitDefaultValue = false)] public string Street { get; set; }
            [DataMember(Name = "building", EmitDefaultValue = false)] public string Building { get; set; }
            [DataMember(Name = "apartment", EmitDefaultValue = false)] public string Apartment { get; set; }
            [DataMember(Name = "floor", EmitDefaultValue = false)] public string Floor { get; set; }
            [DataMember(Name = "note", EmitDefaultValue = false)] public string Note { get; set; }
        }

        private sealed class SupabasePosMenuSyncSnapshot
        {
            public List<SupabasePosCategoryRow> Categories { get; set; }
            public List<SupabasePosProductRow> Products { get; set; }
            public List<SupabaseSourceProductRow> SourceProducts { get; set; }
        }

        private void BeginSupabaseBootstrapIfConfigured()
        {
            EnsureSupabaseBackendExportShell();
            lock (_supabaseRealtimeSync)
            {
                if (_supabaseBootstrapStarted) return;
                _supabaseBootstrapStarted = true;
            }

            Task.Run(async delegate
            {
                var cfg = LoadSupabaseClientConfigResolved();
                if (cfg == null)
                {
                    Debug.WriteLine("Supabase: no client config (env/file). Running local fallback only.");
                    return;
                }

                var probe = await TrySupabaseConnectivityProbeAsync(cfg).ConfigureAwait(false);
                Debug.WriteLine("Supabase: connectivity probe = " + (probe ? "OK" : "FAILED"));

                QueueSupabaseAppOrdersPullFromBackend();
                QueueSupabaseNotificationsPullFromBackend();
                QueueSupabaseDriversPullFromBackend(false);
                QueueSupabasePosMenuPullFromBackend(false);
                QueueSupabaseDistrictsPullFromBackend(false);
                if (probe)
                {
                    QueueSupabaseBackendSnapshotExport();
                }

                if (cfg.RealtimeEnabled == true)
                {
                    StartSupabaseRealtimeSubscriptions(cfg);
                }
            });
        }

        private void EnsureSupabaseBackendExportShell()
        {
            try
            {
                EnsureAppDataDir();

                if (!File.Exists(BackendCombinedSqlExportFilePath))
                {
                    var combined = new StringBuilder();
                    combined.AppendLine("-- Fale7 backend data export");
                    combined.AppendLine("-- Canonical DDL lives in supabase_schema.sql");
                    combined.AppendLine("-- Generated at " + DateTime.UtcNow.ToString("o"));
                    combined.AppendLine("-- Waiting for the first successful POS backend export.");
                    combined.AppendLine();
                    WriteRawTextFile(BackendCombinedSqlExportFilePath, combined.ToString());
                }

                if (!File.Exists(BackendSyncManifestFilePath))
                {
                    var manifest = new SupabaseBackendExportManifestRecord
                    {
                        Version = 1,
                        GeneratedAt = DateTime.UtcNow.ToString("o"),
                        ActorRole = GetCurrentBackendActorRole(),
                        ActorUserId = GetCurrentSessionUserId(),
                        SchemaHint = "supabase_schema.sql",
                        CombinedSqlFile = "exports/" + Path.GetFileName(BackendCombinedSqlExportFilePath),
                        Tables = new List<SupabaseBackendExportManifestTableRecord>(),
                        Skipped = new List<SupabaseBackendExportSkippedRecord>()
                    };
                    WriteJson(BackendSyncManifestFilePath, manifest);
                }
            }
            catch
            {
            }
        }

        private void StartSupabaseRealtimeSubscriptions(SupabaseClientConfigRecord cfg)
        {
            if (cfg == null) return;
            lock (_supabaseRealtimeSync)
            {
                if (_supabaseRealtimeTask != null && !_supabaseRealtimeTask.IsCompleted) return;
                StopSupabaseRealtimeSubscriptions_NoLock();
                lock (_supabaseNotificationSync)
                {
                    _supabaseSeenNotificationIds.Clear();
                    _supabaseUnreadNotificationsCount = 0;
                    _supabaseNotificationsSeenPrimed = false;
                }
                _supabaseRealtimeCts = new CancellationTokenSource();
                var cts = _supabaseRealtimeCts;
                _supabaseRealtimeTask = Task.Run(async delegate
                {
                    try
                    {
                        await SupabaseRealtimeReconnectLoopAsync(cfg, cts.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Supabase realtime stopped: " + ex.Message);
                    }
                }, cts.Token);
            }
        }

        private void StopSupabaseRealtimeSubscriptions()
        {
            lock (_supabaseRealtimeSync)
            {
                StopSupabaseRealtimeSubscriptions_NoLock();
            }
        }

        private void StopSupabaseRealtimeSubscriptions_NoLock()
        {
            try
            {
                if (_supabaseRealtimeCts != null)
                {
                    _supabaseRealtimeCts.Cancel();
                    _supabaseRealtimeCts.Dispose();
                }
            }
            catch { }
            _supabaseRealtimeCts = null;
            _supabaseRealtimeTask = null;
        }

        protected override void OnClosed(EventArgs e)
        {
            StopSupabaseRealtimeSubscriptions();
            StopPosOfflineSensing();
            base.OnClosed(e);
        }

        private SupabaseClientConfigRecord LoadSupabaseClientConfigResolved()
        {
            SupabaseClientConfigRecord fileCfg = null;
            foreach (var path in GetSupabaseClientConfigProbePaths())
            {
                fileCfg = ReadJson<SupabaseClientConfigRecord>(path);
                if (fileCfg != null) break;
            }

            var url = (Environment.GetEnvironmentVariable(SupabaseEnvUrl) ?? string.Empty).Trim();
            var anonKey = (Environment.GetEnvironmentVariable(SupabaseEnvAnonKey) ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(url) && fileCfg != null) url = (fileCfg.Url ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(anonKey) && fileCfg != null) anonKey = (fileCfg.AnonKey ?? string.Empty).Trim();
            if (IsSupabasePlaceholderValue(url)) url = string.Empty;
            if (IsSupabasePlaceholderValue(anonKey)) anonKey = string.Empty;
            if (!IsSupabaseClientUrl(url)) url = string.Empty;
            if (!IsSupabaseSafeClientKey(anonKey)) anonKey = string.Empty;
            if (string.IsNullOrWhiteSpace(url)) url = SupabaseDefaultUrl;
            if (string.IsNullOrWhiteSpace(anonKey)) anonKey = SupabaseDefaultAnonKey;

            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(anonKey)) return null;

            return new SupabaseClientConfigRecord
            {
                Url = url.TrimEnd('/'),
                AnonKey = anonKey,
                Schema = fileCfg != null && !string.IsNullOrWhiteSpace(fileCfg.Schema) ? fileCfg.Schema.Trim() : "public",
                OrdersTable = fileCfg != null && !string.IsNullOrWhiteSpace(fileCfg.OrdersTable) ? fileCfg.OrdersTable.Trim() : "app_orders",
                NotificationsTable = fileCfg != null && !string.IsNullOrWhiteSpace(fileCfg.NotificationsTable) ? fileCfg.NotificationsTable.Trim() : "notifications",
                RealtimeEnabled = fileCfg == null || !fileCfg.RealtimeEnabled.HasValue || fileCfg.RealtimeEnabled.Value,
                RestTimeoutMs = fileCfg != null && fileCfg.RestTimeoutMs > 0 ? fileCfg.RestTimeoutMs : SupabaseDefaultRestTimeoutMs
            };
        }

        private static bool IsSupabasePlaceholderValue(string value)
        {
            var v = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(v)) return true;
            if (v.IndexOf("YOUR_PROJECT", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (v.IndexOf("YOUR_SUPABASE_ANON_KEY", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static bool IsSupabaseClientUrl(string value)
        {
            var v = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(v)) return false;

            Uri uri;
            if (!Uri.TryCreate(v, UriKind.Absolute, out uri) || string.IsNullOrWhiteSpace(uri.Host))
            {
                return false;
            }

            if (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && (
                    string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                );
        }

        private static bool IsSupabaseSafeClientKey(string value)
        {
            var v = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(v)) return false;

            var lowered = v.ToLowerInvariant();
            if (lowered.IndexOf("service_role", StringComparison.Ordinal) >= 0) return false;
            if (lowered.IndexOf("supabase_admin", StringComparison.Ordinal) >= 0) return false;
            if (lowered.IndexOf("begin private key", StringComparison.Ordinal) >= 0) return false;
            if (lowered.IndexOf("firebase-adminsdk", StringComparison.Ordinal) >= 0) return false;
            if (lowered.StartsWith("sb_publishable_", StringComparison.OrdinalIgnoreCase)) return true;

            var jwtRole = TryExtractSupabaseJwtRole(v);
            return string.IsNullOrWhiteSpace(jwtRole)
                || string.Equals(jwtRole, "anon", StringComparison.OrdinalIgnoreCase);
        }

        private static string TryExtractSupabaseJwtRole(string value)
        {
            var v = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(v)) return string.Empty;

            var parts = v.Split('.');
            if (parts.Length != 3) return string.Empty;

            try
            {
                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2:
                        payload += "==";
                        break;
                    case 3:
                        payload += "=";
                        break;
                    case 1:
                        return string.Empty;
                }

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                var match = Regex.Match(
                    json,
                    "\"role\"\\s*:\\s*\"(?<role>[^\"]+)\"",
                    RegexOptions.IgnoreCase
                );
                return match.Success ? (match.Groups["role"].Value ?? string.Empty).Trim() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private IEnumerable<string> GetSupabaseClientConfigProbePaths()
        {
            var paths = new List<string>();
            AddSupabaseClientConfigProbePath(paths, SupabaseClientConfigFilePath);
            try { AddSupabaseClientConfigProbePath(paths, Path.Combine(Environment.CurrentDirectory, "supabase_client.json")); } catch { }
            try { AddSupabaseClientConfigProbePath(paths, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "supabase_client.json")); } catch { }
            return paths;
        }

        private void AddSupabaseClientConfigProbePath(List<string> paths, string path)
        {
            if (paths == null || string.IsNullOrWhiteSpace(path)) return;
            string full = null;
            try { full = Path.GetFullPath(path); } catch { full = path; }
            for (int i = 0; i < paths.Count; i++)
            {
                if (string.Equals(paths[i], full, StringComparison.OrdinalIgnoreCase)) return;
            }
            paths.Add(full);
        }

        private async Task<bool> EnsureSupabaseBackendSessionAsync(SupabaseClientConfigRecord cfg, bool forceRefresh)
        {
            if (cfg == null || _currentSession == null) return false;
            var hasUsableSession = !string.IsNullOrWhiteSpace(GetCurrentSessionToken());
            if (!forceRefresh && hasUsableSession) return true;

            lock (_supabaseSessionRefreshSync)
            {
                hasUsableSession = !string.IsNullOrWhiteSpace(GetCurrentSessionToken());
                if (_supabaseSessionRefreshInFlight)
                {
                    return hasUsableSession;
                }
                if (!forceRefresh)
                {
                    var secondsSinceLastAttempt = (DateTime.UtcNow - _supabaseLastSessionRefreshAttemptUtc).TotalSeconds;
                    if (hasUsableSession && secondsSinceLastAttempt < 20)
                    {
                        return true;
                    }
                    if (!hasUsableSession && secondsSinceLastAttempt < 3)
                    {
                        return false;
                    }
                }

                _supabaseSessionRefreshInFlight = true;
                _supabaseLastSessionRefreshAttemptUtc = DateTime.UtcNow;
            }

            try
            {
                SessionRecord refreshed = null;
                string error = null;
                var ok = await Task.Run(delegate
                {
                    return TryIssueCachedBackendRoleSession(cfg, out refreshed, out error);
                }).ConfigureAwait(false);

                if (!ok || refreshed == null || string.IsNullOrWhiteSpace(refreshed.SessionToken))
                {
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        AppendPosRuntimeLog("EnsureSupabaseBackendSessionAsync", error);
                    }
                    return !string.IsNullOrWhiteSpace(GetCurrentSessionToken());
                }

                if (Dispatcher.CheckAccess())
                {
                    ApplySupabaseRefreshedSession(refreshed);
                }
                else
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        ApplySupabaseRefreshedSession(refreshed);
                    }));
                }
                return true;
            }
            finally
            {
                lock (_supabaseSessionRefreshSync)
                {
                    _supabaseSessionRefreshInFlight = false;
                }
            }
        }

        private bool TryIssueCachedBackendRoleSession(
            SupabaseClientConfigRecord cfg,
            out SessionRecord refreshed,
            out string error)
        {
            refreshed = null;
            error = null;
            if (cfg == null || _currentSession == null)
            {
                error = "session_refresh_unavailable";
                return false;
            }

            var currentSession = _currentSession;
            var currentUserId = (currentSession.UserId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                error = "session_refresh_user_missing";
                return false;
            }

            LocalCashierCredentialRecord cached = null;
            lock (_localCashierCredentialsSync)
            {
                NormalizeLocalCashierCredentialsCacheNoLock();
                cached = CloneLocalCashierCredential(FindLocalCashierCredentialByUserIdNoLock(currentUserId));
                if (cached == null && _localCashierCredentials != null && _localCashierCredentials.Entries != null)
                {
                    for (var i = 0; i < _localCashierCredentials.Entries.Count; i++)
                    {
                        var row = _localCashierCredentials.Entries[i];
                        if (row == null) continue;
                        if (!row.IsActive) continue;
                        if (!LocalIdentifierMatches(row, currentSession.Username)) continue;
                        cached = CloneLocalCashierCredential(row);
                        break;
                    }
                }
            }

            if (cached == null)
            {
                error = "cached_staff_secret_missing";
                return false;
            }

            var plainPassword = UnprotectLocalSecret(cached.PasswordSecretProtected);
            if (string.IsNullOrWhiteSpace(plainPassword))
            {
                error = "cached_staff_secret_unavailable";
                return false;
            }

            var identifiers = new List<string>();
            Action<string> addIdentifier = delegate (string candidate)
            {
                var clean = (candidate ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(clean)) return;
                for (var i = 0; i < identifiers.Count; i++)
                {
                    if (string.Equals(identifiers[i], clean, StringComparison.OrdinalIgnoreCase)) return;
                }
                identifiers.Add(clean);
            };

            addIdentifier(currentSession.Username);
            addIdentifier(cached.Username);
            addIdentifier(cached.Phone);
            addIdentifier(cached.Email);
            addIdentifier(currentSession.UserId);
            addIdentifier(cached.UserId);

            if (identifiers.Count == 0)
            {
                error = "session_refresh_identifier_missing";
                return false;
            }

            using (var http = new HttpClient { BaseAddress = new Uri(cfg.Url.TrimEnd('/')) })
            {
                var timeoutMs = cfg.RestTimeoutMs > 0
                    ? Math.Max(cfg.RestTimeoutMs, SupabaseSessionIssueTimeoutMs)
                    : SupabaseSessionIssueTimeoutMs;
                http.Timeout = TimeSpan.FromMilliseconds(timeoutMs);

                for (var i = 0; i < identifiers.Count; i++)
                {
                    var identifier = identifiers[i];
                    var issued = TryIssueBackendRoleSession(http, cfg, identifier, plainPassword, out var issueError);
                    if (issued == null || !issued.ok || issued.user == null)
                    {
                        if (!string.IsNullOrWhiteSpace(issueError)) error = issueError;
                        continue;
                    }

                    var matched = issued.user;
                    var issuedToken = (issued.sessionToken ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(issuedToken))
                    {
                        error = "session_token_missing";
                        continue;
                    }

                    var backendRole = (matched.role ?? string.Empty).Trim().ToUpperInvariant();
                    var localRole = MapBackendRoleToLocalRole(backendRole);
                    if (string.Equals(localRole, "CUSTOMER", StringComparison.OrdinalIgnoreCase))
                    {
                        error = "staff_session_required";
                        continue;
                    }

                    var resolvedUsername =
                        !string.IsNullOrWhiteSpace(matched.display_name) ? matched.display_name :
                        !string.IsNullOrWhiteSpace(matched.displayName) ? matched.displayName :
                        !string.IsNullOrWhiteSpace(matched.username) ? matched.username :
                        !string.IsNullOrWhiteSpace(matched.email) ? matched.email :
                        !string.IsNullOrWhiteSpace(matched.phone) ? matched.phone :
                        currentSession.Username;

                    refreshed = new SessionRecord
                    {
                        Username = resolvedUsername ?? currentSession.Username,
                        Role = string.IsNullOrWhiteSpace(localRole) ? currentSession.Role : localRole,
                        UserId = !string.IsNullOrWhiteSpace(matched.id) ? matched.id.Trim() : currentSession.UserId,
                        BackendRole = backendRole,
                        SessionToken = issuedToken,
                        SessionExpiresAtMillis = issued.expiresAtMillis > 0
                            ? issued.expiresAtMillis
                            : 0
                    };
                    UpsertLocalCashierCredentialFromBackend(matched, plainPassword);
                    return true;
                }
            }

            return false;
        }

        private void ApplySupabaseRefreshedSession(SessionRecord refreshed)
        {
            if (refreshed == null) return;
            _currentSession = refreshed;
            try
            {
                if (File.Exists(SessionFilePath))
                {
                    SaveSessionToDisk(_currentSession);
                }
            }
            catch
            {
            }

            QueueSupabaseAppOrdersPullFromBackend();
            QueueSupabaseNotificationsPullFromBackend();
            QueueSupabaseDriversPullFromBackend(false);
        }

        private async Task<bool> TrySupabaseConnectivityProbeAsync(SupabaseClientConfigRecord cfg)
        {
            if (cfg == null) return false;

            var probeUrl = cfg.Url + "/rest/v1/" + cfg.NotificationsTable + "?select=id&limit=1";
            try
            {
                using (var req = new HttpRequestMessage(HttpMethod.Get, probeUrl))
                {
                    ApplySupabaseRestHeaders(req, cfg, true);
                    using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                    using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Supabase probe failed: " + ex.Message);
                return false;
            }
        }

        private void QueueSupabaseBackendSnapshotExport()
        {
            var cfg = LoadSupabaseClientConfigResolved();
            if (cfg == null) return;

            lock (_supabaseBackendExportSync)
            {
                if (_supabaseBackendExportInFlight) return;
                if ((DateTime.UtcNow - _supabaseLastBackendExportAtUtc).TotalMinutes < 3) return;
                _supabaseBackendExportInFlight = true;
            }

            Task.Run(async delegate
            {
                try
                {
                    await ExportSupabaseBackendSnapshotAsync(cfg).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    AppendPosRuntimeLog("ExportSupabaseBackendSnapshotAsync", ex);
                }
                finally
                {
                    lock (_supabaseBackendExportSync)
                    {
                        _supabaseBackendExportInFlight = false;
                        _supabaseLastBackendExportAtUtc = DateTime.UtcNow;
                    }
                }
            });
        }

        private async Task ExportSupabaseBackendSnapshotAsync(SupabaseClientConfigRecord cfg)
        {
            if (cfg == null) return;

            EnsureAppDataDir();

            var serializer = CreateUntypedJsonSerializer();
            var descriptors = GetSupabaseTableExportDescriptors();
            var manifestTables = new List<SupabaseBackendExportManifestTableRecord>();
            var skipped = new List<SupabaseBackendExportSkippedRecord>
            {
                new SupabaseBackendExportSkippedRecord { Table = "user_login_secrets", Reason = "skipped_sensitive_credentials" },
                new SupabaseBackendExportSkippedRecord { Table = "user_push_tokens", Reason = "skipped_sensitive_device_tokens" },
                new SupabaseBackendExportSkippedRecord { Table = "app_role_sessions", Reason = "skipped_sensitive_sessions" },
                new SupabaseBackendExportSkippedRecord { Table = "push_delivery_logs", Reason = "skipped_internal_delivery_logs" }
            };

            var combinedSql = new StringBuilder();
            combinedSql.AppendLine("-- Fale7 backend data export");
            combinedSql.AppendLine("-- Canonical DDL lives in supabase_schema.sql");
            combinedSql.AppendLine("-- Generated at " + DateTime.UtcNow.ToString("o"));
            combinedSql.AppendLine("-- Actor role: " + GetCurrentBackendActorRole());
            combinedSql.AppendLine();

            for (int i = 0; i < descriptors.Count; i++)
            {
                var descriptor = descriptors[i];
                if (descriptor == null || string.IsNullOrWhiteSpace(descriptor.TableName)) continue;

                try
                {
                    var rows = await FetchSupabaseTableRowsForExportAsync(cfg, descriptor.TableName, descriptor.OrderBy).ConfigureAwait(false);
                    var rowsJson = serializer.Serialize(rows ?? new List<Dictionary<string, object>>());
                    var jsonPath = GetSupabaseTableJsonExportFilePath(descriptor.TableName);
                    var sqlPath = GetSupabaseTableSqlExportFilePath(descriptor.TableName);
                    var sqlText = BuildSupabaseTableSqlExport(descriptor.TableName, rows, descriptor.ConflictPolicy, serializer);

                    WriteRawJsonTextFile(jsonPath, rowsJson);
                    WriteRawTextFile(sqlPath, sqlText);

                    manifestTables.Add(new SupabaseBackendExportManifestTableRecord
                    {
                        Table = descriptor.TableName,
                        RowCount = rows == null ? 0 : rows.Count,
                        JsonFile = "tables/" + Path.GetFileName(jsonPath),
                        SqlFile = "tables/" + Path.GetFileName(sqlPath),
                        ConflictPolicy = descriptor.ConflictPolicy
                    });

                    combinedSql.AppendLine(sqlText);
                    if (!combinedSql.ToString().EndsWith(Environment.NewLine, StringComparison.Ordinal))
                    {
                        combinedSql.AppendLine();
                    }
                }
                catch (Exception ex)
                {
                    skipped.Add(new SupabaseBackendExportSkippedRecord
                    {
                        Table = descriptor.TableName,
                        Reason = CompactExportError(ex)
                    });
                }
            }

            var manifest = new SupabaseBackendExportManifestRecord
            {
                Version = 1,
                GeneratedAt = DateTime.UtcNow.ToString("o"),
                ActorRole = GetCurrentBackendActorRole(),
                ActorUserId = GetCurrentSessionUserId(),
                SchemaHint = "supabase_schema.sql",
                CombinedSqlFile = "exports/" + Path.GetFileName(BackendCombinedSqlExportFilePath),
                Tables = manifestTables,
                Skipped = skipped
            };

            WriteJson(BackendSyncManifestFilePath, manifest);
            WriteRawTextFile(BackendCombinedSqlExportFilePath, combinedSql.ToString());
        }

        private List<SupabaseTableExportDescriptor> GetSupabaseTableExportDescriptors()
        {
            return new List<SupabaseTableExportDescriptor>
            {
                new SupabaseTableExportDescriptor { TableName = "source_products", OrderBy = "id.asc", ConflictPolicy = "server_wins" },
                new SupabaseTableExportDescriptor { TableName = "inventory_items", OrderBy = "id.asc", ConflictPolicy = "latest_updated_at_then_server" },
                new SupabaseTableExportDescriptor { TableName = "inventory_item_links", OrderBy = "entity_kind.asc", ConflictPolicy = "replace_entity_links" },
                new SupabaseTableExportDescriptor { TableName = "app_categories", OrderBy = "sort_order.asc", ConflictPolicy = "server_wins" },
                new SupabaseTableExportDescriptor { TableName = "app_products", OrderBy = "sort_order.asc", ConflictPolicy = "server_wins" },
                new SupabaseTableExportDescriptor { TableName = "pos_categories", OrderBy = "sort_order.asc", ConflictPolicy = "server_wins" },
                new SupabaseTableExportDescriptor { TableName = "pos_products", OrderBy = "sort_order.asc", ConflictPolicy = "server_wins" },
                new SupabaseTableExportDescriptor { TableName = "addon_groups", OrderBy = "sort_order.asc", ConflictPolicy = "server_wins" },
                new SupabaseTableExportDescriptor { TableName = "addon_options", OrderBy = "sort_order.asc", ConflictPolicy = "server_wins" },
                new SupabaseTableExportDescriptor { TableName = "app_category_default_groups", OrderBy = "category_id.asc", ConflictPolicy = "replace_category_links" },
                new SupabaseTableExportDescriptor { TableName = "app_product_group_overrides", OrderBy = "product_id.asc", ConflictPolicy = "replace_product_overrides" },
                new SupabaseTableExportDescriptor { TableName = "users", OrderBy = "updated_at.desc", ConflictPolicy = "latest_updated_at_then_server" },
                new SupabaseTableExportDescriptor { TableName = "hoods", OrderBy = "id.asc", ConflictPolicy = "server_wins" },
                new SupabaseTableExportDescriptor { TableName = "app_work_hours", OrderBy = "id.asc", ConflictPolicy = "server_wins" },
                new SupabaseTableExportDescriptor { TableName = "coupons", OrderBy = "sort_order.asc", ConflictPolicy = "server_wins" },
                new SupabaseTableExportDescriptor { TableName = "app_orders", OrderBy = "created_at.desc", ConflictPolicy = "status_stage_then_latest_update" },
                new SupabaseTableExportDescriptor { TableName = "inventory_order_reservations", OrderBy = "id.asc", ConflictPolicy = "server_wins" },
                new SupabaseTableExportDescriptor { TableName = "notifications", OrderBy = "created_at.desc", ConflictPolicy = "append_only_dedupe_by_id" },
                new SupabaseTableExportDescriptor { TableName = "coupon_usage", OrderBy = "used_at.desc", ConflictPolicy = "append_only_composite_key" },
                new SupabaseTableExportDescriptor { TableName = "app_settings", OrderBy = "id.asc", ConflictPolicy = "latest_updated_at_millis_then_server" },
                new SupabaseTableExportDescriptor { TableName = "customer_addresses", OrderBy = "id.asc", ConflictPolicy = "latest_updated_at_if_available_else_server" },
                new SupabaseTableExportDescriptor { TableName = "pos_registered_addresses", OrderBy = "id.asc", ConflictPolicy = "latest_registered_at_then_server" },
                new SupabaseTableExportDescriptor { TableName = "support_threads", OrderBy = "updated_at_millis.desc", ConflictPolicy = "latest_updated_at_millis_then_server" },
                new SupabaseTableExportDescriptor { TableName = "support_messages", OrderBy = "created_at_millis.asc", ConflictPolicy = "append_only_dedupe_by_id" }
            };
        }

        private async Task<List<Dictionary<string, object>>> FetchSupabaseTableRowsForExportAsync(
            SupabaseClientConfigRecord cfg,
            string tableName,
            string orderBy)
        {
            var rows = new List<Dictionary<string, object>>();
            var offset = 0;
            const int pageSize = 1000;

            while (true)
            {
                var url = new StringBuilder();
                url.Append(cfg.Url);
                url.Append("/rest/v1/");
                url.Append(Uri.EscapeDataString(tableName ?? string.Empty));
                url.Append("?select=*");
                url.Append("&limit=");
                url.Append(pageSize.ToString(CultureInfo.InvariantCulture));
                url.Append("&offset=");
                url.Append(offset.ToString(CultureInfo.InvariantCulture));
                if (!string.IsNullOrWhiteSpace(orderBy))
                {
                    url.Append("&order=");
                    url.Append(Uri.EscapeDataString(orderBy));
                }

                using (var req = new HttpRequestMessage(HttpMethod.Get, url.ToString()))
                {
                    ApplySupabaseRestHeaders(req, cfg, true);
                    using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs > 0 ? cfg.RestTimeoutMs : 10000)))
                    using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                    {
                        var raw = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                        if (!resp.IsSuccessStatusCode)
                        {
                            throw new InvalidOperationException("table_export_http_" + (int)resp.StatusCode + ":" + ExtractServerErrorMessage(raw));
                        }

                        var page = DeserializeUntypedJsonArray(raw);
                        if (page.Count == 0) break;
                        rows.AddRange(page);
                        if (page.Count < pageSize) break;
                        offset += page.Count;
                    }
                }
            }

            return rows;
        }

        private List<Dictionary<string, object>> DeserializeUntypedJsonArray(string raw)
        {
            var output = new List<Dictionary<string, object>>();
            if (string.IsNullOrWhiteSpace(raw)) return output;

            var serializer = CreateUntypedJsonSerializer();
            var root = serializer.DeserializeObject(raw) as object[];
            if (root == null) return output;

            for (int i = 0; i < root.Length; i++)
            {
                var map = NormalizeUntypedJsonValue(root[i]) as Dictionary<string, object>;
                if (map != null) output.Add(map);
            }

            return output;
        }

        private object NormalizeUntypedJsonValue(object value)
        {
            if (value == null) return null;

            var dict = value as Dictionary<string, object>;
            if (dict != null)
            {
                var normalized = new Dictionary<string, object>(StringComparer.Ordinal);
                foreach (var kv in dict)
                {
                    normalized[kv.Key] = NormalizeUntypedJsonValue(kv.Value);
                }
                return normalized;
            }

            var arr = value as object[];
            if (arr != null)
            {
                var list = new List<object>(arr.Length);
                for (int i = 0; i < arr.Length; i++)
                {
                    list.Add(NormalizeUntypedJsonValue(arr[i]));
                }
                return list;
            }

            return value;
        }

        private JavaScriptSerializer CreateUntypedJsonSerializer()
        {
            return new JavaScriptSerializer
            {
                MaxJsonLength = int.MaxValue,
                RecursionLimit = 128
            };
        }

        private string BuildSupabaseTableSqlExport(
            string tableName,
            List<Dictionary<string, object>> rows,
            string conflictPolicy,
            JavaScriptSerializer serializer)
        {
            var buffer = new StringBuilder();
            buffer.AppendLine("-- Table export: " + tableName);
            buffer.AppendLine("-- Conflict policy: " + conflictPolicy);
            buffer.AppendLine("-- Generated at " + DateTime.UtcNow.ToString("o"));

            if (rows == null || rows.Count == 0)
            {
                buffer.AppendLine("-- No rows");
                return buffer.ToString();
            }

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null || row.Count == 0) continue;

                var columns = new List<string>(row.Keys);
                columns.Sort(StringComparer.Ordinal);

                var columnSql = new StringBuilder();
                var valueSql = new StringBuilder();
                for (int j = 0; j < columns.Count; j++)
                {
                    var column = columns[j];
                    if (j > 0)
                    {
                        columnSql.Append(", ");
                        valueSql.Append(", ");
                    }
                    columnSql.Append(QuoteSqlIdentifier(column));
                    object cell;
                    row.TryGetValue(column, out cell);
                    valueSql.Append(BuildSqlLiteral(cell, serializer));
                }

                buffer.Append("INSERT INTO public.");
                buffer.Append(QuoteSqlIdentifier(tableName));
                buffer.Append(" (");
                buffer.Append(columnSql.ToString());
                buffer.Append(") VALUES (");
                buffer.Append(valueSql.ToString());
                buffer.AppendLine(");");
            }

            return buffer.ToString();
        }

        private string BuildSqlLiteral(object value, JavaScriptSerializer serializer)
        {
            if (value == null) return "NULL";

            if (value is bool) return (bool)value ? "TRUE" : "FALSE";

            if (value is byte || value is sbyte || value is short || value is ushort
                || value is int || value is uint || value is long || value is ulong
                || value is float || value is double || value is decimal)
            {
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            }

            if (value is Dictionary<string, object> || value is List<object> || value is object[])
            {
                var json = serializer.Serialize(value);
                return "'" + EscapeSqlLiteralForExport(json) + "'::jsonb";
            }

            return "'" + EscapeSqlLiteralForExport(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty) + "'";
        }

        private string QuoteSqlIdentifier(string value)
        {
            var text = value ?? string.Empty;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        private string EscapeSqlLiteralForExport(string value)
        {
            return (value ?? string.Empty).Replace("'", "''");
        }

        private string GetSupabaseTableJsonExportFilePath(string tableName)
        {
            return Path.Combine(AppDataSyncTablesDirPath, SanitizeSupabaseExportFileName(tableName) + ".json");
        }

        private string GetSupabaseTableSqlExportFilePath(string tableName)
        {
            return Path.Combine(AppDataSyncTablesDirPath, SanitizeSupabaseExportFileName(tableName) + ".sql");
        }

        private string SanitizeSupabaseExportFileName(string value)
        {
            var raw = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw)) return "unnamed";
            var sb = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                var ch = raw[i];
                if ((ch >= 'a' && ch <= 'z')
                    || (ch >= 'A' && ch <= 'Z')
                    || (ch >= '0' && ch <= '9')
                    || ch == '_' || ch == '-' || ch == '.')
                {
                    sb.Append(ch);
                }
                else
                {
                    sb.Append('_');
                }
            }
            return sb.ToString();
        }

        private void WriteRawJsonTextFile(string path, string raw)
        {
            WriteRawTextFile(path, FormatJsonPretty(raw));
        }

        private void WriteRawTextFile(string path, string raw)
        {
            try
            {
                EnsureAppDataDir();
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(path, raw ?? string.Empty, new UTF8Encoding(false));
            }
            catch
            {
            }
        }

        private string CompactExportError(Exception ex)
        {
            var raw = ex == null ? string.Empty : ((ex.Message ?? string.Empty).Trim());
            if (string.IsNullOrWhiteSpace(raw)) raw = "export_failed";
            raw = Regex.Replace(raw, "\\s+", " ");
            if (raw.Length <= 180) return raw;
            return raw.Substring(0, 177) + "...";
        }

        private void ShowSupabaseServerError(string title, string details)
        {
            var message = (details ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(message)) return;

            var safeTitle = string.IsNullOrWhiteSpace(title) ? "Server Error" : title.Trim();
            var signature = safeTitle + "|" + message;
            var nowUtc = DateTime.UtcNow;
            lock (_supabaseRealtimeSync)
            {
                if (string.Equals(_supabaseLastErrorSignature, signature, StringComparison.Ordinal) &&
                    (nowUtc - _supabaseLastErrorAtUtc).TotalSeconds < 6)
                {
                    return;
                }
                _supabaseLastErrorSignature = signature;
                _supabaseLastErrorAtUtc = nowUtc;
            }

            if (message.Length > 600) message = message.Substring(0, 600) + " ...";
            AppendPosRuntimeLog("SupabaseError", safeTitle + " | " + message);

            Dispatcher.BeginInvoke(new Action(delegate
            {
                // Toast is non-blocking and avoids modal-messagebox freezes during reconnect storms.
                ShowSupabaseToast(safeTitle, message, false);
            }), DispatcherPriority.Background);
        }

        private void QueueSupabaseDriversPullFromBackend(bool showErrors)
        {
            lock (_supabaseRealtimeSync)
            {
                _supabaseDriversPullPending = true;
                if (showErrors) _supabaseDriversPullShowErrors = true;
                if (_supabaseDriversPullInFlight) return;
                _supabaseDriversPullInFlight = true;
            }

            Task.Run(delegate
            {
                while (true)
                {
                    var shouldShowErrors = false;
                    lock (_supabaseRealtimeSync)
                    {
                        shouldShowErrors = _supabaseDriversPullShowErrors;
                        _supabaseDriversPullShowErrors = false;
                        _supabaseDriversPullPending = false;
                    }

                    TryRefreshSupabaseDriversFromBackend(shouldShowErrors);

                    var continueLoop = false;
                    lock (_supabaseRealtimeSync)
                    {
                        continueLoop = _supabaseDriversPullPending;
                        if (!continueLoop) _supabaseDriversPullInFlight = false;
                    }
                    if (!continueLoop) break;
                }
            });
        }

        private void QueueSupabasePosMenuPullFromBackend(bool showErrors, int debounceMs = 0)
        {
            lock (_supabaseRealtimeSync)
            {
                _supabasePosMenuPullPending = true;
                if (debounceMs > 0)
                {
                    var candidate = DateTime.UtcNow.AddMilliseconds(debounceMs);
                    if (candidate > _supabasePosMenuPullNotBeforeUtc) _supabasePosMenuPullNotBeforeUtc = candidate;
                }
                if (_supabasePosMenuPullInFlight) return;
                _supabasePosMenuPullInFlight = true;
            }

            Task.Run(async delegate
            {
                while (true)
                {
                    while (true)
                    {
                        DateTime notBeforeUtc;
                        lock (_supabaseRealtimeSync)
                        {
                            notBeforeUtc = _supabasePosMenuPullNotBeforeUtc;
                        }

                        var wait = notBeforeUtc - DateTime.UtcNow;
                        if (wait > TimeSpan.Zero)
                        {
                            await Task.Delay(wait).ConfigureAwait(false);
                        }

                        var shouldRecheck = false;
                        lock (_supabaseRealtimeSync)
                        {
                            shouldRecheck = DateTime.UtcNow < _supabasePosMenuPullNotBeforeUtc;
                            if (!shouldRecheck)
                            {
                                _supabasePosMenuPullNotBeforeUtc = DateTime.MinValue;
                                _supabasePosMenuPullPending = false;
                            }
                        }
                        if (!shouldRecheck) break;
                    }

                    try
                    {
                        var cfg = LoadSupabaseClientConfigResolved();
                        if (cfg != null)
                        {
                            await RefreshSupabasePosCatalogFromBackendAsync(cfg).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Supabase POS menu pull failed: " + ex.Message);
                        if (showErrors) ShowSupabaseServerError("Failed to refresh POS menu", ex.Message);
                    }

                    var continueLoop = false;
                    lock (_supabaseRealtimeSync)
                    {
                        continueLoop = _supabasePosMenuPullPending;
                        if (!continueLoop) _supabasePosMenuPullInFlight = false;
                    }
                    if (!continueLoop) break;
                }
            });
        }

        private void QueueSupabaseDistrictsPullFromBackend(bool showErrors)
        {
            lock (_supabaseRealtimeSync)
            {
                _supabaseDistrictsPullPending = true;
                if (_supabaseDistrictsPullInFlight) return;
                _supabaseDistrictsPullInFlight = true;
            }

            Task.Run(async delegate
            {
                while (true)
                {
                    lock (_supabaseRealtimeSync) _supabaseDistrictsPullPending = false;
                    try
                    {
                        var cfg = LoadSupabaseClientConfigResolved();
                        if (cfg != null)
                        {
                            var rows = await FetchSupabaseHoodsAsync(cfg).ConfigureAwait(false);
                            ApplySupabaseDistrictCatalogToUi(rows);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Supabase districts pull failed: " + ex.Message);
                        if (showErrors) ShowSupabaseServerError("Failed to refresh districts", ex.Message);
                    }

                    var continueLoop = false;
                    lock (_supabaseRealtimeSync)
                    {
                        continueLoop = _supabaseDistrictsPullPending;
                        if (!continueLoop) _supabaseDistrictsPullInFlight = false;
                    }
                    if (!continueLoop) break;
                }
            });
        }

        private async Task RefreshSupabasePosCatalogFromBackendAsync(SupabaseClientConfigRecord cfg)
        {
            if (cfg == null) return;
            var menuRaw = await PostSupabaseRpcGetStringAsync(cfg, "api_pos_menu_snapshot", "{}").ConfigureAwait(false);
            var productsRaw = await PostSupabaseRpcGetStringAsync(cfg, "api_pos_app_products_snapshot", "{}").ConfigureAwait(false);

            var menuSnapshot = UnwrapSupabaseRpcResult<PosMenuBackendSnapshotRecord>(menuRaw);
            var appProductsRpc = UnwrapSupabaseRpcResult<List<PosAppProductRpcRecord>>(productsRaw);
            if (menuSnapshot == null && (appProductsRpc == null || appProductsRpc.Count == 0)) return;

            var appProducts = new List<AppProductSnapshotRecord>();
            if (appProductsRpc != null)
            {
                for (int i = 0; i < appProductsRpc.Count; i++)
                {
                    var p = appProductsRpc[i];
                    if (p == null || string.IsNullOrWhiteSpace(p.Id)) continue;
                    var productName = p.Name;
                    if (string.IsNullOrWhiteSpace(productName)) continue;
                    var groups = new List<AppAddonGroupSnapshotRecord>();
                    if (p.AddonGroups != null)
                    {
                        for (int g = 0; g < p.AddonGroups.Count; g++)
                        {
                            var srcGroup = p.AddonGroups[g];
                            if (srcGroup == null || string.IsNullOrWhiteSpace(srcGroup.Id)) continue;
                            var groupName = !string.IsNullOrWhiteSpace(srcGroup.NameAr) ? srcGroup.NameAr : srcGroup.Name;
                            if (string.IsNullOrWhiteSpace(groupName)) continue;
                            var mappedGroup = new AppAddonGroupSnapshotRecord
                            {
                                Id = srcGroup.Id.Trim(),
                                NameAr = groupName.Trim(),
                                MultiSelect = srcGroup.MultiSelect,
                                Options = new List<AppAddonOptionSnapshotRecord>()
                            };
                            if (srcGroup.Options != null)
                            {
                                for (int o = 0; o < srcGroup.Options.Count; o++)
                                {
                                    var srcOpt = srcGroup.Options[o];
                                    if (srcOpt == null || string.IsNullOrWhiteSpace(srcOpt.Id)) continue;
                                    var optionName = !string.IsNullOrWhiteSpace(srcOpt.NameAr) ? srcOpt.NameAr : srcOpt.Name;
                                    if (string.IsNullOrWhiteSpace(optionName)) continue;
                                    var extra = srcOpt.ExtraPrice != 0 ? srcOpt.ExtraPrice : srcOpt.Price;
                                    mappedGroup.Options.Add(new AppAddonOptionSnapshotRecord
                                    {
                                        Id = srcOpt.Id.Trim(),
                                        NameAr = optionName.Trim(),
                                        ExtraPrice = extra < 0 ? 0 : (int)Math.Round(extra)
                                    });
                                }
                            }
                            groups.Add(mappedGroup);
                        }
                    }
                    appProducts.Add(new AppProductSnapshotRecord
                    {
                        Id = p.Id.Trim(),
                        SourceProductId = !string.IsNullOrWhiteSpace(p.SourceProductId) ? p.SourceProductId.Trim() : p.Id.Trim(),
                        NameAr = productName.Trim(),
                        BasePrice = p.BasePrice < 0 ? 0 : (int)Math.Round(p.BasePrice),
                        AddonGroups = groups
                    });
                }
            }

            _ = Dispatcher.BeginInvoke(new Action(delegate
            {
                if (ShouldDeferSupabasePosMenuSnapshotApply()) return;

                var changed = false;
                var menuChanged = false;
                var appProductsChanged = false;
                if (appProducts.Count > 0)
                {
                    appProductsChanged = ApplyAppProductsSnapshot(appProducts);
                    changed = appProductsChanged || changed;
                }
                if (menuSnapshot != null)
                {
                    menuChanged = ApplyPosMenuSnapshot(menuSnapshot);
                    changed = menuChanged || changed;
                }
                if (!changed) return;

                if (menuChanged) SaveMenuStateToDisk();
                if (appProductsChanged) SaveAppProductsSnapshotToDisk();

                RefreshMenuPanelsAcrossRoles();
                RefreshDeliveryAppOrdersUi();
                RenderDeliverySharedPickups();
                if (TakeawayView != null && TakeawayView.Visibility == Visibility.Visible)
                {
                    RenderPickups();
                    RenderReceipt();
                }
            }), DispatcherPriority.Background);
        }

        private void ApplySupabaseDistrictCatalogToUi(List<SupabaseHoodRow> rows)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(delegate { ApplySupabaseDistrictCatalogToUi(rows); }), DispatcherPriority.Background);
                return;
            }

            if (rows == null || rows.Count == 0) return;
            var next = new List<DeliveryDistrictFeeRecord>();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null || string.IsNullOrWhiteSpace(row.Name)) continue;
                next.Add(new DeliveryDistrictFeeRecord
                {
                    Id = (row.Id ?? string.Empty).Trim(),
                    Name = row.Name.Trim(),
                    Fee = row.Fee < 0 ? 0 : (int)Math.Round(row.Fee)
                });
            }
            if (next.Count == 0) return;

            next.Sort(delegate (DeliveryDistrictFeeRecord a, DeliveryDistrictFeeRecord b)
            {
                return string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
            });

            var selected = GetSelectedDeliveryDistrict();
            _deliveryDistrictCatalog.Clear();
            for (int i = 0; i < next.Count; i++) _deliveryDistrictCatalog.Add(next[i]);
            RebuildDeliveryDistrictComboItems(selected);
            if (IsDeliveryDistrictPickerOpen())
            {
                var searchTextBox = FindName("DeliveryDistrictSearchTextBox") as TextBox;
                RebuildDeliveryDistrictPickerList(searchTextBox == null ? null : searchTextBox.Text);
            }
        }

        private bool TryRefreshSupabaseDriversFromBackend(bool showErrors)
        {
            try
            {
                var cfg = LoadSupabaseClientConfigResolved();
                if (cfg == null) return false;
                var rows = FetchSupabaseDriversAsync(cfg).GetAwaiter().GetResult();
                ApplySupabaseDriversToUi(rows);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Supabase drivers pull failed: " + ex.Message);
                if (showErrors) ShowSupabaseServerError("تعذر تحميل السائقين", ex.Message);
                return false;
            }
        }

        private async Task<List<SupabaseDriverRow>> FetchSupabaseDriversAsync(SupabaseClientConfigRecord cfg)
        {
            return await FetchSupabaseDriversViaRpcAsync(cfg).ConfigureAwait(false);
        }

        private async Task<List<SupabaseDriverRow>> FetchSupabaseDriversViaRpcAsync(SupabaseClientConfigRecord cfg)
        {
            var merged = new List<SupabaseDriverRow>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Exception lastError = null;

            try
            {
                await AppendSupabaseDriversViaRpcRoleAsync(cfg, "DRIVER", merged, seen).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            try
            {
                await AppendSupabaseDriversViaRpcRoleAsync(cfg, "ADMIN_POS", merged, seen).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (lastError == null) lastError = ex;
            }

            if (merged.Count == 0 && lastError != null) throw lastError;
            return merged;
        }

        private async Task AppendSupabaseDriversViaRpcRoleAsync(SupabaseClientConfigRecord cfg, string role, List<SupabaseDriverRow> target, HashSet<string> seen)
        {
            if (cfg == null || target == null || seen == null) return;
            var normalizedRole = (role ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalizedRole)) return;

            var url = cfg.Url + "/rest/v1/rpc/api_users_search";
            var payload = new SupabaseUsersSearchRequest { Role = normalizedRole, Limit = 300 };
            var rawBody = SerializeJson(payload);
            using (var req = new HttpRequestMessage(HttpMethod.Post, url))
            {
                ApplySupabaseRestHeaders(req, cfg, true);
                req.Content = new StringContent(rawBody, Encoding.UTF8, "application/json");
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    var raw = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException("HTTP " + (int)resp.StatusCode + " " + raw);
                    }
                    var rows = DeserializeJson<List<SupabaseDriverRow>>(raw) ?? new List<SupabaseDriverRow>();
                    for (int i = 0; i < rows.Count; i++)
                    {
                        var row = rows[i];
                        var id = row == null ? string.Empty : (row.Id ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(id)) continue;
                        if (!seen.Add(id)) continue;
                        target.Add(row);
                    }
                }
            }
        }

        private async Task<List<SupabaseCashierCredentialRow>> FetchSupabaseCashierCredentialsSnapshotAsync(SupabaseClientConfigRecord cfg)
        {
            if (cfg == null) return new List<SupabaseCashierCredentialRow>();
            var requestBody = SerializeJson(new SupabaseCashierCredentialsSnapshotRequest
            {
                IncludeInactive = true,
                Limit = 2000
            });
            var raw = await PostSupabaseRpcGetStringAsync(
                cfg,
                "api_pos_cashier_credentials_snapshot",
                requestBody,
                actorRoleOverride: "ADMIN_POS").ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(raw)) return new List<SupabaseCashierCredentialRow>();

            var rows = DeserializeJson<List<SupabaseCashierCredentialRow>>(raw) ?? new List<SupabaseCashierCredentialRow>();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null) continue;
                if (!row.IsActive) row.IsActive = row.IsActiveSnake;
                if (string.IsNullOrWhiteSpace(row.UpdatedAt)) row.UpdatedAt = row.UpdatedAtSnake;
                if (string.IsNullOrWhiteSpace(row.SecretUpdatedAt)) row.SecretUpdatedAt = row.SecretUpdatedAtSnake;
            }
            return rows;
        }

        private async Task<bool> PushLocalCashierCredentialToSupabaseAsync(SupabaseClientConfigRecord cfg, LocalCashierCredentialRecord local)
        {
            if (cfg == null || local == null) return false;

            var userId = (local.UserId ?? string.Empty).Trim();
            var role = (local.Role ?? string.Empty).Trim().ToUpperInvariant();
            var protectedSecret = (local.PasswordSecretProtected ?? string.Empty).Trim();
            var plainPassword = UnprotectLocalSecret(protectedSecret);
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role)) return false;
            if (!IsCachedStaffRole(role)) return false;
            if (string.IsNullOrWhiteSpace(plainPassword)) return false;

            var request = new SupabaseUpsertStaffCredentialRequest
            {
                UserId = userId,
                Username = (local.Username ?? string.Empty).Trim(),
                DisplayName = (local.DisplayName ?? string.Empty).Trim(),
                Phone = (local.Phone ?? string.Empty).Trim(),
                Email = (local.Email ?? string.Empty).Trim(),
                Role = role,
                IsActive = local.IsActive,
                Password = plainPassword,
                Pin = role == "CASHIER" ? plainPassword : null
            };

            var raw = await PostSupabaseRpcGetStringAsync(
                cfg,
                "api_pos_upsert_staff_credential",
                SerializeJson(request),
                actorRoleOverride: "ADMIN_POS",
                actorUserIdOverride: userId).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var parsed = DeserializeJson<SupabaseUpsertStaffCredentialResponse>(raw);
            if (parsed != null) return parsed.Ok;
            return raw.IndexOf("\"ok\":true", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ApplySupabaseDriversToUi(List<SupabaseDriverRow> rows)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(delegate { ApplySupabaseDriversToUi(rows); }), DispatcherPriority.Background);
                return;
            }

            var next = new List<DeliveryDriverRecord>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            rows = rows ?? new List<SupabaseDriverRow>();

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null || string.IsNullOrWhiteSpace(row.Id)) continue;
                var id = row.Id.Trim();
                if (!seen.Add(id)) continue;
                var display = !string.IsNullOrWhiteSpace(row.DisplayName) ? row.DisplayName : row.DisplayNameCamel;
                var name = !string.IsNullOrWhiteSpace(display) ? display.Trim()
                    : (!string.IsNullOrWhiteSpace(row.Username) ? row.Username.Trim() : string.Empty);
                var phone = (row.Phone ?? string.Empty).Trim();
                name = ResolveFriendlyDriverName(id, name, phone);
                next.Add(new DeliveryDriverRecord
                {
                    Id = id,
                    Name = name,
                    Phone = phone,
                    AppUserId = id
                });
            }

            if (next.Count == 0)
            {
                if (_deliveryDrivers.Count > 0)
                {
                    RebuildDeliveryShiftFilterCombo();
                    if (DeliveryDriversOverlay != null && DeliveryDriversOverlay.Visibility == Visibility.Visible)
                    {
                        RenderDeliveryDriversOverlayList();
                    }
                    return;
                }

                next = BuildFallbackDeliveryDriversFromLocalOrders();
                if (next.Count == 0)
                {
                    RebuildDeliveryShiftFilterCombo();
                    if (DeliveryDriversOverlay != null && DeliveryDriversOverlay.Visibility == Visibility.Visible)
                    {
                        RenderDeliveryDriversOverlayList();
                    }
                    return;
                }
            }

            _deliveryDrivers.Clear();
            for (int i = 0; i < next.Count; i++) _deliveryDrivers.Add(next[i]);
            RebuildDeliveryShiftFilterCombo();
            PersistDeliveryState();

            if (DeliveryDriversOverlay != null && DeliveryDriversOverlay.Visibility == Visibility.Visible)
            {
                RenderDeliveryDriversOverlayList();
            }
        }

        private List<DeliveryDriverRecord> BuildFallbackDeliveryDriversFromLocalOrders()
        {
            var next = new List<DeliveryDriverRecord>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Action<DeliveryDriverRecord> addDriver = delegate (DeliveryDriverRecord source)
            {
                if (source == null) return;

                var id = (source.Id ?? string.Empty).Trim();
                var appUserId = (source.AppUserId ?? string.Empty).Trim();
                var phone = (source.Phone ?? string.Empty).Trim();
                var name = (source.Name ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(id))
                {
                    if (!string.IsNullOrWhiteSpace(appUserId)) id = appUserId;
                    else if (!string.IsNullOrWhiteSpace(phone)) id = "phone:" + phone;
                    else if (!string.IsNullOrWhiteSpace(name)) id = "name:" + name;
                }

                if (string.IsNullOrWhiteSpace(id)) return;
                if (!seen.Add(id)) return;
                name = ResolveFriendlyDriverName(id, name, phone);

                next.Add(new DeliveryDriverRecord
                {
                    Id = id,
                    Name = name,
                    Phone = phone,
                    AppUserId = !string.IsNullOrWhiteSpace(appUserId) ? appUserId : id
                });
            };

            for (int i = 0; i < _deliveryOrders.Count; i++)
            {
                var order = _deliveryOrders[i];
                if (order == null) continue;
                addDriver(order.Driver);
            }

            for (int i = 0; i < _deliveryShiftOrders.Count; i++)
            {
                var order = _deliveryShiftOrders[i];
                if (order == null) continue;
                addDriver(order.Driver);
            }

            for (int i = 0; i < _deliveryAppDeliveryOrders.Count; i++)
            {
                var order = _deliveryAppDeliveryOrders[i];
                if (order == null) continue;
                addDriver(order.Driver);
            }

            next.Sort(delegate (DeliveryDriverRecord a, DeliveryDriverRecord b)
            {
                return string.Compare(
                    a == null ? string.Empty : (a.Name ?? string.Empty),
                    b == null ? string.Empty : (b.Name ?? string.Empty),
                    StringComparison.CurrentCultureIgnoreCase);
            });
            return next;
        }

        private string ResolveFriendlyDriverName(string driverId, string proposedName, string phone)
        {
            var id = (driverId ?? string.Empty).Trim();
            var name = (proposedName ?? string.Empty).Trim();
            var normalizedPhone = (phone ?? string.Empty).Trim();

            if (!IsLikelyDriverPlaceholderName(name, id)) return name;

            for (int i = 0; i < _deliveryDrivers.Count; i++)
            {
                var row = _deliveryDrivers[i];
                if (row == null) continue;
                var rowId = (row.Id ?? string.Empty).Trim();
                if (!string.Equals(rowId, id, StringComparison.OrdinalIgnoreCase)) continue;
                var candidate = (row.Name ?? string.Empty).Trim();
                if (!IsLikelyDriverPlaceholderName(candidate, id)) return candidate;
            }

            for (int i = 0; i < _deliveryOrders.Count; i++)
            {
                var order = _deliveryOrders[i];
                var candidate = order == null || order.Driver == null ? string.Empty : (order.Driver.Name ?? string.Empty).Trim();
                var candidateId = order == null || order.Driver == null ? string.Empty : (order.Driver.Id ?? string.Empty).Trim();
                if (!string.Equals(candidateId, id, StringComparison.OrdinalIgnoreCase)) continue;
                if (!IsLikelyDriverPlaceholderName(candidate, id)) return candidate;
            }

            for (int i = 0; i < _deliveryShiftOrders.Count; i++)
            {
                var order = _deliveryShiftOrders[i];
                var candidate = order == null || order.Driver == null ? string.Empty : (order.Driver.Name ?? string.Empty).Trim();
                var candidateId = order == null || order.Driver == null ? string.Empty : (order.Driver.Id ?? string.Empty).Trim();
                if (!string.Equals(candidateId, id, StringComparison.OrdinalIgnoreCase)) continue;
                if (!IsLikelyDriverPlaceholderName(candidate, id)) return candidate;
            }

            for (int i = 0; i < _deliveryAppDeliveryOrders.Count; i++)
            {
                var order = _deliveryAppDeliveryOrders[i];
                var candidate = order == null || order.Driver == null ? string.Empty : (order.Driver.Name ?? string.Empty).Trim();
                var candidateId = order == null || order.Driver == null ? string.Empty : (order.Driver.Id ?? string.Empty).Trim();
                if (!string.Equals(candidateId, id, StringComparison.OrdinalIgnoreCase)) continue;
                if (!IsLikelyDriverPlaceholderName(candidate, id)) return candidate;
            }

            if (!string.IsNullOrWhiteSpace(normalizedPhone)) return normalizedPhone;
            if (!string.IsNullOrWhiteSpace(id))
            {
                var suffix = id.Length > 6 ? id.Substring(id.Length - 6) : id;
                return "سائق " + suffix;
            }
            return "سائق";
        }

        private bool IsLikelyDriverPlaceholderName(string name, string driverId)
        {
            var n = (name ?? string.Empty).Trim();
            var id = (driverId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(n)) return true;
            if (!string.IsNullOrWhiteSpace(id) && string.Equals(n, id, StringComparison.OrdinalIgnoreCase)) return true;
            if (n.StartsWith("drv_", StringComparison.OrdinalIgnoreCase)) return true;
            if (n.StartsWith("driver_", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private void MarkSupabasePosMenuLocalWritePending()
        {
            lock (_supabasePosMenuSync)
            {
                _supabasePosMenuLocalWriteHoldUntilUtc = DateTime.UtcNow.AddSeconds(6);
            }
        }

        private void ClearSupabasePosMenuLocalWritePending()
        {
            lock (_supabasePosMenuSync)
            {
                _supabasePosMenuLocalWriteHoldUntilUtc = DateTime.MinValue;
            }
        }

        private bool ShouldDeferSupabasePosMenuSnapshotApply()
        {
            lock (_supabasePosMenuSync)
            {
                return DateTime.UtcNow < _supabasePosMenuLocalWriteHoldUntilUtc;
            }
        }

        private void QueueSupabasePosMenuPushFromUi()
        {
            MarkSupabasePosMenuLocalWritePending();
            lock (_supabasePosMenuSync)
            {
                _supabasePosMenuPushPending = true;
                if (_supabasePosMenuPushInFlight) return;
                _supabasePosMenuPushInFlight = true;
            }

            Task.Run(async delegate
            {
                while (true)
                {
                    lock (_supabasePosMenuSync) _supabasePosMenuPushPending = false;
                    try
                    {
                        var cfg = LoadSupabaseClientConfigResolved();
                        if (cfg != null)
                        {
                            var snapshot = BuildSupabasePosMenuSyncSnapshotFromUi();
                            await PushSupabasePosMenuSnapshotAsync(cfg, snapshot).ConfigureAwait(false);
                            ClearSupabasePosMenuLocalWritePending();
                            QueueSupabasePosMenuPullFromBackend(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Supabase POS menu sync failed: " + ex.Message);
                        ShowSupabaseServerError("Failed to sync POS menu", ex.Message);
                    }

                    var continueLoop = false;
                    lock (_supabasePosMenuSync)
                    {
                        continueLoop = _supabasePosMenuPushPending;
                        if (!continueLoop) _supabasePosMenuPushInFlight = false;
                    }
                    if (!continueLoop) break;
                }
            });
        }

        private SupabasePosMenuSyncSnapshot BuildSupabasePosMenuSyncSnapshotFromUi()
        {
            if (Dispatcher != null && !Dispatcher.CheckAccess())
            {
                return Dispatcher.Invoke(new Func<SupabasePosMenuSyncSnapshot>(BuildSupabasePosMenuSyncSnapshotFromUiCore));
            }
            return BuildSupabasePosMenuSyncSnapshotFromUiCore();
        }

        private SupabasePosMenuSyncSnapshot BuildSupabasePosMenuSyncSnapshotFromUiCore()
        {
            NormalizeMenuState();
            var snapshot = new SupabasePosMenuSyncSnapshot
            {
                Categories = new List<SupabasePosCategoryRow>(),
                Products = new List<SupabasePosProductRow>(),
                SourceProducts = new List<SupabaseSourceProductRow>()
            };

            var sourceById = new Dictionary<string, SupabaseSourceProductRow>(StringComparer.Ordinal);
            var categorySort = 0;
            for (int c = 0; c < _menu.Count; c++)
            {
                var cat = _menu[c];
                if (cat == null || string.IsNullOrWhiteSpace(cat.Name)) continue;
                if (string.IsNullOrWhiteSpace(cat.RemoteId)) cat.RemoteId = "poscat_" + NewId();
                var categoryId = cat.RemoteId.Trim();

                snapshot.Categories.Add(new SupabasePosCategoryRow
                {
                    Id = categoryId,
                    Name = cat.Name.Trim(),
                    SortOrder = categorySort
                });
                categorySort++;

                if (cat.Items == null) continue;
                var productSort = 0;
                for (int p = 0; p < cat.Items.Count; p++)
                {
                    var item = cat.Items[p];
                    if (item == null || string.Equals(item.Name, "—", StringComparison.Ordinal)) continue;
                    if (string.IsNullOrWhiteSpace(item.RemoteId)) item.RemoteId = "posprd_" + NewId();
                    var productId = item.RemoteId.Trim();
                    var sourceProductId = !string.IsNullOrWhiteSpace(item.SourceProductId)
                        ? item.SourceProductId.Trim()
                        : (!string.IsNullOrWhiteSpace(item.ProductId) ? item.ProductId.Trim() : productId);
                    var baseName = !string.IsNullOrWhiteSpace(item.Name) ? item.Name.Trim() : (item.PosDisplayName ?? string.Empty).Trim();
                    var displayName = !string.IsNullOrWhiteSpace(item.PosDisplayName) ? item.PosDisplayName.Trim() : baseName;
                    var finalPrice = item.Price < 0 ? 0 : item.Price;

                    snapshot.Products.Add(new SupabasePosProductRow
                    {
                        Id = productId,
                        PosCategoryId = categoryId,
                        SourceProductId = sourceProductId,
                        Name = baseName,
                        PosDisplayName = displayName,
                        SortOrder = productSort,
                        Enabled = 1
                    });
                    productSort++;

                    sourceById[sourceProductId] = new SupabaseSourceProductRow
                    {
                        Id = sourceProductId,
                        Price = finalPrice
                    };
                }
            }

            foreach (var kv in sourceById) snapshot.SourceProducts.Add(kv.Value);
            return snapshot;
        }

        private async Task PushSupabasePosMenuSnapshotAsync(SupabaseClientConfigRecord cfg, SupabasePosMenuSyncSnapshot snapshot)
        {
            snapshot = snapshot ?? new SupabasePosMenuSyncSnapshot
            {
                Categories = new List<SupabasePosCategoryRow>(),
                Products = new List<SupabasePosProductRow>(),
                SourceProducts = new List<SupabaseSourceProductRow>()
            };

            var desiredCategoryIds = CollectDistinctSupabaseIds(snapshot.Categories, delegate(SupabasePosCategoryRow row)
            {
                return row == null ? null : row.Id;
            });
            var desiredProductIds = CollectDistinctSupabaseIds(snapshot.Products, delegate(SupabasePosProductRow row)
            {
                return row == null ? null : row.Id;
            });

            await UpsertSupabaseRowsAsync(cfg, "source_products", snapshot.SourceProducts, "id").ConfigureAwait(false);
            await UpsertSupabaseRowsAsync(cfg, "pos_categories", snapshot.Categories, "id").ConfigureAwait(false);
            await UpsertSupabaseRowsAsync(cfg, "pos_products", snapshot.Products, "id").ConfigureAwait(false);
            await DeleteSupabaseRowsMissingFromSnapshotAsync(cfg, "pos_products", desiredProductIds).ConfigureAwait(false);
            await DeleteSupabaseRowsMissingFromSnapshotAsync(cfg, "pos_categories", desiredCategoryIds).ConfigureAwait(false);
        }

        private HashSet<string> CollectDistinctSupabaseIds<TRow>(List<TRow> rows, Func<TRow, string> selector)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (rows == null || selector == null) return ids;

            for (var i = 0; i < rows.Count; i++)
            {
                var id = selector(rows[i]);
                if (string.IsNullOrWhiteSpace(id)) continue;
                ids.Add(id.Trim());
            }
            return ids;
        }

        private async Task DeleteSupabaseRowsMissingFromSnapshotAsync(
            SupabaseClientConfigRecord cfg,
            string table,
            HashSet<string> desiredIds)
        {
            if (cfg == null || string.IsNullOrWhiteSpace(table)) return;
            desiredIds = desiredIds ?? new HashSet<string>(StringComparer.Ordinal);

            var existingIds = await FetchSupabaseIdsAsync(cfg, table).ConfigureAwait(false);
            if (existingIds == null || existingIds.Count == 0) return;

            var staleIds = new List<string>();
            foreach (var id in existingIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (desiredIds.Contains(id)) continue;
                staleIds.Add(id);
            }

            if (staleIds.Count == 0) return;
            await DeleteSupabaseRowsByIdsAsync(cfg, table, staleIds).ConfigureAwait(false);
        }

        private async Task<HashSet<string>> FetchSupabaseIdsAsync(SupabaseClientConfigRecord cfg, string table)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (cfg == null || string.IsNullOrWhiteSpace(table)) return ids;

            var url = cfg.Url + "/rest/v1/" + table.Trim() + "?select=id&limit=5000";
            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                ApplySupabaseRestHeaders(req, cfg, false);
                using (var cts = new CancellationTokenSource(Math.Max(1000, cfg.RestTimeoutMs > 0 ? cfg.RestTimeoutMs : 8000)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    var raw = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                    {
                        throw new Exception("Supabase fetch ids failed for " + table + ": " + raw);
                    }

                    var rows = DeserializeJson<List<SupabaseIdOnlyRow>>(raw) ?? new List<SupabaseIdOnlyRow>();
                    for (var i = 0; i < rows.Count; i++)
                    {
                        var row = rows[i];
                        var id = row == null ? string.Empty : (row.Id ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(id)) continue;
                        ids.Add(id);
                    }
                }
            }

            return ids;
        }

        private async Task DeleteSupabaseRowsByIdsAsync(SupabaseClientConfigRecord cfg, string table, List<string> ids)
        {
            if (cfg == null || string.IsNullOrWhiteSpace(table) || ids == null || ids.Count == 0) return;

            const int chunkSize = 128;
            for (var start = 0; start < ids.Count; start += chunkSize)
            {
                var chunk = new List<string>();
                for (var i = start; i < ids.Count && i < start + chunkSize; i++)
                {
                    var id = (ids[i] ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    chunk.Add(id);
                }
                if (chunk.Count == 0) continue;

                var url = cfg.Url + "/rest/v1/" + table.Trim() + "?id=in.(" + BuildSupabaseIdListFilter(chunk) + ")";
                using (var req = new HttpRequestMessage(HttpMethod.Delete, url))
                {
                    ApplySupabaseRestHeaders(req, cfg, false);
                    using (var cts = new CancellationTokenSource(Math.Max(1000, cfg.RestTimeoutMs > 0 ? cfg.RestTimeoutMs : 8000)))
                    using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                    {
                        if (resp.IsSuccessStatusCode) continue;
                        var err = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                        throw new Exception("Supabase delete rows failed for " + table + ": " + err);
                    }
                }
            }
        }

        private string BuildSupabaseIdListFilter(List<string> ids)
        {
            if (ids == null || ids.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            for (var i = 0; i < ids.Count; i++)
            {
                var id = (ids[i] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(id)) continue;
                if (sb.Length > 0) sb.Append(',');
                sb.Append(Uri.EscapeDataString("\"" + id.Replace("\"", "\\\"") + "\""));
            }
            return sb.ToString();
        }

        private async Task InsertSupabaseRowsAsync<T>(SupabaseClientConfigRecord cfg, string table, List<T> rows)
        {
            if (cfg == null || string.IsNullOrWhiteSpace(table)) return;
            if (rows == null || rows.Count == 0) return;
            var url = cfg.Url + "/rest/v1/" + table.Trim();
            var raw = SerializeJson(rows);

            using (var req = new HttpRequestMessage(HttpMethod.Post, url))
            {
                ApplySupabaseRestHeaders(req, cfg, false);
                req.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
                req.Content = new StringContent(raw, Encoding.UTF8, "application/json");
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    if (resp.IsSuccessStatusCode) return;
                    var err = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    throw new InvalidOperationException("Insert into " + table + " failed: HTTP " + (int)resp.StatusCode + " " + err);
                }
            }
        }

        private async Task UpsertSupabaseRowsAsync<T>(SupabaseClientConfigRecord cfg, string table, List<T> rows, string onConflict)
        {
            if (cfg == null || string.IsNullOrWhiteSpace(table)) return;
            if (rows == null || rows.Count == 0) return;
            var url = cfg.Url + "/rest/v1/" + table.Trim();
            if (!string.IsNullOrWhiteSpace(onConflict))
            {
                url += "?on_conflict=" + Uri.EscapeDataString(onConflict.Trim());
            }
            var raw = SerializeJson(rows);

            using (var req = new HttpRequestMessage(HttpMethod.Post, url))
            {
                ApplySupabaseRestHeaders(req, cfg, false);
                req.Headers.TryAddWithoutValidation("Prefer", "resolution=merge-duplicates,return=minimal");
                req.Content = new StringContent(raw, Encoding.UTF8, "application/json");
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    if (resp.IsSuccessStatusCode) return;
                    var err = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    throw new InvalidOperationException("Upsert into " + table + " failed: HTTP " + (int)resp.StatusCode + " " + err);
                }
            }
        }

        private async Task DeleteAllSupabaseRowsAsync(SupabaseClientConfigRecord cfg, string table, string keyColumn)
        {
            if (cfg == null || string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(keyColumn)) return;
            var url = cfg.Url + "/rest/v1/" + table.Trim() + "?" + keyColumn.Trim() + "=neq.__none__";
            using (var req = new HttpRequestMessage(HttpMethod.Delete, url))
            {
                ApplySupabaseRestHeaders(req, cfg, false);
                req.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    if (resp.IsSuccessStatusCode) return;
                    var err = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    throw new InvalidOperationException("Delete from " + table + " failed: HTTP " + (int)resp.StatusCode + " " + err);
                }
            }
        }

        private bool TryLoadPosRegisteredAddressesForPhone(string phone, out List<DeliverySavedAddressRecord> addresses, out string serverError)
        {
            addresses = new List<DeliverySavedAddressRecord>();
            serverError = null;

            phone = (phone ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(phone)) return true;

            try
            {
                var cfg = LoadSupabaseClientConfigResolved();
                if (cfg == null) return true;
                var rows = FetchPosRegisteredAddressesByPhoneAsync(cfg, phone).GetAwaiter().GetResult();
                for (int i = 0; i < rows.Count; i++)
                {
                    var mapped = MapSupabasePosRegisteredAddress(rows[i]);
                    if (mapped != null) addresses.Add(mapped);
                }
                return true;
            }
            catch (Exception ex)
            {
                serverError = ex.Message;
                return false;
            }
        }

        private bool TryInsertPosRegisteredAddress(string phone, DeliverySavedAddressRecord address, out DeliverySavedAddressRecord savedAddress, out string serverError)
        {
            savedAddress = null;
            serverError = null;
            phone = (phone ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(phone) || address == null)
            {
                serverError = "invalid_address_payload";
                return false;
            }

            try
            {
                var cfg = LoadSupabaseClientConfigResolved();
                if (cfg == null)
                {
                    serverError = "supabase_config_missing";
                    return false;
                }
                var row = InsertPosRegisteredAddressAsync(cfg, phone, address).GetAwaiter().GetResult();
                savedAddress = MapSupabasePosRegisteredAddress(row) ?? address;
                return true;
            }
            catch (Exception ex)
            {
                serverError = ex.Message;
                return false;
            }
        }

        private bool TryUpdatePosRegisteredAddress(string addressId, string phone, DeliverySavedAddressRecord address, out DeliverySavedAddressRecord savedAddress, out string serverError)
        {
            savedAddress = null;
            serverError = null;
            phone = (phone ?? string.Empty).Trim();
            int parsedId;
            if (!int.TryParse((addressId ?? string.Empty).Trim(), out parsedId) || parsedId <= 0 || string.IsNullOrWhiteSpace(phone) || address == null)
            {
                serverError = "invalid_address_payload";
                return false;
            }

            try
            {
                var cfg = LoadSupabaseClientConfigResolved();
                if (cfg == null)
                {
                    serverError = "supabase_config_missing";
                    return false;
                }
                var row = UpdatePosRegisteredAddressByIdAsync(cfg, parsedId, phone, address).GetAwaiter().GetResult();
                savedAddress = MapSupabasePosRegisteredAddress(row) ?? address;
                return true;
            }
            catch (Exception ex)
            {
                serverError = ex.Message;
                return false;
            }
        }

        private bool TryDeletePosRegisteredAddress(string addressId, out string serverError)
        {
            serverError = null;
            int parsedId;
            if (!int.TryParse((addressId ?? string.Empty).Trim(), out parsedId) || parsedId <= 0)
            {
                serverError = "invalid_address_id";
                return false;
            }

            try
            {
                var cfg = LoadSupabaseClientConfigResolved();
                if (cfg == null)
                {
                    serverError = "supabase_config_missing";
                    return false;
                }
                DeletePosRegisteredAddressByIdAsync(cfg, parsedId).GetAwaiter().GetResult();
                return true;
            }
            catch (Exception ex)
            {
                serverError = ex.Message;
                return false;
            }
        }

        private async Task<List<SupabasePosRegisteredAddressRow>> FetchPosRegisteredAddressesByPhoneAsync(SupabaseClientConfigRecord cfg, string phone)
        {
            var url = cfg.Url + "/rest/v1/pos_registered_addresses?select=id,phone,label,address_line,registered_at&phone=eq." + Uri.EscapeDataString(phone) + "&order=registered_at.desc&limit=100";
            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                ApplySupabaseRestHeaders(req, cfg, true);
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    var raw = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException("HTTP " + (int)resp.StatusCode + " " + raw);
                    }
                    return DeserializeJson<List<SupabasePosRegisteredAddressRow>>(raw) ?? new List<SupabasePosRegisteredAddressRow>();
                }
            }
        }

        private async Task<SupabasePosRegisteredAddressRow> InsertPosRegisteredAddressAsync(SupabaseClientConfigRecord cfg, string phone, DeliverySavedAddressRecord address)
        {
            var payload = new SupabasePosRegisteredAddressInsertRow
            {
                Phone = phone,
                Label = string.IsNullOrWhiteSpace(address.District) ? "عنوان" : address.District.Trim(),
                AddressLine = BuildSupabasePosAddressLine(address)
            };
            var rawPayload = SerializeJson(payload);
            var url = cfg.Url + "/rest/v1/pos_registered_addresses";
            using (var req = new HttpRequestMessage(HttpMethod.Post, url))
            {
                ApplySupabaseRestHeaders(req, cfg, false);
                req.Headers.TryAddWithoutValidation("Prefer", "return=representation");
                req.Content = new StringContent(rawPayload, Encoding.UTF8, "application/json");
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    var raw = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException("HTTP " + (int)resp.StatusCode + " " + raw);
                    }
                    var rows = DeserializeJson<List<SupabasePosRegisteredAddressRow>>(raw);
                    if (rows == null || rows.Count == 0) return null;
                    return rows[0];
                }
            }
        }

        private async Task<SupabasePosRegisteredAddressRow> UpdatePosRegisteredAddressByIdAsync(SupabaseClientConfigRecord cfg, int id, string phone, DeliverySavedAddressRecord address)
        {
            var payload = new SupabasePosRegisteredAddressInsertRow
            {
                Phone = phone,
                Label = string.IsNullOrWhiteSpace(address.District) ? "Ø¹Ù†ÙˆØ§Ù†" : address.District.Trim(),
                AddressLine = BuildSupabasePosAddressLine(address)
            };
            var rawPayload = SerializeJson(payload);
            var url = cfg.Url + "/rest/v1/pos_registered_addresses?id=eq." + id;
            using (var req = new HttpRequestMessage(new HttpMethod("PATCH"), url))
            {
                ApplySupabaseRestHeaders(req, cfg, false);
                req.Headers.TryAddWithoutValidation("Prefer", "return=representation");
                req.Content = new StringContent(rawPayload, Encoding.UTF8, "application/json");
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    var raw = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException("HTTP " + (int)resp.StatusCode + " " + raw);
                    }
                    var rows = DeserializeJson<List<SupabasePosRegisteredAddressRow>>(raw);
                    if (rows == null || rows.Count == 0) return null;
                    return rows[0];
                }
            }
        }

        private async Task DeletePosRegisteredAddressByIdAsync(SupabaseClientConfigRecord cfg, int id)
        {
            var url = cfg.Url + "/rest/v1/pos_registered_addresses?id=eq." + id;
            using (var req = new HttpRequestMessage(HttpMethod.Delete, url))
            {
                ApplySupabaseRestHeaders(req, cfg, false);
                req.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    if (resp.IsSuccessStatusCode) return;
                    var raw = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    throw new InvalidOperationException("HTTP " + (int)resp.StatusCode + " " + raw);
                }
            }
        }

        private DeliverySavedAddressRecord MapSupabasePosRegisteredAddress(SupabasePosRegisteredAddressRow row)
        {
            if (row == null) return null;

            var mapped = new DeliverySavedAddressRecord
            {
                Id = row.Id > 0 ? row.Id.ToString() : NewId(),
                District = SanitizeSupabaseAddressText(row.Label),
                Block = string.Empty,
                Street = string.Empty,
                Building = string.Empty,
                Apartment = string.Empty,
                Floor = string.Empty,
                Note = string.Empty
            };

            var payload = ParseSupabasePosAddressPayload(row.AddressLine);
            if (payload != null)
            {
                var parsedDistrict = SanitizeSupabaseAddressText(payload.District);
                mapped.District = string.IsNullOrWhiteSpace(parsedDistrict) ? mapped.District : parsedDistrict;
                mapped.Block = SanitizeSupabaseAddressText(payload.Block);
                mapped.Street = SanitizeSupabaseAddressText(payload.Street);
                mapped.Building = SanitizeSupabaseAddressText(payload.Building);
                mapped.Apartment = SanitizeSupabaseAddressText(payload.Apartment);
                mapped.Floor = SanitizeSupabaseAddressText(payload.Floor);
                mapped.Note = SanitizeSupabaseAddressText(payload.Note);
                return mapped;
            }

            var line = SanitizeSupabaseAddressText(row.AddressLine);
            if (!string.IsNullOrWhiteSpace(line)) mapped.Note = line;
            return mapped;
        }

        private SupabasePosAddressPayload ParseSupabasePosAddressPayload(string rawAddressLine)
        {
            var raw = SanitizeSupabaseAddressText(rawAddressLine);
            if (string.IsNullOrWhiteSpace(raw)) return null;
            if (raw.StartsWith("json:", StringComparison.OrdinalIgnoreCase))
            {
                raw = raw.Substring(5).Trim();
            }
            if (!raw.StartsWith("{", StringComparison.Ordinal)) return null;
            try
            {
                var payload = DeserializeJson<SupabasePosAddressPayload>(raw);
                if (payload == null) return null;
                payload.District = SanitizeSupabaseAddressText(payload.District);
                payload.Block = SanitizeSupabaseAddressText(payload.Block);
                payload.Street = SanitizeSupabaseAddressText(payload.Street);
                payload.Building = SanitizeSupabaseAddressText(payload.Building);
                payload.Apartment = SanitizeSupabaseAddressText(payload.Apartment);
                payload.Floor = SanitizeSupabaseAddressText(payload.Floor);
                payload.Note = SanitizeSupabaseAddressText(payload.Note);
                return payload;
            }
            catch
            {
                return null;
            }
        }

        private string BuildSupabasePosAddressLine(DeliverySavedAddressRecord address)
        {
            var payload = new SupabasePosAddressPayload
            {
                District = address == null ? string.Empty : SanitizeSupabaseAddressText(address.District),
                Block = address == null ? string.Empty : SanitizeSupabaseAddressText(address.Block),
                Street = address == null ? string.Empty : SanitizeSupabaseAddressText(address.Street),
                Building = address == null ? string.Empty : SanitizeSupabaseAddressText(address.Building),
                Apartment = address == null ? string.Empty : SanitizeSupabaseAddressText(address.Apartment),
                Floor = address == null ? string.Empty : SanitizeSupabaseAddressText(address.Floor),
                Note = address == null ? string.Empty : SanitizeSupabaseAddressText(address.Note)
            };
            return SerializeJson(payload);
        }

        private string SanitizeSupabaseAddressText(string value)
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length == 0) return string.Empty;

            var decoded = DecodeSupabaseLatin1Utf8IfNeeded(trimmed);
            decoded = decoded.Replace('\uFFFD', ' ').Trim();
            return decoded;
        }

        private string DecodeSupabaseLatin1Utf8IfNeeded(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            if (ContainsArabicCharacters(value)) return value;
            if (!LooksLikeSupabaseMojibake(value)) return value;

            var best = value;
            var bestScore = SupabaseMojibakeScore(value);
            var current = value;
            for (var i = 0; i < 3; i++)
            {
                var latin1 = DecodeSupabaseOnce(current, Encoding.GetEncoding("ISO-8859-1"));
                var cp1252 = DecodeSupabaseOnce(current, Windows1252Encoding);
                var candidates = new[] { latin1, cp1252 };
                var improved = false;
                for (var c = 0; c < candidates.Length; c++)
                {
                    var candidate = candidates[c];
                    var score = SupabaseMojibakeScore(candidate);
                    if (ContainsArabicCharacters(candidate) && !ContainsArabicCharacters(best))
                    {
                        best = candidate;
                        bestScore = score;
                        current = candidate;
                        improved = true;
                        break;
                    }

                    if (score < bestScore)
                    {
                        best = candidate;
                        bestScore = score;
                        current = candidate;
                        improved = true;
                    }
                }

                if (!improved || !LooksLikeSupabaseMojibake(current))
                {
                    break;
                }
            }

            return best;
        }

        private static string DecodeSupabaseOnce(string input, Encoding sourceEncoding)
        {
            try
            {
                var bytes = sourceEncoding.GetBytes(input ?? string.Empty);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return input ?? string.Empty;
            }
        }

        private static bool ContainsArabicCharacters(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            for (var i = 0; i < input.Length; i++)
            {
                var ch = input[i];
                if (ch >= '\u0600' && ch <= '\u06FF') return true;
            }
            return false;
        }

        private static bool LooksLikeSupabaseMojibake(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            for (var i = 0; i < input.Length; i++)
            {
                var ch = input[i];
                if (ch == '\u00C3' ||
                    ch == '\u00D8' ||
                    ch == '\u00D9' ||
                    ch == '\u00DB' ||
                    ch == '\u00C2' ||
                    ch == '\u00E2')
                {
                    return true;
                }
            }
            return false;
        }

        private static int SupabaseMojibakeScore(string input)
        {
            if (string.IsNullOrEmpty(input)) return 0;
            var markers = 0;
            var replacement = 0;
            for (var i = 0; i < input.Length; i++)
            {
                var ch = input[i];
                if (ch == '\u00C3' ||
                    ch == '\u00D8' ||
                    ch == '\u00D9' ||
                    ch == '\u00DB' ||
                    ch == '\u00C2' ||
                    ch == '\u00E2')
                {
                    markers++;
                }
                if (ch == '\uFFFD' || ch == '?')
                {
                    replacement++;
                }
            }
            return (markers * 3) + (replacement * 2);
        }

        private void QueueSupabaseAppOrderCreatedInternalNotification(AppDeliveryOrderRecord order)
        {
            if (order == null) return;
            QueueSupabaseNotificationInsert(
                userId: null,
                roleTarget: "CASHIER",
                orderId: order.Id,
                orderType: "DELIVERY",
                title: "طلب تطبيق جديد",
                message: "وصول طلب ديلفري من التطبيق #" + order.No);
        }

        private void QueueSupabaseAppOrderCreatedInternalNotification(AppPickupOrderRecord order)
        {
            if (order == null) return;
            QueueSupabaseNotificationInsert(
                userId: null,
                roleTarget: "CASHIER",
                orderId: order.Id,
                orderType: "PICKUP",
                title: "طلب تطبيق جديد",
                message: "وصول طلب استلام من التطبيق #" + order.No);
        }

        private void QueueSupabaseAppDeliveryPreparingNotifications(AppDeliveryOrderRecord order)
        {
            if (order == null) return;
            QueueSupabaseNotificationToUserOrSkip(
                preferredUserId: order.CustomerUserId,
                fallbackRoleTarget: null,
                orderId: order.Id,
                orderType: "DELIVERY",
                title: "طلبك قيد التجهيز",
                message: "بدأ تجهيز طلبك رقم #" + order.No);
        }

        private void QueueSupabaseAppDeliveryNoDriverInternalNotification(AppDeliveryOrderRecord order)
        {
            if (order == null) return;
            QueueSupabaseNotificationInsert(
                userId: null,
                roleTarget: "CASHIER",
                orderId: order.Id,
                orderType: "DELIVERY",
                title: "بدون سائق",
                message: "طلب APP Delivery #" + order.No + " بدون سائق");
        }

        private void QueueSupabaseAppDeliveryDriverAssignedNotifications(AppDeliveryOrderRecord order, DeliveryDriverRecord driver)
        {
            if (order == null) return;

            QueueSupabaseNotificationToUserOrSkip(
                preferredUserId: driver == null ? null : driver.AppUserId,
                fallbackRoleTarget: "DRIVER",
                orderId: order.Id,
                orderType: "DELIVERY",
                title: "تم تعيين طلب جديد",
                message: "تم تعيين الطلب رقم " + order.No + " لك");

            QueueSupabaseNotificationToUserOrSkip(
                preferredUserId: order.CustomerUserId,
                fallbackRoleTarget: null,
                orderId: order.Id,
                orderType: "DELIVERY",
                title: "تم تعيين سائق",
                message: "تم تعيين سائق لطلبك رقم #" + order.No);
        }

        private void QueueSupabaseAppPickupPreparingNotification(AppPickupOrderRecord order)
        {
            if (order == null) return;
            QueueSupabaseNotificationToUserOrSkip(
                preferredUserId: order.CustomerUserId,
                fallbackRoleTarget: null,
                orderId: order.Id,
                orderType: "PICKUP",
                title: "طلبك قيد التجهيز",
                message: "بدأ تجهيز طلب الاستلام رقم #" + order.No);
        }

        private void QueueSupabaseAppPickupReadyNotification(AppPickupOrderRecord order)
        {
            if (order == null) return;
            QueueSupabaseNotificationToUserOrSkip(
                preferredUserId: order.CustomerUserId,
                fallbackRoleTarget: null,
                orderId: order.Id,
                orderType: "PICKUP",
                title: "طلبك جاهز للاستلام",
                message: "طلب الاستلام رقم #" + order.No + " جاهز الآن");
        }

        private void QueueSupabaseAppPickupDeliveredNotification(AppPickupOrderRecord order)
        {
            if (order == null) return;
            QueueSupabaseNotificationToUserOrSkip(
                preferredUserId: order.CustomerUserId,
                fallbackRoleTarget: null,
                orderId: order.Id,
                orderType: "PICKUP",
                title: "تم التسليم",
                message: "تم تسليم طلب الاستلام رقم #" + order.No);
        }

        private void QueueSupabaseNotificationToUserOrSkip(string preferredUserId, string fallbackRoleTarget, string orderId, string orderType, string title, string message)
        {
            var normalizedUserId = NormalizeSupabaseUuidOrNull(preferredUserId);
            if (!string.IsNullOrWhiteSpace(normalizedUserId))
            {
                var roleTarget = !string.IsNullOrWhiteSpace(fallbackRoleTarget) ? fallbackRoleTarget : InferRoleTargetForOrderType(orderType);
                QueueSupabaseNotificationInsert(
                    userId: normalizedUserId,
                    roleTarget: roleTarget,
                    orderId: orderId,
                    orderType: orderType,
                    title: title,
                    message: message);
                return;
            }

            if (string.IsNullOrWhiteSpace(fallbackRoleTarget)) return;
            QueueSupabaseNotificationInsert(
                userId: null,
                roleTarget: fallbackRoleTarget,
                orderId: orderId,
                orderType: orderType,
                title: title,
                message: message);
        }

        private string InferRoleTargetForOrderType(string orderType)
        {
            // End-user notifications default to CUSTOMER unless caller explicitly sets another fallback role.
            return "CUSTOMER";
        }

        private string NormalizeSupabaseUuidOrNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private void QueueSupabaseNotificationInsert(string userId, string roleTarget, string orderId, string orderType, string title, string message)
        {
            Task.Run(async delegate
            {
                try
                {
                    var cfg = LoadSupabaseClientConfigResolved();
                    if (cfg == null) return;
                    await InsertSupabaseNotificationAsync(cfg, new SupabaseNotificationInsertRecord
                    {
                        UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim(),
                        RoleTarget = string.IsNullOrWhiteSpace(roleTarget) ? "CASHIER" : roleTarget.Trim().ToUpperInvariant(),
                        OrderId = string.IsNullOrWhiteSpace(orderId) ? null : orderId.Trim(),
                        OrderType = string.IsNullOrWhiteSpace(orderType) ? null : orderType.Trim().ToUpperInvariant(),
                        Title = string.IsNullOrWhiteSpace(title) ? "Notification" : title.Trim(),
                        Message = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim(),
                        Read = false
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Supabase notification insert skipped/failed: " + ex.Message);
                }
            });
        }

        private async Task InsertSupabaseNotificationAsync(SupabaseClientConfigRecord cfg, SupabaseNotificationInsertRecord payload)
        {
            if (cfg == null || payload == null) return;
            var url = cfg.Url + "/rest/v1/rpc/api_emit_order_notification";
            
            // Build the RPC request with all required parameters
            var requestPayload = new SupabaseEmitOrderNotificationRequest
            {
                UserId = string.IsNullOrWhiteSpace(payload.UserId) ? null : payload.UserId.Trim(),
                RoleTarget = string.IsNullOrWhiteSpace(payload.RoleTarget) ? "CASHIER" : payload.RoleTarget.Trim().ToUpperInvariant(),
                OrderId = string.IsNullOrWhiteSpace(payload.OrderId) ? null : payload.OrderId.Trim(),
                OrderType = string.IsNullOrWhiteSpace(payload.OrderType) ? null : payload.OrderType.Trim().ToUpperInvariant(),
                Title = string.IsNullOrWhiteSpace(payload.Title) ? "Notification" : payload.Title.Trim(),
                Message = string.IsNullOrWhiteSpace(payload.Message) ? string.Empty : payload.Message.Trim(),
                Status = null
            };
            
            var raw = SerializeJson(requestPayload);
            using (var req = new HttpRequestMessage(HttpMethod.Post, url))
            {
                ApplySupabaseRestHeaders(req, cfg, true);
                req.Content = new StringContent(raw, Encoding.UTF8, "application/json");
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    if (!resp.IsSuccessStatusCode)
                    {
                        var err = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                        throw new InvalidOperationException("Supabase api_emit_order_notification failed: HTTP " + (int)resp.StatusCode + " " + err);
                    }
                }
            }
        }

        private string ResolveSupabaseNotificationsTable(SupabaseClientConfigRecord cfg)
        {
            return cfg != null && !string.IsNullOrWhiteSpace(cfg.NotificationsTable) ? cfg.NotificationsTable.Trim() : "notifications";
        }

        private string ResolveSupabaseOrdersTable(SupabaseClientConfigRecord cfg)
        {
            return cfg != null && !string.IsNullOrWhiteSpace(cfg.OrdersTable) ? cfg.OrdersTable.Trim() : "app_orders";
        }

        private void ApplySupabaseRestHeaders(HttpRequestMessage req, SupabaseClientConfigRecord cfg, bool acceptJson)
        {
            if (req == null || cfg == null) return;
            req.Headers.TryAddWithoutValidation("apikey", cfg.AnonKey);
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + cfg.AnonKey);
            var sessionToken = (GetCurrentSessionToken() ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(sessionToken))
            {
                req.Headers.TryAddWithoutValidation("x-fale7-session", sessionToken);
                req.Headers.TryAddWithoutValidation("x_fale7_session", sessionToken);
            }
            var actorRole = (GetCurrentBackendActorRole() ?? string.Empty).Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(actorRole))
            {
                req.Headers.TryAddWithoutValidation("x-fale7-role", actorRole);
                req.Headers.TryAddWithoutValidation("x_fale7_role", actorRole);
            }
            var actorUserId = (GetCurrentSessionUserId() ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(actorUserId))
            {
                req.Headers.TryAddWithoutValidation("x-fale7-user-id", actorUserId);
                req.Headers.TryAddWithoutValidation("x_fale7_user_id", actorUserId);
            }
            var schema = (cfg.Schema ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(schema))
            {
                req.Headers.TryAddWithoutValidation("Accept-Profile", schema);
                req.Headers.TryAddWithoutValidation("Content-Profile", schema);
            }
            if (acceptJson) req.Headers.TryAddWithoutValidation("Accept", "application/json");
        }

        private async Task<string> SafeReadHttpContentAsync(HttpResponseMessage resp)
        {
            try
            {
                return resp != null && resp.Content != null ? await resp.Content.ReadAsStringAsync().ConfigureAwait(false) : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task SupabaseRealtimeReconnectLoopAsync(SupabaseClientConfigRecord cfg, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using (var ws = new ClientWebSocket())
                    {
                        var wsUrl = BuildSupabaseRealtimeWebSocketUrl(cfg);
                        await ws.ConnectAsync(new Uri(wsUrl), ct).ConfigureAwait(false);
                        await SupabaseRealtimeRunSessionAsync(ws, cfg, ct).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    if (ct.IsCancellationRequested) break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Supabase realtime session error: " + ex.Message);
                }

                try { await Task.Delay(3000, ct).ConfigureAwait(false); } catch { }
            }
        }

        private string BuildSupabaseRealtimeWebSocketUrl(SupabaseClientConfigRecord cfg)
        {
            var baseUrl = (cfg.Url ?? string.Empty).Trim().TrimEnd('/');
            if (baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = "wss://" + baseUrl.Substring("https://".Length);
            }
            else if (baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl = "ws://" + baseUrl.Substring("http://".Length);
            }
            return baseUrl + "/realtime/v1/websocket?apikey=" + WebUtility.UrlEncode(cfg.AnonKey) + "&vsn=1.0.0";
        }

        private async Task SupabaseRealtimeRunSessionAsync(ClientWebSocket ws, SupabaseClientConfigRecord cfg, CancellationToken ct)
        {
            var schema = string.IsNullOrWhiteSpace(cfg.Schema) ? "public" : cfg.Schema;
            var notificationsTable = ResolveSupabaseNotificationsTable(cfg);
            var ordersTable = ResolveSupabaseOrdersTable(cfg);

            await SendSupabaseRealtimeJoinAsync(ws, "realtime:" + schema + ":" + notificationsTable, schema, notificationsTable, "INSERT", "1", cfg.AnonKey, ct).ConfigureAwait(false);
            // app_orders is protected by RLS. Subscribing with the anon key causes Supabase
            // Realtime to silently drop UPDATE events for rows the anon role cannot read.
            // Use the active session JWT so the server can evaluate RLS and deliver all
            // change events for orders belonging to this branch/session.
            var appOrdersRealtimeToken = GetCurrentSessionToken();
            if (string.IsNullOrWhiteSpace(appOrdersRealtimeToken)) appOrdersRealtimeToken = cfg.AnonKey;
            await SendSupabaseRealtimeJoinAsync(ws, "realtime:" + schema + ":" + ordersTable, schema, ordersTable, "*", "2", appOrdersRealtimeToken, ct).ConfigureAwait(false);
            await SendSupabaseRealtimeJoinAsync(ws, "realtime:" + schema + ":pos_categories", schema, "pos_categories", "*", "3", cfg.AnonKey, ct).ConfigureAwait(false);
            await SendSupabaseRealtimeJoinAsync(ws, "realtime:" + schema + ":pos_products", schema, "pos_products", "*", "4", cfg.AnonKey, ct).ConfigureAwait(false);
            await SendSupabaseRealtimeJoinAsync(ws, "realtime:" + schema + ":source_products", schema, "source_products", "*", "5", cfg.AnonKey, ct).ConfigureAwait(false);
            await SendSupabaseRealtimeJoinAsync(ws, "realtime:" + schema + ":hoods", schema, "hoods", "*", "6", cfg.AnonKey, ct).ConfigureAwait(false);
            await SendSupabaseRealtimeJoinAsync(ws, "realtime:" + schema + ":users", schema, "users", "*", "7", cfg.AnonKey, ct).ConfigureAwait(false);
            await SendSupabaseRealtimeJoinAsync(ws, "realtime:" + schema + ":app_products", schema, "app_products", "*", "8", cfg.AnonKey, ct).ConfigureAwait(false);
            await SendSupabaseRealtimeJoinAsync(ws, "realtime:" + schema + ":addon_groups", schema, "addon_groups", "*", "9", cfg.AnonKey, ct).ConfigureAwait(false);
            await SendSupabaseRealtimeJoinAsync(ws, "realtime:" + schema + ":addon_options", schema, "addon_options", "*", "10", cfg.AnonKey, ct).ConfigureAwait(false);
            await SendSupabaseRealtimeJoinAsync(ws, "realtime:" + schema + ":app_category_default_groups", schema, "app_category_default_groups", "*", "11", cfg.AnonKey, ct).ConfigureAwait(false);
            await SendSupabaseRealtimeJoinAsync(ws, "realtime:" + schema + ":app_product_group_overrides", schema, "app_product_group_overrides", "*", "12", cfg.AnonKey, ct).ConfigureAwait(false);
            await SendSupabaseRealtimeJoinAsync(ws, "realtime:" + schema + ":inventory_items", schema, "inventory_items", "*", "13", cfg.AnonKey, ct).ConfigureAwait(false);
            await SendSupabaseRealtimeJoinAsync(ws, "realtime:" + schema + ":inventory_item_links", schema, "inventory_item_links", "*", "14", cfg.AnonKey, ct).ConfigureAwait(false);

            QueueSupabaseAppOrdersPullFromBackend();
            QueueSupabaseNotificationsPullFromBackend();
            QueueSupabaseDriversPullFromBackend(false);
            QueueSupabasePosMenuPullFromBackend(false);
            QueueSupabaseDistrictsPullFromBackend(false);

            var nextHeartbeatAt = DateTime.UtcNow.AddSeconds(20);
            var nextProtectedRestPullAt = DateTime.UtcNow.AddSeconds(12);
            var nextDriversRestPullAt = DateTime.UtcNow.AddSeconds(25);
            var hbRef = 1000;
            while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                var nowUtc = DateTime.UtcNow;
                if (DateTime.UtcNow >= nextHeartbeatAt)
                {
                    hbRef++;
                    await SendSupabaseRealtimeTextAsync(ws, "{\"topic\":\"phoenix\",\"event\":\"heartbeat\",\"payload\":{},\"ref\":\"" + hbRef + "\"}", ct).ConfigureAwait(false);
                    nextHeartbeatAt = DateTime.UtcNow.AddSeconds(20);
                }

                // Periodic REST pull: safety-net for eventual consistency if a realtime
                // event is missed (e.g. reconnect gap, token expiry between heartbeats).
                // This poll interval deliberately does NOT fire immediately after a status
                // push — that path now relies on the authenticated realtime channel only.
                if (nowUtc >= nextProtectedRestPullAt)
                {
                    QueueSupabaseAppOrdersPullFromBackend();
                    QueueSupabaseNotificationsPullFromBackend();
                    nextProtectedRestPullAt = nowUtc.AddSeconds(12);
                }
                if (nowUtc >= nextDriversRestPullAt)
                {
                    QueueSupabaseDriversPullFromBackend(false);
                    nextDriversRestPullAt = nowUtc.AddSeconds(25);
                }

                string message = null;
                using (var recvCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    recvCts.CancelAfter(TimeSpan.FromSeconds(3));
                    try
                    {
                        message = await ReceiveSupabaseRealtimeTextAsync(ws, recvCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        if (ct.IsCancellationRequested) throw;
                        continue;
                    }
                }

                if (string.IsNullOrWhiteSpace(message)) continue;
                try
                {
                    HandleSupabaseRealtimeMessageAsync(message, cfg);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Supabase realtime message handling failed: " + ex.Message);
                    AppendPosRuntimeLog("SupabaseRealtime", "message handler failed: " + ex.Message);
                }
            }
        }

        private async Task SendSupabaseRealtimeJoinAsync(ClientWebSocket ws, string topic, string schema, string table, string eventName, string @ref, string accessToken, CancellationToken ct)
        {
            var payload =
                "{\"topic\":\"" + JsonEscape(topic) + "\"," +
                "\"event\":\"phx_join\"," +
                "\"payload\":{" +
                    "\"config\":{" +
                        "\"broadcast\":{\"ack\":false,\"self\":false}," +
                        "\"presence\":{\"enabled\":false,\"key\":\"\"}," +
                        "\"postgres_changes\":[{" +
                            "\"event\":\"" + JsonEscape(eventName) + "\"," +
                            "\"schema\":\"" + JsonEscape(schema) + "\"," +
                            "\"table\":\"" + JsonEscape(table) + "\"" +
                        "}]," +
                        "\"private\":false" +
                    "}," +
                    "\"access_token\":\"" + JsonEscape(accessToken) + "\"" +
                "}," +
                "\"ref\":\"" + JsonEscape(@ref) + "\"}";
            await SendSupabaseRealtimeTextAsync(ws, payload, ct).ConfigureAwait(false);
        }

        private async Task SendSupabaseRealtimeTextAsync(ClientWebSocket ws, string text, CancellationToken ct)
        {
            if (ws == null || ws.State != WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
            var seg = new ArraySegment<byte>(bytes);
            await ws.SendAsync(seg, WebSocketMessageType.Text, true, ct).ConfigureAwait(false);
        }

        private async Task<string> ReceiveSupabaseRealtimeTextAsync(ClientWebSocket ws, CancellationToken ct)
        {
            if (ws == null) return null;
            var buffer = new byte[4096];
            using (var ms = new MemoryStream())
            {
                while (true)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close) return null;
                    if (result.Count > 0) ms.Write(buffer, 0, result.Count);
                    if (result.EndOfMessage) break;
                }
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private void HandleSupabaseRealtimeMessageAsync(string raw, SupabaseClientConfigRecord cfg)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;

            string evt;
            string table;
            string eventType;
            if (!TryParseSupabaseRealtimeMeta(raw, out evt, out table, out eventType)) return;
            if (!string.Equals(evt, "postgres_changes", StringComparison.OrdinalIgnoreCase)) return;
            if (string.IsNullOrWhiteSpace(table)) return;

            var notificationsTable = ResolveSupabaseNotificationsTable(cfg);
            var ordersTable = ResolveSupabaseOrdersTable(cfg);

            if (string.Equals(table, notificationsTable, StringComparison.OrdinalIgnoreCase))
            {
                HandleSupabaseNotificationRealtime(raw, eventType);
                return;
            }

            if (string.Equals(table, ordersTable, StringComparison.OrdinalIgnoreCase))
            {
                SupabaseAppOrderRow record;
                SupabaseAppOrderRow oldRecord;
                TryParseSupabaseRealtimeRecords(raw, out record, out oldRecord);
                ApplySupabaseRealtimeAppOrderChange(eventType, record, oldRecord);
                return;
            }

            if (IsSupabaseMenuRelatedTable(table))
            {
                QueueSupabasePosMenuPullFromBackend(false, SupabasePosMenuRealtimeDebounceMs);
                QueueInventoryOverviewRefreshIfVisible();
                return;
            }

            if (string.Equals(table, "hoods", StringComparison.OrdinalIgnoreCase))
            {
                QueueSupabaseDistrictsPullFromBackend(false);
                return;
            }

            if (string.Equals(table, "users", StringComparison.OrdinalIgnoreCase))
            {
                QueueSupabaseDriversPullFromBackend(false);
                return;
            }
        }

        private bool TryParseSupabaseRealtimeMeta(string raw, out string evt, out string table, out string eventType)
        {
            evt = string.Empty;
            table = string.Empty;
            eventType = string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            try
            {
                var parsed = DeserializeJson<SupabaseRealtimeMessage>(raw);
                if (parsed != null)
                {
                    evt = (parsed.Event ?? string.Empty).Trim();
                    if (parsed.Payload != null)
                    {
                        table = (parsed.Payload.Table ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(table) && parsed.Payload.Data != null)
                        {
                            table = (parsed.Payload.Data.Table ?? string.Empty).Trim();
                        }

                        eventType = (parsed.Payload.EventType ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(eventType))
                        {
                            eventType = (parsed.Payload.Type ?? string.Empty).Trim();
                        }
                        if (string.IsNullOrWhiteSpace(eventType) && parsed.Payload.Data != null)
                        {
                            eventType = (parsed.Payload.Data.EventType ?? string.Empty).Trim();
                        }
                    }
                }
            }
            catch
            {
            }

            if (string.IsNullOrWhiteSpace(evt))
            {
                var eventMatch = SupabaseRealtimeEventRegex.Match(raw);
                if (eventMatch.Success) evt = (eventMatch.Groups["event"].Value ?? string.Empty).Trim();
            }
            if (string.IsNullOrWhiteSpace(table))
            {
                var tableMatch = SupabaseRealtimeTableRegex.Match(raw);
                if (tableMatch.Success) table = (tableMatch.Groups["table"].Value ?? string.Empty).Trim();
            }
            if (string.IsNullOrWhiteSpace(eventType))
            {
                var typeMatch = SupabaseRealtimeEventTypeRegex.Match(raw);
                if (typeMatch.Success) eventType = (typeMatch.Groups["eventType"].Value ?? string.Empty).Trim();
            }
            if (string.IsNullOrWhiteSpace(eventType))
            {
                var payloadTypeMatch = SupabaseRealtimePayloadTypeRegex.Match(raw);
                if (payloadTypeMatch.Success) eventType = (payloadTypeMatch.Groups["type"].Value ?? string.Empty).Trim();
            }

            return !string.IsNullOrWhiteSpace(evt);
        }

        private bool IsSupabaseMenuRelatedTable(string table)
        {
            if (string.IsNullOrWhiteSpace(table)) return false;
            return string.Equals(table, "pos_categories", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(table, "pos_products", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(table, "source_products", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(table, "app_products", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(table, "addon_groups", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(table, "addon_options", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(table, "app_category_default_groups", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(table, "app_product_group_overrides", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(table, "inventory_items", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(table, "inventory_item_links", StringComparison.OrdinalIgnoreCase);
        }

        private void QueueSupabaseNotificationsPullFromBackend()
        {
            Task.Run(async delegate
            {
                SupabaseClientConfigRecord cfg = null;
                try { cfg = LoadSupabaseClientConfigResolved(); } catch { cfg = null; }
                if (cfg == null) return;

                await EnsureSupabaseBackendSessionAsync(cfg, false).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(GetCurrentSessionToken()))
                {
                    await EnsureSupabaseBackendSessionAsync(cfg, true).ConfigureAwait(false);
                }
                if (string.IsNullOrWhiteSpace(GetCurrentSessionToken())) return;

                lock (_supabaseRealtimeSync)
                {
                    if (_supabaseNotificationsPullInFlight)
                    {
                        _supabaseNotificationsPullPending = true;
                        return;
                    }
                    _supabaseNotificationsPullInFlight = true;
                    _supabaseNotificationsPullPending = false;
                }

                while (true)
                {
                    try
                    {
                        var rows = await FetchSupabaseNotificationsAsync(cfg).ConfigureAwait(false);
                        ApplySupabaseNotificationsToLocalUi(rows);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Supabase notifications pull failed: " + ex.Message);
                    }

                    lock (_supabaseRealtimeSync)
                    {
                        if (_supabaseNotificationsPullPending)
                        {
                            _supabaseNotificationsPullPending = false;
                            continue;
                        }

                        _supabaseNotificationsPullInFlight = false;
                        break;
                    }
                }
            });
        }

        private async Task<List<SupabaseNotificationRow>> FetchSupabaseNotificationsAsync(SupabaseClientConfigRecord cfg)
        {
            if (cfg == null) return new List<SupabaseNotificationRow>();

            var requestBody = "{\"p_limit\":100}";
            var raw = await PostSupabaseRpcGetStringAsync(
                cfg,
                "api_pos_notifications_snapshot",
                requestBody,
                actorRoleOverride: GetCurrentBackendActorRole(),
                actorUserIdOverride: GetCurrentSessionUserId()).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(raw))
            {
                var rpcRows = DeserializeJson<List<SupabaseNotificationRow>>(raw);
                if (rpcRows != null) return rpcRows;
            }

            var notificationsTable = ResolveSupabaseNotificationsTable(cfg);
            var select = "id,user_id,role_target,order_id,order_type,title,message,read,created_at";
            var url = cfg.Url + "/rest/v1/" + notificationsTable
                + "?select=" + Uri.EscapeDataString(select)
                + "&order=created_at.desc&limit=100";

            var localRole = GetCurrentLocalRoleTarget();
            var userId = (GetCurrentSessionUserId() ?? string.Empty).Trim();
            if (string.Equals(localRole, "CASHIER", StringComparison.Ordinal))
            {
                var filter = "(and(user_id.is.null,role_target.eq.CASHIER))";
                url += "&or=" + Uri.EscapeDataString(filter);
            }
            else if (string.Equals(localRole, "DRIVER", StringComparison.Ordinal))
            {
                var filter = string.IsNullOrWhiteSpace(userId)
                    ? "(and(user_id.is.null,role_target.in.(DRIVER,DELIVERY)))"
                    : "(user_id.eq." + userId + ",and(user_id.is.null,role_target.in.(DRIVER,DELIVERY)))";
                url += "&or=" + Uri.EscapeDataString(filter);
            }

            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                ApplySupabaseRestHeaders(req, cfg, true);
                using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                {
                    var body = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException("HTTP " + (int)resp.StatusCode + " " + body);
                    }
                    return DeserializeJson<List<SupabaseNotificationRow>>(body) ?? new List<SupabaseNotificationRow>();
                }
            }
        }

        private void ApplySupabaseNotificationsToLocalUi(List<SupabaseNotificationRow> rows)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    try { ApplySupabaseNotificationsToLocalUi(rows); } catch (Exception ex) { Debug.WriteLine("Supabase notifications UI apply failed: " + ex.Message); }
                }), DispatcherPriority.Background);
                return;
            }

            rows = rows ?? new List<SupabaseNotificationRow>();
            var visible = new List<SupabaseNotificationRow>();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null || string.IsNullOrWhiteSpace(row.Id)) continue;
                if (!SupabaseNotificationMatchesCurrentLocalRole(row)) continue;
                visible.Add(row);
            }

            var fresh = new List<SupabaseNotificationRow>();
            var unreadCount = 0;
            var shouldRefresh = false;
            var primeOnly = false;
            lock (_supabaseNotificationSync)
            {
                for (var i = 0; i < visible.Count; i++)
                {
                    if (!visible[i].Read) unreadCount++;
                }

                shouldRefresh = _supabaseUnreadNotificationsCount != unreadCount;
                _supabaseUnreadNotificationsCount = unreadCount;

                if (!_supabaseNotificationsSeenPrimed)
                {
                    for (var i = 0; i < visible.Count; i++)
                    {
                        _supabaseSeenNotificationIds.Add(visible[i].Id);
                    }
                    _supabaseNotificationsSeenPrimed = true;
                    primeOnly = true;
                }
                else
                {
                    for (var i = 0; i < visible.Count; i++)
                    {
                        var row = visible[i];
                        if (!_supabaseSeenNotificationIds.Add(row.Id)) continue;
                        fresh.Add(row);
                    }

                    if (fresh.Count > 0) shouldRefresh = true;
                }
            }

            if (primeOnly)
            {
                if (shouldRefresh)
                {
                    RefreshUiAfterSupabaseStateChanged();
                }
                return;
            }

            for (var i = fresh.Count - 1; i >= 0; i--)
            {
                var row = fresh[i];
                ShowSupabaseToast(row.Title, row.Message, true);
            }

            if (shouldRefresh)
            {
                RefreshUiAfterSupabaseStateChanged();
            }
        }

        private void HandleSupabaseNotificationRealtime(string raw, string eventType)
        {
            if (!string.IsNullOrWhiteSpace(eventType) && !string.Equals(eventType, "INSERT", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            SupabaseNotificationRow row;
            SupabaseNotificationRow oldRow;
            TryParseSupabaseRealtimeRecords(raw, out row, out oldRow);
            if (row == null || string.IsNullOrWhiteSpace(row.Id)) return;

            var shouldDisplay = false;
            lock (_supabaseNotificationSync)
            {
                if (!SupabaseNotificationMatchesCurrentLocalRole(row)) return;
                if (!_supabaseSeenNotificationIds.Add(row.Id)) return;
                if (!row.Read) _supabaseUnreadNotificationsCount++;
                shouldDisplay = true;
            }
            if (!shouldDisplay) return;

            _ = Dispatcher.BeginInvoke(new Action(delegate
            {
                ShowSupabaseToast(row.Title, row.Message, true);
                RefreshUiAfterSupabaseStateChanged();
            }), DispatcherPriority.Background);
        }

        private void TryParseSupabaseRealtimeRecords<T>(string raw, out T record, out T oldRecord) where T : class
        {
            record = null;
            oldRecord = null;
            if (string.IsNullOrWhiteSpace(raw)) return;

            string recordRaw;
            if (TryExtractSupabaseRealtimeObject(raw, "record", out recordRaw) ||
                TryExtractSupabaseRealtimeObject(raw, "new", out recordRaw))
            {
                try { record = DeserializeJson<T>(recordRaw); } catch { record = null; }
            }

            string oldRecordRaw;
            if (TryExtractSupabaseRealtimeObject(raw, "old_record", out oldRecordRaw) ||
                TryExtractSupabaseRealtimeObject(raw, "old", out oldRecordRaw))
            {
                try { oldRecord = DeserializeJson<T>(oldRecordRaw); } catch { oldRecord = null; }
            }
        }

        private bool TryExtractSupabaseRealtimeObject(string raw, string propertyName, out string objectRaw)
        {
            objectRaw = null;
            if (string.IsNullOrWhiteSpace(raw) || string.IsNullOrWhiteSpace(propertyName)) return false;
            var needle = "\"" + propertyName + "\"";
            var startSearch = 0;

            while (startSearch < raw.Length)
            {
                var keyIndex = raw.IndexOf(needle, startSearch, StringComparison.OrdinalIgnoreCase);
                if (keyIndex < 0) return false;

                var colonIndex = raw.IndexOf(':', keyIndex + needle.Length);
                if (colonIndex < 0) return false;

                var i = colonIndex + 1;
                while (i < raw.Length && char.IsWhiteSpace(raw[i])) i++;
                if (i >= raw.Length) return false;
                if (raw[i] != '{')
                {
                    startSearch = i + 1;
                    continue;
                }

                var inString = false;
                var escaped = false;
                var depth = 0;
                var begin = i;
                for (; i < raw.Length; i++)
                {
                    var ch = raw[i];
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }
                    if (ch == '\\')
                    {
                        escaped = true;
                        continue;
                    }
                    if (ch == '"')
                    {
                        inString = !inString;
                        continue;
                    }
                    if (inString) continue;
                    if (ch == '{')
                    {
                        depth++;
                        continue;
                    }
                    if (ch == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            objectRaw = raw.Substring(begin, i - begin + 1);
                            return true;
                        }
                    }
                }
                return false;
            }
            return false;
        }

        private void RefreshUiAfterSupabaseStateChanged()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(RefreshUiAfterSupabaseStateChanged), DispatcherPriority.Background);
                return;
            }

            try { RefreshDeliveryAppOrdersUi(); } catch { }
            try { RenderDeliverySharedPickups(); } catch { }
            try { RefreshMenuPanelsAcrossRoles(); } catch { }
            try { RenderPickups(); } catch { }
            try { RenderReceipt(); } catch { }
        }

        private bool SupabaseNotificationMatchesCurrentLocalRole(SupabaseNotificationRow row)
        {
            if (row == null) return false;
            var local = GetCurrentLocalRoleTarget();
            if (string.IsNullOrWhiteSpace(local)) return false;

            var target = (row.RoleTarget ?? string.Empty).Trim().ToUpperInvariant();
            var rowUserId = (row.UserId ?? string.Empty).Trim();
            var localUserId = (GetCurrentSessionUserId() ?? string.Empty).Trim();
            if (string.Equals(local, "CASHIER", StringComparison.Ordinal))
            {
                return string.IsNullOrWhiteSpace(rowUserId)
                    && string.Equals(target, "CASHIER", StringComparison.Ordinal);
            }
            if (!string.IsNullOrWhiteSpace(rowUserId))
            {
                if (string.IsNullOrWhiteSpace(localUserId)) return false;
                if (!string.Equals(rowUserId, localUserId, StringComparison.Ordinal)) return false;
            }

            if (string.IsNullOrWhiteSpace(target))
            {
                return !string.IsNullOrWhiteSpace(rowUserId);
            }
            if (string.Equals(target, local, StringComparison.Ordinal)) return true;
            if (string.Equals(local, "DRIVER", StringComparison.Ordinal) && string.Equals(target, "DELIVERY", StringComparison.Ordinal)) return true;
            return false;
        }

        private string GetCurrentLocalRoleTarget()
        {
            var role = _currentSession != null ? (_currentSession.Role ?? string.Empty).Trim().ToUpperInvariant() : string.Empty;
            if (role == "TAKEAWAY") return "CASHIER";
            if (role == "DELIVERY") return "DRIVER";
            if (role == "CASHIER") return "CASHIER";
            if (role == "ADMIN_POS" || role == "ADMINPOS") return "CASHIER";
            if (role == "ADMIN") return "CASHIER";
            return string.Empty;
        }

        private void EnsureSupabaseToastUi()
        {
            if (_supabaseToastPopup != null && _supabaseToastTitleText != null && _supabaseToastMessageText != null) return;

            _supabaseToastTitleText = new TextBlock
            {
                FontWeight = FontWeights.Black,
                Foreground = Brushes.White,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            };
            _supabaseToastMessageText = new TextBlock
            {
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE5E7EB")),
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            _supabaseToastCloseButton = new Button
            {
                Content = "×",
                Width = 24,
                Height = 24,
                FontWeight = FontWeights.Black,
                FontSize = 16,
                Padding = new Thickness(0),
                Margin = new Thickness(10, 0, 0, 0),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top
            };
            _supabaseToastCloseButton.Click += delegate(object sender, RoutedEventArgs e)
            {
                e.Handled = true;
                DismissSupabaseToast(_supabaseToastNavigateToAppOrders);
            };

            var titleRow = new Grid();
            titleRow.ColumnDefinitions.Add(new ColumnDefinition());
            titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(_supabaseToastTitleText, 0);
            Grid.SetColumn(_supabaseToastCloseButton, 1);
            titleRow.Children.Add(_supabaseToastTitleText);
            titleRow.Children.Add(_supabaseToastCloseButton);

            var stack = new StackPanel();
            stack.Children.Add(titleRow);
            stack.Children.Add(_supabaseToastMessageText);

            _supabaseToastBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF1F2937")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF111827")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Width = 360,
                Child = stack,
                Effect = null
            };
            _supabaseToastBorder.Cursor = Cursors.Hand;
            _supabaseToastBorder.MouseLeftButtonUp += delegate
            {
                HandleSupabaseToastClicked();
            };

            _supabaseToastPopup = new Popup
            {
                AllowsTransparency = true,
                PlacementTarget = this,
                Placement = PlacementMode.Relative,
                StaysOpen = true,
                IsOpen = false,
                Child = _supabaseToastBorder
            };
        }

        private void ShowSupabaseToast(string title, string message, bool navigateToAppOrdersOnClick)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(delegate { ShowSupabaseToast(title, message, navigateToAppOrdersOnClick); }), DispatcherPriority.Background);
                return;
            }

            EnsureSupabaseToastUi();
            _supabaseToastNavigateToAppOrders = navigateToAppOrdersOnClick;

            _supabaseToastTitleText.Text = string.IsNullOrWhiteSpace(title) ? "إشعار جديد" : title.Trim();
            _supabaseToastMessageText.Text = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();

            var x = ActualWidth - 390;
            if (double.IsNaN(x) || double.IsInfinity(x)) x = 20;
            if (x < 20) x = 20;
            _supabaseToastPopup.HorizontalOffset = x;
            _supabaseToastPopup.VerticalOffset = 24;
            _supabaseToastPopup.IsOpen = true;
        }

        private void HandleSupabaseToastClicked()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(HandleSupabaseToastClicked), DispatcherPriority.Background);
                return;
            }

            DismissSupabaseToast(_supabaseToastNavigateToAppOrders);
        }

        private void DismissSupabaseToast(bool navigateToAppOrders)
        {
            try
            {
                if (_supabaseToastPopup != null) _supabaseToastPopup.IsOpen = false;
            }
            catch { }

            if (!navigateToAppOrders) return;

            try
            {
                if (_currentSession == null) return;
                ShowDeliveryView(_currentSession.Username);
                OpenDeliveryAppOrdersBoardView();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Supabase toast click navigation failed: " + ex.Message);
            }
        }

        private void DismissSupabaseToastFromGlobalClick()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(DismissSupabaseToastFromGlobalClick), DispatcherPriority.Background);
                return;
            }
            DismissSupabaseToast(false);
        }

        private string SerializeJson<T>(T value)
        {
            using (var ms = new MemoryStream())
            {
                var ser = new DataContractJsonSerializer(typeof(T));
                ser.WriteObject(ms, value);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private T DeserializeJson<T>(string raw) where T : class
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(raw)))
            {
                var ser = new DataContractJsonSerializer(typeof(T));
                return ser.ReadObject(ms) as T;
            }
        }

        /// <summary>فك نتيجة RPC من سوبا بيز إن كانت مرجعة كمصفوفة بعنصر واحد.</summary>
        private T UnwrapSupabaseRpcResult<T>(string raw) where T : class
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var t = typeof(T);
            try
            {
                var single = DeserializeJson<T>(raw);
                if (single != null) return single;
            }
            catch { }
            try
            {
                var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(t);
                var ser = new DataContractJsonSerializer(listType);
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(raw)))
                {
                    var list = ser.ReadObject(ms) as System.Collections.IList;
                    if (list != null && list.Count > 0) return list[0] as T;
                }
            }
            catch { }
            return null;
        }

        private string JsonEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }
}




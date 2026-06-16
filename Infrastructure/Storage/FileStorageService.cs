using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        private string ExecutableBaseDirPath { get { return Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory); } }
        private string AppDataDirPath { get { return Path.Combine(ExecutableBaseDirPath, "Fale7_POS_Data"); } }
        private string AppDataStateDirPath { get { return Path.Combine(AppDataDirPath, "state"); } }
        private string AppDataCacheDirPath { get { return Path.Combine(AppDataDirPath, "cache"); } }
        private string AppDataQueueDirPath { get { return Path.Combine(AppDataDirPath, "queue"); } }
        private string AppDataLogsDirPath { get { return Path.Combine(AppDataDirPath, "logs"); } }
        private string AppDataSyncDirPath { get { return Path.Combine(AppDataDirPath, "sync"); } }
        private string AppDataSyncTablesDirPath { get { return Path.Combine(AppDataSyncDirPath, "tables"); } }
        private string AppDataSyncExportsDirPath { get { return Path.Combine(AppDataSyncDirPath, "exports"); } }
        private string LegacyLocalAppDataDirPath
        {
            get
            {
                var localRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrWhiteSpace(localRoot)) return string.Empty;
                return Path.Combine(localRoot, "Fale7_POS");
            }
        }
        private string LegacyRoamingAppDataDirPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fale7_POS"); } }

        private string SessionFilePath { get { return Path.Combine(AppDataStateDirPath, "session.json"); } }
        private string DbPosOrdersFilePath { get { return Path.Combine(AppDataStateDirPath, "db_pos_orders.json"); } }
        private string DbMenuProductsFilePath { get { return Path.Combine(AppDataStateDirPath, "db_menu_products.json"); } }
        private string DbAddressesFilePath { get { return Path.Combine(AppDataStateDirPath, "db_addresses.json"); } }
        private string PosIdqV2FilePath { get { return Path.Combine(AppDataQueueDirPath, "pos_idq_v2.json"); } }
        private string CashierCredentialsCacheFilePath { get { return Path.Combine(AppDataStateDirPath, "cashier_credentials_cache.json"); } }
        private string AppProductsSnapshotCacheFilePath { get { return Path.Combine(AppDataCacheDirPath, "app_products_snapshot.json"); } }
        private string DbAppOrdersFilePath { get { return Path.Combine(AppDataStateDirPath, "db_app_orders.json"); } }
        private string BackendSyncManifestFilePath { get { return Path.Combine(AppDataSyncExportsDirPath, "sync_manifest.json"); } }
        private string BackendCombinedSqlExportFilePath { get { return Path.Combine(AppDataSyncExportsDirPath, "backend_full_export.sql"); } }

        private void EnsureAppDataDir()
        {
            if (!Directory.Exists(AppDataDirPath)) Directory.CreateDirectory(AppDataDirPath);
            if (!Directory.Exists(AppDataStateDirPath)) Directory.CreateDirectory(AppDataStateDirPath);
            if (!Directory.Exists(AppDataCacheDirPath)) Directory.CreateDirectory(AppDataCacheDirPath);
            if (!Directory.Exists(AppDataQueueDirPath)) Directory.CreateDirectory(AppDataQueueDirPath);
            if (!Directory.Exists(AppDataLogsDirPath)) Directory.CreateDirectory(AppDataLogsDirPath);
            if (!Directory.Exists(AppDataSyncDirPath)) Directory.CreateDirectory(AppDataSyncDirPath);
            if (!Directory.Exists(AppDataSyncTablesDirPath)) Directory.CreateDirectory(AppDataSyncTablesDirPath);
            if (!Directory.Exists(AppDataSyncExportsDirPath)) Directory.CreateDirectory(AppDataSyncExportsDirPath);
        }

        private void CleanupLegacyLocalFilesOnStartup()
        {
            EnsureAppDataDir();
            TryMigrateLegacyStorageDirectory(LegacyLocalAppDataDirPath, AppDataDirPath);
            TryMigrateLegacyStorageDirectory(LegacyRoamingAppDataDirPath, AppDataDirPath);
            TryMigrateLegacyLocalFile("session.json", SessionFilePath);
            TryMigrateLegacyLocalFile("db_pos_orders.json", DbPosOrdersFilePath);
            TryMigrateLegacyLocalFile("db_menu_products.json", DbMenuProductsFilePath);
            TryMigrateLegacyLocalFile("cashier_credentials_cache.json", CashierCredentialsCacheFilePath);
            TryMigrateLegacyLocalFile("db_app_orders.json", DbAppOrdersFilePath);
            TryMigrateLegacyLocalFile("app_products_snapshot.json", AppProductsSnapshotCacheFilePath);
            TryMigrateLegacyLocalFile("pos_idq_v2.json", PosIdqV2FilePath);
            // [PHASE 7.5] pos_shift_migration_queue.json removed — no longer used.
            TryMigrateLegacyLocalFile("pos_runtime.log", Path.Combine(AppDataLogsDirPath, "pos_runtime.log"));
        }

        private void TryMigrateLegacyStorageDirectory(string legacyRootPath, string targetRootPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(legacyRootPath) || string.IsNullOrWhiteSpace(targetRootPath)) return;

                var legacyFullPath = Path.GetFullPath(legacyRootPath);
                var targetFullPath = Path.GetFullPath(targetRootPath);

                if (string.Equals(legacyFullPath, targetFullPath, StringComparison.OrdinalIgnoreCase)) return;
                if (!Directory.Exists(legacyFullPath)) return;

                if (!Directory.Exists(targetFullPath))
                {
                    Directory.CreateDirectory(targetFullPath);
                }

                CopyLegacyDirectoryContents(legacyFullPath, targetFullPath);
            }
            catch
            {
            }
        }

        private void CopyLegacyDirectoryContents(string sourceDirPath, string targetDirPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourceDirPath) || string.IsNullOrWhiteSpace(targetDirPath)) return;
                if (!Directory.Exists(sourceDirPath)) return;

                if (!Directory.Exists(targetDirPath))
                {
                    Directory.CreateDirectory(targetDirPath);
                }

                foreach (var filePath in Directory.GetFiles(sourceDirPath))
                {
                    try
                    {
                        var fileName = Path.GetFileName(filePath);
                        if (string.IsNullOrWhiteSpace(fileName)) continue;
                        var targetFilePath = Path.Combine(targetDirPath, fileName);
                        if (!File.Exists(targetFilePath))
                        {
                            File.Copy(filePath, targetFilePath, false);
                        }
                    }
                    catch
                    {
                    }
                }

                foreach (var dirPath in Directory.GetDirectories(sourceDirPath))
                {
                    try
                    {
                        var dirName = Path.GetFileName(dirPath);
                        if (string.IsNullOrWhiteSpace(dirName)) continue;
                        var targetSubDirPath = Path.Combine(targetDirPath, dirName);
                        CopyLegacyDirectoryContents(dirPath, targetSubDirPath);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private IEnumerable<string> GetLegacyFlatFilePaths(string fileName)
        {
            var name = (fileName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name)) yield break;

            var legacyExecutable = Path.Combine(ExecutableBaseDirPath, name);
            yield return legacyExecutable;

            var localAppData = Path.Combine(AppDataDirPath, name);
            if (!string.Equals(
                Path.GetFullPath(legacyExecutable),
                Path.GetFullPath(localAppData),
                StringComparison.OrdinalIgnoreCase))
            {
                yield return localAppData;
            }

            var roaming = Path.Combine(LegacyRoamingAppDataDirPath, name);
            if (!string.Equals(
                Path.GetFullPath(legacyExecutable),
                Path.GetFullPath(roaming),
                StringComparison.OrdinalIgnoreCase))
            {
                yield return roaming;
            }
        }

        private void TryMigrateLegacyLocalFile(string legacyFileName, string targetPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(targetPath)) return;
                var targetFullPath = Path.GetFullPath(targetPath);
                foreach (var legacyPath in GetLegacyFlatFilePaths(legacyFileName))
                {
                    if (string.IsNullOrWhiteSpace(legacyPath)) continue;

                    var legacyFullPath = Path.GetFullPath(legacyPath);
                    if (string.Equals(legacyFullPath, targetFullPath, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!File.Exists(legacyFullPath)) continue;

                    var targetDir = Path.GetDirectoryName(targetFullPath);
                    if (!string.IsNullOrWhiteSpace(targetDir) && !Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    if (!File.Exists(targetFullPath))
                    {
                        File.Move(legacyFullPath, targetFullPath);
                        return;
                    }

                    File.Delete(legacyFullPath);
                }
            }
            catch
            {
            }
        }

        private SessionRecord LoadSessionFromDisk()
        {
            var session = ReadJson<SessionRecord>(SessionFilePath);
            if (session == null) return null;

            if (string.IsNullOrWhiteSpace(session.SessionToken)
                && !string.IsNullOrWhiteSpace(session.ProtectedSessionToken))
            {
                session.SessionToken = UnprotectLocalSecret(session.ProtectedSessionToken);
            }

            session.ProtectedSessionToken = string.Empty;
            return session;
        }

        private void SaveSessionToDisk(SessionRecord session)
        {
            if (session == null)
            {
                DeleteSessionFile();
                return;
            }

            var token = (session.SessionToken ?? string.Empty).Trim();
            var payload = new SessionRecord
            {
                Username = session.Username,
                Role = session.Role,
                UserId = session.UserId,
                BackendRole = session.BackendRole,
                SessionExpiresAtMillis = session.SessionExpiresAtMillis,
                SessionToken = string.Empty,
                ProtectedSessionToken = ProtectLocalSecret(token)
            };

            WriteJson(SessionFilePath, payload);
        }

        private void DeleteSessionFile()
        {
            try
            {
                if (File.Exists(SessionFilePath)) File.Delete(SessionFilePath);
            }
            catch
            {
            }
        }

        private LocalCashierCredentialsCacheRecord LoadCashierCredentialsCacheFromDisk()
        {
            var cache = ReadJson<LocalCashierCredentialsCacheRecord>(CashierCredentialsCacheFilePath);
            if (cache == null) cache = new LocalCashierCredentialsCacheRecord();
            if (cache.Entries == null) cache.Entries = new List<LocalCashierCredentialRecord>();
            if (cache.Version <= 0) cache.Version = 1;
            return cache;
        }

        private void SaveCashierCredentialsCacheToDisk(LocalCashierCredentialsCacheRecord cache)
        {
            if (cache == null) cache = new LocalCashierCredentialsCacheRecord();
            if (cache.Entries == null) cache.Entries = new List<LocalCashierCredentialRecord>();
            if (cache.Version <= 0) cache.Version = 1;
            cache.UpdatedAtMillis = NowUnixMs();
            WriteJson(CashierCredentialsCacheFilePath, cache);
        }

        private void LoadTakeawayStateFromDisk()
        {
            _takeawayState = ReadJson<TakeawayStateRecord>(DbPosOrdersFilePath) ?? new TakeawayStateRecord();
        }

        private void SaveTakeawayStateToDisk()
        {
            NormalizeTakeawayState();
            WriteJson(DbPosOrdersFilePath, _takeawayState);
        }

        private void LoadMenuStateFromDisk()
        {
            if (_menu.Count > 0) return;
            var state = ReadJson<MenuStateRecord>(DbMenuProductsFilePath);
            if (state == null || state.Categories == null || state.Categories.Count == 0) return;

            _menu.Clear();
            for (int i = 0; i < state.Categories.Count; i++)
            {
                var cat = state.Categories[i];
                if (cat == null) continue;
                _menu.Add(cat);
            }

            NormalizeMenuState();
        }

        private void SaveMenuStateToDisk()
        {
            NormalizeMenuState();
            var state = new MenuStateRecord
            {
                Categories = new List<MenuCategory>(_menu)
            };
            WriteJson(DbMenuProductsFilePath, state);
        }

        private bool LoadAppProductsSnapshotFromDisk()
        {
            var snapshot = ReadJson<AppProductsSnapshotRecord>(AppProductsSnapshotCacheFilePath);
            if (snapshot == null || snapshot.Products == null || snapshot.Products.Count == 0) return false;
            return ApplyAppProductsSnapshot(snapshot.Products);
        }

        private void SaveAppProductsSnapshotToDisk()
        {
            if (_appProductsCatalog == null || _appProductsCatalog.Count == 0) return;
            var snapshot = new AppProductsSnapshotRecord
            {
                Source = "POS_LOCAL_CACHE",
                GeneratedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Products = new List<AppProductSnapshotRecord>(_appProductsCatalog)
            };
            WriteJson(AppProductsSnapshotCacheFilePath, snapshot);
        }

        private void PersistMenuState()
        {
            SaveMenuStateToDisk();
            QueueSupabasePosMenuPushFromUi();
        }

        private T ReadJson<T>(string path) where T : class
        {
            try
            {
                if (!File.Exists(path)) return null;
                using (var fs = File.OpenRead(path))
                {
                    var ser = new DataContractJsonSerializer(typeof(T));
                    return ser.ReadObject(fs) as T;
                }
            }
            catch
            {
                return null;
            }
        }

        private void WriteJson<T>(string path, T data) where T : class
        {
            try
            {
                EnsureAppDataDir();
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var raw = SerializeJson(data);
                if (string.IsNullOrWhiteSpace(raw)) return;
                var pretty = FormatJsonPretty(raw);
                File.WriteAllText(path, pretty, new UTF8Encoding(false));
            }
            catch
            {
            }
        }

        private static string FormatJsonPretty(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw ?? string.Empty;

            var sb = new StringBuilder(raw.Length + Math.Min(raw.Length, 2048));
            var indent = 0;
            var inString = false;
            var escaping = false;

            for (var i = 0; i < raw.Length; i++)
            {
                var ch = raw[i];
                if (escaping)
                {
                    sb.Append(ch);
                    escaping = false;
                    continue;
                }

                if (ch == '\\')
                {
                    sb.Append(ch);
                    if (inString) escaping = true;
                    continue;
                }

                if (ch == '"')
                {
                    sb.Append(ch);
                    inString = !inString;
                    continue;
                }

                if (inString)
                {
                    sb.Append(ch);
                    continue;
                }

                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        sb.AppendLine();
                        AppendJsonIndent(sb, ++indent);
                        break;
                    case '}':
                    case ']':
                        sb.AppendLine();
                        AppendJsonIndent(sb, Math.Max(0, --indent));
                        sb.Append(ch);
                        break;
                    case ',':
                        sb.Append(ch);
                        sb.AppendLine();
                        AppendJsonIndent(sb, indent);
                        break;
                    case ':':
                        sb.Append(": ");
                        break;
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }

            return sb.ToString();
        }

        private static void AppendJsonIndent(StringBuilder sb, int indent)
        {
            for (var i = 0; i < indent; i++) sb.Append("  ");
        }

        private void NormalizeTakeawayState()
        {
            if (_takeawayState == null) _takeawayState = new TakeawayStateRecord();
            if (_takeawayState.OpenOrders == null) _takeawayState.OpenOrders = new List<TakeawayOrderRecord>();
            if (_takeawayState.Pickups == null) _takeawayState.Pickups = new List<TakeawayPickupRecord>();
            if (_takeawayState.ShiftRecords == null) _takeawayState.ShiftRecords = new List<PosShiftOrderRecord>();
            for (int i = 0; i < _takeawayState.OpenOrders.Count; i++)
            {
                var o = _takeawayState.OpenOrders[i];
                if (o == null) { _takeawayState.OpenOrders.RemoveAt(i--); continue; }
                if (o.Items == null) o.Items = new List<TakeawayLineRecord>();
                for (int j = 0; j < o.Items.Count; j++)
                {
                    var item = o.Items[j];
                    if (item == null) continue;
                    if (item.OptionIds == null) item.OptionIds = new List<string>();
                }
            }

            for (int i = _takeawayState.ShiftRecords.Count - 1; i >= 0; i--)
            {
                var entry = _takeawayState.ShiftRecords[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.BackendOrderId))
                {
                    _takeawayState.ShiftRecords.RemoveAt(i);
                    continue;
                }
                if (entry.Total < 0) entry.Total = 0;
                if (entry.Subtotal < 0) entry.Subtotal = 0;
                if (entry.DeliveryFee < 0) entry.DeliveryFee = 0;
                if (entry.CreatedAtMillis <= 0) entry.CreatedAtMillis = NowUnixMs();
                if (entry.FinalizedAtMillis <= 0) entry.FinalizedAtMillis = entry.CreatedAtMillis;
            }

            var currentExists = false;
            for (int i = 0; i < _takeawayState.OpenOrders.Count; i++)
            {
                if (_takeawayState.OpenOrders[i].Id == _takeawayState.CurrentOrderId) { currentExists = true; break; }
            }
            if (!currentExists) _takeawayState.CurrentOrderId = _takeawayState.OpenOrders.Count > 0 ? _takeawayState.OpenOrders[0].Id : null;

            if (_takeawayState.Pickups.Count == 0) _takeawayState.ActivePickupId = null;
            else
            {
                var pickExists = false;
                for (int i = 0; i < _takeawayState.Pickups.Count; i++)
                {
                    if (_takeawayState.Pickups[i] != null && _takeawayState.Pickups[i].Id == _takeawayState.ActivePickupId) { pickExists = true; break; }
                }
                if (!pickExists) _takeawayState.ActivePickupId = _takeawayState.Pickups[0].Id;
            }
        }

        private void NormalizeMenuState()
        {
            for (int i = _menu.Count - 1; i >= 0; i--)
            {
                var cat = _menu[i];
                if (cat == null || string.IsNullOrWhiteSpace(cat.Name))
                {
                    _menu.RemoveAt(i);
                    continue;
                }
                cat.Name = cat.Name.Trim();
                if (cat.Items == null) cat.Items = new List<MenuItemDef>();
                for (int j = cat.Items.Count - 1; j >= 0; j--)
                {
                    var item = cat.Items[j];
                    if (item == null || (string.IsNullOrWhiteSpace(item.Name) && string.IsNullOrWhiteSpace(item.PosDisplayName)))
                    {
                        cat.Items.RemoveAt(j);
                        continue;
                    }
                    item.Name = item.Name.Trim();
                    if (!string.IsNullOrWhiteSpace(item.PosDisplayName))
                    {
                        item.PosDisplayName = item.PosDisplayName.Trim();
                        if (string.IsNullOrWhiteSpace(item.Name)) item.Name = item.PosDisplayName;
                    }
                    else
                    {
                        item.PosDisplayName = item.Name;
                    }
                    if (item.Price < 0) item.Price = 0;
                    if (item.FixedOptionIds == null) item.FixedOptionIds = new List<string>();
                    if (string.IsNullOrWhiteSpace(item.SourceProductId)) item.SourceProductId = item.ProductId;
                    if (string.IsNullOrWhiteSpace(item.InventoryStatus)) item.InventoryStatus = "available";
                    item.PosPosition = j;
                }
            }

            if (_menu.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(_activeCategory)) _activeCategory = _menu[0].Name;
                if (string.IsNullOrWhiteSpace(_deliveryActiveCategory)) _deliveryActiveCategory = _menu[0].Name;

                var foundActive = false;
                for (int i = 0; i < _menu.Count; i++)
                {
                    if (string.Equals(_menu[i].Name, _activeCategory, StringComparison.Ordinal))
                    {
                        foundActive = true;
                        break;
                    }
                }
                if (!foundActive) _activeCategory = _menu[0].Name;

                var foundDeliveryActive = false;
                for (int i = 0; i < _menu.Count; i++)
                {
                    if (string.Equals(_menu[i].Name, _deliveryActiveCategory, StringComparison.Ordinal))
                    {
                        foundDeliveryActive = true;
                        break;
                    }
                }
                if (!foundDeliveryActive) _deliveryActiveCategory = _menu[0].Name;
            }
        }

        [DataContract]
        private class SessionRecord
        {
            [DataMember(Name = "username")] public string Username { get; set; }
            [DataMember(Name = "role")] public string Role { get; set; }
            [DataMember(Name = "user_id", EmitDefaultValue = false)] public string UserId { get; set; }
            [DataMember(Name = "backend_role", EmitDefaultValue = false)] public string BackendRole { get; set; }
            [DataMember(Name = "session_expires_at_millis", EmitDefaultValue = false)] public long SessionExpiresAtMillis { get; set; }
            [DataMember(Name = "session_token", EmitDefaultValue = false)] public string SessionToken { get; set; }
            [DataMember(Name = "session_token_protected", EmitDefaultValue = false)] public string ProtectedSessionToken { get; set; }
        }

        [DataContract]
        private class LocalCashierCredentialsCacheRecord
        {
            [DataMember(Name = "version", EmitDefaultValue = false)] public int Version { get; set; }
            [DataMember(Name = "updated_at_millis", EmitDefaultValue = false)] public long UpdatedAtMillis { get; set; }
            [DataMember(Name = "entries")] public List<LocalCashierCredentialRecord> Entries { get; set; }
        }

        [DataContract]
        private class LocalCashierCredentialRecord
        {
            [DataMember(Name = "user_id", EmitDefaultValue = false)] public string UserId { get; set; }
            [DataMember(Name = "username", EmitDefaultValue = false)] public string Username { get; set; }
            [DataMember(Name = "display_name", EmitDefaultValue = false)] public string DisplayName { get; set; }
            [DataMember(Name = "phone", EmitDefaultValue = false)] public string Phone { get; set; }
            [DataMember(Name = "email", EmitDefaultValue = false)] public string Email { get; set; }
            [DataMember(Name = "role", EmitDefaultValue = false)] public string Role { get; set; }
            [DataMember(Name = "is_active", EmitDefaultValue = false)] public bool IsActive { get; set; }
            [DataMember(Name = "password_hash", EmitDefaultValue = false)] public string PasswordHash { get; set; }
            [DataMember(Name = "password_secret_protected", EmitDefaultValue = false)] public string PasswordSecretProtected { get; set; }
            [DataMember(Name = "user_updated_at", EmitDefaultValue = false)] public string UserUpdatedAt { get; set; }
            [DataMember(Name = "secret_updated_at", EmitDefaultValue = false)] public string SecretUpdatedAt { get; set; }
            [DataMember(Name = "dirty", EmitDefaultValue = false)] public bool Dirty { get; set; }
            [DataMember(Name = "last_synced_at", EmitDefaultValue = false)] public string LastSyncedAt { get; set; }
        }

        [DataContract]
        private class TakeawayStateRecord
        {
            [DataMember(Name = "takeaway_open_orders")] public List<TakeawayOrderRecord> OpenOrders { get; set; }
            [DataMember(Name = "takeaway_seq")] public int OrderSequence { get; set; }
            [DataMember(Name = "takeaway_current_id")] public string CurrentOrderId { get; set; }
            [DataMember(Name = "takeaway_pickups")] public List<TakeawayPickupRecord> Pickups { get; set; }
            [DataMember(Name = "takeaway_pick_seq")] public int PickupSequence { get; set; }
            [DataMember(Name = "takeaway_pick_active")] public string ActivePickupId { get; set; }
            [DataMember(Name = "takeaway_shift_records")] public List<PosShiftOrderRecord> ShiftRecords { get; set; }
        }

        [DataContract]
        private class TakeawayOrderRecord
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "idempotency_key", EmitDefaultValue = false)] public string IdempotencyKey { get; set; }
            [DataMember(Name = "no")] public int No { get; set; }
            [DataMember(Name = "createdAt")] public long CreatedAt { get; set; }
            [DataMember(Name = "pickupId", EmitDefaultValue = false)] public string PickupId { get; set; }
            [DataMember(Name = "items")] public List<TakeawayLineRecord> Items { get; set; }
        }

        [DataContract]
        private class TakeawayLineRecord
        {
            [DataMember(Name = "key")] public string Key { get; set; }
            [DataMember(Name = "desc")] public string Desc { get; set; }
            [DataMember(Name = "qty")] public int Qty { get; set; }
            [DataMember(Name = "price")] public int Price { get; set; }
            [DataMember(Name = "product_id", EmitDefaultValue = false)] public string ProductId { get; set; }
            [DataMember(Name = "source_product_id", EmitDefaultValue = false)] public string SourceProductId { get; set; }
            [DataMember(Name = "option_ids", EmitDefaultValue = false)] public List<string> OptionIds { get; set; }
        }

        [DataContract]
        private class TakeawayPickupRecord
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "idempotency_key", EmitDefaultValue = false)] public string IdempotencyKey { get; set; }
            [DataMember(Name = "no")] public int No { get; set; }
            [DataMember(Name = "createdAt")] public long CreatedAt { get; set; }
            [DataMember(Name = "phone")] public string Phone { get; set; }
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "kitchen_printed", EmitDefaultValue = false)] public bool KitchenPrinted { get; set; }
            [DataMember(Name = "kitchen_printed_at", EmitDefaultValue = false)] public long? KitchenPrintedAt { get; set; }
            [DataMember(Name = "receipt_printed", EmitDefaultValue = false)] public bool ReceiptPrinted { get; set; }
            [DataMember(Name = "receipt_printed_at", EmitDefaultValue = false)] public long? ReceiptPrintedAt { get; set; }
        }

        [DataContract]
        private class PosShiftOrderRecord
        {
            [DataMember(Name = "backend_order_id")] public string BackendOrderId { get; set; }
            [DataMember(Name = "source_kind")] public string SourceKind { get; set; }
            [DataMember(Name = "channel_kind")] public string ChannelKind { get; set; }
            [DataMember(Name = "order_no")] public int OrderNo { get; set; }
            [DataMember(Name = "display_ref")] public string DisplayRef { get; set; }
            [DataMember(Name = "customer_name")] public string CustomerName { get; set; }
            [DataMember(Name = "customer_phone")] public string CustomerPhone { get; set; }
            [DataMember(Name = "subtotal")] public int Subtotal { get; set; }
            [DataMember(Name = "delivery_fee")] public int DeliveryFee { get; set; }
            [DataMember(Name = "total")] public int Total { get; set; }
            [DataMember(Name = "created_at_millis")] public long CreatedAtMillis { get; set; }
            [DataMember(Name = "finalized_at_millis")] public long FinalizedAtMillis { get; set; }
            [DataMember(Name = "cashier_user_id", EmitDefaultValue = false)] public string CashierUserId { get; set; }
            [DataMember(Name = "cashier_username", EmitDefaultValue = false)] public string CashierUsername { get; set; }
            [DataMember(Name = "cashier_role", EmitDefaultValue = false)] public string CashierRole { get; set; }
        }

        [DataContract]
        private class MenuStateRecord
        {
            [DataMember(Name = "categories")] public List<MenuCategory> Categories { get; set; }
        }

        [DataContract]
        private class MenuCategory
        {
            [DataMember(Name = "remote_id", EmitDefaultValue = false)] public string RemoteId { get; set; }
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "items")] public List<MenuItemDef> Items { get; set; }
        }

        [DataContract]
        private class MenuItemDef
        {
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "price")] public int Price { get; set; }
            [DataMember(Name = "product_id", EmitDefaultValue = false)] public string ProductId { get; set; }
            [DataMember(Name = "source_product_id", EmitDefaultValue = false)] public string SourceProductId { get; set; }
            [DataMember(Name = "fixed_option_ids", EmitDefaultValue = false)] public List<string> FixedOptionIds { get; set; }
            [DataMember(Name = "pos_display_name", EmitDefaultValue = false)] public string PosDisplayName { get; set; }
            [DataMember(Name = "remote_id", EmitDefaultValue = false)] public string RemoteId { get; set; }
            [DataMember(Name = "pos_position", EmitDefaultValue = false)] public int PosPosition { get; set; }
            [DataMember(Name = "inventory_status", EmitDefaultValue = false)] public string InventoryStatus { get; set; }
            [DataMember(Name = "inventory_remaining", EmitDefaultValue = false)] public double? InventoryRemaining { get; set; }
        }
    }
}

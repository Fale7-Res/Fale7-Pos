using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        private readonly object _posAddressCacheSync = new object();
        private bool _posAddressCacheLoaded = false;
        private List<SupabasePosRegisteredAddressRow> _posAddressCache = new List<SupabasePosRegisteredAddressRow>();

        // Types SupabasePosRegisteredAddressRow and SupabasePosRegisteredAddressInsertRow
        // are defined in DeliveryStateManager.cs (internal to MainWindow partial class).
        // No duplicate definition needed here.

        // [PHASE 7.5 PURE CLOUD] No offline address mutation queue.
        // All address writes go directly to Supabase REST API.
        // The cache is in-memory only, bootstrapped from Realtime events + initial pull.

        private void EnsurePosAddressCacheLoaded()
        {
            lock (_posAddressCacheSync)
            {
                if (_posAddressCacheLoaded) return;
                _posAddressCacheLoaded = true;
            }
        }

        private void PersistPosAddressCacheNoLock()
        {
            // [PHASE 7.5 PURE CLOUD] Bypass disk flush entirely.
            return;
        }

        private List<SupabasePosRegisteredAddressRow> GetPosAddressCacheByPhone(string phone)
        {
            EnsurePosAddressCacheLoaded();
            lock (_posAddressCacheSync)
            {
                var results = new List<SupabasePosRegisteredAddressRow>();
                for (int i = 0; i < _posAddressCache.Count; i++)
                {
                    var row = _posAddressCache[i];
                    if (row != null && string.Equals(row.Phone, phone, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(row);
                    }
                }
                results.Sort(delegate (SupabasePosRegisteredAddressRow a, SupabasePosRegisteredAddressRow b)
                {
                    var aTime = a == null ? string.Empty : (a.RegisteredAt ?? string.Empty);
                    var bTime = b == null ? string.Empty : (b.RegisteredAt ?? string.Empty);
                    return string.Compare(bTime, aTime, StringComparison.Ordinal);
                });
                return results;
            }
        }

        private List<SupabasePosRegisteredAddressRow> GetAllPosAddressCache()
        {
            EnsurePosAddressCacheLoaded();
            lock (_posAddressCacheSync)
            {
                return new List<SupabasePosRegisteredAddressRow>(_posAddressCache);
            }
        }

        private void ApplySupabaseRealtimeRegisteredAddressChange(string eventType, SupabasePosRegisteredAddressRow record, SupabasePosRegisteredAddressRow oldRecord)
        {
            EnsurePosAddressCacheLoaded();
            lock (_posAddressCacheSync)
            {
                if (string.Equals(eventType, "DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    if (oldRecord != null)
                    {
                        _posAddressCache.RemoveAll(r => r.Id == oldRecord.Id);
                    }
                }
                else if (string.Equals(eventType, "INSERT", StringComparison.OrdinalIgnoreCase))
                {
                    if (record != null)
                    {
                        _posAddressCache.RemoveAll(r => r.Id == record.Id);
                        _posAddressCache.Add(record);
                    }
                }
                else if (string.Equals(eventType, "UPDATE", StringComparison.OrdinalIgnoreCase))
                {
                    if (record != null)
                    {
                        var idx = _posAddressCache.FindIndex(r => r.Id == record.Id);
                        if (idx >= 0) _posAddressCache[idx] = record;
                        else _posAddressCache.Add(record);
                    }
                }
                PersistPosAddressCacheNoLock();
            }
        }

        private async Task BootstrapPosAddressCacheAsync(SupabaseClientConfigRecord cfg)
        {
            EnsurePosAddressCacheLoaded();
            try
            {
                var url = cfg.Url + "/rest/v1/pos_registered_addresses?select=id,phone,label,address_line,registered_at,created_at,updated_at,device_id&order=registered_at.desc";
                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    ApplySupabaseRestHeaders(req, cfg, true);
                    using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs * 2)))
                    using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                    {
                        if (resp.IsSuccessStatusCode)
                        {
                            var raw = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                            var rows = DeserializeJson<List<SupabasePosRegisteredAddressRow>>(raw);
                            if (rows != null)
                            {
                                lock (_posAddressCacheSync)
                                {
                                    _posAddressCacheLoaded = true;
                                    _posAddressCache = rows;
                                    PersistPosAddressCacheNoLock();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BootstrapPosAddressCacheAsync error: " + ex.Message);
            }
        }

        // ================================================================
        // Data Types — relocated here after AddressSyncService cleanup.
        // These are used across SupabaseClientFactory.cs, DeliveryMainView.cs,
        // AppOrderSyncService.cs, and DeliveryStateManager.cs
        // ================================================================

        [DataContract]
        private sealed class DeliveryAddressRecord
        {
            [DataMember(Name = "id", EmitDefaultValue = false)] public string Id { get; set; }
            [DataMember(Name = "district", EmitDefaultValue = false)] public string District { get; set; }
            [DataMember(Name = "block", EmitDefaultValue = false)] public string Block { get; set; }
            [DataMember(Name = "street", EmitDefaultValue = false)] public string Street { get; set; }
            [DataMember(Name = "building", EmitDefaultValue = false)] public string Building { get; set; }
            [DataMember(Name = "apartment", EmitDefaultValue = false)] public string Apartment { get; set; }
            [DataMember(Name = "floor", EmitDefaultValue = false)] public string Floor { get; set; }
            [DataMember(Name = "note", EmitDefaultValue = false)] public string Note { get; set; }
        }

        [DataContract]
        private sealed class DeliverySavedAddressRecord
        {
            [DataMember(Name = "id", EmitDefaultValue = false)] public string Id { get; set; }
            [DataMember(Name = "district", EmitDefaultValue = false)] public string District { get; set; }
            [DataMember(Name = "block", EmitDefaultValue = false)] public string Block { get; set; }
            [DataMember(Name = "street", EmitDefaultValue = false)] public string Street { get; set; }
            [DataMember(Name = "building", EmitDefaultValue = false)] public string Building { get; set; }
            [DataMember(Name = "apartment", EmitDefaultValue = false)] public string Apartment { get; set; }
            [DataMember(Name = "floor", EmitDefaultValue = false)] public string Floor { get; set; }
            [DataMember(Name = "note", EmitDefaultValue = false)] public string Note { get; set; }
        }

        [DataContract]
        private sealed class SupabaseHoodRow
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "fee")] public double Fee { get; set; }
        }
    }
}

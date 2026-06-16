using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        // ──────────────────────────────────────────────────────────────────────
        // UNIQUE FIELDS — not defined in any other partial file
        // ──────────────────────────────────────────────────────────────────────
        private int _deliveryNotesSelIndex;
        private readonly List<string> _deliveryNotesFiltered = new List<string>();
        private readonly List<DeliveryDistrictFeeRecord> _deliveryDistrictCatalog = new List<DeliveryDistrictFeeRecord>();

        [DataContract]
        private sealed class DeliveryDistrictFeeRecord
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "fee")] public int Fee { get; set; }
        }

        // ──────────────────────────────────────────────────────────────────────
        // UNIQUE METHODS — not defined in any other partial file
        // ──────────────────────────────────────────────────────────────────────

        private bool IsSupabaseAddressId(string value)
        {
            var v = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(v)) return false;
            int parsed;
            return int.TryParse(v, out parsed) && parsed > 0;
        }

        private string DeliveryAddressSignature(DeliverySavedAddressRecord address)
        {
            if (address == null) return string.Empty;
            return (address.District ?? string.Empty) + "|"
                + (address.Block ?? string.Empty) + "|"
                + (address.Street ?? string.Empty) + "|"
                + (address.Building ?? string.Empty) + "|"
                + (address.Apartment ?? string.Empty) + "|"
                + (address.Floor ?? string.Empty) + "|"
                + (address.Note ?? string.Empty);
        }

        private DeliverySavedAddressRecord BuildDeliverySavedAddressFromAddress(DeliveryAddressRecord address)
        {
            if (address == null) return null;
            return new DeliverySavedAddressRecord
            {
                Id = address.Id, District = address.District, Block = address.Block,
                Street = address.Street, Building = address.Building, Apartment = address.Apartment,
                Floor = address.Floor, Note = address.Note
            };
        }

        private async Task<List<SupabaseHoodRow>> FetchSupabaseHoodsAsync(SupabaseClientConfigRecord cfg)
        {
            if (cfg == null) return new List<SupabaseHoodRow>();
            try
            {
                var url = cfg.Url + "/rest/v1/hoods?select=id,name,fee&order=name.asc";
                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    ApplySupabaseRestHeaders(req, cfg, true);
                    using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(cfg.RestTimeoutMs)))
                    using (var resp = await SupabaseHttpClient.SendAsync(req, cts.Token).ConfigureAwait(false))
                    {
                        var raw = await SafeReadHttpContentAsync(resp).ConfigureAwait(false);
                        if (!resp.IsSuccessStatusCode) return new List<SupabaseHoodRow>();
                        return DeserializeJson<List<SupabaseHoodRow>>(raw) ?? new List<SupabaseHoodRow>();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FetchSupabaseHoodsAsync error: " + ex.Message);
                return new List<SupabaseHoodRow>();
            }
        }

        private Brush GetDeliveryQueueDelayBackground(DeliveryOrderRecord order)
        {
            if (order == null) return Brushes.White;
            var mins = DeliveryMinutesSince(order.CreatedAt);
            if (mins >= 30) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFCCCC"));
            if (mins >= 15) return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFCC"));
            return Brushes.White;
        }

        private string GetSelectedDeliveryDistrict()
        {
            return string.Empty;
        }

        private bool IsDeliveryDistrictPickerOpen()
        {
            return false;
        }

        private void RebuildDeliveryDistrictComboItems(string selected) { }

        private void RebuildDeliveryDistrictPickerList(string search) { }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        private readonly Dictionary<string, List<DeliverySavedAddressRecord>> _deliveryAddressesDb = new Dictionary<string, List<DeliverySavedAddressRecord>>(StringComparer.Ordinal);
        private readonly List<string> _deliveryNotesFiltered = new List<string>();
        private bool _deliveryStateLoaded;
        private bool _deliveryFormShowSavedAddresses;
        private string _deliveryFormSelectedAddressId;
        private string _deliveryFormPrefillOrderId;
        private string _deliveryFormAppOrderId;
        private TextBox _deliveryFormSuggestTargetTextBox;
        private readonly List<string> _deliveryFormSuggestValues = new List<string>();
        private readonly List<DeliveryDistrictFeeRecord> _deliveryDistrictCatalog = new List<DeliveryDistrictFeeRecord>();
        private readonly List<DeliveryDistrictFeeRecord> _deliveryDistrictFiltered = new List<DeliveryDistrictFeeRecord>();
        private int _deliveryFormSuggestSelIndex = -1;
        private int _deliveryNotesSelIndex = -1;
        private bool _deliveryDistrictComboRebuildInProgress;
        private int _deliveryFormBackendRefreshStamp;

        // Districts source of truth is backend table public.hoods.
        // No static local district list should be used.
        private static readonly DeliveryDistrictFeeRecord[] DeliveryDistrictFallbacks = new DeliveryDistrictFeeRecord[0];

        private string DeliveryStateFilePath { get { return Path.Combine(AppDataStateDirPath, "delivery_state.json"); } }
        private string DeliveryAddressesSqlExportFilePath { get { return Path.Combine(AppDataStateDirPath, "customer_addresses_export.sql"); } }

        private void EnsureDeliveryStateLoaded()
        {
            if (_deliveryStateLoaded) return;
            LoadDeliveryStateFromDisk();
            NormalizeDeliveryState();
            _deliveryStateLoaded = true;
        }

        private void LoadDeliveryStateFromDisk()
        {
            var state = ReadJson<DeliveryStateRecord>(DeliveryStateFilePath) ?? new DeliveryStateRecord();
            _deliveryOrderSequence = state.OrderSequence;

            _deliveryOrders.Clear();
            if (state.OpenOrders != null)
            {
                for (int i = 0; i < state.OpenOrders.Count; i++)
                {
                    if (state.OpenOrders[i] != null) _deliveryOrders.Add(state.OpenOrders[i]);
                }
            }

            _deliveryShiftOrders.Clear();
            if (state.ShiftOrders != null)
            {
                for (int i = 0; i < state.ShiftOrders.Count; i++)
                {
                    if (state.ShiftOrders[i] != null) _deliveryShiftOrders.Add(state.ShiftOrders[i]);
                }
            }

            _deliveryNotesHistory.Clear();
            if (state.NotesHistory != null)
            {
                for (int i = 0; i < state.NotesHistory.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(state.NotesHistory[i])) _deliveryNotesHistory.Add(state.NotesHistory[i]);
                }
            }

            _deliveryDistrictCatalog.Clear();
            if (state.DistrictCatalog != null)
            {
                for (int i = 0; i < state.DistrictCatalog.Count; i++)
                {
                    var d = state.DistrictCatalog[i];
                    if (d == null || string.IsNullOrWhiteSpace(d.Name)) continue;
                    _deliveryDistrictCatalog.Add(new DeliveryDistrictFeeRecord
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Fee = d.Fee < 0 ? 0 : d.Fee
                    });
                }
            }

            _deliveryDrivers.Clear();
            if (state.Drivers != null)
            {
                for (int i = 0; i < state.Drivers.Count; i++)
                {
                    var driver = state.Drivers[i];
                    if (driver == null || string.IsNullOrWhiteSpace(driver.Id)) continue;
                    _deliveryDrivers.Add(new DeliveryDriverRecord
                    {
                        Id = driver.Id,
                        Name = driver.Name,
                        Phone = driver.Phone,
                        AppUserId = driver.AppUserId
                    });
                }
            }

            _deliveryAppDeliveryOrders.Clear();
            if (state.AppDeliveryOrders != null)
            {
                for (int i = 0; i < state.AppDeliveryOrders.Count; i++)
                {
                    var row = state.AppDeliveryOrders[i];
                    if (row == null || string.IsNullOrWhiteSpace(row.Id)) continue;
                    var mapped = new AppDeliveryOrderRecord
                    {
                        Id = row.Id,
                        CustomerUserId = row.CustomerUserId,
                        AddressId = row.AddressId,
                        No = row.No,
                        Name = row.Name,
                        Phone = row.Phone,
                        District = row.District,
                        AddressText = row.AddressText,
                        Address = row.Address != null ? new DeliveryAddressRecord
                        {
                            District = row.Address.District,
                            Block = row.Address.Block,
                            Street = row.Address.Street,
                            Building = row.Address.Building,
                            Apartment = row.Address.Apartment,
                            Floor = row.Address.Floor,
                            Note = row.Address.Note
                        } : ParseAppDeliveryAddressText(row.District, row.AddressText),
                        CreatedAt = row.CreatedAt,
                        Subtotal = row.Subtotal,
                        DeliveryFee = row.DeliveryFee < 0 ? 0 : row.DeliveryFee,
                        Total = row.Total,
                        Status = ParseCachedAppDeliveryStatus(row.Status),
                        KitchenPrinted = row.KitchenPrinted,
                        DriverReceiptPrinted = row.DriverReceiptPrinted,
                        Driver = row.Driver == null ? null : new DeliveryDriverRecord
                        {
                            Id = row.Driver.Id,
                            Name = row.Driver.Name,
                            Phone = row.Driver.Phone,
                            AppUserId = row.Driver.AppUserId
                        },
                        Items = new List<AppOrderItemRecord>()
                    };
                    if (row.Items != null)
                    {
                        for (int j = 0; j < row.Items.Count; j++)
                        {
                            var it = row.Items[j];
                            if (it == null) continue;
                            mapped.Items.Add(new AppOrderItemRecord
                            {
                                Name = it.Name,
                                Qty = it.Qty,
                                Price = it.Price
                            });
                        }
                    }
                    mapped.Subtotal = GetStoredAppDeliveryOrderSubtotal(mapped);
                    mapped.Total = GetStoredAppDeliveryOrderTotal(mapped);
                    _deliveryAppDeliveryOrders.Add(mapped);
                }
            }

            _deliveryAppPickupOrders.Clear();
            if (state.AppPickupOrders != null)
            {
                for (int i = 0; i < state.AppPickupOrders.Count; i++)
                {
                    var row = state.AppPickupOrders[i];
                    if (row == null || string.IsNullOrWhiteSpace(row.Id)) continue;
                    var mapped = new AppPickupOrderRecord
                    {
                        Id = row.Id,
                        CustomerUserId = row.CustomerUserId,
                        No = row.No,
                        Name = row.Name,
                        Phone = row.Phone,
                        District = row.District,
                        AddressText = row.AddressText,
                        CreatedAt = row.CreatedAt,
                        Status = ParseCachedAppPickupStatus(row.Status),
                        PartnerReceiptPrinted = row.PartnerReceiptPrinted,
                        Items = new List<AppOrderItemRecord>()
                    };
                    if (row.Items != null)
                    {
                        for (int j = 0; j < row.Items.Count; j++)
                        {
                            var it = row.Items[j];
                            if (it == null) continue;
                            mapped.Items.Add(new AppOrderItemRecord
                            {
                                Name = it.Name,
                                Qty = it.Qty,
                                Price = it.Price
                            });
                        }
                    }
                    _deliveryAppPickupOrders.Add(mapped);
                }
            }

            _deliveryAppDeliveryOrderSequence = state.AppDeliveryOrderSequence;
            _deliveryAppPickupOrderSequence = state.AppPickupOrderSequence;
            if (_deliveryAppDeliveryOrders.Count > 0 || _deliveryAppPickupOrders.Count > 0)
            {
                _deliveryAppOrdersInitialized = true;
            }

            _deliveryAddressesDb.Clear();
            if (state.AddressesByPhone != null)
            {
                foreach (var kv in state.AddressesByPhone)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                    _deliveryAddressesDb[kv.Key] = kv.Value ?? new List<DeliverySavedAddressRecord>();
                }
            }

            _deliveryCurrentOrderId = state.CurrentOrderId;
            if (!string.IsNullOrWhiteSpace(state.ActiveCategory)) _deliveryActiveCategory = state.ActiveCategory;
        }

        private void PersistDeliveryState()
        {
            NormalizeDeliveryState();
            var notesSnapshot = new List<string>();
            for (int i = 0; i < _deliveryNotesHistory.Count; i++)
            {
                var note = (_deliveryNotesHistory[i] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(note)) continue;
                notesSnapshot.Add(note);
            }

            var addressesSnapshot = new Dictionary<string, List<DeliverySavedAddressRecord>>(StringComparer.Ordinal);
            foreach (var kv in _deliveryAddressesDb)
            {
                var phone = (kv.Key ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(phone)) continue;

                var list = kv.Value ?? new List<DeliverySavedAddressRecord>();
                var copy = new List<DeliverySavedAddressRecord>();
                for (int i = 0; i < list.Count; i++)
                {
                    var addr = list[i];
                    if (addr == null) continue;
                    copy.Add(new DeliverySavedAddressRecord
                    {
                        Id = addr.Id,
                        District = addr.District,
                        Block = addr.Block,
                        Street = addr.Street,
                        Building = addr.Building,
                        Apartment = addr.Apartment,
                        Floor = addr.Floor,
                        Note = addr.Note
                    });
                }
                addressesSnapshot[phone] = copy;
            }

            var snapshot = new DeliveryStateRecord
            {
                OrderSequence = _deliveryOrderSequence,
                CurrentOrderId = _deliveryCurrentOrderId,
                ActiveCategory = _deliveryActiveCategory,
                OpenOrders = new List<DeliveryOrderRecord>(_deliveryOrders),
                ShiftOrders = new List<DeliveryOrderRecord>(_deliveryShiftOrders),
                NotesHistory = notesSnapshot,
                AddressesByPhone = addressesSnapshot,
                DistrictCatalog = BuildDeliveryDistrictStateSnapshot(),
                Drivers = BuildDeliveryDriversStateSnapshot(),
                AppDeliveryOrderSequence = _deliveryAppDeliveryOrderSequence,
                AppPickupOrderSequence = _deliveryAppPickupOrderSequence,
                AppDeliveryOrders = BuildAppDeliveryStateSnapshot(),
                AppPickupOrders = BuildAppPickupStateSnapshot()
            };
            WriteJson(DeliveryStateFilePath, snapshot);
            PersistDeliveryAddressesSqlExport(addressesSnapshot);
        }

        private void PersistDeliveryAddressesSqlExport(Dictionary<string, List<DeliverySavedAddressRecord>> addressesSnapshot)
        {
            try
            {
                EnsureAppDataDir();
                var sb = new StringBuilder();
                sb.AppendLine("-- Auto-generated by Fale7 POS");
                sb.AppendLine("-- Replays local customer addresses into public.pos_registered_addresses");
                sb.AppendLine("BEGIN;");

                if (addressesSnapshot != null)
                {
                    foreach (var kv in addressesSnapshot)
                    {
                        var phone = (kv.Key ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(phone)) continue;
                        var list = kv.Value ?? new List<DeliverySavedAddressRecord>();
                        for (int i = 0; i < list.Count; i++)
                        {
                            var addr = list[i];
                            if (addr == null) continue;
                            var label = string.IsNullOrWhiteSpace(addr.District) ? "عنوان" : addr.District.Trim();
                            var addressLine = BuildSupabasePosAddressLine(addr);
                            var phoneSql = EscapeSqlLiteral(phone);
                            var labelSql = EscapeSqlLiteral(label);
                            var lineSql = EscapeSqlLiteral(addressLine);
                            sb.AppendLine(
                                "INSERT INTO public.pos_registered_addresses (phone, label, address_line, registered_at) " +
                                "SELECT '" + phoneSql + "', '" + labelSql + "', '" + lineSql + "', now() " +
                                "WHERE NOT EXISTS (" +
                                "SELECT 1 FROM public.pos_registered_addresses " +
                                "WHERE phone = '" + phoneSql + "' AND address_line = '" + lineSql + "'" +
                                ");");
                        }
                    }
                }

                sb.AppendLine("COMMIT;");
                File.WriteAllText(DeliveryAddressesSqlExportFilePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch
            {
            }
        }

        private static string EscapeSqlLiteral(string value)
        {
            return (value ?? string.Empty).Replace("'", "''");
        }

        private void NormalizeDeliveryState()
        {
            for (int i = _deliveryOrders.Count - 1; i >= 0; i--)
            {
                var o = _deliveryOrders[i];
                if (o == null) { _deliveryOrders.RemoveAt(i); continue; }
                if (o.Items == null) o.Items = new List<DeliveryLineRecord>();
                for (int j = 0; j < o.Items.Count; j++)
                {
                    var item = o.Items[j];
                    if (item == null) continue;
                    if (item.OptionIds == null) item.OptionIds = new List<string>();
                }
                if (o.Address == null) o.Address = new DeliveryAddressRecord();
                if (string.IsNullOrWhiteSpace(o.District) && !string.IsNullOrWhiteSpace(o.Address.District)) o.District = o.Address.District;
                if (string.IsNullOrWhiteSpace(o.DeliveryName)) o.DeliveryName = "خدمة التوصيل";
                o.CashierUserId = (o.CashierUserId ?? string.Empty).Trim();
                o.CashierUsername = (o.CashierUsername ?? string.Empty).Trim();
                o.CashierRole = (o.CashierRole ?? string.Empty).Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(o.CashierUserId)) o.CashierUserId = GetCurrentSessionUserId();
                if (string.IsNullOrWhiteSpace(o.CashierUsername)) o.CashierUsername = GetCurrentSessionDisplayName();
                if (string.IsNullOrWhiteSpace(o.CashierRole)) o.CashierRole = GetCurrentBackendActorRole();
            }

            for (int i = _deliveryShiftOrders.Count - 1; i >= 0; i--)
            {
                var o = _deliveryShiftOrders[i];
                if (o == null) { _deliveryShiftOrders.RemoveAt(i); continue; }
                if (o.Items == null) o.Items = new List<DeliveryLineRecord>();
                for (int j = 0; j < o.Items.Count; j++)
                {
                    var item = o.Items[j];
                    if (item == null) continue;
                    if (item.OptionIds == null) item.OptionIds = new List<string>();
                }
                if (o.Address == null) o.Address = new DeliveryAddressRecord();
                if (string.IsNullOrWhiteSpace(o.District) && !string.IsNullOrWhiteSpace(o.Address.District)) o.District = o.Address.District;
                if (string.IsNullOrWhiteSpace(o.DeliveryName)) o.DeliveryName = "خدمة التوصيل";
                o.CashierUserId = (o.CashierUserId ?? string.Empty).Trim();
                o.CashierUsername = (o.CashierUsername ?? string.Empty).Trim();
                o.CashierRole = (o.CashierRole ?? string.Empty).Trim().ToUpperInvariant();
            }

            for (int i = _deliveryAppDeliveryOrders.Count - 1; i >= 0; i--)
            {
                var o = _deliveryAppDeliveryOrders[i];
                if (o == null || string.IsNullOrWhiteSpace(o.Id)) { _deliveryAppDeliveryOrders.RemoveAt(i); continue; }
                if (o.Items == null) o.Items = new List<AppOrderItemRecord>();
                if (o.Address == null) o.Address = ParseAppDeliveryAddressText(o.District, o.AddressText);
                if (o.Address == null) o.Address = new DeliveryAddressRecord();
                if (string.IsNullOrWhiteSpace(o.Address.District)) o.Address.District = o.District;
                if (string.IsNullOrWhiteSpace(o.District) && !string.IsNullOrWhiteSpace(o.Address.District)) o.District = o.Address.District;
                o.Subtotal = GetStoredAppDeliveryOrderSubtotal(o);
                if (o.DeliveryFee < 0) o.DeliveryFee = 0;
                o.Total = GetStoredAppDeliveryOrderTotal(o);
            }

            var found = false;
            for (int i = 0; i < _deliveryOrders.Count; i++)
            {
                if (_deliveryOrders[i].Id == _deliveryCurrentOrderId) { found = true; break; }
            }
            if (!found) _deliveryCurrentOrderId = _deliveryOrders.Count > 0 ? _deliveryOrders[0].Id : null;
        }

        private void EnsureDeliveryFormUiInitialized()
        {
            if (DeliveryFormDistrictComboBox == null) return;
            EnsureDeliveryDistrictCatalogInitialized();
            if (DeliveryFormDistrictComboBox.Items.Count == 0)
            {
                RebuildDeliveryDistrictComboItems(null);
            }
        }

        private string GetSelectedDeliveryDistrict()
        {
            var item = DeliveryFormDistrictComboBox != null ? DeliveryFormDistrictComboBox.SelectedItem as ComboBoxItem : null;
            return item != null ? (item.Tag as string ?? string.Empty) : string.Empty;
        }

        private int GetSelectedDeliveryDistrictFee()
        {
            var district = GetSelectedDeliveryDistrict();
            for (int i = 0; i < _deliveryDistrictCatalog.Count; i++)
            {
                if (string.Equals(_deliveryDistrictCatalog[i].Name, district, StringComparison.Ordinal)) return _deliveryDistrictCatalog[i].Fee;
            }
            return 0;
        }

        private string GetSelectedDeliveryDistrictId()
        {
            var district = GetSelectedDeliveryDistrict();
            if (string.IsNullOrWhiteSpace(district)) return string.Empty;
            for (int i = 0; i < _deliveryDistrictCatalog.Count; i++)
            {
                var row = _deliveryDistrictCatalog[i];
                if (row == null) continue;
                if (!string.Equals(row.Name, district, StringComparison.Ordinal)) continue;
                return (row.Id ?? string.Empty).Trim();
            }
            return string.Empty;
        }

        private void SetDeliveryDistrictSelection(string districtName)
        {
            if (DeliveryFormDistrictComboBox == null) return;
            EnsureDeliveryDistrictCatalogInitialized();
            if (DeliveryFormDistrictComboBox.Items.Count == 0 && !_deliveryDistrictComboRebuildInProgress)
            {
                RebuildDeliveryDistrictComboItems(null);
            }
            if (DeliveryFormDistrictComboBox.Items.Count == 0) return;
            if (string.IsNullOrWhiteSpace(districtName))
            {
                DeliveryFormDistrictComboBox.SelectedIndex = 0;
                return;
            }

            var index = FindDistrictComboIndexByName(districtName);
            DeliveryFormDistrictComboBox.SelectedIndex = index >= 0 ? index : 0;
        }

        private void EnsureDeliveryDistrictCatalogInitialized()
        {
            if (_deliveryDistrictCatalog.Count > 0) return;
            for (int i = 0; i < DeliveryDistrictFallbacks.Length; i++)
            {
                _deliveryDistrictCatalog.Add(new DeliveryDistrictFeeRecord
                {
                    Id = DeliveryDistrictFallbacks[i].Id,
                    Name = DeliveryDistrictFallbacks[i].Name,
                    Fee = DeliveryDistrictFallbacks[i].Fee
                });
            }
        }

        private void RebuildDeliveryDistrictComboItems(string preferredDistrictName)
        {
            if (DeliveryFormDistrictComboBox == null) return;
            EnsureDeliveryDistrictCatalogInitialized();

            _deliveryDistrictComboRebuildInProgress = true;
            try
            {
                var current = !string.IsNullOrWhiteSpace(preferredDistrictName) ? preferredDistrictName : GetSelectedDeliveryDistrict();
                DeliveryFormDistrictComboBox.Items.Clear();
                DeliveryFormDistrictComboBox.Items.Add(new ComboBoxItem { Content = "اختر الحي", Tag = null });
                if (_deliveryDistrictCatalog.Count == 0)
                {
                    DeliveryFormDistrictComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = "لا توجد أحياء محملة من الباك اند",
                        Tag = null,
                        IsEnabled = false
                    });
                    DeliveryFormDistrictComboBox.SelectedIndex = 0;
                    return;
                }

                for (int i = 0; i < _deliveryDistrictCatalog.Count; i++)
                {
                    var d = _deliveryDistrictCatalog[i];
                    if (d == null || string.IsNullOrWhiteSpace(d.Name)) continue;
                    DeliveryFormDistrictComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = d.Name + " (توصيل " + d.Fee + ")",
                        Tag = d.Name
                    });
                }

                var index = FindDistrictComboIndexByName(current);
                if (index < 0 && !string.IsNullOrWhiteSpace(current))
                {
                    DeliveryFormDistrictComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = current,
                        Tag = current
                    });
                    index = FindDistrictComboIndexByName(current);
                }
                DeliveryFormDistrictComboBox.SelectedIndex = index >= 0 ? index : 0;
            }
            finally
            {
                _deliveryDistrictComboRebuildInProgress = false;
            }
        }

        private int FindDistrictComboIndexByName(string districtName)
        {
            if (DeliveryFormDistrictComboBox == null) return -1;
            if (string.IsNullOrWhiteSpace(districtName)) return 0;
            for (int i = 0; i < DeliveryFormDistrictComboBox.Items.Count; i++)
            {
                var item = DeliveryFormDistrictComboBox.Items[i] as ComboBoxItem;
                if (item != null && string.Equals(item.Tag as string, districtName, StringComparison.Ordinal))
                {
                    return i;
                }
            }
            return -1;
        }

        private void TryRefreshDeliveryDistrictCatalogFromBackend(bool showErrors)
        {
            try
            {
                QueueSupabaseDistrictsPullFromBackend(showErrors);
            }
            catch (Exception ex)
            {
                if (showErrors) ShowSupabaseServerError("تعذر تحميل الأحياء", ex.Message);
            }
        }

        private async System.Threading.Tasks.Task<List<SupabaseHoodRow>> FetchSupabaseHoodsAsync(SupabaseClientConfigRecord cfg)
        {
            var url = cfg.Url + "/rest/v1/hoods?select=id,name,fee&order=name.asc&limit=500";
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
                    return DeserializeJson<List<SupabaseHoodRow>>(raw) ?? new List<SupabaseHoodRow>();
                }
            }
        }

        private List<DeliveryDistrictFeeRecord> MapSupabaseHoodsToDeliveryDistricts(List<SupabaseHoodRow> rows)
        {
            var next = new List<DeliveryDistrictFeeRecord>();
            if (rows == null) return next;

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

            next.Sort(delegate(DeliveryDistrictFeeRecord a, DeliveryDistrictFeeRecord b)
            {
                return string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
            });
            return next;
        }

        private void ReplaceDeliveryDistrictCatalog(List<DeliveryDistrictFeeRecord> next)
        {
            if (next == null || next.Count == 0) return;
            _deliveryDistrictCatalog.Clear();
            for (int i = 0; i < next.Count; i++) _deliveryDistrictCatalog.Add(next[i]);
        }

        private List<DeliverySavedAddressRecord> MapSupabasePosRegisteredAddresses(List<SupabasePosRegisteredAddressRow> rows)
        {
            var mapped = new List<DeliverySavedAddressRecord>();
            if (rows == null) return mapped;

            for (int i = 0; i < rows.Count; i++)
            {
                var address = MapSupabasePosRegisteredAddress(rows[i]);
                if (address != null) mapped.Add(address);
            }

            return mapped;
        }

        private void TrySelectDeliveryFormAddressForCurrentAddress(string phone, DeliveryAddressRecord address, string preferredAddressId = null)
        {
            phone = (phone ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(phone)) return;

            List<DeliverySavedAddressRecord> existingAddresses;
            if (!_deliveryAddressesDb.TryGetValue(phone, out existingAddresses) || existingAddresses == null) return;

            var trimmedAddressId = (preferredAddressId ?? string.Empty).Trim();
            if (IsSupabaseAddressId(trimmedAddressId))
            {
                var byId = FindDeliveryAddressById(existingAddresses, trimmedAddressId);
                if (byId != null)
                {
                    _deliveryFormSelectedAddressId = byId.Id;
                    return;
                }
            }

            if (address == null) return;
            var addressSignature = DeliveryAddressSignature(BuildDeliverySavedAddressFromAddress(address));
            var matched = FindDeliveryAddressBySignature(existingAddresses, addressSignature);
            _deliveryFormSelectedAddressId = matched == null ? null : matched.Id;
        }

        private async void RefreshDeliveryFormBackendDataAsync(string phone, DeliveryAddressRecord preferredAddress, string preferredAddressId = null)
        {
            var refreshStamp = Interlocked.Increment(ref _deliveryFormBackendRefreshStamp);
            phone = (phone ?? string.Empty).Trim();

            SupabaseClientConfigRecord cfg = null;
            try { cfg = LoadSupabaseClientConfigResolved(); } catch { }
            if (cfg == null) return;

            try
            {
                var hoodRows = await FetchSupabaseHoodsAsync(cfg);
                if (refreshStamp != _deliveryFormBackendRefreshStamp) return;
                if (DeliveryFormOverlay == null || DeliveryFormOverlay.Visibility != Visibility.Visible) return;

                var districts = MapSupabaseHoodsToDeliveryDistricts(hoodRows);
                if (districts.Count > 0)
                {
                    var selectedDistrict = GetSelectedDeliveryDistrict();
                    if (string.IsNullOrWhiteSpace(selectedDistrict) && preferredAddress != null)
                    {
                        selectedDistrict = (preferredAddress.District ?? string.Empty).Trim();
                    }

                    ReplaceDeliveryDistrictCatalog(districts);
                    RebuildDeliveryDistrictComboItems(selectedDistrict);
                    if (!string.IsNullOrWhiteSpace(selectedDistrict)) SetDeliveryDistrictSelection(selectedDistrict);
                    UpdateDeliveryFormAddressPreview();
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(phone)) return;

            try
            {
                var backendRows = await FetchPosRegisteredAddressesByPhoneAsync(cfg, phone);
                if (refreshStamp != _deliveryFormBackendRefreshStamp) return;
                if (DeliveryFormOverlay == null || DeliveryFormOverlay.Visibility != Visibility.Visible) return;
                if (!string.Equals(phone, DeliveryFormPhoneKey(), StringComparison.Ordinal)) return;

                var merged = FilterPendingDeliveryAddressDeletes(phone, MapSupabasePosRegisteredAddresses(backendRows));
                var changed = MergeAndSyncLocalAddressesForPhone(phone, merged);
                _deliveryAddressesDb[phone] = merged;
                if (changed) PersistDeliveryState();

                TrySelectDeliveryFormAddressForCurrentAddress(phone, preferredAddress, preferredAddressId);
                RenderDeliveryFormAddressList();
            }
            catch { }
        }

        private void OpenDeliveryForm(bool isEdit)
        {
            if (DeliveryFormOverlay == null) return;
            DeliveryAddressRecord preferredAddress = null;
            EnsureDeliveryFormUiInitialized();
            RebuildDeliveryDistrictComboItems(null);
            SetDeliveryPickupContextActive(false);
            CloseDeliveryFormSuggestPopup();
            CloseDeliveryDistrictPickerPopup();

            _deliveryFormSelectedAddressId = null;
            _deliveryFormShowSavedAddresses = false;
            _deliveryFormPrefillOrderId = null;
            _deliveryFormAppOrderId = null;
            ApplyDeliveryFormModeUi();

            ClearDeliveryFormFields();

            if (isEdit)
            {
                var order = GetCurrentDeliveryOrder();
                if (order != null)
                {
                    _deliveryFormPrefillOrderId = order.Id;
                    preferredAddress = order.Address == null ? null : CloneDeliveryAddress(order.Address);
                    DeliveryFormPhoneTextBox.Text = order.CustomerPhone ?? string.Empty;
                    DeliveryFormCustomerNameTextBox.Text = order.CustomerName ?? string.Empty;
                    SetDeliveryDistrictSelection(order.Address != null ? order.Address.District : order.District);
                    if (order.Address != null)
                    {
                        DeliveryFormBlockTextBox.Text = order.Address.Block ?? string.Empty;
                        DeliveryFormStreetTextBox.Text = order.Address.Street ?? string.Empty;
                        DeliveryFormBuildingTextBox.Text = order.Address.Building ?? string.Empty;
                        DeliveryFormApartmentTextBox.Text = order.Address.Apartment ?? string.Empty;
                        DeliveryFormFloorTextBox.Text = order.Address.Floor ?? string.Empty;
                        DeliveryFormAddressNoteTextBox.Text = order.Address.Note ?? string.Empty;
                    }
                }
            }

            UpdateDeliveryFormAddressPreview();
            RenderDeliveryFormAddressList();
            ShowDeliveryFormOverlay();
            Dispatcher.BeginInvoke(new Action(delegate
            {
                DeliveryFormPhoneTextBox.Focus();
                DeliveryFormPhoneTextBox.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
            RefreshDeliveryFormBackendDataAsync(DeliveryFormPhoneKey(), preferredAddress);
        }

        private void CloseDeliveryForm()
        {
            CloseDeliveryFormSuggestPopup();
            CloseDeliveryDistrictPickerPopup();
            if (DeliveryFormOverlay != null) DeliveryFormOverlay.Visibility = Visibility.Collapsed;
            Interlocked.Increment(ref _deliveryFormBackendRefreshStamp);
            _deliveryFormSelectedAddressId = null;
            _deliveryFormShowSavedAddresses = false;
            _deliveryFormPrefillOrderId = null;
            _deliveryFormAppOrderId = null;
            ApplyDeliveryFormModeUi();
        }

        private bool IsDeliveryFormEditingAppOrder()
        {
            return !string.IsNullOrWhiteSpace((_deliveryFormAppOrderId ?? string.Empty).Trim());
        }

        private void ShowDeliveryFormOverlay()
        {
            if (DeliveryFormOverlay == null) return;
            Panel.SetZIndex(DeliveryFormOverlay, 1003);
            DeliveryFormOverlay.Visibility = Visibility.Visible;
        }

        private void ApplyDeliveryFormModeUi()
        {
            var isAppOrderEdit = IsDeliveryFormEditingAppOrder();
            var readOnlyBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3F4F6"));
            if (DeliveryFormTitleText != null)
            {
                DeliveryFormTitleText.Text = isAppOrderEdit ? "تعديل عنوان طلب التطبيق" : "إنشاء أوردر جديد (أوفلاين)";
            }
            if (DeliveryFormHintText != null)
            {
                DeliveryFormHintText.Text = isAppOrderEdit
                    ? "عدّل نفس فورم الدليفري ثم احفظ ليتم تحديث العنوان والخدمة."
                    : "اكتب الهاتف ثم Enter لإظهار العناوين";
            }

            if (DeliveryFormSaveButton != null)
            {
                DeliveryFormSaveButton.Content = isAppOrderEdit ? "حفظ عنوان طلب التطبيق" : "حفظ العنوان والرجوع";
            }
            if (DeliveryFormDeleteAddressButton != null)
            {
                DeliveryFormDeleteAddressButton.Visibility = isAppOrderEdit ? Visibility.Collapsed : Visibility.Visible;
            }
            if (DeliveryFormPhoneTextBox != null)
            {
                DeliveryFormPhoneTextBox.IsReadOnly = isAppOrderEdit;
                DeliveryFormPhoneTextBox.Background = isAppOrderEdit ? readOnlyBrush : Brushes.White;
            }
            if (DeliveryFormCustomerNameTextBox != null)
            {
                DeliveryFormCustomerNameTextBox.IsReadOnly = isAppOrderEdit;
                DeliveryFormCustomerNameTextBox.Background = isAppOrderEdit ? readOnlyBrush : Brushes.White;
            }
        }

        private void OpenAppDeliveryOrderAddressForm(AppDeliveryOrderRecord order)
        {
            var currentOrder = FindAppDeliveryOrderById(order != null ? order.Id : null) ?? order;
            if (currentOrder == null || string.IsNullOrWhiteSpace(currentOrder.Id)) return;
            if (DeliveryFormOverlay == null)
            {
                MessageBox.Show("تعذر فتح فورم العنوان الآن.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var address = GetEditableAddressForAppDeliveryOrder(currentOrder);
            EnsureDeliveryFormUiInitialized();
            RebuildDeliveryDistrictComboItems(string.IsNullOrWhiteSpace(address.District) ? currentOrder.District : address.District);
            SetDeliveryPickupContextActive(false);
            CloseDeliveryFormSuggestPopup();
            CloseDeliveryDistrictPickerPopup();

            _deliveryFormSelectedAddressId = null;
            _deliveryFormShowSavedAddresses = true;
            _deliveryFormPrefillOrderId = null;
            _deliveryFormAppOrderId = currentOrder.Id;
            ApplyDeliveryFormModeUi();

            ClearDeliveryFormFields();

            DeliveryFormPhoneTextBox.Text = currentOrder.Phone ?? string.Empty;
            DeliveryFormCustomerNameTextBox.Text = currentOrder.Name ?? string.Empty;
            TrySelectDeliveryFormAddressForCurrentAddress(currentOrder.Phone, address, currentOrder.AddressId);
            SetDeliveryDistrictSelection(string.IsNullOrWhiteSpace(address.District) ? currentOrder.District : address.District);
            DeliveryFormBlockTextBox.Text = address.Block ?? string.Empty;
            DeliveryFormStreetTextBox.Text = address.Street ?? string.Empty;
            DeliveryFormBuildingTextBox.Text = address.Building ?? string.Empty;
            DeliveryFormApartmentTextBox.Text = address.Apartment ?? string.Empty;
            DeliveryFormFloorTextBox.Text = address.Floor ?? string.Empty;
            DeliveryFormAddressNoteTextBox.Text = address.Note ?? string.Empty;

            UpdateDeliveryFormAddressPreview();
            RenderDeliveryFormAddressList();
            ShowDeliveryFormOverlay();
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (DeliveryFormStreetTextBox != null)
                {
                    DeliveryFormStreetTextBox.Focus();
                    DeliveryFormStreetTextBox.SelectAll();
                }
            }), System.Windows.Threading.DispatcherPriority.Input);
            RefreshDeliveryFormBackendDataAsync(currentOrder.Phone, address, currentOrder.AddressId);
        }

        private void ClearDeliveryFormFields()
        {
            if (DeliveryFormPhoneTextBox == null) return;
            DeliveryFormPhoneTextBox.Text = string.Empty;
            DeliveryFormCustomerNameTextBox.Text = string.Empty;
            SetDeliveryDistrictSelection(null);
            DeliveryFormBlockTextBox.Text = string.Empty;
            DeliveryFormStreetTextBox.Text = string.Empty;
            DeliveryFormBuildingTextBox.Text = string.Empty;
            DeliveryFormApartmentTextBox.Text = string.Empty;
            DeliveryFormFloorTextBox.Text = string.Empty;
            DeliveryFormAddressNoteTextBox.Text = string.Empty;
        }

        private string DeliveryFormPhoneKey()
        {
            return (DeliveryFormPhoneTextBox.Text ?? string.Empty).Trim();
        }

        private void UpdateDeliveryFormAddressPreview()
        {
            if (DeliveryFormAddressPreviewText == null) return;
            var parts = new List<string>();
            var district = GetSelectedDeliveryDistrict();
            if (!string.IsNullOrWhiteSpace(district)) parts.Add("الحي: " + district);
            if (!string.IsNullOrWhiteSpace(DeliveryFormBlockTextBox.Text)) parts.Add("المجاورة: " + DeliveryFormBlockTextBox.Text.Trim());
            if (!string.IsNullOrWhiteSpace(DeliveryFormStreetTextBox.Text)) parts.Add("الشارع: " + DeliveryFormStreetTextBox.Text.Trim());
            if (!string.IsNullOrWhiteSpace(DeliveryFormBuildingTextBox.Text)) parts.Add("عمارة: " + DeliveryFormBuildingTextBox.Text.Trim());
            if (!string.IsNullOrWhiteSpace(DeliveryFormApartmentTextBox.Text)) parts.Add("شقة: " + DeliveryFormApartmentTextBox.Text.Trim());
            if (!string.IsNullOrWhiteSpace(DeliveryFormFloorTextBox.Text)) parts.Add("دور: " + DeliveryFormFloorTextBox.Text.Trim());
            if (!string.IsNullOrWhiteSpace(DeliveryFormAddressNoteTextBox.Text)) parts.Add("ملاحظة: " + DeliveryFormAddressNoteTextBox.Text.Trim());
            DeliveryFormAddressPreviewText.Text = parts.Count > 0 ? string.Join(" — ", parts.ToArray()) : "العنوان سيظهر هنا أثناء الكتابة…";
        }

        private string NormalizeDeliveryAddressPart(string value)
        {
            return (value ?? string.Empty).Trim().Replace("\r", " ").Replace("\n", " ");
        }

        private string DeliveryAddressSignature(DeliverySavedAddressRecord a)
        {
            if (a == null) return string.Empty;
            return string.Join("|", new[]
            {
                NormalizeDeliveryAddressPart(a.District).ToLowerInvariant(),
                NormalizeDeliveryAddressPart(a.Block).ToLowerInvariant(),
                NormalizeDeliveryAddressPart(a.Street).ToLowerInvariant(),
                NormalizeDeliveryAddressPart(a.Building).ToLowerInvariant(),
                NormalizeDeliveryAddressPart(a.Apartment).ToLowerInvariant(),
                NormalizeDeliveryAddressPart(a.Floor).ToLowerInvariant(),
                NormalizeDeliveryAddressPart(a.Note).ToLowerInvariant()
            });
        }

        private bool IsSupabaseAddressId(string addressId)
        {
            int parsedId;
            return int.TryParse((addressId ?? string.Empty).Trim(), out parsedId) && parsedId > 0;
        }

        private DeliverySavedAddressRecord CloneDeliverySavedAddress(DeliverySavedAddressRecord source)
        {
            if (source == null) return null;
            return new DeliverySavedAddressRecord
            {
                Id = source.Id,
                District = source.District,
                Block = source.Block,
                Street = source.Street,
                Building = source.Building,
                Apartment = source.Apartment,
                Floor = source.Floor,
                Note = source.Note
            };
        }

        private bool MergeAndSyncLocalAddressesForPhone(
            string phone,
            List<DeliverySavedAddressRecord> mergedList,
            List<DeliverySavedAddressRecord> localListOverride = null,
            bool pushToServer = false)
        {
            phone = (phone ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(phone) || mergedList == null) return false;

            List<DeliverySavedAddressRecord> localList;
            if (localListOverride != null)
            {
                localList = localListOverride;
            }
            else if (!_deliveryAddressesDb.TryGetValue(phone, out localList) || localList == null || localList.Count == 0)
            {
                return false;
            }

            if (localList == null || localList.Count == 0) return false;

            var changed = false;
            var existingSigs = new HashSet<string>(StringComparer.Ordinal);
            var backendBySig = new Dictionary<string, DeliverySavedAddressRecord>(StringComparer.Ordinal);
            var backendById = new Dictionary<string, DeliverySavedAddressRecord>(StringComparer.Ordinal);
            for (int i = 0; i < mergedList.Count; i++)
            {
                var current = mergedList[i];
                if (current == null) continue;
                var currentSig = DeliveryAddressSignature(current);
                if (!string.IsNullOrWhiteSpace(currentSig))
                {
                    existingSigs.Add(currentSig);
                    if (!backendBySig.ContainsKey(currentSig)) backendBySig[currentSig] = current;
                }
                var backendId = (current.Id ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(backendId) && !backendById.ContainsKey(backendId)) backendById[backendId] = current;
            }

            for (int i = 0; i < localList.Count; i++)
            {
                var local = localList[i];
                if (local == null) continue;

                var sig = DeliveryAddressSignature(local);
                if (string.IsNullOrWhiteSpace(sig)) continue;

                DeliverySavedAddressRecord backendSameSig;
                backendBySig.TryGetValue(sig, out backendSameSig);
                if (backendSameSig != null)
                {
                    if (!IsSupabaseAddressId(local.Id) && IsSupabaseAddressId(backendSameSig.Id))
                    {
                        changed = true;
                    }
                    continue;
                }

                var syncedAddress = CloneDeliverySavedAddress(local) ?? local;
                var localId = (local.Id ?? string.Empty).Trim();
                if (IsSupabaseAddressId(localId))
                {
                    DeliverySavedAddressRecord backendSameId;
                    backendById.TryGetValue(localId, out backendSameId);
                    if (pushToServer && backendSameId != null)
                    {
                        var backendSig = DeliveryAddressSignature(backendSameId);
                        if (!string.Equals(backendSig, sig, StringComparison.Ordinal))
                        {
                            DeliverySavedAddressRecord updated;
                            string updateError;
                            if (TryUpdatePosRegisteredAddress(localId, phone, local, out updated, out updateError))
                            {
                                syncedAddress = updated ?? syncedAddress;
                            }
                        }
                    }
                    else if (pushToServer)
                    {
                        DeliverySavedAddressRecord inserted;
                        string insertError;
                        if (TryInsertPosRegisteredAddress(phone, local, out inserted, out insertError))
                        {
                            syncedAddress = inserted ?? syncedAddress;
                        }
                    }
                }
                else if (pushToServer)
                {
                    DeliverySavedAddressRecord inserted;
                    string insertError;
                    if (TryInsertPosRegisteredAddress(phone, local, out inserted, out insertError))
                    {
                        syncedAddress = inserted ?? syncedAddress;
                    }
                }

                var syncedSig = DeliveryAddressSignature(syncedAddress);
                var syncedId = (syncedAddress == null ? string.Empty : (syncedAddress.Id ?? string.Empty).Trim());
                if (!string.IsNullOrWhiteSpace(syncedId))
                {
                    for (int j = mergedList.Count - 1; j >= 0; j--)
                    {
                        var existing = mergedList[j];
                        if (existing == null) continue;
                        var existingId = (existing.Id ?? string.Empty).Trim();
                        if (!string.Equals(existingId, syncedId, StringComparison.Ordinal)) continue;
                        var existingSig = DeliveryAddressSignature(existing);
                        if (!string.IsNullOrWhiteSpace(existingSig))
                        {
                            existingSigs.Remove(existingSig);
                            backendBySig.Remove(existingSig);
                        }
                        mergedList.RemoveAt(j);
                        changed = true;
                    }
                }

                mergedList.Insert(0, syncedAddress);
                if (!string.IsNullOrWhiteSpace(syncedSig))
                {
                    existingSigs.Add(syncedSig);
                    backendBySig[syncedSig] = syncedAddress;
                }
                if (!string.IsNullOrWhiteSpace(syncedId))
                {
                    backendById[syncedId] = syncedAddress;
                }
                changed = true;
            }

            while (mergedList.Count > 50)
            {
                mergedList.RemoveAt(mergedList.Count - 1);
                changed = true;
            }

            return changed;
        }

        private bool AreDeliveryAddressListsEquivalent(List<DeliverySavedAddressRecord> left, List<DeliverySavedAddressRecord> right)
        {
            left = left ?? new List<DeliverySavedAddressRecord>();
            right = right ?? new List<DeliverySavedAddressRecord>();
            if (left.Count != right.Count) return false;

            for (int i = 0; i < left.Count; i++)
            {
                var l = left[i];
                var r = right[i];
                var lId = (l == null ? string.Empty : (l.Id ?? string.Empty).Trim());
                var rId = (r == null ? string.Empty : (r.Id ?? string.Empty).Trim());
                if (!string.Equals(lId, rId, StringComparison.Ordinal)) return false;

                var lSig = DeliveryAddressSignature(l);
                var rSig = DeliveryAddressSignature(r);
                if (!string.Equals(lSig, rSig, StringComparison.Ordinal)) return false;
            }

            return true;
        }

        private DeliverySavedAddressRecord BuildDeliveryFormAddressCandidate(string district)
        {
            return new DeliverySavedAddressRecord
            {
                Id = NewId(),
                District = district,
                Block = (DeliveryFormBlockTextBox.Text ?? string.Empty).Trim(),
                Street = (DeliveryFormStreetTextBox.Text ?? string.Empty).Trim(),
                Building = (DeliveryFormBuildingTextBox.Text ?? string.Empty).Trim(),
                Apartment = (DeliveryFormApartmentTextBox.Text ?? string.Empty).Trim(),
                Floor = (DeliveryFormFloorTextBox.Text ?? string.Empty).Trim(),
                Note = (DeliveryFormAddressNoteTextBox.Text ?? string.Empty).Trim()
            };
        }

        private DeliverySavedAddressRecord BuildDeliverySavedAddressFromOrder(DeliveryOrderRecord order)
        {
            if (order == null) return null;
            var address = order.Address ?? new DeliveryAddressRecord();
            return new DeliverySavedAddressRecord
            {
                Id = string.Empty,
                District = string.IsNullOrWhiteSpace(address.District) ? order.District : address.District,
                Block = address.Block,
                Street = address.Street,
                Building = address.Building,
                Apartment = address.Apartment,
                Floor = address.Floor,
                Note = address.Note
            };
        }

        private DeliverySavedAddressRecord BuildDeliverySavedAddressFromAddress(DeliveryAddressRecord address)
        {
            if (address == null) return null;
            return new DeliverySavedAddressRecord
            {
                Id = string.Empty,
                District = address.District,
                Block = address.Block,
                Street = address.Street,
                Building = address.Building,
                Apartment = address.Apartment,
                Floor = address.Floor,
                Note = address.Note
            };
        }

        private DeliverySavedAddressRecord FindDeliveryAddressById(List<DeliverySavedAddressRecord> list, string addressId)
        {
            if (list == null || string.IsNullOrWhiteSpace(addressId)) return null;
            var trimmed = addressId.Trim();
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null) continue;
                if (string.Equals((row.Id ?? string.Empty).Trim(), trimmed, StringComparison.Ordinal)) return row;
            }
            return null;
        }

        private int FindDeliveryAddressIndexById(List<DeliverySavedAddressRecord> list, string addressId)
        {
            if (list == null || string.IsNullOrWhiteSpace(addressId)) return -1;
            var trimmed = addressId.Trim();
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null) continue;
                if (string.Equals((row.Id ?? string.Empty).Trim(), trimmed, StringComparison.Ordinal)) return i;
            }
            return -1;
        }

        private DeliverySavedAddressRecord FindDeliveryAddressBySignature(List<DeliverySavedAddressRecord> list, string signature)
        {
            if (list == null || string.IsNullOrWhiteSpace(signature)) return null;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null) continue;
                if (DeliveryAddressSignature(row) == signature) return row;
            }
            return null;
        }

        private DeliverySavedAddressRecord SaveDeliveryAddressAsNew(string phone, DeliverySavedAddressRecord candidate, List<DeliverySavedAddressRecord> list)
        {
            if (candidate == null) return null;
            var copy = CloneDeliverySavedAddress(candidate) ?? candidate;
            DeliverySavedAddressRecord inserted;
            string insertError;
            if (TryInsertPosRegisteredAddress(phone, copy, out inserted, out insertError))
            {
                copy = inserted ?? copy;
            }
            else if (string.IsNullOrWhiteSpace(copy.Id))
            {
                copy.Id = NewId();
            }

            if (list != null)
            {
                list.Insert(0, copy);
                while (list.Count > 50) list.RemoveAt(list.Count - 1);
            }
            return copy;
        }

        private DeliverySavedAddressRecord UpdateExistingDeliveryAddress(string phone, DeliverySavedAddressRecord existing, DeliverySavedAddressRecord candidate, List<DeliverySavedAddressRecord> list)
        {
            if (candidate == null) return existing;
            if (existing == null) return SaveDeliveryAddressAsNew(phone, candidate, list);

            var updated = CloneDeliverySavedAddress(existing) ?? new DeliverySavedAddressRecord();
            updated.District = candidate.District;
            updated.Block = candidate.Block;
            updated.Street = candidate.Street;
            updated.Building = candidate.Building;
            updated.Apartment = candidate.Apartment;
            updated.Floor = candidate.Floor;
            updated.Note = candidate.Note;

            DeliverySavedAddressRecord backendRow;
            string serverError;
            if (IsSupabaseAddressId(existing.Id))
            {
                if (TryUpdatePosRegisteredAddress(existing.Id, phone, updated, out backendRow, out serverError))
                {
                    updated = backendRow ?? updated;
                }
            }
            else if (TryInsertPosRegisteredAddress(phone, updated, out backendRow, out serverError))
            {
                updated = backendRow ?? updated;
            }

            if (list != null)
            {
                var index = FindDeliveryAddressIndexById(list, existing.Id);
                if (index >= 0) list[index] = updated;
                else list.Insert(0, updated);
                while (list.Count > 50) list.RemoveAt(list.Count - 1);
            }

            return updated;
        }

        private bool IsDeliveryOrderUnchangedByForm(DeliveryOrderRecord order, string phone, string customerName, string district, int fee, DeliverySavedAddressRecord candidate)
        {
            if (order == null || candidate == null) return false;

            var currentPhone = (phone ?? string.Empty).Trim();
            var orderPhone = (order.CustomerPhone ?? string.Empty).Trim();
            if (!string.Equals(currentPhone, orderPhone, StringComparison.Ordinal)) return false;

            var currentName = string.IsNullOrWhiteSpace(customerName) ? "عميل" : customerName.Trim();
            var orderName = string.IsNullOrWhiteSpace(order.CustomerName) ? "عميل" : order.CustomerName.Trim();
            if (!string.Equals(currentName, orderName, StringComparison.Ordinal)) return false;

            var currentDistrict = (district ?? string.Empty).Trim();
            var orderDistrict = (order.District ?? string.Empty).Trim();
            if (!string.Equals(currentDistrict, orderDistrict, StringComparison.Ordinal)) return false;
            if (fee != order.DeliveryFee) return false;

            var orderAddress = BuildDeliverySavedAddressFromOrder(order);
            return string.Equals(DeliveryAddressSignature(orderAddress), DeliveryAddressSignature(candidate), StringComparison.Ordinal);
        }

        private void RenderDeliveryFormAddressList()
        {
            if (DeliveryFormAddressListPanel == null) return;
            DeliveryFormAddressListPanel.Children.Clear();
            if (!_deliveryFormShowSavedAddresses) return;

            var phone = DeliveryFormPhoneKey();
            if (string.IsNullOrWhiteSpace(phone)) return;

            List<DeliverySavedAddressRecord> list;
            if (!_deliveryAddressesDb.TryGetValue(phone, out list) || list == null || list.Count == 0)
            {
                DeliveryFormAddressListPanel.Children.Add(new TextBlock
                {
                    Text = "لا توجد عناوين محفوظة لهذا الرقم.",
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF555555")),
                    FlowDirection = FlowDirection.RightToLeft
                });
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (a == null) continue;
                var isActive = a.Id == _deliveryFormSelectedAddressId;
                var card = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isActive ? "#FF3A7BD5" : "#FF999999")),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isActive ? "#FFD7EBFF" : "#FFF7F7F7")),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 10),
                    Cursor = Cursors.Hand
                };
                card.Child = new TextBlock
                {
                    Text = string.Format("{0} - {1} - {2} - عمارة {3} - شقة {4}", a.District, a.Block, a.Street, a.Building, a.Apartment),
                    FontWeight = FontWeights.Bold,
                    FlowDirection = FlowDirection.RightToLeft,
                    TextWrapping = TextWrapping.Wrap
                };
                var captured = a;
                card.MouseLeftButtonUp += delegate
                {
                    _deliveryFormSelectedAddressId = captured.Id;
                    SetDeliveryDistrictSelection(captured.District);
                    DeliveryFormBlockTextBox.Text = captured.Block ?? string.Empty;
                    DeliveryFormStreetTextBox.Text = captured.Street ?? string.Empty;
                    DeliveryFormBuildingTextBox.Text = captured.Building ?? string.Empty;
                    DeliveryFormApartmentTextBox.Text = captured.Apartment ?? string.Empty;
                    DeliveryFormFloorTextBox.Text = captured.Floor ?? string.Empty;
                    DeliveryFormAddressNoteTextBox.Text = captured.Note ?? string.Empty;
                    UpdateDeliveryFormAddressPreview();
                    RenderDeliveryFormAddressList();
                };
                DeliveryFormAddressListPanel.Children.Add(card);
            }
        }

        private string ValidateDeliveryForm()
        {
            if (string.IsNullOrWhiteSpace(DeliveryFormPhoneKey()))
            {
                if (!IsDeliveryFormEditingAppOrder()) return "رقم الهاتف إجباري";
                var currentOrder = FindAppDeliveryOrderById(_deliveryFormAppOrderId);
                if (currentOrder == null || (string.IsNullOrWhiteSpace(currentOrder.Phone) && string.IsNullOrWhiteSpace(currentOrder.CustomerUserId)))
                {
                    return "رقم الهاتف إجباري";
                }
            }
            if (string.IsNullOrWhiteSpace(GetSelectedDeliveryDistrict())) return "الحي إجباري";
            if (string.IsNullOrWhiteSpace(DeliveryFormBlockTextBox.Text)) return "المجاورة إجبارية";
            if (string.IsNullOrWhiteSpace(DeliveryFormStreetTextBox.Text)) return "الشارع إجباري";
            if (string.IsNullOrWhiteSpace(DeliveryFormBuildingTextBox.Text)) return "العمارة إجبارية";
            if (string.IsNullOrWhiteSpace(DeliveryFormApartmentTextBox.Text)) return "الشقة إجبارية";
            return null;
        }

        private void EnsureDeliveryAddressesLoadedForPhone(string phone, bool showErrors)
        {
            phone = (phone ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(phone)) return;

            if (DeliveryFormOverlay != null && DeliveryFormOverlay.Visibility == Visibility.Visible)
            {
                RefreshDeliveryFormBackendDataAsync(phone, null);
            }

            QueuePosOfflineSync();
        }

        private void SaveDeliveryFormAndCreateOrder()
        {
            if (IsDeliveryFormEditingAppOrder())
            {
                SaveAppDeliveryOrderAddressFromForm();
                return;
            }

            var prefillId = (_deliveryFormPrefillOrderId ?? string.Empty).Trim();
            var editingExisting = !string.IsNullOrWhiteSpace(prefillId);
            if (!editingExisting && TryBlockNewOrderWhileSyncing()) return;

            EnsureDeliveryStateLoaded();
            var err = ValidateDeliveryForm();
            if (!string.IsNullOrEmpty(err))
            {
                MessageBox.Show(err, "الدليفري", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var phone = DeliveryFormPhoneKey();
            var district = GetSelectedDeliveryDistrict();
            var fee = GetSelectedDeliveryDistrictFee();
            var customerName = string.IsNullOrWhiteSpace(DeliveryFormCustomerNameTextBox.Text) ? "عميل" : DeliveryFormCustomerNameTextBox.Text.Trim();
            EnsureDeliveryAddressesLoadedForPhone(phone, false);

            List<DeliverySavedAddressRecord> list;
            if (!_deliveryAddressesDb.TryGetValue(phone, out list) || list == null)
            {
                list = new List<DeliverySavedAddressRecord>();
                _deliveryAddressesDb[phone] = list;
            }

            var candidate = BuildDeliveryFormAddressCandidate(district);
            var candidateSig = DeliveryAddressSignature(candidate);

            DeliveryOrderRecord order = null;
            if (editingExisting)
            {
                for (int i = 0; i < _deliveryOrders.Count; i++)
                {
                    var existing = _deliveryOrders[i];
                    if (existing == null) continue;
                    if (!string.Equals(existing.Id, prefillId, StringComparison.Ordinal)) continue;
                    order = existing;
                    break;
                }

                if (order == null)
                {
                    CloseDeliveryForm();
                    RenderDeliveryQueue();
                    RenderDeliveryReceipt();
                    return;
                }

                if (IsDeliveryOrderUnchangedByForm(order, phone, customerName, district, fee, candidate))
                {
                    CloseDeliveryForm();
                    RenderDeliveryQueue();
                    RenderDeliveryReceipt();
                    return;
                }
            }

            DeliverySavedAddressRecord addrToUse = null;
            var selectedAddress = FindDeliveryAddressById(list, _deliveryFormSelectedAddressId);
            if (selectedAddress == null && editingExisting && order != null)
            {
                var orderAddressSig = DeliveryAddressSignature(BuildDeliverySavedAddressFromOrder(order));
                selectedAddress = FindDeliveryAddressBySignature(list, orderAddressSig);
            }

            var selectedAddressEdited = selectedAddress != null && !string.Equals(DeliveryAddressSignature(selectedAddress), candidateSig, StringComparison.Ordinal);
            if (selectedAddressEdited)
            {
                var decision = MessageBox.Show(
                    "تم تعديل عنوان محفوظ.\n\nنعم = تعديل العنوان الحالي\nلا = إضافة كعنوان جديد\nإلغاء = الرجوع بدون حفظ",
                    "حفظ العنوان",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);
                if (decision == MessageBoxResult.Cancel) return;
                if (decision == MessageBoxResult.Yes)
                {
                    addrToUse = UpdateExistingDeliveryAddress(phone, selectedAddress, candidate, list);
                }
                else
                {
                    addrToUse = SaveDeliveryAddressAsNew(phone, candidate, list);
                }
            }
            else
            {
                if (selectedAddress != null)
                {
                    addrToUse = selectedAddress;
                }
                if (addrToUse == null) addrToUse = FindDeliveryAddressBySignature(list, candidateSig);
                if (addrToUse == null) addrToUse = SaveDeliveryAddressAsNew(phone, candidate, list);
            }

            if (addrToUse == null) addrToUse = candidate;
            _deliveryFormSelectedAddressId = addrToUse.Id;

            if (!editingExisting)
            {
                order = CreateDeliveryOrder(phone, customerName, district, fee);
            }

            order.Address = new DeliveryAddressRecord
            {
                District = addrToUse.District,
                Block = addrToUse.Block,
                Street = addrToUse.Street,
                Building = addrToUse.Building,
                Apartment = addrToUse.Apartment,
                Floor = addrToUse.Floor,
                Note = addrToUse.Note
            };
            order.CustomerPhone = phone;
            order.CustomerName = customerName;
            order.District = district;
            order.DeliveryFee = fee;
            order.DeliveryName = "خدمة التوصيل";

            DeliveryRecalcOrder(order);
            SetDeliveryPickupContextActive(false);
            _deliveryCurrentOrderId = order.Id;
            _deliverySelectedRowKey = null;
            PersistDeliveryState();
            QueuePosOfflineSync();

            CloseDeliveryForm();
            RenderDeliveryQueue();
            RenderDeliveryReceipt();
        }

        private void SaveAppDeliveryOrderAddressFromForm()
        {
            var appOrderId = (_deliveryFormAppOrderId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(appOrderId)) return;

            var order = FindAppDeliveryOrderById(appOrderId);
            if (order == null)
            {
                CloseDeliveryForm();
                return;
            }

            var err = ValidateDeliveryForm();
            if (!string.IsNullOrEmpty(err))
            {
                MessageBox.Show(err, "الدليفري", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var district = GetSelectedDeliveryDistrict();
            var districtId = GetSelectedDeliveryDistrictId();
            if (string.IsNullOrWhiteSpace(districtId))
            {
                MessageBox.Show("تعذر تحديد الحي من الباك اند. حدّث الأحياء ثم حاول مرة أخرى.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var candidate = BuildDeliveryFormAddressCandidate(district);
            var candidateAddress = new DeliveryAddressRecord
            {
                District = candidate.District,
                Block = candidate.Block,
                Street = candidate.Street,
                Building = candidate.Building,
                Apartment = candidate.Apartment,
                Floor = candidate.Floor,
                Note = candidate.Note
            };
            var fee = GetSelectedDeliveryDistrictFee();

            var currentAddress = GetEditableAddressForAppDeliveryOrder(order);
            if (string.Equals((order.District ?? string.Empty).Trim(), district, StringComparison.Ordinal)
                && fee == GetStoredAppDeliveryOrderDeliveryFee(order)
                && string.Equals(DeliveryAddressSignature(candidate), DeliveryAddressSignature(BuildDeliverySavedAddressFromAddress(currentAddress)), StringComparison.Ordinal))
            {
                CloseDeliveryForm();
                return;
            }

            if (!TryBeginAppOrderBusyState(order.Id)) return;
            var selectedAddressId = !string.IsNullOrWhiteSpace(_deliveryFormSelectedAddressId)
                ? _deliveryFormSelectedAddressId.Trim()
                : (order.AddressId ?? string.Empty).Trim();
            ApplyAppDeliveryCustomerAddressChangeLocally(order, candidateAddress, fee, selectedAddressId);

            if (!QueueSupabaseBackendAppCustomerDeliveryAddressUpdate(order, districtId, candidateAddress, fee, selectedAddressId))
            {
                EndAppOrderBusyState(order.Id);
                MessageBox.Show("تعذر جدولة مزامنة تعديل عنوان طلب التطبيق.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            CloseDeliveryForm();
        }

        private void DeleteSelectedDeliveryAddressFromForm()
        {
            var phone = DeliveryFormPhoneKey();
            if (string.IsNullOrWhiteSpace(phone))
            {
                MessageBox.Show("اكتب رقم الهاتف أولاً", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (!_deliveryFormShowSavedAddresses)
            {
                MessageBox.Show("اضغط Enter بعد كتابة الرقم لإظهار العناوين.", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(_deliveryFormSelectedAddressId))
            {
                MessageBox.Show("حدد عنوان من القائمة بالأسفل أولاً", "الدليفري", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int serverId;
            var selectedAddressId = (_deliveryFormSelectedAddressId ?? string.Empty).Trim();
            var requiresServerDelete = int.TryParse(selectedAddressId, out serverId) && serverId > 0;
            var queuedServerDelete = false;
            if (requiresServerDelete)
            {
                string deleteError;
                if (TryDeletePosRegisteredAddress(selectedAddressId, out deleteError))
                {
                    MarkPendingDeliveryAddressDeleteAsSynced(selectedAddressId);
                }
                else
                {
                    EnqueuePendingDeliveryAddressDelete(phone, selectedAddressId);
                    queuedServerDelete = true;
                }
            }

            List<DeliverySavedAddressRecord> list;
            if (_deliveryAddressesDb.TryGetValue(phone, out list) && list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] != null && list[i].Id == selectedAddressId) list.RemoveAt(i);
                }
            }
            _deliveryFormSelectedAddressId = null;
            PersistDeliveryState();
            QueuePosOfflineSync();
            RenderDeliveryFormAddressList();
            MessageBox.Show(
                queuedServerDelete
                    ? "تم حذف العنوان محلياً وسيتم حذفه من السيرفر عند عودة الاتصال."
                    : "تم حذف العنوان.",
                "الدليفري",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private bool EnsureDeliveryOrderSelectedOrOpenForm()
        {
            if (GetCurrentDeliveryOrder() != null) return true;
            OpenDeliveryForm(false);
            return false;
        }

        private Brush GetDeliveryQueueDelayBackground(DeliveryOrderRecord order)
        {
            if (order == null || !order.KitchenPrintedAt.HasValue || order.Driver != null) return Brushes.White;
            var m = DeliveryMinutesSince(order.KitchenPrintedAt.Value);
            if (m < 3) return Brushes.White;

            var t = (m - 3) / 17.0;
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            var g = (byte)Math.Round(255 - (160 * t));
            var b = (byte)Math.Round(255 - (180 * t));
            return new SolidColorBrush(Color.FromRgb(255, g, b));
        }

        private void RebuildDeliveryShiftFilterCombo()
        {
            if (DeliveryShiftDriverFilterComboBox == null) return;

            var selected = DeliveryShiftDriverFilterComboBox.SelectedItem as ComboBoxItem;
            var selectedId = selected != null ? (selected.Tag as string ?? "ALL") : "ALL";

            DeliveryShiftDriverFilterComboBox.Items.Clear();
            DeliveryShiftDriverFilterComboBox.Items.Add(new ComboBoxItem { Content = "كل السائقين", Tag = "ALL" });
            for (int i = 0; i < _deliveryDrivers.Count; i++)
            {
                DeliveryShiftDriverFilterComboBox.Items.Add(new ComboBoxItem { Content = _deliveryDrivers[i].Name, Tag = _deliveryDrivers[i].Id });
            }

            for (int i = 0; i < DeliveryShiftDriverFilterComboBox.Items.Count; i++)
            {
                var item = DeliveryShiftDriverFilterComboBox.Items[i] as ComboBoxItem;
                if (item != null && string.Equals(item.Tag as string, selectedId, StringComparison.Ordinal))
                {
                    DeliveryShiftDriverFilterComboBox.SelectedIndex = i;
                    return;
                }
            }
            DeliveryShiftDriverFilterComboBox.SelectedIndex = 0;
        }

        private string GetDeliveryShiftFilterId()
        {
            var item = DeliveryShiftDriverFilterComboBox != null ? DeliveryShiftDriverFilterComboBox.SelectedItem as ComboBoxItem : null;
            return item != null ? (item.Tag as string ?? "ALL") : "ALL";
        }

        private string GetDeliveryShiftFilterTitle()
        {
            var id = GetDeliveryShiftFilterId();
            if (id == "ALL") return "كل السائقين";
            for (int i = 0; i < _deliveryDrivers.Count; i++) if (_deliveryDrivers[i].Id == id) return _deliveryDrivers[i].Name;
            return "سائق";
        }

        private List<DeliveryOrderRecord> GetFilteredDeliveryShiftOrders()
        {
            var list = new List<DeliveryOrderRecord>();
            var id = GetDeliveryShiftFilterId();
            for (int i = 0; i < _deliveryShiftOrders.Count; i++)
            {
                var o = _deliveryShiftOrders[i];
                if (o == null) continue;
                if (id != "ALL" && (o.Driver == null || o.Driver.Id != id)) continue;
                list.Add(o);
            }
            return list;
        }

        private FlowDocument BuildDeliveryShiftReportDocument(List<DeliveryOrderRecord> list)
        {
            var doc = new FlowDocument { FontFamily = new FontFamily("Tahoma"), FontSize = 12, FlowDirection = FlowDirection.RightToLeft };
            doc.Blocks.Add(new Paragraph(new Run("تقرير مبيعات اليوم")) { FontSize = 18, FontWeight = FontWeights.Black, Margin = new Thickness(0, 0, 0, 10) });
            doc.Blocks.Add(new Paragraph(new Run("فلتر التقرير: " + GetDeliveryShiftFilterTitle())) { FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });

            var restaurantSales = 0;
            var deliveryAccount = 0;
            for (int i = 0; i < list.Count; i++)
            {
                restaurantSales += list[i].Subtotal;
                deliveryAccount += list[i].DeliveryFee;
            }
            var grand = restaurantSales + deliveryAccount;

            var t = new Table();
            t.Columns.Add(new TableColumn { Width = new GridLength(280) });
            t.Columns.Add(new TableColumn { Width = new GridLength(140) });
            var g = new TableRowGroup();
            t.RowGroups.Add(g);
            AddDeliveryShiftReportRow(g, "عدد أوردرات الدليفري", list.Count.ToString(), true);
            AddDeliveryShiftReportRow(g, "إجمالي مبيعات المطعم (مبلغ الطلبات)", restaurantSales.ToString(), false);
            AddDeliveryShiftReportRow(g, "حساب الدليفري (خدمة التوصيل)", deliveryAccount.ToString(), false);
            AddDeliveryShiftReportRow(g, "الإجمالي الكلي", grand.ToString(), true);
            doc.Blocks.Add(t);
            return doc;
        }

        private void AddDeliveryShiftReportRow(TableRowGroup g, string label, string value, bool bold)
        {
            var r = new TableRow();
            var p1 = new Paragraph(new Run(label)) { Margin = new Thickness(0), TextAlignment = TextAlignment.Right, FlowDirection = FlowDirection.RightToLeft };
            var p2 = new Paragraph(new Run(value)) { Margin = new Thickness(0), TextAlignment = TextAlignment.Center, FlowDirection = FlowDirection.LeftToRight };
            if (bold) { p1.FontWeight = FontWeights.Black; p2.FontWeight = FontWeights.Black; }
            r.Cells.Add(new TableCell(p1) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(6) });
            r.Cells.Add(new TableCell(p2) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(6) });
            g.Rows.Add(r);
        }

        private void DeliveryFormOverlay_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DeliveryFormOverlay == null || DeliveryFormOverlay.Visibility != Visibility.Visible) return;
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CloseDeliveryDistrictPickerPopup();
                CloseDeliveryForm();
            }
        }

        private void DeliveryFormPhoneTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                EnsureDeliveryAddressesLoadedForPhone(DeliveryFormPhoneKey(), false);
                _deliveryFormShowSavedAddresses = true;
                RenderDeliveryFormAddressList();
                MoveDeliveryFormFocusNext(sender as Control);
            }
        }

        private void DeliveryFormInput_KeyDown(object sender, KeyEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            if (e.Key == Key.Down)
            {
                if (TryMoveDeliveryFormSuggestSelection(1))
                {
                    e.Handled = true;
                    return;
                }
                return;
            }

            if (e.Key == Key.Up)
            {
                if (TryMoveDeliveryFormSuggestSelection(-1))
                {
                    e.Handled = true;
                    return;
                }
                return;
            }

            if (e.Key == Key.Escape)
            {
                if (IsDeliveryFormSuggestPopupOpen())
                {
                    CloseDeliveryFormSuggestPopup();
                    e.Handled = true;
                }
                return;
            }

            if (e.Key != Key.Enter) return;
            e.Handled = true;
            if (TryApplyDeliveryFormSuggestion(tb))
            {
                MoveDeliveryFormFocusNext(tb);
                return;
            }
            MoveDeliveryFormFocusNext(tb);
        }

        private void DeliveryFormDistrictComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null) return;

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (!IsDeliveryDistrictPickerOpen())
                {
                    OpenDeliveryDistrictPickerPopup();
                    return;
                }

                if (ApplySelectedDistrictFromPicker())
                {
                    CloseDeliveryDistrictPickerPopup();
                    MoveDeliveryFormFocusNext(combo);
                    return;
                }
                return;
            }

            if (e.Key == Key.Down || e.Key == Key.Up)
            {
                if (!IsDeliveryDistrictPickerOpen()) OpenDeliveryDistrictPickerPopup();
                var listBox = FindName("DeliveryDistrictListBox") as ListBox;
                if (listBox != null) listBox.Focus();
                MoveDistrictPickerSelection(e.Key == Key.Down ? 1 : -1);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape && IsDeliveryDistrictPickerOpen())
            {
                CloseDeliveryDistrictPickerPopup();
                e.Handled = true;
            }
        }

        private void MoveDeliveryFormFocusNext(Control current)
        {
            if (current == null) return;

            var order = new Control[]
            {
                DeliveryFormPhoneTextBox,
                DeliveryFormCustomerNameTextBox,
                DeliveryFormDistrictComboBox,
                DeliveryFormBlockTextBox,
                DeliveryFormStreetTextBox,
                DeliveryFormBuildingTextBox,
                DeliveryFormApartmentTextBox,
                DeliveryFormFloorTextBox,
                DeliveryFormAddressNoteTextBox
            };

            var idx = -1;
            for (int i = 0; i < order.Length; i++)
            {
                if (ReferenceEquals(order[i], current))
                {
                    idx = i;
                    break;
                }
            }
            if (idx < 0) return;

            for (int i = idx + 1; i < order.Length; i++)
            {
                var target = order[i];
                if (target == null || !target.IsEnabled || target.Visibility != Visibility.Visible) continue;
                target.Focus();
                var tb = target as TextBox;
                if (tb != null) tb.SelectAll();
                if (ReferenceEquals(target, DeliveryFormDistrictComboBox))
                {
                    OpenDeliveryDistrictPickerPopup();
                }
                return;
            }

            if (DeliveryFormSaveButton != null && DeliveryFormSaveButton.IsEnabled && DeliveryFormSaveButton.Visibility == Visibility.Visible)
            {
                DeliveryFormSaveButton.Focus();
            }
        }

        private void DeliveryFormField_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateDeliveryFormAddressPreview();
            var tb = sender as TextBox;
            if (tb == null) return;

            if (ReferenceEquals(tb, DeliveryFormPhoneTextBox))
            {
                _deliveryFormShowSavedAddresses = true;
                EnsureDeliveryAddressesLoadedForPhone(DeliveryFormPhoneKey(), false);
                RenderDeliveryFormAddressList();
                return;
            }

            if (!IsDeliveryFormSuggestField(tb))
            {
                if (_deliveryFormSuggestTargetTextBox != null && !ReferenceEquals(_deliveryFormSuggestTargetTextBox, tb))
                {
                    CloseDeliveryFormSuggestPopup();
                }
                return;
            }
            UpdateDeliveryFormSuggestForTextBox(tb);
        }

        private void DeliveryFormSuggestListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DeliveryFormSuggestListBox == null || _deliveryFormSuggestTargetTextBox == null) return;
            var element = e.OriginalSource as DependencyObject;
            while (element != null && !(element is ListBoxItem)) element = VisualTreeHelper.GetParent(element);
            var item = element as ListBoxItem;
            if (item == null) return;
            var value = item.Content as string;
            if (string.IsNullOrWhiteSpace(value)) return;
            _deliveryFormSuggestTargetTextBox.Text = value;
            _deliveryFormSuggestTargetTextBox.CaretIndex = _deliveryFormSuggestTargetTextBox.Text.Length;
            var target = _deliveryFormSuggestTargetTextBox;
            CloseDeliveryFormSuggestPopup();
            e.Handled = true;
            Dispatcher.BeginInvoke(new Action(delegate
            {
                MoveDeliveryFormFocusNext(target);
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private bool IsDeliveryFormSuggestField(TextBox tb)
        {
            return ReferenceEquals(tb, DeliveryFormBlockTextBox)
                || ReferenceEquals(tb, DeliveryFormStreetTextBox)
                || ReferenceEquals(tb, DeliveryFormBuildingTextBox)
                || ReferenceEquals(tb, DeliveryFormApartmentTextBox);
        }

        private string GetDeliverySuggestFieldValue(DeliverySavedAddressRecord a, TextBox tb)
        {
            if (a == null || tb == null) return null;
            if (ReferenceEquals(tb, DeliveryFormBlockTextBox)) return a.Block;
            if (ReferenceEquals(tb, DeliveryFormStreetTextBox)) return a.Street;
            if (ReferenceEquals(tb, DeliveryFormBuildingTextBox)) return a.Building;
            if (ReferenceEquals(tb, DeliveryFormApartmentTextBox)) return a.Apartment;
            return null;
        }

        private string NormalizeSuggestQuery(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private void UpdateDeliveryFormSuggestForTextBox(TextBox tb)
        {
            if (tb == null || !IsDeliveryFormSuggestField(tb))
            {
                CloseDeliveryFormSuggestPopup();
                return;
            }

            var query = NormalizeSuggestQuery(tb.Text);
            if (string.IsNullOrWhiteSpace(query))
            {
                CloseDeliveryFormSuggestPopup();
                return;
            }

            _deliveryFormSuggestValues.Clear();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in _deliveryAddressesDb)
            {
                var list = kv.Value;
                if (list == null) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    var value = NormalizeSuggestQuery(GetDeliverySuggestFieldValue(list[i], tb));
                    if (string.IsNullOrWhiteSpace(value)) continue;
                    if (value.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (seen.Add(value)) _deliveryFormSuggestValues.Add(value);
                    if (_deliveryFormSuggestValues.Count >= 30) break;
                }
                if (_deliveryFormSuggestValues.Count >= 30) break;
            }

            if (_deliveryFormSuggestValues.Count == 0)
            {
                CloseDeliveryFormSuggestPopup();
                return;
            }

            _deliveryFormSuggestTargetTextBox = tb;
            _deliveryFormSuggestSelIndex = 0;
            ShowDeliveryFormSuggestPopup();
        }

        private void ShowDeliveryFormSuggestPopup()
        {
            if (DeliveryFormSuggestPopup == null || DeliveryFormSuggestListBox == null || _deliveryFormSuggestTargetTextBox == null) return;

            DeliveryFormSuggestListBox.Items.Clear();
            for (int i = 0; i < _deliveryFormSuggestValues.Count; i++)
            {
                DeliveryFormSuggestListBox.Items.Add(_deliveryFormSuggestValues[i]);
            }

            if (DeliveryFormSuggestPopupBorder != null)
            {
                DeliveryFormSuggestPopupBorder.Width = Math.Max(180, _deliveryFormSuggestTargetTextBox.ActualWidth);
            }

            DeliveryFormSuggestPopup.PlacementTarget = _deliveryFormSuggestTargetTextBox;
            DeliveryFormSuggestPopup.HorizontalOffset = 0;
            DeliveryFormSuggestPopup.VerticalOffset = 2;
            DeliveryFormSuggestPopup.IsOpen = true;
            if (DeliveryFormSuggestListBox.Items.Count > 0)
            {
                DeliveryFormSuggestListBox.SelectedIndex = Math.Max(0, Math.Min(_deliveryFormSuggestSelIndex, DeliveryFormSuggestListBox.Items.Count - 1));
                DeliveryFormSuggestListBox.ScrollIntoView(DeliveryFormSuggestListBox.SelectedItem);
            }
        }

        private bool IsDeliveryFormSuggestPopupOpen()
        {
            return DeliveryFormSuggestPopup != null && DeliveryFormSuggestPopup.IsOpen;
        }

        private void CloseDeliveryFormSuggestPopup()
        {
            if (DeliveryFormSuggestPopup != null) DeliveryFormSuggestPopup.IsOpen = false;
            if (DeliveryFormSuggestListBox != null) DeliveryFormSuggestListBox.Items.Clear();
            _deliveryFormSuggestTargetTextBox = null;
            _deliveryFormSuggestValues.Clear();
            _deliveryFormSuggestSelIndex = -1;
        }

        private bool TryMoveDeliveryFormSuggestSelection(int delta)
        {
            if (!IsDeliveryFormSuggestPopupOpen() || DeliveryFormSuggestListBox == null || _deliveryFormSuggestTargetTextBox == null) return false;
            if (DeliveryFormSuggestListBox.Items.Count == 0) return false;

            if (_deliveryFormSuggestSelIndex < 0) _deliveryFormSuggestSelIndex = 0;
            _deliveryFormSuggestSelIndex += delta;
            if (_deliveryFormSuggestSelIndex < 0) _deliveryFormSuggestSelIndex = 0;
            if (_deliveryFormSuggestSelIndex >= DeliveryFormSuggestListBox.Items.Count) _deliveryFormSuggestSelIndex = DeliveryFormSuggestListBox.Items.Count - 1;

            DeliveryFormSuggestListBox.SelectedIndex = _deliveryFormSuggestSelIndex;
            DeliveryFormSuggestListBox.ScrollIntoView(DeliveryFormSuggestListBox.SelectedItem);
            return true;
        }

        private bool TryApplyDeliveryFormSuggestion(TextBox source)
        {
            if (source == null) return false;
            if (!IsDeliveryFormSuggestPopupOpen()) return false;
            if (!ReferenceEquals(_deliveryFormSuggestTargetTextBox, source)) return false;
            if (DeliveryFormSuggestListBox == null || DeliveryFormSuggestListBox.Items.Count == 0) return false;

            if (_deliveryFormSuggestSelIndex < 0) _deliveryFormSuggestSelIndex = 0;
            if (_deliveryFormSuggestSelIndex >= DeliveryFormSuggestListBox.Items.Count) _deliveryFormSuggestSelIndex = DeliveryFormSuggestListBox.Items.Count - 1;
            var value = DeliveryFormSuggestListBox.Items[_deliveryFormSuggestSelIndex] as string;
            if (string.IsNullOrWhiteSpace(value)) return false;

            source.Text = value;
            source.CaretIndex = source.Text.Length;
            CloseDeliveryFormSuggestPopup();
            return true;
        }

        private bool TrySyncDeliveryDistrictSelectionFromText(ComboBox combo)
        {
            if (combo == null) return false;
            var text = (combo.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                combo.SelectedIndex = 0;
                return false;
            }

            var normalized = text;
            for (int i = 0; i < combo.Items.Count; i++)
            {
                var item = combo.Items[i] as ComboBoxItem;
                if (item == null) continue;
                var tag = (item.Tag as string ?? string.Empty).Trim();
                var content = (item.Content as string ?? string.Empty).Trim();
                if (string.Equals(normalized, tag, StringComparison.OrdinalIgnoreCase) ||
                    tag.StartsWith(normalized, StringComparison.OrdinalIgnoreCase) ||
                    content.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return !string.IsNullOrWhiteSpace(tag);
                }
            }
            return false;
        }

        private bool IsDeliveryDistrictPickerOpen()
        {
            var popup = FindName("DeliveryDistrictPickerPopup") as Popup;
            return popup != null && popup.IsOpen;
        }

        private void OpenDeliveryDistrictPickerPopup()
        {
            var popup = FindName("DeliveryDistrictPickerPopup") as Popup;
            var popupBorder = FindName("DeliveryDistrictPickerPopupBorder") as Border;
            var searchTextBox = FindName("DeliveryDistrictSearchTextBox") as TextBox;
            var listBox = FindName("DeliveryDistrictListBox") as ListBox;
            if (DeliveryFormDistrictComboBox == null || popup == null || searchTextBox == null || listBox == null) return;
            EnsureDeliveryFormUiInitialized();
            TryRefreshDeliveryDistrictCatalogFromBackend(false);
            RebuildDeliveryDistrictComboItems(GetSelectedDeliveryDistrict());
            if (_deliveryDistrictCatalog.Count == 0 && popup.IsOpen)
            {
                ShowSupabaseServerError("لا توجد أحياء", "تعذر تحميل الأحياء من جدول hoods في الباك اند.");
                return;
            }

            if (popupBorder != null)
            {
                popupBorder.Width = Math.Max(360, DeliveryFormDistrictComboBox.ActualWidth);
            }

            searchTextBox.Text = string.Empty;
            RebuildDeliveryDistrictPickerList(null);
            popup.PlacementTarget = DeliveryFormDistrictComboBox;
            popup.HorizontalOffset = 0;
            popup.VerticalOffset = 2;
            popup.IsOpen = true;

            Dispatcher.BeginInvoke(new Action(delegate
            {
                searchTextBox.Focus();
                searchTextBox.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void CloseDeliveryDistrictPickerPopup()
        {
            var popup = FindName("DeliveryDistrictPickerPopup") as Popup;
            var searchTextBox = FindName("DeliveryDistrictSearchTextBox") as TextBox;
            var listBox = FindName("DeliveryDistrictListBox") as ListBox;
            if (popup != null) popup.IsOpen = false;
            _deliveryDistrictFiltered.Clear();
            if (listBox != null) listBox.Items.Clear();
            if (searchTextBox != null) searchTextBox.Text = string.Empty;
        }

        private void RebuildDeliveryDistrictPickerList(string query)
        {
            var listBox = FindName("DeliveryDistrictListBox") as ListBox;
            if (listBox == null) return;
            _deliveryDistrictFiltered.Clear();
            listBox.Items.Clear();
            var q = (query ?? string.Empty).Trim();
            for (int i = 0; i < _deliveryDistrictCatalog.Count; i++)
            {
                var d = _deliveryDistrictCatalog[i];
                if (d == null || string.IsNullOrWhiteSpace(d.Name)) continue;
                if (!string.IsNullOrWhiteSpace(q) && d.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                _deliveryDistrictFiltered.Add(d);
            }

            if (_deliveryDistrictFiltered.Count == 0)
            {
                listBox.Items.Add(new ListBoxItem
                {
                    Content = _deliveryDistrictCatalog.Count == 0
                        ? "جاري تحميل الأحياء..."
                        : "لا توجد نتائج مطابقة",
                    IsEnabled = false,
                    FlowDirection = FlowDirection.RightToLeft
                });
                listBox.SelectedIndex = -1;
                return;
            }

            for (int i = 0; i < _deliveryDistrictFiltered.Count; i++)
            {
                var d = _deliveryDistrictFiltered[i];
                listBox.Items.Add(new ListBoxItem
                {
                    Tag = d,
                    Content = d.Name + " (توصيل " + d.Fee + ")",
                    FontWeight = FontWeights.Bold,
                    FlowDirection = FlowDirection.RightToLeft
                });
            }

            if (listBox.Items.Count > 0)
            {
                var selectedIndex = 0;
                if (string.IsNullOrWhiteSpace(q))
                {
                    var selectedDistrict = GetSelectedDeliveryDistrict();
                    if (!string.IsNullOrWhiteSpace(selectedDistrict))
                    {
                        for (int i = 0; i < listBox.Items.Count; i++)
                        {
                            var item = listBox.Items[i] as ListBoxItem;
                            var d = item == null ? null : item.Tag as DeliveryDistrictFeeRecord;
                            if (d != null && string.Equals(d.Name, selectedDistrict, StringComparison.Ordinal))
                            {
                                selectedIndex = i;
                                break;
                            }
                        }
                    }
                }
                listBox.SelectedIndex = selectedIndex;
                listBox.ScrollIntoView(listBox.SelectedItem);
            }
        }

        private bool MoveDistrictPickerSelection(int delta)
        {
            var listBox = FindName("DeliveryDistrictListBox") as ListBox;
            if (listBox == null || listBox.Items.Count == 0) return false;
            var next = listBox.SelectedIndex;
            if (next < 0)
            {
                next = delta >= 0 ? 0 : listBox.Items.Count - 1;
            }
            else
            {
                next += delta;
                if (next < 0) next = 0;
                if (next >= listBox.Items.Count) next = listBox.Items.Count - 1;
            }
            listBox.SelectedIndex = next;
            listBox.ScrollIntoView(listBox.SelectedItem);
            return true;
        }

        private bool ApplySelectedDistrictFromPicker()
        {
            var listBox = FindName("DeliveryDistrictListBox") as ListBox;
            if (listBox == null) return false;
            var item = listBox.SelectedItem as ListBoxItem;
            if (item == null && listBox.Items.Count > 0)
            {
                listBox.SelectedIndex = 0;
                item = listBox.SelectedItem as ListBoxItem;
            }
            var district = item == null ? null : item.Tag as DeliveryDistrictFeeRecord;
            if (district == null || string.IsNullOrWhiteSpace(district.Name)) return false;
            SetDeliveryDistrictSelection(district.Name);
            UpdateDeliveryFormAddressPreview();
            return true;
        }

        private void DeliveryFormDistrictComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDeliveryFormAddressPreview();
        }

        private void DeliveryDistrictSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTextBox = FindName("DeliveryDistrictSearchTextBox") as TextBox;
            RebuildDeliveryDistrictPickerList(searchTextBox == null ? string.Empty : searchTextBox.Text);
        }

        private void DeliveryDistrictSearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (MoveDistrictPickerSelection(1))
                {
                    var listBox = FindName("DeliveryDistrictListBox") as ListBox;
                    if (listBox != null) listBox.Focus();
                    e.Handled = true;
                }
                return;
            }
            if (e.Key == Key.Up)
            {
                if (MoveDistrictPickerSelection(-1))
                {
                    var listBox = FindName("DeliveryDistrictListBox") as ListBox;
                    if (listBox != null) listBox.Focus();
                    e.Handled = true;
                }
                return;
            }
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (!ApplySelectedDistrictFromPicker()) return;
                CloseDeliveryDistrictPickerPopup();
                MoveDeliveryFormFocusNext(DeliveryFormDistrictComboBox);
                return;
            }
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CloseDeliveryDistrictPickerPopup();
                DeliveryFormDistrictComboBox?.Focus();
            }
        }

        private void DeliveryDistrictListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!ApplySelectedDistrictFromPicker()) return;
            CloseDeliveryDistrictPickerPopup();
            MoveDeliveryFormFocusNext(DeliveryFormDistrictComboBox);
        }

        private void DeliveryDistrictListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            if (!ApplySelectedDistrictFromPicker()) return;
            CloseDeliveryDistrictPickerPopup();
            MoveDeliveryFormFocusNext(DeliveryFormDistrictComboBox);
        }

        private void DeliveryFormSaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveDeliveryFormAndCreateOrder();
        }

        private void DeliveryFormCancelButton_Click(object sender, RoutedEventArgs e)
        {
            CloseDeliveryForm();
        }

        private void DeliveryFormDeleteAddressButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedDeliveryAddressFromForm();
        }

        private void DeliveryShiftFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeliveryShiftDriverFilterComboBox == null) return;
            DeliveryShiftDriverFilterComboBox.Focus();
            DeliveryShiftDriverFilterComboBox.IsDropDownOpen = true;
        }

        private void DeliveryShiftDriverFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeliveryShiftOverlay != null && DeliveryShiftOverlay.Visibility == Visibility.Visible) RenderDeliveryShiftRows();
        }

        private void DeliveryShiftReportButton_Click(object sender, RoutedEventArgs e)
        {
            TryPrintFlowDocument(BuildDeliveryShiftReportDocument(GetFilteredDeliveryShiftOrders()));
        }


        private class DeliveryDistrictFeeRecord
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int Fee { get; set; }
        }

        private List<DeliveryDistrictStateRecord> BuildDeliveryDistrictStateSnapshot()
        {
            var list = new List<DeliveryDistrictStateRecord>();
            for (int i = 0; i < _deliveryDistrictCatalog.Count; i++)
            {
                var d = _deliveryDistrictCatalog[i];
                if (d == null || string.IsNullOrWhiteSpace(d.Name)) continue;
                list.Add(new DeliveryDistrictStateRecord
                {
                    Id = d.Id,
                    Name = d.Name,
                    Fee = d.Fee
                });
            }
            return list;
        }

        private List<DeliveryDriverRecord> BuildDeliveryDriversStateSnapshot()
        {
            var list = new List<DeliveryDriverRecord>();
            for (int i = 0; i < _deliveryDrivers.Count; i++)
            {
                var d = _deliveryDrivers[i];
                if (d == null || string.IsNullOrWhiteSpace(d.Id)) continue;
                list.Add(new DeliveryDriverRecord
                {
                    Id = d.Id,
                    Name = d.Name,
                    Phone = d.Phone,
                    AppUserId = d.AppUserId
                });
            }
            return list;
        }

        private List<AppDeliveryOrderStateRecord> BuildAppDeliveryStateSnapshot()
        {
            var list = new List<AppDeliveryOrderStateRecord>();
            for (int i = 0; i < _deliveryAppDeliveryOrders.Count; i++)
            {
                var o = _deliveryAppDeliveryOrders[i];
                if (o == null || string.IsNullOrWhiteSpace(o.Id)) continue;
                var row = new AppDeliveryOrderStateRecord
                {
                    Id = o.Id,
                    CustomerUserId = o.CustomerUserId,
                    AddressId = o.AddressId,
                    No = o.No,
                    Name = o.Name,
                    Phone = o.Phone,
                    District = o.District,
                    AddressText = o.AddressText,
                    Address = o.Address == null ? null : new DeliveryAddressRecord
                    {
                        District = o.Address.District,
                        Block = o.Address.Block,
                        Street = o.Address.Street,
                        Building = o.Address.Building,
                        Apartment = o.Address.Apartment,
                        Floor = o.Address.Floor,
                        Note = o.Address.Note
                    },
                    CreatedAt = o.CreatedAt,
                    Subtotal = GetStoredAppDeliveryOrderSubtotal(o),
                    DeliveryFee = GetStoredAppDeliveryOrderDeliveryFee(o),
                    Total = GetStoredAppDeliveryOrderTotal(o),
                    Status = ToCachedAppDeliveryStatus(o.Status),
                    KitchenPrinted = o.KitchenPrinted,
                    DriverReceiptPrinted = o.DriverReceiptPrinted,
                    Driver = o.Driver == null ? null : new DeliveryDriverRecord
                    {
                        Id = o.Driver.Id,
                        Name = o.Driver.Name,
                        Phone = o.Driver.Phone,
                        AppUserId = o.Driver.AppUserId
                    },
                    Items = new List<AppOrderItemStateRecord>()
                };
                if (o.Items != null)
                {
                    for (int j = 0; j < o.Items.Count; j++)
                    {
                        var it = o.Items[j];
                        if (it == null) continue;
                        row.Items.Add(new AppOrderItemStateRecord
                        {
                            Name = it.Name,
                            Qty = it.Qty,
                            Price = it.Price
                        });
                    }
                }
                list.Add(row);
            }
            return list;
        }

        private List<AppPickupOrderStateRecord> BuildAppPickupStateSnapshot()
        {
            var list = new List<AppPickupOrderStateRecord>();
            for (int i = 0; i < _deliveryAppPickupOrders.Count; i++)
            {
                var o = _deliveryAppPickupOrders[i];
                if (o == null || string.IsNullOrWhiteSpace(o.Id)) continue;
                var row = new AppPickupOrderStateRecord
                {
                    Id = o.Id,
                    CustomerUserId = o.CustomerUserId,
                    No = o.No,
                    Name = o.Name,
                    Phone = o.Phone,
                    District = o.District,
                    AddressText = o.AddressText,
                    CreatedAt = o.CreatedAt,
                    Status = ToCachedAppPickupStatus(o.Status),
                    PartnerReceiptPrinted = o.PartnerReceiptPrinted,
                    Items = new List<AppOrderItemStateRecord>()
                };
                if (o.Items != null)
                {
                    for (int j = 0; j < o.Items.Count; j++)
                    {
                        var it = o.Items[j];
                        if (it == null) continue;
                        row.Items.Add(new AppOrderItemStateRecord
                        {
                            Name = it.Name,
                            Qty = it.Qty,
                            Price = it.Price
                        });
                    }
                }
                list.Add(row);
            }
            return list;
        }

        private string ToCachedAppDeliveryStatus(AppDeliveryOrderStatus status)
        {
            if (status == AppDeliveryOrderStatus.Preparing) return "PREPARING";
            if (status == AppDeliveryOrderStatus.DriverAssigned) return "DRIVER_ASSIGNED";
            if (status == AppDeliveryOrderStatus.Done) return "DONE";
            return "PENDING";
        }

        private AppDeliveryOrderStatus ParseCachedAppDeliveryStatus(string raw)
        {
            var s = (raw ?? string.Empty).Trim().ToUpperInvariant();
            if (s == "PREPARING") return AppDeliveryOrderStatus.Preparing;
            if (s == "DRIVER_ASSIGNED" || s == "ON_ROAD" || s == "DRIVER_PICKED") return AppDeliveryOrderStatus.DriverAssigned;
            if (s == "DONE" || s == "DELIVERED") return AppDeliveryOrderStatus.Done;
            return AppDeliveryOrderStatus.Pending;
        }

        private string ToCachedAppPickupStatus(AppPickupOrderStatus status)
        {
            if (status == AppPickupOrderStatus.Accepted) return "ACCEPTED";
            if (status == AppPickupOrderStatus.Preparing) return "PREPARING";
            if (status == AppPickupOrderStatus.Ready) return "READY";
            if (status == AppPickupOrderStatus.Delivered) return "DELIVERED";
            return "PENDING";
        }

        private AppPickupOrderStatus ParseCachedAppPickupStatus(string raw)
        {
            var s = (raw ?? string.Empty).Trim().ToUpperInvariant();
            if (s == "ACCEPTED") return AppPickupOrderStatus.Accepted;
            if (s == "PREPARING") return AppPickupOrderStatus.Preparing;
            if (s == "READY") return AppPickupOrderStatus.Ready;
            if (s == "DELIVERED" || s == "DONE") return AppPickupOrderStatus.Delivered;
            return AppPickupOrderStatus.Pending;
        }

        [DataContract]
        private sealed class SupabaseHoodRow
        {
            [DataMember(Name = "id", EmitDefaultValue = false)] public string Id { get; set; }
            [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }
            [DataMember(Name = "fee", EmitDefaultValue = false)] public double Fee { get; set; }
        }

        [DataContract]
        private class DeliveryStateRecord
        {
            [DataMember(Name = "delivery_seq")] public int OrderSequence { get; set; }
            [DataMember(Name = "delivery_current_id")] public string CurrentOrderId { get; set; }
            [DataMember(Name = "delivery_active_cat")] public string ActiveCategory { get; set; }
            [DataMember(Name = "delivery_orders")] public List<DeliveryOrderRecord> OpenOrders { get; set; }
            [DataMember(Name = "delivery_shift_orders")] public List<DeliveryOrderRecord> ShiftOrders { get; set; }
            [DataMember(Name = "delivery_notes_history")] public List<string> NotesHistory { get; set; }
            [DataMember(Name = "delivery_addresses")] public Dictionary<string, List<DeliverySavedAddressRecord>> AddressesByPhone { get; set; }
            [DataMember(Name = "delivery_district_catalog")] public List<DeliveryDistrictStateRecord> DistrictCatalog { get; set; }
            [DataMember(Name = "delivery_drivers")] public List<DeliveryDriverRecord> Drivers { get; set; }
            [DataMember(Name = "app_delivery_order_seq")] public int AppDeliveryOrderSequence { get; set; }
            [DataMember(Name = "app_pickup_order_seq")] public int AppPickupOrderSequence { get; set; }
            [DataMember(Name = "app_delivery_orders")] public List<AppDeliveryOrderStateRecord> AppDeliveryOrders { get; set; }
            [DataMember(Name = "app_pickup_orders")] public List<AppPickupOrderStateRecord> AppPickupOrders { get; set; }
        }

        [DataContract]
        private class DeliveryDistrictStateRecord
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "fee")] public int Fee { get; set; }
        }

        [DataContract]
        private class AppOrderItemStateRecord
        {
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "qty")] public int Qty { get; set; }
            [DataMember(Name = "price")] public int Price { get; set; }
        }

        [DataContract]
        private class AppDeliveryOrderStateRecord
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "customer_user_id")] public string CustomerUserId { get; set; }
            [DataMember(Name = "address_id", EmitDefaultValue = false)] public string AddressId { get; set; }
            [DataMember(Name = "no")] public int No { get; set; }
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "phone")] public string Phone { get; set; }
            [DataMember(Name = "district", EmitDefaultValue = false)] public string District { get; set; }
            [DataMember(Name = "address_text", EmitDefaultValue = false)] public string AddressText { get; set; }
            [DataMember(Name = "address", EmitDefaultValue = false)] public DeliveryAddressRecord Address { get; set; }
            [DataMember(Name = "created_at")] public long CreatedAt { get; set; }
            [DataMember(Name = "subtotal", EmitDefaultValue = false)] public int Subtotal { get; set; }
            [DataMember(Name = "delivery_fee", EmitDefaultValue = false)] public int DeliveryFee { get; set; }
            [DataMember(Name = "total", EmitDefaultValue = false)] public int Total { get; set; }
            [DataMember(Name = "status")] public string Status { get; set; }
            [DataMember(Name = "kitchen_printed")] public bool KitchenPrinted { get; set; }
            [DataMember(Name = "driver_receipt_printed")] public bool DriverReceiptPrinted { get; set; }
            [DataMember(Name = "driver")] public DeliveryDriverRecord Driver { get; set; }
            [DataMember(Name = "items")] public List<AppOrderItemStateRecord> Items { get; set; }
        }

        [DataContract]
        private class AppPickupOrderStateRecord
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "customer_user_id")] public string CustomerUserId { get; set; }
            [DataMember(Name = "no")] public int No { get; set; }
            [DataMember(Name = "name")] public string Name { get; set; }
            [DataMember(Name = "phone")] public string Phone { get; set; }
            [DataMember(Name = "district", EmitDefaultValue = false)] public string District { get; set; }
            [DataMember(Name = "address_text", EmitDefaultValue = false)] public string AddressText { get; set; }
            [DataMember(Name = "created_at")] public long CreatedAt { get; set; }
            [DataMember(Name = "status")] public string Status { get; set; }
            [DataMember(Name = "partner_receipt_printed")] public bool PartnerReceiptPrinted { get; set; }
            [DataMember(Name = "items")] public List<AppOrderItemStateRecord> Items { get; set; }
        }

        [DataContract]
        private class DeliverySavedAddressRecord
        {
            [DataMember(Name = "id")] public string Id { get; set; }
            [DataMember(Name = "district")] public string District { get; set; }
            [DataMember(Name = "block")] public string Block { get; set; }
            [DataMember(Name = "street")] public string Street { get; set; }
            [DataMember(Name = "building")] public string Building { get; set; }
            [DataMember(Name = "apartment")] public string Apartment { get; set; }
            [DataMember(Name = "floor")] public string Floor { get; set; }
            [DataMember(Name = "note")] public string Note { get; set; }
        }

        [DataContract]
        private class DeliveryAddressRecord
        {
            [DataMember(Name = "district")] public string District { get; set; }
            [DataMember(Name = "block")] public string Block { get; set; }
            [DataMember(Name = "street")] public string Street { get; set; }
            [DataMember(Name = "building")] public string Building { get; set; }
            [DataMember(Name = "apartment")] public string Apartment { get; set; }
            [DataMember(Name = "floor")] public string Floor { get; set; }
            [DataMember(Name = "note")] public string Note { get; set; }
        }
    }
}

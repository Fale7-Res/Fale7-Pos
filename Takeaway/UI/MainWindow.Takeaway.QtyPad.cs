using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        private string _qtyEditingKey;
        private string _qtyBuffer = string.Empty;

        private void OpenQtyPad(string itemKey)
        {
            var o = GetActiveTakeawayContextOrder();
            if (o == null) return;
            for (int i = 0; i < o.Items.Count; i++)
            {
                var it = o.Items[i];
                if (it.Key != itemKey) continue;
                _qtyEditingKey = itemKey;
                _qtyBuffer = (it.Qty <= 0 ? 1 : it.Qty).ToString();
                QtyDisplayText.Text = _qtyBuffer;
                QtyOverlay.Visibility = Visibility.Visible;
                return;
            }
        }

        private void CloseQtyPad()
        {
            QtyOverlay.Visibility = Visibility.Collapsed;
            _qtyEditingKey = null;
            _qtyBuffer = string.Empty;
            QtyDisplayText.Text = "1";
        }

        private void AppendQtyDigit(string digit)
        {
            if (string.IsNullOrEmpty(digit)) return;
            if (_qtyBuffer == "0") _qtyBuffer = string.Empty;
            _qtyBuffer += digit;
            if (_qtyBuffer.Length > 6) _qtyBuffer = _qtyBuffer.Substring(0, 6);
            QtyDisplayText.Text = string.IsNullOrEmpty(_qtyBuffer) ? "0" : _qtyBuffer;
        }

        private void QtyDelete()
        {
            if (string.IsNullOrEmpty(_qtyBuffer)) { QtyDisplayText.Text = "0"; return; }
            _qtyBuffer = _qtyBuffer.Substring(0, _qtyBuffer.Length - 1);
            QtyDisplayText.Text = string.IsNullOrEmpty(_qtyBuffer) ? "0" : _qtyBuffer;
        }

        private void QtyApply()
        {
            var o = GetActiveTakeawayContextOrder();
            if (o == null) { CloseQtyPad(); return; }
            TakeawayLineRecord target = null;
            for (int i = 0; i < o.Items.Count; i++) if (o.Items[i].Key == _qtyEditingKey) { target = o.Items[i]; break; }
            if (target == null) { CloseQtyPad(); return; }

            if (!int.TryParse(string.IsNullOrEmpty(_qtyBuffer) ? "0" : _qtyBuffer, out var n)) n = 1;
            if (n <= 0) n = 1;
            var menuItem = FindMenuItemDefinitionForLine(target.ProductId, target.SourceProductId, target.OptionIds);
            if (menuItem != null)
            {
                BuildLocalInventoryReservationCounters(out var productReservations, out var optionReservations);
                var currentQty = target.Qty <= 0 ? 1 : target.Qty;
                GetMenuItemInventoryStatus(menuItem, productReservations, optionReservations, out var effectiveRemaining);
                if (effectiveRemaining.HasValue)
                {
                    var maxAllowed = currentQty + effectiveRemaining.Value;
                    if (maxAllowed <= 0) maxAllowed = currentQty;
                    if (n > maxAllowed)
                    {
                        n = maxAllowed;
                        MessageBox.Show(
                            "\u0627\u0644\u0643\u0645\u064a\u0629 \u0627\u0644\u0645\u062a\u0627\u062d\u0629 \u0644\u0647\u0630\u0627 \u0627\u0644\u0635\u0646\u0641 \u0623\u0635\u0628\u062d\u062a " + maxAllowed.ToString() + ".",
                            "Fale7 POS",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            target.Qty = n;
            PersistTakeawayState();
            CloseQtyPad();
            RenderReceipt();
            RefreshMenuInventoryTiles();
        }

        private bool HandleQtyOverlayKey(KeyEventArgs e)
        {
            if (QtyOverlay.Visibility != Visibility.Visible) return false;
            if (e.Key == Key.Escape) { CloseQtyPad(); return true; }
            if (e.Key == Key.Enter) { QtyApply(); return true; }
            if (e.Key == Key.Back) { QtyDelete(); return true; }
            if (e.Key >= Key.D0 && e.Key <= Key.D9) { AppendQtyDigit(((int)(e.Key - Key.D0)).ToString()); return true; }
            if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) { AppendQtyDigit(((int)(e.Key - Key.NumPad0)).ToString()); return true; }
            return false;
        }

        private void QtyOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ReferenceEquals(e.OriginalSource, QtyOverlay)) CloseQtyPad();
        }

        private void QtyModalBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Intentionally empty: overlay close is already limited to direct clicks on QtyOverlay.
        }

        private void QtyCloseButton_Click(object sender, RoutedEventArgs e) { CloseQtyPad(); }
        private void QtyDelButton_Click(object sender, RoutedEventArgs e) { QtyDelete(); }
        private void QtyOkButton_Click(object sender, RoutedEventArgs e) { QtyApply(); }

        private void QtyDigitButton_Click(object sender, RoutedEventArgs e)
        {
            var t = sender is Button b ? (b.Tag as string ?? b.Content as string) : null;
            AppendQtyDigit(t);
        }
    }
}

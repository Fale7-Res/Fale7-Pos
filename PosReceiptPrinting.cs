using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Fale7_POS
{
    public partial class MainWindow
    {
        private int GetReceiptWidth()
        {
            try
            {
                return Properties.Settings.Default["PrinterWidth80mm"] is bool width80 && width80 ? 42 : 32;
            }
            catch
            {
                return 32;
            }
        }

        private string BuildReceiptSeparator()
        {
            return new string('-', GetReceiptWidth());
        }

        private string SanitizeReceiptText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return (text ?? string.Empty)
                .Replace("\t", " ")
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Trim();
        }

        private string TrimReceiptText(string text, int maxLength)
        {
            var value = SanitizeReceiptText(text);
            if (maxLength <= 0) return string.Empty;
            if (value.Length <= maxLength) return value;
            if (maxLength <= 2) return value.Substring(0, maxLength);
            return value.Substring(0, maxLength - 2) + "..";
        }

        private string CenterReceiptText(string text)
        {
            var value = SanitizeReceiptText(text);
            var w = GetReceiptWidth();
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            if (value.Length >= w) return value;
            var spaces = (w - value.Length) / 2;
            return new string(' ', Math.Max(0, spaces)) + value;
        }

        private string BuildReceiptLeftRightRow(string rightPart, string leftPart)
        {
            var w = GetReceiptWidth();
            var rightValue = SanitizeReceiptText(rightPart);
            var leftValue = SanitizeReceiptText(leftPart);

            if (string.IsNullOrWhiteSpace(rightValue) && string.IsNullOrWhiteSpace(leftValue))
            {
                return string.Empty;
            }

            if (rightValue.Length + leftValue.Length >= w)
            {
                var maxRight = Math.Max(5, w - leftValue.Length - 2);
                if (rightValue.Length > maxRight)
                {
                    rightValue = TrimReceiptText(rightValue, maxRight);
                }

                if (rightValue.Length + leftValue.Length >= w)
                {
                    var maxLeft = Math.Max(5, w - rightValue.Length - 2);
                    if (leftValue.Length > maxLeft)
                    {
                        leftValue = TrimReceiptText(leftValue, maxLeft);
                    }
                }
            }

            var spaceCount = w - (rightValue.Length + leftValue.Length);
            if (spaceCount < 1) spaceCount = 1;
            return rightValue + new string(' ', spaceCount) + leftValue;
        }

        private string FormatReceiptInt(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private string FormatReceiptLong(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private string FormatReceiptDecimal(decimal value, string format = "0.###")
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private string FormatReceiptDouble(double value, string format = "0.###")
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private string FormatReceiptDate(DateTime value, string format = "dd/MM/yyyy HH:mm")
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private string EnsureEnglishDigits(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (ch >= '٠' && ch <= '٩')
                {
                    sb.Append((char)('0' + (ch - '٠')));
                    continue;
                }

                if (ch >= '۰' && ch <= '۹')
                {
                    sb.Append((char)('0' + (ch - '۰')));
                    continue;
                }

                sb.Append(ch);
            }
            return sb.ToString();
        }

        private string BuildReceiptTitleBlock(string title)
        {
            return CenterReceiptText(title);
        }

        private string BuildReceiptSubtitleBlock(string subtitle)
        {
            return CenterReceiptText(subtitle);
        }

        private string BuildReceiptNumericLine(string label, int value)
        {
            return BuildReceiptLeftRightRow(label, FormatReceiptInt(value));
        }

        private string BuildReceiptNumericLine(string label, decimal value)
        {
            return BuildReceiptLeftRightRow(label, FormatReceiptDecimal(value));
        }

        private string BuildReceiptItemRow(string itemName, int qty, int unitPrice)
        {
            var leftPart = FormatReceiptInt(qty) + " x " + FormatReceiptInt(unitPrice);
            var rightPart = SanitizeReceiptText(itemName);
            return BuildReceiptLeftRightRow(rightPart, leftPart);
        }

        private string BuildReceiptItemRow(string itemName, int qty, decimal unitPrice)
        {
            var leftPart = FormatReceiptInt(qty) + " x " + FormatReceiptDecimal(unitPrice);
            var rightPart = SanitizeReceiptText(itemName);
            return BuildReceiptLeftRightRow(rightPart, leftPart);
        }

        [DataContract]
        private sealed class ReceiptPrintData
        {
            [DataMember(Name = "printType", EmitDefaultValue = false)] public string PrintType { get; set; }
            [DataMember(Name = "title", EmitDefaultValue = false)] public string Title { get; set; }
            [DataMember(Name = "subtitle", EmitDefaultValue = false)] public string Subtitle { get; set; }
            [DataMember(Name = "copyStamp", EmitDefaultValue = false)] public string CopyStamp { get; set; }
            [DataMember(Name = "isCopy", EmitDefaultValue = false)] public bool IsCopy { get; set; }

            [DataMember(Name = "orderId", EmitDefaultValue = false)] public string OrderId { get; set; }
            [DataMember(Name = "backendOrderId", EmitDefaultValue = false)] public string BackendOrderId { get; set; }
            [DataMember(Name = "orderNo", EmitDefaultValue = false)] public int OrderNo { get; set; }
            [DataMember(Name = "shiftId", EmitDefaultValue = false)] public string ShiftId { get; set; }

            [DataMember(Name = "dateText", EmitDefaultValue = false)] public string DateText { get; set; }
            [DataMember(Name = "cashierName", EmitDefaultValue = false)] public string CashierName { get; set; }
            [DataMember(Name = "customerName", EmitDefaultValue = false)] public string CustomerName { get; set; }
            [DataMember(Name = "customerPhone", EmitDefaultValue = false)] public string CustomerPhone { get; set; }
            [DataMember(Name = "district", EmitDefaultValue = false)] public string District { get; set; }
            [DataMember(Name = "addressLine", EmitDefaultValue = false)] public string AddressLine { get; set; }
            [DataMember(Name = "driverName", EmitDefaultValue = false)] public string DriverName { get; set; }
            [DataMember(Name = "driverPhone", EmitDefaultValue = false)] public string DriverPhone { get; set; }
            [DataMember(Name = "notes", EmitDefaultValue = false)] public string Notes { get; set; }

            [DataMember(Name = "subtotal", EmitDefaultValue = false)] public decimal Subtotal { get; set; }
            [DataMember(Name = "deliveryFee", EmitDefaultValue = false)] public decimal DeliveryFee { get; set; }
            [DataMember(Name = "discount", EmitDefaultValue = false)] public decimal Discount { get; set; }
            [DataMember(Name = "total", EmitDefaultValue = false)] public decimal Total { get; set; }

            [DataMember(Name = "items", EmitDefaultValue = false)] public List<ReceiptPrintItemData> Items { get; set; }
            [DataMember(Name = "footerLines", EmitDefaultValue = false)] public List<string> FooterLines { get; set; }
            [DataMember(Name = "metadata", EmitDefaultValue = false)] public ReceiptPrintMetadataData Metadata { get; set; }
        }

        [DataContract]
        private sealed class ReceiptPrintItemData
        {
            [DataMember(Name = "name", EmitDefaultValue = false)] public string Name { get; set; }
            [DataMember(Name = "qty", EmitDefaultValue = false)] public int Qty { get; set; }
            [DataMember(Name = "unitPrice", EmitDefaultValue = false)] public decimal UnitPrice { get; set; }
            [DataMember(Name = "lineTotal", EmitDefaultValue = false)] public decimal LineTotal { get; set; }
            [DataMember(Name = "notes", EmitDefaultValue = false)] public string Notes { get; set; }
            [DataMember(Name = "isDivider", EmitDefaultValue = false)] public bool IsDivider { get; set; }
        }

        [DataContract]
        private sealed class ReceiptPrintMetadataData
        {
            [DataMember(Name = "channel", EmitDefaultValue = false)] public string Channel { get; set; }
            [DataMember(Name = "source", EmitDefaultValue = false)] public string Source { get; set; }
            [DataMember(Name = "status", EmitDefaultValue = false)] public string Status { get; set; }
            [DataMember(Name = "layout", EmitDefaultValue = false)] public string Layout { get; set; }
            [DataMember(Name = "reference", EmitDefaultValue = false)] public string Reference { get; set; }
        }

        private static ReceiptPrintItemData CreateReceiptPrintItem(
            string name,
            int qty,
            decimal unitPrice,
            string notes = null,
            bool isDivider = false)
        {
            var safeQty = qty <= 0 ? 1 : qty;
            var safePrice = unitPrice < 0 ? 0 : unitPrice;
            return new ReceiptPrintItemData
            {
                Name = name ?? string.Empty,
                Qty = safeQty,
                UnitPrice = safePrice,
                LineTotal = safeQty * safePrice,
                Notes = notes ?? string.Empty,
                IsDivider = isDivider
            };
        }

        private static ReceiptPrintData CreateEmptyReceiptPrintData()
        {
            return new ReceiptPrintData
            {
                Items = new List<ReceiptPrintItemData>(),
                FooterLines = new List<string>(),
                Metadata = new ReceiptPrintMetadataData()
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
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

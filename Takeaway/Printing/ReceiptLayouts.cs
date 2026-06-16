using Drawing2D = System.Drawing.Drawing2D;
using DrawingText = System.Drawing.Text;
using System.Windows.Media;
using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Printing;
using Drawing = System.Drawing;
using DrawingPrinting = System.Drawing.Printing;
namespace Fale7_POS
{
    public partial class MainWindow
    {
        public class GraphicalReceiptData
        {
            public int OrderNumber { get; set; }
            public string CustomerName { get; set; }
            public DateTime Date { get; set; }
            public string Time { get; set; }
            public List<ReceiptItem> Items { get; set; }
            public string InvoiceType => "سفري";
        }

        private bool PrintGraphicalTakeawayReceipt(OrderPrintData order)
        {
            if (order == null) return false;

            var receipt = BuildGraphicalReceiptData(order);

            if (_virtualPrinterMode)
            {
                var preview = BuildGraphicalTakeawayPreviewDocument(receipt);
                return ShowFlowDocumentPreview(preview, "معاينة فاتورة التيك أواي");
            }

            var printDoc = new DrawingPrinting.PrintDocument();
            var printerName = ResolveThermalPrinterName();
            if (!string.IsNullOrWhiteSpace(printerName))
            {
                printDoc.PrinterSettings.PrinterName = printerName;
            }

            printDoc.PrintController = new DrawingPrinting.StandardPrintController();
            printDoc.DefaultPageSettings.PaperSize = new DrawingPrinting.PaperSize("80mmThermal", 315, 3276);
            printDoc.DefaultPageSettings.Margins = new DrawingPrinting.Margins(5, 5, 5, 5);
            printDoc.OriginAtMargins = true;

            printDoc.PrintPage += (sender, e) =>
            {
                DrawTakeawayReceipt(e.Graphics, e.MarginBounds, receipt, e.PageSettings);
                e.HasMorePages = false;
            };

            try
            {
                printDoc.Print();
                return true;
            }
            catch (Exception ex)
            {
                AppendPosRuntimeLog("GRAPHICAL_TAKEAWAY_RECEIPT", ex);
                MessageBox.Show("فشلت طباعة فاتورة التيك أواي الرسومية: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private FlowDocument BuildGraphicalTakeawayPreviewDocument(GraphicalReceiptData data)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Tahoma"),
                FontSize = 12,
                FlowDirection = FlowDirection.RightToLeft,
                Background = Brushes.White,
                Foreground = Brushes.Black,
                PagePadding = new Thickness(8),
                ColumnWidth = 302
            };

            doc.Blocks.Add(new Paragraph(new Run("فالح ابو العنبه"))
            {
                FontSize = 18,
                FontWeight = FontWeights.Black,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            });

            doc.Blocks.Add(new Paragraph(new Run("فاتورة تيك أواي"))
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            });

            doc.Blocks.Add(new Paragraph(new Run("رقم الأوردر: " + (data == null ? 0 : data.OrderNumber)))
            {
                Margin = new Thickness(0, 0, 0, 2)
            });
            doc.Blocks.Add(new Paragraph(new Run("نوع الفاتورة: " + (data == null ? "سفري" : data.InvoiceType)))
            {
                Margin = new Thickness(0, 0, 0, 2)
            });
            doc.Blocks.Add(new Paragraph(new Run("التاريخ: " + (data == null ? DateTime.Now.ToString("dd-MM-yyyy") : data.Date.ToString("dd-MM-yyyy"))))
            {
                Margin = new Thickness(0, 0, 0, 2)
            });
            doc.Blocks.Add(new Paragraph(new Run("الوقت: " + (data == null ? DateTime.Now.ToString("HH:mm") : data.Time)))
            {
                Margin = new Thickness(0, 0, 0, 2)
            });
            doc.Blocks.Add(new Paragraph(new Run("العميل: " + (data == null || string.IsNullOrWhiteSpace(data.CustomerName) ? "استلام" : data.CustomerName.Trim())))
            {
                Margin = new Thickness(0, 0, 0, 6)
            });

            var table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(55) });
            table.Columns.Add(new TableColumn { Width = new GridLength(120) });
            table.Columns.Add(new TableColumn { Width = new GridLength(60) });
            table.Columns.Add(new TableColumn { Width = new GridLength(65) });

            var group = new TableRowGroup();
            table.RowGroups.Add(group);

            var header = new TableRow();
            header.Cells.Add(MakePrintCell("الكمية", true));
            header.Cells.Add(MakePrintCell("الطلب", true));
            header.Cells.Add(MakePrintCell("السعر", true));
            header.Cells.Add(MakePrintCell("الإجمالي", true));
            group.Rows.Add(header);

            decimal total = 0m;
            var items = data == null ? new List<ReceiptItem>() : (data.Items ?? new List<ReceiptItem>());
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;
                total += item.TotalPrice;

                var row = new TableRow();
                row.Cells.Add(MakePrintCell(item.Quantity.ToString(), false));
                row.Cells.Add(MakePrintCell(item.ItemName ?? string.Empty, false));
                row.Cells.Add(MakePrintCell(item.UnitPrice.ToString("0.000", CultureInfo.InvariantCulture) + " د.ك", false));
                row.Cells.Add(MakePrintCell(item.TotalPrice.ToString("0.000", CultureInfo.InvariantCulture) + " د.ك", false));
                group.Rows.Add(row);
            }

            doc.Blocks.Add(table);
            doc.Blocks.Add(new Paragraph(new Run("الإجمالي: " + total.ToString("0.000", CultureInfo.InvariantCulture) + " د.ك"))
            {
                FontWeight = FontWeights.Black,
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(0, 6, 0, 0)
            });

            doc.Blocks.Add(new Paragraph(new Run("0238361323"))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            });
            doc.Blocks.Add(new Paragraph(new Run("01000600362"))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 0)
            });
            doc.Blocks.Add(new Paragraph(new Run("0114474115"))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 0)
            });
            doc.Blocks.Add(new Paragraph(new Run("سعداء بخدمتكم"))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 0)
            });

            return doc;
        }

        private GraphicalReceiptData BuildGraphicalReceiptData(OrderPrintData order)
        {
            var items = new List<ReceiptItem>
            {
                new ReceiptItem { Quantity = 2, ItemName = "اومليت سندويش سماح", UnitPrice = 35 },
                new ReceiptItem { Quantity = 1, ItemName = "بطاطس رومي صاج", UnitPrice = 45 },
                new ReceiptItem { Quantity = 1, ItemName = "فلافل عراقي مصري", UnitPrice = 25 },
                new ReceiptItem { Quantity = 3, ItemName = "فلافل خبز سماح", UnitPrice = 30 }
            };

            if (order != null && order.Items != null)
            {
                items = new List<ReceiptItem>();
                for (int i = 0; i < order.Items.Count; i++)
                {
                    var item = order.Items[i];
                    if (item == null) continue;
                    items.Add(new ReceiptItem
                    {
                        Quantity = item.Qty <= 0 ? 1 : item.Qty,
                        ItemName = item.Name ?? string.Empty,
                        UnitPrice = item.UnitPrice
                    });
                }

                if (items.Count == 0)
                {
                    items.Add(new ReceiptItem { Quantity = 2, ItemName = "اومليت سندويش سماح", UnitPrice = 35 });
                    items.Add(new ReceiptItem { Quantity = 1, ItemName = "بطاطس رومي صاج", UnitPrice = 45 });
                    items.Add(new ReceiptItem { Quantity = 1, ItemName = "فلافل عراقي مصري", UnitPrice = 25 });
                    items.Add(new ReceiptItem { Quantity = 3, ItemName = "فلافل خبز سماح", UnitPrice = 30 });
                }
            }

            return new GraphicalReceiptData
            {
                OrderNumber = order != null ? order.OrderNo : 1,
                CustomerName = order != null && !string.IsNullOrWhiteSpace(order.CustomerName) ? order.CustomerName : "مدير",
                Date = DateTime.Now,
                Time = DateTime.Now.ToString("HH:mm"),
                Items = items
            };
        }

        private void DrawTakeawayReceipt(Drawing.Graphics g, Drawing.Rectangle bounds, GraphicalReceiptData data, DrawingPrinting.PageSettings settings)
        {
            if (g == null || data == null || settings == null) return;

            g.SmoothingMode = Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = DrawingText.TextRenderingHint.ClearTypeGridFit;
            g.PageUnit = Drawing.GraphicsUnit.Millimeter;
            g.PageScale = 1f;

            float paperWidth = settings.PaperSize.Width * 25.4f / 100f;
            float usableWidth = Math.Max(60f, paperWidth - 10f);
            float startX = 5f;
            float startY = 4f;

            using (var fontBrand = new Drawing.Font("Arial", 12, Drawing.FontStyle.Bold))
            using (var fontTitle = new Drawing.Font("Arial", 12, Drawing.FontStyle.Bold))
            using (var fontBody = new Drawing.Font("Arial", 9, Drawing.FontStyle.Regular))
            using (var fontBold = new Drawing.Font("Arial", 9, Drawing.FontStyle.Bold))
            using (var fontTotal = new Drawing.Font("Arial", 11, Drawing.FontStyle.Bold))
            using (var fontFooter = new Drawing.Font("Arial", 9, Drawing.FontStyle.Regular))
            using (var separatorPen = new Drawing.Pen(Drawing.Color.Black, 0.35f))
            {
                separatorPen.DashStyle = Drawing2D.DashStyle.Dot;

                var rtlLeft = new Drawing.StringFormat(Drawing.StringFormatFlags.DirectionRightToLeft);
                rtlLeft.Alignment = Drawing.StringAlignment.Far;

                var rtlCenter = new Drawing.StringFormat(Drawing.StringFormatFlags.DirectionRightToLeft);
                rtlCenter.Alignment = Drawing.StringAlignment.Center;

                var rtlRight = new Drawing.StringFormat(Drawing.StringFormatFlags.DirectionRightToLeft);
                rtlRight.Alignment = Drawing.StringAlignment.Near;

                Drawing.Brush textBrush = Drawing.Brushes.Black;
                float lineHeight = fontBody.GetHeight(g);
                float y = startY;

                g.DrawString("فالح ابو العنبه", fontBrand, textBrush,
                    new Drawing.RectangleF(startX, y, usableWidth, lineHeight + 1f), rtlRight);
                y += lineHeight + 1f;

                g.DrawLine(separatorPen, startX, y, startX + usableWidth, y);
                y += 3f;

                g.DrawString("فاتورة المطعم", fontTitle, textBrush,
                    new Drawing.RectangleF(startX, y, usableWidth, lineHeight + 1f), rtlCenter);
                y += lineHeight + 2f;

                g.DrawLine(separatorPen, startX, y, startX + usableWidth, y);
                y += 3f;

                DrawRtlLine(g, $"رقم الأوردر: {data.OrderNumber}", fontBody, textBrush, startX, ref y, usableWidth, rtlRight);
                DrawRtlLine(g, $"نوع الفاتورة: {data.InvoiceType}", fontBody, textBrush, startX, ref y, usableWidth, rtlRight);
                DrawRtlLine(g, $"التاريخ: {data.Date:dd-MM-yyyy}", fontBody, textBrush, startX, ref y, usableWidth, rtlRight);
                DrawRtlLine(g, $"الوقت: {data.Time}", fontBody, textBrush, startX, ref y, usableWidth, rtlRight);
                DrawRtlLine(g, $"العميل: {data.CustomerName}", fontBody, textBrush, startX, ref y, usableWidth, rtlRight);

                y += 2f;
                g.DrawLine(separatorPen, startX, y, startX + usableWidth, y);
                y += 3f;

                var items = data.Items ?? new List<ReceiptItem>();
                decimal total = 0m;

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item == null) continue;

                    var qtyText = item.Quantity.ToString(CultureInfo.InvariantCulture);
                    var nameText = item.ItemName ?? string.Empty;
                    var priceText = item.UnitPrice.ToString("0.000", CultureInfo.InvariantCulture) + " د.ك";
                    var totalText = item.TotalPrice.ToString("0.000", CultureInfo.InvariantCulture) + " د.ك";

                    g.DrawString(qtyText, fontBold, textBrush,
                        new Drawing.RectangleF(startX, y, 12f, lineHeight), rtlRight);
                    g.DrawString(nameText, fontBody, textBrush,
                        new Drawing.RectangleF(startX + 12f, y, usableWidth - 28f, lineHeight), rtlRight);
                    g.DrawString(priceText, fontBody, textBrush,
                        new Drawing.RectangleF(startX + usableWidth - 16f, y, 16f, lineHeight), rtlLeft);
                    y += lineHeight + 2f;

                    g.DrawString(totalText, fontBody, textBrush,
                        new Drawing.RectangleF(startX + usableWidth - 20f, y, 20f, lineHeight), rtlLeft);
                    y += lineHeight + 1f;

                    g.DrawLine(separatorPen, startX, y, startX + usableWidth, y);
                    y += 2f;

                    total += item.TotalPrice;
                }

                y += 2f;
                g.DrawLine(separatorPen, startX, y, startX + usableWidth, y);
                y += 4f;

                DrawRtlLine(g, $"الإجمالي: {total.ToString("0.000", CultureInfo.InvariantCulture)} د.ك", fontTotal, textBrush, startX, ref y, usableWidth, rtlRight);

                y += 2f;
                g.DrawLine(separatorPen, startX, y, startX + usableWidth, y);
                y += 4f;

                g.DrawString("0238361323", fontFooter, textBrush,
                    new Drawing.RectangleF(startX, y, usableWidth, lineHeight), rtlCenter);
                y += lineHeight;
                g.DrawString("01000600362", fontFooter, textBrush,
                    new Drawing.RectangleF(startX, y, usableWidth, lineHeight), rtlCenter);
                y += lineHeight;
                g.DrawString("0114474115", fontFooter, textBrush,
                    new Drawing.RectangleF(startX, y, usableWidth, lineHeight), rtlCenter);
                y += lineHeight + 4f;
                g.DrawString("سعداء بخدمتكم", fontFooter, textBrush,
                    new Drawing.RectangleF(startX, y, usableWidth, lineHeight), rtlCenter);
            }
        }

        private static void DrawRtlLine(Drawing.Graphics g, string text, Drawing.Font font, Drawing.Brush brush, float x, ref float y, float width, Drawing.StringFormat format)
        {
            g.DrawString(text ?? string.Empty, font, brush, new Drawing.RectangleF(x, y, width, font.GetHeight(g) + 2), format);
            y += font.GetHeight(g) + 4;
        }

        private static string FormatMoney(decimal value)
        {
            return value.ToString("0.000", CultureInfo.InvariantCulture) + " د.ك";
        }

        private bool ShowFlowDocumentPreview(FlowDocument doc, string title)
        {
            if (doc == null) return false;

            try
            {
                PrepareFlowDocumentForPreview(doc);

                var previewWindow = new Window
                {
                    Title = string.IsNullOrWhiteSpace(title) ? "معاينة الفاتورة" : title,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStyle = WindowStyle.None,
                    WindowState = WindowState.Maximized,
                    Topmost = true,
                    Width = 420,
                    Height = 760,
                    MinWidth = 360,
                    MinHeight = 520,
                    Background = Brushes.Black,
                    FlowDirection = FlowDirection.RightToLeft,
                    ShowInTaskbar = false,
                    Cursor = Cursors.Arrow
                };

                var root = new Grid();
                root.Background = Brushes.Black;
                root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                previewWindow.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control
                        && (e.Key == Key.Add || e.Key == Key.OemPlus || e.Key == Key.Subtract || e.Key == Key.OemMinus || e.Key == Key.D0))
                    {
                        e.Handled = true;
                    }
                };
                previewWindow.PreviewMouseWheel += delegate(object sender, MouseWheelEventArgs e)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) e.Handled = true;
                };

                root.Children.Add(new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF2B5F9F")),
                    Padding = new Thickness(12, 10, 12, 10),
                    Child = new TextBlock
                    {
                        Text = "وضع المعاينة للطباعة",
                        Foreground = Brushes.White,
                        FontSize = 16,
                        FontWeight = FontWeights.Black,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FlowDirection = FlowDirection.RightToLeft
                    }
                });

                var viewer = new FlowDocumentScrollViewer
                {
                    Document = doc,
                    IsToolBarVisible = false,
                    Background = Brushes.White,
                    Margin = new Thickness(0),
                    Width = 302,
                    MinWidth = 302,
                    MaxWidth = 302,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
                };

                var paperFrame = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF202020")),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8),
                    Margin = new Thickness(0),
                    Width = 320,
                    MinWidth = 320,
                    MaxWidth = 320,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = viewer
                };
                Grid.SetRow(paperFrame, 1);
                root.Children.Add(paperFrame);

                var footer = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF6F6F6")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCCCCCC")),
                    BorderThickness = new Thickness(0, 1, 0, 0),
                    Padding = new Thickness(12),
                    Child = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        FlowDirection = FlowDirection.LeftToRight,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Children =
                        {
                            new Button
                            {
                                Content = "إغلاق",
                                Width = 120,
                                Height = 34,
                                FontWeight = FontWeights.Black,
                                IsCancel = true
                            }
                        }
                    }
                };
                Grid.SetRow(footer, 2);
                root.Children.Add(footer);

                previewWindow.Content = root;
                previewWindow.ShowDialog();
                return true;
            }
            catch (Exception ex)
            {
                AppendPosRuntimeLog("FlowPreview", ex);
                MessageBox.Show("تعذر فتح المعاينة: " + ex.Message, "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void PrepareFlowDocumentForPreview(FlowDocument doc)
        {
            if (doc == null) return;

            doc.Background = Brushes.White;
            doc.Foreground = Brushes.Black;
            doc.PagePadding = new Thickness(6);
            doc.ColumnWidth = 302;
            doc.PageWidth = 302;
            doc.MinPageWidth = 302;
            doc.MaxPageWidth = 302;
            ResizeDocumentTablesForPrinting(doc, 286);
        }

        private void PrepareFlowDocumentForPrinting(FlowDocument doc, PrintDialog dlg)
        {
            doc.PagePadding = new Thickness(12);
            doc.ColumnWidth = double.PositiveInfinity;

            var printableAreaWidth = dlg.PrintableAreaWidth;
            if ((double.IsNaN(printableAreaWidth) || printableAreaWidth <= 0) && dlg.PrintQueue != null)
            {
                try
                {
                    var capabilities = dlg.PrintQueue.GetPrintCapabilities(dlg.PrintTicket ?? dlg.PrintQueue.DefaultPrintTicket);
                    if (capabilities != null && capabilities.PageImageableArea != null)
                    {
                        printableAreaWidth = capabilities.PageImageableArea.ExtentWidth;
                    }
                }
                catch
                {
                }
            }

            if (!double.IsNaN(printableAreaWidth) && printableAreaWidth > 0)
            {
                var contentWidth = Math.Max(120, printableAreaWidth - doc.PagePadding.Left - doc.PagePadding.Right);
                doc.PageWidth = printableAreaWidth;
                ResizeDocumentTablesForPrinting(doc, contentWidth);
            }
        }

        private void ResizeDocumentTablesForPrinting(FlowDocument doc, double maxTableWidth)
        {
            if (doc == null || double.IsNaN(maxTableWidth) || maxTableWidth <= 0) return;

            foreach (Block block in doc.Blocks)
            {
                var table = block as Table;
                if (table == null || table.Columns == null || table.Columns.Count == 0) continue;

                var totalWidth = 0d;
                var hasOnlyAbsoluteColumns = true;
                foreach (TableColumn column in table.Columns)
                {
                    if (!column.Width.IsAbsolute)
                    {
                        hasOnlyAbsoluteColumns = false;
                        break;
                    }
                    totalWidth += column.Width.Value;
                }

                if (!hasOnlyAbsoluteColumns || totalWidth <= maxTableWidth || totalWidth <= 0) continue;

                var scale = maxTableWidth / totalWidth;
                foreach (TableColumn column in table.Columns)
                {
                    column.Width = new GridLength(Math.Max(36, column.Width.Value * scale));
                }
            }
        }

        private FlowDocument BuildKitchenDocument(TakeawayOrderRecord order)
        {
            var doc = new FlowDocument { FontFamily = new FontFamily("Tahoma"), FontSize = 12, FlowDirection = FlowDirection.RightToLeft };
            var blankMode = order == null;
            doc.Blocks.Add(new Paragraph(new Run(blankMode ? "فاتورة المطبخ (فارغة)" : "فاتورة المطبخ (Takeaway)")) { FontSize = 18, FontWeight = FontWeights.Black, Margin = new Thickness(0, 0, 0, 8) });

            var table = new Table();
            table.Columns.Add(new TableColumn { Width = new GridLength(400) });
            table.Columns.Add(new TableColumn { Width = new GridLength(90) });
            var g = new TableRowGroup();
            table.RowGroups.Add(g);
            var hr = new TableRow(); g.Rows.Add(hr);
            hr.Cells.Add(MakePrintCell("الصنف", true));
            hr.Cells.Add(MakePrintCell("الكمية", true));

            if (blankMode)
            {
                for (int i = 0; i < 3; i++) { var r = new TableRow(); r.Cells.Add(MakePrintCell(string.Empty, false)); r.Cells.Add(MakePrintCell(string.Empty, false)); g.Rows.Add(r); }
                doc.Blocks.Add(table);
                return doc;
            }

            var hasItems = order.Items != null && order.Items.Count > 0;
            if (hasItems)
            {
                for (int i = 0; i < order.Items.Count; i++)
                {
                    var it = order.Items[i];
                    var r = new TableRow();
                    r.Cells.Add(MakePrintCell(it.Desc, false));
                    r.Cells.Add(MakePrintCell(it.Qty.ToString(), false));
                    g.Rows.Add(r);
                }
            }
            else
            {
                var r = new TableRow();
                r.Cells.Add(MakePrintCell(string.Empty, false));
                r.Cells.Add(MakePrintCell(string.Empty, false));
                g.Rows.Add(r);
            }

            doc.Blocks.Add(table);
            var subtotal = 0;
            if (hasItems) for (int i = 0; i < order.Items.Count; i++) subtotal += order.Items[i].Qty * order.Items[i].Price;
            doc.Blocks.Add(new Paragraph(new Run("الإجمالي: " + (hasItems ? subtotal.ToString() : string.Empty))) { FontWeight = FontWeights.Black, Margin = new Thickness(0, 10, 0, 0) });
            return doc;
        }

        private TableCell MakePrintCell(string text, bool header)
        {
            var isQty = text == "الكمية";
            var p = new Paragraph(new Run(text ?? string.Empty)) { Margin = new Thickness(0), TextAlignment = isQty ? TextAlignment.Center : TextAlignment.Right, FlowDirection = isQty ? FlowDirection.LeftToRight : FlowDirection.RightToLeft };
            var c = new TableCell(p) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Padding = new Thickness(6) };
            if (header) { c.Background = Brushes.Gainsboro; p.FontWeight = FontWeights.Black; }
            return c;
        }

        private FlowDocument BuildPlainTextReceiptDocument(string text)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                FlowDirection = FlowDirection.RightToLeft
            };
            doc.Blocks.Add(new Paragraph(new Run(StripPrinterControlChars(text))) { Margin = new Thickness(0) });
            return doc;
        }

    }
}



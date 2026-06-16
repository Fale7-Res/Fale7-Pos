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
        private string ResolveThermalPrinterName()
        {
            try
            {
                foreach (string printer in DrawingPrinting.PrinterSettings.InstalledPrinters)
                {
                    var name = (printer ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (name.IndexOf("XP-80C", StringComparison.OrdinalIgnoreCase) >= 0) return name;
                    if (name.IndexOf("XPrinter", StringComparison.OrdinalIgnoreCase) >= 0) return name;
                }
            }
            catch
            {
            }

            try
            {
                using (var server = new LocalPrintServer())
                {
                    var queue = server.DefaultPrintQueue;
                    if (queue != null && !string.IsNullOrWhiteSpace(queue.Name)) return queue.Name;
                    if (queue != null && !string.IsNullOrWhiteSpace(queue.FullName)) return queue.FullName;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private bool TryPrintFlowDocument(FlowDocument doc)
        {
            return TryPrintFlowDocumentInternal(doc, false);
        }

        private bool TryPrintFlowDocumentWithDialog(FlowDocument doc)
        {
            return TryPrintFlowDocumentInternal(doc, true);
        }

        private bool TryPrintFlowDocumentInternal(FlowDocument doc, bool showDialog)
        {
            if (doc == null) return false;
            try
            {
                if (_virtualPrinterMode)
                {
                    return ShowFlowDocumentPreview(doc, "معاينة الفاتورة");
                }

                var dlg = showDialog ? new PrintDialog() : CreateSilentReceiptPrintDialog();
                if (dlg == null)
                {
                    return ShowFlowDocumentPreview(doc, "معاينة الفاتورة");
                }
                if (showDialog && dlg.ShowDialog() != true) return false;

                PrepareFlowDocumentForPrinting(doc, dlg);
                dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Fale7 POS Kitchen Ticket");
                return true;
            }
            catch (Exception ex)
            {
                AppendPosRuntimeLog("TryPrintFlowDocumentInternal", ex);
                return ShowFlowDocumentPreview(doc, "معاينة الفاتورة");
            }
        }

        private PrintDialog CreateSilentReceiptPrintDialog()
        {
            using (var printServer = new LocalPrintServer())
            {
                var queue = printServer.DefaultPrintQueue;
                if (queue == null)
                {
                    return null;
                }

                var dlg = new PrintDialog
                {
                    PrintQueue = queue,
                    PrintTicket = queue.DefaultPrintTicket
                };
                return dlg;
            }
        }

    }
}

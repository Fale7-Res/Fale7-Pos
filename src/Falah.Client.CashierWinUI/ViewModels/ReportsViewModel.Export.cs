using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Falah.Application.Reports.Export;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Export and print on the reports screen. The user picks a report (only the ones the server says this user may
/// export), the period is the one the screen is showing, and a format: Excel, PDF, Word or an image. The file is
/// generated on the server from one snapshot of the same report services the screens read, so it says what the
/// screen says; this class only saves or opens what came back. Generation runs off the UI thread; the till keeps
/// working. Print = the PDF opened in the viewer, where the user previews, chooses the printer and prints.
/// </summary>
public partial class ReportsViewModel
{
    public IReportFileService Files { get; set; } = ReportFiles.Current;

    public ObservableCollection<ReportExportCatalogEntry> ExportReports { get; } = new();
    [ObservableProperty] private ReportExportCatalogEntry? _selectedExportReport;
    [ObservableProperty] private bool _isExporting;
    [ObservableProperty] private string? _exportMessage;
    [ObservableProperty] private string? _exportError;
    [ObservableProperty] private string? _lastExportPath;
    private bool _catalogLoaded;

    public bool HasExportReports => ExportReports.Count > 0;
    public bool CanOpenLastExport => LastExportPath is not null;
    partial void OnLastExportPathChanged(string? value) => OnPropertyChanged(nameof(CanOpenLastExport));

    /// <summary>The reports this user may export: the shared catalog filtered by the same permission rule the server
    /// enforces on every export - no extra request on each load of the screen. The server remains the authority: an
    /// export it refuses is reported as refused.</summary>
    public void EnsureExportCatalog()
    {
        if (_catalogLoaded || _session.CurrentUser is null) return;
        _catalogLoaded = true;
        ExportReports.Clear();
        foreach (var r in ReportExportCatalog.Reports.Where(r => _session.HasPermission("all") || r.AnyOf.Any(_session.HasPermission)))
            ExportReports.Add(new ReportExportCatalogEntry(r.Key, r.Title, r.Description, ReportExportCatalog.AllFormats));
        SelectedExportReport ??= ExportReports.FirstOrDefault();
        OnPropertyChanged(nameof(HasExportReports));
    }

    private static string FormatLabel(string format) => format switch
    {
        "xlsx" => "Excel", "pdf" => "PDF", "docx" => "Word", "png" => "صورة", _ => format,
    };

    private async Task<Falah.Client.CashierWinUI.Services.ApiClient.ExportedReport?> GenerateAsync(string format)
    {
        if (SelectedExportReport is not { } report) { ExportError = "اختر التقرير أولًا."; return null; }
        var (from, to) = ResolveRange();
        if (from > to) { ExportError = "تاريخ البداية بعد تاريخ النهاية."; return null; }
        ExportMessage = $"جاري إنشاء {report.Title} ({FormatLabel(format)}) للفترة {RangeLabel}...";
        var result = await _session.Api.ExportReportAsync(_session.CurrentUser!.Id, report.Key, format, from, to);
        if (!result.Success || result.Value is null)
        {
            ExportMessage = null;
            ExportError = result.Error ?? "تعذر إنشاء التقرير.";
            return null;
        }
        return result.Value;
    }

    [RelayCommand]
    private async Task ExportAsync(string? format)
    {
        if (IsExporting || format is null) return;
        IsExporting = true;
        ExportError = null;
        try
        {
            var file = await GenerateAsync(format);
            if (file is null) return;
            string? path;
            try { path = await Files.SaveAsAsync(file.Content, file.FileName); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Falah.Client.Diagnostics.ClientLog.WriteException("reports: saving the exported file", ex);
                ExportMessage = null;
                ExportError = "تعذر حفظ الملف: قد يكون مفتوحًا في برنامج آخر، أو المكان غير مسموح بالكتابة فيه، أو القرص ممتلئ. أغلق الملف أو اختر مكانًا آخر.";
                return;
            }
            if (path is null) { ExportMessage = "أُلغي الحفظ."; return; }
            LastExportPath = path;
            ExportMessage = $"تم حفظ {SelectedExportReport!.Title} ({FormatLabel(format)}) في {path}" +
                (file.Fingerprint is { Length: >= 16 } fp ? $" · بصمة البيانات {fp[..16]}" : "");
        }
        finally { IsExporting = false; }
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (IsExporting) return;
        IsExporting = true;
        ExportError = null;
        try
        {
            var file = await GenerateAsync("pdf");
            if (file is null) return;
            try
            {
                var path = Files.WriteForPrinting(file.Content, file.FileName);
                Files.Open(path);
                ExportMessage = "فُتح التقرير في عارض PDF للمعاينة؛ اختر الطابعة واطبع من هناك (A4).";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                Falah.Client.Diagnostics.ClientLog.WriteException("reports: opening the report for printing", ex);
                ExportMessage = null;
                ExportError = "تعذر فتح التقرير للطباعة. تأكد من وجود برنامج لعرض ملفات PDF، أو صدّر PDF واحفظه ثم اطبعه.";
            }
        }
        finally { IsExporting = false; }
    }

    [RelayCommand]
    private void OpenLastExport()
    {
        if (LastExportPath is not { } path) return;
        try { Files.Open(path); }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Falah.Client.Diagnostics.ClientLog.WriteException("reports: opening the saved file", ex);
            ExportError ="تعذر فتح الملف. افتحه من مكان حفظه.";
        }
    }
}

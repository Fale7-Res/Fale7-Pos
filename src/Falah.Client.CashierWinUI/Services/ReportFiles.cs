namespace Falah.Client.CashierWinUI.Services;

/// <summary>Where an exported report goes on this PC. Behind an interface so the screen's logic is testable; the app
/// registers the real one (Platform.ReportFileService - the Windows save dialog and the PDF viewer) at startup.</summary>
public interface IReportFileService
{
    /// <summary>Asks where to save (the Windows save dialog) and writes the file there. Null when the user cancelled.</summary>
    Task<string?> SaveAsAsync(byte[] content, string suggestedFileName);

    /// <summary>Writes a file for printing into this user's own temporary folder and returns its path.</summary>
    string WriteForPrinting(byte[] content, string fileName);

    /// <summary>Opens a file with the program Windows associates with it (Excel, Word, the PDF viewer...).</summary>
    void Open(string path);
}

public static class ReportFiles
{
    /// <summary>Set by App at startup. Unset (tests, layout probes) it refuses rather than touching the disk.</summary>
    public static IReportFileService Current { get; set; } = new Unavailable();

    private sealed class Unavailable : IReportFileService
    {
        public Task<string?> SaveAsAsync(byte[] content, string suggestedFileName) => throw new IOException("حفظ الملفات غير متاح هنا.");
        public string WriteForPrinting(byte[] content, string fileName) => throw new IOException("الطباعة غير متاحة هنا.");
        public void Open(string path) => throw new IOException("فتح الملفات غير متاح هنا.");
    }
}

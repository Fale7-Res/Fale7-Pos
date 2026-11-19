using System.Diagnostics;
using Falah.Client.CashierWinUI.Services;
using Windows.Storage.Pickers;

namespace Falah.Client.CashierWinUI.Platform;

/// <summary>
/// The real implementation. Saving uses the Windows save dialog parented to the app's window (required for an
/// unpackaged app, as in ImageFilePicker); the file name is the one the server generated (ASCII, no path). Printing
/// writes the PDF to %TEMP%\Falah\Reports - private to the signed-in Windows user, cleaned of files older than a day
/// on every use - and opens it in the PDF viewer, where the user sees the preview, picks the printer and prints.
/// Nothing is ever sent to a printer by this app on its own.
/// </summary>
public sealed class ReportFileService : IReportFileService
{
    public static string PrintFolder => Path.Combine(Path.GetTempPath(), "Falah", "Reports");

    public async Task<string?> SaveAsAsync(byte[] content, string suggestedFileName)
    {
        var window = App.MainAppWindow ?? throw new InvalidOperationException("نافذة البرنامج غير متاحة.");
        var extension = Path.GetExtension(suggestedFileName);
        var picker = new FileSavePicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary, SuggestedFileName = Path.GetFileNameWithoutExtension(suggestedFileName) };
        picker.FileTypeChoices.Add(extension.TrimStart('.').ToUpperInvariant(), new List<string> { extension });
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(window));
        var file = await picker.PickSaveFileAsync();
        if (file is null) return null;
        await File.WriteAllBytesAsync(file.Path, content);
        return file.Path;
    }

    public string WriteForPrinting(byte[] content, string fileName)
    {
        Directory.CreateDirectory(PrintFolder);
        foreach (var old in Directory.EnumerateFiles(PrintFolder))
        {
            try { if (File.GetLastWriteTimeUtc(old) < DateTime.UtcNow.AddDays(-1)) File.Delete(old); }
            catch (IOException) { /* still open in a viewer; next time */ }
            catch (UnauthorizedAccessException) { }
        }
        var safe = string.Concat(Path.GetFileName(fileName).Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        var path = Path.Combine(PrintFolder, $"{Path.GetFileNameWithoutExtension(safe)}-{DateTime.Now:HHmmss}{Path.GetExtension(safe)}");
        File.WriteAllBytes(path, content);
        return path;
    }

    public void Open(string path) => Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
}

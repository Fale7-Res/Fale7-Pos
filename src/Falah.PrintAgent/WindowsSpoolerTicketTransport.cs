using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Falah.PrintAgent;

/// <summary>
/// Sends ESC/POS bytes to a printer installed as a Windows print queue, which is how a USB thermal
/// printer normally appears once its driver is installed (the Xprinter XP-80C class this restaurant
/// uses shows up exactly this way, on a USB port).
///
/// The bytes must reach the device untouched, so the job is submitted with the `RAW` datatype: the
/// driver then passes the stream straight through instead of rendering it as a document. Anything
/// else would turn ESC/POS command bytes into printed characters.
///
/// Submitting through the spooler also means a ticket queued while the printer is off or out of
/// paper is held by Windows and printed when the device comes back, rather than being lost - which
/// is why this is the right transport for USB hardware even though a direct port write would be
/// fewer moving parts.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsSpoolerTicketTransport : ITicketTransport
{
    private readonly string _printerName;

    public WindowsSpoolerTicketTransport(string printerName) => _printerName = printerName;

    public string Description => $"windows printer \"{_printerName}\" (RAW)";

    public Task SendAsync(byte[] payload, CancellationToken ct)
    {
        Send(_printerName, payload);
        return Task.CompletedTask;
    }

    /// <summary>Synchronous on purpose: the spooler API is blocking, and wrapping it in a task would
    /// only move the blocking somewhere less obvious. A ticket is a few hundred bytes.</summary>
    internal static void Send(string printerName, byte[] payload)
    {
        if (!OpenPrinter(printerName, out var printer, IntPtr.Zero))
            throw new InvalidOperationException(
                $"Could not open the printer \"{printerName}\": {LastErrorMessage()}");

        try
        {
            var info = new DocInfo
            {
                DocName = "Falah ticket",
                OutputFile = null,
                // Without RAW the driver renders the bytes as a document and the ESC/POS commands
                // print as garbage characters instead of controlling the printer.
                DataType = "RAW",
            };

            if (StartDocPrinter(printer, 1, info) == 0)
                throw new InvalidOperationException($"The printer rejected the job: {LastErrorMessage()}");

            try
            {
                if (!StartPagePrinter(printer))
                    throw new InvalidOperationException($"The printer rejected the page: {LastErrorMessage()}");

                try
                {
                    var buffer = Marshal.AllocHGlobal(payload.Length);
                    try
                    {
                        Marshal.Copy(payload, 0, buffer, payload.Length);
                        if (!WritePrinter(printer, buffer, payload.Length, out var written))
                            throw new InvalidOperationException($"Writing to the printer failed: {LastErrorMessage()}");
                        // A short write means a truncated ticket, which prints as a plausible-looking
                        // but wrong receipt - worse than an outright failure, so it is one.
                        if (written != payload.Length)
                            throw new InvalidOperationException(
                                $"Only {written} of {payload.Length} bytes reached the printer.");
                    }
                    finally { Marshal.FreeHGlobal(buffer); }
                }
                finally { EndPagePrinter(printer); }
            }
            finally { EndDocPrinter(printer); }
        }
        finally { ClosePrinter(printer); }
    }

    private static string LastErrorMessage() => new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class DocInfo
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string? DocName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? OutputFile;
        [MarshalAs(UnmanagedType.LPWStr)] public string? DataType;
    }

    [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPWStr)] string printerName, out IntPtr printer, IntPtr defaults);

    [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr printer);

    [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int StartDocPrinter(IntPtr printer, int level, [In] DocInfo documentInfo);

    [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr printer);

    [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr printer);

    [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr printer);

    [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr printer, IntPtr bytes, int count, out int written);
}

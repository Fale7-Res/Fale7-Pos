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
///
/// PRT-0004 (audit 2026-11-18): being accepted by the queue is not being printed. After submitting,
/// SendAsync watches the job (SpoolerJobWatch) and fails when the printer is offline, out of paper or
/// in error, or the job is still queued after the confirmation time. The job is left in the queue
/// (owner, 2026-11-18), so the failure message warns that it may still print by itself.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsSpoolerTicketTransport : ITicketTransport
{
    private readonly string _printerName;
    private readonly string _documentName;
    private readonly Action<int>? _submitted;
    private readonly TimeSpan _confirmTimeout;

    /// <summary>How long a submitted ticket may sit in the queue before it is reported as not printed.
    /// A healthy USB thermal printer takes it in about a second.</summary>
    public static readonly TimeSpan DefaultConfirmTimeout = TimeSpan.FromSeconds(15);

    public WindowsSpoolerTicketTransport(string printerName, string documentName = "Falah ticket", Action<int>? submitted = null,
        TimeSpan? confirmTimeout = null)
    {
        _printerName = printerName;
        _documentName = documentName;
        _submitted = submitted;
        _confirmTimeout = confirmTimeout ?? DefaultConfirmTimeout;
    }

    public string Description => $"windows printer \"{_printerName}\" (RAW)";

    /// <summary>The winspool submit (printer, payload, document name, submitted callback → job id).
    /// Replaceable so a test can stand in for a wedged driver; production always uses <see cref="Send"/>.</summary>
    public Func<string, byte[], string, Action<int>?, int> Submit { get; init; } = Send;

    public async Task SendAsync(byte[] payload, CancellationToken ct)
    {
        // PRT-0005 (2026-11-22): the spooler call is synchronous and a wedged driver can hold it for
        // ever. It runs off the agent's loop, and the agent stops waiting when it is asked to stop
        // (service stop, shutdown). A hung call then keeps one pool thread, not the whole agent.
        var jobId = await Task.Run(() => Submit(_printerName, payload, _documentName, _submitted), CancellationToken.None).WaitAsync(ct);
        if (await WatchAsync(_printerName, jobId, _confirmTimeout, ct) is { } notPrinted)
            throw new InvalidOperationException(notPrinted);
    }

    /// <summary>Null when the job printed (or its state cannot be read - the old behaviour), else why not.</summary>
    private static async Task<string?> WatchAsync(string printerName, int jobId, TimeSpan timeout, CancellationToken ct)
    {
        if (!OpenPrinter(printerName, out var printer, IntPtr.Zero)) return null;
        try
        {
            return await SpoolerJobWatch.WaitForOutcomeAsync(new WinspoolProbe(printer), jobId, timeout,
                TimeSpan.FromMilliseconds(250), Task.Delay, () => DateTimeOffset.UtcNow, ct);
        }
        finally { ClosePrinter(printer); }
    }

    /// <summary>Synchronous on purpose: the spooler API is blocking, and wrapping it in a task would
    /// only move the blocking somewhere less obvious. A ticket is a few hundred bytes. Returns the
    /// queue's job id.</summary>
    internal static int Send(string printerName, byte[] payload, string documentName = "Falah ticket", Action<int>? submitted = null)
    {
        if (!OpenPrinter(printerName, out var printer, IntPtr.Zero))
            throw new InvalidOperationException(
                $"Could not open the printer \"{printerName}\": {LastErrorMessage()}");

        try
        {
            var info = new DocInfo
            {
                DocName = documentName,
                OutputFile = null,
                // Without RAW the driver renders the bytes as a document and the ESC/POS commands
                // print as garbage characters instead of controlling the printer.
                DataType = "RAW",
            };

            var submittedJobId = StartDocPrinter(printer, 1, info);
            if (submittedJobId == 0)
                throw new InvalidOperationException($"The printer rejected the job: {LastErrorMessage()}");

            try
            {
                submitted?.Invoke(submittedJobId);
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
            return submittedJobId;
        }
        finally { ClosePrinter(printer); }
    }

    private sealed class WinspoolProbe : ISpoolerProbe
    {
        private readonly IntPtr _printer;
        public WinspoolProbe(IntPtr printer) => _printer = printer;

        public SpoolerPrinterState? ReadPrinter()
        {
            GetPrinter(_printer, 2, IntPtr.Zero, 0, out var needed);
            if (needed <= 0) return null;
            var buffer = Marshal.AllocHGlobal(needed);
            try
            {
                if (!GetPrinter(_printer, 2, buffer, needed, out _)) return null;
                var info = Marshal.PtrToStructure<PrinterInfo2>(buffer);
                return new SpoolerPrinterState(info.Status, info.Attributes);
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }

        public int? ReadJobStatus(int jobId)
        {
            GetJob(_printer, jobId, 1, IntPtr.Zero, 0, out var needed);
            if (needed <= 0) return null; // no such job any more: it left the queue
            var buffer = Marshal.AllocHGlobal(needed);
            try
            {
                if (!GetJob(_printer, jobId, 1, buffer, needed, out _)) return null;
                return Marshal.PtrToStructure<JobInfo1>(buffer).Status;
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct JobInfo1
    {
        public int JobId;
        public IntPtr PrinterName, MachineName, UserName, Document, Datatype, StatusText;
        public int Status, Priority, Position, TotalPages, PagesPrinted;
        public SystemTime Submitted;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemTime { public short Year, Month, DayOfWeek, Day, Hour, Minute, Second, Milliseconds; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PrinterInfo2
    {
        public IntPtr ServerName, PrinterName, ShareName, PortName, DriverName, Comment, Location, DevMode,
            SepFile, PrintProcessor, Datatype, Parameters, SecurityDescriptor;
        public int Attributes, Priority, DefaultPriority, StartTime, UntilTime, Status, Jobs, AveragePpm;
    }

    [DllImport("winspool.drv", EntryPoint = "GetJobW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetJob(IntPtr printer, int jobId, int level, IntPtr job, int bufferSize, out int needed);

    [DllImport("winspool.drv", EntryPoint = "GetPrinterW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetPrinter(IntPtr printer, int level, IntPtr info, int bufferSize, out int needed);

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

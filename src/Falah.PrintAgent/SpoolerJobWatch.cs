namespace Falah.PrintAgent;

/// <summary>What the Windows print queue says about a printer: GetPrinter level 2's Status and
/// Attributes words.</summary>
public readonly record struct SpoolerPrinterState(int Status, int Attributes);

/// <summary>Reads the queue's state. The real one calls winspool (WindowsSpoolerTicketTransport); tests
/// use a scripted one.</summary>
public interface ISpoolerProbe
{
    /// <summary>Null when the printer's state cannot be read.</summary>
    SpoolerPrinterState? ReadPrinter();

    /// <summary>The job's JOB_INFO_1 Status, or null once the job has left the queue.</summary>
    int? ReadJobStatus(int jobId);
}

/// <summary>
/// PRT-0004 (audit 2026-11-18). A RAW job handed to the Windows queue used to count as printed the
/// moment the queue accepted it. With the printer unplugged, out of paper or pointed at the wrong
/// port, the kitchen got nothing and the dashboard still said the ticket had printed - which is what
/// happened in the owner's trial on 2026-11-17 ("nothing printed").
///
/// After the job is submitted this watches it: it has printed when it leaves the queue (or the queue
/// marks it printed or complete); it has not when the printer or the job reports an error, offline or
/// paper out, or when it is still waiting after <c>timeout</c>. A job that did not print is reported as
/// a failure and LEFT in the Windows queue (owner, 2026-11-18): Windows prints it when the printer comes
/// back, so the failure message says so, to stop a reprint becoming a second ticket.
/// </summary>
public static class SpoolerJobWatch
{
    // winspool.h PRINTER_STATUS_* - each means the device cannot print right now.
    private const int PrinterPaused = 0x1, PrinterError = 0x2, PrinterPaperJam = 0x8, PrinterPaperOut = 0x10,
        PrinterOffline = 0x80, PrinterNotAvailable = 0x1000, PrinterNoToner = 0x40000,
        PrinterUserIntervention = 0x100000, PrinterDoorOpen = 0x400000;
    private const int PrinterBlocked = PrinterPaused | PrinterError | PrinterPaperJam | PrinterPaperOut | PrinterOffline
        | PrinterNotAvailable | PrinterNoToner | PrinterUserIntervention | PrinterDoorOpen;
    private const int AttributeWorkOffline = 0x400;

    // winspool.h JOB_STATUS_*.
    private const int JobPaused = 0x1, JobError = 0x2, JobOffline = 0x20, JobPaperOut = 0x40, JobPrinted = 0x80,
        JobDeleted = 0x100, JobBlockedDevq = 0x200, JobUserIntervention = 0x400, JobComplete = 0x1000;
    private const int JobBlocked = JobPaused | JobError | JobOffline | JobPaperOut | JobBlockedDevq | JobUserIntervention;

    public const string LeftInQueueAdvice =
        "التذكرة باقية في طابور ويندوز وستُطبع وحدها عند عودة الطابعة - تأكد قبل إعادة طباعتها حتى لا تتكرر.";

    /// <summary>Why the printer cannot print, in Arabic, or null when nothing says it cannot.</summary>
    public static string? DescribePrinterProblem(SpoolerPrinterState? state)
    {
        if (state is not { } s) return null;
        if ((s.Attributes & AttributeWorkOffline) != 0 || (s.Status & (PrinterOffline | PrinterNotAvailable)) != 0)
            return "الطابعة غير متصلة (Offline).";
        if ((s.Status & PrinterPaperOut) != 0) return "الورق خلص في الطابعة.";
        if ((s.Status & PrinterPaperJam) != 0) return "الورق محشور في الطابعة.";
        if ((s.Status & PrinterDoorOpen) != 0) return "غطاء الطابعة مفتوح.";
        if ((s.Status & PrinterPaused) != 0) return "طابور الطابعة متوقف مؤقتًا في ويندوز.";
        if ((s.Status & PrinterBlocked) != 0) return $"الطابعة في حالة خطأ (0x{s.Status:X}).";
        return null;
    }

    /// <summary>Waits for the submitted job's outcome. Returns null when it printed, or the Arabic
    /// reason it did not.</summary>
    public static async Task<string?> WaitForOutcomeAsync(ISpoolerProbe probe, int jobId, TimeSpan timeout, TimeSpan pollEvery,
        Func<TimeSpan, CancellationToken, Task> delay, Func<DateTimeOffset> now, CancellationToken ct)
    {
        var deadline = now() + timeout;
        while (true)
        {
            var job = probe.ReadJobStatus(jobId);
            if (job is null || (job.Value & (JobPrinted | JobComplete | JobDeleted)) != 0) return null;

            if (DescribePrinterProblem(probe.ReadPrinter()) is { } printerProblem)
                return $"{printerProblem} {LeftInQueueAdvice}";
            if ((job.Value & JobBlocked) != 0)
                return $"ويندوز لم يستطع إرسال التذكرة للطابعة (حالة 0x{job.Value:X}). {LeftInQueueAdvice}";
            if (now() >= deadline)
                return $"لم تخرج التذكرة من طابور ويندوز خلال {timeout.TotalSeconds:0} ثانية. {LeftInQueueAdvice}";

            await delay(pollEvery, ct);
        }
    }
}

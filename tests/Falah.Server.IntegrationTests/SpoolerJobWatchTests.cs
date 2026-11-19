namespace Falah.Server.IntegrationTests;

using Falah.PrintAgent;
using Xunit;

/// <summary>PRT-0004: a ticket counts as printed only when the Windows queue lets go of it, never merely
/// because the queue accepted it. No database; a scripted queue stands in for winspool.</summary>
public class SpoolerJobWatchTests
{
    private sealed class ScriptedQueue : ISpoolerProbe
    {
        private readonly Queue<int?> _jobStatuses;
        public SpoolerPrinterState? Printer { get; set; } = new(0, 0);
        public ScriptedQueue(params int?[] jobStatuses) => _jobStatuses = new Queue<int?>(jobStatuses);
        public SpoolerPrinterState? ReadPrinter() => Printer;
        public int? ReadJobStatus(int jobId) => _jobStatuses.Count > 1 ? _jobStatuses.Dequeue() : _jobStatuses.Peek();
    }

    private static readonly DateTimeOffset Start = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private static Task<string?> Watch(ScriptedQueue queue, TimeSpan? timeout = null)
    {
        var clock = Start;
        return SpoolerJobWatch.WaitForOutcomeAsync(queue, 7, timeout ?? TimeSpan.FromSeconds(15), TimeSpan.FromMilliseconds(250),
            (wait, _) => { clock += wait; return Task.CompletedTask; }, () => clock, default);
    }

    [Fact]
    public async Task A_job_that_leaves_the_queue_has_printed()
    {
        Assert.Null(await Watch(new ScriptedQueue(0x8 /* spooling */, 0x10 /* printing */, null)));
    }

    [Theory]
    [InlineData(0x80)]   // printed
    [InlineData(0x1000)] // complete
    public async Task A_job_the_queue_marks_printed_or_complete_has_printed(int status)
    {
        Assert.Null(await Watch(new ScriptedQueue(status)));
    }

    [Fact]
    public async Task An_offline_printer_is_a_failure_and_the_message_says_the_ticket_is_still_queued()
    {
        var queue = new ScriptedQueue(0) { Printer = new SpoolerPrinterState(Status: 0x80, Attributes: 0) };

        var outcome = await Watch(queue);

        Assert.NotNull(outcome);
        Assert.Contains("غير متصلة", outcome);
        Assert.Contains("طابور ويندوز", outcome);
    }

    [Fact]
    public async Task A_queue_set_to_work_offline_is_a_failure()
    {
        var queue = new ScriptedQueue(0) { Printer = new SpoolerPrinterState(Status: 0, Attributes: 0x400) };

        Assert.Contains("غير متصلة", await Watch(queue));
    }

    [Fact]
    public async Task Paper_out_is_named()
    {
        var queue = new ScriptedQueue(0x40) { Printer = new SpoolerPrinterState(Status: 0x10, Attributes: 0) };

        Assert.Contains("الورق خلص", await Watch(queue));
    }

    [Fact]
    public async Task A_job_in_error_is_a_failure_even_when_the_printer_says_nothing()
    {
        Assert.Contains("0x2", await Watch(new ScriptedQueue(0x2)));
    }

    [Fact]
    public async Task A_job_still_waiting_after_the_timeout_is_a_failure()
    {
        var outcome = await Watch(new ScriptedQueue(0x8), TimeSpan.FromSeconds(5));

        Assert.Contains("5 ثانية", outcome);
    }

    [Fact]
    public async Task An_unreadable_printer_state_does_not_fail_a_job_that_prints()
    {
        var queue = new ScriptedQueue(0x10, null) { Printer = null };

        Assert.Null(await Watch(queue));
    }
}

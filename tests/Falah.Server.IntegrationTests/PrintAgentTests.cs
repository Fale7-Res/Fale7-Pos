namespace Falah.Server.IntegrationTests;

using System.Text;
using Falah.Application.Printing;
using Falah.PrintAgent;
using Xunit;

/// <summary>
/// The print agent's job loop, exercised with no printer and no server. What matters here is not
/// that bytes reach hardware - that needs the physical Xprinter - but that the agent tells the
/// server the truth about every attempt, because a job's Status is what the operator sees and what
/// a reprint decision is made from.
/// </summary>
public class PrintAgentTests
{
    private sealed class FakeServer : IPrintServerClient
    {
        public List<PrintJobDto> Pending { get; } = new();
        public List<ReportPrintJobResultRequest> Reports { get; } = new();
        public Func<string, ComposedTicketDto>? TicketFactory { get; set; }
        public Exception? ReportFailure { get; set; }

        public Task<IReadOnlyList<PrintJobDto>> GetPendingAsync(string route, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<PrintJobDto>>(Pending.Where(j => j.Route == route).ToList());

        public Task<ComposedTicketDto> GetTicketAsync(string jobId, CancellationToken ct) =>
            Task.FromResult(TicketFactory?.Invoke(jobId)
                ?? new ComposedTicketDto("Receipt", "ord-1", "A-1", new[] { new TicketLineDto("line", false, false, false) }));

        public Task ReportResultAsync(ReportPrintJobResultRequest request, CancellationToken ct)
        {
            Reports.Add(request);
            if (ReportFailure is not null) throw ReportFailure;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTransport : ITicketTransport
    {
        public List<byte[]> Sent { get; } = new();
        public Exception? Failure { get; set; }
        public string Description => "fake";

        public Task SendAsync(byte[] payload, CancellationToken ct)
        {
            if (Failure is not null) throw Failure;
            Sent.Add(payload);
            return Task.CompletedTask;
        }
    }

    private static PrintJobDto Job(string id, string route = "Receipt") =>
        new(id, "ord-1", "A-1", route, "Pending", 0, null, null, false, DateTimeOffset.UtcNow);

    /// <summary>Each processor gets its own store file. The agent remembers an attempt it could not
    /// report ON DISK, deliberately - a station that loses power between printing and reporting is
    /// the whole reason it exists - and a single shared file would let one test's unreported job
    /// make the next test re-report instead of print.</summary>
    private static PrintJobProcessor Processor(FakeServer server, FakeTransport transport,
        UnreportedAttemptStore? store = null) =>
        new(server, transport, Encoding.UTF8, "usr-1", _ => { }, store ?? NewStore());

    private static UnreportedAttemptStore NewStore() =>
        new(Path.Combine(Path.GetTempPath(), $"falah-print-agent-{Guid.NewGuid():N}.json"));

    [Fact]
    public async Task A_printed_job_is_reported_as_succeeded()
    {
        var server = new FakeServer();
        var transport = new FakeTransport();
        server.Pending.Add(Job("job-1"));

        await Processor(server, transport).DrainAsync("Receipt", CancellationToken.None);

        Assert.Single(transport.Sent);
        var report = Assert.Single(server.Reports);
        Assert.True(report.Succeeded);
        Assert.Null(report.Error);
        Assert.Equal("job-1", report.PrintJobId);
    }

    [Fact]
    public async Task A_transport_failure_is_reported_as_failed_and_never_as_printed()
    {
        var server = new FakeServer();
        var transport = new FakeTransport { Failure = new IOException("printer offline") };
        server.Pending.Add(Job("job-1"));

        await Processor(server, transport).DrainAsync("Receipt", CancellationToken.None);

        Assert.Empty(transport.Sent);
        var report = Assert.Single(server.Reports);
        Assert.False(report.Succeeded);
        Assert.Contains("printer offline", report.Error);
    }

    [Fact]
    public async Task A_failure_composing_the_ticket_is_still_reported()
    {
        var server = new FakeServer { TicketFactory = _ => throw new InvalidOperationException("no such order") };
        var transport = new FakeTransport();
        server.Pending.Add(Job("job-1"));

        await Processor(server, transport).DrainAsync("Receipt", CancellationToken.None);

        var report = Assert.Single(server.Reports);
        Assert.False(report.Succeeded);
        Assert.Contains("no such order", report.Error);
    }

    [Fact]
    public async Task Only_this_stations_route_is_drained()
    {
        var server = new FakeServer();
        var transport = new FakeTransport();
        server.Pending.Add(Job("job-1", "Receipt"));
        server.Pending.Add(Job("job-2", "Grill"));

        var processed = await Processor(server, transport).DrainAsync("Grill", CancellationToken.None);

        Assert.Equal(1, processed);
        Assert.Equal("job-2", Assert.Single(server.Reports).PrintJobId);
    }

    [Fact]
    public async Task A_backlog_is_drained_oldest_first_in_queue_order()
    {
        var server = new FakeServer();
        var transport = new FakeTransport();
        server.Pending.Add(Job("job-1"));
        server.Pending.Add(Job("job-2"));
        server.Pending.Add(Job("job-3"));

        await Processor(server, transport).DrainAsync("Receipt", CancellationToken.None);

        Assert.Equal(new[] { "job-1", "job-2", "job-3" }, server.Reports.Select(r => r.PrintJobId));
    }

    [Fact]
    public async Task A_report_that_cannot_reach_the_server_does_not_stop_the_queue()
    {
        // The printer worked; the server did not hear about it. The agent must keep going rather
        // than dying with a backlog of paper already printed.
        var server = new FakeServer { ReportFailure = new HttpRequestException("server unreachable") };
        var transport = new FakeTransport();
        server.Pending.Add(Job("job-1"));
        server.Pending.Add(Job("job-2"));

        await Processor(server, transport).DrainAsync("Receipt", CancellationToken.None);

        // Both physically printed, and both outcomes were offered to the server more than once
        // before being kept for later - each retry reusing that attempt's own id, so the server
        // can recognise the replay rather than record attempts that never physically happened.
        Assert.Equal(2, transport.Sent.Count);
        Assert.Equal(new[] { "job-1", "job-2" }, server.Reports.Select(r => r.PrintJobId).Distinct());
        Assert.All(server.Reports.GroupBy(r => r.PrintJobId),
            byJob => Assert.Single(byJob.Select(r => r.RequestId).Distinct()));
    }

    /// <summary>The defect this store exists for: when the report could not be delivered, the job
    /// stayed Pending on the server, the next poll handed the agent the same job, and it PRINTED IT
    /// AGAIN. Three failed reports meant three tickets out of the kitchen printer for one order.</summary>
    [Fact]
    public async Task A_job_whose_outcome_could_not_be_reported_is_re_reported_not_reprinted()
    {
        var store = NewStore();
        var transport = new FakeTransport();

        var unreachable = new FakeServer { ReportFailure = new HttpRequestException("server unreachable") };
        unreachable.Pending.Add(Job("job-1"));
        await Processor(unreachable, transport, store).DrainAsync("Receipt", CancellationToken.None);

        Assert.Single(transport.Sent);
        var attemptId = unreachable.Reports[0].RequestId;

        // The server comes back, and hands out the same still-Pending job.
        var recovered = new FakeServer();
        recovered.Pending.Add(Job("job-1"));
        await Processor(recovered, transport, store).DrainAsync("Receipt", CancellationToken.None);

        Assert.Single(transport.Sent);                       // no second ticket
        Assert.Equal(attemptId, Assert.Single(recovered.Reports).RequestId);

        // Once the server has it, the record is gone - a later job of the same id would print.
        Assert.Null(store.Find("job-1"));
    }

    [Fact]
    public async Task Each_attempt_carries_its_own_request_id()
    {
        var server = new FakeServer();
        var transport = new FakeTransport();
        server.Pending.Add(Job("job-1"));
        server.Pending.Add(Job("job-2"));

        await Processor(server, transport).DrainAsync("Receipt", CancellationToken.None);

        Assert.Equal(2, server.Reports.Select(r => r.RequestId).Distinct().Count());
        Assert.All(server.Reports, r => Assert.False(string.IsNullOrWhiteSpace(r.RequestId)));
    }

    [Fact]
    public void The_wire_ticket_maps_to_the_domain_ticket_without_losing_formatting()
    {
        var dto = new ComposedTicketDto("Grill", "ord-9", "B-9", new[]
        {
            new TicketLineDto("عنوان", Bold: true, Centered: true, LargeFont: true),
            new TicketLineDto("سطر عادي", Bold: false, Centered: false, LargeFont: false),
        });

        var ticket = PrintJobProcessor.ToDomain(dto);

        Assert.Equal(Falah.Domain.Printing.PrintRoute.Grill, ticket.Route);
        Assert.Equal("B-9", ticket.OrderNumber);
        Assert.Collection(ticket.Lines,
            first =>
            {
                Assert.Equal("عنوان", first.Text);
                Assert.True(first.Bold);
                Assert.True(first.Centered);
                Assert.True(first.LargeFont);
            },
            second =>
            {
                Assert.Equal("سطر عادي", second.Text);
                Assert.False(second.Bold);
                Assert.False(second.Centered);
                Assert.False(second.LargeFont);
            });
    }

    [Fact]
    public void Configuration_is_rejected_before_the_agent_starts_printing_nowhere()
    {
        var problems = new AgentOptions
        {
            ServerBaseAddress = "http://localhost:5151",
            Route = "NotARoute",
            ReportedByUserId = "",
            Transport = "network",
            PrinterHost = "",
        }.Validate();

        Assert.Contains(problems, p => p.Contains("Route"));
        Assert.Contains(problems, p => p.Contains("ReportedByUserId"));
        Assert.Contains(problems, p => p.Contains("PrinterHost"));
    }

    [Fact]
    public void A_valid_station_configuration_reports_no_problems()
    {
        var problems = new AgentOptions
        {
            ServerBaseAddress = "http://localhost:5151",
            Route = "Charcoal",
            ReportedByUserId = "usr-1",
            Transport = "network",
            PrinterHost = "192.168.1.50",
        }.Validate();

        Assert.Empty(problems);
    }
}

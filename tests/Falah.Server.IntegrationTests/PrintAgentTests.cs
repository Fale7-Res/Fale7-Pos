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
    [Theory]
    [InlineData("Receipt")]
    [InlineData("receipt")]
    [InlineData("RECEIPT")]
    [InlineData("0")]
    public void Route_aliases_use_one_lock_and_queue_identity(string route)
    {
        var options = new AgentOptions { Route = route, Pin = "test", ReportedByUserId = "test" };
        Assert.Empty(options.Validate());
        Assert.Equal("Receipt", options.Route);
    }

    [Fact]
    public async Task Cancellation_during_send_leaves_a_durable_uncertain_attempt_and_does_not_reprint()
    {
        var path = Path.Combine(Path.GetTempPath(), $"falah-interrupted-{Guid.NewGuid():N}.json");
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var server = new FakeServer();
        var transport = new FakeTransport { Failure = new OperationCanceledException(cancelled.Token) };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Processor(server, transport, new UnreportedAttemptStore(path)).ProcessAsync(Job("interrupted"), cancelled.Token));
        Assert.NotNull(new UnreportedAttemptStore(path).Find("interrupted"));
        transport.Failure = null;
        await Processor(server, transport, new UnreportedAttemptStore(path)).ProcessAsync(Job("interrupted"), CancellationToken.None);
        Assert.Empty(transport.Sent);
        Assert.False(Assert.Single(server.Reports).Succeeded);
    }

    [Fact]
    public void Corrupt_attempt_journal_fails_closed()
    {
        var path = Path.Combine(Path.GetTempPath(), $"falah-corrupt-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{broken");
        Assert.Throws<InvalidOperationException>(() => new UnreportedAttemptStore(path));
    }

    private sealed class FakeServer : IPrintServerClient
    {
        public List<PrintJobDto> Pending { get; } = new();
        public List<ReportPrintJobResultRequest> Reports { get; } = new();
        public Func<string, ComposedTicketDto>? TicketFactory { get; set; }
        public Exception? ReportFailure { get; set; }

        public Task AuthenticateAsync(string pin, CancellationToken ct) => Task.CompletedTask;

        public Task<IReadOnlyList<PrintJobDto>> GetPendingAsync(string route, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<PrintJobDto>>(Pending.Where(j => j.Route == route).ToList());

        public Task<ComposedTicketDto> GetTicketAsync(string jobId, CancellationToken ct) =>
            Task.FromResult(TicketFactory?.Invoke(jobId)
                ?? new ComposedTicketDto("Receipt", "ord-1", "A-1", new[] { new TicketElementDto("text", "line") }));

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
            new TicketElementDto("text", "عنوان", Bold: true, Align: "center", Size: "large"),
            new TicketElementDto("text", "سطر عادي"),
        });

        var ticket = PrintJobProcessor.ToDomain(dto);

        Assert.Equal(Falah.Domain.Printing.PrintRoute.Grill, ticket.Route);
        Assert.Equal("B-9", ticket.OrderNumber);
        Assert.Collection(ticket.Lines,
            first =>
            {
                var text = Assert.IsType<Falah.Domain.Printing.TicketText>(first);
                Assert.Equal("عنوان", text.Text);
                Assert.True(text.Bold);
                Assert.Equal(Falah.Domain.Printing.TicketAlign.Center, text.Align);
                Assert.Equal(Falah.Domain.Printing.TicketTextSize.Large, text.Size);
            },
            second =>
            {
                var text = Assert.IsType<Falah.Domain.Printing.TicketText>(second);
                Assert.Equal("سطر عادي", text.Text);
                Assert.False(text.Bold);
                Assert.Equal(Falah.Domain.Printing.TicketAlign.Start, text.Align);
                Assert.Equal(Falah.Domain.Printing.TicketTextSize.Normal, text.Size);
            });
    }

    /// <summary>The wire contract in BOTH directions, for every element kind. The server composes
    /// and the agent renders in separate processes, so a mapping that is lossy or asymmetric in
    /// either direction shows up as a wrong ticket on paper rather than as a failing build.</summary>
    [Fact]
    public void Every_element_kind_survives_a_round_trip_through_the_wire_contract()
    {
        var original = new Falah.Domain.Printing.TicketElement[]
        {
            new Falah.Domain.Printing.TicketText("عنوان", Bold: true,
                Align: Falah.Domain.Printing.TicketAlign.Center, Size: Falah.Domain.Printing.TicketTextSize.Display,
                Inverted: true),
            new Falah.Domain.Printing.TicketRow(new[]
            {
                new Falah.Domain.Printing.TicketCell("شاورما", 50),
                new Falah.Domain.Printing.TicketCell("2", 20, Falah.Domain.Printing.TicketAlign.Center),
                new Falah.Domain.Printing.TicketCell("85 ج", 30, Falah.Domain.Printing.TicketAlign.End, Bold: true),
            }),
            new Falah.Domain.Printing.TicketRule(Falah.Domain.Printing.TicketRuleStyle.Dashed),
            new Falah.Domain.Printing.TicketCut(),
            new Falah.Domain.Printing.TicketRow(new[] { new Falah.Domain.Printing.TicketCell("ملاحظة", 100) },
                Falah.Domain.Printing.TicketTextSize.Title, Inverted: false, Boxed: true, Continuation: true),
        };

        var roundTripped = original
            .Select(Falah.Application.Printing.TicketElementMapping.ToDto)
            .Select(Falah.Application.Printing.TicketElementMapping.ToDomain)
            .ToList();

        Assert.Equal(original.Length, roundTripped.Count);
        Assert.Equal(original[0], roundTripped[0]);
        Assert.Equal(original[2], roundTripped[2]);
        Assert.IsType<Falah.Domain.Printing.TicketCut>(roundTripped[3]);

        var continuation = Assert.IsType<Falah.Domain.Printing.TicketRow>(roundTripped[4]);
        Assert.True(continuation.Boxed);
        Assert.True(continuation.Continuation);
        Assert.False(continuation.Inverted);
        Assert.Equal(Falah.Domain.Printing.TicketTextSize.Title, continuation.Size);

        var row = Assert.IsType<Falah.Domain.Printing.TicketRow>(roundTripped[1]);
        Assert.Collection(row.Cells,
            c => { Assert.Equal("شاورما", c.Text); Assert.Equal(50, c.WidthPercent); Assert.False(c.Bold); },
            c => { Assert.Equal("2", c.Text); Assert.Equal(Falah.Domain.Printing.TicketAlign.Center, c.Align); },
            c => { Assert.Equal("85 ج", c.Text); Assert.Equal(Falah.Domain.Printing.TicketAlign.End, c.Align); Assert.True(c.Bold); });
    }

    /// <summary>A cut is a real boundary: one job must be able to produce more than one physical
    /// ticket, which is how the cashier flow prints a customer order-number ticket and a detailed
    /// ticket under a single idempotency key.</summary>
    [Fact]
    public void A_cut_element_makes_the_renderer_emit_a_second_cut_command()
    {
        Falah.Domain.Printing.ComposedTicket Build(params Falah.Domain.Printing.TicketElement[] elements) =>
            new(Falah.Domain.Printing.PrintRoute.Receipt, "ord-1", "#0001", elements);

        var withoutCut = Falah.Domain.Printing.EscPosTicketRenderer.Render(
            Build(new Falah.Domain.Printing.TicketText("أ"), new Falah.Domain.Printing.TicketText("ب")));
        var withCut = Falah.Domain.Printing.EscPosTicketRenderer.Render(
            Build(new Falah.Domain.Printing.TicketText("أ"), new Falah.Domain.Printing.TicketCut(), new Falah.Domain.Printing.TicketText("ب")));

        Assert.Equal(1, CountCuts(withoutCut));
        Assert.Equal(2, CountCuts(withCut));
    }

    private static int CountCuts(byte[] bytes)
    {
        var count = 0;
        for (var i = 0; i + 2 < bytes.Length; i++)
            if (bytes[i] == 0x1D && bytes[i + 1] == 0x56 && bytes[i + 2] == 0x00) count++;
        return count;
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
            Pin = "1234",
            Transport = "network",
            PrinterHost = "192.168.1.50",
        }.Validate();

        Assert.Empty(problems);
    }
}

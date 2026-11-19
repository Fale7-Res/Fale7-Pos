namespace Falah.Server.IntegrationTests;

using Falah.Application.Printing;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.PrintAgent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Two print agents serving the same route from two machines never print the same ticket.
///
/// The pending queue was handed to whoever asked, so a second agent for the same route on another
/// machine - which the owner wants to be possible - printed every ticket a second time (the named
/// mutex only stops a second agent on the SAME machine). An agent now claims the jobs it will print,
/// for a lease: nobody else is handed them until it reports a result or the lease runs out, which is
/// what frees the jobs of an agent that crashed or lost its printer.
/// </summary>
public class PrintJobClaimTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private string _agentUserId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        var database = await _factory.QueryAsync(db => Task.FromResult(db.Database.GetDbConnection().Database));
        Assert.StartsWith("FalahTest_", database);
        // A print station's own account: print_agent and nothing else (SEC5).
        _agentUserId = await _factory.SeedUserAsync("محطة الطباعة", "print_agent", "print_agent");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Two_agents_polling_the_same_route_at_once_never_receive_the_same_job()
    {
        var jobIds = await SeedPendingJobsAsync("Grill", 12);
        using var kitchen = await AgentAsync("kitchen-pc");
        using var counter = await AgentAsync("counter-pc");

        var received = new HashSet<string>();
        for (var round = 0; round < 24 && received.Count < jobIds.Count; round++)
        {
            var both = await Task.WhenAll(kitchen.GetPendingAsync("Grill", default), counter.GetPendingAsync("Grill", default));
            foreach (var batch in both) Assert.InRange(batch.Count, 0, 1);
            Assert.Empty(both[0].Select(j => j.Id).Intersect(both[1].Select(j => j.Id)));
            for (var i = 0; i < both.Length; i++)
            {
                if (both[i].Count == 0) continue; // READPAST may temporarily skip locked rows.
                var job = both[i][0]; Assert.True(received.Add(job.Id));
                await (i == 0 ? kitchen : counter).ReportResultAsync(new ReportPrintJobResultRequest(
                    Guid.NewGuid().ToString("N"), job.Id, true, null, _agentUserId), default);
            }
        }
        Assert.Equal(jobIds.ToHashSet(), received);

    }

    [Fact]
    public async Task A_claimed_job_goes_to_another_agent_only_once_its_lease_has_run_out()
    {
        var jobId = (await SeedPendingJobsAsync("Receipt", 1)).Single();
        using var holder = await AgentAsync("pc-a");
        using var other = await AgentAsync("pc-b");

        Assert.Single(await holder.GetPendingAsync("Receipt", CancellationToken.None));
        Assert.Empty(await other.GetPendingAsync("Receipt", CancellationToken.None));

        await ExpireClaimAsync(jobId);   // the holder crashed and never came back

        Assert.Equal(jobId, Assert.Single(await other.GetPendingAsync("Receipt", CancellationToken.None)).Id);
    }

    [Fact]
    public async Task Reporting_a_result_releases_the_claim()
    {
        // A failed job is marked Failed (retried by a reprint or a later report, never re-polled), so
        // what matters is that no agent is left holding it.
        var jobId = (await SeedPendingJobsAsync("Receipt", 1)).Single();
        using var holder = await AgentAsync("pc-a");
        Assert.Single(await holder.GetPendingAsync("Receipt", CancellationToken.None));

        await holder.ReportResultAsync(new ReportPrintJobResultRequest(
            $"pra-{Guid.NewGuid():N}", jobId, false, "paper out", _agentUserId), CancellationToken.None);

        var job = await _factory.QueryAsync(db => db.PrintJobs.AsNoTracking().FirstAsync(j => j.Id == jobId));
        Assert.Equal("Failed", job.Status);
        Assert.Null(job.ClaimedBy);
        Assert.Null(job.ClaimedUntilUtc);
    }

    [Fact]
    public async Task An_agent_restarted_under_the_same_identity_picks_its_own_claims_back_up()
    {
        (await SeedPendingJobsAsync("Receipt", 1)).Single();
        using (var before = await AgentAsync("pc-a"))
            Assert.Single(await before.GetPendingAsync("Receipt", CancellationToken.None));

        using var after = await AgentAsync("pc-a");
        Assert.Single(await after.GetPendingAsync("Receipt", CancellationToken.None));
    }

    // ---------- Helpers ----------

    private async Task<PrintServerClient> AgentAsync(string agentId)
    {
        var agent = new PrintServerClient(_factory.CreateClient(), agentId);
        await agent.AuthenticateAsync(_factory.PinFor(_agentUserId), CancellationToken.None);
        return agent;
    }

    private async Task<List<string>> SeedPendingJobsAsync(string route, int count)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var ids = new List<string>();
        for (var i = 0; i < count; i++)
        {
            var id = $"printjob-{Guid.NewGuid():N}";
            db.PrintJobs.Add(new PrintJobEntity
            {
                Id = id, CreateRequestId = $"req-{Guid.NewGuid():N}", OrderId = $"ord-{i}", OrderNumber = $"#{1000 + i}",
                Route = route, Status = "Pending", AttemptCount = 0, IsReprint = false, CreatedBy = "seed",
                CreatedAtUtc = DateTimeOffset.UtcNow.AddSeconds(i),
            });
            ids.Add(id);
        }
        await db.SaveChangesAsync();
        return ids;
    }

    private async Task ExpireClaimAsync(string jobId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE PrintJobs SET ClaimedUntilUtc = DATEADD(minute, -1, SYSDATETIMEOFFSET()) WHERE Id = {0}", jobId);
    }

    [Fact]
    public async Task A_late_result_cannot_overwrite_the_agent_that_took_over_the_job()
    {
        var id = (await SeedPendingJobsAsync("Receipt", 1)).Single();
        using var first = await AgentAsync("old"); using var next = await AgentAsync("new");
        Assert.Single(await first.GetPendingAsync("Receipt", default));
        await ExpireClaimAsync(id);
        Assert.Single(await next.GetPendingAsync("Receipt", default));
        await next.ReportResultAsync(new ReportPrintJobResultRequest(Guid.NewGuid().ToString("N"), id, true, null, _agentUserId), default);
        await first.ReportResultAsync(new ReportPrintJobResultRequest(Guid.NewGuid().ToString("N"), id, false, "late paper-out", _agentUserId), default);
        var job = await _factory.QueryAsync(db => db.PrintJobs.AsNoTracking().SingleAsync(j => j.Id == id));
        Assert.Equal("Succeeded", job.Status);
        Assert.Equal(1, job.AttemptCount);
        Assert.Equal(2, await _factory.QueryAsync(db => db.PrintJobAttempts.CountAsync(a => a.PrintJobId == id)));
    }
    [Fact]
    public async Task An_agent_claims_only_the_next_ticket_instead_of_leasing_the_whole_backlog()
    {
        await SeedPendingJobsAsync("Receipt", 12);
        using var agent = await AgentAsync("bounded");
        Assert.Single(await agent.GetPendingAsync("Receipt", default));
    }


    private sealed class TicketClient(PrintServerClient inner) : IPrintServerClient
    {
        public Task AuthenticateAsync(string pin, CancellationToken ct) => inner.AuthenticateAsync(pin, ct);
        public Task<IReadOnlyList<PrintJobDto>> GetPendingAsync(string route, CancellationToken ct) => inner.GetPendingAsync(route, ct);
        public Task<ComposedTicketDto> GetTicketAsync(string id, CancellationToken ct) => Task.FromResult(new ComposedTicketDto("Receipt", id, id, new[] { new TicketElementDto("text", id) }));
        public Task ReportResultAsync(ReportPrintJobResultRequest r, CancellationToken ct) => inner.ReportResultAsync(r, ct);
        public Task<bool> StartSendingAsync(PrintJobDto job, CancellationToken ct) => inner.StartSendingAsync(job, ct);
    }
    private sealed class PausedTransport(bool pause) : ITicketTransport
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Sent { get; private set; }
        public string Description => "test transport";
        public async Task SendAsync(byte[] data, CancellationToken ct) { Entered.TrySetResult(); if (pause) await Release.Task.WaitAsync(ct); Sent++; }
    }
    [Fact]
    public async Task A_slow_physical_send_is_not_reclaimed_even_after_its_lease_expires()
    {
        var ids = await SeedPendingJobsAsync("Receipt", 2);
        using var first = await AgentAsync("slow"); using var other = await AgentAsync("fast");
        var slow = new PausedTransport(true); var fast = new PausedTransport(false);
        var store1 = new UnreportedAttemptStore(Path.Combine(Path.GetTempPath(), $"falah-fenced-{Guid.NewGuid():N}.json"));
        var store2 = new UnreportedAttemptStore(Path.Combine(Path.GetTempPath(), $"falah-fenced-{Guid.NewGuid():N}.json"));
        var one = new PrintJobProcessor(new TicketClient(first), slow, System.Text.Encoding.UTF8, _agentUserId, _ => { }, store1);
        var two = new PrintJobProcessor(new TicketClient(other), fast, System.Text.Encoding.UTF8, _agentUserId, _ => { }, store2);
        var inFlight = one.DrainAsync("Receipt", default);
        await slow.Entered.Task.WaitAsync(TimeSpan.FromSeconds(20));
        try
        {
            await ExpireClaimAsync(ids[0]);
            Assert.Equal(1, await two.DrainAsync("Receipt", default));
            Assert.Equal("Sending", await _factory.QueryAsync(db => db.PrintJobs.Where(j => j.Id == ids[0]).Select(j => j.Status).SingleAsync()));
        }
        finally { slow.Release.TrySetResult(); }
        Assert.Equal(1, await inFlight);
        Assert.Equal(1, slow.Sent); Assert.Equal(1, fast.Sent);
        Assert.Equal(2, await _factory.QueryAsync(db => db.PrintJobs.CountAsync(j => ids.Contains(j.Id) && j.Status == "Succeeded")));
        Assert.Empty(await other.GetPendingAsync("Receipt", default));
    }


    [Fact]
    public async Task Claim_identity_is_validated_before_it_can_overflow_the_database_column()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.TokenFor(_agentUserId));
        foreach (var id in new[] { new string('a', 161), "station|spoofed" })
        {
            var response = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(client, "/api/print-jobs/claim", new ClaimPrintJobsRequest("Receipt", id));
            Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity, response.StatusCode);
        }
    }

}

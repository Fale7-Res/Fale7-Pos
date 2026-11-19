namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.CashDrawer;
using Falah.Application.Printing;
using Falah.Application.Shifts;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.PrintAgent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// SEC5. The print agent logged in as the demo manager - "all" permissions, PIN in a tracked file -
/// when the work it does needs none of that: claim its route's jobs, read their content, report what
/// happened. Those calls were open to any session, so a till could also take a route's tickets (they
/// would never print) or mark an unprinted ticket printed. Claiming and reporting now need
/// print_agent, a station account holding only that permission can do the whole job, and nothing
/// else.
/// </summary>
public class PrintAgentIdentityTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _agentId = default!;
    private string _cashierId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "cash_drawer");
        _agentId = await _factory.SeedUserAsync("محطة الطباعة", "print_agent", "print_agent");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> SeedPendingJobAsync(string route = "Receipt")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var id = $"pj-{Guid.NewGuid():N}";
        db.PrintJobs.Add(new PrintJobEntity
        {
            Id = id, CreateRequestId = $"req-{Guid.NewGuid():N}", OrderId = $"ord-{Guid.NewGuid():N}", OrderNumber = "#0001",
            Route = route, Status = "Pending", AttemptCount = 0, IsReprint = false, CreatedBy = "seed", CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private Task<PrintJobEntity> JobAsync(string id) =>
        _factory.QueryAsync(db => db.PrintJobs.AsNoTracking().FirstAsync(j => j.Id == id));

    [Fact]
    public async Task A_till_cannot_take_a_routes_print_jobs()
    {
        var jobId = await SeedPendingJobAsync();
        var response = await _factory.PostAsSessionOfAsync(_client, _cashierId, "/api/print-jobs/claim", new ClaimPrintJobsRequest("Receipt", "till-1"));

        Assert.False(response.IsSuccessStatusCode);
        Assert.Null((await JobAsync(jobId)).ClaimedBy);
    }

    [Fact]
    public async Task A_till_cannot_mark_a_ticket_printed()
    {
        var jobId = await SeedPendingJobAsync();
        var response = await _client.PostAsJsonAsync($"/api/print-jobs/{jobId}/report-result",
            new ReportPrintJobResultRequest($"pra-{Guid.NewGuid():N}", jobId, true, null, _cashierId));

        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal("Pending", (await JobAsync(jobId)).Status);
    }

    [Fact]
    public async Task A_till_cannot_report_what_happened_to_a_cash_drawer_kick()
    {
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        var kick = await (await _client.PostAsJsonAsync("/api/cash-drawer/open",
            new OpenCashDrawerRequest($"req-{Guid.NewGuid():N}", opened!.ShiftId, "فحص", _cashierId, null, null))).Content.ReadFromJsonAsync<CashDrawerOpenDto>();

        var response = await _client.PostAsJsonAsync("/api/cash-drawer/report-result",
            new ReportCashDrawerResultRequest(kick!.Id, true, null, _cashierId));
        Assert.False(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task A_station_account_holding_only_print_agent_does_the_whole_job()
    {
        // A real sale, so the ticket has real content to read.
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var shift = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        var productId = await _factory.SeedProductAsync(5000);
        var sale = await (await _client.PostAsJsonAsync("/api/orders/checkout", new Falah.Application.Orders.CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shift!.ShiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new Falah.Application.Orders.CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 5000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null)))
            .Content.ReadFromJsonAsync<Falah.Application.Orders.CheckoutOrderResult>();
        (await _client.PostAsJsonAsync($"/api/orders/{sale!.OrderId}/print-jobs",
            new CreatePrintJobsRequest($"pjr-{Guid.NewGuid():N}", sale.OrderId, "Receipt", false, _cashierId, IncludeKitchenRoutes: false)))
            .EnsureSuccessStatusCode();

        using var agent = new PrintServerClient(_factory.CreateClient(), agentId: "pc-1");
        await agent.AuthenticateAsync(_factory.PinFor(_agentId), CancellationToken.None);
        Assert.Equal(_agentId, agent.UserId);

        var claimed = await agent.GetPendingAsync("Receipt", CancellationToken.None);
        Assert.NotEmpty(claimed);
        foreach (var job in claimed)
        {
            Assert.NotEmpty((await agent.GetTicketAsync(job.Id, CancellationToken.None)).Lines);
            await agent.ReportResultAsync(new ReportPrintJobResultRequest($"pra-{Guid.NewGuid():N}", job.Id, true, null, agent.UserId!), CancellationToken.None);
            Assert.Equal("Succeeded", (await JobAsync(job.Id)).Status);
        }
    }

    [Fact]
    public async Task A_station_account_can_do_nothing_else()
    {
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var openShift = await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _agentId, 0));
        Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity, openShift.StatusCode);

        var report = await _client.GetAsync($"/api/reports/owner-dashboard?actorUserId={_agentId}&from=2026-10-01&to=2026-10-02");
        Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity, report.StatusCode);
    }

    /// <summary>The server counts wrong PINs per machine; an agent retrying a dead PIN on every poll
    /// would spend that allowance in seconds and lock out the till on the same machine.</summary>
    [Fact]
    public async Task After_its_pin_is_refused_the_agent_leaves_login_alone_for_a_while()
    {
        using var agent = new PrintServerClient(_factory.CreateClient());
        await agent.AuthenticateAsync(_factory.PinFor(_agentId), CancellationToken.None);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
            db.UserSessions.RemoveRange(await db.UserSessions.Where(s => s.UserId == _agentId).ToListAsync());
            (await db.Users.FirstAsync(u => u.Id == _agentId)).Active = false;
            await db.SaveChangesAsync();
        }

        for (var poll = 0; poll < 5; poll++)
            await Assert.ThrowsAsync<HttpRequestException>(() => agent.GetPendingAsync("Receipt", CancellationToken.None));

        Assert.Equal(1, await _factory.QueryAsync(db => db.ControlAuditEvents.CountAsync(e => e.Action == "pin_login_failed")));
    }
}

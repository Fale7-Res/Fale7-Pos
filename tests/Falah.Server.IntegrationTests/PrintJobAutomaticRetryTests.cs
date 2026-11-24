namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Application.Shifts;
using Falah.Infrastructure.Persistence;
using Falah.PrintAgent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>PRT-0013, owner decision 2026-11-22: a ticket whose print failed is queued again by itself
/// (two more attempts, a pause between them), and every paper printed again says "إعادة طباعة" on it -
/// the failed one may still come out of the Windows queue, and the kitchen must see the second is a copy
/// of the same order, not a new one. Only after the last attempt fails is the job left Failed.
/// The test host turns automatic retries off by default; this class turns them on as production has them.</summary>
public class PrintJobAutomaticRetryTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _agentUserId = default!;
    private string _orderId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?>
        {
            ["PrintJobs:AutomaticRetries"] = "2", ["PrintJobs:RetryDelaySeconds"] = "30",
        });
        _client = _factory.CreateAuthenticatedClient();
        _agentUserId = await _factory.SeedUserAsync("محطة الطباعة", "print_agent", "print_agent");
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var shift = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", cashierId, 0))).Content.ReadFromJsonAsync<OpenShiftResult>();
        var productId = await _factory.SeedProductAsync(10_000);
        _orderId = (await (await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"co-{Guid.NewGuid():N}", CashierId: cashierId, ShiftId: shift!.ShiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 10_000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null,
            BusinessDayId: null))).Content.ReadFromJsonAsync<CheckoutOrderResult>())!.OrderId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<PrintServerClient> AgentAsync()
    {
        var agent = new PrintServerClient(_factory.CreateClient(), "counter-pc");
        await agent.AuthenticateAsync(_factory.PinFor(_agentUserId), CancellationToken.None);
        return agent;
    }

    private async Task EndRetryPauseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<FalahDbContext>().Database.ExecuteSqlRawAsync(
            "UPDATE PrintJobs SET ClaimedUntilUtc = DATEADD(minute, -1, SYSDATETIMEOFFSET()) WHERE OrderId = {0}", _orderId);
    }

    private static List<string> Text(ComposedTicketDto ticket) =>
        ticket.Lines.SelectMany(TicketDtoText.Of).Where(t => t is not null).Select(t => t!).ToList();

    private async Task FailAsync(PrintServerClient agent, string jobId) =>
        await agent.ReportResultAsync(new ReportPrintJobResultRequest($"pra-{Guid.NewGuid():N}", jobId, false, "الورق خلص", _agentUserId), default);

    [Fact]
    public async Task A_failed_ticket_comes_back_after_the_pause_marked_as_a_reprint()
    {
        using var agent = await AgentAsync();
        var job = Assert.Single(await agent.GetPendingAsync("Receipt", default));
        Assert.DoesNotContain("إعادة طباعة", Text(await agent.GetTicketAsync(job.Id, default)));

        await FailAsync(agent, job.Id);
        var waiting = await _factory.QueryAsync(db => db.PrintJobs.AsNoTracking().SingleAsync(j => j.Id == job.Id));
        Assert.Equal("Pending", waiting.Status);
        Assert.Equal("الورق خلص", waiting.LastError);
        Assert.Empty(await agent.GetPendingAsync("Receipt", default));   // not before the pause

        await EndRetryPauseAsync();
        var again = Assert.Single(await agent.GetPendingAsync("Receipt", default));
        Assert.Equal(job.Id, again.Id);
        Assert.Contains("إعادة طباعة", Text(await agent.GetTicketAsync(again.Id, default)));
        Assert.Equal(1, await _factory.QueryAsync(db => db.Orders.CountAsync(o => o.Id == _orderId)));
    }

    [Fact]
    public async Task After_the_last_automatic_attempt_fails_the_ticket_is_left_failed()
    {
        using var agent = await AgentAsync();
        var jobId = Assert.Single(await agent.GetPendingAsync("Receipt", default)).Id;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            if (attempt > 1) { await EndRetryPauseAsync(); Assert.Single(await agent.GetPendingAsync("Receipt", default)); }
            await FailAsync(agent, jobId);
        }

        var job = await _factory.QueryAsync(db => db.PrintJobs.AsNoTracking().SingleAsync(j => j.Id == jobId));
        Assert.Equal("Failed", job.Status);
        Assert.Equal(3, job.AttemptCount);
        await EndRetryPauseAsync();
        Assert.Empty(await agent.GetPendingAsync("Receipt", default));
    }
}

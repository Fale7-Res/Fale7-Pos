namespace Falah.Server.IntegrationTests;
using System.Net;
using Falah.Application.Printing;
using Falah.PrintAgent;
public class AgentStartupLoginTests
{
    private sealed class Server(int failures, bool refused) : IPrintServerClient
    {
        public int Calls;
        public Task AuthenticateAsync(string pin, CancellationToken ct)
        {
            if (++Calls <= failures) throw new HttpRequestException("Do not log request secrets", null,
                refused ? HttpStatusCode.Unauthorized : null);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<PrintJobDto>> GetPendingAsync(string route, CancellationToken ct) => throw new NotSupportedException();
        public Task<ComposedTicketDto> GetTicketAsync(string jobId, CancellationToken ct) => throw new NotSupportedException();
        public Task ReportResultAsync(ReportPrintJobResultRequest request, CancellationToken ct) => throw new NotSupportedException();
    }
    [Fact]
    public async Task A_station_started_before_the_server_retries_with_bounded_backoff()
    {
        var server = new Server(3, false); var waits = new List<TimeSpan>(); var messages = new List<string>();
        await AgentLogin.LogInAsync(server, "secret-test-pin", messages.Add, CancellationToken.None,
            (wait, _) => { waits.Add(wait); return Task.CompletedTask; });
        Assert.Equal(4, server.Calls);
        Assert.Equal(new[] { 2d, 4d, 8d }, waits.Select(w => w.TotalSeconds));
        Assert.DoesNotContain("secret-test-pin", string.Join(" ", messages));
    }
    [Fact]
    public async Task A_refused_station_waits_five_minutes_without_exiting_or_logging_the_pin()
    {
        var server = new Server(1, true); var waits = new List<TimeSpan>(); var messages = new List<string>();
        await AgentLogin.LogInAsync(server, "secret-test-pin", messages.Add, CancellationToken.None,
            (wait, _) => { waits.Add(wait); return Task.CompletedTask; });
        Assert.Equal(2, server.Calls);
        Assert.True(Assert.Single(waits) >= PrintServerClient.RefusedLoginPause);
        Assert.Contains("FALAH_PRINT_AGENT_PIN", string.Join(" ", messages));
        Assert.DoesNotContain("secret-test-pin", string.Join(" ", messages));
    }
    [Fact]
    public async Task Shutdown_interrupts_the_startup_retry_loop()
    {
        using var stop = new CancellationTokenSource(); var server = new Server(100, false);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AgentLogin.LogInAsync(server, "secret-test-pin", _ => { }, stop.Token,
            (_, ct) => { stop.Cancel(); return Task.FromCanceled(ct); }));
        Assert.Equal(1, server.Calls);
    }
}

namespace Falah.Server.IntegrationTests;

using Falah.PrintAgent;
using Xunit;

/// <summary>Runtime logging audit 2026-11-23 (L-05): while the server is unreachable the agent polled and
/// wrote "poll failed" every 3 seconds - about 28,800 lines a day, pushing the useful lines out of its
/// rotating log. Now: one line when the outage starts, one when it ends, and a line again if the reason
/// changes.</summary>
public class PollOutageLogTests
{
    [Fact]
    public void An_outage_is_logged_once_when_it_starts_and_once_when_it_ends()
    {
        var lines = new List<string>();
        var log = new PollOutageLog(lines.Add);

        for (var i = 0; i < 100; i++) log.Failed(new HttpRequestException("No connection could be made"));
        log.Succeeded();
        log.Succeeded();

        Assert.Equal(2, lines.Count);
        Assert.Contains("poll failed", lines[0]);
        Assert.Contains("HttpRequestException", lines[0]);
        Assert.Contains("100", lines[1]);
    }

    [Fact]
    public void A_different_failure_during_an_outage_is_logged()
    {
        var lines = new List<string>();
        var log = new PollOutageLog(lines.Add);
        log.Failed(new HttpRequestException("down"));
        log.Failed(new HttpRequestException("down"));
        log.Failed(new TimeoutException("slow"));
        Assert.Equal(2, lines.Count);
        Assert.Contains("TimeoutException", lines[1]);
    }

    [Fact]
    public void A_healthy_poll_writes_nothing()
    {
        var lines = new List<string>();
        new PollOutageLog(lines.Add).Succeeded();
        Assert.Empty(lines);
    }
}

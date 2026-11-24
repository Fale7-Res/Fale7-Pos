namespace Falah.PrintAgent;

/// <summary>Runtime logging audit 2026-11-23 (L-05): a server outage is written once when it starts (and
/// again only if the failure changes), and once when polling works again with how many polls failed - not
/// one line every poll, which filled the rotating log and pushed the useful lines out.</summary>
public sealed class PollOutageLog(Action<string> log)
{
    private int _failures;
    private string? _lastFailure;

    public void Failed(Exception ex)
    {
        _failures++;
        var failure = $"{ex.GetType().Name}: {ex.Message}";
        if (failure == _lastFailure) return;
        _lastFailure = failure;
        log($"poll failed: {failure} (further identical failures are not logged until polling works again)");
    }

    public void Succeeded()
    {
        if (_failures == 0) return;
        log($"server reachable again after {_failures} failed polls");
        _failures = 0;
        _lastFailure = null;
    }
}

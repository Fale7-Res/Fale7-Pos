namespace Falah.PrintAgent;

/// <summary>Startup survives a late server without rapidly retrying a refused credential.</summary>
public static class AgentLogin
{
    public static async Task LogInAsync(IPrintServerClient server, string pin, Action<string> log,
        CancellationToken ct, Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        delay ??= (wait, token) => Task.Delay(wait, token);
        var backoff = TimeSpan.FromSeconds(2);
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try { await server.AuthenticateAsync(pin, ct); return; }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                var refused = ex is HttpRequestException { StatusCode: not null };
                var wait = refused ? PrintServerClient.RefusedLoginPause : backoff;
                // Exception messages and response bodies can contain credentials. Log only the type.
                log(refused
                    ? $"Login refused; check FALAH_PRINT_AGENT_PIN or appsettings.local.json. Retrying in {wait.TotalMinutes:0} minutes."
                    : $"Login unavailable ({ex.GetType().Name}); retrying in {wait.TotalSeconds:0} seconds.");
                await delay(wait, ct);
                if (!refused) backoff = TimeSpan.FromSeconds(Math.Min(60, backoff.TotalSeconds * 2));
            }
        }
    }
}

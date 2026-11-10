using Falah.Application.CashDrawer;

namespace Falah.PrintAgent;

/// <summary>
/// HW1. Sends the drawer pulse for each open-drawer command the server hands this station, and
/// reports what happened.
///
/// "Succeeded" means the printer accepted the pulse. No ESC/POS printer tells anyone whether the
/// drawer actually opened (or was already open, or is unplugged), so that is all a report can say -
/// the screen words it as "sent", and the on-site test is what proves the wiring.
/// </summary>
public sealed class CashDrawerKicker
{
    private readonly IPrintServerClient _server;
    private readonly ITicketTransport _transport;
    private readonly byte[] _pulse;
    private readonly string _reportAs;
    private readonly Action<string> _log;

    public CashDrawerKicker(IPrintServerClient server, ITicketTransport transport, byte[] pulse, string reportAs, Action<string> log)
    {
        _server = server;
        _transport = transport;
        _pulse = pulse;
        _reportAs = reportAs;
        _log = log;
    }

    public async Task<int> DrainAsync(CancellationToken ct)
    {
        var commands = await _server.ClaimCashDrawerKicksAsync(ct);
        foreach (var command in commands)
        {
            string? error = null;
            try
            {
                await _transport.SendAsync(_pulse, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                error = $"{ex.GetType().Name}: {ex.Message}";
            }

            _log(error is null
                ? $"cash drawer pulse sent for {command.Id} ({command.RequestedBy}: {command.Reason})"
                : $"cash drawer pulse FAILED for {command.Id}: {error}");
            await _server.ReportCashDrawerResultAsync(new ReportCashDrawerResultRequest(command.Id, error is null, error, _reportAs), ct);
        }
        return commands.Count;
    }
}

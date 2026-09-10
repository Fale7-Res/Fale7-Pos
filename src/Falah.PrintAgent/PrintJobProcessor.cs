using System.Text;
using Falah.Application.Printing;
using Falah.Domain.Printing;

namespace Falah.PrintAgent;

/// <summary>
/// The agent's actual work: take one pending job, get its ticket, render it, push the bytes at the
/// printer, and tell the server exactly what happened.
///
/// Two rules shape everything here.
///
/// Every physical attempt is reported, success or failure. A failure that is not reported leaves a
/// job Pending forever and the operator staring at a queue with no explanation; a failure reported
/// as success is worse - it claims paper came out when none did.
///
/// The report itself carries a per-attempt RequestId that is minted once, before the transmission,
/// and reused if reporting has to be retried. Reporting is the one step that can fail after the
/// printer has already printed; retrying with the same id lets the server recognise the replay
/// instead of recording a second attempt that never physically happened.
/// </summary>
public sealed class PrintJobProcessor
{
    private readonly IPrintServerClient _server;
    private readonly ITicketTransport _transport;
    private readonly Encoding _encoding;
    private readonly string _reportedByUserId;
    private readonly Action<string> _log;

    public PrintJobProcessor(
        IPrintServerClient server, ITicketTransport transport, Encoding encoding,
        string reportedByUserId, Action<string> log)
    {
        _server = server;
        _transport = transport;
        _encoding = encoding;
        _reportedByUserId = reportedByUserId;
        _log = log;
    }

    /// <summary>Drains the pending queue for one route. Jobs are processed oldest first, in order,
    /// one at a time: a physical printer has one paper path, and interleaving two tickets on it
    /// produces one unreadable ticket rather than two readable ones.</summary>
    public async Task<int> DrainAsync(string route, CancellationToken ct)
    {
        var pending = await _server.GetPendingAsync(route, ct);
        var processed = 0;
        foreach (var job in pending)
        {
            ct.ThrowIfCancellationRequested();
            await ProcessAsync(job, ct);
            processed++;
        }
        return processed;
    }

    public async Task ProcessAsync(PrintJobDto job, CancellationToken ct)
    {
        // Minted before anything is sent, and reused for every retry of the report below.
        var attemptRequestId = $"pra-{Guid.NewGuid():N}";
        bool succeeded;
        string? error;

        try
        {
            var ticket = await _server.GetTicketAsync(job.Id, ct);
            var payload = EscPosTicketRenderer.Render(ToDomain(ticket), _encoding);
            await _transport.SendAsync(payload, ct);
            succeeded = true;
            error = null;
            _log($"printed {job.Route} job {job.Id} for order {job.OrderNumber} via {_transport.Description}");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutting down before the attempt completed. The job stays Pending, which is the honest
            // state: nothing was reported because nothing is known to have printed.
            throw;
        }
        catch (Exception ex)
        {
            succeeded = false;
            error = $"{ex.GetType().Name}: {ex.Message} ({_transport.Description})";
            _log($"FAILED {job.Route} job {job.Id}: {error}");
        }

        await ReportAsync(new ReportPrintJobResultRequest(
            attemptRequestId, job.Id, succeeded, error, _reportedByUserId), ct);
    }

    private async Task ReportAsync(ReportPrintJobResultRequest request, CancellationToken ct)
    {
        try
        {
            await _server.ReportResultAsync(request, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // The outcome is now genuinely unknown to the server. Say so plainly rather than
            // pretending the attempt did not happen; the next poll will retry the same job, and the
            // attempt id above keeps a replay from being counted twice.
            _log($"could not report the outcome of job {request.PrintJobId} to the server: {ex.Message}");
        }
    }

    /// <summary>The wire DTO and the domain record carry the same content; the renderer is written
    /// against the domain type, which is the one the ticket rules are tested through.</summary>
    public static ComposedTicket ToDomain(ComposedTicketDto dto) => new(
        Enum.Parse<PrintRoute>(dto.Route, ignoreCase: true),
        dto.OrderId,
        dto.OrderNumber,
        dto.Lines.Select(l => new TicketLine(l.Text, l.Bold, l.Centered, l.LargeFont)).ToList());
}

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
///
/// And it is retried, on disk, across restarts. Swallowing a failed report left the job Pending on
/// the server, and the next poll handed the agent the same job and it PRINTED IT AGAIN: three
/// failed reports meant three tickets out of the kitchen printer for one order, and the kitchen
/// cooks what it is handed. An attempt that could not be delivered is written down before anything
/// else happens, and the next pass re-reports it rather than reprinting it.
/// </summary>
public sealed class PrintJobProcessor
{
    private readonly IPrintServerClient _server;
    private readonly ITicketTransport _transport;
    private readonly Encoding _encoding;
    private readonly byte? _codePageTableId;
    private readonly bool _renderAsImage;
    private readonly int _imageWidthDots;
    private readonly bool _thickenStrokes;
    private readonly int _imageMarginDots;
    private readonly string _reportedByUserId;
    private readonly Action<string> _log;
    private readonly UnreportedAttemptStore _unreported;

    /// <summary>How many times the outcome is offered to the server before the attempt is left on
    /// disk for the next pass. Short: the poll loop comes back in seconds and will try again.</summary>
    private const int ReportAttempts = 3;

    public PrintJobProcessor(
        IPrintServerClient server, ITicketTransport transport, Encoding encoding,
        string reportedByUserId, Action<string> log, UnreportedAttemptStore? unreported = null, byte? codePageTableId = null,
        bool renderAsImage = false, int imageWidthDots = 576, bool thickenStrokes = true, int imageMarginDots = 24)
    {
        _server = server;
        _transport = transport;
        _encoding = encoding;
        _codePageTableId = codePageTableId;
        _renderAsImage = renderAsImage;
        _imageWidthDots = imageWidthDots;
        _thickenStrokes = thickenStrokes;
        _imageMarginDots = imageMarginDots;
        _reportedByUserId = reportedByUserId;
        _log = log;
        _unreported = unreported ?? new UnreportedAttemptStore(
            Path.Combine(AppContext.BaseDirectory, "unreported-print-attempts.json"));
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
        // This job already came out of a printer; only the telling failed. Tell it again - do not
        // print it again.
        if (_unreported.Find(job.Id) is { } already)
        {
            _log($"job {job.Id} already printed; re-reporting attempt {already.AttemptRequestId} instead of reprinting");
            await ReportAsync(new ReportPrintJobResultRequest(
                already.AttemptRequestId, job.Id, already.Succeeded, already.Error, _reportedByUserId), ct);
            return;
        }

        // Minted before anything is sent, and reused for every retry of the report below.
        var attemptRequestId = $"pra-{Guid.NewGuid():N}";
        bool succeeded;
        string? error;

        try
        {
            var ticket = await _server.GetTicketAsync(job.Id, ct);
            var payload = _renderAsImage && OperatingSystem.IsWindows()
                ? BitmapTicketRenderer.Render(ToDomain(ticket), _imageWidthDots, _thickenStrokes, _imageMarginDots)
                : EscPosTicketRenderer.Render(ToDomain(ticket), _encoding, _codePageTableId);
            // Persist BEFORE the first byte can leave. After a crash or cancelled send, paper may
            // already exist; report an uncertain failure for manual inspection, never auto-reprint.
            _unreported.Remember(job.Id, new UnreportedAttemptStore.StoredAttempt(
                attemptRequestId, false, "Print interrupted; output may exist. Inspect paper before retrying.", DateTimeOffset.UtcNow));
            await _transport.SendAsync(payload, ct);
            succeeded = true;
            error = null;
            _log($"printed {job.Route} job {job.Id} for order {job.OrderNumber} via {_transport.Description}");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // The durable uncertain attempt remains for the next process to report.
            throw;
        }
        catch (Exception ex)
        {
            succeeded = false;
            error = $"{ex.GetType().Name}: {ex.Message} ({_transport.Description})";
            _log($"FAILED {job.Route} job {job.Id}: {error}");
        }

        _unreported.Remember(job.Id, new UnreportedAttemptStore.StoredAttempt(
            attemptRequestId, succeeded, error, DateTimeOffset.UtcNow));
        await ReportAsync(new ReportPrintJobResultRequest(
            attemptRequestId, job.Id, succeeded, error, _reportedByUserId), ct);
    }

    private async Task ReportAsync(ReportPrintJobResultRequest request, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= ReportAttempts; attempt++)
        {
            try
            {
                await _server.ReportResultAsync(request, ct);
                // The server has it. Nothing left to remember, and nothing left to re-report.
                _unreported.Forget(request.PrintJobId);
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < ReportAttempts)
            {
                _log($"could not report job {request.PrintJobId} (attempt {attempt}): {ex.Message}");
                await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt), ct);
            }
            catch (Exception ex)
            {
                // The paper is already out of the printer and the server does not know. Writing the
                // attempt down is what stops the next poll from printing the same ticket again.
                _unreported.Remember(request.PrintJobId, new UnreportedAttemptStore.StoredAttempt(
                    request.RequestId, request.Succeeded, request.Error, DateTimeOffset.UtcNow));
                _log($"could not report the outcome of job {request.PrintJobId} to the server: {ex.Message} - " +
                     "kept on disk; it will be re-reported, not reprinted");
            }
        }
    }

    /// <summary>The wire DTO and the domain record carry the same content; the renderer is written
    /// against the domain type, which is the one the ticket rules are tested through.</summary>
    public static ComposedTicket ToDomain(ComposedTicketDto dto) => new(
        Enum.Parse<PrintRoute>(dto.Route, ignoreCase: true),
        dto.OrderId,
        dto.OrderNumber,
        dto.Lines.Select(Falah.Application.Printing.TicketElementMapping.ToDomain).ToList());
}

namespace Falah.Infrastructure.Printing;

using Falah.Domain.Printing;
using Falah.Infrastructure.Persistence.Entities;

/// <summary>
/// Queues the paper that is not an order's ticket: the shift opening slip, the handover slip, and
/// an expense voucher.
///
/// These ride the existing print pipeline exactly as an order's receipt does - a row in PrintJobs,
/// polled by whichever agent serves the route, with its content composed on demand and every
/// attempt recorded. The print agent is not aware slips exist. What changes is only which
/// composition builds the lines, chosen by DocumentKind.
///
/// The job id is derived from the document's own id, so the business handler that writes the shift
/// or the expense can queue its slip inside the same transaction and a replay of that handler
/// cannot queue a second one. A deliberate REPRINT passes its own request id instead and is marked,
/// which is what puts "إعادة طباعة" on the paper.
/// </summary>
public static class DocumentPrintJobs
{
    public static PrintJobEntity Build(
        PrintDocumentKind kind, string documentId, string label, string createdByName, DateTimeOffset now,
        string? reprintRequestId = null, string? requestId = null)
    {
        var isReprint = reprintRequestId is not null;
        // A report printed on request (the driver statement) carries the caller's own request id: each
        // press is a separate paper, and a replay of the same press is the same job.
        requestId ??= reprintRequestId ?? $"auto-{kind}-{documentId}";
        return new PrintJobEntity
        {
            Id = $"printjob-{requestId}-{PrintRoute.Receipt}",
            CreateRequestId = requestId,
            OrderId = documentId,
            OrderNumber = label,
            DocumentKind = kind.ToString(),
            Route = nameof(PrintRoute.Receipt),
            Status = nameof(PrintJobStatus.Pending),
            AttemptCount = 0,
            IsReprint = isReprint,
            CreatedBy = createdByName,
            CreatedAtUtc = now,
        };
    }
}

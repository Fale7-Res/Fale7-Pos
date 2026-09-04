namespace Falah.Application.Printing;

/// <summary>Ported from the four-printer topology directly (Cashier/Call Center receipt printers,
/// Charcoal/Grill kitchen printers) - there is no legacy precedent for this exact split (the
/// reference app only ever did a browser window.print(), never real ESC/POS, and combined charcoal
/// and grill into one ticket). RequestId is minted once per "print this order" button press;
/// CustomerRoute is which customer-facing ticket the CALLING station wants (Receipt for Cashier,
/// CallCenter for a phoned-in order's own confirmation copy) - kitchen routes are never requested,
/// they are always and only derived from the order's own items (see PrintRules.DetermineKitchenRoutes).
/// Creating jobs NEVER touches the order/payment/inventory it describes - the critical invariant
/// this whole feature exists to preserve.</summary>
public sealed record CreatePrintJobsRequest(string RequestId, string OrderId, string? CustomerRoute, bool IsReprint, string ActorUserId);

public sealed record PrintJobDto(
    string Id, string OrderId, string OrderNumber, string Route, string Status, int AttemptCount,
    DateTimeOffset? LastAttemptAtUtc, string? LastError, bool IsReprint, DateTimeOffset CreatedAtUtc);

public sealed record CreatePrintJobsResult(IReadOnlyList<PrintJobDto> Jobs, bool WasReplayed);

/// <summary>Ported from nothing in the reference app (it never tracked print outcomes at all) -
/// this is the "track print jobs/status" requirement made real: a station's print agent calls this
/// once per physical attempt, success or failure, and the job's own AttemptCount/LastError/Status
/// update from it. Never creates another order/payment/inventory movement - it only describes what
/// happened when a printer was asked to print.</summary>
public sealed record ReportPrintJobResultRequest(string RequestId, string PrintJobId, bool Succeeded, string? Error, string ReportedByUserId);

public sealed record PrintJobAttemptDto(DateTimeOffset AttemptedAtUtc, bool Succeeded, string? Error, string ReportedBy);

public sealed record PrintJobDetailsDto(PrintJobDto Job, IReadOnlyList<PrintJobAttemptDto> Attempts);

public sealed class PrintingConflictException : Exception
{
    public PrintingConflictException(string message) : base(message) { }
}

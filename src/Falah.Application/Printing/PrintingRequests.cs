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
/// <param name="IncludeKitchenRoutes">False suppresses the preparation tickets and prints only the
/// customer copy. Exists because the restaurant can turn preparation tickets off in Settings; a
/// caller that wants the ordinary behaviour leaves it alone.</param>
public sealed record CreatePrintJobsRequest(
    string RequestId, string OrderId, string? CustomerRoute, bool IsReprint, string ActorUserId,
    bool IncludeKitchenRoutes = true,
    /// <summary>Why this was reprinted, in the cashier's words. Recorded in the audit trail; the
    /// paper itself says only that it is a reprint, by whom and when. Trailing optional so every
    /// existing caller keeps compiling.</summary>
    string? Reason = null);

/// <summary>Reprints a slip that belongs to no order - a shift opening or handover slip, or an
/// expense voucher. The original is queued by the handler that wrote the document; this is the
/// "print it again" path, and its paper is marked as a reprint.</summary>
public sealed record ReprintDocumentRequest(
    string RequestId, string DocumentKind, string DocumentId, string ActorUserId, string? Reason = null);

public sealed record PrintJobDto(
    string Id, string OrderId, string OrderNumber, string Route, string Status, int AttemptCount,
    DateTimeOffset? LastAttemptAtUtc, string? LastError, bool IsReprint, DateTimeOffset CreatedAtUtc);

/// <summary>Every print route's queue at a glance, and the jobs that need a person.
///
/// A FAILED job is the one somebody has to act on - a kitchen ticket that never came out is an
/// order nobody is cooking - and the screen used to list only Pending, so the state that matters
/// most was the one it could not show. StuckCount is Pending jobs old enough that the station's
/// agent is evidently not draining them: nothing is wrong with the job, something is wrong with
/// the printer or the agent, and that is a different thing to go and fix.</summary>
public sealed record PrintRouteStatusDto(
    string Route, int PendingCount, int FailedCount, int StuckCount,
    DateTimeOffset? OldestPendingAtUtc, DateTimeOffset? LastSucceededAtUtc);

public sealed record PrintingOverviewDto(
    IReadOnlyList<PrintRouteStatusDto> Routes,
    IReadOnlyList<PrintJobDto> NeedsAttention,
    int TotalPendingCount, int TotalFailedCount, int TotalStuckCount);

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

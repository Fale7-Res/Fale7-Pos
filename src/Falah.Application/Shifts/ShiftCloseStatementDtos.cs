namespace Falah.Application.Shifts;

using Falah.Domain.Printing;

/// <summary>UI-REQ-002: one print attempt series of the closing statement - the original, then any
/// reprints - so the screen can say whether the paper is queued, printed or failed.</summary>
public sealed record ShiftClosePrintStatusDto(
    string JobId, string Status, bool IsReprint, int AttemptCount, string? LastError, DateTimeOffset CreatedAtUtc);

/// <summary>The closing statement exactly as the close froze it (or rebuilt and flagged
/// <see cref="Recomputed"/> for a shift closed before statements were frozen), with its paper's status.</summary>
public sealed record ShiftCloseStatementResponse(
    ShiftCloseStatement Statement, bool Recomputed, IReadOnlyList<ShiftClosePrintStatusDto> PrintJobs,
    /// <summary>The same composed paper the printer receives, so the screen shows exactly what prints.</summary>
    Falah.Application.Printing.ComposedTicketDto Paper);

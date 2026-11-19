namespace Falah.Application.Delivery;

using Falah.Domain.Delivery;

/// <summary>UI-REQ-001: one driver's statement for one business day and payment filter. The screen,
/// the summary print and the detail print are all built from this one result.</summary>
public sealed record DriverDailyStatementDto(
    string DriverId,
    string DriverName,
    DateOnly BusinessDate,
    string PaymentFilter,
    IReadOnlyList<DriverStatementOrder> Orders,
    DriverStatementTotals Totals,
    DateTimeOffset GeneratedAtUtc);

/// <summary>Queues the statement's paper. <see cref="Detailed"/> adds one block per order under the summary.</summary>
public sealed record PrintDriverDailyStatementRequest(
    string RequestId, DateOnly BusinessDate, string? PaymentFilter, bool Detailed);

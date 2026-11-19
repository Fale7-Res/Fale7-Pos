namespace Falah.Application.Reports;

/// <summary>Money through each treasury account over a span of Cairo calendar dates: what it held
/// at the start, what came in and went out (by movement type), and what it held at the end. Posted
/// movements only, by their own timestamp - the calendar-day basis (docs/METRIC_DEFINITIONS.md).
/// Cash flow, not revenue and not profit: a sale's money appears when and where it was received,
/// a supplier payment when it was paid.</summary>
public sealed record CashFlowReportDto(string FromBusinessDate, string ToBusinessDate, IReadOnlyList<AccountCashFlowDto> Accounts);

/// <summary>ClosingPiastres = OpeningPiastres + InflowPiastres − OutflowPiastres, always; a transfer
/// between two accounts is an outflow of one and an inflow of the other.</summary>
public sealed record AccountCashFlowDto(
    string AccountId, long OpeningPiastres, long InflowPiastres, long OutflowPiastres, long ClosingPiastres,
    IReadOnlyList<CashFlowLineDto> Lines);

public sealed record CashFlowLineDto(string MovementType, long InflowPiastres, long OutflowPiastres, int MovementCount);

/// <summary>Orders voided in a span of Cairo calendar dates (by when they were voided): never sales,
/// so they appear in no sales figure - this is where they are seen. Kind is "electronic_unconfirmed"
/// (an InstaPay / wallet claim that never arrived) or "undelivered_cash_on_delivery" (a delivery that
/// failed or came back). StockDisposition is the operator's answer - "prepared" (waste) or
/// "not_prepared" (back to stock) - or null for a void recorded before the question was asked.</summary>
public sealed record VoidsReportDto(int Count, long TotalPiastres, IReadOnlyList<VoidedOrderDto> Voids);

public sealed record VoidedOrderDto(
    string OrderId, string OrderNumber, string Kind, long TotalPiastres, string? Reason, string? CancelledBy,
    DateTimeOffset? CancelledAtUtc, string? StockDisposition, long ReturnedPiastres = 0);

/// <summary>Every InstaPay / wallet payment a customer says was sent and nobody has confirmed yet -
/// a claim, not money. Current state, not a period: a transfer still unconfirmed from last week is
/// the one that most needs chasing.</summary>
public sealed record PendingElectronicReportDto(int Count, long TotalPiastres, double? OldestAgeMinutes, IReadOnlyList<PendingElectronicPaymentDto> Payments);

/// <summary>AmountPiastres is the pending leg alone - the whole order for a standalone electronic
/// sale, only the electronic part of a split.</summary>
public sealed record PendingElectronicPaymentDto(
    string OrderId, string OrderNumber, string Method, string? Provider, long AmountPiastres,
    DateTimeOffset CreatedAtUtc, double AgeMinutes, string ShiftId);

namespace Falah.Application.Reports;

/// <summary>
/// The owner's daily financial report (P5): every figure the owner asked for, each with its exact definition, its
/// date basis and the records it is read from, so any number can be traced back to transactions. A figure the system
/// cannot know is reported with <see cref="FinancialKpiDto.AmountPiastres"/> = null and Status "unavailable" - never
/// as zero. Estimated profit is Status "estimate", never "actual".
///
/// Two reconciliations travel with it: <see cref="SalesBridge"/> follows the period's own sales to where their money
/// is now, and <see cref="MoneyFlow"/> classifies every posted treasury movement of the period so that internal
/// transfers, corrections and funding are never counted as customer collections. Each carries a residual that is
/// zero when the books agree.
/// </summary>
public sealed record OwnerFinancialReportDto(
    string FromBusinessDate, string ToBusinessDate, DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<FinancialKpiSectionDto> Sections,
    SalesBridgeDto SalesBridge,
    MoneyFlowSummaryDto MoneyFlow,
    IReadOnlyList<string> TimingNotes);

public sealed record FinancialKpiSectionDto(string Key, string Title, IReadOnlyList<FinancialKpiDto> Kpis);

/// <summary>Status: "actual" (from recorded transactions), "estimate" (computed with assumptions, e.g. food cost from
/// current recipes), "partial" (some inputs are missing; the value is a floor - see Note), "unavailable" (cannot be
/// known from the records; AmountPiastres is null). Basis: "sale" (when the sale was made), "movement" (when money
/// moved), "now" (current position), "shift" (shifts of the period's business dates).</summary>
public sealed record FinancialKpiDto(
    string Key, string Label, long? AmountPiastres, int? Count, string Status,
    string Definition, string Basis, string Sources, string? Note = null);

/// <summary>
/// The period's sales (non-cancelled orders created in the period, at full value) followed to where their money is
/// now. GrossSales = ReceivedThroughTreasury + CodSettled + CodHeldByDrivers + CodNotYetCollected + PendingElectronic
/// + ShortfallUnresolved + ShortfallReceivable + ShortfallRealised + Residual, and Residual is 0 when every order is
/// accounted for. CodCollectedAboveDue is money collected beyond what was due - not a sale, outside the identity.
/// Refunds are subtracted only for net sales (a refund counts when made), not in the bridge.
/// </summary>
public sealed record SalesBridgeDto(
    long GrossSalesPiastres,
    long ReceivedThroughTreasuryPiastres,
    long CodSettledPiastres,
    long CodHeldByDriversPiastres,
    long CodNotYetCollectedPiastres,
    long PendingElectronicPiastres,
    long ShortfallUnresolvedPiastres,
    long ShortfallReceivablePiastres,
    long ShortfallRealisedPiastres,
    long ResidualPiastres,
    long CodCollectedAboveDuePiastres,
    long RefundsMadePiastres,
    long NetSalesPiastres,
    long ReceivedAfterPeriodPiastres,
    long ReceivedInPeriodForEarlierSalesPiastres);

/// <summary>
/// Every posted treasury movement of the period (by when it was posted), classified by MoneyFlowRules. Inflow and
/// outflow are seen from the restaurant as a whole: a movement between two of its own accounts is an internal
/// transfer (shown, never counted as received or paid). Closing = Opening + In − Out; Residual = Closing − the sum of
/// the account balances at the period's end, and is 0.
/// </summary>
public sealed record MoneyFlowSummaryDto(
    IReadOnlyList<MoneyFlowLineDto> Lines,
    long CustomerReceiptsPiastres, long CustomerReceiptsCashPiastres, long CustomerReceiptsElectronicPiastres,
    long InternalTransfersPiastres, long OpeningBalancePiastres, long InflowPiastres, long OutflowPiastres,
    long ClosingBalancePiastres, long ResidualPiastres, int UnclassifiedMovementCount);

public sealed record MoneyFlowLineDto(
    string Category, string Label, long InflowPiastres, long OutflowPiastres, long MovedInternallyPiastres,
    int MovementCount, bool CountsAsCustomerReceipt);

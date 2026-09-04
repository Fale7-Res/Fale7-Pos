namespace Falah.Domain.Shifts;

using Falah.Domain.Common;
using Falah.Domain.Financial;

public enum ShiftStatus { Open, Closed }

/// <summary>
/// A single cashier's session from opening the drawer to closing it. Ported from the
/// "blind close" workflow in the current web app's lib/store.tsx (closeShiftBlind /
/// calculateShiftExpectedCash): the cashier reports what they physically counted BEFORE the
/// system reveals what it expected, and a difference beyond the configured threshold is blocked
/// until a manager approves it with a reason.
///
/// This aggregate deliberately does not compute expectedCash itself - that figure is derived
/// from orders, expenses and cash movements the Shift does not own, so the caller (the future
/// application-layer shift-closing use case) supplies it.
/// </summary>
public sealed class Shift
{
    public string Id { get; }
    public string ShiftName { get; }
    public string OpenedByUserId { get; }
    public string? ClosedByUserId { get; private set; }
    public Money OpeningCash { get; }
    public Money? ActualCash { get; private set; }
    public Money? ExpectedCash { get; private set; }
    public Money? CashDifference { get; private set; }
    public string? DifferenceReason { get; private set; }
    public string? DifferenceAuthorizedBy { get; private set; }
    public ShiftStatus Status { get; private set; }
    public DateOnly BusinessDate { get; }
    public DateTimeOffset OpenedAtUtc { get; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }

    private Shift(string id, string shiftName, string openedByUserId, Money openingCash, DateOnly businessDate, DateTimeOffset openedAtUtc)
    {
        if (openingCash.IsNegative) throw new DomainException("رصيد فتح الوردية لا يمكن أن يكون سالبًا.");
        Id = id;
        ShiftName = shiftName;
        OpenedByUserId = openedByUserId;
        OpeningCash = openingCash;
        BusinessDate = businessDate;
        OpenedAtUtc = openedAtUtc;
        Status = ShiftStatus.Open;
    }

    public static Shift Open(string id, string shiftName, string openedByUserId, Money openingCash, DateTimeOffset nowUtc)
        => new(id, shiftName, openedByUserId, openingCash, BusinessClock.BusinessDate(nowUtc), nowUtc);

    /// <summary>Rehydrates a shift already recorded by persistence - open or closed - so a handler
    /// can call Close() on a legitimate aggregate instance (enforcing Status/reason invariants)
    /// rather than writing close-state fields onto the entity directly. Mirrors Order.Reconstitute.</summary>
    private Shift(string id, string shiftName, string openedByUserId, Money openingCash, DateOnly businessDate,
        DateTimeOffset openedAtUtc, ShiftStatus status, string? closedByUserId, Money? actualCash, Money? expectedCash,
        Money? cashDifference, string? differenceReason, string? differenceAuthorizedBy, DateTimeOffset? closedAtUtc)
    {
        Id = id;
        ShiftName = shiftName;
        OpenedByUserId = openedByUserId;
        OpeningCash = openingCash;
        BusinessDate = businessDate;
        OpenedAtUtc = openedAtUtc;
        Status = status;
        ClosedByUserId = closedByUserId;
        ActualCash = actualCash;
        ExpectedCash = expectedCash;
        CashDifference = cashDifference;
        DifferenceReason = differenceReason;
        DifferenceAuthorizedBy = differenceAuthorizedBy;
        ClosedAtUtc = closedAtUtc;
    }

    public static Shift Reconstitute(string id, string shiftName, string openedByUserId, Money openingCash,
        DateOnly businessDate, DateTimeOffset openedAtUtc, ShiftStatus status, string? closedByUserId,
        Money? actualCash, Money? expectedCash, Money? cashDifference, string? differenceReason,
        string? differenceAuthorizedBy, DateTimeOffset? closedAtUtc) =>
        new(id, shiftName, openedByUserId, openingCash, businessDate, openedAtUtc, status, closedByUserId,
            actualCash, expectedCash, cashDifference, differenceReason, differenceAuthorizedBy, closedAtUtc);

    public readonly record struct BlindCloseResult(Money Expected, Money Actual, Money Difference, bool RequiresManagerApproval)
    {
        public bool IsBalanced => Difference.IsZero;
    }

    /// <summary>
    /// Step 1 of closing: compare what was counted against what was expected, and decide whether
    /// a manager approval is required. Does not mutate the shift, so the UI can show this result
    /// and let the cashier back out before anything is committed.
    /// </summary>
    public BlindCloseResult PrepareBlindClose(Money actualCashCounted, Money expectedCash, Money discrepancyThreshold)
    {
        if (Status != ShiftStatus.Open) throw new DomainException("الوردية ليست مفتوحة.");
        if (actualCashCounted.IsNegative) throw new DomainException("لا يمكن أن يكون العد الفعلي سالبًا.");

        var difference = actualCashCounted - expectedCash;
        var requiresApproval = FinancialRules.DiscrepancyNeedsApproval(difference, discrepancyThreshold);
        return new BlindCloseResult(expectedCash, actualCashCounted, difference, requiresApproval);
    }

    /// <summary>Step 2: commit the close. Rejects a discrepancy above the threshold with no
    /// manager name and reason attached - mirrors authorizeShiftClose in the current app.</summary>
    public void Close(string closedByUserId, BlindCloseResult result, string? reason, string? authorizedBy, DateTimeOffset nowUtc)
    {
        if (Status != ShiftStatus.Open) throw new DomainException("الوردية ليست مفتوحة.");
        if (result.RequiresManagerApproval && (string.IsNullOrWhiteSpace(authorizedBy) || string.IsNullOrWhiteSpace(reason)))
            throw new DomainException("الفارق يتجاوز الحد المسموح ويتطلب اعتماد مدير وسببًا واضحًا.");

        ClosedByUserId = closedByUserId;
        ActualCash = result.Actual;
        ExpectedCash = result.Expected;
        CashDifference = result.Difference;
        DifferenceReason = reason;
        DifferenceAuthorizedBy = authorizedBy;
        Status = ShiftStatus.Closed;
        ClosedAtUtc = nowUtc;
    }
}

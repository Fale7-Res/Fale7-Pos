namespace Falah.Domain.Orders;

using Falah.Domain.Common;
using Falah.Domain.Financial;

/// <summary>An immutable historical record of one partial or full refund against an order. An
/// order can carry several of these (repeated partial refunds); the sum of their amounts can never
/// exceed the order's Total - enforced by Order.ApplyRefund.</summary>
public sealed record OrderRefund(
    string Id,
    string OperationId,
    string IdempotencyKey,
    Money Amount,
    PaymentMethod PaymentMethod,
    string TreasuryMovementId,
    DateTimeOffset CreatedAtUtc,
    string CreatedBy,
    string AuthorizedBy,
    string Reason,
    string OriginalShiftId,
    string OperationalShiftId);

namespace Falah.Domain.Financial;

using Falah.Domain.Common;

public enum MovementStatus { Posted, Reversed }

/// <summary>One posted financial fact. Every write anywhere in the system that moves real or
/// claimed money produces exactly one of these, keyed by an idempotency key that a retry or a
/// same-task double click must reuse unchanged. Ported from the shapes built by
/// buildSaleMovement / buildRefundMovement / buildCashLifecycleMovement in
/// lib/financial-domain.mjs.</summary>
public sealed record TreasuryMovement(
    string Id,
    string IdempotencyKey,
    string OperationId,
    string Type,
    string? SourceAccountId,
    string? DestinationAccountId,
    Money Amount,
    string ReferenceType,
    string ReferenceId,
    string? BusinessDayId,
    string? ShiftId,
    string Reason,
    string CreatedBy,
    string? ApprovedBy,
    DateTimeOffset CreatedAtUtc,
    MovementStatus Status);

public static class TreasuryMovementFactory
{
    public static string PaymentAccountFor(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => TreasuryAccounts.Drawer,
        PaymentMethod.Card => TreasuryAccounts.Bank,
        PaymentMethod.InstaPay => TreasuryAccounts.InstaPay,
        PaymentMethod.Wallet => TreasuryAccounts.Wallet,
        _ => throw new DomainException("الدفع المتعدد غير مدعوم في مسار التحصيل المالي الحالي."),
    };

    public static TreasuryMovement BuildSaleMovement(
        string movementId, string operationId, string checkoutRequestId, string orderId,
        PaymentMethod paymentMethod, Money amount, string? businessDayId, string shiftId,
        string createdBy, DateTimeOffset createdAtUtc) => new(
        Id: movementId,
        IdempotencyKey: $"sale-{checkoutRequestId}",
        OperationId: operationId,
        Type: "sale_collection",
        SourceAccountId: null,
        DestinationAccountId: PaymentAccountFor(paymentMethod),
        Amount: amount,
        ReferenceType: "order",
        ReferenceId: orderId,
        BusinessDayId: businessDayId,
        ShiftId: shiftId,
        Reason: $"تحصيل طلب {orderId}",
        CreatedBy: createdBy,
        ApprovedBy: null,
        CreatedAtUtc: createdAtUtc,
        Status: MovementStatus.Posted);

    public static TreasuryMovement BuildRefundMovement(
        string movementId, string operationId, string refundRequestId, string orderId,
        PaymentMethod paymentMethod, Money amount, string? businessDayId, string operationalShiftId,
        string createdBy, string approvedBy, string reason, DateTimeOffset createdAtUtc) => new(
        Id: movementId,
        IdempotencyKey: $"refund-{refundRequestId}",
        OperationId: operationId,
        Type: "refund",
        SourceAccountId: PaymentAccountFor(paymentMethod),
        DestinationAccountId: null,
        Amount: amount,
        ReferenceType: "order_refund",
        ReferenceId: orderId,
        BusinessDayId: businessDayId,
        ShiftId: operationalShiftId,
        Reason: reason,
        CreatedBy: createdBy,
        ApprovedBy: approvedBy,
        CreatedAtUtc: createdAtUtc,
        Status: MovementStatus.Posted);

    public static TreasuryMovement BuildCashLifecycleMovement(
        string movementId, string idempotencyKey, string operationId, string type,
        string? sourceAccountId, string? destinationAccountId, Money amount,
        string referenceType, string referenceId, string? businessDayId, string? shiftId,
        string reason, string createdBy, string? approvedBy, DateTimeOffset createdAtUtc) => new(
        Id: movementId,
        IdempotencyKey: idempotencyKey,
        OperationId: operationId,
        Type: type,
        SourceAccountId: sourceAccountId,
        DestinationAccountId: destinationAccountId,
        Amount: amount,
        ReferenceType: referenceType,
        ReferenceId: referenceId,
        BusinessDayId: businessDayId,
        ShiftId: shiftId,
        Reason: reason,
        CreatedBy: createdBy,
        ApprovedBy: approvedBy,
        CreatedAtUtc: createdAtUtc,
        Status: MovementStatus.Posted);
}

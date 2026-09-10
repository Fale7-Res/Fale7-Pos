namespace Falah.Infrastructure.Orders;

using Falah.Application.Orders;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Infrastructure.Persistence.Entities;

/// <summary>
/// Translates between the Falah.Domain.Orders.Order aggregate and its EF-friendly persistence
/// shape (OrderEntity). Kept entirely in Infrastructure - Domain has no idea this mapping, EF Core,
/// or SQLite exist. ToDomain is the reconstruction path a handler uses before it can call an
/// existing order's own methods (ApplyRefund, ConfirmElectronicPayment); ToEntity is the one-way
/// path a freshly-created order takes on its way into the database.
/// </summary>
public static class OrderMapper
{
    public static Actor ToActor(UserEntity user) => new(
        user.Id, user.Name, user.Role, user.Active,
        user.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet());

    /// <summary>The proper read-side API contract for a client - not the raw EF entity.</summary>
    public static OrderDetailsDto ToDto(OrderEntity order) => new(
        order.Id, order.OrderNumber, order.SequenceNumber, order.OrderType, order.PaymentMethod,
        order.Items.Select(i => new OrderItemDto(i.ProductName, i.VariantName, i.UnitPricePiastres, i.Quantity,
            i.TotalPricePiastres, i.PriceRuleName, i.Notes)).ToList(),
        order.SubtotalPiastres, order.TaxAmountPiastres, order.CompAmountPiastres, order.TotalPiastres,
        order.AmountPaidPiastres, order.ChangeDuePiastres, order.Status, order.IsPaid,
        order.ElectronicPaymentStatus, order.RefundAmountPiastres, order.CreatedAtUtc, order.CashierId,
        order.CustomerName, order.CustomerPhone, order.DeliveryArea, order.DeliveryAddress,
        order.DeliveryStreet, order.DeliveryBuilding, order.DeliveryFloor, order.DeliveryApartment,
        order.DeliveryLandmark, order.DeliveryNotes, order.DeliveryServiceId, order.DeliveryServiceName,
        order.DeliveryFeePiastres, order.DiscountPiastres, order.ShiftId,
        order.SplitPaymentsJson is null ? null
            : System.Text.Json.JsonSerializer.Deserialize<List<WireSplit>>(order.SplitPaymentsJson)!
                .Select(s => new OrderSplitPaymentDto(s.method, s.amountPiastres)).ToList(),
        order.TableNumber);

    public static Order ToDomain(OrderEntity entity)
    {
        var items = entity.Items
            .Select(i => new OrderItem(
                i.Id.ToString(), i.ProductId, i.VariantId, i.ProductName, i.VariantName,
                Money.FromPiastres(i.UnitPricePiastres), Money.FromPiastres(i.ListUnitPricePiastres),
                i.Quantity, Money.FromPiastres(i.TotalPricePiastres), i.SendToGrill,
                i.PriceRuleId, i.PriceRuleName, i.Notes, i.PreparationStation))
            .ToList();

        var refunds = entity.Refunds
            .Select(r => new OrderRefund(
                r.Id.ToString(), r.OperationId, r.IdempotencyKey, Money.FromPiastres(r.AmountPiastres),
                OrderEnumMapping.ParsePaymentMethod(r.PaymentMethod), r.TreasuryMovementId, r.CreatedAtUtc,
                r.CreatedBy, r.AuthorizedBy, r.Reason, r.OriginalShiftId, r.OperationalShiftId))
            .ToList();

        ElectronicPayment? electronicPayment = entity.ElectronicPaymentMethod is null ? null : new ElectronicPayment(
            OrderEnumMapping.ParsePaymentMethod(entity.ElectronicPaymentMethod),
            entity.ElectronicPaymentProvider,
            Money.FromPiastres(entity.ElectronicPaymentAmountPiastres ?? 0),
            ParseElectronicPaymentStatus(entity.ElectronicPaymentStatus!));

        List<PaymentSplit>? splits = entity.SplitPaymentsJson is null ? null :
            System.Text.Json.JsonSerializer.Deserialize<List<WireSplit>>(entity.SplitPaymentsJson)!
                .Select(s => new PaymentSplit(OrderEnumMapping.ParsePaymentMethod(s.method), Money.FromPiastres(s.amountPiastres)))
                .ToList();

        return Order.Reconstitute(
            entity.Id, entity.OrderNumber, entity.SequenceNumber, entity.CashierId, entity.ShiftId,
            OrderEnumMapping.ParseOrderType(entity.OrderType), entity.CustomerName, entity.CustomerPhone,
            entity.DeliveryAddress, entity.DeliveryArea, Money.FromPiastres(entity.DeliveryFeePiastres),
            items, Money.FromPiastres(entity.SubtotalPiastres), Money.FromPiastres(entity.CompAmountPiastres),
            entity.CompReasonCode, entity.CompAuthorizedBy, entity.CompApprovalId, Money.FromPiastres(entity.TaxAmountPiastres),
            Money.FromPiastres(entity.TotalPiastres), OrderEnumMapping.ParsePaymentMethod(entity.PaymentMethod), splits,
            Money.FromPiastres(entity.AmountPaidPiastres), Money.FromPiastres(entity.ChangeDuePiastres),
            entity.CheckoutRequestId, entity.BusinessDayId, entity.CreatedAtUtc,
            ParseOrderStatus(entity.Status), entity.IsPaid, electronicPayment,
            Money.FromPiastres(entity.RefundAmountPiastres), refunds,
            entity.CancelledAtUtc, entity.CancelReason, entity.CancelledBy, Money.FromPiastres(entity.DiscountPiastres),
            Money.FromPiastres(entity.TaxableBasePiastres), entity.TaxRateBasisPoints, entity.TaxMode, entity.TaxRuleName);
    }

    public static OrderEntity ToEntity(Order order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        SequenceNumber = order.SequenceNumber,
        CashierId = order.CashierId,
        ShiftId = order.ShiftId,
        OrderType = order.OrderType.ToWireString(),
        CustomerName = order.CustomerName,
        CustomerPhone = order.CustomerPhone,
        DeliveryAddress = order.DeliveryAddress,
        DeliveryArea = order.DeliveryArea,
        DeliveryFeePiastres = order.DeliveryFee.Piastres,
        SubtotalPiastres = order.Subtotal.Piastres,
        DiscountPiastres = order.Discount.Piastres,
        CompAmountPiastres = order.CompAmount.Piastres,
        CompReasonCode = order.CompReasonCode,
        CompAuthorizedBy = order.CompAuthorizedBy,
        CompApprovalId = order.CompApprovalId,
        TaxAmountPiastres = order.TaxAmount.Piastres,
        TaxableBasePiastres = order.TaxableBase.Piastres,
        TaxRateBasisPoints = order.TaxRateBasisPoints,
        TaxMode = order.TaxMode,
        TaxRuleName = order.TaxRuleName,
        TotalPiastres = order.Total.Piastres,
        PaymentMethod = order.PaymentMethod.ToWireString(),
        SplitPaymentsJson = order.SplitPayments is null ? null :
            System.Text.Json.JsonSerializer.Serialize(order.SplitPayments.Select(s => new WireSplit(s.Method.ToWireString(), s.Amount.Piastres))),
        AmountPaidPiastres = order.AmountPaid.Piastres,
        ChangeDuePiastres = order.ChangeDue.Piastres,
        CheckoutRequestId = order.CheckoutRequestId,
        BusinessDayId = order.BusinessDayId,
        CreatedAtUtc = order.CreatedAtUtc,
        Status = order.Status.ToWireString(),
        IsPaid = order.IsPaid,
        ElectronicPaymentMethod = order.ElectronicPayment?.Method.ToWireString(),
        ElectronicPaymentProvider = order.ElectronicPayment?.Provider,
        ElectronicPaymentAmountPiastres = order.ElectronicPayment?.Amount.Piastres,
        ElectronicPaymentStatus = order.ElectronicPayment?.Status.ToWireString(),
        RefundAmountPiastres = order.RefundAmount.Piastres,
        CancelledAtUtc = order.CancelledAtUtc,
        CancelReason = order.CancelReason,
        CancelledBy = order.CancelledBy,
        Items = order.Items.Select(i => new OrderItemEntity
        {
            OrderId = order.Id,
            ProductId = i.ProductId,
            VariantId = i.VariantId,
            ProductName = i.ProductName,
            VariantName = i.VariantName,
            UnitPricePiastres = i.UnitPrice.Piastres,
            ListUnitPricePiastres = i.ListUnitPrice.Piastres,
            Quantity = i.Quantity,
            TotalPricePiastres = i.TotalPrice.Piastres,
            SendToGrill = i.SendToGrill,
            PriceRuleId = i.PriceRuleId,
            PriceRuleName = i.PriceRuleName,
            Notes = i.Notes,
            PreparationStation = i.PreparationStation,
        }).ToList(),
    };

    /// <summary>OrderId is deliberately not set here - the caller adds the result to the owning
    /// OrderEntity.Refunds collection, which lets EF Core's change tracker set the foreign key
    /// itself rather than this mapper guessing at an id it was never given.</summary>
    public static OrderRefundEntity ToEntity(OrderRefund refund) => new()
    {
        OperationId = refund.OperationId,
        IdempotencyKey = refund.IdempotencyKey,
        AmountPiastres = refund.Amount.Piastres,
        PaymentMethod = refund.PaymentMethod.ToWireString(),
        TreasuryMovementId = refund.TreasuryMovementId,
        CreatedAtUtc = refund.CreatedAtUtc,
        CreatedBy = refund.CreatedBy,
        AuthorizedBy = refund.AuthorizedBy,
        Reason = refund.Reason,
        OriginalShiftId = refund.OriginalShiftId,
        OperationalShiftId = refund.OperationalShiftId,
    };

    public static TreasuryMovementEntity ToEntity(TreasuryMovement movement) => new()
    {
        Id = movement.Id,
        IdempotencyKey = movement.IdempotencyKey,
        OperationId = movement.OperationId,
        Type = movement.Type,
        SourceAccountId = movement.SourceAccountId,
        DestinationAccountId = movement.DestinationAccountId,
        AmountPiastres = movement.Amount.Piastres,
        ReferenceType = movement.ReferenceType,
        ReferenceId = movement.ReferenceId,
        BusinessDayId = movement.BusinessDayId,
        ShiftId = movement.ShiftId,
        Reason = movement.Reason,
        CreatedBy = movement.CreatedBy,
        ApprovedBy = movement.ApprovedBy,
        CreatedAtUtc = movement.CreatedAtUtc,
        Status = movement.Status.ToString(),
    };

    private static OrderStatus ParseOrderStatus(string value) => value switch
    {
        "pending" => OrderStatus.Pending,
        "preparing" => OrderStatus.Preparing,
        "ready" => OrderStatus.Ready,
        "completed" => OrderStatus.Completed,
        "cancelled" => OrderStatus.Cancelled,
        "refunded" => OrderStatus.Refunded,
        _ => throw new ArgumentException($"حالة طلب غير معروفة: {value}"),
    };

    private static ElectronicPaymentStatus ParseElectronicPaymentStatus(string value) => value switch
    {
        "pending" => ElectronicPaymentStatus.Pending,
        "confirmed" => ElectronicPaymentStatus.Confirmed,
        "cancelled" => ElectronicPaymentStatus.Cancelled,
        _ => throw new ArgumentException($"حالة دفع إلكتروني غير معروفة: {value}"),
    };

    private sealed record WireSplit(string method, long amountPiastres);
}

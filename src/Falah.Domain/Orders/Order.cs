namespace Falah.Domain.Orders;

using Falah.Domain.Common;
using Falah.Domain.Financial;

/// <summary>The most important aggregate in the system - every field that feeds money or a
/// historical report is frozen at creation time (snapshot naming, snapshot pricing) so a later
/// menu/offer/driver edit can never retroactively change how a past sale reads. Built via
/// CheckoutRules' pure decision functions plus the sequence/id the Host allocates; this
/// constructor performs no I/O and does not itself post any Treasury movement - that orchestration
/// is an Application-layer concern.</summary>
public sealed class Order
{
    public required string Id { get; init; }
    public required string OrderNumber { get; init; }
    public required long SequenceNumber { get; init; }
    public required string CashierId { get; init; }
    public required string ShiftId { get; init; }
    public required OrderType OrderType { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? DeliveryAddress { get; init; }
    public string? DeliveryArea { get; init; }
    public Money DeliveryFee { get; init; } = Money.Zero;
    public required IReadOnlyList<OrderItem> Items { get; init; }
    public required Money Subtotal { get; init; }
    /// <summary>Manual discount applied to the taxable base at checkout time - a plain historical
    /// fact carried alongside the order (like CustomerName/DeliveryArea), never fed back into any
    /// Domain calculation here since it was already spent computing Subtotal/TaxAmount/Total via
    /// CheckoutRules.BuildPlan before this aggregate is ever created. Kept separate from CompAmount
    /// (PIN-authorized hospitality) and any live price rule's own discount - three distinct
    /// giveaway buckets a report must never merge (see Falah.Domain.Reports.SalesRules).</summary>
    public Money Discount { get; init; } = Money.Zero;
    public Money CompAmount { get; init; } = Money.Zero;
    public string? CompReasonCode { get; init; }
    public string? CompAuthorizedBy { get; init; }
    public string? CompApprovalId { get; init; }
    public Money TaxAmount { get; init; } = Money.Zero;
    /// <summary>The base tax was actually computed on at checkout time (subtotal-discount) - a
    /// real gap found during the legacy-parity audit: previously only the final TaxAmount was
    /// frozen, so a later Tax Report could not tell "gross" from "taxable" for a historical order
    /// without recomputing from today's rules (exactly what this snapshot exists to prevent).</summary>
    public Money TaxableBase { get; init; } = Money.Zero;
    /// <summary>Null when no tax rule was active at checkout - never recomputed from whatever rule
    /// happens to be active today (see TaxScreen.tsx's own comment on this exact failure mode).</summary>
    public int? TaxRateBasisPoints { get; init; }
    public string? TaxMode { get; init; }
    public string? TaxRuleName { get; init; }
    public required Money Total { get; init; }
    public required PaymentMethod PaymentMethod { get; init; }
    public IReadOnlyList<PaymentSplit>? SplitPayments { get; init; }
    public required Money AmountPaid { get; init; }
    public Money ChangeDue { get; init; } = Money.Zero;
    public required string CheckoutRequestId { get; init; }
    public string? BusinessDayId { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }

    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public bool IsPaid { get; private set; }
    public ElectronicPayment? ElectronicPayment { get; private set; }
    public Money RefundAmount { get; private set; } = Money.Zero;

    private readonly List<OrderRefund> _refunds = new();
    public IReadOnlyList<OrderRefund> Refunds => _refunds;

    public static Order Create(CheckoutRules.CheckoutPlan plan, string id, string orderNumber, long sequenceNumber,
        string cashierId, string shiftId, IReadOnlyList<OrderItem> items, string checkoutRequestId,
        string? businessDayId, DateTimeOffset createdAtUtc, string? customerName = null, string? customerPhone = null,
        string? deliveryAddress = null, string? deliveryArea = null, Money discount = default,
        string? compReasonCode = null, string? compAuthorizedBy = null, string? compApprovalId = null,
        int? taxRateBasisPoints = null, string? taxMode = null, string? taxRuleName = null)
    {
        var order = new Order
        {
            Id = id,
            OrderNumber = orderNumber,
            SequenceNumber = sequenceNumber,
            CashierId = cashierId,
            ShiftId = shiftId,
            OrderType = plan.OrderType,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            DeliveryAddress = deliveryAddress,
            DeliveryArea = deliveryArea,
            DeliveryFee = plan.DeliveryFee,
            Items = items,
            Subtotal = plan.Subtotal,
            Discount = discount,
            CompAmount = plan.CompAmount,
            CompReasonCode = compReasonCode,
            CompAuthorizedBy = compAuthorizedBy,
            CompApprovalId = compApprovalId,
            TaxAmount = plan.Tax.Tax,
            TaxableBase = plan.Tax.Taxable,
            TaxRateBasisPoints = taxRateBasisPoints,
            TaxMode = taxMode,
            TaxRuleName = taxRuleName,
            Total = plan.PayableTotal,
            PaymentMethod = plan.PaymentMethod,
            SplitPayments = plan.SplitPayments,
            AmountPaid = plan.AmountPaid,
            ChangeDue = plan.ChangeDue,
            CheckoutRequestId = checkoutRequestId,
            BusinessDayId = businessDayId,
            CreatedAtUtc = createdAtUtc,
        };
        order.Status = items.Any(i => i.SendToGrill) ? OrderStatus.Preparing : OrderStatus.Completed;
        order.IsPaid = plan.IsPaid;
        order.ElectronicPayment = plan.ElectronicLeg;
        return order;
    }

    /// <summary>
    /// Rehydrates an already-persisted order into a live aggregate so a later operation (refund,
    /// payment confirmation) runs through the exact same invariants a freshly-created order would.
    /// A plain reconstruction from primitive values - it performs no I/O and knows nothing about
    /// how or where the caller actually stored them, so no persistence concern leaks into Domain.
    /// </summary>
    public static Order Reconstitute(
        string id, string orderNumber, long sequenceNumber, string cashierId, string shiftId, OrderType orderType,
        string? customerName, string? customerPhone, string? deliveryAddress, string? deliveryArea, Money deliveryFee,
        IReadOnlyList<OrderItem> items, Money subtotal, Money compAmount, string? compReasonCode, string? compAuthorizedBy,
        string? compApprovalId, Money taxAmount, Money total, PaymentMethod paymentMethod, IReadOnlyList<PaymentSplit>? splitPayments,
        Money amountPaid, Money changeDue, string checkoutRequestId, string? businessDayId, DateTimeOffset createdAtUtc,
        OrderStatus status, bool isPaid, ElectronicPayment? electronicPayment, Money refundAmount, IEnumerable<OrderRefund> refunds,
        DateTimeOffset? cancelledAtUtc = null, string? cancelReason = null, string? cancelledBy = null, Money discount = default,
        Money taxableBase = default, int? taxRateBasisPoints = null, string? taxMode = null, string? taxRuleName = null)
    {
        var order = new Order
        {
            Id = id,
            OrderNumber = orderNumber,
            SequenceNumber = sequenceNumber,
            CashierId = cashierId,
            ShiftId = shiftId,
            OrderType = orderType,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            DeliveryAddress = deliveryAddress,
            DeliveryArea = deliveryArea,
            DeliveryFee = deliveryFee,
            Items = items,
            Subtotal = subtotal,
            Discount = discount,
            CompAmount = compAmount,
            CompReasonCode = compReasonCode,
            CompAuthorizedBy = compAuthorizedBy,
            CompApprovalId = compApprovalId,
            TaxAmount = taxAmount,
            TaxableBase = taxableBase,
            TaxRateBasisPoints = taxRateBasisPoints,
            TaxMode = taxMode,
            TaxRuleName = taxRuleName,
            Total = total,
            PaymentMethod = paymentMethod,
            SplitPayments = splitPayments,
            AmountPaid = amountPaid,
            ChangeDue = changeDue,
            CheckoutRequestId = checkoutRequestId,
            BusinessDayId = businessDayId,
            CreatedAtUtc = createdAtUtc,
        };
        order.Status = status;
        order.IsPaid = isPaid;
        order.ElectronicPayment = electronicPayment;
        order.RefundAmount = refundAmount;
        order._refunds.AddRange(refunds);
        order.CancelledAtUtc = cancelledAtUtc;
        order.CancelReason = cancelReason;
        order.CancelledBy = cancelledBy;
        return order;
    }

    /// <summary>Confirms a still-pending electronic (InstaPay/wallet) claim. This is the moment the
    /// money is treated as real - the Application layer posts the deferred Treasury movement and
    /// activates kitchen/delivery operations in the same step, but that orchestration is not this
    /// aggregate's job.</summary>
    public void ConfirmElectronicPayment()
    {
        if (ElectronicPayment is not { Status: ElectronicPaymentStatus.Pending } leg)
            throw new DomainException("لا يوجد تحويل إلكتروني معلّق لتأكيده.");
        ElectronicPayment = leg with { Status = ElectronicPaymentStatus.Confirmed };
        IsPaid = true;
    }

    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancelReason { get; private set; }
    public string? CancelledBy { get; private set; }

    /// <summary>
    /// Cancels a still-pending electronic (InstaPay/wallet) claim that never arrived. Ported
    /// verbatim from cancelPendingElectronicPayment in the reference app's lib/store.tsx (found
    /// during the legacy audit - this was a genuine reading gap, not an operational ambiguity, and
    /// is now resolved from evidence): the WHOLE order is cancelled (not returned to the cart, not
    /// partially reversed), no Treasury movement exists to reverse (checkout deferred it entirely),
    /// and - notably - inventory already deducted at checkout is deliberately NOT reversed either;
    /// the reference app never touches stock on this path. Idempotent: cancelling an
    /// already-cancelled order is a no-op, matching the original exactly.
    /// </summary>
    public void CancelElectronicPayment(string reason, string cancelledBy, DateTimeOffset nowUtc)
    {
        if (Status == OrderStatus.Cancelled) return;
        if (ElectronicPayment is not { } leg) throw new DomainException("لا يوجد تحصيل إلكتروني على هذا الطلب.");
        if (leg.Status == ElectronicPaymentStatus.Confirmed)
            throw new DomainException("تم تأكيد التحصيل بالفعل. استخدم الاسترجاع المالي بدلًا من الإلغاء.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("سبب الإلغاء مطلوب.");

        Status = OrderStatus.Cancelled;
        IsPaid = false;
        ElectronicPayment = leg with { Status = ElectronicPaymentStatus.Cancelled };
        CancelledAtUtc = nowUtc;
        CancelReason = reason.Trim();
        CancelledBy = cancelledBy;
    }

    /// <summary>Appends one refund and, when it exactly exhausts the order's remaining refundable
    /// value, marks the order fully Refunded. The order and multi-payment restrictions are enforced
    /// here too (not only by the caller) so this aggregate can never be pushed into an invalid
    /// state regardless of what the Application layer forgets to check.</summary>
    public void ApplyRefund(OrderRefund refund)
    {
        if (Status == OrderStatus.Cancelled) throw new DomainException("لا يمكن استرجاع طلب ملغي.");
        if (PaymentMethod == PaymentMethod.Multi) throw new DomainException("مرتجع الدفع المتعدد غير مدعوم في المسار المالي الحالي.");

        var refundableBefore = Total - RefundAmount;
        if (refund.Amount.IsZero || refund.Amount.IsNegative || refund.Amount > refundableBefore)
            throw new DomainException("مبلغ المرتجع غير صالح.");

        _refunds.Add(refund);
        RefundAmount += refund.Amount;
        if (RefundAmount == Total) Status = OrderStatus.Refunded;
    }
}

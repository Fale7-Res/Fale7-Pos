namespace Falah.Domain.Orders;

using Falah.Domain.Common;
using Falah.Domain.Financial;

/// <summary>
/// Pure checkout decision rules ported from the `checkout` callback in the current web app's
/// lib/store.tsx. This intentionally stops short of the full checkout USE CASE (which also opens
/// a financial journal entry, posts Treasury movements, deducts inventory, and updates shift
/// totals across several aggregates) - that orchestration belongs to an Application-layer command
/// handler once repositories exist. What lives here is everything that can be wrong on its own:
/// how much is actually payable, whether the order defers its money, and whether it counts paid.
/// </summary>
public static class CheckoutRules
{
    public readonly record struct CheckoutPlan(
        OrderType OrderType,
        PaymentMethod PaymentMethod,
        Money Subtotal,
        Money DeliveryFee,
        Money CompAmount,
        TaxCalculation Tax,
        Money PayableTotal,
        Money AmountPaid,
        Money ChangeDue,
        IReadOnlyList<PaymentSplit>? SplitPayments,
        ElectronicPayment? ElectronicLeg,
        bool CashOnDelivery,
        bool DefersAllMoney,
        bool IsPaid,
        /// <summary>UI-0025 (owner decision 2026-11-22): a pickup registered and sent to the kitchen
        /// unpaid; the customer pays on collection (BuildCollectionPlan). Until then it is no sale.</summary>
        bool PayOnCollection = false);

    /// <summary>How an awaiting pickup is paid when the customer collects it.</summary>
    public readonly record struct CollectionPlan(
        PaymentMethod PaymentMethod, Money AmountPaid, Money ChangeDue, IReadOnlyList<PaymentSplit>? SplitPayments,
        ElectronicPayment? ElectronicLeg, bool DefersAllMoney, bool IsPaid);

    /// <summary>UI-0025: the payment of a pickup at collection, by any method, under the same rules as a
    /// checkout - enough money, a split that adds up, one transfer at most, a transfer only a claim until
    /// confirmed. The order's total was fixed when it was registered.</summary>
    public static CollectionPlan BuildCollectionPlan(Money payableTotal, PaymentMethod paymentMethod, Money amountPaid,
        IReadOnlyList<PaymentSplit>? splitPayments, string? walletProvider)
    {
        if (paymentMethod == PaymentMethod.Multi && (splitPayments is null || splitPayments.Count == 0))
            throw new DomainException("يجب تحديد تفاصيل الدفع المتعدد.");
        var electronicLeg = SelectElectronicLeg(paymentMethod, payableTotal, splitPayments, walletProvider);
        if (amountPaid < payableTotal) throw new DomainException("المبلغ المدفوع أقل من إجمالي الطلب.");
        if (paymentMethod == PaymentMethod.Multi && splitPayments is not null)
            FinancialRules.AssertSplitPaymentAllocation(splitPayments, payableTotal);
        var changeDue = amountPaid - payableTotal;
        return new CollectionPlan(paymentMethod, amountPaid, changeDue.IsNegative ? Money.Zero : changeDue,
            paymentMethod == PaymentMethod.Multi ? splitPayments : null, electronicLeg,
            DefersAllMoney(payableTotal, false, electronicLeg, paymentMethod), ComputeIsPaid(false, electronicLeg));
    }

    public static Money ComputeSubtotal(IReadOnlyList<OrderItem> items) =>
        Money.FromPiastres(items.Sum(i => i.TotalPrice.Piastres));

    public static Money ComputePayableTotal(Money cartTotal, Money deliveryFee, Money compAmount)
    {
        var result = cartTotal + deliveryFee - compAmount;
        return result.IsNegative ? Money.Zero : result;
    }

    /// <summary>A fully comped delivery has nothing for the driver to collect, so it is not a
    /// cash-on-delivery order at all - treating it as one would put a phantom 0 ج custody on the
    /// driver.</summary>
    public static bool IsCashOnDelivery(OrderType orderType, PaymentMethod paymentMethod, Money payableTotal) =>
        orderType == OrderType.Delivery && paymentMethod == PaymentMethod.Cash && payableTotal.IsPositive;

    /// <summary>An InstaPay/wallet leg - standalone or the one electronic leg inside a split - is
    /// only a claim until confirmed, so it is deferred exactly like cash-on-delivery: no Treasury
    /// movement now, money posts when someone confirms receipt.</summary>
    public static ElectronicPayment? SelectElectronicLeg(
        PaymentMethod paymentMethod, Money payableTotal, IReadOnlyList<PaymentSplit>? splitPayments, string? walletProvider)
    {
        // A fully comped order collects nothing: there is no transfer to wait for, and a 0 ج pending
        // claim would hold the kitchen ticket and the day close for a payment that cannot arrive.
        if (paymentMethod is PaymentMethod.InstaPay or PaymentMethod.Wallet && !payableTotal.IsPositive) return null;
        if (paymentMethod is PaymentMethod.InstaPay or PaymentMethod.Wallet)
            return new ElectronicPayment(paymentMethod, paymentMethod == PaymentMethod.Wallet ? walletProvider : null, payableTotal, ElectronicPaymentStatus.Pending);

        if (paymentMethod == PaymentMethod.Multi && splitPayments is not null)
        {
            if (splitPayments.Count(p => p.Method is PaymentMethod.InstaPay or PaymentMethod.Wallet) > 1)
                throw new DomainException("الدفع المقسم يدعم تحويلًا إلكترونيًا واحدًا فقط لكل طلب.");
            var hasElectronic = splitPayments.Any(p => p.Method is PaymentMethod.InstaPay or PaymentMethod.Wallet);
            if (!hasElectronic) return null;
            var part = splitPayments.First(p => p.Method is PaymentMethod.InstaPay or PaymentMethod.Wallet);
            return new ElectronicPayment(part.Method, part.Method == PaymentMethod.Wallet ? walletProvider : null, part.Amount, ElectronicPaymentStatus.Pending);
        }
        return null;
    }

    /// <summary>A fully comped order collects nothing, so there is no money to post - inventing a
    /// zero-value Treasury movement for it would be a lie in the ledger.</summary>
    public static bool DefersAllMoney(Money payableTotal, bool cashOnDelivery, ElectronicPayment? electronicLeg, PaymentMethod paymentMethod)
    {
        var collectsNothing = !payableTotal.IsPositive;
        return collectsNothing || cashOnDelivery || (electronicLeg is not null && paymentMethod != PaymentMethod.Multi);
    }

    /// <summary>Not "paid" while any leg is an unverified electronic claim or nothing has actually
    /// changed hands yet (cash on delivery) - keeps the money out of expected drawer cash and out
    /// of collected revenue until it is actually confirmed.</summary>
    public static bool ComputeIsPaid(bool cashOnDelivery, ElectronicPayment? electronicLeg) =>
        !cashOnDelivery && electronicLeg is null;

    /// <summary>Builds the full checkout plan and throws on any of the checkout-time validation
    /// rules the original app enforces (multi-payment split presence, sufficient payment, exact
    /// split allocation). Cart emptiness, shift-open, and permission checks are the caller's
    /// responsibility - they depend on state this pure function does not have.</summary>
    public static CheckoutPlan BuildPlan(
        OrderType orderType, PaymentMethod paymentMethod, IReadOnlyList<OrderItem> items,
        Money discount, TaxRule? taxRule, Money deliveryFee, Money compAmount, Money amountPaid,
        IReadOnlyList<PaymentSplit>? splitPayments, string? walletProvider, bool payOnCollection = false)
    {
        if (payOnCollection && orderType != OrderType.Pickup)
            throw new DomainException("الدفع عند الاستلام متاح لطلبات الاستلام بس.");
        // Delivery audit, owner decision 2026-11-24 (D3): a delivery is either all cash with the driver or all paid.
        // A split's cash leg used to post into the till's drawer at the sale, while the driver is the one who collects it.
        if (orderType == OrderType.Delivery && paymentMethod == PaymentMethod.Multi)
            throw new DomainException("الدفع المقسّم مش متاح مع طلبات التوصيل: يا الطلب كله كاش مع السائق، يا مدفوع كله ببطاقة أو تحويل.");
        if (discount.IsNegative || deliveryFee.IsNegative || compAmount.IsNegative)
            throw new DomainException("الخصم ورسوم التوصيل والمجانيات يجب ألا تكون سالبة.");

        if (paymentMethod == PaymentMethod.Multi && (splitPayments is null || splitPayments.Count == 0))
            throw new DomainException("يجب تحديد تفاصيل الدفع المتعدد.");

        var subtotal = ComputeSubtotal(items);
        // A discount above the items' value used to be accepted and stored whole while the taxable base
        // clamped to 0, so the discount reports showed money that was never given away.
        if (discount > subtotal)
            throw new DomainException("الخصم لا يمكن أن يتجاوز قيمة الأصناف.");
        var taxableBase = subtotal - discount;
        if (taxableBase.IsNegative) taxableBase = Money.Zero;
        var tax = TaxEngine.Calculate(taxableBase, taxRule);
        var cartTotal = tax.Final;
        var payableTotal = ComputePayableTotal(cartTotal, deliveryFee, compAmount);

        // UI-0025: nothing is taken now. The method is a placeholder until the customer pays at collection.
        if (payOnCollection && payableTotal.IsPositive)
            return new CheckoutPlan(orderType, PaymentMethod.Cash, subtotal, deliveryFee, compAmount, tax, payableTotal,
                Money.Zero, Money.Zero, null, null, CashOnDelivery: false, DefersAllMoney: true, IsPaid: false, PayOnCollection: true);

        var cashOnDelivery = IsCashOnDelivery(orderType, paymentMethod, payableTotal);
        var electronicLeg = SelectElectronicLeg(paymentMethod, payableTotal, splitPayments, walletProvider);

        if (!cashOnDelivery && amountPaid < payableTotal)
            throw new DomainException("المبلغ المدفوع أقل من إجمالي الطلب.");

        if (paymentMethod == PaymentMethod.Multi && splitPayments is not null)
            FinancialRules.AssertSplitPaymentAllocation(splitPayments, payableTotal);

        var defers = DefersAllMoney(payableTotal, cashOnDelivery, electronicLeg, paymentMethod);
        var isPaid = ComputeIsPaid(cashOnDelivery, electronicLeg);
        var changeDue = amountPaid - payableTotal;
        if (changeDue.IsNegative) changeDue = Money.Zero;

        return new CheckoutPlan(orderType, paymentMethod, subtotal, deliveryFee, compAmount, tax, payableTotal,
            amountPaid, changeDue, splitPayments, electronicLeg, cashOnDelivery, defers, isPaid);
    }

    /// <summary>How far ahead a pickup may be booked. Not a restaurant policy - no opening hours,
    /// no lead time, no cut-off are encoded anywhere in this system and none are invented here. It
    /// is a typo guard: a year entered as 2027 instead of 2026 should be refused at the till rather
    /// than printed on a ticket nobody will look at for twelve months.</summary>
    public static readonly TimeSpan MaxPickupLeadTime = TimeSpan.FromDays(14);

    /// <summary>A few minutes of slack, so a cashier who picks "now" and then finishes ringing up
    /// the order is not refused for a time that went stale while they worked.</summary>
    public static readonly TimeSpan PickupGracePeriod = TimeSpan.FromMinutes(5);

    /// <summary>Validates an optional requested pickup time. Optional is the normal case: most
    /// pickups are "on the way", and a time nobody asked for must not be invented for them.</summary>
    public static void AssertRequestedPickupTime(OrderType orderType, DateTimeOffset? requestedAtUtc, DateTimeOffset nowUtc)
    {
        if (requestedAtUtc is not { } requested) return;

        if (orderType != OrderType.Pickup)
            throw new DomainException("موعد الاستلام يُسجَّل لطلبات الاستلام من المطعم فقط.");

        if (requested < nowUtc - PickupGracePeriod)
            throw new DomainException("موعد الاستلام في الماضي - اختر وقتًا قادمًا.");

        if (requested > nowUtc + MaxPickupLeadTime)
            throw new DomainException($"موعد الاستلام بعيد أكثر من اللازم - الحد الأقصى {MaxPickupLeadTime.TotalDays:0} يومًا.");
    }
}
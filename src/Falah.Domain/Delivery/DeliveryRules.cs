namespace Falah.Domain.Delivery;

using Falah.Domain.Common;
using Falah.Domain.Financial;

/// <summary>Pure delivery/custody/settlement rules ported 1:1 from lib/delivery-domain.mjs.</summary>
public static class DeliveryRules
{
    /// <summary>Cash payment method: the driver is responsible for the whole amount. Any other
    /// method: responsible only if it has not already settled electronically.</summary>
    public static Money DeliveryCashResponsibility(PaymentMethod paymentMethod, bool paymentAlreadySettled, Money amount) =>
        paymentMethod == PaymentMethod.Cash ? amount : (paymentAlreadySettled ? Money.Zero : amount);

    /// <summary>Returns null (no custody row needed) when there is nothing to collect - a genuinely
    /// prepaid or fully-comped delivery.</summary>
    public static DriverCustody? CreateCashCustody(string custodyId, string tripId, DeliveryFacts delivery, string driverId, DateTimeOffset createdAtUtc)
    {
        var expected = DeliveryCashResponsibility(delivery.PaymentMethod, delivery.PaymentAlreadySettled, delivery.AmountToCollect);
        if (expected.IsZero) return null;
        return new DriverCustody(custodyId, tripId, driverId, delivery.OrderId, delivery.Id, expected, Money.Zero, Money.Zero, Money.Zero, CustodyStatus.Open, createdAtUtc);
    }

    /// <summary>Reassignment transfers custody by re-labeling the SAME custody record's driver - it
    /// never creates a second custody row, so the old driver's total drops by exactly this amount
    /// and the new driver's total rises by exactly this amount, with no possibility of double
    /// counting. A settled custody can never be reassigned.</summary>
    public static IReadOnlyList<DriverCustody> ReassignCustody(IEnumerable<DriverCustody> custodies, string deliveryId, string newDriverId) =>
        custodies.Select(c => c.DeliveryId == deliveryId ? c.ReassignTo(newDriverId) : c).ToList();

    public readonly record struct TripSettlementResult(Money Expected, Money Actual, Money Difference);

    public static TripSettlementResult CalculateTripSettlement(IEnumerable<DriverCustody> custodies, Money actual)
    {
        if (actual.IsNegative) throw new DomainException("النقدية الفعلية يجب أن تكون عدد قروش صحيحًا وغير سالب.");
        var unsettled = custodies.Where(c => c.Status != CustodyStatus.Settled).ToList();
        var expected = Money.FromPiastres(unsettled.Sum(c => c.Collected.Piastres));
        return new TripSettlementResult(expected, actual, actual - expected);
    }

    public readonly record struct DriverSettlementScope(
        IReadOnlyList<DeliveryFacts> ActiveDeliveries,
        IReadOnlyList<DeliveryFacts> ResolvedDeliveries,
        IReadOnlyList<DriverCustody> UnsettledCustodies,
        IReadOnlyList<string> OrderIds,
        IReadOnlyList<string> TripIds,
        Money GrossOrderValue,
        Money PrepaidValue);

    /// <summary>A driver settlement closes the operational work as well as the cash custody.
    /// Paid-in-advance orders have no cash-custody row, but must still be included and closed so
    /// they cannot leave an auto-created trip permanently open and block the business day.</summary>
    public static DriverSettlementScope BuildDriverSettlementScope(
        string driverId, IEnumerable<DeliveryFacts> deliveries, IEnumerable<DriverCustody> custodies)
    {
        var driverDeliveries = deliveries.Where(d => d.DriverId == driverId
            && d.Status is not (DeliveryStatus.Settled or DeliveryStatus.Cancelled)).ToList();
        var activeDeliveries = driverDeliveries.Where(d =>
            d.Status is DeliveryStatus.Assigned or DeliveryStatus.HandedToDriver or DeliveryStatus.OutForDelivery).ToList();
        var resolvedDeliveries = driverDeliveries.Where(d =>
            d.Status is DeliveryStatus.Delivered or DeliveryStatus.Failed or DeliveryStatus.Returned).ToList();
        var unsettledCustodies = custodies.Where(c => c.DriverId == driverId && c.Status != CustodyStatus.Settled).ToList();
        var successful = resolvedDeliveries.Where(d => d.Status == DeliveryStatus.Delivered).ToList();

        var grossOrderValue = Money.FromPiastres(successful.Sum(d => d.OrderTotal.Piastres));
        var prepaidValue = Money.FromPiastres(successful
            .Where(d => DeliveryCashResponsibility(d.PaymentMethod, d.PaymentAlreadySettled, d.AmountToCollect).IsZero)
            .Sum(d => d.OrderTotal.Piastres));

        var orderIds = resolvedDeliveries.Select(d => d.OrderId).ToList();
        var tripIds = resolvedDeliveries.Select(d => d.TripId)
            .Concat(unsettledCustodies.Select(c => c.TripId))
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList()!;

        return new DriverSettlementScope(activeDeliveries, resolvedDeliveries, unsettledCustodies, orderIds, tripIds!, grossOrderValue, prepaidValue);
    }

    /// <summary>A collection amount that differs from what was expected can never be silently
    /// auto-reconciled - it always needs a written reason.</summary>
    public static void AssertCollectionAmount(Money expected, Money collected, string? reason)
    {
        if (collected.IsNegative) throw new DomainException("المبلغ المحصل غير صالح.");
        if (expected != collected && string.IsNullOrWhiteSpace(reason))
            throw new DomainException("فرق التحصيل يتطلب سببًا واضحًا ولا يمكن تسويته تلقائيًا.");
    }

    public static void AssertShiftHasNoUnsettledCustody(IEnumerable<DriverCustody> custodies, IEnumerable<DeliveryFacts> deliveries, string shiftId)
    {
        var deliveryIds = deliveries.Where(d => d.ShiftId == shiftId).Select(d => d.Id).ToHashSet();
        var blocked = custodies.Where(c => c.Status != CustodyStatus.Settled && deliveryIds.Contains(c.DeliveryId)).ToList();
        if (blocked.Count > 0)
            throw new DomainException($"لا يمكن إغلاق الوردية: توجد عهدة سائق غير مسوّاة لعدد {blocked.Count} طلب.");
    }

    /// <summary>Settlement overwrites a delivery's status to Settled regardless of its original
    /// outcome - this recovers what actually happened from the timestamps settlement never
    /// touches. ReturnedAt takes priority (a failed order can later be marked returned), then
    /// DeliveredAt, then FailedAt.</summary>
    public static DeliveryStatus? FinalOutcome(DeliveryFacts delivery)
    {
        if (delivery.ReturnedAtUtc is not null) return DeliveryStatus.Returned;
        if (delivery.DeliveredAtUtc is not null) return DeliveryStatus.Delivered;
        if (delivery.FailedAtUtc is not null) return DeliveryStatus.Failed;
        return null;
    }

    /// <summary>A cashier who filled four of five required delivery fields must be told the ONE
    /// they missed, checked in the same order the fields appear on the POS form.</summary>
    public static string? FirstMissingDeliveryField(
        string? customerPhone, string? customerName, string? deliveryServiceId, string? deliveryArea, string? deliveryStreet)
    {
        if (string.IsNullOrWhiteSpace(customerPhone)) return "أدخل رقم هاتف العميل قبل المتابعة.";
        if (string.IsNullOrWhiteSpace(customerName)) return "أدخل اسم العميل قبل المتابعة.";
        if (string.IsNullOrEmpty(deliveryServiceId)) return "اختر خدمة التوصيل قبل المتابعة.";
        if (string.IsNullOrWhiteSpace(deliveryArea)) return "أدخل المنطقة قبل المتابعة.";
        if (string.IsNullOrWhiteSpace(deliveryStreet)) return "أدخل الشارع قبل المتابعة.";
        return null;
    }

    public static bool EmployeeDriverEligible(Employee? employee) => employee is { IsActive: true, CanDeliver: true };

    /// <summary>The one gate every dispatch action (assign, reassign-in, explicit reactivate) must
    /// pass. A driver with no linked employee is judged on its own Active flag alone; one linked to
    /// an employee additionally requires that employee to still be active and flagged
    /// CanDeliver.</summary>
    public static void AssertDriverAssignable(Driver? driver, Employee? employee)
    {
        if (driver is null) throw new DomainException("السائق غير موجود.");
        if (!driver.Active) throw new DomainException($"السائق {driver.Name} معطّل ولا يمكن تعيين طلبات له.");
        if (driver.EmployeeId is not null && !EmployeeDriverEligible(employee))
            throw new DomainException($"الموظف المرتبط بالسائق {driver.Name} غير نشط أو غير مؤهل للتوصيل حاليًا.");
    }

    public enum DeliveryPaymentState { Collect, Comp, Prepaid }

    /// <summary>The three, and only three, ways a delivery order's money can end at the driver's
    /// hands: real cash still to be collected, nothing because it was already paid electronically,
    /// or nothing because management gave the value away as a comp.</summary>
    public static DeliveryPaymentState ResolvePaymentState(Money amountToCollect, Money compAmount)
    {
        if (amountToCollect.IsPositive) return DeliveryPaymentState.Collect;
        if (compAmount.IsPositive) return DeliveryPaymentState.Comp;
        return DeliveryPaymentState.Prepaid;
    }
}

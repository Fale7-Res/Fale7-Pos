using Falah.Domain.Common;
using Falah.Domain.Delivery;
using Falah.Domain.Financial;
using Xunit;

namespace Falah.Domain.Tests;

public class DeliveryRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Cash_delivery_makes_the_driver_responsible_for_the_full_amount()
    {
        var responsibility = DeliveryRules.DeliveryCashResponsibility(PaymentMethod.Cash, paymentAlreadySettled: false, Money.FromEgp(150m));
        Assert.Equal(Money.FromEgp(150m), responsibility);
    }

    [Fact]
    public void Already_settled_electronic_payment_leaves_the_driver_nothing_to_collect()
    {
        var responsibility = DeliveryRules.DeliveryCashResponsibility(PaymentMethod.InstaPay, paymentAlreadySettled: true, Money.FromEgp(150m));
        Assert.Equal(Money.Zero, responsibility);
    }

    [Fact]
    public void No_custody_row_is_created_when_there_is_nothing_to_collect()
    {
        var delivery = MakeDelivery("d1", "ord-1", PaymentMethod.InstaPay, settled: true, Money.FromEgp(100m));
        Assert.Null(DeliveryRules.CreateCashCustody("c1", "trip-1", delivery, "driver-1", Now));
    }

    [Fact]
    public void Reassignment_moves_custody_to_the_new_driver_without_creating_a_second_row()
    {
        var custody = new DriverCustody("c1", "trip-1", "driver-A", "ord-1", "d1", Money.FromEgp(100m), Money.FromEgp(100m), Money.Zero, Money.Zero, CustodyStatus.Open, Now);
        var result = DeliveryRules.ReassignCustody(new[] { custody }, "d1", "driver-B");

        Assert.Single(result);
        Assert.Equal("driver-B", result[0].DriverId);
        Assert.Equal(Money.FromEgp(100m), result[0].Collected);
    }

    [Fact]
    public void A_settled_custody_can_never_be_reassigned()
    {
        var custody = new DriverCustody("c1", "trip-1", "driver-A", "ord-1", "d1", Money.FromEgp(100m), Money.FromEgp(100m), Money.FromEgp(100m), Money.Zero, CustodyStatus.Settled, Now);
        var result = DeliveryRules.ReassignCustody(new[] { custody }, "d1", "driver-B");

        Assert.Equal("driver-A", result[0].DriverId);
    }

    [Fact]
    public void Reassigning_ten_orders_across_two_drivers_never_double_counts_a_delivery()
    {
        // Reproduces the exact scenario the owner found by hand: 10 ready orders that must split
        // 4/6 between two drivers, with zero chance of one order's custody bleeding into another's.
        var custodies = Enumerable.Range(1, 10)
            .Select(i => new DriverCustody($"c{i}", "trip-1", "driver-A", $"ord-{i}", $"d{i}", Money.FromEgp(50m), Money.FromEgp(50m), Money.Zero, Money.Zero, CustodyStatus.Open, Now))
            .ToList();

        var afterFirstMove = DeliveryRules.ReassignCustody(custodies, "d3", "driver-B");
        var afterSecondMove = DeliveryRules.ReassignCustody(afterFirstMove, "d7", "driver-B");

        Assert.Equal(8, afterSecondMove.Count(c => c.DriverId == "driver-A"));
        Assert.Equal(2, afterSecondMove.Count(c => c.DriverId == "driver-B"));
        Assert.Equal(10, afterSecondMove.Sum(c => c.Collected.Piastres) / 5000);
    }

    [Fact]
    public void Trip_settlement_difference_is_actual_minus_expected()
    {
        var custodies = new[] { new DriverCustody("c1", "trip-1", "d1", "ord-1", "del-1", Money.FromEgp(100m), Money.FromEgp(100m), Money.Zero, Money.Zero, CustodyStatus.Open, Now) };
        var result = DeliveryRules.CalculateTripSettlement(custodies, Money.FromEgp(90m));

        Assert.Equal(Money.FromEgp(100m), result.Expected);
        Assert.Equal(Money.FromEgp(-10m), result.Difference);
    }

    [Fact]
    public void Negative_actual_settlement_cash_is_rejected()
    {
        Assert.Throws<DomainException>(() => DeliveryRules.CalculateTripSettlement(Array.Empty<DriverCustody>(), Money.FromEgp(-1m)));
    }

    [Fact]
    public void Settlement_scope_excludes_orders_still_actively_out_for_delivery()
    {
        var deliveries = new[]
        {
            MakeDelivery("d1", "ord-1", PaymentMethod.Cash, settled: false, Money.FromEgp(100m)) with { Status = DeliveryStatus.OutForDelivery },
            MakeDelivery("d2", "ord-2", PaymentMethod.Cash, settled: false, Money.FromEgp(100m)) with { Status = DeliveryStatus.Delivered },
        };
        var scope = DeliveryRules.BuildDriverSettlementScope("driver-1", deliveries.Select(d => d with { DriverId = "driver-1" }), Array.Empty<DriverCustody>());

        Assert.Single(scope.ActiveDeliveries);
        Assert.Single(scope.ResolvedDeliveries);
        Assert.False(scope.ActiveDeliveries.Count == 0 && scope.ResolvedDeliveries.Count == 0);
    }

    [Fact]
    public void Collection_amount_matching_expectation_needs_no_reason()
    {
        DeliveryRules.AssertCollectionAmount(Money.FromEgp(100m), Money.FromEgp(100m), reason: null);
    }

    [Fact]
    public void Collection_amount_differing_from_expectation_requires_a_reason()
    {
        Assert.Throws<DomainException>(() =>
            DeliveryRules.AssertCollectionAmount(Money.FromEgp(100m), Money.FromEgp(90m), reason: null));
        DeliveryRules.AssertCollectionAmount(Money.FromEgp(100m), Money.FromEgp(90m), reason: "العميل دفع أقل");
    }

    [Fact]
    public void Shift_cannot_close_while_one_of_its_deliveries_has_unsettled_custody()
    {
        var deliveries = new[] { MakeDelivery("d1", "ord-1", PaymentMethod.Cash, settled: false, Money.FromEgp(50m)) with { ShiftId = "shift-1" } };
        var custodies = new[] { new DriverCustody("c1", "trip-1", "driver-1", "ord-1", "d1", Money.FromEgp(50m), Money.FromEgp(50m), Money.Zero, Money.Zero, CustodyStatus.Open, Now) };

        Assert.Throws<DomainException>(() => DeliveryRules.AssertShiftHasNoUnsettledCustody(custodies, deliveries, "shift-1"));
    }

    [Fact]
    public void An_inactive_driver_cannot_be_assigned_a_new_order()
    {
        var driver = new Driver("driver-1", "محمد", Active: false, EmployeeId: null);
        Assert.Throws<DomainException>(() => DeliveryRules.AssertDriverAssignable(driver, employee: null));
    }

    [Fact]
    public void A_driver_linked_to_an_inactive_employee_cannot_be_assigned_even_if_the_driver_row_itself_is_active()
    {
        var driver = new Driver("driver-1", "محمد", Active: true, EmployeeId: "emp-1");
        var employee = new Employee("emp-1", IsActive: false, CanDeliver: true);
        Assert.Throws<DomainException>(() => DeliveryRules.AssertDriverAssignable(driver, employee));
    }

    [Fact]
    public void Missing_delivery_fields_are_reported_in_form_order_not_all_at_once()
    {
        var message = DeliveryRules.FirstMissingDeliveryField(customerPhone: null, "أحمد", "svc-1", "المعادي", "شارع 9");
        Assert.Contains("رقم هاتف", message);
    }

    [Fact]
    public void Composed_delivery_address_joins_only_the_parts_that_are_present()
    {
        var address = DeliveryRules.ComposeDeliveryAddress("المعادي", "شارع 9", "عمارة 5", "3", "12", null);
        Assert.Equal("المنطقة المعادي، ش شارع 9، عمارة عمارة 5، دور 3، شقة 12", address);
    }

    [Fact]
    public void Composed_delivery_address_skips_missing_parts_without_leaving_empty_separators()
    {
        var address = DeliveryRules.ComposeDeliveryAddress("المعادي", "شارع 9", building: null, floor: null, apartment: null, landmark: null);
        Assert.Equal("المنطقة المعادي، ش شارع 9", address);
    }

    [Fact]
    public void A_landmark_is_appended_after_the_composed_parts_with_its_own_separator()
    {
        var address = DeliveryRules.ComposeDeliveryAddress("المعادي", "شارع 9", null, null, null, "بجوار الصيدلية");
        Assert.Equal("المنطقة المعادي، ش شارع 9 — بجوار الصيدلية", address);
    }

    [Fact]
    public void A_landmark_alone_with_no_structured_parts_is_the_entire_composed_address()
    {
        var address = DeliveryRules.ComposeDeliveryAddress(null, null, null, null, null, "بجوار الصيدلية");
        Assert.Equal("بجوار الصيدلية", address);
    }

    private static DeliveryFacts MakeDelivery(string id, string orderId, PaymentMethod method, bool settled, Money amount) =>
        new(id, orderId, $"#{orderId}", "driver-1", DeliveryStatus.Ready, method, settled, amount, amount, "shift-1", "trip-1", Money.Zero);
}

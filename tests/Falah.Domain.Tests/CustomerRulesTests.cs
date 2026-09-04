using Falah.Domain.Common;
using Falah.Domain.Customers;
using Xunit;

namespace Falah.Domain.Tests;

public class CustomerRulesTests
{
    [Fact]
    public void Differently_formatted_phone_numbers_normalize_to_the_same_value()
    {
        Assert.Equal(CustomerRules.NormalizePhone("010-1234-5678"), CustomerRules.NormalizePhone("01012345678"));
    }

    [Fact]
    public void FindByPhone_matches_regardless_of_formatting()
    {
        var customers = new[] { new Customer("cust-1", "010 1234 5678", "أحمد") };
        var found = CustomerRules.FindByPhone(customers, "01012345678");
        Assert.Equal("cust-1", found?.Id);
    }

    private static CustomerAddress Address(string area = "المعادي", string street = "شارع 9", string? building = "5", string? floor = "3", string? apartment = "12") =>
        new("addr-1", "cust-1", area, street, building, floor, apartment, Landmark: null, ServiceId: "svc-1");

    [Fact]
    public void An_address_with_the_same_area_and_street_but_different_casing_of_whitespace_still_matches()
    {
        var existing = Address(area: " المعادي ", street: " شارع 9 ");
        Assert.True(CustomerRules.AddressMatches(existing, "المعادي", "شارع 9", "5", "3", "12"));
    }

    [Fact]
    public void An_address_with_a_different_building_does_not_match()
    {
        var existing = Address(building: "5");
        Assert.False(CustomerRules.AddressMatches(existing, "المعادي", "شارع 9", "6", "3", "12"));
    }

    [Fact]
    public void Landmark_and_service_selection_are_never_part_of_the_match_key()
    {
        // Same geographic content: matches regardless of what FindMatchingAddress's caller intends
        // to update the landmark/service to - those two fields are refreshed on the matched row,
        // never used to decide whether it IS the same row.
        var existing = new CustomerAddress("addr-1", "cust-1", "المعادي", "شارع 9", "5", "3", "12", Landmark: "بجوار المسجد", ServiceId: "svc-old");
        Assert.True(CustomerRules.AddressMatches(existing, "المعادي", "شارع 9", "5", "3", "12"));
    }

    [Fact]
    public void FindMatchingAddress_returns_null_when_no_saved_address_has_the_same_geographic_content()
    {
        var addresses = new[] { Address() };
        Assert.Null(CustomerRules.FindMatchingAddress(addresses, "منطقة أخرى", "شارع آخر", null, null, null));
    }

    [Fact]
    public void Upserting_with_an_empty_phone_is_rejected()
    {
        Assert.Throws<DomainException>(() => CustomerRules.AssertUpsertInput("", "أحمد"));
    }

    [Fact]
    public void Upserting_with_a_blank_name_is_rejected()
    {
        Assert.Throws<DomainException>(() => CustomerRules.AssertUpsertInput("01012345678", "   "));
    }

    [Fact]
    public void Upserting_with_a_valid_phone_and_name_does_not_throw()
    {
        CustomerRules.AssertUpsertInput("010-1234-5678", "أحمد");
    }
}

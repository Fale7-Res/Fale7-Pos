namespace Falah.Client.Tests;

using System.Text.Json;
using CallCenterServices = Falah.Client.CallCenterWinUI.Services;
using Falah.Application.Orders;
using Xunit;

/// <summary>
/// UI pass 2026-11-08: the Call Center order lookup printed "العنوان: {area} - {street}" and dropped the
/// building, floor, apartment, landmark and delivery notes, though the order holds all of them. An operator
/// answering "where is my order?" read back half an address.
/// </summary>
[Collection(RetainedRequestStorageCollection.Name)]
public class CallCenterLookupAddressTests
{
    private static Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel Vm() =>
        new(new CallCenterServices.AppSession(new CallCenterServices.ApiClient(new HttpClient { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("operator", "موظف", "cashier", new[] { "pos" }) }, new());

    private static OrderDetailsDto Order(string json) =>
        JsonSerializer.Deserialize<OrderDetailsDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    [Fact]
    public void The_lookup_shows_the_whole_stored_address_and_the_delivery_notes()
    {
        var vm = Vm();
        vm.LookedUpOrder = Order("""{"id":"o","items":[],"deliveryAddress":"المنطقة أ، ش شارع الاختبار، عمارة 7، دور 2، شقة 4 — بجوار المخبز","deliveryArea":"المنطقة أ","deliveryStreet":"شارع الاختبار","deliveryBuilding":"7","deliveryNotes":"اتصل قبل الوصول"}""");
        Assert.Equal("المنطقة أ، ش شارع الاختبار، عمارة 7، دور 2، شقة 4 — بجوار المخبز", vm.LookedUpAddressLine);
        Assert.Equal("اتصل قبل الوصول", vm.LookedUpDeliveryNotes);
        Assert.True(vm.HasLookedUpDeliveryNotes);
    }

    [Fact]
    public void Without_a_stored_address_the_parts_are_composed_with_the_shared_rule()
    {
        var vm = Vm();
        vm.LookedUpOrder = Order("""{"id":"o","items":[],"deliveryArea":"المنطقة أ","deliveryStreet":"شارع 9","deliveryBuilding":"7","deliveryFloor":"2","deliveryApartment":"4"}""");
        Assert.Equal(Falah.Domain.Delivery.DeliveryRules.ComposeDeliveryAddress("المنطقة أ", "شارع 9", "7", "2", "4", null), vm.LookedUpAddressLine);
        Assert.Contains("7", vm.LookedUpAddressLine);
        Assert.False(vm.HasLookedUpDeliveryNotes);
    }

    [Fact]
    public void A_counter_order_has_no_address_line()
    {
        var vm = Vm();
        vm.LookedUpOrder = Order("""{"id":"o","items":[]}""");
        Assert.Equal("", vm.LookedUpAddressLine);
        Assert.False(vm.HasLookedUpAddress);
    }
}

namespace Falah.Client.Tests;

using System.Net;
using System.Net.Http.Json;
using CallCenterServices = Falah.Client.CallCenterWinUI.Services;
using CallCenterModels = Falah.Client.CallCenterWinUI.Models;
using Falah.Application.Delivery;
using Falah.Application.Shifts;
using Xunit;

/// <summary>
/// AUD-01: the server charges the zone's current fee and refuses a stale one. The Call Center loads its zones once
/// at start-up, so after such a refusal it must reload them - keeping the operator's zone selected, but now carrying
/// the fee the server will accept - or every retry would send the same stale fee again.
/// </summary>
[Collection(RetainedRequestStorageCollection.Name)]
public class CallCenterZoneFeeRefreshTests
{
    private sealed class ZoneServer : HttpMessageHandler
    {
        public long ZoneFee = 1000;
        public int ServiceLoads;
        public int Checkouts;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            HttpResponseMessage response = path switch
            {
                "/api/delivery/services" => Json(new[] { new DeliveryServiceDto("svc-7", "الحي السابع", ZoneFee, true, "owner", DateTimeOffset.UtcNow) }, () => ServiceLoads++),
                "/api/shifts/current" => Json(new CurrentShiftDto("shift-1", "مسائية", "usr-c", 100000, DateTimeOffset.UtcNow, "كاشير")),
                "/api/orders/checkout" => Refuse(),
                _ => new HttpResponseMessage(HttpStatusCode.NotFound),
            };
            return Task.FromResult(response);
        }

        private HttpResponseMessage Refuse()
        {
            Checkouts++;
            return new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
            {
                Content = JsonContent.Create(new { error = "رسوم التوصيل لهذه المنطقة 15 ج وليست 10 ج. ربما تغيّرت الرسوم - حدّث قائمة خدمات التوصيل وأعد المحاولة." }),
            };
        }

        private static HttpResponseMessage Json<T>(T body, Action? onServe = null)
        {
            onServe?.Invoke();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
        }
    }

    [Fact]
    public async Task A_refused_stale_zone_fee_reloads_the_zones_and_keeps_the_zone_selected_at_its_new_fee()
    {
        var server = new ZoneServer();
        var vm = new Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel(
            new CallCenterServices.AppSession(new CallCenterServices.ApiClient(new HttpClient(server) { BaseAddress = new Uri("http://fixture.invalid") }))
            { CurrentUser = new("operator", "موظف", "cashier", new[] { "pos" }) }, new());

        await vm.LoadDeliveryServicesCommand.ExecuteAsync(null);
        vm.SelectedDeliveryService = vm.DeliveryServices.Single();
        vm.Cart.Add(new CallCenterModels.CartLine { ProductId = "p", ProductName = "فلافل", UnitPricePiastres = 2500, Quantity = 1 });
        vm.CustomerPhone = "01000000091";
        vm.CustomerName = "عميل";
        vm.DeliveryArea = "الحي السابع";
        vm.DeliveryStreet = "شارع المدارس";

        server.ZoneFee = 1500; // re-priced in the back office after this desk loaded its zones
        await vm.SubmitOrderCommand.ExecuteAsync(null);

        Assert.Equal(1, server.Checkouts);
        Assert.Contains("رسوم التوصيل", vm.CheckoutError);
        Assert.Equal(2, server.ServiceLoads);
        Assert.Equal("svc-7", vm.SelectedDeliveryService?.Id);
        Assert.Equal(1500, vm.SelectedDeliveryService?.FeePiastres);
    }
}

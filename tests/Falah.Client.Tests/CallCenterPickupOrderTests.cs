namespace Falah.Client.Tests;

using System.Net;
using System.Net.Http.Json;
using CallCenterServices = Falah.Client.CallCenterWinUI.Services;
using CallCenterModels = Falah.Client.CallCenterWinUI.Models;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Xunit;

/// <summary>Owner request 2026-11-24: the Call Center desk takes pickup orders as well as deliveries - the customer
/// phones, the order goes to the kitchen, and they pay at the counter when they collect it (UI-0025, pay on
/// collection) or by a transfer. The desk used to send every order as a delivery, and its payment buttons
/// ("عند الاستلام") read as if they chose the order type.</summary>
[Collection(RetainedRequestStorageCollection.Name)]
public class CallCenterPickupOrderTests
{
    private static readonly System.Xml.Linq.XName XName = System.Xml.Linq.XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml");

    private sealed class Server : HttpMessageHandler
    {
        public CheckoutOrderRequest? Checkout;
        /// <summary>When set, the checkout is recorded (the default fixture refuses it).</summary>
        public bool Accept;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            switch (request.RequestUri!.AbsolutePath)
            {
                case "/api/shifts/current":
                    return Json(new CurrentShiftDto("shift-1", "مسائية", "usr-c", 100000, DateTimeOffset.UtcNow, "كاشير"));
                case "/api/delivery/services":
                    return Json(new[] { new DeliveryServiceDto("svc-7", "الحي السابع", 1500, true, "owner", DateTimeOffset.UtcNow) });
                case "/api/orders/checkout":
                    Checkout = await request.Content!.ReadFromJsonAsync<CheckoutOrderRequest>(cancellationToken: ct);
                    return Accept
                        ? Json(new CheckoutOrderResult("ord-1", "#0131", 131, 4500, 0, 0, "preparing", false, null, false,
                            AwaitingCollection: Checkout!.PayOnCollection))
                        : new HttpResponseMessage(HttpStatusCode.UnprocessableEntity) { Content = JsonContent.Create(new { error = "fixture" }) };
                default:
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }

        private static HttpResponseMessage Json<T>(T body) => new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
    }

    private static Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel Desk(Server server) =>
        new(new CallCenterServices.AppSession(new CallCenterServices.ApiClient(new HttpClient(server) { BaseAddress = new Uri("http://fixture.invalid") }))
            { CurrentUser = new("operator", "موظف", "cashier", new[] { "pos" }) }, new());

    [Fact]
    public async Task A_pickup_paid_at_the_counter_is_sent_as_an_unpaid_pickup_with_no_address_and_no_delivery_fee()
    {
        var server = new Server();
        var vm = Desk(server);
        await vm.LoadDeliveryServicesCommand.ExecuteAsync(null);
        vm.SelectedDeliveryService = vm.DeliveryServices.Single(); // chosen before the operator switched to pickup

        vm.SetOrderTypeCommand.Execute("pickup");
        vm.Cart.Add(new CallCenterModels.CartLine { ProductId = "p", ProductName = "فلافل", UnitPricePiastres = 2500, Quantity = 2 });
        vm.CustomerPhone = "01000000092";
        vm.CustomerName = "عميل";

        Assert.True(vm.IsPickup);
        Assert.True(vm.HasDeliveryDetails); // a pickup needs only the name and the phone
        Assert.Equal(5000, vm.GrandTotalPiastres); // no delivery fee on a pickup
        Assert.Equal("يدفع في المطعم", vm.CashPaymentLabel);

        await vm.SubmitOrderCommand.ExecuteAsync(null);

        var sent = Assert.IsType<CheckoutOrderRequest>(server.Checkout);
        Assert.Equal("pickup", sent.OrderType);
        Assert.True(sent.PayOnCollection);
        Assert.Equal(0, sent.AmountPaidPiastres);
        Assert.Equal(0, sent.DeliveryFeePiastres);
        Assert.Null(sent.DeliveryServiceId);
        Assert.Null(sent.DeliveryArea);
        Assert.Null(sent.DeliveryStreet);
    }

    [Fact]
    public async Task A_pickup_paid_by_instapay_is_a_claimed_transfer_not_a_pay_on_collection()
    {
        var server = new Server();
        var vm = Desk(server);
        vm.SetOrderTypeCommand.Execute("pickup");
        vm.SetPaymentMethodCommand.Execute("instapay");
        vm.Cart.Add(new CallCenterModels.CartLine { ProductId = "p", ProductName = "فلافل", UnitPricePiastres = 2500, Quantity = 1 });
        vm.CustomerPhone = "01000000093";
        vm.CustomerName = "عميل";

        await vm.SubmitOrderCommand.ExecuteAsync(null);

        var sent = Assert.IsType<CheckoutOrderRequest>(server.Checkout);
        Assert.Equal("pickup", sent.OrderType);
        Assert.Equal("instapay", sent.PaymentMethod);
        Assert.False(sent.PayOnCollection);
        Assert.Equal(2500, sent.AmountPaidPiastres);
    }

    /// <summary>Seen on the trial station 2026-11-24: bound, the empty optional pickers showed "1/1/1926" and
    /// "12:00 AM" as if a time had been chosen - the same fault the cashier's PickupPickerBindingTests guards.</summary>
    [Fact]
    public void The_optional_pickup_pickers_are_not_bound_and_are_wired_to_the_code_behind()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Falah.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);
        var elements = System.Xml.Linq.XDocument.Load(Path.Combine(dir!.FullName, "src", "Falah.Client.CallCenterWinUI", "Views",
            "CallCenterDeliveryEditorView.xaml")).Descendants().ToList();
        var date = Assert.Single(elements, e => e.Name.LocalName == "CalendarDatePicker");

        Assert.Null(date.Attribute("Date"));
        Assert.Equal("OnPickupDateChanged", (string?)date.Attribute("DateChanged"));
        // Audit 2026-11-25 U16: the time is two lists (hour, minute) with Arabic placeholders - the framework TimePicker
        // showed "hour"/"minute" in English. Still not bound (a bound null showed "0:00"); the code-behind owns them.
        Assert.DoesNotContain(elements, e => e.Name.LocalName == "TimePicker");
        var boxes = elements.Where(e => e.Name.LocalName == "ComboBox" && ((string?)e.Attribute(XName))?.EndsWith("Box") == true).ToList();
        Assert.Equal(new[] { "PickupHourBox", "PickupMinuteBox" }, boxes.Select(b => (string?)b.Attribute(XName)));
        Assert.All(boxes, b => Assert.Null(b.Attribute("SelectedItem")));
        Assert.All(boxes, b => Assert.Equal("OnPickupTimePartChanged", (string?)b.Attribute("SelectionChanged")));
        Assert.Equal(new[] { "الساعة", "الدقيقة" }, boxes.Select(b => (string?)b.Attribute("PlaceholderText")));
    }

    /// <summary>UI review 2026-11-24: a pickup was registered and the confirmation read "السائق يحصّل المبلغ عند
    /// التسليم" - the driver collects - for an order no driver ever touches. The note is now the registered order's.</summary>
    [Fact]
    public async Task The_confirmation_of_a_registered_pickup_says_the_customer_pays_at_the_restaurant_not_the_driver()
    {
        var server = new Server { Accept = true };
        var vm = Desk(server);
        vm.SetOrderTypeCommand.Execute("pickup");
        vm.Cart.Add(new CallCenterModels.CartLine { ProductId = "p", ProductName = "كبدة", UnitPricePiastres = 4500, Quantity = 1 });
        vm.CustomerPhone = "01000000095";
        vm.CustomerName = "عميل";

        await vm.SubmitOrderCommand.ExecuteAsync(null);

        Assert.True(vm.HasCheckoutResult);
        Assert.True(vm.IsDelivery); // the form is reset for the next call...
        Assert.Contains("في المطعم", vm.LastOrderPaymentNote); // ...but the note is about the order just registered
        Assert.DoesNotContain("السائق", vm.LastOrderPaymentNote);
    }

    [Fact]
    public async Task The_confirmation_of_a_registered_cash_delivery_says_the_driver_collects()
    {
        var server = new Server { Accept = true };
        var vm = Desk(server);
        await vm.LoadDeliveryServicesCommand.ExecuteAsync(null);
        vm.SelectedDeliveryService = vm.DeliveryServices.Single();
        vm.Cart.Add(new CallCenterModels.CartLine { ProductId = "p", ProductName = "كبدة", UnitPricePiastres = 4500, Quantity = 1 });
        vm.CustomerPhone = "01000000096";
        vm.CustomerName = "عميل";
        vm.DeliveryArea = "الحي السابع";
        vm.DeliveryStreet = "شارع 9";

        await vm.SubmitOrderCommand.ExecuteAsync(null);

        Assert.True(vm.HasCheckoutResult);
        Assert.Contains("السائق", vm.LastOrderPaymentNote);
    }

    /// <summary>The same review: looking up that unpaid pickup read "تحصيل عند التسليم" - collect on delivery.</summary>
    [Fact]
    public void A_looked_up_unpaid_pickup_is_paid_at_the_restaurant_not_on_delivery()
    {
        var vm = Desk(new Server());
        vm.LookedUpOrder = System.Text.Json.JsonSerializer.Deserialize<OrderDetailsDto>(
            """{"Id":"o","OrderNumber":"#0131","OrderType":"pickup","PaymentMethod":"cash","Status":"preparing","IsPaid":false,"Items":[]}""");

        Assert.Contains("في المطعم", vm.LookedUpPaymentLine);
        Assert.DoesNotContain("التسليم", vm.LookedUpPaymentLine);
    }

    [Fact]
    public void A_delivery_still_needs_its_address_and_names_cash_as_paid_to_the_driver()
    {
        var vm = Desk(new Server());
        vm.CustomerPhone = "01000000094";
        vm.CustomerName = "عميل";

        Assert.True(vm.IsDelivery);
        Assert.False(vm.HasDeliveryDetails);
        Assert.Equal("كاش مع الطيار", vm.CashPaymentLabel);
    }
}

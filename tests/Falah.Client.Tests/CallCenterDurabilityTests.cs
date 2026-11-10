namespace Falah.Client.Tests;

using CallCenterServices = Falah.Client.CallCenterWinUI.Services;
using Falah.Application.Orders;
using Falah.Application.Printing;
using Xunit;

/// <summary>
/// The Call Center desk keeps its own hand-rolled retention rather than the Cashier's
/// RetainedRequest, so its durability is proved separately: the same power-cut case, on the screen
/// that takes phoned-in orders.
///
/// The store is written to before the request is sent, which is the whole point - a checkout that
/// died mid-flight has to be recoverable, and one that was never sent has nothing to recover.
/// </summary>
// Shares the process-wide RetainedRequestStorage.Current with the other durability classes; running them in
// parallel let one class's store leak into another's assertions.
[Collection(RetainedRequestStorageCollection.Name)]
public class CallCenterDurabilityTests : IDisposable
{
    private readonly string _directory;
    private readonly CallCenterServices.FileRetainedRequestStore _store;

    public CallCenterDurabilityTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "falah-cc-retained-" + Guid.NewGuid().ToString("N"));
        _store = new CallCenterServices.FileRetainedRequestStore(_directory);
        CallCenterServices.RetainedRequestStorage.Current = _store;
    }

    public void Dispose()
    {
        CallCenterServices.RetainedRequestStorage.Current = null;
        try { Directory.Delete(_directory, recursive: true); } catch { /* temp cleanup only */ }
    }

    private static CheckoutOrderRequest Checkout(string id) => new(
        CheckoutRequestId: id,
        CashierId: "usr-1",
        ShiftId: "shift-1",
        OrderType: "delivery",
        PaymentMethod: "cash",
        Items: Array.Empty<CheckoutItemRequest>(),
        DiscountPiastres: 0,
        DeliveryFeePiastres: 0,
        CompAmountPiastres: 0,
        CompApprovalId: null,
        CompApproverUserId: null,
        CompReason: null,
        AmountPaidPiastres: 0,
        SplitPayments: null,
        WalletProvider: null,
        TableNumber: null,
        CustomerName: "عميل",
        CustomerPhone: "01000000000",
        DeliveryAddress: null,
        DeliveryArea: "الحي السابع",
        BusinessDayId: null,
        DeliveryStreet: "شارع");

    [Fact]
    public void A_checkout_left_in_flight_is_recovered_with_its_own_idempotency_key()
    {
        var submitted = Checkout("req-original");
        _store.Save("callcenter.checkout.usr-1", submitted);

        var recovered = Assert.IsType<CheckoutOrderRequest>(_store.Load("callcenter.checkout.usr-1"));

        // Re-sending with the same key is what lets the server say whether the first attempt landed.
        Assert.Equal("req-original", recovered.CheckoutRequestId);
        // Record equality would compare the item collection by reference, and a round trip through
        // JSON necessarily rebuilds it. What has to hold is that the payload the server receives is
        // identical, so that is what is compared.
        Assert.Equal(
            System.Text.Json.JsonSerializer.Serialize(submitted),
            System.Text.Json.JsonSerializer.Serialize(recovered));
    }

    [Fact]
    public void A_resolved_checkout_leaves_nothing_behind()
    {
        _store.Save("callcenter.checkout.usr-1", Checkout("req-1"));
        _store.Clear("callcenter.checkout.usr-1");

        Assert.Null(_store.Load("callcenter.checkout.usr-1"));
        Assert.Empty(_store.List());
    }

    [Fact]
    public void Two_operators_never_recover_each_others_unresolved_checkout()
    {
        _store.Save("callcenter.checkout.usr-1", Checkout("req-a"));

        Assert.NotNull(_store.Load("callcenter.checkout.usr-1"));
        Assert.Null(_store.Load("callcenter.checkout.usr-2"));
    }

    [Fact]
    public void A_print_request_left_in_flight_is_recovered_separately_from_the_checkout()
    {
        var print = new CreatePrintJobsRequest("pj-1", "ord-1", "CallCenter", true, "usr-1");
        _store.Save("callcenter.checkout.usr-1", Checkout("req-1"));
        _store.Save("callcenter.print.usr-1", print);

        var recoveredPrint = Assert.IsType<CreatePrintJobsRequest>(_store.Load("callcenter.print.usr-1"));
        Assert.Equal(print, recoveredPrint);   // no collections on this one, so value equality holds
        Assert.IsType<CheckoutOrderRequest>(_store.Load("callcenter.checkout.usr-1"));
        Assert.Equal(2, _store.List().Count);
    }

    [Fact]
    public void The_call_center_store_is_scoped_to_its_own_station()
    {
        var callCenter = CallCenterServices.FileRetainedRequestStore.DirectoryFor("callcenter");
        var cashier = Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.DirectoryFor("cashier");

        // Both clients can run on one machine; neither may surface the other's pending work.
        Assert.NotEqual(callCenter, cashier);
        Assert.EndsWith("callcenter", callCenter);
        Assert.EndsWith("cashier", cashier);
    }

    private sealed class PrintCaptureHandler : System.Net.Http.HttpMessageHandler
    {
        public CreatePrintJobsRequest? Sent { get; private set; }
        protected override async Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, CancellationToken ct)
        {
            Sent = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<CreatePrintJobsRequest>(request.Content!, cancellationToken: ct);
            return new(System.Net.HttpStatusCode.OK) { Content = new System.Net.Http.StringContent("{\"jobs\":[],\"wasReplayed\":false}", System.Text.Encoding.UTF8, "application/json") };
        }
    }
    [Fact]
    public async Task A_call_center_customer_copy_never_requests_new_kitchen_tickets()
    {
        var handler = new PrintCaptureHandler();
        var session = new CallCenterServices.AppSession(new CallCenterServices.ApiClient(new System.Net.Http.HttpClient(handler) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("operator", "موظف", "cashier", new[] { "pos" }) };
        var vm = new Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel(session, new());
        vm.LookedUpOrder = System.Text.Json.JsonSerializer.Deserialize<OrderDetailsDto>("{\"id\":\"order\",\"items\":[]}", new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        await vm.ReprintLookedUpOrderCommand.ExecuteAsync(null);
        Assert.NotNull(handler.Sent);
        Assert.True(handler.Sent.IsReprint);
        Assert.Equal("CallCenter", handler.Sent.CustomerRoute);
        Assert.False(handler.Sent.IncludeKitchenRoutes);
    }


    [Fact]
    public void A_checkout_left_in_flight_does_not_expose_the_customer_on_disk()
    {
        var request = Checkout("private-call");
        _store.Save("callcenter.private", request);
        var raw = File.ReadAllText(Path.Combine(_directory, "callcenter.private.json"));
        Assert.DoesNotContain(request.CustomerPhone!, raw);
        var recovered = Assert.IsType<CheckoutOrderRequest>(_store.Load("callcenter.private"));
        Assert.Equal(request.CustomerPhone, recovered.CustomerPhone);
        Assert.Equal(request.CustomerName, recovered.CustomerName);
    }


    private sealed class DelayedLookupHandler : System.Net.Http.HttpMessageHandler
    {
        public TaskCompletionSource<System.Net.Http.HttpResponseMessage> First { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<System.Net.Http.HttpResponseMessage> Second { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _calls;
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, CancellationToken ct) =>
            Interlocked.Increment(ref _calls) == 1 ? First.Task : Second.Task;
        public static System.Net.Http.HttpResponseMessage Customer(string id, string name) => new(System.Net.HttpStatusCode.OK)
        { Content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(new { id, name, phone = "01000000000", addresses = Array.Empty<object>() }), System.Text.Encoding.UTF8, "application/json") };
    }

    [Fact]
    public async Task A_late_customer_lookup_cannot_replace_a_newer_phone_result()
    {
        var handler = new DelayedLookupHandler();
        var session = new CallCenterServices.AppSession(new CallCenterServices.ApiClient(new System.Net.Http.HttpClient(handler) { BaseAddress = new Uri("http://fixture.invalid") }));
        var vm = new Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel(session, new());
        // Set the backing field to run both lookup tasks explicitly, rather than losing the
        // fire-and-forget automatic task. Phone-change invalidation is exercised below separately.
        var phone = typeof(Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel).GetField("_customerPhone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        phone.SetValue(vm, "01000000001");
        var older = vm.LookupCustomerCommand.ExecuteAsync(null);
        phone.SetValue(vm, "01000000002");
        var newer = vm.LookupCustomerCommand.ExecuteAsync(null);
        handler.Second.SetResult(DelayedLookupHandler.Customer("new", "العميل الجديد"));
        await newer;
        handler.First.SetResult(DelayedLookupHandler.Customer("old", "العميل القديم"));
        await older;
        Assert.Equal("new", vm.CustomerId);
        Assert.Equal("العميل الجديد", vm.CustomerName);
    }

    [Fact]
    public async Task Clearing_the_phone_invalidates_an_outstanding_lookup()
    {
        var handler = new DelayedLookupHandler();
        var session = new CallCenterServices.AppSession(new CallCenterServices.ApiClient(new System.Net.Http.HttpClient(handler) { BaseAddress = new Uri("http://fixture.invalid") }));
        var vm = new Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel(session, new());
        typeof(Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel).GetField("_customerPhone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(vm, "01000000001");
        var pending = vm.LookupCustomerCommand.ExecuteAsync(null);
        vm.CustomerPhone = "";
        handler.First.SetResult(DelayedLookupHandler.Customer("old", "العميل القديم"));
        await pending;
        Assert.Null(vm.CustomerId);
        Assert.Empty(vm.CustomerName);
        Assert.Empty(vm.SavedAddresses);
        Assert.False(vm.IsLookingUpCustomer);
    }


    private sealed class RefusedPrintHandler : System.Net.Http.HttpMessageHandler
    {
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.UnprocessableEntity) { Content = new System.Net.Http.StringContent("{\"error\":\"refused\"}", System.Text.Encoding.UTF8, "application/json") });
    }
    [Fact]
    public async Task Discarding_a_definitively_refused_reprint_also_clears_its_durable_copy()
    {
        var session = new CallCenterServices.AppSession(new CallCenterServices.ApiClient(new System.Net.Http.HttpClient(new RefusedPrintHandler()) { BaseAddress = new Uri("http://fixture.invalid") }))
            { CurrentUser = new("operator", "موظف", "cashier", new[] { "pos" }) };
        var vm = new Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel(session, new());
        vm.LookedUpOrder = System.Text.Json.JsonSerializer.Deserialize<OrderDetailsDto>("{\"id\":\"order\",\"items\":[]}", new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        await vm.ReprintLookedUpOrderCommand.ExecuteAsync(null);
        Assert.NotNull(_store.Load("callcenter.print.operator"));
        vm.StartNewOrderCommand.Execute(null);
        Assert.Null(_store.Load("callcenter.print.operator"));
        Assert.False(new Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel(session, new()).HasPendingPrint);
    }


    [Theory]
    [InlineData("cash", "نقدي")]
    [InlineData("instapay", "إنستاباي")]
    [InlineData("wallet", "محفظة")]
    [InlineData("card", "بطاقة")]
    public void Lookup_payment_labels_are_in_Arabic_and_a_cancelled_order_is_never_collectible(string method, string label)
    {
        var session = new CallCenterServices.AppSession(new CallCenterServices.ApiClient(new System.Net.Http.HttpClient(new RefusedPrintHandler()) { BaseAddress = new Uri("http://fixture.invalid") }));
        var vm = new Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel(session, new());
        vm.LookedUpOrder = System.Text.Json.JsonSerializer.Deserialize<OrderDetailsDto>(System.Text.Json.JsonSerializer.Serialize(new { id = "order", items = Array.Empty<object>(), paymentMethod = method, status = "cancelled", isPaid = false }), new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Contains(label, vm.LookedUpPaymentLine);
        Assert.Contains("ملغى", vm.LookedUpPaymentLine);
        Assert.DoesNotContain("تحصيل عند التسليم", vm.LookedUpPaymentLine);
    }

}

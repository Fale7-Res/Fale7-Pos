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
}

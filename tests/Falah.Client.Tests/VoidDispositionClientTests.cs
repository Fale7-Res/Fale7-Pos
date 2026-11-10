namespace Falah.Client.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// Cancelling an unconfirmed electronic sale asks the operator what became of the food, because only
/// they know - and the server will not accept the cancel without the answer (owner's decision:
/// prepared food is recorded as waste, unprepared ingredients go back into stock).
/// </summary>
public class VoidDispositionClientTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static AppSession Session(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) =>
        new(new ApiClient(new HttpClient(new Handler(send)) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("actor-void", "اختبار", "manager", new[] { "all" }), ShiftId = "shift" };

    private static CashierViewModel WithPendingSale(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)
    {
        var vm = new CashierViewModel(Session(send), new HeaderViewModel());
        vm.LastCheckoutResult = new CheckoutOrderResult("o1", "#0128", 128, 12000, 12000, 0, "completed", false, "pending", false);
        vm.CancelPendingReason = "لم يصل التحويل";
        return vm;
    }

    [Fact]
    public async Task Cancelling_without_saying_whether_the_food_was_prepared_sends_nothing()
    {
        var sent = 0;
        var vm = WithPendingSale(_ => { sent++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); });

        await vm.CancelPendingElectronicPaymentCommand.ExecuteAsync(null);

        Assert.Equal(0, sent);
        Assert.NotNull(vm.PendingPaymentActionError);
    }

    [Theory]
    [InlineData(true, "prepared")]
    [InlineData(false, "not_prepared")]
    public async Task The_operators_answer_travels_with_the_cancel(bool prepared, string expected)
    {
        CancelPendingElectronicPaymentRequest? sent = null;
        var vm = WithPendingSale(async request =>
        {
            sent = await request.Content!.ReadFromJsonAsync<CancelPendingElectronicPaymentRequest>();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"orderId":"o1","status":"cancelled","electronicPaymentStatus":"cancelled","wasReplayed":false,"cashToHandBackPiastres":5000}""", Encoding.UTF8, "application/json"),
            };
        });
        if (prepared) vm.IsCancelFoodPrepared = true; else vm.IsCancelFoodNotPrepared = true;

        await vm.CancelPendingElectronicPaymentCommand.ExecuteAsync(null);

        Assert.Equal(expected, sent!.StockDisposition);
        Assert.Equal("cancelled", vm.LastCheckoutResult?.Status);
        Assert.False(vm.IsElectronicPaymentPending);
        Assert.Equal("تم إلغاء الطلب", vm.CheckoutTitle);
        Assert.Equal(5000, vm.VoidCashToHandBackPiastres);
        Assert.Contains("50", vm.VoidCashToHandBackText);
        vm.DismissCheckoutResultCommand.Execute(null);
        Assert.Null(vm.VoidCashToHandBackText);
    }

    // ---------- A cash-on-delivery order that came back ----------

    private static DeliveryDto CashOnDelivery() => new(
        "delivery-1", "o1", "#0128", "OutForDelivery", "driver-1", "سائق", 12000, 12000, "cash", false, "shift", DateTimeOffset.UtcNow);

    [Fact]
    public async Task A_failed_cash_delivery_asks_what_became_of_the_food_before_saving()
    {
        var sent = 0;
        var vm = new DeliveryViewModel(Session(_ => { sent++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); }));
        vm.OpenOutcomeCommand.Execute(CashOnDelivery());
        vm.SetOutcomeResultCommand.Execute("failed");
        vm.SetFailureReasonCommand.Execute("customer_unreachable");

        Assert.True(vm.NeedsFoodDisposition);
        await vm.ConfirmOutcomeCommand.ExecuteAsync(null);

        Assert.Equal(0, sent);
        Assert.NotNull(vm.OutcomeError);
    }

    [Fact]
    public async Task The_answer_for_a_returned_cash_delivery_travels_with_the_result()
    {
        Falah.Application.Delivery.RecordDeliveryResultRequest? sent = null;
        var vm = new DeliveryViewModel(Session(async request =>
        {
            if (request.Method == HttpMethod.Post)
                sent = await request.Content!.ReadFromJsonAsync<Falah.Application.Delivery.RecordDeliveryResultRequest>();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
        }));
        vm.OpenOutcomeCommand.Execute(CashOnDelivery());
        vm.SetOutcomeResultCommand.Execute("returned");
        vm.OutcomeReason = "رجع الطلب";
        vm.IsOutcomeFoodPrepared = true;

        await vm.ConfirmOutcomeCommand.ExecuteAsync(null);

        Assert.Equal("prepared", sent!.StockDisposition);
    }

    [Fact]
    public void A_delivered_or_prepaid_order_is_never_asked()
    {
        var vm = new DeliveryViewModel(Session(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));
        vm.OpenOutcomeCommand.Execute(CashOnDelivery());
        Assert.False(vm.NeedsFoodDisposition);   // delivered

        vm.OpenOutcomeCommand.Execute(CashOnDelivery() with { PaymentMethod = "card", PaymentAlreadySettled = true, AmountToCollectPiastres = 0 });
        vm.SetOutcomeResultCommand.Execute("failed");
        Assert.False(vm.NeedsFoodDisposition);   // its money goes back through a refund instead
    }
}

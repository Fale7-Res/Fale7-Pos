using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
namespace Falah.Client.Tests;
public class PickupDstTests
{
    private sealed class RejectNetwork : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            throw new InvalidOperationException("Invalid pickup time must never reach the network.");
        }
    }
    [Fact]
    public async Task A_skipped_Cairo_pickup_time_is_explained_without_a_binding_exception()
    {
        var network = new RejectNetwork();
        var session = new AppSession(new ApiClient(new HttpClient(network) { BaseAddress = new Uri("http://fixture.invalid") }))
            { CurrentUser = new("cashier", "كاشير", "cashier", new[] { "pos" }) };
        var vm = session.GetCashier(new());
        vm.CustomerName = "عميل"; vm.CustomerPhone = "01000000000";
        vm.IsPickupEditorOpen = true;
        vm.PickupDate = new DateTimeOffset(2027, 4, 30, 0, 0, 0, TimeSpan.Zero);
        vm.PickupTime = TimeSpan.FromMinutes(30);
        Assert.Null(vm.RequestedPickupAtUtc);
        Assert.NotEmpty(vm.PickupSummary);
        vm.SavePickupDetailsCommand.Execute(null);
        Assert.True(vm.IsPickupEditorOpen);
        Assert.Contains("التوقيت الصيفي", vm.PickupError);
        vm.OrderType = "pickup";
        await vm.SubmitPaymentCommand.ExecuteAsync(null);
        Assert.Contains("التوقيت الصيفي", vm.CheckoutError);
        Assert.Equal(0, network.Calls);
    }
}

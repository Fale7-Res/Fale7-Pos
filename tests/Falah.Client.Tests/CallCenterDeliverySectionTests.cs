namespace Falah.Client.Tests;

using System.Net;
using System.Net.Http.Json;
using CallCenterServices = Falah.Client.CallCenterWinUI.Services;
using CallCenterViewModels = Falah.Client.CallCenterWinUI.ViewModels;
using Falah.Application.Delivery;
using Xunit;

/// <summary>Owner request 2026-11-24: the Call Center station takes orders, looks them up and manages their
/// delivery (assign, record the result, settle a driver, the driver's statement). The screens are the cashier's,
/// ported; these tests cover what the port added: who gets the delivery tab, a delivery command left unanswered
/// by an earlier run, and what one operator leaves behind for the next.</summary>
[Collection(RetainedRequestStorageCollection.Name)]
public class CallCenterDeliverySectionTests : IDisposable
{
    private readonly string _directory;
    private readonly CallCenterServices.FileRetainedRequestStore _store;

    public CallCenterDeliverySectionTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "falah-cc-delivery-" + Guid.NewGuid().ToString("N"));
        _store = new CallCenterServices.FileRetainedRequestStore(_directory);
        CallCenterServices.RetainedRequestStorage.Current = _store;
    }

    public void Dispose()
    {
        CallCenterServices.RetainedRequestStorage.Current = null;
        try { Directory.Delete(_directory, recursive: true); } catch { /* temp cleanup only */ }
    }

    private sealed class Server : HttpMessageHandler
    {
        public readonly List<string> AssignRequestIds = new();
        public string OrdersJson = "[]";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/assign", StringComparison.Ordinal))
            {
                var body = await request.Content!.ReadFromJsonAsync<AssignDeliveryRequest>(cancellationToken: ct);
                AssignRequestIds.Add(body!.RequestId);
                return Json(new AssignDeliveryResult(body.DeliveryId, "OutForDelivery", body.DriverId, "سائق", "trip-1", WasReplayed: true));
            }
            return path switch
            {
                "/api/delivery/board" => Json(Array.Empty<DeliveryDto>()),
                "/api/delivery/drivers" => Json(Array.Empty<DriverDto>()),
                "/api/orders" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(OrdersJson, System.Text.Encoding.UTF8, "application/json") },
                _ => new HttpResponseMessage(HttpStatusCode.NotFound),
            };
        }

        private static HttpResponseMessage Json<T>(T body) => new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };
    }

    private static CallCenterServices.AppSession Session(Server server, params string[] permissions) =>
        new(new CallCenterServices.ApiClient(new HttpClient(server) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("usr-op", "موظف", "cashier", permissions) };

    [Fact]
    public void The_delivery_tab_needs_delivery_manage()
    {
        var session = Session(new Server(), "pos");
        var main = new CallCenterViewModels.MainViewModel(session) { IsLoggedIn = true };

        Assert.False(main.CanSeeDelivery);
        main.NavigateCommand.Execute("delivery");
        Assert.Equal("desk", main.Section);
        Assert.IsType<CallCenterViewModels.CallCenterViewModel>(main.CurrentView);

        main.NavigateCommand.Execute("orders");
        Assert.Equal("orders", main.Section);
        Assert.IsType<CallCenterViewModels.OrdersViewModel>(main.CurrentView);
        main.Stop();
    }

    [Fact]
    public void A_dispatcher_with_delivery_manage_reaches_the_board()
    {
        var session = Session(new Server(), "pos", "delivery_manage");
        var main = new CallCenterViewModels.MainViewModel(session) { IsLoggedIn = true };

        Assert.True(main.CanSeeDelivery);
        main.NavigateCommand.Execute("delivery");
        Assert.Equal("delivery", main.Section);
        Assert.Same(session.DeliveryBoard, main.CurrentView);
        main.Stop();
    }

    [Fact]
    public async Task An_assignment_left_unanswered_blocks_the_lock_until_it_is_retried_with_its_own_request_id()
    {
        _store.Save("delivery.assign", new AssignDeliveryRequest("asg-original", "del-1", "drv-1", "usr-op", null, null));
        var server = new Server();
        var session = Session(server, "pos", "delivery_manage");

        var board = session.DeliveryBoard;
        Assert.True(board.HasRecoveredRequest);
        Assert.False(board.CanEditAction);
        Assert.True(session.HasUnresolvedOperation);

        await board.RetryRecoveredCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "asg-original" }, server.AssignRequestIds);
        Assert.False(board.HasRecoveredRequest);
        Assert.True(board.CanEditAction);
        Assert.False(session.HasUnresolvedOperation);
        Assert.Null(_store.Load("delivery.assign"));
    }

    [Fact]
    public async Task The_next_operator_does_not_see_the_last_ones_orders_before_their_own_load()
    {
        var server = new Server
        {
            OrdersJson = """[{"id":"ord-1","orderNumber":"A-0001","orderType":"delivery","paymentMethod":"cash","items":[],"totalPiastres":5000,"status":"completed","customerName":"عميل","customerPhone":"01000000000","cashierId":"usr-op","shiftId":"shift-1"}]""",
        };
        var session = Session(server, "pos", "orders_view_all");
        var orders = session.OrderHistory;
        await orders.LoadAsync();
        Assert.Single(orders.Orders);

        session.CurrentUser = new("usr-next", "موظف آخر", "cashier", new[] { "pos" });
        orders.RefreshPermissions();

        Assert.Empty(orders.Orders);
        Assert.False(orders.CanPickDates);
    }
}

using System.Net;
using System.Text;
using System.Text.Json;
using Falah.Application.Delivery;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>
/// Owner workflow (2026-11-08) on the screens: the settlement dialog offers the driver's net after his fees (or
/// the payout owed to him) exactly as the server will compute it, and the close dialog splits the count into the
/// float left in the drawer and the cash handed to a named receiver. Nothing here computes money the server does
/// not; the tests check what each screen SENDS.
/// </summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class DriverSettlementAndHandoverViewModelTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "falah-settle-" + Guid.NewGuid().ToString("N"));
    private readonly IRetainedRequestStore? _previous = RetainedRequestStorage.Current;

    public DriverSettlementAndHandoverViewModelTests() => RetainedRequestStorage.Current = new FileRetainedRequestStore(_dir);

    public void Dispose()
    {
        RetainedRequestStorage.Current = _previous;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    // ---------- driver settlement ----------

    private static (DeliveryViewModel Vm, List<JsonElement> Sent) Delivery(string accountJson)
    {
        var sent = new List<JsonElement>();
        var http = new HttpClient(new Handler(async r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            if (r.Method == HttpMethod.Post && path.EndsWith("/settle"))
            {
                sent.Add(JsonDocument.Parse(await r.Content!.ReadAsStringAsync()).RootElement.Clone());
                return Json("""{"settlementId":"s-1","driverId":"driver","orderCount":3,"actualPiastres":0,"wasReplayed":false}""");
            }
            if (path.EndsWith("/account")) return Json(accountJson);
            return Json("[]");
        })) { BaseAddress = new Uri("http://fixture.invalid") };
        var session = new AppSession(new ApiClient(http))
        {
            CurrentUser = new("actor", "اختبار", "manager", new[] { "all" }), ShiftId = "shift",
        };
        return (new DeliveryViewModel(session), sent);
    }

    private static readonly DriverDto Driver = new("driver", "سائق", true);

    [Fact]
    public async Task The_settlement_offers_the_net_after_fees_and_sends_it_without_a_difference()
    {
        var (vm, sent) = Delivery("""{"driverId":"driver","cashCustodyPiastres":35000,"pendingFeesPiastres":7500,"carriedBalancePiastres":0,"netDuePiastres":27500,"canSettle":true}""");
        await vm.OpenDriverAccountCommand.ExecuteAsync(Driver);

        Assert.Equal(CashInput.Format(27_500), vm.SettleActualCashText);
        Assert.False(vm.RestaurantOwesDriver);
        Assert.Equal(CashInput.Format(7_500) + " ج", vm.SettleFeesText);
        Assert.Equal("صافي يسلّمه السائق", vm.SettleNetLabel);

        await vm.SettleDriverCommand.ExecuteAsync(null);
        var request = Assert.Single(sent);
        Assert.Equal(27_500, request.GetProperty("actualCashPiastres").GetInt64());
        Assert.Equal(0, request.GetProperty("paidToDriverPiastres").GetInt64());
        Assert.False(request.GetProperty("keepRemainderOpen").GetBoolean());
        Assert.Null(vm.SettleError);
    }

    [Fact]
    public async Task When_the_restaurant_owes_the_driver_the_dialog_offers_an_explicit_payout()
    {
        var (vm, sent) = Delivery("""{"driverId":"driver","cashCustodyPiastres":0,"pendingFeesPiastres":2500,"netDuePiastres":-2500,"canSettle":true}""");
        await vm.OpenDriverAccountCommand.ExecuteAsync(Driver);

        Assert.True(vm.RestaurantOwesDriver);
        Assert.Equal("مستحق للسائق يُصرف له", vm.SettleNetLabel);
        Assert.Equal(CashInput.Format(0), vm.SettleActualCashText);
        Assert.Equal(CashInput.Format(2_500), vm.SettlePaidToDriverText);

        await vm.SettleDriverCommand.ExecuteAsync(null);
        var request = Assert.Single(sent);
        Assert.Equal(0, request.GetProperty("actualCashPiastres").GetInt64());
        Assert.Equal(2_500, request.GetProperty("paidToDriverPiastres").GetInt64());
    }

    [Fact]
    public async Task Receiving_and_paying_in_one_settlement_or_paying_more_than_owed_is_refused_before_anything_is_sent()
    {
        var (vm, sent) = Delivery("""{"driverId":"driver","cashCustodyPiastres":0,"pendingFeesPiastres":2500,"netDuePiastres":-2500,"canSettle":true}""");
        await vm.OpenDriverAccountCommand.ExecuteAsync(Driver);

        vm.SettleActualCashText = "1";
        await vm.SettleDriverCommand.ExecuteAsync(null);
        Assert.NotNull(vm.SettleError);

        vm.SettleActualCashText = "0";
        vm.SettlePaidToDriverText = "30";
        await vm.SettleDriverCommand.ExecuteAsync(null);
        Assert.NotNull(vm.SettleError);
        Assert.Empty(sent);
    }

    [Fact]
    public async Task A_partial_settlement_carries_the_rest_and_needs_no_difference_reason()
    {
        var (vm, sent) = Delivery("""{"driverId":"driver","cashCustodyPiastres":35000,"pendingFeesPiastres":7500,"netDuePiastres":27500,"canSettle":true}""");
        await vm.OpenDriverAccountCommand.ExecuteAsync(Driver);
        vm.SettleActualCashText = CashInput.Format(20_000);
        vm.SettleKeepRemainderOpen = true;

        await vm.SettleDriverCommand.ExecuteAsync(null);
        var request = Assert.Single(sent);
        Assert.Equal(20_000, request.GetProperty("actualCashPiastres").GetInt64());
        Assert.True(request.GetProperty("keepRemainderOpen").GetBoolean());
        Assert.Equal(JsonValueKind.Null, request.GetProperty("reason").ValueKind);
    }

    [Fact]
    public async Task A_final_settlement_short_of_the_net_still_needs_a_reason()
    {
        var (vm, sent) = Delivery("""{"driverId":"driver","cashCustodyPiastres":35000,"pendingFeesPiastres":7500,"netDuePiastres":27500,"canSettle":true}""");
        await vm.OpenDriverAccountCommand.ExecuteAsync(Driver);
        vm.SettleActualCashText = CashInput.Format(20_000);

        await vm.SettleDriverCommand.ExecuteAsync(null);
        Assert.NotNull(vm.SettleError);
        Assert.Empty(sent);
    }

    // ---------- the close: retained float and receiver ----------

    private static (ShiftsViewModel Vm, List<JsonElement> Sent) Shifts(long targetFloat)
    {
        var sent = new List<JsonElement>();
        var http = new HttpClient(new Handler(async r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            if (path == "/api/shifts/close")
            {
                sent.Add(JsonDocument.Parse(await r.Content!.ReadAsStringAsync()).RootElement.Clone());
                return Json("""{"shiftId":"sh-1","status":"Closed","expectedCashPiastres":125000,"actualCashPiastres":125000,"differencePiastres":0,"wasReplayed":false,"retainedFloatPiastres":100000,"handedOverPiastres":25000}""");
            }
            if (path == "/api/shifts/prepare-close")
                return Json("""{"shiftId":"sh-1","expectedCashPiastres":125000,"actualCashPiastres":125000,"differencePiastres":0,"requiresManagerApproval":false}""");
            if (path == "/api/shifts/current")
                return Json("""{"shiftId":"sh-1","shiftName":"وردية صباحية","openedByUserId":"owner","openingCashPiastres":100000,"openedAtUtc":"2026-11-07T08:00:00+00:00"}""");
            if (path == "/api/shifts/handover-receivers") return Json("""[{"userId":"mgr","name":"المدير"}]""");
            if (path == "/api/settings") return Json($$"""{"defaultOpeningCashPiastres":{{targetFloat}}}""");
            return Json("[]");
        })) { BaseAddress = new Uri("http://fixture.invalid") };
        var session = new AppSession(new ApiClient(http)) { CurrentUser = new("owner", "المالك", "owner", new[] { "shifts_manage" }) };
        return (new ShiftsViewModel(session, new HeaderViewModel()), sent);
    }

    [Fact]
    public async Task The_close_offers_the_target_float_and_hands_over_the_rest_to_the_named_receiver()
    {
        var (vm, sent) = Shifts(targetFloat: 100_000);
        await vm.LoadAsync();
        vm.OpenCloseShiftModalCommand.Execute(null);
        vm.CloseShiftActualCashText = CashInput.Format(125_000);
        await vm.PrepareCloseShiftCommand.ExecuteAsync(null);

        Assert.Equal(CashInput.Format(100_000), vm.CloseShiftRetainedText);
        Assert.Equal(CashInput.Format(25_000) + " ج", vm.CloseShiftHandedOverText);

        // Cash leaves the drawer, so the person who took it must be named.
        await vm.ConfirmCloseShiftCommand.ExecuteAsync(null);
        Assert.Empty(sent);
        Assert.NotNull(vm.CloseShiftError);

        vm.SelectedHandoverReceiver = Assert.Single(vm.HandoverReceivers);
        await vm.ConfirmCloseShiftCommand.ExecuteAsync(null);
        var request = Assert.Single(sent);
        Assert.Equal(125_000, request.GetProperty("actualCashCountedPiastres").GetInt64());
        Assert.Equal(100_000, request.GetProperty("retainedFloatPiastres").GetInt64());
        Assert.Equal("mgr", request.GetProperty("handoverReceiverUserId").GetString());
    }

    [Fact]
    public async Task The_retained_float_is_never_offered_above_what_was_counted_and_cannot_be_typed_above_it()
    {
        var (vm, sent) = Shifts(targetFloat: 200_000);
        await vm.LoadAsync();
        vm.OpenCloseShiftModalCommand.Execute(null);
        vm.CloseShiftActualCashText = CashInput.Format(125_000);
        await vm.PrepareCloseShiftCommand.ExecuteAsync(null);
        Assert.Equal(CashInput.Format(125_000), vm.CloseShiftRetainedText);   // nothing handed over, no receiver needed

        vm.CloseShiftRetainedText = CashInput.Format(130_000);
        await vm.ConfirmCloseShiftCommand.ExecuteAsync(null);
        Assert.Empty(sent);
        Assert.NotNull(vm.CloseShiftError);
    }
}

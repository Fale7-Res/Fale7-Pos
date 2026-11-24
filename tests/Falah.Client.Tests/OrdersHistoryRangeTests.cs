using System.Net;
using System.Text;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Independent audit 2026-11-23, F-06: the Orders screen fetched every order ever taken, with its lines,
/// in one 10-second call - a screen that stops loading as the restaurant's history grows. It now asks for a
/// date range (today by default) and a bounded number of rows, and says so when the cap is reached.</summary>
public class OrdersHistoryRangeTests
{
    private sealed class Capture : HttpMessageHandler
    {
        public Uri? Last;
        public string Body = "[]";
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Last = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Body, Encoding.UTF8, "application/json") });
        }
    }

    private static (OrdersViewModel Vm, Capture Capture) Make(params string[] permissions)
    {
        var capture = new Capture();
        var session = new AppSession(new ApiClient(new HttpClient(capture) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("u1", "مدير", "manager", permissions) };
        return (new OrdersViewModel(session), capture);
    }

    [Fact]
    public async Task The_order_history_asks_for_todays_orders_and_a_bounded_number_by_default()
    {
        var (vm, capture) = Make("orders_view_all");
        await vm.LoadAsync();

        var query = Uri.UnescapeDataString(capture.Last!.Query);
        var today = Falah.Domain.Common.BusinessClock.Today().ToString("yyyy-MM-dd");
        Assert.Contains($"from={today}", query);
        Assert.Contains($"to={today}", query);
        Assert.Contains($"limit={OrdersViewModel.PageLimit}", query);
    }

    [Fact]
    public async Task A_chosen_range_is_sent_as_cairo_dates()
    {
        var (vm, capture) = Make("orders_view_all");
        vm.FromDate = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.FromHours(2));
        vm.ToDate = new DateTimeOffset(2026, 11, 7, 0, 0, 0, TimeSpan.FromHours(2));
        await vm.LoadAsync();

        var query = Uri.UnescapeDataString(capture.Last!.Query);
        Assert.Contains("from=2026-11-01", query);
        Assert.Contains("to=2026-11-07", query);
    }

    [Fact]
    public void Only_a_user_who_sees_all_orders_picks_dates()
    {
        Assert.True(Make("orders_view_all").Vm.CanPickDates);
        Assert.False(Make("pos").Vm.CanPickDates);
    }
}

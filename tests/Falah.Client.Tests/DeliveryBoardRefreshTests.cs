using System.Net;
using System.Text;
using Falah.Application.Delivery;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Independent audit 2026-11-23, F-16: the delivery board was read once and then only when someone
/// pressed "تحديث", so a dispatcher worked from orders that had already moved on. The board now refreshes itself
/// while it is on screen - and never while the dispatcher is in the middle of something.</summary>
public class DeliveryBoardRefreshTests
{
    private sealed class Counting : HttpMessageHandler
    {
        public int BoardReads;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri!.AbsolutePath == "/api/delivery/board") BoardReads++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]", Encoding.UTF8, "application/json") });
        }
    }

    private static (DeliveryViewModel Vm, Counting Handler) Make()
    {
        var handler = new Counting();
        var session = new AppSession(new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("u1", "مدير", "manager", new[] { "all" }) };
        return (new DeliveryViewModel(session), handler);
    }

    [Fact]
    public async Task An_idle_board_refreshes_itself()
    {
        var (vm, handler) = Make();
        await vm.AutoRefreshCommand.ExecuteAsync(null);
        Assert.Equal(1, handler.BoardReads);
    }

    [Fact]
    public async Task The_board_does_not_refresh_under_an_open_dialog()
    {
        var (vm, handler) = Make();
        vm.AssignTarget = new DeliveryDto("d1", "o1", "#0128", "Ready", null, null, 5_000, 5_000, "cash", false, "shift-1", DateTimeOffset.UtcNow);

        await vm.AutoRefreshCommand.ExecuteAsync(null);

        Assert.Equal(0, handler.BoardReads);
    }

    [Fact]
    public async Task No_active_drivers_says_so()
    {
        var (vm, _) = Make();
        await vm.LoadAsync();
        Assert.True(vm.HasNoDrivers);
    }
}

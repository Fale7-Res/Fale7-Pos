namespace Falah.Client.Tests;

using System.Net;
using System.Reflection;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

/// <summary>Audit 2026-11-18: the connection check runs every 5 s and a ping may take up to the 10 s
/// request timeout, so with the server unreachable the checks piled up on top of each other.</summary>
public class HealthCheckOverlapTests
{
    private sealed class HangingHealth : HttpMessageHandler
    {
        public int HealthCalls;
        public readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri!.AbsolutePath == "/health")
            {
                Interlocked.Increment(ref HealthCalls);
                await Release.Task;
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task A_check_still_waiting_for_the_server_is_not_joined_by_another()
    {
        var handler = new HangingHealth();
        var session = new AppSession(new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("actor", "اختبار", "cashier", new[] { "all" }) };
        var root = new MainViewModel(session);
        var timer = (Microsoft.UI.Xaml.DispatcherTimer)typeof(MainViewModel)
            .GetField("_healthTimer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(root)!;

        timer.RaiseTick();
        timer.RaiseTick();

        Assert.Equal(1, handler.HealthCalls);
        handler.Release.SetResult();
        var checking = typeof(MainViewModel).GetField("_checkingHealth", BindingFlags.NonPublic | BindingFlags.Instance)!;
        for (var waited = 0; (bool)checking.GetValue(root)! && waited < 5000; waited += 10) await Task.Delay(10);
        timer.RaiseTick();
        Assert.Equal(2, handler.HealthCalls);
    }
}

using System.Net;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;

namespace Falah.Client.Tests;

public class WindowClosingSafetyTests
{
    private sealed class Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Cashier_window_close_respects_both_pos_and_management_operations(bool management)
    {
        var session = new AppSession(new ApiClient(new HttpClient(new Handler()) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("actor", "اختبار", "cashier", new[] { "all" }) };
        var root = new MainViewModel(session);
        var desk = session.GetCashier(new());
        var pendingOwner = new object();
        if (management) session.SetManagementOperationPending(pendingOwner, true);
        else desk.IsProcessingCheckout = true;
        Assert.False(root.TryClose()); Assert.True(root.IsCloseBlocked);
        if (management) session.SetManagementOperationPending(pendingOwner, false);
        else desk.IsProcessingCheckout = false;
        Assert.True(root.TryClose()); Assert.False(root.IsCloseBlocked);
        root.Stop();
    }
}

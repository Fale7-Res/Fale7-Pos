using System.Net;
using System.Text;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Found in the native run on 2026-11-07: a manager who logged in and landed on the
/// dashboard saw the header pill say "مغلق" while a shift was open, because only the POS screen ever
/// asked the server for the current shift.</summary>
[Collection(RetainedRequestStorageCollection.Name)]
public class HeaderShiftStateTests
{
    private sealed class Handler(bool shiftOpen) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var path = request.RequestUri!.AbsolutePath;
            string? body = path switch
            {
                "/api/auth/login-by-pin" => """{"id":"mgr","name":"مدير","role":"manager","permissions":["all"],"token":"t"}""",
                "/api/shifts/current" when shiftOpen => """{"shiftId":"shift-1","shiftName":"الصباحية","openedByUserId":"c","openingCashPiastres":0,"openedAtUtc":"2026-11-07T08:00:00Z"}""",
                "/api/shifts/current" => null,
                _ => "[]",
            };
            return Task.FromResult(body is null
                ? new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("""{"error":"لا توجد وردية مفتوحة"}""", Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    private static async Task<(MainViewModel Root, AppSession Session)> LogInAsync(bool shiftOpen)
    {
        var session = new AppSession(new ApiClient(new HttpClient(new Handler(shiftOpen)) { BaseAddress = new Uri("http://fixture.invalid") }));
        var root = new MainViewModel(session);
        var login = Assert.IsType<LoginViewModel>(root.CurrentView);
        foreach (var digit in "4321") login.PressDigitCommand.Execute(digit.ToString());
        await login.SubmitCommand.ExecuteAsync(null);
        Assert.IsType<ShellViewModel>(root.CurrentView);
        for (var i = 0; i < 50 && session.ShiftId is null && shiftOpen; i++) await Task.Delay(10);
        return (root, session);
    }

    [Fact]
    public async Task Logging_into_the_management_shell_learns_the_open_shift_for_the_header()
    {
        var (root, session) = await LogInAsync(shiftOpen: true);
        Assert.Equal("shift-1", session.ShiftId);
        Assert.Equal("الصباحية", session.ShiftName);
        root.Stop();
    }

    [Fact]
    public async Task With_no_open_shift_the_header_keeps_showing_closed()
    {
        var (root, session) = await LogInAsync(shiftOpen: false);
        await Task.Delay(50);
        Assert.Null(session.ShiftId);
        root.Stop();
    }
}

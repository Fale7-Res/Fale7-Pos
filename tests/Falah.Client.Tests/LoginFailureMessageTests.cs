namespace Falah.Client.Tests;

using System.Net;
using System.Text;
using CashierServices = Falah.Client.CashierWinUI.Services;
using CallCenterServices = Falah.Client.CallCenterWinUI.Services;
using Xunit;

/// <summary>
/// UI acceptance 2026-11-09: with the server unreachable, both login screens answered a correct PIN with
/// "رمز PIN غير صحيح.". The cashier retypes a good PIN, and a manager may think it was changed. The message must
/// name the real cause. Only a refusal from the server says the PIN is wrong.
/// </summary>
[Collection(RetainedRequestStorageCollection.Name)]
public class LoginFailureMessageTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => Task.FromResult(send(r));
    }

    private static HttpResponseMessage Status(HttpStatusCode code, string json) => new(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    private static HttpResponseMessage Down(HttpRequestMessage _) => throw new HttpRequestException("No connection could be made");

    private static async Task<string?> CashierLogin(Func<HttpRequestMessage, HttpResponseMessage> send)
    {
        var vm = new Falah.Client.CashierWinUI.ViewModels.LoginViewModel(new CashierServices.AppSession(
            new CashierServices.ApiClient(new HttpClient(new Handler(send)) { BaseAddress = new Uri("http://fixture.invalid") })));
        vm.Pin = "4321";
        await vm.SubmitCommand.ExecuteAsync(null);
        return vm.ErrorMessage;
    }

    private static async Task<string?> CallCenterLogin(Func<HttpRequestMessage, HttpResponseMessage> send)
    {
        var vm = new Falah.Client.CallCenterWinUI.ViewModels.LoginViewModel(new CallCenterServices.AppSession(
            new CallCenterServices.ApiClient(new HttpClient(new Handler(send)) { BaseAddress = new Uri("http://fixture.invalid") })));
        vm.Pin = "4321";
        await vm.SubmitCommand.ExecuteAsync(null);
        return vm.ErrorMessage;
    }

    [Fact]
    public async Task An_unreachable_server_is_not_reported_as_a_wrong_pin()
    {
        foreach (var message in new[] { await CashierLogin(Down), await CallCenterLogin(Down) })
        {
            Assert.DoesNotContain("غير صحيح", message);
            Assert.Contains("تعذر الاتصال بالسيرفر", message);
        }
    }

    [Fact]
    public async Task A_refused_pin_is_reported_as_wrong()
    {
        Assert.Equal("رمز PIN غير صحيح.", await CashierLogin(_ => Status(HttpStatusCode.Unauthorized, """{"error":"x"}""")));
        Assert.Equal("رمز PIN غير صحيح.", await CallCenterLogin(_ => Status(HttpStatusCode.Unauthorized, """{"error":"x"}""")));
    }

    [Fact]
    public async Task Too_many_attempts_and_server_errors_say_so()
    {
        var limited = await CashierLogin(_ => Status(HttpStatusCode.TooManyRequests, """{"error":"محاولات كثيرة. انتظر دقيقة ثم أعد المحاولة."}"""));
        Assert.Equal("محاولات كثيرة. انتظر دقيقة ثم أعد المحاولة.", limited);
        var broken = await CallCenterLogin(_ => Status(HttpStatusCode.InternalServerError, """{"error":"boom"}"""));
        Assert.DoesNotContain("غير صحيح", broken);
        Assert.Contains("السيرفر", broken);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Falah.Application.Delivery;
using Falah.Application.Users;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Owner decision 2026-11-25 (audit U24): a driver has an optional phone - given when added, changed later from
/// the drivers list.</summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class DriverPhoneDirectoryTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private const string Karim = """{"id":"driver-1","name":"كريم الطيار","active":true,"phone":null}""";

    private static (DeliveryDirectoryViewModel Vm, List<(string Method, string Path, string Body)> Sent) Directory()
    {
        var sent = new List<(string, string, string)>();
        var session = new AppSession(new ApiClient(new HttpClient(new Handler(async r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            if (r.Method == HttpMethod.Get)
                return path.Contains("drivers") ? Json("[" + Karim + "]") : Json("[]");
            sent.Add((r.Method.Method, path, await r.Content!.ReadAsStringAsync()));
            return Json(Karim);
        })) { BaseAddress = new Uri("http://test") }))
        { CurrentUser = new UserSummaryDto("manager", "مدير", "manager", new[] { "all" }) };
        return (new DeliveryDirectoryViewModel(session), sent);
    }

    [Fact]
    public async Task A_new_driver_is_sent_with_the_phone_typed()
    {
        var (vm, sent) = Directory();
        await vm.LoadCommand.ExecuteAsync(null);
        vm.DriverName = "سامح";
        vm.DriverPhone = "01001234567";

        await vm.AddDriverCommand.ExecuteAsync(null);

        var (method, path, body) = Assert.Single(sent);
        Assert.Equal(("POST", "/api/delivery/drivers"), (method, path));
        Assert.Equal("01001234567", System.Text.Json.JsonSerializer.Deserialize<CreateDriverRequest>(body,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!.Phone);
        Assert.Equal("", vm.DriverPhone);
    }

    [Fact]
    public async Task An_existing_drivers_phone_is_changed_from_the_list()
    {
        var (vm, sent) = Directory();
        await vm.LoadCommand.ExecuteAsync(null);

        vm.EditDriverPhoneCommand.Execute(vm.Drivers.Single());
        Assert.True(vm.IsEditingDriverPhone);
        vm.DriverPhoneEditText = "01112223334";
        await vm.SaveDriverPhoneCommand.ExecuteAsync(null);

        var (method, path, body) = Assert.Single(sent);
        Assert.Equal(("PUT", "/api/delivery/drivers/driver-1/phone"), (method, path));
        Assert.Contains("01112223334", body);
        Assert.False(vm.IsEditingDriverPhone);
    }
}

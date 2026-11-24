using System.Net;
using System.Text;
using System.Text.Json;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>CUS-0002 (owner decision 2026-11-22): the owner deletes a customer from the Customers
/// screen after confirming; staff never see the button. The server hides the customer from staff and
/// keeps the record for the owner, who sees it marked as deleted.</summary>
public sealed class CustomerDeleteViewModelTests
{
    private const string CustomerJson = """{"id":"cus-1","phone":"01012345678","name":"أحمد","addresses":[],"createdAtUtc":"2026-11-20T10:00:00Z","updatedAtUtc":"2026-11-20T10:00:00Z","isHidden":false}""";

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (CustomersViewModel Vm, List<JsonElement> Hides) Screen(string lookupJson, params string[] permissions)
    {
        var hides = new List<JsonElement>();
        var http = new HttpClient(new Handler(async r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            if (path == "/api/customers/lookup") return Json(lookupJson);
            if (path == "/api/customers/cus-1/hide")
            {
                hides.Add(JsonDocument.Parse(await r.Content!.ReadAsStringAsync()).RootElement.Clone());
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            return Json("{}", HttpStatusCode.NotFound);
        })) { BaseAddress = new Uri("http://fixture.invalid") };
        var session = new AppSession(new ApiClient(http)) { CurrentUser = new("actor", "اختبار", "owner", permissions) };
        return (new CustomersViewModel(session) { PhoneQuery = "01012345678" }, hides);
    }

    [Fact]
    public async Task The_owner_deletes_a_customer_only_after_confirming()
    {
        var (vm, hides) = Screen(CustomerJson, "all");
        await vm.SearchCommand.ExecuteAsync(null);
        Assert.True(vm.CanDeleteCustomer);

        vm.RequestDeleteCommand.Execute(null);
        Assert.True(vm.IsDeleteConfirmOpen);
        Assert.Empty(hides);

        await vm.ConfirmDeleteCommand.ExecuteAsync(null);
        var sent = Assert.Single(hides);
        Assert.Equal("cus-1", sent.GetProperty("customerId").GetString());
        Assert.Equal("actor", sent.GetProperty("actorUserId").GetString());
        Assert.False(vm.IsDeleteConfirmOpen);
        Assert.Null(vm.Customer);
        Assert.False(string.IsNullOrEmpty(vm.DeleteMessage));
    }

    [Fact]
    public async Task Staff_never_get_the_delete_button()
    {
        var (vm, hides) = Screen(CustomerJson, "pos", "delivery_manage");
        await vm.SearchCommand.ExecuteAsync(null);
        Assert.True(vm.HasCustomer);
        Assert.False(vm.CanDeleteCustomer);

        vm.RequestDeleteCommand.Execute(null);
        Assert.False(vm.IsDeleteConfirmOpen);
        Assert.Empty(hides);
    }

    [Fact]
    public async Task The_owner_sees_a_deleted_customer_marked_as_deleted_and_cannot_delete_it_again()
    {
        var (vm, _) = Screen(CustomerJson.Replace("\"isHidden\":false", "\"isHidden\":true"), "all");
        await vm.SearchCommand.ExecuteAsync(null);
        Assert.True(vm.IsHiddenCustomer);
        Assert.False(vm.CanDeleteCustomer);
    }
}

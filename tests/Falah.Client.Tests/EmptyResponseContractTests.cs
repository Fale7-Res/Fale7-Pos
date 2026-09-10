using System.Net;
using System.Text.Json;
using Falah.Client.CashierWinUI.Services;

namespace Falah.Client.Tests;

public class EmptyResponseContractTests
{
    private sealed class EmptyHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
    private static T Request<T>() => JsonSerializer.Deserialize<T>(
        """{"id":"id","productId":"id","deliveryId":"id","userId":"id","businessDayId":"id","itemId":"id","exceptionId":"id"}""",
        new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    [Theory]
    [InlineData("availability")]
    [InlineData("inventory")]
    [InlineData("ready")]
    [InlineData("user-active")]
    [InlineData("permissions")]
    [InlineData("pin")]
    [InlineData("checklist")]
    [InlineData("exception")]
    public async Task Empty_success_response_is_a_confirmed_write_not_an_unknown_json_failure(string action)
    {
        var api = new ApiClient(new HttpClient(new EmptyHandler()) { BaseAddress = new Uri("http://fixture.invalid") });
        var result = await (action switch
        {
            "availability" => api.SetProductAvailabilityAsync(Request<Falah.Application.Menu.SetProductAvailabilityRequest>()),
            "inventory" => api.UpdateInventoryItemAsync(Request<Falah.Application.Inventory.UpdateInventoryItemRequest>()),
            "ready" => api.MarkDeliveryReadyAsync(Request<Falah.Application.Delivery.MarkDeliveryReadyRequest>()),
            "user-active" => api.SetUserActiveAsync(Request<Falah.Application.Users.SetUserActiveRequest>()),
            "permissions" => api.UpdateUserPermissionsAsync(Request<Falah.Application.Users.UpdateUserPermissionsRequest>()),
            "pin" => api.ChangePinAsync(Request<Falah.Application.Users.ChangePinRequest>()),
            "checklist" => api.CompleteChecklistItemAsync(Request<Falah.Application.BusinessDay.CompleteChecklistItemRequest>()),
            _ => api.SetExceptionStatusAsync(Request<Falah.Application.BusinessDay.SetExceptionStatusRequest>())
        });
        Assert.True(result.Success);
        Assert.True(result.Value);
        Assert.False(result.OutcomeUnknown);
        Assert.Null(result.Error);
    }
}

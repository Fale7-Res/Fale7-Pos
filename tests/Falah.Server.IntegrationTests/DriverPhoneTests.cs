namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Delivery;
using Xunit;

/// <summary>Owner decision 2026-11-25 (audit U24): a driver was a name only - nobody at the counter or the call center
/// could ring the driver carrying an order. A driver has an optional phone, given when added and changed later.</summary>
public class DriverPhoneTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _dispatcherId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _dispatcherId = await _factory.SeedUserAsync("موظف التوصيل", "dispatcher", "delivery_manage");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task A_driver_is_added_with_a_phone_and_the_phone_can_be_changed_and_is_listed()
    {
        var created = await (await _client.PostAsJsonAsync("/api/delivery/drivers",
            new CreateDriverRequest($"drv-{Guid.NewGuid():N}", "كريم الطيار", _dispatcherId, " 01001234567 "))).Content.ReadFromJsonAsync<DriverDto>();
        Assert.Equal("01001234567", created!.Phone);

        var changed = await _client.PutAsJsonAsync($"/api/delivery/drivers/{created.Id}/phone",
            new SetDriverPhoneRequest(created.Id, "01112223334", _dispatcherId));
        changed.EnsureSuccessStatusCode();

        var listed = await _client.GetFromJsonAsync<List<DriverDto>>($"/api/delivery/drivers?actorUserId={_dispatcherId}");
        Assert.Equal("01112223334", Assert.Single(listed!).Phone);

        // Cleared with an empty value; a driver added without one has none.
        (await _client.PutAsJsonAsync($"/api/delivery/drivers/{created.Id}/phone", new SetDriverPhoneRequest(created.Id, "", _dispatcherId))).EnsureSuccessStatusCode();
        Assert.Null((await _client.GetFromJsonAsync<List<DriverDto>>($"/api/delivery/drivers?actorUserId={_dispatcherId}"))!.Single().Phone);
    }

    [Theory]
    [InlineData("مش رقم")]
    [InlineData("0100123456789012345678")]
    public async Task A_phone_that_is_not_a_phone_is_refused(string phone)
    {
        var response = await _client.PostAsJsonAsync("/api/delivery/drivers",
            new CreateDriverRequest($"drv-{Guid.NewGuid():N}", "طيار", _dispatcherId, phone));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Only_a_delivery_manager_changes_a_drivers_phone()
    {
        var created = await (await _client.PostAsJsonAsync("/api/delivery/drivers",
            new CreateDriverRequest($"drv-{Guid.NewGuid():N}", "طيار", _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>();
        var cashier = await _factory.SeedUserAsync("كاشير", "cashier", "pos");

        var response = await _client.PutAsJsonAsync($"/api/delivery/drivers/{created!.Id}/phone", new SetDriverPhoneRequest(created.Id, "01001234567", cashier));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}

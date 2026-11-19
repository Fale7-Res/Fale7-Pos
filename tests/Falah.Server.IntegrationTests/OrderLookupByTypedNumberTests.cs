namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Xunit;

/// <summary>Native UI pass 2026-11-08: "128" typed for order #0128 was "not found" (exact text match
/// only). The lookup now reads the number the way a person types it, and says plainly when it is
/// genuinely absent.</summary>
public class OrderLookupByTypedNumberTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _userId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _userId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task An_order_is_found_by_its_short_number_and_a_missing_one_is_explained()
    {
        var shiftId = (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _userId, 50_000))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
        var productId = await _factory.SeedProductAsync(4_500);
        var sold = await (await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            $"req-{Guid.NewGuid():N}", _userId, shiftId, "takeaway", "cash",
            new[] { new CheckoutItemRequest(productId, null, 1, null, null) }, 0, 0, 0, null, null, null, 4_500, null, null,
            null, null, null, null, null, null))).Content.ReadFromJsonAsync<CheckoutOrderResult>();

        var digits = sold!.OrderNumber.TrimStart('#').TrimStart('0');
        var arabic = new string(digits.Select(c => (char)('٠' + (c - '0'))).ToArray());
        foreach (var typed in new[] { sold.OrderNumber, digits, "0" + digits, arabic })
        {
            var found = await _client.GetAsync($"/api/orders/lookup?orderNumber={Uri.EscapeDataString(typed)}");
            Assert.True(found.IsSuccessStatusCode, $"'{typed}' -> {(int)found.StatusCode}");
            Assert.Equal(sold.OrderId, (await found.Content.ReadFromJsonAsync<OrderDetailsDto>())!.Id);
        }

        var missing = await _client.GetAsync("/api/orders/lookup?orderNumber=99999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Contains("لا يوجد طلب بالرقم 99999", await missing.Content.ReadAsStringAsync());
    }
}

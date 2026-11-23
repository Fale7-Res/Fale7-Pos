namespace Falah.Server.IntegrationTests;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Falah.Domain.Common;
using Microsoft.EntityFrameworkCore;

/// <summary>Independent audit 2026-11-23, F-06: the order history returned every order ever taken, with its lines,
/// in one response. It now returns one Cairo date range (today when none is given) and a bounded number of rows.</summary>
public class OrderHistoryRangeTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private string _managerId = default!;
    private string _productId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        Assert.StartsWith("FalahTest_", _factory.DatabaseName);
        _managerId = await _factory.SeedUserAsync("مدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        _productId = await _factory.SeedProductAsync(5_000);
        _shiftId = (await (await As().PostAsJsonAsync("/api/shifts/open", new OpenShiftRequest($"open-{Guid.NewGuid():N}", "وردية", _managerId, 0)))
            .Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private HttpClient As()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(_managerId));
        return client;
    }

    private async Task<string> SellAsync()
    {
        var response = await As().PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _managerId, ShiftId: _shiftId, OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(_productId, null, 1, null, null) }, DiscountPiastres: 0, DeliveryFeePiastres: 0,
            CompAmountPiastres: 0, CompApprovalId: null, CompApproverUserId: null, CompReason: null, AmountPaidPiastres: 5_000,
            SplitPayments: null, WalletProvider: null, TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null,
            DeliveryArea: null, BusinessDayId: null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!.OrderId;
    }

    private Task<List<OrderDetailsDto>?> ListAsync(string query) =>
        As().GetFromJsonAsync<List<OrderDetailsDto>>($"/api/orders?actorUserId={_managerId}{query}");

    [Fact]
    public async Task Without_dates_the_history_is_today_and_an_older_range_is_asked_for_explicitly()
    {
        var old = await SellAsync();
        var todays = await SellAsync();
        var tenDaysAgo = BusinessClock.Today().AddDays(-10);
        var (oldStart, _) = BusinessClock.UtcRangeForBusinessDate(tenDaysAgo);
        await _factory.QueryAsync(async db =>
        {
            var order = await db.Orders.FirstAsync(o => o.Id == old);
            order.CreatedAtUtc = oldStart.AddHours(12);
            return await db.SaveChangesAsync();
        });

        var defaultList = await ListAsync("");
        var olderList = await ListAsync($"&from={tenDaysAgo:yyyy-MM-dd}&to={tenDaysAgo:yyyy-MM-dd}");

        Assert.Equal(new[] { todays }, defaultList!.Select(o => o.Id));
        Assert.Equal(new[] { old }, olderList!.Select(o => o.Id));
    }

    [Fact]
    public async Task The_number_of_rows_is_bounded_newest_first()
    {
        await SellAsync();
        var second = await SellAsync();
        var third = await SellAsync();

        var list = await ListAsync("&limit=2");

        Assert.Equal(new[] { third, second }, list!.Select(o => o.Id));
    }
}

namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Shifts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// UI-REQ-001 / UI-REQ-002 across midnight: the restaurant opened its business day yesterday evening and
/// is still trading after midnight. A shift opened now carries today's calendar date but runs in
/// yesterday's business day; so do its orders. The closing paper must name the business day it ran in,
/// and a driver's statement for that business day must hold the after-midnight orders - and today's
/// date must not, because no business day of today's date has been opened.
/// </summary>
public class StatementsAcrossMidnightTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _managerId = default!;

    private static readonly DateOnly Today = BusinessClock.Today();
    private static readonly DateOnly Yesterday = Today.AddDays(-1);

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.FundMainAccountAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        db.BusinessDays.Add(new BusinessDayEntity
        {
            Id = $"day-{Guid.NewGuid():N}", BusinessDate = Yesterday, OpenedBy = "seed", OpenedAtUtc = DateTimeOffset.UtcNow.AddHours(-3),
            Status = "Open", CloseRevision = 0, OpenRequestId = $"seed-{Guid.NewGuid():N}",
        });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private static async Task Ok(Task<HttpResponseMessage> call)
    {
        var r = await call;
        Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.RequestUri?.AbsolutePath} -> {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
    }

    private async Task<string> OpenShiftAsync() =>
        (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "ليلية", _managerId, 50_000))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;

    [Fact]
    public async Task The_closing_paper_names_the_business_day_the_shift_ran_in_not_the_calendar_date()
    {
        var shiftId = await OpenShiftAsync();
        Assert.Equal(Today, await _factory.QueryAsync(db => db.Shifts.Where(s => s.Id == shiftId).Select(s => s.BusinessDate).SingleAsync())); // the shape this test is about

        await Ok(_client.PostAsJsonAsync("/api/shifts/close", new CloseShiftRequest($"close-{Guid.NewGuid():N}", shiftId, _managerId, 50_000, null, null, null)));

        var statement = await _factory.QueryAsync(db => ShiftCloseStatements.LoadFrozenAsync(db, shiftId, default));
        Assert.Equal(Yesterday, statement!.BusinessDate);
        var response = await _client.GetFromJsonAsync<ShiftCloseStatementResponse>($"/api/shifts/{shiftId}/close-statement");
        var paper = string.Join("\n", response!.Paper.Lines.SelectMany(TicketDtoText.Of).Where(t => t is not null));
        Assert.Contains(Yesterday.ToString("dd-MM-yyyy"), paper);
    }

    [Fact]
    public async Task A_drivers_after_midnight_orders_belong_to_the_business_day_they_were_sold_in()
    {
        var shiftId = await OpenShiftAsync();
        await _factory.SeedDeliveryServiceAsync("svc-1", 1_500, "توصيل سريع"); // AUD-01: the zone the order names, at the fee it sends
        var productId = await _factory.SeedProductAsync(10_000);
        var sold = (await (await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _managerId, ShiftId: shiftId,
            OrderType: "delivery", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 1_500, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: "عميل تجريبي", CustomerPhone: "01000000002", DeliveryAddress: "شارع تجريبي 2", DeliveryArea: "المنطقة أ", BusinessDayId: null,
            DeliveryStreet: "شارع 2", DeliveryServiceId: "svc-1", DeliveryServiceName: "توصيل سريع"))).Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
        var driverId = (await (await _client.PostAsJsonAsync("/api/delivery/drivers", new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق الليل", _managerId)))
            .Content.ReadFromJsonAsync<DriverDto>())!.Id;
        var deliveryId = await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == sold.OrderId).Select(d => d.Id).FirstAsync());
        await Ok(_client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _managerId, null, null)));
        await Ok(_client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign", new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driverId, _managerId, null, null)));

        var night = await _client.GetFromJsonAsync<DriverDailyStatementDto>($"/api/delivery/drivers/{driverId}/daily-statement?businessDate={Yesterday:yyyy-MM-dd}");
        Assert.Equal(new[] { sold.OrderNumber }, night!.Orders.Select(o => o.OrderNumber).ToArray());
        Assert.Equal(11_500, night.Totals.OrderTotalPiastres);

        var calendarToday = await _client.GetFromJsonAsync<DriverDailyStatementDto>($"/api/delivery/drivers/{driverId}/daily-statement?businessDate={Today:yyyy-MM-dd}");
        Assert.Empty(calendarToday!.Orders);
    }
}

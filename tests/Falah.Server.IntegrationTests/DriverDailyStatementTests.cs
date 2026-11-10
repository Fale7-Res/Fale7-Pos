namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// UI-REQ-001: one driver's daily statement. Rows are the driver's orders of the selected business
/// day (by the order's own business day, not a timestamp); a reassigned order belongs to the driver
/// who ended with it; another driver's orders never leak in; food, delivery charges, total, prepaid,
/// to-collect, collected and settled stay separate; the payment filter and the paper use the same rows.
/// </summary>
public class DriverDailyStatementTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private HttpClient _agent = default!;
    private string _cashierId = default!;
    private string _dispatcherId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        var agentId = await _factory.SeedUserAsync("وكيل طباعة", "print_agent", "print_agent");
        _agent = _factory.CreateClient();
        _agent.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.TokenFor(agentId));
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _dispatcherId = await _factory.SeedUserAsync("موظف التوصيل", "dispatcher", "delivery_manage");
        await _factory.SeedUserAsync("المدير", "manager", "all");
        _factory.StationUserId = _dispatcherId;
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private static async Task Ok(Task<HttpResponseMessage> call)
    {
        var r = await call;
        Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.RequestUri?.AbsolutePath} -> {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
    }

    private async Task<CheckoutOrderResult> SellAsync(string shiftId, long price, long fee, string method)
    {
        var productId = await _factory.SeedProductAsync(price);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "delivery", PaymentMethod: method,
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: fee, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: method == "cash" ? 0 : price + fee, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: "عميل تجريبي", CustomerPhone: "01000000001", DeliveryAddress: "شارع تجريبي 1", DeliveryArea: "المنطقة أ", BusinessDayId: null,
            DeliveryStreet: "شارع 9", DeliveryServiceId: "svc-1", DeliveryServiceName: "توصيل سريع"));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    private Task<string> DeliveryOf(string orderId) =>
        _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == orderId).Select(d => d.Id).FirstAsync());

    private async Task<string> DriverAsync(string name) =>
        (await (await _client.PostAsJsonAsync("/api/delivery/drivers", new CreateDriverRequest($"req-{Guid.NewGuid():N}", name, _dispatcherId)))
            .Content.ReadFromJsonAsync<DriverDto>())!.Id;

    private async Task DispatchAsync(string orderId, string driverId)
    {
        var d = await DeliveryOf(orderId);
        await Ok(_client.PostAsJsonAsync($"/api/delivery/{d}/ready", new MarkDeliveryReadyRequest(d, _dispatcherId, null, null)));
        await Ok(_client.PostAsJsonAsync($"/api/delivery/{d}/assign", new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", d, driverId, _dispatcherId, null, null)));
    }

    private async Task ResultAsync(string orderId, string result, long? collected) =>
        await Ok(_client.PostAsJsonAsync($"/api/delivery/{await DeliveryOf(orderId)}/result",
            new RecordDeliveryResultRequest($"req-{Guid.NewGuid():N}", await DeliveryOf(orderId), result, collected,
                result == "Delivered" ? null : "customer_unreachable", null, _dispatcherId, null, null, result == "Delivered" ? null : "not_prepared")));

    private async Task<DriverDailyStatementDto> StatementAsync(string driverId, DateOnly date, string? filter = null)
    {
        var r = await _client.GetAsync($"/api/delivery/drivers/{driverId}/daily-statement?businessDate={date:yyyy-MM-dd}{(filter is null ? "" : "&paymentFilter=" + filter)}");
        Assert.True(r.IsSuccessStatusCode, await r.Content.ReadAsStringAsync());
        return (await r.Content.ReadFromJsonAsync<DriverDailyStatementDto>())!;
    }

    [Fact]
    public async Task A_drivers_statement_reconciles_rows_totals_filters_other_drivers_reassignment_settlement_and_paper()
    {
        var shiftId = (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50_000))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
        var date = await _factory.QueryAsync(db => db.BusinessDays.Select(d => d.BusinessDate).FirstAsync());
        var a = await DriverAsync("السائق أ");
        var b = await DriverAsync("السائق ب");

        var o1 = await SellAsync(shiftId, 10_000, 1_500, "cash");
        var o2 = await SellAsync(shiftId, 8_000, 1_500, "card");
        var o3 = await SellAsync(shiftId, 6_000, 1_500, "cash");      // goes to B, then reassigned to A
        var o4 = await SellAsync(shiftId, 5_000, 0, "cash");          // B's own order
        var o5 = await SellAsync(shiftId, 4_000, 1_500, "cash");      // A, fails

        await DispatchAsync(o1.OrderId, a);
        await DispatchAsync(o2.OrderId, a);
        await DispatchAsync(o3.OrderId, b);
        await DispatchAsync(o4.OrderId, b);
        await DispatchAsync(o5.OrderId, a);
        var d3 = await DeliveryOf(o3.OrderId);
        await Ok(_client.PostAsJsonAsync($"/api/delivery/{d3}/reassign",
            new ReassignDeliveryRequest($"req-{Guid.NewGuid():N}", d3, a, "السائق ب تعطل", _dispatcherId, null, null)));

        await ResultAsync(o1.OrderId, "Delivered", o1.TotalPiastres);
        await ResultAsync(o2.OrderId, "Delivered", null);
        await ResultAsync(o3.OrderId, "Delivered", o3.TotalPiastres);
        await ResultAsync(o4.OrderId, "Delivered", o4.TotalPiastres);
        await ResultAsync(o5.OrderId, "Failed", null);

        var before = await StatementAsync(a, date);
        Assert.Equal(new[] { o1.OrderNumber, o2.OrderNumber, o3.OrderNumber, o5.OrderNumber }, before.Orders.Select(r => r.OrderNumber).ToArray());
        Assert.DoesNotContain(before.Orders, r => r.OrderNumber == o4.OrderNumber);              // another driver's order never leaks in
        Assert.Equal("السائق ب", before.Orders.Single(r => r.OrderNumber == o3.OrderNumber).ReassignedFromName);
        // A failed cash-on-delivery order that was never prepared is voided by the delivery result: listed, not a sale.
        var voided = before.Orders.Single(r => r.OrderNumber == o5.OrderNumber);
        Assert.True(voided.Cancelled);
        Assert.Equal(0, voided.CashToCollectPiastres);   // a voided order owes nothing and was prepaid by nobody
        Assert.Equal(0, voided.PrepaidPiastres);
        Assert.Equal(1, before.Totals.CancelledCount);
        Assert.Equal(3, before.Totals.InvoiceCount);
        Assert.Equal(4_500, before.Totals.DeliveryFeesPiastres);                                 // charges to customers, kept apart
        Assert.Equal(o1.TotalPiastres + o2.TotalPiastres + o3.TotalPiastres, before.Totals.OrderTotalPiastres);
        Assert.Equal(before.Totals.OrderTotalPiastres - 4_500, before.Totals.FoodSalesPiastres);
        Assert.Equal(o2.TotalPiastres, before.Totals.PrepaidPiastres);                           // the card order was paid before it left
        Assert.Equal(0, before.Orders.Single(r => r.OrderNumber == o2.OrderNumber).CashToCollectPiastres);
        Assert.Equal(o1.TotalPiastres + o3.TotalPiastres, before.Totals.CollectedPiastres);
        Assert.Equal(before.Totals.CollectedPiastres, before.Totals.CollectedUnsettledPiastres);
        Assert.Equal(1, before.Totals.FailedOrReturnedCount);
        Assert.Equal(before.Orders.Where(r => !r.Cancelled).Sum(r => r.OrderTotalPiastres), before.Totals.OrderTotalPiastres); // totals are the live rows' sums

        var other = await StatementAsync(b, date);
        Assert.Equal(new[] { o4.OrderNumber }, other.Orders.Select(r => r.OrderNumber).ToArray()); // the reassigned order left B

        var cashOnly = await StatementAsync(a, date, "cash");
        Assert.Equal(new[] { o1.OrderNumber, o3.OrderNumber, o5.OrderNumber }, cashOnly.Orders.Select(r => r.OrderNumber).ToArray());
        Assert.Equal(2, cashOnly.Totals.InvoiceCount);

        Assert.Empty((await StatementAsync(a, date.AddDays(1))).Orders);                          // a different business day is a different statement

        // Settle A's custody, then refund part of o1: settled moves, refunds are shown, nothing else changes.
        await Ok(_client.PostAsJsonAsync($"/api/delivery/drivers/{a}/settle",
            new SettleDriverCustodyRequest($"req-{Guid.NewGuid():N}", a, o1.TotalPiastres + o3.TotalPiastres, null, _dispatcherId, null, null)));
        await Ok(_client.PostAsJsonAsync("/api/orders/refund", new RefundOrderRequest($"rf-{Guid.NewGuid():N}", o1.OrderId, 2_000, "صنف ناقص",
            await _factory.QueryAsync(db => db.Users.Where(u => u.Role == "manager").Select(u => u.Id).FirstAsync()), null, null)));
        var after = await StatementAsync(a, date);
        Assert.Equal(after.Totals.CollectedPiastres, after.Totals.CollectedSettledPiastres);
        Assert.Equal(0, after.Totals.CollectedUnsettledPiastres);
        Assert.Equal(2_000, after.Totals.RefundedPiastres);
        Assert.Equal(1, after.Totals.FailedOrReturnedCount);                                      // still says it failed after settlement
        Assert.Equal("Failed", after.Orders.Single(r => r.OrderNumber == o5.OrderNumber).DeliveryStatus);
        Assert.Equal(before.Totals.OrderTotalPiastres, after.Totals.OrderTotalPiastres);         // a refund is shown, never netted into the sale

        // The paper: same rows and totals; a replayed press is the same job.
        var requestId = $"dsp-{Guid.NewGuid():N}";
        var print = new PrintDriverDailyStatementRequest(requestId, date, null, Detailed: true);
        await Ok(_client.PostAsJsonAsync($"/api/delivery/drivers/{a}/daily-statement/print", print));
        await Ok(_client.PostAsJsonAsync($"/api/delivery/drivers/{a}/daily-statement/print", print));
        var job = await _factory.QueryAsync(db => db.PrintJobs.AsNoTracking().SingleAsync(j => j.CreateRequestId == requestId));
        var ticket = await (await _agent.GetAsync($"/api/print-jobs/{job.Id}/ticket")).Content.ReadFromJsonAsync<ComposedTicketDto>();
        var text = string.Join("\n", ticket!.Lines.SelectMany(TicketDtoText.Of).Where(t => t is not null));
        Assert.Contains("كشف يومية سائق - تفصيلي", text);
        Assert.Contains("السائق أ", text);
        Assert.Contains($"{after.Totals.OrderTotalPiastres / 100} ج", text);
        Assert.Contains($"{after.Totals.DeliveryFeesPiastres / 100} ج", text);
        Assert.Contains("خدمة التوصيل (من العملاء)", text);
        foreach (var row in after.Orders) Assert.Contains(row.OrderNumber, text);
        Assert.Contains("أُعيد إسناده من السائق ب", text);
        Assert.DoesNotContain(o4.OrderNumber, text);
    }
}

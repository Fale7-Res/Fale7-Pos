namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A sale that never happens leaves the books, and its food is accounted for the way the operator
/// says it went.
///
/// Two paths left an order's stock deduction in place with nothing to explain it. Cancelling an
/// unconfirmed InstaPay / wallet payment cancelled the order but never touched the ingredients its
/// checkout had taken out of stock. And a cash-on-delivery order whose delivery failed or came back
/// stayed a SALE - its value in gross and net sales, its cash "with the driver" forever, its open
/// custody blocking the business-day close. The owner's decision for both: the operator says each
/// time whether the food was prepared. Prepared: it was used and wasted - the usage becomes waste at
/// cost, stock unchanged. Not prepared: the ingredients go back into stock.
///
/// Also here, because it reads the same stock balance: the dashboard's low-stock check summed
/// movement quantities without their direction, so every usage and every waste RAISED the balance it
/// tested against the minimum.
/// </summary>
public class OrderVoidDispositionTests : IAsyncLifetime
{
    private const long QuantityPerItem = 250;   // milli-units of the ingredient per item sold

    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _dispatcherId = default!;
    private string _managerId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        var database = await _factory.QueryAsync(db => Task.FromResult(db.Database.GetDbConnection().Database));
        Assert.StartsWith("FalahTest_", database);

        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _dispatcherId = await _factory.SeedUserAsync("موظف التوصيل", "dispatcher", "delivery_manage");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        // Actorless control-center reads come from the manager station; mutations keep their named session.
        _factory.StationUserId = _managerId;
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var shift = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 0))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = shift!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    // ---------- FIN-0051: cancelling a cash-on-delivery order before it leaves (owner decision 2026-11-22) ----------

    private Task<HttpResponseMessage> CancelUndispatchedAsync(string orderId, string actorId, string? disposition,
        string? approvalId = null, string? approverId = null, string? requestId = null) =>
        _client.PostAsJsonAsync("/api/orders/cancel-undispatched-delivery", new CancelUndispatchedDeliveryRequest(
            requestId ?? $"cud-{Guid.NewGuid():N}", orderId, "العميل لغى الطلب", actorId, disposition, approvalId, approverId));

    private Task<string> DeliveryStatusAsync(string orderId) =>
        _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == orderId).Select(d => d.Status).FirstAsync());

    [Fact]
    public async Task A_manager_cancels_a_cash_on_delivery_order_that_has_not_left_and_its_food_goes_back()
    {
        var (orderId, itemId) = await SellAsync("cash", "delivery");
        Assert.Equal(-QuantityPerItem, await StockAsync(itemId));

        var requestId = $"cud-{Guid.NewGuid():N}";
        var response = await CancelUndispatchedAsync(orderId, _managerId, "not_prepared", requestId: requestId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("cancelled", await OrderStatusAsync(orderId));
        Assert.Equal("Cancelled", await DeliveryStatusAsync(orderId));
        Assert.Equal(0, await StockAsync(itemId));

        var replay = await (await CancelUndispatchedAsync(orderId, _managerId, "not_prepared", requestId: requestId))
            .Content.ReadFromJsonAsync<CancelUndispatchedDeliveryResult>();
        Assert.True(replay!.WasReplayed);
        Assert.Equal(0, await StockAsync(itemId));
        Assert.Equal(0, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == orderId)));
    }

    [Fact]
    public async Task Without_the_permission_a_cancellation_needs_a_managers_approval()
    {
        var (orderId, itemId) = await SellAsync("cash", "delivery");

        var refused = await CancelUndispatchedAsync(orderId, _dispatcherId, "prepared");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.NotEqual("cancelled", await OrderStatusAsync(orderId));

        var issued = await _factory.PostAsSessionOfAsync(_client, _dispatcherId, "/api/approvals/issue",
            new Falah.Application.Approvals.IssueManagerApprovalRequest(_factory.PinFor(_managerId), "delivery_override", "إلغاء طلب لم يخرج"));
        issued.EnsureSuccessStatusCode();
        var approvalId = (await issued.Content.ReadFromJsonAsync<Falah.Application.Approvals.IssueManagerApprovalResult>())!.ApprovalId;

        var approved = await CancelUndispatchedAsync(orderId, _dispatcherId, "prepared", approvalId, _managerId);
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        Assert.Equal("cancelled", await OrderStatusAsync(orderId));
        Assert.Equal(QuantityPerItem, await WasteAsync(itemId));
    }

    [Fact]
    public async Task An_order_already_with_a_driver_is_not_cancelled_this_way()
    {
        var (orderId, _) = await SellAsync("cash", "delivery");
        await DispatchAsync(orderId);

        var response = await CancelUndispatchedAsync(orderId, _managerId, "prepared");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.NotEqual("cancelled", await OrderStatusAsync(orderId));
    }

    [Fact]
    public async Task A_paid_delivery_order_is_refunded_not_cancelled_this_way()
    {
        var (orderId, _) = await SellAsync("card", "delivery");

        var response = await CancelUndispatchedAsync(orderId, _managerId, "prepared");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.NotEqual("cancelled", await OrderStatusAsync(orderId));
    }

    // ---------- C10: cancelling an unconfirmed electronic payment ----------

    [Fact]
    public async Task Cancelling_an_unprepared_electronic_order_puts_its_ingredients_back()
    {
        var (orderId, itemId) = await SellAsync("instapay", "takeaway");
        Assert.Equal(-QuantityPerItem, await StockAsync(itemId));

        var response = await CancelPendingAsync(orderId, "not_prepared");

        response.EnsureSuccessStatusCode();
        Assert.Equal(0, await StockAsync(itemId));
        Assert.Equal(0, await WasteAsync(itemId));
    }

    [Fact]
    public async Task Cancelling_a_prepared_electronic_order_records_its_ingredients_as_waste()
    {
        var (orderId, itemId) = await SellAsync("instapay", "takeaway");

        (await CancelPendingAsync(orderId, "prepared")).EnsureSuccessStatusCode();

        // The food was made and thrown away: still out of stock, now accounted as waste, not a sale.
        Assert.Equal(-QuantityPerItem, await StockAsync(itemId));
        Assert.Equal(QuantityPerItem, await WasteAsync(itemId));
    }

    [Fact]
    public async Task Cancelling_without_saying_whether_the_food_was_prepared_is_refused()
    {
        var (orderId, itemId) = await SellAsync("instapay", "takeaway");

        var response = await CancelPendingAsync(orderId, null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("pending", await _factory.QueryAsync(db => db.Orders.Where(o => o.Id == orderId).Select(o => o.ElectronicPaymentStatus!).FirstAsync()));
        Assert.Equal(-QuantityPerItem, await StockAsync(itemId));
    }

    // ---------- C7: a cash-on-delivery order that is never delivered ----------

    [Theory]
    [InlineData("Failed", "customer_unreachable")]
    [InlineData("Returned", "رفض العميل الاستلام")]
    public async Task An_undelivered_cash_order_is_voided_its_custody_closed_and_its_food_restored(string result, string reason)
    {
        var (orderId, itemId) = await SellAsync("cash", "delivery");
        var deliveryId = await DispatchAsync(orderId);

        var response = await RecordResultAsync(deliveryId, result, reason, "not_prepared");

        response.EnsureSuccessStatusCode();
        Assert.Equal("cancelled", await OrderStatusAsync(orderId));
        Assert.Equal(0, await StockAsync(itemId));

        var custody = await _factory.QueryAsync(db => db.DriverCustodies.AsNoTracking().FirstAsync(c => c.OrderId == orderId));
        Assert.Equal("Settled", custody.Status);
        Assert.Equal(0, custody.ExpectedPiastres);

        // Not a sale, not money with a driver, not a reason the day cannot close.
        var sales = await _client.GetFromJsonAsync<SalesReportDto>(
            $"/api/reports/sales?actorUserId={_managerId}&businessDate={BusinessClock.Today():yyyy-MM-dd}");
        Assert.Equal(0, sales!.GrossSalesPiastres);
        Assert.Equal(0, sales.CashWithDriversPiastres);
        var dayId = await _factory.QueryAsync(db => db.BusinessDays.Select(d => d.Id).FirstAsync());
        var blockers = await _client.GetStringAsync($"/api/business-day/{dayId}/close-blockers");
        Assert.DoesNotContain("UNSETTLED_DRIVER_CUSTODY", blockers);
    }

    [Fact]
    public async Task An_undelivered_cash_order_whose_food_was_made_records_it_as_waste()
    {
        var (orderId, itemId) = await SellAsync("cash", "delivery");
        var deliveryId = await DispatchAsync(orderId);

        (await RecordResultAsync(deliveryId, "Returned", "رجع الطلب", "prepared")).EnsureSuccessStatusCode();

        Assert.Equal("cancelled", await OrderStatusAsync(orderId));
        Assert.Equal(-QuantityPerItem, await StockAsync(itemId));
        Assert.Equal(QuantityPerItem, await WasteAsync(itemId));
    }

    [Fact]
    public async Task An_undelivered_cash_order_needs_the_operator_to_say_whether_the_food_was_made()
    {
        var (orderId, _) = await SellAsync("cash", "delivery");
        var deliveryId = await DispatchAsync(orderId);

        var response = await RecordResultAsync(deliveryId, "Failed", "customer_unreachable", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.NotEqual("cancelled", await OrderStatusAsync(orderId));
    }

    // ---------- REP4: the voids report ----------

    [Fact]
    public async Task Every_void_of_the_day_is_listed_with_its_kind_value_reason_and_what_became_of_the_food()
    {
        var (electronic, _) = await SellAsync("instapay", "takeaway");
        (await CancelPendingAsync(electronic, "prepared")).EnsureSuccessStatusCode();
        var (cod, _) = await SellAsync("cash", "delivery");
        var deliveryId = await DispatchAsync(cod);
        (await RecordResultAsync(deliveryId, "Returned", "رجع الطلب", "not_prepared")).EnsureSuccessStatusCode();

        var report = await _client.GetFromJsonAsync<VoidsReportDto>(
            $"/api/reports/voids?actorUserId={_managerId}&businessDate={BusinessClock.Today():yyyy-MM-dd}");

        Assert.Equal(2, report!.Count);
        Assert.Equal(20000, report.TotalPiastres);
        var e = Assert.Single(report.Voids, v => v.OrderId == electronic);
        Assert.Equal("electronic_unconfirmed", e.Kind);
        Assert.Equal("prepared", e.StockDisposition);
        var d = Assert.Single(report.Voids, v => v.OrderId == cod);
        Assert.Equal("undelivered_cash_on_delivery", d.Kind);
        Assert.Equal("not_prepared", d.StockDisposition);
        Assert.Equal("رجع الطلب", d.Reason);

        var statement = await _client.GetFromJsonAsync<DailyStatementDto>(
            $"/api/reports/daily-statement?actorUserId={_managerId}&businessDate={BusinessClock.Today():yyyy-MM-dd}");
        Assert.Equal(2, statement!.Voids!.Count);
    }

    // ---------- C13: the dashboard's low-stock check ----------

    [Fact]
    public async Task The_dashboards_low_stock_check_subtracts_what_was_used()
    {
        var itemId = await SeedItemAsync(minimumMilliUnits: 1000);
        await SeedStockMovementAsync(itemId, "ManualReceipt", "In", 2000);
        await SeedStockMovementAsync(itemId, "Usage", "Out", 1500);   // 500 left, below the 1000 minimum

        var dashboard = await _client.GetFromJsonAsync<OwnerDashboardDto>(
            $"/api/reports/owner-dashboard?actorUserId={_managerId}&businessDate={BusinessClock.Today():yyyy-MM-dd}");

        var low = Assert.Single(dashboard!.Stock.LowStockItems, i => i.Id == itemId);
        Assert.Equal(500, low.BalanceMilliUnits);
    }

    // ---------- Helpers ----------

    private async Task<(string OrderId, string InventoryItemId)> SellAsync(string paymentMethod, string orderType)
    {
        await _factory.SeedDeliveryServiceAsync("svc-1", 0, "توصيل"); // AUD-01: the zone the order names, at the fee it sends
        var (productId, _, itemId) = await _factory.SeedProductWithRecipeAsync(10000, QuantityPerItem);
        var isDelivery = orderType == "delivery";
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: orderType, PaymentMethod: paymentMethod,
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: paymentMethod == "cash" && isDelivery ? 0 : 10000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: isDelivery ? "عميل" : null, CustomerPhone: isDelivery ? "01000000000" : null,
            DeliveryAddress: null, DeliveryArea: isDelivery ? "المنطقة أ" : null, BusinessDayId: null,
            DeliveryStreet: isDelivery ? "شارع 9" : null, DeliveryServiceId: isDelivery ? "svc-1" : null,
            DeliveryServiceName: isDelivery ? "توصيل" : null));
        response.EnsureSuccessStatusCode();
        return ((await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!.OrderId, itemId);
    }

    /// <summary>Ready, then out with a driver - the state a delivery fails or comes back from.</summary>
    private async Task<string> DispatchAsync(string orderId)
    {
        var deliveryId = await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == orderId).Select(d => d.Id).FirstAsync());
        var driver = await (await _client.PostAsJsonAsync("/api/delivery/drivers",
            new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق", _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>();
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready",
            new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign",
            new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driver!.Id, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        return deliveryId;
    }

    private Task<HttpResponseMessage> RecordResultAsync(string deliveryId, string result, string reason, string? disposition) =>
        _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result",
            new RecordDeliveryResultRequest($"req-{Guid.NewGuid():N}", deliveryId, result, null, reason, null, _dispatcherId, null, null,
                StockDisposition: disposition));

    private Task<HttpResponseMessage> CancelPendingAsync(string orderId, string? disposition) =>
        _client.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment",
            new CancelPendingElectronicPaymentRequest(orderId, "لم يصل التحويل", _cashierId, StockDisposition: disposition));

    private Task<string> OrderStatusAsync(string orderId) =>
        _factory.QueryAsync(db => db.Orders.Where(o => o.Id == orderId).Select(o => o.Status).FirstAsync());

    /// <summary>The direction-signed balance, the way the inventory screen computes it.</summary>
    private Task<long> StockAsync(string itemId) =>
        _factory.QueryAsync(db => db.StockMovements.Where(m => m.InventoryItemId == itemId)
            .SumAsync(m => m.Direction == "In" ? m.QuantityMilliUnits : -m.QuantityMilliUnits));

    private Task<long> WasteAsync(string itemId) =>
        _factory.QueryAsync(db => db.StockMovements.Where(m => m.InventoryItemId == itemId && m.Type == "Waste")
            .SumAsync(m => (long?)m.QuantityMilliUnits ?? 0));

    private async Task<string> SeedItemAsync(long minimumMilliUnits)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var id = $"inv-{Guid.NewGuid():N}";
        db.InventoryItems.Add(new InventoryItemEntity
        {
            Id = id, Name = "طماطم", CostSource = "Purchase", Category = "خامات", Unit = "kg",
            MinimumMilliUnits = minimumMilliUnits, Active = true,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task SeedStockMovementAsync(string itemId, string type, string direction, long quantity)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        db.StockMovements.Add(new StockMovementEntity
        {
            IdempotencyKey = $"seed-{Guid.NewGuid():N}", InventoryItemId = itemId, Type = type, Direction = direction,
            QuantityMilliUnits = quantity, ReferenceType = "seed", ReferenceId = "seed", Reason = "seed",
            CreatedBy = "seed", CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}

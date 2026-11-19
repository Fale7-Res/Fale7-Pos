namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// AUD-02: a delivery fee is part of what the customer paid (net sales), and the driver keeps the whole fee of every
/// delivery he completes (owner rule, 2026-11-08). Counting the fee as revenue without counting the driver's pay as a
/// cost overstated the operating result by every completed delivery's fee.
///
/// The scenario, in piastres (100 = 1 EGP): one product at 10,000 whose recipe costs 3,000; zone fee 2,000; three
/// cash-on-delivery orders, two delivered and one still out with the driver.
/// - Net sales = 3 x 12,000 = 36,000. Food cost = 3 x 3,000 = 9,000.
/// - Driver pay earned = 2 x 2,000 = 4,000 (the delivered ones). Still to be earned = 2,000 (the one out).
/// - Operating result = 36,000 - 9,000 - 4,000 = 23,000 (was 27,000).
/// </summary>
public class OwnerProfitabilityDeliveryCostTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _dispatcherId = default!;
    private string _managerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own");
        _dispatcherId = await _factory.SeedUserAsync("موظف التوصيل", "dispatcher", "delivery_manage");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        await _factory.SeedDeliveryServiceAsync("svc-7", 2_000, "الحي السابع");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private static async Task<HttpResponseMessage> Ok(Task<HttpResponseMessage> call)
    {
        var r = await call;
        Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.RequestUri?.AbsolutePath} -> {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return r;
    }

    private async Task<string> DeliveryOrderAsync(string shiftId, string productId)
    {
        var checkout = (await (await Ok(_client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "delivery", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 2_000, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: "عميل", CustomerPhone: "01000000000", DeliveryAddress: null, DeliveryArea: "الحي السابع",
            BusinessDayId: null, DeliveryStreet: "شارع المدارس", DeliveryServiceId: "svc-7", DeliveryServiceName: "الحي السابع"))))
            .Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
        return await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == checkout.OrderId).Select(d => d.Id).FirstAsync());
    }

    private async Task DispatchAsync(string deliveryId, string driverId)
    {
        await Ok(_client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null)));
        await Ok(_client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign",
            new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driverId, _dispatcherId, null, null)));
    }

    private Task DeliveredAsync(string deliveryId) => Ok(_client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result",
        new RecordDeliveryResultRequest($"req-{Guid.NewGuid():N}", deliveryId, "delivered", null, null, null, _dispatcherId, null, null, null)));

    [Fact]
    public async Task The_operating_result_counts_the_drivers_pay_for_completed_deliveries_as_a_cost()
    {
        var (productId, _, itemId) = await _factory.SeedProductWithRecipeAsync(10_000, 1_000); // 1 kg per sandwich
        await _factory.QueryAsync(db => db.InventoryItems.Where(i => i.Id == itemId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.EstimatedUnitCostPiastres, 3_000L)));  // 30 EGP per kg

        var shiftId = (await (await Ok(_client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 0)))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
        var driverId = (await (await Ok(_client.PostAsJsonAsync("/api/delivery/drivers",
            new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق", _dispatcherId)))).Content.ReadFromJsonAsync<DriverDto>())!.Id;

        var first = await DeliveryOrderAsync(shiftId, productId);
        var second = await DeliveryOrderAsync(shiftId, productId);
        var stillOut = await DeliveryOrderAsync(shiftId, productId);
        foreach (var d in new[] { first, second, stillOut }) await DispatchAsync(d, driverId);
        await DeliveredAsync(first);
        await DeliveredAsync(second);

        var dashboard = (await _client.GetFromJsonAsync<OwnerDashboardDto>(
            $"/api/reports/owner-dashboard?actorUserId={_managerId}&businessDate={BusinessClock.Today():yyyy-MM-dd}"))!;
        var p = dashboard.Profitability;

        Assert.Equal(36_000, p.NetSalesPiastres);
        Assert.Equal(9_000, p.KnownFoodCostPiastres);
        Assert.Equal(4_000, p.DriverCompensationPiastres);
        Assert.Equal(2_000, p.DriverCompensationPendingPiastres);
        Assert.Equal(1, p.DriverCompensationPendingCount);
        Assert.Equal(23_000, p.OperatingResultPiastres);
    }

    /// <summary>Delivers one 12,000 order (10,000 + 2,000 fee; food 3,000) collecting only 10,000, approved by a manager.</summary>
    private async Task<string> ShortPaidDeliveryAsync()
    {
        var (productId, _, itemId) = await _factory.SeedProductWithRecipeAsync(10_000, 1_000);
        await _factory.QueryAsync(db => db.InventoryItems.Where(i => i.Id == itemId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.EstimatedUnitCostPiastres, 3_000L)));
        var shiftId = (await (await Ok(_client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 0)))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
        var driverId = (await (await Ok(_client.PostAsJsonAsync("/api/delivery/drivers",
            new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق", _dispatcherId)))).Content.ReadFromJsonAsync<DriverDto>())!.Id;
        var deliveryId = await DeliveryOrderAsync(shiftId, productId);
        await DispatchAsync(deliveryId, driverId);

        var approval = (await (await Ok(_factory.PostAsSessionOfAsync(_client, _dispatcherId, "/api/approvals/issue",
            new Falah.Application.Approvals.IssueManagerApprovalRequest(_factory.PinFor(_managerId), "delivery_settlement_approve", "العميل دفع 100 فقط"))))
            .Content.ReadFromJsonAsync<Falah.Application.Approvals.IssueManagerApprovalResult>())!;
        await Ok(_client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result",
            new RecordDeliveryResultRequest($"req-{Guid.NewGuid():N}", deliveryId, "delivered", 10_000, "العميل دفع 100 فقط", null, _dispatcherId,
                approval.ApprovalId, approval.ApproverUserId, null)));
        return deliveryId;
    }

    private async Task<OwnerDashboardDto> DashboardAsync() =>
        (await _client.GetFromJsonAsync<OwnerDashboardDto>(
            $"/api/reports/owner-dashboard?actorUserId={_managerId}&businessDate={BusinessClock.Today():yyyy-MM-dd}"))!;

    private Task<HttpResponseMessage> ClassifyAsync(string deliveryId, string classification, string actorId, string? evidence = "إيصال رقم 7 - موافقة المالك هاتفيًا",
        string? requestId = null, string? approvalId = null, string? approverId = null) =>
        _factory.PostAsSessionOfAsync(_client, actorId, $"/api/delivery/{deliveryId}/collection-shortfall/classify",
            new ClassifyCollectionShortfallRequest(requestId ?? $"cls-{Guid.NewGuid():N}", deliveryId, classification, evidence!, actorId, approvalId, approverId));

    /// <summary>
    /// AUD-03: the customer paid 10,000 of the 12,000 due, and a manager approved the delivery result. Net sales still
    /// say 12,000. The 2,000 is not allowed to vanish - but until a manager says what it is, it is NOT a loss either:
    /// it is reported as unresolved, beside the result, not inside it.
    /// Operating = 12,000 - 3,000 food - 2,000 driver pay = 7,000, with 2,000 unresolved shown.
    /// </summary>
    [Fact]
    public async Task An_unclassified_shortfall_is_reported_as_unresolved_and_not_as_a_loss()
    {
        var deliveryId = await ShortPaidDeliveryAsync();

        var dashboard = await DashboardAsync();
        Assert.Equal(12_000, dashboard.Cash.NetSalesPiastres);
        Assert.Equal(2_000, dashboard.Leakage.DeliveryCollectionShortfallPiastres);
        Assert.Equal(1, dashboard.Leakage.DeliveryCollectionShortfallCount);
        Assert.Equal(2_000, dashboard.Leakage.DeliveryShortfallUnresolvedPiastres);
        Assert.Equal(0, dashboard.Profitability.DeliveryCollectionShortfallPiastres);
        Assert.Equal(2_000, dashboard.Profitability.DeliveryShortfallUnresolvedPiastres);
        Assert.Equal(7_000, dashboard.Profitability.OperatingResultPiastres);

        var outcomes = (await _client.GetFromJsonAsync<DeliveryOutcomesReportDto>(
            $"/api/reports/delivery-outcomes?actorUserId={_managerId}&from={BusinessClock.Today():yyyy-MM-dd}&to={BusinessClock.Today():yyyy-MM-dd}"))!;
        Assert.Equal(2_000, outcomes.CollectionShortfallPiastres);

        var list = (await _client.GetFromJsonAsync<CollectionShortfallListDto>($"/api/delivery/collection-shortfalls?actorUserId={_managerId}"))!;
        Assert.Equal((2_000L, 1), (list.UnresolvedPiastres, list.UnresolvedCount));
        Assert.Equal("unresolved", Assert.Single(list.Items, i => i.DeliveryId == deliveryId).Status);
    }

    [Fact]
    public async Task A_write_off_approved_with_evidence_is_deducted_once_and_cannot_be_reclassified()
    {
        var deliveryId = await ShortPaidDeliveryAsync();
        var requestId = $"cls-{Guid.NewGuid():N}";

        var classified = await Ok(ClassifyAsync(deliveryId, "write_off", _managerId, requestId: requestId));
        var dto = (await classified.Content.ReadFromJsonAsync<CollectionShortfallDto>())!;
        Assert.Equal(("classified", "write_off", 2_000L), (dto.Status, dto.Classification, dto.ShortfallPiastres));

        var dashboard = await DashboardAsync();
        Assert.Equal(2_000, dashboard.Profitability.DeliveryCollectionShortfallPiastres);
        Assert.Equal(0, dashboard.Profitability.DeliveryShortfallUnresolvedPiastres);
        Assert.Equal(5_000, dashboard.Profitability.OperatingResultPiastres);

        // A retry replays; a different decision on the same shortfall is refused.
        var replay = (await (await Ok(ClassifyAsync(deliveryId, "write_off", _managerId, requestId: requestId))).Content.ReadFromJsonAsync<CollectionShortfallDto>())!;
        Assert.True(replay.WasReplayed);
        Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity, (await ClassifyAsync(deliveryId, "customer_receivable", _managerId)).StatusCode);
        Assert.Equal(1, await _factory.QueryAsync(db => db.CollectionShortfallResolutions.CountAsync()));
        Assert.True(await _factory.QueryAsync(db => db.AuditEvents.AnyAsync(a => a.ReferenceId == deliveryId && a.Action == "تصنيف فرق تحصيل")));
    }

    [Fact]
    public async Task Money_still_owed_is_a_receivable_not_a_loss()
    {
        var deliveryId = await ShortPaidDeliveryAsync();
        await Ok(ClassifyAsync(deliveryId, "customer_receivable", _managerId));

        var dashboard = await DashboardAsync();
        Assert.Equal(0, dashboard.Profitability.DeliveryCollectionShortfallPiastres);
        Assert.Equal(2_000, dashboard.Profitability.DeliveryShortfallReceivablePiastres);
        Assert.Equal(7_000, dashboard.Profitability.OperatingResultPiastres);

        var list = (await _client.GetFromJsonAsync<CollectionShortfallListDto>($"/api/delivery/collection-shortfalls?actorUserId={_managerId}"))!;
        Assert.Equal((0L, 0, 2_000L), (list.UnresolvedPiastres, list.UnresolvedCount, list.ReceivablePiastres));
    }

    [Fact]
    public async Task A_classification_needs_evidence_and_a_managers_authority()
    {
        var deliveryId = await ShortPaidDeliveryAsync();

        Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity, (await ClassifyAsync(deliveryId, "write_off", _managerId, evidence: " ")).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity, (await ClassifyAsync(deliveryId, "lost", _managerId)).StatusCode);
        // The dispatcher holds delivery_manage, not delivery_settlement_approve: without a manager's PIN it is refused.
        Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity, (await ClassifyAsync(deliveryId, "write_off", _dispatcherId)).StatusCode);
        Assert.Equal(0, await _factory.QueryAsync(db => db.CollectionShortfallResolutions.CountAsync()));

        var approval = (await (await Ok(_factory.PostAsSessionOfAsync(_client, _dispatcherId, "/api/approvals/issue",
            new Falah.Application.Approvals.IssueManagerApprovalRequest(_factory.PinFor(_managerId), "delivery_settlement_approve", "خصم"))))
            .Content.ReadFromJsonAsync<Falah.Application.Approvals.IssueManagerApprovalResult>())!;
        var approved = (await (await Ok(ClassifyAsync(deliveryId, "approved_discount", _dispatcherId, approvalId: approval.ApprovalId, approverId: approval.ApproverUserId)))
            .Content.ReadFromJsonAsync<CollectionShortfallDto>())!;
        Assert.Equal("المدير", approved.ApprovedBy);
    }
}

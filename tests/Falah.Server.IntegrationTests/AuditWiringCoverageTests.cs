namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Audit;
using Falah.Application.Delivery;
using Falah.Application.Inventory;
using Falah.Application.Orders;
using Falah.Application.Purchasing;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Proves the remaining sensitive-operation categories the general Audit Log was extended to cover
/// this pass: checkout comp/hospitality, the full delivery lifecycle (assign/reassign/result/
/// settlement), inventory adjustments/waste, purchasing (receive/pay), and print reprints. Each of
/// these already had its own narrower, feature-specific trail (DeliveryEvent, StockMovement's own
/// ApprovedBy, PrintJobAttempt) before this pass - this proves the UNIFIED cross-cutting view now
/// also captures them, per the explicit instruction to complete that wiring.
/// </summary>
public class AuditWiringCoverageTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _dispatcherId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "inventory_manage");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _dispatcherId = await _factory.SeedUserAsync("موظف التوصيل", "dispatcher", "delivery_manage");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<List<AuditEventDto>> ListEventsAsync(string? category = null) =>
        (await (await _client.GetAsync($"/api/audit/events?actorUserId={_managerId}" + (category is null ? "" : $"&category={category}")))
            .Content.ReadFromJsonAsync<List<AuditEventDto>>())!;

    // ---- Checkout comp ----

    [Fact]
    public async Task A_comp_checkout_is_audited_as_critical()
    {
        var productId = await _factory.SeedProductAsync(10000);
        var approvalId = (await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new Falah.Application.Approvals.IssueManagerApprovalRequest(_factory.PinFor(_managerId), "orders_comp", "ضيافة عميل مميز")))
            .Content.ReadFromJsonAsync<Falah.Application.Approvals.IssueManagerApprovalResult>())!.ApprovalId;

        var checkout = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 10000,
            CompApprovalId: approvalId, CompApproverUserId: _managerId, CompReason: "ضيافة عميل مميز",
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null,
            CompReasonCode: "hospitality"));
        checkout.EnsureSuccessStatusCode();

        var events = await ListEventsAsync(category: "order");
        Assert.Contains(events, e => e.Action.Contains("ضيافة") && e.Severity == "critical");
    }

    // ---- Delivery lifecycle ----

    [Fact]
    public async Task Assigning_reassigning_and_recording_a_delivery_result_are_all_audited()
    {
        var productId = await _factory.SeedProductAsync(10000);
        var checkout = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "delivery", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            CustomerName: "عميل", CustomerPhone: "01000000000", DeliveryAddress: "شارع", DeliveryArea: "منطقة", BusinessDayId: null,
            DeliveryStreet: "شارع 1", DeliveryServiceId: "svc-1", DeliveryServiceName: "توصيل"));
        checkout.EnsureSuccessStatusCode();
        var order = await checkout.Content.ReadFromJsonAsync<CheckoutOrderResult>();
        var deliveryId = await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == order!.OrderId).Select(d => d.Id).FirstAsync());

        var driver1 = await (await _client.PostAsJsonAsync("/api/delivery/drivers", new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق أول", _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>();
        var driver2 = await (await _client.PostAsJsonAsync("/api/delivery/drivers", new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق ثاني", _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>();

        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign", new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driver1!.Id, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/reassign", new ReassignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driver2!.Id, "السائق الأول مشغول", _dispatcherId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result", new RecordDeliveryResultRequest($"req-{Guid.NewGuid():N}", deliveryId, "delivered", 10000, null, null, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/delivery/drivers/{driver2.Id}/settle", new SettleDriverCustodyRequest($"req-{Guid.NewGuid():N}", driver2.Id, 10000, null, _dispatcherId, null, null))).EnsureSuccessStatusCode();

        var orderEvents = await ListEventsAsync(category: "order");
        Assert.Contains(orderEvents, e => e.Action.Contains("تعيين سائق") && e.ReferenceId == deliveryId);
        Assert.Contains(orderEvents, e => e.Action.Contains("نقل طلب") && e.ReferenceId == deliveryId);
        Assert.Contains(orderEvents, e => e.Action.Contains("نتيجة توصيل") && e.ReferenceId == deliveryId);

        var cashEvents = await ListEventsAsync(category: "cash");
        Assert.Contains(cashEvents, e => e.Action.Contains("تسوية عهدة سائق") && e.ReferenceId == driver2.Id);
    }

    // ---- Inventory ----

    [Fact]
    public async Task Count_adjustments_and_waste_are_audited()
    {
        var itemId = (await (await _client.PostAsJsonAsync("/api/inventory/items",
            new CreateInventoryItemRequest($"req-{Guid.NewGuid():N}", "طماطم", "خامات", "kg", 0, false, "Purchase", null, _cashierId)))
            .Content.ReadFromJsonAsync<CreateInventoryItemResult>())!.Id;

        (await _client.PostAsJsonAsync("/api/inventory/count-adjustments",
            new RecordCountAdjustmentRequest($"req-{Guid.NewGuid():N}", itemId, 500, "in", "جرد شهري", _cashierId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync("/api/inventory/waste",
            new RecordWasteRequest($"req-{Guid.NewGuid():N}", itemId, 200, "Waste", "خطأ تحضير", _cashierId, null, null))).EnsureSuccessStatusCode();

        var events = await ListEventsAsync(category: "inventory");
        Assert.Contains(events, e => e.Action.Contains("فرق جرد") && e.ReferenceId == itemId);
        Assert.Contains(events, e => e.Action.Contains("هالك") && e.ReferenceId == itemId);
    }

    // ---- Purchasing ----

    [Fact]
    public async Task Receiving_and_paying_a_purchase_are_audited()
    {
        var supplierId = (await (await _client.PostAsJsonAsync("/api/suppliers",
            new AddSupplierRequest($"req-{Guid.NewGuid():N}", "مورد الاختبار", null, null, null, null, _managerId, null, null)))
            .Content.ReadFromJsonAsync<AddSupplierResult>())!.SupplierId;
        var itemId = (await (await _client.PostAsJsonAsync("/api/inventory/items",
            new CreateInventoryItemRequest($"req-{Guid.NewGuid():N}", "أرز", "خامات", "kg", 0, false, "Purchase", null, _managerId)))
            .Content.ReadFromJsonAsync<CreateInventoryItemResult>())!.Id;

        var purchase = await (await _client.PostAsJsonAsync("/api/purchases", new ReceivePurchaseRequest(
            $"req-{Guid.NewGuid():N}", supplierId, null, new[] { new PurchaseLineRequest(itemId, 10_000, 500) },
            2000, "cash", _managerId, null, null, null))).Content.ReadFromJsonAsync<PurchaseDto>();

        (await _client.PostAsJsonAsync($"/api/purchases/{purchase!.Id}/payments",
            new PayPurchaseRequest($"req-{Guid.NewGuid():N}", purchase.Id, 3000, "cash", _managerId, null, null))).EnsureSuccessStatusCode();

        var events = await ListEventsAsync(category: "cash");
        Assert.Contains(events, e => e.Action.Contains("استلام فاتورة مشتريات") && e.ReferenceId == purchase.Id);
        Assert.Contains(events, e => e.Action.Contains("سداد مورد") && e.ReferenceId == purchase.Id);
    }

    // ---- Printing reprint ----

    [Fact]
    public async Task Only_a_genuine_reprint_is_audited_not_the_original_print()
    {
        var productId = await _factory.SeedProductAsync(10000);
        var checkout = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 10000, SplitPayments: null, WalletProvider: null,
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null));
        checkout.EnsureSuccessStatusCode();
        var order = await checkout.Content.ReadFromJsonAsync<CheckoutOrderResult>();

        await _client.PostAsJsonAsync($"/api/orders/{order!.OrderId}/print-jobs",
            new Falah.Application.Printing.CreatePrintJobsRequest($"preq-{Guid.NewGuid():N}", order.OrderId, "Receipt", false, _cashierId));
        var beforeReprintEvents = await ListEventsAsync(category: "order");
        Assert.DoesNotContain(beforeReprintEvents, e => e.Action.Contains("إعادة طباعة"));

        await _client.PostAsJsonAsync($"/api/orders/{order.OrderId}/print-jobs",
            new Falah.Application.Printing.CreatePrintJobsRequest($"preq-{Guid.NewGuid():N}", order.OrderId, "Receipt", true, _cashierId));
        var afterReprintEvents = await ListEventsAsync(category: "order");
        Assert.Contains(afterReprintEvents, e => e.Action.Contains("إعادة طباعة") && e.ReferenceId == order.OrderId);
    }
}

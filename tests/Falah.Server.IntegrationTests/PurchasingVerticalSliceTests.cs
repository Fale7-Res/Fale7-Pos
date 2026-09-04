namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Inventory;
using Falah.Application.Purchasing;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Ported from Suppliers/Purchases/AP in the reference app's lib/enterprise-store.tsx
/// (addSupplier/receivePurchase/payPurchase) and the pure PurchasingRules already built (but never
/// wired end to end) in the Inventory slice. Deliberately deferred out of that earlier slice as a
/// distinctly separate finance feature - see IMPLEMENTATION_MATRIX.md row 9's note. Covers: supplier
/// authorization (a real gap PurchasingScreen.tsx's own comment documents finding - adding a supplier
/// previously had NO authorization at all), receiving an invoice (stock + treasury + supplier ledger
/// posted together, atomically), and later settling a credit/partially-paid invoice.
/// </summary>
public class PurchasingVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _purchasingManagerId = default!;
    private string _cashierId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateClient();
        _purchasingManagerId = await _factory.SeedUserAsync("مسؤول المشتريات", "supervisor", "purchasing_manage", "inventory_manage");
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> CreateSupplierAsync(string name = "مورد الخامات", string? actorId = null)
    {
        var response = await _client.PostAsJsonAsync("/api/suppliers",
            new AddSupplierRequest($"req-{Guid.NewGuid():N}", name, "01000000000", null, null, null, actorId ?? _purchasingManagerId, null, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AddSupplierResult>())!.SupplierId;
    }

    private async Task<string> CreateInventoryItemAsync(string name = "طماطم")
    {
        var response = await _client.PostAsJsonAsync("/api/inventory/items",
            new CreateInventoryItemRequest($"req-{Guid.NewGuid():N}", name, "خامات", "kg", 0, false, "Purchase", null, _purchasingManagerId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateInventoryItemResult>())!.Id;
    }

    private Task<HttpResponseMessage> ReceiveAsync(string supplierId, string itemId, long quantityMilliUnits, long unitCostPiastres,
            long paidPiastres, string paymentMethod, string? requestId = null, string? invoiceNumber = null, string? actorId = null) =>
        _client.PostAsJsonAsync("/api/purchases", new ReceivePurchaseRequest(
            requestId ?? $"preq-{Guid.NewGuid():N}", supplierId, invoiceNumber,
            new[] { new PurchaseLineRequest(itemId, quantityMilliUnits, unitCostPiastres) },
            paidPiastres, paymentMethod, actorId ?? _purchasingManagerId, null, null, null));

    private Task<HttpResponseMessage> PayAsync(string purchaseId, long amountPiastres, string paymentMethod, string? requestId = null, string? actorId = null) =>
        _client.PostAsJsonAsync($"/api/purchases/{purchaseId}/payments", new PayPurchaseRequest(
            requestId ?? $"payreq-{Guid.NewGuid():N}", purchaseId, amountPiastres, paymentMethod, actorId ?? _purchasingManagerId, null, null));

    // ---- Supplier authorization: a real gap the reference app's own UI comment documents finding ----

    [Fact]
    public async Task Adding_a_supplier_requires_purchasing_manage_or_suppliers_manage()
    {
        var response = await _client.PostAsJsonAsync("/api/suppliers",
            new AddSupplierRequest($"req-{Guid.NewGuid():N}", "مورد بلا صلاحية", null, null, null, null, _cashierId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Suppliers_manage_alone_is_enough_to_add_a_supplier_even_without_purchasing_manage()
    {
        var suppliersManagerId = await _factory.SeedUserAsync("مسؤول الموردين", "supervisor", "suppliers_manage");
        var response = await _client.PostAsJsonAsync("/api/suppliers",
            new AddSupplierRequest($"req-{Guid.NewGuid():N}", "مورد جديد", null, null, null, null, suppliersManagerId, null, null));
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Adding_a_supplier_requires_a_name()
    {
        var response = await _client.PostAsJsonAsync("/api/suppliers",
            new AddSupplierRequest($"req-{Guid.NewGuid():N}", "  ", null, null, null, null, _purchasingManagerId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Retrying_the_same_add_supplier_request_id_never_creates_a_duplicate()
    {
        var requestId = $"req-{Guid.NewGuid():N}";
        var request = new AddSupplierRequest(requestId, "مورد مكرر", null, null, null, null, _purchasingManagerId, null, null);
        var first = await (await _client.PostAsJsonAsync("/api/suppliers", request)).Content.ReadFromJsonAsync<AddSupplierResult>();
        var second = await (await _client.PostAsJsonAsync("/api/suppliers", request)).Content.ReadFromJsonAsync<AddSupplierResult>();
        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(first.SupplierId, second.SupplierId);
    }

    // ---- Receiving a purchase: stock + treasury + supplier ledger posted together ----

    [Fact]
    public async Task Receiving_a_fully_paid_purchase_posts_stock_treasury_and_a_paid_ledger_pair()
    {
        var supplierId = await CreateSupplierAsync();
        var itemId = await CreateInventoryItemAsync();

        var response = await ReceiveAsync(supplierId, itemId, quantityMilliUnits: 10_000, unitCostPiastres: 500, paidPiastres: 5000, "cash");
        response.EnsureSuccessStatusCode();
        var purchase = await response.Content.ReadFromJsonAsync<PurchaseDto>();

        Assert.Equal(5000, purchase!.TotalPiastres); // 10.000 kg * 500 piastres/kg = 5000
        Assert.Equal(5000, purchase.PaidPiastres);
        Assert.Equal(0, purchase.RemainingAmountPiastres);
        Assert.Equal("paid", purchase.PaymentStatus);
        Assert.Equal("tre-main", purchase.PaymentAccountId);

        var stockCount = await _factory.QueryAsync(db => db.StockMovements.CountAsync(m => m.InventoryItemId == itemId && m.Type == "PurchaseReceipt"));
        Assert.Equal(1, stockCount);
        var treasuryMovement = await _factory.QueryAsync(db => db.TreasuryMovements.FirstAsync(m => m.ReferenceId == purchase.Id));
        Assert.Equal("tre-main", treasuryMovement.SourceAccountId);
        Assert.Equal(5000, treasuryMovement.AmountPiastres);

        var supplier = await (await _client.GetAsync($"/api/suppliers/{supplierId}")).Content.ReadFromJsonAsync<SupplierDto>();
        Assert.Equal(5000, supplier!.TotalInvoicedPiastres);
        Assert.Equal(5000, supplier.TotalPaidPiastres);
        Assert.Equal(0, supplier.OutstandingBalancePiastres);
    }

    [Fact]
    public async Task Receiving_a_purchase_on_credit_posts_no_treasury_movement_and_leaves_it_fully_outstanding()
    {
        var supplierId = await CreateSupplierAsync();
        var itemId = await CreateInventoryItemAsync();

        var response = await ReceiveAsync(supplierId, itemId, quantityMilliUnits: 10_000, unitCostPiastres: 500, paidPiastres: 0, "credit");
        var purchase = await response.Content.ReadFromJsonAsync<PurchaseDto>();

        Assert.Equal("unpaid", purchase!.PaymentStatus);
        Assert.Equal("received", purchase.Status);
        Assert.Null(purchase.PaymentAccountId);
        Assert.Equal(5000, purchase.RemainingAmountPiastres);

        var treasuryCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == purchase.Id));
        Assert.Equal(0, treasuryCount);

        // Stock is still received even though nothing was paid - receiving and paying are independent facts.
        var stockCount = await _factory.QueryAsync(db => db.StockMovements.CountAsync(m => m.InventoryItemId == itemId));
        Assert.Equal(1, stockCount);

        var supplier = await (await _client.GetAsync($"/api/suppliers/{supplierId}")).Content.ReadFromJsonAsync<SupplierDto>();
        Assert.Equal(5000, supplier!.OutstandingBalancePiastres);
    }

    [Fact]
    public async Task A_partially_paid_purchase_leaves_the_remainder_outstanding_on_the_supplier_ledger()
    {
        var supplierId = await CreateSupplierAsync();
        var itemId = await CreateInventoryItemAsync();

        var response = await ReceiveAsync(supplierId, itemId, quantityMilliUnits: 10_000, unitCostPiastres: 500, paidPiastres: 2000, "cash");
        var purchase = await response.Content.ReadFromJsonAsync<PurchaseDto>();

        Assert.Equal("partially_paid", purchase!.PaymentStatus);
        Assert.Equal(3000, purchase.RemainingAmountPiastres);
    }

    [Fact]
    public async Task Paying_more_than_the_invoice_total_at_receipt_is_rejected()
    {
        var supplierId = await CreateSupplierAsync();
        var itemId = await CreateInventoryItemAsync();
        var response = await ReceiveAsync(supplierId, itemId, quantityMilliUnits: 10_000, unitCostPiastres: 500, paidPiastres: 5001, "cash");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Receiving_a_purchase_for_an_unknown_supplier_is_rejected()
    {
        var itemId = await CreateInventoryItemAsync();
        var response = await ReceiveAsync("sup-does-not-exist", itemId, 10_000, 500, 0, "credit");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Receiving_a_purchase_with_no_lines_is_rejected()
    {
        var supplierId = await CreateSupplierAsync();
        var response = await _client.PostAsJsonAsync("/api/purchases", new ReceivePurchaseRequest(
            $"preq-{Guid.NewGuid():N}", supplierId, null, Array.Empty<PurchaseLineRequest>(), 0, "credit", _purchasingManagerId, null, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Receiving_a_purchase_without_purchasing_manage_is_rejected()
    {
        var supplierId = await CreateSupplierAsync();
        var itemId = await CreateInventoryItemAsync();
        var response = await ReceiveAsync(supplierId, itemId, 10_000, 500, 0, "credit", actorId: _cashierId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Insufficient_main_treasury_balance_blocks_a_cash_payment_at_receipt()
    {
        var supplierId = await CreateSupplierAsync();
        var itemId = await CreateInventoryItemAsync();
        // The seeded funding is 100,000.00 EGP - ask to pay far more than that at receipt.
        var response = await ReceiveAsync(supplierId, itemId, quantityMilliUnits: 1000, unitCostPiastres: 20_000_000, paidPiastres: 20_000_000, "cash");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Retrying_the_same_receive_purchase_request_id_never_double_posts_stock_or_treasury()
    {
        var supplierId = await CreateSupplierAsync();
        var itemId = await CreateInventoryItemAsync();
        var requestId = $"preq-{Guid.NewGuid():N}";

        var first = await (await ReceiveAsync(supplierId, itemId, 10_000, 500, 5000, "cash", requestId)).Content.ReadFromJsonAsync<PurchaseDto>();
        var second = await (await ReceiveAsync(supplierId, itemId, 10_000, 500, 5000, "cash", requestId)).Content.ReadFromJsonAsync<PurchaseDto>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(first.Id, second.Id);
        var stockCount = await _factory.QueryAsync(db => db.StockMovements.CountAsync(m => m.InventoryItemId == itemId));
        Assert.Equal(1, stockCount);
        var treasuryCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == first.Id));
        Assert.Equal(1, treasuryCount);
        var purchaseCount = await _factory.QueryAsync(db => db.Purchases.CountAsync());
        Assert.Equal(1, purchaseCount);
    }

    [Fact]
    public async Task Concurrent_receive_purchase_requests_with_the_same_request_id_post_exactly_once()
    {
        var supplierId = await CreateSupplierAsync();
        var itemId = await CreateInventoryItemAsync();
        var requestId = $"preq-{Guid.NewGuid():N}";

        var tasks = Enumerable.Range(0, 6).Select(_ => ReceiveAsync(supplierId, itemId, 10_000, 500, 5000, "cash", requestId));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<PurchaseDto>()));
        Assert.Equal(1, results.Count(r => !r!.WasReplayed));

        var stockCount = await _factory.QueryAsync(db => db.StockMovements.CountAsync(m => m.InventoryItemId == itemId));
        Assert.Equal(1, stockCount);
        var purchaseCount = await _factory.QueryAsync(db => db.Purchases.CountAsync());
        Assert.Equal(1, purchaseCount);
    }

    // ---- Paying an outstanding invoice later ----

    [Fact]
    public async Task Paying_off_a_credit_purchase_later_settles_it_and_posts_a_treasury_movement()
    {
        var supplierId = await CreateSupplierAsync();
        var itemId = await CreateInventoryItemAsync();
        var purchase = await (await ReceiveAsync(supplierId, itemId, 10_000, 500, 0, "credit")).Content.ReadFromJsonAsync<PurchaseDto>();

        var payResponse = await PayAsync(purchase!.Id, 5000, "cash");
        payResponse.EnsureSuccessStatusCode();
        var updated = await payResponse.Content.ReadFromJsonAsync<PurchaseDto>();

        Assert.Equal("paid", updated!.PaymentStatus);
        Assert.Equal(0, updated.RemainingAmountPiastres);
        Assert.Equal(5000, updated.PaidPiastres);

        var treasuryMovement = await _factory.QueryAsync(db => db.TreasuryMovements.FirstAsync(m => m.ReferenceId == purchase.Id && m.Type == "purchase_payment"));
        Assert.Equal("tre-main", treasuryMovement.SourceAccountId);

        var supplier = await (await _client.GetAsync($"/api/suppliers/{supplierId}")).Content.ReadFromJsonAsync<SupplierDto>();
        Assert.Equal(0, supplier!.OutstandingBalancePiastres);
    }

    [Fact]
    public async Task Paying_more_than_the_outstanding_amount_is_rejected()
    {
        var supplierId = await CreateSupplierAsync();
        var itemId = await CreateInventoryItemAsync();
        var purchase = await (await ReceiveAsync(supplierId, itemId, 10_000, 500, 2000, "cash")).Content.ReadFromJsonAsync<PurchaseDto>();

        var response = await PayAsync(purchase!.Id, 3001, "bank"); // only 3000 is actually outstanding
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Paying_zero_is_rejected()
    {
        var supplierId = await CreateSupplierAsync();
        var itemId = await CreateInventoryItemAsync();
        var purchase = await (await ReceiveAsync(supplierId, itemId, 10_000, 500, 0, "credit")).Content.ReadFromJsonAsync<PurchaseDto>();

        var response = await PayAsync(purchase!.Id, 0, "cash");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Retrying_the_same_payment_request_id_never_double_pays()
    {
        var supplierId = await CreateSupplierAsync();
        var itemId = await CreateInventoryItemAsync();
        var purchase = await (await ReceiveAsync(supplierId, itemId, 10_000, 500, 0, "credit")).Content.ReadFromJsonAsync<PurchaseDto>();
        var requestId = $"payreq-{Guid.NewGuid():N}";

        var first = await (await PayAsync(purchase!.Id, 2000, "cash", requestId)).Content.ReadFromJsonAsync<PurchaseDto>();
        var second = await (await PayAsync(purchase.Id, 2000, "cash", requestId)).Content.ReadFromJsonAsync<PurchaseDto>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(2000, second.PaidPiastres);
        var treasuryCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == purchase.Id && m.Type == "purchase_payment"));
        Assert.Equal(1, treasuryCount);
    }

    [Fact]
    public async Task Concurrent_payment_requests_with_the_same_request_id_pay_exactly_once()
    {
        var supplierId = await CreateSupplierAsync();
        var itemId = await CreateInventoryItemAsync();
        var purchase = await (await ReceiveAsync(supplierId, itemId, 10_000, 500, 0, "credit")).Content.ReadFromJsonAsync<PurchaseDto>();
        var requestId = $"payreq-{Guid.NewGuid():N}";

        var tasks = Enumerable.Range(0, 6).Select(_ => PayAsync(purchase!.Id, 2000, "cash", requestId));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<PurchaseDto>()));
        Assert.All(results, r => Assert.Equal(2000, r!.PaidPiastres)); // never 6x the paid amount
        var treasuryCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == purchase!.Id && m.Type == "purchase_payment"));
        Assert.Equal(1, treasuryCount);
    }

    [Fact]
    public async Task Paying_a_cancelled_or_unknown_purchase_is_rejected()
    {
        var response = await PayAsync("purchase-does-not-exist", 1000, "cash");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

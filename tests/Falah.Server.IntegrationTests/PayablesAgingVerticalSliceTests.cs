namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Inventory;
using Falah.Application.Purchasing;
using Falah.Domain.Common;
using Xunit;

/// <summary>
/// Supplier due dates and the payables aging report, end to end.
///
/// The behaviour worth guarding is what the report says about an invoice nobody agreed a term for.
/// Most invoices in a real restaurant arrive that way, and a report that quietly treated them as due
/// on arrival would put nearly all of them in an overdue bucket on day one - at which point the
/// overdue figure stops meaning anything and stops being read.
/// </summary>
public class PayablesAgingVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _managerId = default!;
    private string _cashierId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _managerId = await _factory.SeedUserAsync("مسؤول المشتريات", "supervisor", "purchasing_manage", "inventory_manage");
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> CreateSupplierAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/suppliers",
            new AddSupplierRequest($"req-{Guid.NewGuid():N}", name, "01000000000", null, null, null, _managerId, null, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AddSupplierResult>())!.SupplierId;
    }

    private async Task<string> CreateItemAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/inventory/items",
            new CreateInventoryItemRequest($"req-{Guid.NewGuid():N}", name, "خامات", "kg", 0, false, "Purchase", null, _managerId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateInventoryItemResult>())!.Id;
    }

    /// <summary>Receives an entirely unpaid (credit) invoice, so it lands on the payables report.</summary>
    private async Task<PurchaseDto> ReceiveCreditAsync(string supplierId, string itemId, long totalPiastres, DateOnly? dueDate, string invoiceNumber)
    {
        var response = await _client.PostAsJsonAsync("/api/purchases", new ReceivePurchaseRequest(
            $"preq-{Guid.NewGuid():N}", supplierId, invoiceNumber,
            new[] { new PurchaseLineRequest(itemId, 1000, totalPiastres) },
            0, "credit", _managerId, null, null, null, dueDate));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PurchaseDto>())!;
    }

    private async Task<PayablesAgingDto> GetAgingAsync()
    {
        var response = await _client.GetAsync("/api/purchases/payables-aging");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PayablesAgingDto>())!;
    }

    [Fact]
    public async Task An_invoice_with_no_due_date_is_reported_as_unscheduled_never_as_overdue()
    {
        var supplier = await CreateSupplierAsync("مورد بلا شروط");
        var item = await CreateItemAsync("طماطم");
        await ReceiveCreditAsync(supplier, item, 50_000, null, "INV-NOTERM");

        var aging = await GetAgingAsync();

        Assert.Equal(0, aging.OverdueInvoiceCount);
        Assert.Equal(0, aging.OverduePiastres);
        Assert.Equal(1, aging.UnscheduledInvoiceCount);
        Assert.Equal(50_000, aging.UnscheduledPiastres);

        var invoice = Assert.Single(aging.Invoices);
        Assert.Null(invoice.DueDate);
        Assert.Null(invoice.DaysOverdue);
        Assert.Equal("NoDueDate", invoice.Bucket);
    }

    [Fact]
    public async Task A_past_due_invoice_is_bucketed_by_how_late_it_actually_is()
    {
        var supplier = await CreateSupplierAsync("مورد متأخر");
        var item = await CreateItemAsync("زيت");
        var due = BusinessClock.Today().AddDays(-40);
        await ReceiveCreditAsync(supplier, item, 120_000, due, "INV-LATE");

        var aging = await GetAgingAsync();

        Assert.Equal(1, aging.OverdueInvoiceCount);
        Assert.Equal(120_000, aging.OverduePiastres);

        var invoice = Assert.Single(aging.Invoices);
        Assert.Equal(40, invoice.DaysOverdue);
        Assert.Equal("Days31To60", invoice.Bucket);
    }

    [Fact]
    public async Task An_invoice_due_in_the_future_is_outstanding_but_not_late()
    {
        var supplier = await CreateSupplierAsync("مورد منتظم");
        var item = await CreateItemAsync("دقيق");
        await ReceiveCreditAsync(supplier, item, 80_000, BusinessClock.Today().AddDays(14), "INV-FUTURE");

        var aging = await GetAgingAsync();

        Assert.Equal(80_000, aging.TotalOutstandingPiastres);
        Assert.Equal(0, aging.OverdueInvoiceCount);
        Assert.Equal(0, aging.UnscheduledInvoiceCount);
        Assert.Equal("NotYetDue", Assert.Single(aging.Invoices).Bucket);
    }

    [Fact]
    public async Task A_settled_invoice_leaves_the_payables_report_entirely()
    {
        var supplier = await CreateSupplierAsync("مورد مسدد");
        var item = await CreateItemAsync("سكر");
        var purchase = await ReceiveCreditAsync(supplier, item, 30_000, BusinessClock.Today().AddDays(-5), "INV-PAID");

        var pay = await _client.PostAsJsonAsync($"/api/purchases/{purchase.Id}/payments",
            new PayPurchaseRequest($"payreq-{Guid.NewGuid():N}", purchase.Id, 30_000, "cash", _managerId, null, null));
        pay.EnsureSuccessStatusCode();

        var aging = await GetAgingAsync();
        Assert.Empty(aging.Invoices);
        Assert.Equal(0, aging.TotalOutstandingPiastres);
    }

    [Fact]
    public async Task A_due_date_can_be_agreed_after_the_goods_arrived()
    {
        var supplier = await CreateSupplierAsync("مورد اتُفق لاحقًا");
        var item = await CreateItemAsync("جبن");
        var purchase = await ReceiveCreditAsync(supplier, item, 45_000, null, "INV-AFTER");

        Assert.Equal(1, (await GetAgingAsync()).UnscheduledInvoiceCount);

        var due = BusinessClock.Today().AddDays(-3);
        var response = await _client.PutAsJsonAsync($"/api/purchases/{purchase.Id}/due-date",
            new SetPurchaseDueDateRequest($"due-{Guid.NewGuid():N}", purchase.Id, due, _managerId));
        response.EnsureSuccessStatusCode();

        var aging = await GetAgingAsync();
        Assert.Equal(0, aging.UnscheduledInvoiceCount);
        Assert.Equal(1, aging.OverdueInvoiceCount);
        Assert.Equal(3, Assert.Single(aging.Invoices).DaysOverdue);
    }

    [Fact]
    public async Task Clearing_a_due_date_returns_the_invoice_to_unscheduled()
    {
        var supplier = await CreateSupplierAsync("مورد أُلغي شرطه");
        var item = await CreateItemAsync("أرز");
        var purchase = await ReceiveCreditAsync(supplier, item, 20_000, BusinessClock.Today().AddDays(-9), "INV-CLEAR");

        var response = await _client.PutAsJsonAsync($"/api/purchases/{purchase.Id}/due-date",
            new SetPurchaseDueDateRequest($"due-{Guid.NewGuid():N}", purchase.Id, null, _managerId));
        response.EnsureSuccessStatusCode();

        var aging = await GetAgingAsync();
        Assert.Equal(0, aging.OverdueInvoiceCount);
        Assert.Equal(1, aging.UnscheduledInvoiceCount);
    }

    [Fact]
    public async Task An_invoice_entered_late_keeps_its_already_expired_term()
    {
        // The common case this system must not refuse: the goods arrived a while back, the invoice
        // is being keyed in now, and its agreed term has already passed. Receipt is always stamped
        // "now", so rejecting a past due date would force the operator to leave the field blank and
        // lose a true fact.
        var supplier = await CreateSupplierAsync("مورد أُدخلت فاتورته متأخرة");
        var item = await CreateItemAsync("بصل");
        await ReceiveCreditAsync(supplier, item, 10_000, BusinessClock.Today().AddDays(-6), "INV-LATEENTRY");

        var aging = await GetAgingAsync();
        var invoice = Assert.Single(aging.Invoices);
        Assert.Equal(6, invoice.DaysOverdue);
        Assert.Equal(1, aging.OverdueInvoiceCount);
    }

    [Fact]
    public async Task Setting_a_due_date_requires_purchasing_manage()
    {
        var supplier = await CreateSupplierAsync("مورد للصلاحيات");
        var item = await CreateItemAsync("فلفل");
        var purchase = await ReceiveCreditAsync(supplier, item, 15_000, null, "INV-PERM");

        var response = await _client.PutAsJsonAsync($"/api/purchases/{purchase.Id}/due-date",
            new SetPurchaseDueDateRequest($"due-{Guid.NewGuid():N}", purchase.Id, BusinessClock.Today(), _cashierId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task The_owner_dashboard_agrees_with_the_payables_report()
    {
        var supplier = await CreateSupplierAsync("مورد لوحة التحكم");
        var item = await CreateItemAsync("لحم");
        await ReceiveCreditAsync(supplier, item, 200_000, BusinessClock.Today().AddDays(-15), "INV-DASH-LATE");
        await ReceiveCreditAsync(supplier, item, 90_000, null, "INV-DASH-NOTERM");

        var aging = await GetAgingAsync();
        var ownerId = await _factory.SeedUserAsync("المالك", "owner", "reports_view");
        var dashboard = await _client.GetFromJsonAsync<Falah.Application.Reports.OwnerDashboardDto>(
            $"/api/reports/owner-dashboard?actorUserId={ownerId}");

        Assert.NotNull(dashboard);
        // Two independent code paths read the same invoices; if they can disagree, one of them is
        // telling the owner something the detail screen will contradict.
        Assert.Equal(aging.OverduePiastres, dashboard!.Obligations.OverduePayablesPiastres);
        Assert.Equal(aging.OverdueInvoiceCount, dashboard.Obligations.OverduePayableCount);
        Assert.Equal(aging.UnscheduledPiastres, dashboard.Obligations.UnscheduledPayablesPiastres);
        Assert.Equal(aging.UnscheduledInvoiceCount, dashboard.Obligations.UnscheduledPayableCount);
        Assert.Equal(15, dashboard.Obligations.OldestOverdueDays);
    }
}

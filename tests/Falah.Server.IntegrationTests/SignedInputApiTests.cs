namespace Falah.Server.IntegrationTests;
using System.Net;
using System.Net.Http.Json;
using Falah.Application.Delivery;
using Falah.Application.Menu;
using Falah.Application.Employees;
using Falah.Application.Purchasing;
using Microsoft.EntityFrameworkCore;
public class SignedInputApiTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _actor = default!;
    public async Task InitializeAsync()
    {
        _factory = new(); _client = _factory.CreateAuthenticatedClient();
        _actor = await _factory.SeedUserAsync("المالك", "owner", "all");
    }
    public Task DisposeAsync() { _client.Dispose(); _factory.Dispose(); return Task.CompletedTask; }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Negative_delivery_fees_are_refused_on_create_and_update(bool update)
    {
        HttpResponseMessage response;
        if (update)
        {
            var created = await _client.PostAsJsonAsync("/api/delivery/services", new CreateDeliveryServiceRequest(Guid.NewGuid().ToString("N"), "منطقة", 100, _actor, null, null));
            created.EnsureSuccessStatusCode();
            var service = (await created.Content.ReadFromJsonAsync<DeliveryServiceDto>())!;
            response = await _client.PutAsJsonAsync($"/api/delivery/services/{service.Id}", new UpdateDeliveryServiceRequest(service.Id, null, -1, null, _actor, null, null));
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal(100, await _factory.QueryAsync(db => db.DeliveryServices.Where(s => s.Id == service.Id).Select(s => s.FeePiastres).SingleAsync()));
        }
        else
        {
            response = await _client.PostAsJsonAsync("/api/delivery/services", new CreateDeliveryServiceRequest(Guid.NewGuid().ToString("N"), "منطقة", -1, _actor, null, null));
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.False(await _factory.QueryAsync(db => db.DeliveryServices.AnyAsync(s => s.FeePiastres < 0)));
        }
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Negative_variant_prices_are_refused_on_create_and_update(bool update)
    {
        var product = await _factory.SeedProductAsync(1000);
        var category = await _factory.QueryAsync(db => db.Products.Where(p => p.Id == product).Select(p => p.CategoryId).SingleAsync());
        var variants = new[] { new ProductVariantInput(null, "حجم", -1, null) };
        var response = update
            ? await _client.PutAsJsonAsync($"/api/products/{product}", new UpdateProductRequest(product, category, "صنف", 1000, false, null, 0, null, variants, _actor))
            : await _client.PostAsJsonAsync("/api/products", new CreateProductRequest(Guid.NewGuid().ToString("N"), category, "صنف", 1000, false, null, 0, null, variants, _actor));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.False(await _factory.QueryAsync(db => db.ProductVariants.AnyAsync(v => v.PricePiastres < 0)));
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Negative_custom_wages_and_rates_cannot_reduce_employee_balances(bool daily)
    {
        var employeeId = "employee-negative-test";
        await _factory.QueryAsync(async db => {
            db.Employees.Add(new Falah.Infrastructure.Persistence.Entities.EmployeeEntity {
                Id = employeeId, Name = "موظف", EmployeeNumber = "NEG", JobTitle = "عامل", CompensationType = "Daily",
                Phone = "01000000000", Role = "cashier", CreatedAtUtc = DateTimeOffset.UtcNow,
                DailyWagePiastres = 1000, ShiftValuePiastres = 1000, IsActive = true,
                HireDate = new DateOnly(2026, 1, 1) });
            await db.SaveChangesAsync(); return true;
        });
        var path = $"/api/employees/{employeeId}/" + (daily ? "daily-wage" : "overtime");
        object request = daily
            ? new RecordDailyWageRequest(employeeId, -1, null, _actor, null, null, Guid.NewGuid().ToString("N"))
            : new RecordOvertimeRequest(Guid.NewGuid().ToString("N"), employeeId, 1, -1, null, _actor, null, null);
        var response = await _client.PostAsJsonAsync(path, request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await _factory.QueryAsync(db => db.EmployeeLedgerEntries.CountAsync(e => e.EmployeeId == employeeId)));
    }
    [Theory]
    [InlineData(-1000, -100)]
    [InlineData(0, 100)]
    public async Task Invalid_purchase_lines_do_not_create_stock_or_supplier_debt(long quantity, long cost)
    {
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var seeded = await _factory.SeedProductWithRecipeAsync(1000, 1000);
        var supplierResponse = await _client.PostAsJsonAsync("/api/suppliers", new AddSupplierRequest(Guid.NewGuid().ToString("N"), "مورد", null, null, null, null, _actor, null, null));
        supplierResponse.EnsureSuccessStatusCode();
        var supplier = (await supplierResponse.Content.ReadFromJsonAsync<AddSupplierResult>())!;
        var id = Guid.NewGuid().ToString("N");
        var response = await _client.PostAsJsonAsync("/api/purchases", new ReceivePurchaseRequest(id, supplier.SupplierId, null,
            new[] { new PurchaseLineRequest(seeded.InventoryItemId, quantity, cost) }, 0, "credit", _actor, null, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.False(await _factory.QueryAsync(db => db.Purchases.AnyAsync(p => p.Id == "purchase-" + id)));
    }
}

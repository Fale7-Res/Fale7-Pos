namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Falah.Application.Inventory;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class OwnerOnlyReadPermissionTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private string _owner = default!, _till = default!, _specialist = default!;
    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _owner = await _factory.SeedUserAsync("مالك", "owner", "all");
        _till = await _factory.SeedUserAsync("كاشير", "cashier", "pos");
        _specialist = await _factory.SeedUserAsync("مسؤول", "station", "employees_manage", "employees_ledger", "attendance_manage", "payroll_approve", "backup_manage", "suppliers_manage", "purchasing_manage");
    }
    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }
    private HttpClient As(string id)
    {
        var c = _factory.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(id));
        return c;
    }
    [Fact]
    public async Task Personnel_payroll_backup_and_purchase_reads_require_the_owner_grant()
    {
        string[] routes = { "/api/employees", "/api/employees/probe", "/api/employees/probe/ledger", "/api/employees/probe/balance", "/api/attendance",
            "/api/payroll-runs", "/api/payroll-runs/probe", "/api/payroll-runs/probe/lines", "/api/backups", "/api/backups/probe",
            "/api/suppliers", "/api/suppliers/probe", "/api/suppliers/probe/ledger", "/api/purchases", "/api/purchases/payables-aging", "/api/purchases/probe" };
        var errors = new List<string>();
        foreach (var route in routes)
        {
            foreach (var id in new[] { _till, _specialist })
                if ((await As(id).GetAsync(route)).StatusCode != HttpStatusCode.UnprocessableEntity) errors.Add(route + " leaked to " + id);
            var ownerResponse = await As(_owner).GetAsync(route);
            if (ownerResponse.StatusCode != HttpStatusCode.OK && ownerResponse.StatusCode != HttpStatusCode.NotFound) errors.Add(route + " refused owner: " + ownerResponse.StatusCode);
        }
        Assert.Empty(errors);
    }
    [Fact]
    public async Task User_picker_exposes_names_without_roles_or_permission_grants_to_a_till()
    {
        var users = await As(_till).GetFromJsonAsync<System.Text.Json.JsonElement>("/api/users");
        Assert.NotEmpty(users.EnumerateArray());
        Assert.All(users.EnumerateArray(), u => {
            Assert.False(string.IsNullOrWhiteSpace(u.GetProperty("name").GetString()));
            Assert.Equal("", u.GetProperty("role").GetString());
            Assert.Empty(u.GetProperty("permissions").EnumerateArray());
        });
        var full = await As(_owner).GetFromJsonAsync<System.Text.Json.JsonElement>("/api/users");
        Assert.Contains(full.EnumerateArray(), u => u.GetProperty("role").GetString() == "owner");
    }
    [Fact]
    public async Task Stock_quantities_remain_visible_while_costs_are_withheld_from_the_till()
    {
        var response = await As(_owner).PostAsJsonAsync("/api/inventory/items", new CreateInventoryItemRequest($"req-{Guid.NewGuid():N}", "خامة", "food", "kg", 0, false, "purchase", null, _owner));
        response.EnsureSuccessStatusCode();
        var id = (await response.Content.ReadFromJsonAsync<CreateInventoryItemResult>())!.Id;
        (await As(_owner).PutAsJsonAsync($"/api/inventory/items/{id}/estimated-cost", new SetEstimatedCostRequest(id, 1234, _owner))).EnsureSuccessStatusCode();
        var items = await As(_till).GetFromJsonAsync<List<InventoryItemDto>>("/api/inventory/items");
        Assert.Null(Assert.Single(items!, i => i.Id == id).EstimatedUnitCostPiastres);
        var ownerItems = await As(_owner).GetFromJsonAsync<List<InventoryItemDto>>("/api/inventory/items");
        Assert.Equal(1234, Assert.Single(ownerItems!, i => i.Id == id).EstimatedUnitCostPiastres);
    }
}

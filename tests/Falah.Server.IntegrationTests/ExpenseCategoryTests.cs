namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Expenses;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// An expense is one of a fixed list of categories, none of which is an ingredient (owner's
/// decision). Ingredients and packaging bought out of the drawer as an "expense" were counted
/// twice - once in operating expenses and again in the food cost the recipes compute - and never
/// reached stock, so the inventory ran negative while the shelf was full. They go through Purchasing,
/// which receives them into stock at their cost.
/// </summary>
public class ExpenseCategoryTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        var database = await _factory.QueryAsync(db => Task.FromResult(db.Database.GetDbConnection().Database));
        Assert.StartsWith("FalahTest_", database);

        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "expenses_add_drawer");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var shift = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = shift!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    [Theory]
    [InlineData("مستلزمات مطبخ وخضار")]   // vegetables: an ingredient
    [InlineData("أكياس وتغليف")]          // packaging: tracked stock
    [InlineData("خامات")]
    public async Task Ingredients_and_stock_cannot_be_entered_as_an_expense(string category)
    {
        var response = await AddAsync(category);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("المشتريات", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, await _factory.QueryAsync(db => db.Expenses.CountAsync()));
    }

    [Theory]
    [InlineData("نظافة ومطهرات")]
    [InlineData("صيانة سريعة")]
    [InlineData("مستلزمات تشغيل")]
    public async Task An_operating_expense_in_the_fixed_list_is_recorded(string category)
    {
        (await AddAsync(category)).EnsureSuccessStatusCode();
    }

    private Task<HttpResponseMessage> AddAsync(string category) =>
        _client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest(
            $"exp-{Guid.NewGuid():N}", _shiftId, category, 3000, "بيان", "tre-drawer", _cashierId, null, null));
}

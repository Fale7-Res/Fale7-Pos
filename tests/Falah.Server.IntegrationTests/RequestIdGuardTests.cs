namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Falah.Application.Expenses;
using Falah.Application.Shifts;
using Falah.Application.Treasury;
using Microsoft.EntityFrameworkCore;

/// <summary>Independent audit 2026-11-23, F-04. QA-03 refused an empty request id in checkout, refund,
/// shift close and pickup collection only. Every other write still accepted one, and a second, DIFFERENT
/// operation with an empty id "replayed" the first and was never recorded. The id is now checked once,
/// for every POST/PUT, before any handler runs.</summary>
public class RequestIdGuardTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private string _managerId = default!;
    private string _cashierId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        Assert.StartsWith("FalahTest_", _factory.DatabaseName);
        _managerId = await _factory.SeedUserAsync("مدير", "manager", "all");
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "expenses_add_drawer");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private HttpClient As(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId));
        return client;
    }

    private async Task<string> OpenShiftAsync()
    {
        var response = await As(_cashierId).PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "وردية", _cashierId, 50_000));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    private static AddExpenseRequest Expense(string? requestId, string shiftId, long amount, string description, string cashierId) =>
        new(requestId!, shiftId, Falah.Domain.Financial.ExpenseCategories.All[0], amount, description, "tre-drawer", cashierId, null, null);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task An_expense_without_a_request_id_is_refused_and_records_nothing(string? requestId)
    {
        var shiftId = await OpenShiftAsync();

        var response = await As(_cashierId).PostAsJsonAsync("/api/expenses", Expense(requestId, shiftId, 1_000, "مصروف", _cashierId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await _factory.QueryAsync(db => db.Expenses.CountAsync()));
    }

    [Fact]
    public async Task A_request_id_longer_than_the_limit_is_refused()
    {
        var shiftId = await OpenShiftAsync();
        var response = await As(_cashierId).PostAsJsonAsync("/api/expenses",
            Expense(new string('x', Falah.Domain.Common.InputRules.MaxRequestIdLength + 1), shiftId, 1_000, "مصروف", _cashierId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_treasury_deposit_without_a_request_id_is_refused()
    {
        var response = await As(_managerId).PostAsJsonAsync("/api/treasury/deposit", new DepositRequest(
            "", "capital_injection", 10_000, "tre-main", "إيداع", null, _managerId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_shift_cannot_be_opened_without_a_request_id()
    {
        var response = await As(_cashierId).PostAsJsonAsync("/api/shifts/open", new OpenShiftRequest("", "وردية", _cashierId, 0));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await _factory.QueryAsync(db => db.Shifts.CountAsync()));
    }

    /// <summary>An optional request id may still be left out entirely; only a blank one is refused.</summary>
    [Fact]
    public async Task An_optional_request_id_may_be_omitted_but_not_blank()
    {
        var productId = await _factory.SeedProductAsync(5_000);
        var omitted = await As(_managerId).PostAsJsonAsync($"/api/products/{productId}/availability",
            new Falah.Application.Menu.SetProductAvailabilityRequest(productId, false, _managerId));
        var blank = await As(_managerId).PostAsJsonAsync($"/api/products/{productId}/availability",
            new Falah.Application.Menu.SetProductAvailabilityRequest(productId, true, _managerId, ""));

        Assert.Equal(HttpStatusCode.OK, omitted.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blank.StatusCode);
    }

    [Fact]
    public async Task Two_different_expenses_with_real_ids_are_both_recorded()
    {
        var shiftId = await OpenShiftAsync();
        (await As(_cashierId).PostAsJsonAsync("/api/expenses", Expense($"e-{Guid.NewGuid():N}", shiftId, 1_000, "أول", _cashierId))).EnsureSuccessStatusCode();
        (await As(_cashierId).PostAsJsonAsync("/api/expenses", Expense($"e-{Guid.NewGuid():N}", shiftId, 3_000, "تاني", _cashierId))).EnsureSuccessStatusCode();
        Assert.Equal(4_000, await _factory.QueryAsync(db => db.Expenses.SumAsync(e => e.AmountPiastres)));
    }
}

namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Every shift, every sale and every movement a shift posts belongs to a business day.
///
/// Two gaps let money fall outside one. A shift opened with no opening cash needed no business day,
/// and a sale only stamps the day that happens to be open - so a zero-float shift could ring up
/// sales that no business-day reconciliation or close snapshot ever counted. And a shift's close
/// posted its cash handover and its shortage/overage with no business day at all, so a closed day's
/// drawer appeared to still hold every sale and main treasury appeared never to receive it.
/// </summary>
public class BusinessDayAssociationTests : IAsyncLifetime
{
    private const long OpeningCash = 50000;

    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?>
        {
            ["Backup:Automatic:Enabled"] = "false",
        });
        var database = await _factory.QueryAsync(db => Task.FromResult(db.Database.GetDbConnection().Database));
        Assert.StartsWith("FalahTest_", database);

        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    // ---------- C4: no shift without a business day ----------

    // Superseded 2026-11-09 (owner-approved cashier handover): this used to assert that a shift is REFUSED
    // without an open day. That forced the cashier through a manager login. The guarantee it protected stays:
    // no shift - not even a zero-float one - exists outside a business day. An authorized opening now creates the
    // day in the same transaction (BusinessDayGate.ResolveForShiftOpenAsync) instead of refusing.
    [Fact]
    public async Task A_shift_never_exists_outside_a_business_day_even_with_no_opening_cash()
    {
        await _factory.FundMainAccountAsync();   // money exists; only the business day is missing

        var response = await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 0));

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var shift = await _factory.QueryAsync(db => db.Shifts.AsNoTracking().SingleAsync());
        var day = await _factory.QueryAsync(db => db.BusinessDays.AsNoTracking().SingleAsync());
        Assert.Equal(day.Id, shift.BusinessDayId);
        Assert.Equal("Open", day.Status);
        Assert.Equal(0, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "shift_opening_float")));

        var order = await CheckoutCashAsync(shift.Id, 10000);
        Assert.Equal(day.Id, await _factory.QueryAsync(db => db.Orders.Where(o => o.Id == order.OrderId).Select(o => o.BusinessDayId).FirstAsync()));
    }

    [Fact]
    public async Task A_zero_float_shift_opens_once_a_business_day_is_open_and_its_sales_carry_that_day()
    {
        var dayId = await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var shiftId = await OpenShiftAsync(0);

        var order = await CheckoutCashAsync(shiftId, 10000);

        var stamped = await _factory.QueryAsync(db => db.Orders.Where(o => o.Id == order.OrderId).Select(o => o.BusinessDayId).FirstAsync());
        Assert.Equal(dayId, stamped);
    }

    // ---------- C5: a shift's close belongs to the business day it happens in ----------

    [Fact]
    public async Task A_shift_close_posts_its_handover_and_its_difference_under_the_open_business_day()
    {
        var dayId = await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var shiftId = await OpenShiftAsync(OpeningCash);
        await CheckoutCashAsync(shiftId, 10000);

        // 1000 short of the 60000 expected - within the default threshold, so no approval needed,
        // and it produces both a reconciliation movement and a handover.
        await CloseAsync(shiftId, OpeningCash + 10000 - 1000);

        var closeMovements = await _factory.QueryAsync(db => db.TreasuryMovements
            .Where(m => m.ReferenceId == shiftId && (m.Type == "shift_handover" || m.Type == "cash_shortage"))
            .Select(m => new { m.Type, m.BusinessDayId }).ToListAsync());
        Assert.Equal(2, closeMovements.Count);
        Assert.All(closeMovements, m => Assert.Equal(dayId, m.BusinessDayId));
    }

    [Fact]
    public async Task Once_every_shift_has_closed_the_business_days_drawer_holds_nothing()
    {
        var dayId = await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var shiftId = await OpenShiftAsync(OpeningCash);
        await CheckoutCashAsync(shiftId, 10000);
        await CloseAsync(shiftId, OpeningCash + 10000);

        var day = await _client.GetFromJsonAsync<BusinessDayReconciliationDto>(
            $"/api/reports/business-day/{dayId}?actorUserId={_managerId}");

        // In: the 500 ج float from main and the 100 ج sale. Out: the 600 ج counted and handed over.
        Assert.Equal(0, day!.Treasury.DrawerPiastres);
        // Main sent the float and received the handover - a net 100 ج for the day.
        Assert.Equal(10000, day.Treasury.MainPiastres);
    }

    // ---------- A1: the blind count's expected figure is for those who may close ----------

    [Fact]
    public async Task Only_someone_allowed_to_close_a_shift_can_see_its_expected_cash()
    {
        // Prepare-close reveals what the drawer SHOULD hold. Anyone who could read it without being
        // allowed to close could count to the answer, which defeats the blind count.
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var shiftId = await OpenShiftAsync(OpeningCash);
        var waiterId = await _factory.SeedUserAsync("نادل", "cashier", "pos");

        var refused = await As(waiterId).PostAsJsonAsync("/api/shifts/prepare-close", new PrepareShiftCloseRequest(shiftId, 0));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.DoesNotContain("50000", await refused.Content.ReadAsStringAsync());

        var allowed = await As(_cashierId).PostAsJsonAsync("/api/shifts/prepare-close", new PrepareShiftCloseRequest(shiftId, 0));
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    // ---------- Helpers ----------

    private HttpClient As(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId));
        return client;
    }

    private async Task<string> OpenShiftAsync(long openingCash)
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, openingCash));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    private async Task<CheckoutOrderResult> CheckoutCashAsync(string shiftId, long totalPiastres)
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: totalPiastres, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    private async Task CloseAsync(string shiftId, long counted)
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"creq-{Guid.NewGuid():N}", shiftId, _cashierId, counted, "فرق بسيط", null, null));
        response.EnsureSuccessStatusCode();
    }
}

namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Xunit;

/// <summary>Ported from AppContextType.visibleOrders in lib/store.tsx: a holder of
/// 'orders_view_all' (owner/manager) browses the full order history; everyone else is scoped to
/// whichever shift is currently open, system-wide - a real gap found during the legacy-parity
/// audit, since no list endpoint existed at all before this pass to enforce the boundary on.</summary>
public class OrderHistoryVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> OpenShiftAsync() =>
        (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;

    private async Task CheckoutAsync(string shiftId, long totalPiastres = 10000)
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: totalPiastres, SplitPayments: null, WalletProvider: null,
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        (await _client.PostAsJsonAsync("/api/orders/checkout", request)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_cashier_without_orders_view_all_sees_only_the_currently_open_shifts_orders()
    {
        var firstShiftId = await OpenShiftAsync();
        await CheckoutAsync(firstShiftId);
        (await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"creq-{Guid.NewGuid():N}", firstShiftId, _cashierId, 60000, null, null, null))).EnsureSuccessStatusCode();

        var secondShiftId = await OpenShiftAsync();
        await CheckoutAsync(secondShiftId);

        var orders = await (await _client.GetAsync($"/api/orders?actorUserId={_cashierId}")).Content.ReadFromJsonAsync<List<OrderDetailsDto>>();
        Assert.All(orders!, o => Assert.Equal(secondShiftId, o.ShiftId));
        Assert.DoesNotContain(orders!, o => o.ShiftId == firstShiftId);
    }

    [Fact]
    public async Task A_manager_holding_orders_view_all_sees_the_full_order_history()
    {
        var firstShiftId = await OpenShiftAsync();
        await CheckoutAsync(firstShiftId);
        (await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"creq-{Guid.NewGuid():N}", firstShiftId, _cashierId, 60000, null, null, null))).EnsureSuccessStatusCode();

        var secondShiftId = await OpenShiftAsync();
        await CheckoutAsync(secondShiftId);

        var orders = await (await _client.GetAsync($"/api/orders?actorUserId={_managerId}")).Content.ReadFromJsonAsync<List<OrderDetailsDto>>();
        Assert.Contains(orders!, o => o.ShiftId == firstShiftId);
        Assert.Contains(orders!, o => o.ShiftId == secondShiftId);
    }

    [Fact]
    public async Task A_cashier_without_orders_view_all_sees_an_empty_list_when_no_shift_is_open()
    {
        var shiftId = await OpenShiftAsync();
        await CheckoutAsync(shiftId);
        (await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"creq-{Guid.NewGuid():N}", shiftId, _cashierId, 60000, null, null, null))).EnsureSuccessStatusCode();

        var response = await _client.GetAsync($"/api/orders?actorUserId={_cashierId}");
        response.EnsureSuccessStatusCode();
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDetailsDto>>();
        Assert.Empty(orders!);
    }
}

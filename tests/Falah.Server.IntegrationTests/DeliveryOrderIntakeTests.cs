namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Ported from checkout()'s own re-validation of delivery orders in lib/store.tsx (`if (orderType
/// === 'delivery') { const missing = firstMissingDeliveryField(...); if (missing) throw ... }`) and
/// its composedAddress/effectiveAddress logic. This is the server-authoritative gate a client with
/// no pre-checkout form of its own (Call Center, submitting straight to /api/orders/checkout) must
/// pass through - unlike the Cashier app, which today has no delivery order-type UI yet and so never
/// exercised this path at all before this slice.
/// </summary>
public class DeliveryOrderIntakeTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();

        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<CheckoutOrderRequest> DeliveryOrderAsync(
        long totalPiastres = 10000, string? customerPhone = "01000000000", string? customerName = "أحمد",
        string? deliveryServiceId = "svc-1", string? deliveryArea = "المعادي", string? deliveryStreet = "شارع 9",
        string? building = "5", string? floor = "3", string? apartment = "12", string? landmark = null)
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        return new(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "delivery", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 1000, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            CustomerName: customerName, CustomerPhone: customerPhone, DeliveryAddress: null, DeliveryArea: deliveryArea,
            BusinessDayId: null, DeliveryStreet: deliveryStreet, DeliveryBuilding: building, DeliveryFloor: floor,
            DeliveryApartment: apartment, DeliveryLandmark: landmark, DeliveryNotes: null,
            DeliveryServiceId: deliveryServiceId, DeliveryServiceName: "توصيل سريع");
    }

    [Fact]
    public async Task A_complete_delivery_order_succeeds()
    {
        var request = await DeliveryOrderAsync();
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData(null, "أحمد", "svc-1", "المعادي", "شارع 9", "رقم هاتف")]
    [InlineData("01000000000", null, "svc-1", "المعادي", "شارع 9", "اسم العميل")]
    [InlineData("01000000000", "أحمد", null, "المعادي", "شارع 9", "خدمة التوصيل")]
    [InlineData("01000000000", "أحمد", "svc-1", null, "شارع 9", "المنطقة")]
    [InlineData("01000000000", "أحمد", "svc-1", "المعادي", null, "الشارع")]
    public async Task A_delivery_order_missing_any_required_field_is_rejected_with_the_first_missing_one(
        string? phone, string? name, string? serviceId, string? area, string? street, string expectedMessageFragment)
    {
        var request = await DeliveryOrderAsync(customerPhone: phone, customerName: name, deliveryServiceId: serviceId, deliveryArea: area, deliveryStreet: street);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedMessageFragment, body);
    }

    [Fact]
    public async Task A_non_delivery_order_never_requires_any_delivery_field()
    {
        var productId = await _factory.SeedProductAsync(10000);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 10000, SplitPayments: null, WalletProvider: null,
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task The_delivery_address_is_composed_server_side_from_structured_parts_not_trusted_from_the_client()
    {
        var request = await DeliveryOrderAsync(building: "5", floor: "3", apartment: "12", landmark: "بجوار الصيدلية");
        var checkout = await (await _client.PostAsJsonAsync("/api/orders/checkout", request)).Content.ReadFromJsonAsync<CheckoutOrderResult>();

        var order = await (await _client.GetAsync($"/api/orders/{checkout!.OrderId}")).Content.ReadFromJsonAsync<OrderDetailsDto>();
        Assert.Equal("المنطقة المعادي، ش شارع 9، عمارة 5، دور 3، شقة 12 — بجوار الصيدلية", order!.DeliveryAddress);
    }

    [Fact]
    public async Task The_full_structured_delivery_snapshot_is_readable_back_from_order_lookup()
    {
        var request = await DeliveryOrderAsync();
        var checkout = await (await _client.PostAsJsonAsync("/api/orders/checkout", request)).Content.ReadFromJsonAsync<CheckoutOrderResult>();

        var order = await (await _client.GetAsync($"/api/orders/{checkout!.OrderId}")).Content.ReadFromJsonAsync<OrderDetailsDto>();
        Assert.Equal("أحمد", order!.CustomerName);
        Assert.Equal("01000000000", order.CustomerPhone);
        Assert.Equal("المعادي", order.DeliveryArea);
        Assert.Equal("شارع 9", order.DeliveryStreet);
        Assert.Equal("5", order.DeliveryBuilding);
        Assert.Equal("3", order.DeliveryFloor);
        Assert.Equal("12", order.DeliveryApartment);
        Assert.Equal("svc-1", order.DeliveryServiceId);
        Assert.Equal("توصيل سريع", order.DeliveryServiceName);
        Assert.Equal(1000, order.DeliveryFeePiastres);
    }

    [Fact]
    public async Task A_delivery_order_still_auto_creates_a_delivery_row_exactly_as_before_this_slice()
    {
        // Preserving the already-proven Delivery vertical slice behavior - this new checkout-time
        // validation must not disturb the existing per-order dispatch pipeline it feeds into.
        var request = await DeliveryOrderAsync();
        var checkout = await (await _client.PostAsJsonAsync("/api/orders/checkout", request)).Content.ReadFromJsonAsync<CheckoutOrderResult>();

        var delivery = await _factory.QueryAsync(db => db.Deliveries.AsNoTracking().FirstAsync(d => d.OrderId == checkout!.OrderId));
        Assert.Equal("New", delivery.Status);
    }
}

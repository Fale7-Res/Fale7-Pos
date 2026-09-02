namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class RefundVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateClient();
        // Holds both pos and orders_refund so most tests don't need a manager approval detour;
        // one dedicated test below exercises the approval path with a separate, unprivileged user.
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "orders_refund", "orders_comp");

        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<CheckoutOrderResult> CheckoutAsync(long totalPiastres, string paymentMethod = "cash", IReadOnlyList<CheckoutSplitRequest>? splits = null)
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: paymentMethod,
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: totalPiastres, SplitPayments: splits, WalletProvider: null,
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    private RefundOrderRequest RefundRequest(string orderId, long amountPiastres, string requestId, string? approvalId = null, string? approverId = null) =>
        new(requestId, orderId, amountPiastres, "طلب العميل", _cashierId, approvalId, approverId);

    [Fact]
    public async Task A_full_refund_marks_the_order_refunded_and_posts_exactly_one_movement()
    {
        var order = await CheckoutAsync(20000);
        var response = await _client.PostAsJsonAsync("/api/orders/refund", RefundRequest(order.OrderId, 20000, $"rreq-{Guid.NewGuid():N}"));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RefundOrderResult>();

        Assert.False(result!.WasReplayed);
        Assert.Equal("refunded", result.OrderStatus);
        Assert.Equal(0, result.RemainingRefundablePiastres);

        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "refund" && m.ReferenceId == order.OrderId));
        Assert.Equal(1, movementCount);
    }

    [Fact]
    public async Task A_partial_refund_keeps_the_order_completed_and_tracks_what_remains()
    {
        var order = await CheckoutAsync(20000);
        var response = await _client.PostAsJsonAsync("/api/orders/refund", RefundRequest(order.OrderId, 5000, $"rreq-{Guid.NewGuid():N}"));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RefundOrderResult>();

        Assert.Equal("completed", result!.OrderStatus);
        Assert.Equal(15000, result.RemainingRefundablePiastres);
    }

    [Fact]
    public async Task A_refund_request_larger_than_what_remains_is_clamped_not_rejected()
    {
        var order = await CheckoutAsync(20000);
        var response = await _client.PostAsJsonAsync("/api/orders/refund", RefundRequest(order.OrderId, 999_999, $"rreq-{Guid.NewGuid():N}"));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RefundOrderResult>();

        Assert.Equal(20000, result!.AmountPiastres); // clamped down to the order's actual total
        Assert.Equal("refunded", result.OrderStatus);

        var totalRefunded = await _factory.QueryAsync(db => db.TreasuryMovements.Where(m => m.ReferenceId == order.OrderId && m.Type == "refund").SumAsync(m => m.AmountPiastres));
        Assert.Equal(20000, totalRefunded); // never more than the order was ever worth
    }

    [Fact]
    public async Task Retrying_the_exact_same_refund_request_sequentially_never_creates_a_second_movement()
    {
        var order = await CheckoutAsync(20000);
        var requestId = $"rreq-{Guid.NewGuid():N}";
        var first = await (await _client.PostAsJsonAsync("/api/orders/refund", RefundRequest(order.OrderId, 8000, requestId))).Content.ReadFromJsonAsync<RefundOrderResult>();
        var second = await (await _client.PostAsJsonAsync("/api/orders/refund", RefundRequest(order.OrderId, 8000, requestId))).Content.ReadFromJsonAsync<RefundOrderResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(first.RefundId, second.RefundId);

        var refundCount = await _factory.QueryAsync(db => db.OrderRefunds.CountAsync(r => r.IdempotencyKey == $"refund-{requestId}"));
        Assert.Equal(1, refundCount);
        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == order.OrderId && m.Type == "refund"));
        Assert.Equal(1, movementCount);
    }

    [Fact]
    public async Task Concurrent_refund_requests_with_the_same_idempotency_key_race_to_exactly_one_refund()
    {
        var order = await CheckoutAsync(20000);
        var requestId = $"rreq-{Guid.NewGuid():N}";
        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/orders/refund", RefundRequest(order.OrderId, 8000, requestId)));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<RefundOrderResult>()));
        Assert.Single(results.Select(r => r!.RefundId).Distinct());
        Assert.Equal(1, results.Count(r => !r!.WasReplayed));

        var refundCount = await _factory.QueryAsync(db => db.OrderRefunds.CountAsync(r => r.OrderId == order.OrderId));
        Assert.Equal(1, refundCount);
    }

    [Fact]
    public async Task Concurrent_refunds_for_different_amounts_on_the_same_order_never_overdraw_the_order_total()
    {
        // Two genuinely different refund attempts race on the SAME order, together requesting more
        // (150+150=300) than the order is even worth (200) - the total actually posted must never
        // exceed what the order was worth, regardless of which request the server processes first.
        var order = await CheckoutAsync(20000);
        var tasks = new[]
        {
            _client.PostAsJsonAsync("/api/orders/refund", RefundRequest(order.OrderId, 15000, $"rreq-{Guid.NewGuid():N}")),
            _client.PostAsJsonAsync("/api/orders/refund", RefundRequest(order.OrderId, 15000, $"rreq-{Guid.NewGuid():N}")),
        };
        var responses = await Task.WhenAll(tasks);
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode));

        var totalRefunded = await _factory.QueryAsync(db => db.TreasuryMovements.Where(m => m.ReferenceId == order.OrderId && m.Type == "refund").SumAsync(m => m.AmountPiastres));
        var orderRow = await _factory.QueryAsync(db => db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.OrderId));

        Assert.Equal(20000, totalRefunded); // exactly the order's total, never 30000
        Assert.Equal(20000, orderRow.RefundAmountPiastres);
        Assert.Equal("refunded", orderRow.Status);
    }

    [Fact]
    public async Task Refund_without_permission_or_approval_is_rejected_and_nothing_is_written()
    {
        var order = await CheckoutAsync(20000);
        var unprivilegedCashier = await _factory.SeedUserAsync("كاشير بلا صلاحية", "cashier", "pos");
        var request = RefundRequest(order.OrderId, 5000, $"rreq-{Guid.NewGuid():N}") with { CashierId = unprivilegedCashier };

        var response = await _client.PostAsJsonAsync("/api/orders/refund", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var refundCount = await _factory.QueryAsync(db => db.OrderRefunds.CountAsync(r => r.OrderId == order.OrderId));
        Assert.Equal(0, refundCount);
    }

    [Fact]
    public async Task A_multi_payment_order_cannot_be_refunded_through_this_path()
    {
        var order = await CheckoutAsync(20000, "multi", new[]
        {
            new CheckoutSplitRequest("cash", 10000),
            new CheckoutSplitRequest("card", 10000),
        });

        var response = await _client.PostAsJsonAsync("/api/orders/refund", RefundRequest(order.OrderId, 5000, $"rreq-{Guid.NewGuid():N}"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var refundCount = await _factory.QueryAsync(db => db.OrderRefunds.CountAsync(r => r.OrderId == order.OrderId));
        Assert.Equal(0, refundCount);
    }

    [Fact]
    public async Task A_refund_preserves_full_references_between_order_refund_and_treasury_movement()
    {
        var order = await CheckoutAsync(20000);
        var result = (await (await _client.PostAsJsonAsync("/api/orders/refund", RefundRequest(order.OrderId, 20000, $"rreq-{Guid.NewGuid():N}"))).Content.ReadFromJsonAsync<RefundOrderResult>())!;

        var refundRow = await _factory.QueryAsync(db => db.OrderRefunds.AsNoTracking().FirstAsync(r => r.OrderId == order.OrderId));
        var movementRow = await _factory.QueryAsync(db => db.TreasuryMovements.AsNoTracking().FirstAsync(m => m.Id == result.TreasuryMovementId));

        Assert.Equal(order.OrderId, refundRow.OrderId);
        Assert.Equal(movementRow.Id, refundRow.TreasuryMovementId);
        Assert.Equal(order.OrderId, movementRow.ReferenceId);
        Assert.Equal("order_refund", movementRow.ReferenceType);
    }

    [Fact]
    public async Task A_refund_never_modifies_the_orders_original_frozen_fields()
    {
        var order = await CheckoutAsync(20000);
        var before = await _factory.QueryAsync(db => db.Orders.AsNoTracking().Include(o => o.Items).FirstAsync(o => o.Id == order.OrderId));

        await _client.PostAsJsonAsync("/api/orders/refund", RefundRequest(order.OrderId, 5000, $"rreq-{Guid.NewGuid():N}"));

        var after = await _factory.QueryAsync(db => db.Orders.AsNoTracking().Include(o => o.Items).FirstAsync(o => o.Id == order.OrderId));
        Assert.Equal(before.TotalPiastres, after.TotalPiastres);
        Assert.Equal(before.OrderNumber, after.OrderNumber);
        Assert.Equal(before.CreatedAtUtc, after.CreatedAtUtc);
        Assert.Equal(before.Items.Single().TotalPricePiastres, after.Items.Single().TotalPricePiastres);
    }

    [Fact]
    public async Task A_refund_key_the_database_already_disagrees_with_leaves_no_partial_state()
    {
        var order = await CheckoutAsync(20000);
        var requestId = $"rreq-{Guid.NewGuid():N}";
        await _factory.QueryAsync(async db =>
        {
            db.TreasuryMovements.Add(new Falah.Infrastructure.Persistence.Entities.TreasuryMovementEntity
            {
                Id = $"tm-{Guid.NewGuid():N}", IdempotencyKey = $"refund-{requestId}", OperationId = "op-seed",
                Type = "refund", SourceAccountId = "tre-drawer", AmountPiastres = 1, ReferenceType = "order_refund",
                ReferenceId = order.OrderId, ShiftId = _shiftId, Reason = "seed", CreatedBy = "seed",
                CreatedAtUtc = DateTimeOffset.UtcNow, Status = "Posted",
            });
            await db.SaveChangesAsync();
            return true;
        });

        var response = await _client.PostAsJsonAsync("/api/orders/refund", RefundRequest(order.OrderId, 5000, requestId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var refundCount = await _factory.QueryAsync(db => db.OrderRefunds.CountAsync(r => r.OrderId == order.OrderId));
        Assert.Equal(0, refundCount);
        var orderRow = await _factory.QueryAsync(db => db.Orders.AsNoTracking().FirstAsync(o => o.Id == order.OrderId));
        Assert.Equal(0, orderRow.RefundAmountPiastres);
    }
}

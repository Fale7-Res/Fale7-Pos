namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class CheckoutVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "orders_comp", "orders_discount", "shifts_open_own");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();

        var openResponse = await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000));
        openResponse.EnsureSuccessStatusCode();
        var opened = await openResponse.Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<CheckoutOrderRequest> CashOrder(string checkoutRequestId, long totalPiastres = 10000)
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        return new(
        CheckoutRequestId: checkoutRequestId,
        CashierId: _cashierId,
        ShiftId: _shiftId,
        OrderType: "takeaway",
        PaymentMethod: "cash",
        Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
        DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
        CompApprovalId: null, CompApproverUserId: null, CompReason: null,
        AmountPaidPiastres: totalPiastres, SplitPayments: null, WalletProvider: null,
        CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
    }

    [Fact]
    public async Task Successful_cash_checkout_creates_an_order_and_exactly_one_treasury_movement()
    {
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", await CashOrder($"req-{Guid.NewGuid():N}"));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CheckoutOrderResult>();

        Assert.False(result!.WasReplayed);
        Assert.Equal("#0128", result.OrderNumber);
        Assert.True(result.IsPaid);

        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == result.OrderId));
        Assert.Equal(1, movementCount);
    }

    [Fact]
    public async Task Retrying_the_exact_same_checkout_request_sequentially_never_creates_a_second_order()
    {
        var requestId = $"req-{Guid.NewGuid():N}";
        var request = await CashOrder(requestId);
        var first = await (await _client.PostAsJsonAsync("/api/orders/checkout", request)).Content.ReadFromJsonAsync<CheckoutOrderResult>();
        var second = await (await _client.PostAsJsonAsync("/api/orders/checkout", request)).Content.ReadFromJsonAsync<CheckoutOrderResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(first.OrderId, second.OrderId);
        Assert.Equal(first.OrderNumber, second.OrderNumber);

        var orderCount = await _factory.QueryAsync(db => db.Orders.CountAsync(o => o.CheckoutRequestId == requestId));
        Assert.Equal(1, orderCount);
        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == first.OrderId));
        Assert.Equal(1, movementCount);
    }

    [Fact]
    public async Task Concurrent_requests_with_the_same_idempotency_key_race_to_exactly_one_order()
    {
        // Simulates a same-task double click AND a Cashier/Call Center network retry landing at
        // the server at nearly the same instant - the two hardest cases to get right.
        var requestId = $"req-{Guid.NewGuid():N}";
        var request = await CashOrder(requestId);
        var tasks = Enumerable.Range(0, 8).Select(_ => _client.PostAsJsonAsync("/api/orders/checkout", request));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<CheckoutOrderResult>()));

        var distinctOrderIds = results.Select(r => r!.OrderId).Distinct().ToList();
        Assert.Single(distinctOrderIds);
        Assert.Equal(1, results.Count(r => !r!.WasReplayed));
        Assert.Equal(7, results.Count(r => r!.WasReplayed));

        var orderCount = await _factory.QueryAsync(db => db.Orders.CountAsync(o => o.CheckoutRequestId == requestId));
        Assert.Equal(1, orderCount);
        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == distinctOrderIds[0]));
        Assert.Equal(1, movementCount);
    }

    [Fact]
    public async Task Concurrent_requests_with_different_idempotency_keys_all_succeed_with_distinct_never_colliding_sequence_numbers()
    {
        // Cashier and Call Center both firing genuinely different orders at once must never step
        // on each other's order number.
        var requests = new List<CheckoutOrderRequest>();
        for (var i = 0; i < 12; i++) requests.Add(await CashOrder($"req-{Guid.NewGuid():N}"));
        var responses = await Task.WhenAll(requests.Select(r => _client.PostAsJsonAsync("/api/orders/checkout", r)));

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<CheckoutOrderResult>()));

        var sequenceNumbers = results.Select(r => r!.SequenceNumber).ToList();
        Assert.Equal(12, sequenceNumbers.Distinct().Count());
        var orderNumbers = results.Select(r => r!.OrderNumber).ToList();
        Assert.Equal(12, orderNumbers.Distinct().Count());
    }

    [Fact]
    public async Task Checkout_without_an_open_shift_is_rejected_and_nothing_is_written()
    {
        var request = (await CashOrder($"req-{Guid.NewGuid():N}")) with { ShiftId = "shift-does-not-exist" };
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var orderCount = await _factory.QueryAsync(db => db.Orders.CountAsync(o => o.CheckoutRequestId == request.CheckoutRequestId));
        Assert.Equal(0, orderCount);
    }

    [Fact]
    public async Task InstaPay_checkout_defers_money_no_treasury_movement_until_confirmed()
    {
        var request = (await CashOrder($"req-{Guid.NewGuid():N}")) with { PaymentMethod = "instapay", AmountPaidPiastres = 10000 };
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CheckoutOrderResult>();

        Assert.False(result!.IsPaid);
        Assert.Equal("pending", result.ElectronicPaymentStatus);
        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == result.OrderId));
        Assert.Equal(0, movementCount);
    }

    [Fact]
    public async Task Reprinting_an_order_by_fetching_it_repeatedly_never_changes_any_financial_state()
    {
        var checkoutResponse = await _client.PostAsJsonAsync("/api/orders/checkout", await CashOrder($"req-{Guid.NewGuid():N}"));
        var order = await checkoutResponse.Content.ReadFromJsonAsync<CheckoutOrderResult>();

        for (var i = 0; i < 5; i++)
        {
            var getResponse = await _client.GetAsync($"/api/orders/{order!.OrderId}");
            getResponse.EnsureSuccessStatusCode();
        }

        var orderCount = await _factory.QueryAsync(db => db.Orders.CountAsync(o => o.Id == order!.OrderId));
        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == order!.OrderId));
        var journalCount = await _factory.QueryAsync(db => db.FinancialJournalEntries.CountAsync());
        Assert.Equal(1, orderCount);
        Assert.Equal(1, movementCount);
        Assert.Equal(1, journalCount); // exactly the one entry the original checkout created - reprint added none.
    }

    [Fact]
    public async Task A_journal_key_that_the_database_already_disagrees_with_leaves_no_partial_order_or_movement()
    {
        // Simulates the database already holding a conflicting idempotency-key row for this exact
        // request (e.g. a prior crash left an inconsistent journal entry with no matching order) -
        // a case the fast early-replay check cannot resolve as "already succeeded" because no
        // Order exists to return. The handler must refuse rather than silently create a second,
        // untracked financial event, and must leave the database exactly as it found it. The
        // concurrency test below covers the sibling case - genuinely simultaneous requests that
        // both stage a full Order+Items+Movement and only one wins at the real SaveChanges-level
        // constraint.
        var requestId = $"req-{Guid.NewGuid():N}";
        await _factory.QueryAsync(async db =>
        {
            db.FinancialJournalEntries.Add(new FinancialJournalEntryEntity
            {
                IdempotencyKey = $"sale-{requestId}",
                Kind = "sale",
                Status = "Complete",
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
            return true;
        });

        // Baseline, not zero: InitializeAsync's business-day/shift setup already posts its own
        // funding + shift_opening_float movements - the point of this assertion is that the failed
        // attempt below adds no NEW movement on top of those, not that the table is literally empty.
        var movementCountBefore = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());

        var response = await _client.PostAsJsonAsync("/api/orders/checkout", await CashOrder(requestId));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var orderCount = await _factory.QueryAsync(db => db.Orders.CountAsync(o => o.CheckoutRequestId == requestId));
        var itemCount = await _factory.QueryAsync(db => db.OrderItems.CountAsync());
        var movementCountAfter = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());
        Assert.Equal(0, orderCount);
        Assert.Equal(0, itemCount);
        Assert.Equal(movementCountBefore, movementCountAfter);
    }

    // Legacy-parity audit (2026-09-04): comp checkouts used to skip the base 'pos' permission check
    // entirely (only orders_comp was checked). Fixed so 'pos' is required unconditionally, matching
    // checkout()'s own unconditional hasPermission('pos') check in lib/store.tsx.
    [Fact]
    public async Task A_comp_checkout_still_requires_the_base_pos_permission()
    {
        var managerId = await _factory.SeedUserAsync("مدير", "manager", "all");
        var noPosCashier = await _factory.SeedUserAsync("كاشير بلا pos", "cashier", "orders_comp");
        var approval = await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(managerId), "orders_comp", "اعتماد"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>();

        var productId = await _factory.SeedProductAsync(10000);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: noPosCashier, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 10000,
            CompApprovalId: approval!.ApprovalId, CompApproverUserId: managerId, CompReason: "ضيافة",
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null,
            CompReasonCode: "hospitality");

        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // Legacy-parity audit (2026-09-04): a manual discount had no server-side authorization anywhere
    // - orders_discount did not even exist in the permission catalog. Fixed to require it whenever
    // DiscountPiastres > 0.
    [Fact]
    public async Task A_manual_discount_requires_the_orders_discount_permission()
    {
        var noDiscountCashier = await _factory.SeedUserAsync("كاشير بلا خصم", "cashier", "pos");
        var productId = await _factory.SeedProductAsync(10000);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: noDiscountCashier, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 1000, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 9000, SplitPayments: null, WalletProvider: null,
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);

        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var orderCount = await _factory.QueryAsync(db => db.Orders.CountAsync(o => o.CheckoutRequestId == request.CheckoutRequestId));
        Assert.Equal(0, orderCount);
    }

    [Fact]
    public async Task A_manual_discount_succeeds_for_a_cashier_holding_orders_discount()
    {
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", (await CashOrder($"req-{Guid.NewGuid():N}", 10000)) with { DiscountPiastres = 1000, AmountPaidPiastres = 9000 });
        response.EnsureSuccessStatusCode();
    }

    // Legacy-parity audit (2026-09-04): the reference app's own comp-reason-code taxonomy
    // (hospitality/complaint/kitchen_error/staff_meal/owner_guest) was never captured at checkout -
    // only a free-text CompReason reached the audit log. Fixed to require and persist one.
    [Fact]
    public async Task A_comp_checkout_requires_a_valid_comp_reason_code()
    {
        var managerId = await _factory.SeedUserAsync("مدير", "manager", "all");
        var approval = await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(managerId), "orders_comp", "اعتماد"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>();
        var productId = await _factory.SeedProductAsync(10000);

        CheckoutOrderRequest MakeRequest(string? reasonCode) => new(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 10000,
            CompApprovalId: approval!.ApprovalId, CompApproverUserId: managerId, CompReason: "ضيافة",
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null,
            CompReasonCode: reasonCode);

        var missing = await _client.PostAsJsonAsync("/api/orders/checkout", MakeRequest(null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missing.StatusCode);

        var invalid = await _client.PostAsJsonAsync("/api/orders/checkout", MakeRequest("made_up_reason"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);

        var valid = await _client.PostAsJsonAsync("/api/orders/checkout", MakeRequest("staff_meal"));
        valid.EnsureSuccessStatusCode();
        var order = await valid.Content.ReadFromJsonAsync<CheckoutOrderResult>();
        var storedCode = await _factory.QueryAsync(db => db.Orders.Where(o => o.Id == order!.OrderId).Select(o => o.CompReasonCode).FirstAsync());
        Assert.Equal("staff_meal", storedCode);
    }
}

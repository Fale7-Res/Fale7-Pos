namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Cash handed back in one shift for a sale made in another.
///
/// A closed shift is immutable, so a refund of one of its orders is posted against the shift that is
/// open now (RefundRules.ResolveOperationalShift) - and the cash physically leaves the drawer that is
/// open now. The expected-cash computation only ever looked at refunds on the shift's OWN orders, so
/// that cash vanished from the open shift's reconciliation: the drawer came up short by exactly the
/// refund, and above the threshold the cashier needed a manager to approve a shortage that was not one.
/// </summary>
public class CrossShiftRefundTests : IAsyncLifetime
{
    private const long OpeningCash = 50000;

    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private HttpClient _ticketClient = default!;
    private string _cashierId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?>
        {
            ["Backup:Automatic:Enabled"] = "false",
        });
        var database = await _factory.QueryAsync(db => Task.FromResult(db.Database.GetDbConnection().Database));
        Assert.StartsWith("FalahTest_", database);

        _client = _factory.CreateAuthenticatedClient();
        var ticketAgent = await _factory.SeedUserAsync("قارئ تذاكر الطباعة", "print_agent", "print_agent");
        _ticketClient = _factory.CreateClient();
        _ticketClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.TokenFor(ticketAgent));
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier",
            "pos", "orders_refund", "shifts_open_own", "shifts_blind_close", "cash_drawer");
        _factory.StationUserId = _cashierId;
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task A_cash_refund_of_an_earlier_shifts_order_lowers_the_open_shifts_expected_cash()
    {
        var earlier = await OpenShiftAsync();
        var order = await CheckoutAsync(earlier, 10000, "cash");
        await CloseExactlyAsync(earlier, OpeningCash + 10000);

        var current = await OpenShiftAsync();
        await RefundAsync(order.OrderId, 4000);

        // The 4000 left THIS drawer, so this drawer should hold 4000 less than it opened with.
        Assert.Equal(OpeningCash - 4000, await ExpectedCashAsync(current));
    }

    [Fact]
    public async Task A_cash_refund_within_the_same_shift_is_still_subtracted_exactly_once()
    {
        var shift = await OpenShiftAsync();
        var order = await CheckoutAsync(shift, 10000, "cash");
        await RefundAsync(order.OrderId, 4000);

        Assert.Equal(OpeningCash + 10000 - 4000, await ExpectedCashAsync(shift));
    }

    [Fact]
    public async Task A_card_refund_of_an_earlier_shifts_order_leaves_the_open_drawer_alone()
    {
        // The money goes back from the bank account, not the till.
        var earlier = await OpenShiftAsync();
        var order = await CheckoutAsync(earlier, 10000, "card");
        await CloseExactlyAsync(earlier, OpeningCash);

        var current = await OpenShiftAsync();
        await RefundAsync(order.OrderId, 4000);

        Assert.Equal(OpeningCash, await ExpectedCashAsync(current));
    }

    [Fact]
    public async Task A_card_refund_with_no_shift_open_is_refused_plainly_and_records_nothing()
    {
        // No drawer is touched, so the domain allows it - but every refund records the shift that
        // processed it, and there is none. It must say so, not report a key conflict.
        var earlier = await OpenShiftAsync();
        var order = await CheckoutAsync(earlier, 10000, "card");
        await CloseExactlyAsync(earlier, OpeningCash);

        var response = await _client.PostAsJsonAsync("/api/orders/refund",
            new RefundOrderRequest($"rreq-{Guid.NewGuid():N}", order.OrderId, 4000, "طلب العميل", _cashierId, null, null));

        Assert.Equal(System.Net.HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("وردية مفتوحة", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, await _factory.QueryAsync(db => db.OrderRefunds.CountAsync()));
    }

    [Fact]
    public async Task A_closed_shifts_handover_slip_is_not_rewritten_by_a_later_refund_of_its_order()
    {
        // The slip recomputes the breakdown that explains its expected figure every time it is
        // composed (a reprint does exactly this). A refund made in a LATER shift belongs to that
        // shift; it must not reach back and change the lines of the slip this shift was closed on.
        var earlier = await OpenShiftAsync();
        var order = await CheckoutAsync(earlier, 10000, "cash");
        await CloseExactlyAsync(earlier, OpeningCash + 10000);
        var before = await HandoverSlipTextAsync(earlier);

        await OpenShiftAsync();
        await RefundAsync(order.OrderId, 4000);

        Assert.Equal(before, await HandoverSlipTextAsync(earlier));
    }

    // ---------- Helpers ----------

    private async Task<string> OpenShiftAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "وردية", _cashierId, OpeningCash));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    private async Task<CheckoutOrderResult> CheckoutAsync(string shiftId, long totalPiastres, string paymentMethod)
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: paymentMethod,
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: totalPiastres, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    private async Task CloseExactlyAsync(string shiftId, long counted)
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"creq-{Guid.NewGuid():N}", shiftId, _cashierId, counted, null, null, null));
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<CloseShiftResult>())!;
        Assert.Equal(0, result.DifferencePiastres);
    }

    private async Task RefundAsync(string orderId, long amountPiastres)
    {
        var response = await _client.PostAsJsonAsync("/api/orders/refund",
            new RefundOrderRequest($"rreq-{Guid.NewGuid():N}", orderId, amountPiastres, "طلب العميل", _cashierId, null, null));
        response.EnsureSuccessStatusCode();
    }

    private async Task<long> ExpectedCashAsync(string shiftId)
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/prepare-close", new PrepareShiftCloseRequest(shiftId, 0));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PrepareShiftCloseResult>())!.ExpectedCashPiastres;
    }

    /// <summary>Every readable string on the handover slip queued when the shift closed, as the
    /// server composes it now. Its job id is keyed on the shift (DocumentPrintJobs.Build).</summary>
    private async Task<string> HandoverSlipTextAsync(string shiftId)
    {
        var ticket = await _ticketClient.GetFromJsonAsync<Falah.Application.Printing.ComposedTicketDto>(
            $"/api/print-jobs/printjob-auto-ShiftClose-{shiftId}-Receipt/ticket");
        return string.Join("\n", ticket!.Lines.SelectMany(TicketDtoText.Of));
    }
}

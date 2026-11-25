using System.Net;
using System.Net.Http.Json;
using Falah.Application.Users;
using Falah.Client.CashierWinUI.Models;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;

namespace Falah.Client.Tests;

public class CashierCorrectnessTests
{
    [Theory]
    [InlineData("١٢٣٫٤٥", 12345)] [InlineData("۱۲۳.۴۵", 12345)]
    [InlineData("0.01", 1)] [InlineData(" 200 ", 20000)]
    public void Money_entry_preserves_piastre_units(string input, long expected)
    { Assert.True(CashInput.TryParse(input, out var value)); Assert.Equal(expected, value); }

    [Theory]
    [InlineData("1.001")] [InlineData("-1")] [InlineData("100 ج")]
    [InlineData("1,000")] [InlineData("92233720368547758.08")]
    public void Ambiguous_or_unrepresentable_money_is_rejected(string input)
        => Assert.False(CashInput.TryParse(input, out _));

    [Fact]
    public void Exact_tender_and_change_use_egp_to_piastre_conversion()
    {
        var (vm, _) = Create(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        vm.Cart.Add(new CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 12550 });
        vm.OpenCheckoutCommand.Execute(null);
        Assert.Equal("125.50", vm.AmountPaidText);
        vm.AmountPaidText = "٢٠٠";
        Assert.Equal(7450, vm.ChangeDuePiastres);
        vm.QuickCashCommand.Execute("exact");
        Assert.Equal(0, vm.ChangeDuePiastres);
    }

    [Fact]
    public void Touch_keypad_replaces_the_initial_exact_amount()
    {
        var (vm, _) = Create(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        vm.Cart.Add(new CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 12550 });
        vm.OpenCheckoutCommand.Execute(null);
        vm.PressAmountKeyCommand.Execute("2"); vm.PressAmountKeyCommand.Execute("0"); vm.PressAmountKeyCommand.Execute("0");
        Assert.Equal("200", vm.AmountPaidText); Assert.Equal(7450, vm.ChangeDuePiastres);
    }

    [Fact]
    public async Task Lost_checkout_response_retries_identical_payload_despite_changed_fields()
    {
        var bodies = new List<string>();
        var (vm, session) = Create(async request => {
            bodies.Add(await request.Content!.ReadAsStringAsync());
            throw new HttpRequestException("Response lost after commit");
        });
        vm.Cart.Add(new CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 10000 });
        vm.OpenCheckoutCommand.Execute(null);
        await vm.SubmitPaymentCommand.ExecuteAsync(null);
        Assert.False(vm.CanEditOperation);
        Assert.True(session.HasUnresolvedOperation);
        vm.AmountPaidText = "invalid";
        await vm.SubmitPaymentCommand.ExecuteAsync(null);
        Assert.Equal(2, bodies.Count);
        Assert.Equal(bodies[0], bodies[1]);
        vm.CloseCheckoutCommand.Execute(null);
        Assert.True(vm.IsCheckoutOpen);
        vm.ClearCartCommand.Execute(null);
        Assert.Single(vm.Cart);
    }

    [Fact]
    public async Task Lost_cash_movement_response_retries_even_if_form_is_now_invalid()
    {
        var bodies = new List<string>();
        var (vm, _) = Create(async request => { bodies.Add(await request.Content!.ReadAsStringAsync()); throw new HttpRequestException(); });
        vm.OpenCashMovementCommand.Execute("cash_out");
        vm.CashMovementAmountText = "25"; vm.CashMovementReason = "test";
        await vm.SubmitCashMovementCommand.ExecuteAsync(null);
        vm.CashMovementAmountText = "invalid"; vm.CashMovementReason = "";
        await vm.SubmitCashMovementCommand.ExecuteAsync(null);
        Assert.Equal(2, bodies.Count); Assert.Equal(bodies[0], bodies[1]);
    }

    /// <summary>UI review 2026-11-24: an expense and a drawer cash-in were recorded, and the dialog simply vanished -
    /// the cashier could not tell a recorded expense from a dialog that was closed. The till's last-action line says so.</summary>
    [Fact]
    public async Task A_recorded_expense_is_confirmed_on_the_till()
    {
        var (vm, _) = Create(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent.Create(new Falah.Application.Expenses.AddExpenseResult("e1", "نظافة ومطهرات", 3000, "tre-drawer", true, "tm1", false)) }));
        vm.OpenAddExpenseCommand.Execute(null);
        vm.ExpenseAmountText = "30"; vm.ExpenseDescription = "صابون";
        await vm.SubmitExpenseCommand.ExecuteAsync(null);
        Assert.False(vm.IsAddExpenseOpen);
        Assert.Equal("تم تسجيل مصروف 30.00 ج", vm.LastSaleNote);
    }

    /// <summary>Delivery audit, owner decision 2026-11-24 (D3): a delivery is all cash with the driver or all paid.</summary>
    [Fact]
    public void A_delivery_order_cannot_be_split_between_two_payment_methods()
    {
        var (vm, _) = Create(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        vm.IsSplitPaymentEnabled = true;

        vm.OrderType = "delivery";

        Assert.False(vm.IsSplitPaymentEnabled);
        Assert.False(vm.CanSplitPayment);
        vm.IsSplitPaymentEnabled = true;
        Assert.False(vm.IsSplitPaymentEnabled);
        Assert.Contains("التوصيل", vm.CheckoutError);
    }

    /// <summary>Owner decision 2026-11-24: a delivery is paid cash, InstaPay or wallet - the card choice goes away.</summary>
    [Fact]
    public void A_delivery_order_offers_no_card_payment()
    {
        var (vm, _) = Create(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        vm.PaymentMethod = "card";

        vm.OrderType = "delivery";

        Assert.False(vm.CanPayByCard);
        Assert.Equal("cash", vm.PaymentMethod);
        vm.SetPaymentMethodCommand.Execute("card");
        Assert.Equal("cash", vm.PaymentMethod);
        vm.OrderType = "pickup";
        Assert.True(vm.CanPayByCard);
    }

    [Fact]
    public async Task A_recorded_cash_in_is_confirmed_on_the_till()
    {
        var (vm, _) = Create(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = JsonContent.Create(new Falah.Application.CashDrawer.CashierCashMovementResult("tm1", "cash_in", 10000, false)) }));
        vm.OpenCashMovementCommand.Execute("cash_in");
        vm.CashMovementAmountText = "100"; vm.CashMovementReason = "فكة";
        await vm.SubmitCashMovementCommand.ExecuteAsync(null);
        Assert.False(vm.IsCashMovementOpen);
        Assert.Equal("تم تسجيل إيداع نقدي في الدرج 100.00 ج", vm.LastSaleNote);
    }

    [Theory]
    [InlineData("null")] [InlineData("not-json")]
    public async Task Malformed_success_response_is_unresolved(string body)
    {
        var api = new ApiClient(new HttpClient(new Handler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") }))) { BaseAddress = new Uri("http://test") });
        var result = await api.GetCurrentShiftAsync();
        Assert.False(result.Success); Assert.True(result.OutcomeUnknown);
    }

    [Fact]
    public void Draft_is_retained_per_user_across_navigation()
    {
        var (vm, session) = Create(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        session.CurrentUser = new("other", "Other", "cashier", new[] { "pos" });
        Assert.NotSame(vm, session.GetCashier(new()));
        session.CurrentUser = new("cashier", "Cashier", "cashier", new[] { "all" });
        Assert.Same(vm, session.GetCashier(new()));
    }

    [Fact]
    public async Task Tax_snapshot_uses_existing_domain_rounding_for_exact_tender()
    {
        var (vm, _) = Create(request => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
            Content = request.RequestUri!.AbsolutePath == "/api/menu"
                ? JsonContent.Create(new { categories = Array.Empty<object>() })
                : JsonContent.Create(new[] { new { id = "tax", name = "VAT", rateBasisPoints = 1400, mode = "Exclusive", enabled = true } }) }));
        await vm.LoadMenuAsync();
        vm.Cart.Add(new CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 10001 });
        vm.OpenCheckoutCommand.Execute(null);
        Assert.Equal(11401, vm.GrandTotalPiastres);
        Assert.Equal("114.01", vm.AmountPaidText);
    }

    [Fact]
    public async Task Lost_refund_response_preserves_id_amount_and_reason()
    {
        var bodies = new List<string>();
        var (vm, _) = Create(async request => { bodies.Add(await request.Content!.ReadAsStringAsync()); throw new HttpRequestException(); });
        vm.OrderLookupResult = System.Text.Json.JsonSerializer.Deserialize<Falah.Application.Orders.OrderDetailsDto>(
            """{"id":"order","orderNumber":"42","items":[],"isPaid":true,"paymentMethod":"cash","status":"completed","totalPiastres":10000,"refundAmountPiastres":0}""",
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        vm.OpenRefundCommand.Execute(null); vm.RefundAmountText = "25"; vm.RefundReason = "سبب الاسترداد";
        await vm.SubmitRefundCommand.ExecuteAsync(null);
        vm.RefundAmountText = "bad"; vm.RefundReason = "changed";
        await vm.SubmitRefundCommand.ExecuteAsync(null);
        Assert.Equal(2, bodies.Count); Assert.Equal(bodies[0], bodies[1]);
        vm.CancelRefundCommand.Execute(null); Assert.True(vm.IsRefundOpen);
    }

    [Fact]
    public async Task Unauthorized_discount_never_reaches_checkout_api()
    {
        var calls = 0;
        var (vm, session) = Create(_ => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)); });
        session.CurrentUser = new("cashier", "Cashier", "cashier", new[] { "pos" });
        vm.Cart.Add(new CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 10000 });
        vm.OpenCheckoutCommand.Execute(null); vm.IsDiscountEnabled = true; vm.DiscountAmountText = "10";
        await vm.SubmitPaymentCommand.ExecuteAsync(null);
        Assert.Equal(0, calls); Assert.NotNull(vm.CheckoutError);
    }

    // Owner decisions 2026-11-18: a discount above 10% of the items, and a refund of 500 ج or more,
    // need a manager's PIN even from a cashier who holds the permission. The till asks for it up front.
    [Fact]
    public void A_discount_above_ten_percent_asks_for_a_manager_and_one_within_it_does_not()
    {
        var (vm, _) = Create(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        vm.Cart.Add(new CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 10000 });
        vm.OpenCheckoutCommand.Execute(null); vm.IsDiscountEnabled = true;

        vm.DiscountAmountText = "10";
        Assert.False(vm.DiscountNeedsApproval);
        vm.DiscountAmountText = "10.01";
        Assert.True(vm.DiscountNeedsApproval);
    }

    [Fact]
    public async Task A_discount_above_the_limit_sends_the_manager_approval_with_the_checkout()
    {
        var paths = new List<string>(); string? checkoutBody = null;
        var (vm, _) = Create(async request =>
        {
            paths.Add(request.RequestUri!.AbsolutePath);
            if (request.RequestUri.AbsolutePath == "/api/approvals/issue")
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { approvalId = "ap-1", approverUserId = "mgr", approverName = "المدير", action = "orders_discount", approvedAtUtc = DateTimeOffset.UtcNow }) };
            if (request.RequestUri.AbsolutePath == "/api/orders/checkout") checkoutBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.UnprocessableEntity) { Content = JsonContent.Create(new { error = "stop" }) };
        });
        vm.Cart.Add(new CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 10000 });
        vm.OpenCheckoutCommand.Execute(null); vm.IsDiscountEnabled = true; vm.DiscountAmountText = "50";
        vm.DiscountManagerPin = "4321";

        await vm.SubmitPaymentCommand.ExecuteAsync(null);

        Assert.Contains("/api/approvals/issue", paths);
        Assert.NotNull(checkoutBody);
        Assert.Contains("\"discountApprovalId\":\"ap-1\"", checkoutBody);
        Assert.Contains("\"discountApproverUserId\":\"mgr\"", checkoutBody);
    }

    [Theory]
    [InlineData("499.99", false)]
    [InlineData("500", true)]
    public void A_refund_of_500_or_more_asks_for_a_manager_even_from_a_permission_holder(string amount, bool needsManager)
    {
        var (vm, _) = Create(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        vm.OrderLookupResult = System.Text.Json.JsonSerializer.Deserialize<Falah.Application.Orders.OrderDetailsDto>(
            """{"id":"order","orderNumber":"42","items":[],"isPaid":true,"paymentMethod":"cash","status":"completed","totalPiastres":80000,"refundAmountPiastres":0}""",
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        vm.OpenRefundCommand.Execute(null);

        vm.RefundAmountText = amount;

        Assert.Equal(needsManager, vm.RefundNeedsApproval);
    }

    // The server lets a manager authorise their own large discount or refund, so the till does not
    // ask a manager at the till for a manager's PIN.
    [Fact]
    public void A_manager_at_the_till_is_not_asked_for_a_manager_pin()
    {
        var (vm, session) = Create(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        session.CurrentUser = new UserSummaryDto("mgr", "مدير", "manager", new[] { "all" });
        vm.Cart.Add(new CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 10000 });
        vm.OpenCheckoutCommand.Execute(null); vm.IsDiscountEnabled = true;
        vm.DiscountAmountText = "50";
        Assert.False(vm.DiscountNeedsApproval);

        vm.OrderLookupResult = System.Text.Json.JsonSerializer.Deserialize<Falah.Application.Orders.OrderDetailsDto>(
            """{"id":"order","orderNumber":"42","items":[],"isPaid":true,"paymentMethod":"cash","status":"completed","totalPiastres":80000,"refundAmountPiastres":0}""",
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        vm.OpenRefundCommand.Execute(null);
        vm.RefundAmountText = "800";
        Assert.False(vm.RefundNeedsApproval);
    }

    // Owner, 2026-11-19: a sale that went through needs no "تم تسجيل الطلب / طلب جديد" card - the
    // till goes straight back to an empty order. Owner's trial 2026-11-25 ("شيل اللي بتظهر دي من أي حاجة"):
    // not even when something is left to do - an unconfirmed transfer or tickets the server could not
    // queue now leave a warning above the footer, naming the order and where to finish it, and the next
    // order starts at once. The card still opens from the order lookup to confirm or cancel a transfer.
    [Theory]
    [InlineData("null", true, null)]                     // paid, printed: nothing to say
    [InlineData("\"pending\"", true, "التحويل")]         // transfer awaiting confirmation
    [InlineData("null", false, "المطبخ")]                // tickets not queued
    public async Task After_payment_no_card_stops_the_till_and_what_is_left_to_do_is_a_warning(
        string electronicStatus, bool printJobsQueued, string? notice)
    {
        var (vm, _) = Create(r => Task.FromResult(r.RequestUri!.AbsolutePath == "/api/orders/checkout"
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(
                $$"""{"orderId":"o1","orderNumber":"#0128","sequenceNumber":128,"totalPiastres":8500,"amountPaidPiastres":10000,"changeDuePiastres":1500,"status":"completed","isPaid":true,"electronicPaymentStatus":{{electronicStatus}},"wasReplayed":false,"printJobsQueued":{{(printJobsQueued ? "true" : "false")}}}""",
                System.Text.Encoding.UTF8, "application/json") }
            : new HttpResponseMessage(HttpStatusCode.NotFound)));
        vm.Cart.Add(new CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 8500 });
        vm.OpenCheckoutCommand.Execute(null);
        vm.AmountPaidText = "100";

        await vm.SubmitPaymentCommand.ExecuteAsync(null);

        Assert.False(vm.HasCheckoutResult);
        Assert.Empty(vm.Cart);
        Assert.False(vm.IsCheckoutOpen);
        Assert.True(vm.CanUseWorkspaceShortcuts);   // the next order can start at once
        if (notice is null) Assert.Null(vm.PostSaleNotice);
        else
        {
            Assert.Contains("#0128", vm.PostSaleNotice);
            Assert.Contains(notice, vm.PostSaleNotice);
        }

        // Gone once the next payment opens, like the footer's last-sale line.
        vm.Cart.Add(new CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 8500 });
        vm.OpenCheckoutCommand.Execute(null);
        Assert.Null(vm.PostSaleNotice);
    }

    // Independent audit 2026-11-23, F-07: with no card after a paid sale (owner, 2026-11-19) and the dialog's change
    // row scrolled out of sight, the cashier never saw the change. The footer now keeps one quiet line - the order
    // number and the change - until the next payment is opened.
    [Fact]
    public async Task After_a_paid_sale_the_footer_says_the_order_number_and_the_change_until_the_next_payment()
    {
        var (vm, _) = Create(r => Task.FromResult(r.RequestUri!.AbsolutePath == "/api/orders/checkout"
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(
                """{"orderId":"o1","orderNumber":"#0128","sequenceNumber":128,"totalPiastres":4500,"amountPaidPiastres":10000,"changeDuePiastres":5500,"status":"completed","isPaid":true,"electronicPaymentStatus":null,"wasReplayed":false,"printJobsQueued":true}""",
                System.Text.Encoding.UTF8, "application/json") }
            : new HttpResponseMessage(HttpStatusCode.NotFound)));
        vm.Cart.Add(new CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 4500 });
        vm.OpenCheckoutCommand.Execute(null);
        vm.AmountPaidText = "100";

        await vm.SubmitPaymentCommand.ExecuteAsync(null);

        Assert.False(vm.HasCheckoutResult);
        Assert.Contains("#0128", vm.LastSaleNote);
        Assert.Contains("55.00", vm.LastSaleNote);

        vm.Cart.Add(new CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 4500 });
        vm.OpenCheckoutCommand.Execute(null);
        Assert.Null(vm.LastSaleNote);
    }

    private static (CashierViewModel, AppSession) Create(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)
    {
        var session = new AppSession(new ApiClient(new HttpClient(new Handler(send)) { BaseAddress = new Uri("http://test") })) {
            CurrentUser = new UserSummaryDto("cashier", "Cashier", "cashier", new[] { "all" }), ShiftId = "shift" };
        return (session.GetCashier(new()), session);
    }
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }
}

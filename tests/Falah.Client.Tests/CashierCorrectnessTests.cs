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

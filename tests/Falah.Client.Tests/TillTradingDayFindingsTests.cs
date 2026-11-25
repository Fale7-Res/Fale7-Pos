using System.Net;
using System.Text;
using System.Text.Json;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Falah.Application.Users;
using Falah.Client.CashierWinUI.Models;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Audit 2026-11-25: findings from a full trading day driven through the real Cashier app.</summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class TillTradingDayFindingsTests
{
    private static readonly string Lrm = ((char)0x200E).ToString();   // left-to-right mark

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static CashierViewModel Till(Func<HttpRequestMessage, Task<HttpResponseMessage>>? send = null)
    {
        send ??= _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        var session = new AppSession(new ApiClient(new HttpClient(new Handler(send)) { BaseAddress = new Uri("http://test") }))
        { CurrentUser = new UserSummaryDto("cashier", "كاشير", "cashier", new[] { "all" }), ShiftId = "shift" };
        return session.GetCashier(new());
    }

    private static CheckoutOrderResult Result(string json) =>
        JsonSerializer.Deserialize<CheckoutOrderResult>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    // U21: a comp lowered the total from 90 to 45 but "amount received" stayed 90, so the screen told the cashier to
    // hand back 45 the customer never paid.
    [Fact]
    public void A_comp_moves_the_prefilled_cash_tender_to_the_new_total_but_never_a_typed_one()
    {
        var vm = Till();
        vm.Cart.Add(new CartLine { ProductId = "p", ProductName = "كبدة", UnitPricePiastres = 4500, Quantity = 2 });
        vm.OpenCheckoutCommand.Execute(null);
        Assert.Equal("90.00", vm.AmountPaidText);

        vm.IsCompEnabled = true;
        vm.CompAmountText = "45";

        Assert.Equal("45.00", vm.AmountPaidText);
        Assert.Equal(0, vm.ChangeDuePiastres);

        vm.AmountPaidText = "100";        // the cashier typed what the customer handed over
        vm.CompAmountText = "40";
        Assert.Equal("100", vm.AmountPaidText);
    }

    // U11: after a sale the menu search kept its word ("فلافل"), so the next customer's order started on a filtered
    // menu - even after a lock and another cashier's login.
    [Fact]
    public async Task A_completed_sale_clears_the_menu_search_and_the_footer_keeps_the_order_number_left_to_right()
    {
        var vm = Till(r => Task.FromResult(r.RequestUri!.AbsolutePath == "/api/orders/checkout"
            ? Json("""{"orderId":"o1","orderNumber":"#0130","sequenceNumber":130,"totalPiastres":4500,"amountPaidPiastres":5000,"changeDuePiastres":500,"status":"completed","isPaid":true,"electronicPaymentStatus":null,"wasReplayed":false,"printJobsQueued":true}""")
            : new HttpResponseMessage(HttpStatusCode.NotFound)));
        vm.SearchQuery = "فلافل";
        vm.Cart.Add(new CartLine { ProductId = "p", ProductName = "فلافل", UnitPricePiastres = 4500 });
        vm.OpenCheckoutCommand.Execute(null);
        vm.AmountPaidText = "50";

        await vm.SubmitPaymentCommand.ExecuteAsync(null);

        Assert.Equal("", vm.SearchQuery);
        // U10: "#0130" inside Arabic text rendered as "0130#"; the number is isolated left-to-right.
        Assert.Contains(Lrm + "#0130" + Lrm, vm.LastSaleNote);
    }

    // U9 / U20: a transfer NOT received showed a green tick; a cancelled order showed a green tick; a transfer
    // confirmed later was titled "تم تسجيل الطلب" as if a new sale had been made.
    [Fact]
    public void The_result_card_marks_waiting_cancelled_and_confirmed_for_what_they_are()
    {
        var vm = Till();
        vm.LastCheckoutResult = Result("""{"orderId":"o1","orderNumber":"#0130","sequenceNumber":130,"totalPiastres":4500,"amountPaidPiastres":0,"changeDuePiastres":0,"status":"completed","isPaid":false,"electronicPaymentStatus":"pending","wasReplayed":false,"printJobsQueued":true}""");
        Assert.True(vm.ShowPendingMark);
        Assert.False(vm.ShowSuccessMark);
        Assert.Equal(Lrm + "#0130" + Lrm, vm.LastCheckoutOrderNumberDisplay);

        vm.LastCheckoutResult = vm.LastCheckoutResult! with { ElectronicPaymentStatus = "confirmed", IsPaid = true };
        Assert.True(vm.ShowSuccessMark);
        Assert.Equal("تم تأكيد استلام التحويل", vm.CheckoutTitle);

        vm.LastCheckoutResult = vm.LastCheckoutResult with { Status = "cancelled" };
        Assert.True(vm.ShowCancelledMark);
        Assert.False(vm.ShowSuccessMark);
    }

    // U14: the refund amount is the whole refundable balance by default; the button now names the amount it refunds.
    [Fact]
    public void The_refund_button_says_how_much_it_refunds()
    {
        var vm = Till();
        vm.OrderLookupResult = new OrderDetailsDto("o1", "#0128", 128, "takeaway", "cash", Array.Empty<OrderItemDto>(), 14500, 0, 0,
            14500, 20000, 5500, "completed", true, null, 0, DateTimeOffset.UtcNow, "cashier", null, null, null, null, null, null,
            null, null, null, null, null, null, 0, 0, "shift");
        vm.OpenRefundCommand.Execute(null);
        Assert.Equal("تأكيد استرداد 145.00 ج", vm.RefundConfirmText);

        vm.RefundAmountText = "45";
        Assert.Equal("تأكيد استرداد 45.00 ج", vm.RefundConfirmText);
    }

    // U26: the queue card printed "الحي السابع · المنطقة الحي السابع، ..." - the area, then the address that names it.
    [Theory]
    [InlineData("الحي السابع", "المنطقة الحي السابع، شارع النصر", "المنطقة الحي السابع، شارع النصر")]
    [InlineData("الحي السابع", "شارع النصر، عمارة 12", "الحي السابع · شارع النصر، عمارة 12")]
    [InlineData("الحي السابع", null, "الحي السابع")]
    public void The_queue_card_names_the_area_once(string? area, string? address, string expected)
    {
        var card = new DeliveryViewModel.QueueCard { Order = new QueueOrderDto("o1", "#0134", "delivery", "خالد", "011", area, address,
            8000, "cash", "with_driver", "new", "جديد", null, null, false, false, false, null, null, null, null, 8000,
            false, null, DateTimeOffset.UtcNow, null, null, null, false) };
        Assert.Equal(expected, card.AddressDisplay);
    }

    // U29: a re-count that matched the expected cash (difference 0.00) was told "the difference is over the limit".
    [Fact]
    public void A_recount_says_a_recount_needs_a_manager_not_that_the_difference_is_over_the_limit()
    {
        var vm = Till();
        vm.CloseShiftPreview = new PrepareShiftCloseResult("shift", 78000, 78000, 0, RequiresManagerApproval: true, IsRecount: true);
        Assert.Contains("إعادة العدّ", vm.CloseShiftApprovalMessage);
        Assert.DoesNotContain("يتجاوز", vm.CloseShiftApprovalMessage);

        vm.CloseShiftPreview = new PrepareShiftCloseResult("shift", 78000, 71500, -6500, RequiresManagerApproval: true);
        Assert.Contains("يتجاوز", vm.CloseShiftApprovalMessage);
    }
}

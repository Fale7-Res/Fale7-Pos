using System.Net;
using System.Net.Http.Json;
using System.Text;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Falah.Client.CashierWinUI.Models;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;

namespace Falah.Client.Tests;

public class CloseoutRegressionTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }
    private static HttpResponseMessage Json(string text, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
    private static AppSession Session(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) =>
        new(new ApiClient(new HttpClient(new Handler(send)) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("actor", "اختبار", "manager", new[] { "all" }), ShiftId = "shift" };

    [Fact]
    public async Task Pickup_checkout_sends_the_identity_collected_by_the_form()
    {
        CheckoutOrderRequest? sent = null;
        var vm = new CashierViewModel(Session(async request => {
            sent = await request.Content!.ReadFromJsonAsync<CheckoutOrderRequest>();
            return Json("{\"error\":\"fixture stops before sale\"}", HttpStatusCode.UnprocessableEntity);
        }), new HeaderViewModel());
        vm.Cart.Add(new CartLine { ProductId = "product", ProductName = "شاورما", UnitPricePiastres = 8500 });
        vm.OrderType = "pickup";
        vm.CustomerName = "سارة";
        vm.CustomerPhone = "01012345678";
        vm.AmountPaidText = "100";
        await vm.SubmitPaymentCommand.ExecuteAsync(null);
        Assert.NotNull(sent);
        Assert.Equal("سارة", sent!.CustomerName);
        Assert.Equal("01012345678", sent.CustomerPhone);
        Assert.Equal("pickup", sent.OrderType);
        Assert.Null(sent.DeliveryArea);
        Assert.Equal(0, sent.DeliveryFeePiastres);
    }

    [Fact]
    public async Task Uncertain_safe_drop_keeps_the_exact_request_and_blocks_editing()
    {
        var sent = new List<string>();
        var vm = new ControlCenterViewModel(Session(async r => {
            sent.Add(await r.Content!.ReadAsStringAsync());
            throw new HttpRequestException("response lost");
        }));
        vm.IsSafeDropOpen = true;
        vm.SafeDropAmountText = "100";
        vm.SafeDropReason = "توريد";
        await vm.SubmitSafeDropCommand.ExecuteAsync(null);
        Assert.True(vm.HasPendingOperation);
        Assert.False(vm.CanEditAction);
        vm.CloseSafeDropCommand.Execute(null);
        Assert.True(vm.IsSafeDropOpen);
        vm.SafeDropAmountText = "200";
        await vm.SubmitSafeDropCommand.ExecuteAsync(null);
        Assert.Equal(2, sent.Count);
        Assert.Equal(sent[0], sent[1]);
    }

    [Fact]
    public async Task Daily_statement_reloads_if_the_date_changes_while_loading()
    {
        var started = new TaskCompletionSource();
        var release = new TaskCompletionSource();
        var requests = new List<string>();
        var vm = new DailyStatementViewModel(Session(async r => {
            requests.Add(r.RequestUri!.Query);
            if (requests.Count == 1) { started.SetResult(); await release.Task; }
            return Json("{\"error\":\"fixture\"}", HttpStatusCode.ServiceUnavailable);
        }));
        var loading = vm.LoadAsync();
        await started.Task;
        vm.PreviousDayCommand.Execute(null);
        var selected = vm.SelectedDate;
        release.SetResult();
        await loading;
        Assert.Equal(2, requests.Count);
        Assert.Contains(selected.ToString("yyyy-MM-dd"), requests[1]);
        Assert.False(vm.IsLoading);
    }
    [Fact]
    public async Task Daily_statement_never_shows_a_response_for_a_date_no_longer_selected()
    {
        var firstStarted = new TaskCompletionSource();
        var releaseFirst = new TaskCompletionSource();
        var secondStarted = new TaskCompletionSource();
        var releaseSecond = new TaskCompletionSource();
        var requests = 0;
        var vm = new DailyStatementViewModel(Session(async r => {
            if (++requests == 1)
            {
                firstStarted.SetResult(); await releaseFirst.Task;
                return Json("{\"error\":\"stale-day\"}", HttpStatusCode.UnprocessableEntity);
            }
            secondStarted.SetResult(); await releaseSecond.Task;
            return Json("{\"error\":\"selected-day\"}", HttpStatusCode.UnprocessableEntity);
        }));
        var loading = vm.LoadAsync();
        await firstStarted.Task;
        vm.PreviousDayCommand.Execute(null);   // the user moves on while the first day is loading

        releaseFirst.SetResult();               // the first day's answer arrives late
        await secondStarted.Task;
        Assert.DoesNotContain("stale-day", vm.LoadError ?? "");

        releaseSecond.SetResult();
        await loading;
        Assert.Equal("selected-day", vm.LoadError);
    }
    /// <summary>The reprint buttons on the Shifts and Daily Statement screens ask the server to
    /// reproduce a document, and do nothing else: one POST to the document-reprint endpoint, with
    /// the document's own id and a reason. No shift is opened or closed, no expense is recorded.</summary>
    [Fact]
    public async Task The_shift_and_expense_reprint_buttons_only_ask_for_a_reprint()
    {
        var calls = new List<(string Path, Falah.Application.Printing.ReprintDocumentRequest Body)>();
        async Task<HttpResponseMessage> Record(HttpRequestMessage request)
        {
            calls.Add((request.RequestUri!.AbsolutePath,
                (await request.Content!.ReadFromJsonAsync<Falah.Application.Printing.ReprintDocumentRequest>())!));
            return Json("{\"jobs\":[],\"wasReplayed\":false}");
        }

        var shifts = new ShiftsViewModel(Session(Record), new HeaderViewModel());
        var row = new ShiftsViewModel.ShiftRow(new Falah.Application.Reports.ShiftSummaryDto(
            "shift-77", "صباحية", "user-1", "Closed", 50_000, 60_000, 60_000, 0,
            new Falah.Application.Reports.SalesReportDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            0, "أحمد", "أحمد", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        await shifts.ReprintShiftOpenCommand.ExecuteAsync(row);
        await shifts.ReprintShiftCloseCommand.ExecuteAsync(row);

        var statement = new DailyStatementViewModel(Session(Record));
        await statement.ReprintExpenseCommand.ExecuteAsync(new Falah.Application.Reports.DailyExpenseLineDto(
            "exp-9", DateTimeOffset.UtcNow, "مستلزمات", "أكياس", 16_000, "أحمد", null, "tre-drawer", "shift-77"));

        Assert.Equal(3, calls.Count);
        Assert.All(calls, c => Assert.Equal("/api/print-jobs/reprint-document", c.Path));
        Assert.Equal(new[] { "ShiftOpen", "ShiftClose", "Expense" }, calls.Select(c => c.Body.DocumentKind));
        Assert.Equal(new[] { "shift-77", "shift-77", "exp-9" }, calls.Select(c => c.Body.DocumentId));
        Assert.All(calls, c => Assert.False(string.IsNullOrWhiteSpace(c.Body.Reason)));
        // Every press is its own reprint, so a reviewer sees each one.
        Assert.Equal(3, calls.Select(c => c.Body.RequestId).Distinct().Count());
    }
    /// <summary>The open-shift dialog offers the configured float rather than a number baked into
    /// the screen, and whatever the cashier types over it is what gets sent.</summary>
    [Fact]
    public async Task The_open_shift_dialog_offers_the_configured_default_and_sends_what_was_typed()
    {
        OpenShiftRequest? sent = null;
        var session = Session(async request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/api/settings")
                return Json("{\"defaultOpeningCashPiastres\":150000,\"restaurantName\":\"فالح أبو العنبه\"}");
            if (path == "/api/treasury/balance") return Json("{\"accountId\":\"tre-main\",\"balancePiastres\":500000}");
            if (path == "/api/shifts/open")
            {
                sent = await request.Content!.ReadFromJsonAsync<OpenShiftRequest>();
                return Json("{\"error\":\"fixture stops before the shift opens\"}", HttpStatusCode.UnprocessableEntity);
            }
            return Json("{}", HttpStatusCode.NotFound);
        });

        var vm = new ShiftsViewModel(session, new HeaderViewModel());
        await vm.OpenOpenShiftModalCommand.ExecuteAsync(null);

        Assert.True(vm.IsOpenShiftModalOpen);
        Assert.Equal("1500.00", vm.OpeningCashText);   // 150000 piastres, offered

        vm.OpeningCashText = "1750";                    // the cashier types over it
        await vm.ConfirmOpenShiftCommand.ExecuteAsync(null);

        Assert.Equal(175_000, sent!.OpeningCashPiastres);
    }

    /// <summary>A treasury that holds less than the configured float is never offered as more than
    /// it has - the server would refuse handing out cash that is not there.</summary>
    [Fact]
    public async Task The_offered_float_is_capped_by_what_the_main_treasury_holds()
    {
        var session = Session(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/api/settings") return Task.FromResult(Json("{\"defaultOpeningCashPiastres\":150000}"));
            if (path == "/api/treasury/balance") return Task.FromResult(Json("{\"accountId\":\"tre-main\",\"balancePiastres\":40000}"));
            return Task.FromResult(Json("{}", HttpStatusCode.NotFound));
        });

        var vm = new ShiftsViewModel(session, new HeaderViewModel());
        await vm.OpenOpenShiftModalCommand.ExecuteAsync(null);

        Assert.Equal("400.00", vm.OpeningCashText);
    }
}

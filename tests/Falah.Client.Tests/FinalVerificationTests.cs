using System.Net;
using System.Text;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using CC = Falah.Client.CallCenterWinUI;

namespace Falah.Client.Tests;

public class FinalVerificationTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpClient Http(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) =>
        new(new Handler(send)) { BaseAddress = new Uri("http://fixture.invalid") };

    private static AppSession Session(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) =>
        new(new ApiClient(Http(send))) { CurrentUser = new("actor", "اختبار", "manager", new[] { "all" }) };

    private static string ReadFixture(string path) => path switch
    {
        "/api/menu" => """{"categories":[]} """,
        "/api/business-day/current" => """{"id":"day","businessDate":"2026-09-08","status":"Open"}""",
        "/api/reports/sales" => "{}",
        "/api/reports/expenses" => """{"totalPiastres":12345,"expenses":[]}""",
        "/api/reports/delivery-outcomes" => "{}",
        "/api/reports/tax" => "{}",
        "/api/reports/food-cost" => "{}",
        _ => "[]"
    };

    [Theory]
    [InlineData("menu", "/api/tax-rules")]
    [InlineData("menu", "/api/price-rules")]
    [InlineData("purchasing", "/api/purchases")]
    [InlineData("purchasing", "/api/inventory/items")]
    [InlineData("treasury", "/api/treasury/movements")]
    [InlineData("delivery", "/api/delivery/drivers")]
    [InlineData("users", "/api/permissions/catalog")]
    [InlineData("recipes", "/api/menu")]
    [InlineData("inventory", "/api/inventory/movements")]
    public async Task Secondary_read_failure_is_visible_and_retry_recovers(string module, string failedPath)
    {
        var fail = true;
        var hitFailure = false;
        var session = Session(r => {
            var path = r.RequestUri!.AbsolutePath;
            if (fail && path == failedPath) { hitFailure = true; return Task.FromResult(Json("{\"error\":\"secondary unavailable\"}", HttpStatusCode.ServiceUnavailable)); }
            return Task.FromResult(Json(ReadFixture(path)));
        });
        var vm = module switch {
            "menu" => (object)new MenuViewModel(session), "purchasing" => new PurchasingViewModel(session),
            "treasury" => new TreasuryViewModel(session), "users" => new UsersViewModel(session),
            "recipes" => new RecipesViewModel(session), "inventory" => new InventoryViewModel(session), _ => new DeliveryViewModel(session)
        };
        async Task Load() => await (Task)vm.GetType().GetMethod("LoadAsync")!.Invoke(vm, null)!;
        await Load();
        Assert.True(hitFailure, "The test must exercise the actual secondary endpoint.");
        Assert.NotNull(vm.GetType().GetProperty("LoadError")!.GetValue(vm));
        fail = false;
        await Load();
        Assert.Null(vm.GetType().GetProperty("LoadError")!.GetValue(vm));
    }

    [Fact]
    public async Task Missing_blocker_response_never_means_business_day_can_close()
    {
        var fail = true;
        var session = Session(r => Task.FromResult(r.RequestUri!.AbsolutePath.Contains("close-blockers") && fail
            ? Json("{\"error\":\"unavailable\"}", HttpStatusCode.ServiceUnavailable)
            : Json(ReadFixture(r.RequestUri!.AbsolutePath))));
        var vm = new ControlCenterViewModel(session);
        await vm.LoadAsync();
        Assert.NotNull(vm.BusinessDay);
        Assert.NotNull(vm.LoadError);
        Assert.False(vm.CanClose);
        fail = false;
        await vm.LoadAsync();
        Assert.Null(vm.LoadError);
        Assert.True(vm.CanClose);
    }

    [Fact]
    public async Task Failed_expenses_are_not_presented_as_a_zero_expense_report()
    {
        var vm = new ReportsViewModel(Session(r => Task.FromResult(r.RequestUri!.AbsolutePath == "/api/reports/expenses"
            ? Json("{\"error\":\"unavailable\"}", HttpStatusCode.ServiceUnavailable)
            : Json(ReadFixture(r.RequestUri!.AbsolutePath)))));
        await vm.LoadAsync();
        Assert.NotNull(vm.LoadError);
        Assert.False(vm.HasData);
    }

    [Fact]
    public async Task Changing_report_date_during_a_read_reloads_a_consistent_new_date()
    {
        var started = new TaskCompletionSource();
        var release = new TaskCompletionSource();
        var requests = new List<Uri>();
        var vm = new ReportsViewModel(Session(async r => {
            requests.Add(r.RequestUri!);
            if (requests.Count == 1) { started.SetResult(); await release.Task; }
            return Json(ReadFixture(r.RequestUri!.AbsolutePath));
        }));
        var original = vm.BusinessDate;
        var loading = vm.LoadAsync();
        await started.Task;
        vm.NextDayCommand.Execute(null);
        Assert.False(vm.HasData);
        release.SetResult();
        await loading;
        Assert.Null(vm.LoadError);
        Assert.True(vm.HasData);
        Assert.Equal(18, requests.Count);
        Assert.All(requests.Take(9), r => Assert.Contains(original.ToString("yyyy-MM-dd"), r.Query));
        Assert.All(requests.Skip(9), r => Assert.Contains(original.AddDays(1).ToString("yyyy-MM-dd"), r.Query));
    }

    [Fact]
    public async Task Printing_empty_state_is_notified_after_loading_finishes()
    {
        var vm = new PrintingViewModel(Session(_ => Task.FromResult(Json("[]"))));
        var seen = false;
        vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.HasNoPending) && vm.HasNoPending) seen = true; };
        await vm.LoadAsync();
        Assert.True(seen);
        Assert.True(vm.HasNoPending);
    }

    [Fact]
    public async Task Payroll_does_not_show_a_failed_line_read_as_zero_payable()
    {
        var vm = new PayrollViewModel(Session(_ => Task.FromResult(
            Json("{\"error\":\"unavailable\"}", HttpStatusCode.ServiceUnavailable))));
        vm.SelectedRun = new Falah.Application.Employees.PayrollRunDto(
            "run", "2026-09", "reviewed", "actor", DateTimeOffset.UtcNow, null, null, null, null, null, null);
        for (var i = 0; vm.IsLoadingLines && i < 100; i++) await Task.Delay(10);
        Assert.False(vm.IsLoadingLines);
        Assert.NotNull(vm.LoadError);
        Assert.Equal("—", vm.SelectedRunTotalDisplay);
    }

    [Fact]
    public void Payroll_approval_permission_alone_does_not_bypass_the_management_gate()
    {
        var session = Session(_ => Task.FromResult(Json("[]")));
        session.CurrentUser = new("actor", "اختبار", "manager", new[] { "payroll_approve" });
        var vm = new PayrollViewModel(session) {
            SelectedRun = new Falah.Application.Employees.PayrollRunDto(
                "run", "2026-09", "reviewed", "actor", DateTimeOffset.UtcNow, null, null, null, null, null, null)
        };
        Assert.False(vm.CanAdvance);
    }

    [Fact]
    public async Task Control_center_view_permission_cannot_change_the_close_checklist()
    {
        var writes = 0;
        var session = Session(r => {
            if (r.Method != HttpMethod.Get) writes++;
            return Task.FromResult(Json(ReadFixture(r.RequestUri!.AbsolutePath)));
        });
        session.CurrentUser = new("actor", "اختبار", "manager", new[] { "control_center" });
        var vm = new ControlCenterViewModel(session);
        await vm.LoadAsync();
        Assert.True(vm.HasBusinessDay);
        Assert.False(vm.CanControl);
        Assert.False(vm.CanClose);
        // Even direct command invocation must respect the server's write permission.
        await vm.ToggleChecklistItemCommand.ExecuteAsync(null);
        Assert.Equal(0, writes);
    }

    [Theory]
    [InlineData("deposit")]
    [InlineData("withdraw")]
    [InlineData("transfer")]
    public async Task Treasury_retries_original_payload_and_blocks_abandonment_after_an_unknown_outcome(string action)
    {
        var bodies = new List<string>();
        var notifications = 0;
        var session = Session(async r => {
            if (r.Method == HttpMethod.Get) return Json("[]");
            bodies.Add(await r.Content!.ReadAsStringAsync());
            if (bodies.Count == 1) throw new HttpRequestException("response lost");
            if (bodies.Count == 2) return Json("{\"error\":\"closed\"}", HttpStatusCode.UnprocessableEntity);
            return Json("{\"movementId\":\"movement\",\"amountPiastres\":1000,\"wasReplayed\":true}");
        });
        session.OperationStateChanged += () => notifications++;
        var vm = new TreasuryViewModel(session);
        vm.OpenTreasuryActionCommand.Execute(action);
        vm.AmountText = "10"; vm.Reason = "اختبار";
        await vm.SubmitTreasuryActionCommand.ExecuteAsync(null);
        Assert.False(vm.CanEditAction);
        Assert.True(session.HasUnresolvedOperation);
        vm.CloseTreasuryActionCommand.Execute(null);
        Assert.Equal(action, vm.OpenAction);
        vm.OpenTreasuryActionCommand.Execute("deposit");
        vm.AmountText = "not a number"; vm.Reason = "changed";
        await vm.SubmitTreasuryActionCommand.ExecuteAsync(null);
        Assert.True(session.HasUnresolvedOperation);
        await vm.SubmitTreasuryActionCommand.ExecuteAsync(null);
        Assert.Equal(3, bodies.Count);
        Assert.All(bodies, b => Assert.Equal(bodies[0], b));
        Assert.True(vm.CanEditAction);
        Assert.False(session.HasUnresolvedOperation);
        Assert.Null(vm.OpenAction);
        Assert.Contains("10", vm.ActionNote);
        Assert.True(notifications >= 2);
    }

    [Fact]
    public async Task Treasury_definite_rejection_allows_correction_and_releases_the_session()
    {
        var vmSession = Session(_ => Task.FromResult(Json("{\"error\":\"invalid\"}", HttpStatusCode.UnprocessableEntity)));
        var vm = new TreasuryViewModel(vmSession);
        vm.OpenTreasuryActionCommand.Execute("deposit");
        vm.AmountText = "10"; vm.Reason = "اختبار";
        await vm.SubmitTreasuryActionCommand.ExecuteAsync(null);
        Assert.True(vm.CanEditAction);
        Assert.False(vmSession.HasUnresolvedOperation);
        Assert.NotNull(vm.ActionError);
        vm.CloseTreasuryActionCommand.Execute(null);
        Assert.Null(vm.OpenAction);
    }

    [Fact]
    public void Call_center_recovers_each_operators_own_draft_after_switching_back()
    {
        var session = new CC.Services.AppSession(new CC.Services.ApiClient(Http(_ => Task.FromResult(Json("[]"))))) {
            CurrentUser = new("first", "الأول", "cashier", new[] { "pos" })
        };
        var first = session.GetOrderDesk(new());
        first.CustomerName = "عميل الأول";
        first.Cart.Add(new CC.Models.CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 500 });
        session.CurrentUser = new("second", "الثاني", "cashier", new[] { "pos" });
        var second = session.GetOrderDesk(new());
        Assert.Empty(second.Cart);
        session.CurrentUser = new("first", "الأول", "cashier", new[] { "pos" });
        Assert.Same(first, session.GetOrderDesk(new()));
        Assert.Equal("عميل الأول", first.CustomerName);
        Assert.Single(first.Cart);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Electronic_payment_decision_cannot_switch_or_be_abandoned_after_response_loss(bool confirm)
    {
        var requests = new List<(string Path, string Body)>();
        var session = Session(async r =>
        {
            requests.Add((r.RequestUri!.AbsolutePath, await r.Content!.ReadAsStringAsync()));
            if (requests.Count == 1) throw new HttpRequestException("lost");
            if (requests.Count == 2) return Json("{\"error\":\"unavailable\"}", HttpStatusCode.UnprocessableEntity);
            return Json("{\"orderId\":\"order\",\"electronicPaymentStatus\":\"confirmed\",\"isPaid\":true}");
        });
        var vm = session.GetCashier(new());
        vm.LastCheckoutResult = System.Text.Json.JsonSerializer.Deserialize<Falah.Application.Orders.CheckoutOrderResult>(
            """{"orderId":"order","electronicPaymentStatus":"pending"}""",
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        vm.CancelPendingReason = "لم يصل التحويل";
        var submit = confirm ? vm.ConfirmElectronicPaymentCommand : vm.CancelPendingElectronicPaymentCommand;
        var opposite = confirm ? vm.CancelPendingElectronicPaymentCommand : vm.ConfirmElectronicPaymentCommand;
        await submit.ExecuteAsync(null);
        Assert.True(session.HasUnresolvedOperation);
        Assert.False(vm.CanEditOperation);
        await opposite.ExecuteAsync(null);
        Assert.Single(requests);
        vm.StartNewOrderCommand.Execute(null);
        vm.DismissCheckoutResultCommand.Execute(null);
        Assert.NotNull(vm.LastCheckoutResult);
        vm.CancelPendingReason = "";
        await submit.ExecuteAsync(null);
        Assert.True(session.HasUnresolvedOperation);
        await submit.ExecuteAsync(null);
        Assert.Equal(3, requests.Count);
        Assert.All(requests, r => Assert.Equal(requests[0], r));
        Assert.False(session.HasUnresolvedOperation);
        Assert.Null(vm.PendingPaymentActionError);
    }

    [Fact]
    public async Task Call_center_reprint_retries_the_original_request_and_prevents_abandonment()
    {
        var bodies = new List<string>();
        var session = new CC.Services.AppSession(new CC.Services.ApiClient(Http(async r => {
            bodies.Add(await r.Content!.ReadAsStringAsync());
            if (bodies.Count == 1) throw new HttpRequestException("response lost");
            if (bodies.Count == 2) return Json("{\"error\":\"unavailable\"}", HttpStatusCode.Forbidden);
            return Json("{\"jobs\":[],\"wasReplayed\":true}");
        }))) { CurrentUser = new("actor", "اختبار", "cashier", new[] { "pos" }) };
        var vm = session.GetOrderDesk(new());
        vm.IsOrderLookupOpen = true;
        vm.LookedUpOrder = System.Text.Json.JsonSerializer.Deserialize<Falah.Application.Orders.OrderDetailsDto>(
            """{"id":"original","orderNumber":"#1","items":[]}""",
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        await vm.ReprintLookedUpOrderCommand.ExecuteAsync(null);
        Assert.True(vm.HasPendingPrint);
        Assert.False(vm.CanEditOperation);
        Assert.True(session.HasUnresolvedOperation);
        vm.CloseOrderLookupCommand.Execute(null);
        Assert.True(vm.IsOrderLookupOpen);
        Assert.NotNull(vm.LookedUpOrder);
        await vm.RetryPrintCommand.ExecuteAsync(null);
        Assert.False(vm.CanEditOperation);
        await vm.RetryPrintCommand.ExecuteAsync(null);
        Assert.Equal(3, bodies.Count);
        Assert.All(bodies, b => Assert.Equal(bodies[0], b));
        Assert.False(vm.HasPendingPrint);
        Assert.True(vm.CanEditOperation);
        Assert.False(session.HasUnresolvedOperation);
    }

    [Fact]
    public async Task Call_center_keeps_unresolved_request_after_a_later_rejection()
    {
        var bodies = new List<string>();
        var session = new CC.Services.AppSession(new CC.Services.ApiClient(Http(async r => {
            if (r.Method == HttpMethod.Get) return Json("""{"shiftId":"shift","shiftName":"وردية"}""");
            bodies.Add(await r.Content!.ReadAsStringAsync());
            if (bodies.Count == 1) throw new HttpRequestException("response lost");
            return Json("{\"error\":\"shift closed\"}", HttpStatusCode.UnprocessableEntity);
        }))) { CurrentUser = new("actor", "اختبار", "cashier", new[] { "pos" }) };
        var vm = session.GetOrderDesk(new());
        var root = new CC.ViewModels.MainViewModel(session) { CurrentView = vm, IsLoggedIn = true };
        vm.CustomerPhone = "01000000000"; vm.CustomerName = "اختبار";
        vm.DeliveryArea = "منطقة"; vm.DeliveryStreet = "شارع";
        vm.SelectedDeliveryService = System.Text.Json.JsonSerializer.Deserialize<Falah.Application.Delivery.DeliveryServiceDto>(
            """{"id":"service","name":"خدمة","feePiastres":1000,"active":true}""",
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        vm.Cart.Add(new CC.Models.CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 10000 });
        await vm.SubmitOrderCommand.ExecuteAsync(null);
        Assert.True(vm.HasPendingCheckout);
        root.LockCommand.Execute(null);
        Assert.Same(vm, root.CurrentView);
        Assert.True(root.IsLoggedIn);
        Assert.False(root.TryClose());
        Assert.True(root.IsCloseBlocked);
        vm.CustomerName = "changed";
        await vm.SubmitOrderCommand.ExecuteAsync(null);
        Assert.Equal(2, bodies.Count);
        Assert.Equal(bodies[0], bodies[1]);
        Assert.True(vm.HasPendingCheckout);
        Assert.False(vm.CanEditOperation);
        Assert.True(session.HasUnresolvedOperation);
        vm.ClearOrderCommand.Execute(null);
        Assert.Single(vm.Cart);
        root.LockCommand.Execute(null);
        Assert.Same(vm, root.CurrentView);
        Assert.True(root.IsLoggedIn);
    }

    [Fact]
    public void Call_center_lock_waits_for_an_in_flight_operation_then_returns_to_login()
    {
        var session = new CC.Services.AppSession(new CC.Services.ApiClient(Http(_ => Task.FromResult(Json("{}")))))
        {
            CurrentUser = new("actor", "اختبار", "cashier", new[] { "pos" })
        };
        var desk = session.GetOrderDesk(new());
        var root = new CC.ViewModels.MainViewModel(session) { CurrentView = desk, IsLoggedIn = true };
        desk.IsProcessingCheckout = true;
        Assert.False(root.TryClose());
        root.LockCommand.Execute(null);
        Assert.Same(desk, root.CurrentView);
        Assert.True(root.IsLoggedIn);
        desk.IsProcessingCheckout = false;
        Assert.True(root.TryClose());
        Assert.False(root.IsCloseBlocked);
        root.LockCommand.Execute(null);
        Assert.IsType<CC.ViewModels.LoginViewModel>(root.CurrentView);
        Assert.False(root.IsLoggedIn);
        Assert.Same(desk, session.GetOrderDesk(new()));
    }

    [Fact]
    public async Task Call_center_no_longer_requests_its_own_tickets_after_checkout()
    {
        // This used to assert the opposite: the app fired its own print request after checkout and,
        // if the confirmation was lost, held the operator in an unresolved state until they retried.
        // The tickets are now created by the server inside the checkout call itself
        // (OrderAutoPrintService), keyed off the same CheckoutRequestId - so there is no second
        // request to lose, and no window in which a station can die between the sale and the print.
        //
        // What still has to hold: exactly one checkout, no print call of its own, and the operator
        // is not left blocked behind a print that never happened.
        var checkouts = 0;
        var prints = new List<string>();
        var session = new CC.Services.AppSession(new CC.Services.ApiClient(Http(async r => {
            var path = r.RequestUri!.AbsolutePath;
            if (path.EndsWith("print-jobs"))
            {
                prints.Add(await r.Content!.ReadAsStringAsync());
                return Json("{\"jobs\":[],\"wasReplayed\":false}");
            }
            if (r.Method == HttpMethod.Get) return Json("{\"shiftId\":\"shift\",\"shiftName\":\"وردية\"}");
            if (path.Contains("checkout"))
            {
                checkouts++;
                return Json("{\"orderId\":\"order\",\"orderNumber\":\"#1\",\"totalPiastres\":11000,\"printJobsQueued\":true}");
            }
            return Json("{}");
        }))) { CurrentUser = new("actor", "اختبار", "cashier", new[] { "pos" }) };
        var vm = session.GetOrderDesk(new());
        vm.CustomerPhone = "01000000000"; vm.CustomerName = "اختبار";
        vm.DeliveryArea = "منطقة"; vm.DeliveryStreet = "شارع";
        vm.SelectedDeliveryService = System.Text.Json.JsonSerializer.Deserialize<Falah.Application.Delivery.DeliveryServiceDto>(
            """{"id":"service","name":"خدمة","feePiastres":1000,"active":true}""",
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        vm.Cart.Add(new CC.Models.CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 10000 });

        await vm.SubmitOrderCommand.ExecuteAsync(null);

        Assert.Equal(1, checkouts);
        Assert.Empty(prints);              // the station asks for nothing; the server already did it
        Assert.True(vm.HasCheckoutResult);
        Assert.False(vm.HasPendingPrint);  // nothing outstanding to block the next order
        Assert.False(vm.HasPrintWarning);  // the server said the tickets were queued
        Assert.True(vm.CanEditOperation);
        Assert.Empty(vm.Cart);

        vm.StartNewOrderCommand.Execute(null);
        Assert.False(vm.HasCheckoutResult);
        Assert.True(vm.SubmitOrderCommand.CanExecute(null));
    }

    [Fact]
    public async Task Call_center_says_so_when_the_server_could_not_queue_the_tickets()
    {
        // A printing problem must never fail the sale - which means the operator would otherwise
        // have no way to tell an order whose confirmation copy printed from one whose ticket was
        // never created at all.
        var session = new CC.Services.AppSession(new CC.Services.ApiClient(Http(r => Task.FromResult(
            r.Method == HttpMethod.Get
                ? Json("{\"shiftId\":\"shift\",\"shiftName\":\"وردية\"}")
                : r.RequestUri!.AbsolutePath.Contains("checkout")
                    ? Json("{\"orderId\":\"order\",\"orderNumber\":\"#2\",\"totalPiastres\":11000,\"printJobsQueued\":false}")
                    : Json("{}")))))
        { CurrentUser = new("actor", "اختبار", "cashier", new[] { "pos" }) };
        var vm = session.GetOrderDesk(new());
        vm.CustomerPhone = "01000000000"; vm.CustomerName = "اختبار";
        vm.DeliveryArea = "منطقة"; vm.DeliveryStreet = "شارع";
        vm.SelectedDeliveryService = System.Text.Json.JsonSerializer.Deserialize<Falah.Application.Delivery.DeliveryServiceDto>(
            """{"id":"service","name":"خدمة","feePiastres":1000,"active":true}""",
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        vm.Cart.Add(new CC.Models.CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 10000 });

        await vm.SubmitOrderCommand.ExecuteAsync(null);

        // The order still succeeded - that is the whole point of not failing it.
        Assert.True(vm.HasCheckoutResult);
        Assert.True(vm.HasPrintWarning);
        Assert.Contains("أعد الطباعة", vm.PrintWarningText);
    }

    [Theory]
    [InlineData("Exclusive", 11401)]
    [InlineData("Inclusive", 10001)]
    public async Task Call_center_estimate_uses_the_existing_tax_rules(string mode, long expected)
    {
        var session = new CC.Services.AppSession(new CC.Services.ApiClient(Http(r => Task.FromResult(
            r.RequestUri!.AbsolutePath == "/api/menu" ? Json("{\"categories\":[]}") :
            Json($$"""[{"id":"tax","name":"ضريبة","rateBasisPoints":1400,"mode":"{{mode}}","enabled":true}]""")))))
            { CurrentUser = new("actor", "اختبار", "cashier", new[] { "pos" }) };
        var vm = session.GetOrderDesk(new());
        await vm.RetryLoadMenuCommand.ExecuteAsync(null);
        Assert.Null(vm.MenuError);
        Assert.True(vm.HasNoResults);
        vm.Cart.Add(new CC.Models.CartLine { ProductId = "p", ProductName = "صنف", UnitPricePiastres = 10001 });
        Assert.Equal(expected, vm.GrandTotalPiastres);
    }
}

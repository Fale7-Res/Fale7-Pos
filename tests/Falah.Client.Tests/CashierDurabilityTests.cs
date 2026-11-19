namespace Falah.Client.Tests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Falah.Client.CashierWinUI.Models;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;

/// <summary>
/// A sale, a refund or a shift close whose answer never came back survives the till going down.
///
/// The cashier screen held these three in memory only. The answer is lost exactly when the station
/// loses power or the app is killed mid-request - and with it the only evidence the request was
/// ever sent. On the next start the operator could not ask the server what had happened, and the
/// natural next move (ring the order up again, refund again, count again) is precisely the duplicate
/// the idempotency key exists to prevent. The Call Center desk already wrote its checkout to disk
/// before sending; these now do the same, keyed per operator, and come back on the next login ready
/// to be retried with the same key.
///
/// Runs alone: the store is process-wide (RetainedRequestStorage.Current), so a cashier screen built
/// by any other test class while this one has a store installed would recover these requests.
/// </summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class CashierDurabilityTests : IDisposable
{
    private readonly string _directory;
    private readonly FileRetainedRequestStore _store;

    public CashierDurabilityTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "falah-cashier-retained-" + Guid.NewGuid().ToString("N"));
        _store = new FileRetainedRequestStore(_directory);
        RetainedRequestStorage.Current = _store;
    }

    public void Dispose()
    {
        RetainedRequestStorage.Current = null;
        try { Directory.Delete(_directory, recursive: true); } catch { /* temp cleanup only */ }
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static HttpResponseMessage Json(string text, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(text, Encoding.UTF8, "application/json") };

    private static AppSession Session(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) =>
        new(new ApiClient(new HttpClient(new Handler(send)) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("actor", "اختبار", "manager", new[] { "all" }), ShiftId = "shift" };

    /// <summary>The power cut: the request leaves and no answer ever arrives.</summary>
    private static Task<HttpResponseMessage> LostInTransit(HttpRequestMessage _) =>
        throw new HttpRequestException("connection reset");

    // ---------- Checkout ----------

    [Fact]
    public async Task A_checkout_whose_answer_never_came_back_is_on_disk_before_the_app_goes_down()
    {
        var vm = new CashierViewModel(Session(LostInTransit), new HeaderViewModel());
        RingUpOneItem(vm);

        await vm.SubmitPaymentCommand.ExecuteAsync(null);

        var saved = Assert.IsType<CheckoutOrderRequest>(_store.Load("cashier.checkout.actor"));
        Assert.False(string.IsNullOrEmpty(saved.CheckoutRequestId));
    }

    [Fact]
    public async Task After_a_restart_the_unresolved_checkout_blocks_new_work_and_retries_with_its_own_key()
    {
        var lost = new CashierViewModel(Session(LostInTransit), new HeaderViewModel());
        RingUpOneItem(lost);
        await lost.SubmitPaymentCommand.ExecuteAsync(null);
        var originalKey = ((CheckoutOrderRequest)_store.Load("cashier.checkout.actor")!).CheckoutRequestId;

        CheckoutOrderRequest? resent = null;
        var restarted = new CashierViewModel(Session(async request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/orders/checkout") return Json("{}", HttpStatusCode.NotFound);
            resent = await request.Content!.ReadFromJsonAsync<CheckoutOrderRequest>();
            return Json($$"""{"orderId":"o1","orderNumber":"#0128","sequenceNumber":128,"totalPiastres":8500,"amountPaidPiastres":10000,"changeDuePiastres":1500,"status":"completed","isPaid":true,"electronicPaymentStatus":null,"wasReplayed":true,"printJobsQueued":true}""");
        }), new HeaderViewModel());

        // Nothing new can be rung up over an unanswered sale.
        Assert.False(restarted.CanEditOperation);
        Assert.True(restarted.IsCheckoutOpen);

        await restarted.SubmitPaymentCommand.ExecuteAsync(null);

        Assert.Equal(originalKey, resent!.CheckoutRequestId);
        Assert.Null(_store.Load("cashier.checkout.actor"));
        Assert.True(restarted.CanEditOperation);
    }

    [Fact]
    public async Task One_operators_unresolved_checkout_is_never_recovered_by_another()
    {
        var lost = new CashierViewModel(Session(LostInTransit), new HeaderViewModel());
        RingUpOneItem(lost);
        await lost.SubmitPaymentCommand.ExecuteAsync(null);

        var otherSession = Session(LostInTransit);
        otherSession.CurrentUser = new("other", "زميل", "cashier", new[] { "pos" });
        var other = new CashierViewModel(otherSession, new HeaderViewModel());

        Assert.True(other.CanEditOperation);
        Assert.NotNull(_store.Load("cashier.checkout.actor"));
    }

    // ---------- Refund ----------

    [Fact]
    public async Task A_refund_whose_answer_never_came_back_is_on_disk_and_retried_after_a_restart()
    {
        var lost = new CashierViewModel(Session(LostInTransit), new HeaderViewModel());
        lost.OrderLookupResult = PaidCashOrder();
        lost.OpenRefundCommand.Execute(null);
        lost.RefundReason = "طلب العميل";
        await lost.SubmitRefundCommand.ExecuteAsync(null);

        var saved = Assert.IsType<RefundOrderRequest>(_store.Load("cashier.refund.actor"));

        RefundOrderRequest? resent = null;
        var restarted = new CashierViewModel(Session(async request =>
        {
            resent = await request.Content!.ReadFromJsonAsync<RefundOrderRequest>();
            return Json("""{"refundId":"1","orderId":"o1","amountPiastres":8500,"orderStatus":"refunded","remainingRefundablePiastres":0,"treasuryMovementId":"tm","wasReplayed":true}""");
        }), new HeaderViewModel());

        Assert.False(restarted.CanEditOperation);
        Assert.True(restarted.IsRefundOpen);

        // No order lookup survives a restart; the recovered request alone is enough to retry.
        await restarted.SubmitRefundCommand.ExecuteAsync(null);

        Assert.Equal(saved.RefundRequestId, resent!.RefundRequestId);
        Assert.Null(_store.Load("cashier.refund.actor"));
        Assert.True(restarted.CanEditOperation);
    }

    // ---------- Shift close ----------

    [Fact]
    public async Task A_shift_close_whose_answer_never_came_back_is_on_disk_and_retried_after_a_restart()
    {
        var lost = new CashierViewModel(Session(request => request.RequestUri!.AbsolutePath == "/api/shifts/prepare-close"
            ? Task.FromResult(Json("""{"shiftId":"shift","expectedCashPiastres":50000,"actualCashPiastres":50000,"differencePiastres":0,"requiresManagerApproval":false}"""))
            : LostInTransit(request)), new HeaderViewModel());
        lost.CloseShiftActualCashText = "500";
        await lost.PrepareCloseShiftCommand.ExecuteAsync(null);
        // Cash leaves the drawer at the close, so the manager who takes it is named (owner, 2026-11-08).
        lost.SelectedHandoverReceiver = new HandoverReceiverDto("mgr", "المدير");
        await lost.ConfirmCloseShiftCommand.ExecuteAsync(null);

        var saved = Assert.IsType<CloseShiftRequest>(_store.Load("cashier.shift-close.actor"));

        CloseShiftRequest? resent = null;
        var restarted = new CashierViewModel(Session(async request =>
        {
            resent = await request.Content!.ReadFromJsonAsync<CloseShiftRequest>();
            return Json("""{"shiftId":"shift","status":"Closed","expectedCashPiastres":50000,"actualCashPiastres":50000,"differencePiastres":0,"wasReplayed":true}""");
        }), new HeaderViewModel());

        Assert.False(restarted.CanEditOperation);
        Assert.True(restarted.IsCloseShiftOpen);

        // The count's preview does not survive a restart either; the recovered close is retried as sent.
        await restarted.ConfirmCloseShiftCommand.ExecuteAsync(null);

        Assert.Equal(saved.CloseRequestId, resent!.CloseRequestId);
        Assert.Null(_store.Load("cashier.shift-close.actor"));
    }

    // ---------- Drawer money commands (expense, safe drop, cash in/out, opening the drawer) ----------

    [Fact]
    public async Task A_drawer_expense_whose_answer_never_came_back_is_on_disk_before_the_app_goes_down()
    {
        var vm = new CashierViewModel(Session(LostInTransit), new HeaderViewModel());
        vm.ExpenseAmountText = "50";
        vm.ExpenseDescription = "أكياس";

        await vm.SubmitExpenseCommand.ExecuteAsync(null);

        Assert.IsType<Falah.Application.Expenses.AddExpenseRequest>(_store.Load("cashier.expense.actor"));
    }

    [Fact]
    public Task A_recovered_expense_is_retried_with_its_own_key() => RecoveredAndRetried(
        "cashier.expense.actor",
        new Falah.Application.Expenses.AddExpenseRequest("exp-req-1", "shift", "مستلزمات تشغيل", 5000, "أدوات", "tre-drawer", "actor", null, null),
        "exp-req-1", vm => vm.IsAddExpenseOpen, vm => vm.SubmitExpenseCommand.ExecuteAsync(null));

    [Fact]
    public Task A_recovered_safe_drop_is_retried_with_its_own_key() => RecoveredAndRetried(
        "cashier.safe-drop.actor",
        new Falah.Application.Treasury.SafeDropRequest("drop-req-1", "shift", 5000, "تأمين", "actor", null, null),
        "drop-req-1", vm => vm.IsSafeDropOpen, vm => vm.SubmitSafeDropCommand.ExecuteAsync(null));

    [Fact]
    public Task A_recovered_cash_movement_is_retried_with_its_own_key() => RecoveredAndRetried(
        "cashier.cash-movement.actor",
        new Falah.Application.CashDrawer.CashierCashMovementRequest("cm-req-1", "shift", "cash_in", 5000, "فكة", "actor", null, null),
        "cm-req-1", vm => vm.IsCashMovementOpen, vm => vm.SubmitCashMovementCommand.ExecuteAsync(null));

    [Fact]
    public Task A_recovered_cash_drawer_open_is_retried_with_its_own_key() => RecoveredAndRetried(
        "cashier.cash-drawer.actor",
        new Falah.Application.CashDrawer.OpenCashDrawerRequest("drawer-req-1", "shift", "فكة", "actor", null, null),
        "drawer-req-1", vm => vm.IsCashDrawerOpen, vm => vm.SubmitCashDrawerCommand.ExecuteAsync(null));

    /// <summary>A request left on disk by an earlier run: the screen comes back with it open, blocks
    /// new work, resends exactly those bytes, and forgets it once the server answers.</summary>
    private async Task RecoveredAndRetried(string key, object request, string requestId,
        Func<CashierViewModel, bool> dialogIsOpen, Func<CashierViewModel, Task> retry)
    {
        _store.Save(key, request);
        string? resentBody = null;
        var restarted = new CashierViewModel(Session(async message =>
        {
            resentBody = await message.Content!.ReadAsStringAsync();
            return Json("{}");
        }), new HeaderViewModel());

        Assert.False(restarted.CanEditOperation);
        Assert.True(dialogIsOpen(restarted));

        await retry(restarted);

        Assert.Contains(requestId, resentBody);
        Assert.Null(_store.Load(key));
        Assert.True(restarted.CanEditOperation);
    }

    /// <summary>Whole-project review 2026-11-17: a transfer confirm or cancel whose answer was lost
    /// came back from disk after a restart, blocked every till operation, and offered no control at
    /// all - the confirm/cancel buttons need the checkout result the restart had wiped. It is now
    /// surfaced for the operator to check and let go, like every other kept till request.</summary>
    [Theory]
    [InlineData("confirm")]
    [InlineData("cancel")]
    public void A_recovered_transfer_confirm_or_cancel_no_longer_freezes_the_till(string which)
    {
        if (which == "confirm")
            _store.Save("cashier.electronic-confirm", new ConfirmElectronicPaymentRequest("order-77", "actor"));
        else
            _store.Save("cashier.electronic-cancel", new CancelPendingElectronicPaymentRequest("order-77", "لم يصل التحويل", "actor", "not_prepared"));

        var restarted = new CashierViewModel(Session(LostInTransit), new HeaderViewModel());

        Assert.False(restarted.CanEditOperation);
        Assert.True(restarted.HasRefusedRetry, "the kept request is shown, with a way to let it go");
        restarted.DiscardRefusedRetriesCommand.Execute(null);

        Assert.True(restarted.CanEditOperation);
        Assert.Null(_store.Load("cashier.electronic-confirm"));
        Assert.Null(_store.Load("cashier.electronic-cancel"));
    }

    // ---------- Helpers ----------

    // ---------- REC3: a kept request the server refuses on retry ----------

    [Fact]
    public async Task A_recovered_checkout_the_server_refuses_is_kept_until_the_operator_lets_it_go()
    {
        var lost = new CashierViewModel(Session(LostInTransit), new HeaderViewModel());
        RingUpOneItem(lost);
        await lost.SubmitPaymentCommand.ExecuteAsync(null);

        // After the restart the shift had been closed by a manager: the server refuses the retry.
        var restarted = new CashierViewModel(Session(request => Task.FromResult(request.RequestUri!.AbsolutePath == "/api/orders/checkout"
            ? Json("""{"error":"يجب فتح وردية قبل تسجيل أي عملية بيع."}""", HttpStatusCode.Conflict)
            : Json("{}", HttpStatusCode.NotFound))), new HeaderViewModel());
        await restarted.SubmitPaymentCommand.ExecuteAsync(null);

        // Still kept and still blocking - a refused RETRY is not dropped by the till on its own...
        Assert.NotNull(_store.Load("cashier.checkout.actor"));
        Assert.False(restarted.CanEditOperation);
        // ...but the operator is told why and can let it go.
        Assert.True(restarted.HasRefusedRetry);
        Assert.Contains("يجب فتح وردية", restarted.RefusedRetryMessage);

        restarted.DiscardRefusedRetriesCommand.Execute(null);

        Assert.Null(_store.Load("cashier.checkout.actor"));
        Assert.True(restarted.CanEditOperation);
        Assert.False(restarted.HasRefusedRetry);
    }

    [Fact]
    public async Task A_recovered_request_whose_retry_is_still_unanswered_is_never_offered_for_discarding()
    {
        var lost = new CashierViewModel(Session(LostInTransit), new HeaderViewModel());
        RingUpOneItem(lost);
        await lost.SubmitPaymentCommand.ExecuteAsync(null);

        var restarted = new CashierViewModel(Session(LostInTransit), new HeaderViewModel());
        await restarted.SubmitPaymentCommand.ExecuteAsync(null);

        Assert.False(restarted.HasRefusedRetry);
        restarted.DiscardRefusedRetriesCommand.Execute(null);
        Assert.NotNull(_store.Load("cashier.checkout.actor"));
    }

    private static void RingUpOneItem(CashierViewModel vm)
    {
        vm.Cart.Add(new CartLine { ProductId = "product", ProductName = "شاورما", UnitPricePiastres = 8500 });
        vm.OpenCheckoutCommand.Execute(null);
        vm.AmountPaidText = "100";
    }

    private static OrderDetailsDto PaidCashOrder() => new(
        "o1", "#0128", 128, "takeaway", "cash", Array.Empty<OrderItemDto>(), 8500, 0, 0,
        8500, 10000, 1500, "completed", true, null, 0, DateTimeOffset.UtcNow, "actor",
        null, null, null, null, null, null, null, null, null, null, null, null, 0, 0, "shift");
}

/// <summary>Test classes that install a process-wide retained-request store run on their own, never
/// alongside another class that builds a screen which would read it.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RetainedRequestStorageCollection
{
    public const string Name = "RetainedRequestStorage";
}

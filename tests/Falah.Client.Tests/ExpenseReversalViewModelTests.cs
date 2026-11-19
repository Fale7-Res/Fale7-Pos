using System.Net;
using System.Text;
using System.Text.Json;
using Falah.Application.Reports;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>
/// AUD-04 on the daily statement: cancelling a mistaken expense needs a reason; a drawer expense cancelled by a user
/// without shifts_manage first gets a manager's approval issued for shifts_manage; the request carries the expense id
/// and is sent once.
/// </summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class ExpenseReversalViewModelTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "falah-reversal-" + Guid.NewGuid().ToString("N"));
    private readonly IRetainedRequestStore? _previous = RetainedRequestStorage.Current;

    public ExpenseReversalViewModelTests() => RetainedRequestStorage.Current = new FileRetainedRequestStore(_dir);

    public void Dispose()
    {
        RetainedRequestStorage.Current = _previous;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (DailyStatementViewModel Vm, List<string> Calls, List<JsonElement> Reversals) Screen(params string[] permissions)
    {
        var calls = new List<string>();
        var reversals = new List<JsonElement>();
        var http = new HttpClient(new Handler(async r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            calls.Add($"{r.Method} {path}");
            if (path == "/api/approvals/issue")
                return Json("""{"approvalId":"apr-9","approverUserId":"mgr","approverName":"المدير","action":"shifts_manage","approvedAtUtc":"2026-11-13T10:00:00Z"}""");
            if (path.EndsWith("/reverse"))
            {
                reversals.Add(JsonDocument.Parse(await r.Content!.ReadAsStringAsync()).RootElement.Clone());
                return Json("""{"reversalExpenseId":"exp-rev-exp-1","originalExpenseId":"exp-1","amountPiastres":50000,"paymentAccountId":"tre-drawer","treasuryMovementId":"tm-1","approvedBy":"المدير","wasReplayed":false}""");
            }
            return Json("{}", HttpStatusCode.NotFound);
        })) { BaseAddress = new Uri("http://fixture.invalid") };
        var session = new AppSession(new ApiClient(http)) { CurrentUser = new("actor", "اختبار", "supervisor", permissions), ShiftId = "shift" };
        return (new DailyStatementViewModel(session), calls, reversals);
    }

    private static readonly DailyExpenseLineDto DrawerLine = new("exp-1", DateTimeOffset.UtcNow, "نظافة ومطهرات", "منظفات", 50_000, "كاشير", null, "tre-drawer", "shift");

    [Fact]
    public async Task A_drawer_expense_cancelled_without_shifts_manage_takes_a_manager_pin_and_a_reason()
    {
        var (vm, calls, reversals) = Screen("reports_view");
        vm.OpenReversalCommand.Execute(DrawerLine);
        Assert.True(vm.ReversalNeedsManagerPin);

        await vm.ConfirmReversalCommand.ExecuteAsync(null);   // no reason
        Assert.Empty(reversals);
        vm.ReversalReason = "كُتب 500 بدل 50";
        await vm.ConfirmReversalCommand.ExecuteAsync(null);   // what happened to the money not stated
        Assert.Empty(reversals);
        vm.IsReversalNotPaid = true;
        await vm.ConfirmReversalCommand.ExecuteAsync(null);   // no PIN
        Assert.Empty(reversals);

        vm.ReversalManagerPin = "4321";
        await vm.ConfirmReversalCommand.ExecuteAsync(null);

        Assert.Contains("POST /api/approvals/issue", calls);
        var sent = Assert.Single(reversals);
        Assert.Equal("exp-1", sent.GetProperty("expenseId").GetString());
        Assert.Equal("apr-9", sent.GetProperty("managerApprovalId").GetString());
        Assert.Equal("not_paid", sent.GetProperty("cashDisposition").GetString());
        Assert.Equal(JsonValueKind.Null, sent.GetProperty("reversedAmountPiastres").ValueKind);
        Assert.False(vm.IsReversalOpen);
    }

    [Fact]
    public async Task A_manager_cancels_without_a_pin()
    {
        var (vm, calls, reversals) = Screen("shifts_manage", "reports_view");
        vm.OpenReversalCommand.Execute(DrawerLine);
        vm.ReversalReason = "مكرر";
        vm.IsReversalCashReturned = true;

        Assert.False(vm.ReversalNeedsManagerPin);
        await vm.ConfirmReversalCommand.ExecuteAsync(null);

        Assert.DoesNotContain("POST /api/approvals/issue", calls);
        Assert.Equal("cash_returned", Assert.Single(reversals).GetProperty("cashDisposition").GetString());
    }

    [Fact]
    public void A_reversal_line_and_an_expense_already_reversed_offer_no_second_reversal()
    {
        // Found on screen: the "إلغاء" action was offered on the reversal line itself.
        var (vm, _, _) = Screen("shifts_manage", "reports_view");
        var reversal = DrawerLine with { Id = "exp-rev-exp-1", AmountPiastres = -45_000 };
        vm.ExpenseLines.Add(DrawerLine);
        vm.ExpenseLines.Add(reversal);

        vm.OpenReversalCommand.Execute(reversal);
        Assert.False(vm.IsReversalOpen);
        vm.OpenReversalCommand.Execute(DrawerLine);
        Assert.False(vm.IsReversalOpen);
        Assert.Contains("بالفعل", vm.ReprintMessage);
    }

    [Fact]
    public async Task Only_the_part_that_never_left_is_reversed_and_never_more_than_the_expense()
    {
        var (vm, _, reversals) = Screen("shifts_manage", "reports_view");
        vm.OpenReversalCommand.Execute(DrawerLine);   // 500.00 ج
        vm.ReversalReason = "كُتب 500 وصُرف 50";
        vm.IsReversalNotPaid = true;

        vm.ReversalAmountText = "600";
        await vm.ConfirmReversalCommand.ExecuteAsync(null);
        Assert.Empty(reversals);

        vm.ReversalAmountText = "450";
        await vm.ConfirmReversalCommand.ExecuteAsync(null);
        Assert.Equal(45_000, Assert.Single(reversals).GetProperty("reversedAmountPiastres").GetInt64());
    }
}

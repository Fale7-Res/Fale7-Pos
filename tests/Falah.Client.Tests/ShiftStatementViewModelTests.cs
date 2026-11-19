using System.Net;
using System.Text;
using Falah.Application.Shifts;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>UI-REQ-002: the closing statement on screen is the printed paper, line for line; the
/// paper's print state is said plainly; and closing a shift shows it.</summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class ShiftStatementViewModelTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "falah-stmt-" + Guid.NewGuid().ToString("N"));
    private readonly IRetainedRequestStore? _previous = RetainedRequestStorage.Current;
    public ShiftStatementViewModelTests() => RetainedRequestStorage.Current = new FileRetainedRequestStore(_dir);
    public void Dispose() { RetainedRequestStorage.Current = _previous; if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) => send(r);
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private const string Statement = """
    {"statement":{"kind":"shift-close-statement","schemaVersion":1,"restaurantName":"فالح","subName":null,"shiftId":"sh-1","shiftReference":"ABC123",
      "shiftName":"صباحية","businessDate":"2026-11-08","openedAtUtc":"2026-11-08T06:00:00+00:00","closedAtUtc":"2026-11-08T15:00:00+00:00",
      "cashierName":"أحمد","closedByName":"أحمد","approvedByName":null,
      "sales":{"orderCount":4,"listSalesPiastres":55000,"offersPiastres":0,"compsPiastres":0,"manualDiscountsPiastres":0,"grossSalesPiastres":55000,
        "refundsPiastres":2000,"netSalesPiastres":53000,"cashPiastres":18000,"cardPiastres":15000,"instaPayPiastres":0,"walletPiastres":8000,
        "pendingElectronicPiastres":12000,"cashWithDriversPiastres":0,"collectedPiastres":41000,"cancelledOrderCount":0},
      "expenses":[],"cash":{"openingPiastres":50000,"cashSalesPiastres":20000,"refundsPiastres":2000,"cashInPiastres":0,"drawerExpensesPiastres":0,
        "withdrawalsPiastres":0,"safeDropsPiastres":0,"expectedPiastres":68000,"countedPiastres":67000,"differencePiastres":-1000,
        "handedOverPiastres":67000,"handoverDestinationName":"الخزينة الرئيسية"},"differenceReason":"عجز بسيط","outstanding":[]},
     "recomputed":false,
     "printJobs":[{"jobId":"pj-1","status":"Failed","isReprint":false,"attemptCount":3,"lastError":"الطابعة غير متصلة","createdAtUtc":"2026-11-08T15:00:00+00:00"}],
     "paper":{"route":"Receipt","orderId":"ABC123","orderNumber":"إغلاق وردية","lines":[
       {"kind":"text","text":"إغلاق وتسليم وردية","bold":true,"align":"center"},
       {"kind":"rule"},
       {"kind":"row","cells":[{"text":"صافي المبيعات","widthPercent":60,"bold":true},{"text":"530 ج","widthPercent":40,"align":"end","bold":true}]},
       {"kind":"cut"}]}}
    """;

    [Fact]
    public async Task The_statement_on_screen_is_the_printed_paper_and_says_the_print_failed()
    {
        var session = new AppSession(new ApiClient(new HttpClient(new Handler(r => Task.FromResult(
            r.RequestUri!.AbsolutePath == "/api/shifts/sh-1/close-statement" ? Json(Statement) : Json("[]"))))
            { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("u", "أحمد", "cashier", new[] { "pos" }) };
        var vm = new ShiftStatementViewModel(session);

        await vm.ShowAsync("sh-1");

        Assert.True(vm.IsOpen);
        Assert.Null(vm.Error);
        Assert.Equal(3, vm.Lines.Count);                                   // text, rule, row - the cut is not a screen line
        Assert.Equal("إغلاق وتسليم وردية", vm.Lines[0].Text);
        Assert.True(vm.Lines[0].Centered);
        Assert.True(vm.Lines[1].IsRule);
        Assert.Equal(("صافي المبيعات", "530 ج"), (vm.Lines[2].Label, vm.Lines[2].Value));
        Assert.True(vm.PrintFailed);
        Assert.Contains("الطابعة غير متصلة", vm.PrintStatus);
        Assert.Contains("الإغلاق نفسه تم ولن يتكرر", vm.PrintStatus);
        Assert.Contains("صباحية", vm.Title);
    }

    [Theory]
    [InlineData("Pending", "في انتظار الطابعة")]
    [InlineData("Succeeded", "أُرسلت ورقة الإغلاق للطابعة")]
    [InlineData("Sending", "قيد الإرسال")]
    public void The_print_state_is_said_plainly(string status, string expected)
    {
        var (text, failed) = ShiftStatementViewModel.DescribePrint(new[]
        {
            new ShiftClosePrintStatusDto("pj-1", status, false, 0, null, DateTimeOffset.UtcNow),
            new ShiftClosePrintStatusDto("pj-2", "Pending", true, 0, null, DateTimeOffset.UtcNow),
        });
        Assert.Contains(expected, text);
        Assert.Contains("أعيدت طباعتها 1 مرة", text);
        Assert.False(failed);
    }

    [Fact]
    public async Task Closing_a_shift_from_the_shifts_screen_shows_its_statement()
    {
        var statementRequested = false;
        var session = new AppSession(new ApiClient(new HttpClient(new Handler(r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            if (path == "/api/shifts/current") return Task.FromResult(Json("""{"shiftId":"sh-1","shiftName":"صباحية","openedByUserId":"u","openingCashPiastres":50000,"openedAtUtc":"2026-11-08T06:00:00+00:00"}"""));
            if (path == "/api/shifts/prepare-close") return Task.FromResult(Json("""{"shiftId":"sh-1","expectedCashPiastres":68000,"actualCashPiastres":68000,"differencePiastres":0,"requiresManagerApproval":false}"""));
            if (path == "/api/shifts/close") return Task.FromResult(Json("""{"shiftId":"sh-1","status":"Closed","expectedCashPiastres":68000,"actualCashPiastres":68000,"differencePiastres":0,"wasReplayed":false}"""));
            if (path == "/api/shifts/sh-1/close-statement") { statementRequested = true; return Task.FromResult(Json(Statement)); }
            return Task.FromResult(Json("[]"));
        })) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("u", "أحمد", "cashier", new[] { "shifts_manage" }) };
        var vm = new ShiftsViewModel(session, new HeaderViewModel());
        await vm.LoadAsync();
        vm.OpenCloseShiftModalCommand.Execute(null);
        vm.CloseShiftActualCashText = "680";
        await vm.PrepareCloseShiftCommand.ExecuteAsync(null);
        // Cash leaves the drawer at the close, so the manager who takes it is named (owner, 2026-11-08).
        vm.SelectedHandoverReceiver = new Falah.Application.Shifts.HandoverReceiverDto("mgr", "المدير");
        await vm.ConfirmCloseShiftCommand.ExecuteAsync(null);

        Assert.True(statementRequested);
        Assert.True(vm.Statement.IsOpen);
        Assert.NotEmpty(vm.Statement.Lines);
    }
}

using System.Net;
using System.Text;
using System.Text.Json;
using Falah.Application.Purchasing;
using Falah.Application.Reports;
using Falah.Application.Treasury;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>
/// P2-P5 on the owner's screens: an unknown figure reads "غير متاح" (never 0) and an estimate says so; a shortfall is
/// classified only with evidence (and a manager's PIN when the user lacks the authority); a handover is confirmed by
/// the receiver's PIN on anyone else's screen; a treasury correction requires saying the money did not move; a
/// supplier credit never exceeds what is owed. Each command is sent once.
/// </summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class FinancialControlOwnerReportTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "falah-owner-" + Guid.NewGuid().ToString("N"));
    private readonly IRetainedRequestStore? _previous = RetainedRequestStorage.Current;

    public FinancialControlOwnerReportTests() => RetainedRequestStorage.Current = new FileRetainedRequestStore(_dir);

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

    private static (AppSession Session, List<string> Calls, List<(string Path, JsonElement Body)> Posts) Session(
        Func<string, string?>? reply, string userId, params string[] permissions)
    {
        var calls = new List<string>();
        var posts = new List<(string, JsonElement)>();
        var http = new HttpClient(new Handler(async r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            calls.Add($"{r.Method} {path}");
            if (r.Method == HttpMethod.Post) posts.Add((path, JsonDocument.Parse(await r.Content!.ReadAsStringAsync()).RootElement.Clone()));
            if (path == "/api/approvals/issue")
                return Json("""{"approvalId":"apr-1","approverUserId":"mgr","approverName":"المدير","action":"x","approvedAtUtc":"2026-11-13T10:00:00Z"}""");
            return reply?.Invoke(path) is { } body ? Json(body) : Json("{}", HttpStatusCode.NotFound);
        })) { BaseAddress = new Uri("http://fixture.invalid") };
        return (new AppSession(new ApiClient(http)) { CurrentUser = new(userId, "مستخدم", "supervisor", permissions), ShiftId = "shift" }, calls, posts);
    }

    [Fact]
    public void An_unknown_figure_reads_unavailable_and_an_estimate_says_so()
    {
        var unknown = FinancialControlViewModel.ToRow(new FinancialKpiDto("operating_result_estimate", "النتيجة التشغيلية التقديرية", null, null,
            "unavailable", "تعريف", "sale", "Profitability"));
        Assert.Equal(("غير متاح", "غير متاح", true), (unknown.AmountDisplay, unknown.StatusLabel, unknown.IsUnavailable));

        var estimate = FinancialControlViewModel.ToRow(new FinancialKpiDto("gross_profit_estimate", "مجمل الربح", 12_345, null,
            "estimate", "تعريف", "sale", "Orders"));
        Assert.Equal(("123.45 ج", "تقديري", true), (estimate.AmountDisplay, estimate.StatusLabel, estimate.IsEstimateOrPartial));

        var zero = FinancialControlViewModel.ToRow(new FinancialKpiDto("expenses_net", "صافي المصروفات", 0, 0, "actual", "تعريف", "business_date", "Expenses"));
        Assert.Equal(("0.00 ج", "فعلي"), (zero.AmountDisplay, zero.StatusLabel));
    }

    [Fact]
    public void A_report_that_does_not_reconcile_says_so()
    {
        var (session, _, _) = Session(null, "owner", "all");
        var vm = new FinancialControlViewModel(session);
        vm.FillOwnerReport(new OwnerFinancialReportDto("2026-11-13", "2026-11-13", DateTimeOffset.UtcNow,
            new[] { new FinancialKpiSectionDto("sales", "المبيعات", Array.Empty<FinancialKpiDto>()) },
            new SalesBridgeDto(10_000, 9_000, 0, 0, 0, 0, 0, 0, 0, 1_000, 0, 0, 10_000, 0, 0),
            new MoneyFlowSummaryDto(Array.Empty<MoneyFlowLineDto>(), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            Array.Empty<string>()));

        Assert.False(vm.IsReconciled);
        Assert.Contains("10.00", vm.ReconciliationLabel);
    }

    [Fact]
    public async Task A_shortfall_is_classified_only_with_evidence_and_a_managers_pin()
    {
        var (session, calls, posts) = Session(path => path.EndsWith("/classify")
            ? """{"deliveryId":"d1","orderId":"o1","orderNumber":"42","driverId":"drv","driverName":"سائق","expectedPiastres":12000,"collectedPiastres":10000,"shortfallPiastres":2000,"deliveredAtUtc":null,"status":"classified","classification":"write_off","evidence":"x","classifiedBy":"م","approvedBy":"المدير","classifiedAtUtc":null}"""
            : null, "disp", "reports_view", "delivery_manage");
        var vm = new FinancialControlViewModel(session);
        vm.FillShortfalls(new Falah.Application.Delivery.CollectionShortfallListDto(2_000, 1, 0, 0, new[]
        {
            new Falah.Application.Delivery.CollectionShortfallDto("d1", "o1", "42", "drv", "سائق", 12_000, 10_000, 2_000, null, "unresolved", null, null, null, null, null),
        }));
        Assert.True(vm.ClassificationNeedsManagerPin);

        vm.OpenClassifyCommand.Execute(vm.Shortfalls[0]);
        vm.SelectedClassification = vm.ClassificationOptions.Single(o => o.Key == "write_off");
        await vm.ConfirmClassifyCommand.ExecuteAsync(null);    // no evidence
        vm.ClassificationEvidence = "موافقة المالك - إيصال 7";
        await vm.ConfirmClassifyCommand.ExecuteAsync(null);    // no PIN
        Assert.DoesNotContain(posts, p => p.Path.EndsWith("/classify"));

        vm.ClassificationManagerPin = "4321";
        await vm.ConfirmClassifyCommand.ExecuteAsync(null);

        Assert.Contains("POST /api/approvals/issue", calls);
        var sent = Assert.Single(posts, p => p.Path.EndsWith("/classify")).Body;
        Assert.Equal(("write_off", "apr-1"), (sent.GetProperty("classification").GetString(), sent.GetProperty("managerApprovalId").GetString()));
    }

    [Fact]
    public async Task On_someone_elses_screen_the_receiver_confirms_with_a_pin()
    {
        var (session, _, posts) = Session(path => path.EndsWith("/acknowledge")
            ? """{"shiftId":"s1","shiftName":"صباحية","businessDate":"2026-11-13","closedAtUtc":null,"closedBy":"كاشير","handedOverPiastres":100000,"receiverUserId":"mgr","receiverName":"المدير","status":"acknowledged","receivedPiastres":100000,"differencePiastres":0,"acknowledgedBy":"المدير","acknowledgedAtUtc":null,"note":null}"""
            : null, "cashier", "pos");
        var vm = new FinancialControlViewModel(session);
        vm.FillHandovers(new[]
        {
            new Falah.Application.Shifts.ShiftHandoverReceiptDto("s1", "صباحية", new DateOnly(2026, 11, 13), null, "كاشير", 100_000, "mgr", "المدير",
                "pending", null, null, null, null, null),
        });
        vm.OpenAcknowledgeCommand.Execute(vm.Handovers[0]);
        Assert.True(vm.AcknowledgeNeedsReceiverPin);

        vm.ReceivedAmountText = "950";
        await vm.ConfirmAcknowledgeCommand.ExecuteAsync(null);   // different amount, no note
        Assert.Empty(posts.Where(p => p.Path.EndsWith("/acknowledge")));
        vm.ReceivedAmountText = "1000";
        await vm.ConfirmAcknowledgeCommand.ExecuteAsync(null);   // no PIN
        Assert.Empty(posts.Where(p => p.Path.EndsWith("/acknowledge")));

        vm.ReceiverPin = "1234";
        await vm.ConfirmAcknowledgeCommand.ExecuteAsync(null);
        var sent = Assert.Single(posts, p => p.Path.EndsWith("/acknowledge")).Body;
        Assert.Equal((100_000L, "apr-1"), (sent.GetProperty("receivedPiastres").GetInt64(), sent.GetProperty("receiverApprovalId").GetString()));
    }

    [Fact]
    public async Task A_treasury_correction_must_say_the_money_did_not_move()
    {
        var (session, _, posts) = Session(path => path.EndsWith("/correct")
            ? """{"correctionMovementId":"tm-corr-tm-1","originalMovementId":"tm-1","originalType":"transfer","amountPiastres":200000,"sourceAccountId":"tre-bank","destinationAccountId":"tre-main","approvedBy":"المالك","wasReplayed":false}"""
            : path == "/api/treasury/balances" ? """{"accounts":[]}""" : "[]", "owner", "treasury_manage");
        var vm = new TreasuryViewModel(session);
        var row = new TreasuryViewModel.MovementRow
        {
            Movement = new TreasuryMovementDto("tm-1", "transfer", "tre-main", "tre-bank", 200_000, "treasury_transfer", "x", null, "إيداع", "المالك", null, DateTimeOffset.UtcNow),
        };
        Assert.True(row.IsCorrectable);

        vm.OpenCorrectionCommand.Execute(row);
        vm.CorrectionReason = "لم يُودَع فعلًا";
        await vm.ConfirmCorrectionCommand.ExecuteAsync(null);
        Assert.DoesNotContain(posts, p => p.Path.EndsWith("/correct"));

        vm.CorrectionConfirmedNotMoved = true;
        await vm.ConfirmCorrectionCommand.ExecuteAsync(null);
        var sent = Assert.Single(posts, p => p.Path.EndsWith("/correct")).Body;
        Assert.True(sent.GetProperty("confirmNotPhysicallyMoved").GetBoolean());
        Assert.False(new TreasuryViewModel.MovementRow
        {
            Movement = row.Movement with { Type = "sale_collection" },
        }.IsCorrectable);
    }

    [Fact]
    public async Task A_supplier_credit_never_exceeds_what_is_owed()
    {
        var (session, _, posts) = Session(path => path.EndsWith("/credits")
            ? """{"ledgerEntryId":"l1","purchaseId":"p1","kind":"invoice_error","amountPiastres":3000,"remainingAmountPiastres":17000,"wasReplayed":false}"""
            : "[]", "owner", "purchasing_manage");
        var vm = new PurchasingViewModel(session);
        var invoice = new PurchaseDto("p1", "sup", "INV-7", new[] { new PurchaseLineDto("item", 10_000, 2_000, 20_000) },
            20_000, 20_000, 0, 20_000, "credit", null, "received", "unpaid", "المالك", DateTimeOffset.UtcNow, Array.Empty<PurchasePaymentDto>());

        vm.OpenCreditCommand.Execute(invoice);
        vm.SelectedCreditKind = vm.CreditKinds.Single(k => k.Key == "invoice_error");
        vm.CreditReason = "سعر خاطئ";
        vm.CreditAmountText = "200.01";
        await vm.SubmitCreditCommand.ExecuteAsync(null);
        Assert.DoesNotContain(posts, p => p.Path.EndsWith("/credits"));

        vm.CreditAmountText = "30";
        await vm.SubmitCreditCommand.ExecuteAsync(null);
        var sent = Assert.Single(posts, p => p.Path.EndsWith("/credits")).Body;
        Assert.Equal(("invoice_error", 3_000L), (sent.GetProperty("kind").GetString(), sent.GetProperty("amountPiastres").GetInt64()));
    }
}

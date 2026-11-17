using System.Net;
using System.Text;
using System.Text.Json;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>UI-0001: a treasury deposit whose response was lost lived only in memory. After a crash or
/// power cut the operator saw nothing pending, typed the deposit again, and the server - seeing a new
/// request id - recorded the money twice. It must survive a restart with its original request id.</summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class TreasuryDurabilityTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "falah-treasury-" + Guid.NewGuid().ToString("N"));
    private readonly IRetainedRequestStore? _previous = RetainedRequestStorage.Current;

    public TreasuryDurabilityTests() => RetainedRequestStorage.Current = new FileRetainedRequestStore(_dir);

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

    private static AppSession Session(List<string> depositIds, bool loseResponse) => new(new ApiClient(new HttpClient(new Handler(async r =>
    {
        var path = r.RequestUri!.AbsolutePath;
        if (path == "/api/treasury/deposit")
        {
            using var doc = JsonDocument.Parse(await r.Content!.ReadAsStringAsync());
            depositIds.Add(doc.RootElement.GetProperty("depositRequestId").GetString()!);
            if (loseResponse) throw new HttpRequestException("response lost");
            return Json("""{"movementId":"mv-1","type":"capital_injection","amountPiastres":10000,"destinationAccountId":"tre-main","wasReplayed":true}""");
        }
        return Json(path.EndsWith("/position") ? """{"accounts":[],"pendingElectronicPiastres":0,"pendingElectronicCount":0,"cashWithDriversPiastres":0}""" : "[]");
    })) { BaseAddress = new Uri("http://fixture.invalid") }))
    { CurrentUser = new("owner", "المالك", "owner", new[] { "all" }) };

    [Fact]
    public async Task A_deposit_whose_response_was_lost_is_resent_with_the_same_id_after_a_restart()
    {
        var sent = new List<string>();
        var before = new TreasuryViewModel(Session(sent, loseResponse: true));
        before.OpenTreasuryActionCommand.Execute("deposit");
        before.AmountText = "100";
        before.Reason = "إيداع اختبار";
        await before.SubmitTreasuryActionCommand.ExecuteAsync(null);
        Assert.Single(sent);

        // The app dies here. A new process builds a new view model over the same station storage.
        var resent = new List<string>();
        var after = new TreasuryViewModel(Session(resent, loseResponse: false));
        Assert.False(after.CanEditAction);   // the unresolved deposit is surfaced, not forgotten
        await after.SubmitTreasuryActionCommand.ExecuteAsync(null);

        Assert.Equal(sent, resent);           // the same request id: the server replays, never doubles
        Assert.True(after.CanEditAction);
        Assert.Empty(Directory.Exists(_dir) ? Directory.GetFiles(_dir) : Array.Empty<string>());
    }

    [Fact]
    public async Task A_recovered_deposit_the_server_refuses_stays_until_the_owner_discards_it_after_review()
    {
        var sent = new List<string>();
        var before = new TreasuryViewModel(Session(sent, loseResponse: true));
        before.OpenTreasuryActionCommand.Execute("deposit");
        before.AmountText = "100";
        before.Reason = "إيداع اختبار";
        await before.SubmitTreasuryActionCommand.ExecuteAsync(null);

        var session = new AppSession(new ApiClient(new HttpClient(new Handler(r => Task.FromResult(
            r.RequestUri!.AbsolutePath == "/api/treasury/deposit"
                ? Json("""{"error":"لا يوجد يوم عمل مفتوح"}""", HttpStatusCode.UnprocessableEntity)
                : Json("[]")))) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("owner", "المالك", "owner", new[] { "all" }) };
        var after = new TreasuryViewModel(session);
        await after.SubmitTreasuryActionCommand.ExecuteAsync(null);

        // A definitive refusal of a request whose first attempt may have landed is not proof it did not:
        // it stays pending and is listed for review instead of being silently dropped.
        Assert.False(after.CanEditAction);
        var item = Assert.Single(session.Recovery.Items);
        item.ServerRecordReviewed = true;
        item.DiscardCommand.Execute(null);

        Assert.True(after.CanEditAction);
        Assert.Empty(session.Recovery.Items);
        Assert.Empty(Directory.GetFiles(_dir));
    }

    private AppSession QuietSession() => new(new ApiClient(new HttpClient(new Handler(r => Task.FromResult(Json("[]"))))
        { BaseAddress = new Uri("http://fixture.invalid") }))
    { CurrentUser = new("owner", "المالك", "owner", new[] { "all" }) };

    /// <summary>Whole-project review 2026-11-17: a treasury correction and a supplier credit that were
    /// in flight when the app died came back from disk but were listed nowhere - their dialogs were
    /// closed, so they could be neither retried nor reviewed, and they silently blocked the next one.
    /// Like every other kept money command, they now appear in the recovery list.</summary>
    [Fact]
    public void A_correction_left_in_flight_is_listed_for_review_and_can_be_let_go()
    {
        new FileRetainedRequestStore(_dir).Save("treasury.correction",
            new Falah.Application.Treasury.CorrectTreasuryMovementRequest("corr-1", "tm-1", "قيد بالخطأ", "owner", true));
        var session = QuietSession();
        _ = new TreasuryViewModel(session);

        var item = Assert.Single(session.Recovery.Items);
        item.ServerRecordReviewed = true;
        item.DiscardCommand.Execute(null);

        Assert.Empty(session.Recovery.Items);
        Assert.Empty(Directory.GetFiles(_dir));
    }

    [Fact]
    public void A_supplier_credit_left_in_flight_is_listed_and_blocks_new_purchasing_work_until_reviewed()
    {
        new FileRetainedRequestStore(_dir).Save("purchasing.credit",
            new Falah.Application.Purchasing.RecordSupplierCreditRequest("cred-1", "pur-1", "correction", 5_000, "تصحيح فاتورة", "owner"));
        var session = QuietSession();
        var purchasing = new PurchasingViewModel(session);

        Assert.False(purchasing.CanEditAction);
        var item = Assert.Single(session.Recovery.Items);
        item.ServerRecordReviewed = true;
        item.DiscardCommand.Execute(null);

        Assert.True(purchasing.CanEditAction);
        Assert.Empty(Directory.GetFiles(_dir));
    }
}

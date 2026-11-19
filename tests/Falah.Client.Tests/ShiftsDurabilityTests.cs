using System.Net;
using System.Text;
using System.Text.Json;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>UI-0001 (shift screens): the management Shifts screen built a new request id on every
/// press and kept nothing on disk. Opening a shift moves the float out of the main treasury, so a
/// lost response followed by a second press - or a restart and a second press - sent a new id the
/// server could not recognise as the same operation.</summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class ShiftsDurabilityTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "falah-shifts-" + Guid.NewGuid().ToString("N"));
    private readonly IRetainedRequestStore? _previous = RetainedRequestStorage.Current;

    public ShiftsDurabilityTests() => RetainedRequestStorage.Current = new FileRetainedRequestStore(_dir);

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

    private const string OpenShift = """{"shiftId":"sh-1","shiftName":"وردية صباحية","openedByUserId":"owner","openedByUserName":"المالك","openedAtUtc":"2026-11-07T08:00:00+00:00","status":"Open","openingCashPiastres":100000}""";

    /// <summary>A fixture server. <paramref name="lose"/> makes the write commit and its response vanish.</summary>
    private static AppSession Session(List<string> ids, string writePath, string idField, bool lose, bool shiftIsOpen) =>
        new(new ApiClient(new HttpClient(new Handler(async r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            if (path == writePath)
            {
                using var doc = JsonDocument.Parse(await r.Content!.ReadAsStringAsync());
                ids.Add(doc.RootElement.GetProperty(idField).GetString()!);
                if (lose) throw new HttpRequestException("response lost");
                return path.EndsWith("/open")
                    ? Json("""{"shiftId":"sh-1","shiftName":"وردية صباحية","status":"Open","openingCashPiastres":100000,"wasReplayed":true}""")
                    : Json("""{"shiftId":"sh-1","status":"Closed","expectedCashPiastres":100000,"actualCashPiastres":100000,"differencePiastres":0,"wasReplayed":true}""");
            }
            if (path == "/api/shifts/prepare-close")
                return Json("""{"shiftId":"sh-1","expectedCashPiastres":100000,"actualCashPiastres":100000,"differencePiastres":0,"requiresManagerApproval":false}""");
            if (path == "/api/shifts/current")
                return shiftIsOpen ? Json(OpenShift) : Json("""{"error":"لا توجد وردية مفتوحة"}""", HttpStatusCode.NotFound);
            if (path == "/api/settings") return Json("""{"defaultOpeningCashPiastres":100000}""");
            if (path == "/api/treasury/balance") return Json("""{"accountId":"tre-main","balancePiastres":500000}""");
            return Json("[]");
        })) { BaseAddress = new Uri("http://fixture.invalid") }))
        // shifts_manage only: the screen's write path, without the totals reads it would add for reports_view.
        { CurrentUser = new("owner", "المالك", "owner", new[] { "shifts_manage" }) };

    private string[] StoredFiles() => Directory.Exists(_dir) ? Directory.GetFiles(_dir) : Array.Empty<string>();

    [Fact]
    public async Task A_shift_open_whose_response_was_lost_is_retried_with_the_same_id_in_the_same_session()
    {
        var sent = new List<string>();
        var vm = new ShiftsViewModel(Session(sent, "/api/shifts/open", "requestId", lose: true, shiftIsOpen: false), new HeaderViewModel());
        await vm.OpenOpenShiftModalCommand.ExecuteAsync(null);
        await vm.ConfirmOpenShiftCommand.ExecuteAsync(null);
        await vm.ConfirmOpenShiftCommand.ExecuteAsync(null);

        Assert.Equal(2, sent.Count);
        Assert.Equal(sent[0], sent[1]);
    }

    [Fact]
    public async Task A_shift_open_whose_response_was_lost_is_resent_with_the_same_id_after_a_restart()
    {
        var sent = new List<string>();
        var before = new ShiftsViewModel(Session(sent, "/api/shifts/open", "requestId", lose: true, shiftIsOpen: false), new HeaderViewModel());
        await before.OpenOpenShiftModalCommand.ExecuteAsync(null);
        before.OpeningCashText = "1000";
        await before.ConfirmOpenShiftCommand.ExecuteAsync(null);
        Assert.Single(sent);

        // The app dies here; the server did open the shift. A new process, the same station storage.
        var resent = new List<string>();
        var after = new ShiftsViewModel(Session(resent, "/api/shifts/open", "requestId", lose: false, shiftIsOpen: true), new HeaderViewModel());
        Assert.True(after.HasRecoveredShiftOperation);
        await after.RetryRecoveredShiftOperationCommand.ExecuteAsync(null);

        Assert.Equal(sent, resent);
        Assert.False(after.HasRecoveredShiftOperation);
        Assert.Empty(StoredFiles());
    }

    [Fact]
    public async Task A_shift_close_whose_response_was_lost_is_resent_with_the_same_id_after_a_restart()
    {
        var sent = new List<string>();
        var before = new ShiftsViewModel(Session(sent, "/api/shifts/close", "closeRequestId", lose: true, shiftIsOpen: true), new HeaderViewModel());
        await before.LoadAsync();
        before.OpenCloseShiftModalCommand.Execute(null);
        before.CloseShiftActualCashText = "1000";
        await before.PrepareCloseShiftCommand.ExecuteAsync(null);
        await before.ConfirmCloseShiftCommand.ExecuteAsync(null);
        Assert.Single(sent);

        // The close committed; after the restart there is no open shift, so the close modal cannot be
        // reached any more - the recovered close must still be retryable from the screen.
        var resent = new List<string>();
        var after = new ShiftsViewModel(Session(resent, "/api/shifts/close", "closeRequestId", lose: false, shiftIsOpen: false), new HeaderViewModel());
        await after.LoadAsync();
        Assert.True(after.HasRecoveredShiftOperation);
        await after.RetryRecoveredShiftOperationCommand.ExecuteAsync(null);

        Assert.Equal(sent, resent);
        Assert.False(after.HasRecoveredShiftOperation);
        Assert.Empty(StoredFiles());
    }
}

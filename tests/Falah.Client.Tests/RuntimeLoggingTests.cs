using System.Net;
using System.Text;
using Falah.Client.Diagnostics;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Runtime logging audit 2026-11-23 (docs/FINAL_RUNTIME_LOGGING_REPORT.md), fixes approved by the
/// owner: the station writes down every failed call with a reference the server logs too, the employee
/// sees that reference, and old logs are cleaned up - without ever logging a body, a PIN or a query.</summary>
[Collection(ClientLogCollection.Name)]
public sealed class RuntimeLoggingTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "falah-clientlog-" + Guid.NewGuid().ToString("N"));

    public RuntimeLoggingTests() => ClientLog.Configure(_dir, "cashier");
    public void Dispose() { ClientLog.Configure(null, "cashier"); if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    private string TodayLog() => File.ReadAllText(Path.Combine(_dir, $"cashier-{DateTime.UtcNow:yyyyMMdd}.log"));

    private sealed class Stub(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        public HttpRequestMessage? Last;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        { Last = request; return Task.FromResult(reply(request)); }
    }

    private static HttpResponseMessage Json(HttpStatusCode code, string body) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    // QA-01: the reference the server logged is in the message the employee is told to quote.
    [Fact]
    public async Task A_server_error_shows_the_reference_to_quote()
    {
        var http = new HttpClient(new Stub(_ => Json(HttpStatusCode.InternalServerError,
            "{\"error\":\"حدث خطأ غير متوقع ولم تكتمل العملية. أعد المحاولة، وإن تكرر أبلغ الدعم بالرقم المرجعي.\",\"reference\":\"abc123\"}")))
        { BaseAddress = new Uri("http://qa.invalid") };
        var cashier = await new Falah.Client.CashierWinUI.Services.ApiClient(http).GetCurrentShiftAsync();
        Assert.Contains("abc123", cashier.Error);

        var callCenter = await new Falah.Client.CallCenterWinUI.Services.ApiClient(http).GetCurrentShiftAsync();
        Assert.Contains("abc123", callCenter.Error);
    }

    [Fact]
    public async Task A_validation_refusal_keeps_its_own_message_without_a_reference()
    {
        var http = new HttpClient(new Stub(_ => Json(HttpStatusCode.UnprocessableEntity, "{\"error\":\"أدخل سبب الاسترداد.\"}"))) { BaseAddress = new Uri("http://qa.invalid") };
        var result = await new Falah.Client.CashierWinUI.Services.ApiClient(http).GetCurrentShiftAsync();
        Assert.Equal("أدخل سبب الاسترداد.", result.Error);
    }

    // L-03 + correlation: every request carries an id; a failure is logged with it, never with the query.
    [Fact]
    public async Task Every_request_carries_a_correlation_id_and_a_server_error_is_logged_with_it()
    {
        var stub = new Stub(_ => Json(HttpStatusCode.InternalServerError, "{\"error\":\"x\"}"));
        var http = new HttpClient(new CorrelationLoggingHandler(stub)) { BaseAddress = new Uri("http://qa.invalid") };

        await http.GetAsync("/api/customers/lookup?phone=01012345678");

        var id = Assert.Single(stub.Last!.Headers.GetValues(CorrelationLoggingHandler.HeaderName));
        var log = TodayLog();
        Assert.Contains(id, log);
        Assert.Contains("/api/customers/lookup", log);
        Assert.Contains("500", log);
        Assert.DoesNotContain("01012345678", log);
    }

    [Fact]
    public async Task A_lost_connection_is_logged_and_still_reaches_the_caller()
    {
        var http = new HttpClient(new CorrelationLoggingHandler(new FailingHandler())) { BaseAddress = new Uri("http://qa.invalid") };
        await Assert.ThrowsAsync<HttpRequestException>(() => http.PostAsync("/api/orders/checkout", new StringContent("{\"pin\":\"1234\"}")));
        var log = TodayLog();
        Assert.Contains("/api/orders/checkout", log);
        Assert.Contains("HttpRequestException", log);
        Assert.DoesNotContain("1234", log);
    }

    // Owner request 2026-11-27 (detailed logging): a refusal is written with the words the employee saw,
    // so "it said the drawer is short" can be found in the log. Until then refusals were not logged at all.
    [Fact]
    public async Task A_refusal_is_logged_with_the_message_the_employee_saw()
    {
        var stub = new Stub(_ => Json(HttpStatusCode.UnprocessableEntity, "{\"error\":\"رصيد الدرج لا يكفي.\"}"));
        var http = new HttpClient(new CorrelationLoggingHandler(stub)) { BaseAddress = new Uri("http://qa.invalid") };

        var response = await http.PostAsync("/api/expenses", new StringContent("{\"amount\":1800}"));

        var log = TodayLog();
        Assert.Contains("422", log);
        Assert.Contains("POST /api/expenses", log);
        Assert.Contains("رصيد الدرج لا يكفي.", log);
        Assert.Contains(Assert.Single(stub.Last!.Headers.GetValues(CorrelationLoggingHandler.HeaderName)), log);
        Assert.DoesNotContain("1800", log);
        // The caller still reads the same answer.
        Assert.Contains("رصيد الدرج لا يكفي.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_command_that_succeeded_is_logged_with_its_status_and_time()
    {
        var http = new HttpClient(new CorrelationLoggingHandler(new Stub(_ => Json(HttpStatusCode.OK, "{}")))) { BaseAddress = new Uri("http://qa.invalid") };
        await http.PostAsync("/api/orders/checkout", new StringContent("{}"));
        var log = TodayLog();
        Assert.Contains("POST /api/orders/checkout", log);
        Assert.Contains("200", log);
        Assert.Contains(" ms", log);
    }

    [Fact]
    public async Task A_successful_read_is_logged_only_in_the_detailed_mode()
    {
        var http = new HttpClient(new CorrelationLoggingHandler(new Stub(_ => Json(HttpStatusCode.OK, "{}")))) { BaseAddress = new Uri("http://qa.invalid") };
        try
        {
            ClientLog.DetailedOverride = false;
            await http.GetAsync("/api/shifts/current");
            Assert.False(File.Exists(Path.Combine(_dir, $"cashier-{DateTime.UtcNow:yyyyMMdd}.log")));

            ClientLog.DetailedOverride = true;
            await http.GetAsync("/api/customers/lookup?phone=01012345678");
            var log = TodayLog();
            Assert.Contains("GET /api/customers/lookup", log);
            Assert.DoesNotContain("01012345678", log);
        }
        finally { ClientLog.DetailedOverride = null; }
    }

    [Fact]
    public async Task A_phone_search_that_finds_nobody_is_not_logged_as_a_refusal()
    {
        var http = new HttpClient(new CorrelationLoggingHandler(new Stub(_ => new HttpResponseMessage(HttpStatusCode.NotFound)))) { BaseAddress = new Uri("http://qa.invalid") };
        try
        {
            ClientLog.DetailedOverride = false;
            await http.GetAsync("/api/customers/lookup?phone=01099999999");
            Assert.False(File.Exists(Path.Combine(_dir, $"cashier-{DateTime.UtcNow:yyyyMMdd}.log")));
        }
        finally { ClientLog.DetailedOverride = null; }
    }

    [Fact]
    public async Task An_answer_the_station_cannot_read_is_logged()
    {
        var http = new HttpClient(new Stub(_ => Json(HttpStatusCode.OK, "<html>not json</html>"))) { BaseAddress = new Uri("http://qa.invalid") };
        var result = await new Falah.Client.CashierWinUI.Services.ApiClient(http).GetCurrentShiftAsync();
        Assert.False(result.Success);
        var log = TodayLog();
        Assert.Contains("could not be read", log);
        Assert.Contains("JsonException", log);
    }

    [Fact]
    public void A_caught_error_is_written_with_its_stack()
    {
        try { throw new InvalidOperationException("report file locked"); }
        catch (Exception ex) { ClientLog.WriteException("report export", ex); }
        var log = TodayLog();
        Assert.Contains("report export: InvalidOperationException: report file locked", log);
        Assert.Contains(nameof(A_caught_error_is_written_with_its_stack), log);
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => throw new HttpRequestException("network down");
    }

    // L-04: who wrote it, and old files go.
    [Fact]
    public void A_log_line_names_the_station_version_and_machine()
    {
        ClientLog.Write("ERROR", "test");
        var line = TodayLog();
        Assert.Contains("cashier", line);
        Assert.Contains(Environment.MachineName, line);
        Assert.Contains(ClientLog.Version, line);
    }

    [Fact]
    public void Logs_older_than_the_retention_are_deleted_and_recent_ones_kept()
    {
        Directory.CreateDirectory(_dir);
        var old = Path.Combine(_dir, "crash-20200101.log");
        var recent = Path.Combine(_dir, "cashier-recent.log");
        File.WriteAllText(old, "x"); File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-40));
        File.WriteAllText(recent, "x");
        ClientLog.DeleteOldLogs(retentionDays: 30);
        Assert.False(File.Exists(old));
        Assert.True(File.Exists(recent));
    }

    [Fact]
    public void Logging_into_an_unusable_folder_never_throws()
    {
        ClientLog.Configure(Path.Combine(_dir, "file-not-folder"), "cashier");
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "file-not-folder"), "a file where the folder should be");
        ClientLog.Write("ERROR", "still fine");
        ClientLog.DeleteOldLogs(30);
    }

    [Fact]
    public void A_crash_entry_names_the_version_and_machine()
    {
        var path = CrashLog.Write(_dir, "cashier", new InvalidOperationException("boom"), "UI");
        var text = File.ReadAllText(path!);
        Assert.Contains("boom", text);
        Assert.Contains(Environment.MachineName, text);
        Assert.Contains(ClientLog.Version, text);
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ClientLogCollection { public const string Name = "client-log"; }

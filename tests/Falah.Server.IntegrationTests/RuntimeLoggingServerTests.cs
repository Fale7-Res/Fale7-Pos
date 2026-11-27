namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Falah.Server.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// Runtime logging audit 2026-11-23 (docs/FINAL_RUNTIME_LOGGING_REPORT.md, L-01 / QA-01), fixes approved by
/// the owner: the server keeps its own rolling log file; a request names its correlation id, which the
/// server uses as the reference it returns and logs, so the station's report and the server's line meet.
/// </summary>
public class RuntimeLoggingServerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "falah-serverlog-" + Guid.NewGuid().ToString("N"));

    public void Dispose() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    private FalahApiFactory LoggingFactory(string? detailedFlagFile = null) => new(configOverrides: new Dictionary<string, string?>
    {
        ["Logging:File:Enabled"] = "true",
        ["Logging:File:Directory"] = _dir,
        // Never the machine's real flag: a test must not depend on whether this PC has detailed mode on.
        ["Logging:DetailedFlagFile"] = detailedFlagFile ?? Path.Combine(_dir, "no-such-flag.on"),
    });

    // ---- Owner request 2026-11-27: what each request did, refusals with their words, never a body ----

    [Fact]
    public async Task A_refusal_is_logged_with_its_message_user_and_reference_but_not_the_body()
    {
        using var factory = LoggingFactory();
        var cashier = await factory.SeedUserAsync("كاشير السجل", "cashier", "pos", "expenses_add_drawer");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/expenses")
        {
            Content = JsonContent.Create(new Falah.Application.Expenses.AddExpenseRequest(
                "qa-log-exp-1", "shift-that-does-not-exist", "غاز وفحم", 180_000, "secret-body-marker", "tre-drawer", cashier, null, null)),
        };
        request.Headers.Add("X-Correlation-Id", "qa-refusal-1");

        var response = await factory.CreateAuthenticatedClient().SendAsync(request);

        Assert.False(response.IsSuccessStatusCode);
        var log = await WaitForAsync(ReadLogs, "qa-refusal-1");
        var line = Assert.Single(log.Split('\n'), l => l.Contains("qa-refusal-1") && l.Contains("/api/expenses"));
        Assert.Contains("refused " + (int)response.StatusCode, line);
        Assert.Contains("الوردية غير موجودة", line);
        Assert.Contains(cashier, line);
        Assert.Contains(" ms", line);
        Assert.DoesNotContain("secret-body-marker", log);
        Assert.DoesNotContain("180000", log);
    }

    [Fact]
    public async Task A_wrong_pin_is_logged_as_a_refusal_without_the_pin()
    {
        using var factory = LoggingFactory();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login-by-pin") { Content = JsonContent.Create(new { pin = "864213" }) };
        request.Headers.Add("X-Correlation-Id", "qa-pin-1");

        await factory.CreateClient().SendAsync(request);

        var log = await WaitForAsync(ReadLogs, "qa-pin-1");
        Assert.Contains("POST /api/auth/login-by-pin", log);
        Assert.DoesNotContain("864213", log);
    }

    [Fact]
    public async Task A_command_that_succeeded_is_logged_with_its_status_and_user()
    {
        using var factory = LoggingFactory();
        var user = await factory.SeedUserAsync("مستخدم السجل", "cashier", "pos");
        var client = factory.CreateAuthenticatedClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout") { Content = JsonContent.Create(new { }) };
        request.Headers.Add("X-Correlation-Id", "qa-write-1");

        var response = await client.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode, $"logout answered {(int)response.StatusCode}");
        var log = await WaitForAsync(ReadLogs, "qa-write-1");
        var line = Assert.Single(log.Split('\n'), l => l.Contains("qa-write-1"));
        Assert.Contains($"POST /api/auth/logout -> {(int)response.StatusCode}", line);
        Assert.Contains(user, line);
    }

    [Fact]
    public async Task A_read_is_logged_only_in_the_detailed_mode_and_never_with_its_query()
    {
        var flag = Path.Combine(_dir, "detailed-logging.on");
        using (var normal = LoggingFactory(flag))
        {
            await normal.SeedUserAsync("قارئ", "cashier", "pos", "delivery_manage");
            var quiet = new HttpRequestMessage(HttpMethod.Get, "/api/delivery/services/all?phone=01055512345");
            quiet.Headers.Add("X-Correlation-Id", "qa-read-quiet");
            await normal.CreateAuthenticatedClient().SendAsync(quiet);
            await normal.CreateClient().GetAsync("/health");   // flushes nothing on its own; a marker that the host ran
            await Task.Delay(500);
            Assert.DoesNotContain("qa-read-quiet", ReadLogs());
        }

        Directory.CreateDirectory(_dir);
        File.WriteAllText(flag, "");
        using var detailed = LoggingFactory(flag);
        await detailed.SeedUserAsync("قارئ", "cashier", "pos", "delivery_manage");
        var loud = new HttpRequestMessage(HttpMethod.Get, "/api/delivery/services/all?phone=01055512345");
        loud.Headers.Add("X-Correlation-Id", "qa-read-loud");
        await detailed.CreateAuthenticatedClient().SendAsync(loud);

        var log = await WaitForAsync(ReadLogs, "qa-read-loud");
        Assert.Contains("GET /api/delivery/services/all", log);
        Assert.DoesNotContain("01055512345", log);
    }

    private string ReadLogs()
    {
        var text = new StringBuilder();
        foreach (var file in Directory.GetFiles(_dir, "*.log"))
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream)) text.Append(reader.ReadToEnd());
        return text.ToString();
    }

    private static async Task<string> WaitForAsync(Func<string> read, string expected)
    {
        for (var i = 0; i < 50; i++)
        {
            var text = read();
            if (text.Contains(expected)) return text;
            await Task.Delay(100);
        }
        return read();
    }

    [Fact]
    public async Task The_reference_returned_and_logged_is_the_callers_correlation_id()
    {
        using var factory = LoggingFactory();
        var client = factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login-by-pin") { Content = new StringContent("{pin:", Encoding.UTF8, "application/json") };
        request.Headers.Add("X-Correlation-Id", "qa-corr-1");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("qa-corr-1", body.GetProperty("reference").GetString());
        Assert.Equal("qa-corr-1", Assert.Single(response.Headers.GetValues("X-Correlation-Id")));
        var log = await WaitForAsync(ReadLogs, "qa-corr-1");
        Assert.Contains("qa-corr-1", log);
        Assert.Contains("Rejected malformed", log);
    }

    [Fact]
    public async Task A_correlation_id_that_is_not_a_plain_token_is_ignored()
    {
        using var factory = LoggingFactory();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login-by-pin") { Content = new StringContent("{pin:", Encoding.UTF8, "application/json") };
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", new string('a', 65) + "<script>");

        var body = await (await factory.CreateClient().SendAsync(request)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.DoesNotContain("script", body.GetProperty("reference").GetString());
    }

    [Fact]
    public async Task Startup_is_logged_with_the_version_and_machine()
    {
        using var factory = LoggingFactory();
        await factory.CreateClient().GetAsync("/health");
        var log = await WaitForAsync(ReadLogs, "started");
        Assert.Contains("started", log);
        Assert.Contains(Environment.MachineName, log);
        Assert.Contains(RollingFileLoggerProvider.Version, log);
    }

    [Fact]
    public async Task The_test_host_writes_no_log_file_unless_asked()
    {
        using var factory = new FalahApiFactory();
        await factory.CreateClient().GetAsync("/health");
        Assert.False(factory.Services.GetService(typeof(IEnumerable<ILoggerProvider>)) is IEnumerable<ILoggerProvider> providers
            && providers.OfType<RollingFileLoggerProvider>().Any(p => p.Enabled));
    }

    // Rotation and retention, on the provider itself.
    [Fact]
    public void A_full_file_rolls_over_and_files_past_retention_are_deleted()
    {
        Directory.CreateDirectory(_dir);
        var stale = Path.Combine(_dir, "server-20200101.log");
        File.WriteAllText(stale, "old");
        File.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddDays(-40));

        using (var provider = new RollingFileLoggerProvider(new FileLogOptions(true, _dir, MaxFileBytes: 2048, RetentionDays: 30)))
        {
            var logger = provider.CreateLogger("Test");
            for (var i = 0; i < 200; i++) logger.LogWarning("line {Index} {Padding}", i, new string('x', 40));
        }

        Assert.False(File.Exists(stale));
        var files = Directory.GetFiles(_dir, "server-*.log");
        Assert.True(files.Length >= 2, "expected the file to roll over at 2 KB");
        Assert.All(files, f => Assert.True(new FileInfo(f).Length <= 2048 + 512));
    }

    [Fact]
    public void A_critical_line_is_on_disk_at_once_even_if_the_process_ends_right_after()
    {
        var provider = new RollingFileLoggerProvider(new FileLogOptions(true, _dir, 1024 * 1024, 30));
        provider.CreateLogger("Test").LogCritical("startup failed: database unreachable");
        // Not disposed: a process ending on a startup failure never gets that far.
        Assert.Contains("startup failed", ReadLogs());
        provider.Dispose();
    }

    [Fact]
    public void An_unusable_log_folder_never_throws()
    {
        Directory.CreateDirectory(_dir);
        var blocked = Path.Combine(_dir, "a-file");
        File.WriteAllText(blocked, "not a folder");
        using var provider = new RollingFileLoggerProvider(new FileLogOptions(true, blocked, 1024 * 1024, 30));
        provider.CreateLogger("Test").LogError(new InvalidOperationException("boom"), "still fine");
    }
}

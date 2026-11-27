namespace Falah.Server.Diagnostics;

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

/// <summary>
/// Owner request 2026-11-27 (detailed logging): one line in the server log per API request worth knowing
/// about - method, path, status, time taken, the signed-in user and the reference the station quotes:
/// <list type="bullet">
/// <item>every command (anything but GET), except the print agents' claim polls, which run every few seconds;</item>
/// <item>every refusal (4xx) and server error (5xx), with the server's own Arabic message - the words the
/// employee saw (a 5xx's stack trace is already written by the exception handler under the same reference);</item>
/// <item>anything slower than two seconds;</item>
/// <item>in the detailed mode (<see cref="DetailedLogging"/>) every read and poll as well.</item>
/// </list>
/// Never written: the query string (a phone number travels in one), headers, or the request body (PINs,
/// amounts). Only the first 4 KB of an answer is looked at, and only to read the <c>error</c> of a refusal.
/// Levels: Information, Warning for slow requests and server errors - so the Windows Event Log, which only
/// takes warnings, is not filled with ordinary refusals.
/// </summary>
public sealed class RequestLogMiddleware
{
    public const int SlowMilliseconds = 2000;
    private const int CaptureBytes = 4096;

    private static readonly string[] PollPaths = { "/api/print-jobs/claim", "/api/cash-drawer/claim" };

    private readonly RequestDelegate _next;
    private readonly ILogger _log;
    private readonly DetailedLogging _detailed;

    public RequestLogMiddleware(RequestDelegate next, ILoggerFactory loggers, DetailedLogging detailed)
    {
        _next = next;
        _log = loggers.CreateLogger("Falah.Server.Requests");
        _detailed = detailed;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var clock = Stopwatch.StartNew();
        var original = context.Response.Body;
        var capture = new CapturingStream(original, CaptureBytes);
        context.Response.Body = capture;
        try
        {
            await _next(context);
        }
        finally
        {
            context.Response.Body = original;
            try { Write(context, clock.ElapsedMilliseconds, capture); }
            catch { /* a log line is never worth a failed request */ }
        }
    }

    private void Write(HttpContext context, long ms, CapturingStream capture)
    {
        var request = context.Request;
        var status = context.Response.StatusCode;
        var path = request.Path.Value ?? "";
        var isRead = HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method);
        var isPoll = PollPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase));
        var user = (context.Items[AuthenticationMiddleware.ActorItemKey] as Falah.Infrastructure.Persistence.Entities.UserEntity)?.Id ?? "-";
        var reference = context.TraceIdentifier;
        var aborted = context.RequestAborted.IsCancellationRequested;

        var message = status >= 400 ? ErrorText(capture.Captured, context.Response.ContentType) : null;
        // A read answering 404 with nothing to say is "no such customer yet" - the ordinary answer to a phone
        // search, not a refusal. It is written like any read.
        var plainNotFound = isRead && status == 404 && message is null;

        if (status >= 400 && !plainNotFound)
        {
            var level = status >= 500 ? LogLevel.Warning : LogLevel.Information;
            _log.Log(level, "{Method} {Path} {Outcome} {Status} in {Ms} ms, user {User} (reference {Reference}){Message}",
                request.Method, path, status >= 500 ? "failed" : isRead && status == 404 ? "found nothing" : "refused", status, ms, user, reference, message is null ? "" : ": " + message);
        }
        else if (ms >= SlowMilliseconds)
            _log.LogWarning("slow: {Method} {Path} -> {Status} in {Ms} ms, user {User} (reference {Reference})",
                request.Method, path, status, ms, user, reference);
        else if ((!isRead && !isPoll) || _detailed.IsOn)
            _log.LogInformation("{Method} {Path} -> {Status} in {Ms} ms, user {User} (reference {Reference}){Aborted}",
                request.Method, path, status, ms, user, reference, aborted ? " - the station hung up first" : "");
    }

    private static string? ErrorText(byte[] body, string? contentType)
    {
        if (body.Length == 0 || contentType is null || !contentType.Contains("json", StringComparison.OrdinalIgnoreCase)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
            {
                var text = error.GetString();
                return text is { Length: > 500 } ? text[..500] + "…" : text;
            }
        }
        catch (JsonException) { /* cut off at 4 KB, or not the {error} shape */ }
        return null;
    }

    /// <summary>Passes every byte straight through and keeps a copy of the first few kilobytes.</summary>
    private sealed class CapturingStream(Stream inner, int limit) : Stream
    {
        private readonly MemoryStream _copy = new();
        public byte[] Captured => _copy.ToArray();

        private void Keep(ReadOnlySpan<byte> data)
        {
            var room = limit - (int)_copy.Length;
            if (room > 0) _copy.Write(data[..Math.Min(room, data.Length)]);
        }

        public override void Write(byte[] buffer, int offset, int count) { Keep(buffer.AsSpan(offset, count)); inner.Write(buffer, offset, count); }
        public override void Write(ReadOnlySpan<byte> buffer) { Keep(buffer); inner.Write(buffer); }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
        { Keep(buffer.AsSpan(offset, count)); return inner.WriteAsync(buffer, offset, count, ct); }
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
        { Keep(buffer.Span); return inner.WriteAsync(buffer, ct); }
        public override void Flush() => inner.Flush();
        public override Task FlushAsync(CancellationToken ct) => inner.FlushAsync(ct);

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}

/// <summary>The detailed mode: on while the file <c>Logging:DetailedFlagFile</c> exists (default
/// <c>%ProgramData%\Falah\detailed-logging.on</c>, the same file the stations look for - the drive's
/// detailed-on.cmd makes it, detailed-off.cmd removes it). Looked up at most every 15 seconds.</summary>
public sealed class DetailedLogging
{
    private readonly string _flagFile;
    private long _checkedAt;
    private bool _on;

    public DetailedLogging(IConfiguration configuration) =>
        _flagFile = configuration["Logging:DetailedFlagFile"] is { Length: > 0 } path
            ? path
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Falah", "detailed-logging.on");

    public bool IsOn
    {
        get
        {
            var now = Environment.TickCount64;
            if (_checkedAt != 0 && now - _checkedAt < 15_000) return _on;
            try { _on = File.Exists(_flagFile); } catch { _on = false; }
            _checkedAt = now;
            return _on;
        }
    }
}

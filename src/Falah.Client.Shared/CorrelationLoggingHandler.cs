using System.Diagnostics;
using System.Text.Json;

namespace Falah.Client.Diagnostics;

/// <summary>
/// Runtime logging audit 2026-11-23 (QA-01 / L-03): every request leaves the station with an
/// <c>X-Correlation-Id</c>. The server adopts it as the request's reference - the number in its error
/// answer and in its own log - so the line written here and the server's line carry the same value,
/// even when the answer never arrived.
///
/// Owner request 2026-11-27 (detailed logging): written here, each with the time it took -
/// <list type="bullet">
/// <item>a command (anything but GET) and its status;</item>
/// <item>a refusal (4xx) with the server's own message - the words the employee saw;</item>
/// <item>a server error (5xx) with its message, and a request with no answer;</item>
/// <item>anything slower than two seconds;</item>
/// <item>in the detailed mode (<see cref="ClientLog.Detailed"/>) every read as well.</item>
/// </list>
/// Only the method and the path are written - never the query string (a phone number travels in one),
/// headers or the request body. The answer is read only when it is a refusal or an error.
/// </summary>
public sealed class CorrelationLoggingHandler : DelegatingHandler
{
    public const string HeaderName = "X-Correlation-Id";
    public const int SlowMilliseconds = 2000;

    public CorrelationLoggingHandler(HttpMessageHandler inner) : base(inner) { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(HeaderName))
            request.Headers.TryAddWithoutValidation(HeaderName, Guid.NewGuid().ToString("N"));
        var id = string.Join(",", request.Headers.GetValues(HeaderName));
        var path = request.RequestUri is { IsAbsoluteUri: true } uri ? uri.AbsolutePath : request.RequestUri?.OriginalString.Split('?')[0];
        var clock = Stopwatch.StartNew();

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            ClientLog.Write("WARN", $"no answer to {request.Method} {path} after {clock.ElapsedMilliseconds} ms (reference {id}): {ex.GetType().Name}: {ex.Message}");
            throw;
        }
        var ms = clock.ElapsedMilliseconds;
        var code = (int)response.StatusCode;
        var isRead = request.Method == HttpMethod.Get || request.Method == HttpMethod.Head;
        // The header's health check runs every five seconds: written only when it fails.
        var isHealth = string.Equals(path, "/health", StringComparison.OrdinalIgnoreCase);

        var message = code >= 400 ? await ServerMessageAsync(response, cancellationToken) : null;
        // A read answering 404 with nothing to say is "no such customer yet" - the ordinary answer to a phone
        // search, not a refusal. It is written like any read.
        var plainNotFound = isRead && code == 404 && message is null;

        if (code >= 400 && !plainNotFound)
        {
            // A read that found nothing but says so ("no shift is open") is information, not a refusal.
            var readNotFound = isRead && code == 404;
            ClientLog.Write(code >= 500 ? "ERROR" : readNotFound ? "INFO" : "WARN",
                $"server {(code >= 500 ? "answered" : readNotFound ? "found nothing" : "refused")} {code} to {request.Method} {path} in {ms} ms (reference {id})"
                + (message is null ? "" : $": {message}"));
        }
        else if (ms >= SlowMilliseconds)
            ClientLog.Write("WARN", $"slow: {request.Method} {path} -> {code} in {ms} ms (reference {id})");
        else if (!isRead)
            ClientLog.Write("INFO", $"{request.Method} {path} -> {code} in {ms} ms (reference {id})");
        else if (!isHealth && ClientLog.Detailed)
            ClientLog.Write("DEBUG", $"{request.Method} {path} -> {code} in {ms} ms (reference {id})");
        return response;
    }

    /// <summary>The server's <c>error</c> text. The answer is buffered first, so the caller still reads it in full.</summary>
    private static async Task<string?> ServerMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            await response.Content.LoadIntoBufferAsync();
            var text = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(text)) return null;
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
            {
                var message = error.GetString();
                return message is { Length: > 500 } ? message[..500] + "…" : message;
            }
            return null;
        }
        catch
        {
            return null;   // not the {error} shape: the status alone is logged
        }
    }
}

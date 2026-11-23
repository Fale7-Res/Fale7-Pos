namespace Falah.Client.Diagnostics;

/// <summary>
/// Runtime logging audit 2026-11-23 (QA-01 / L-03): every request leaves the station with an
/// <c>X-Correlation-Id</c>. The server adopts it as the request's reference - the number in its error
/// answer and in its own log - so the line written here and the server's line carry the same value,
/// even when the answer never arrived. Logged here: a server error (5xx) and a request with no answer.
/// A business refusal (4xx) is the server's ordinary answer and is not logged. Only the method and the
/// path are written - never the query string (a phone number travels in one), headers or the body.
/// </summary>
public sealed class CorrelationLoggingHandler : DelegatingHandler
{
    public const string HeaderName = "X-Correlation-Id";

    public CorrelationLoggingHandler(HttpMessageHandler inner) : base(inner) { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(HeaderName))
            request.Headers.TryAddWithoutValidation(HeaderName, Guid.NewGuid().ToString("N"));
        var id = string.Join(",", request.Headers.GetValues(HeaderName));
        var path = request.RequestUri is { IsAbsoluteUri: true } uri ? uri.AbsolutePath : request.RequestUri?.OriginalString.Split('?')[0];

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            ClientLog.Write("WARN", $"no answer to {request.Method} {path} (reference {id}): {ex.GetType().Name}: {ex.Message}");
            throw;
        }
        if ((int)response.StatusCode >= 500)
            ClientLog.Write("ERROR", $"server answered {(int)response.StatusCode} to {request.Method} {path} (reference {id})");
        return response;
    }
}

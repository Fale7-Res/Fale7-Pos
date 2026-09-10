namespace Falah.Server.IntegrationTests;

using System.Text.Json;

/// <summary>
/// Attaches the session token of whichever user a request says is acting.
///
/// The server no longer believes the <c>actorUserId</c> a request carries - it requires a session
/// token and rejects any request whose claimed actor is not that session. Four hundred and sixty-odd
/// call sites across this suite are written in the older shape, and rewriting them all would change
/// far more test code than the fix it is verifying.
///
/// So this reads the actor out of the outgoing request exactly the way the server's middleware
/// reads it, and presents that user's real token. These are genuine sessions seeded through the
/// real table, resolved by the real SessionService, through the real middleware - there is no
/// test-only bypass anywhere in the server, and a test that names one user while another is
/// authenticated still gets the 403 a caller would.
///
/// A request that names nobody - a plain read, or manager approval, whose approver comes from the
/// PIN it carries - goes out on any seeded session, because in the real system every request comes
/// from a station where somebody is logged in. The security property still holds where it matters:
/// when a request DOES name an actor, the token presented has to be that actor's.
///
/// Tests that assert unauthenticated access is refused build their own client without this handler.
/// </summary>
public sealed class ActorTokenHandler : DelegatingHandler
{
    private readonly Func<string, string?> _tokenFor;
    private readonly Func<string?> _anyToken;

    public ActorTokenHandler(Func<string, string?> tokenFor, Func<string?> anyToken)
    {
        _tokenFor = tokenFor;
        _anyToken = anyToken;
    }

    /// <summary>The same field names the server's own middleware treats as "who is acting".</summary>
    private static readonly string[] ActorFields =
    {
        "actorUserId", "cashierId", "createdByUserId", "openedByUserId", "reportedByUserId",
    };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var actorId = FromQuery(request) ?? await FromBodyAsync(request, ct);
        var token = actorId is not null ? _tokenFor(actorId) : null;

        // Nothing named an actor - a plain read, or manager approval whose approver comes from its
        // PIN. Either way it arrives from a station where somebody is logged in.
        token ??= _anyToken();

        if (token is not null)
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, ct);
    }

    private static string? FromQuery(HttpRequestMessage request)
    {
        var query = request.RequestUri?.Query;
        if (string.IsNullOrEmpty(query)) return null;
        var parsed = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(query);
        foreach (var field in ActorFields)
            if (parsed.TryGetValue(field, out var value) && !string.IsNullOrEmpty(value))
                return value.ToString();
        return null;
    }

    private static async Task<string?> FromBodyAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (request.Content is null) return null;
        var media = request.Content.Headers.ContentType?.MediaType;
        if (media is null || !media.Contains("json", StringComparison.OrdinalIgnoreCase)) return null;

        // Buffered so the body can still be sent afterwards.
        var body = await request.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
            foreach (var field in ActorFields)
                if (document.RootElement.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String)
                    return value.GetString();
        }
        catch (JsonException)
        {
            // A test deliberately sending malformed JSON should reach the server malformed.
        }
        return null;
    }
}

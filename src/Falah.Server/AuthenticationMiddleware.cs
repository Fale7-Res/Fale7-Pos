namespace Falah.Server;

using Falah.Infrastructure.Authorization;

/// <summary>
/// Establishes who the caller is, before any endpoint runs.
///
/// The rule this enforces is simple and the whole point: <b>the token is the identity</b>. Requests
/// still carry an <c>actorUserId</c> in their bodies - hundreds of call sites and two shipped
/// clients depend on that shape - but the server no longer believes it. Endpoints read the actor
/// from here instead, and this value comes from a session the server itself issued.
///
/// Left open deliberately: the login endpoint (there is no session yet to present) and the health
/// probe (it answers nothing about the business and is how a station checks connectivity before a
/// user has logged in).
/// </summary>
public sealed class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    public AuthenticationMiddleware(RequestDelegate next) => _next = next;

    private static readonly string[] AnonymousPaths =
    {
        "/health",
        "/api/auth/login-by-pin",
    };

    public const string ActorItemKey = "falah.actor";

    public async Task InvokeAsync(HttpContext context, SessionService sessions)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Swagger and static files are not the business API and carry no actor.
        var isApi = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
        if (!isApi || AnonymousPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var header = context.Request.Headers.Authorization.ToString();
        var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : null;

        var user = await sessions.ResolveAsync(token, context.RequestAborted);
        if (user is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "انتهت الجلسة أو لم يتم تسجيل الدخول. سجّل الدخول مرة أخرى.",
            });
            return;
        }

        context.Items[ActorItemKey] = user;

        // Requests still carry the acting user's id in their query or body - hundreds of call sites
        // and two shipped clients are built that shape, and rewriting them all would be a far
        // larger change than the one that closes the hole. So the id is kept, and disbelieved:
        // whatever it claims must match the session that was actually presented.
        //
        // Deliberately NOT checked: ManagerApproverUserId and ApproverUserId. Those name a SECOND
        // person - a manager approving on a cashier's screen - and forcing them to equal the
        // session would break manager approval entirely.
        var mismatch = await FindActorMismatchAsync(context, user.Id);
        if (mismatch is not null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "لا يمكن تنفيذ عملية باسم مستخدم آخر.",
            });
            return;
        }

        await _next(context);
    }

    /// <summary>Field names that mean "the user performing this request". Every one of them used to
    /// be taken on trust.</summary>
    private static readonly string[] ActorFields =
    {
        "actorUserId", "cashierId", "createdByUserId", "openedByUserId", "reportedByUserId",
    };

    private static async Task<string?> FindActorMismatchAsync(HttpContext context, string authenticatedUserId)
    {
        foreach (var field in ActorFields)
        {
            if (context.Request.Query.TryGetValue(field, out var values))
            {
                var claimed = values.ToString();
                if (!string.IsNullOrEmpty(claimed) && claimed != authenticatedUserId) return field;
            }
        }

        if (!context.Request.HasJsonContentType()) return null;

        // Buffered so the endpoint's own model binding can still read the body afterwards.
        context.Request.EnableBuffering();
        try
        {
            using var document = await System.Text.Json.JsonDocument.ParseAsync(
                context.Request.Body, cancellationToken: context.RequestAborted);
            if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) return null;

            foreach (var field in ActorFields)
            {
                if (document.RootElement.TryGetProperty(field, out var value)
                    && value.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var claimed = value.GetString();
                    if (!string.IsNullOrEmpty(claimed) && claimed != authenticatedUserId) return field;
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Malformed JSON is the endpoint's problem to report, not this middleware's.
            return null;
        }
        finally
        {
            context.Request.Body.Position = 0;
        }

        return null;
    }
}

public static class HttpContextActorExtensions
{
    /// <summary>The authenticated user for this request. Endpoints use this instead of whatever the
    /// request body claims - that is the entire fix.</summary>
    public static Falah.Infrastructure.Persistence.Entities.UserEntity Actor(this HttpContext context) =>
        context.Items[AuthenticationMiddleware.ActorItemKey] as Falah.Infrastructure.Persistence.Entities.UserEntity
            ?? throw new InvalidOperationException("No authenticated actor on this request.");

    public static string ActorId(this HttpContext context) => context.Actor().Id;
}

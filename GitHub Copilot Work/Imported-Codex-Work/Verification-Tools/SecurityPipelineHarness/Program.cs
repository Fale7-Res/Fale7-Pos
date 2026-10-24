using System.Text;
using System.Text.Json;
using Falah.Application.Users;
using Falah.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

// Pure in-memory real RequestDelegateFactory binding. No host, network listener, database,
// real credential/session, handler mutation, application startup, service, file data or printer.
using var services = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
var cases = new[] {
    new Probe("matching", "\"actorUserId\":\"restricted-placeholder\"", true, 204, "restricted-placeholder"),
    new Probe("canonical-mismatch", "\"actorUserId\":\"privileged-placeholder\"", true, 403, null),
    new Probe("alternate-case", "\"ActorUserId\":\"privileged-placeholder\"", true, 204, "privileged-placeholder"),
    new Probe("upper-case", "\"ACTORUSERID\":\"privileged-placeholder\"", true, 204, "privileged-placeholder"),
    new Probe("mixed-distinct-keys", "\"actorUserId\":\"restricted-placeholder\",\"ActorUserId\":\"privileged-placeholder\"", true, 204, "privileged-placeholder"),
    new Probe("mixed-reversed-keys", "\"ActorUserId\":\"privileged-placeholder\",\"actorUserId\":\"restricted-placeholder\"", true, 204, "restricted-placeholder"),
    new Probe("duplicate-exact-mismatch-last", "\"actorUserId\":\"restricted-placeholder\",\"actorUserId\":\"privileged-placeholder\"", true, 403, null),
    new Probe("escaped-canonical-mismatch", "\"\\u0061ctorUserId\":\"privileged-placeholder\"", true, 403, null),
    new Probe("null-actor", "\"actorUserId\":null", true, 204, null),
    new Probe("missing-session", "\"ActorUserId\":\"privileged-placeholder\"", false, 401, null),
};
var observations = new List<object>();
foreach (var test in cases)
{
    Console.Error.WriteLine("audit-probe-start:" + test.Name);
    string? bound = null;
    var reached = false;
    var endpoint = RequestDelegateFactory.Create((UpdateUserPermissionsRequest body) =>
    {
        reached = true;
        bound = body.ActorUserId;
        return Results.NoContent();
    }, new RequestDelegateFactoryOptions { ServiceProvider = services, ThrowOnBadRequest = true });
    Console.Error.WriteLine("audit-binding-created:" + test.Name);
    var http = new DefaultHttpContext { RequestServices = services };
    http.Request.Method = "POST";
    http.Request.Path = "/api/isolated-audit-probe";
    http.Request.ContentType = "application/json";
    if (test.Authenticated) http.Request.Headers.Authorization = "Bearer synthetic-placeholder";
    var bytes = Encoding.UTF8.GetBytes("{\"userId\":\"restricted-placeholder\",\"grant\":[],\"revoke\":[]," + test.Fields + "}");
    http.Request.ContentLength = bytes.Length;
    http.Request.Body = new MemoryStream(bytes);
    http.Response.Body = new MemoryStream();
    http.Features.Set<IHttpRequestBodyDetectionFeature>(new BodyFeature());
    await new AuthenticationMiddleware(endpoint.RequestDelegate).InvokeAsync(http, new Falah.Infrastructure.Authorization.SessionService());
    Console.Error.WriteLine("audit-probe-returned:" + test.Name);
    if (http.Response.StatusCode != test.ExpectedStatus || bound != test.ExpectedActor || reached != (test.ExpectedStatus == 204))
        throw new InvalidOperationException("Unexpected defensive probe result: " + test.Name);
    observations.Add(new { test.Name, status = http.Response.StatusCode, endpointReached = reached,
        boundActor = bound, assertedExpectedOutcome = true });
}
Console.WriteLine(JsonSerializer.Serialize(new {
    baseline = "c35816ecae49633674775dd32061c6ee92f94aaf", casesExecuted = observations.Count,
    boundary = "Real Minimal API RequestDelegateFactory + baseline middleware/DTO; fake session resolver, no real server or permission handler/database",
    fullEndToEndExploitVerified = false, observations
}, new JsonSerializerOptions { WriteIndented = true }));

record Probe(string Name, string Fields, bool Authenticated, int ExpectedStatus, string? ExpectedActor);
sealed class BodyFeature : IHttpRequestBodyDetectionFeature { public bool CanHaveBody => true; }
namespace Falah.Infrastructure.Persistence.Entities
{
    public sealed class UserEntity { public required string Id { get; init; } }
}
namespace Falah.Infrastructure.Authorization
{
    public sealed class SessionService
    {
        public Task<Falah.Infrastructure.Persistence.Entities.UserEntity?> ResolveAsync(string? token, CancellationToken ct) =>
            Task.FromResult<Falah.Infrastructure.Persistence.Entities.UserEntity?>(token == "synthetic-placeholder" ? new() { Id = "restricted-placeholder" } : null);
    }
}

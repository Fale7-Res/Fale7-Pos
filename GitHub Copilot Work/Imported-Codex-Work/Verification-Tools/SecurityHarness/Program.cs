using System.Text;
using System.Text.Json;
using Falah.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

// Pure in-memory parser/middleware experiment. No server, sockets, database, real accounts,
// business operations, credentials, files, or printers. Baseline middleware is linked unchanged.
var results = new List<object>();
foreach (var field in new[] { "actorUserId", "ActorUserId", "ACTORUSERID" })
{
    var context = new DefaultHttpContext();
    context.RequestServices = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
    context.Request.Path = "/api/isolated-audit-harness";
    context.Request.Method = "POST";
    context.Request.ContentType = "application/json";
    context.Request.Headers.Authorization = "Bearer synthetic-session-placeholder";
    context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"userId\":\"restricted-user-placeholder\",\"grant\":[\"all\"],\"revoke\":[],\"" + field + "\":\"privileged-user-placeholder\"}"));
    context.Response.Body = new MemoryStream();
    string? boundActor = null;
    var reached = false;
    var web = new JsonOptions().SerializerOptions;
    var middleware = new AuthenticationMiddleware(async http =>
    {
        reached = true;
        var request = await JsonSerializer.DeserializeAsync<Falah.Application.Users.UpdateUserPermissionsRequest>(http.Request.Body, web);
        boundActor = request?.ActorUserId;
    });
    await middleware.InvokeAsync(context, new Falah.Infrastructure.Authorization.SessionService());
    if (field == "actorUserId" && (reached || context.Response.StatusCode != 403))
        throw new InvalidOperationException("Expected baseline camelCase mismatch rejection");
    if (field != "actorUserId" && (!reached || boundActor != "privileged-user-placeholder"))
        throw new InvalidOperationException("Expected alternate-case parser differential not reproduced");
    results.Add(new { field, status = context.Response.StatusCode, endpointReached = reached,
        boundActor, authenticatedActor = "restricted-user-placeholder", web.PropertyNameCaseInsensitive });
}
Console.WriteLine(JsonSerializer.Serialize(new { purpose = "Baseline middleware/parser mismatch only; no live exploit or database operation",
    baseline = "c35816ecae49633674775dd32061c6ee92f94aaf", results }, new JsonSerializerOptions { WriteIndented = true }));
if (results.Count != 3) throw new InvalidOperationException("Harness did not run every case");

namespace Falah.Infrastructure.Persistence.Entities
{
    public sealed class UserEntity { public required string Id { get; init; } }
}

namespace Falah.Infrastructure.Authorization
{
    public sealed class SessionService
    {
        public Task<Falah.Infrastructure.Persistence.Entities.UserEntity?> ResolveAsync(string? token, CancellationToken ct) =>
            Task.FromResult<Falah.Infrastructure.Persistence.Entities.UserEntity?>(new() { Id = "restricted-user-placeholder" });
    }
}

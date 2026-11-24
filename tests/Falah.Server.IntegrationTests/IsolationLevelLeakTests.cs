namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Falah.Application.Printing;
using Falah.Application.Users;

/// <summary>Found on the restaurant laptop 2026-11-23: right after a manager changed a user's permissions, the
/// print agents' poll failed with HTTP 500 - "You can only specify the READPAST lock in the READ COMMITTED or
/// REPEATABLE READ isolation levels". The permission and activation commands run SERIALIZABLE transactions, and
/// SQL Server keeps a connection's isolation level when the pool hands it to the next request, so whatever ran
/// next on that connection ran SERIALIZABLE too: the print claim failed, and every other read would take range
/// locks it never asked for.</summary>
public class IsolationLevelLeakTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private string _ownerId = default!;
    private string _cashierId = default!;
    private string _agentId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        Assert.StartsWith("FalahTest_", _factory.DatabaseName);
        _ownerId = await _factory.SeedUserAsync("المالك", "owner", "all");
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos");
        _agentId = await _factory.SeedUserAsync("وكيل الطباعة", "print_agent", "print_agent");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private HttpClient As(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId));
        return client;
    }

    [Fact]
    public async Task The_print_claim_still_works_after_a_permission_change()
    {
        for (var i = 0; i < 5; i++)
        {
            var grant = await As(_ownerId).PostAsJsonAsync($"/api/users/{_cashierId}/permissions",
                new UpdateUserPermissionsRequest(_cashierId, new[] { i % 2 == 0 ? "cash_count" : "delivery_manage" }, Array.Empty<string>(), _ownerId));
            grant.EnsureSuccessStatusCode();

            var claim = await As(_agentId).PostAsJsonAsync("/api/print-jobs/claim", new ClaimPrintJobsRequest("Receipt", "station-1"));
            Assert.True(claim.StatusCode == HttpStatusCode.OK, $"round {i}: {(int)claim.StatusCode} {await claim.Content.ReadAsStringAsync()}");
        }
    }

    [Fact]
    public async Task The_print_claim_still_works_after_a_user_is_deactivated()
    {
        for (var i = 0; i < 5; i++)
        {
            // Reactivating goes through choosing a new PIN (ChangePin), so each round deactivates a different user.
            var userId = await _factory.SeedUserAsync($"موظف {i}", "cashier", "pos");
            (await As(_ownerId).PostAsJsonAsync($"/api/users/{userId}/active",
                new SetUserActiveRequest(userId, false, "اختبار", _ownerId))).EnsureSuccessStatusCode();

            var claim = await As(_agentId).PostAsJsonAsync("/api/print-jobs/claim", new ClaimPrintJobsRequest("Receipt", "station-1"));
            Assert.True(claim.StatusCode == HttpStatusCode.OK, $"round {i}: {(int)claim.StatusCode} {await claim.Content.ReadAsStringAsync()}");
        }
    }
}

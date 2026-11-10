namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// SEC1. Logging in is "this PIN alone", so no two active users may share a PIN, and choosing a PIN
/// therefore answers "is this somebody else's PIN?". Any logged-in cashier could ask that through
/// their own PIN change - each answer that is not a collision merely changes their own PIN - and
/// read every colleague's PIN, the owner's included, in an afternoon. A user administrator could ask
/// it through user creation.
///
/// The answer cannot be hidden without giving up PIN-only login, so the number of questions is
/// limited per person, counted in the database (a restart does not reset it), and every collision is
/// written down against the account whose PIN it hit, so that PIN can be changed.
/// </summary>
public class PinChoiceOracleTests : IDisposable
{
    private const int Allowance = 3;
    private readonly FalahApiFactory _factory;
    private readonly HttpClient _client;

    public PinChoiceOracleTests()
    {
        _factory = new FalahApiFactory(configOverrides: Limits(Allowance));
        _client = _factory.CreateAuthenticatedClient();
    }

    public void Dispose() => _factory.Dispose();

    private static Dictionary<string, string?> Limits(int allowance) => new()
    {
        ["Security:PinChoice:PerActorLimit"] = allowance.ToString(),
        ["Security:PinChoice:WindowHours"] = "24",
        // Change-PIN also sits behind the per-address login limiter; keep it out of the way so these
        // tests see only the allowance under test.
        ["RateLimit:Login:PermitLimit"] = "1000",
    };

    private static Task<HttpResponseMessage> ChangePinAsync(HttpClient client, string userId, string? currentPin, string newPin, string actorId) =>
        client.PostAsJsonAsync($"/api/users/{userId}/change-pin", new ChangePinRequest(userId, currentPin, newPin, actorId));

    private static Task<HttpResponseMessage> CreateUserAsync(HttpClient client, string pin, string actorId) =>
        client.PostAsJsonAsync("/api/users", new CreateUserRequest($"req-{Guid.NewGuid():N}", "موظف", "cashier", pin, new[] { "pos" }, actorId));

    // Distinct, valid, and never one of the seeded users' six-digit PINs.
    private static string Candidate(int i) => (4100 + i * 7).ToString();

    [Fact]
    public async Task A_cashier_cannot_keep_testing_candidate_pins_through_their_own_pin_change()
    {
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos");
        var current = _factory.PinFor(cashierId);

        for (var i = 0; i < Allowance; i++)
        {
            var next = Candidate(i);
            var accepted = await ChangePinAsync(_client, cashierId, current, next, cashierId);
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
            current = next;
        }

        var refused = await ChangePinAsync(_client, cashierId, current, Candidate(Allowance), cashierId);
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.True(refused.Headers.RetryAfter?.Delta > TimeSpan.Zero, "a refused choice says when to try again");
        Assert.Contains("error", await refused.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_choice_that_hits_someone_elses_pin_is_counted_and_written_down_against_that_account()
    {
        var victimId = await _factory.SeedUserAsync("مدير", "manager", "all");
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos");
        var cashierPin = _factory.PinFor(cashierId);

        var collision = await ChangePinAsync(_client, cashierId, cashierPin, _factory.PinFor(victimId), cashierId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, collision.StatusCode);

        var audit = await _factory.QueryAsync(db => db.ControlAuditEvents.AsNoTracking()
            .Where(e => e.Action == "pin_collision").ToListAsync());
        var entry = Assert.Single(audit);
        Assert.Equal(victimId, entry.EntityId);
        Assert.Contains(cashierId, entry.Reason ?? "");
        Assert.DoesNotContain(_factory.PinFor(victimId), entry.Reason ?? "");

        // The collision used one of the cashier's choices.
        for (var i = 0; i < Allowance - 1; i++)
        {
            var next = Candidate(i);
            Assert.Equal(HttpStatusCode.OK, (await ChangePinAsync(_client, cashierId, cashierPin, next, cashierId)).StatusCode);
            cashierPin = next;
        }
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await ChangePinAsync(_client, cashierId, cashierPin, Candidate(Allowance), cashierId)).StatusCode);
    }

    [Fact]
    public async Task A_wrong_current_pin_uses_up_the_allowance_too()
    {
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos");
        for (var i = 0; i < Allowance; i++)
        {
            var wrong = await ChangePinAsync(_client, cashierId, "0192", Candidate(i), cashierId);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, wrong.StatusCode);
        }

        var refused = await ChangePinAsync(_client, cashierId, _factory.PinFor(cashierId), Candidate(9), cashierId);
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
    }

    [Fact]
    public async Task The_allowance_is_not_reset_by_restarting_the_server()
    {
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos");
        var current = _factory.PinFor(cashierId);
        for (var i = 0; i < Allowance; i++)
        {
            var next = Candidate(i);
            Assert.Equal(HttpStatusCode.OK, (await ChangePinAsync(_client, cashierId, current, next, cashierId)).StatusCode);
            current = next;
        }

        using var restarted = new FalahApiFactory(reuseDatabaseName: _factory.DatabaseName, configOverrides: Limits(Allowance));
        using var client = restarted.CreateAuthenticatedClient(_factory);
        var refused = await ChangePinAsync(client, cashierId, current, Candidate(Allowance), cashierId);
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
    }

    [Fact]
    public async Task A_user_administrator_cannot_keep_testing_pins_through_user_creation()
    {
        var adminId = await _factory.SeedUserAsync("مسؤول", "supervisor", "users_manage", "pos");
        for (var i = 0; i < Allowance; i++)
            Assert.Equal(HttpStatusCode.OK, (await CreateUserAsync(_client, Candidate(i), adminId)).StatusCode);

        var refused = await CreateUserAsync(_client, Candidate(Allowance), adminId);
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
    }

    [Fact]
    public async Task An_administrator_resetting_pins_is_limited_the_same_way()
    {
        var adminId = await _factory.SeedUserAsync("مسؤول", "manager", "users_manage", "pos");
        var staffId = await _factory.SeedUserAsync("موظف", "cashier", "pos");
        for (var i = 0; i < Allowance; i++)
            Assert.Equal(HttpStatusCode.OK, (await ChangePinAsync(_client, staffId, null, Candidate(i), adminId)).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await ChangePinAsync(_client, staffId, null, Candidate(Allowance), adminId)).StatusCode);
    }

    /// <summary>The owner can already reset every PIN but their own, so the question tells them
    /// nothing; limiting them would only stop a restaurant being set up in one sitting.</summary>
    [Fact]
    public async Task The_owner_setting_up_staff_is_not_limited()
    {
        var ownerId = await _factory.SeedUserAsync("المالك", "owner", "all");
        for (var i = 0; i < Allowance + 2; i++)
            Assert.Equal(HttpStatusCode.OK, (await CreateUserAsync(_client, Candidate(i), ownerId)).StatusCode);
    }

    [Fact]
    public async Task One_persons_allowance_is_theirs_alone()
    {
        var firstId = await _factory.SeedUserAsync("كاشير ١", "cashier", "pos");
        var secondId = await _factory.SeedUserAsync("كاشير ٢", "cashier", "pos");
        for (var i = 0; i < Allowance; i++)
            await ChangePinAsync(_client, firstId, "0192", Candidate(i), firstId);

        var second = await ChangePinAsync(_client, secondId, _factory.PinFor(secondId), Candidate(20), secondId);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task A_create_user_retry_with_the_same_request_id_does_not_use_up_a_choice()
    {
        var adminId = await _factory.SeedUserAsync("مسؤول", "supervisor", "users_manage", "pos");
        var request = new CreateUserRequest($"req-{Guid.NewGuid():N}", "موظف", "cashier", Candidate(0), new[] { "pos" }, adminId);
        for (var i = 0; i < Allowance + 2; i++)
            Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/users", request)).StatusCode);
    }
}

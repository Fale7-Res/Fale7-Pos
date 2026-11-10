namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Users;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// SEC2. login-by-PIN is the one endpoint anybody on the network can call, and a PIN is four to six
/// digits matched against every active account at once, so each guess has (active users) chances in
/// 10 000. It used to be limited only by ten requests a minute per address - about 14 000 guesses a
/// day from one machine, unlimited across machines, nothing recorded about where they came from, and
/// all forgotten on restart.
///
/// Wrong PINs are now counted per machine (short and daily allowances), a right PIN never resets the
/// count, a server under attack refuses machines that never logged in or that took part, and the
/// counts are rebuilt from the audit trail on restart. What must keep working is the restaurant: one
/// till being stopped never stops another, and tills that have logged in before keep working while
/// guesses arrive from elsewhere.
///
/// Every test runs its own factory with small limits and a window far longer than the test, and sets
/// the per-address request limiter out of the way so only the allowance under test is seen.
/// </summary>
public class LoginPinGuessingTests : IDisposable
{
    private readonly List<IDisposable> _owned = new();

    public void Dispose()
    {
        for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose();
    }

    private static Dictionary<string, string?> Limits(int perSource = 100, int perSourceDaily = 1000, int global = 1000, int globalDaily = 10_000) => new()
    {
        ["Security:Login:WindowSeconds"] = "900",
        ["Security:Login:PerSourceFailures"] = perSource.ToString(),
        ["Security:Login:PerSourceDailyFailures"] = perSourceDaily.ToString(),
        ["Security:Login:GlobalFailures"] = global.ToString(),
        ["Security:Login:GlobalDailyFailures"] = globalDaily.ToString(),
        ["RateLimit:Login:PermitLimit"] = "100000",
    };

    private FalahApiFactory Factory(int perSource = 100, int perSourceDaily = 1000, int global = 1000, int globalDaily = 10_000)
    {
        var factory = new FalahApiFactory(configOverrides: Limits(perSource, perSourceDaily, global, globalDaily));
        _owned.Add(factory);
        return factory;
    }

    private HttpClient From(FalahApiFactory factory, string address)
    {
        var client = factory.CreateClientFrom(address);
        _owned.Add(client);
        return client;
    }

    // Seeded PINs are six digits, so a four-digit guess can never be right by accident. 1234 and
    // 4321 are left out: they are the demo PINs a fresh database is seeded with.
    private static string WrongPin(int i) => (5000 + i).ToString();

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string pin) =>
        client.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(pin));

    private sealed record ErrorBody(string? error);

    private static async Task AssertRefusedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.True(response.Headers.RetryAfter?.Delta > TimeSpan.Zero, "a refused login must say when to try again");
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.False(string.IsNullOrWhiteSpace(body?.error), "the till shows the server's own words");
    }

    [Fact]
    public async Task One_machine_that_keeps_guessing_is_stopped_before_it_can_reach_a_right_pin()
    {
        var factory = Factory(perSource: 3);
        var userId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var attacker = From(factory, "10.0.0.66");

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(attacker, WrongPin(i))).StatusCode);

        // The right PIN, once the allowance is spent, is refused like any other guess.
        await AssertRefusedAsync(await LoginAsync(attacker, factory.PinFor(userId)));
    }

    [Fact]
    public async Task A_stopped_machine_never_stops_the_other_tills()
    {
        var factory = Factory(perSource: 3);
        var userId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var attacker = From(factory, "10.0.0.66");
        for (var i = 0; i < 4; i++) await LoginAsync(attacker, WrongPin(i));

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(From(factory, "10.0.0.21"), factory.PinFor(userId))).StatusCode);
    }

    [Fact]
    public async Task Logging_in_with_your_own_pin_between_guesses_does_not_reset_the_count()
    {
        var factory = Factory(perSource: 3);
        var insiderId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var till = From(factory, "10.0.0.30");

        await LoginAsync(till, WrongPin(0));
        await LoginAsync(till, WrongPin(1));
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(till, factory.PinFor(insiderId))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(till, WrongPin(2))).StatusCode);

        await AssertRefusedAsync(await LoginAsync(till, WrongPin(3)));
    }

    [Fact]
    public async Task Right_pins_do_not_use_up_the_allowance_for_mistakes()
    {
        var factory = Factory(perSource: 3);
        var userId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var till = From(factory, "10.0.0.31");

        for (var i = 0; i < 6; i++)
            Assert.Equal(HttpStatusCode.OK, (await LoginAsync(till, factory.PinFor(userId))).StatusCode);
        await LoginAsync(till, WrongPin(0));
        await LoginAsync(till, WrongPin(1));
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(till, factory.PinFor(userId))).StatusCode);
    }

    [Fact]
    public async Task Guesses_sent_all_at_once_cannot_outrun_the_limit()
    {
        var factory = Factory(perSource: 3);
        var attacker = From(factory, "10.0.0.66");

        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(i => LoginAsync(attacker, WrongPin(i))));

        Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized));
        Assert.Equal(17, responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests));
    }

    [Fact]
    public async Task Guesses_spread_over_many_machines_cannot_continue_from_a_new_machine()
    {
        var factory = Factory(perSource: 3, global: 5);
        var userId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var guess = 0;
        foreach (var address in new[] { "10.0.1.1", "10.0.1.2" })
        {
            var machine = From(factory, address);
            for (var i = 0; i < (address == "10.0.1.1" ? 3 : 2); i++)
                Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(machine, WrongPin(guess++))).StatusCode);
        }

        await AssertRefusedAsync(await LoginAsync(From(factory, "10.0.1.3"), factory.PinFor(userId)));
    }

    [Fact]
    public async Task Tills_that_have_logged_in_before_keep_working_while_the_server_is_under_attack()
    {
        var factory = Factory(perSource: 3, global: 5);
        var userId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var till = From(factory, "10.0.0.21");
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(till, factory.PinFor(userId))).StatusCode);

        var guess = 0;
        foreach (var address in new[] { "10.0.1.1", "10.0.1.2", "10.0.1.3" })
        {
            var machine = From(factory, address);
            for (var i = 0; i < 2; i++) await LoginAsync(machine, WrongPin(guess++));
        }

        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(till, factory.PinFor(userId))).StatusCode);
    }

    [Fact]
    public async Task A_machine_that_took_part_in_an_attack_is_refused_while_it_lasts()
    {
        var factory = Factory(perSource: 3, global: 5);
        var userId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var insider = From(factory, "10.0.0.40");
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(insider, factory.PinFor(userId))).StatusCode);

        await LoginAsync(insider, WrongPin(0));
        var guess = 1;
        foreach (var address in new[] { "10.0.1.1", "10.0.1.2" })
        {
            var machine = From(factory, address);
            for (var i = 0; i < 2; i++) await LoginAsync(machine, WrongPin(guess++));
        }

        // Recognised, but it guessed during the attack: it may not continue.
        await AssertRefusedAsync(await LoginAsync(insider, factory.PinFor(userId)));
    }

    [Fact]
    public async Task Addresses_in_one_ipv6_network_share_one_allowance()
    {
        var factory = Factory(perSource: 3);
        var userId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        for (var i = 0; i < 3; i++)
            await LoginAsync(From(factory, $"2001:db8:5:7::{i + 10:x}"), WrongPin(i));

        await AssertRefusedAsync(await LoginAsync(From(factory, "2001:db8:5:7::99"), factory.PinFor(userId)));
    }

    [Fact]
    public async Task A_restart_does_not_hand_a_guessing_machine_a_fresh_allowance()
    {
        var factory = Factory(perSource: 3);
        var userId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var attacker = From(factory, "10.0.0.66");
        for (var i = 0; i < 3; i++) await LoginAsync(attacker, WrongPin(i));

        var restarted = new FalahApiFactory(reuseDatabaseName: factory.DatabaseName, configOverrides: Limits(perSource: 3));
        _owned.Add(restarted);
        await AssertRefusedAsync(await LoginAsync(From(restarted, "10.0.0.66"), factory.PinFor(userId)));
    }

    [Fact]
    public async Task Slow_guessing_that_stays_under_the_short_window_is_stopped_by_the_daily_allowance()
    {
        var factory = Factory(perSource: 3, perSourceDaily: 5);
        var clock = new ManualClock(DateTimeOffset.UtcNow);
        var host = factory.WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<TimeProvider>(clock)));
        _owned.Add(host);
        var userId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var attacker = host.CreateClient();
        attacker.DefaultRequestHeaders.Add(FalahApiFactory.RemoteAddressHeader, "10.0.0.66");

        for (var i = 0; i < 3; i++) Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(attacker, WrongPin(i))).StatusCode);
        clock.Advance(TimeSpan.FromMinutes(16));
        for (var i = 3; i < 5; i++) Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(attacker, WrongPin(i))).StatusCode);
        clock.Advance(TimeSpan.FromMinutes(16));

        await AssertRefusedAsync(await LoginAsync(attacker, factory.PinFor(userId)));
    }

    [Fact]
    public async Task Wrong_pins_and_logins_are_written_down_with_the_machine_they_came_from()
    {
        var factory = Factory();
        var userId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var till = From(factory, "10.0.0.21");
        await LoginAsync(till, WrongPin(0));
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(till, factory.PinFor(userId))).StatusCode);

        var audit = await factory.QueryAsync(db => db.ControlAuditEvents.AsNoTracking()
            .Where(e => e.Action == "pin_login_failed" || e.Action == "pin_login_succeeded").ToListAsync());
        var failed = Assert.Single(audit, e => e.Action == "pin_login_failed");
        Assert.Equal("10.0.0.21", failed.EntityId);
        Assert.DoesNotContain(WrongPin(0), (failed.Reason ?? "") + failed.EntityId);
        var succeeded = Assert.Single(audit, e => e.Action == "pin_login_succeeded");
        Assert.Equal("10.0.0.21", succeeded.EntityId);
        Assert.Contains(userId, succeeded.Reason ?? "");
    }

    [Fact]
    public async Task A_user_administrator_can_see_and_clear_a_stopped_machine_and_a_cashier_cannot()
    {
        var factory = Factory(perSource: 3);
        var userId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var managerId = await factory.SeedUserAsync("مدير", "manager", "users_manage");
        var stopped = From(factory, "10.0.0.66");
        for (var i = 0; i < 3; i++) await LoginAsync(stopped, WrongPin(i));

        var manager = factory.CreateAuthenticatedClient();
        _owned.Add(manager);
        var sources = await manager.GetFromJsonAsync<List<LoginSourceDto>>($"/api/security/login-sources?actorUserId={managerId}");
        var listed = Assert.Single(sources!, s => s.Source == "10.0.0.66");
        Assert.Equal(3, listed.WrongPinsInWindow);
        Assert.True(listed.RefusedForSeconds > 0);

        var cashierClear = await manager.PostAsJsonAsync($"/api/security/login-sources/clear?actorUserId={userId}", new ClearLoginSourceRequest("10.0.0.66"));
        Assert.False(cashierClear.IsSuccessStatusCode);
        await AssertRefusedAsync(await LoginAsync(stopped, factory.PinFor(userId)));

        var clear = await manager.PostAsJsonAsync($"/api/security/login-sources/clear?actorUserId={managerId}", new ClearLoginSourceRequest("10.0.0.66"));
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(stopped, factory.PinFor(userId))).StatusCode);
        Assert.True(await factory.QueryAsync(db => db.ControlAuditEvents.AnyAsync(e => e.Action == "login_source_cleared" && e.EntityId == "10.0.0.66")));
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now;
        public ManualClock(DateTimeOffset start) => _now = start;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }
}

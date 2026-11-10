namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// /api/approvals/issue takes a manager's PIN from whoever holds a session - every cashier. A PIN is
/// four to six digits, so a limit on guessing is the only thing between a logged-in cashier and a
/// manager's approval (and, through login-by-pin, the manager's whole account). The endpoint's own
/// comment claimed it was rate limited like login; it was not limited at all.
///
/// Wrong guesses are counted per caller, per client machine and across the whole server, so neither
/// one session, one machine, nor many sessions on many machines can keep guessing. Each test sets its
/// own small limits on its own factory and a window far longer than the test, so nothing races the
/// window boundary.
/// </summary>
public class ApprovalPinGuessingTests : IDisposable
{
    private readonly List<FalahApiFactory> _factories = new();

    public void Dispose()
    {
        foreach (var factory in _factories) factory.Dispose();
    }

    private FalahApiFactory Factory(int perCaller = 100, int perClient = 100, int global = 100)
    {
        var factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?>
        {
            ["RateLimit:ApprovalPin:WindowSeconds"] = "900",
            ["RateLimit:ApprovalPin:PerCallerFailures"] = perCaller.ToString(),
            ["RateLimit:ApprovalPin:PerClientFailures"] = perClient.ToString(),
            ["RateLimit:ApprovalPin:GlobalFailures"] = global.ToString(),
        });
        _factories.Add(factory);
        return factory;
    }

    private static HttpClient AuthedAs(FalahApiFactory factory, string userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", factory.TokenFor(userId));
        return client;
    }

    // Seeded PINs are six digits, so a four-digit guess can never be right by accident. 1234 and
    // 4321 are left out: they are the demo PINs a fresh database is seeded with.
    private static string WrongPin(int i) => (5000 + i).ToString();

    private static Task<HttpResponseMessage> IssueAsync(HttpClient client, string pin, string action = "orders_comp") =>
        client.PostAsJsonAsync("/api/approvals/issue", new IssueManagerApprovalRequest(pin, action, "اختبار"));

    private static async Task<int> ApprovalCountAsync(FalahApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<FalahDbContext>().ManagerApprovals.CountAsync();
    }

    private static async Task AssertGuessingStoppedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.True(response.Headers.RetryAfter?.Delta > TimeSpan.Zero, "a refused guess must say when to try again");
        // The till shows the server's own words; an empty 429 would leave it guessing too.
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.False(string.IsNullOrWhiteSpace(body?.error));
    }

    private sealed record ErrorBody(string? error);

    [Fact]
    public async Task One_session_that_keeps_guessing_is_stopped_before_it_can_reach_the_right_pin()
    {
        var factory = Factory(perCaller: 3);
        var managerId = await factory.SeedUserAsync("المدير", "manager", "all");
        var cashier = AuthedAs(factory, await factory.SeedUserAsync("كاشير", "cashier", "pos"));

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.UnprocessableEntity, (await IssueAsync(cashier, WrongPin(i))).StatusCode);

        // The right PIN, once the allowance is spent, is refused like any other guess - otherwise
        // the limit would only slow the attacker down on the guesses that do not matter.
        await AssertGuessingStoppedAsync(await IssueAsync(cashier, factory.PinFor(managerId)));
        Assert.Equal(0, await ApprovalCountAsync(factory));
    }

    [Fact]
    public async Task Guessing_from_one_machine_is_stopped_even_when_it_changes_sessions()
    {
        var factory = Factory(perCaller: 3, perClient: 4);
        var managerId = await factory.SeedUserAsync("المدير", "manager", "all");
        var first = AuthedAs(factory, await factory.SeedUserAsync("كاشير ١", "cashier", "pos"));
        var second = AuthedAs(factory, await factory.SeedUserAsync("كاشير ٢", "cashier", "pos"));

        for (var i = 0; i < 3; i++) await IssueAsync(first, WrongPin(i));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await IssueAsync(second, WrongPin(3))).StatusCode);

        await AssertGuessingStoppedAsync(await IssueAsync(second, factory.PinFor(managerId)));
    }

    [Fact]
    public async Task Guesses_spread_over_many_sessions_are_stopped_across_the_whole_server()
    {
        var factory = Factory(perCaller: 3, perClient: 1000, global: 5);
        var managerId = await factory.SeedUserAsync("المدير", "manager", "all");
        var guesser = 0;
        for (var session = 0; session < 2; session++)
        {
            var cashier = AuthedAs(factory, await factory.SeedUserAsync($"كاشير {session}", "cashier", "pos"));
            for (var i = 0; i < (session == 0 ? 3 : 2); i++)
                Assert.Equal(HttpStatusCode.UnprocessableEntity, (await IssueAsync(cashier, WrongPin(guesser++))).StatusCode);
        }

        var fresh = AuthedAs(factory, await factory.SeedUserAsync("كاشير جديد", "cashier", "pos"));
        await AssertGuessingStoppedAsync(await IssueAsync(fresh, factory.PinFor(managerId)));
    }

    [Fact]
    public async Task Guesses_sent_all_at_once_cannot_outrun_the_limit()
    {
        var factory = Factory(perCaller: 3);
        var cashier = AuthedAs(factory, await factory.SeedUserAsync("كاشير", "cashier", "pos"));

        // Each guess costs a slow PIN hash per candidate. Counted only after that hash, twenty
        // simultaneous guesses would all be checked before the first failure was recorded.
        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(i => IssueAsync(cashier, WrongPin(i))));

        Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity));
        Assert.Equal(17, responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests));
    }

    [Fact]
    public async Task Approvals_that_succeed_do_not_use_up_the_allowance_for_mistakes()
    {
        var factory = Factory(perCaller: 2, perClient: 2, global: 2);
        var managerId = await factory.SeedUserAsync("المدير", "manager", "all");
        var cashier = AuthedAs(factory, await factory.SeedUserAsync("كاشير", "cashier", "pos"));

        // A busy evening: many comps and refunds, every one approved on the first try.
        for (var i = 0; i < 6; i++)
            (await IssueAsync(cashier, factory.PinFor(managerId))).EnsureSuccessStatusCode();

        // A manager's slip of the finger is still forgiven.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await IssueAsync(cashier, WrongPin(0))).StatusCode);
        (await IssueAsync(cashier, factory.PinFor(managerId))).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_pin_of_someone_who_may_not_approve_is_answered_exactly_like_a_wrong_pin()
    {
        var factory = Factory();
        var cashierId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var supervisorId = await factory.SeedUserAsync("مشرف", "supervisor", "pos");
        var caller = AuthedAs(factory, cashierId);

        var wrong = await IssueAsync(caller, WrongPin(0));
        var cashiersPin = await IssueAsync(caller, factory.PinFor(cashierId));
        var supervisorsPin = await IssueAsync(caller, factory.PinFor(supervisorId));

        // A different answer would tell the guesser the PIN is real - and a real PIN logs in.
        var expected = await wrong.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, cashiersPin.StatusCode);
        Assert.Equal(expected, await cashiersPin.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, supervisorsPin.StatusCode);
        Assert.Equal(expected, await supervisorsPin.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_request_without_an_action_says_nothing_about_the_pin()
    {
        var factory = Factory(perCaller: 1);
        var managerId = await factory.SeedUserAsync("المدير", "manager", "all");
        var cashier = AuthedAs(factory, await factory.SeedUserAsync("كاشير", "cashier", "pos"));

        var withRightPin = await IssueAsync(cashier, factory.PinFor(managerId), action: "");
        var withWrongPin = await IssueAsync(cashier, WrongPin(0), action: "");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, withRightPin.StatusCode);
        Assert.Equal(await withRightPin.Content.ReadAsStringAsync(), await withWrongPin.Content.ReadAsStringAsync());
        // No PIN was checked, so no guess was spent: the one allowed mistake is still there.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await IssueAsync(cashier, WrongPin(1))).StatusCode);
    }

    [Fact]
    public async Task A_wrong_approval_pin_is_audited_against_the_session_that_sent_it()
    {
        var factory = Factory();
        var cashierId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var guess = WrongPin(7);

        await IssueAsync(AuthedAs(factory, cashierId), guess);

        using var scope = factory.Services.CreateScope();
        var events = await scope.ServiceProvider.GetRequiredService<FalahDbContext>().ControlAuditEvents
            .Where(e => e.Action == "approval_pin_failed").ToListAsync();
        var failure = Assert.Single(events);
        // The audit trail names people the way every other entry does; the id pins down which one.
        Assert.Equal("كاشير", failure.Actor);
        Assert.Equal(cashierId, failure.EntityId);
        Assert.DoesNotContain(guess, $"{failure.EntityId}|{failure.Reason}|{failure.OperationId}|{failure.ApprovedBy}");
    }

    [Theory]
    [InlineData("owner", "all")]
    [InlineData("manager", "all")]
    [InlineData("supervisor", "orders_comp")]
    public async Task Everyone_who_may_approve_still_can(string role, string permission)
    {
        var factory = Factory();
        var approverId = await factory.SeedUserAsync("المعتمد", role, permission);
        var cashier = AuthedAs(factory, await factory.SeedUserAsync("كاشير", "cashier", "pos"));

        var response = await IssueAsync(cashier, factory.PinFor(approverId));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IssueManagerApprovalResult>();
        Assert.Equal(approverId, result!.ApproverUserId);
    }

    [Fact]
    public async Task A_deactivated_managers_pin_no_longer_approves()
    {
        var factory = Factory();
        var managerId = await factory.SeedUserAsync("مدير سابق", "manager", "all");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
            (await db.Users.SingleAsync(u => u.Id == managerId)).Active = false;
            await db.SaveChangesAsync();
        }
        var cashier = AuthedAs(factory, await factory.SeedUserAsync("كاشير", "cashier", "pos"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await IssueAsync(cashier, factory.PinFor(managerId))).StatusCode);
        Assert.Equal(0, await ApprovalCountAsync(factory));
    }

    private static HttpClient AuthedFrom(FalahApiFactory factory, string userId, string address)
    {
        var client = AuthedAs(factory, userId);
        client.DefaultRequestHeaders.Add(FalahApiFactory.RemoteAddressHeader, address);
        return client;
    }

    /// <summary>SEC-APIN-2. The server-wide allowance used to stop every approval in the restaurant
    /// once spent, so a few colluding sessions could hold up comps and refunds at every till. It now
    /// stops those who took part - their sessions and their machines - and serves everyone else.</summary>
    [Fact]
    public async Task A_till_that_took_no_part_still_gets_approvals_while_guesses_hold_up_the_server()
    {
        var factory = Factory(perCaller: 3, perClient: 100, global: 5);
        var managerId = await factory.SeedUserAsync("المدير", "manager", "all");
        var first = AuthedFrom(factory, await factory.SeedUserAsync("كاشير ١", "cashier", "pos"), "10.0.2.1");
        var second = AuthedFrom(factory, await factory.SeedUserAsync("كاشير ٢", "cashier", "pos"), "10.0.2.2");
        for (var i = 0; i < 3; i++) await IssueAsync(first, WrongPin(i));
        for (var i = 3; i < 5; i++) await IssueAsync(second, WrongPin(i));

        // Whoever guessed is refused, right PIN or not...
        await AssertGuessingStoppedAsync(await IssueAsync(second, factory.PinFor(managerId)));
        // ...and so is a fresh session on a machine that guessed...
        var sameMachine = AuthedFrom(factory, await factory.SeedUserAsync("كاشير ٤", "cashier", "pos"), "10.0.2.2");
        await AssertGuessingStoppedAsync(await IssueAsync(sameMachine, factory.PinFor(managerId)));
        // ...while a cashier at another till, who never guessed, is served.
        var bystander = AuthedFrom(factory, await factory.SeedUserAsync("كاشير ٣", "cashier", "pos"), "10.0.2.3");
        (await IssueAsync(bystander, factory.PinFor(managerId))).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Addresses_in_one_ipv6_network_count_as_one_machine()
    {
        var factory = Factory(perCaller: 100, perClient: 3);
        var managerId = await factory.SeedUserAsync("المدير", "manager", "all");
        for (var i = 0; i < 3; i++)
        {
            var cashier = AuthedFrom(factory, await factory.SeedUserAsync($"كاشير {i}", "cashier", "pos"), $"2001:db8:9:1::{i + 10:x}");
            await IssueAsync(cashier, WrongPin(i));
        }

        var another = AuthedFrom(factory, await factory.SeedUserAsync("كاشير جديد", "cashier", "pos"), "2001:db8:9:1::77");
        await AssertGuessingStoppedAsync(await IssueAsync(another, factory.PinFor(managerId)));
    }

    [Fact]
    public async Task A_restart_does_not_hand_a_guessing_session_a_fresh_allowance()
    {
        var limits = new Dictionary<string, string?>
        {
            ["RateLimit:ApprovalPin:WindowSeconds"] = "900",
            ["RateLimit:ApprovalPin:PerCallerFailures"] = "3",
        };
        var factory = new FalahApiFactory(configOverrides: limits);
        _factories.Add(factory);
        var managerId = await factory.SeedUserAsync("المدير", "manager", "all");
        var cashierId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var cashier = AuthedAs(factory, cashierId);
        for (var i = 0; i < 3; i++) await IssueAsync(cashier, WrongPin(i));

        var restarted = new FalahApiFactory(reuseDatabaseName: factory.DatabaseName, configOverrides: limits);
        _factories.Add(restarted);
        var afterRestart = restarted.CreateClient();
        afterRestart.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", factory.TokenFor(cashierId));
        await AssertGuessingStoppedAsync(await IssueAsync(afterRestart, factory.PinFor(managerId)));
    }
}

/// <summary>The window itself, on a clock the test moves - no database, no waiting.</summary>
public class ApprovalPinAttemptGuardTests
{
    private sealed class ManualClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 25, 18, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static Falah.Infrastructure.Approvals.ApprovalPinAttemptGuard Guard(ManualClock clock) =>
        new(new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RateLimit:ApprovalPin:WindowSeconds"] = "600",
            ["RateLimit:ApprovalPin:PerCallerFailures"] = "2",
        }).Build(), clock);

    private static readonly Falah.Infrastructure.Approvals.ApprovalCaller Cashier = new("user-1", "كاشير", "10.0.0.5");

    [Fact]
    public void A_spent_allowance_comes_back_as_each_wrong_pin_leaves_the_window()
    {
        var clock = new ManualClock();
        var guard = Guard(clock);
        Assert.NotNull(guard.TryBegin(Cashier, out _));
        clock.Now += TimeSpan.FromMinutes(4);
        Assert.NotNull(guard.TryBegin(Cashier, out _));

        Assert.Null(guard.TryBegin(Cashier, out var retryAfter));
        Assert.Equal(TimeSpan.FromMinutes(6), retryAfter);

        clock.Now += TimeSpan.FromMinutes(6);
        Assert.NotNull(guard.TryBegin(Cashier, out _));
        Assert.Null(guard.TryBegin(Cashier, out _));
    }

    [Fact]
    public void Defaults_apply_when_nothing_is_configured()
    {
        var guard = new Falah.Infrastructure.Approvals.ApprovalPinAttemptGuard(
            new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), new ManualClock());
        for (var i = 0; i < 5; i++) Assert.NotNull(guard.TryBegin(Cashier, out _));
        Assert.Null(guard.TryBegin(Cashier, out var retryAfter));
        Assert.Equal(TimeSpan.FromMinutes(15), retryAfter);
    }
}

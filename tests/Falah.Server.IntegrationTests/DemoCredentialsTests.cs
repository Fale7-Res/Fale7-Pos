namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// SEC4. A fresh database used to get two demo accounts in every environment - a cashier on PIN 1234
/// and a manager holding "all" on PIN 4321 - so a production install started life with a published
/// manager login, and nothing ever said so afterwards. Demo users are now development-only, a
/// production install creates its first owner from a one-time setting, and live accounts still on a
/// published PIN are reported (never changed: that is the owner's decision).
/// </summary>
public class DemoCredentialsTests : IDisposable
{
    private readonly List<IDisposable> _owned = new();
    public void Dispose()
    {
        for (var i = _owned.Count - 1; i >= 0; i--) _owned[i].Dispose();
    }

    private FalahApiFactory Factory(string? environment, Dictionary<string, string?>? config = null, string? reuse = null)
    {
        var factory = new FalahApiFactory(reuseDatabaseName: reuse, configOverrides: config, environment: environment);
        _owned.Add(factory);
        return factory;
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string pin) =>
        client.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(pin));

    [Fact]
    public async Task A_production_install_gets_no_demo_accounts()
    {
        var factory = Factory("Production");
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, "4321")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client, "1234")).StatusCode);
        Assert.Equal(0, await factory.QueryAsync(db => db.Users.CountAsync()));
        // The menu still arrives, so there is something to sell once someone can log in.
        Assert.True(await factory.QueryAsync(db => db.Products.AnyAsync()));
    }

    [Fact]
    public async Task A_production_install_creates_its_first_owner_from_the_one_time_setting()
    {
        var factory = Factory("Production", new() { ["Bootstrap:OwnerPin"] = "582914" });
        var login = await LoginAsync(factory.CreateClient(), "582914");

        login.EnsureSuccessStatusCode();
        var owner = await login.Content.ReadFromJsonAsync<LoginResultDto>();
        Assert.Equal("owner", owner!.Role);
        Assert.Contains("all", owner.Permissions);
    }

    [Fact]
    public async Task The_one_time_setting_never_adds_an_account_to_an_installation_that_has_users()
    {
        var first = Factory("Production", new() { ["Bootstrap:OwnerPin"] = "582914" });
        await first.QueryAsync(db => db.Users.CountAsync());

        var restarted = Factory("Production", new() { ["Bootstrap:OwnerPin"] = "736251" }, reuse: first.DatabaseName);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(restarted.CreateClient(), "736251")).StatusCode);
        Assert.Equal(1, await restarted.QueryAsync(db => db.Users.CountAsync()));
    }

    [Fact]
    public async Task A_weak_bootstrap_pin_is_refused_like_any_other_pin()
    {
        var factory = Factory("Production", new() { ["Bootstrap:OwnerPin"] = "1111" });

        // The server still starts (and says why in its log); it just creates nobody on that PIN.
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(factory.CreateClient(), "1111")).StatusCode);
        Assert.Equal(0, await factory.QueryAsync(db => db.Users.CountAsync()));
    }

    [Fact]
    public async Task Development_hosts_still_get_the_demo_accounts()
    {
        var factory = Factory(environment: null);
        (await LoginAsync(factory.CreateClient(), "4321")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Live_accounts_on_a_published_pin_are_reported_and_left_alone()
    {
        // A database seeded the old way, now served by a production host.
        var seededBefore = Factory(environment: null);
        await seededBefore.QueryAsync(db => db.Users.CountAsync());
        var production = Factory("Production", reuse: seededBefore.DatabaseName);
        var adminId = await production.SeedUserAsync("مسؤول", "manager", "users_manage");

        var posture = await production.CreateAuthenticatedClient()
            .GetFromJsonAsync<SecurityPostureDto>($"/api/security/posture?actorUserId={adminId}");

        Assert.Contains("مدير المحل", posture!.UsersWithPublishedPins);
        Assert.Contains("أحمد الكاشير", posture.UsersWithPublishedPins);
        Assert.DoesNotContain("مسؤول", posture.UsersWithPublishedPins);
        // Reported, not deactivated.
        (await LoginAsync(production.CreateClient(), "4321")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Only_a_user_administrator_can_read_the_security_posture()
    {
        var factory = Factory(environment: null);
        var cashierId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var response = await factory.CreateAuthenticatedClient().GetAsync($"/api/security/posture?actorUserId={cashierId}");
        Assert.False(response.IsSuccessStatusCode);
    }
}

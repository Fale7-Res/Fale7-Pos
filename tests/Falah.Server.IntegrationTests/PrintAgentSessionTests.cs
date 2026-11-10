namespace Falah.Server.IntegrationTests;

using Falah.PrintAgent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A print agent outlives its session.
///
/// The agent logged in once at startup and reused that token forever. A session ends - it expires
/// after its idle lifetime (the machine was off overnight, the server was down), someone logs it out,
/// the user is deactivated and reactivated - and from then on every poll came back 401, the agent
/// logged "poll failed" every few seconds, and no ticket reached the kitchen until somebody noticed
/// and restarted it by hand.
/// </summary>
public class PrintAgentSessionTests
{
    [Fact]
    public async Task An_agent_whose_session_has_ended_logs_in_again_and_keeps_polling()
    {
        using var factory = new FalahApiFactory();
        var agentUserId = await factory.SeedUserAsync("محطة الطباعة", "print_agent", "print_agent");
        using var agent = new PrintServerClient(factory.CreateClient());
        await agent.AuthenticateAsync(factory.PinFor(agentUserId), CancellationToken.None);
        Assert.Empty(await agent.GetPendingAsync("Receipt", CancellationToken.None));

        await EndEverySessionAsync(factory);

        // The same call, after the server has forgotten the agent's session.
        Assert.Empty(await agent.GetPendingAsync("Receipt", CancellationToken.None));
    }

    [Fact]
    public async Task An_agent_whose_pin_no_longer_works_still_fails_loudly()
    {
        // Logging in again is only a recovery from an ended session, never a way around a refusal.
        using var factory = new FalahApiFactory();
        var agentUserId = await factory.SeedUserAsync("محطة الطباعة", "print_agent", "print_agent");
        using var agent = new PrintServerClient(factory.CreateClient());
        await agent.AuthenticateAsync(factory.PinFor(agentUserId), CancellationToken.None);

        await EndEverySessionAsync(factory);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Falah.Infrastructure.Persistence.FalahDbContext>();
            var user = await db.Users.FirstAsync(u => u.Id == agentUserId);
            user.Active = false;
            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<HttpRequestException>(() => agent.GetPendingAsync("Receipt", CancellationToken.None));
    }

    private static async Task EndEverySessionAsync(FalahApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Falah.Infrastructure.Persistence.FalahDbContext>();
        db.UserSessions.RemoveRange(await db.UserSessions.ToListAsync());
        await db.SaveChangesAsync();
    }
}

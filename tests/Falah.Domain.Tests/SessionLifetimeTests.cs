using Falah.Domain.Authorization;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>SEC-0007, owner decision 2026-11-22: a session ends 16 hours after it was last used, and in
/// any case 24 hours after the login that created it, however busy it has been.</summary>
public class SessionLifetimeTests
{
    private static readonly DateTimeOffset Login = new(2026, 11, 22, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_session_in_constant_use_still_ends_24_hours_after_login()
    {
        var now = Login.AddHours(24).AddMinutes(1);
        Assert.True(SessionToken.IsExpired(Login, lastSeenUtc: now.AddMinutes(-1), nowUtc: now));
    }

    [Fact]
    public void A_session_in_use_inside_its_24_hours_stays_open()
    {
        var now = Login.AddHours(23).AddMinutes(59);
        Assert.False(SessionToken.IsExpired(Login, lastSeenUtc: now.AddMinutes(-1), nowUtc: now));
    }

    [Fact]
    public void A_session_unused_for_16_hours_ends_as_before()
    {
        var now = Login.AddHours(17);
        Assert.True(SessionToken.IsExpired(Login, lastSeenUtc: Login.AddMinutes(30), nowUtc: now));
    }
}

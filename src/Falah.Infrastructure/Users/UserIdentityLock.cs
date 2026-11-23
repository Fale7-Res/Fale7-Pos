namespace Falah.Infrastructure.Users;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>All writes that can change PIN ownership share a transaction-owned database lock.
/// A salted hash cannot be compared to a different user's salted hash without the supplied PIN.
/// Reactivation therefore happens with a fresh PIN through ChangePinHandler.</summary>
internal static class UserIdentityLock
{
    internal static Task AcquireAsync(FalahDbContext db, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("A user identity transaction is required.");
        return db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'FalahUserIdentity', @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=" + Falah.Infrastructure.Persistence.LockTimeouts.Milliseconds + "; IF @r<0 THROW 51000, 'User identity lock timeout', 1;", ct);
    }
}

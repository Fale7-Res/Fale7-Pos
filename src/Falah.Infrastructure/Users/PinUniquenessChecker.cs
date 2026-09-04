namespace Falah.Infrastructure.Users;

using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported rule: "PIN uniqueness among active users" (IMPLEMENTATION_MATRIX.md row 4's own
/// note, and the same check setEmployeePin makes in the reference app). Hashing forces this out of a
/// database unique index (two different rows holding the SAME pin now produce different PinHash
/// values by design, since each has its own random salt) and into an application-level check: verify
/// the candidate PIN against every OTHER active user's own hash. Realistic staff counts make this
/// trivially cheap - this is not a check that needs to scale past a few dozen rows.</summary>
public static class PinUniquenessChecker
{
    public static async Task AssertNotInUseAsync(FalahDbContext db, string candidatePin, string? excludeUserId, CancellationToken ct)
    {
        var activeUsers = await db.Users.AsNoTracking()
            .Where(u => u.Active && u.Id != excludeUserId)
            .Select(u => new { u.PinHash, u.PinSalt, u.PinIterations }).ToListAsync(ct);
        if (activeUsers.Any(u => PinHasher.Verify(candidatePin, u.PinHash, u.PinSalt, u.PinIterations)))
            throw new DomainException("رمز PIN مستخدم بالفعل لحساب آخر - اختر رمزًا مختلفًا.");
    }
}

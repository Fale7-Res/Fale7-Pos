namespace Falah.Infrastructure.Users;

using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported rule: "PIN uniqueness among active users" (IMPLEMENTATION_MATRIX.md row 4's own
/// note, and the same check setEmployeePin makes in the reference app). Hashing forces this out of a
/// database unique index (two different rows holding the SAME pin now produce different PinHash
/// values by design, since each has its own random salt) and into an application-level check: verify
/// the candidate PIN against every OTHER active user's own hash. Realistic staff counts make this
/// trivially cheap - this is not a check that needs to scale past a few dozen rows.
///
/// A refusal tells whoever chose the PIN that it is somebody's login, so how often anyone may ask is
/// limited by <see cref="PinChoiceGuard"/>, and every collision is written down against the account
/// it hit: that account's PIN is no longer secret and should be changed.</summary>
public static class PinUniquenessChecker
{
    public const string CollisionAuditAction = "pin_collision";

    public static async Task AssertNotInUseAsync(FalahDbContext db, string candidatePin, string? excludeUserId, UserEntity chosenBy, CancellationToken ct)
    {
        var activeUsers = await db.Users.AsNoTracking()
            .Where(u => u.Active && u.Id != excludeUserId)
            .Select(u => new { u.Id, u.PinHash, u.PinSalt, u.PinIterations }).ToListAsync(ct);
        var holder = activeUsers.FirstOrDefault(u => PinHasher.Verify(candidatePin, u.PinHash, u.PinSalt, u.PinIterations));
        if (holder is null) return;

        // Never the PIN itself - only whose PIN was hit, and by whom.
        db.ControlAuditEvents.Add(new ControlAuditEventEntity
        {
            Id = $"audit-{Guid.NewGuid():N}", Action = CollisionAuditAction, Actor = chosenBy.Name, EntityId = holder.Id,
            Reason = $"اختار {chosenBy.Name} ({chosenBy.Id}) رمزاً يطابق رمز هذا الحساب - الرمز لم يعد سرياً ويجب تغييره.",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        throw new DomainException("رمز PIN مستخدم بالفعل لحساب آخر - اختر رمزًا مختلفًا.");
    }
}

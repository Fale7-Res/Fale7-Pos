namespace Falah.Infrastructure.Authorization;

using Falah.Domain.Authorization;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Issues and resolves login sessions.
///
/// This is what makes "who is acting" a fact the server establishes rather than a claim the caller
/// makes. Every command in this system authorises against an actor; before sessions existed that
/// actor came from an <c>actorUserId</c> field in the request body, which any caller could set to
/// the manager's id after reading it from the unauthenticated user list. The PIN screen in the
/// client was the only thing in the way, and a screen guards nothing a caller can skip.
/// </summary>
public sealed class SessionService
{
    private readonly FalahDbContext _db;
    public SessionService(FalahDbContext db) => _db = db;

    public async Task<string> IssueAsync(string userId, CancellationToken ct = default)
    {
        var token = SessionToken.Issue();
        var now = DateTimeOffset.UtcNow;
        _db.UserSessions.Add(new UserSessionEntity
        {
            Id = $"sess-{Guid.NewGuid():N}",
            TokenHash = SessionToken.Hash(token),
            UserId = userId,
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
        });
        await _db.SaveChangesAsync(ct);
        // The only moment the plain token exists. It is never stored and cannot be recovered later.
        return token;
    }

    /// <summary>The user this token currently belongs to, or null. Null covers every failure the
    /// caller should be unable to tell apart: no such token, revoked, expired, or a user who has
    /// since been deactivated.</summary>
    public async Task<UserEntity?> ResolveAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var hash = SessionToken.Hash(token);
        var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.TokenHash == hash, ct);
        if (session is null || session.RevokedAtUtc is not null) return null;

        var now = DateTimeOffset.UtcNow;
        if (SessionToken.IsExpired(session.LastSeenAtUtc, now)) return null;

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == session.UserId, ct);
        // A deactivated account stops working immediately rather than waiting for its session to
        // age out - deactivating a user has to mean something the moment it happens.
        if (user is null || !user.Active) return null;

        // Keep the session alive while it is in use. Written only once the clock has actually
        // moved, so a busy till does not write a row on every request.
        if (now - session.LastSeenAtUtc > TimeSpan.FromMinutes(1))
        {
            session.LastSeenAtUtc = now;
            await _db.SaveChangesAsync(ct);
        }

        return user;
    }

    public async Task RevokeAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        var hash = SessionToken.Hash(token);
        var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.TokenHash == hash, ct);
        if (session is null || session.RevokedAtUtc is not null) return;
        session.RevokedAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}

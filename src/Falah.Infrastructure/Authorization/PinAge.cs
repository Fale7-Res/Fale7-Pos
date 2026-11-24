namespace Falah.Infrastructure.Authorization;

using System.Collections.Concurrent;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Owner decision 2026-11-24 (group A): a person's PIN is changed every 90 days (OperatingLimits:PinMaxAgeDays).
///
/// When a PIN was last set is read from the control audit trail the system already keeps - "pin_changed" (self or
/// administrator), "user_created" - so no column is added. An account with neither (made before this rule, seeded at
/// install) starts its clock at its first login under this rule, which writes a "pin_age_baseline" row: nobody is
/// locked out the day the rule is installed. A print agent's account (print_agent and nothing else) is a service
/// account whose PIN lives in a station file; it is never asked.
///
/// An expired PIN still logs in - the person needs a session to change it - but every other request of that session
/// is refused until they do (AuthenticationMiddleware). "Valid until" is remembered per user for the process, so the
/// check costs one query per user, not per request; an expired answer is never remembered, so a change takes effect
/// on the very next request.
/// </summary>
public static class PinAge
{
    public const string BaselineAction = "pin_age_baseline";
    private static readonly string[] SetActions = { "pin_changed", "user_created", BaselineAction };
    private static readonly ConcurrentDictionary<string, DateTimeOffset> ValidUntil = new();

    public static bool IsServiceAccount(UserEntity user) =>
        string.Equals(user.Permissions?.Trim(), "print_agent", StringComparison.OrdinalIgnoreCase);

    public static Task<DateTimeOffset?> LastSetAtAsync(FalahDbContext db, string userId, CancellationToken ct) =>
        db.ControlAuditEvents.AsNoTracking()
            .Where(e => e.EntityId == userId && SetActions.Contains(e.Action))
            .MaxAsync(e => (DateTimeOffset?)e.CreatedAtUtc, ct);

    /// <summary>At a successful login: starts the clock for an account that has none (the row is added to the
    /// context; the login's own save writes it) and says whether the PIN must be changed now.</summary>
    public static async Task<bool> OnLoginAsync(FalahDbContext db, UserEntity user, int maxAgeDays, DateTimeOffset nowUtc, CancellationToken ct)
    {
        if (maxAgeDays <= 0 || IsServiceAccount(user)) return false;
        var lastSet = await LastSetAtAsync(db, user.Id, ct);
        if (lastSet is null)
        {
            db.ControlAuditEvents.Add(new ControlAuditEventEntity
            {
                // Not keyed on the user: two stations logging the same person in at once must not fail a login.
                Id = $"audit-{Guid.NewGuid():N}", Action = BaselineAction, Actor = user.Name, EntityId = user.Id,
                Reason = "بداية حساب مدة الرمز", CreatedAtUtc = nowUtc,
            });
            return false;
        }
        return nowUtc - lastSet.Value > TimeSpan.FromDays(maxAgeDays);
    }

    /// <summary>For the middleware: true when this user's PIN is past its age and must be changed first.</summary>
    public static async Task<bool> IsExpiredAsync(FalahDbContext db, UserEntity user, int maxAgeDays, DateTimeOffset nowUtc, CancellationToken ct)
    {
        if (maxAgeDays <= 0 || IsServiceAccount(user)) return false;
        if (ValidUntil.TryGetValue(user.Id, out var until) && nowUtc < until) return false;
        var lastSet = await LastSetAtAsync(db, user.Id, ct);
        if (lastSet is null)
        {
            // No clock yet: it starts at this person's next PIN login. Asked again in a while, not on every request.
            ValidUntil[user.Id] = nowUtc + TimeSpan.FromMinutes(10);
            return false;
        }
        var expiresAt = lastSet.Value + TimeSpan.FromDays(maxAgeDays);
        if (nowUtc >= expiresAt) return true;
        ValidUntil[user.Id] = expiresAt;
        return false;
    }
}

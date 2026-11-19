namespace Falah.Infrastructure.Users;

using Falah.Domain.Authorization;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

/// <summary>This person has chosen PINs too often; nothing was checked or changed.</summary>
public sealed class PinChoiceLimitException : Exception
{
    public PinChoiceLimitException(TimeSpan retryAfter)
        : base($"تم إيقاف تغيير الرموز السرية لهذا المستخدم مؤقتاً بسبب كثرة المحاولات. حاول مرة أخرى بعد {Describe(retryAfter)}.")
        => RetryAfter = retryAfter;

    public TimeSpan RetryAfter { get; }

    private static string Describe(TimeSpan wait) => wait.TotalMinutes <= 90
        ? $"{Math.Max(1, (int)Math.Ceiling(wait.TotalMinutes))} دقيقة"
        : $"{(int)Math.Ceiling(wait.TotalHours)} ساعة";
}

/// <summary>
/// Limits how often one person may choose a PIN - their own, a colleague's on reset, or a new user's.
///
/// Logging in is "this PIN alone", so no two active users may share one, and choosing a PIN therefore
/// answers "is this somebody else's PIN?". That answer cannot be hidden without giving up PIN-only
/// login, so the number of questions is what is limited: any cashier could otherwise read every
/// colleague's PIN (the owner's included) through their own PIN change within hours, and a user
/// administrator could do the same through user creation.
///
/// Counted from the audit trail itself (one <see cref="AuditAction"/> row per choice, written before
/// anything is checked), so a restart does not hand out a fresh allowance. The owner is not limited:
/// they can already reset every other PIN, so the answer tells them nothing, and limiting them would
/// only stop a restaurant being set up in one sitting. Their choices are still written down.
/// </summary>
public sealed class PinChoiceGuard
{
    public const string AuditAction = "pin_choice";

    // One process serves the restaurant; serialising the count and the row that claims a place in it
    // is what stops simultaneous requests all reading the same count. PIN choices are rare.
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeProvider _time;
    private readonly int _limit;
    private readonly TimeSpan _window;

    public PinChoiceGuard(IConfiguration configuration, TimeProvider time)
    {
        _time = time;
        // At least one: a limit of zero would stop anyone ever setting a PIN.
        _limit = int.TryParse(configuration["Security:PinChoice:PerActorLimit"], out var limit) ? Math.Max(1, limit) : 10;
        _window = TimeSpan.FromHours(double.TryParse(configuration["Security:PinChoice:WindowHours"], out var hours) ? Math.Max(1, hours) : 24);
    }

    /// <summary>Claims one choice for <paramref name="actor"/>, or throws <see cref="PinChoiceLimitException"/>
    /// when their allowance is spent. Saves the claim at once: a choice refused later (wrong current
    /// PIN, a PIN already in use) stays counted, because it was still a question asked.</summary>
    public async Task ClaimAsync(FalahDbContext db, UserEntity actor, string what, CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var now = _time.GetUtcNow();
            if (!UserManagementRules.IsRole(actor.Role, UserManagementRules.OwnerRole))
            {
                var since = now - _window;
                var recent = await db.ControlAuditEvents.AsNoTracking()
                    .Where(e => e.Action == AuditAction && e.EntityId == actor.Id && e.CreatedAtUtc > since)
                    .OrderByDescending(e => e.CreatedAtUtc)
                    .Select(e => e.CreatedAtUtc)
                    .Take(_limit)
                    .ToListAsync(ct);
                // The allowance frees up when the oldest of the last `limit` choices leaves the window.
                if (recent.Count >= _limit) throw new PinChoiceLimitException(recent[_limit - 1] + _window - now);
            }

            db.ControlAuditEvents.Add(new ControlAuditEventEntity
            {
                Id = $"audit-{Guid.NewGuid():N}", Action = AuditAction, Actor = actor.Name,
                EntityId = actor.Id, Reason = what, CreatedAtUtc = now,
            });
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            _gate.Release();
        }
    }
}

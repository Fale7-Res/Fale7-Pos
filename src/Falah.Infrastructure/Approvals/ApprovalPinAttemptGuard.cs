namespace Falah.Infrastructure.Approvals;

using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

/// <summary>Who sent an approval request: the session's user, and the machine it came from.</summary>
public sealed record ApprovalCaller(string UserId, string UserName, string ClientAddress);

/// <summary>The PIN guessing allowance of the approval endpoint is spent; nothing was checked.</summary>
public sealed class ApprovalGuessingLimitException : Exception
{
    public ApprovalGuessingLimitException(TimeSpan retryAfter)
        : base($"تم إيقاف اعتماد المدير مؤقتاً بسبب تكرار إدخال رمز خاطئ. حاول مرة أخرى بعد {Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes))} دقيقة.")
        => RetryAfter = retryAfter;

    public TimeSpan RetryAfter { get; }
}

/// <summary>
/// Limits wrong PINs on /api/approvals/issue. Every cashier holds a session, and a manager's PIN is
/// four to six digits, so an unlimited endpoint lets any of them find it - and with it the manager's
/// account, through login-by-pin.
///
/// Three allowances, each a sliding window of wrong PINs: per caller (one session guessing), per
/// client address (one machine changing sessions), and for the whole server (many sessions on many
/// machines). A guess is counted when it starts, before the slow PIN hash, so simultaneous requests
/// cannot all be checked before the first failure lands; a PIN that turns out right gives its place
/// back, so a busy evening of approvals never uses up the allowance for mistakes.
///
/// Once the server-wide allowance is spent, only those who took part are refused: a session with a
/// wrong PIN in the window, or a session on a machine that sent one. A cashier at another till who
/// never guessed is still served, so a few colluding sessions cannot hold up comps and refunds at
/// every till; the guesses that remain possible are one per untouched session-and-machine. A
/// manager can always act from their own session, and every wrong PIN is audited against the
/// session that sent it.
///
/// Machines are named by <see cref="Falah.Domain.Authorization.ClientSource"/> (an IPv6 /64 is one
/// machine). Per-session and server-wide counts are read back from the audit trail after a restart;
/// per-machine counts are not (the audit row names the session, not the machine).
/// </summary>
public sealed class ApprovalPinAttemptGuard
{
    public const string FailedAuditAction = "approval_pin_failed";

    private readonly object _gate = new();
    private readonly Dictionary<string, List<DateTimeOffset>> _attempts = new();
    private readonly TimeProvider _time;
    private readonly TimeSpan _window;
    private readonly int _perCaller;
    private readonly int _perClient;
    private readonly int _global;

    public ApprovalPinAttemptGuard(IConfiguration configuration, TimeProvider time)
    {
        _time = time;
        _window = TimeSpan.FromSeconds(Setting(configuration, "WindowSeconds", 900));
        _perCaller = Setting(configuration, "PerCallerFailures", 5);
        _perClient = Setting(configuration, "PerClientFailures", 10);
        _global = Setting(configuration, "GlobalFailures", 30);
    }

    // At least one: an allowance (or window) of zero would refuse every manager, right PIN or not.
    private static int Setting(IConfiguration configuration, string key, int fallback) =>
        int.TryParse(configuration[$"RateLimit:ApprovalPin:{key}"], out var value) ? Math.Max(1, value) : fallback;

    /// <summary>Reserves one guess against every allowance, or returns null with how long until one
    /// frees up. An attempt that is never marked <see cref="Attempt.Succeeded"/> stays counted.</summary>
    public Attempt? TryBegin(ApprovalCaller caller, out TimeSpan retryAfter)
    {
        var callerKey = $"caller:{caller.UserId}";
        var clientKey = $"client:{caller.ClientAddress}";
        var keys = new[] { callerKey, clientKey, "global" };

        lock (_gate)
        {
            var now = _time.GetUtcNow();
            retryAfter = TimeSpan.Zero;
            foreach (var (key, limit) in new[] { (callerKey, _perCaller), (clientKey, _perClient) })
            {
                var stamps = Live(key, now);
                // Oldest-first, so the allowance frees up when the earliest guess leaves the window.
                if (stamps.Count >= limit) retryAfter = Max(retryAfter, stamps[stamps.Count - limit] + _window - now);
            }

            var global = Live("global", now);
            if (global.Count >= _global)
            {
                var own = Live(callerKey, now).Concat(Live(clientKey, now)).ToList();
                if (own.Count > 0)
                {
                    // Refused until this caller's own guesses leave the window or the server's
                    // allowance frees up, whichever comes first.
                    var tookPartUntil = own.Max() + _window - now;
                    var serverFrees = global[global.Count - _global] + _window - now;
                    retryAfter = Max(retryAfter, tookPartUntil < serverFrees ? tookPartUntil : serverFrees);
                }
            }
            if (retryAfter > TimeSpan.Zero) return null;

            foreach (var key in keys)
            {
                if (!_attempts.TryGetValue(key, out var stamps)) _attempts[key] = stamps = new List<DateTimeOffset>();
                stamps.Add(now);
            }
            return new Attempt(this, keys, now);
        }
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;

    private volatile bool _loaded;
    private readonly SemaphoreSlim _loadGate = new(1, 1);

    /// <summary>Reads back the wrong PINs still inside the window from the audit trail, once per
    /// process, so restarting the server does not hand a guessing session a fresh allowance.</summary>
    public async Task EnsureLoadedAsync(FalahDbContext db, CancellationToken ct)
    {
        if (_loaded) return;
        await _loadGate.WaitAsync(ct);
        try
        {
            if (_loaded) return;
            var since = _time.GetUtcNow() - _window;
            var rows = await db.ControlAuditEvents.AsNoTracking()
                .Where(e => e.Action == FailedAuditAction && e.CreatedAtUtc > since)
                .Select(e => new { e.EntityId, e.CreatedAtUtc })
                .ToListAsync(ct);
            lock (_gate)
            {
                foreach (var row in rows.OrderBy(r => r.CreatedAtUtc))
                {
                    foreach (var key in new[] { $"caller:{row.EntityId}", "global" })
                    {
                        if (!_attempts.TryGetValue(key, out var stamps)) _attempts[key] = stamps = new List<DateTimeOffset>();
                        stamps.Add(row.CreatedAtUtc);
                    }
                }
                foreach (var stamps in _attempts.Values) stamps.Sort();
            }
            _loaded = true;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private List<DateTimeOffset> Live(string key, DateTimeOffset now)
    {
        if (!_attempts.TryGetValue(key, out var stamps)) return new List<DateTimeOffset>();
        stamps.RemoveAll(s => s <= now - _window);
        if (stamps.Count == 0) _attempts.Remove(key);
        return stamps;
    }

    private void Release(string[] keys, DateTimeOffset stamp)
    {
        lock (_gate)
        {
            foreach (var key in keys)
            {
                if (!_attempts.TryGetValue(key, out var stamps)) continue;
                stamps.Remove(stamp);
                if (stamps.Count == 0) _attempts.Remove(key);
            }
        }
    }

    public sealed class Attempt
    {
        private readonly ApprovalPinAttemptGuard _guard;
        private readonly string[] _keys;
        private readonly DateTimeOffset _stamp;

        internal Attempt(ApprovalPinAttemptGuard guard, string[] keys, DateTimeOffset stamp)
        {
            _guard = guard;
            _keys = keys;
            _stamp = stamp;
        }

        /// <summary>The PIN was right: this was not a guess, so it gives its place back.</summary>
        public void Succeeded() => _guard.Release(_keys, _stamp);
    }
}

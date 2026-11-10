namespace Falah.Infrastructure.Authorization;

using Falah.Application.Users;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>A login refused before any PIN was looked at.</summary>
public sealed class LoginThrottledException : Exception
{
    public LoginThrottledException(TimeSpan retryAfter, string message) : base(message) => RetryAfter = retryAfter;
    public TimeSpan RetryAfter { get; }
}

/// <summary>
/// Limits PIN guessing on login-by-PIN, the one endpoint anyone on the network can call. A PIN is
/// four to six digits and is matched against every active account at once, so each guess has
/// (active users) chances in 10 000; a per-address request limit alone allowed ~14 000 guesses a day
/// from one machine and any number across machines.
///
/// Layers, each keyed by the machine (<see cref="Falah.Domain.Authorization.ClientSource"/>):
/// <list type="number">
/// <item>Per machine: a short allowance of wrong PINs (default 5 per 15 minutes) and a daily one (20
/// per 24 hours). A guess is counted when it starts, before the slow PIN hash, so simultaneous
/// requests cannot all be checked before the first failure lands. A right PIN gives back only its own
/// place - logging in with your own PIN between guesses resets nothing.</item>
/// <item>Server under attack (default 30 wrong PINs in 15 minutes or 100 in a day, all machines
/// together): machines that never logged in successfully (in the last 30 days), and machines that
/// themselves sent wrong PINs during the attack, are refused until it subsides. Tills that have
/// logged in before and are not guessing keep working - so guesses spread over many addresses stop,
/// and one cashier cannot stop every till.</item>
/// <item>At most a few PIN verifications run at once (default 4), so a flood of logins cannot take
/// the server's processors away from the tills.</item>
/// </list>
/// Counts are rebuilt from the audit trail when the server starts (failures and logins record the
/// machine), so a restart is not a fresh allowance. A user administrator can see the machines being
/// counted and let one log in again.
/// </summary>
public sealed class LoginAttemptGuard
{
    public const string FailedAction = "pin_login_failed";
    public const string SucceededAction = "pin_login_succeeded";
    public const string ClearedAction = "login_source_cleared";

    private static readonly TimeSpan Day = TimeSpan.FromHours(24);

    private readonly object _gate = new();
    private readonly Dictionary<string, List<DateTimeOffset>> _wrong = new();
    private readonly Dictionary<string, DateTimeOffset> _lastLogin = new();
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private volatile bool _loaded;
    private readonly SemaphoreSlim _verifications;
    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _time;
    private readonly TimeSpan _window;
    private readonly TimeSpan _recognizedFor;
    private readonly int _perSource;
    private readonly int _perSourceDaily;
    private readonly int _global;
    private readonly int _globalDaily;

    public LoginAttemptGuard(IConfiguration configuration, TimeProvider time, IServiceScopeFactory scopes)
    {
        _time = time;
        _scopes = scopes;
        _window = TimeSpan.FromSeconds(Setting(configuration, "WindowSeconds", 900));
        _perSource = Setting(configuration, "PerSourceFailures", 5);
        _perSourceDaily = Setting(configuration, "PerSourceDailyFailures", 20);
        _global = Setting(configuration, "GlobalFailures", 30);
        _globalDaily = Setting(configuration, "GlobalDailyFailures", 100);
        _recognizedFor = TimeSpan.FromDays(Setting(configuration, "RecognizedStationDays", 30));
        _verifications = new SemaphoreSlim(Setting(configuration, "MaxConcurrentVerifications", 4));
    }

    // At least one: an allowance of zero would refuse every PIN, right or not.
    private static int Setting(IConfiguration configuration, string key, int fallback) =>
        int.TryParse(configuration[$"Security:Login:{key}"], out var value) ? Math.Max(1, value) : fallback;

    /// <summary>Waits for one of the few PIN verification places, or refuses when the server is too
    /// busy verifying to answer soon.</summary>
    public async Task<IDisposable> EnterVerificationAsync(CancellationToken ct)
    {
        if (!await _verifications.WaitAsync(TimeSpan.FromSeconds(10), ct))
            throw new LoginThrottledException(TimeSpan.FromSeconds(5), "الخادم مشغول بالتحقق من رموز الدخول. حاول مرة أخرى بعد لحظات.");
        return new Release(_verifications);
    }

    /// <summary>Reserves one wrong PIN for <paramref name="source"/>, or throws
    /// <see cref="LoginThrottledException"/>. A verified wrong PIN stays counted when
    /// <see cref="Attempt.Failed"/> confirms it; disposal releases an undecided reservation.</summary>
    public async Task<Attempt> BeginAsync(string source, CancellationToken ct)
    {
        await EnsureLoadedAsync(ct);
        lock (_gate)
        {
            var now = _time.GetUtcNow();
            var wait = RefusalFor(source, now);
            if (wait > TimeSpan.Zero) throw new LoginThrottledException(wait, Refused(wait));
            Wrong(source).Add(now);
            return new Attempt(this, source, now);
        }
    }

    /// <summary>The machines with wrong PINs in the last day, for the user administrator.</summary>
    public async Task<IReadOnlyList<LoginSourceDto>> ListAsync(CancellationToken ct)
    {
        await EnsureLoadedAsync(ct);
        lock (_gate)
        {
            var now = _time.GetUtcNow();
            Prune(now);
            return _wrong
                .Select(entry => new LoginSourceDto(
                    entry.Key,
                    entry.Value.Count(t => t > now - _window),
                    entry.Value.Count,
                    IsRecognized(entry.Key, now),
                    entry.Value.Count > 0 ? entry.Value[^1] : null,
                    (int)Math.Ceiling(RefusalFor(entry.Key, now).TotalSeconds)))
                .OrderByDescending(s => s.LastWrongPinUtc)
                .ToList();
        }
    }

    /// <summary>Forgets every wrong PIN counted for <paramref name="source"/>. The caller records who
    /// did it (<see cref="ClearedAction"/>), which is also what a restart reads back.</summary>
    public async Task ClearAsync(string source, CancellationToken ct)
    {
        await EnsureLoadedAsync(ct);
        lock (_gate) _wrong.Remove(source);
    }

    private TimeSpan RefusalFor(string source, DateTimeOffset now)
    {
        Prune(now);
        var own = _wrong.TryGetValue(source, out var stamps) ? stamps : new List<DateTimeOffset>();
        var wait = Max(FreesAt(own, _perSource, _window, now), FreesAt(own, _perSourceDaily, Day, now));

        var everyone = _wrong.Values.SelectMany(s => s).OrderBy(t => t).ToList();
        var shortAttack = FreesAt(everyone, _global, _window, now);
        var dailyAttack = FreesAt(everyone, _globalDaily, Day, now);
        if (shortAttack <= TimeSpan.Zero && dailyAttack <= TimeSpan.Zero) return wait;

        var attackEnds = Max(shortAttack, dailyAttack);
        if (!IsRecognized(source, now)) return Max(wait, attackEnds);

        // A known till that guessed while the attack was on is part of it, until its own wrong PINs
        // fall out of the span that triggered (or the attack subsides, whichever is sooner).
        var tookPart = TimeSpan.Zero;
        if (shortAttack > TimeSpan.Zero && own.LastOrDefault(t => t > now - _window) is var lastShort && lastShort != default)
            tookPart = Max(tookPart, lastShort + _window - now);
        if (dailyAttack > TimeSpan.Zero && own.LastOrDefault(t => t > now - Day) is var lastDaily && lastDaily != default)
            tookPart = Max(tookPart, lastDaily + Day - now);
        return Max(wait, tookPart < attackEnds ? tookPart : attackEnds);
    }

    private bool IsRecognized(string source, DateTimeOffset now) =>
        _lastLogin.TryGetValue(source, out var last) && now - last <= _recognizedFor;

    // Ascending stamps; how long until fewer than `limit` remain inside `span`.
    private static TimeSpan FreesAt(List<DateTimeOffset> stamps, int limit, TimeSpan span, DateTimeOffset now)
    {
        var live = stamps.Where(t => t > now - span).ToList();
        return live.Count >= limit ? live[live.Count - limit] + span - now : TimeSpan.Zero;
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;

    private List<DateTimeOffset> Wrong(string source)
    {
        if (!_wrong.TryGetValue(source, out var stamps)) _wrong[source] = stamps = new List<DateTimeOffset>();
        return stamps;
    }

    private void Prune(DateTimeOffset now)
    {
        foreach (var source in _wrong.Keys.ToList())
        {
            var stamps = _wrong[source];
            stamps.RemoveAll(t => t <= now - Day);
            if (stamps.Count == 0) _wrong.Remove(source);
        }
        foreach (var source in _lastLogin.Where(e => now - e.Value > _recognizedFor).Select(e => e.Key).ToList())
            _lastLogin.Remove(source);
    }

    private void ReleaseReservation(string source, DateTimeOffset stamp, bool succeeded)
    {
        lock (_gate)
        {
            if (_wrong.TryGetValue(source, out var stamps))
            {
                stamps.Remove(stamp);
                if (stamps.Count == 0) _wrong.Remove(source);
            }
            if (succeeded) _lastLogin[source] = _time.GetUtcNow();
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_loaded) return;
        await _loadGate.WaitAsync(ct);
        try
        {
            if (_loaded) return;
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
            var now = _time.GetUtcNow();
            var since = now - Max(Day, _recognizedFor);
            var rows = await db.ControlAuditEvents.AsNoTracking()
                .Where(e => (e.Action == FailedAction || e.Action == SucceededAction || e.Action == ClearedAction) && e.CreatedAtUtc > since)
                .Select(e => new { e.Action, e.EntityId, e.CreatedAtUtc })
                .ToListAsync(ct);

            var cleared = rows.Where(r => r.Action == ClearedAction)
                .GroupBy(r => r.EntityId).ToDictionary(g => g.Key, g => g.Max(r => r.CreatedAtUtc));
            lock (_gate)
            {
                // Rows written before failures named their machine say "login"; they belong to no machine.
                foreach (var row in rows.Where(r => r.Action == FailedAction && r.EntityId != "login" && r.CreatedAtUtc > now - Day)
                             .Where(r => !cleared.TryGetValue(r.EntityId, out var at) || r.CreatedAtUtc > at)
                             .OrderBy(r => r.CreatedAtUtc))
                {
                    Wrong(row.EntityId).Add(row.CreatedAtUtc);
                }
                foreach (var login in rows.Where(r => r.Action == SucceededAction).GroupBy(r => r.EntityId))
                {
                    var last = login.Max(r => r.CreatedAtUtc);
                    if (!_lastLogin.TryGetValue(login.Key, out var known) || known < last) _lastLogin[login.Key] = last;
                }
                foreach (var source in _wrong.Keys) _wrong[source].Sort();
            }
            _loaded = true;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private static string Refused(TimeSpan wait)
    {
        var when = wait.TotalMinutes <= 90
            ? $"{Math.Max(1, (int)Math.Ceiling(wait.TotalMinutes))} دقيقة"
            : $"{(int)Math.Ceiling(wait.TotalHours)} ساعة";
        return $"تم إيقاف تسجيل الدخول من هذا الجهاز مؤقتاً بسبب تكرار إدخال رمز خاطئ. حاول مرة أخرى بعد {when}، أو اطلب من المدير إعادة السماح لهذا الجهاز.";
    }

    public sealed class Attempt : IDisposable
    {
        private readonly LoginAttemptGuard _guard;
        private readonly string _source;
        private readonly DateTimeOffset _stamp;
        private int _outcome; // 0 = undecided, 1 = wrong PIN, 2 = released

        internal Attempt(LoginAttemptGuard guard, string source, DateTimeOffset stamp)
        {
            _guard = guard;
            _source = source;
            _stamp = stamp;
        }

        /// <summary>The PIN was right: this was not a guess. Gives back this attempt's place only, and
        /// marks the machine as one that logs in.</summary>
        public void Succeeded()
        {
            if (Interlocked.CompareExchange(ref _outcome, 2, 0) == 0)
                _guard.ReleaseReservation(_source, _stamp, succeeded: true);
        }

        /// <summary>Only a verified wrong PIN retains its reserved allowance.</summary>
        public void Failed() => Interlocked.CompareExchange(ref _outcome, 1, 0);

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _outcome, 2, 0) == 0)
                _guard.ReleaseReservation(_source, _stamp, succeeded: false);
        }
    }

    private sealed class Release : IDisposable
    {
        private SemaphoreSlim? _semaphore;
        public Release(SemaphoreSlim semaphore) => _semaphore = semaphore;
        public void Dispose() => Interlocked.Exchange(ref _semaphore, null)?.Release();
    }
}

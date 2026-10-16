namespace Falah.Infrastructure.Backup;

using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Takes the backup nobody remembers to take. Until this existed the only way a backup happened was
/// an operator opening the Backup screen and pressing a button; the owner dashboard would warn that
/// the last one was days old, and warning was the whole of it.
/// <para>Deliberately NOT a precise timer that fires at 03:30. A restaurant till is switched off at
/// night, so a service that only ever acts at one instant would sleep through every scheduled run
/// and back nothing up. Instead this wakes on an interval and asks one question - "is the newest
/// good backup older than <c>Backup:MaxAgeHours</c>?" - which is true whenever the machine has been
/// off, has just been switched on, or has simply been running a long day. The schedule is a maximum
/// age, not an appointment.</para>
/// <para>Every backup it takes is verified with RESTORE VERIFYONLY straight after, because a .bak
/// nobody has ever read back is a promise rather than a backup; and old files are pruned, because a
/// backup that fills the disk it lives on takes the restaurant down by itself.</para>
/// </summary>
public sealed class ScheduledBackupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ScheduledBackupService> _log;

    public ScheduledBackupService(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<ScheduledBackupService> log)
    {
        _scopes = scopes;
        _configuration = configuration;
        _log = log;
    }

    private bool Enabled => Setting("Enabled", 1d) != 0d;
    private double MaxAgeHours => Setting("MaxAgeHours", 20d);
    private TimeSpan CheckInterval => TimeSpan.FromMinutes(Setting("CheckIntervalMinutes", 30d));
    private int KeepCount => (int)Setting("KeepCount", 60d);

    /// <summary>A grace period before the first check so a restart during service hours does not
    /// fire a backup while the database is still warming up behind it.</summary>
    private TimeSpan StartupDelay => TimeSpan.FromSeconds(Setting("StartupDelaySeconds", 90d));

    /// <summary>Read straight off IConfiguration rather than through the binder package, which this
    /// project does not otherwise need. An unparseable value falls back to the default rather than
    /// throwing on a background thread at startup, where nobody would see it.</summary>
    private double Setting(string key, double fallback)
    {
        var raw = _configuration[$"Backup:Automatic:{key}"];
        if (string.IsNullOrWhiteSpace(raw)) return fallback;
        if (bool.TryParse(raw, out var flag)) return flag ? 1d : 0d;
        return double.TryParse(raw, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : fallback;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Enabled)
        {
            _log.LogInformation("النسخ الاحتياطي التلقائي معطّل عبر الإعدادات (Backup:Automatic:Enabled=false).");
            return;
        }

        try { await Task.Delay(StartupDelay, stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            // One bad cycle - the database briefly unreachable, the backup directory unmounted -
            // must never end the loop. It would be silently fatal: the service stays "running"
            // while nothing is ever backed up again.
            try { await RunCycleAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { _log.LogError(ex, "دورة النسخ الاحتياطي التلقائي فشلت؛ ستُعاد المحاولة في الدورة التالية."); }

            try { await Task.Delay(CheckInterval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();

        var newest = await db.BackupRecords.AsNoTracking()
            .Where(b => b.Kind == nameof(BackupRecordKind.Backup)
                        && b.Status == nameof(BackupRecordStatus.Succeeded))
            .OrderByDescending(b => b.CompletedAtUtc)
            .FirstOrDefaultAsync(ct);

        var now = DateTimeOffset.UtcNow;
        if (!Falah.Domain.Backup.BackupRules.IsAutomaticBackupDue(newest?.CompletedAtUtc, now, MaxAgeHours)) return;

        _log.LogInformation("آخر نسخة احتياطية ناجحة عمرها {Hours} ساعة؛ يجري أخذ نسخة جديدة.",
            newest?.CompletedAtUtc is { } completed ? Math.Round((now - completed).TotalHours, 1).ToString() : "—");

        var create = scope.ServiceProvider.GetRequiredService<CreateBackupHandler>();
        var record = await create.RunAsync(BackupActor.System, destinationDirectory: null, ct);

        if (record.Status != nameof(BackupRecordStatus.Succeeded))
        {
            _log.LogError("فشل النسخ الاحتياطي التلقائي: {Error}", record.ErrorMessage);
            return;
        }

        var verify = scope.ServiceProvider.GetRequiredService<VerifyBackupHandler>();
        var verified = await verify.RunAsync(BackupActor.System, record.Id, ct);
        if (verified.VerificationStatus != "Valid")
            _log.LogError("النسخة الاحتياطية التلقائية أُنشئت لكن فشل التحقق منها: {Path}", record.FilePath);
        else
            _log.LogInformation("نسخة احتياطية تلقائية ناجحة ومتحقَّق منها: {Path}", record.FilePath);

        await PruneAsync(db, ct);
    }

    /// <summary>Keeps the newest <c>KeepCount</c> successful backups and deletes the files older than
    /// those. Only files this system recorded are ever touched, and the record itself is kept with
    /// its file marked gone - the history of what was backed up and when outlives the .bak, which is
    /// what an audit of "were we taking backups in September" actually needs to read.</summary>
    private async Task PruneAsync(FalahDbContext db, CancellationToken ct)
    {
        if (KeepCount <= 0) return;

        var recorded = await db.BackupRecords
            .Where(b => b.Kind == nameof(BackupRecordKind.Backup)
                        && b.Status == nameof(BackupRecordStatus.Succeeded)
                        && b.FileSizeBytes != null)
            .ToListAsync(ct);

        var prunable = Falah.Domain.Backup.BackupRules.SelectPrunableBackups(
            recorded.Select(b => new Falah.Domain.Backup.BackupRules.RetainedBackup(
                b.Id, b.CompletedAtUtc ?? b.RequestedAtUtc, b.VerificationStatus == "Valid")),
            KeepCount).ToHashSet();

        if (prunable.Count == 0) return;

        foreach (var record in recorded.Where(b => prunable.Contains(b.Id)))
        {
            try
            {
                if (File.Exists(record.FilePath)) File.Delete(record.FilePath);
                // The row stays, with its file marked gone: the history of what was backed up and
                // when outlives the .bak, which is what an audit of "were we taking backups in
                // September" actually needs to read.
                record.FileSizeBytes = null;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "تعذر حذف نسخة احتياطية قديمة: {Path}", record.FilePath);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}

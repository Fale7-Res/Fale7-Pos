namespace Falah.Domain.Backup;

using Falah.Domain.Common;

/// <summary>
/// A real gap found during the legacy-parity audit: the reference app's own backup feature
/// (lib/backup-domain.mjs - a versioned JSON envelope with an FNV-1a32 checksum, exported/imported
/// through the browser) is fundamentally a browser-localStorage-era mechanism that does not
/// translate to this port's real SQL Server architecture - per direct instruction, this is designed
/// for the CURRENT architecture (native SQL Server BACKUP DATABASE/RESTORE DATABASE) rather than
/// blindly copied. What DOES carry over from the reference app's own design intent: a real backup
/// history record, integrity verification before anyone trusts a backup file, and a restore path
/// gated far more strictly than any other command in this system (a restore is the one action that
/// can discard every transaction since the backup was taken).
/// </summary>
public static class BackupRules
{
    /// <summary>A restore's confirmation phrase is a deliberate, fixed literal a caller must echo
    /// back verbatim - not a UI checkbox, so an automated or careless caller cannot trigger a
    /// database-wide restore by accident. This is new, evidence-independent hardening: the
    /// reference app's own restore path only ever asked for a manager PIN, appropriate for its
    /// low-blast-radius local-JSON-replace; a SQL Server RESTORE DATABASE discards every committed
    /// transaction since the backup, a strictly larger blast radius that warrants a strictly
    /// stronger gate.</summary>
    public const string RequiredRestoreConfirmationPhrase = "RESTORE-CONFIRMED";

    public static void AssertDestinationDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new DomainException("مسار حفظ النسخة الاحتياطية مطلوب.");
        if (directory.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
            throw new DomainException("مسار حفظ النسخة الاحتياطية غير صالح.");
    }

    public static void AssertRestoreReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("سبب استعادة النسخة الاحتياطية مطلوب.");
    }

    public static void AssertRestoreConfirmationPhrase(string? providedPhrase)
    {
        if (!string.Equals(providedPhrase, RequiredRestoreConfirmationPhrase, StringComparison.Ordinal))
            throw new DomainException($"يجب كتابة عبارة التأكيد بالضبط: {RequiredRestoreConfirmationPhrase}");
    }

    /// <summary>Ported from the spirit of the reference app's own restore precondition (it required
    /// no unsynced local writes before importing) - the SQL Server-native equivalent is refusing a
    /// restore while any financial operation is mid-flight (a journal entry started but not yet
    /// completed/discarded), the same "started, not committed" state EfFinancialJournal already
    /// tracks for every money-moving command in this system.</summary>
    public static void AssertNoActiveOperations(int pendingJournalEntryCount)
    {
        if (pendingJournalEntryCount > 0)
            throw new DomainException($"لا يمكن استعادة النسخة الاحتياطية أثناء وجود {pendingJournalEntryCount} عملية مالية قيد التنفيذ - انتظر اكتمالها أو راجعها أولًا.");
    }

    /// <summary>BACKUP DATABASE/RESTORE DATABASE/ALTER DATABASE all require the database name as a
    /// literal bracketed identifier - SQL Server has no way to parameterize an object name the way
    /// it can a value, so this string is always interpolated directly into the statement text. It is
    /// never attacker-controlled (it comes from this app's own configured connection string, not any
    /// request), but this check closes the gap properly rather than only suppressing the analyzer's
    /// EF1002 warning - a name that isn't a plain identifier is rejected before it ever reaches SQL.</summary>
    public static void AssertSafeDatabaseIdentifier(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName) || !System.Text.RegularExpressions.Regex.IsMatch(databaseName, @"^[A-Za-z0-9_]+$"))
            throw new DomainException("اسم قاعدة البيانات غير صالح لعملية النسخ الاحتياطي/الاستعادة.");
    }

    /// <summary>Whether a new automatic backup is owed. Expressed as a MAXIMUM AGE rather than a
    /// time of day on purpose: a restaurant till is switched off overnight, so a rule that fires at
    /// 03:30 would sleep through every run it was supposed to make. An age question is answerable
    /// whenever the machine happens to be on, which is the only time a backup can be taken at all.
    /// <para>No backup ever taken is always due - that is the state a fresh install is in, and it
    /// is the one case where waiting is least defensible.</para></summary>
    public static bool IsAutomaticBackupDue(DateTimeOffset? lastSucceededAtUtc, DateTimeOffset nowUtc, double maxAgeHours)
    {
        if (maxAgeHours <= 0) throw new DomainException("الحد الأقصى لعمر النسخة الاحتياطية يجب أن يكون أكبر من صفر.");
        if (lastSucceededAtUtc is not { } last) return true;
        return nowUtc - last >= TimeSpan.FromHours(maxAgeHours);
    }

    /// <summary>How many of the recorded backups to delete, keeping the newest <paramref name="keepCount"/>.
    /// Automatic backups accumulate without anyone deciding to keep them, and a disk filled by its
    /// own backups takes the restaurant down exactly the way losing the database would - so the
    /// retention is part of automating them, not a separate nicety.
    /// <para>A keepCount of zero or less disables pruning entirely rather than deleting everything:
    /// the destructive reading of an unset number must never be the one that runs.</para></summary>
    public static int PrunableBackupCount(int recordedBackupCount, int keepCount)
    {
        if (keepCount <= 0) return 0;
        return Math.Max(0, recordedBackupCount - keepCount);
    }

    /// <summary>One backup in the retention list.</summary>
    /// <param name="IsVerifiedValid">RESTORE VERIFYONLY has actually read this file back.</param>
    public readonly record struct RetainedBackup(string Id, DateTimeOffset CompletedAtUtc, bool IsVerifiedValid);

    /// <summary>Which backups may be deleted, given how many to keep. Two rules, in this order:
    /// keep the newest <paramref name="keepCount"/>, and never delete the newest VERIFIED-VALID one
    /// whatever its age.
    /// <para>The second rule is the one that matters. Retention counts files, not trustworthy files
    /// - so a run of failed verifications at the top of the list could otherwise push the last
    /// backup anybody has actually read back off the end, and delete it. A system holding sixty
    /// unverifiable files and no good one is in a worse position than one holding a single old
    /// backup that is known to restore.</para></summary>
    public static IReadOnlyList<string> SelectPrunableBackups(IEnumerable<RetainedBackup> backups, int keepCount)
    {
        if (keepCount <= 0) return Array.Empty<string>();

        var ordered = backups.OrderByDescending(b => b.CompletedAtUtc).ToList();
        var newestValidId = ordered.FirstOrDefault(b => b.IsVerifiedValid).Id;

        return ordered.Skip(keepCount)
            .Where(b => b.Id != newestValidId)
            .Select(b => b.Id)
            .ToList();
    }
}

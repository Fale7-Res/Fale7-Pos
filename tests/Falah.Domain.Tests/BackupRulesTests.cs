using Falah.Domain.Backup;
using Falah.Domain.Common;
using Xunit;

namespace Falah.Domain.Tests;

public class BackupRulesTests
{
    [Fact]
    public void AssertDestinationDirectory_rejects_an_empty_path()
    {
        Assert.Throws<DomainException>(() => BackupRules.AssertDestinationDirectory(""));
        Assert.Throws<DomainException>(() => BackupRules.AssertDestinationDirectory(null));
    }

    [Fact]
    public void AssertDestinationDirectory_accepts_a_normal_path()
    {
        BackupRules.AssertDestinationDirectory(@"E:\falah-restaurant\Backups");
    }

    [Fact]
    public void AssertRestoreReason_requires_a_non_empty_reason()
    {
        Assert.Throws<DomainException>(() => BackupRules.AssertRestoreReason(""));
        Assert.Throws<DomainException>(() => BackupRules.AssertRestoreReason("   "));
        Assert.Throws<DomainException>(() => BackupRules.AssertRestoreReason(null));
    }

    [Fact]
    public void AssertRestoreConfirmationPhrase_rejects_anything_but_the_exact_literal()
    {
        Assert.Throws<DomainException>(() => BackupRules.AssertRestoreConfirmationPhrase("restore-confirmed"));
        Assert.Throws<DomainException>(() => BackupRules.AssertRestoreConfirmationPhrase("RESTORE"));
        Assert.Throws<DomainException>(() => BackupRules.AssertRestoreConfirmationPhrase(""));
        Assert.Throws<DomainException>(() => BackupRules.AssertRestoreConfirmationPhrase(null));
    }

    [Fact]
    public void AssertRestoreConfirmationPhrase_accepts_the_exact_literal()
    {
        BackupRules.AssertRestoreConfirmationPhrase(BackupRules.RequiredRestoreConfirmationPhrase);
    }

    [Fact]
    public void AssertNoActiveOperations_blocks_when_anything_is_pending()
    {
        Assert.Throws<DomainException>(() => BackupRules.AssertNoActiveOperations(1));
        Assert.Throws<DomainException>(() => BackupRules.AssertNoActiveOperations(5));
    }

    [Fact]
    public void AssertNoActiveOperations_allows_a_fully_settled_system()
    {
        BackupRules.AssertNoActiveOperations(0);
    }

    // ---- Automatic backup scheduling ----

    private static readonly DateTimeOffset Now = new(2026, 9, 12, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_system_that_has_never_backed_up_is_always_due()
    {
        // The state a fresh install sits in, and the one where waiting is least defensible.
        Assert.True(BackupRules.IsAutomaticBackupDue(null, Now, maxAgeHours: 20));
    }

    [Fact]
    public void A_backup_younger_than_the_limit_is_not_due_and_one_older_is()
    {
        Assert.False(BackupRules.IsAutomaticBackupDue(Now.AddHours(-19), Now, maxAgeHours: 20));
        Assert.True(BackupRules.IsAutomaticBackupDue(Now.AddHours(-21), Now, maxAgeHours: 20));
    }

    [Fact]
    public void A_backup_exactly_at_the_limit_is_due()
    {
        // Due at the boundary rather than one interval later: the check only runs every half hour,
        // so rounding the other way would stretch every limit by that interval in practice.
        Assert.True(BackupRules.IsAutomaticBackupDue(Now.AddHours(-20), Now, maxAgeHours: 20));
    }

    [Fact]
    public void A_till_switched_off_for_days_is_due_the_moment_it_comes_back()
    {
        // The whole reason this is an age question and not an alarm clock.
        Assert.True(BackupRules.IsAutomaticBackupDue(Now.AddDays(-4), Now, maxAgeHours: 20));
    }

    [Fact]
    public void A_nonsense_age_limit_is_refused_rather_than_backing_up_continuously()
    {
        Assert.Throws<DomainException>(() => BackupRules.IsAutomaticBackupDue(Now, Now, maxAgeHours: 0));
        Assert.Throws<DomainException>(() => BackupRules.IsAutomaticBackupDue(Now, Now, maxAgeHours: -3));
    }

    [Fact]
    public void Retention_keeps_the_newest_and_prunes_only_the_excess()
    {
        Assert.Equal(0, BackupRules.PrunableBackupCount(recordedBackupCount: 60, keepCount: 60));
        Assert.Equal(0, BackupRules.PrunableBackupCount(recordedBackupCount: 12, keepCount: 60));
        Assert.Equal(5, BackupRules.PrunableBackupCount(recordedBackupCount: 65, keepCount: 60));
    }

    [Fact]
    public void An_unset_retention_deletes_nothing_rather_than_everything()
    {
        // Zero read as "keep none" would wipe the entire backup history on the first run, which is
        // the exact catastrophe automatic backups exist to prevent.
        Assert.Equal(0, BackupRules.PrunableBackupCount(recordedBackupCount: 500, keepCount: 0));
        Assert.Equal(0, BackupRules.PrunableBackupCount(recordedBackupCount: 500, keepCount: -1));
    }

    private static BackupRules.RetainedBackup B(string id, int daysOld, bool valid) =>
        new(id, Now.AddDays(-daysOld), valid);

    [Fact]
    public void Retention_deletes_only_what_falls_past_the_keep_count()
    {
        var backups = new[] { B("a", 0, true), B("b", 1, true), B("c", 2, true), B("d", 3, true) };
        Assert.Equal(new[] { "c", "d" }, BackupRules.SelectPrunableBackups(backups, keepCount: 2));
    }

    [Fact]
    public void The_last_verified_backup_is_never_deleted_however_old_it_is()
    {
        // Retention counts files, not trustworthy ones. A run of failed verifications at the top
        // would otherwise push the only backup anybody has actually read back off the end.
        var backups = new[]
        {
            B("newest-unverified", 0, false),
            B("also-unverified", 1, false),
            B("old-but-good", 9, true),
        };

        var prunable = BackupRules.SelectPrunableBackups(backups, keepCount: 1);

        Assert.DoesNotContain("old-but-good", prunable);
        Assert.Contains("also-unverified", prunable);
    }

    [Fact]
    public void With_a_fresh_verified_backup_older_verified_ones_may_still_go()
    {
        // Only the NEWEST valid one is protected - the rule keeps a good backup, not every good one.
        var backups = new[] { B("fresh-good", 0, true), B("older-good", 5, true), B("oldest-good", 9, true) };
        var prunable = BackupRules.SelectPrunableBackups(backups, keepCount: 1);

        Assert.DoesNotContain("fresh-good", prunable);
        Assert.Equal(new[] { "older-good", "oldest-good" }, prunable);
    }

    [Fact]
    public void An_unset_retention_selects_nothing_for_deletion()
    {
        var backups = new[] { B("a", 0, true), B("b", 1, true) };
        Assert.Empty(BackupRules.SelectPrunableBackups(backups, keepCount: 0));
    }
}
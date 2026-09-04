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
}

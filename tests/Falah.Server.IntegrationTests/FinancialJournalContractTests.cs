namespace Falah.Server.IntegrationTests;
using Falah.Domain.Idempotency;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
public class FinancialJournalContractTests
{
    [Fact]
    public async Task Journal_find_reads_local_committed_and_pending_deleted_state_through_its_interface()
    {
        using var factory = new FalahApiFactory();
        await factory.QueryAsync(async db => {
            IFinancialJournal journal = new EfFinancialJournal(db);
            Assert.Null(journal.Find("missing"));
            Assert.True(journal.TryStart("journal-contract", "sale"));
            var local = journal.Find("journal-contract"); Assert.NotNull(local); Assert.Equal(JournalStatus.Pending, local.Status);
            await db.SaveChangesAsync(); journal.Complete("journal-contract"); await db.SaveChangesAsync();
            db.ChangeTracker.Clear(); var saved = journal.Find("journal-contract");
            Assert.NotNull(saved); Assert.Equal(JournalStatus.Complete, saved.Status); Assert.Equal(local.CreatedAtUtc, saved.CreatedAtUtc); Assert.Equal("sale", saved.Kind);
            Assert.True(journal.TryStart("discard-contract", "sale")); await db.SaveChangesAsync();
            Assert.True(journal.DiscardPending("discard-contract")); Assert.Null(journal.Find("discard-contract"));
            await db.SaveChangesAsync(); db.ChangeTracker.Clear(); Assert.Null(journal.Find("discard-contract"));
            return true;
        });
    }
}

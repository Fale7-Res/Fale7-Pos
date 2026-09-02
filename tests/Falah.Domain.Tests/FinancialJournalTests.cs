using Falah.Domain.Common;
using Falah.Domain.Idempotency;
using Xunit;

namespace Falah.Domain.Tests;

public class FinancialJournalTests
{
    // These reproduce the exact attacks the final release audit ran against the current web
    // app: a same-task double click and a network retry, both reusing the same idempotency key.

    [Fact]
    public void Same_task_double_click_posts_exactly_once()
    {
        var journal = new InMemoryFinancialJournal();
        const string key = "checkout-order-abc123";

        var firstClickStarted = journal.TryStart(key, "checkout");
        var secondClickStarted = journal.TryStart(key, "checkout"); // same task, key not rotated

        Assert.True(firstClickStarted);
        Assert.False(secondClickStarted, "a second start with the same key must be rejected, not posted again");
    }

    [Fact]
    public void Network_retry_of_the_same_request_is_also_rejected()
    {
        var journal = new InMemoryFinancialJournal();
        const string key = "expense-req-xyz";

        Assert.True(journal.TryStart(key, "expense"));
        journal.Complete(key);

        // The client never saw the success response and retries the exact same request.
        var retryStarted = journal.TryStart(key, "expense");
        Assert.False(retryStarted);
        Assert.Equal(JournalStatus.Complete, journal.Find(key)!.Status);
    }

    [Fact]
    public void Deliberate_separate_operations_use_different_keys_and_both_post()
    {
        var journal = new InMemoryFinancialJournal();

        // Two genuinely separate expenses the cashier enters back-to-back get their own keys
        // (minted per form-opening in the application layer), so both must succeed.
        Assert.True(journal.TryStart("expense-1", "expense"));
        Assert.True(journal.TryStart("expense-2", "expense"));
    }

    [Fact]
    public void Completing_an_unknown_key_throws_instead_of_silently_succeeding()
    {
        var journal = new InMemoryFinancialJournal();
        Assert.Throws<DomainException>(() => journal.Complete("never-started"));
    }

    [Fact]
    public void Completing_an_already_complete_key_throws()
    {
        var journal = new InMemoryFinancialJournal();
        journal.TryStart("k1", "expense");
        journal.Complete("k1");
        Assert.Throws<DomainException>(() => journal.Complete("k1"));
    }

    [Fact]
    public void Discarding_a_pending_entry_after_a_rejected_command_allows_a_clean_retry()
    {
        var journal = new InMemoryFinancialJournal();
        const string key = "transfer-req-1";

        journal.TryStart(key, "transfer");
        // The underlying command threw (e.g. insufficient balance) before any money moved.
        var discarded = journal.DiscardPending(key);

        Assert.True(discarded);
        Assert.Null(journal.Find(key));
        Assert.True(journal.TryStart(key, "transfer"), "same key must be reusable once no financial effect was ever applied");
    }

    [Fact]
    public void A_completed_entry_can_never_be_discarded()
    {
        var journal = new InMemoryFinancialJournal();
        const string key = "posted-1";
        journal.TryStart(key, "expense");
        journal.Complete(key);

        var discarded = journal.DiscardPending(key);

        Assert.False(discarded, "a posted movement must survive - it is a real historical record");
        Assert.Equal(JournalStatus.Complete, journal.Find(key)!.Status);
    }
}

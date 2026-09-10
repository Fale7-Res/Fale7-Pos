namespace Falah.Infrastructure.Treasury;

using Microsoft.Data.SqlClient;

/// <summary>
/// Losing the race for the balance lock.
///
/// Every money-moving command reads its account's movements under a range lock, inside its own
/// transaction, immediately before spending - that is what stops two concurrent commands from each
/// reading the same balance, each deciding it covers them, and each writing.
///
/// The cost of that lock is contention. When several commands reach for the same account at once,
/// SQL Server resolves it by choosing a deadlock victim (error 1205) or letting a wait expire
/// (1222), and it raises that on the SELECT - not on SaveChanges. So it arrived as a bare
/// SqlException, sailed past catch filters written for DbUpdateException, and left the caller with
/// an HTTP 500 for what is an ordinary concurrency outcome: three "post exactly once" tests failed
/// intermittently under load for exactly this reason.
///
/// A command that loses this race has changed nothing - its transaction rolled back - so the
/// handler treats it the same way it treats any other conflict: look for the movement its request
/// id would have produced, return that as a replay if a sibling request already posted it, and
/// otherwise tell the caller plainly that nothing was written and the request can be sent again.
/// </summary>
internal static class BalanceLockContention
{
    private const int DeadlockVictim = 1205;
    private const int LockRequestTimeout = 1222;

    public static bool IsContention(Exception ex) =>
        Sql(ex) is { Number: DeadlockVictim or LockRequestTimeout };

    private static SqlException? Sql(Exception? ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
            if (current is SqlException sql) return sql;
        return null;
    }

    /// <summary>Looks again for what this request id should have produced.
    ///
    /// A command that lost the lock race is, by definition, one of several requests carrying the
    /// SAME id: a sibling is going to post it, and may simply not have committed yet at the instant
    /// this one rolled back. Looking once and giving up turns "somebody else is doing it" into an
    /// error the caller cannot act on - which is what made three "post exactly once" tests fail
    /// under load. Bounded and short: if no sibling appears in about a second, none is coming.</summary>
    public static async Task<T?> AwaitSiblingAsync<T>(Func<Task<T?>> lookup, CancellationToken ct) where T : class
    {
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            await Task.Delay(40 * attempt, ct);
            if (await lookup() is { } found) return found;
        }
        return null;
    }

    /// <summary>What a caller is told when the race was lost and no sibling had posted either.
    /// Deliberately different from the "needs manual review" message a genuine key collision gets:
    /// nothing was written, and sending the same request again is the right thing to do.</summary>
    public const string RetryMessage =
        "لم تُنفَّذ العملية بسبب ازدحام على نفس الحساب في نفس اللحظة، ولم يتغيّر أي رصيد. أعد المحاولة.";
}

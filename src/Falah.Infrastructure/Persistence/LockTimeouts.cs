namespace Falah.Infrastructure.Persistence;

/// <summary>
/// How long a command waits for one of the application locks on the sales path: financial admission,
/// stock posting and an account's spending lock.
///
/// API-0004 (audit 2026-11-18). These waited 15 s while the tills give up on a request after 10 s
/// (ApiClient). A checkout queued behind a long stock count or a close went on to commit after the till
/// had already told the cashier it could not confirm the sale; the request id kept it from being charged
/// twice, but at a rush the cashier saw a run of false failures. Waiting less than the till does means
/// the server always answers first: either done, or an Arabic "busy, send it again" that changed nothing.
/// </summary>
internal static class LockTimeouts
{
    internal const int Milliseconds = 5000;
}

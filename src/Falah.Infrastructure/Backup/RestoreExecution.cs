namespace Falah.Infrastructure.Backup;

/// <summary>Records the irreversible replacement independently of later connectivity recovery.</summary>
public static class RestoreExecution
{
    public sealed record Outcome(bool DatabaseWasReplaced, string? Error);
    public static async Task<Outcome> RunAsync(Func<Action, Task> execute, Func<Task> recover)
    {
        var replaced = false;
        try
        {
            await execute(() => replaced = true);
            if (!replaced) throw new InvalidOperationException("Restore returned without confirming database replacement.");
            return new(replaced, null);
        }
        catch (Exception ex)
        {
            var error = ex.Message;
            try { await recover(); }
            catch (Exception recovery) { error += " | Recovery: " + recovery.Message; }
            return new(replaced, error);
        }
    }
}
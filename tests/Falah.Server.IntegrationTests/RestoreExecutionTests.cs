namespace Falah.Server.IntegrationTests;
using Falah.Infrastructure.Backup;
using Xunit;

public class RestoreExecutionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_post_restore_failure_preserves_replacement_even_if_recovery_also_fails(bool recoveryFails)
    {
        var recovered = 0;
        var result = await RestoreExecution.RunAsync(mark => {
            mark(); throw new InvalidOperationException("SET MULTI_USER failed");
        }, () => {
            recovered++;
            if (recoveryFails) throw new InvalidOperationException("release failed");
            return Task.CompletedTask;
        });
        Assert.True(result.DatabaseWasReplaced);
        Assert.Equal(1, recovered);
        Assert.Contains("SET MULTI_USER failed", result.Error);
        if (recoveryFails) Assert.Contains("release failed", result.Error);
    }
    [Fact]
    public async Task A_failure_before_replacement_remains_a_failed_restore()
    {
        var result = await RestoreExecution.RunAsync(_ => throw new InvalidOperationException("RESTORE failed"), () => Task.CompletedTask);
        Assert.False(result.DatabaseWasReplaced); Assert.Equal("RESTORE failed", result.Error);
    }
    [Fact]
    public async Task Success_needs_no_recovery()
    {
        var result = await RestoreExecution.RunAsync(mark => { mark(); return Task.CompletedTask; }, () => throw new Exception("must not recover"));
        Assert.True(result.DatabaseWasReplaced); Assert.Null(result.Error);
    }

    [Fact]
    public async Task An_unconfirmed_executor_cannot_report_success()
    {
        var result = await RestoreExecution.RunAsync(_ => Task.CompletedTask, () => Task.CompletedTask);
        Assert.False(result.DatabaseWasReplaced); Assert.NotNull(result.Error);
    }

}
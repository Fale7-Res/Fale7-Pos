namespace Falah.Server.IntegrationTests;
using Falah.PrintAgent;
public class AgentLogTests
{
    [Fact]
    public void Log_files_roll_by_size_and_retain_a_bounded_history()
    {
        var dir = Path.Combine(Path.GetTempPath(), "falah-log-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var log = new AgentLog(dir, "Receipt", maxBytes: 50, keepFiles: 2, console: _ => { });
            for (var i = 0; i < 6; i++) log.Write("A log message long enough to roll the prior record: " + i);
            var path = Path.Combine(dir, "print-agent-Receipt.log");
            Assert.Contains("record: 5", File.ReadAllText(path));
            Assert.True(File.Exists(path + ".1")); Assert.True(File.Exists(path + ".2"));
            Assert.Equal(3, Directory.GetFiles(dir).Length);
        }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }
    [Fact]
    public void An_unwritable_log_falls_back_and_warns_only_once()
    {
        var blocker = Path.Combine(Path.GetTempPath(), "falah-log-blocker-" + Guid.NewGuid().ToString("N"));
        File.WriteAllText(blocker, "not a directory");
        try
        {
            var messages = new List<string>();
            var log = new AgentLog(blocker, "Receipt", console: messages.Add);
            log.Write("first"); log.Write("second");
            Assert.Contains("first", messages); Assert.Contains("second", messages);
            Assert.Single(messages.Where(m => m.Contains("file log", StringComparison.OrdinalIgnoreCase)));
        }
        finally { File.Delete(blocker); }
    }
}

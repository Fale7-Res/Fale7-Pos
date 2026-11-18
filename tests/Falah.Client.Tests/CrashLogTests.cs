namespace Falah.Client.Tests;

using Falah.Client.Diagnostics;
using Xunit;

public class CrashLogTests
{
    [Fact]
    public void A_crash_is_written_with_its_station_source_type_and_message()
    {
        var directory = Path.Combine(Path.GetTempPath(), "falah-crash-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = CrashLog.Write(directory, "cashier", new InvalidOperationException("انقطع شيء"), "UI");

            var text = File.ReadAllText(Assert.IsType<string>(path));
            Assert.Contains("cashier UI", text);
            Assert.Contains("InvalidOperationException", text);
            Assert.Contains("انقطع شيء", text);
        }
        finally { try { Directory.Delete(directory, true); } catch { } }
    }

    [Fact]
    public void A_log_that_cannot_be_written_does_not_throw()
    {
        var file = Path.GetTempFileName(); // a file where a directory is expected
        try
        {
            Assert.Null(CrashLog.Write(file, "cashier", new Exception("x"), "UI"));
        }
        finally { File.Delete(file); }
    }
}

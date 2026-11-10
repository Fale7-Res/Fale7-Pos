namespace Falah.Server.IntegrationTests;

using Falah.PrintAgent;

/// <summary>
/// The print agent's PIN does not have to live in a file under version control.
///
/// The agent read its PIN only from appsettings.json, which is tracked by git - so the PIN of
/// whichever user the agent logs in as sat in the repository in plain text. The PIN can now come
/// from the FALAH_PRINT_AGENT_PIN environment variable (set per machine, or on the service), which
/// wins over the file, so the file can carry an empty value.
/// </summary>
public class AgentOptionsSecretTests
{
    private const string PinVariable = "FALAH_PRINT_AGENT_PIN";

    [Fact]
    public void The_pin_comes_from_the_environment_when_it_is_set_there()
    {
        var path = WriteConfig("""{ "ReportedByUserId": "user-print", "Pin": "" }""");
        var previous = Environment.GetEnvironmentVariable(PinVariable);
        try
        {
            Environment.SetEnvironmentVariable(PinVariable, "730914");
            var options = AgentOptions.Load(path);

            Assert.Equal("730914", options.Pin);
            Assert.DoesNotContain(options.Validate(), p => p.Contains("Pin", StringComparison.Ordinal));
        }
        finally
        {
            Environment.SetEnvironmentVariable(PinVariable, previous);
            File.Delete(path);
        }
    }

    [Fact]
    public void Without_the_variable_the_file_value_is_used_as_before()
    {
        var path = WriteConfig("""{ "ReportedByUserId": "user-print", "Pin": "4321" }""");
        var previous = Environment.GetEnvironmentVariable(PinVariable);
        try
        {
            Environment.SetEnvironmentVariable(PinVariable, null);
            Assert.Equal("4321", AgentOptions.Load(path).Pin);
        }
        finally
        {
            Environment.SetEnvironmentVariable(PinVariable, previous);
            File.Delete(path);
        }
    }

    private static string WriteConfig(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"falah-agent-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }

    // ---- SEC5: no PIN in the tracked file; the station's own secrets file ----

    [Fact]
    public void The_tracked_agent_configuration_names_no_user_and_holds_no_pin()
    {
        var root = AppContext.BaseDirectory;
        while (root is not null && !File.Exists(Path.Combine(root, "Falah.sln"))) root = Path.GetDirectoryName(root);
        Assert.NotNull(root);
        var tracked = Path.Combine(root!, "src", "Falah.PrintAgent", "appsettings.json");

        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(tracked));
        var settings = document.RootElement;
        Assert.True(!settings.TryGetProperty("Pin", out var pin) || string.IsNullOrEmpty(pin.GetString()), "a PIN in a tracked file is a published PIN");
        Assert.False(settings.TryGetProperty("ReportedByUserId", out _), "the agent is whoever its PIN logs in as");
    }

    [Fact]
    public void A_secrets_file_beside_the_agent_supplies_its_pin_and_the_environment_still_wins()
    {
        // Its own folder: the agent looks for the secrets file next to the configuration it loads.
        var folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"falah-agent-{Guid.NewGuid():N}")).FullName;
        var config = Path.Combine(folder, "appsettings.json");
        File.WriteAllText(config, """{ "Route": "Grill" }""");
        File.WriteAllText(Path.Combine(folder, AgentOptions.LocalSecretsFileName), """{ "Pin": "639214" }""");
        var previous = Environment.GetEnvironmentVariable(PinVariable);
        try
        {
            Environment.SetEnvironmentVariable(PinVariable, null);
            var options = AgentOptions.Load(config);
            Assert.Equal("639214", options.Pin);
            Assert.Equal("Grill", options.Route);
            Assert.Empty(options.Validate().Where(p => p.Contains("PIN", StringComparison.Ordinal)));

            Environment.SetEnvironmentVariable(PinVariable, "730914");
            Assert.Equal("730914", AgentOptions.Load(config).Pin);
        }
        finally
        {
            Environment.SetEnvironmentVariable(PinVariable, previous);
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Without_any_pin_the_agent_refuses_to_start_and_says_where_to_put_one()
    {
        var folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"falah-agent-{Guid.NewGuid():N}")).FullName;
        var config = Path.Combine(folder, "appsettings.json");
        File.WriteAllText(config, """{ "Route": "Receipt", "Transport": "file" }""");
        var previous = Environment.GetEnvironmentVariable(PinVariable);
        try
        {
            Environment.SetEnvironmentVariable(PinVariable, null);
            var problem = Assert.Single(AgentOptions.Load(config).Validate());
            Assert.Contains(PinVariable, problem);
            Assert.Contains(AgentOptions.LocalSecretsFileName, problem);
        }
        finally
        {
            Environment.SetEnvironmentVariable(PinVariable, previous);
            Directory.Delete(folder, recursive: true);
        }
    }
}

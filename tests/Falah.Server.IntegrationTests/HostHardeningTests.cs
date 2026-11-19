namespace Falah.Server.IntegrationTests;

using System.Net;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The server fails safely, and test hosts stay out of the restaurant's real resources.
/// </summary>
public class HostHardeningTests
{
    /// <summary>
    /// The last line of defence must cover authentication too. The authentication middleware was
    /// registered BEFORE the exception handler, so anything it threw - the session lookup failing
    /// because SQL Server is down or restarting - skipped the handler: no logged reference, no Arabic
    /// message, just a failed request the till cannot explain. This test makes the session lookup
    /// fail by dropping its own throwaway database out from under a running server.
    /// </summary>
    [Fact]
    public async Task A_failure_while_authenticating_returns_the_handled_error_with_a_reference()
    {
        using var factory = new FalahApiFactory();
        var cashierId = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", factory.TokenFor(cashierId));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/menu")).StatusCode);

        Assert.StartsWith("FalahTest_", factory.DatabaseName);
        SqlConnection.ClearAllPools();
        using (var master = new SqlConnection(@"Server=.\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"))
        {
            master.Open();
            using var drop = master.CreateCommand();
            drop.CommandText = $"ALTER DATABASE [{factory.DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{factory.DatabaseName}];";
            drop.ExecuteNonQuery();
        }

        var response = await client.GetAsync("/api/menu");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("reference", body);
        Assert.Contains("حدث خطأ غير متوقع", body);
    }

    /// <summary>The automatic backup worker writes to the restaurant's real backup folder by default.
    /// A test host must never do that, whichever test class built it.</summary>
    [Fact]
    public void Test_hosts_never_run_the_automatic_backup()
    {
        using var factory = new FalahApiFactory();
        var configuration = factory.Services.GetRequiredService<IConfiguration>();

        Assert.Equal("false", configuration["Backup:Automatic:Enabled"]);
    }
}

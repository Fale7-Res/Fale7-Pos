namespace Falah.Server.IntegrationTests;
using System.Net;
using Xunit;
public class TransportSecurityTests
{
    [Theory]
    [InlineData("http://0.0.0.0:5151")]
    [InlineData("http://*:5151")]
    [InlineData("http://restaurant-pc:5151")]
    public void Remote_cleartext_bindings_are_refused_before_database_startup(string address)
    {
        using var factory = new FalahApiFactory(configOverrides: new Dictionary<string,string?> { ["Urls"] = address });
        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("HTTPS", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task Remote_HTTP_cannot_use_a_spoofed_forwarded_proto_header()
    {
        using var factory = new FalahApiFactory(); using var client = factory.CreateClient();
        var context = await factory.Server.SendAsync(c => {
            c.Request.Method = "GET"; c.Request.Path = "/health"; c.Request.Scheme = "http";
            c.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.20");
            c.Request.Headers["X-Forwarded-Proto"] = "https";
        });
        Assert.Equal(400, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("Kestrel:Endpoints:Remote:Url", "http://0.0.0.0:5151")]
    [InlineData("HTTP_PORTS", "5151")]
    public void Alternate_Kestrel_cleartext_configuration_is_refused(string key, string value)
    {
        using var factory = new FalahApiFactory(configOverrides: new Dictionary<string,string?> { [key] = value, ["Urls"] = key == "HTTP_PORTS" ? "" : "http://localhost:5151" });
        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("HTTPS", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task HTTPS_remote_and_loopback_development_requests_remain_available()
    {
        using var factory = new FalahApiFactory(); using var client = factory.CreateClient();
        foreach (var https in new[] { true, false })
        {
            var context = await factory.Server.SendAsync(c => {
                c.Request.Method = "GET"; c.Request.Path = "/health"; c.Request.Scheme = https ? "https" : "http";
                c.Connection.RemoteIpAddress = IPAddress.Parse(https ? "192.168.1.20" : "127.0.0.1");
            });
            Assert.Equal(200, context.Response.StatusCode);
        }
    }
    [Fact]
    public void Agent_validates_remote_transport_before_login()
    {
        var options = new Falah.PrintAgent.AgentOptions { Pin = "fixture-pin", ServerBaseAddress = "http://192.168.1.20:5151" };
        Assert.Contains(options.Validate(), p => p.Contains("HTTPS"));
        Assert.Throws<ArgumentException>(() => new Falah.PrintAgent.PrintServerClient(options.ServerBaseAddress, TimeSpan.FromSeconds(10)));
        options.ServerBaseAddress = "https://restaurant-pc:5152";
        Assert.Empty(options.Validate());
    }

}

namespace Falah.Server.IntegrationTests;

using Xunit;

/// <summary>
/// SEC7. The server allowed cross-origin requests from any web page. No browser client exists - the
/// tills and the print agents are native programs, which CORS does not concern - so the only thing
/// that permission did was let a web page opened on any staff PC on the LAN call the API through that
/// browser: send PIN guesses to login-by-pin and read the session token a right one returns. Origins
/// are now refused unless configured (Cors:AllowedOrigins) for a browser client that actually exists.
/// </summary>
public class CorsTests
{
    private static HttpRequestMessage Preflight(string origin) => new(HttpMethod.Options, "/api/auth/login-by-pin")
    {
        Headers =
        {
            { "Origin", origin },
            { "Access-Control-Request-Method", "POST" },
            { "Access-Control-Request-Headers", "content-type" },
        },
    };

    [Fact]
    public async Task A_web_page_from_anywhere_is_not_allowed_to_call_the_api()
    {
        using var factory = new FalahApiFactory();
        var response = await factory.CreateClient().SendAsync(Preflight("http://evil.example"));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task An_origin_the_restaurant_configures_is_allowed()
    {
        using var factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "https://office.falah.local",
        });
        var client = factory.CreateClient();

        var allowed = await client.SendAsync(Preflight("https://office.falah.local"));
        Assert.Equal("https://office.falah.local", Assert.Single(allowed.Headers.GetValues("Access-Control-Allow-Origin")));

        var other = await client.SendAsync(Preflight("http://evil.example"));
        Assert.False(other.Headers.Contains("Access-Control-Allow-Origin"));
    }
}

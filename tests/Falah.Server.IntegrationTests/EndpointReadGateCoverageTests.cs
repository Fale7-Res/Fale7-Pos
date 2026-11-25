namespace Falah.Server.IntegrationTests;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class EndpointReadGateCoverageTests
{
    // Every deliberately open authenticated read has an explicit reason. New routes fail closed
    // in this coverage test until their access rule is reviewed.
    private static readonly Dictionary<string, string> OpenToAnySession = new()
    {
        ["/api/shifts/current"] = "Station operational state; sensitive fields redacted",
        ["/api/business-day/current"] = "Station operational state; sensitive fields redacted",
        ["/api/settings"] = "Station operational state; sensitive fields redacted",
        ["/api/business-day/cash-count-denominations"] = "Station operational state; sensitive fields redacted",
        ["/api/inventory/items"] = "Recipe and stock operational data; costs withheld",
        ["/api/delivery/services"] = "Delivery service names and fees for checkout on every till; no money or people",
        ["/api/delivery/services/all"] = "Delivery service names and fees for checkout on every till; no money or people",
        ["/api/menu"] = "Menu or staff picker needed by stations; no PINs or grants",
        ["/api/products/{id}/image"] = "Menu or staff picker needed by stations; no PINs or grants",
        ["/api/categories/all"] = "Menu or staff picker needed by stations; no PINs or grants",
        ["/api/products/all"] = "Menu or staff picker needed by stations; no PINs or grants",
        ["/api/products/{id}"] = "Menu or staff picker needed by stations; no PINs or grants",
        ["/api/tax-rules"] = "Menu or staff picker needed by stations; no PINs or grants",
        ["/api/price-rules"] = "Menu or staff picker needed by stations; no PINs or grants",
        ["/api/users"] = "Menu or staff picker needed by stations; no PINs or grants",
        ["/api/permissions/catalog"] = "Menu or staff picker needed by stations; no PINs or grants",
        ["/api/reports/export/catalog"] = "Lists only the reports this session may export; empty without a report permission",
    };
    [Fact]
    public async Task Every_api_read_refuses_a_permissionless_session_unless_explicitly_listed()
    {
        using var factory = new FalahApiFactory();
        var id = await factory.SeedUserAsync("محطة بلا صلاحيات", "station");
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.TokenFor(id));
        var source = factory.Services.GetRequiredService<EndpointDataSource>();
        var routes = source.Endpoints.OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Contains("GET") == true)
            .Select(e => e.RoutePattern.RawText!).Where(r => r.StartsWith("/api/")).Distinct().ToList();
        Assert.NotEmpty(routes);
        var errors = new List<string>();
        foreach (var stale in OpenToAnySession.Keys.Except(routes)) errors.Add("Stale allow-list route: " + stale);
        foreach (var route in routes)
        {
            var url = Regex.Replace(route, "\\{[^}]+\\}", "probe-id")
                + "?actorUserId=" + id + "&phone=01000000000&orderNumber=%239999&route=Receipt&accountId=tre-drawer&businessDate=2026-11-06&limit=1&from=2026-11-05&to=2026-11-06&report=owner&format=pdf";
            var response = await client.GetAsync(url);
            if (response.StatusCode is HttpStatusCode.UnprocessableEntity or HttpStatusCode.Forbidden)
            {
                if (OpenToAnySession.ContainsKey(route)) errors.Add("Remove newly gated route from allow-list: " + route);
                continue;
            }
            if (response.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.NotFound)) errors.Add(route + ": harness/route error " + response.StatusCode);
            else if (!OpenToAnySession.ContainsKey(route)) errors.Add("Ungated route missing rationale: " + route);
        }
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }
}

using System.Net;
using System.Text;
using Falah.Client.CashierWinUI.Services;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Native run 2026-11-07: the Inventory screen always showed "الطلب ناقص أو غير صالح" because
/// the movements read sent `from=&amp;to=`, which the server cannot bind to DateTimeOffset? (400).</summary>
public class InventoryMovementsQueryTests
{
    private sealed class Capture : HttpMessageHandler
    {
        public Uri? Last;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Last = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[]", Encoding.UTF8, "application/json") });
        }
    }

    [Fact]
    public async Task Recent_movements_send_no_empty_date_parameters()
    {
        var capture = new Capture();
        var api = new ApiClient(new HttpClient(capture) { BaseAddress = new Uri("http://fixture.invalid") });

        await api.GetStockMovementsAsync(limit: 60);

        var query = capture.Last!.Query;
        Assert.DoesNotContain("from=", query);
        Assert.DoesNotContain("to=", query);
        Assert.DoesNotContain("inventoryItemId=&", query + "&");
        Assert.Contains("limit=60", query);
    }

    [Fact]
    public async Task A_dated_loss_query_still_sends_its_range()
    {
        var capture = new Capture();
        var api = new ApiClient(new HttpClient(capture) { BaseAddress = new Uri("http://fixture.invalid") });
        var from = new DateTimeOffset(2026, 11, 6, 22, 0, 0, TimeSpan.Zero);

        await api.GetStockMovementsAsync(limit: 500, from: from, to: from.AddDays(1), lossesOnly: true, inventoryItemId: "inv-1");

        var query = Uri.UnescapeDataString(capture.Last!.Query);
        Assert.Contains("from=2026-11-06T22:00:00.0000000+00:00", query);
        Assert.Contains("to=2026-11-07T22:00:00.0000000+00:00", query);
        Assert.Contains("lossesOnly=true", query);
        Assert.Contains("inventoryItemId=inv-1", query);
    }
}

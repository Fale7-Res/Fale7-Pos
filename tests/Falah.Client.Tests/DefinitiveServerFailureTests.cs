namespace Falah.Client.Tests;
using System.Net;
using Falah.Client.CashierWinUI.Services;
using Falah.Application.Menu;

public class DefinitiveServerFailureTests
{
    private sealed class FailureHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") });
    }
    [Theory]
    [InlineData("{\"error\":\"refused\",\"retryable\":false}", false)]
    [InlineData("{\"error\":\"retry\",\"retryable\":true}", true)]
    [InlineData("{\"error\":\"unknown\"}", true)]
    public async Task Only_an_explicit_definitive_server_refusal_resolves_a_new_attempt(string body, bool unknown)
    {
        var api = new ApiClient(new HttpClient(new FailureHandler(body)) { BaseAddress = new Uri("http://fixture.invalid") });
        var request = new SetProductAvailabilityRequest("prod", false, "actor", "req");
        var result = await api.SetProductAvailabilityAsync(request);
        Assert.False(result.Success);
        Assert.Equal(unknown, result.OutcomeUnknown);
        var retained = new RetainedRequest<SetProductAvailabilityRequest>();
        await retained.SendAsync(request, _ => Task.FromResult(result));
        Assert.Equal(unknown, retained.HasPending);
    }
}

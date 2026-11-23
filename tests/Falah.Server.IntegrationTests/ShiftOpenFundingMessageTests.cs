namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Falah.Application.Shifts;

/// <summary>Independent audit 2026-11-23, F-10: on a new installation the main treasury is empty, so the till's
/// default float could not be issued - and the answer was only "source account balance insufficient", naming
/// neither the account nor the remedy.</summary>
public class ShiftOpenFundingMessageTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private string _managerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        Assert.StartsWith("FalahTest_", _factory.DatabaseName);
        _managerId = await _factory.SeedUserAsync("مدير", "manager", "all");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task An_unfunded_main_treasury_is_named_with_what_to_do_and_no_balance_figure()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(_managerId));

        var response = await client.PostAsJsonAsync("/api/shifts/open", new OpenShiftRequest($"open-{Guid.NewGuid():N}", "وردية", _managerId, 100_000));
        using var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = json.RootElement.GetProperty("error").GetString()!;

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("رصيد حساب المصدر غير كافٍ", body);   // the Shifts screen's masking keys on this
        Assert.Contains("الخزينة الرئيسية", body);
        Assert.Contains("إيداع", body);
        Assert.DoesNotContain("0.00", body);
    }
}

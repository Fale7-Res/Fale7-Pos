namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Settings;
using Xunit;

/// <summary>
/// Whole-project review 2026-11-17: the settings update checked only the default float and the menu
/// link. A negative delivery fee lowered order totals, zero receipt copies printed nothing, and zero
/// workday hours divided payroll by zero. Every numeric setting now refuses a value no restaurant can
/// mean, with an Arabic message, and stores nothing.
/// </summary>
public class SettingsValidationTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _managerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private UpdateSettingsRequest Invalid(string field) => field switch
    {
        "OvertimeHourlyRate" => new(_managerId, OvertimeHourlyRatePiastres: -1),
        "DeliveryFee" => new(_managerId, DeliveryFeePiastres: -100),
        "CashDiscrepancyThreshold" => new(_managerId, CashDiscrepancyThresholdPiastres: -1),
        "AutoLockMinutes" => new(_managerId, AutoLockMinutes: -5),
        "NumberOfReceiptCopies" => new(_managerId, NumberOfReceiptCopies: 0),
        "StandardWorkdayHours" => new(_managerId, StandardWorkdayHours: 0),
        "LateGraceMinutes" => new(_managerId, LateGraceMinutes: -1),
        "MaxLatePenaltyHours" => new(_managerId, MaxLatePenaltyHours: -1),
        "LatePenaltyMultiplier" => new(_managerId, LatePenaltyMultiplier: -0.5),
        "OvertimeMultiplier" => new(_managerId, OvertimeMultiplier: -1),
        _ => throw new ArgumentOutOfRangeException(nameof(field)),
    };

    [Theory]
    [InlineData("OvertimeHourlyRate")]
    [InlineData("DeliveryFee")]
    [InlineData("CashDiscrepancyThreshold")]
    [InlineData("AutoLockMinutes")]
    [InlineData("NumberOfReceiptCopies")]
    [InlineData("StandardWorkdayHours")]
    [InlineData("LateGraceMinutes")]
    [InlineData("MaxLatePenaltyHours")]
    [InlineData("LatePenaltyMultiplier")]
    [InlineData("OvertimeMultiplier")]
    public async Task An_impossible_value_is_refused_in_arabic_and_nothing_changes(string field)
    {
        var before = await _client.GetStringAsync("/api/settings");

        var response = await _client.PutAsJsonAsync("/api/settings", Invalid(field));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Matches("[؀-ۿ]", body!["error"]);
        Assert.Equal(before, await _client.GetStringAsync("/api/settings"));
    }

    [Fact]
    public async Task Ordinary_values_are_still_accepted()
    {
        (await _client.PutAsJsonAsync("/api/settings", new UpdateSettingsRequest(_managerId,
            OvertimeHourlyRatePiastres: 0, DeliveryFeePiastres: 2_500, CashDiscrepancyThresholdPiastres: 0, AutoLockMinutes: 0,
            NumberOfReceiptCopies: 1, StandardWorkdayHours: 9, LateGraceMinutes: 0, MaxLatePenaltyHours: 0,
            LatePenaltyMultiplier: 1, OvertimeMultiplier: 1.5))).EnsureSuccessStatusCode();
        var settings = await _client.GetFromJsonAsync<SettingsDto>("/api/settings");
        Assert.Equal(2_500, settings!.DeliveryFeePiastres);
        Assert.Equal(9, settings.StandardWorkdayHours);
    }
}

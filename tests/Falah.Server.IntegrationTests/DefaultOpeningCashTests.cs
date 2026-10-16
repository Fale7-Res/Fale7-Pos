namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Printing;
using Falah.Application.Settings;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// The configured float a shift opening dialog starts from.
///
/// The distinction every test here protects: the setting is a SUGGESTION, and the amount the
/// cashier confirmed is the FACT. A shift reconciles against what it stored, its slip prints what
/// it stored, and changing the setting later cannot reach back and alter either.
/// </summary>
public class DefaultOpeningCashTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _managerId = default!;
    private string _cashierId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private Task<SettingsDto> ReadSettingsAsync() => _client.GetFromJsonAsync<SettingsDto>("/api/settings")!;

    private Task<HttpResponseMessage> SetDefaultOpeningCashAsync(long piastres) =>
        _client.PutAsJsonAsync("/api/settings", new UpdateSettingsRequest(_managerId, DefaultOpeningCashPiastres: piastres));

    private async Task<OpenShiftResult> OpenShiftAsync(long openingCashPiastres) =>
        (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, openingCashPiastres)))
            .Content.ReadFromJsonAsync<OpenShiftResult>())!;

    private async Task CloseShiftAsync(string shiftId, long countedPiastres) =>
        (await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"close-{Guid.NewGuid():N}", shiftId, _cashierId, countedPiastres, null, null, null)))
            .EnsureSuccessStatusCode();

    [Fact]
    public async Task The_default_opening_cash_is_one_thousand_pounds()
    {
        var settings = await ReadSettingsAsync();
        Assert.Equal(100_000, settings.DefaultOpeningCashPiastres);
    }

    /// <summary>What the dialog reads when it opens: whatever the owner configured, not a number
    /// baked into the screen.</summary>
    [Fact]
    public async Task The_open_shift_dialog_reads_the_configured_default()
    {
        (await SetDefaultOpeningCashAsync(150_000)).EnsureSuccessStatusCode();

        var settings = await ReadSettingsAsync();
        Assert.Equal(150_000, settings.DefaultOpeningCashPiastres);
    }

    [Fact]
    public async Task A_cashier_can_open_with_a_different_amount_and_that_amount_is_what_is_stored()
    {
        (await SetDefaultOpeningCashAsync(100_000)).EnsureSuccessStatusCode();

        var opened = await OpenShiftAsync(150_000);   // typed over the offered 1000 ج

        Assert.Equal(150_000, opened.OpeningCashPiastres);
        var stored = await _factory.QueryAsync(db => db.Shifts.AsNoTracking().FirstAsync(s => s.Id == opened.ShiftId));
        Assert.Equal(150_000, stored.OpeningCashPiastres);
    }

    /// <summary>The float is an input to the shift's own reconciliation, so the stored amount - not
    /// the setting - is what the expected cash is built from.</summary>
    [Fact]
    public async Task Changing_the_setting_later_leaves_an_existing_shift_untouched()
    {
        (await SetDefaultOpeningCashAsync(100_000)).EnsureSuccessStatusCode();
        var opened = await OpenShiftAsync(150_000);

        (await SetDefaultOpeningCashAsync(25_000)).EnsureSuccessStatusCode();

        var stored = await _factory.QueryAsync(db => db.Shifts.AsNoTracking().FirstAsync(s => s.Id == opened.ShiftId));
        Assert.Equal(150_000, stored.OpeningCashPiastres);

        var prepared = await (await _client.PostAsJsonAsync("/api/shifts/prepare-close",
            new PrepareShiftCloseRequest(opened.ShiftId, 150_000))).Content.ReadFromJsonAsync<PrepareShiftCloseResult>();
        Assert.Equal(150_000, prepared!.ExpectedCashPiastres);   // built from the shift, not the setting
        Assert.Equal(0, prepared.DifferencePiastres);
    }

    [Fact]
    public async Task The_next_shift_is_offered_the_current_default_again()
    {
        (await SetDefaultOpeningCashAsync(100_000)).EnsureSuccessStatusCode();
        var first = await OpenShiftAsync(150_000);
        await CloseShiftAsync(first.ShiftId, 150_000);

        (await SetDefaultOpeningCashAsync(80_000)).EnsureSuccessStatusCode();

        var settings = await ReadSettingsAsync();
        Assert.Equal(80_000, settings.DefaultOpeningCashPiastres);
        // And the closed shift still holds what it was opened with.
        var stored = await _factory.QueryAsync(db => db.Shifts.AsNoTracking().FirstAsync(s => s.Id == first.ShiftId));
        Assert.Equal(150_000, stored.OpeningCashPiastres);
    }

    [Fact]
    public async Task The_opening_slip_prints_the_amount_this_shift_actually_opened_with()
    {
        (await SetDefaultOpeningCashAsync(100_000)).EnsureSuccessStatusCode();
        var opened = await OpenShiftAsync(150_000);

        // Changed again before the slip is composed: the paper must still say 1500.
        (await SetDefaultOpeningCashAsync(20_000)).EnsureSuccessStatusCode();

        var slip = await _factory.QueryAsync(db => db.PrintJobs.AsNoTracking()
            .FirstAsync(j => j.OrderId == opened.ShiftId && j.DocumentKind == "ShiftOpen"));
        var ticket = await (await _client.GetAsync($"/api/print-jobs/{slip.Id}/ticket")).Content.ReadFromJsonAsync<ComposedTicketDto>();
        var text = ticket!.Lines.SelectMany(TicketDtoText.Of).Where(t => t is not null).Select(t => t!).ToList();

        Assert.Contains("رصيد افتتاحي", text);
        Assert.Contains("1500 ج", text);
        Assert.DoesNotContain("200 ج", text);
    }

    [Fact]
    public async Task A_negative_default_is_rejected_with_an_arabic_message_and_nothing_is_stored()
    {
        var before = (await ReadSettingsAsync()).DefaultOpeningCashPiastres;

        var response = await SetDefaultOpeningCashAsync(-1);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Contains("الرصيد الافتتاحي الافتراضي", body!["error"]);
        Assert.Equal(before, (await ReadSettingsAsync()).DefaultOpeningCashPiastres);
    }

    /// <summary>Zero is a real answer - a till that starts empty - so it must be accepted.</summary>
    [Fact]
    public async Task Zero_is_accepted_and_the_change_is_audited()
    {
        (await SetDefaultOpeningCashAsync(0)).EnsureSuccessStatusCode();

        Assert.Equal(0, (await ReadSettingsAsync()).DefaultOpeningCashPiastres);

        var audit = await _factory.QueryAsync(db => db.AuditEvents.AsNoTracking()
            .Where(a => a.Category == "settings").OrderByDescending(a => a.TimestampUtc).FirstAsync());
        Assert.Contains("الرصيد الافتتاحي الافتراضي", audit.Details);
        Assert.Equal("المدير", audit.UserName);
    }
}

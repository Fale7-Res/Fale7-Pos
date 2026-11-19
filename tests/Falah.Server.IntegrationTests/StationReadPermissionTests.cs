namespace Falah.Server.IntegrationTests;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Falah.Application.Settings;
using Falah.Application.Customers;
using Xunit;

public class StationReadPermissionTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private string _station = default!, _owner = default!, _agent = default!, _pos = default!, _printing = default!;
    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _station = await _factory.SeedUserAsync("محطة", "station");
        _owner = await _factory.SeedUserAsync("مالك", "owner", "all");
        _agent = await _factory.SeedUserAsync("طباعة", "print_agent", "print_agent");
        _pos = await _factory.SeedUserAsync("كاشير", "cashier", "pos");
        _printing = await _factory.SeedUserAsync("إعداد", "station", "settings_manage");
    }
    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }
    private HttpClient As(string id) { var c = _factory.CreateClient(); c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(id)); return c; }
    [Fact]
    public async Task Ticket_queue_day_history_and_customer_reads_use_their_own_permission_grants()
    {
        var errors = new List<string>();
        foreach (var route in new[] { "/api/print-jobs/probe/ticket", "/api/print-jobs/pending?route=Receipt", "/api/business-days", "/api/business-day/probe", "/api/customers/lookup?phone=01000000000", "/api/customers/probe" })
            if ((await As(_station).GetAsync(route)).StatusCode != HttpStatusCode.UnprocessableEntity) errors.Add(route);
        Assert.Empty(errors);
        Assert.Equal(HttpStatusCode.NotFound, (await As(_agent).GetAsync("/api/print-jobs/probe/ticket")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await As(_printing).GetAsync("/api/print-jobs/probe/ticket")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await As(_printing).GetAsync("/api/print-jobs/pending?route=Receipt")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await As(_pos).GetAsync("/api/customers/probe")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await As(_owner).GetAsync("/api/business-days")).StatusCode);
    }
    [Fact]
    public async Task Settings_read_and_sound_toggle_never_reveal_the_tolerated_shortage_to_a_till()
    {
        (await As(_owner).PutAsJsonAsync("/api/settings", new UpdateSettingsRequest(_owner, CashDiscrepancyThresholdPiastres: 54321))).EnsureSuccessStatusCode();
        var redacted = (await As(_pos).GetFromJsonAsync<SettingsDto>("/api/settings"))!;
        Assert.Equal(0, redacted.CashDiscrepancyThresholdPiastres);
        var response = await As(_pos).PutAsJsonAsync("/api/settings", new UpdateSettingsRequest(_pos, GrillScreenSoundAlert: !redacted.GrillScreenSoundAlert));
        response.EnsureSuccessStatusCode();
        Assert.Equal(0, (await response.Content.ReadFromJsonAsync<SettingsDto>())!.CashDiscrepancyThresholdPiastres);
        Assert.Equal(54321, (await As(_printing).GetFromJsonAsync<SettingsDto>("/api/settings"))!.CashDiscrepancyThresholdPiastres);
    }

    [Fact]
    public async Task Order_customer_details_and_driver_money_are_not_visible_to_unrelated_stations()
    {
        var routes = new[] { "/api/orders?actorUserId=" + _station, "/api/orders/probe", "/api/orders/lookup?orderNumber=%239999",
            "/api/delivery/board", "/api/delivery/ready", "/api/delivery/drivers/probe/active", "/api/delivery/probe",
            "/api/delivery/drivers/probe/account", "/api/delivery/drivers/probe/statistics" };
        var errors = new List<string>();
        foreach (var route in routes)
            if ((await As(_station).GetAsync(route)).StatusCode != HttpStatusCode.UnprocessableEntity) errors.Add(route);
        Assert.Empty(errors);
        Assert.Equal(HttpStatusCode.NotFound, (await As(_pos).GetAsync("/api/orders/probe")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await As(_pos).GetAsync("/api/delivery/drivers/probe/account")).StatusCode);
        var reports = await _factory.SeedUserAsync("تقارير", "station", "reports_view");
        Assert.Equal(HttpStatusCode.NotFound, (await As(reports).GetAsync("/api/delivery/drivers/probe/account")).StatusCode);
    }

}

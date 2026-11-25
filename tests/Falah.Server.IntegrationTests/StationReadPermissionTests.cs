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

    // Audit F-19 (owner, 2026-11-25: fix all findings): recipes, stock levels, drivers, delivery timelines and print
    // status were readable by any session - a print agent's PIN, or a station with no role, read them all.
    [Fact]
    public async Task Recipes_stock_drivers_timelines_and_print_status_need_a_role_that_uses_them()
    {
        var routes = new[] { "/api/recipes", "/api/recipes/probe", "/api/prep-recipes", "/api/order-packaging-rules",
            "/api/inventory/items/probe/balance", "/api/delivery/drivers", "/api/delivery/drivers/all", "/api/delivery/probe/timeline",
            "/api/orders/probe/print-jobs", "/api/print-jobs/overview", "/api/print-jobs/probe" };
        var errors = new List<string>();
        foreach (var route in routes)
            if ((await As(_station).GetAsync(route)).StatusCode != HttpStatusCode.UnprocessableEntity) errors.Add(route);
        foreach (var route in new[] { "/api/recipes", "/api/inventory/items/probe/balance", "/api/delivery/drivers", "/api/print-jobs/overview" })
            if ((await As(_agent).GetAsync(route)).StatusCode != HttpStatusCode.UnprocessableEntity) errors.Add("agent " + route);
        Assert.Empty(errors);

        // Each still answers the screens that use it.
        var menu = await _factory.SeedUserAsync("قائمة", "station", "menu_manage");
        var stock = await _factory.SeedUserAsync("مخزن", "station", "inventory_manage");
        Assert.Equal(HttpStatusCode.OK, (await As(menu).GetAsync("/api/recipes")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await As(stock).GetAsync("/api/inventory/items/probe/balance")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await As(_pos).GetAsync("/api/delivery/drivers")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await As(_pos).GetAsync("/api/delivery/probe/timeline")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await As(_pos).GetAsync("/api/orders/probe/print-jobs")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await As(_printing).GetAsync("/api/print-jobs/overview")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await As(_owner).GetAsync("/api/recipes")).StatusCode);
    }

}

namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Audit;
using Falah.Application.CashDrawer;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Ported from the 'drawer_open' case of addCashMovement in the reference app's lib/store.tsx - the
/// ONE case of that command that posts NO Treasury movement at all. Kept completely separate from
/// payment/printing semantics: a drawer-open command never touches Order/Payment/Treasury, and
/// nothing about a sale's success ever depends on this succeeding, matching the same invariant
/// Printing already established ("successful order/payment != successful print").
/// </summary>
public class CashDrawerVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "cash_drawer");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Opening_the_drawer_with_permission_succeeds_and_starts_in_requested_status()
    {
        var response = await _client.PostAsJsonAsync("/api/cash-drawer/open",
            new OpenCashDrawerRequest($"req-{Guid.NewGuid():N}", _shiftId, "فحص الدرج", _cashierId, null, null));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CashDrawerOpenDto>();
        Assert.Equal("Requested", result!.Status);
        Assert.Null(result.LastError);
    }

    [Fact]
    public async Task Opening_the_drawer_posts_no_treasury_movement_at_all()
    {
        var before = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());
        (await _client.PostAsJsonAsync("/api/cash-drawer/open",
            new OpenCashDrawerRequest($"req-{Guid.NewGuid():N}", _shiftId, "فحص الدرج", _cashierId, null, null))).EnsureSuccessStatusCode();
        var after = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Opening_the_drawer_without_permission_is_rejected()
    {
        var noPermissionUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");
        var response = await _client.PostAsJsonAsync("/api/cash-drawer/open",
            new OpenCashDrawerRequest($"req-{Guid.NewGuid():N}", _shiftId, "فحص الدرج", noPermissionUser, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Opening_the_drawer_requires_a_reason()
    {
        var response = await _client.PostAsJsonAsync("/api/cash-drawer/open",
            new OpenCashDrawerRequest($"req-{Guid.NewGuid():N}", _shiftId, "", _cashierId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Opening_the_drawer_requires_the_shift_to_be_open()
    {
        var closeResponse = await _client.PostAsJsonAsync("/api/shifts/close",
            new Falah.Application.Shifts.CloseShiftRequest($"creq-{Guid.NewGuid():N}", _shiftId, _cashierId, 50000, null, null, null));
        closeResponse.EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("/api/cash-drawer/open",
            new OpenCashDrawerRequest($"req-{Guid.NewGuid():N}", _shiftId, "فحص الدرج", _cashierId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Retrying_the_same_open_request_id_replays_not_duplicates()
    {
        var requestId = $"req-{Guid.NewGuid():N}";
        var first = await (await _client.PostAsJsonAsync("/api/cash-drawer/open",
            new OpenCashDrawerRequest(requestId, _shiftId, "فحص الدرج", _cashierId, null, null))).Content.ReadFromJsonAsync<CashDrawerOpenDto>();
        var second = await (await _client.PostAsJsonAsync("/api/cash-drawer/open",
            new OpenCashDrawerRequest(requestId, _shiftId, "فحص الدرج", _cashierId, null, null))).Content.ReadFromJsonAsync<CashDrawerOpenDto>();
        Assert.Equal(first!.Id, second!.Id);
        var count = await _factory.QueryAsync(db => db.CashDrawerOpens.CountAsync(c => c.ShiftId == _shiftId));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Concurrent_open_requests_with_the_same_request_id_create_exactly_one_command_row()
    {
        var requestId = $"req-{Guid.NewGuid():N}";
        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/cash-drawer/open",
            new OpenCashDrawerRequest(requestId, _shiftId, "فحص الدرج", _cashierId, null, null)));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var count = await _factory.QueryAsync(db => db.CashDrawerOpens.CountAsync(c => c.ShiftId == _shiftId));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Reporting_a_successful_open_updates_its_status()
    {
        var created = await (await _client.PostAsJsonAsync("/api/cash-drawer/open",
            new OpenCashDrawerRequest($"req-{Guid.NewGuid():N}", _shiftId, "فحص الدرج", _cashierId, null, null))).Content.ReadFromJsonAsync<CashDrawerOpenDto>();

        var reported = await (await _client.PostAsJsonAsync("/api/cash-drawer/report-result",
            new ReportCashDrawerResultRequest(created!.Id, true, null, _cashierId))).Content.ReadFromJsonAsync<CashDrawerOpenDto>();
        Assert.Equal("Succeeded", reported!.Status);
    }

    [Fact]
    public async Task Reporting_a_failed_open_records_the_error_without_touching_anything_financial()
    {
        var created = await (await _client.PostAsJsonAsync("/api/cash-drawer/open",
            new OpenCashDrawerRequest($"req-{Guid.NewGuid():N}", _shiftId, "فحص الدرج", _cashierId, null, null))).Content.ReadFromJsonAsync<CashDrawerOpenDto>();
        var before = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());

        var reported = await (await _client.PostAsJsonAsync("/api/cash-drawer/report-result",
            new ReportCashDrawerResultRequest(created!.Id, false, "لا يستجيب", _cashierId))).Content.ReadFromJsonAsync<CashDrawerOpenDto>();
        Assert.Equal("Failed", reported!.Status);
        Assert.Equal("لا يستجيب", reported.LastError);

        var after = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Opening_the_drawer_is_audited_as_a_warning_severity_cash_event()
    {
        (await _client.PostAsJsonAsync("/api/cash-drawer/open",
            new OpenCashDrawerRequest($"req-{Guid.NewGuid():N}", _shiftId, "فحص الدرج الصباحي", _cashierId, null, null))).EnsureSuccessStatusCode();

        var events = await (await _client.GetAsync($"/api/audit/events?actorUserId={_managerId}&category=cash"))
            .Content.ReadFromJsonAsync<List<AuditEventDto>>();
        Assert.Contains(events!, e => e.Action.Contains("فتح درج") && e.Severity == "warning" && e.ShiftId == _shiftId);
    }
}

namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.BusinessDay;
using Falah.Application.Shifts;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Shifts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Owner-approved cashier handover (2026-11-09, docs/UI_OWNER_REQUESTS.md "Cashier shift handover clarification").
/// A cashier signs in with their own PIN and opens their own shift with the cash actually received. No manager
/// login is needed. When no accounting day is active, the shift opening itself resolves one: it reuses an active
/// day, or creates one for the server's business date. The day, float, shift and opening slip commit together.
/// The cashier gains no day-management permission. A closed date is never reopened or duplicated. A still-open
/// shift blocks the next. Concurrent opens cannot create two days or two shifts.
/// </summary>
public class CashierShiftStartupTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _ahmed = default!;
    private string _sara = default!;
    private string _waiter = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _ahmed = await _factory.SeedUserAsync("أحمد", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _sara = await _factory.SeedUserAsync("سارة", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _waiter = await _factory.SeedUserAsync("نادل", "cashier", "pos");
        await _factory.SeedUserAsync("المدير", "manager", "all");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private Task<HttpResponseMessage> Open(string user, long cash, string? requestId = null) =>
        _client.PostAsJsonAsync("/api/shifts/open", new OpenShiftRequest(requestId ?? $"shift-open-{Guid.NewGuid():N}", "وردية", user, cash));

    private Task<T> Q<T>(Func<Falah.Infrastructure.Persistence.FalahDbContext, Task<T>> q) => _factory.QueryAsync(q);

    private async Task<(int days, int shifts, int floats, int slips)> Counts() => (
        await Q(db => db.BusinessDays.CountAsync()),
        await Q(db => db.Shifts.CountAsync()),
        await Q(db => db.TreasuryMovements.CountAsync(m => m.Type == "shift_opening_float")),
        await Q(db => db.PrintJobs.CountAsync(j => j.DocumentKind == "ShiftOpen")));

    private async Task SeedDay(DateOnly date, string status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Falah.Infrastructure.Persistence.FalahDbContext>();
        db.BusinessDays.Add(new BusinessDayEntity
        {
            Id = $"day-{Guid.NewGuid():N}", BusinessDate = date, OpenedBy = "seed", OpenedAtUtc = DateTimeOffset.UtcNow.AddHours(-6),
            Status = status, CloseRevision = 0, OpenRequestId = $"seed-{Guid.NewGuid():N}",
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task A_cashier_opens_their_own_shift_without_a_manager_and_the_accounting_day_is_created_with_it()
    {
        await _factory.FundMainAccountAsync();

        var response = await Open(_ahmed, 99_000);   // 990 actually received, not the 1,000 target

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var shift = await Q(db => db.Shifts.AsNoTracking().SingleAsync());
        var day = await Q(db => db.BusinessDays.AsNoTracking().SingleAsync());
        Assert.Equal(99_000, shift.OpeningCashPiastres);
        Assert.Equal(day.Id, shift.BusinessDayId);
        Assert.Equal("Open", day.Status);
        Assert.Equal(_ahmed, day.OpenedBy);
        Assert.Equal(BusinessClock.Today(), day.BusinessDate);
        var opening = await Q(db => db.TreasuryMovements.AsNoTracking().SingleAsync(m => m.Type == "shift_opening_float"));
        Assert.Equal(99_000, opening.AmountPiastres);
        Assert.Equal(day.Id, opening.BusinessDayId);
        Assert.Equal(1, await Q(db => db.PrintJobs.CountAsync(j => j.DocumentKind == "ShiftOpen" && j.OrderId == shift.Id)));

        // The cashier still has no standalone day management.
        var dayOpen = await _client.PostAsJsonAsync("/api/business-day/open", new OpenBusinessDayRequest($"day-{Guid.NewGuid():N}", _ahmed));
        Assert.False(dayOpen.IsSuccessStatusCode);
    }

    [Fact]
    public async Task An_existing_active_day_is_reused_and_handover_keeps_the_same_day()
    {
        var dayId = await _factory.SeedOpenBusinessDayWithFundedMainAsync();

        var a = await Open(_ahmed, 100_000);
        Assert.True(a.IsSuccessStatusCode, await a.Content.ReadAsStringAsync());
        var ahmedShift = (await a.Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;

        // Sara cannot open while Ahmed's shift is open; nothing is closed for her and no day is created.
        var blocked = await Open(_sara, 100_000);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("Open", await Q(db => db.Shifts.Where(s => s.Id == ahmedShift).Select(s => s.Status).SingleAsync()));

        // Ahmed blind-counts: the count is submitted first, the server answers with expected and difference.
        var prepare = await _client.PostAsJsonAsync("/api/shifts/prepare-close", new { shiftId = ahmedShift, actualCashCountedPiastres = 99_500L });
        Assert.True(prepare.IsSuccessStatusCode, await prepare.Content.ReadAsStringAsync());
        var close = await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"close-{Guid.NewGuid():N}", ahmedShift, _ahmed, 99_500, "فرق بسيط في الفكة", null, null));
        Assert.True(close.IsSuccessStatusCode, await close.Content.ReadAsStringAsync());
        Assert.NotNull(await Q(db => ShiftCloseStatements.LoadFrozenAsync(db, ahmedShift, default)));

        // Sara signs in and opens her own shift with the cash she actually received, on the same day.
        var b = await Open(_sara, 99_000);
        Assert.True(b.IsSuccessStatusCode, await b.Content.ReadAsStringAsync());
        var saraShift = (await b.Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
        Assert.Equal(1, await Q(db => db.BusinessDays.CountAsync()));
        Assert.Equal(dayId, await Q(db => db.Shifts.Where(s => s.Id == saraShift).Select(s => s.BusinessDayId).SingleAsync()));
        Assert.Equal(99_000, await Q(db => db.Shifts.Where(s => s.Id == saraShift).Select(s => s.OpeningCashPiastres).SingleAsync()));
    }

    [Fact]
    public async Task A_user_without_shift_permission_creates_nothing()
    {
        await _factory.FundMainAccountAsync();
        var response = await Open(_waiter, 100_000);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal((0, 0, 0, 0), await Counts());
    }

    [Fact]
    public async Task Insufficient_float_funds_or_invalid_input_leave_no_partial_opening()
    {
        // Main treasury empty: the day must not be created on its own.
        var poor = await Open(_ahmed, 100_000);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, poor.StatusCode);
        Assert.Equal((0, 0, 0, 0), await Counts());

        await _factory.FundMainAccountAsync();
        var negative = await Open(_ahmed, -500);
        Assert.False(negative.IsSuccessStatusCode);
        Assert.Equal((0, 0, 0, 0), await Counts());
    }

    [Fact]
    public async Task A_lost_response_retried_with_the_same_request_records_everything_once()
    {
        await _factory.FundMainAccountAsync();
        var requestId = $"shift-open-{Guid.NewGuid():N}";

        var first = await Open(_ahmed, 100_000, requestId);
        var retry = await Open(_ahmed, 100_000, requestId);   // the reply was lost; the till retries after a restart

        Assert.True(first.IsSuccessStatusCode && retry.IsSuccessStatusCode);
        var replay = (await retry.Content.ReadFromJsonAsync<OpenShiftResult>())!;
        Assert.True(replay.WasReplayed);
        Assert.Equal((await first.Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId, replay.ShiftId);
        Assert.Equal((1, 1, 1, 1), await Counts());
    }

    [Fact]
    public async Task Concurrent_opens_create_one_day_and_one_shift()
    {
        await _factory.FundMainAccountAsync();

        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(i => Open(i % 2 == 0 ? _ahmed : _sara, 50_000)));

        Assert.Equal(1, results.Count(r => r.IsSuccessStatusCode));
        Assert.All(results.Where(r => !r.IsSuccessStatusCode), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        Assert.Equal((1, 1, 1, 1), await Counts());
    }

    [Fact]
    public async Task A_closed_day_for_today_is_never_reopened_or_duplicated_by_a_cashier()
    {
        await _factory.FundMainAccountAsync();
        await SeedDay(BusinessClock.Today(), "Closed");

        var response = await Open(_ahmed, 100_000);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(1, await Q(db => db.BusinessDays.CountAsync()));
        Assert.Equal("Closed", await Q(db => db.BusinessDays.Select(d => d.Status).SingleAsync()));
        Assert.Equal(0, await Q(db => db.Shifts.CountAsync()));
    }
}

namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A shift belongs to the business day it was opened under - not to the calendar date it was opened on.
///
/// The two differ whenever the restaurant trades past midnight: the business day opened yesterday
/// evening is still the one running, while a shift opened after midnight carries today's calendar
/// date. Business-day figures (the reconciliation, the Daily Summary, the close exceptions and the
/// drawer check that can block the close) picked their shifts by calendar date, so such a shift was
/// missing from the day it ran in - its cash count, its expected cash and any shortage with it.
/// </summary>
public class BusinessDayMembershipTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;

    private static readonly DateOnly Yesterday = BusinessClock.Today().AddDays(-1);

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?>
        {
            ["Backup:Automatic:Enabled"] = "false",
        });
        var database = await _factory.QueryAsync(db => Task.FromResult(db.Database.GetDbConnection().Database));
        Assert.StartsWith("FalahTest_", database);

        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.FundMainAccountAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task A_shift_opened_after_midnight_counts_in_the_business_day_it_ran_under()
    {
        var dayId = await SeedDayOpenedYesterdayEveningAsync();
        var shiftId = await OpenShiftAsync(50000);
        await CloseAsync(shiftId, 49000);   // 1000 short, within the default threshold

        var day = await _client.GetFromJsonAsync<BusinessDayReconciliationDto>($"/api/reports/business-day/{dayId}?actorUserId={_managerId}");
        Assert.Equal(1, day!.ShiftCount);
        Assert.Equal(50000, day.ShiftsExpectedCashPiastres);
        Assert.Equal(49000, day.ShiftsActualCashPiastres);
        Assert.Equal(1000, day.ShiftsShortagePiastres);

        var summary = await _client.GetFromJsonAsync<DailySummaryDto>($"/api/reports/daily-summary/{dayId}?actorUserId={_managerId}");
        Assert.Equal(1000, summary!.ShiftsShortagePiastres);
    }

    [Fact]
    public async Task A_shift_from_before_shifts_recorded_their_business_day_still_counts_by_its_date()
    {
        // Rows written before the business day was recorded on the shift have only their calendar
        // date to go on; that is the one rule they were reported under, and it must keep working.
        var dayId = await SeedDayAsync(BusinessClock.Today(), DateTimeOffset.UtcNow.AddHours(-1));
        await SeedLegacyClosedShiftAsync(BusinessClock.Today(), expected: 50000, actual: 48000);

        var day = await _client.GetFromJsonAsync<BusinessDayReconciliationDto>($"/api/reports/business-day/{dayId}?actorUserId={_managerId}");
        Assert.Equal(1, day!.ShiftCount);
        Assert.Equal(2000, day.ShiftsShortagePiastres);
    }

    // ---------- Helpers ----------

    /// <summary>A business day dated yesterday and still open now: the shape of a trading night
    /// that has run past midnight.</summary>
    private Task<string> SeedDayOpenedYesterdayEveningAsync() =>
        SeedDayAsync(Yesterday, DateTimeOffset.UtcNow.AddHours(-3));

    private async Task<string> SeedDayAsync(DateOnly businessDate, DateTimeOffset openedAtUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var id = $"day-{Guid.NewGuid():N}";
        db.BusinessDays.Add(new BusinessDayEntity
        {
            Id = id, BusinessDate = businessDate, OpenedBy = "seed", OpenedAtUtc = openedAtUtc,
            Status = "Open", CloseRevision = 0, OpenRequestId = $"seed-{Guid.NewGuid():N}",
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task SeedLegacyClosedShiftAsync(DateOnly businessDate, long expected, long actual)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        db.Shifts.Add(new ShiftEntity
        {
            Id = $"shift-{Guid.NewGuid():N}", ShiftName = "قديمة", OpenedByUserId = _cashierId, OpeningCashPiastres = expected,
            Status = "Closed", BusinessDate = businessDate, OpenedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-30),
            ClosedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5), ClosedByUserId = _cashierId,
            ExpectedCashPiastres = expected, ActualCashPiastres = actual, CashDifferencePiastres = actual - expected,
            OpenRequestId = $"open-{Guid.NewGuid():N}", CloseRequestId = $"close-{Guid.NewGuid():N}",
        });
        await db.SaveChangesAsync();
    }

    private async Task<string> OpenShiftAsync(long openingCash)
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "ليلية", _cashierId, openingCash));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    private async Task CloseAsync(string shiftId, long counted)
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"creq-{Guid.NewGuid():N}", shiftId, _cashierId, counted, "فرق بسيط", null, null));
        response.EnsureSuccessStatusCode();
    }
}

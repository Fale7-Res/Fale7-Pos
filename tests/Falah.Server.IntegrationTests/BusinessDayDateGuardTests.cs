namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.BusinessDay;
using Falah.Application.Employees;
using Falah.Application.Shifts;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Independent audit 2026-11-23, F-02. The Control Center lets a manager type the date of a new
/// business day. A date whose day is already closed was accepted - a second day record for a closed
/// period - and so was a future date, whose close then accrued every monthly salary through that date
/// (and the real days in between were skipped, because accrual resumes after the latest accrued day).</summary>
public class BusinessDayDateGuardTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _managerId = default!;
    private string _cashierId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        Assert.StartsWith("FalahTest_", _factory.DatabaseName);
        _client = _factory.CreateAuthenticatedClient();
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        await _factory.FundMainAccountAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task A_business_day_cannot_be_opened_for_a_date_already_closed()
    {
        var closedDate = BusinessClock.Today().AddDays(-22);
        await _factory.QueryAsync(async db =>
        {
            db.BusinessDays.Add(new BusinessDayEntity
            {
                Id = $"day-{Guid.NewGuid():N}", BusinessDate = closedDate, OpenedBy = "seed", OpenedAtUtc = DateTimeOffset.UtcNow.AddDays(-22),
                Status = "Closed", CloseRevision = 1, OpenRequestId = $"seed-{Guid.NewGuid():N}",
                ClosedAtUtc = DateTimeOffset.UtcNow.AddDays(-22), ClosedBy = "seed", SnapshotId = "snap-seed",
            });
            return await db.SaveChangesAsync();
        });

        var open = await _client.PostAsJsonAsync("/api/business-day/open",
            new OpenBusinessDayRequest($"day-{Guid.NewGuid():N}", _managerId, closedDate));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, open.StatusCode);
        Assert.Equal(1, await _factory.QueryAsync(db => db.BusinessDays.CountAsync(d => d.BusinessDate == closedDate)));
    }

    [Fact]
    public async Task A_business_day_cannot_be_opened_for_a_future_date()
    {
        var open = await _client.PostAsJsonAsync("/api/business-day/open",
            new OpenBusinessDayRequest($"day-{Guid.NewGuid():N}", _managerId, BusinessClock.Today().AddDays(60)));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, open.StatusCode);
        Assert.Equal(0, await _factory.QueryAsync(db => db.BusinessDays.CountAsync()));
    }

    [Fact]
    public async Task Opening_today_without_a_date_still_works()
    {
        var open = await _client.PostAsJsonAsync("/api/business-day/open",
            new OpenBusinessDayRequest($"day-{Guid.NewGuid():N}", _managerId));

        open.EnsureSuccessStatusCode();
        var day = await open.Content.ReadFromJsonAsync<OpenBusinessDayResult>();
        Assert.Equal(BusinessClock.Today().ToString("yyyy-MM-dd"), day!.BusinessDate);
    }

    /// <summary>Defence in depth: a day already carrying a future date (opened before this guard) must
    /// not accrue salary beyond today when it closes.</summary>
    [Fact]
    public async Task Closing_a_day_dated_in_the_future_accrues_no_salary_beyond_today()
    {
        var employee = await _client.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest(
            $"req-{Guid.NewGuid():N}", "محاسب المطعم", $"EMP-{Guid.NewGuid():N}", "محاسب", "01000000000", new DateOnly(2024, 1, 1),
            "Monthly", 0, 0, 300_000, false, "cashier", null, null, null, null, null, null, null, null, _managerId));
        employee.EnsureSuccessStatusCode();
        var employeeId = (await employee.Content.ReadFromJsonAsync<EmployeeDto>())!.Id;

        var today = BusinessClock.Today();
        var dayId = $"day-{Guid.NewGuid():N}";
        await _factory.QueryAsync(async db =>
        {
            db.BusinessDays.Add(new BusinessDayEntity
            {
                Id = dayId, BusinessDate = today.AddDays(60), OpenedBy = "seed", OpenedAtUtc = DateTimeOffset.UtcNow,
                Status = "Open", CloseRevision = 0, OpenRequestId = $"seed-{Guid.NewGuid():N}",
            });
            return await db.SaveChangesAsync();
        });
        var shift = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 0))).Content.ReadFromJsonAsync<OpenShiftResult>();
        (await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"creq-{Guid.NewGuid():N}", shift!.ShiftId, _cashierId, 0, null, null, null))).EnsureSuccessStatusCode();
        foreach (var itemId in new[] { "close-shifts", "close-drawer", "close-delivery" })
            (await _client.PostAsJsonAsync($"/api/business-day/{dayId}/checklist/{itemId}",
                new CompleteChecklistItemRequest(dayId, itemId, true, _managerId))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/business-day/{dayId}/close",
            new CloseBusinessDayRequest($"close-{Guid.NewGuid():N}", dayId, _managerId, null, null))).EnsureSuccessStatusCode();

        var accruedDays = await _factory.QueryAsync(db => db.EmployeeLedgerEntries
            .Where(e => e.EmployeeId == employeeId && e.Type == "Salary").Select(e => e.ReferenceId!).ToListAsync());
        Assert.NotEmpty(accruedDays);
        Assert.All(accruedDays, d => Assert.True(string.CompareOrdinal(d, today.ToString("yyyy-MM-dd")) <= 0, $"accrued {d}, after today {today:yyyy-MM-dd}"));
        Assert.Equal(today.Day, accruedDays.Count);
    }
}

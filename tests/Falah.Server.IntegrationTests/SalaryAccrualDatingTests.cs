namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using System.Text.Json;
using Falah.Application.Employees;
using Falah.Infrastructure.Employees;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// FIN1. A monthly salary is earned a share each day (owner's rule), and those shares are posted after
/// a business day closes - often several at once: a close after midnight, the first close after days
/// without one, a payroll true-up. Every date-based figure read the ledger by when an entry was
/// POSTED, so a catch-up put days of salary on the posting day and none on the days the work was done;
/// the Daily Statement's employee rows and the Daily Summary's employee totals did not show salary at
/// all. Salary accruals now count on the day they were earned for, everywhere.
///
/// September 2026 has 30 days; 310 000 piastres a month is 10 333 a day with the last day taking the
/// remainder, 10 343.
/// </summary>
public class SalaryAccrualDatingTests : IAsyncLifetime
{
    private const long MonthlySalary = 310_000;
    private const long DailyShare = 10_333;
    private const long LastDayShare = 10_343;

    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _managerId = default!;
    private string _employeeId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        var response = await _client.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest(
            $"req-{Guid.NewGuid():N}", "محاسب المطعم", $"EMP-{Guid.NewGuid():N}", "محاسب", "01000000000", new DateOnly(2024, 1, 1),
            "Monthly", 0, 0, MonthlySalary, false, "cashier", null, null, null, null, null, null, null, null, _managerId));
        response.EnsureSuccessStatusCode();
        _employeeId = (await response.Content.ReadFromJsonAsync<EmployeeDto>())!.Id;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    /// <summary>Posts the accruals today for days long gone - exactly what a late catch-up does.</summary>
    private async Task AccrueThroughAsync(DateOnly through)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        await SalaryAccrual.AccrueThroughAsync(db, through, "اختبار", new[] { _employeeId }, CancellationToken.None);
        await db.SaveChangesAsync();
    }

    private async Task<long> WagesAsync(string query)
    {
        using var body = JsonDocument.Parse(await _client.GetStringAsync($"/api/reports/owner-dashboard?actorUserId={_managerId}&{query}"));
        return body.RootElement.GetProperty("profitability").GetProperty("wagesPiastres").GetInt64();
    }

    private static long Optional(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value.GetInt64() : 0;

    [Fact]
    public async Task A_catch_up_counts_each_days_salary_on_the_day_it_was_earned_not_the_day_it_was_posted()
    {
        await AccrueThroughAsync(new DateOnly(2026, 9, 20));

        Assert.Equal(DailyShare, await WagesAsync("businessDate=2026-09-10"));
        Assert.Equal(0, await WagesAsync($"businessDate={Falah.Domain.Common.BusinessClock.Today():yyyy-MM-dd}"));
    }

    [Fact]
    public async Task A_month_of_days_adds_up_to_the_month_and_the_last_day_carries_the_remainder()
    {
        await AccrueThroughAsync(new DateOnly(2026, 9, 30));

        Assert.Equal(MonthlySalary, await WagesAsync("from=2026-09-01&to=2026-09-30"));
        Assert.Equal(LastDayShare, await WagesAsync("businessDate=2026-09-30"));
        Assert.Equal(DailyShare * 3, await WagesAsync("from=2026-09-07&to=2026-09-09"));
    }

    [Fact]
    public async Task The_daily_statement_shows_the_days_salary_against_the_employee()
    {
        await AccrueThroughAsync(new DateOnly(2026, 9, 20));

        using var body = JsonDocument.Parse(await _client.GetStringAsync($"/api/reports/daily-statement?actorUserId={_managerId}&businessDate=2026-09-10"));
        var row = body.RootElement.GetProperty("employees").EnumerateArray().Single(e => e.GetProperty("employeeId").GetString() == _employeeId);
        Assert.Equal(DailyShare, Optional(row, "salaryAccruedPiastres"));
    }

    [Fact]
    public async Task The_daily_summary_of_a_business_day_includes_the_salary_earned_that_day()
    {
        await AccrueThroughAsync(new DateOnly(2026, 9, 20));
        var dayId = $"day-{Guid.NewGuid():N}";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
            db.BusinessDays.Add(new BusinessDayEntity
            {
                Id = dayId, BusinessDate = new DateOnly(2026, 9, 10), OpenedBy = "seed", Status = "Closed", CloseRevision = 1,
                OpenedAtUtc = new DateTimeOffset(2026, 9, 10, 6, 0, 0, TimeSpan.Zero), OpenRequestId = $"seed-{Guid.NewGuid():N}",
            });
            await db.SaveChangesAsync();
        }

        using var body = JsonDocument.Parse(await _client.GetStringAsync($"/api/reports/daily-summary/{dayId}?actorUserId={_managerId}"));
        Assert.Equal(DailyShare, Optional(body.RootElement.GetProperty("employeeLedger"), "salaryAccruedPiastres"));
    }

    [Fact]
    public async Task Paying_the_salary_is_not_counted_as_wage_cost_a_second_time()
    {
        await AccrueThroughAsync(new DateOnly(2026, 9, 30));
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
            db.EmployeeLedgerEntries.Add(new EmployeeLedgerEntryEntity
            {
                Id = $"led-{Guid.NewGuid():N}", EmployeeId = _employeeId, Type = "PayrollSettlement", Description = "صرف راتب سبتمبر",
                DebitPiastres = MonthlySalary, RecordedBy = "seed", CreatedAtUtc = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero),
            });
            await db.SaveChangesAsync();
        }

        Assert.Equal(MonthlySalary, await WagesAsync("from=2026-09-01&to=2026-09-30"));
    }
}

namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using System.Text.Json;
using Falah.Application.BusinessDay;
using Falah.Application.Employees;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Domain.Common;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// A monthly salary is earned a share at a time, every day, and paying it settles exactly what was
/// earned.
///
/// A monthly employee's payroll line was computed from the salary on their profile, but nothing ever
/// recorded the salary as EARNED: paying it debited their ledger with no credit to match, so after
/// payday they appeared to owe the restaurant a month's pay, dropped out of "owed to employees", and
/// the wage cost the dashboard reports never included them at all. The owner's decision: the salary
/// accrues daily - an even share of the month each day, the last day taking the remainder so a full
/// month adds up to the salary exactly - posted when the business day closes, with whatever is left
/// of the month trued up when the payroll run is paid.
/// </summary>
public class SalaryAccrualTests : IAsyncLifetime
{
    private const long MonthlySalary = 300_000;

    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _managerId = default!;
    private string _cashierId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        var database = await _factory.QueryAsync(db => Task.FromResult(db.Database.GetDbConnection().Database));
        Assert.StartsWith("FalahTest_", database);

        _client = _factory.CreateAuthenticatedClient();
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        await _factory.FundMainAccountAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Paying_a_monthly_salary_leaves_the_employee_owing_nothing_and_owed_nothing()
    {
        await OpenBusinessDayAsync();
        var employee = await CreateMonthlyEmployeeAsync();

        await PayCurrentPayrollAsync();

        Assert.Equal(0, await BalanceAsync(employee.Id));
        Assert.Equal(MonthlySalary, await SalaryEarnedAsync(employee.Id));
    }

    [Fact]
    public async Task Closing_a_business_day_accrues_the_salary_for_each_day_of_the_month_so_far()
    {
        var employee = await CreateMonthlyEmployeeAsync();
        await CloseADayAsync();

        var today = BusinessClock.Today();
        var entries = await _factory.QueryAsync(db => db.EmployeeLedgerEntries
            .Where(e => e.EmployeeId == employee.Id && e.Type == "Salary").ToListAsync());

        Assert.Equal(today.Day, entries.Count);
        Assert.Equal(ExpectedThrough(today), entries.Sum(e => e.CreditPiastres));
    }

    [Fact]
    public async Task Accrued_days_and_the_payroll_true_up_add_up_to_the_salary_exactly_once()
    {
        var employee = await CreateMonthlyEmployeeAsync();
        await CloseADayAsync();
        await OpenBusinessDayAsync();

        await PayCurrentPayrollAsync();

        Assert.Equal(MonthlySalary, await SalaryEarnedAsync(employee.Id));
        Assert.Equal(0, await BalanceAsync(employee.Id));
    }

    [Fact]
    public async Task The_days_accrued_are_part_of_the_wage_cost_the_owner_sees()
    {
        await CreateMonthlyEmployeeAsync();
        await CloseADayAsync();

        // Month to date: every accrued day counts on its own date (FIN1), so the days the close posted
        // are in the span that covers them - not all on the day they happened to be posted.
        var today = BusinessClock.Today();
        var dashboard = await _client.GetFromJsonAsync<OwnerDashboardDto>(
            $"/api/reports/owner-dashboard?actorUserId={_managerId}&from={new DateOnly(today.Year, today.Month, 1):yyyy-MM-dd}&to={today:yyyy-MM-dd}");

        Assert.Equal(ExpectedThrough(today), dashboard!.Profitability.WagesPiastres);
    }

    // ---------- Helpers ----------

    /// <summary>Days 1..date of date's month at an even share, the last day of the month taking the
    /// remainder - the owner's rule, restated independently of the code under test.</summary>
    private static long ExpectedThrough(DateOnly date)
    {
        var days = DateTime.DaysInMonth(date.Year, date.Month);
        var share = MonthlySalary / days;
        return date.Day == days ? MonthlySalary : share * date.Day;
    }

    private async Task<EmployeeDto> CreateMonthlyEmployeeAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest(
            $"req-{Guid.NewGuid():N}", "محاسب المطعم", $"EMP-{Guid.NewGuid():N}", "محاسب", "01000000000", new DateOnly(2024, 1, 1),
            "Monthly", 0, 0, MonthlySalary, false, "cashier", null, null, null, null, null, null, null, null, _managerId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EmployeeDto>())!;
    }

    private async Task<string> OpenBusinessDayAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/business-day/open", new OpenBusinessDayRequest($"bd-{Guid.NewGuid():N}", _managerId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OpenBusinessDayResult>())!.BusinessDayId;
    }

    /// <summary>A whole trading day: open it, run and close one shift, tick the closing checklist,
    /// close it.</summary>
    private async Task CloseADayAsync()
    {
        var dayId = await OpenBusinessDayAsync();
        var shift = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 0))).Content.ReadFromJsonAsync<OpenShiftResult>();
        (await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"creq-{Guid.NewGuid():N}", shift!.ShiftId, _cashierId, 0, null, null, null))).EnsureSuccessStatusCode();
        foreach (var itemId in new[] { "close-shifts", "close-drawer", "close-delivery" })
            (await _client.PostAsJsonAsync($"/api/business-day/{dayId}/checklist/{itemId}",
                new CompleteChecklistItemRequest(dayId, itemId, true, _managerId))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/business-day/{dayId}/close",
            new CloseBusinessDayRequest($"close-{Guid.NewGuid():N}", dayId, _managerId, null, null))).EnsureSuccessStatusCode();
    }

    private async Task PayCurrentPayrollAsync()
    {
        var period = BusinessClock.Today().ToString("yyyy-MM");
        var run = await (await _client.PostAsJsonAsync("/api/payroll-runs",
            new CreatePayrollRequest($"req-{Guid.NewGuid():N}", period, _managerId))).Content.ReadFromJsonAsync<PayrollRunDto>();
        foreach (var status in new[] { "Calculated", "Reviewed", "Approved" })
            (await _client.PostAsJsonAsync($"/api/payroll-runs/{run!.Id}/transition",
                new TransitionPayrollRequest(run.Id, status, _managerId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/payroll-runs/{run!.Id}/pay",
            new PayPayrollRequest(run.Id, "tre-main", _managerId, null, null))).EnsureSuccessStatusCode();
    }

    private async Task<long> BalanceAsync(string employeeId)
    {
        using var body = JsonDocument.Parse(await _client.GetStringAsync($"/api/employees/{employeeId}/balance"));
        return body.RootElement.GetProperty("balancePiastres").GetInt64();
    }

    private Task<long> SalaryEarnedAsync(string employeeId) =>
        _factory.QueryAsync(db => db.EmployeeLedgerEntries
            .Where(e => e.EmployeeId == employeeId && e.Type == "Salary").SumAsync(e => e.CreditPiastres));
}

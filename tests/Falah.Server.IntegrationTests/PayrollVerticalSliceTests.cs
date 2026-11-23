namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Employees;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Ported from createPayroll/transitionPayroll/payPayroll in lib/enterprise-store.tsx +
/// PAYROLL_TRANSITIONS/calculatePayrollLines in lib/payroll-domain.mjs. Built on top of
/// EmployeeVerticalSliceTests' own roster/ledger foundation. A real gap found during the
/// legacy-parity audit: none of this existed in the C# port before this pass.
/// </summary>
public class PayrollVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _hrUserId = default!;
    private string _managerId = default!;
    private string _payerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        // Read-only assertions use the owner session; mutation bodies still name their actual actor.
        _factory.StationUserId = await _factory.SeedUserAsync("قارئ بيانات المالك للاختبار", "owner", "all");
        _hrUserId = await _factory.SeedUserAsync("مسؤول الموارد البشرية", "supervisor", "employees_manage", "employees_ledger");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _payerId = await _factory.SeedUserAsync("صاحب صلاحية الدفع", "supervisor", "employees_manage", "payroll_approve", "payroll_pay");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<EmployeeDto> CreateEmployeeAsync(long dailyWagePiastres = 20000)
    {
        var request = new CreateEmployeeRequest(
            $"req-{Guid.NewGuid():N}", "أحمد النادل", $"EMP-{Guid.NewGuid():N}", "نادل", "01000000000", new DateOnly(2024, 1, 1),
            "Daily", dailyWagePiastres, 15000, null, false, "cashier", null, null, null, null, null, null, null, null, _hrUserId);
        var response = await _client.PostAsJsonAsync("/api/employees", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EmployeeDto>())!;
    }

    private static string CurrentPeriod() => DateTime.UtcNow.ToString("yyyy-MM");

    [Fact]
    public async Task Creating_a_payroll_run_is_idempotent_per_period()
    {
        var requestId = $"req-{Guid.NewGuid():N}";
        var period = CurrentPeriod();
        var request = new CreatePayrollRequest(requestId, period, _hrUserId);
        var first = await (await _client.PostAsJsonAsync("/api/payroll-runs", request)).Content.ReadFromJsonAsync<PayrollRunDto>();
        Assert.Equal("Draft", first!.Status);

        // A second create for the SAME period (even with a different requestId) is a safe no-op
        // that returns the existing run, matching legacy's own `if (existing) return`.
        var second = await (await _client.PostAsJsonAsync("/api/payroll-runs", new CreatePayrollRequest($"req-{Guid.NewGuid():N}", period, _hrUserId)))
            .Content.ReadFromJsonAsync<PayrollRunDto>();
        Assert.Equal(first.Id, second!.Id);
    }

    /// <summary>The sequential no-op above was already covered. Under load it was not: eight
    /// requests arriving together all read "no run for this period yet" before any of them had
    /// inserted, and the month ended up with several payroll runs, each carrying its own set of
    /// employee lines waiting to be paid.</summary>
    [Fact]
    public async Task Eight_simultaneous_creates_still_produce_one_run_for_the_month()
    {
        await CreateEmployeeAsync();
        var period = CurrentPeriod();

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            _client.PostAsJsonAsync("/api/payroll-runs", new CreatePayrollRequest($"req-{Guid.NewGuid():N}", period, _hrUserId))));

        foreach (var response in responses) response.EnsureSuccessStatusCode();
        var runs = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<PayrollRunDto>()));
        Assert.Single(runs.Select(r => r!.Id).Distinct());

        var stored = await _factory.QueryAsync(db => db.PayrollRuns
            .Where(r => r.Period == period && r.Status != "Cancelled").CountAsync());
        Assert.Equal(1, stored);
    }

    [Fact]
    public async Task Creating_a_payroll_run_requires_employees_manage()
    {
        var noPermissionUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");
        var response = await _client.PostAsJsonAsync("/api/payroll-runs", new CreatePayrollRequest($"req-{Guid.NewGuid():N}", CurrentPeriod(), noPermissionUser));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_payroll_run_computes_one_line_per_employee_with_earning_activity_this_period()
    {
        var employee = await CreateEmployeeAsync(dailyWagePiastres: 20000);
        (await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/daily-wage",
            new RecordDailyWageRequest(employee.Id, null, null, _hrUserId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/overtime",
            new RecordOvertimeRequest($"req-{Guid.NewGuid():N}", employee.Id, 2, 3000, null, _hrUserId, null, null))).EnsureSuccessStatusCode();

        var run = await (await _client.PostAsJsonAsync("/api/payroll-runs", new CreatePayrollRequest($"req-{Guid.NewGuid():N}", CurrentPeriod(), _hrUserId)))
            .Content.ReadFromJsonAsync<PayrollRunDto>();
        var lines = await (await _client.GetAsync($"/api/payroll-runs/{run!.Id}/lines")).Content.ReadFromJsonAsync<List<PayrollLineDto>>();
        var line = Assert.Single(lines!, l => l.EmployeeId == employee.Id);
        Assert.Equal(20000, line.BasePiastres);
        Assert.Equal(6000, line.OvertimePiastres);
        Assert.Equal(26000, line.NetPayablePiastres);
    }

    /// <summary>The ledger accepted "bonus" because it parsed the type case-insensitively, then
    /// stored the string exactly as sent - and payroll adds up entries whose type is "Bonus". The
    /// bonus showed on the employee's statement and was quietly left out of what they were paid.</summary>
    [Fact]
    public async Task A_bonus_entered_in_lowercase_is_still_paid()
    {
        var employee = await CreateEmployeeAsync(dailyWagePiastres: 20000);
        (await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/daily-wage",
            new RecordDailyWageRequest(employee.Id, null, null, _hrUserId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/ledger-entries",
            new RecordManualLedgerEntryRequest(employee.Id, "bonus", 5000, true, "مكافأة", _hrUserId, null, null)))
            .EnsureSuccessStatusCode();

        var run = await (await _client.PostAsJsonAsync("/api/payroll-runs", new CreatePayrollRequest($"req-{Guid.NewGuid():N}", CurrentPeriod(), _hrUserId)))
            .Content.ReadFromJsonAsync<PayrollRunDto>();
        var lines = await (await _client.GetAsync($"/api/payroll-runs/{run!.Id}/lines")).Content.ReadFromJsonAsync<List<PayrollLineDto>>();
        var line = Assert.Single(lines!, l => l.EmployeeId == employee.Id);

        Assert.Equal(5000, line.BonusesPiastres);
        Assert.Equal(25000, line.NetPayablePiastres);
    }

    // ---- Transitions ----

    private async Task<string> CreateDraftRunAsync(string period)
    {
        var run = await (await _client.PostAsJsonAsync("/api/payroll-runs", new CreatePayrollRequest($"req-{Guid.NewGuid():N}", period, _hrUserId)))
            .Content.ReadFromJsonAsync<PayrollRunDto>();
        return run!.Id;
    }

    [Fact]
    public async Task The_lifecycle_moves_draft_to_calculated_to_reviewed_to_approved_to_paid()
    {
        var runId = await CreateDraftRunAsync(CurrentPeriod());
        (await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/transition", new TransitionPayrollRequest(runId, "Calculated", _hrUserId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/transition", new TransitionPayrollRequest(runId, "Reviewed", _hrUserId, null, null))).EnsureSuccessStatusCode();

        var approved = await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/transition", new TransitionPayrollRequest(runId, "Approved", _payerId, null, null));
        approved.EnsureSuccessStatusCode();
        var approvedDto = await approved.Content.ReadFromJsonAsync<PayrollRunDto>();
        Assert.Equal("Approved", approvedDto!.Status);
        Assert.NotNull(approvedDto.ApprovedBy);
    }

    [Fact]
    public async Task Approving_a_run_requires_payroll_approve_not_just_employees_manage()
    {
        var runId = await CreateDraftRunAsync(CurrentPeriod());
        (await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/transition", new TransitionPayrollRequest(runId, "Calculated", _hrUserId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/transition", new TransitionPayrollRequest(runId, "Reviewed", _hrUserId, null, null))).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/transition", new TransitionPayrollRequest(runId, "Approved", _hrUserId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task An_illegal_transition_is_rejected()
    {
        var runId = await CreateDraftRunAsync(CurrentPeriod());
        var response = await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/transition", new TransitionPayrollRequest(runId, "Approved", _payerId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---- Payment ----

    private async Task<string> CreateApprovedRunWithEmployeeAsync(string period, EmployeeDto employee)
    {
        (await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/daily-wage",
            new RecordDailyWageRequest(employee.Id, null, null, _hrUserId, null, null))).EnsureSuccessStatusCode();
        var runId = await CreateDraftRunAsync(period);
        (await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/transition", new TransitionPayrollRequest(runId, "Calculated", _hrUserId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/transition", new TransitionPayrollRequest(runId, "Reviewed", _hrUserId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/transition", new TransitionPayrollRequest(runId, "Approved", _payerId, null, null))).EnsureSuccessStatusCode();
        return runId;
    }

    [Fact]
    public async Task Paying_an_approved_run_posts_one_movement_and_settles_every_line()
    {
        var employee = await CreateEmployeeAsync(dailyWagePiastres: 20000);
        var runId = await CreateApprovedRunWithEmployeeAsync(CurrentPeriod(), employee);

        var response = await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/pay", new PayPayrollRequest(runId, "tre-main", _payerId, null, null));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PayPayrollResult>();
        Assert.False(result!.WasReplayed);
        Assert.Equal(20000, result.TotalPaidPiastres);
        Assert.Equal(1, result.SettlementCount);

        var balance = await (await _client.GetAsync($"/api/employees/{employee.Id}/balance")).Content.ReadFromJsonAsync<BalanceResponse>();
        Assert.Equal(0, balance!.balancePiastres); // credited 20000 by daily wage, debited 20000 by the settlement

        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Id == result.TreasuryMovementId && m.Type == "payroll_payment"));
        Assert.Equal(1, movementCount);
    }

    // PAYR-0007, owner decision 2026-11-22: a monthly employee hired during the period is paid for the
    // days employed, the same daily shares the salary accrual posts.
    [Fact]
    public async Task A_monthly_employee_hired_mid_period_is_paid_for_the_days_employed()
    {
        var periodStart = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var hired = periodStart.AddDays(10);
        var request = new CreateEmployeeRequest(
            $"req-{Guid.NewGuid():N}", "محاسب", $"EMP-{Guid.NewGuid():N}", "محاسب", "01000000000", hired,
            "Monthly", 0, 0, 600000, false, "cashier", null, null, null, null, null, null, null, null, _hrUserId);
        var employee = (await (await _client.PostAsJsonAsync("/api/employees", request)).Content.ReadFromJsonAsync<EmployeeDto>())!;

        var runId = await CreateDraftRunAsync(CurrentPeriod());
        var line = Assert.Single((await (await _client.GetAsync($"/api/payroll-runs/{runId}/lines")).Content.ReadFromJsonAsync<List<PayrollLineDto>>())!,
            l => l.EmployeeId == employee.Id);
        var expected = Enumerable.Range(0, periodEnd.DayNumber - hired.DayNumber + 1)
            .Sum(i => Falah.Domain.Payroll.PayrollRules.DailySalaryAccrual(Falah.Domain.Common.Money.FromPiastres(600000), hired.AddDays(i)).Piastres);
        Assert.Equal(expected, line.BasePiastres);
        Assert.True(line.BasePiastres < 600000);
    }

    private async Task PayOutsideTheRunAsync(string employeeId, long amountPiastres) =>
        (await _client.PostAsJsonAsync($"/api/employees/{employeeId}/pay",
            new PayEmployeeRequest($"pay-{Guid.NewGuid():N}", employeeId, amountPiastres, "tre-main", _managerId, "دفعة من المستحقات", null, null)))
            .EnsureSuccessStatusCode();

    // PAYR-0004, owner decision 2026-11-22: a mid-month "دفع مستحقات" comes off the run.
    [Fact]
    public async Task A_run_created_after_a_payout_pays_only_what_is_still_owed()
    {
        var employee = await CreateEmployeeAsync(dailyWagePiastres: 20000);
        (await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/daily-wage",
            new RecordDailyWageRequest(employee.Id, null, null, _hrUserId, null, null))).EnsureSuccessStatusCode();
        await PayOutsideTheRunAsync(employee.Id, 15000);

        var runId = await CreateDraftRunAsync(CurrentPeriod());
        var line = Assert.Single((await (await _client.GetAsync($"/api/payroll-runs/{runId}/lines")).Content.ReadFromJsonAsync<List<PayrollLineDto>>())!);
        Assert.Equal(15000, line.PaidOutsidePayrollPiastres);
        Assert.Equal(5000, line.NetPayablePiastres);

        foreach (var (status, actor) in new[] { ("Calculated", _hrUserId), ("Reviewed", _hrUserId), ("Approved", _payerId) })
            (await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/transition", new TransitionPayrollRequest(runId, status, actor, null, null))).EnsureSuccessStatusCode();
        var paid = await (await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/pay", new PayPayrollRequest(runId, "tre-main", _payerId, null, null)))
            .Content.ReadFromJsonAsync<PayPayrollResult>();
        Assert.Equal(5000, paid!.TotalPaidPiastres);
        var balance = await (await _client.GetAsync($"/api/employees/{employee.Id}/balance")).Content.ReadFromJsonAsync<BalanceResponse>();
        Assert.Equal(0, balance!.balancePiastres);
    }

    // PAYR-0004: a payout between creating and paying the run is not paid a second time either.
    [Fact]
    public async Task A_payout_made_after_the_run_was_created_is_not_paid_again()
    {
        var employee = await CreateEmployeeAsync(dailyWagePiastres: 20000);
        var runId = await CreateApprovedRunWithEmployeeAsync(CurrentPeriod(), employee);
        await PayOutsideTheRunAsync(employee.Id, 15000);

        var paid = await (await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/pay", new PayPayrollRequest(runId, "tre-main", _payerId, null, null)))
            .Content.ReadFromJsonAsync<PayPayrollResult>();
        Assert.Equal(5000, paid!.TotalPaidPiastres);
        var line = Assert.Single((await (await _client.GetAsync($"/api/payroll-runs/{runId}/lines")).Content.ReadFromJsonAsync<List<PayrollLineDto>>())!);
        Assert.Equal(5000, line.NetPayablePiastres);
        Assert.Equal(15000, line.PaidOutsidePayrollPiastres);
        var balance = await (await _client.GetAsync($"/api/employees/{employee.Id}/balance")).Content.ReadFromJsonAsync<BalanceResponse>();
        Assert.Equal(0, balance!.balancePiastres);
    }

    [Fact]
    public async Task Paying_the_same_run_twice_never_double_pays()
    {
        var employee = await CreateEmployeeAsync(dailyWagePiastres: 20000);
        var runId = await CreateApprovedRunWithEmployeeAsync(CurrentPeriod(), employee);

        var first = await (await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/pay", new PayPayrollRequest(runId, "tre-main", _payerId, null, null)))
            .Content.ReadFromJsonAsync<PayPayrollResult>();
        var second = await (await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/pay", new PayPayrollRequest(runId, "tre-main", _payerId, null, null)))
            .Content.ReadFromJsonAsync<PayPayrollResult>();
        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(first.TreasuryMovementId, second.TreasuryMovementId);

        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "payroll_payment"));
        Assert.Equal(1, movementCount);
    }

    /// <summary>The generic transition accepted Approved → Paid and only changed the status: no
    /// treasury movement, no settlement, nobody paid. Paying it afterwards was then refused because
    /// the run was no longer Approved, so it stayed "Paid" with nothing paid for good.</summary>
    [Fact]
    public async Task Marking_a_run_paid_by_a_status_change_is_refused_and_only_paying_it_settles_it()
    {
        var employee = await CreateEmployeeAsync(dailyWagePiastres: 20000);
        var runId = await CreateApprovedRunWithEmployeeAsync(CurrentPeriod(), employee);

        var transition = await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/transition", new TransitionPayrollRequest(runId, "Paid", _payerId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, transition.StatusCode);

        var untouched = await _factory.QueryAsync(db => db.PayrollRuns.AsNoTracking().SingleAsync(r => r.Id == runId));
        Assert.Equal("Approved", untouched.Status);
        Assert.Null(untouched.PaidAtUtc);
        Assert.Null(untouched.TreasuryMovementId);
        Assert.Equal(0, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "payroll_payment")));
        Assert.Equal(0, await _factory.QueryAsync(db => db.EmployeeLedgerEntries.CountAsync(e => e.Type == "PayrollSettlement")));

        var pay = await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/pay", new PayPayrollRequest(runId, "tre-main", _payerId, null, null));
        pay.EnsureSuccessStatusCode();
        var result = await pay.Content.ReadFromJsonAsync<PayPayrollResult>();
        Assert.False(result!.WasReplayed);

        var paid = await _factory.QueryAsync(db => db.PayrollRuns.AsNoTracking().SingleAsync(r => r.Id == runId));
        Assert.Equal("Paid", paid.Status);
        Assert.Equal(result.TreasuryMovementId, paid.TreasuryMovementId);
        Assert.Equal(1, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "payroll_payment")));
    }

    [Fact]
    public async Task Paying_a_run_that_is_not_approved_is_rejected()
    {
        var employee = await CreateEmployeeAsync();
        var runId = await CreateDraftRunAsync(CurrentPeriod());
        var response = await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/pay", new PayPayrollRequest(runId, "tre-main", _payerId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Paying_a_run_requires_payroll_pay()
    {
        var employee = await CreateEmployeeAsync(dailyWagePiastres: 20000);
        var runId = await CreateApprovedRunWithEmployeeAsync(CurrentPeriod(), employee);
        var response = await _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/pay", new PayPayrollRequest(runId, "tre-main", _hrUserId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Six_concurrent_pay_requests_for_the_same_run_settle_exactly_once()
    {
        var employee = await CreateEmployeeAsync(dailyWagePiastres: 20000);
        var runId = await CreateApprovedRunWithEmployeeAsync(CurrentPeriod(), employee);

        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync($"/api/payroll-runs/{runId}/pay", new PayPayrollRequest(runId, "tre-main", _payerId, null, null)));
        var responses = await Task.WhenAll(tasks);
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));

        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "payroll_payment"));
        Assert.Equal(1, movementCount);
        var settlementCount = await _factory.QueryAsync(db => db.EmployeeLedgerEntries.CountAsync(e => e.Type == "PayrollSettlement"));
        Assert.Equal(1, settlementCount);
    }

    private sealed record BalanceResponse(string employeeId, long balancePiastres);

    [Fact]
    public async Task Payroll_months_use_the_same_Cairo_dates_as_employee_reports()
    {
        var employee = await CreateEmployeeAsync();
        var credited = await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/daily-wage", new RecordDailyWageRequest(employee.Id, null, null, _hrUserId, null, null));
        credited.EnsureSuccessStatusCode();
        await _factory.QueryAsync(db => db.EmployeeLedgerEntries.Where(e => e.EmployeeId == employee.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.CreatedAtUtc, DateTimeOffset.Parse("2026-10-31T22:30:00Z"))));
        foreach (var period in new[] { "2026-10", "2026-11" })
        {
            var response = await _client.PostAsJsonAsync("/api/payroll-runs", new CreatePayrollRequest(Guid.NewGuid().ToString("N"), period, _hrUserId));
            response.EnsureSuccessStatusCode();
            var run = (await response.Content.ReadFromJsonAsync<PayrollRunDto>())!;
            var line = await _factory.QueryAsync(db => db.PayrollLines.SingleAsync(l => l.PayrollRunId == run.Id && l.EmployeeId == employee.Id));
            Assert.Equal(period == "2026-11" ? 20000 : 0, line.NetPayablePiastres);
        }
    }
    [Theory]
    [InlineData("2026-00")]
    [InlineData("2026-13")]
    [InlineData("0000-01")]
    public async Task An_impossible_payroll_month_is_refused_before_a_run_is_saved(string period)
    {
        var response = await _client.PostAsJsonAsync("/api/payroll-runs", new CreatePayrollRequest(Guid.NewGuid().ToString("N"), period, _hrUserId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.False(await _factory.QueryAsync(db => db.PayrollRuns.AnyAsync(r => r.Period == period)));
    }

}

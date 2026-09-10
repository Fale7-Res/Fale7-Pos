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
}

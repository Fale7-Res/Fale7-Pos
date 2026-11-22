namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Employees;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Ported from addEmployee/updateEmployee/calculateEmployeeBalance/recordManualLedgerEntry/
/// recordAdvance/recordWithdrawal in lib/store.tsx - the roster + financial-identity half of the
/// Employees module (Attendance/DailyWages/Payroll build on top of this in later slices). A real
/// gap found during the legacy-parity audit: none of this existed in the C# port at all before this
/// pass, despite being genuine functional data (not a mockup like Device Management).
/// </summary>
public class EmployeeVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _managerId = default!;
    private string _hrUserId = default!;
    private string _cashierId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _hrUserId = await _factory.SeedUserAsync("مسؤول الموارد البشرية", "supervisor", "employees_manage", "employees_ledger");
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<EmployeeDto> CreateEmployeeAsync(string name = "أحمد النادل", string employeeNumber = "EMP-1")
    {
        var request = new CreateEmployeeRequest(
            $"req-{Guid.NewGuid():N}", name, employeeNumber, "نادل", "01000000000", new DateOnly(2024, 1, 1),
            "Daily", 20000, 15000, null, false, "cashier", null, null, null, null, null, null, null, null, _hrUserId);
        var response = await _client.PostAsJsonAsync("/api/employees", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EmployeeDto>())!;
    }

    [Fact]
    public async Task Creating_an_employee_succeeds_and_is_idempotent_by_request_id()
    {
        var requestId = $"req-{Guid.NewGuid():N}";
        var request = new CreateEmployeeRequest(
            requestId, "سارة الكاشير", "EMP-2", "كاشير", "01111111111", new DateOnly(2024, 3, 1),
            "Monthly", 0, 0, 800000, false, "cashier", null, null, null, null, null, null, null, null, _hrUserId);
        var first = await (await _client.PostAsJsonAsync("/api/employees", request)).Content.ReadFromJsonAsync<EmployeeDto>();
        var second = await (await _client.PostAsJsonAsync("/api/employees", request)).Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.Equal(first!.Id, second!.Id);
        Assert.True(first.IsActive);
        Assert.Null(first.UserId);
    }

    [Fact]
    public async Task Creating_an_employee_without_employees_manage_is_rejected()
    {
        var noPermissionUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");
        var request = new CreateEmployeeRequest(
            $"req-{Guid.NewGuid():N}", "بدون صلاحية", "EMP-3", "نادل", "01000000000", new DateOnly(2024, 1, 1),
            "Daily", 20000, 15000, null, false, "cashier", null, null, null, null, null, null, null, null, noPermissionUser);
        var response = await _client.PostAsJsonAsync("/api/employees", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Updating_an_employee_never_changes_its_user_link()
    {
        var employee = await CreateEmployeeAsync();
        var update = new UpdateEmployeeRequest(
            employee.Id, "أحمد بعد التعديل", employee.EmployeeNumber, "نادل أول", employee.Phone, employee.HireDate,
            employee.CompensationType, 25000, employee.ShiftValuePiastres, employee.MonthlySalaryPiastres,
            true, employee.Role, employee.IsActive, null, null, null, null, null, null, null, null, _hrUserId);
        var updated = await (await _client.PutAsJsonAsync($"/api/employees/{employee.Id}", update)).Content.ReadFromJsonAsync<EmployeeDto>();
        Assert.Equal("أحمد بعد التعديل", updated!.Name);
        Assert.Equal(25000, updated.DailyWagePiastres);
        Assert.True(updated.CanDeliver);
        Assert.Null(updated.UserId);
    }

    // ---- Ledger ----

    // PERF-0006 (2026-11-22): the employees screen asked for one balance per employee; this answers for
    // all of them in one read, the same sum(credit) - sum(debit) as the single-employee balance.
    [Fact]
    public async Task Every_employees_balance_comes_back_in_one_read()
    {
        var first = await CreateEmployeeAsync("أحمد", "EMP-B1");
        var second = await CreateEmployeeAsync("محمود", "EMP-B2");
        await _client.PostAsJsonAsync($"/api/employees/{first.Id}/ledger-entries",
            new RecordManualLedgerEntryRequest(first.Id, "DailyWage", 20000, true, "يومية", _hrUserId, null, null));
        await _client.PostAsJsonAsync($"/api/employees/{first.Id}/ledger-entries",
            new RecordManualLedgerEntryRequest(first.Id, "Deduction", 5000, false, "خصم تأخير", _hrUserId, null, null));

        var balances = await (await _client.GetAsync("/api/employees/balances")).Content.ReadFromJsonAsync<Dictionary<string, long>>();
        Assert.Equal(15000, balances![first.Id]);
        Assert.Equal(0, balances.GetValueOrDefault(second.Id));
    }

    [Fact]
    public async Task A_manual_credit_and_debit_compute_the_correct_running_balance()
    {
        var employee = await CreateEmployeeAsync();
        await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/ledger-entries",
            new RecordManualLedgerEntryRequest(employee.Id, "DailyWage", 20000, true, "يومية", _hrUserId, null, null));
        await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/ledger-entries",
            new RecordManualLedgerEntryRequest(employee.Id, "Deduction", 5000, false, "خصم تأخير", _hrUserId, null, null));

        var balance = await (await _client.GetAsync($"/api/employees/{employee.Id}/balance")).Content.ReadFromJsonAsync<BalanceResponse>();
        Assert.Equal(15000, balance!.balancePiastres);

        var entries = await (await _client.GetAsync($"/api/employees/{employee.Id}/ledger")).Content.ReadFromJsonAsync<List<EmployeeLedgerEntryDto>>();
        Assert.Equal(2, entries!.Count);
    }

    [Fact]
    public async Task Recording_a_manual_ledger_entry_requires_employees_ledger()
    {
        var employee = await CreateEmployeeAsync();
        var noPermissionUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");
        var response = await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/ledger-entries",
            new RecordManualLedgerEntryRequest(employee.Id, "Bonus", 5000, true, "مكافأة", noPermissionUser, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---- Advance / withdrawal (postEmployeeCashOut) ----

    [Fact]
    public async Task An_advance_posts_a_treasury_movement_from_the_drawer_and_debits_the_ledger()
    {
        var employee = await CreateEmployeeAsync();
        var response = await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/cash-out",
            new RecordEmployeeCashOutRequest($"req-{Guid.NewGuid():N}", employee.Id, "advance", 10000, "سلفة عاجلة", _shiftId, _hrUserId, null, null));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EmployeeCashOutResult>();
        Assert.False(result!.WasReplayed);
        Assert.Equal(-10000, result.BalanceAfterPiastres);
        Assert.NotNull(result.TreasuryMovementId);

        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Id == result.TreasuryMovementId && m.Type == "employee_advance"));
        Assert.Equal(1, movementCount);
    }

    [Fact]
    public async Task Retrying_the_same_advance_request_never_posts_twice()
    {
        var employee = await CreateEmployeeAsync();
        var requestId = $"req-{Guid.NewGuid():N}";
        var request = new RecordEmployeeCashOutRequest(requestId, employee.Id, "advance", 10000, "سلفة", _shiftId, _hrUserId, null, null);
        var first = await (await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/cash-out", request)).Content.ReadFromJsonAsync<EmployeeCashOutResult>();
        var second = await (await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/cash-out", request)).Content.ReadFromJsonAsync<EmployeeCashOutResult>();
        Assert.True(second!.WasReplayed);
        Assert.Equal(first!.LedgerEntryId, second.LedgerEntryId);

        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == $"employee-advance-led-advance-{requestId}"));
        Assert.Equal(1, movementCount);
    }

    [Fact]
    public async Task Six_concurrent_advance_requests_with_the_same_request_id_post_exactly_once()
    {
        var employee = await CreateEmployeeAsync();
        var request = new RecordEmployeeCashOutRequest($"req-{Guid.NewGuid():N}", employee.Id, "advance", 5000, "سلفة", _shiftId, _hrUserId, null, null);
        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync($"/api/employees/{employee.Id}/cash-out", request));
        var responses = await Task.WhenAll(tasks);
        foreach (var response in responses)
            Assert.True(response.IsSuccessStatusCode, $"unexpected status {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        var entryCount = await _factory.QueryAsync(db => db.EmployeeLedgerEntries.CountAsync(e => e.EmployeeId == employee.Id));
        Assert.Equal(1, entryCount);
    }

    [Fact]
    public async Task A_withdrawal_cannot_exceed_the_employees_current_balance()
    {
        var employee = await CreateEmployeeAsync();
        await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/ledger-entries",
            new RecordManualLedgerEntryRequest(employee.Id, "DailyWage", 10000, true, "يومية", _hrUserId, null, null));

        var response = await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/cash-out",
            new RecordEmployeeCashOutRequest($"req-{Guid.NewGuid():N}", employee.Id, "withdrawal", 20000, "سحب", _shiftId, _hrUserId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [InlineData("advance")]
    [InlineData("withdrawal")]
    public async Task Concurrent_cash_out_replays_do_not_reconsume_approval_or_recheck_spent_balances(string kind)
    {
        var employee = await CreateEmployeeAsync();
        if (kind == "withdrawal")
            (await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/ledger-entries",
                new RecordManualLedgerEntryRequest(employee.Id, "DailyWage", 50000, true, "رصيد مستحق", _hrUserId, null, null))).EnsureSuccessStatusCode();
        // Asked for at the cashier's till, which spends it: an approval is spent only by the session that asked.
        var issued = await _factory.PostAsSessionOfAsync(_client, _cashierId, "/api/approvals/issue",
            new Falah.Application.Approvals.IssueManagerApprovalRequest(_factory.PinFor(_managerId), "employees_ledger", "اعتماد صرف"));
        issued.EnsureSuccessStatusCode();
        var approval = await issued.Content.ReadFromJsonAsync<Falah.Application.Approvals.IssueManagerApprovalResult>();
        // The first commit consumes the approval and empties the drawer (and, for a
        // withdrawal, the employee balance). All duplicates must return that commit.
        var request = new RecordEmployeeCashOutRequest($"req-{Guid.NewGuid():N}", employee.Id, kind,
            50000, "اعتماد صرف", _shiftId, _cashierId, approval!.ApprovalId, _managerId);
        var responses = await Task.WhenAll(Enumerable.Range(0, 6)
            .Select(_ => _client.PostAsJsonAsync($"/api/employees/{employee.Id}/cash-out", request)));
        var results = new List<EmployeeCashOutResult>();
        foreach (var response in responses)
        {
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            results.Add((await response.Content.ReadFromJsonAsync<EmployeeCashOutResult>())!);
        }
        Assert.Single(results, r => !r.WasReplayed);
        Assert.Single(results.Select(r => r.LedgerEntryId).Distinct());
        Assert.Equal(1, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Id == results[0].TreasuryMovementId)));
    }

    [Fact]
    public async Task A_withdrawal_within_balance_succeeds()
    {
        var employee = await CreateEmployeeAsync();
        await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/ledger-entries",
            new RecordManualLedgerEntryRequest(employee.Id, "DailyWage", 10000, true, "يومية", _hrUserId, null, null));

        var response = await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/cash-out",
            new RecordEmployeeCashOutRequest($"req-{Guid.NewGuid():N}", employee.Id, "withdrawal", 4000, "سحب", _shiftId, _hrUserId, null, null));
        response.EnsureSuccessStatusCode();
        var balance = await (await _client.GetAsync($"/api/employees/{employee.Id}/balance")).Content.ReadFromJsonAsync<BalanceResponse>();
        Assert.Equal(6000, balance!.balancePiastres);
    }

    [Fact]
    public async Task An_advance_requires_an_open_shift()
    {
        var employee = await CreateEmployeeAsync();
        (await _client.PostAsJsonAsync("/api/shifts/close",
            new Falah.Application.Shifts.CloseShiftRequest($"creq-{Guid.NewGuid():N}", _shiftId, _cashierId, 50000, null, null, null))).EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/cash-out",
            new RecordEmployeeCashOutRequest($"req-{Guid.NewGuid():N}", employee.Id, "advance", 5000, "سلفة", _shiftId, _hrUserId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task An_advance_requires_employees_ledger_not_just_employees_manage()
    {
        var employee = await CreateEmployeeAsync();
        var response = await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/cash-out",
            new RecordEmployeeCashOutRequest($"req-{Guid.NewGuid():N}", employee.Id, "advance", 5000, "سلفة", _shiftId, _cashierId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private sealed record BalanceResponse(string employeeId, long balancePiastres);

    [Theory]
    [InlineData("monthly", "Monthly")]
    [InlineData("daily", "Daily")]
    public async Task Compensation_names_are_canonical_on_create_and_update(string input, string expected)
    {
        var request = new CreateEmployeeRequest($"case-{Guid.NewGuid():N}", "عامل", "CASE", "عمل", "01000000000", new DateOnly(2024, 1, 1), input, 20000, 15000, 800000, false, "cashier", null, null, null, null, null, null, null, null, _hrUserId);
        var response = await _client.PostAsJsonAsync("/api/employees", request);
        response.EnsureSuccessStatusCode();
        var emp = (await response.Content.ReadFromJsonAsync<EmployeeDto>())!;
        Assert.Equal(expected, emp.CompensationType);
        var update = new UpdateEmployeeRequest(emp.Id, emp.Name, emp.EmployeeNumber, emp.JobTitle, emp.Phone, emp.HireDate,
            input, emp.DailyWagePiastres, emp.ShiftValuePiastres, emp.MonthlySalaryPiastres, false, emp.Role, true, null, null, null, null, null, null, null, null, _hrUserId);
        var changed = await _client.PutAsJsonAsync($"/api/employees/{emp.Id}", update);
        changed.EnsureSuccessStatusCode();
        Assert.Equal(expected, (await changed.Content.ReadFromJsonAsync<EmployeeDto>())!.CompensationType);
    }

    [Fact]
    public async Task Undefined_numeric_compensation_type_is_refused()
    {
        var request = new CreateEmployeeRequest($"case-{Guid.NewGuid():N}", "عامل", "CASE", "عمل", "01000000000", new DateOnly(2024, 1, 1), "999", 20000, 15000, null, false, "cashier", null, null, null, null, null, null, null, null, _hrUserId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _client.PostAsJsonAsync("/api/employees", request)).StatusCode);
    }


    [Fact]
    public async Task Undefined_numeric_ledger_type_is_refused()
    {
        var emp = await CreateEmployeeAsync();
        var response = await _client.PostAsJsonAsync($"/api/employees/{emp.Id}/ledger-entries", new RecordManualLedgerEntryRequest(emp.Id, "42", 5000, true, "اختبار", _hrUserId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await _factory.QueryAsync(db => db.EmployeeLedgerEntries.CountAsync(e => e.EmployeeId == emp.Id)));
    }


    [Fact]
    public async Task Manual_ledger_post_waits_for_employee_lock_and_uses_the_committed_balance()
    {
        var employee = await CreateEmployeeAsync();
        using var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(_factory.Services);
        var db = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Falah.Infrastructure.Persistence.FalahDbContext>(scope.ServiceProvider);
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Employees.FromSqlRaw("SELECT * FROM Employees WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", employee.Id).Select(e=>e.Id).SingleAsync();
        var post = _client.PostAsJsonAsync($"/api/employees/{employee.Id}/ledger-entries",
            new RecordManualLedgerEntryRequest(employee.Id, "Bonus", 1000, true, "بعد الحركة المتزامنة", _hrUserId, null, null));
        await Task.WhenAny(post, Task.Delay(800));
        var waited = !post.IsCompleted;
        db.EmployeeLedgerEntries.Add(new Falah.Infrastructure.Persistence.Entities.EmployeeLedgerEntryEntity {
            Id = "held-credit", EmployeeId = employee.Id, Type = "Bonus", Description = "حركة تحت القفل",
            CreditPiastres = 3000, BalanceAfterPiastres = 3000, RecordedBy = "fixture", CreatedAtUtc = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(); await tx.CommitAsync();
        var response = await post; response.EnsureSuccessStatusCode();
        Assert.True(waited, "Manual ledger writer bypassed the employee serialization lock.");
        Assert.Equal(4000, (await response.Content.ReadFromJsonAsync<EmployeeLedgerEntryDto>())!.BalanceAfterPiastres);
    }

    [Fact]
    public async Task Concurrent_manual_ledger_credits_have_every_running_balance_once()
    {
        var employee = await CreateEmployeeAsync();
        var responses = await Task.WhenAll(Enumerable.Range(0,8).Select(i=>_client.PostAsJsonAsync($"/api/employees/{employee.Id}/ledger-entries",
            new RecordManualLedgerEntryRequest(employee.Id, "Bonus", 1000, true, $"مكافأة {i}", _hrUserId, null, null))));
        var entries = new List<EmployeeLedgerEntryDto>();
        foreach(var response in responses) { response.EnsureSuccessStatusCode(); entries.Add((await response.Content.ReadFromJsonAsync<EmployeeLedgerEntryDto>())!); }
        Assert.Equal(Enumerable.Range(1,8).Select(i=>(long)i*1000), entries.Select(e=>e.BalanceAfterPiastres).OrderBy(x=>x));
    }

}

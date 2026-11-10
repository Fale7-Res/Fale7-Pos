namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Employees;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// The standalone "PAY EMPLOYEE" flow - final pre-go-live business rule. The authoritative model
/// stays the continuous Employee Ledger balance; this proves it can now be paid down at any time, in
/// any amount up to that balance, without a payroll run, and that the existing Advance flow keeps
/// working exactly as before, distinct from a payment, never merged or renamed.
/// </summary>
public class PayEmployeeVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _managerId = default!;
    private string _hrUserId = default!;
    private string _cashierId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _hrUserId = await _factory.SeedUserAsync("الموارد البشرية", "supervisor", "employees_manage", "employees_ledger");
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<EmployeeDto> CreateEmployeeAsync(string name = "علي النادل", string employeeNumber = "EMP-PAY-1")
    {
        var request = new CreateEmployeeRequest(
            $"req-{Guid.NewGuid():N}", name, employeeNumber, "نادل", "01000000000", new DateOnly(2024, 1, 1),
            "Daily", 20000, 15000, null, false, "cashier", null, null, null, null, null, null, null, null, _hrUserId);
        var response = await _client.PostAsJsonAsync("/api/employees", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EmployeeDto>())!;
    }

    private async Task CreditAsync(string employeeId, long amountPiastres, string type = "DailyWage") =>
        (await _client.PostAsJsonAsync($"/api/employees/{employeeId}/ledger-entries",
            new RecordManualLedgerEntryRequest(employeeId, type, amountPiastres, true, "تحصيل اختبار", _hrUserId, null, null)))
            .EnsureSuccessStatusCode();

    private Task<HttpResponseMessage> PayAsync(
        string employeeId, long amountPiastres, string account = "tre-main", string? requestId = null,
        string actorId = "", string? reason = "دفعة اختبار", string? approvalId = null, string? approverId = null) =>
        _client.PostAsJsonAsync($"/api/employees/{employeeId}/pay",
            new PayEmployeeRequest(requestId ?? $"pay-{Guid.NewGuid():N}", employeeId, amountPiastres, account,
                string.IsNullOrEmpty(actorId) ? _hrUserId : actorId, reason, approvalId, approverId));

    private async Task<long> GetBalanceAsync(string employeeId) =>
        (await (await _client.GetAsync($"/api/employees/{employeeId}/balance")).Content.ReadFromJsonAsync<BalanceResponse>())!.balancePiastres;

    private sealed record BalanceResponse(string employeeId, long balancePiastres);

    // ---- Core running-balance / partial payment ----

    [Fact]
    public async Task Paying_partially_leaves_exactly_the_expected_remaining_balance()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 5300);

        var response = await PayAsync(employee.Id, 2000);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PayEmployeeResult>();

        Assert.Equal(3300, result!.BalanceAfterPiastres); // 5300 - 2000, exactly the user's own worked example
        Assert.Equal(3300, await GetBalanceAsync(employee.Id));
    }

    [Fact]
    public async Task Future_earnings_keep_accumulating_on_top_of_a_remaining_balance_after_a_partial_payment()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 5300);
        (await PayAsync(employee.Id, 2000)).EnsureSuccessStatusCode();
        Assert.Equal(3300, await GetBalanceAsync(employee.Id));

        await CreditAsync(employee.Id, 1000); // a new day's wage, posted after the payment
        Assert.Equal(4300, await GetBalanceAsync(employee.Id)); // adds on top of the remaining balance, never resets it
    }

    [Fact]
    public async Task Paying_the_full_balance_zeroes_it_out()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 4000);
        (await PayAsync(employee.Id, 4000)).EnsureSuccessStatusCode();
        Assert.Equal(0, await GetBalanceAsync(employee.Id));
    }

    [Fact]
    public async Task Paying_more_than_the_current_balance_is_rejected()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 1000);
        var response = await PayAsync(employee.Id, 1001);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(1000, await GetBalanceAsync(employee.Id)); // untouched by the rejected attempt
    }

    // ---- Idempotency / concurrency ----

    [Fact]
    public async Task Retrying_the_same_payment_request_id_never_pays_twice()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 5000);
        var requestId = $"pay-{Guid.NewGuid():N}";

        var first = await (await PayAsync(employee.Id, 2000, requestId: requestId)).Content.ReadFromJsonAsync<PayEmployeeResult>();
        var second = await (await PayAsync(employee.Id, 2000, requestId: requestId)).Content.ReadFromJsonAsync<PayEmployeeResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(first.LedgerEntryId, second.LedgerEntryId);
        Assert.Equal(3000, await GetBalanceAsync(employee.Id)); // debited once, not twice

        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == first.LedgerEntryId));
        Assert.Equal(1, movementCount);
    }

    [Fact]
    public async Task Concurrent_double_click_payments_with_the_same_request_id_post_exactly_once()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 5000);
        var requestId = $"pay-{Guid.NewGuid():N}";

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => PayAsync(employee.Id, 2000, requestId: requestId)));
        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));

        Assert.Equal(3000, await GetBalanceAsync(employee.Id));
        var ledgerCount = await _factory.QueryAsync(db => db.EmployeeLedgerEntries.CountAsync(e => e.Id == $"led-employee-payment-{requestId}"));
        Assert.Equal(1, ledgerCount);
    }

    [Fact]
    public async Task Two_different_payment_requests_for_the_same_employee_never_both_read_the_same_stale_balance()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 3000);

        // Two DIFFERENT request ids, both asking for the full 3000 - only one may legitimately win;
        // the balance lock must serialize them so the loser sees the post-first-payment balance.
        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => PayAsync(employee.Id, 3000)));
        var succeeded = responses.Count(r => r.IsSuccessStatusCode);
        Assert.Equal(1, succeeded); // the second must be refused as overpayment, not silently accepted
        Assert.Equal(0, await GetBalanceAsync(employee.Id));
    }

    // ---- Authorization / accounts ----

    [Fact]
    public async Task Paying_without_employees_ledger_permission_is_rejected()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 1000);
        var noPermissionUser = await _factory.SeedUserAsync("بدون صلاحية", "cashier", "pos");
        var response = await PayAsync(employee.Id, 500, actorId: noPermissionUser);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Paying_from_the_drawer_account_is_rejected_that_flow_stays_withdrawal_only()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 1000);
        var response = await PayAsync(employee.Id, 500, account: "tre-drawer");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Paying_from_bank_or_instapay_is_accepted_same_account_set_as_payroll()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 4000);
        // SeedOpenBusinessDayWithFundedMainAsync only funds tre-main - bank/instapay need their own
        // deposit before a payment can actually be drawn from them.
        foreach (var account in new[] { "tre-bank", "tre-instapay" })
            (await _client.PostAsJsonAsync("/api/treasury/deposit",
                new Falah.Application.Treasury.DepositRequest($"req-{Guid.NewGuid():N}", "owner_deposit", 10000, account, "تمويل اختبار", null, _managerId, null, null)))
                .EnsureSuccessStatusCode();

        (await PayAsync(employee.Id, 1000, account: "tre-bank")).EnsureSuccessStatusCode();
        (await PayAsync(employee.Id, 1000, account: "tre-instapay")).EnsureSuccessStatusCode();
        Assert.Equal(2000, await GetBalanceAsync(employee.Id));
    }

    // ---- Treasury reconciliation / audit ----

    [Fact]
    public async Task A_payment_posts_exactly_one_treasury_movement_debiting_the_chosen_account()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 5000);
        var result = await (await PayAsync(employee.Id, 2000, account: "tre-main")).Content.ReadFromJsonAsync<PayEmployeeResult>();

        var movement = await _factory.QueryAsync(db => db.TreasuryMovements.SingleAsync(m => m.Id == result!.TreasuryMovementId));
        Assert.Equal("tre-main", movement.SourceAccountId);
        Assert.Null(movement.DestinationAccountId);
        Assert.Equal(2000, movement.AmountPiastres);
        Assert.Equal("employee_payment", movement.Type);
    }

    [Fact]
    public async Task A_payment_is_audited_as_its_own_action()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 3000);
        (await PayAsync(employee.Id, 1000)).EnsureSuccessStatusCode();

        var events = await (await _client.GetAsync($"/api/audit/events?actorUserId={_managerId}&category=employee"))
            .Content.ReadFromJsonAsync<List<Falah.Application.Audit.AuditEventDto>>();
        Assert.Contains(events!, e => e.Action.Contains("دفع مستحقات") && e.Action.Contains(employee.Name));
    }

    /// <summary>The ledger row itself must carry Settlement, not Advance/Withdrawal/PayrollSettlement
    /// - the explicit instruction that a payment is never merged with, or mistaken for, an advance.</summary>
    [Fact]
    public async Task A_payment_is_recorded_as_its_own_distinct_ledger_type_never_advance_or_withdrawal_or_payroll()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 3000);
        (await PayAsync(employee.Id, 1000)).EnsureSuccessStatusCode();

        var entry = await _factory.QueryAsync(db => db.EmployeeLedgerEntries.SingleAsync(e => e.EmployeeId == employee.Id && e.DebitPiastres == 1000));
        Assert.Equal("Settlement", entry.Type);
        Assert.NotEqual("Advance", entry.Type);
        Assert.NotEqual("Withdrawal", entry.Type);
        Assert.NotEqual("PayrollSettlement", entry.Type);
    }

    // ---- Advance stays a separate, untouched flow ----

    [Fact]
    public async Task An_advance_and_a_payment_coexist_in_ledger_history_as_two_distinct_entries()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 5000);
        var shiftId = await OpenShiftAsync();

        var advance = await (await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/cash-out",
            new RecordEmployeeCashOutRequest($"req-{Guid.NewGuid():N}", employee.Id, "advance", 500, "سلفة اختبار", shiftId, _hrUserId, null, null)))
            .Content.ReadFromJsonAsync<EmployeeCashOutResult>();
        var payment = await (await PayAsync(employee.Id, 1000)).Content.ReadFromJsonAsync<PayEmployeeResult>();

        var entries = await (await _client.GetAsync($"/api/employees/{employee.Id}/ledger")).Content.ReadFromJsonAsync<List<EmployeeLedgerEntryDto>>();
        Assert.Contains(entries!, e => e.Id == advance!.LedgerEntryId && e.Type == "Advance");
        Assert.Contains(entries!, e => e.Id == payment!.LedgerEntryId && e.Type == "Settlement");
        Assert.Equal(3500, await GetBalanceAsync(employee.Id)); // 5000 - 500 (advance) - 1000 (payment)
    }

    /// <summary>Preserved exactly as before: an advance is money given in expectation of FUTURE
    /// work and has no balance cap, unlike a payment or a withdrawal.</summary>
    [Fact]
    public async Task An_advance_can_still_exceed_the_employees_current_balance_exactly_as_before()
    {
        var employee = await CreateEmployeeAsync(); // zero balance - no earnings credited at all
        var shiftId = await OpenShiftAsync();

        var response = await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/cash-out",
            new RecordEmployeeCashOutRequest($"req-{Guid.NewGuid():N}", employee.Id, "advance", 2000, "سلفة قبل أي أجر", shiftId, _hrUserId, null, null));
        response.EnsureSuccessStatusCode();
        Assert.Equal(-2000, await GetBalanceAsync(employee.Id)); // negative balance is the expected, accepted shape of an advance
    }

    // ---- Employee financial reporting by range ----

    [Fact]
    public async Task Financial_report_separates_earnings_advances_and_payments_into_distinct_columns_with_the_correct_remaining_balance()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 5000, "DailyWage"); // earnings
        await CreditAsync(employee.Id, 300, "Bonus");      // additions
        var shiftId = await OpenShiftAsync();
        (await _client.PostAsJsonAsync($"/api/employees/{employee.Id}/cash-out",
            new RecordEmployeeCashOutRequest($"req-{Guid.NewGuid():N}", employee.Id, "advance", 400, "سلفة", shiftId, _hrUserId, null, null)))
            .EnsureSuccessStatusCode();
        (await PayAsync(employee.Id, 1000)).EnsureSuccessStatusCode();

        var report = await (await _client.GetAsync($"/api/employees/financial-report?actorUserId={_managerId}"))
            .Content.ReadFromJsonAsync<List<EmployeeFinancialReportLineDto>>();
        var row = Assert.Single(report!, r => r.EmployeeId == employee.Id);

        Assert.Equal(5000, row.EarningsPiastres);
        Assert.Equal(300, row.AdditionsPiastres);
        Assert.Equal(0, row.DeductionsPiastres);
        Assert.Equal(400, row.AdvancesPiastres);
        Assert.Equal(1000, row.PaymentsPiastres);
        Assert.Equal(3900, row.RemainingBalancePiastres); // 5000 + 300 - 400 - 1000
    }

    [Fact]
    public async Task Financial_report_respects_the_requested_date_range_for_flow_columns_but_not_for_the_running_balance()
    {
        var employee = await CreateEmployeeAsync();
        await CreditAsync(employee.Id, 2000, "DailyWage");
        (await PayAsync(employee.Id, 500)).EnsureSuccessStatusCode();

        var lastWeek = Falah.Domain.Common.BusinessClock.Today().AddDays(-7);
        var report = await (await _client.GetAsync(
            $"/api/employees/financial-report?actorUserId={_managerId}&from={lastWeek:yyyy-MM-dd}&to={lastWeek:yyyy-MM-dd}"))
            .Content.ReadFromJsonAsync<List<EmployeeFinancialReportLineDto>>();
        var row = Assert.Single(report!, r => r.EmployeeId == employee.Id);

        Assert.Equal(0, row.EarningsPiastres); // today's activity is outside last week's range
        Assert.Equal(0, row.PaymentsPiastres);
        Assert.Equal(1500, row.RemainingBalancePiastres); // the running balance is never range-scoped
    }

    private async Task<string> OpenShiftAsync(long openingCashPiastres = 50000) =>
        (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new Falah.Application.Shifts.OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, openingCashPiastres)))
            .Content.ReadFromJsonAsync<Falah.Application.Shifts.OpenShiftResult>())!.ShiftId;
}

namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Expenses;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// AUD-04: an expense recorded by mistake is cancelled by a linked reversal - never deleted - while its shift is open.
/// Amounts in piastres. The drawer opens on 100,000; a 50,000 expense is typed instead of 5,000.
/// </summary>
public class ExpenseReversalTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "expenses_add_drawer");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        _shiftId = (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 100_000))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> AddDrawerExpenseAsync(long amount) =>
        (await (await _client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest(
            $"exp-{Guid.NewGuid():N}", _shiftId, "نظافة ومطهرات", amount, "منظفات", "tre-drawer", _cashierId, null, null)))
            .Content.ReadFromJsonAsync<AddExpenseResult>())!.ExpenseId;

    private Task<HttpResponseMessage> ReverseAsync(string expenseId, string actorId, string requestId, string? approvalId = null, string? approverId = null,
        string? disposition = "not_paid", long? amount = null) =>
        _factory.PostAsSessionOfAsync(_client, actorId, $"/api/expenses/{expenseId}/reverse",
            new ReverseExpenseRequest(requestId, expenseId, "كُتب 500 بدل 50", actorId, approvalId, approverId, disposition, amount));

    private Task<long> ExpectedDrawerAsync() => _factory.ExpectedDrawerCashAsync(_shiftId);

    [Fact]
    public async Task A_reversal_puts_the_money_back_nets_every_total_to_zero_and_keeps_both_records()
    {
        var expenseId = await AddDrawerExpenseAsync(50_000);
        Assert.Equal(50_000, await ExpectedDrawerAsync());

        var response = await ReverseAsync(expenseId, _managerId, $"rev-{Guid.NewGuid():N}");
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<ReverseExpenseResult>())!;
        Assert.Equal(50_000, result.AmountPiastres);
        Assert.Equal(expenseId, result.OriginalExpenseId);

        // The drawer is expected to hold the float again; the treasury drawer is back where it was.
        Assert.Equal(100_000, await ExpectedDrawerAsync());
        var movement = await _factory.QueryAsync(db => db.TreasuryMovements.AsNoTracking().SingleAsync(m => m.Type == "expense_correction"));
        Assert.Equal(("tre-drawer", 50_000L, expenseId), (movement.DestinationAccountId, movement.AmountPiastres, movement.ReferenceId));

        // Nothing was deleted: the original and its reversal both remain, and the report nets them.
        var report = (await _client.GetFromJsonAsync<ExpenseReportDto>(
            $"/api/reports/expenses?actorUserId={_managerId}&businessDate={BusinessClock.Today():yyyy-MM-dd}"))!;
        Assert.Equal(0, report.TotalPiastres);
        Assert.Equal(2, report.Expenses.Count);
        Assert.Contains(report.Expenses, e => e.ExpenseId == expenseId && e.AmountPiastres == 50_000);
        Assert.Contains(report.Expenses, e => e.AmountPiastres == -50_000);

        Assert.True(await _factory.QueryAsync(db => db.AuditEvents.AnyAsync(a => a.ReferenceId == expenseId && a.Action == "إلغاء مصروف")));
    }

    [Fact]
    public async Task A_retried_reversal_replays_and_a_second_one_is_refused()
    {
        var expenseId = await AddDrawerExpenseAsync(50_000);
        var requestId = $"rev-{Guid.NewGuid():N}";
        (await ReverseAsync(expenseId, _managerId, requestId)).EnsureSuccessStatusCode();

        var replay = (await (await ReverseAsync(expenseId, _managerId, requestId)).Content.ReadFromJsonAsync<ReverseExpenseResult>())!;
        Assert.True(replay.WasReplayed);

        var another = await ReverseAsync(expenseId, _managerId, $"rev-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, another.StatusCode);
        Assert.Equal(1, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "expense_correction")));
        Assert.Equal(100_000, await ExpectedDrawerAsync());
    }

    [Fact]
    public async Task A_cashier_needs_a_managers_approval_to_reverse()
    {
        var expenseId = await AddDrawerExpenseAsync(50_000);

        var alone = await ReverseAsync(expenseId, _cashierId, $"rev-{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, alone.StatusCode);

        var approval = (await (await _factory.PostAsSessionOfAsync(_client, _cashierId, "/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "shifts_manage", "إلغاء مصروف خاطئ"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!;
        var approved = await ReverseAsync(expenseId, _cashierId, $"rev-{Guid.NewGuid():N}", approval.ApprovalId, approval.ApproverUserId);
        approved.EnsureSuccessStatusCode();
        Assert.Equal("المدير", (await approved.Content.ReadFromJsonAsync<ReverseExpenseResult>())!.ApprovedBy);
    }

    [Fact]
    public async Task An_expense_of_a_closed_shift_is_not_reversed()
    {
        var expenseId = await AddDrawerExpenseAsync(5_000);
        (await _client.PostAsJsonAsync("/api/shifts/close", new CloseShiftRequest(
            $"close-{Guid.NewGuid():N}", _shiftId, _cashierId, 95_000, null, null, null))).EnsureSuccessStatusCode();

        var response = await ReverseAsync(expenseId, _managerId, $"rev-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "expense_correction")));
    }

    [Fact]
    public async Task A_reversal_must_say_what_happened_to_the_money()
    {
        // Money that left and stays gone is not reversible; the system must not guess that it came back.
        var expenseId = await AddDrawerExpenseAsync(50_000);

        var response = await ReverseAsync(expenseId, _managerId, $"rev-{Guid.NewGuid():N}", disposition: null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ReferenceId == expenseId && m.DestinationAccountId != null)));
        Assert.Equal(50_000, await ExpectedDrawerAsync());
    }

    [Fact]
    public async Task A_wrong_amount_is_corrected_by_the_part_that_never_left_and_the_drawer_closes_without_difference()
    {
        // 50,000 typed; 5,000 really left the drawer. Reversing the whole 50,000 would credit the drawer with 5,000
        // that is not in it; only the 45,000 that never left is reversed.
        var expenseId = await AddDrawerExpenseAsync(50_000);

        var response = await ReverseAsync(expenseId, _managerId, $"rev-{Guid.NewGuid():N}", amount: 45_000);
        response.EnsureSuccessStatusCode();
        Assert.Equal(45_000, (await response.Content.ReadFromJsonAsync<ReverseExpenseResult>())!.AmountPiastres);

        Assert.Equal(95_000, await ExpectedDrawerAsync());
        var report = (await _client.GetFromJsonAsync<ExpenseReportDto>(
            $"/api/reports/expenses?actorUserId={_managerId}&businessDate={BusinessClock.Today():yyyy-MM-dd}"))!;
        Assert.Equal(5_000, report.TotalPiastres);

        // The physical count matches: 100,000 float - 5,000 paid = 95,000 counted, no difference, drawer nets to 0.
        var close = (await (await _client.PostAsJsonAsync("/api/shifts/close", new CloseShiftRequest(
            $"close-{Guid.NewGuid():N}", _shiftId, _cashierId, 95_000, null, null, null))).Content.ReadFromJsonAsync<CloseShiftResult>())!;
        Assert.Equal(0, close.DifferencePiastres);
        var drawer = await _factory.QueryAsync(db => db.TreasuryMovements.AsNoTracking()
            .Where(m => m.Status == "Posted" && (m.SourceAccountId == "tre-drawer" || m.DestinationAccountId == "tre-drawer"))
            .SumAsync(m => m.DestinationAccountId == "tre-drawer" ? m.AmountPiastres : -m.AmountPiastres));
        Assert.Equal(0, drawer);
    }

    [Fact]
    public async Task Cash_that_came_back_is_a_reimbursement_and_never_a_collection()
    {
        var expenseId = await AddDrawerExpenseAsync(20_000);

        var response = await ReverseAsync(expenseId, _managerId, $"rev-{Guid.NewGuid():N}", disposition: "cash_returned");
        response.EnsureSuccessStatusCode();
        Assert.Equal("cash_returned", (await response.Content.ReadFromJsonAsync<ReverseExpenseResult>())!.CashDisposition);

        var movement = await _factory.QueryAsync(db => db.TreasuryMovements.AsNoTracking().SingleAsync(m => m.ReferenceId == expenseId && m.DestinationAccountId == "tre-drawer"));
        Assert.Equal("expense_refund", movement.Type);

        var flow = (await _client.GetFromJsonAsync<CashFlowReportDto>(
            $"/api/reports/cash-flow?actorUserId={_managerId}&businessDate={BusinessClock.Today():yyyy-MM-dd}"))!;
        var drawerLines = flow.Accounts.Single(a => a.AccountId == "tre-drawer").Lines;
        Assert.Equal(20_000, drawerLines.Single(l => l.MovementType == "expense_refund").InflowPiastres);
        Assert.DoesNotContain(drawerLines, l => l.MovementType == "sale_collection");
        Assert.Equal(100_000, await ExpectedDrawerAsync());
    }

    [Fact]
    public async Task A_reversal_needs_a_reason()
    {
        var expenseId = await AddDrawerExpenseAsync(5_000);
        var response = await _factory.PostAsSessionOfAsync(_client, _managerId, $"/api/expenses/{expenseId}/reverse",
            new ReverseExpenseRequest($"rev-{Guid.NewGuid():N}", expenseId, " ", _managerId, null, null, "not_paid"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}

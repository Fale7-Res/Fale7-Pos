namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Expenses;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Ported from addExpense/expensesVisibleToActor in the reference app's lib/store.tsx and
/// lib/expense-visibility.mjs. The one invariant that makes this different from every other
/// Treasury-shaped command so far: a main-treasury expense has NO approval-token fallback at all -
/// see AuthorizationPolicy.AuthorizeExpense's own doc comment.
/// </summary>
public class ExpenseVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _managerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> OpenShiftAsync(string userId, long openingCashPiastres = 50000) =>
        (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", userId, openingCashPiastres)))
            .Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;

    private static AddExpenseRequest MakeRequest(string shiftId, string userId, string paymentAccountId,
        long amountPiastres = 5000, string? approvalId = null, string? approverId = null, string requestId = "") =>
        new(string.IsNullOrEmpty(requestId) ? $"exp-{Guid.NewGuid():N}" : requestId, shiftId, "مستلزمات",
            amountPiastres, "شراء أدوات تنظيف", paymentAccountId, userId, approvalId, approverId);

    [Fact]
    public async Task A_drawer_expense_with_permission_succeeds_and_posts_a_movement_debiting_the_drawer()
    {
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "expenses_add_drawer");
        var shiftId = await OpenShiftAsync(cashierId);

        var response = await _client.PostAsJsonAsync("/api/expenses", MakeRequest(shiftId, cashierId, "tre-drawer"));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AddExpenseResult>();

        Assert.True(result!.PaidFromDrawer);
        Assert.False(result.WasReplayed);

        var movement = await _factory.QueryAsync(db => db.TreasuryMovements.FirstAsync(m => m.Id == result.TreasuryMovementId));
        Assert.Equal("expense_payment", movement.Type);
        Assert.Equal("tre-drawer", movement.SourceAccountId);
        Assert.Null(movement.DestinationAccountId);
        Assert.Equal(5000, movement.AmountPiastres);
    }

    [Fact]
    public async Task A_drawer_expense_without_permission_or_approval_is_rejected()
    {
        var cashierId = await _factory.SeedUserAsync("كاشير بلا صلاحية", "cashier", "pos", "shifts_open_own");
        var shiftId = await OpenShiftAsync(cashierId);

        var response = await _client.PostAsJsonAsync("/api/expenses", MakeRequest(shiftId, cashierId, "tre-drawer"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var count = await _factory.QueryAsync(db => db.Expenses.CountAsync(e => e.ShiftId == shiftId));
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task A_drawer_expense_can_be_authorized_by_a_matching_manager_approval_instead_of_the_permission()
    {
        var cashierId = await _factory.SeedUserAsync("كاشير بلا صلاحية", "cashier", "pos", "shifts_open_own");
        var shiftId = await OpenShiftAsync(cashierId);
        var approval = await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "expenses_add_drawer", "اعتماد مصروف"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>();

        var response = await _client.PostAsJsonAsync("/api/expenses",
            MakeRequest(shiftId, cashierId, "tre-drawer", approvalId: approval!.ApprovalId, approverId: _managerId));
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_main_treasury_expense_has_no_approval_fallback_a_manager_approval_does_not_authorize_it()
    {
        // The key differentiator from every other Treasury command: even a VALID, correctly-scoped
        // approval token must not substitute for holding expenses_main_treasury directly.
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own");
        var shiftId = await OpenShiftAsync(cashierId);
        var approval = await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "expenses_main_treasury", "محاولة اعتماد"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>();

        var response = await _client.PostAsJsonAsync("/api/expenses",
            MakeRequest(shiftId, cashierId, "tre-main", approvalId: approval!.ApprovalId, approverId: _managerId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_main_treasury_expense_succeeds_only_for_a_user_holding_the_permission_directly()
    {
        var shiftId = await OpenShiftAsync(_managerId); // manager holds "all" -> has expenses_main_treasury
        var response = await _client.PostAsJsonAsync("/api/expenses", MakeRequest(shiftId, _managerId, "tre-main"));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AddExpenseResult>();
        Assert.False(result!.PaidFromDrawer);
    }

    [Fact]
    public async Task An_expense_requires_an_open_shift()
    {
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "expenses_add_drawer");
        var shiftId = await OpenShiftAsync(cashierId);
        await _client.PostAsJsonAsync("/api/shifts/close", new CloseShiftRequest($"creq-{Guid.NewGuid():N}", shiftId, cashierId, 50000, null, null, null));

        var response = await _client.PostAsJsonAsync("/api/expenses", MakeRequest(shiftId, cashierId, "tre-drawer"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task An_expense_requires_an_open_business_day_even_when_the_shift_itself_is_open()
    {
        // A shift opened with ZERO cash never touches Treasury or requires a business day (see
        // OpenShiftHandler) - the only way to get an open shift with NO open business day in this
        // system, to isolate this gate from the shift-open gate that would otherwise also require one.
        using var freshFactory = new FalahApiFactory();
        // Its own factory AND its own database - not a restart over a shared one, so its
        // sessions are its own. TokenFor mints lazily, so the client can be built before the
        // user it will act as exists.
        var freshClient = freshFactory.CreateAuthenticatedClient();
        var cashierId = await freshFactory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "expenses_add_drawer");
        var shift = await (await freshClient.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", cashierId, 0))).Content.ReadFromJsonAsync<OpenShiftResult>();

        var response = await freshClient.PostAsJsonAsync("/api/expenses", MakeRequest(shift!.ShiftId, cashierId, "tre-drawer"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task An_expense_larger_than_the_source_accounts_balance_is_rejected()
    {
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "expenses_add_drawer");
        var shiftId = await OpenShiftAsync(cashierId, openingCashPiastres: 10000);

        var response = await _client.PostAsJsonAsync("/api/expenses", MakeRequest(shiftId, cashierId, "tre-drawer", amountPiastres: 999999));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var count = await _factory.QueryAsync(db => db.Expenses.CountAsync(e => e.ShiftId == shiftId));
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Retrying_the_exact_same_expense_request_never_posts_twice()
    {
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "expenses_add_drawer");
        var shiftId = await OpenShiftAsync(cashierId);
        var requestId = $"exp-{Guid.NewGuid():N}";

        var first = await (await _client.PostAsJsonAsync("/api/expenses", MakeRequest(shiftId, cashierId, "tre-drawer", requestId: requestId))).Content.ReadFromJsonAsync<AddExpenseResult>();
        var second = await (await _client.PostAsJsonAsync("/api/expenses", MakeRequest(shiftId, cashierId, "tre-drawer", requestId: requestId))).Content.ReadFromJsonAsync<AddExpenseResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(first.ExpenseId, second.ExpenseId);
        var count = await _factory.QueryAsync(db => db.Expenses.CountAsync(e => e.ShiftId == shiftId));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Concurrent_expense_requests_with_the_same_request_id_post_exactly_once()
    {
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "expenses_add_drawer");
        var shiftId = await OpenShiftAsync(cashierId);
        var requestId = $"exp-{Guid.NewGuid():N}";

        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/expenses", MakeRequest(shiftId, cashierId, "tre-drawer", requestId: requestId)));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<AddExpenseResult>()));
        Assert.Equal(1, results.Count(r => !r!.WasReplayed));
        var count = await _factory.QueryAsync(db => db.Expenses.CountAsync(e => e.ShiftId == shiftId));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task A_cashier_sees_only_their_own_shifts_drawer_expenses_never_main_treasury_ones()
    {
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "expenses_add_drawer");
        var shiftId = await OpenShiftAsync(cashierId);
        await _client.PostAsJsonAsync("/api/expenses", MakeRequest(shiftId, cashierId, "tre-drawer"));
        await _client.PostAsJsonAsync("/api/expenses", MakeRequest(shiftId, _managerId, "tre-main"));

        // A drawer expense on a DIFFERENT shift must never leak into this cashier's own-shift view -
        // proven below by scoping the query to `shiftId` and asserting every visible row matches it,
        // without needing a second REAL shift open (single system-wide active shift; see
        // OpenShiftHandler - this cashier's shift must stay open for their own-shift view to work at
        // all, per ExpenseQueryService's own doc comment).
        var visible = await (await _client.GetAsync($"/api/expenses?actorUserId={cashierId}&shiftId={shiftId}"))
            .Content.ReadFromJsonAsync<List<ExpenseDto>>();

        Assert.Single(visible!);
        Assert.True(visible!.Single().PaidFromDrawer);
        Assert.All(visible!, e => Assert.Equal(shiftId, e.ShiftId));
    }

    [Fact]
    public async Task A_manager_sees_all_expenses_for_the_business_date_including_main_treasury_ones()
    {
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "expenses_add_drawer");
        var shiftId = await OpenShiftAsync(cashierId);
        await _client.PostAsJsonAsync("/api/expenses", MakeRequest(shiftId, cashierId, "tre-drawer"));
        await _client.PostAsJsonAsync("/api/expenses", MakeRequest(shiftId, _managerId, "tre-main"));

        // Cairo business date, not raw UTC - the handler stamps expenses via BusinessClock.BusinessDate,
        // which can already have rolled to the next day (or not yet rolled from the previous one)
        // relative to a plain DateTimeOffset.UtcNow.ToString() near the UTC/Cairo midnight boundary.
        var today = Falah.Domain.Common.BusinessClock.BusinessDate(DateTimeOffset.UtcNow).ToString("yyyy-MM-dd");
        var visible = await (await _client.GetAsync($"/api/expenses?actorUserId={_managerId}&businessDate={today}"))
            .Content.ReadFromJsonAsync<List<ExpenseDto>>();

        Assert.True(visible!.Count >= 2);
        Assert.Contains(visible, e => e.PaymentAccountId == "tre-drawer");
        Assert.Contains(visible, e => e.PaymentAccountId == "tre-main");
    }

    [Fact]
    public async Task A_drawer_expense_correctly_reduces_this_shifts_expected_cash()
    {
        // Exercises the specific wiring this slice adds to ShiftReconciliation - not re-testing
        // shift-close's own already-proven ledger mechanics, just that expenses now actually feed it.
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "expenses_add_drawer");
        var shiftId = await OpenShiftAsync(cashierId, openingCashPiastres: 50000);

        await _client.PostAsJsonAsync("/api/expenses", MakeRequest(shiftId, cashierId, "tre-drawer", amountPiastres: 8000));

        var preview = await (await _client.PostAsJsonAsync("/api/shifts/prepare-close",
            new PrepareShiftCloseRequest(shiftId, 42000))).Content.ReadFromJsonAsync<PrepareShiftCloseResult>();

        Assert.Equal(42000, preview!.ExpectedCashPiastres); // 50000 opening - 8000 drawer expense
        Assert.Equal(0, preview.DifferencePiastres);
    }
}

namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.BusinessDay;
using Falah.Application.Shifts;
using Falah.Application.Treasury;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Business Day open (the minimal gate Treasury needs - see BusinessDayEntity's doc comment) plus
/// the four Treasury cash-lifecycle commands: deposit (external funding), withdrawal (owner draw),
/// transfer (between the business's own accounts), and safe drop (drawer -> safe mid-shift).
/// Ported from lib/enterprise-store.tsx's postExternalFunding/postOwnerWithdrawal/transferTreasury/
/// postSafeDrop.
/// </summary>
public class TreasuryVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _managerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task OpenBusinessDayAsync() =>
        (await _client.PostAsJsonAsync("/api/business-day/open", new OpenBusinessDayRequest($"bd-{Guid.NewGuid():N}", _managerId))).EnsureSuccessStatusCode();

    private async Task<long> BalanceAsync(string accountId) =>
        (await (await _client.GetAsync($"/api/treasury/balance?accountId={accountId}")).Content.ReadFromJsonAsync<BalanceDto>())!.balancePiastres;

    private sealed record BalanceDto(string accountId, long balancePiastres);

    [Fact]
    public async Task Opening_a_business_day_twice_with_the_same_request_id_replays_not_duplicates()
    {
        var requestId = $"bd-{Guid.NewGuid():N}";
        var first = await (await _client.PostAsJsonAsync("/api/business-day/open", new OpenBusinessDayRequest(requestId, _managerId))).Content.ReadFromJsonAsync<OpenBusinessDayResult>();
        var second = await (await _client.PostAsJsonAsync("/api/business-day/open", new OpenBusinessDayRequest(requestId, _managerId))).Content.ReadFromJsonAsync<OpenBusinessDayResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(first.BusinessDayId, second.BusinessDayId);
    }

    [Fact]
    public async Task A_second_business_day_cannot_open_while_one_is_already_active()
    {
        await OpenBusinessDayAsync();
        var response = await _client.PostAsJsonAsync("/api/business-day/open", new OpenBusinessDayRequest($"bd-{Guid.NewGuid():N}", _managerId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_deposit_requires_an_open_business_day()
    {
        var request = new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 100000, "tre-main", "تمويل افتتاحي", null, _managerId, null, null);
        var response = await _client.PostAsJsonAsync("/api/treasury/deposit", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_deposit_increases_the_destination_account_balance()
    {
        await OpenBusinessDayAsync();
        var before = await BalanceAsync("tre-main");
        var request = new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 100000, "tre-main", "تمويل افتتاحي", null, _managerId, null, null);
        var response = await _client.PostAsJsonAsync("/api/treasury/deposit", request);
        response.EnsureSuccessStatusCode();

        Assert.Equal(before + 100000, await BalanceAsync("tre-main"));
    }

    [Fact]
    public async Task A_deposit_can_never_land_in_the_drawer_or_wallet()
    {
        await OpenBusinessDayAsync();
        var request = new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 100000, "tre-drawer", "محاولة غير صالحة", null, _managerId, null, null);
        var response = await _client.PostAsJsonAsync("/api/treasury/deposit", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task An_invalid_deposit_type_is_rejected()
    {
        await OpenBusinessDayAsync();
        var request = new DepositRequest($"dep-{Guid.NewGuid():N}", "not_a_real_type", 100000, "tre-main", "سبب", null, _managerId, null, null);
        var response = await _client.PostAsJsonAsync("/api/treasury/deposit", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Retrying_the_exact_same_deposit_request_never_posts_twice()
    {
        await OpenBusinessDayAsync();
        var requestId = $"dep-{Guid.NewGuid():N}";
        DepositRequest MakeRequest() => new(requestId, "owner_deposit", 50000, "tre-main", "تمويل", null, _managerId, null, null);
        var first = await (await _client.PostAsJsonAsync("/api/treasury/deposit", MakeRequest())).Content.ReadFromJsonAsync<DepositResult>();
        var second = await (await _client.PostAsJsonAsync("/api/treasury/deposit", MakeRequest())).Content.ReadFromJsonAsync<DepositResult>();

        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(50000, await BalanceAsync("tre-main"));
    }

    // ---- Different concurrent operations against ONE balance ----
    //
    // The tests above prove that retrying the SAME request posts once. That is a different
    // guarantee from the one below, and an independent audit found the gap between them: each
    // handler read the account balance, decided it was sufficient, and only then opened its
    // transaction, so several DIFFERENT commands could each read the same balance, each decide it
    // covered them, and each write. An account holding 30 paid out three separate transfers of 20
    // and finished at -30.

    [Fact]
    public async Task Different_concurrent_withdrawals_cannot_between_them_overdraw_the_account()
    {
        await OpenBusinessDayAsync();
        await _client.PostAsJsonAsync("/api/treasury/deposit",
            new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 3000, "tre-main", "تمويل", null, _managerId, null, null));

        // Six genuinely distinct withdrawals of 2,000 against a balance of 3,000. Exactly one can
        // be afforded; idempotency cannot help, because no two of these are the same request.
        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/treasury/withdraw",
            new WithdrawalRequest($"wd-{Guid.NewGuid():N}", 2000, "tre-main", "سحب", null, _managerId, null, null)));
        var responses = await Task.WhenAll(tasks);

        var accepted = responses.Count(r => r.IsSuccessStatusCode);
        var balance = await BalanceAsync("tre-main");

        Assert.Equal(1, accepted);
        Assert.Equal(1000, balance);
        // The real point, stated so a future regression reads as what it is rather than as an
        // arithmetic surprise: the account must never end below zero.
        Assert.True(balance >= 0, $"account was overdrawn to {balance}");
    }

    [Fact]
    public async Task Different_concurrent_transfers_cannot_between_them_overdraw_the_account()
    {
        await OpenBusinessDayAsync();
        await _client.PostAsJsonAsync("/api/treasury/deposit",
            new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 3000, "tre-main", "تمويل", null, _managerId, null, null));

        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/treasury/transfer",
            new TransferRequest($"tr-{Guid.NewGuid():N}", "tre-main", "tre-bank", 2000, "تحويل", _managerId, null, null)));
        var responses = await Task.WhenAll(tasks);

        Assert.Equal(1, responses.Count(r => r.IsSuccessStatusCode));
        Assert.Equal(1000, await BalanceAsync("tre-main"));
        // Every piastre that left the source has to have arrived somewhere.
        Assert.Equal(2000, await BalanceAsync("tre-bank"));
    }

    [Fact]
    public async Task A_safe_drop_moves_the_cash_into_the_main_treasury_rather_than_out_of_the_books()
    {
        // The drawer used to fall while nothing rose: the movement carried no destination account
        // at all, so the money simply left the books - while the audit line for the same operation
        // said, and still says, that it had gone to the main treasury.
        await OpenBusinessDayAsync();
        await _client.PostAsJsonAsync("/api/treasury/deposit",
            new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 20000, "tre-main", "تمويل", null, _managerId, null, null));
        var shift = await _client.PostAsJsonAsync("/api/shifts/open",
            new Falah.Application.Shifts.OpenShiftRequest($"sh-{Guid.NewGuid():N}", "وردية", _managerId, 5000));
        shift.EnsureSuccessStatusCode();
        var shiftId = (await shift.Content.ReadFromJsonAsync<Falah.Application.Shifts.OpenShiftResult>())!.ShiftId;

        var mainBefore = await BalanceAsync("tre-main");
        var drawerBefore = await BalanceAsync("tre-drawer");

        var response = await _client.PostAsJsonAsync("/api/treasury/safe-drop",
            new SafeDropRequest($"sd-{Guid.NewGuid():N}", shiftId, 1000, "إيداع آمن", _managerId, null, null));
        response.EnsureSuccessStatusCode();

        Assert.Equal(drawerBefore - 1000, await BalanceAsync("tre-drawer"));
        // The half that was missing: cash out of the till has to be cash into the safe.
        Assert.Equal(mainBefore + 1000, await BalanceAsync("tre-main"));
    }

    [Fact]
    public async Task Concurrent_deposits_with_the_same_request_id_post_exactly_once()
    {
        await OpenBusinessDayAsync();
        var requestId = $"dep-{Guid.NewGuid():N}";
        DepositRequest MakeRequest() => new(requestId, "owner_deposit", 50000, "tre-main", "تمويل", null, _managerId, null, null);
        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/treasury/deposit", MakeRequest()));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<DepositResult>()));
        Assert.Equal(1, results.Count(r => !r!.WasReplayed));
        Assert.Equal(50000, await BalanceAsync("tre-main"));
    }

    [Fact]
    public async Task Concurrent_withdrawals_with_the_same_request_id_post_exactly_once()
    {
        await OpenBusinessDayAsync();
        await _client.PostAsJsonAsync("/api/treasury/deposit", new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 100000, "tre-main", "تمويل", null, _managerId, null, null));

        var requestId = $"wd-{Guid.NewGuid():N}";
        WithdrawalRequest MakeRequest() => new(requestId, 40000, "tre-main", "سحب مالك", null, _managerId, null, null);
        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/treasury/withdraw", MakeRequest()));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<WithdrawalResult>()));
        Assert.Equal(1, results.Count(r => !r!.WasReplayed));
        // A single 40,000 withdrawal against a 100,000 balance - never 6x the amount despite 6 concurrent posts.
        Assert.Equal(60000, await BalanceAsync("tre-main"));
    }

    [Fact]
    public async Task Concurrent_transfers_with_the_same_request_id_post_exactly_once()
    {
        await OpenBusinessDayAsync();
        await _client.PostAsJsonAsync("/api/treasury/deposit", new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 100000, "tre-main", "تمويل", null, _managerId, null, null));

        var requestId = $"tr-{Guid.NewGuid():N}";
        TransferRequest MakeRequest() => new(requestId, "tre-main", "tre-bank", 30000, "تحويل للبنك", _managerId, null, null);
        var tasks = Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/treasury/transfer", MakeRequest()));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<TransferResult>()));
        Assert.Equal(1, results.Count(r => !r!.WasReplayed));
        Assert.Equal(70000, await BalanceAsync("tre-main"));
        Assert.Equal(30000, await BalanceAsync("tre-bank"));
    }

    [Fact]
    public async Task A_withdrawal_decreases_the_source_balance_and_is_blocked_beyond_it()
    {
        await OpenBusinessDayAsync();
        await _client.PostAsJsonAsync("/api/treasury/deposit", new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 100000, "tre-main", "تمويل", null, _managerId, null, null));

        var withdrawOk = await _client.PostAsJsonAsync("/api/treasury/withdraw",
            new WithdrawalRequest($"wd-{Guid.NewGuid():N}", 40000, "tre-main", "سحب مالك", null, _managerId, null, null));
        withdrawOk.EnsureSuccessStatusCode();
        Assert.Equal(60000, await BalanceAsync("tre-main"));

        var withdrawTooMuch = await _client.PostAsJsonAsync("/api/treasury/withdraw",
            new WithdrawalRequest($"wd-{Guid.NewGuid():N}", 999999, "tre-main", "سحب مبالغ فيه", null, _managerId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, withdrawTooMuch.StatusCode);
        Assert.Equal(60000, await BalanceAsync("tre-main")); // unchanged - the rejected attempt posted nothing
    }

    [Fact]
    public async Task A_withdrawal_can_never_be_sourced_from_the_drawer_or_wallet()
    {
        await OpenBusinessDayAsync();
        var response = await _client.PostAsJsonAsync("/api/treasury/withdraw",
            new WithdrawalRequest($"wd-{Guid.NewGuid():N}", 1000, "tre-drawer", "محاولة غير صالحة", null, _managerId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_transfer_moves_the_exact_amount_between_two_accounts()
    {
        await OpenBusinessDayAsync();
        await _client.PostAsJsonAsync("/api/treasury/deposit", new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 100000, "tre-main", "تمويل", null, _managerId, null, null));

        var response = await _client.PostAsJsonAsync("/api/treasury/transfer",
            new TransferRequest($"tr-{Guid.NewGuid():N}", "tre-main", "tre-bank", 30000, "تحويل للبنك", _managerId, null, null));
        response.EnsureSuccessStatusCode();

        Assert.Equal(70000, await BalanceAsync("tre-main"));
        Assert.Equal(30000, await BalanceAsync("tre-bank"));
    }

    // ---- Cash handover (createHandover - a real gap found during the legacy-parity audit: a
    // transfer's own dedicated "who handed money to whom" receipt record had no equivalent) ----

    [Fact]
    public async Task A_transfer_creates_exactly_one_cash_handover_record()
    {
        await OpenBusinessDayAsync();
        await _client.PostAsJsonAsync("/api/treasury/deposit", new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 100000, "tre-main", "تمويل", null, _managerId, null, null));

        var transferRequestId = $"tr-{Guid.NewGuid():N}";
        (await _client.PostAsJsonAsync("/api/treasury/transfer",
            new TransferRequest(transferRequestId, "tre-main", "tre-bank", 30000, "تحويل للبنك", _managerId, null, null))).EnsureSuccessStatusCode();

        var handovers = await (await _client.GetAsync("/api/treasury/handovers?limit=50")).Content.ReadFromJsonAsync<List<System.Text.Json.JsonElement>>();
        var matching = handovers!.Where(h => h.GetProperty("fromAccountId").GetString() == "tre-main"
            && h.GetProperty("toAccountId").GetString() == "tre-bank" && h.GetProperty("amountPiastres").GetInt64() == 30000).ToList();
        Assert.Single(matching);

        // Retrying the exact same transfer request is a replay, not a second transfer - the
        // handover row (unique on TreasuryMovementId) must never double either.
        (await _client.PostAsJsonAsync("/api/treasury/transfer",
            new TransferRequest(transferRequestId, "tre-main", "tre-bank", 30000, "تحويل للبنك", _managerId, null, null))).EnsureSuccessStatusCode();
        var afterRetry = await (await _client.GetAsync("/api/treasury/handovers?limit=50")).Content.ReadFromJsonAsync<List<System.Text.Json.JsonElement>>();
        Assert.Single(afterRetry!.Where(h => h.GetProperty("amountPiastres").GetInt64() == 30000));
    }

    [Fact]
    public async Task A_transfer_to_the_same_account_is_rejected()
    {
        await OpenBusinessDayAsync();
        var response = await _client.PostAsJsonAsync("/api/treasury/transfer",
            new TransferRequest($"tr-{Guid.NewGuid():N}", "tre-main", "tre-main", 1000, "نفس الحساب", _managerId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Treasury_operations_require_treasury_manage_permission_or_a_matching_approval()
    {
        await OpenBusinessDayAsync();
        var unprivileged = await _factory.SeedUserAsync("موظف بلا صلاحية", "cashier", "pos");

        var denied = await _client.PostAsJsonAsync("/api/treasury/deposit",
            new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 10000, "tre-main", "محاولة", null, unprivileged, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, denied.StatusCode);

        var approval = await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "treasury_manage", "اعتماد إيداع"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>();
        var allowed = await _client.PostAsJsonAsync("/api/treasury/deposit",
            new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 10000, "tre-main", "محاولة بموافقة", null, unprivileged, approval!.ApprovalId, _managerId));
        allowed.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_safe_drop_requires_the_dedicated_safe_drop_permission_not_treasury_manage()
    {
        // Needs real funding, not just an open day: opening a shift with cash now posts a real
        // shift_opening_float movement (tre-main -> tre-drawer), which itself requires tre-main to
        // have the funds - see OpenShiftHandler.
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own");
        var shift = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();

        var denied = await _client.PostAsJsonAsync("/api/treasury/safe-drop",
            new SafeDropRequest($"drop-{Guid.NewGuid():N}", shift!.ShiftId, 20000, "إيداع آمن", cashierId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, denied.StatusCode);

        // Single system-wide active shift (see OpenShiftHandler) - close the first before opening
        // a second one. 50000 = exact opening cash, no discrepancy approval needed.
        await _client.PostAsJsonAsync("/api/shifts/close",
            new Falah.Application.Shifts.CloseShiftRequest($"creq-{Guid.NewGuid():N}", shift.ShiftId, _managerId, 50000, null, null, null));

        var cashierWithSafeDrop = await _factory.SeedUserAsync("كاشير مخوّل", "cashier", "pos,safe_drop,shifts_open_own");
        var shift2 = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", cashierWithSafeDrop, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        var allowed = await _client.PostAsJsonAsync("/api/treasury/safe-drop",
            new SafeDropRequest($"drop-{Guid.NewGuid():N}", shift2!.ShiftId, 20000, "إيداع آمن", cashierWithSafeDrop, null, null));
        allowed.EnsureSuccessStatusCode();

        var movementCount = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ShiftId == shift2.ShiftId && m.Type == "safe_drop"));
        Assert.Equal(1, movementCount);
    }

    [Fact]
    public async Task A_safe_drop_references_a_shift_that_must_exist()
    {
        await OpenBusinessDayAsync();
        var cashierWithSafeDrop = await _factory.SeedUserAsync("كاشير مخوّل", "cashier", "pos,safe_drop");
        var response = await _client.PostAsJsonAsync("/api/treasury/safe-drop",
            new SafeDropRequest($"drop-{Guid.NewGuid():N}", "shift-does-not-exist", 1000, "سبب", cashierWithSafeDrop, null, null));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

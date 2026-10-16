namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Expenses;
using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// The paper a shift and an expense produce, and what a reprint is allowed to touch.
///
/// The invariant under every test here: printing is a hardware dispatch fact and nothing else.
/// Reproducing a receipt, an opening slip or an expense voucher must never create an order, move
/// money, touch stock or change what the original document says.
/// </summary>
public class SlipPrintingAndReprintTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _managerId = default!;
    private string _cashierId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "expenses_add_drawer");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> OpenShiftAsync(string requestId, long openingCashPiastres = 50_000) =>
        (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest(requestId, "صباحية", _cashierId, openingCashPiastres)))
            .Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;

    private Task<List<PrintJobDto>> JobsAsync(string documentId) =>
        _factory.QueryAsync(async db => await db.PrintJobs.AsNoTracking()
            .Where(j => j.OrderId == documentId)
            .Select(j => new PrintJobDto(j.Id, j.OrderId, j.OrderNumber, j.Route, j.Status, j.AttemptCount,
                j.LastAttemptAtUtc, j.LastError, j.IsReprint, j.CreatedAtUtc))
            .ToListAsync());

    private async Task<List<string>> TicketTextAsync(string jobId)
    {
        var ticket = await (await _client.GetAsync($"/api/print-jobs/{jobId}/ticket")).Content.ReadFromJsonAsync<ComposedTicketDto>();
        return ticket!.Lines.SelectMany(TicketDtoText.Of).Where(t => t is not null).Select(t => t!).ToList();
    }

    // ---- Shift slips ----

    [Fact]
    public async Task Opening_a_shift_queues_exactly_one_opening_slip_and_a_replay_queues_none()
    {
        var requestId = $"open-{Guid.NewGuid():N}";
        var shiftId = await OpenShiftAsync(requestId);

        var job = Assert.Single(await JobsAsync(shiftId));
        Assert.Equal("Receipt", job.Route);
        Assert.Equal("Pending", job.Status);
        Assert.False(job.IsReprint);

        var text = await TicketTextAsync(job.Id);
        Assert.Contains("افتتاح وردية", text);
        Assert.Contains("رصيد افتتاحي", text);
        Assert.Contains("500 ج", text);           // the float, in pounds
        Assert.Contains("كاشير", text.First(t => t.Contains("كاشير")));

        // The same open request again is the same shift - and must not queue a second slip.
        (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest(requestId, "صباحية", _cashierId, 50_000))).EnsureSuccessStatusCode();
        Assert.Single(await JobsAsync(shiftId));
    }

    [Fact]
    public async Task Closing_a_shift_queues_a_handover_slip_stating_the_persisted_figures()
    {
        var shiftId = await OpenShiftAsync($"open-{Guid.NewGuid():N}");

        var closed = await (await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"close-{Guid.NewGuid():N}", shiftId, _cashierId, 50_000, null, null, null)))
            .Content.ReadFromJsonAsync<CloseShiftResult>();
        Assert.Equal("Closed", closed!.Status);

        var handover = Assert.Single(await JobsAsync(shiftId), j => j.OrderNumber == "إغلاق وردية");
        var text = await TicketTextAsync(handover.Id);
        Assert.Contains("إغلاق وتسليم وردية", text);
        Assert.Contains("النقدية المتوقعة", text);
        Assert.Contains("النقدية الفعلية", text);
        // The slip states what the close persisted, never its own recomputation.
        Assert.Contains($"{closed.ExpectedCashPiastres / 100} ج", text);
        Assert.Contains($"{closed.ActualCashPiastres / 100} ج", text);
        Assert.Contains("الفرق - مطابق", text);
    }

    /// <summary>
    /// Cash handed to an employee out of the drawer leaves the drawer, so the shift must expect
    /// less. Before this was fixed the advance was invisible to the reconciliation and every such
    /// shift closed with a false shortage exactly equal to what was paid out.
    /// </summary>
    [Fact]
    public async Task An_employee_advance_paid_from_the_drawer_lowers_the_expected_cash()
    {
        var shiftId = await OpenShiftAsync($"open-{Guid.NewGuid():N}");
        var employee = await (await _client.PostAsJsonAsync("/api/employees",
            new Falah.Application.Employees.CreateEmployeeRequest(
                $"emp-{Guid.NewGuid():N}", "عامل", "EMP-SLIP-1", "نادل", "01000000000", new DateOnly(2024, 1, 1),
                "Daily", 20000, 15000, null, false, "cashier", null, null, null, null, null, null, null, null, _managerId)))
            .Content.ReadFromJsonAsync<Falah.Application.Employees.EmployeeDto>();

        var approval = await (await _client.PostAsJsonAsync("/api/approvals/issue",
            new Falah.Application.Approvals.IssueManagerApprovalRequest(_factory.PinFor(_managerId), "employees_ledger", "سلفة")))
            .Content.ReadFromJsonAsync<Falah.Application.Approvals.IssueManagerApprovalResult>();

        var advance = await _client.PostAsJsonAsync($"/api/employees/{employee!.Id}/cash-out",
            new Falah.Application.Employees.RecordEmployeeCashOutRequest(
                $"adv-{Guid.NewGuid():N}", employee.Id, "advance", 10_000, "سلفة", shiftId, _managerId, approval!.ApprovalId, _managerId));
        Assert.True(advance.IsSuccessStatusCode, await advance.Content.ReadAsStringAsync());

        var prepared = await (await _client.PostAsJsonAsync("/api/shifts/prepare-close",
            new PrepareShiftCloseRequest(shiftId, 40_000))).Content.ReadFromJsonAsync<PrepareShiftCloseResult>();

        // 500 float - 100 advance = 400 expected, and a count of 400 is therefore balanced.
        Assert.Equal(40_000, prepared!.ExpectedCashPiastres);
        Assert.Equal(0, prepared.DifferencePiastres);
    }

    // ---- Expense voucher ----

    [Fact]
    public async Task Recording_an_expense_queues_one_voucher_and_printing_it_moves_no_money()
    {
        var shiftId = await OpenShiftAsync($"open-{Guid.NewGuid():N}");
        var expense = await (await _client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest(
            $"exp-{Guid.NewGuid():N}", shiftId, "مستلزمات مطبخ", 16_000, "شراء أكياس", "tre-drawer", _cashierId, null, null)))
            .Content.ReadFromJsonAsync<AddExpenseResult>();

        var job = Assert.Single(await JobsAsync(expense!.ExpenseId));
        var text = await TicketTextAsync(job.Id);
        Assert.Contains("مصروف", text);
        Assert.Contains("160 ج", text);
        Assert.Contains("مستلزمات مطبخ", text);
        Assert.Contains("درج الكاشير", text);

        var movementsBefore = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());
        await TicketTextAsync(job.Id);   // composing the ticket again changes nothing
        Assert.Equal(movementsBefore, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync()));
    }

    [Fact]
    public async Task A_slip_reprint_is_marked_audited_and_posts_nothing()
    {
        var shiftId = await OpenShiftAsync($"open-{Guid.NewGuid():N}");
        var before = await FinancialFootprintAsync();

        var requestId = $"rp-{Guid.NewGuid():N}";
        var reprint = await _client.PostAsJsonAsync("/api/print-jobs/reprint-document",
            new ReprintDocumentRequest(requestId, "ShiftOpen", shiftId, _managerId, "العميل فقد الورقة"));
        reprint.EnsureSuccessStatusCode();

        var jobs = await JobsAsync(shiftId);
        var reprinted = Assert.Single(jobs, j => j.IsReprint);
        Assert.Contains("إعادة طباعة", await TicketTextAsync(reprinted.Id));

        // Replaying the same press returns the same job rather than queuing a second one.
        (await _client.PostAsJsonAsync("/api/print-jobs/reprint-document",
            new ReprintDocumentRequest(requestId, "ShiftOpen", shiftId, _managerId, null))).EnsureSuccessStatusCode();
        Assert.Equal(jobs.Count, (await JobsAsync(shiftId)).Count);

        Assert.Equal(before, await FinancialFootprintAsync());
        var audit = await _factory.QueryAsync(db => db.AuditEvents.AsNoTracking()
            .Where(a => a.ReferenceId == shiftId && a.Action.Contains("إعادة طباعة")).ToListAsync());
        Assert.Single(audit);
        Assert.Contains("العميل فقد الورقة", audit[0].Details + audit[0].Reason);
    }

    [Fact]
    public async Task Reprinting_a_document_that_does_not_exist_is_refused()
    {
        var response = await _client.PostAsJsonAsync("/api/print-jobs/reprint-document",
            new ReprintDocumentRequest($"rp-{Guid.NewGuid():N}", "Expense", "exp-does-not-exist", _managerId, null));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Order reprint ----

    [Fact]
    public async Task Reprinting_an_order_duplicates_no_money_stock_or_order_state()
    {
        var shiftId = await OpenShiftAsync($"open-{Guid.NewGuid():N}");
        var productId = await _factory.SeedProductAsync(10_000);
        var order = await (await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"co-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 10_000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null,
            BusinessDayId: null))).Content.ReadFromJsonAsync<CheckoutOrderResult>();

        var before = await FinancialFootprintAsync();
        var orderBefore = await _factory.QueryAsync(db => db.Orders.AsNoTracking().FirstAsync(o => o.Id == order!.OrderId));

        for (var i = 0; i < 2; i++)
            (await _client.PostAsJsonAsync($"/api/orders/{order!.OrderId}/print-jobs", new CreatePrintJobsRequest(
                RequestId: $"rp-{Guid.NewGuid():N}", OrderId: order.OrderId, CustomerRoute: "Receipt",
                IsReprint: true, ActorUserId: _managerId, IncludeKitchenRoutes: false, Reason: "الورقة اتمزقت")))
                .EnsureSuccessStatusCode();

        // Nothing financial moved, and the order itself is untouched.
        Assert.Equal(before, await FinancialFootprintAsync());
        var orderAfter = await _factory.QueryAsync(db => db.Orders.AsNoTracking().FirstAsync(o => o.Id == order!.OrderId));
        Assert.Equal(orderBefore.TotalPiastres, orderAfter.TotalPiastres);
        Assert.Equal(orderBefore.AmountPaidPiastres, orderAfter.AmountPaidPiastres);
        Assert.Equal(orderBefore.Status, orderAfter.Status);
        Assert.Equal(orderBefore.IsPaid, orderAfter.IsPaid);
        Assert.Equal(orderBefore.Version, orderAfter.Version);

        // Two separate presses are two separate reprint jobs, both marked, and no kitchen ticket.
        var jobs = await JobsAsync(order!.OrderId);
        Assert.Equal(2, jobs.Count(j => j.IsReprint));
        Assert.All(jobs, j => Assert.Equal("Receipt", j.Route));

        foreach (var job in jobs.Where(j => j.IsReprint))
        {
            var ticket = await (await _client.GetAsync($"/api/print-jobs/{job.Id}/ticket")).Content.ReadFromJsonAsync<ComposedTicketDto>();
            // Both pieces - the customer's token and the detailed receipt - say so.
            var cuts = ticket!.Lines.Count(l => l.Kind == "cut");
            Assert.Equal(cuts + 1, ticket.Lines.Count(l => l.Text == "إعادة طباعة"));
        }

        var audit = await _factory.QueryAsync(db => db.AuditEvents.AsNoTracking()
            .Where(a => a.ReferenceId == order.OrderId && a.Action == "إعادة طباعة طلب").ToListAsync());
        Assert.Equal(2, audit.Count);
        Assert.All(audit, a => Assert.Contains("الورقة اتمزقت", a.Details + a.Reason));
        Assert.All(audit, a => Assert.Equal("المدير", a.UserName));
    }

    /// <summary>Every row that would reveal a duplicated financial or stock effect.</summary>
    private Task<string> FinancialFootprintAsync() => _factory.QueryAsync(async db =>
        $"orders={await db.Orders.CountAsync()};" +
        $"treasury={await db.TreasuryMovements.CountAsync()}/{await db.TreasuryMovements.SumAsync(m => (long?)m.AmountPiastres) ?? 0};" +
        $"stock={await db.StockMovements.CountAsync()};" +
        $"journal={await db.FinancialJournalEntries.CountAsync()};" +
        $"refunds={await db.OrderRefunds.CountAsync()};" +
        $"expenses={await db.Expenses.CountAsync()}");
}

namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Expenses;
using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Application.Shifts;
using Falah.Domain.Printing;
using Falah.Infrastructure.Reports;
using Falah.Infrastructure.Shifts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// UI-REQ-002: one seeded shift end to end - float, cash/card/InstaPay(pending)/wallet sales, a cash
/// refund, a drawer expense and a main-treasury expense, a counted shortage with a reason, the close,
/// a confirmation that happens AFTER the close, a reprint and a retried close. The statement the close
/// froze must agree with the shift report and the persisted shift row, list what was still open, and
/// never change, duplicate or move money afterwards.
/// </summary>
public class ShiftCloseStatementTests : IAsyncLifetime
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

    private async Task<CheckoutOrderResult> SellAsync(string shiftId, long price, string method)
    {
        var productId = await _factory.SeedProductAsync(price);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _managerId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: method,
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: price, SplitPayments: null, WalletProvider: method == "wallet" ? "vodafone_cash" : null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    private async Task<List<string>> TicketTextAsync(string jobId)
    {
        var ticket = await (await _client.GetAsync($"/api/print-jobs/{jobId}/ticket")).Content.ReadFromJsonAsync<ComposedTicketDto>();
        return ticket!.Lines.SelectMany(TicketDtoText.Of).Where(t => t is not null).Select(t => t!).ToList();
    }

    private static async Task Ok(Task<HttpResponseMessage> call)
    {
        var r = await call;
        Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.RequestUri?.AbsolutePath} -> {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
    }

    private Task<int> CountAsync(Func<Falah.Infrastructure.Persistence.FalahDbContext, Task<int>> q) => _factory.QueryAsync(q);

    [Fact]
    public async Task The_close_freezes_a_statement_that_reconciles_and_never_changes_or_duplicates()
    {
        // ---- the shift ----
        var shiftId = (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _managerId, 50_000)))
            .Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;

        var cash = await SellAsync(shiftId, 20_000, "cash");
        await SellAsync(shiftId, 15_000, "card");
        var pending = await SellAsync(shiftId, 12_000, "instapay");          // left unconfirmed at close
        var wallet = await SellAsync(shiftId, 8_000, "wallet");
        await Ok(_client.PostAsJsonAsync("/api/orders/confirm-electronic-payment",
            new ConfirmElectronicPaymentRequest(wallet.OrderId, _managerId)));

        var refund = await _client.PostAsJsonAsync("/api/orders/refund",
            new RefundOrderRequest($"rf-{Guid.NewGuid():N}", cash.OrderId, 2_000, "صنف ناقص", _managerId, null, null));
        Assert.True(refund.IsSuccessStatusCode, await refund.Content.ReadAsStringAsync());

        (await _client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest(
            $"exp-{Guid.NewGuid():N}", shiftId, "مستلزمات تشغيل", 3_000, "شراء ثلج", "tre-drawer", _managerId, null, null)))
            .EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest(
            $"exp-{Guid.NewGuid():N}", shiftId, "صيانة سريعة", 5_000, "إصلاح ثلاجة", "tre-main", _managerId, null, null)))
            .EnsureSuccessStatusCode();

        // ---- the blind close, 10 short, with a reason ----
        var prepared = await (await _client.PostAsJsonAsync("/api/shifts/prepare-close",
            new PrepareShiftCloseRequest(shiftId, 0))).Content.ReadFromJsonAsync<PrepareShiftCloseResult>();
        var counted = prepared!.ExpectedCashPiastres - 1_000;
        var close = new CloseShiftRequest($"close-{Guid.NewGuid():N}", shiftId, _managerId, counted, "عجز بسيط", null, null);
        var closed = await (await _client.PostAsJsonAsync("/api/shifts/close", close)).Content.ReadFromJsonAsync<CloseShiftResult>();
        Assert.Equal("Closed", closed!.Status);

        // ---- the frozen statement reconciles with the shift report and the persisted shift row ----
        var statement = await _factory.QueryAsync(db => ShiftCloseStatements.LoadFrozenAsync(db, shiftId, default));
        Assert.NotNull(statement);
        using (var scope = _factory.Services.CreateScope())
        {
            var report = await scope.ServiceProvider.GetRequiredService<ReportingQueryService>().GetShiftSummaryAsync(shiftId);
            Assert.Equal(report.Sales.OrderCount, statement!.Sales.OrderCount);
            Assert.Equal(report.Sales.GrossSalesPiastres, statement.Sales.GrossSalesPiastres);
            Assert.Equal(report.Sales.NetSalesPiastres, statement.Sales.NetSalesPiastres);
            Assert.Equal(report.Sales.RefundsPiastres, statement.Sales.RefundsPiastres);
            Assert.Equal(report.Sales.CashPiastres, statement.Sales.CashPiastres);
            Assert.Equal(report.Sales.CardPiastres, statement.Sales.CardPiastres);
            Assert.Equal(report.Sales.WalletPiastres, statement.Sales.WalletPiastres);
            Assert.Equal(report.Sales.PendingElectronicSalesPiastres, statement.Sales.PendingElectronicPiastres);
            Assert.Equal(report.ExpensesTotalPiastres, statement.DrawerExpensesPiastres + statement.OtherAccountExpensesPiastres);
        }
        Assert.Equal(4, statement.Sales.OrderCount);
        Assert.Equal(55_000, statement.Sales.GrossSalesPiastres);
        Assert.Equal(2_000, statement.Sales.RefundsPiastres);
        Assert.Equal(12_000, statement.Sales.PendingElectronicPiastres);
        Assert.Equal(0, statement.Sales.InstaPayPiastres);

        Assert.Equal(new[] { ("مستلزمات تشغيل", 3_000L, true), ("صيانة سريعة", 5_000L, false) },
            statement.Expenses.Select(e => (e.Category, e.AmountPiastres, e.PaidFromDrawer)).ToArray());
        Assert.Equal(3_000, statement.Cash.DrawerExpensesPiastres);             // only the drawer one enters the cash count
        Assert.Equal(50_000 + 20_000 - 2_000 - 3_000, statement.Cash.ExpectedPiastres);

        var shift = await _factory.QueryAsync(db => db.Shifts.AsNoTracking().SingleAsync(s => s.Id == shiftId));
        Assert.Equal(shift.ExpectedCashPiastres, statement.Cash.ExpectedPiastres);
        Assert.Equal(shift.ActualCashPiastres, statement.Cash.CountedPiastres);
        Assert.Equal(shift.CashDifferencePiastres, statement.Cash.DifferencePiastres);
        Assert.Equal(-1_000, statement.Cash.DifferencePiastres);
        Assert.Equal("عجز بسيط", statement.DifferenceReason);
        var handover = await _factory.QueryAsync(db => db.TreasuryMovements.AsNoTracking().SingleAsync(m => m.ShiftId == shiftId && m.Type == "shift_handover"));
        Assert.Equal(handover.AmountPiastres, statement.Cash.HandedOverPiastres);
        Assert.Contains(statement.Outstanding, o => o.Contains(pending.OrderNumber));

        // ---- the printed statement states the same figures ----
        var closeJob = await _factory.QueryAsync(db => db.PrintJobs.AsNoTracking().SingleAsync(j => j.OrderId == shiftId && j.OrderNumber == PrintRules.ShiftCloseLabel));
        var original = await TicketTextAsync(closeJob.Id);
        foreach (var heading in new[] { "المبيعات", "طرق الدفع", "المصروفات", "نقدية الدرج", "التسليم", "بنود مفتوحة عند التسليم" })
            Assert.Contains(heading, original);
        Assert.Contains("شراء ثلج - من الدرج", string.Join("\n", original));
        Assert.Contains("إصلاح ثلاجة - من الخزينة الرئيسية", string.Join("\n", original));
        Assert.Contains("الفرق - عجز", original);

        // ---- after the close: the pending transfer is confirmed, then the statement is reprinted ----
        // A confirmation needs an open shift: it happens during the NEXT shift, as it would at the till.
        await Ok(_client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "مسائية", _managerId, 10_000)));
        var movementsBefore = await CountAsync(db => db.TreasuryMovements.CountAsync());
        await Ok(_client.PostAsJsonAsync("/api/orders/confirm-electronic-payment",
            new ConfirmElectronicPaymentRequest(pending.OrderId, _managerId)));
        var movementsAfterConfirm = await CountAsync(db => db.TreasuryMovements.CountAsync());

        await Ok(_client.PostAsJsonAsync("/api/print-jobs/reprint-document",
            new ReprintDocumentRequest($"rp-{Guid.NewGuid():N}", "ShiftClose", shiftId, _managerId, "نسخة للمحاسب")));
        var reprintJob = await _factory.QueryAsync(db => db.PrintJobs.AsNoTracking().SingleAsync(j => j.OrderId == shiftId && j.IsReprint));
        var reprinted = await TicketTextAsync(reprintJob.Id);
        // Same paper as the close produced, plus the reprint banner - the later confirmation moved nothing.
        Assert.Contains("إعادة طباعة", reprinted);
        Assert.Equal(original, reprinted.Skip(reprinted.Count - original.Count).ToList());
        Assert.Equal(movementsAfterConfirm, await CountAsync(db => db.TreasuryMovements.CountAsync()));
        Assert.True(movementsAfterConfirm > movementsBefore);                    // the confirmation itself posted, the reprint did not

        // ---- a retried close is a replay: no second close, statement, handover or paper ----
        var retry = await (await _client.PostAsJsonAsync("/api/shifts/close", close)).Content.ReadFromJsonAsync<CloseShiftResult>();
        Assert.True(retry!.WasReplayed);
        Assert.Equal(1, await CountAsync(db => db.AuditEvents.CountAsync(a => a.ShiftId == shiftId && a.NewValue != null && a.NewValue.StartsWith("{\"Kind\":\"shift-close-statement\""))));
        Assert.Equal(1, await CountAsync(db => db.TreasuryMovements.CountAsync(m => m.ShiftId == shiftId && m.Type == "shift_handover")));
        Assert.Equal(1, await CountAsync(db => db.PrintJobs.CountAsync(j => j.OrderId == shiftId && j.OrderNumber == PrintRules.ShiftCloseLabel && !j.IsReprint)));
        Assert.Equal(movementsAfterConfirm, await CountAsync(db => db.TreasuryMovements.CountAsync()));
    }

    /// <summary>A shift closed before statements were frozen still prints, rebuilt from its records
    /// around the persisted closing figures, and the paper says it was recomputed.</summary>
    [Fact]
    public async Task A_shift_closed_before_statements_were_frozen_prints_a_recomputed_statement_that_says_so()
    {
        var shiftId = (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _managerId, 50_000)))
            .Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
        await Ok(_client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"close-{Guid.NewGuid():N}", shiftId, _managerId, 50_000, null, null, null)));
        // Simulate a pre-change close: remove the frozen copy from the TEST database only.
        await _factory.QueryAsync(async db =>
        {
            var events = await db.AuditEvents.Where(a => a.ShiftId == shiftId && a.NewValue != null).ToListAsync();
            foreach (var e in events) e.NewValue = null;
            return await db.SaveChangesAsync();
        });

        var job = await _factory.QueryAsync(db => db.PrintJobs.AsNoTracking().SingleAsync(j => j.OrderId == shiftId && j.OrderNumber == PrintRules.ShiftCloseLabel));
        var text = await TicketTextAsync(job.Id);
        Assert.Contains("محسوبة من السجلات الحالية - أُغلقت قبل حفظ ورقة الإغلاق", text);
        Assert.Contains("الفرق - مطابق", text);
    }
}

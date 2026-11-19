namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Purchasing;
using Falah.Application.Shifts;
using Falah.Application.Treasury;
using Falah.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// P3: treasury entry errors and purchasing errors are corrected by linked entries that keep the original, are
/// authorised, replay idempotently, and leave every balance - treasury, drawer, supplier - exactly right.
/// Amounts in piastres. Main is funded with 1,000,000.
/// </summary>
public class FinancialCorrectionWorkflowTests : IAsyncLifetime
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
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "safe_drop");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync(1_000_000);
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private static async Task<HttpResponseMessage> Ok(Task<HttpResponseMessage> call)
    {
        var r = await call;
        Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.RequestUri?.AbsolutePath} -> {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return r;
    }

    private Task<long> BalanceAsync(string account) => _factory.QueryAsync(db => db.TreasuryMovements.AsNoTracking()
        .Where(m => m.Status == "Posted" && (m.SourceAccountId == account || m.DestinationAccountId == account))
        .SumAsync(m => m.DestinationAccountId == account ? m.AmountPiastres : -m.AmountPiastres));

    private Task<HttpResponseMessage> CorrectAsync(string movementId, string actorId, string? requestId = null, bool confirm = true, long? amount = null) =>
        _factory.PostAsSessionOfAsync(_client, actorId, $"/api/treasury/movements/{movementId}/correct",
            new CorrectTreasuryMovementRequest(requestId ?? $"corr-{Guid.NewGuid():N}", movementId, "حُوِّل بالخطأ - لم يخرج المبلغ", actorId, confirm, amount));

    [Fact]
    public async Task A_transfer_that_never_happened_is_undone_by_its_linked_opposite_once()
    {
        var transfer = (await (await Ok(_factory.PostAsSessionOfAsync(_client, _managerId, "/api/treasury/transfer",
            new TransferRequest($"tr-{Guid.NewGuid():N}", "tre-main", "tre-bank", 200_000, "إيداع بنكي", _managerId, null, null))))
            .Content.ReadFromJsonAsync<TransferResult>())!;
        Assert.Equal(200_000, await BalanceAsync("tre-bank"));

        // Saying it did not happen is required: money that moved and came back is a new transfer.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await CorrectAsync(transfer.MovementId, _managerId, confirm: false)).StatusCode);

        var requestId = $"corr-{Guid.NewGuid():N}";
        var result = (await (await Ok(CorrectAsync(transfer.MovementId, _managerId, requestId))).Content.ReadFromJsonAsync<CorrectTreasuryMovementResult>())!;
        Assert.Equal(("tre-bank", "tre-main", 200_000L, "transfer"), (result.SourceAccountId, result.DestinationAccountId, result.AmountPiastres, result.OriginalType));
        Assert.Equal(0, await BalanceAsync("tre-bank"));
        Assert.Equal(1_000_000, await BalanceAsync("tre-main"));

        Assert.True((await (await Ok(CorrectAsync(transfer.MovementId, _managerId, requestId))).Content.ReadFromJsonAsync<CorrectTreasuryMovementResult>())!.WasReplayed);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await CorrectAsync(transfer.MovementId, _managerId)).StatusCode);
        // The original is kept as it was.
        Assert.Equal(2, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Id == transfer.MovementId || m.ReferenceId == transfer.MovementId)));
        Assert.True(await _factory.QueryAsync(db => db.AuditEvents.AnyAsync(a => a.ReferenceId == transfer.MovementId && a.Action == "تصحيح حركة خزينة")));
    }

    [Fact]
    public async Task A_safe_drop_of_the_wrong_amount_is_corrected_in_the_shift_and_the_drawer_still_closes_exactly()
    {
        var shiftId = (await (await Ok(_client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 100_000)))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
        // 50,000 typed; 5,000 really went into the safe.
        var drop = (await (await Ok(_client.PostAsJsonAsync("/api/treasury/safe-drop",
            new SafeDropRequest($"drop-{Guid.NewGuid():N}", shiftId, 50_000, "تأمين", _cashierId, null, null)))).Content.ReadFromJsonAsync<SafeDropResult>())!;

        await Ok(CorrectAsync(drop.MovementId, _managerId, amount: 45_000));

        var expected = await _factory.ExpectedDrawerCashAsync(shiftId);
        Assert.Equal(95_000, expected);
        var close = (await (await Ok(_client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"close-{Guid.NewGuid():N}", shiftId, _cashierId, 95_000, null, null, null)))).Content.ReadFromJsonAsync<CloseShiftResult>())!;
        Assert.Equal(0, close.DifferencePiastres);
        Assert.Equal(0, await BalanceAsync("tre-drawer"));
    }

    [Fact]
    public async Task Only_hand_typed_entries_are_corrected_here_and_only_by_treasury_authority()
    {
        var shiftId = (await (await Ok(_client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 100_000)))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
        var opening = await _factory.QueryAsync(db => db.TreasuryMovements.Where(m => m.Type == "shift_opening_float").Select(m => m.Id).SingleAsync());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await CorrectAsync(opening, _managerId)).StatusCode);

        var drop = (await (await Ok(_client.PostAsJsonAsync("/api/treasury/safe-drop",
            new SafeDropRequest($"drop-{Guid.NewGuid():N}", shiftId, 10_000, "تأمين", _cashierId, null, null)))).Content.ReadFromJsonAsync<SafeDropResult>())!;
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await CorrectAsync(drop.MovementId, _cashierId)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await CorrectAsync(drop.MovementId, _managerId, amount: 20_000)).StatusCode);
        Assert.Equal(0, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "movement_correction")));
    }

    // ---------------------------------------------------------------- supplier invoices

    private async Task<(string PurchaseId, string ItemId, string SupplierId)> InvoiceAsync(long paid = 0)
    {
        var (_, _, itemId) = await _factory.SeedProductWithRecipeAsync(5_000, 1_000);
        var supplierId = (await (await Ok(_client.PostAsJsonAsync("/api/suppliers",
            new AddSupplierRequest($"req-{Guid.NewGuid():N}", "مورد الخضار", null, null, null, null, _managerId, null, null))))
            .Content.ReadFromJsonAsync<AddSupplierResult>())!.SupplierId;
        // 10 kg at 2,000 per kg = 20,000.
        var purchase = (await (await Ok(_client.PostAsJsonAsync("/api/purchases", new ReceivePurchaseRequest(
            $"req-{Guid.NewGuid():N}", supplierId, "INV-7", new[] { new PurchaseLineRequest(itemId, 10_000, 2_000) },
            paid, paid > 0 ? "cash" : "credit", _managerId, null, null, null)))).Content.ReadFromJsonAsync<PurchaseDto>())!;
        return (purchase.Id, itemId, supplierId);
    }

    private Task<HttpResponseMessage> CreditAsync(string purchaseId, string kind, long? amount, string? requestId = null, params SupplierReturnLineRequest[] lines) =>
        _factory.PostAsSessionOfAsync(_client, _managerId, $"/api/purchases/{purchaseId}/credits",
            new RecordSupplierCreditRequest(requestId ?? $"cr-{Guid.NewGuid():N}", purchaseId, kind, amount, "سعر خاطئ في الفاتورة", _managerId,
                lines.Length == 0 ? null : lines));

    private Task<long> OutstandingAsync(string supplierId) => _factory.QueryAsync(db => db.SupplierLedgerEntries
        .Where(e => e.SupplierId == supplierId).SumAsync(e => e.DebitPiastres - e.CreditPiastres));

    [Fact]
    public async Task An_invoice_entered_wrong_is_credited_on_the_supplier_ledger_without_moving_money()
    {
        var (purchaseId, _, supplierId) = await InvoiceAsync();
        var movementsBefore = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());
        var requestId = $"cr-{Guid.NewGuid():N}";

        var credit = (await (await Ok(CreditAsync(purchaseId, "invoice_error", 3_000, requestId))).Content.ReadFromJsonAsync<SupplierCreditResult>())!;

        Assert.Equal((3_000L, 17_000L), (credit.AmountPiastres, credit.RemainingAmountPiastres));
        Assert.Equal(17_000, await OutstandingAsync(supplierId));
        Assert.Equal(movementsBefore, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync()));
        var invoice = await _factory.QueryAsync(db => db.Purchases.AsNoTracking().SingleAsync(p => p.Id == purchaseId));
        Assert.Equal((20_000L, 17_000L), (invoice.TotalPiastres, invoice.RemainingAmountPiastres));   // the invoice as received is kept

        Assert.True((await (await Ok(CreditAsync(purchaseId, "invoice_error", 3_000, requestId))).Content.ReadFromJsonAsync<SupplierCreditResult>())!.WasReplayed);
        Assert.Equal(17_000, await OutstandingAsync(supplierId));
        // Never more than is still owed: a paid invoice's credit is money the supplier owes back - an owner decision.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await CreditAsync(purchaseId, "supplier_credit", 17_001)).StatusCode);
    }

    [Fact]
    public async Task Goods_returned_leave_stock_at_the_invoices_own_cost_and_never_more_than_was_received()
    {
        var (purchaseId, itemId, supplierId) = await InvoiceAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await CreditAsync(purchaseId, "goods_returned", null, null, new SupplierReturnLineRequest(itemId, 10_001))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await CreditAsync(purchaseId, "goods_returned", 9_999, null, new SupplierReturnLineRequest(itemId, 2_500))).StatusCode);

        var credit = (await (await Ok(CreditAsync(purchaseId, "goods_returned", null, null, new SupplierReturnLineRequest(itemId, 2_500))))
            .Content.ReadFromJsonAsync<SupplierCreditResult>())!;
        Assert.Equal(5_000, credit.AmountPiastres);   // 2.5 kg x 2,000
        Assert.Equal(15_000, await OutstandingAsync(supplierId));

        var stock = await _factory.QueryAsync(db => db.StockMovements.AsNoTracking().Where(m => m.InventoryItemId == itemId)
            .SumAsync(m => m.Direction == "In" ? m.QuantityMilliUnits : -m.QuantityMilliUnits));
        Assert.Equal(7_500, stock);
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await CreditAsync(purchaseId, "goods_returned", null, null, new SupplierReturnLineRequest(itemId, 7_501))).StatusCode);
    }
}

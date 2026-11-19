namespace Falah.Infrastructure.Shifts;

using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Domain.Printing;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Expected-cash computation shared by prepare (preview) and close (authoritative
/// recompute - a close request's own supplied "expected" is never trusted, only actualCash is).
/// Ported from calculateShiftExpectedCash in the current app's lib/store.tsx.</summary>
public static class ShiftReconciliation
{
    /// <summary>
    /// Every posted movement type that takes cash OUT of this shift's drawer by a hand, as opposed
    /// to the close itself moving the counted money on.
    ///
    /// employee_advance and employee_withdrawal are here because of a confirmed defect: paying an
    /// employee out of the drawer (RecordEmployeeCashOutHandler, always sourced from tre-drawer and
    /// tagged with the shift) removed real cash while expected cash stayed where it was, so every
    /// such shift closed with a false shortage exactly equal to what was handed over. The close's
    /// own movements - shift_handover, cash_overage, cash_shortage - are deliberately NOT here: they
    /// are posted from the counted figure after this runs, and counting them would feed the close
    /// back into its own input.
    /// </summary>
    private static readonly string[] DrawerWithdrawalTypes = { "cash_out", "employee_advance", "employee_withdrawal", "void_return" };

    public static async Task<Money> ComputeExpectedCashAsync(FalahDbContext db, string shiftId, Money openingCash, CancellationToken ct) =>
        (await ComputeBreakdownAsync(db, shiftId, openingCash, ct)).ExpectedCash;

    /// <summary>
    /// The same computation, with every component kept separate so the handover slip can state how
    /// the expected figure was reached. ActualCash and Difference are left zero here; the close
    /// fills them in from what the cashier counted.
    ///
    /// Cash sales are this shift's own orders at their full paid value; refunds are the cash that
    /// actually left THIS drawer, read from the refund movements posted against this shift. The two
    /// are deliberately not the same set of orders. A closed shift is immutable, so a refund of one
    /// of its orders is posted against the shift open when it happens (RefundRules.
    /// ResolveOperationalShift) and leaves that drawer, not the original one. Netting refunds against
    /// the shift's own orders instead (Total - RefundAmount) charged the refund to a drawer it never
    /// left, missed it in the drawer it did leave, and rewrote a closed shift's handover breakdown
    /// whenever one of its orders was refunded later.
    /// </summary>
    public static async Task<ShiftCashBreakdown> ComputeBreakdownAsync(FalahDbContext db, string shiftId, Money openingCash, CancellationToken ct)
    {
        var orderEntities = await db.Orders.AsNoTracking()
            .Include(o => o.Items).Include(o => o.Refunds)
            .Where(o => o.ShiftId == shiftId)
            .ToListAsync(ct);
        var orders = orderEntities.Select(OrderMapper.ToDomain).ToList();
        // CashSalesFromOrders is net of each order's refunds and CashRefundsFromOrders is exactly those
        // refunds (same conditions, by design), so their sum is the gross cash taken - independent of
        // when, or in which shift, any of it was later handed back.
        var grossCashSales = OrderPaymentQueries.CashSalesFromOrders(orders) + OrderPaymentQueries.CashRefundsFromOrders(orders);

        // A void does not erase cash that this shift really collected. Its separate return
        // movement belongs to the shift that physically hands it back (possibly a later one).
        var voidedOrderIds = orderEntities.Where(o => o.Status == "cancelled").Select(o => o.Id).ToList();
        grossCashSales += Money.FromPiastres(await db.TreasuryMovements.AsNoTracking()
            .Where(m => voidedOrderIds.Contains(m.ReferenceId) && m.Type == "sale_collection"
                && m.DestinationAccountId == TreasuryAccounts.Drawer && m.Status == nameof(MovementStatus.Posted))
            .SumAsync(m => (long?)m.AmountPiastres, ct) ?? 0);

        var cashRefunds = Money.FromPiastres(await db.TreasuryMovements.AsNoTracking()
            .Where(m => m.ShiftId == shiftId && m.Status == nameof(MovementStatus.Posted)
                && m.Type == "refund" && m.SourceAccountId == TreasuryAccounts.Drawer)
            .SumAsync(m => (long?)m.AmountPiastres, ct) ?? 0);
        var netCashSales = grossCashSales - cashRefunds;

        // Matches calculateShiftExpectedCash's own shiftExpenses filter exactly: only expenses paid
        // FROM THIS SHIFT'S DRAWER count against it - an expense paid from Main Treasury/bank/
        // InstaPay never touches this shift's cash reconciliation at all.
        var drawerExpenses = Money.FromPiastres(await db.Expenses.AsNoTracking()
            .Where(e => e.ShiftId == shiftId && e.PaidFromDrawer)
            .SumAsync(e => e.AmountPiastres, ct));

        var movements = await db.TreasuryMovements.AsNoTracking()
            .Where(m => m.ShiftId == shiftId && m.Status == nameof(MovementStatus.Posted)
                && (m.Type == "cash_in" || m.Type == "safe_drop" || (DrawerWithdrawalTypes.Contains(m.Type) && m.SourceAccountId == TreasuryAccounts.Drawer)))
            .ToListAsync(ct);
        // A treasury entry of this shift recorded by mistake (a safe drop, a cash in/out) is corrected by its linked
        // opposite against the same shift (CorrectTreasuryMovementHandler): money the drawer never gave or never got.
        // Read by direction, so a corrected safe drop brings expected cash back up and a corrected cash in takes it down.
        var corrections = await db.TreasuryMovements.AsNoTracking()
            .Where(m => m.ShiftId == shiftId && m.Status == nameof(MovementStatus.Posted) && m.Type == TreasuryCorrectionRules.MovementType
                && (m.SourceAccountId == TreasuryAccounts.Drawer || m.DestinationAccountId == TreasuryAccounts.Drawer))
            .Select(m => new { m.SourceAccountId, m.AmountPiastres })
            .ToListAsync(ct);
        var correctionsIn = Money.FromPiastres(corrections.Where(m => m.SourceAccountId != TreasuryAccounts.Drawer).Sum(m => m.AmountPiastres));
        var correctionsOut = Money.FromPiastres(corrections.Where(m => m.SourceAccountId == TreasuryAccounts.Drawer).Sum(m => m.AmountPiastres));

        var cashIn = Money.FromPiastres(movements.Where(m => m.Type == "cash_in").Sum(m => m.AmountPiastres)) + correctionsIn;
        var withdrawals = Money.FromPiastres(movements.Where(m => DrawerWithdrawalTypes.Contains(m.Type)).Sum(m => m.AmountPiastres)) + correctionsOut;
        var safeDrops = Money.FromPiastres(movements.Where(m => m.Type == "safe_drop").Sum(m => m.AmountPiastres));

        // A driver settles with the cashier who receives his money: his net cash came INTO this drawer, and a fee
        // the restaurant owed him on prepaid deliveries went OUT of it. Neither is a sale; the orders were.
        var driverMovements = await db.TreasuryMovements.AsNoTracking()
            .Where(m => m.ShiftId == shiftId && m.Status == nameof(MovementStatus.Posted)
                && ((m.Type == "driver_settlement" && m.DestinationAccountId == TreasuryAccounts.Drawer)
                    || (m.Type == "driver_fee_payout" && m.SourceAccountId == TreasuryAccounts.Drawer)))
            .Select(m => new { m.Type, m.AmountPiastres })
            .ToListAsync(ct);
        var driverCash = Money.FromPiastres(driverMovements.Where(m => m.Type == "driver_settlement").Sum(m => m.AmountPiastres));
        var driverPayouts = Money.FromPiastres(driverMovements.Where(m => m.Type == "driver_fee_payout").Sum(m => m.AmountPiastres));

        var expected = FinancialRules.CalculateExpectedDrawerCash(
            openingCash, netCashSales, drawerExpenses, cashIn + driverCash, withdrawals + safeDrops + driverPayouts);

        return new ShiftCashBreakdown(
            OpeningCash: openingCash,
            CashSales: grossCashSales,
            Refunds: cashRefunds,
            CashIn: cashIn,
            Expenses: drawerExpenses,
            Withdrawals: withdrawals,
            SafeDrops: safeDrops,
            ExpectedCash: expected,
            ActualCash: Money.Zero,
            Difference: Money.Zero,
            DriverCash: driverCash,
            DriverPayouts: driverPayouts);
    }
}

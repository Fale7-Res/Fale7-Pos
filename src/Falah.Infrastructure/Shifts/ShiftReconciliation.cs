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
internal static class ShiftReconciliation
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
    private static readonly string[] DrawerWithdrawalTypes = { "cash_out", "employee_advance", "employee_withdrawal" };

    public static async Task<Money> ComputeExpectedCashAsync(FalahDbContext db, string shiftId, Money openingCash, CancellationToken ct) =>
        (await ComputeBreakdownAsync(db, shiftId, openingCash, ct)).ExpectedCash;

    /// <summary>
    /// The same computation, with every component kept separate so the handover slip can state how
    /// the expected figure was reached. ActualCash and Difference are left zero here; the close
    /// fills them in from what the cashier counted.
    ///
    /// Refunds are reported on their own line but are NOT subtracted again: cash sales already come
    /// back net of them (OrderPaymentQueries counts Total - RefundAmount), so the slip shows the
    /// gross take and what went back out, and their net is what the drawer should hold.
    /// </summary>
    public static async Task<ShiftCashBreakdown> ComputeBreakdownAsync(FalahDbContext db, string shiftId, Money openingCash, CancellationToken ct)
    {
        var orderEntities = await db.Orders.AsNoTracking()
            .Include(o => o.Items).Include(o => o.Refunds)
            .Where(o => o.ShiftId == shiftId)
            .ToListAsync(ct);
        var orders = orderEntities.Select(OrderMapper.ToDomain).ToList();
        var netCashSales = OrderPaymentQueries.CashSalesFromOrders(orders);
        var cashRefunds = OrderPaymentQueries.CashRefundsFromOrders(orders);

        // Matches calculateShiftExpectedCash's own shiftExpenses filter exactly: only expenses paid
        // FROM THIS SHIFT'S DRAWER count against it - an expense paid from Main Treasury/bank/
        // InstaPay never touches this shift's cash reconciliation at all.
        var drawerExpenses = Money.FromPiastres(await db.Expenses.AsNoTracking()
            .Where(e => e.ShiftId == shiftId && e.PaidFromDrawer)
            .SumAsync(e => e.AmountPiastres, ct));

        var movements = await db.TreasuryMovements.AsNoTracking()
            .Where(m => m.ShiftId == shiftId && m.Status == nameof(MovementStatus.Posted)
                && (m.Type == "cash_in" || m.Type == "safe_drop" || DrawerWithdrawalTypes.Contains(m.Type)))
            .ToListAsync(ct);
        var cashIn = Money.FromPiastres(movements.Where(m => m.Type == "cash_in").Sum(m => m.AmountPiastres));
        var withdrawals = Money.FromPiastres(movements.Where(m => DrawerWithdrawalTypes.Contains(m.Type)).Sum(m => m.AmountPiastres));
        var safeDrops = Money.FromPiastres(movements.Where(m => m.Type == "safe_drop").Sum(m => m.AmountPiastres));

        var expected = FinancialRules.CalculateExpectedDrawerCash(
            openingCash, netCashSales, drawerExpenses, cashIn, withdrawals + safeDrops);

        return new ShiftCashBreakdown(
            OpeningCash: openingCash,
            CashSales: netCashSales + cashRefunds,
            Refunds: cashRefunds,
            CashIn: cashIn,
            Expenses: drawerExpenses,
            Withdrawals: withdrawals,
            SafeDrops: safeDrops,
            ExpectedCash: expected,
            ActualCash: Money.Zero,
            Difference: Money.Zero);
    }
}

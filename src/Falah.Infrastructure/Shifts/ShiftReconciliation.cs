namespace Falah.Infrastructure.Shifts;

using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Expected-cash computation shared by prepare (preview) and close (authoritative
/// recompute - a close request's own supplied "expected" is never trusted, only actualCash is).
/// Ported from calculateShiftExpectedCash in the current app's lib/store.tsx.</summary>
internal static class ShiftReconciliation
{
    public static async Task<Money> ComputeExpectedCashAsync(FalahDbContext db, string shiftId, Money openingCash, CancellationToken ct)
    {
        var orderEntities = await db.Orders.AsNoTracking()
            .Include(o => o.Items).Include(o => o.Refunds)
            .Where(o => o.ShiftId == shiftId)
            .ToListAsync(ct);
        var cashSales = OrderPaymentQueries.CashSalesFromOrders(orderEntities.Select(OrderMapper.ToDomain));

        // Matches calculateShiftExpectedCash's own shiftExpenses filter exactly: only expenses paid
        // FROM THIS SHIFT'S DRAWER count against it - an expense paid from Main Treasury/bank/
        // InstaPay never touches this shift's cash reconciliation at all.
        var drawerExpenses = Money.FromPiastres(await db.Expenses.AsNoTracking()
            .Where(e => e.ShiftId == shiftId && e.PaidFromDrawer)
            .SumAsync(e => e.AmountPiastres, ct));

        var movements = await db.TreasuryMovements.AsNoTracking()
            .Where(m => m.ShiftId == shiftId && m.Status == nameof(MovementStatus.Posted)
                && (m.Type == "cash_in" || m.Type == "cash_out" || m.Type == "safe_drop"))
            .ToListAsync(ct);
        var cashIn = Money.FromPiastres(movements.Where(m => m.Type == "cash_in").Sum(m => m.AmountPiastres));
        var cashOut = Money.FromPiastres(movements.Where(m => m.Type is "cash_out" or "safe_drop").Sum(m => m.AmountPiastres));

        return FinancialRules.CalculateExpectedDrawerCash(openingCash, cashSales, drawerExpenses, cashIn, cashOut);
    }
}

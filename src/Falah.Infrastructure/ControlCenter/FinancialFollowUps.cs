namespace Falah.Infrastructure.ControlCenter;

using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The money questions still open for a business day, read once for both the live control center and the close
/// snapshot so the two lists cannot disagree:
/// - handovers of the day's shifts the receiver has not confirmed, or counted differently (P4);
/// - delivered orders whose collection shortfall nobody has classified yet (AUD-03) - system-wide, like unsettled
///   custody, because an unresolved shortfall from yesterday is still unresolved today.
/// </summary>
internal static class FinancialFollowUps
{
    public static async Task<List<(string ShiftId, string ShiftName, string Status)>> UnconfirmedHandoversAsync(
        FalahDbContext db, BusinessDayEntity day, CancellationToken ct) =>
        (await Falah.Infrastructure.BusinessDay.BusinessDayShifts.Of(db.Shifts.AsNoTracking(), day)
            .Where(s => s.Status == "Closed" && (s.HandoverReceiptStatus == "pending" || s.HandoverReceiptStatus == "disputed"))
            .Select(s => new { s.Id, s.ShiftName, s.HandoverReceiptStatus })
            .ToListAsync(ct))
        .Select(s => (s.Id, s.ShiftName, s.HandoverReceiptStatus!)).ToList();

    public static async Task<List<(string DeliveryId, string? OrderNumber, long AmountPiastres)>> UnresolvedShortfallsAsync(
        FalahDbContext db, CancellationToken ct) =>
        (await (from d in db.Deliveries.AsNoTracking()
                join c in db.DriverCustodies.AsNoTracking() on d.Id equals c.DeliveryId
                join o in db.Orders.AsNoTracking() on d.OrderId equals o.Id
                where d.DeliveredAtUtc != null && c.CollectedPiastres < c.ExpectedPiastres
                    && !db.CollectionShortfallResolutions.Any(r => r.DeliveryId == d.Id)
                select new { d.Id, o.OrderNumber, Amount = c.ExpectedPiastres - c.CollectedPiastres })
            .ToListAsync(ct))
        .Select(x => (x.Id, (string?)x.OrderNumber, x.Amount)).ToList();
}

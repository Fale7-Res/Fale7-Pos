namespace Falah.Infrastructure.Delivery;

using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// What each driver still owes, or is owed, after his latest settlement. A partial settlement carries its
/// remainder forward (DriverSettlementEntity.CarriedOutPiastres): positive means the driver owes the restaurant,
/// negative means the restaurant owes him. A final settlement carries nothing.
///
/// This is restaurant money outside the drawer, like open custody, so it is shown and blocks the day close in the
/// same way. It is never dropped.
/// </summary>
public static class DriverBalances
{
    public sealed record OpenBalance(string DriverId, string DriverName, long CarriedPiastres);

    public static async Task<IReadOnlyList<OpenBalance>> OpenAsync(FalahDbContext db, CancellationToken ct)
    {
        var rows = await db.DriverSettlements.AsNoTracking()
            .Select(s => new { s.Id, s.DriverId, s.DriverName, s.CarriedOutPiastres, s.CreatedAtUtc })
            .ToListAsync(ct);
        return rows.GroupBy(r => r.DriverId)
            .Select(g => g.OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id).First())
            .Where(r => r.CarriedOutPiastres != 0)
            .Select(r => new OpenBalance(r.DriverId, r.DriverName ?? r.DriverId, r.CarriedOutPiastres))
            .ToList();
    }

    public static async Task<long> CarriedForDriverAsync(FalahDbContext db, string driverId, CancellationToken ct) =>
        await db.DriverSettlements.AsNoTracking().Where(s => s.DriverId == driverId)
            .OrderByDescending(s => s.CreatedAtUtc).ThenByDescending(s => s.Id)
            .Select(s => (long?)s.CarriedOutPiastres).FirstOrDefaultAsync(ct) ?? 0;
}

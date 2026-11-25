namespace Falah.Infrastructure.Orders;
using Falah.Application.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

public static class OrderReadProjection
{
    public static async Task<IReadOnlyList<OrderDetailsDto>> MapAsync(FalahDbContext db, IReadOnlyList<OrderEntity> orders, CancellationToken ct)
    {
        var ids = orders.Where(o => !o.IsPaid && o.TotalPiastres > 0).Select(o => o.Id).ToList();
        var settlements = ids.Count == 0 ? new Dictionary<string, long>()
            : (await db.DriverCustodies.AsNoTracking().Where(c => ids.Contains(c.OrderId) && c.Status == "Settled")
                .Select(c => new { c.OrderId, c.CollectedPiastres }).ToListAsync(ct))
                .GroupBy(c => c.OrderId).ToDictionary(g => g.Key, g => g.Max(c => c.CollectedPiastres));
        return orders.Select(o => OrderMapper.ToDto(o) with {
            CustomerFundsCollected = o.IsPaid || o.OrderType == "delivery" && o.PaymentMethod == "cash"
                && o.TotalPiastres > 0 && settlements.GetValueOrDefault(o.Id) >= o.TotalPiastres,
            AwaitingCollection = o.AwaitingCollection && o.Status != "cancelled",
            HandedOverAtUtc = o.HandedOverAtUtc,
            HandedOverBy = o.HandedOverBy,
        }).ToList();
    }
}

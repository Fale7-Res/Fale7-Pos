namespace Falah.Infrastructure.Pricing;

using Falah.Domain.Common;
using Falah.Domain.Pricing;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Owner, 2026-11-25: how many pieces each offer has sold - today (the Cairo calendar day) and over its whole
/// life - for its limits. A cancelled order gives its pieces back; any other order counts, refunded or not, because
/// the offer was used.</summary>
public static class OfferUsage
{
    public readonly record struct Sold(int Today, int Total);

    public static async Task<Dictionary<string, Sold>> SoldAsync(FalahDbContext db, IReadOnlyCollection<PriceRule> rules, DateTimeOffset nowUtc, CancellationToken ct)
    {
        var ids = rules.Where(r => r.MaxPerDay is not null || r.MaxTotal is not null).Select(r => r.Id).ToList();
        if (ids.Count == 0) return new();
        var (dayStart, dayEnd) = BusinessClock.UtcRangeForBusinessDate(BusinessClock.BusinessDate(nowUtc));
        var lines = await (from i in db.OrderItems.AsNoTracking()
                           join o in db.Orders.AsNoTracking() on i.OrderId equals o.Id
                           where i.PriceRuleId != null && ids.Contains(i.PriceRuleId) && o.Status != "cancelled"
                           group new { i.Quantity, o.CreatedAtUtc } by i.PriceRuleId into g
                           select new
                           {
                               RuleId = g.Key!,
                               Total = g.Sum(x => x.Quantity),
                               Today = g.Sum(x => x.CreatedAtUtc >= dayStart && x.CreatedAtUtc < dayEnd ? x.Quantity : 0),
                           }).ToListAsync(ct);
        return lines.ToDictionary(l => l.RuleId, l => new Sold(l.Today, l.Total));
    }

    /// <summary>Whether a limit is already used up, so the offer is not shown any more today (or ever).</summary>
    public static bool IsExhausted(PriceRule rule, Sold sold) =>
        (rule.MaxPerDay is { } day && sold.Today >= day) || (rule.MaxTotal is { } total && sold.Total >= total);
}

namespace Falah.Infrastructure.Reports;

using Falah.Domain.Common;
using Falah.Domain.Reports;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The refunds made in a period, as events of that period - the other half of every sales figure
/// SalesRules computes. A period is a span of Cairo calendar dates (by when the refund was made),
/// a shift (the shift that processed it, which for a refund of a closed shift's order is the shift
/// open at the time), or a business day (the business day its treasury movement was posted under).
/// Any combination narrows the same set; none at all means every refund ever made.
/// </summary>
internal static class RefundEventQueries
{
    public static async Task<IReadOnlyList<SalesRules.RefundEvent>> LoadAsync(
        FalahDbContext db, BusinessDateRange? range, string? processingShiftId, string? businessDayId, CancellationToken ct)
    {
        var refunds = db.OrderRefunds.AsNoTracking().AsQueryable();
        if (range is { } period)
        {
            var (start, end) = period.ToUtcRange();
            refunds = refunds.Where(r => r.CreatedAtUtc >= start && r.CreatedAtUtc < end);
        }
        // Not "is not null", for the same reason LoadSalesOrdersAsync gives: an empty shiftId query
        // value means "no shift filter", never "shift ''".
        if (!string.IsNullOrEmpty(processingShiftId))
            refunds = refunds.Where(r => r.OperationalShiftId == processingShiftId);
        if (!string.IsNullOrEmpty(businessDayId))
            refunds = refunds.Where(r => db.TreasuryMovements.Any(m => m.Id == r.TreasuryMovementId && m.BusinessDayId == businessDayId));

        var rows = await refunds
            .Join(db.Orders.AsNoTracking(), r => r.OrderId, o => o.Id, (r, o) => new
            {
                r.OrderId, r.AmountPiastres, r.PaymentMethod, o.OrderType, o.TotalPiastres, o.TaxAmountPiastres,
            })
            .ToListAsync(ct);

        return rows.Select(r => new SalesRules.RefundEvent(
                r.OrderId, Money.FromPiastres(r.AmountPiastres), OrderEnumMapping.ParsePaymentMethod(r.PaymentMethod),
                OrderEnumMapping.ParseOrderType(r.OrderType), Money.FromPiastres(r.TotalPiastres), Money.FromPiastres(r.TaxAmountPiastres)))
            .ToList();
    }
}

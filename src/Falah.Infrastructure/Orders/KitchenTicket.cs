namespace Falah.Infrastructure.Orders;

using Falah.Domain.Printing;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Workflow redesign (owner decision 2026-11-24): "the kitchen invoice was printed" is read from the print queue the
/// system already keeps - no status of its own is stored. The kitchen invoice is the order's first papers into the
/// restaurant: every non-reprint order paper except the driver's (DriverDispatch) and the pickup's handover receipt.
/// It counts as printed once an agent took it to the printer (Sending) or reported it printed (Succeeded); a paper
/// still waiting in the queue, or one that failed, has not reached the kitchen.
///
/// Used where it matters: cancelling a delivery or a pickup before the kitchen has the paper needs no admin code and
/// gives every ingredient back; after it, a manager authorises and says whether the food was prepared.
/// </summary>
public static class KitchenTicket
{
    public static Task<bool> IsPrintedAsync(FalahDbContext db, string orderId, CancellationToken ct) =>
        db.PrintJobs.AsNoTracking().AnyAsync(j => j.OrderId == orderId && !j.IsReprint
            && j.Route != nameof(PrintRoute.DriverDispatch)
            && j.DocumentKind != nameof(PrintDocumentKind.PickupReceipt)
            && (j.Status == nameof(PrintJobStatus.Succeeded) || j.Status == nameof(PrintJobStatus.Sending)), ct);
}

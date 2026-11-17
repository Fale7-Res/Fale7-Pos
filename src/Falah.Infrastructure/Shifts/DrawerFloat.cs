namespace Falah.Infrastructure.Shifts;

using Falah.Application.Shifts;
using Falah.Domain.Authorization;
using Falah.Domain.Shifts;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The float a close left in the drawer for the next cashier (owner, 2026-11-08), and who may receive a close's
/// handed-over cash. The open dialog reads the first to tell the cashier what they should find; OpenShiftHandler
/// reads it to open on that cash instead of issuing a second float from the main treasury.
/// </summary>
public static class DrawerFloat
{
    public sealed record Left(string ShiftId, string ShiftName, long RetainedPiastres, string? ClosedByUserId);

    /// <summary>The most recent close's retained float, when it left one; null on an empty drawer.</summary>
    public static async Task<Left?> LastRetainedAsync(FalahDbContext db, CancellationToken ct)
    {
        var last = await db.Shifts.AsNoTracking()
            .Where(s => s.Status == nameof(ShiftStatus.Closed) && s.ClosedAtUtc != null)
            .OrderByDescending(s => s.ClosedAtUtc).ThenByDescending(s => s.Id)
            .Select(s => new { s.Id, s.ShiftName, s.RetainedFloatPiastres, s.ClosedByUserId })
            .FirstOrDefaultAsync(ct);
        return last?.RetainedFloatPiastres is > 0 ? new Left(last.Id, last.ShiftName, last.RetainedFloatPiastres.Value, last.ClosedByUserId) : null;
    }

    public static async Task<DrawerFloatDto> LoadAsync(FalahDbContext db, CancellationToken ct)
    {
        var left = await LastRetainedAsync(db, ct);
        if (left is null) return new DrawerFloatDto(null, null, null, null);
        var by = left.ClosedByUserId is null ? null
            : await db.Users.AsNoTracking().Where(u => u.Id == left.ClosedByUserId).Select(u => u.Name).FirstOrDefaultAsync(ct);
        return new DrawerFloatDto(left.RetainedPiastres, left.ShiftId, left.ShiftName, by);
    }

    /// <summary>Active users who may receive a close's cash: the same authority that manages shifts.</summary>
    public static async Task<IReadOnlyList<HandoverReceiverDto>> ReceiversAsync(FalahDbContext db, CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking().Where(u => u.Active).OrderBy(u => u.Name).ToListAsync(ct);
        return users.Where(u => AuthorizationPolicy.HasPermission(OrderMapper.ToActor(u), "shifts_manage"))
            .Select(u => new HandoverReceiverDto(u.Id, u.Name)).ToList();
    }
}

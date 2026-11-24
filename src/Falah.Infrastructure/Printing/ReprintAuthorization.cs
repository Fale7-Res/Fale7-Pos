namespace Falah.Infrastructure.Printing;

using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>SEC-0030 (owner decision 2026-11-22): printing a paper again - an order's receipt or a
/// shift/expense slip - needs <see cref="Permission"/> (owners and managers hold it through "all"), or a
/// manager's approval issued for that action at the till. The first print of an order is not a reprint
/// and needs nothing. A reprint stays audited by the handler that queues it.</summary>
internal static class ReprintAuthorization
{
    public const string Permission = "print_reprint";

    public static async Task<AuthorizationPolicy.AuthorizationResult> AuthorizeAsync(
        FalahDbContext db, IApprovalRegistry registry, UserEntity actor, string? approvalId, string? approverUserId, string? reason, CancellationToken ct)
    {
        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(approvalId))
        {
            var approver = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == approverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(approvalId, approver.Id, approver.Name, approver.Role, DateTimeOffset.UtcNow, Permission, reason);
        }
        if (!AuthorizationPolicy.HasPermission(OrderMapper.ToActor(actor), Permission) && approval is null)
            throw new DomainException("إعادة الطباعة تحتاج اعتماد مدير.");
        return AuthorizationPolicy.AuthorizeCommand(registry, OrderMapper.ToActor(actor), Permission, approval, requireApproval: true);
    }
}

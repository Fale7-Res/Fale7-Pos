namespace Falah.Infrastructure.Users;

using Falah.Application.Users;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from the reference app's own Users screen deactivate/reactivate button - but
/// fixes two real gaps found there: (1) the owner account was fully eligible for this button (only
/// `u.id !== currentUser.id` guarded it - a manager holding users_manage could deactivate the owner
/// through the UI with nothing but a manager-approval prompt in the way); (2) it never wrote an audit
/// event at all (updateUser, the function the button ultimately calls, calls logAuditEvent zero
/// times). Both fixed here: owner is unconditionally immune, and every change is audited.</summary>
public sealed class SetUserActiveHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public SetUserActiveHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task HandleAsync(SetUserActiveRequest request, CancellationToken ct = default)
    {
        // Keep the surviving-administrator check and the update in one serializable transaction.
        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "users_manage");

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new InvalidOperationException("المستخدم المستهدف غير موجود.");
        UserManagementRules.AssertTargetIsNotOwner(target.Role, request.Active ? "إعادة تفعيل" : "تعطيل");
        // Ported from the reference app's own `u.id !== currentUser.id` guard on this exact button -
        // an admin can never lock themselves out by deactivating their own account.
        if (request.UserId == request.ActorUserId && !request.Active)
            throw new DomainException("لا يمكنك تعطيل حسابك الخاص.");

        if (target.Active == request.Active) return; // idempotent no-op - already in the requested state

        // The self-deactivation guard above covers the common case, but an actor acting on a one-off
        // manager approval rather than on a permission of their own can still deactivate the last
        // real administrator and close the door behind them.
        if (!request.Active && UserManagementRules.CanManageUsers(
                target.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
        {
            var othersWhoCanAdminister = await _db.Users.AsNoTracking()
                .Where(u => u.Active && u.Id != target.Id)
                .Select(u => u.Permissions)
                .ToListAsync(ct);

            UserManagementRules.AssertUserAdministrationSurvives(
                othersWhoCanAdminister.Count(permissions => UserManagementRules.CanManageUsers(
                    permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))));
        }

        target.Active = request.Active;
        _db.ControlAuditEvents.Add(new Falah.Infrastructure.Persistence.Entities.ControlAuditEventEntity
        {
            Id = $"audit-{Guid.NewGuid():N}", Action = request.Active ? "user_reactivated" : "user_deactivated",
            Actor = actorUser.Name, EntityId = target.Id, Reason = request.Reason, CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}

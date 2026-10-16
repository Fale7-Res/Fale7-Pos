namespace Falah.Infrastructure.Users;

using Falah.Application.Users;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Fixes the real gap IMPLEMENTATION_MATRIX.md row 4b calls out: `updateUser` in the
/// reference app has NO owner-immutability check at all - the only place `role === 'owner'` is
/// checked is an early-return inside UsersPermissionsScreen.tsx's own click handler (bypassable by
/// calling the underlying command directly) - and no privilege-escalation guard either (any
/// users_manage holder could grant `orders_refund`, or even `all`, to anyone, regardless of their own
/// permissions). Both enforced here at the handler level instead.</summary>
public sealed class UpdateUserPermissionsHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public UpdateUserPermissionsHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task HandleAsync(UpdateUserPermissionsRequest request, CancellationToken ct = default)
    {
        // Concurrent revocations must not each count the other administrator as a survivor.
        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "users_manage");

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new InvalidOperationException("المستخدم المستهدف غير موجود.");
        UserManagementRules.AssertTargetIsNotOwner(target.Role, "تعديل صلاحيات");

        var grant = request.Grant.Distinct().ToList();
        var revoke = request.Revoke.Distinct().ToList();
        UserManagementRules.AssertKnownPermissions(grant.Concat(revoke));
        UserManagementRules.AssertNoPrivilegeEscalation(actor.Permissions, grant);

        var current = target.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
        current.UnionWith(grant);
        current.ExceptWith(revoke);

        // Counted AFTER the change, against everyone else, because the question is only ever "who
        // is left" - and the target themselves counts only if this change leaves them both active
        // and still able. Revoking users_manage from the last holder is the realistic way a
        // restaurant locks itself out of its own till while tidying up permissions.
        if (!UserManagementRules.CanManageUsers(current))
        {
            var othersWhoCanAdminister = await _db.Users.AsNoTracking()
                .Where(u => u.Active && u.Id != target.Id)
                .Select(u => u.Permissions)
                .ToListAsync(ct);

            UserManagementRules.AssertUserAdministrationSurvives(
                othersWhoCanAdminister.Count(permissions => UserManagementRules.CanManageUsers(
                    permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))));
        }

        target.Permissions = string.Join(",", current);

        _db.ControlAuditEvents.Add(new ControlAuditEventEntity
        {
            Id = $"audit-{Guid.NewGuid():N}", Action = "permissions_changed", Actor = actorUser.Name, EntityId = target.Id,
            Reason = $"منح: [{string.Join(", ", grant)}] - سحب: [{string.Join(", ", revoke)}]", CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}

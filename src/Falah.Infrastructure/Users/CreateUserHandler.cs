namespace Falah.Infrastructure.Users;

using Falah.Application.Users;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>New in this pass - the reference app has no "add a user" command at all (only
/// `updateUser`, which maps an EXISTING id, so it structurally cannot create one; `INITIAL_USERS` in
/// mock-data.ts is the only way a user ever entered the array). Gated on `users_manage` with NO
/// approval-token fallback, matching the reference app's own `updateUser` gate exactly (a plain
/// `authorizeCommand({permission:'users_manage'})`, no approval param ever passed) - this system's
/// most sensitive administrative action deliberately has the fewest escalation paths, the same
/// pattern AuthorizeExpense's own doc comment already establishes for `expenses_main_treasury`.</summary>
public sealed class CreateUserHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public CreateUserHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<CreateUserResult> HandleAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var id = $"user-{request.RequestId}";
        var existing = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (existing is not null) return new CreateUserResult(existing.Id, WasReplayed: true);

        if (string.IsNullOrWhiteSpace(request.Name)) throw new DomainException("اسم المستخدم مطلوب.");
        if (string.IsNullOrWhiteSpace(request.Role)) throw new DomainException("دور المستخدم مطلوب.");
        // "Provisioned once" is now enforced as exactly that rather than as "never": an installation
        // with no owner at all has no protected emergency account, and the owner is the ONE account
        // that cannot be deactivated or stripped of its permissions. A till whose only administrator
        // is an ordinary manager has no way back if that manager is lost. So the first owner may be
        // created here - and only the first. Every later attempt is refused exactly as before.
        if (request.Role == UserManagementRules.OwnerRole
            && await _db.Users.AsNoTracking().AnyAsync(u => u.Role == UserManagementRules.OwnerRole, ct))
        {
            throw new DomainException("يوجد حساب مالك بالفعل - لا يمكن إنشاء حساب مالك ثانٍ.");
        }

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "users_manage");

        var permissions = request.Permissions.Distinct().ToList();
        UserManagementRules.AssertKnownPermissions(permissions);
        UserManagementRules.AssertNoPrivilegeEscalation(actor.Permissions, permissions);

        try
        {
            await PinUniquenessChecker.AssertNotInUseAsync(_db, request.InitialPin, excludeUserId: null, ct);
        }
        catch (DomainException)
        {
            // A concurrent request sharing this exact RequestId may have already committed its insert
            // between our earlier existence check and now - that is a replay, not a genuine PIN
            // collision with a different user, so re-check by the deterministic id before rejecting.
            var replay = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
            if (replay is not null) return new CreateUserResult(replay.Id, WasReplayed: true);
            throw;
        }
        var hashed = PinHasher.Hash(request.InitialPin);

        var entity = new UserEntity
        {
            Id = id, Name = request.Name.Trim(), Role = request.Role, Active = true,
            Permissions = string.Join(",", permissions),
            PinHash = hashed.HashBase64, PinSalt = hashed.SaltBase64, PinIterations = hashed.Iterations,
        };
        try
        {
            _db.Users.Add(entity);
            _db.ControlAuditEvents.Add(new ControlAuditEventEntity
            {
                Id = $"audit-{Guid.NewGuid():N}", Action = "user_created", Actor = actorUser.Name,
                EntityId = id, Reason = $"دور: {request.Role}", CreatedAtUtc = DateTimeOffset.UtcNow,
            });
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
            if (replay is null) throw;
            return new CreateUserResult(replay.Id, WasReplayed: true);
        }
        return new CreateUserResult(entity.Id, WasReplayed: false);
    }
}

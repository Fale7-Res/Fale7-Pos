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
    private readonly PinChoiceGuard _pinChoices;

    public CreateUserHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, PinChoiceGuard pinChoices)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _pinChoices = pinChoices;
    }

    public async Task<CreateUserResult> HandleAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var id = $"user-{request.RequestId}";
        var existing = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (existing is not null) return new CreateUserResult(existing.Id, WasReplayed: true);

        if (string.IsNullOrWhiteSpace(request.Name)) throw new DomainException("اسم المستخدم مطلوب.");
        var role = UserManagementRules.CanonicalRole(request.Role);
        // "Provisioned once" is now enforced as exactly that rather than as "never": an installation
        // with no owner at all has no protected emergency account, and the owner is the ONE account
        // that cannot be deactivated or stripped of its permissions. A till whose only administrator
        // is an ordinary manager has no way back if that manager is lost. So the first owner may be
        // created here - and only the first. Every later attempt is refused exactly as before.
        if (role == UserManagementRules.OwnerRole
            && await _db.Users.AsNoTracking().AnyAsync(u => u.Role.Trim().ToLower() == UserManagementRules.OwnerRole, ct))
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
        // Owner/manager can issue approvals for every action even without explicit permissions.
        // Creating either role therefore grants authority equivalent to all.
        if (role is "owner" or "manager")
            UserManagementRules.AssertNoPrivilegeEscalation(actor.Permissions, new[] { "all" });

        // A mistyped PIN is refused before it costs a choice; then the choice is claimed, because the
        // uniqueness check below answers "is this somebody's PIN?" (PinChoiceGuard).
        PinHasher.AssertValidPin(request.InitialPin);
        await _pinChoices.ClaimAsync(_db, actorUser, $"إنشاء المستخدم {request.Name.Trim()}", ct);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        // PIN-choice audit was committed first. Serialize actual account creation across hosts,
        // then recheck replay and the single-owner invariant inside this transaction.
        await UserIdentityLock.AcquireAsync(_db, ct);
        var committed = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (committed is not null) return new CreateUserResult(committed.Id, WasReplayed: true);
        if (role == UserManagementRules.OwnerRole && await _db.Users.AsNoTracking().AnyAsync(u => u.Role.Trim().ToLower() == UserManagementRules.OwnerRole, ct))
            throw new DomainException("يوجد حساب مالك بالفعل - لا يمكن إنشاء حساب مالك ثانٍ.");

        try
        {
            // This request's own id is excluded: a concurrent retry of the same request that has
            // already inserted it is a replay (handled below), not a collision to be written down.
            await PinUniquenessChecker.AssertNotInUseAsync(_db, request.InitialPin, excludeUserId: id, chosenBy: actorUser, ct);
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
            Id = id, Name = request.Name.Trim(), Role = role, Active = true,
            Permissions = string.Join(",", permissions),
            PinHash = hashed.HashBase64, PinSalt = hashed.SaltBase64, PinIterations = hashed.Iterations,
        };
        try
        {
            _db.Users.Add(entity);
            _db.ControlAuditEvents.Add(new ControlAuditEventEntity
            {
                Id = $"audit-{Guid.NewGuid():N}", Action = "user_created", Actor = actorUser.Name,
                EntityId = id, Reason = $"دور: {role}", CreatedAtUtc = DateTimeOffset.UtcNow,
            });
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);
            if (replay is null) throw;
            return new CreateUserResult(replay.Id, WasReplayed: true);
        }
        await transaction.CommitAsync(ct);
        return new CreateUserResult(entity.Id, WasReplayed: false);
    }
}

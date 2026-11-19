namespace Falah.Infrastructure.Users;

using Falah.Application.Users;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Two paths, ported from the reference app's closest analog (setEmployeePin, which
/// operates on Employee then propagates to the linked User - this system has no separate Employee
/// concept, so both paths act on User directly): self-service (ActorUserId == UserId, proves
/// identity by supplying the CURRENT pin - there is no separate password/session system in this
/// PIN-only app, so the current PIN itself is the only proof of identity available) and admin reset
/// (ActorUserId != UserId, gated on users_manage instead, and never permitted against the owner -
/// only the owner can ever change their own PIN). Same PIN-strength and uniqueness rules either
/// way.</summary>
public sealed class ChangePinHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly PinChoiceGuard _pinChoices;

    public ChangePinHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, PinChoiceGuard pinChoices)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _pinChoices = pinChoices;
    }

    public async Task HandleAsync(ChangePinRequest request, CancellationToken ct = default)
    {
        var auditId = string.IsNullOrWhiteSpace(request.RequestId) ? null
            : $"audit-pin-changed-{request.ActorUserId}-{request.RequestId}";
        async Task<bool> IsReplayAsync()
        {
            if (auditId is null) return false;
            var receipt = await _db.ControlAuditEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == auditId, ct);
            if (receipt is null) return false;
            if (receipt.Action != "pin_changed" || receipt.EntityId != request.UserId)
                throw new DomainException("مفتاح الطلب مستخدم لعملية أخرى.");
            return true;
        }
        if (await IsReplayAsync()) return;
        if (request.RequestId?.Length > 100) throw new DomainException("مفتاح الطلب أطول من الحد المسموح.");
        try { await ApplyAsync(request, auditId, ct); }
        catch (Exception ex) when (ex is DbUpdateException or DomainException)
        {
            if (await IsReplayAsync()) return;
            throw;
        }
    }

    private async Task ApplyAsync(ChangePinRequest request, string? auditId, CancellationToken ct)
    {
        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");

        // First, and free: a mistyped new PIN tells nobody anything, so it should not cost a choice.
        PinHasher.AssertValidPin(request.NewPin);

        UserEntity actorUser;
        if (request.ActorUserId == request.UserId)
        {
            // Claimed before the current PIN is checked: a wrong current PIN is a guess too.
            await _pinChoices.ClaimAsync(_db, target, "تغيير الرمز السري الشخصي", ct);
            if (string.IsNullOrEmpty(request.CurrentPin) || !PinHasher.Verify(request.CurrentPin, target.PinHash, target.PinSalt, target.PinIterations))
                throw new DomainException("رمز PIN الحالي غير صحيح.");
            actorUser = target;
        }
        else
        {
            actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
                ?? throw new InvalidOperationException("المستخدم غير موجود.");
            AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "users_manage");
            UserManagementRules.AssertTargetIsNotOwner(target.Role, "تغيير رمز PIN لـ");
            AssertResetIsNoEscalation(actorUser, target);
            await _pinChoices.ClaimAsync(_db, actorUser, $"إعادة تعيين الرمز السري لـ {target.Name}", ct);
        }
        var actorName = actorUser.Name;
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await UserIdentityLock.AcquireAsync(_db, ct);
        if (auditId is not null)
        {
            var receipt = await _db.ControlAuditEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == auditId, ct);
            if (receipt is not null)
            {
                if (receipt.Action != "pin_changed" || receipt.EntityId != target.Id) throw new DomainException("مفتاح الطلب مستخدم لعملية أخرى.");
                return;
            }
        }
        await _db.Entry(target).ReloadAsync(ct);
        if (request.ActorUserId == request.UserId)
        {
            if (request.Reactivate) throw new DomainException("إعادة تفعيل الحساب تتطلب مسؤول المستخدمين.");
            if (string.IsNullOrEmpty(request.CurrentPin) || !PinHasher.Verify(request.CurrentPin, target.PinHash, target.PinSalt, target.PinIterations))
                throw new DomainException("رمز PIN الحالي غير صحيح.");
        }
        else
        {
            UserManagementRules.AssertTargetIsNotOwner(target.Role, "تغيير رمز PIN لـ");
            AssertResetIsNoEscalation(actorUser, target);
        }
        if (request.Reactivate && target.Active) throw new DomainException("الحساب نشط بالفعل؛ حدّث قائمة المستخدمين.");

        try
        {
            await PinUniquenessChecker.AssertNotInUseAsync(_db, request.NewPin, excludeUserId: target.Id, chosenBy: actorUser, ct);
        }
        catch (DomainException)
        {
            // The checker has saved a pin_collision record against the account whose PIN was hit;
            // inside this transaction it would roll back with the refusal and the exposure would go
            // unrecorded. Nothing else is pending here (target was only reloaded), so commit it.
            await transaction.CommitAsync(ct);
            throw;
        }
        var hashed = PinHasher.Hash(request.NewPin);
        target.PinHash = hashed.HashBase64;
        target.PinSalt = hashed.SaltBase64;
        target.PinIterations = hashed.Iterations;
        if (request.Reactivate)
        {
            target.Active = true;
            _db.ControlAuditEvents.Add(new ControlAuditEventEntity { Id = $"audit-{Guid.NewGuid():N}", Action = "user_reactivated", Actor = actorName, EntityId = target.Id, Reason = "تفعيل برمز سري تم التحقق من عدم تكراره", CreatedAtUtc = DateTimeOffset.UtcNow });
        }

        // Never logs the old or new PIN value itself - only that a change happened and who did it,
        // matching the reference app's own setEmployeePin audit line ("لم يتم عرض أو تخزين الرمز القديم").
        _db.ControlAuditEvents.Add(new ControlAuditEventEntity
        {
            Id = auditId ?? $"audit-{Guid.NewGuid():N}", Action = "pin_changed", Actor = actorName, EntityId = target.Id,
            OperationId = request.RequestId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    /// <summary>Whoever sets a PIN can log in with it, so resetting another account's PIN hands the
    /// resetter everything that account may do. It is refused unless the resetter could have granted
    /// those permissions directly (the same guard CreateUser and UpdateUserPermissions apply).</summary>
    private static void AssertResetIsNoEscalation(UserEntity actorUser, UserEntity target)
    {
        try
        {
            UserManagementRules.AssertNoPrivilegeEscalation(OrderMapper.ToActor(actorUser).Permissions,
                target.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        catch (DomainException)
        {
            throw new DomainException($"لا يمكنك إعادة تعيين رمز {target.Name}: هذا الحساب يملك صلاحيات لا تملكها أنت.");
        }
    }
}

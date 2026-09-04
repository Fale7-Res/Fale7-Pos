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

    public ChangePinHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task HandleAsync(ChangePinRequest request, CancellationToken ct = default)
    {
        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");

        string actorName;
        if (request.ActorUserId == request.UserId)
        {
            if (string.IsNullOrEmpty(request.CurrentPin) || !PinHasher.Verify(request.CurrentPin, target.PinHash, target.PinSalt, target.PinIterations))
                throw new DomainException("رمز PIN الحالي غير صحيح.");
            actorName = target.Name;
        }
        else
        {
            var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
                ?? throw new InvalidOperationException("المستخدم غير موجود.");
            AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "users_manage");
            UserManagementRules.AssertTargetIsNotOwner(target.Role, "تغيير رمز PIN لـ");
            actorName = actorUser.Name;
        }

        await PinUniquenessChecker.AssertNotInUseAsync(_db, request.NewPin, excludeUserId: target.Id, ct);
        var hashed = PinHasher.Hash(request.NewPin);
        target.PinHash = hashed.HashBase64;
        target.PinSalt = hashed.SaltBase64;
        target.PinIterations = hashed.Iterations;

        // Never logs the old or new PIN value itself - only that a change happened and who did it,
        // matching the reference app's own setEmployeePin audit line ("لم يتم عرض أو تخزين الرمز القديم").
        _db.ControlAuditEvents.Add(new ControlAuditEventEntity
        {
            Id = $"audit-{Guid.NewGuid():N}", Action = "pin_changed", Actor = actorName, EntityId = target.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(ct);
    }
}

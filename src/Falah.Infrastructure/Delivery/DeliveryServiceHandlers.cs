namespace Falah.Infrastructure.Delivery;

using Falah.Application.Delivery;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from addDeliveryArea in lib/enterprise-store.tsx: gated on `delivery_manage`
/// with a manager-approval fallback, name uniqueness checked case-insensitively after
/// whitespace-normalization, idempotent-by-name (a repeat create with the same normalized name
/// returns the existing row rather than erroring or duplicating).</summary>
public sealed class CreateDeliveryServiceHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public CreateDeliveryServiceHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<DeliveryServiceDto> HandleAsync(CreateDeliveryServiceRequest request, CancellationToken ct = default)
    {
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "delivery_manage", null);
        }
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "delivery_manage", approval, requireApproval: true);

        var normalized = NormalizeName(request.Name);
        if (string.IsNullOrEmpty(normalized)) throw new DomainException("اسم خدمة التوصيل مطلوب.");

        var existing = await _db.DeliveryServices.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name.ToLower() == normalized.ToLower(), ct);
        if (existing is not null) return ToDto(existing);

        var entity = new DeliveryServiceEntity
        {
            Id = $"delivery-service-{request.RequestId}", Name = normalized, FeePiastres = request.FeePiastres,
            Active = true, CreatedBy = actorUser.Name, CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        try
        {
            _db.DeliveryServices.Add(entity);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.DeliveryServices.AsNoTracking().FirstOrDefaultAsync(s => s.Id == entity.Id, ct);
            if (replay is null) throw;
            return ToDto(replay);
        }
        return ToDto(entity);
    }

    internal static string NormalizeName(string name) =>
        System.Text.RegularExpressions.Regex.Replace(name.Trim(), @"\s+", " ");

    internal static DeliveryServiceDto ToDto(DeliveryServiceEntity e) => new(e.Id, e.Name, e.FeePiastres, e.Active, e.CreatedBy, e.CreatedAtUtc);
}

/// <summary>Ported from updateDeliveryArea - never touches a historical order (Order already
/// snapshots DeliveryServiceId/DeliveryServiceName/DeliveryFee at checkout time), so re-pricing or
/// renaming a service today changes nothing about what a customer was charged yesterday. Rejects a
/// rename that would clash with a DIFFERENT existing service's normalized name.</summary>
public sealed class UpdateDeliveryServiceHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public UpdateDeliveryServiceHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<DeliveryServiceDto> HandleAsync(UpdateDeliveryServiceRequest request, CancellationToken ct = default)
    {
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "delivery_manage", null);
        }
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "delivery_manage", approval, requireApproval: true);

        var entity = await _db.DeliveryServices.FirstOrDefaultAsync(s => s.Id == request.ServiceId, ct)
            ?? throw new InvalidOperationException("خدمة التوصيل غير موجودة.");

        var normalized = request.Name is not null ? CreateDeliveryServiceHandler.NormalizeName(request.Name) : entity.Name;
        if (string.IsNullOrEmpty(normalized)) throw new DomainException("اسم الخدمة مطلوب.");
        if (request.Name is not null)
        {
            var clash = await _db.DeliveryServices.AsNoTracking()
                .AnyAsync(s => s.Id != entity.Id && s.Name.ToLower() == normalized.ToLower(), ct);
            if (clash) throw new DomainException("يوجد خدمة توصيل أخرى بنفس الاسم بالفعل.");
        }

        entity.Name = normalized;
        if (request.FeePiastres is not null) entity.FeePiastres = request.FeePiastres;
        if (request.Active is not null) entity.Active = request.Active.Value;
        await _db.SaveChangesAsync(ct);
        return CreateDeliveryServiceHandler.ToDto(entity);
    }
}

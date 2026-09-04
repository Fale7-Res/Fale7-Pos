namespace Falah.Infrastructure.Delivery;

using Falah.Application.Delivery;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>No driver-management screen exists in the reference app's own audit trail as a separate
/// concept from Employees - drivers there are auto-created/synced from eligible employees
/// (ensureEmployeeDriver). This system has no Employees/HR feature yet (docs/IMPLEMENTATION_MATRIX.md),
/// so a standalone driver (EmployeeId always null) is the correct, evidence-based degradation the
/// domain itself already anticipates - not an invented shortcut. Gated on delivery_manage, matching
/// every other dispatch-adjacent write.</summary>
public sealed class CreateDriverHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public CreateDriverHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<DriverDto> HandleAsync(CreateDriverRequest request, CancellationToken ct = default)
    {
        var driverId = $"driver-{request.RequestId}";
        var existing = await _db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == driverId, ct);
        if (existing is not null) return new DriverDto(existing.Id, existing.Name, existing.Active);

        if (string.IsNullOrWhiteSpace(request.Name)) throw new DomainException("اسم السائق مطلوب.");

        var createdByUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CreatedByUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(createdByUser);
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "delivery_manage");

        var entity = new DriverEntity { Id = driverId, Name = request.Name.Trim(), Active = true, CreatedAtUtc = DateTimeOffset.UtcNow };
        try
        {
            _db.Drivers.Add(entity);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == driverId, ct);
            if (replay is null) throw;
            return new DriverDto(replay.Id, replay.Name, replay.Active);
        }
        return new DriverDto(entity.Id, entity.Name, entity.Active);
    }
}

/// <summary>Ported from setDriverActive in lib/enterprise-store.tsx. Never a hard-delete (matching
/// every other soft-archive pattern in this codebase - categories, products, users). Idempotent by
/// nature: setting the same active state twice is a harmless no-op.</summary>
public sealed class SetDriverActiveHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public SetDriverActiveHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<DriverDto> HandleAsync(SetDriverActiveRequest request, CancellationToken ct = default)
    {
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "delivery_manage");

        var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId, ct)
            ?? throw new InvalidOperationException("السائق غير موجود.");
        if (driver.Active != request.Active)
        {
            driver.Active = request.Active;
            await _db.SaveChangesAsync(ct);
        }
        return new DriverDto(driver.Id, driver.Name, driver.Active);
    }
}

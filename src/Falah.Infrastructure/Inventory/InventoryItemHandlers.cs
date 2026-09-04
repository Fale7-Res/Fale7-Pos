namespace Falah.Infrastructure.Inventory;

using Falah.Application.Inventory;
using Falah.Domain.Authorization;
using Falah.Domain.Inventory;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from addInventoryItem. The reference app gated this only in the UI
/// (authorizeInventoryAction) - this system enforces inventory_manage server-side, matching every
/// other command in this codebase.</summary>
public sealed class CreateInventoryItemHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public CreateInventoryItemHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<CreateInventoryItemResult> HandleAsync(CreateInventoryItemRequest request, CancellationToken ct = default)
    {
        var id = $"inv-{request.RequestId}";
        var existing = await _db.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
        if (existing is not null) return new CreateInventoryItemResult(existing.Id, WasReplayed: true);

        if (string.IsNullOrWhiteSpace(request.Name)) throw new Falah.Domain.Common.DomainException("اسم الخامة مطلوب.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "inventory_manage");

        var entity = new InventoryItemEntity
        {
            Id = id, Name = request.Name.Trim(), Category = request.Category, Unit = request.Unit,
            MinimumMilliUnits = request.MinimumMilliUnits, Active = true, IsPackaging = request.IsPackaging,
            CostSource = request.CostSource, StandardDailyUsageMilliUnits = request.StandardDailyUsageMilliUnits,
        };
        try
        {
            _db.InventoryItems.Add(entity);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
            if (replay is null) throw;
            return new CreateInventoryItemResult(replay.Id, WasReplayed: true);
        }
        return new CreateInventoryItemResult(entity.Id, WasReplayed: false);
    }
}

/// <summary>Ported from updateInventoryItem. Metadata-only; never touches balance or history.
/// Enforces the unit-lock invariant (InventoryRules.AssertUnitChangeAllowed) - once any stock
/// movement exists against this item, its unit can never change without corrupting the meaning of
/// every past quantity.</summary>
public sealed class UpdateInventoryItemHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public UpdateInventoryItemHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task HandleAsync(UpdateInventoryItemRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new Falah.Domain.Common.DomainException("اسم الخامة مطلوب.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "inventory_manage");

        var entity = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            ?? throw new InvalidOperationException("الخامة غير موجودة.");

        var unitChanged = entity.Unit != request.Unit;
        if (unitChanged)
        {
            var hasMovements = await _db.StockMovements.AsNoTracking().AnyAsync(m => m.InventoryItemId == entity.Id, ct);
            InventoryRules.AssertUnitChangeAllowed(hasMovements, unitChanged: true);
        }

        entity.Name = request.Name.Trim();
        entity.Category = request.Category;
        entity.Unit = request.Unit;
        entity.MinimumMilliUnits = request.MinimumMilliUnits;
        entity.Active = request.Active;
        entity.IsPackaging = request.IsPackaging;
        entity.CostSource = request.CostSource;
        entity.StandardDailyUsageMilliUnits = request.StandardDailyUsageMilliUnits;

        await _db.SaveChangesAsync(ct);
    }
}

namespace Falah.Infrastructure.Menu;

using Falah.Application.Menu;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from addProduct in lib/store.tsx - gated on menu_manage with no approval
/// fallback, matching every other menu-authoring command. PreparationStation is validated the same
/// way SetProductStationHandler already does (Charcoal/Grill or null) - this create path is a second
/// way to set it (at creation time instead of a later PUT), never a second source of truth.</summary>
public sealed class CreateProductHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public CreateProductHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<CreateProductResult> HandleAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var id = $"prod-{request.RequestId}";
        var existing = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (existing is not null) return new CreateProductResult(existing.Id, WasReplayed: true);

        if (string.IsNullOrWhiteSpace(request.Name)) throw new DomainException("اسم الصنف مطلوب.");
        if (request.BasePricePiastres <= 0) throw new DomainException("السعر الأساسي يجب أن يكون أكبر من صفر.");
        var station = MenuValidation.AssertValidStation(request.PreparationStation);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "menu_manage");

        var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct)
            ?? throw new DomainException("القسم المحدد غير موجود.");

        var entity = new ProductEntity
        {
            Id = id, CategoryId = category.Id, Name = request.Name.Trim(), BasePricePiastres = request.BasePricePiastres,
            SendToGrill = request.SendToGrill, PreparationStation = station, IsAvailable = true,
            SortOrder = request.SortOrder, RecipeId = request.RecipeId,
            Variants = request.Variants.Select(v => new ProductVariantEntity
            {
                Id = $"var-{Guid.NewGuid():N}", ProductId = id, Name = v.Name.Trim(), PricePiastres = v.PricePiastres, RecipeId = v.RecipeId,
            }).ToList(),
        };
        try
        {
            _db.Products.Add(entity);
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                $"إضافة صنف جديد بالمنيو: {entity.Name}", "menu",
                $"السعر الأساسي: {entity.BasePricePiastres} قرش - يرسل للفحم: {(entity.SendToGrill ? "نعم" : "لا")}",
                "normal", DateTimeOffset.UtcNow, ReferenceId: id));
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (replay is null) throw;
            return new CreateProductResult(replay.Id, WasReplayed: true);
        }
        return new CreateProductResult(entity.Id, WasReplayed: false);
    }
}

/// <summary>Ported from updateProduct - replaces the WHOLE product, variants array included. There
/// is no separate add/remove-variant command anywhere in the reference app either, so Variants here
/// is a full "replace the set" upsert, the same idiom SaveRecipeHandler already uses for recipe
/// lines: a variant editor naturally edits several rows then saves once. A variant carrying an Id
/// that already exists keeps that row (and its identity for anything that might reference it later);
/// one with a null Id is newly added; any existing row not present in the new list is removed.</summary>
public sealed class UpdateProductHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public UpdateProductHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task HandleAsync(UpdateProductRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new DomainException("اسم الصنف مطلوب.");
        if (request.BasePricePiastres <= 0) throw new DomainException("السعر الأساسي يجب أن يكون أكبر من صفر.");
        var station = MenuValidation.AssertValidStation(request.PreparationStation);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "menu_manage");

        var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct)
            ?? throw new DomainException("القسم المحدد غير موجود.");

        var entity = await _db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new InvalidOperationException("الصنف غير موجود.");

        entity.CategoryId = category.Id;
        entity.Name = request.Name.Trim();
        entity.BasePricePiastres = request.BasePricePiastres;
        entity.SendToGrill = request.SendToGrill;
        entity.PreparationStation = station;
        entity.SortOrder = request.SortOrder;
        entity.RecipeId = request.RecipeId;

        var keepIds = request.Variants.Where(v => v.Id is not null).Select(v => v.Id).ToHashSet();
        var toRemove = entity.Variants.Where(v => !keepIds.Contains(v.Id)).ToList();
        _db.ProductVariants.RemoveRange(toRemove);
        foreach (var input in request.Variants)
        {
            if (input.Id is not null)
            {
                var existingVariant = entity.Variants.FirstOrDefault(v => v.Id == input.Id);
                if (existingVariant is not null)
                {
                    existingVariant.Name = input.Name.Trim();
                    existingVariant.PricePiastres = input.PricePiastres;
                    existingVariant.RecipeId = input.RecipeId;
                    continue;
                }
            }
            entity.Variants.Add(new ProductVariantEntity
            {
                Id = $"var-{Guid.NewGuid():N}", ProductId = entity.Id, Name = input.Name.Trim(),
                PricePiastres = input.PricePiastres, RecipeId = input.RecipeId,
            });
        }

        _auditLog.Append(new AuditEvent(
            $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
            $"تعديل صنف بالمنيو: {entity.Name}", "menu",
            $"السعر: {entity.BasePricePiastres} قرش، فحم: {(entity.SendToGrill ? "نعم" : "لا")}",
            "normal", DateTimeOffset.UtcNow, ReferenceId: entity.Id));
        await _db.SaveChangesAsync(ct);
    }
}

/// <summary>Ported from deleteProduct - never hard-deletes, only flips IsAvailable. Idempotent by
/// nature: setting the same availability twice is a harmless no-op.</summary>
public sealed class SetProductAvailabilityHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public SetProductAvailabilityHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task HandleAsync(SetProductAvailabilityRequest request, CancellationToken ct = default)
    {
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "menu_manage");

        var entity = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new InvalidOperationException("الصنف غير موجود.");
        if (entity.IsAvailable == request.IsAvailable) return;

        entity.IsAvailable = request.IsAvailable;
        _auditLog.Append(new AuditEvent(
            $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
            request.IsAvailable ? $"إتاحة صنف: {entity.Name}" : $"إخفاء صنف: {entity.Name}", "menu",
            $"الصنف {entity.Id}", "normal", DateTimeOffset.UtcNow, ReferenceId: entity.Id));
        await _db.SaveChangesAsync(ct);
    }
}

internal static class MenuValidation
{
    public static string? AssertValidStation(string? station)
    {
        if (string.IsNullOrEmpty(station)) return null;
        if (!Enum.TryParse<Falah.Domain.Printing.PrintRoute>(station, out var route) || route is not (Falah.Domain.Printing.PrintRoute.Charcoal or Falah.Domain.Printing.PrintRoute.Grill))
            throw new DomainException("محطة التجهيز غير صالحة - يجب أن تكون Charcoal أو Grill.");
        return route.ToString();
    }
}

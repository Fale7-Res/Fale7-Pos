namespace Falah.Infrastructure.Inventory;

using Falah.Application.Inventory;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Ported from saveRecipe: a "replace the whole line set" save, not incremental add/remove-line
/// commands - a recipe editor naturally edits several lines then saves once. Idempotent by
/// construction: re-applying the exact same save (same RequestId, so the same recipeId when
/// creating and the same line ids either way) reproduces the exact same resulting rows, so unlike
/// every financial/stock command in this codebase, no separate duplicate-submit guard is needed.
/// Gated on menu_manage, matching the reference app - recipes are a menu-authoring concern, not an
/// inventory one.
/// </summary>
public sealed class SaveRecipeHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public SaveRecipeHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<RecipeDto> HandleAsync(SaveRecipeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new DomainException("اسم الوصفة مطلوب.");
        if (string.IsNullOrEmpty(request.ProductId)) throw new DomainException("يجب اختيار الصنف المرتبط بالوصفة.");
        if (request.Lines.Any(l => l.QuantityMilliUnits <= 0)) throw new DomainException("كمية كل مكوّن يجب أن تكون رقمًا صحيحًا أكبر من صفر.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "menu_manage", null);
        }
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "menu_manage", approval, requireApproval: true);

        var recipeId = request.RecipeId ?? $"recipe-{request.RequestId}";
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var entity = await _db.Recipes.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == recipeId, ct);
        if (entity is null)
        {
            entity = new RecipeEntity { Id = recipeId };
            _db.Recipes.Add(entity);
        }
        else
        {
            _db.RecipeItems.RemoveRange(entity.Items);
        }
        entity.ProductId = request.ProductId;
        entity.Name = request.Name.Trim();
        entity.Active = request.Active;
        entity.Items = request.Lines.Select((line, i) => new RecipeItemEntity
        {
            RecipeId = recipeId, InventoryItemId = line.InventoryItemId, QuantityMilliUnits = line.QuantityMilliUnits,
            Kind = line.Kind, YieldUnitsPerBaseUnit = line.YieldUnitsPerBaseUnit,
        }).ToList();

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return ToDto(entity);
    }

    private static RecipeDto ToDto(RecipeEntity entity) => new(entity.Id, entity.ProductId, entity.Name, entity.Active,
        entity.Items.Select(i => new RecipeLineDto(i.InventoryItemId, i.QuantityMilliUnits, i.Kind, i.YieldUnitsPerBaseUnit)).ToList());
}

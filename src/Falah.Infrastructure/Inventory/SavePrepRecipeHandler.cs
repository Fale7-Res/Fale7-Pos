namespace Falah.Infrastructure.Inventory;

using Falah.Application.Inventory;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from savePrepRecipe - same replace-whole-line-set shape and idempotent-by-
/// construction reasoning as SaveRecipeHandler, for a batch/prep recipe. Gated on menu_manage,
/// matching the reference app exactly (prep recipes are authored alongside menu recipes).</summary>
public sealed class SavePrepRecipeHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public SavePrepRecipeHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<PrepRecipeDto> HandleAsync(SavePrepRecipeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw new DomainException("اسم الخلطة مطلوب.");
        if (string.IsNullOrEmpty(request.OutputInventoryItemId)) throw new DomainException("يجب اختيار الصنف الناتج عن الخلطة.");
        if (request.OutputQuantityMilliUnits <= 0) throw new DomainException("كمية الإنتاج غير صالحة.");
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

        var prepRecipeId = request.PrepRecipeId ?? $"prep-{request.RequestId}";
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var entity = await _db.PrepRecipes.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == prepRecipeId, ct);
        if (entity is null)
        {
            entity = new PrepRecipeEntity { Id = prepRecipeId };
            _db.PrepRecipes.Add(entity);
        }
        else
        {
            _db.PrepRecipeLines.RemoveRange(entity.Lines);
        }
        entity.Name = request.Name.Trim();
        entity.OutputInventoryItemId = request.OutputInventoryItemId;
        entity.OutputQuantityMilliUnits = request.OutputQuantityMilliUnits;
        entity.Active = request.Active;
        entity.Lines = request.Lines.Select(line => new PrepRecipeLineEntity
        {
            PrepRecipeId = prepRecipeId, InventoryItemId = line.InventoryItemId, QuantityMilliUnits = line.QuantityMilliUnits,
        }).ToList();

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return ToDto(entity);
    }

    private static PrepRecipeDto ToDto(PrepRecipeEntity entity) => new(entity.Id, entity.Name, entity.OutputInventoryItemId,
        entity.OutputQuantityMilliUnits, entity.Active, entity.Lines.Select(l => new PrepRecipeLineDto(l.InventoryItemId, l.QuantityMilliUnits)).ToList());
}

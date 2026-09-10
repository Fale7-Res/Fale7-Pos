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
        await AssertDoesNotConsumeItsOwnOutputAsync(prepRecipeId, request, ct);

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

    /// <summary>A batch that consumes what it produces - tahini whose recipe lists tahini, or the
    /// same loop around a chain of other batches - has no cost that can be worked out: pricing it
    /// walks the loop forever. The costing engine now stops at the loop rather than recursing
    /// until the process dies, but the recipe itself is a data mistake, and the place to refuse it
    /// is here, before it is stored and starts making every dish that uses it uncostable.
    ///
    /// Checked whether or not the recipe is being saved active: an inactive one is a switch away
    /// from being the same problem.</summary>
    private async Task AssertDoesNotConsumeItsOwnOutputAsync(string prepRecipeId, SavePrepRecipeRequest request, CancellationToken ct)
    {
        var output = request.OutputInventoryItemId;
        if (request.Lines.Any(l => l.InventoryItemId == output))
            throw new DomainException("لا يمكن أن تحتوي الخلطة على الصنف الناتج عنها نفسه.");

        // Every other stored batch, as "what it produces" -> "what it consumes". The chain is
        // walked outward from this recipe's ingredients; reaching our own output closes a loop.
        var others = await _db.PrepRecipes.AsNoTracking()
            .Where(p => p.Active && p.Id != prepRecipeId)
            .Select(p => new { p.OutputInventoryItemId, Inputs = p.Lines.Select(l => l.InventoryItemId).ToList() })
            .ToListAsync(ct);
        var producers = others.ToLookup(p => p.OutputInventoryItemId, p => p.Inputs, StringComparer.Ordinal);

        var seen = new HashSet<string>(request.Lines.Select(l => l.InventoryItemId), StringComparer.Ordinal);
        var pending = new Queue<string>(seen);
        while (pending.Count > 0)
        {
            foreach (var producerInputs in producers[pending.Dequeue()])
                foreach (var input in producerInputs)
                {
                    if (input == output)
                        throw new DomainException("هذه الخلطة تستهلك ناتجها من خلال خلطة أخرى، فلا يمكن حساب تكلفتها.");
                    if (seen.Add(input)) pending.Enqueue(input);
                }
        }
    }

    private static PrepRecipeDto ToDto(PrepRecipeEntity entity) => new(entity.Id, entity.Name, entity.OutputInventoryItemId,
        entity.OutputQuantityMilliUnits, entity.Active, entity.Lines.Select(l => new PrepRecipeLineDto(l.InventoryItemId, l.QuantityMilliUnits)).ToList());
}

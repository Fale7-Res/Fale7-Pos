namespace Falah.Infrastructure.Printing;

using Falah.Application.Printing;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Printing;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Assigns/clears which kitchen station prints a product's prep ticket - the "persisted/
/// menu configuration" that print-job routing (PrintRules.DetermineKitchenRoutes) reads. Gated on
/// menu_manage, matching every other menu-authoring command in this codebase (SaveRecipeHandler,
/// SavePrepRecipeHandler) - assigning a station is a menu-authoring decision, not an inventory or
/// print-operations one.</summary>
public sealed class SetProductStationHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public SetProductStationHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task HandleAsync(string productId, SetProductStationRequest request, CancellationToken ct = default)
    {
        string? station = null;
        if (!string.IsNullOrEmpty(request.Station))
        {
            if (!Enum.TryParse<PrintRoute>(request.Station, out var route) || route is not (PrintRoute.Charcoal or PrintRoute.Grill))
                throw new DomainException("محطة التجهيز غير صالحة - يجب أن تكون Charcoal أو Grill.");
            station = route.ToString();
        }

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "menu_manage");

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new InvalidOperationException("الصنف غير موجود.");
        product.PreparationStation = station;
        await _db.SaveChangesAsync(ct);
    }
}

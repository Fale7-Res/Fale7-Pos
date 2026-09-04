namespace Falah.Infrastructure.Purchasing;

using Falah.Application.Purchasing;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from addSupplier in the reference app's lib/enterprise-store.tsx. See
/// AuthorizationPolicy.AuthorizeAddSupplier's own doc comment for the real authorization gap found
/// (and fixed) in PurchasingScreen.tsx's own "Adding a supplier had no authorization at all" comment.</summary>
public sealed class AddSupplierHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public AddSupplierHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<AddSupplierResult> HandleAsync(AddSupplierRequest request, CancellationToken ct = default)
    {
        var id = $"sup-{request.RequestId}";
        var existing = await _db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (existing is not null) return new AddSupplierResult(existing.Id, WasReplayed: true);

        if (string.IsNullOrWhiteSpace(request.Name)) throw new DomainException("اسم المورد مطلوب.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "suppliers_manage", request.Name);
        }
        AuthorizationPolicy.AuthorizeAddSupplier(_approvalRegistry, actor, approval);

        var entity = new SupplierEntity
        {
            Id = id, Name = request.Name.Trim(), Phone = request.Phone ?? "", Address = request.Address ?? "",
            Category = string.IsNullOrWhiteSpace(request.Category) ? "مورد خامات" : request.Category, Active = true,
            Notes = request.Notes,
        };
        try
        {
            _db.Suppliers.Add(entity);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
            if (replay is null) throw;
            return new AddSupplierResult(replay.Id, WasReplayed: true);
        }
        return new AddSupplierResult(entity.Id, WasReplayed: false);
    }
}

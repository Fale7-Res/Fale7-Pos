namespace Falah.Infrastructure.Menu;

using Falah.Application.Menu;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Menu;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Attaches or replaces a product's photo. Gated on menu_manage with no approval fallback,
/// like every other menu-authoring command - a photo is menu authoring, not an exception to it.</summary>
public sealed class SetProductImageHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public SetProductImageHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<SetProductImageResult> HandleAsync(SetProductImageRequest request, CancellationToken ct = default)
    {
        var contentType = ProductImageRules.NormalizeContentType(request.ContentType);

        byte[] bytes;
        try { bytes = Convert.FromBase64String(request.Base64Bytes ?? string.Empty); }
        catch (FormatException) { throw new DomainException("تعذّر قراءة محتوى الصورة."); }

        ProductImageRules.AssertAcceptableSize(bytes.Length);
        ProductImageRules.AssertMatchesContentType(contentType, bytes);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "menu_manage");

        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new DomainException("الصنف غير موجود.");

        var version = Guid.NewGuid().ToString("N")[..12];
        var existing = await _db.ProductImages.FirstOrDefaultAsync(i => i.ProductId == product.Id, ct);
        if (existing is null)
        {
            _db.ProductImages.Add(new ProductImageEntity
            {
                ProductId = product.Id, ContentType = contentType, Bytes = bytes, Version = version,
                UpdatedAtUtc = DateTimeOffset.UtcNow, UpdatedByUserId = actorUser.Id,
            });
        }
        else
        {
            existing.ContentType = contentType;
            existing.Bytes = bytes;
            existing.Version = version;
            existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
            existing.UpdatedByUserId = actorUser.Id;
        }

        _auditLog.Append(new AuditEvent(
            $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
            $"{(existing is null ? "إضافة" : "تغيير")} صورة الصنف: {product.Name}", "menu",
            $"الصيغة: {contentType} - الحجم: {bytes.Length / 1024} كيلوبايت",
            "normal", DateTimeOffset.UtcNow, ReferenceId: product.Id));

        await _db.SaveChangesAsync(ct);
        return new SetProductImageResult(product.Id, version);
    }
}

/// <summary>Removes a product's photo. The card falls back to its category icon, which is what it
/// showed before a photo was ever added - so this is a return to a working state, not a gap.</summary>
public sealed class RemoveProductImageHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public RemoveProductImageHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task HandleAsync(RemoveProductImageRequest request, CancellationToken ct = default)
    {
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "menu_manage");

        var existing = await _db.ProductImages.FirstOrDefaultAsync(i => i.ProductId == request.ProductId, ct);
        // Already gone. Removing nothing is the outcome the caller asked for, so this is not an error.
        if (existing is null) return;

        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);
        _db.ProductImages.Remove(existing);
        _auditLog.Append(new AuditEvent(
            $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
            $"حذف صورة الصنف: {product?.Name ?? request.ProductId}", "menu", "عاد الصنف لأيقونة قسمه",
            "normal", DateTimeOffset.UtcNow, ReferenceId: request.ProductId));
        await _db.SaveChangesAsync(ct);
    }
}

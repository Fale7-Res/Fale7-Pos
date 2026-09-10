namespace Falah.Infrastructure.Menu;

using Falah.Application.Menu;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from addCategory in lib/store.tsx - gated on menu_manage with no approval
/// fallback (a bare authorizeCommand({permission:'menu_manage'}), matching SetProductStationHandler's
/// existing gate). Idempotent via the same deterministic-id-then-replay pattern as every other create
/// command in this codebase.</summary>
public sealed class CreateCategoryHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public CreateCategoryHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<CreateCategoryResult> HandleAsync(CreateCategoryRequest request, CancellationToken ct = default)
    {
        var id = $"cat-{request.RequestId}";
        var existing = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (existing is not null) return new CreateCategoryResult(existing.Id, WasReplayed: true);

        if (string.IsNullOrWhiteSpace(request.Name)) throw new DomainException("اسم القسم مطلوب.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "menu_manage");

        var entity = new CategoryEntity { Id = id, Name = request.Name.Trim(), Icon = request.Icon, SortOrder = request.SortOrder, Active = true };
        try
        {
            _db.Categories.Add(entity);
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                $"إضافة قسم بالمنيو: {entity.Name}", "menu", $"قسم جديد: {entity.Name}", "normal", DateTimeOffset.UtcNow,
                ReferenceId: id));
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
            if (replay is null) throw;
            return new CreateCategoryResult(replay.Id, WasReplayed: true);
        }
        return new CreateCategoryResult(entity.Id, WasReplayed: false);
    }
}

/// <summary>Ported from updateCategory - replaces the whole editable field set (name/icon/sort
/// order), matching the reference app's own "pass the whole object" shape. Active is deliberately
/// NOT one of these fields - see SetCategoryActiveHandler, a genuinely separate concern in the
/// reference app (`deleteCategory` is its own dedicated action, not routed through updateCategory).</summary>
public sealed class UpdateCategoryHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public UpdateCategoryHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task HandleAsync(UpdateCategoryRequest request, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var receiptId = CommandReceipt.Id("category-update", request.RequestId);
        if (request.RequestId is not null && await CommandReceipt.FindAsync(_db, receiptId, request.CategoryId, request.ActorUserId, ct) is not null) return;
        if (string.IsNullOrWhiteSpace(request.Name)) throw new DomainException("اسم القسم مطلوب.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "menu_manage");

        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct)
            ?? throw new InvalidOperationException("القسم غير موجود.");
        entity.Name = request.Name.Trim();
        entity.Icon = request.Icon;
        entity.SortOrder = request.SortOrder;

        _auditLog.Append(new AuditEvent(
            receiptId, actorUser.Id, actorUser.Name, actorUser.Role,
            $"تعديل قسم بالمنيو: {entity.Name}", "menu", $"تعديل القسم {entity.Id}", "normal", DateTimeOffset.UtcNow,
            ReferenceId: entity.Id));
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}

/// <summary>Ported from deleteCategory - never hard-deletes, only flips Active. Idempotent by
/// nature: setting the same active state twice is a harmless no-op.</summary>
public sealed class SetCategoryActiveHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public SetCategoryActiveHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task HandleAsync(SetCategoryActiveRequest request, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var receiptId = CommandReceipt.Id("category-status", request.RequestId);
        if (request.RequestId is not null && await CommandReceipt.FindAsync(_db, receiptId, request.CategoryId, request.ActorUserId, ct) is not null) return;
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "menu_manage");

        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct)
            ?? throw new InvalidOperationException("القسم غير موجود.");
        if (entity.Active == request.Active && request.RequestId is null) return;

        entity.Active = request.Active;
        _auditLog.Append(new AuditEvent(
            receiptId, actorUser.Id, actorUser.Name, actorUser.Role,
            request.Active ? $"تفعيل قسم: {entity.Name}" : $"تعطيل قسم: {entity.Name}", "menu",
            $"القسم {entity.Id}", "normal", DateTimeOffset.UtcNow, ReferenceId: entity.Id));
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}

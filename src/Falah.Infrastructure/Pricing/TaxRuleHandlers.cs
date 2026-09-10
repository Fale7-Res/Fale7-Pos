namespace Falah.Infrastructure.Pricing;

using Falah.Application.Pricing;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from addTaxRule - gated on settings_manage, no approval fallback, matching the
/// reference app's own bare authorizeCommand call exactly.</summary>
public sealed class CreateTaxRuleHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public CreateTaxRuleHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task<CreateTaxRuleResult> HandleAsync(CreateTaxRuleRequest request, CancellationToken ct = default)
    {
        var id = $"tax-{request.RequestId}";
        var existing = await _db.TaxRules.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        if (existing is not null) return new CreateTaxRuleResult(existing.Id, WasReplayed: true);

        if (string.IsNullOrWhiteSpace(request.Name)) throw new DomainException("اسم قاعدة الضريبة مطلوب.");
        if (request.RateBasisPoints < 0) throw new DomainException("نسبة الضريبة يجب أن تكون صفرًا أو أكبر.");
        if (!Enum.TryParse<TaxMode>(request.Mode, out _)) throw new DomainException("نوع الضريبة يجب أن يكون Inclusive أو Exclusive.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "settings_manage");

        var entity = new TaxRuleEntity { Id = id, Name = request.Name.Trim(), RateBasisPoints = request.RateBasisPoints, Mode = request.Mode, Enabled = request.Enabled };
        try
        {
            _db.TaxRules.Add(entity);
            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
                $"إضافة قاعدة ضريبة: {entity.Name}", "menu",
                $"النسبة: {entity.RateBasisPoints / 100.0}% - {entity.Mode} - مفعّلة: {(entity.Enabled ? "نعم" : "لا")}",
                "warning", DateTimeOffset.UtcNow, ReferenceId: id));
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.TaxRules.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
            if (replay is null) throw;
            return new CreateTaxRuleResult(replay.Id, WasReplayed: true);
        }
        return new CreateTaxRuleResult(entity.Id, WasReplayed: false);
    }
}

/// <summary>Ported from updateTaxRule - replaces the whole rule, Enabled included (there is no
/// separate activate/deactivate command in the reference app). Every checkout resolves the active
/// rule via `FirstOrDefaultAsync(t => t.Enabled)` (CheckoutOrderHandler) - exactly like the reference
/// app's own `taxRules.find(rule => rule.enabled)` - so this never rewrites a historical order's
/// already-frozen tax figures; it only changes what a FUTURE checkout resolves.</summary>
public sealed class UpdateTaxRuleHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly IAuditLog _auditLog;

    public UpdateTaxRuleHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }

    public async Task HandleAsync(UpdateTaxRuleRequest request, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var receiptId = CommandReceipt.Id("tax-update", request.RequestId);
        if (request.RequestId is not null && await CommandReceipt.FindAsync(_db, receiptId, request.TaxRuleId, request.ActorUserId, ct) is not null) return;
        if (string.IsNullOrWhiteSpace(request.Name)) throw new DomainException("اسم قاعدة الضريبة مطلوب.");
        if (request.RateBasisPoints < 0) throw new DomainException("نسبة الضريبة يجب أن تكون صفرًا أو أكبر.");
        if (!Enum.TryParse<TaxMode>(request.Mode, out _)) throw new DomainException("نوع الضريبة يجب أن يكون Inclusive أو Exclusive.");

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "settings_manage");

        var entity = await _db.TaxRules.FirstOrDefaultAsync(t => t.Id == request.TaxRuleId, ct)
            ?? throw new InvalidOperationException("قاعدة الضريبة غير موجودة.");
        entity.Name = request.Name.Trim();
        entity.RateBasisPoints = request.RateBasisPoints;
        entity.Mode = request.Mode;
        entity.Enabled = request.Enabled;

        _auditLog.Append(new AuditEvent(
            receiptId, actorUser.Id, actorUser.Name, actorUser.Role,
            $"تعديل قاعدة ضريبة: {entity.Name}", "menu",
            $"النسبة: {entity.RateBasisPoints / 100.0}% - {entity.Mode} - مفعّلة: {(entity.Enabled ? "نعم" : "لا")}",
            "warning", DateTimeOffset.UtcNow, ReferenceId: entity.Id));
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}

public sealed class TaxRuleQueryService
{
    private readonly FalahDbContext _db;
    public TaxRuleQueryService(FalahDbContext db) => _db = db;

    public async Task<List<TaxRuleDto>> ListAsync(CancellationToken ct) =>
        (await _db.TaxRules.AsNoTracking().ToListAsync(ct)).Select(t => new TaxRuleDto(t.Id, t.Name, t.RateBasisPoints, t.Mode, t.Enabled)).ToList();
}

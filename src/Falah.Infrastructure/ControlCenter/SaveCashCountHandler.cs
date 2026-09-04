namespace Falah.Infrastructure.ControlCenter;

using System.Text.Json;
using Falah.Application.BusinessDay;
using Falah.Domain.Authorization;
using Falah.Domain.BusinessDay;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from saveCashCount in lib/enterprise-store.tsx: gated on the dedicated
/// `cash_count` permission, finalized once per Id (a retry is a harmless replay, matching legacy's
/// own `if(existing)return existing` - no separate DbUpdateException race path needed since this is
/// a read-check-then-insert on a fresh id, not a high-contention financial command). Writes a
/// `ControlAuditEventEntity` row (`cash_count_finalized`), the same narrower control-plane audit
/// trail business-day close/reopen already use.</summary>
public sealed class SaveCashCountHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;

    public SaveCashCountHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
    }

    public async Task<CashDenominationCountDto> HandleAsync(SaveCashCountRequest request, CancellationToken ct = default)
    {
        var existing = await _db.CashDenominationCounts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.Id, ct);
        if (existing is not null) return ToDto(existing, wasReplayed: true);

        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "cash_count");

        _ = await _db.BusinessDays.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.BusinessDayId, ct)
            ?? throw new InvalidOperationException("يوم العمل غير موجود.");

        var calculated = BusinessDayRules.CalculateDenominationCount(
            request.Rows.Select(r => (r.DenominationPiastres, r.Quantity)).ToList(), request.ExpectedPiastres);

        var now = DateTimeOffset.UtcNow;
        var entity = new CashDenominationCountEntity
        {
            Id = request.Id, BusinessDayId = request.BusinessDayId, ShiftId = request.ShiftId,
            RowsJson = JsonSerializer.Serialize(calculated.Rows),
            TotalPiastres = calculated.TotalPiastres, ExpectedPiastres = calculated.ExpectedPiastres,
            DifferencePiastres = calculated.DifferencePiastres, CountedBy = actorUser.Name,
            CreatedAtUtc = now, FinalizedAtUtc = now,
        };
        try
        {
            _db.CashDenominationCounts.Add(entity);
            _db.ControlAuditEvents.Add(new ControlAuditEventEntity
            {
                Id = $"control-audit-{Guid.NewGuid():N}", Action = "cash_count_finalized", Actor = actorUser.Name,
                EntityId = entity.Id, AmountPiastres = entity.TotalPiastres, CreatedAtUtc = now,
            });
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var replay = await _db.CashDenominationCounts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.Id, ct);
            if (replay is null) throw;
            return ToDto(replay, wasReplayed: true);
        }
        return ToDto(entity, wasReplayed: false);
    }

    private static CashDenominationCountDto ToDto(CashDenominationCountEntity e, bool wasReplayed) => new(
        e.Id, e.BusinessDayId, e.ShiftId,
        JsonSerializer.Deserialize<List<BusinessDayRules.DenominationRow>>(e.RowsJson)!
            .Select(r => new DenominationRowDto(r.DenominationPiastres, r.Quantity, r.TotalPiastres)).ToList(),
        e.TotalPiastres, e.ExpectedPiastres, e.DifferencePiastres, e.CountedBy, e.CreatedAtUtc, e.FinalizedAtUtc, wasReplayed);
}

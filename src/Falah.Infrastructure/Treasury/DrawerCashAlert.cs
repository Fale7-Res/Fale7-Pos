namespace Falah.Infrastructure.Treasury;

using Falah.Domain.Audit;
using Falah.Domain.Financial;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Owner decision 2026-11-24 (group A): when the drawer holds more than the safe-drop limit (3,000 ج unless
/// OperatingLimits:SafeDropWarningPiastres says otherwise), the till is told to make a safe drop and the moment is
/// recorded in the audit log. A warning, not a stop: a sale is never refused for it in the rush.
///
/// Recorded once per crossing: the audit id carries the number of safe drops already made in the shift, so after a
/// drop brings the drawer down the next crossing is recorded again. The amount in the drawer is not put in the till's
/// message (who may see a balance is governed elsewhere); the audit line has it.
/// </summary>
public sealed class DrawerCashAlert
{
    private readonly FalahDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IAuditLog _auditLog;

    public DrawerCashAlert(FalahDbContext db, IConfiguration configuration, IAuditLog auditLog)
    {
        _db = db;
        _configuration = configuration;
        _auditLog = auditLog;
    }

    public long LimitPiastres => Falah.Infrastructure.Settings.OperatingLimits.SafeDropWarningPiastres(_configuration);

    /// <returns>The till's warning, or null when the drawer is within the limit (or the limit is switched off with 0).</returns>
    public async Task<string?> CheckAsync(string shiftId, string actorUserId, CancellationToken ct = default)
    {
        var limit = LimitPiastres;
        if (limit <= 0) return null;
        var balance = await TreasuryBalanceQuery.ReadBalanceAsync(_db, TreasuryAccounts.Drawer, ct);
        if (balance.Piastres <= limit) return null;

        var drops = await _db.TreasuryMovements.AsNoTracking().CountAsync(m => m.ShiftId == shiftId && m.Type == "safe_drop", ct);
        var auditId = $"audit-drawer-over-limit-{shiftId}-{drops}";
        if (!await _db.AuditEvents.AsNoTracking().AnyAsync(a => a.Id == auditId, ct))
        {
            var actor = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorUserId, ct);
            _auditLog.Append(new AuditEvent(
                auditId, actorUserId, actor?.Name ?? actorUserId, actor?.Role ?? "", "الدرج عدّى حد الإيداع الآمن", "cash",
                $"نقدية الدرج {balance.Piastres} قرش - الحد {limit} قرش", "warning", DateTimeOffset.UtcNow, ShiftId: shiftId));
            try { await _db.SaveChangesAsync(ct); }
            catch (DbUpdateException) { /* the same crossing recorded by a sale at the same instant */ }
        }
        return $"الدرج عدّى {limit / 100:0} ج. اعمل إيداع آمن للخزينة الرئيسية دلوقتي.";
    }
}

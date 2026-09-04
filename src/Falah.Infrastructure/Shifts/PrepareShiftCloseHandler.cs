namespace Falah.Infrastructure.Shifts;

using Falah.Application.Shifts;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Shifts;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Step 1 of the blind-close workflow: read-only, commits nothing. Lets the WinUI show
/// the cashier what the system expected only AFTER they've entered what they counted, and gives
/// them a chance to back out before anything is written.</summary>
public sealed class PrepareShiftCloseHandler
{
    // 50 EGP - the historical default from the current app's settings (lib/mock-data.ts
    // cashDiscrepancyThreshold: 50). There is no Settings screen/table yet to make this
    // configurable per docs/IMPLEMENTATION_MATRIX.md, so it is read from configuration with this
    // exact value as the fallback rather than hardcoded, so a future settings table can override
    // it without another code change.
    public const long DefaultDiscrepancyThresholdPiastres = 5000;

    private readonly FalahDbContext _db;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public PrepareShiftCloseHandler(FalahDbContext db, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<PrepareShiftCloseResult> HandleAsync(PrepareShiftCloseRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
            ?? throw new InvalidOperationException("الوردية غير موجودة.");
        if (entity.Status != nameof(ShiftStatus.Open)) throw new DomainException("الوردية ليست مفتوحة.");

        var openingCash = Money.FromPiastres(entity.OpeningCashPiastres);
        var expected = await ShiftReconciliation.ComputeExpectedCashAsync(_db, entity.Id, openingCash, ct);
        var actual = Money.FromPiastres(request.ActualCashCountedPiastres);

        var threshold = Money.FromPiastres(await ReadDiscrepancyThresholdAsync(_db, _configuration, ct));

        var shift = Shift.Reconstitute(entity.Id, entity.ShiftName, entity.OpenedByUserId, openingCash,
            entity.BusinessDate, entity.OpenedAtUtc, ShiftStatus.Open, null, null, null, null, null, null, null);
        var result = shift.PrepareBlindClose(actual, expected, threshold);

        return new PrepareShiftCloseResult(
            entity.Id, result.Expected.Piastres, result.Actual.Piastres, result.Difference.Piastres, result.RequiresManagerApproval);
    }

    /// <summary>Ported from settings.cashDiscrepancyThreshold in lib/store.tsx (a live, owner-
    /// editable Settings field) - a real gap found during the legacy-parity audit: this port only
    /// ever read a deploy-time IConfiguration value, with no in-app admin control at all. The
    /// Settings singleton (see SettingsEntity) is now checked first; IConfiguration remains a
    /// deploy-time override for when no Settings row has ever been saved yet, falling back to the
    /// same historical default either way.</summary>
    internal static async Task<long> ReadDiscrepancyThresholdAsync(
        FalahDbContext db, Microsoft.Extensions.Configuration.IConfiguration configuration, CancellationToken ct)
    {
        var settingsValue = await db.Settings.AsNoTracking()
            .Where(s => s.Id == SettingsEntity.SingletonId)
            .Select(s => (long?)s.CashDiscrepancyThresholdPiastres)
            .FirstOrDefaultAsync(ct);
        if (settingsValue is not null) return settingsValue.Value;
        return long.TryParse(configuration["Shifts:CashDiscrepancyThresholdPiastres"], out var v) ? v : DefaultDiscrepancyThresholdPiastres;
    }

    /// <summary>Same Settings-first lookup as ReadDiscrepancyThresholdAsync, for the callers
    /// (BusinessDayCloseEvaluator/CloseBusinessDayHandler/ControlCenterQueryService) that have no
    /// IConfiguration of their own to fall back to - they fall back straight to the historical
    /// default instead, exactly as they did before Settings existed.</summary>
    internal static async Task<long> ReadDiscrepancyThresholdAsync(FalahDbContext db, CancellationToken ct)
    {
        var settingsValue = await db.Settings.AsNoTracking()
            .Where(s => s.Id == SettingsEntity.SingletonId)
            .Select(s => (long?)s.CashDiscrepancyThresholdPiastres)
            .FirstOrDefaultAsync(ct);
        return settingsValue ?? DefaultDiscrepancyThresholdPiastres;
    }
}

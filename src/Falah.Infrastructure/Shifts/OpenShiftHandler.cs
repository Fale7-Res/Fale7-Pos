namespace Falah.Infrastructure.Shifts;

using Falah.Application.Shifts;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Shifts;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Treasury;
using Microsoft.EntityFrameworkCore;

/// <summary>Shift-open orchestration. Blind-close and discrepancy approval remain out of scope here
/// (close lives in CloseShiftHandler). Enforces the reference app's single system-wide active-shift
/// invariant (`if (activeShift) throw`) via the database's own filtered unique index on
/// Status='Open' - a fast pre-check gives a clean error in the common case, and the index itself is
/// the actual race-safety net for two concurrent open attempts. A nonzero opening float DOES now
/// post a real Treasury movement - ported from openNewShift in
/// lib/store.tsx: type 'shift_opening_float', tre-main -> tre-drawer, only when openingCash > 0
/// (matching the reference app's own `openingCash > 0 ?` guard exactly, including that a zero-float
/// shift never touches Treasury or requires a business day at all). This closes a real gap found
/// while building Treasury: without it, tre-drawer's movement-derived balance never reflected a
/// shift's own float, so a legitimate mid-shift safe drop would be wrongly rejected as
/// insufficient-balance even for a cashier holding real counted cash.</summary>
public sealed class OpenShiftHandler
{
    private readonly FalahDbContext _db;
    public OpenShiftHandler(FalahDbContext db) => _db = db;

    public async Task<OpenShiftResult> HandleAsync(OpenShiftRequest request, CancellationToken ct = default)
    {
        var existing = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.OpenRequestId == request.RequestId, ct);
        if (existing is not null) return ToResult(existing, wasReplayed: true);

        var openedByUserForAuth = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.OpenedByUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        AuthorizationPolicy.AuthorizeShiftOpen(OrderMapper.ToActor(openedByUserForAuth));

        if (await _db.Shifts.AsNoTracking().AnyAsync(s => s.Status == nameof(ShiftStatus.Open), ct))
            throw new ShiftAlreadyOpenException("توجد وردية مفتوحة بالفعل. أغلقها قبل فتح وردية جديدة.");

        var now = DateTimeOffset.UtcNow;
        var openingCash = Money.FromPiastres(request.OpeningCashPiastres);
        var shift = Shift.Open($"shift-{Guid.NewGuid():N}", request.ShiftName, request.OpenedByUserId, openingCash, now);

        string? businessDayId = null;
        if (openingCash.IsPositive)
        {
            businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
            var mainMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, TreasuryAccounts.Main, ct);
            FinancialRules.AssertProtectedSourceBalance(mainMovements, TreasuryAccounts.Main, openingCash);
        }

        var entity = new ShiftEntity
        {
            Id = shift.Id,
            ShiftName = shift.ShiftName,
            OpenedByUserId = shift.OpenedByUserId,
            OpeningCashPiastres = shift.OpeningCash.Piastres,
            Status = nameof(ShiftStatus.Open),
            BusinessDate = shift.BusinessDate,
            OpenedAtUtc = shift.OpenedAtUtc,
            OpenRequestId = request.RequestId,
        };

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            _db.Shifts.Add(entity);

            if (openingCash.IsPositive)
            {
                var openedByUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.OpenedByUserId, ct)
                    ?? throw new InvalidOperationException("المستخدم غير موجود.");
                var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                    $"tm-{Guid.NewGuid():N}", $"shift-open-{shift.Id}", $"op-{Guid.NewGuid():N}", "shift_opening_float",
                    TreasuryAccounts.Main, TreasuryAccounts.Drawer, openingCash,
                    "shift_opening", shift.Id, businessDayId, shift.Id,
                    $"عهدة افتتاح {shift.ShiftName}", openedByUser.Name, approvedBy: null, now);
                _db.TreasuryMovements.Add(OrderMapper.ToEntity(movement));
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ToResult(entity, wasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException
                                   || Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
        {
            await transaction.RollbackAsync(ct);
            var replay = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.OpenRequestId == request.RequestId, ct);
            if (replay is not null) return ToResult(replay, wasReplayed: true);
            // Not our own request replaying - a genuinely concurrent, differently-keyed open-shift
            // request won the single-active-shift filtered unique index race instead.
            if (await _db.Shifts.AsNoTracking().AnyAsync(s => s.Status == nameof(ShiftStatus.Open), ct))
                throw new ShiftAlreadyOpenException("توجد وردية مفتوحة بالفعل. أغلقها قبل فتح وردية جديدة.");
            throw;
        }
    }

    private static OpenShiftResult ToResult(ShiftEntity entity, bool wasReplayed) =>
        new(entity.Id, entity.ShiftName, entity.Status, entity.OpeningCashPiastres, wasReplayed);
}

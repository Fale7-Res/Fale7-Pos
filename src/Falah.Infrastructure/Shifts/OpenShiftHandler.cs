namespace Falah.Infrastructure.Shifts;

using Falah.Application.Shifts;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Printing;
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
/// (matching the reference app's own `openingCash > 0 ?` guard; unlike the reference app, every
/// shift requires an open business day - see HandleAsync). This closes a real gap found
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


        var now = DateTimeOffset.UtcNow;
        var openingCash = Money.FromPiastres(request.OpeningCashPiastres);
        var shift = Shift.Open($"shift-{Guid.NewGuid():N}", request.ShiftName, request.OpenedByUserId, openingCash, now);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await Falah.Infrastructure.BusinessDay.FinancialAdmission.AcquireAsync(_db, ct);
            var admissionReplay = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.OpenRequestId == request.RequestId, ct);
            if (admissionReplay is not null) return ToResult(admissionReplay, wasReplayed: true);
            if (await _db.Shifts.AsNoTracking().AnyAsync(s => s.Status == nameof(ShiftStatus.Open), ct))
                throw new ShiftAlreadyOpenException("توجد وردية مفتوحة بالفعل. أغلقها قبل فتح وردية جديدة.");
            // Every shift needs an open business day, whatever it opens with - a departure from the
            // reference app, where a zero-float shift needed none. Sales stamp the open business day
            // (CheckoutOrderHandler) but do not require one, so a zero-float shift was a way to ring up
            // sales no business-day reconciliation or close snapshot ever counted. Requiring the day
            // here is enough to close that: a business day cannot close while any shift is open
            // (BusinessDayRules' OPEN_SHIFT blocker), so an open shift now always has an open day.
            // Owner-approved handover (2026-11-09): an authorized cashier no longer needs a manager to open
            // the day first. The active day is reused, or one is created in THIS transaction, so the day,
            // float and shift commit or roll back together (no opening slip since 2026-11-17).
            // See BusinessDayGate.ResolveForShiftOpenAsync.
            var businessDayId = await BusinessDayGate.ResolveForShiftOpenAsync(_db, request.OpenedByUserId, $"{request.RequestId}:day", now, ct);

            // Physical handover (owner, 2026-11-08). When the previous close left a float in the drawer, this shift
            // opens ON that cash: no main -> drawer float is issued for it (that would count it twice). The opening
            // figure is what this cashier actually found and counted; if it differs from what the close says it
            // left, the gap is recorded on its own as an opening discrepancy against the previous shift. It is not
            // charged to this cashier (this shift's expected cash starts from what was really there) and not hidden.
            var previous = await DrawerFloat.LastRetainedAsync(_db, ct);
            Money? inherited = previous is null ? null : Money.FromPiastres(previous.RetainedPiastres);

            if (inherited is null && openingCash.IsPositive)
            {
                var mainMovements = await TreasuryBalanceQuery.LockAndReadBalanceAsync(_db, TreasuryAccounts.Main, ct);
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
                BusinessDayId = businessDayId,
                OpenedAtUtc = shift.OpenedAtUtc,
                OpenRequestId = request.RequestId,
                InheritedFloatPiastres = inherited?.Piastres,
            };

            _db.Shifts.Add(entity);

            if (inherited is null && openingCash.IsPositive)
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
            else if (inherited is { } left && openingCash != left)
            {
                var openedByUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.OpenedByUserId, ct)
                    ?? throw new InvalidOperationException("المستخدم غير موجود.");
                var gap = openingCash - left;
                var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                    $"tm-{Guid.NewGuid():N}", $"shift-open-gap-{shift.Id}", $"op-{Guid.NewGuid():N}",
                    gap.IsNegative ? "opening_float_shortfall" : "opening_float_overage",
                    sourceAccountId: gap.IsNegative ? TreasuryAccounts.Drawer : null,
                    destinationAccountId: gap.IsPositive ? TreasuryAccounts.Drawer : null,
                    gap.Abs(), "shift_opening", previous!.ShiftId, businessDayId, shiftId: null,
                    $"فرق استلام عهدة {previous.ShiftName}: المتروك {left.Egp} ج والمستلم فعلًا {openingCash.Egp} ج",
                    openedByUser.Name, approvedBy: null, now);
                _db.TreasuryMovements.Add(OrderMapper.ToEntity(movement));
            }

            // No opening slip: retired by the owner (paper redesign 2026-11-17). The float is recorded
            // above exactly as before; only the paper is gone.

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
        new(entity.Id, entity.ShiftName, entity.Status, entity.OpeningCashPiastres, wasReplayed, entity.InheritedFloatPiastres);
}

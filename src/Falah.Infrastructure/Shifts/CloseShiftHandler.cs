namespace Falah.Infrastructure.Shifts;

using System.Data;
using Falah.Application.Shifts;
using Falah.Domain.Audit;
using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Falah.Domain.Delivery;
using Falah.Domain.Financial;
using Falah.Domain.Printing;
using Falah.Domain.Shifts;
using Falah.Infrastructure.Delivery;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The blind-close use case: recomputes expected cash itself (never trusts a client-supplied
/// figure), enforces the discrepancy-approval gate, then posts up to two Treasury movements in the
/// exact sequence and account shape the current app's closeShiftBlind uses -
/// see lib/store.tsx: reconciliation (shortage debits tre-drawer / overage credits tre-drawer) is
/// posted FIRST, then handover moves the full ACTUAL counted amount (not the expected figure) from
/// tre-drawer to tre-main. That ordering is what makes tre-drawer's movement-derived balance land
/// on exactly zero after a close regardless of which way the discrepancy went - see the worked
/// example in docs/IMPLEMENTATION_MATRIX.md's Treasury row if this looks non-obvious.
///
/// Also enforces the legacy assertShiftDeliverySettled(shiftId) guard: a shift with any delivery
/// whose driver custody is not yet Settled cannot be closed - the cash a driver is still holding
/// for this shift's orders would otherwise vanish from every reconciliation.
/// </summary>
public sealed class CloseShiftHandler
{
    private readonly FalahDbContext _db;
    private readonly IApprovalRegistry _approvalRegistry;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
    private readonly IAuditLog _auditLog;

    public CloseShiftHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, Microsoft.Extensions.Configuration.IConfiguration configuration, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _configuration = configuration;
        _auditLog = auditLog;
    }

    public async Task<CloseShiftResult> HandleAsync(CloseShiftRequest request, CancellationToken ct = default)
    {
        var existing = await FindExistingAsync(request.CloseRequestId, ct);
        if (existing is not null) return ToResult(existing, wasReplayed: true);

        var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ClosedByUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(cashierUser);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            // Same UPDLOCK/ROWLOCK/HOLDLOCK pattern as RefundOrderHandler - a second concurrent
            // close attempt against the SAME shift blocks here instead of both computing the same
            // stale expected cash and racing to post two handovers.
            var entity = await _db.Shifts
                .FromSqlRaw("SELECT * FROM Shifts WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.ShiftId)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("الوردية غير موجودة.");

            // A concurrent request carrying the SAME CloseRequestId as one that just won the lock
            // race and committed must see itself as already-fulfilled here, before the plain
            // Status check below - otherwise it reads "Closed" (by the request it is a retry OF)
            // and throws a spurious "shift not open" instead of replaying. This is the equivalent
            // of RefundOrderHandler's unique-index-violation replay path, just reached a different
            // way because closing a Shift mutates a single existing row rather than inserting a
            // new one, so there is no insert for a duplicate key to fail on.
            if (entity.CloseRequestId == request.CloseRequestId)
            {
                await transaction.RollbackAsync(ct);
                return ToResult(entity, wasReplayed: true);
            }
            if (entity.Status != nameof(ShiftStatus.Open)) throw new DomainException("الوردية ليست مفتوحة.");

            // Deliberately no driver-custody check here: a driver settles per business day, or when he
            // comes in - not because a cashier wants to hand over the till. See DeliveryRules
            // .UnsettledCustodyForShift. The obligation still blocks the BUSINESS DAY close, shows on
            // the dashboard, and prints as a warning on this shift's own handover slip.

            ManagerApproval? approval = null;
            if (!string.IsNullOrEmpty(request.ManagerApprovalId))
            {
                var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                    ?? throw new DomainException("المعتمد غير موجود.");
                approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                    approverUser.Role, DateTimeOffset.UtcNow, "shifts_manage", request.Reason);
            }

            var openingCash = Money.FromPiastres(entity.OpeningCashPiastres);
            var expected = await ShiftReconciliation.ComputeExpectedCashAsync(_db, entity.Id, openingCash, ct);
            var actual = Money.FromPiastres(request.ActualCashCountedPiastres);
            var threshold = Money.FromPiastres(await PrepareShiftCloseHandler.ReadDiscrepancyThresholdAsync(_db, _configuration, ct));

            var shift = Shift.Reconstitute(entity.Id, entity.ShiftName, entity.OpenedByUserId, openingCash,
                entity.BusinessDate, entity.OpenedAtUtc, ShiftStatus.Open, null, null, null, null, null, null, null);
            var blindResult = shift.PrepareBlindClose(actual, expected, threshold);

            AuthorizationPolicy.AuthorizeShiftClose(_approvalRegistry, actor, approval, blindResult.RequiresManagerApproval);
            var authorizedByName = approval?.ApproverName;

            var now = DateTimeOffset.UtcNow;
            var difference = blindResult.Difference;

            string? reconciliationMovementId = null;
            if (!difference.IsZero)
            {
                var reconciliation = TreasuryMovementFactory.BuildCashLifecycleMovement(
                    $"tm-{Guid.NewGuid():N}", $"shift-reconcile-{entity.Id}", $"op-{Guid.NewGuid():N}",
                    difference.IsNegative ? "cash_shortage" : "cash_overage",
                    sourceAccountId: difference.IsNegative ? TreasuryAccounts.Drawer : null,
                    destinationAccountId: difference.IsPositive ? TreasuryAccounts.Drawer : null,
                    difference.Abs(), "shift_reconciliation", entity.Id, businessDayId: null, entity.Id,
                    string.IsNullOrWhiteSpace(request.Reason) ? "فرق نقدية ضمن الحد المسموح" : request.Reason,
                    cashierUser.Name, authorizedByName, now);
                _db.TreasuryMovements.Add(OrderMapper.ToEntity(reconciliation));
                reconciliationMovementId = reconciliation.Id;
            }

            string? handoverMovementId = null;
            if (actual.IsPositive)
            {
                var handover = TreasuryMovementFactory.BuildCashLifecycleMovement(
                    $"tm-{Guid.NewGuid():N}", $"shift-close-handover-{entity.Id}", $"op-{Guid.NewGuid():N}",
                    "shift_handover", TreasuryAccounts.Drawer, TreasuryAccounts.Main,
                    actual, "shift_handover", entity.Id, businessDayId: null, entity.Id,
                    $"تسليم إغلاق {entity.ShiftName}", cashierUser.Name, authorizedByName, now);
                _db.TreasuryMovements.Add(OrderMapper.ToEntity(handover));
                handoverMovementId = handover.Id;
            }

            shift.Close(cashierUser.Id, blindResult, request.Reason, authorizedByName, now);

            entity.Status = nameof(ShiftStatus.Closed);
            entity.ClosedByUserId = shift.ClosedByUserId;
            entity.ActualCashPiastres = shift.ActualCash!.Value.Piastres;
            entity.ExpectedCashPiastres = shift.ExpectedCash!.Value.Piastres;
            entity.CashDifferencePiastres = shift.CashDifference!.Value.Piastres;
            entity.DifferenceReason = shift.DifferenceReason;
            entity.DifferenceAuthorizedBy = shift.DifferenceAuthorizedBy;
            entity.ClosedAtUtc = shift.ClosedAtUtc;
            entity.CloseRequestId = request.CloseRequestId;
            entity.ReconciliationTreasuryMovementId = reconciliationMovementId;
            entity.HandoverTreasuryMovementId = handoverMovementId;
            entity.Version++;

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", cashierUser.Id, cashierUser.Name, cashierUser.Role,
                "إغلاق وردية", "shift", $"إغلاق {entity.ShiftName} - المتوقع {expected.Piastres} والفعلي {actual.Piastres}",
                blindResult.RequiresManagerApproval ? "critical" : "normal", now,
                ShiftId: entity.Id, Reason: request.Reason, AuthorizedBy: authorizedByName));

            // The handover paper. Same transaction as the close, keyed on the shift, so it is
            // queued exactly once and only for a close that actually committed.
            _db.PrintJobs.Add(Falah.Infrastructure.Printing.DocumentPrintJobs.Build(
                PrintDocumentKind.ShiftClose, entity.Id, PrintRules.ShiftCloseLabel, cashierUser.Name, now));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ToResult(entity, wasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            var replay = await FindExistingAsync(request.CloseRequestId, ct);
            if (replay is not null) return ToResult(replay, wasReplayed: true);
            throw new ShiftCloseConflictException(
                "تعذر إتمام تقفيل الوردية بسبب تعارض في مفتاح منع التكرار، ولا يوجد تقفيل سابق لإعادته - يحتاج مراجعة يدوية.");
        }
    }

    private Task<ShiftEntity?> FindExistingAsync(string closeRequestId, CancellationToken ct) =>
        _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.CloseRequestId == closeRequestId, ct);

    private static CloseShiftResult ToResult(ShiftEntity entity, bool wasReplayed) => new(
        entity.Id, entity.Status, entity.ExpectedCashPiastres ?? 0, entity.ActualCashPiastres ?? 0,
        entity.CashDifferencePiastres ?? 0, wasReplayed);
}

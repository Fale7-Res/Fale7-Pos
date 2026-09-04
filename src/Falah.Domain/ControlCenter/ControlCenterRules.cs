namespace Falah.Domain.ControlCenter;

/// <summary>Ported from buildControlExceptions in lib/control-layer-domain.mjs. Severity/category
/// classify WHAT kind of review this is; status (open/acknowledged/resolved) is deliberately NOT
/// part of this record - these candidates are recomputed fresh from current data on every read, and
/// the Infrastructure layer overlays any persisted acknowledgment/resolution onto them by the
/// exception's own deterministic Id, exactly like the reference app's separate `exceptionStates` map
/// keyed by the same id.</summary>
public sealed record ControlException(string Id, string Category, string Severity, string Message, string EntityType, string EntityId);

/// <summary>Business-day-level exception mining, ported from buildControlExceptions. One of the
/// reference app's original categories is deliberately NOT ported here: backup-overdue has no
/// analog because this system has no local-backup mechanism to go overdue. selectDeliveryExceptions'
/// own trip-scoped types (trip_unsettled, driver_shortage, driver_overage, and cash_unsettled -
/// which is already subsumed by the broader UnsettledCustodyIds check below, see its own inline
/// note) are also not separately ported - trip_unsettled/driver_shortage/driver_overage genuinely
/// depend on a "trip" concept this system's per-order delivery model does not have. Its OTHER types
/// (ready_unassigned, out_too_long, plus failed/returned) are per-delivery, need no trip concept at
/// all, and are now ported below (a real gap found during the legacy-parity audit: the original doc
/// comment here over-broadly excluded all of selectDeliveryExceptions on the trip-concept rationale,
/// when only part of it actually depended on that).</summary>
public static class ControlCenterRules
{
    // Ported verbatim (already in piastres) from CONTROL_THRESHOLDS in lib/control-layer-domain.mjs.
    public const long LargeRefundThresholdPiastres = 50_000;
    public const long LargeExpenseThresholdPiastres = 100_000;
    public const int DeliveryOutTooLongMinutes = 90;

    public sealed record BuildExceptionsInputs(
        IReadOnlyList<(string Id, long CashDifferencePiastres)> ClosedShiftsWithDifference,
        long ShiftDiscrepancyThresholdPiastres,
        IReadOnlyList<(string Id, long RefundAmountPiastres)> RefundedOrders,
        IReadOnlyList<(string Id, long AmountPiastres)> Expenses,
        IReadOnlyList<string> UnsettledCustodyIds,
        IReadOnlyList<string> IncompleteJournalIds,
        IReadOnlyList<(string Id, string? OrderNumber, string Status, bool HasDriver, DateTimeOffset? OutForDeliveryAtUtc)>? Deliveries = null,
        DateTimeOffset? NowUtc = null);

    public static IReadOnlyList<ControlException> BuildExceptions(BuildExceptionsInputs input)
    {
        var result = new List<ControlException>();

        foreach (var shift in input.ClosedShiftsWithDifference.Where(s => s.CashDifferencePiastres != 0))
        {
            var severity = Math.Abs(shift.CashDifferencePiastres) >= input.ShiftDiscrepancyThresholdPiastres ? "critical" : "warning";
            var message = shift.CashDifferencePiastres < 0 ? "عجز في الوردية" : "زيادة في الوردية";
            result.Add(new ControlException($"shift-{shift.Id}", "financial", severity, message, "shift", shift.Id));
        }

        foreach (var order in input.RefundedOrders.Where(o => o.RefundAmountPiastres >= LargeRefundThresholdPiastres))
            result.Add(new ControlException($"refund-{order.Id}", "orders", "warning", "مرتجع كبير يحتاج مراجعة", "order", order.Id));

        foreach (var expense in input.Expenses.Where(e => e.AmountPiastres >= LargeExpenseThresholdPiastres))
            result.Add(new ControlException($"expense-{expense.Id}", "expenses", "warning", "مصروف كبير يحتاج مراجعة", "expense", expense.Id));

        foreach (var custodyId in input.UnsettledCustodyIds)
            result.Add(new ControlException($"custody-{custodyId}", "delivery", "critical", "عهدة سائق غير مسواة", "driver_custody", custodyId));

        foreach (var operationId in input.IncompleteJournalIds)
            result.Add(new ControlException($"journal-{operationId}", "system", "critical", "عملية مالية معلقة أو تحتاج استردادًا", "journal", operationId));

        if (input.Deliveries is not null)
        {
            var now = input.NowUtc ?? DateTimeOffset.UtcNow;
            foreach (var d in input.Deliveries)
            {
                var label = d.OrderNumber ?? d.Id;
                if (d.Status == "Ready" && !d.HasDriver)
                    result.Add(new ControlException($"delivery-ready-{d.Id}", "delivery", "warning", $"طلب {label} جاهز دون سائق", "delivery", d.Id));
                if (d.Status is "Failed" or "Returned")
                    result.Add(new ControlException($"delivery-{d.Status.ToLowerInvariant()}-{d.Id}", "delivery", "warning", $"طلب {label}: {d.Status}", "delivery", d.Id));
                if (d.Status == "OutForDelivery" && d.OutForDeliveryAtUtc is { } outAt
                    && now - outAt > TimeSpan.FromMinutes(DeliveryOutTooLongMinutes))
                    result.Add(new ControlException($"delivery-out-too-long-{d.Id}", "delivery", "warning", $"طلب {label} خارج للتوصيل منذ وقت طويل", "delivery", d.Id));
            }
        }

        // Note: legacy's own 'cash_unsettled' (unsettled custody with money actually collected) is
        // NOT re-added here as a separate exception - the UnsettledCustodyIds loop above already
        // flags every unsettled custody as critical regardless of amount, a strict superset of
        // legacy's narrower collected>0 condition, so adding it again would just duplicate the same
        // custody as two exceptions.

        return result;
    }
}

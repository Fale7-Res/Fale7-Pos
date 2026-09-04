namespace Falah.Domain.BusinessDay;

using Falah.Domain.Common;

public enum BusinessDayStatus { Open, Closing, Closed, Reopened }

public sealed record BusinessDayRecord(
    string Id, DateOnly BusinessDate, string OpenedBy, DateTimeOffset OpenedAtUtc, BusinessDayStatus Status,
    int CloseRevision, string? SnapshotId = null, string? ReopenReason = null, string? ReopenedBy = null);

/// <summary>A blocker that must be resolved before the business day can close. Ported from
/// evaluateCloseBlockers in lib/control-layer-domain.mjs - every one of these is "critical" in the
/// original (there is no soft/advisory blocker in this function).</summary>
public sealed record CloseBlocker(string Code, string EntityType, string EntityId, string Message);

/// <summary>Business-day lifecycle and close-gating rules ported from
/// lib/control-layer-domain.mjs. reconcileBusinessDay (the full financial rollup/snapshot content)
/// is a cross-aggregate reporting query, not a gating rule, and is deferred to the
/// Application-layer reporting phase once repositories exist.</summary>
public static class BusinessDayRules
{
    private static readonly BusinessDayStatus[] ActiveStatuses = { BusinessDayStatus.Open, BusinessDayStatus.Closing, BusinessDayStatus.Reopened };

    public static bool IsActive(BusinessDayRecord? day) => day is not null && ActiveStatuses.Contains(day.Status);

    private static readonly IReadOnlyDictionary<BusinessDayStatus, BusinessDayStatus[]> Legal = new Dictionary<BusinessDayStatus, BusinessDayStatus[]>
    {
        [BusinessDayStatus.Open] = new[] { BusinessDayStatus.Closing },
        [BusinessDayStatus.Reopened] = new[] { BusinessDayStatus.Closing },
        [BusinessDayStatus.Closing] = new[] { BusinessDayStatus.Closed },
        [BusinessDayStatus.Closed] = new[] { BusinessDayStatus.Reopened },
    };

    public static void AssertTransition(BusinessDayStatus from, BusinessDayStatus to)
    {
        if (!Legal.TryGetValue(from, out var next) || !next.Contains(to))
            throw new DomainException($"انتقال حالة يوم العمل غير صالح: {from} → {to}");
    }

    public static BusinessDayRecord Open(string id, DateOnly businessDate, string openedBy, DateTimeOffset nowUtc, IEnumerable<BusinessDayRecord> existingDays)
    {
        if (existingDays.Any(IsActive)) throw new DomainException("يوجد يوم عمل نشط بالفعل.");
        return new BusinessDayRecord(id, businessDate, openedBy, nowUtc, BusinessDayStatus.Open, CloseRevision: 0);
    }

    public static BusinessDayRecord Reopen(
        BusinessDayRecord day, IEnumerable<BusinessDayRecord> allDays, string reason, string approverName, DateTimeOffset nowUtc)
    {
        if (day.Status != BusinessDayStatus.Closed) throw new DomainException("لا يمكن إعادة فتح يوم غير مغلق.");
        if (allDays.Any(other => other.Id != day.Id && IsActive(other)))
            throw new DomainException("لا يمكن إعادة الفتح أثناء وجود يوم عمل نشط.");
        if (day.SnapshotId is null) throw new DomainException("لقطة الإغلاق الأصلية غير موجودة.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("سبب إعادة الفتح مطلوب.");

        return day with { Status = BusinessDayStatus.Reopened, ReopenReason = reason.Trim(), ReopenedBy = approverName };
    }

    public sealed record CloseBlockerInputs(
        IReadOnlyList<(string Id, bool Closed)> Shifts,
        IReadOnlyList<(string Id, bool Settled)> DeliveryTrips,
        IReadOnlyList<(string Id, bool Settled)> DriverCustodies,
        IReadOnlyList<(string OperationId, bool Complete)> JournalEntries,
        IReadOnlyList<(string Id, bool ElectronicPaymentPending)> Orders,
        bool PersistenceHealthy,
        bool WriterOwned,
        IReadOnlyList<(string Id, string Label, bool Completed)> RequiredChecklist,
        IReadOnlyList<(string Code, bool Critical)> ReconciliationChecks);

    /// <summary>Every one of these gates a close - there is no "close with warnings" path. An
    /// unconfirmed InstaPay/wallet transfer in particular is real customer money that has neither
    /// been verified nor posted to Treasury; nothing else in the system surfaces it, so a day could
    /// close while it sits in limbo forever if this check were ever skipped.</summary>
    public static IReadOnlyList<CloseBlocker> EvaluateCloseBlockers(CloseBlockerInputs input)
    {
        var blockers = new List<CloseBlocker>();

        foreach (var shift in input.Shifts.Where(s => !s.Closed))
            blockers.Add(new CloseBlocker("OPEN_SHIFT", "shift", shift.Id, "توجد وردية كاشير غير مغلقة."));

        foreach (var trip in input.DeliveryTrips.Where(t => !t.Settled))
            blockers.Add(new CloseBlocker("UNSETTLED_DELIVERY_TRIP", "delivery_trip", trip.Id, "توجد رحلة دليفري لم تتم تسويتها."));

        foreach (var custody in input.DriverCustodies.Where(c => !c.Settled))
            blockers.Add(new CloseBlocker("UNSETTLED_DRIVER_CUSTODY", "driver_custody", custody.Id, "توجد عهدة سائق غير مسواة."));

        foreach (var journal in input.JournalEntries.Where(j => !j.Complete))
            blockers.Add(new CloseBlocker("JOURNAL_INCOMPLETE", "journal", journal.OperationId, "توجد عملية مالية غير مكتملة في سجل الاسترداد."));

        foreach (var order in input.Orders.Where(o => o.ElectronicPaymentPending))
            blockers.Add(new CloseBlocker("PENDING_ELECTRONIC_PAYMENT", "order", order.Id, "يوجد طلب بانتظار تأكيد تحويل إلكتروني (إنستاباي/محفظة) لم يُحسم بعد."));

        if (!input.PersistenceHealthy)
            blockers.Add(new CloseBlocker("PERSISTENCE_UNHEALTHY", "system", "persistence", "التخزين المحلي غير سليم."));

        if (!input.WriterOwned)
            blockers.Add(new CloseBlocker("WRITER_NOT_OWNED", "system", "writer", "هذه النافذة ليست الكاتب المالي الحالي."));

        foreach (var item in input.RequiredChecklist.Where(c => !c.Completed))
            blockers.Add(new CloseBlocker("CHECKLIST_INCOMPLETE", "checklist", item.Id, $"بند الإغلاق غير مكتمل: {item.Label}"));

        foreach (var check in input.ReconciliationChecks.Where(c => c.Critical))
            blockers.Add(new CloseBlocker("RECONCILIATION_CRITICAL", "reconciliation", check.Code, "يوجد فرق مالي حرج يحتاج تسوية فعلية."));

        return blockers;
    }

    public static void AssertCanClose(IReadOnlyList<CloseBlocker> blockers)
    {
        if (blockers.Count > 0) throw new DomainException("لا يمكن إغلاق يوم العمل قبل معالجة الموانع المالية الحرجة.");
    }

    /// <summary>Ported verbatim from EGYPTIAN_DENOMINATIONS_PIASTRES in lib/control-layer-domain.mjs
    /// - the fixed set of Egyptian pound cash denominations (piastres) a blind physical count is
    /// broken down by, largest first.</summary>
    public static readonly IReadOnlyList<long> EgyptianDenominationsPiastres =
        new long[] { 20000, 10000, 5000, 2000, 1000, 500, 200, 100, 50, 25 };

    public readonly record struct DenominationRow(long DenominationPiastres, long Quantity, long TotalPiastres);
    public readonly record struct DenominationCountResult(
        IReadOnlyList<DenominationRow> Rows, long TotalPiastres, long ExpectedPiastres, long DifferencePiastres);

    /// <summary>Ported verbatim from calculateDenominationCount in lib/control-layer-domain.mjs - a
    /// negative counted quantity is clamped to zero (a physical count can never be negative), never
    /// rejected outright, matching the reference app's own tolerant `Math.max(0, ...)`.</summary>
    public static DenominationCountResult CalculateDenominationCount(
        IReadOnlyList<(long DenominationPiastres, long Quantity)> rows, long expectedPiastres)
    {
        var normalized = rows.Select(r =>
        {
            var quantity = Math.Max(0, r.Quantity);
            return new DenominationRow(r.DenominationPiastres, quantity, r.DenominationPiastres * quantity);
        }).ToList();
        var total = normalized.Sum(r => r.TotalPiastres);
        return new DenominationCountResult(normalized, total, expectedPiastres, total - expectedPiastres);
    }
}

namespace Falah.Infrastructure.Employees;

using Falah.Domain.Common;
using Falah.Infrastructure.Persistence.Entities;

/// <summary>
/// Which employee-ledger entries belong to a span of days (Cairo dates).
///
/// An entry belongs to the day it was recorded - except a daily salary accrual, which belongs to the
/// day it was earned for (its ReferenceId). Accruals are posted after a business day closes, often
/// several days at once: a close after midnight, the first close after days without one, a payroll
/// true-up at month end. Dating them by when they were posted put days of salary on the posting day
/// and none on the days the work was done. The entry keeps its real CreatedAtUtc (the audit truth);
/// only date-based figures read it by its earning date.
/// </summary>
public static class EmployeeLedgerPeriods
{
    public static IQueryable<EmployeeLedgerEntryEntity> ForDays(this IQueryable<EmployeeLedgerEntryEntity> entries, DateOnly from, DateOnly to)
    {
        var (start, _) = BusinessClock.UtcRangeForBusinessDate(from);
        var (_, end) = BusinessClock.UtcRangeForBusinessDate(to);
        // ReferenceId holds the earning date as yyyy-MM-dd, which orders correctly as text.
        var first = from.ToString("yyyy-MM-dd");
        var last = to.ToString("yyyy-MM-dd");
        return entries.Where(e =>
            (e.Id.StartsWith(SalaryAccrual.EntryPrefix) && string.Compare(e.ReferenceId, first) >= 0 && string.Compare(e.ReferenceId, last) <= 0)
            || (!e.Id.StartsWith(SalaryAccrual.EntryPrefix) && e.CreatedAtUtc >= start && e.CreatedAtUtc < end));
    }
}

namespace Falah.Infrastructure.Employees;

using Falah.Domain.Common;
using Falah.Domain.Employees;
using Falah.Domain.Payroll;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Posts the daily share of every monthly salary as an earning on the employee's ledger - the owner's
/// rule that a monthly salary is earned day by day (PayrollRules.DailySalaryAccrual).
///
/// Each day is one Salary credit with a deterministic id (salary-accrual-{employee}-{yyyyMMdd}) and
/// the date it is for in ReferenceId, so posting is naturally idempotent: running it twice for the
/// same day, or a payroll true-up over days a close already accrued, posts each day exactly once.
/// It accrues from the day after the last day already accrued; for an employee never accrued before,
/// from the first day of the month being accrued (or the hire date if later). Nothing is back-filled
/// before that: months already paid were settled under the old rule, and crediting them now would
/// pay them twice on paper.
///
/// Payroll keeps computing a monthly employee's line from the salary on their profile; these credits
/// are what makes that payout settle against something earned, and what puts the salary into the
/// wage cost the owner sees.
/// </summary>
public static class SalaryAccrual
{
    public const string EntryPrefix = "salary-accrual-";

    public static string EntryId(string employeeId, DateOnly date) => $"{EntryPrefix}{employeeId}-{date:yyyyMMdd}";

    /// <summary>Adds the missing daily accruals through <paramref name="throughDate"/> to the context.
    /// Uses the caller transaction when supplied (the caller saves); otherwise owns, saves and
    /// commits its transaction so the employee lock remains held through the ledger insert.</summary>
    /// <param name="employeeIds">Only these employees, or every active monthly employee when null.</param>
    public static async Task<int> AccrueThroughAsync(
        FalahDbContext db, DateOnly throughDate, string recordedBy, IReadOnlyCollection<string>? employeeIds, CancellationToken ct)
    {
        await using var ownedTransaction = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;
        var monthly = nameof(CompensationType.Monthly);
        var employees = await db.Employees.AsNoTracking()
            .Where(e => e.IsActive && e.CompensationType == monthly && e.MonthlySalaryPiastres > 0)
            .ToListAsync(ct);
        if (employeeIds is not null) employees = employees.Where(e => employeeIds.Contains(e.Id)).ToList();

        var posted = 0;
        foreach (var employee in employees.OrderBy(e => e.Id, StringComparer.Ordinal))
        {
            await LedgerCreditReplay.LockEmployeeAsync(db, employee.Id, ct);
            var prefix = $"salary-accrual-{employee.Id}-";
            var lastAccrued = await db.EmployeeLedgerEntries.AsNoTracking()
                .Where(e => e.EmployeeId == employee.Id && e.Type == nameof(LedgerEntryType.Salary) && e.Id.StartsWith(prefix))
                .MaxAsync(e => e.ReferenceId, ct);

            var start = lastAccrued is not null && DateOnly.TryParse(lastAccrued, out var last)
                ? last.AddDays(1)
                : new DateOnly(throughDate.Year, throughDate.Month, 1);
            if (employee.HireDate > start) start = employee.HireDate;

            var salary = Money.FromPiastres(employee.MonthlySalaryPiastres!.Value);
            for (var day = start; day <= throughDate; day = day.AddDays(1))
            {
                var entry = await EmployeeLedgerPoster.PostIfNewAsync(db, EntryId(employee.Id, day), employee.Id,
                    nameof(LedgerEntryType.Salary), $"استحقاق راتب يوم {day:yyyy-MM-dd}",
                    Money.Zero, PayrollRules.DailySalaryAccrual(salary, day), recordedBy, null, null,
                    day.ToString("yyyy-MM-dd"), null, null, salary.Piastres, ct);
                if (entry is not null) posted++;
            }
        }
        if (ownedTransaction is not null) { await db.SaveChangesAsync(ct); await ownedTransaction.CommitAsync(ct); }
        return posted;
    }
}

/// <summary>The business-day close's call into SalaryAccrual: after the close has committed, never
/// inside it, and never able to fail it - the next close, or the payroll payout's true-up, posts any
/// day this one missed.</summary>
public sealed class SalaryAccrualService
{
    private readonly FalahDbContext _db;
    private readonly ILogger<SalaryAccrualService> _log;

    public SalaryAccrualService(FalahDbContext db, ILogger<SalaryAccrualService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task TryAccrueThroughAsync(DateOnly throughDate, string recordedBy, CancellationToken ct = default)
    {
        try
        {
            var posted = await SalaryAccrual.AccrueThroughAsync(_db, throughDate, recordedBy, employeeIds: null, ct);
            if (posted > 0) await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _db.ChangeTracker.Clear();
            _log.LogError(ex, "Daily salary accrual through {Date} failed; the next close or the payroll payout will post it.", throughDate);
        }
    }
}

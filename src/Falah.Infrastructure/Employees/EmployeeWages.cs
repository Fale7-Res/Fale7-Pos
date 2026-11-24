namespace Falah.Infrastructure.Employees;

using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>PAYR-0005 / PAYR-0006 (owner decision 2026-11-22): what an employee was paid on a given day.
/// Changing a wage or salary records the old pay (from the hire date, the first time) and the new pay from
/// today, so a correction or an accrual for a past day keeps that day's pay. An employee with no history
/// is paid at the values on the employee row, which is what they were paid all along.</summary>
public static class EmployeeWages
{
    public static async Task<IReadOnlyList<EmployeeWageHistoryEntity>> LoadAsync(FalahDbContext db, string employeeId, CancellationToken ct) =>
        await db.EmployeeWageHistory.AsNoTracking().Where(h => h.EmployeeId == employeeId)
            .OrderBy(h => h.EffectiveFrom).ThenBy(h => h.RecordedAtUtc).ToListAsync(ct);

    public static async Task<ILookup<string, EmployeeWageHistoryEntity>> LoadAllAsync(FalahDbContext db, CancellationToken ct) =>
        (await db.EmployeeWageHistory.AsNoTracking().OrderBy(h => h.EffectiveFrom).ThenBy(h => h.RecordedAtUtc).ToListAsync(ct))
            .ToLookup(h => h.EmployeeId);

    /// <summary>The newest row in effect on <paramref name="date"/>, else the employee's own values.</summary>
    public static (long DailyWage, long? MonthlySalary) At(IEnumerable<EmployeeWageHistoryEntity> history, EmployeeEntity employee, DateOnly date)
    {
        var row = history.LastOrDefault(h => h.EffectiveFrom <= date);
        return row is null ? (employee.DailyWagePiastres, employee.MonthlySalaryPiastres) : (row.DailyWagePiastres, row.MonthlySalaryPiastres);
    }

    public static void RecordInitial(FalahDbContext db, EmployeeEntity employee, string recordedBy, DateTimeOffset now) =>
        db.EmployeeWageHistory.Add(Row(employee.Id, employee.HireDate, employee.DailyWagePiastres, employee.MonthlySalaryPiastres, recordedBy, now));

    public static async Task RecordChangeAsync(FalahDbContext db, EmployeeEntity employee, long oldDailyWage, long? oldMonthlySalary,
        string recordedBy, DateTimeOffset now, CancellationToken ct)
    {
        if (employee.DailyWagePiastres == oldDailyWage && employee.MonthlySalaryPiastres == oldMonthlySalary) return;
        var today = BusinessClock.BusinessDate(now);
        if (!await db.EmployeeWageHistory.AnyAsync(h => h.EmployeeId == employee.Id, ct) && employee.HireDate < today)
            db.EmployeeWageHistory.Add(Row(employee.Id, employee.HireDate, oldDailyWage, oldMonthlySalary, recordedBy, now));

        var todays = await db.EmployeeWageHistory.FirstOrDefaultAsync(h => h.EmployeeId == employee.Id && h.EffectiveFrom == today, ct);
        if (todays is null)
            db.EmployeeWageHistory.Add(Row(employee.Id, today, employee.DailyWagePiastres, employee.MonthlySalaryPiastres, recordedBy, now));
        else
        {
            todays.DailyWagePiastres = employee.DailyWagePiastres;
            todays.MonthlySalaryPiastres = employee.MonthlySalaryPiastres;
            todays.RecordedAtUtc = now;
            todays.RecordedBy = recordedBy;
        }
    }

    private static EmployeeWageHistoryEntity Row(string employeeId, DateOnly from, long dailyWage, long? monthlySalary, string by, DateTimeOffset now) => new()
    {
        Id = $"wage-{Guid.NewGuid():N}", EmployeeId = employeeId, EffectiveFrom = from,
        DailyWagePiastres = dailyWage, MonthlySalaryPiastres = monthlySalary, RecordedAtUtc = now, RecordedBy = by,
    };
}

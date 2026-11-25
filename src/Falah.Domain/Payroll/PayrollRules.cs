namespace Falah.Domain.Payroll;

using Falah.Domain.Common;
using Falah.Domain.Employees;

public enum PayrollRunStatus { Draft, Calculated, Reviewed, Approved, Paid, Cancelled }

/// <summary>Ported verbatim from lib/payroll-domain.mjs - the pure logic behind both Attendance's
/// automatic late-penalty/overtime postings and the full Payroll run lifecycle (both consume the
/// same file in the reference app, so they share this one Domain module here too, not two
/// independently-invented ones).</summary>
public static class PayrollRules
{
    // Paid is not a status transition. It is the result of PayPayrollHandler, which checks the run
    // is Approved itself and posts the treasury payment and every line's settlement in the same
    // transaction as the status. A status change alone would show the run as paid with no money
    // moved, and leave it unpayable because it is no longer Approved.
    private static readonly IReadOnlyDictionary<PayrollRunStatus, PayrollRunStatus[]> Transitions = new Dictionary<PayrollRunStatus, PayrollRunStatus[]>
    {
        [PayrollRunStatus.Draft] = new[] { PayrollRunStatus.Calculated, PayrollRunStatus.Cancelled },
        [PayrollRunStatus.Calculated] = new[] { PayrollRunStatus.Reviewed, PayrollRunStatus.Cancelled },
        [PayrollRunStatus.Reviewed] = new[] { PayrollRunStatus.Approved, PayrollRunStatus.Cancelled },
        [PayrollRunStatus.Approved] = new[] { PayrollRunStatus.Cancelled },
        [PayrollRunStatus.Paid] = Array.Empty<PayrollRunStatus>(),
        [PayrollRunStatus.Cancelled] = Array.Empty<PayrollRunStatus>(),
    };

    public static void AssertTransition(PayrollRunStatus from, PayrollRunStatus to)
    {
        if (!Transitions.TryGetValue(from, out var allowed) || !allowed.Contains(to))
            throw new DomainException($"انتقال حالة Payroll غير مسموح: {from} → {to}");
    }

    /// <summary>Ported verbatim from normalHourlyRate - a non-positive workday-hours config
    /// produces a zero rate rather than a division error/NaN.</summary>
    public static Money NormalHourlyRate(Money dailyWage, int workdayHours) =>
        workdayHours <= 0 ? Money.Zero : Money.FromPiastres(dailyWage.Piastres / workdayHours);

    /// <summary>Ported verbatim: 0-graceMinutes late = no penalty; up to 60 min late = 1 penalty
    /// hour; beyond that = maxPenaltyHours (the hard ceiling), regardless of how late.</summary>
    public static int CalculateLatePenaltyHours(int lateMinutes, int graceMinutes, int maxPenaltyHours)
    {
        if (lateMinutes <= graceMinutes) return 0;
        return lateMinutes <= 60 ? Math.Min(1, maxPenaltyHours) : maxPenaltyHours;
    }

    public readonly record struct LateDeductionResult(int Hours, Money Rate, Money Amount);

    public static LateDeductionResult CalculateLateDeduction(
        Money dailyWage, int workdayHours, int lateMinutes, int graceMinutes, int maxPenaltyHours, double latePenaltyMultiplier)
    {
        var rate = NormalHourlyRate(dailyWage, workdayHours);
        var hours = CalculateLatePenaltyHours(lateMinutes, graceMinutes, maxPenaltyHours);
        var amount = HalfPiastreUp(rate, hours, latePenaltyMultiplier); // PAYR-0019: half a piastre up
        return new LateDeductionResult(hours, rate, amount);
    }

    public readonly record struct OvertimeAmountResult(Money Rate, Money Amount);

    public static OvertimeAmountResult CalculateOvertimeAmount(Money dailyWage, int workdayHours, double overtimeHours, double overtimeMultiplier)
    {
        var rate = NormalHourlyRate(dailyWage, workdayHours);
        if (overtimeHours <= 0) return new OvertimeAmountResult(rate, Money.Zero);
        var amount = HalfPiastreUp(rate, overtimeHours, overtimeMultiplier); // PAYR-0019
        return new OvertimeAmountResult(rate, amount);
    }

    /// <summary>rate x hours x multiplier, half a piastre up (PAYR-0019), computed in decimal. The hours and the
    /// multiplier arrive as double (settings and attendance store them so); multiplied in double, a multiplier such as
    /// 1.15 made 57.5 into 57.4999... and lost a piastre. The double-to-decimal conversion keeps 15 significant digits,
    /// so 1.15 becomes exactly 1.15m and the half is seen as a half (audit 2026-11-25).</summary>
    private static Money HalfPiastreUp(Money rate, double hours, double multiplier) =>
        Money.FromPiastres((long)decimal.Round(rate.Piastres * (decimal)hours * (decimal)multiplier, MidpointRounding.AwayFromZero));

    /// <summary>Ported verbatim from outstandingAdvance: total advances taken minus total already
    /// deducted via a payroll settlement, floored at zero (an employee's outstanding advance can
    /// never go negative).</summary>
    public static Money OutstandingAdvance(IEnumerable<(string Type, Money Debit, Money AdvanceDeduction)> entries, long floorAtZero = 0)
    {
        var net = entries.Sum(e =>
            (string.Equals(e.Type, nameof(LedgerEntryType.Advance), StringComparison.OrdinalIgnoreCase) ? e.Debit.Piastres : 0)
            - (string.Equals(e.Type, nameof(LedgerEntryType.PayrollSettlement), StringComparison.OrdinalIgnoreCase)
                || string.Equals(e.Type, "payroll_settlement", StringComparison.OrdinalIgnoreCase) ? e.AdvanceDeduction.Piastres : 0));
        return Money.FromPiastres(Math.Max(floorAtZero, net));
    }

    public readonly record struct PayrollLine(
        string EmployeeId, Money Base, Money Overtime, Money ExtraShifts, Money Bonuses, Money Deductions,
        Money AdvanceDeductions, Money NetPayable, Money PaidOutsidePayroll = default);

    public sealed record PayrollLineInput(
        string EmployeeId, bool IsActive, bool CompensationIsMonthly, Money? MonthlySalary,
        IReadOnlyList<(string Type, Money Credit, Money Debit)> PeriodEntries, Money AvailableAdvance, Money RequestedAdvanceDeduction);

    /// <summary>Ported verbatim from calculatePayrollLines - an inactive employee with no earning
    /// activity this period is simply excluded (not a zero-payout line), matching legacy's own
    /// `if (!emp.isActive && !has) return []`.</summary>
    /// <summary>
    /// The share of a monthly salary earned on one calendar day: the month split evenly by its own
    /// number of days, the last day taking whatever the integer split left over, so a full month of
    /// shares adds up to exactly the salary - never a piastre more or less. The owner's rule for
    /// monthly staff: the salary is earned daily, so the wage cost of a day includes it.
    /// </summary>
    public static Money DailySalaryAccrual(Money monthlySalary, DateOnly date)
    {
        if (!monthlySalary.IsPositive) return Money.Zero;
        var days = DateTime.DaysInMonth(date.Year, date.Month);
        var share = monthlySalary.Piastres / days;
        return Money.FromPiastres(date.Day == days ? monthlySalary.Piastres - share * (days - 1) : share);
    }

    /// <summary>PAYR-0007 (owner decision 2026-11-22): the part of a monthly salary a payroll period owes -
    /// the daily shares (<see cref="DailySalaryAccrual"/>) of the days from the hire date (or the period's
    /// first day) to the period's last day. A whole month adds up to exactly the salary.</summary>
    public static Money MonthlyBaseForPeriod(Money monthlySalary, DateOnly periodStart, DateOnly periodEnd, DateOnly hireDate) =>
        MonthlyBaseForPeriod(_ => monthlySalary, periodStart, periodEnd, hireDate);

    /// <summary>The same, with the salary in effect on each day (PAYR-0006: a raise mid-period counts
    /// from its own day), so the base equals the accrual posted day by day.</summary>
    public static Money MonthlyBaseForPeriod(Func<DateOnly, Money> salaryOn, DateOnly periodStart, DateOnly periodEnd, DateOnly hireDate)
    {
        var first = hireDate > periodStart ? hireDate : periodStart;
        long total = 0;
        for (var day = first; day <= periodEnd; day = day.AddDays(1)) total += DailySalaryAccrual(salaryOn(day), day).Piastres;
        return Money.FromPiastres(total);
    }

    public static IReadOnlyList<PayrollLine> CalculatePayrollLines(IEnumerable<PayrollLineInput> inputs)
    {
        var result = new List<PayrollLine>();
        foreach (var emp in inputs)
        {
            var hasCredit = emp.PeriodEntries.Any(e => e.Credit.IsPositive);
            if (!emp.IsActive && !hasCredit) continue;

            // Type strings match this codebase's own PascalCase enum-name convention
            // (nameof(LedgerEntryType.X), e.g. "DailyWage") - NOT legacy's own lowercase-snake-case
            // literals ('daily_wage') from LedgerEntryType in types.ts, since every ledger row in
            // this port is written via nameof(...) (see EmployeeLedgerPoster/RecordDailyWageHandlers).
            var baseAmount = emp.CompensationIsMonthly
                ? (emp.MonthlySalary ?? Money.Zero)
                : Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type is "DailyWage" or "Salary").Sum(e => e.Credit.Piastres));
            var overtime = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type == "Overtime").Sum(e => e.Credit.Piastres));
            var extraShifts = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type == "ExtraShift").Sum(e => e.Credit.Piastres));
            var bonuses = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type is "Bonus" or "OtherEarning").Sum(e => e.Credit.Piastres));
            var deductions = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type is "Deduction" or "Adjustment")
                .Sum(e => e.Debit.Piastres - (e.Type == "Adjustment" ? e.Credit.Piastres : 0)));

            if (emp.RequestedAdvanceDeduction.IsNegative || emp.RequestedAdvanceDeduction > emp.AvailableAdvance)
                throw new DomainException($"استقطاع السلفة للموظف {emp.EmployeeId} يتجاوز الرصيد المتفق عليه.");

            // PAYR-0004 (owner decision 2026-11-22): money already paid out of these earnings this period,
            // outside the run - "دفع مستحقات" (Settlement) and "سحب" (Withdrawal) - is not paid again.
            // An advance is a loan, not a payout of earnings; its recovery is a separate decision.
            var paidOutside = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type is "Settlement" or "Withdrawal").Sum(e => e.Debit.Piastres));

            var net = baseAmount + overtime + extraShifts + bonuses - deductions - emp.RequestedAdvanceDeduction - paidOutside;
            if (net.IsNegative) net = Money.Zero;

            result.Add(new PayrollLine(emp.EmployeeId, baseAmount, overtime, extraShifts, bonuses, deductions, emp.RequestedAdvanceDeduction, net, paidOutside));
        }
        return result;
    }

    public readonly record struct PayrollReportResult(Money Earned, Money Paid, Money Outstanding);

    public static PayrollReportResult PayrollReport(IEnumerable<(Money Base, Money Overtime, Money ExtraShifts, Money Bonuses, Money NetPayable, bool IsPaid)> lines)
    {
        long earned = 0, paid = 0, outstanding = 0;
        foreach (var line in lines)
        {
            earned += line.Base.Piastres + line.Overtime.Piastres + line.ExtraShifts.Piastres + line.Bonuses.Piastres;
            if (line.IsPaid) paid += line.NetPayable.Piastres; else outstanding += line.NetPayable.Piastres;
        }
        return new PayrollReportResult(Money.FromPiastres(earned), Money.FromPiastres(paid), Money.FromPiastres(outstanding));
    }
}

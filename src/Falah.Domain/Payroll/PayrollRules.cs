namespace Falah.Domain.Payroll;

using Falah.Domain.Common;

public enum PayrollRunStatus { Draft, Calculated, Reviewed, Approved, Paid, Cancelled }

/// <summary>Ported verbatim from lib/payroll-domain.mjs - the pure logic behind both Attendance's
/// automatic late-penalty/overtime postings and the full Payroll run lifecycle (both consume the
/// same file in the reference app, so they share this one Domain module here too, not two
/// independently-invented ones).</summary>
public static class PayrollRules
{
    private static readonly IReadOnlyDictionary<PayrollRunStatus, PayrollRunStatus[]> Transitions = new Dictionary<PayrollRunStatus, PayrollRunStatus[]>
    {
        [PayrollRunStatus.Draft] = new[] { PayrollRunStatus.Calculated, PayrollRunStatus.Cancelled },
        [PayrollRunStatus.Calculated] = new[] { PayrollRunStatus.Reviewed, PayrollRunStatus.Cancelled },
        [PayrollRunStatus.Reviewed] = new[] { PayrollRunStatus.Approved, PayrollRunStatus.Cancelled },
        [PayrollRunStatus.Approved] = new[] { PayrollRunStatus.Paid, PayrollRunStatus.Cancelled },
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
        var amount = Money.FromPiastres((long)Math.Round(rate.Piastres * hours * latePenaltyMultiplier));
        return new LateDeductionResult(hours, rate, amount);
    }

    public readonly record struct OvertimeAmountResult(Money Rate, Money Amount);

    public static OvertimeAmountResult CalculateOvertimeAmount(Money dailyWage, int workdayHours, double overtimeHours, double overtimeMultiplier)
    {
        var rate = NormalHourlyRate(dailyWage, workdayHours);
        if (overtimeHours <= 0) return new OvertimeAmountResult(rate, Money.Zero);
        var amount = Money.FromPiastres((long)Math.Round(rate.Piastres * overtimeHours * overtimeMultiplier));
        return new OvertimeAmountResult(rate, amount);
    }

    /// <summary>Ported verbatim from outstandingAdvance: total advances taken minus total already
    /// deducted via a payroll settlement, floored at zero (an employee's outstanding advance can
    /// never go negative).</summary>
    public static Money OutstandingAdvance(IEnumerable<(string Type, Money Debit, Money AdvanceDeduction)> entries, long floorAtZero = 0)
    {
        var net = entries.Sum(e => (e.Type == "advance" ? e.Debit.Piastres : 0) - (e.Type == "payroll_settlement" ? e.AdvanceDeduction.Piastres : 0));
        return Money.FromPiastres(Math.Max(floorAtZero, net));
    }

    public readonly record struct PayrollLine(
        string EmployeeId, Money Base, Money Overtime, Money ExtraShifts, Money Bonuses, Money Deductions,
        Money AdvanceDeductions, Money NetPayable);

    public sealed record PayrollLineInput(
        string EmployeeId, bool IsActive, bool CompensationIsMonthly, Money? MonthlySalary,
        IReadOnlyList<(string Type, Money Credit, Money Debit)> PeriodEntries, Money AvailableAdvance, Money RequestedAdvanceDeduction);

    /// <summary>Ported verbatim from calculatePayrollLines - an inactive employee with no earning
    /// activity this period is simply excluded (not a zero-payout line), matching legacy's own
    /// `if (!emp.isActive && !has) return []`.</summary>
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
            var deductions = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type is "Deduction" or "Adjustment").Sum(e => e.Debit.Piastres));

            if (emp.RequestedAdvanceDeduction.IsNegative || emp.RequestedAdvanceDeduction > emp.AvailableAdvance)
                throw new DomainException($"استقطاع السلفة للموظف {emp.EmployeeId} يتجاوز الرصيد المتفق عليه.");

            var net = baseAmount + overtime + extraShifts + bonuses - deductions - emp.RequestedAdvanceDeduction;
            if (net.IsNegative) net = Money.Zero;

            result.Add(new PayrollLine(emp.EmployeeId, baseAmount, overtime, extraShifts, bonuses, deductions, emp.RequestedAdvanceDeduction, net));
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

using Falah.Domain.Common;
using Falah.Domain.Payroll;
using Xunit;

namespace Falah.Domain.Tests;

public class PayrollRulesTests
{
    // ---- Late penalty (calculateLatePenaltyHours/calculateLateDeduction) ----

    [Theory]
    [InlineData(0, 15, 2, 0)]
    [InlineData(15, 15, 2, 0)]
    [InlineData(16, 15, 2, 1)]
    [InlineData(60, 15, 2, 1)]
    [InlineData(61, 15, 2, 2)]
    [InlineData(300, 15, 2, 2)]
    public void CalculateLatePenaltyHours_matches_the_owner_approved_tiers(int lateMinutes, int grace, int maxHours, int expected) =>
        Assert.Equal(expected, PayrollRules.CalculateLatePenaltyHours(lateMinutes, grace, maxHours));

    [Fact]
    public void NormalHourlyRate_is_zero_when_workday_hours_is_non_positive()
    {
        Assert.Equal(Money.Zero, PayrollRules.NormalHourlyRate(Money.FromPiastres(20000), 0));
        Assert.Equal(Money.Zero, PayrollRules.NormalHourlyRate(Money.FromPiastres(20000), -1));
    }

    [Fact]
    public void CalculateLateDeduction_computes_rate_hours_and_amount_together()
    {
        // 20000 piastres/10 hours = 2000/hour rate; 30 min late (grace 15) = 1 penalty hour;
        // 200% multiplier => 4000 piastres.
        var result = PayrollRules.CalculateLateDeduction(Money.FromPiastres(20000), 10, 30, 15, 2, 2.0);
        Assert.Equal(1, result.Hours);
        Assert.Equal(2000, result.Rate.Piastres);
        Assert.Equal(4000, result.Amount.Piastres);
    }

    [Fact]
    public void CalculateLateDeduction_is_zero_within_grace()
    {
        var result = PayrollRules.CalculateLateDeduction(Money.FromPiastres(20000), 10, 10, 15, 2, 2.0);
        Assert.Equal(0, result.Hours);
        Assert.Equal(0, result.Amount.Piastres);
    }

    // ---- Overtime (calculateOvertimeAmount) ----

    [Fact]
    public void CalculateOvertimeAmount_is_zero_for_non_positive_hours()
    {
        var result = PayrollRules.CalculateOvertimeAmount(Money.FromPiastres(20000), 10, 0, 1.5);
        Assert.Equal(0, result.Amount.Piastres);
    }

    [Fact]
    public void CalculateOvertimeAmount_applies_the_overtime_multiplier()
    {
        // rate = 2000/hour, 2 hours overtime, 150% multiplier => 6000 piastres.
        var result = PayrollRules.CalculateOvertimeAmount(Money.FromPiastres(20000), 10, 2, 1.5);
        Assert.Equal(2000, result.Rate.Piastres);
        Assert.Equal(6000, result.Amount.Piastres);
    }

    // ---- Payroll run transitions ----

    [Theory]
    [InlineData(PayrollRunStatus.Draft, PayrollRunStatus.Calculated, true)]
    [InlineData(PayrollRunStatus.Draft, PayrollRunStatus.Cancelled, true)]
    [InlineData(PayrollRunStatus.Draft, PayrollRunStatus.Reviewed, false)]
    [InlineData(PayrollRunStatus.Calculated, PayrollRunStatus.Reviewed, true)]
    [InlineData(PayrollRunStatus.Reviewed, PayrollRunStatus.Approved, true)]
    [InlineData(PayrollRunStatus.Approved, PayrollRunStatus.Paid, true)]
    [InlineData(PayrollRunStatus.Paid, PayrollRunStatus.Draft, false)]
    [InlineData(PayrollRunStatus.Cancelled, PayrollRunStatus.Draft, false)]
    public void AssertTransition_enforces_the_payroll_lifecycle(PayrollRunStatus from, PayrollRunStatus to, bool allowed)
    {
        if (allowed) PayrollRules.AssertTransition(from, to);
        else Assert.Throws<DomainException>(() => PayrollRules.AssertTransition(from, to));
    }

    // ---- Outstanding advance ----

    [Fact]
    public void OutstandingAdvance_nets_advances_against_prior_settlement_deductions_floored_at_zero()
    {
        var entries = new[]
        {
            ("advance", Money.FromPiastres(10000), Money.Zero),
            ("payroll_settlement", Money.Zero, Money.FromPiastres(4000)),
        };
        Assert.Equal(6000, PayrollRules.OutstandingAdvance(entries).Piastres);
    }

    [Fact]
    public void OutstandingAdvance_never_goes_negative()
    {
        var entries = new[]
        {
            ("advance", Money.FromPiastres(3000), Money.Zero),
            ("payroll_settlement", Money.Zero, Money.FromPiastres(10000)),
        };
        Assert.Equal(0, PayrollRules.OutstandingAdvance(entries).Piastres);
    }

    // ---- Payroll line calculation ----

    private static PayrollRules.PayrollLineInput DailyEmployee(string id, bool active, IReadOnlyList<(string, Money, Money)> entries,
        Money availableAdvance = default, Money requestedDeduction = default) =>
        new(id, active, false, null, entries, availableAdvance, requestedDeduction);

    [Fact]
    public void CalculatePayrollLines_sums_daily_wage_overtime_extra_shifts_bonuses_and_deductions()
    {
        var entries = new (string, Money, Money)[]
        {
            ("DailyWage", Money.FromPiastres(20000), Money.Zero),
            ("DailyWage", Money.FromPiastres(20000), Money.Zero),
            ("Overtime", Money.FromPiastres(5000), Money.Zero),
            ("ExtraShift", Money.FromPiastres(15000), Money.Zero),
            ("Bonus", Money.FromPiastres(3000), Money.Zero),
            ("Deduction", Money.Zero, Money.FromPiastres(2000)),
        };
        var lines = PayrollRules.CalculatePayrollLines(new[] { DailyEmployee("emp-1", true, entries) });
        var line = Assert.Single(lines);
        Assert.Equal(40000, line.Base.Piastres);
        Assert.Equal(5000, line.Overtime.Piastres);
        Assert.Equal(15000, line.ExtraShifts.Piastres);
        Assert.Equal(3000, line.Bonuses.Piastres);
        Assert.Equal(2000, line.Deductions.Piastres);
        // 40000 + 5000 + 15000 + 3000 - 2000 = 61000
        Assert.Equal(61000, line.NetPayable.Piastres);
    }

    [Fact]
    public void CalculatePayrollLines_uses_the_monthly_salary_for_a_monthly_employee_not_period_entries()
    {
        var input = new PayrollRules.PayrollLineInput("emp-2", true, true, Money.FromPiastres(800000),
            Array.Empty<(string, Money, Money)>(), Money.Zero, Money.Zero);
        var line = Assert.Single(PayrollRules.CalculatePayrollLines(new[] { input }));
        Assert.Equal(800000, line.Base.Piastres);
        Assert.Equal(800000, line.NetPayable.Piastres);
    }

    [Fact]
    public void CalculatePayrollLines_excludes_an_inactive_employee_with_no_earning_activity()
    {
        var lines = PayrollRules.CalculatePayrollLines(new[] { DailyEmployee("emp-3", false, Array.Empty<(string, Money, Money)>()) });
        Assert.Empty(lines);
    }

    [Fact]
    public void CalculatePayrollLines_includes_an_inactive_employee_if_they_still_earned_something_this_period()
    {
        var entries = new (string, Money, Money)[] { ("DailyWage", Money.FromPiastres(20000), Money.Zero) };
        var lines = PayrollRules.CalculatePayrollLines(new[] { DailyEmployee("emp-4", false, entries) });
        Assert.Single(lines);
    }

    [Fact]
    public void CalculatePayrollLines_rejects_an_advance_deduction_beyond_what_is_available()
    {
        var input = DailyEmployee("emp-5", true, Array.Empty<(string, Money, Money)>(),
            availableAdvance: Money.FromPiastres(5000), requestedDeduction: Money.FromPiastres(10000));
        Assert.Throws<DomainException>(() => PayrollRules.CalculatePayrollLines(new[] { input }));
    }

    [Fact]
    public void CalculatePayrollLines_never_produces_a_negative_net_payable()
    {
        var entries = new (string, Money, Money)[] { ("Deduction", Money.Zero, Money.FromPiastres(50000)) };
        var line = Assert.Single(PayrollRules.CalculatePayrollLines(new[] { DailyEmployee("emp-6", true, entries) }));
        Assert.Equal(0, line.NetPayable.Piastres);
    }
}

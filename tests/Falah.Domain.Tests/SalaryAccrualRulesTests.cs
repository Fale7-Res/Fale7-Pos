using Falah.Domain.Common;
using Falah.Domain.Payroll;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>A monthly salary is earned an even share a day; a whole month of shares is the salary.</summary>
public class SalaryAccrualRulesTests
{
    [Theory]
    [InlineData(2026, 2)]   // 28 days
    [InlineData(2028, 2)]   // 29 days
    [InlineData(2026, 4)]   // 30 days
    [InlineData(2026, 10)]  // 31 days
    public void A_full_month_of_daily_shares_adds_up_to_exactly_the_salary(int year, int month)
    {
        var salary = Money.FromPiastres(300_001);   // deliberately not divisible by any month length
        var total = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
            .Sum(day => PayrollRules.DailySalaryAccrual(salary, new DateOnly(year, month, day)).Piastres);

        Assert.Equal(salary.Piastres, total);
    }

    [Fact]
    public void Every_day_but_the_last_earns_the_same_share_and_the_last_takes_the_remainder()
    {
        var salary = Money.FromPiastres(310_000);   // 31-day month: 10000 a day exactly
        Assert.Equal(10_000, PayrollRules.DailySalaryAccrual(salary, new DateOnly(2026, 10, 1)).Piastres);
        Assert.Equal(10_000, PayrollRules.DailySalaryAccrual(salary, new DateOnly(2026, 10, 31)).Piastres);

        var uneven = Money.FromPiastres(100);       // 30-day month: 3 a day, the 30th gets 100 - 3*29 = 13
        Assert.Equal(3, PayrollRules.DailySalaryAccrual(uneven, new DateOnly(2026, 4, 15)).Piastres);
        Assert.Equal(13, PayrollRules.DailySalaryAccrual(uneven, new DateOnly(2026, 4, 30)).Piastres);
    }

    [Fact]
    public void No_salary_accrues_nothing()
    {
        Assert.Equal(Money.Zero, PayrollRules.DailySalaryAccrual(Money.Zero, new DateOnly(2026, 10, 1)));
    }
}

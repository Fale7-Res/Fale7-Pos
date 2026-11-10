using Falah.Domain.Common;
namespace Falah.Domain.Tests;
public class ArithmeticBoundaryTests
{
    [Fact] public void Financial_addition_refuses_overflow_instead_of_wrapping() =>
        Assert.Throws<DomainException>(() => Money.FromPiastres(long.MaxValue) + Money.FromPiastres(1));
    [Fact] public void Financial_subtraction_refuses_overflow_instead_of_wrapping() =>
        Assert.Throws<DomainException>(() => Money.FromPiastres(long.MinValue) - Money.FromPiastres(1));
    [Fact] public void Financial_multiplication_refuses_overflow_instead_of_wrapping() =>
        Assert.Throws<DomainException>(() => Money.FromPiastres(long.MaxValue) * 2);
    [Fact] public void Financial_negation_refuses_unrepresentable_positive_amount() =>
        Assert.Throws<DomainException>(() => -Money.FromPiastres(long.MinValue));
    [Fact] public void Absolute_minimum_money_reports_a_domain_refusal() =>
        Assert.Throws<DomainException>(() => Money.FromPiastres(long.MinValue).Abs());
    [Fact] public void Unrepresentable_boundary_egp_reports_a_domain_refusal() =>
        Assert.Throws<DomainException>(() => Money.FromEgp(decimal.MaxValue));
}

using Falah.Domain.BusinessDay;
using Falah.Domain.Common;
using Falah.Domain.Customers;
using Falah.Domain.Employees;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>Defensive QA audit 2026-11-23 (docs/FINAL_DEFENSIVE_QA_REPORT.md), fixes approved by the
/// owner: the rules the screens already apply are now enforced where the data enters the server too.</summary>
public class InputHardeningTests
{
    // QA-02: a phone typed or pasted in Arabic-Indic (or Persian) digits is the same customer.
    [Theory]
    [InlineData("٠١٠١٢٣٤٥٦٧٨")]
    [InlineData("۰۱۰۱۲۳۴۵۶۷۸")]
    [InlineData("010-1234 5678")]
    [InlineData("01012345678")]
    public void A_phone_in_any_digits_normalises_to_western_digits(string typed) =>
        Assert.Equal("01012345678", CustomerRules.NormalizePhone(typed));

    [Fact]
    public void Digits_are_normalised_without_touching_other_text() =>
        Assert.Equal("رقم 0123", InputRules.NormalizeDigits("رقم ٠١٢٣"));

    // QA-03: an empty request id cannot identify one operation - two different sales would share it.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_request_id_is_refused(string? id) =>
        Assert.Contains("رقم العملية", Assert.Throws<DomainException>(() => InputRules.AssertRequestId(id)).Message);

    [Fact]
    public void An_overlong_request_id_is_refused()
    {
        InputRules.AssertRequestId(new string('a', InputRules.MaxRequestIdLength));
        Assert.Throws<DomainException>(() => InputRules.AssertRequestId(new string('a', InputRules.MaxRequestIdLength + 1)));
    }

    // QA-11: free text has a sane upper bound; the message names the field.
    [Fact]
    public void Text_longer_than_its_limit_is_refused_with_the_field_name()
    {
        InputRules.AssertMaxLength(new string('س', 10), 10, "اسم العميل");
        InputRules.AssertMaxLength(null, 10, "اسم العميل");
        var ex = Assert.Throws<DomainException>(() => InputRules.AssertMaxLength(new string('س', 11), 10, "اسم العميل"));
        Assert.Contains("اسم العميل", ex.Message);
    }

    // QA-10: a negative count of a banknote is a typing mistake, never silently zero.
    [Fact]
    public void A_negative_banknote_count_is_refused() =>
        Assert.Throws<DomainException>(() => BusinessDayRules.CalculateDenominationCount(new[] { (20000L, 2L), (10000L, -3L) }, 0));

    // QA-06: no negative pay, whatever the client.
    [Theory]
    [InlineData(-1, 0, null, null)]
    [InlineData(0, -1, null, null)]
    [InlineData(0, 0, -1L, null)]
    [InlineData(0, 0, null, -1L)]
    public void Negative_pay_is_refused(long daily, long shift, long? monthly, long? overtime) =>
        Assert.Throws<DomainException>(() => EmployeeRules.AssertCompensation(daily, shift, monthly, overtime));

    [Fact]
    public void Zero_pay_is_allowed() => EmployeeRules.AssertCompensation(0, 0, null, null);

    // QA-08 / QA-11: names and numbers that do not fit the database are refused with a message, not a 500.
    [Fact]
    public void An_overlong_employee_name_or_number_is_refused()
    {
        Assert.Throws<DomainException>(() => EmployeeRules.AssertEmployeeInput(new string('م', InputRules.MaxNameLength + 1), "E-1", "طباخ", "010"));
        Assert.Throws<DomainException>(() => EmployeeRules.AssertEmployeeInput("موظف", new string('9', EmployeeRules.MaxEmployeeNumberLength + 1), "طباخ", "010"));
    }

    [Fact]
    public void A_phone_longer_than_any_real_number_is_refused() =>
        Assert.Throws<DomainException>(() => CustomerRules.AssertPhone(new string('1', CustomerRules.MaxPhoneDigits + 1)));
}

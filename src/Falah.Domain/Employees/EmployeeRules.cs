namespace Falah.Domain.Employees;

using Falah.Domain.Common;

/// <summary>Ported from calculateEmployeeBalance / recordManualLedgerEntry / recordWithdrawal in
/// lib/store.tsx. This system's integer-piastre Money type makes legacy's own float-rounding
/// workaround ("Math.round((totalCredits - totalDebits) * 100) / 100") unnecessary - a plain integer
/// sum is already exact.</summary>
public static class EmployeeRules
{
    public static void AssertEmployeeInput(string name, string employeeNumber, string jobTitle, string phone)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("اسم الموظف مطلوب.");
        if (string.IsNullOrWhiteSpace(employeeNumber)) throw new DomainException("الرقم الوظيفي مطلوب.");
        if (string.IsNullOrWhiteSpace(jobTitle)) throw new DomainException("المسمى الوظيفي مطلوب.");
        if (string.IsNullOrWhiteSpace(phone)) throw new DomainException("رقم هاتف الموظف مطلوب.");
    }

    /// <summary>Ported verbatim from calculateEmployeeBalance's own reduce: sum(credit) -
    /// sum(debit) across every ledger entry for this employee.</summary>
    public static Money CalculateBalance(IEnumerable<EmployeeLedgerEntry> entries)
    {
        var totalCredits = Money.FromPiastres(entries.Sum(e => e.Credit.Piastres));
        var totalDebits = Money.FromPiastres(entries.Sum(e => e.Debit.Piastres));
        return totalCredits - totalDebits;
    }

    /// <summary>Ported from recordManualLedgerEntry's own guard (`!Number.isFinite(amount) ||
    /// amount &lt;= 0`) and postEmployeeCashOut's identical one.</summary>
    public static void AssertLedgerAmount(Money amount)
    {
        if (!amount.IsPositive) throw new DomainException("قيمة الحركة يجب أن تكون أكبر من صفر.");
    }

    /// <summary>Ported verbatim from recordWithdrawal's own guard: a withdrawal against an
    /// employee's balance can never exceed what they are currently owed.</summary>
    public static void AssertWithdrawalWithinBalance(Money amount, Money currentBalance)
    {
        if (amount > currentBalance) throw new DomainException("لا يمكن سحب مبلغ أكبر من رصيد الموظف الحالي.");
    }

    /// <summary>Same comparison as AssertWithdrawalWithinBalance, worded for the standalone "دفع
    /// للموظف" flow rather than a drawer withdrawal - a payment against what is owed can never
    /// exceed the current running balance either, by the same final business rule's own example
    /// (Balance 5300, pay 2000, remaining 3300 - never negative).</summary>
    public static void AssertPaymentWithinBalance(Money amount, Money currentBalance)
    {
        if (amount > currentBalance) throw new DomainException("لا يمكن دفع مبلغ أكبر من رصيد الموظف الحالي المستحق.");
    }
}

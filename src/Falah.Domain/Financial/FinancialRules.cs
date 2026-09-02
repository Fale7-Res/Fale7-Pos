namespace Falah.Domain.Financial;

using Falah.Domain.Common;

public readonly record struct PaymentSplit(PaymentMethod Method, Money Amount);

public static class FinancialRules
{
    /// <summary>A split must allocate EXACTLY the amount due. Over-allocation used to be accepted
    /// silently, which posted more money into Treasury than the customer actually owed (e.g. 150
    /// cash + 100 card against a 200 order posted 250). Change is a cash-only concept, handled by
    /// the single-cash path, not here.</summary>
    public static void AssertSplitPaymentAllocation(IReadOnlyList<PaymentSplit> splits, Money total)
    {
        if (splits.Count < 2) throw new DomainException("الدفع المقسم يتطلب طريقتي دفع على الأقل.");
        if (splits.Any(part => part.Amount.Piastres <= 0))
            throw new DomainException("كل مبلغ في الدفع المقسم يجب أن يكون أكبر من صفر.");
        if (splits.Select(part => part.Method).Distinct().Count() != splits.Count)
            throw new DomainException("لا يمكن تكرار نفس طريقة الدفع في الدفع المقسم.");

        var allocated = Money.FromPiastres(splits.Sum(part => part.Amount.Piastres));
        if (allocated != total)
            throw new DomainException($"إجمالي المبالغ المقسمة ({allocated.Egp} ج) لا يساوي إجمالي الطلب ({total.Egp} ج).");
    }

    /// <summary>Sequence numbers are allocated centrally (by the Host) and never reused, even across
    /// a restart that lost track of the in-memory counter - existingMax (read back from storage)
    /// covers exactly that recovery case. 127 is the historical floor carried over from the current
    /// system's seed data.</summary>
    public static long AllocateNextOrderSequence(long lastAllocated, long existingMax) =>
        Math.Max(127, Math.Max(lastAllocated, existingMax)) + 1;

    public static string FormatOrderNumber(long sequence) => $"#{sequence:D4}";

    public static Money TreasuryBalanceFromMovements(IEnumerable<TreasuryMovement> movements, string accountId) =>
        movements.Where(m => m.Status == MovementStatus.Posted).Aggregate(Money.Zero, (sum, m) =>
            sum + (m.DestinationAccountId == accountId ? m.Amount : Money.Zero)
                - (m.SourceAccountId == accountId ? m.Amount : Money.Zero));

    public static Money CalculateExpectedDrawerCash(
        Money openingCash, Money cashSales, Money drawerExpenses, Money cashIn, Money cashOut) =>
        openingCash + cashSales + cashIn - drawerExpenses - cashOut;

    public static bool DiscrepancyNeedsApproval(Money difference, Money threshold)
    {
        var flooredThreshold = threshold.IsNegative ? Money.Zero : threshold;
        return difference.Abs() > flooredThreshold;
    }

    /// <summary>The four historically-fixed accounts (tre-wallet deliberately excluded, matching
    /// the original) may never be drawn below zero by a command that debits them directly.</summary>
    public static void AssertProtectedSourceBalance(IEnumerable<TreasuryMovement> movements, string sourceAccountId, Money amount)
    {
        if (TreasuryAccounts.Protected.Contains(sourceAccountId)
            && TreasuryBalanceFromMovements(movements, sourceAccountId) < amount)
            throw new DomainException("رصيد حساب المصدر غير كافٍ لتنفيذ الحركة.");
    }
}

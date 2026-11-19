namespace Falah.Domain.Financial;

using Falah.Domain.Common;

/// <summary>
/// What a posted treasury movement IS, in the owner's terms. Every balance is derived from movements, but the
/// movements mix very different facts: a customer paying, money changing drawers inside the restaurant, a supplier
/// being paid, a typing mistake being corrected. A report that adds them up without this distinction counts an
/// internal transfer or a correction as new money received.
///
/// The rule is exhaustive on purpose: a type nobody has classified is <see cref="MoneyFlowCategory.Unclassified"/>
/// and is shown as such, never silently folded into collections.
/// </summary>
public enum MoneyFlowCategory
{
    /// <summary>A customer's payment reaching a restaurant account: a sale's cash/card leg, a confirmed transfer.</summary>
    CustomerCollection,
    /// <summary>Cash a driver collected from customers and handed in (net of the fee he kept).</summary>
    DriverRemittance,
    /// <summary>Money paid back to a customer: a refund, a voided sale's cash handed back.</summary>
    CustomerRefund,
    Expense,
    /// <summary>An expense recorded by mistake whose money never left: the account is credited back. Not income.</summary>
    ExpenseCorrection,
    /// <summary>An expense's money physically came back (the shop refunded it). Real cash in, but not a sale.</summary>
    ExpenseReimbursement,
    SupplierPayment,
    Payroll,
    DriverPay,
    OwnerDrawing,
    /// <summary>Money put into the business from outside it (owner deposit, capital). Not a sale.</summary>
    ExternalFunding,
    /// <summary>Money moving between two of the restaurant's own accounts. Never a receipt, never a payment.</summary>
    InternalTransfer,
    /// <summary>A counted-vs-expected difference posted at a close (shortage / overage).</summary>
    CashCountDifference,
    /// <summary>A treasury entry recorded by mistake, corrected by its opposite. Not income, not spending.</summary>
    EntryCorrection,
    Unclassified,
}

public static class MoneyFlowRules
{
    public static MoneyFlowCategory Classify(string movementType) => movementType switch
    {
        "sale_collection" or "sale" => MoneyFlowCategory.CustomerCollection,
        "driver_settlement" => MoneyFlowCategory.DriverRemittance,
        "refund" or "void_return" => MoneyFlowCategory.CustomerRefund,
        "expense" or "expense_payment" => MoneyFlowCategory.Expense,
        ExpenseCorrectionRules.PostingErrorMovementType => MoneyFlowCategory.ExpenseCorrection,
        ExpenseCorrectionRules.CashReturnedMovementType => MoneyFlowCategory.ExpenseReimbursement,
        "purchase" or "purchase_payment" or "supplier_payment" => MoneyFlowCategory.SupplierPayment,
        "payroll_payment" or "employee_payment" or "employee_advance" or "employee_withdrawal" => MoneyFlowCategory.Payroll,
        "driver_fee_payout" => MoneyFlowCategory.DriverPay,
        "owner_withdrawal" => MoneyFlowCategory.OwnerDrawing,
        "owner_deposit" or "capital_injection" or "other_external_inflow" => MoneyFlowCategory.ExternalFunding,
        "transfer" or "treasury_transfer" or "safe_drop" or "shift_handover" or "shift_opening_float"
            or "cash_in" or "cash_out" => MoneyFlowCategory.InternalTransfer,
        "cash_shortage" or "cash_overage" or "opening_float_shortfall" or "opening_float_overage" => MoneyFlowCategory.CashCountDifference,
        TreasuryCorrectionRules.MovementType => MoneyFlowCategory.EntryCorrection,
        _ => MoneyFlowCategory.Unclassified,
    };

    /// <summary>Money a customer actually handed over, through any channel: the only categories that count as
    /// "collections". Funding, transfers, corrections and reimbursements are money in, but none is a customer's.</summary>
    public static bool IsCustomerReceipt(MoneyFlowCategory category) =>
        category is MoneyFlowCategory.CustomerCollection or MoneyFlowCategory.DriverRemittance;

    public static bool IsCashAccount(string accountId) =>
        accountId is TreasuryAccounts.Drawer or TreasuryAccounts.Main;
}

/// <summary>
/// How a mistaken expense is undone. The two cases must not be confused, because they say opposite things about the
/// drawer: an entry that was wrong (the money never left) and a payment that was real but came back.
///
/// Both credit the account the expense was charged to and both bring the drawer's expected cash back up; only these
/// two may, because only in these two is the money physically there. Money that left and stays gone is not a
/// mistake to reverse: the expense is true (a wrong category is a reclassification, which is not offered here).
/// </summary>
public static class ExpenseCorrectionRules
{
    /// <summary>The entry was wrong - a duplicate, a typo, the wrong amount: the money never left the account.</summary>
    public const string NotPaid = "not_paid";
    /// <summary>The money did leave and has now physically come back (the supplier or shop handed it back).</summary>
    public const string CashReturned = "cash_returned";

    public const string PostingErrorMovementType = "expense_correction";
    public const string CashReturnedMovementType = "expense_refund";

    public static string MovementTypeFor(string? disposition) => disposition switch
    {
        NotPaid => PostingErrorMovementType,
        CashReturned => CashReturnedMovementType,
        _ => throw new DomainException("حدّد ما حدث للمبلغ: لم يُصرف أصلًا (خطأ تسجيل)، أم صُرف ثم عاد نقدًا. إذا خرج المبلغ فعلًا ولن يعود فالمصروف صحيح ولا يُلغى."),
    };

    /// <summary>The part of the expense being undone: all of it when unspecified, otherwise a part (5000 typed, 500
    /// paid: 4500 never left). Never more than was recorded, never zero.</summary>
    public static Money ReversedAmount(Money recorded, long? requestedPiastres)
    {
        if (requestedPiastres is null) return recorded;
        if (requestedPiastres <= 0) throw new DomainException("مبلغ التصحيح يجب أن يكون أكبر من صفر.");
        if (requestedPiastres > recorded.Piastres) throw new DomainException("مبلغ التصحيح لا يمكن أن يتجاوز مبلغ المصروف المسجل.");
        return Money.FromPiastres(requestedPiastres.Value);
    }
}

/// <summary>
/// A manual treasury entry recorded by mistake (a transfer that never happened, a deposit typed twice, the wrong
/// amount) is corrected by posting its opposite, linked to it, never by deleting or editing it.
///
/// Only for entries that did NOT physically happen. Money that really moved and was later moved back is a new,
/// ordinary entry (another transfer, a deposit), because both moves happened and both belong in the history.
/// </summary>
public static class TreasuryCorrectionRules
{
    public const string MovementType = "movement_correction";

    /// <summary>Entries a person typed in and can therefore have typed wrong. Sales, refunds, settlements, payroll,
    /// supplier payments and shift-close postings are produced by their own workflows and are corrected there.</summary>
    public static readonly IReadOnlySet<string> CorrectableTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "transfer", "safe_drop", "cash_in", "cash_out", "owner_withdrawal",
        "owner_deposit", "capital_injection", "other_external_inflow",
    };

    public static void EnsureCorrectable(string movementType, string status)
    {
        if (status != nameof(MovementStatus.Posted)) throw new DomainException("الحركة غير مرحّلة ولا يمكن تصحيحها.");
        if (movementType == MovementType) throw new DomainException("هذه حركة تصحيح ولا يمكن تصحيحها مرة أخرى.");
        if (!CorrectableTypes.Contains(movementType))
            throw new DomainException("هذه الحركة ناتجة عن عملية أخرى (بيع، مرتجع، تسوية، مصروف، مورد، رواتب، إغلاق) وتُصحَّح من شاشتها، لا من الخزينة.");
    }

    public static Money CorrectedAmount(Money recorded, long? requestedPiastres)
    {
        if (requestedPiastres is null) return recorded;
        if (requestedPiastres <= 0) throw new DomainException("مبلغ التصحيح يجب أن يكون أكبر من صفر.");
        if (requestedPiastres > recorded.Piastres) throw new DomainException("مبلغ التصحيح لا يمكن أن يتجاوز مبلغ الحركة المسجلة.");
        return Money.FromPiastres(requestedPiastres.Value);
    }
}

/// <summary>
/// AUD-03 classification. A delivered order whose customer paid less than was due leaves a gap that is NOT, by
/// itself, a loss: the customer may still owe it, a manager may have granted it as a discount, it may be written off,
/// or the driver may owe it. Until a manager classifies it, with evidence, it is an unresolved discrepancy - shown on
/// its own, never as realised profit or loss.
/// </summary>
public static class CollectionShortfallRules
{
    public const string CustomerReceivable = "customer_receivable";
    public const string ApprovedDiscount = "approved_discount";
    public const string WriteOff = "write_off";
    public const string DriverLiability = "driver_liability";

    public static readonly IReadOnlyList<string> Classifications = new[] { CustomerReceivable, ApprovedDiscount, WriteOff, DriverLiability };

    /// <summary>Still owed to the restaurant by someone (customer or driver): an outstanding balance, not a loss.</summary>
    public static bool IsReceivable(string classification) => classification is CustomerReceivable or DriverLiability;

    /// <summary>Given up by an authorised decision: a realised reduction of what the sale brought in.</summary>
    public static bool IsRealisedReduction(string classification) => classification is ApprovedDiscount or WriteOff;

    public static void EnsureValid(string? classification, string? evidence)
    {
        if (classification is null || !Classifications.Contains(classification))
            throw new DomainException("تصنيف فرق التحصيل غير صالح. الخيارات: مديونية على العميل، خصم معتمد، إعدام دين معتمد، أو مديونية على السائق.");
        if (string.IsNullOrWhiteSpace(evidence) || evidence.Trim().Length < 5)
            throw new DomainException("اكتب الدليل أو المبرر لهذا التصنيف (من وافق، ولماذا، وأي مستند).");
    }
}

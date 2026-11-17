namespace Falah.Domain.Purchasing;

using Falah.Domain.Common;

/// <summary>
/// Correcting what the restaurant owes a supplier on one invoice, without touching the invoice as received. Each is
/// a credit entry on the supplier ledger (which is what "owed" is computed from), linked to the invoice; none moves
/// money, because none of them is a payment.
///
/// - invoice_error: the invoice was entered wrong (an amount, a price, a line that was not delivered).
/// - supplier_credit: the supplier granted a reduction (a discount, a price correction, compensation).
/// - goods_returned: goods were handed back; the returned quantities leave stock at the invoice's own unit cost, and
///   the credit is exactly their value.
///
/// A credit is limited to what is still owed on the invoice. A credit on an invoice already paid means the supplier
/// owes the restaurant money back - how that is settled (a cash refund, an offset against the next invoice) is the
/// owner's decision, so it is refused here rather than guessed.
/// </summary>
public static class SupplierCreditRules
{
    public const string InvoiceError = "invoice_error";
    public const string SupplierCredit = "supplier_credit";
    public const string GoodsReturned = "goods_returned";

    public static string LedgerTypeFor(string? kind) => kind switch
    {
        InvoiceError => "purchase_correction",
        SupplierCredit => "supplier_credit",
        GoodsReturned => "supplier_return",
        _ => throw new DomainException("نوع التصحيح غير صالح. الخيارات: خطأ في الفاتورة، خصم من المورد، أو مرتجع بضاعة."),
    };

    public static void AssertWithinOutstanding(Money credit, Money outstanding)
    {
        if (!credit.IsPositive) throw new DomainException("مبلغ التصحيح يجب أن يكون أكبر من صفر.");
        if (credit > outstanding)
            throw new DomainException($"مبلغ التصحيح ({credit.Piastres / 100m:0.##} ج) أكبر من المتبقي على الفاتورة ({outstanding.Piastres / 100m:0.##} ج). " +
                "إذا كانت الفاتورة مدفوعة فالمورد مدين لكم بالفرق؛ طريقة استرداده قرار من المالك.");
    }
}

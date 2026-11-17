namespace Falah.Client.CashierWinUI.Services;

/// <summary>
/// Arabic labels for the system's stored vocabularies, as plain functions so view models (and the
/// client tests that compile them without WinUI) can use the same words the XAML converters show.
/// The stored values are English identifiers that reports group by and must never be renamed; an
/// unknown value falls through unchanged so something added server-side later is still visible.
/// </summary>
public static class DisplayLabels
{
    public static string PaymentMethod(string? method) => method?.ToLowerInvariant() switch
    {
        "cash" => "كاش",
        "card" => "فيزا",
        "instapay" => "إنستاباي",
        "wallet" => "محفظة",
        "multi" => "دفع مقسّم",
        null or "" => "",
        _ => method!,
    };

    /// <summary>A null/empty account is "خارج النظام" - a movement with no counterpart account (a
    /// safe drop) is a real case, not missing data.</summary>
    public static string TreasuryAccount(string? accountId) => accountId switch
    {
        "tre-main" => "الخزينة الرئيسية",
        "tre-drawer" => "درج الكاشير",
        "tre-bank" => "البنك",
        "tre-instapay" => "إنستاباي",
        "tre-wallet" => "المحفظة",
        null or "" => "خارج النظام",
        var other => other,
    };

    public static string TreasuryMovementType(string? type) => type switch
    {
        "sale_collection" => "تحصيل مبيعات",
        "shift_handover" => "تسليم وردية",
        "shift_opening_float" => "عهدة افتتاح وردية",
        "safe_drop" => "إيداع بالخزنة",
        "cash_in" => "إيداع نقدي بالدرج",
        "cash_out" => "سحب نقدي من الدرج",
        "driver_settlement" => "تسوية عهدة سائق",
        "owner_deposit" => "إيداع من المالك",
        "capital_injection" => "ضخ رأس مال",
        "other_external_inflow" => "تدفق خارجي آخر",
        "owner_withdrawal" => "سحب المالك",
        "transfer" => "تحويل بين الحسابات",
        "expense" or "expense_payment" => "مصروف",
        "expense_reversal" => "إلغاء مصروف",
        "expense_correction" => "تصحيح مصروف مسجل بالخطأ",
        "expense_refund" => "مبلغ مصروف عاد نقدًا",
        "movement_correction" => "تصحيح حركة مسجلة بالخطأ",
        "refund" => "مرتجع",
        "purchase_payment" => "سداد مشتريات",
        "supplier_payment" => "سداد مورد",
        "reconciliation" => "تسوية",
        // Written by shift close, employee cash-outs, payroll and driver settlement - all of which
        // reached the ledger in English before these were added.
        "cash_overage" => "زيادة نقدية في الدرج",
        "cash_shortage" => "عجز نقدي في الدرج",
        "driver_fee_payout" => "صرف أجر توصيل لسائق",
        "opening_float_shortfall" => "عجز عند استلام عهدة الدرج",
        "opening_float_overage" => "زيادة عند استلام عهدة الدرج",
        "cash_in_transfer" => "تحويل إلى الدرج",
        "cash_out_transfer" => "تحويل من الدرج",
        "employee_advance" => "سلفة موظف",
        "employee_withdrawal" => "مسحوبات موظف",
        "employee_payment" => "صرف مستحقات موظف",
        "payroll_payment" => "صرف رواتب",
        "settlement_overage" => "زيادة في تسوية سائق",
        "settlement_shortage" => "عجز في تسوية سائق",
        null or "" => "",
        var other => other,
    };
}

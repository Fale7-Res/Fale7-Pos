namespace Falah.Domain.Financial;

using Falah.Domain.Common;

/// <summary>
/// The categories an operating expense may be recorded under - a fixed list with no ingredient or
/// stock item in it (owner's decision). Ingredients and packaging bought as an "expense" were counted
/// twice, in operating expenses and again in the food cost recipes compute, and never reached stock;
/// they are bought through Purchasing, which receives them into stock at their cost.
///
/// The names are the cashier screen's own, less the two that were stock ("مستلزمات مطبخ وخضار" -
/// vegetables - and "أكياس وتغليف" - packaging, a tracked inventory item), plus a neutral
/// "مستلزمات تشغيل" for the non-ingredient supplies the first of those also covered. Expenses already
/// recorded under any other name keep it: the list governs what is entered from now on.
/// </summary>
public static class ExpenseCategories
{
    public static readonly IReadOnlyList<string> All = new[]
    {
        "مستلزمات تشغيل",
        "نظافة ومطهرات",
        "صيانة سريعة",
        "غاز وفحم",
        "مواصلات ودليفري",
        "أخرى",
    };

    public static void AssertAllowed(string? category)
    {
        if (string.IsNullOrWhiteSpace(category) || !All.Contains(category.Trim()))
            throw new DomainException(
                $"فئة المصروف غير مسموحة. الفئات المتاحة: {string.Join("، ", All)}. الخامات والتغليف تُشترى من شاشة المشتريات لتدخل المخزون بتكلفتها.");
    }
}

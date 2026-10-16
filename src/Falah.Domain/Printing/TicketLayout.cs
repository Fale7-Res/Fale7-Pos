namespace Falah.Domain.Printing;

/// <summary>
/// The shared visual vocabulary every Falah ticket is built from.
///
/// This exists so the tickets look like one system rather than six. Each composition in PrintRules
/// decides WHAT a ticket says; this decides how a brand line, an order number, an item table or a
/// money line is shaped, once. Without it every route re-invents its own column widths and its own
/// idea of a separator, and they drift apart the first time one of them is edited.
///
/// The column percentages are not arbitrary. They were measured against the real 80mm printer:
/// the quantity column at 14% broke the word "الكمية" across two lines, which Arabic must never do,
/// so the priced table settles at 42/20/17/21 and the unpriced station table at 55/27/18.
/// </summary>
internal static class TicketLayout
{
    // Priced table (cashier, delivery, call center).
    private const int ColItem = 42;
    private const int ColVariant = 20;
    private const int ColQty = 17;
    private const int ColPrice = 21;

    // Unpriced station table (charcoal, grill) - the price column's share is given back to the
    // item name, which is what a cook actually reads.
    private const int StationItem = 55;
    private const int StationVariant = 27;
    private const int StationQty = 18;

    /// <summary>Shown when an item has no variant. A dash states "nothing here"; an empty cell
    /// reads as a rendering fault.</summary>
    internal const string NoValue = "—";

    internal static TicketElement Brand(string restaurantName) =>
        new TicketText(restaurantName, Bold: true, Align: TicketAlign.Center, Size: TicketTextSize.Large);

    internal static TicketElement Caption(string text) =>
        new TicketText(text, Align: TicketAlign.Center);

    /// <summary>The line under the restaurant name. Comes from Settings (SubName), never hardcoded -
    /// it is the owner's own wording and they can change it without a rebuild.</summary>
    internal static TicketElement Tagline(string text) =>
        new TicketText(text, Align: TicketAlign.Center, Size: TicketTextSize.Large);

    /// <summary>Date and time as one compact row, each in its own cell.
    ///
    /// Two cells rather than one string because "18-09-2026 02:08 م" mixes a Latin-digit run with an
    /// Arabic meridiem inside a right-to-left line, and the bidi algorithm reorders the pieces of
    /// such a run in ways that read wrong on paper. Separate cells give each its own run and fix the
    /// order by construction.</summary>
    internal static TicketElement DateTimeRow(string date, string time) =>
        new TicketRow(new[]
        {
            new TicketCell(date, 50, TicketAlign.Center),
            new TicketCell(time, 50, TicketAlign.Center),
        }, TicketTextSize.Large);

    // ---- Cashier ticket vocabulary --------------------------------------------------------------
    // Used by the till's two tickets only; the other routes keep the helpers above until they are
    // reviewed in their own right, so redesigning the cashier cannot move them by side effect.
    //
    // Sized for the printer's real 576-dot raster (528 dots between the safe margins). Plain rows,
    // thin rules, no inverted bands and no boxed grid: on paper, whitespace and weight separate the
    // content better than ink does.

    // The item table. The product name takes the width the wider canvas bought; quantity is wide
    // enough that the header word "الكمية" can never break mid-word.
    private const int CashItem = 62;
    private const int CashQty = 16;
    private const int CashPrice = 22;

    internal static TicketElement BrandTitle(string restaurantName) =>
        new TicketText(restaurantName, Bold: true, Align: TicketAlign.Center, Size: TicketTextSize.Title);

    internal static TicketElement ThinRule() => new TicketRule(TicketRuleStyle.Thin);

    /// <summary>The Falah signature: a short centred rule under the brand name. The only ornament
    /// the tickets carry, so it stays recognisable rather than decorative.</summary>
    internal static TicketElement AccentRule() => new TicketRule(TicketRuleStyle.Accent);

    internal static TicketElement DashedRule() => new TicketRule(TicketRuleStyle.Dashed);

    internal static TicketElement CenteredLine(string text, bool bold = false, TicketTextSize size = TicketTextSize.Large) =>
        new TicketText(text, Bold: bold, Align: TicketAlign.Center, Size: size);

    /// <summary>Two facts side by side, one pinned to each edge - how the metadata breathes without
    /// taking a line each.</summary>
    internal static TicketElement InfoPair(string start, string end) =>
        new TicketRow(new[]
        {
            new TicketCell(start, 50, TicketAlign.Start),
            new TicketCell(end, 50, TicketAlign.End),
        }, TicketTextSize.Large);

    /// <summary>Column captions a size down from the rows beneath them - they label the table, they
    /// are not part of it.</summary>
    internal static TicketElement CashierItemHeader() =>
        new TicketRow(new[]
        {
            new TicketCell("الصنف", CashItem, Bold: true),
            new TicketCell("الكمية", CashQty, TicketAlign.Center, Bold: true),
            new TicketCell("السعر", CashPrice, TicketAlign.End, Bold: true),
        });

    /// <summary>Quantity bold and centred, price at the far edge: two numbers on one row, placed and
    /// weighted so they can never be read as each other.</summary>
    internal static TicketElement CashierItemRow(TicketItem item) =>
        new TicketRow(new[]
        {
            new TicketCell(string.IsNullOrWhiteSpace(item.VariantName)
                ? item.ProductName : $"{item.ProductName} - {item.VariantName}", CashItem, Bold: true),
            new TicketCell(item.Quantity.ToString(), CashQty, TicketAlign.Center, Bold: true),
            new TicketCell($"{item.TotalPrice.Egp} ج", CashPrice, TicketAlign.End),
        }, TicketTextSize.Large);

    /// <summary>A note or offer qualifying the item directly above it, printed immediately after
    /// that item's row and before the separator that ends it.</summary>
    internal static TicketElement CashierItemNote(string text) =>
        new TicketText(text, Size: TicketTextSize.Large);

    internal static TicketElement Rule() => new TicketRule();

    internal static TicketElement Space() => new TicketRule(TicketRuleStyle.Blank);

    /// <summary>The order number on an operational ticket: large enough to find across a kitchen,
    /// without taking the space the items need.
    ///
    /// The number gets a line of its own rather than sharing one with its label. That is partly
    /// hierarchy - it is the thing anyone picking up this ticket looks for first - and partly
    /// correctness: "#" is a bidi-neutral character, so inside an Arabic line it inherits the
    /// paragraph's right-to-left direction and renders on the wrong side of the digits
    /// ("رقم الطلب 0172#"). Alone on its line the number is its own run and reads "#0172".</summary>
    internal static IEnumerable<TicketElement> LargeOrderNumber(string orderNumber)
    {
        yield return new TicketText("رقم الطلب", Align: TicketAlign.Center);
        yield return new TicketText(orderNumber, Bold: true, Align: TicketAlign.Center, Size: TicketTextSize.Large);
    }

    internal static TicketElement Field(string label, string value) => new TicketText($"{label}: {value}");

    /// <summary>A slip's own title under the brand: what this piece of paper is.</summary>
    internal static TicketElement SlipTitle(string text) =>
        new TicketText(text, Bold: true, Align: TicketAlign.Center, Size: TicketTextSize.Large);

    /// <summary>A label at the reading edge and its value at the other, in body size. The slips'
    /// metadata lines - deliberately the same shape as MoneyLine so labels and values line up
    /// whether the value is a name, a time or an amount.</summary>
    internal static TicketElement SlipLine(string label, string value, bool bold = false,
        TicketTextSize size = TicketTextSize.Large) =>
        new TicketRow(new[]
        {
            new TicketCell(label, 52, TicketAlign.Start, bold),
            new TicketCell(value, 48, TicketAlign.End, bold),
        }, size);

    /// <summary>A label and an amount on one line, pinned to opposite edges. Two real columns, not
    /// padding: under a proportional RTL font a padded string does not line up, and every total on
    /// the ticket has to sit in the same place to be scannable.</summary>
    internal static TicketElement MoneyLine(string label, string amount, bool bold = false,
        TicketTextSize size = TicketTextSize.Normal) =>
        new TicketRow(new[]
        {
            new TicketCell(label, 60, TicketAlign.Start, bold),
            new TicketCell(amount, 40, TicketAlign.End, bold),
        }, size);

    internal static TicketElement ItemTableHeader(bool withPrice) => withPrice
        ? new TicketRow(new[]
        {
            new TicketCell("الصنف", ColItem, Bold: true),
            new TicketCell("الحجم", ColVariant, Bold: true),
            new TicketCell("الكمية", ColQty, TicketAlign.Center, Bold: true),
            new TicketCell("السعر", ColPrice, TicketAlign.End, Bold: true),
        })
        : new TicketRow(new[]
        {
            new TicketCell("الصنف", StationItem, Bold: true),
            new TicketCell("الحجم", StationVariant, Bold: true),
            new TicketCell("الكمية", StationQty, TicketAlign.Center, Bold: true),
        });

    /// <summary>Quantity is bold and centred while the price is plain and edge-aligned, so the two
    /// numbers on a row can never be read as each other.</summary>
    internal static TicketElement ItemRow(TicketItem item, bool withPrice) => withPrice
        ? new TicketRow(new[]
        {
            new TicketCell(item.ProductName, ColItem),
            new TicketCell(VariantOf(item), ColVariant),
            new TicketCell(item.Quantity.ToString(), ColQty, TicketAlign.Center, Bold: true),
            new TicketCell($"{item.TotalPrice.Egp} ج", ColPrice, TicketAlign.End),
        })
        : new TicketRow(new[]
        {
            new TicketCell(item.ProductName, StationItem),
            new TicketCell(VariantOf(item), StationVariant),
            new TicketCell(item.Quantity.ToString(), StationQty, TicketAlign.Center, Bold: true),
        });

    internal static string VariantOf(TicketItem item) =>
        string.IsNullOrWhiteSpace(item.VariantName) ? NoValue : item.VariantName!;

    /// <summary>Anything qualifying the item directly above it - a modifier, a kitchen note, the
    /// offer that set its price. Emitted immediately after the row and before the next separator,
    /// so it reads as belonging to that item rather than floating between two of them.</summary>
    internal static IEnumerable<TicketElement> ItemAnnotations(TicketItem item, bool withPriceRule)
    {
        if (!string.IsNullOrWhiteSpace(item.Notes))
            yield return new TicketText($"ملاحظات: {item.Notes}", Bold: true);
        if (withPriceRule && !string.IsNullOrWhiteSpace(item.PriceRuleName))
            yield return new TicketText($"عرض: {item.PriceRuleName}"
                + (item.ListUnitPrice is { IsPositive: true } list ? $" (بدل {list.Egp} ج)" : ""));
    }
}

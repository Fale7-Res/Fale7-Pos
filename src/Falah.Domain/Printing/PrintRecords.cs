namespace Falah.Domain.Printing;

using Falah.Domain.Common;

/// <summary>The four physical printers this restaurant actually has: a cashier receipt printer, a
/// call-center receipt printer, and two independent kitchen station printers (charcoal, grill).
/// Fixed and small like every other real-world catalog in this codebase (TreasuryAccounts,
/// PaymentMethod) - there is no dynamic N-station concept to model, because there is no evidence of
/// one; a fifth station later is a trivial enum addition.</summary>
// DeliveryOrder and DriverDispatch added for the two delivery print events (final pre-go-live
// pass): DeliveryOrder is printed once when a delivery order is CREATED at checkout, DriverDispatch
// once when that delivery is assigned/reassigned to a driver. Both are distinct from the
// customer-facing Receipt/CallCenter ticket and from the kitchen Charcoal/Grill tickets. Existing
// routes and their behavior are unchanged.
public enum PrintRoute { Receipt, CallCenter, Charcoal, Grill, DriverDispatch, DeliveryOrder }

public enum PrintJobStatus { Pending, Succeeded, Failed }

/// <summary>What a print job reproduces. Order is the default and what every job was before the
/// shift and expense slips existed, so it is first and a null column reads as Order.</summary>
public enum PrintDocumentKind { Order, ShiftOpen, ShiftClose, Expense }

/// <summary>Where a piece of text sits across the paper. Start/End rather than Right/Left because
/// these tickets are laid out right-to-left: Start is the right edge, which is where an Arabic line
/// begins. Naming them Right/Left would invert the moment anyone reads the code in reading order.</summary>
public enum TicketAlign { Start, Center, End }

/// <summary>A small fixed scale, not a free-form point size: a thermal ticket has one job - be read at
/// a glance across a counter. Normal is body text, Large a heading, Title the brand line, Huge an
/// order number on an operational ticket, and Display the order number on the customer's own ticket,
/// the one thing on that paper meant to be read from across a room. Title and Display are appended
/// rather than inserted so no existing value changes meaning.</summary>
public enum TicketTextSize { Normal, Large, Huge, Title, Display }

/// <summary>Thin is a one-dot solid line - three dots on paper after stroke thickening - for
/// separators that should divide without shouting. Accent is the Falah signature mark: a short
/// centred rule under the brand name, the one ornament the tickets carry. Appended, so no existing
/// value changes meaning.</summary>
public enum TicketRuleStyle { Light, Heavy, Blank, Dashed, Thin, Accent }

/// <summary>
/// One piece of a printed ticket. Domain never touches bytes or codepages - only content and layout
/// intent - so Arabic lives here as ordinary .NET strings and encoding stays an Infrastructure
/// concern (see EscPosTicketRenderer).
///
/// This is a small closed set of elements rather than a single "line of text with some flags"
/// because the tickets genuinely need more than text: an item table has to align its columns, and a
/// cashier order prints two physically separate tickets from one job. Expressing those as
/// pre-padded strings does not survive a proportional font or right-to-left text, and expressing
/// them as renderer special-cases puts layout decisions in the one place that cannot be unit-tested
/// against paper.
/// </summary>
public abstract record TicketElement;

/// <summary>Inverted draws the text white inside a solid black pill - the thermal equivalent of a
/// highlighted heading. A thermal head has no grey, so a shaded band either dithers into noise or,
/// under stroke thickening, fills in solid; a black pill with white text prints crisp.</summary>
public sealed record TicketText(
    string Text, bool Bold = false,
    TicketAlign Align = TicketAlign.Start,
    TicketTextSize Size = TicketTextSize.Normal,
    bool Inverted = false) : TicketElement;

/// <summary>One cell of a table row. WidthPercent is a share of the printable width rather than a
/// character count, because the ticket is rendered with a proportional font through the platform's
/// own text shaping - character counts only line up in a monospaced world this printer does not
/// live in once Arabic is involved.</summary>
public sealed record TicketCell(
    string Text, int WidthPercent,
    TicketAlign Align = TicketAlign.Start, bool Bold = false);

/// <summary>
/// Inverted fills the row as a full-width black band with white text (a table header, a grand total).
/// Boxed draws it as part of a ruled grid - consecutive boxed rows share their dividing lines.
/// Continuation marks a row that belongs to the boxed row above it (an item's note), so no divider is
/// drawn between them and the note reads as part of its item rather than as a row of its own.
/// </summary>
public sealed record TicketRow(
    IReadOnlyList<TicketCell> Cells,
    TicketTextSize Size = TicketTextSize.Normal,
    bool Inverted = false,
    bool Boxed = false,
    bool Continuation = false) : TicketElement;

public sealed record TicketRule(TicketRuleStyle Style = TicketRuleStyle.Light) : TicketElement;

/// <summary>Cut the paper here and carry on. Lets ONE print job produce more than one physical
/// ticket - the cashier flow prints a compact customer order-number ticket, cuts, then prints the
/// detailed preparation ticket. Doing it this way keeps both under a single job id and therefore a
/// single idempotency key: they print together or not at all, and a retry cannot produce one
/// without the other.</summary>
public sealed record TicketCut : TicketElement;

/// <summary>A QR code, centred, carrying Payload (the restaurant's menu link). Only the image
/// renderer draws it as a code; the character-mode renderer prints the payload as text, because a
/// firmware QR command is unproven on this printer and unknown commands corrupt the stream.</summary>
public sealed record TicketQr(string Payload) : TicketElement;

public sealed record ComposedTicket(PrintRoute Route, string OrderId, string OrderNumber, IReadOnlyList<TicketElement> Lines);

/// <summary>One sold line, exactly as frozen on the order - PreparationStation here is
/// OrderItem.PreparationStation, the station that was authoritative AT SALE TIME, never the
/// product's current live configuration. A later menu change (moving a product from Charcoal to
/// Grill) must never alter how an already-committed order prints or reprints - the same "historical
/// snapshot never changes" guarantee ProductName/UnitPrice/PriceRuleName already give every other
/// field on this record.</summary>
public sealed record TicketItem(
    string ProductName, string? VariantName, int Quantity, Money UnitPrice, Money TotalPrice,
    string? Notes, string? PriceRuleName, Money? ListUnitPrice, string? PreparationStation);

public sealed record ReceiptHeader(
    string OrderId, string RestaurantName, string SubName, string Address, string OrderNumber, DateTimeOffset CreatedAtUtc,
    string CashierName, Orders.OrderType OrderType, string? TableNumber, string? CustomerName,
    string? DeliveryAddress, string? DeliveryArea,
    /// <summary>Ported from SettingsScreen.tsx's phone1/phone2/complaintsPhone, printed by
    /// ReceiptModal.tsx alongside the restaurant identity block - a real gap found during the
    /// legacy-parity audit (these were previously absent from every composed receipt). Trailing
    /// optional so every existing caller/test keeps compiling unchanged; null means "not configured
    /// yet", never printed as an empty line.</summary>
    string? Phone1 = null, string? Phone2 = null, string? ComplaintsPhone = null,
    /// <summary>Ported from ReceiptModal.tsx's own "⚠ غير مدفوع" banner on an order whose electronic
    /// leg is still pending confirmation - a customer-facing warning that money has not actually
    /// arrived yet.</summary>
    bool ElectronicPaymentPending = false,
    /// <summary>Settings-driven footer text (SettingsScreen.tsx's own receiptFooter), replacing the
    /// previously hardcoded "شكرًا لزيارتكم" when configured.</summary>
    string? Footer = null,
    /// <summary>When the customer said they would collect a pickup order. Trailing optional so every
    /// existing caller keeps compiling; null means nobody named a time, which is most pickups.</summary>
    DateTimeOffset? RequestedPickupAtUtc = null,
    /// <summary>The configured menu link, printed as a QR on the detailed receipt. Null prints no QR.</summary>
    string? MenuUrl = null,
    bool? IsPaid = null);

public sealed record ReceiptTotals(
    Money Subtotal, Money Discount, Money TaxAmount, string? TaxRuleName, Money DeliveryFee, Money CompAmount,
    Money Total, Money AmountPaid, Money ChangeDue, Financial.PaymentMethod PaymentMethod);

/// <summary>The handful of facts the DeliveryOrder ticket needs that ReceiptHeader/ReceiptTotals do
/// not already carry: the CUSTOMER's phone (ReceiptHeader's phones are the restaurant's own), the
/// delivery note, and the explicit collect-or-prepaid statement. Kept as its own record rather than
/// widened onto ReceiptHeader so the existing Receipt/CallCenter ticket composition is untouched.
/// <para>There is deliberately no order-level note field: this system only has per-ITEM notes
/// (OrderItemEntity.Notes, printed under each line) and the delivery note below. Inventing an
/// order-level one to fill the ticket would print a field nothing can ever populate.</para></summary>
public sealed record DeliveryOrderTicketInfo(
    string? CustomerPhone, string? DeliveryNotes, Money AmountToCollect, bool IsCashOnDelivery);

/// <summary>Who and when a shift slip is about. ShiftReference is a SHORT human code (not the
/// internal id) so the opening slip and the closing slip of one shift can be matched on paper
/// without printing a GUID at anybody.</summary>
public sealed record ShiftSlipIdentity(
    string RestaurantName, string? SubName, string ShiftReference, string ShiftName,
    string CashierName, DateTimeOffset OpenedAtUtc);

/// <summary>
/// Every figure the handover slip states, exactly as the shift's own reconciliation produced them.
/// Nothing here is re-derived at print time: the slip is a statement of what was persisted.
///
/// The identity that must hold, and which ShiftCashRules.AssertBalances checks:
/// Opening + (CashSales - Refunds) + CashIn - Expenses - Withdrawals - SafeDrops = Expected.
/// CashSales is the GROSS cash taken; Refunds is what was handed back out of those same sales, so
/// the two are shown on their own lines while their net is what the drawer should hold.
/// </summary>
public sealed record ShiftCashBreakdown(
    Money OpeningCash, Money CashSales, Money Refunds, Money CashIn, Money Expenses,
    Money Withdrawals, Money SafeDrops, Money ExpectedCash, Money ActualCash, Money Difference);

/// <summary>One expense on the closing slip's summary - the category and what it cost.</summary>
public sealed record ExpenseSummaryLine(string Category, Money Amount);

/// <summary>A single expense, as its own printed voucher.</summary>
public sealed record ExpenseSlipInfo(
    string RestaurantName, string? SubName, string Reference, DateTimeOffset CreatedAtUtc,
    Money Amount, string Category, string Description, string EmployeeName, string PaymentMethodLabel,
    string? Notes);

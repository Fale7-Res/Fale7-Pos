namespace Falah.Domain.Printing;

using Falah.Domain.Common;

/// <summary>The four physical printers this restaurant actually has: a cashier receipt printer, a
/// call-center receipt printer, and two independent kitchen station printers (charcoal, grill).
/// Fixed and small like every other real-world catalog in this codebase (TreasuryAccounts,
/// PaymentMethod) - there is no dynamic N-station concept to model, because there is no evidence of
/// one; a fifth station later is a trivial enum addition.</summary>
public enum PrintRoute { Receipt, CallCenter, Charcoal, Grill }

public enum PrintJobStatus { Pending, Succeeded, Failed }

/// <summary>One line of a printed ticket, in whatever plain text the ESC/POS renderer will later
/// encode - Domain never touches bytes/codepages, only content and layout intent (bold/centered/
/// large). Arabic text lives here as ordinary .NET strings; encoding is entirely an Infrastructure
/// concern (see EscPosTicketRenderer), and its correctness on the real Xprinter must still be
/// verified physically - this record makes no claim about that.</summary>
public sealed record TicketLine(string Text, bool Bold = false, bool Centered = false, bool LargeFont = false);

public sealed record ComposedTicket(PrintRoute Route, string OrderId, string OrderNumber, IReadOnlyList<TicketLine> Lines);

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
    string? Footer = null);

public sealed record ReceiptTotals(
    Money Subtotal, Money Discount, Money TaxAmount, string? TaxRuleName, Money DeliveryFee, Money CompAmount,
    Money Total, Money AmountPaid, Money ChangeDue, Financial.PaymentMethod PaymentMethod);

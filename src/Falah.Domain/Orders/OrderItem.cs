namespace Falah.Domain.Orders;

using Falah.Domain.Common;

/// <summary>One line of a sold order. ProductName/VariantName/PriceRuleName are frozen snapshots
/// taken at sale time - a later product rename, re-price, or offer edit must never change how a
/// historical order reads. PreparationStation is the same kind of snapshot: which kitchen printer
/// this line routed to AT SALE TIME, frozen so a later menu change (moving a product from Charcoal
/// to Grill) can never alter how an already-committed order prints or reprints - see
/// Falah.Domain.Printing.PrintRules' own doc comment for why routing must read this frozen value,
/// never the product's current live configuration.</summary>
public sealed record OrderItem(
    string Id,
    string ProductId,
    string? VariantId,
    string ProductName,
    string? VariantName,
    Money UnitPrice,
    Money ListUnitPrice,
    int Quantity,
    Money TotalPrice,
    bool SendToGrill,
    string? PriceRuleId,
    string? PriceRuleName,
    string? Notes,
    string? PreparationStation = null);

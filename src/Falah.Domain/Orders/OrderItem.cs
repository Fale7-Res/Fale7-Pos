namespace Falah.Domain.Orders;

using Falah.Domain.Common;

/// <summary>One line of a sold order. ProductName/VariantName/PriceRuleName are frozen snapshots
/// taken at sale time - a later product rename, re-price, or offer edit must never change how a
/// historical order reads.</summary>
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
    string? Notes);

namespace Falah.Domain.Orders;

/// <summary>Reads an order number the way a person types it from a receipt: "#0128", "0128", "128",
/// with or without spaces, in Western or Arabic-Indic digits. Returns the order's sequence number.</summary>
public static class OrderNumberInput
{
    public static bool TryParseSequence(string? typed, out long sequence)
    {
        sequence = 0;
        if (string.IsNullOrWhiteSpace(typed)) return false;
        var text = typed.Trim().TrimStart('#').Trim();
        if (text.Length == 0 || text.Length > 18) return false;
        foreach (var c in text)
        {
            int digit = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= '٠' and <= '٩' => c - '٠',   // Arabic-Indic
                >= '۰' and <= '۹' => c - '۰',   // Extended Arabic-Indic (Persian)
                _ => -1,
            };
            if (digit < 0) return false;
            sequence = sequence * 10 + digit;
        }
        return sequence > 0;
    }
}

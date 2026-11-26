namespace Falah.Application.Delivery;

/// <summary>Ported from addDeliveryArea/updateDeliveryArea in lib/enterprise-store.tsx - a real
/// gap found during the legacy-parity audit: Order already carries DeliveryServiceId/
/// DeliveryServiceName snapshot fields (row 1 of the implementation matrix), but there was no
/// management surface to create/edit/retire the delivery services themselves at all. Editing name
/// or fee never touches a historical order - every order already froze the service name/fee it
/// was actually sold with; Active=false is the only way to retire one (never a hard delete), same
/// soft-archive idiom as Categories/Products/Drivers.</summary>
public sealed record CreateDeliveryServiceRequest(string RequestId, string Name, long? FeePiastres, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record UpdateDeliveryServiceRequest(
    string ServiceId, string? Name, long? FeePiastres, bool? Active, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record DeliveryServiceDto(string Id, string Name, long? FeePiastres, bool Active, string CreatedBy, DateTimeOffset CreatedAtUtc)
{
    /// <summary>How a suggestion reads in the order taker's search box: the name and its fee («مدينة نصر — 30 ج»).
    /// Display only, not on the wire.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string PickerLabel => FeePiastres is { } fee
        ? $"{Name} — {fee / 100}{(fee % 100 == 0 ? "" : "." + (fee % 100).ToString("00"))} ج"
        : Name;
}

/// <summary>Owner, 2026-11-26: with about 50 delivery services the order taker types part of a name and picks from
/// suggestions. Arabic spellings that people mix are treated alike (أ إ آ ا, ة ه, ى ي; diacritics and tatweel
/// ignored); a name that starts with the text comes first, then one with a word that does, then one that only
/// contains it, each group in the list's own order. An empty text lists every service.</summary>
public static class DeliveryServiceSearch
{
    public static IReadOnlyList<DeliveryServiceDto> Find(IEnumerable<DeliveryServiceDto> services, string? text)
    {
        var query = Normalize(text);
        var all = services.ToList();
        if (query.Length == 0) return all;
        var starts = new List<DeliveryServiceDto>();
        var wordStarts = new List<DeliveryServiceDto>();
        var contains = new List<DeliveryServiceDto>();
        foreach (var s in all)
        {
            var name = Normalize(s.Name);
            if (name.StartsWith(query, StringComparison.Ordinal)) starts.Add(s);
            else if (name.Contains(" " + query, StringComparison.Ordinal)) wordStarts.Add(s);
            else if (name.Contains(query, StringComparison.Ordinal)) contains.Add(s);
        }
        return starts.Concat(wordStarts).Concat(contains).ToList();
    }

    /// <summary>The service whose whole name is the text (spelling differences aside), if exactly one is.</summary>
    public static DeliveryServiceDto? Exact(IEnumerable<DeliveryServiceDto> services, string? text)
    {
        var query = Normalize(text);
        if (query.Length == 0) return null;
        var matches = services.Where(s => Normalize(s.Name) == query).Take(2).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }

    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var sb = new System.Text.StringBuilder(text.Length);
        var space = false;
        foreach (var raw in text.Trim().ToLowerInvariant())
        {
            var c = raw switch
            {
                'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
                'ة' => 'ه',
                'ى' => 'ي',
                _ => raw,
            };
            if (c == 'ـ' || (c >= 'ً' && c <= 'ٟ') || c == 'ٰ') continue;   // tatweel, harakat
            if (char.IsWhiteSpace(c)) { if (!space) sb.Append(' '); space = true; continue; }
            space = false;
            sb.Append(c);
        }
        return sb.ToString();
    }
}

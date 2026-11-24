namespace Falah.Domain.Common;

/// <summary>
/// Defensive QA audit 2026-11-23 (docs/FINAL_DEFENSIVE_QA_REPORT.md, fixes approved by the owner): the
/// checks every screen already makes, enforced again where data enters the server, so a request that
/// did not come from a screen is refused with an Arabic message instead of a 500 or a silent mistake.
/// </summary>
public static class InputRules
{
    /// <summary>The longest request id accepted. Clients send a 32-character GUID; the columns that
    /// store request ids hold 450.</summary>
    public const int MaxRequestIdLength = 200;
    public const int MaxNameLength = 200;
    public const int MaxNoteLength = 500;
    public const int MaxReasonLength = 500;
    public const int MaxDescriptionLength = 1000;

    /// <summary>QA-03: a request id is what makes a retry return the original result. An empty one
    /// would make two different operations the "same" one, so it is refused before any replay lookup.</summary>
    public static void AssertRequestId(string? requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId)) throw new DomainException("رقم العملية مطلوب. حدّث البرنامج وأعد المحاولة.");
        if (requestId.Length > MaxRequestIdLength) throw new DomainException("رقم العملية أطول من المسموح.");
    }

    /// <summary>QA-11: free text has an upper bound; the message names the field so it can be corrected.</summary>
    public static void AssertMaxLength(string? value, int maxLength, string fieldLabel)
    {
        if (value is not null && value.Length > maxLength)
            throw new DomainException($"{fieldLabel} أطول من المسموح (الحد {maxLength} حرف).");
    }

    /// <summary>QA-02: Arabic-Indic (٠-٩) and Persian (۰-۹) digits become 0-9; everything else is kept.</summary>
    public static string NormalizeDigits(string value) => string.Concat(value.Select(c =>
        c is >= '٠' and <= '٩' ? (char)('0' + (c - '٠')) :
        c is >= '۰' and <= '۹' ? (char)('0' + (c - '۰')) : c));
}

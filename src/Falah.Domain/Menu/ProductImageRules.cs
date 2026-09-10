namespace Falah.Domain.Menu;

using Falah.Domain.Common;

/// <summary>
/// What the system will accept as a product photo.
///
/// The limits exist for one reason: every till in the restaurant downloads every menu image, over
/// whatever network the branch has, and holds them all in memory while the POS is open. A menu of
/// two hundred products at half a megabyte each is a hundred megabytes of pictures on a machine
/// whose actual job is taking orders. A card on screen is about 250 pixels wide, so anything past
/// a few hundred kilobytes is detail nobody will ever see.
/// </summary>
public static class ProductImageRules
{
    public const int MaxBytes = 512 * 1024;

    /// <summary>Formats a POS card can render and a phone or a camera actually produces. Deliberately
    /// not a general image-format list: SVG is a script host, and the rest are not worth the
    /// decoder surface.</summary>
    public static readonly IReadOnlyList<string> AllowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };

    public static string NormalizeContentType(string? contentType)
    {
        var normalized = (contentType ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized == "image/jpg") normalized = "image/jpeg";
        if (!AllowedContentTypes.Contains(normalized))
            throw new DomainException("صيغة الصورة غير مدعومة. المسموح: JPEG أو PNG أو WebP.");
        return normalized;
    }

    public static void AssertAcceptableSize(int byteCount)
    {
        if (byteCount <= 0) throw new DomainException("ملف الصورة فارغ.");
        if (byteCount > MaxBytes)
            throw new DomainException($"حجم الصورة أكبر من الحد المسموح ({MaxBytes / 1024} كيلوبايت).");
    }

    /// <summary>Verifies the bytes really are the format they claim, by their own header. A file
    /// renamed to .jpg is the ordinary case here, not an attack - but a POS that stores it will
    /// show a broken card on every till until someone works out why.</summary>
    public static void AssertMatchesContentType(string normalizedContentType, ReadOnlySpan<byte> bytes)
    {
        var matches = normalizedContentType switch
        {
            "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            "image/png" => bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47,
            "image/webp" => bytes.Length >= 12
                && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
                && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50,
            _ => false,
        };
        if (!matches) throw new DomainException("محتوى الملف لا يطابق صيغة الصورة المحددة.");
    }
}

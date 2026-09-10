namespace Falah.Domain.Tests;

using Falah.Domain.Common;
using Falah.Domain.Menu;
using Xunit;

/// <summary>
/// Menu photos are downloaded by every till and held in memory for the whole shift, so the size cap
/// is the load-bearing rule here. The header check matters for a duller reason: a file renamed to
/// .jpg is the ordinary mistake, and storing it would leave a broken card on every till until
/// somebody worked out why.
/// </summary>
public class ProductImageRulesTests
{
    private static byte[] Jpeg(int size = 64)
    {
        var bytes = new byte[size];
        bytes[0] = 0xFF; bytes[1] = 0xD8; bytes[2] = 0xFF;
        return bytes;
    }

    private static byte[] Png()
    {
        var bytes = new byte[64];
        bytes[0] = 0x89; bytes[1] = 0x50; bytes[2] = 0x4E; bytes[3] = 0x47;
        return bytes;
    }

    private static byte[] WebP()
    {
        var bytes = new byte[64];
        bytes[0] = 0x52; bytes[1] = 0x49; bytes[2] = 0x46; bytes[3] = 0x46;
        bytes[8] = 0x57; bytes[9] = 0x45; bytes[10] = 0x42; bytes[11] = 0x50;
        return bytes;
    }

    [Theory]
    [InlineData("image/jpeg", "image/jpeg")]
    [InlineData("IMAGE/JPEG", "image/jpeg")]
    [InlineData(" image/png ", "image/png")]
    [InlineData("image/jpg", "image/jpeg")]
    public void Content_types_are_normalised(string input, string expected) =>
        Assert.Equal(expected, ProductImageRules.NormalizeContentType(input));

    [Theory]
    [InlineData("image/svg+xml")]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    [InlineData("")]
    [InlineData(null)]
    public void Unsupported_formats_are_refused(string? contentType) =>
        Assert.Throws<DomainException>(() => ProductImageRules.NormalizeContentType(contentType));

    [Fact]
    public void An_empty_file_is_refused()
    {
        var ex = Assert.Throws<DomainException>(() => ProductImageRules.AssertAcceptableSize(0));
        Assert.Contains("فارغ", ex.Message);
    }

    [Fact]
    public void A_file_at_exactly_the_limit_is_accepted_and_one_byte_over_is_not()
    {
        ProductImageRules.AssertAcceptableSize(ProductImageRules.MaxBytes);
        Assert.Throws<DomainException>(() => ProductImageRules.AssertAcceptableSize(ProductImageRules.MaxBytes + 1));
    }

    [Fact]
    public void Each_format_is_recognised_by_its_own_header()
    {
        ProductImageRules.AssertMatchesContentType("image/jpeg", Jpeg());
        ProductImageRules.AssertMatchesContentType("image/png", Png());
        ProductImageRules.AssertMatchesContentType("image/webp", WebP());
    }

    [Fact]
    public void A_png_renamed_to_jpeg_is_refused()
    {
        var ex = Assert.Throws<DomainException>(() => ProductImageRules.AssertMatchesContentType("image/jpeg", Png()));
        Assert.Contains("لا يطابق", ex.Message);
    }

    [Fact]
    public void A_file_too_short_to_carry_a_header_is_refused()
    {
        Assert.Throws<DomainException>(() => ProductImageRules.AssertMatchesContentType("image/jpeg", new byte[] { 0xFF }));
        Assert.Throws<DomainException>(() => ProductImageRules.AssertMatchesContentType("image/webp", new byte[] { 0x52, 0x49, 0x46, 0x46 }));
    }

    [Fact]
    public void A_riff_container_that_is_not_webp_is_refused()
    {
        // RIFF is also the wrapper for WAV and AVI - matching only the first four bytes would let
        // an audio file through as a product photo.
        var riffWave = new byte[64];
        riffWave[0] = 0x52; riffWave[1] = 0x49; riffWave[2] = 0x46; riffWave[3] = 0x46;
        riffWave[8] = 0x57; riffWave[9] = 0x41; riffWave[10] = 0x56; riffWave[11] = 0x45;
        Assert.Throws<DomainException>(() => ProductImageRules.AssertMatchesContentType("image/webp", riffWave));
    }
}

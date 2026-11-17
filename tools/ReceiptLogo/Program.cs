using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

// Converts the restaurant's colour logo into the 1-bit, 256 x 256-dot logo the print agent embeds
// (src/Falah.PrintAgent/Assets/receipt-logo.png). A thermal head prints black or nothing, so the
// colours are separated by hue rather than by brightness - brightness alone made the red field and
// the green "F" the same grey and the "F" disappeared on paper.
//
// The rules are the owner's choice on real paper, 2026-11-17 ("variant C"):
//   green (the F, the lettering), dark shadows and the silver rim -> ink
//   red on the left half (the shield's red field)                  -> paper
//   red on the right half ("ale7" and its underline)               -> ink
//   yellow, cream, white, and the black background outside        -> paper
//
// usage: dotnet run --project tools/ReceiptLogo -- docs/assets/fale7-logo-original.png src/Falah.PrintAgent/Assets/receipt-logo.png
const int Size = 256;
var source = args.ElementAtOrDefault(0) ?? "docs/assets/fale7-logo-original.png";
var target = args.ElementAtOrDefault(1) ?? "src/Falah.PrintAgent/Assets/receipt-logo.png";

using var original = new Bitmap(source);
using var small = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
using (var g = Graphics.FromImage(small))
{
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    g.DrawImage(original, 0, 0, Size, Size);
}

using var logo = new Bitmap(Size, Size, PixelFormat.Format24bppRgb);
var inked = 0;
for (var y = 0; y < Size; y++)
    for (var x = 0; x < Size; x++)
    {
        var ink = Ink(small.GetPixel(x, y), x);
        if (ink) inked++;
        logo.SetPixel(x, y, ink ? Color.Black : Color.White);
    }
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(target))!);
logo.Save(target, ImageFormat.Png);
Console.WriteLine($"{target}: {Size}x{Size}, {inked} ink dots ({inked * 100 / (Size * Size)}%)");

static bool Ink(Color c, int x)
{
    if (c.R < 28 && c.G < 28 && c.B < 28) return false;                 // background outside the shield
    var hue = c.GetHue();
    var sat = c.GetSaturation();
    var v = Math.Max(c.R, Math.Max(c.G, c.B)) / 255f;
    if (sat < 0.18f) return v < 0.80f;                                   // silver rim and shadows; white/cream stay paper
    if (hue >= 75 && hue <= 200) return true;                            // greens
    if (hue < 18 || hue > 335) return x > Size * 0.505 || v < 0.45f;     // reds: only "ale7" side, and deep shade
    return v < 0.40f;                                                    // yellows and cream: only their deepest shade
}

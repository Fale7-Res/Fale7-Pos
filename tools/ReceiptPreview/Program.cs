using System.Drawing;
using System.Drawing.Imaging;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Domain.Printing;
using Falah.PrintAgent;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

// Offline preview of every piece of paper the till produces. No server, no queue, no printer and no
// configuration is opened: each document is built by the PRODUCTION composition in PrintRules,
// rendered by the PRODUCTION BitmapTicketRenderer at 576 dots / 24-dot margins with stroke
// thickening, and then read back out of the finished ESC/POS bytes - so a preview is the bits, not
// a mock-up. Menu QRs are decoded with an independent reader (ZXing) before anything is reported.
const string menu = "https://fale7-res.vercel.app/";
const string brand = "فالح أبو العنبه";
const string tagline = "ألذ طعم";
var output = Path.GetFullPath(args.FirstOrDefault() ?? "artifacts/receipt-previews");
Directory.CreateDirectory(output);

var soldAt = new DateTimeOffset(2026, 9, 19, 16, 42, 0, TimeSpan.Zero);
var header = new ReceiptHeader("preview-only", brand, tagline, "", "#0286", soldAt, "أحمد محمد",
    OrderType.Takeaway, null, null, null, null, MenuUrl: menu, IsPaid: true);
var items = new[]
{
    new TicketItem("شاورما فراخ", "جامبو", 2, Money.FromEgp(85), Money.FromEgp(170), "بدون بصل، زيادة طحينة", null, null, "Grill"),
    new TicketItem("وجبة مشويات مشكلة مع أرز بسمتي وسلطة وطحينة", "عائلي", 1, Money.FromEgp(245), Money.FromEgp(245), null, null, null, "Charcoal"),
    new TicketItem("بطاطس محمرة", "كبير", 3, Money.FromEgp(30), Money.FromEgp(90), null, null, null, null),
    new TicketItem("بيبسي", null, 2, Money.FromEgp(20), Money.FromEgp(40), null, null, null, null),
};
var totals = new ReceiptTotals(Money.FromEgp(545), Money.Zero, Money.Zero, null, Money.Zero, Money.Zero,
    Money.FromEgp(545), Money.FromEgp(600), Money.FromEgp(55), PaymentMethod.Cash);

var receipt = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, header, items, totals);
Save("A-receipt", receipt, expectQr: true);
Save("B-receipt-reprint", PrintRules.MarkAsReprint(receipt, soldAt.AddMinutes(38), "مدير المحل"), expectQr: true);

var shift = new ShiftSlipIdentity(brand, tagline, "A7F3C1", "صباحية", "أحمد محمد",
    new DateTimeOffset(2026, 9, 19, 5, 10, 0, TimeSpan.Zero));
Save("C-shift-open", PrintRules.ComposeShiftOpenTicket(shift, Money.FromEgp(500)), expectQr: false);

// Opening 500 + (cash sales 4,310 - refunds 95) + cash in 200 - expenses 260 - withdrawals 150
// - safe drops 1,000 = 3,505 expected; counted 3,470, so 35 ج short.
var cash = new ShiftCashBreakdown(
    OpeningCash: Money.FromEgp(500), CashSales: Money.FromEgp(4310), Refunds: Money.FromEgp(95),
    CashIn: Money.FromEgp(200), Expenses: Money.FromEgp(260), Withdrawals: Money.FromEgp(150),
    SafeDrops: Money.FromEgp(1000), ExpectedCash: Money.FromEgp(3505), ActualCash: Money.FromEgp(3470),
    Difference: Money.FromEgp(-35));
var expenses = new[]
{
    new ExpenseSummaryLine("مستلزمات مطبخ", Money.FromEgp(160)),
    new ExpenseSummaryLine("صيانة", Money.FromEgp(100)),
};
var warnings = new[]
{
    "طلبات بانتظار تأكيد تحويل إلكتروني: 2",
    "عهدة سائقين غير مسواة: 1 بقيمة 185 ج",
};
Save("D-shift-close", PrintRules.ComposeShiftCloseTicket(shift, new DateTimeOffset(2026, 9, 19, 18, 05, 0, TimeSpan.Zero),
    cash, expenses, warnings, "فرق بسبب باقي عميل لم يُستلم"), expectQr: false);

Save("E-expense", PrintRules.ComposeExpenseTicket(new ExpenseSlipInfo(
    brand, tagline, "exp-req-77c1", new DateTimeOffset(2026, 9, 19, 12, 30, 0, TimeSpan.Zero),
    Money.FromEgp(160), "مستلزمات مطبخ", "شراء أكياس وتغليف من السوق", "أحمد محمد", "درج الكاشير",
    "اعتماد: مدير المحل")), expectQr: false);

Console.WriteLine($"\npreviews written to {output}");

void Save(string name, ComposedTicket ticket, bool expectQr)
{
    var bytes = BitmapTicketRenderer.Render(ticket, 576, thickenStrokes: true, marginDots: 24);
    File.WriteAllBytes(Path.Combine(output, name + ".escpos"), bytes);

    var pieces = new List<Bitmap>();
    var decoded = new List<string?>();
    for (var p = 0; p + 8 < bytes.Length;)
    {
        if (bytes[p] != 0x1d || bytes[p + 1] != 0x76) { p++; continue; }
        int widthBytes = bytes[p + 4] | bytes[p + 5] << 8, height = bytes[p + 6] | bytes[p + 7] << 8;
        var bmp = new Bitmap(widthBytes * 8, height);
        var gray = new byte[bmp.Width * height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < bmp.Width; x++)
            {
                var ink = (bytes[p + 8 + y * widthBytes + x / 8] & (1 << (7 - x % 8))) != 0;
                bmp.SetPixel(x, y, ink ? Color.Black : Color.White);
                gray[y * bmp.Width + x] = ink ? (byte)0 : (byte)255;
            }
        decoded.Add(DecodeQr(gray, bmp.Width, height));
        bmp.Save(Path.Combine(output, $"{name}-{pieces.Count + 1}.png"), ImageFormat.Png);
        pieces.Add(bmp);
        p += 8 + widthBytes * height;
    }

    Console.WriteLine($"\n== {name}: {pieces.Count} piece(s), {CountCuts(bytes)} cut(s), {bytes.Length} bytes");
    for (var i = 0; i < pieces.Count; i++)
    {
        var piece = pieces[i];
        var (left, top, right, bottom) = InkBounds(piece);
        Console.WriteLine($"   piece {i + 1}: {piece.Width} x {piece.Height} dots = {Mm(piece.Height)} mm long; " +
            $"ink x={left}..{right} ({right - left + 1} dots / {Mm(right - left + 1)} mm), margins {left}/{piece.Width - 1 - right}");
        if (decoded[i] is { } text)
        {
            var qr = QrBounds(piece);
            Console.WriteLine($"      QR decoded: \"{text}\" ({(text == menu ? "exact match" : "MISMATCH")}); " +
                $"{qr.Side} x {qr.Side} dots = {Mm(qr.Side)} mm; quiet zone {qr.Quiet} dots / {Mm(qr.Quiet)} mm");
        }
    }
    var qrPieces = decoded.Count(d => d is not null);
    if (expectQr && qrPieces != 1) throw new Exception($"{name}: expected exactly one QR, found {qrPieces}");
    if (!expectQr && qrPieces != 0) throw new Exception($"{name}: a slip must carry no QR, found {qrPieces}");
    if (expectQr && decoded.Any(d => d is not null && d != menu)) throw new Exception($"{name}: QR is not the configured menu link");
    foreach (var piece in pieces) piece.Dispose();
}

static string? DecodeQr(byte[] gray, int width, int height)
{
    try
    {
        return new QRCodeReader().decode(new BinaryBitmap(new HybridBinarizer(
            new RGBLuminanceSource(gray, width, height, RGBLuminanceSource.BitmapFormat.Gray8))),
            new Dictionary<DecodeHintType, object> { [DecodeHintType.TRY_HARDER] = true })?.Text;
    }
    catch (ReaderException) { return null; }
}

static double Mm(int dots) => Math.Round(dots / 203.0 * 25.4, 1);

static int CountCuts(byte[] bytes)
{
    var cuts = 0;
    for (var i = 0; i + 2 < bytes.Length; i++)
        if (bytes[i] == 0x1D && bytes[i + 1] == 0x56 && bytes[i + 2] == 0x00) cuts++;
    return cuts;
}

static (int Left, int Top, int Right, int Bottom) InkBounds(Bitmap bmp)
{
    int left = bmp.Width, top = bmp.Height, right = -1, bottom = -1;
    for (var y = 0; y < bmp.Height; y++)
        for (var x = 0; x < bmp.Width; x++)
            if (bmp.GetPixel(x, y).R < 128)
            {
                left = Math.Min(left, x); right = Math.Max(right, x);
                top = Math.Min(top, y); bottom = Math.Max(bottom, y);
            }
    return (left, top, right, bottom);
}

/// <summary>The QR's own square: the widest solid block of rows near the bottom of a receipt. Found
/// by the finder pattern's 7-module solid run, the same way the renderer draws it.</summary>
static (int Side, int Quiet) QrBounds(Bitmap bmp)
{
    for (var y = 0; y < bmp.Height; y++)
    {
        for (var x = 0; x < bmp.Width; x++)
        {
            if (bmp.GetPixel(x, y).R >= 128) continue;
            var run = 0;
            while (x + run < bmp.Width && bmp.GetPixel(x + run, y).R < 128) run++;
            if (run >= 21 && run % 7 == 0)
            {
                var module = run / 7;
                var square = true;
                for (var k = 0; k < run && square; k++)
                    square = y + run - 1 < bmp.Height && bmp.GetPixel(x, y + k).R < 128
                        && bmp.GetPixel(x + run - 1, y + k).R < 128 && bmp.GetPixel(x + k, y + run - 1).R < 128;
                if (square && y + module < bmp.Height && bmp.GetPixel(x + module, y + module).R >= 128)
                {
                    var right = x;
                    for (var sx = x; sx < bmp.Width; sx++) if (bmp.GetPixel(sx, y).R < 128) right = sx;
                    var side = right - x + 1;
                    var quiet = 0;
                    while (y - quiet - 1 >= 0 && AllWhite(bmp, x, right, y - quiet - 1)) quiet++;
                    return (side, quiet);
                }
            }
            x += run;
        }
    }
    return (0, 0);
}

static bool AllWhite(Bitmap bmp, int left, int right, int y)
{
    for (var x = left; x <= right; x++) if (bmp.GetPixel(x, y).R < 128) return false;
    return true;
}

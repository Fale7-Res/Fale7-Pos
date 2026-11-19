using System.Drawing;
using System.Drawing.Imaging;
using Falah.Domain.Common;
using Falah.Domain.Delivery;
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
const string tagline = "ألذ سندوتش";   // the owner's wording (Settings sub-name), 2026-11-17
var output = Path.GetFullPath(args.FirstOrDefault() ?? "artifacts/receipt-previews");
Directory.CreateDirectory(output);

// Every paper the system prints (paper redesign 2026-11-17), with synthetic data only.
var at = new DateTimeOffset(2026, 11, 17, 17, 42, 0, TimeSpan.Zero); // 19:42 Cairo
var items = new[]
{
    new TicketItem("شاورما فراخ", "جامبو", 2, Money.FromEgp(85), Money.FromEgp(170), "بدون بصل، زيادة طحينة", null, null, "Grill"),
    new TicketItem("كفتة على الفحم", "نص كيلو", 1, Money.FromEgp(245), Money.FromEgp(245), null, null, null, "Charcoal"),
    new TicketItem("كبدة إسكندراني", "ربع", 2, Money.FromEgp(60), Money.FromEgp(120), "حار", "عرض الغدا", Money.FromEgp(70), "Charcoal"),
    new TicketItem("بطاطس محمرة", "كبير", 3, Money.FromEgp(30), Money.FromEgp(90), null, null, null, null),
    new TicketItem("بيبسي", null, 2, Money.FromEgp(20), Money.FromEgp(40), null, null, null, null),
};
var takeaway = new ReceiptHeader("o-1", brand, tagline, "شارع النصر - المعادي", "#0286", at, "أحمد محمد",
    OrderType.Takeaway, null, null, null, null, Phone1: "01000000000", MenuUrl: menu, IsPaid: true);
var cashTotals = new ReceiptTotals(Money.FromEgp(665), Money.Zero, Money.Zero, null, Money.Zero, Money.Zero,
    Money.FromEgp(665), Money.FromEgp(700), Money.FromEgp(35), PaymentMethod.Cash);
var delivery = new ReceiptHeader("o-2", brand, tagline, "شارع النصر - المعادي", "#0287", at, "منى علي",
    OrderType.Delivery, null, "عميل تجريبي", "15 شارع 9، عمارة 4، الدور 3، شقة 12، بجوار الصيدلية", "المعادي",
    Phone1: "01000000000", MenuUrl: menu, IsPaid: false);
var deliveryTotals = new ReceiptTotals(Money.FromEgp(665), Money.Zero, Money.Zero, null, Money.FromEgp(25), Money.Zero,
    Money.FromEgp(690), Money.Zero, Money.Zero, PaymentMethod.Cash);

var receipt = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, takeaway, items, cashTotals);
Save("01-receipt", receipt, expectQr: false);
Save("01b-receipt-reprint", PrintRules.MarkAsReprint(receipt, at.AddMinutes(38), "مدير المحل"), expectQr: false);
Save("02-callcenter", PrintRules.ComposeReceiptTicket(PrintRoute.CallCenter, delivery, items, deliveryTotals), expectQr: false);
Save("03-charcoal", PrintRules.ComposeKitchenTicket(PrintRoute.Charcoal, brand, "o-1", "#0286", OrderType.Takeaway, items, at), expectQr: false);
Save("04-grill", PrintRules.ComposeKitchenTicket(PrintRoute.Grill, brand, "o-1", "#0286", OrderType.Takeaway, items, at), expectQr: false);
Save("05-delivery-order", PrintRules.ComposeDeliveryOrderTicket(delivery, items, deliveryTotals,
    new DeliveryOrderTicketInfo("01000000000", "الجرس بايظ - كلم العميل لما توصل", Money.FromEgp(690), true)), expectQr: false);
Save("06-driver-dispatch", PrintRules.ComposeDriverDispatchTicket(delivery, items, deliveryTotals,
    new DeliveryOrderTicketInfo("01000000000", "الجرس بايظ - كلم العميل لما توصل", Money.FromEgp(690), true),
    "محمود السواق", at.AddMinutes(20), false), expectQr: true);

DriverStatementOrder Row(string n, string status, long food, long fee, string pay, long collected) => new(
    $"o-{n}", n, at.AddHours(-3), "عميل تجريبي", "01000000000", "المعادي - شارع 9", pay, status,
    food, fee, food + fee, 0, pay == "cash" ? 0 : food + fee, pay == "cash" ? food + fee : 0, collected, false, null, false);
var rows = new[]
{
    Row("#0251", "Delivered", 45_000, 2_500, "cash", 47_500),
    Row("#0263", "Delivered", 32_000, 2_500, "instapay", 0),
    Row("#0270", "Delivered", 61_500, 3_000, "cash", 64_500),
    Row("#0287", "OutForDelivery", 66_500, 2_500, "cash", 0),
};
var driverTotals = DriverStatementRules.Totals(rows);
Save("07-driver-statement-summary", PrintRules.ComposeDriverStatement(brand, tagline, "محمود السواق", new DateOnly(2026, 11, 17), null,
    rows, driverTotals, false, "المدير", at), expectQr: false);
Save("08-driver-statement-detail", PrintRules.ComposeDriverStatement(brand, tagline, "محمود السواق", new DateOnly(2026, 11, 17), null,
    rows, driverTotals, true, "المدير", at), expectQr: false);

var opened = at.AddHours(-6);
Save("09-shift-close", PrintRules.ComposeShiftCloseStatement(new ShiftCloseStatement(
    ShiftCloseStatement.KindName, 1, brand, tagline, "shift-abcdef123456", "A7F3C1", "مسائية", new DateOnly(2026, 11, 17),
    opened, at, "أحمد محمد", "منى علي", null,
    new ShiftStatementSales(42, 1_250_000, 15_000, 10_000, 0, 1_225_000, 9_500, 1_215_500, 900_000, 315_500, 0, 0, 0, 0, 1_215_500, 3),
    new[]
    {
        new ShiftStatementExpense("مستلزمات تشغيل", "ثلج وفحم", 30_000, true, "درج الكاشير", "أحمد محمد", opened.AddHours(1)),
        new ShiftStatementExpense("صيانة سريعة", "تصليح شواية", 50_000, false, "الخزينة الرئيسية", "المدير", opened.AddHours(3)),
    },
    new ShiftStatementCash(100_000, 900_000, 0, 9_500, 30_000, 0, 0, 960_500, 958_000, -2_500, 958_000, "الخزينة الرئيسية"),
    "فرق بسبب باقي عميل لم يُستلم",
    new[] { "تحويلات إلكترونية بانتظار التأكيد: 1 (" + TicketFormat.Code("#0281") + ")" })), expectQr: false);

Save("10-expense", PrintRules.ComposeExpenseTicket(new ExpenseSlipInfo(brand, tagline, "exp-77c1", at, Money.FromEgp(300),
    "مستلزمات تشغيل", "ثلج وفحم للشواية", "أحمد محمد", "درج الكاشير", "اعتماد: مدير المحل")), expectQr: false);

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

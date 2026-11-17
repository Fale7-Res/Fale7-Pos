namespace Falah.Server.IntegrationTests;

using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using Falah.Application.Expenses;
using Falah.Application.Orders;
using Falah.Application.Reports;
using Falah.Application.Reports.Export;
using Falah.Application.Shifts;
using Falah.Domain.Common;
using Falah.Infrastructure.Reports.Export;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Report export: one snapshot, four formats, the same figures. A deterministic day, in piastres: three sales of
/// 10,000 (two cash, one card), 4,000 refunded in cash, a 500 drawer expense whose description is a spreadsheet
/// formula. Net sales = 30,000 − 4,000 = 26,000 (260.00 ج.م).
/// </summary>
public class ReportExportTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _ownerId = default!;
    private string _productId = default!;
    private static readonly DateOnly Today = BusinessClock.Today();
    private const string Injection = "=HYPERLINK(\"http://evil.invalid\",\"اضغط\")";

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "expenses_add_drawer", "orders_refund");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "reports_view");
        _ownerId = await _factory.SeedUserAsync("المالك", "owner", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync(1_000_000);
        _productId = await _factory.SeedProductAsync(10_000, "شاورما فراخ");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private static async Task<T> Ok<T>(Task<HttpResponseMessage> call)
    {
        var r = await call;
        Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.RequestUri?.PathAndQuery} -> {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return (await r.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task TradeAsync()
    {
        var shift = (await Ok<OpenShiftResult>(_client.PostAsJsonAsync("/api/shifts/open", new OpenShiftRequest($"o-{Guid.NewGuid():N}", "صباحية", _cashierId, 50_000)))).ShiftId;
        async Task<CheckoutOrderResult> Sell(string payment) => await Ok<CheckoutOrderResult>(_client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            $"req-{Guid.NewGuid():N}", _cashierId, shift, "takeaway", payment, new[] { new CheckoutItemRequest(_productId, null, 1, null, null) },
            0, 0, 0, null, null, null, 10_000, null, null, null, null, null, null, null, null, null, null, null)));
        var first = await Sell("cash");
        await Sell("cash");
        await Sell("card");
        await Ok<RefundOrderResult>(_client.PostAsJsonAsync("/api/orders/refund", new RefundOrderRequest($"r-{Guid.NewGuid():N}", first.OrderId, 4_000, "صنف ناقص", _cashierId, null, null)));
        await Ok<AddExpenseResult>(_client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest($"e-{Guid.NewGuid():N}", shift, "نظافة ومطهرات", 500, Injection, "tre-drawer", _cashierId, null, null)));
    }

    private Task<HttpResponseMessage> Export(string user, string report, string format, DateOnly? from = null, DateOnly? to = null) =>
        _client.GetAsync($"/api/reports/export?actorUserId={user}&report={report}&format={format}&from={from ?? Today:yyyy-MM-dd}&to={to ?? Today:yyyy-MM-dd}");

    private async Task<(byte[] Bytes, string Fingerprint, string? FileName)> File(string user, string report, string format)
    {
        var r = await Export(user, report, format);
        Assert.True(r.IsSuccessStatusCode, $"{report}.{format} -> {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return (await r.Content.ReadAsByteArrayAsync(), r.Headers.GetValues("X-Falah-Snapshot").Single(), r.Content.Headers.ContentDisposition?.FileNameStar ?? r.Content.Headers.ContentDisposition?.FileName);
    }

    private static Dictionary<string, string> Zip(byte[] bytes)
    {
        using var zip = new ZipArchive(new MemoryStream(bytes));
        return zip.Entries.ToDictionary(e => e.FullName, e => new StreamReader(e.Open(), Encoding.UTF8).ReadToEnd());
    }

    [Fact]
    public async Task Every_format_is_rendered_from_one_snapshot_whose_figures_match_the_screen()
    {
        await TradeAsync();
        var snapshot = (await _client.GetFromJsonAsync<ReportDocument>($"/api/reports/export/snapshot?actorUserId={_managerId}&report=sales&from={Today:yyyy-MM-dd}&to={Today:yyyy-MM-dd}"))!;
        var screen = (await _client.GetFromJsonAsync<SalesReportDto>($"/api/reports/sales?actorUserId={_managerId}&businessDate={Today:yyyy-MM-dd}"))!;
        var net = snapshot.Summary.SelectMany(g => g.Kpis).Single(k => k.Label == "صافي المبيعات").Value.Number;
        Assert.Equal((26_000L, 26_000L), (screen.NetSalesPiastres, net!.Value));
        Assert.Equal(4_000, snapshot.Summary.SelectMany(g => g.Kpis).Single(k => k.Label == "المرتجعات المدفوعة في الفترة").Value.Number);

        var xlsx = await File(_managerId, "sales", "xlsx");
        var docx = await File(_managerId, "sales", "docx");
        var pdf = await File(_managerId, "sales", "pdf");
        var png = await File(_managerId, "sales", "png");
        Assert.All(new[] { xlsx.Fingerprint, docx.Fingerprint, pdf.Fingerprint, png.Fingerprint }, f => Assert.Equal(snapshot.Fingerprint, f));

        // Each file carries that fingerprint and the 260.00 net sales.
        var sheets = Zip(xlsx.Bytes);
        Assert.Contains(snapshot.Fingerprint, sheets["docProps/core.xml"]);
        Assert.Contains(sheets.Where(s => s.Key.StartsWith("xl/worksheets/")), s => s.Value.Contains("<v>260</v>"));
        var word = Zip(docx.Bytes)["word/document.xml"];
        Assert.Contains("260.00 ج.م", word);
        Assert.Contains(snapshot.Fingerprint, Encoding.ASCII.GetString(pdf.Bytes));
        Assert.Contains(snapshot.Fingerprint, Encoding.Latin1.GetString(png.Bytes));
        Assert.Equal($"falah-sales-{Today:yyyyMMdd}-{Today:yyyyMMdd}.xlsx", xlsx.FileName);
    }

    [Fact]
    public async Task The_workbook_is_real_right_to_left_typed_filtered_frozen_and_cannot_run_a_formula()
    {
        await TradeAsync();
        var sheets = Zip((await File(_ownerId, "expenses", "xlsx")).Bytes);

        Assert.Contains("[Content_Types].xml", sheets.Keys);
        var worksheets = sheets.Where(s => s.Key.StartsWith("xl/worksheets/sheet")).Select(s => s.Value).ToList();
        Assert.True(worksheets.Count >= 3);
        Assert.All(worksheets, s => Assert.Contains("rightToLeft=\"1\"", s));
        Assert.Contains(worksheets, s => s.Contains("<autoFilter ref=") && s.Contains("state=\"frozen\""));
        Assert.Contains("ج.م", sheets["xl/styles.xml"]);

        // The formula-looking description is text: an inline string with the quote prefix, and no formula anywhere.
        Assert.DoesNotContain(sheets.Values, v => v.Contains("<f>") || v.Contains("<f "));
        Assert.Contains(sheets.Where(s => s.Key.StartsWith("xl/worksheets/")), s => Regex.IsMatch(s.Value,
            "<c r=\"[A-Z]+\\d+\" t=\"inlineStr\" s=\"12\"><is><t xml:space=\"preserve\">=HYPERLINK"));
        Assert.DoesNotContain(sheets.Keys, k => k.Contains("vbaProject") || k.EndsWith(".bin"));
        // Net expense 500 piastres = 5 pounds, as a number cell with the money style.
        Assert.Contains(worksheets, s => s.Contains("s=\"7\"><v>5</v>"));
    }

    [Fact]
    public async Task Exports_follow_the_screens_permissions_and_are_audited_without_their_figures()
    {
        await TradeAsync();
        var till = await _factory.SeedUserAsync("كاشير فقط", "cashier", "pos");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Export(till, "sales", "pdf")).StatusCode);
        Assert.Empty((await _client.GetFromJsonAsync<List<ReportExportCatalogEntry>>($"/api/reports/export/catalog?actorUserId={till}"))!);

        // reports_view is not the owner: supplier balances are owner-only on screen, and in export.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Export(_managerId, "suppliers", "xlsx")).StatusCode);
        var managerCatalog = (await _client.GetFromJsonAsync<List<ReportExportCatalogEntry>>($"/api/reports/export/catalog?actorUserId={_managerId}"))!;
        Assert.DoesNotContain(managerCatalog, c => c.Key == "suppliers");
        Assert.Contains(managerCatalog, c => c.Key == "owner");
        Assert.NotEmpty((await File(_ownerId, "suppliers", "xlsx")).Bytes);

        Assert.Equal(0, await _factory.QueryAsync(db => db.AuditEvents.CountAsync(a => a.Action == "تصدير تقرير" && a.UserId == till)));
        var audit = await _factory.QueryAsync(db => db.AuditEvents.AsNoTracking().FirstAsync(a => a.Action == "تصدير تقرير" && a.UserId == _ownerId));
        Assert.Contains("suppliers", audit.Details);
        Assert.DoesNotContain("ج.م", audit.Details);   // the event names the export, never its figures
    }

    [Theory]
    [InlineData("nonsense", "pdf", 0, 0)]
    [InlineData("sales", "exe", 0, 0)]
    [InlineData("sales", "pdf", 0, -1)]        // to before from
    [InlineData("sales", "pdf", -400, 0)]      // longer than 366 days
    [InlineData("sales", "pdf", 0, 1)]         // ends after today
    public async Task An_invalid_scope_is_refused_before_anything_is_built(string report, string format, int fromOffset, int toOffset)
    {
        var r = await Export(_ownerId, report, format, Today.AddDays(fromOffset), Today.AddDays(toOffset));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task An_empty_period_still_exports_and_says_there_is_no_data()
    {
        var sheets = Zip((await File(_ownerId, "expenses", "xlsx")).Bytes);
        Assert.Contains(sheets.Values, v => v.Contains("لا توجد بيانات في هذه الفترة"));
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, (await File(_ownerId, "expenses", "png")).Bytes[..4]);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString((await File(_ownerId, "refunds", "pdf")).Bytes[..5]));
    }

    [Fact]
    public async Task A_later_price_change_does_not_move_a_past_report()
    {
        await TradeAsync();
        var before = (await File(_ownerId, "sales", "xlsx")).Fingerprint;
        await _factory.QueryAsync(db => db.Products.Where(p => p.Id == _productId).ExecuteUpdateAsync(s => s.SetProperty(p => p.BasePricePiastres, 99_000L)));
        Assert.Equal(before, (await File(_ownerId, "sales", "xlsx")).Fingerprint);
    }

    [Fact]
    public async Task Sales_are_shown_by_shift_and_by_business_day_each_on_its_own_basis()
    {
        await TradeAsync();
        var snapshot = (await _client.GetFromJsonAsync<ReportDocument>($"/api/reports/export/snapshot?actorUserId={_ownerId}&report=sales&from={Today:yyyy-MM-dd}&to={Today:yyyy-MM-dd}"))!;
        var shifts = snapshot.Tables.Single(t => t.Key == "shifts");
        Assert.Equal(26_000, Assert.Single(shifts.Rows)[5].Number);
        var days = snapshot.Tables.Single(t => t.Key == "business_days");
        Assert.Equal(26_000, Assert.Single(days.Rows)[5].Number);
        Assert.Contains("أيام العمل", days.Basis);
        var channels = snapshot.Tables.Single(t => t.Key == "channels");
        Assert.Equal(26_000, channels.TotalRow![1].Number);   // cash 16,000 + card 10,000
    }

    [Fact]
    public async Task The_owner_report_exports_with_its_reconciliation_and_never_shows_unknown_profit_as_zero()
    {
        await TradeAsync();
        var snapshot = (await _client.GetFromJsonAsync<ReportDocument>($"/api/reports/export/snapshot?actorUserId={_ownerId}&report=owner&from={Today:yyyy-MM-dd}&to={Today:yyyy-MM-dd}"))!;
        var profit = snapshot.Summary.SelectMany(g => g.Kpis).Single(k => k.Label == "النتيجة التشغيلية التقديرية");
        Assert.Null(profit.Value.Number);
        Assert.Equal("unavailable", profit.Status);
        Assert.Contains(snapshot.Warnings, w => w.Contains("غير متاحة"));
        Assert.Contains(snapshot.Notes, n => n.Contains("المطابقة سليمة"));
        Assert.Equal(0, snapshot.Tables.Single(t => t.Key == "bridge").Rows.Single(r => r[0].Text!.StartsWith("فرق غير مفسَّر"))[1].Number);

        var word = Zip((await File(_ownerId, "owner", "docx")).Bytes)["word/document.xml"];
        Assert.Contains("غير متاح", word);
        Assert.Contains("<w:bidi/>", word);
        Assert.Contains("<w:tblHeader/>", word);
    }
}

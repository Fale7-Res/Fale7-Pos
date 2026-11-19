using System.Net;
using System.Text;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>
/// The reports screen's export: the catalog comes from the server (so the list is what this user may export); the
/// request carries the screen's own period; the file is saved where the user chose; a cancelled dialog, a locked file
/// and a server refusal each say so; print opens the PDF for preview instead of printing anything by itself.
/// </summary>
public sealed class ReportExportViewModelTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromResult(send(request));
    }

    private sealed class FakeFiles : IReportFileService
    {
        public Func<byte[], string, string?> Save = (_, name) => $@"C:\Reports\{name}";
        public readonly List<string> Saved = new();
        public readonly List<string> Opened = new();
        public readonly List<string> Printed = new();
        public Task<string?> SaveAsAsync(byte[] content, string suggestedFileName)
        {
            var path = Save(content, suggestedFileName);
            if (path is not null) Saved.Add(suggestedFileName);
            return Task.FromResult(path);
        }
        public string WriteForPrinting(byte[] content, string fileName) { Printed.Add(fileName); return $@"C:\Temp\{fileName}"; }
        public void Open(string path) => Opened.Add(path);
    }

    private static (ReportsViewModel Vm, List<string> Queries, FakeFiles Files) Screen(HttpStatusCode exportStatus = HttpStatusCode.OK)
    {
        var queries = new List<string>();
        var http = new HttpClient(new Handler(r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            if (path == "/api/reports/export/catalog")
                return Json("""[{"key":"sales","title":"تقرير المبيعات","description":"وصف","formats":["xlsx","pdf","docx","png"]}]""");
            if (path == "/api/reports/export")
            {
                queries.Add(r.RequestUri.Query);
                if (exportStatus != HttpStatusCode.OK) return Json("""{"error":"ليس لديك صلاحية هذا التقرير."}""", exportStatus);
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[] { 1, 2, 3 }) };
                response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = "falah-sales-20261114-20261114.xlsx" };
                response.Headers.Add("X-Falah-Snapshot", "0123456789ABCDEF0123");
                return response;
            }
            return Json("{}", HttpStatusCode.NotFound);
        })) { BaseAddress = new Uri("http://fixture.invalid") };
        var session = new AppSession(new ApiClient(http)) { CurrentUser = new("owner", "المالك", "owner", new[] { "all" }) };
        var files = new FakeFiles();
        return (new ReportsViewModel(session) { Files = files }, queries, files);
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task Excel_is_generated_for_the_screens_period_and_saved_where_the_user_chose()
    {
        var (vm, queries, files) = Screen();
        vm.EnsureExportCatalog();
        Assert.True(vm.HasExportReports);
        Assert.Equal("owner", vm.SelectedExportReport!.Key);   // the first report of the shared catalog
        vm.SelectedExportReport = vm.ExportReports.Single(r => r.Key == "sales");

        await vm.ExportCommand.ExecuteAsync("xlsx");

        var query = Assert.Single(queries);
        var today = Falah.Domain.Common.BusinessClock.Today();
        Assert.Contains("report=sales", query);
        Assert.Contains("format=xlsx", query);
        Assert.Contains($"from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}", query);
        Assert.Equal("falah-sales-20261114-20261114.xlsx", Assert.Single(files.Saved));
        Assert.Contains("0123456789ABCDEF", vm.ExportMessage);
        Assert.True(vm.CanOpenLastExport);
        Assert.Null(vm.ExportError);
    }

    [Fact]
    public async Task A_cancelled_dialog_a_locked_file_and_a_refusal_each_say_what_happened()
    {
        var (vm, _, files) = Screen();
        vm.EnsureExportCatalog();

        files.Save = (_, _) => null;
        await vm.ExportCommand.ExecuteAsync("pdf");
        Assert.Equal("أُلغي الحفظ.", vm.ExportMessage);

        files.Save = (_, _) => throw new IOException("locked");
        await vm.ExportCommand.ExecuteAsync("pdf");
        Assert.Contains("تعذر حفظ الملف", vm.ExportError);
        Assert.False(vm.IsExporting);

        var (refused, _, refusedFiles) = Screen(HttpStatusCode.UnprocessableEntity);
        refused.EnsureExportCatalog();
        await refused.ExportCommand.ExecuteAsync("docx");
        Assert.Equal("ليس لديك صلاحية هذا التقرير.", refused.ExportError);
        Assert.Empty(refusedFiles.Saved);
    }

    [Fact]
    public void The_list_follows_the_users_permissions_with_supplier_balances_for_the_owner_only()
    {
        var session = new AppSession(new ApiClient(new HttpClient(new Handler(_ => Json("{}"))) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("mgr", "المدير", "manager", new[] { "reports_view" }) };
        var vm = new ReportsViewModel(session);
        vm.EnsureExportCatalog();
        Assert.Contains(vm.ExportReports, r => r.Key == "owner");
        Assert.DoesNotContain(vm.ExportReports, r => r.Key == "suppliers");

        var till = new ReportsViewModel(new AppSession(new ApiClient(new HttpClient(new Handler(_ => Json("{}"))) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("c", "كاشير", "cashier", new[] { "pos" }) });
        till.EnsureExportCatalog();
        Assert.False(till.HasExportReports);
    }

    [Fact]
    public async Task Print_opens_the_pdf_for_preview_and_never_prints_by_itself()
    {
        var (vm, queries, files) = Screen();
        vm.EnsureExportCatalog();

        await vm.PrintCommand.ExecuteAsync(null);

        Assert.Contains("format=pdf", Assert.Single(queries));
        Assert.Single(files.Printed);
        Assert.Single(files.Opened);
        Assert.Contains("معاينة", vm.ExportMessage);
    }
}

using System.Net;
using System.Text;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Falah.Domain.Common;

namespace Falah.Client.Tests;

/// <summary>UI1: the owner's financial control screen shows each server report as it came - cash flow
/// per account, unconfirmed electronic payments, voids, refunds with their sale's date - for the
/// period chosen, and says so plainly when one of them cannot be read.</summary>
public class FinancialControlViewModelTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static AppSession Session(Func<HttpRequestMessage, Task<HttpResponseMessage>> send, params string[] permissions) =>
        new(new ApiClient(new HttpClient(new Handler(send)) { BaseAddress = new Uri("http://fixture.invalid") }))
        {
            CurrentUser = new("owner", "المالك", "owner", permissions.Length == 0 ? new[] { "all" } : permissions),
        };

    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static string Fixture(string path) => path switch
    {
        "/api/reports/cash-flow" => """
            {"fromBusinessDate":"2026-11-04","toBusinessDate":"2026-11-04","accounts":[
              {"accountId":"tre-drawer","openingPiastres":50000,"inflowPiastres":30000,"outflowPiastres":10000,"closingPiastres":70000,
               "lines":[{"movementType":"sale_collection","inflowPiastres":30000,"outflowPiastres":0,"movementCount":3},
                        {"movementType":"safe_drop","inflowPiastres":0,"outflowPiastres":10000,"movementCount":1}]},
              {"accountId":"tre-main","openingPiastres":100000,"inflowPiastres":10000,"outflowPiastres":0,"closingPiastres":110000,"lines":[]}]}
            """,
        "/api/reports/pending-electronic" => $$"""
            {"count":2,"totalPiastres":15000,"oldestAgeMinutes":150,"payments":[
              {"orderId":"o1","orderNumber":"#0101","method":"instapay","provider":null,"amountPiastres":5000,"createdAtUtc":"{{Now.AddMinutes(-20):O}}","ageMinutes":20,"shiftId":"s1"},
              {"orderId":"o2","orderNumber":"#0102","method":"wallet","provider":"فودافون","amountPiastres":10000,"createdAtUtc":"{{Now.AddMinutes(-150):O}}","ageMinutes":150,"shiftId":"s1"}]}
            """,
        "/api/reports/voids" => $$"""
            {"count":1,"totalPiastres":8000,"voids":[
              {"orderId":"o3","orderNumber":"#0103","kind":"undelivered_cash_on_delivery","totalPiastres":8000,"reason":"العميل لم يرد",
               "cancelledBy":"موظف التوصيل","cancelledAtUtc":"{{Now.AddHours(-1):O}}","stockDisposition":"prepared"}]}
            """,
        "/api/reports/refunds" => $$"""
            [{"orderId":"o4","orderNumber":"#0090","amountPiastres":3000,"paymentMethod":"cash","reason":"صنف ناقص","createdBy":"كاشير",
              "authorizedBy":"المدير","createdAtUtc":"{{Now.AddMinutes(-30):O}}","originalShiftId":"s0","processingShiftId":"s1",
              "originalSaleAtUtc":"{{Now.AddDays(-3):O}}"},
             {"orderId":"o5","orderNumber":"#0104","amountPiastres":2000,"paymentMethod":"card","reason":"تغيير الطلب","createdBy":"كاشير",
              "authorizedBy":"كاشير","createdAtUtc":"{{Now.AddMinutes(-10):O}}","originalShiftId":"s1","processingShiftId":"s1",
              "originalSaleAtUtc":"{{Now:O}}"}]
            """,
        _ => "{}",
    };

    [Fact]
    public async Task Each_section_shows_what_the_server_reported_in_arabic()
    {
        var vm = new FinancialControlViewModel(Session(r => Task.FromResult(Json(Fixture(r.RequestUri!.AbsolutePath)))));
        await vm.LoadAsync();

        Assert.True(vm.HasData);
        var drawer = vm.Accounts.Single(a => a.AccountId == "tre-drawer");
        Assert.Equal("درج الكاشير", drawer.AccountLabel);
        Assert.Equal((50000, 30000, 10000, 70000), (drawer.OpeningPiastres, drawer.InflowPiastres, drawer.OutflowPiastres, drawer.ClosingPiastres));
        Assert.Equal(new[] { "تحصيل مبيعات", "إيداع بالخزنة" }, drawer.Lines.Select(l => l.Label));
        Assert.False(vm.Accounts.Single(a => a.AccountId == "tre-main").HasMovement);

        // Oldest claim first: it is the one that most needs chasing.
        Assert.Equal(new[] { "#0102", "#0101" }, vm.Pending.Select(p => p.OrderNumber));
        Assert.Equal("محفظة (فودافون)", vm.Pending[0].MethodLabel);
        Assert.Equal((2, 15000L, "2 ساعة"), (vm.PendingCount, vm.PendingTotalPiastres, vm.OldestPendingDisplay));

        var voided = Assert.Single(vm.Voids);
        Assert.Equal("توصيل لم يتم (دفع عند الاستلام)", voided.KindLabel);
        Assert.Equal("حُضّر - سُجّل هدرًا", voided.FoodLabel);
        Assert.Equal(8000, vm.VoidsTotalPiastres);

        Assert.Equal(5000, vm.RefundsTotalPiastres);
        // Keep the recent sale on today's date even in the first 40 minutes after Cairo midnight.
        // The sale made three days ago is the only refund classified as an earlier-period sale.
        Assert.Equal(3000, vm.RefundsOfEarlierSalesPiastres);
        Assert.True(vm.Refunds.Single(r => r.OrderNumber == "#0090").SaleBeforePeriod);
        Assert.False(vm.Refunds.Single(r => r.OrderNumber == "#0104").SaleBeforePeriod);
    }

    [Fact]
    public async Task One_unreadable_report_shows_an_error_instead_of_a_partial_page()
    {
        var vm = new FinancialControlViewModel(Session(r => Task.FromResult(r.RequestUri!.AbsolutePath == "/api/reports/voids"
            ? Json("{\"error\":\"تعذر قراءة الإلغاءات\"}", HttpStatusCode.ServiceUnavailable)
            : Json(Fixture(r.RequestUri!.AbsolutePath)))));
        await vm.LoadAsync();

        Assert.False(vm.HasData);
        Assert.Equal("تعذر قراءة الإلغاءات", vm.LoadError);
    }

    [Fact]
    public async Task The_chosen_period_is_what_the_server_is_asked_for()
    {
        var asked = new List<string>();
        var vm = new FinancialControlViewModel(Session(r =>
        {
            asked.Add(r.RequestUri!.PathAndQuery);
            return Task.FromResult(Json(Fixture(r.RequestUri!.AbsolutePath)));
        }));
        asked.Clear();

        vm.SelectedPeriod = vm.Periods.Single(p => p.Key == "last_month");
        await vm.LoadAsync();

        var today = BusinessClock.Today();
        var first = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
        var last = new DateOnly(today.Year, today.Month, 1).AddDays(-1);
        Assert.Contains(asked, q => q.StartsWith("/api/reports/cash-flow") && q.Contains($"from={first:yyyy-MM-dd}") && q.Contains($"to={last:yyyy-MM-dd}"));
        Assert.Contains(asked, q => q.StartsWith("/api/reports/voids") && q.Contains($"from={first:yyyy-MM-dd}"));
        // Pending payments are the current state, never a period.
        Assert.Contains(asked, q => q.StartsWith("/api/reports/pending-electronic") && !q.Contains("from="));
    }

    [Fact]
    public async Task A_custom_span_that_ends_before_it_starts_is_refused_without_asking_the_server()
    {
        var calls = 0;
        var vm = new FinancialControlViewModel(Session(r => { calls++; return Task.FromResult(Json(Fixture(r.RequestUri!.AbsolutePath))); }));
        calls = 0;

        vm.SelectedPeriod = vm.Periods.Single(p => p.Key == "custom");
        vm.CustomFrom = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero);
        vm.CustomTo = new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);
        await vm.LoadAsync();

        Assert.Equal(0, calls);
        Assert.NotNull(vm.LoadError);
    }

    [Fact]
    public async Task Without_reports_view_nothing_is_requested()
    {
        var calls = 0;
        var vm = new FinancialControlViewModel(Session(r => { calls++; return Task.FromResult(Json("{}")); }, "pos"));
        await vm.LoadAsync();

        Assert.False(vm.CanView);
        Assert.Equal(0, calls);
    }

    // FIN-0006 follow-up (2026-11-07): the server now reports a void it cannot attribute to an
    // unconfirmed transfer or an undelivered COD order as "other"; the owner must never read that word.
    [Theory]
    [InlineData("other")]
    [InlineData("some_future_kind")]
    public void Every_void_kind_is_shown_in_arabic(string kind)
    {
        var label = FinancialControlViewModel.VoidKind(kind);
        Assert.DoesNotContain(kind, label);
        Assert.Matches(@"\p{IsArabic}", label);
    }}

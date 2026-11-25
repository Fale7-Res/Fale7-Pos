using System.Net;
using System.Text.Json;
using Falah.Application.BusinessDay;
using Falah.Application.Reports;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Audit 2026-11-25, a full trading day on the real Cashier app - the Control Center's day dialogs.
/// U1: opening a day asked the manager to type the date as raw "YYYY-MM-DD" text, and never said which day would open.
/// U33: closing a day - the end of the restaurant's books for the day - confirmed with only a date and "تأكيد العملية".
/// U32: "عدّ النقدية" stayed the main blue action with no shift open, and refused only after a full denomination count.</summary>
public sealed class ControlCenterTradingDayTests
{
    private static readonly string Lrm = ((char)0x200E).ToString();   // left-to-right mark

    private sealed class Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private static ControlCenterViewModel Center(string? shiftId)
    {
        var session = new AppSession(new ApiClient(new HttpClient(new Handler()) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("actor", "مدير", "manager", new[] { "all" }), ShiftId = shiftId };
        return new ControlCenterViewModel(session);
    }

    private static BusinessDayDto Day(string status = "Open") => new("day-1", "2026-11-25", status, 0, null, null, null, null, null);

    [Fact]
    public void Closing_a_day_confirms_with_its_figures_and_names_the_day()
    {
        var vm = Center(null);
        vm.BusinessDay = Day();
        vm.Reconciliation = JsonSerializer.Deserialize<BusinessDayReconciliationDto>(
            """{"businessDayId":"day-1","businessDate":"2026-11-25","sales":{"orderCount":7,"grossSalesPiastres":46000,"refundsPiastres":4500,"netSalesPiastres":41500},"expensesTotalPiastres":3000,"shiftCount":1,"shiftsShortagePiastres":0,"shiftsOveragePiastres":0}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        vm.DayAction = "close";

        Assert.Equal("إغلاق يوم " + Lrm + "2026-11-25" + Lrm, vm.DayActionConfirmText);   // the date isolated left-to-right
        Assert.Contains("415.00", vm.DayActionSummary);
        Assert.Contains("45.00", vm.DayActionSummary);
        Assert.Contains("30.00", vm.DayActionSummary);
    }

    [Fact]
    public void Opening_a_day_says_which_date_opens_without_typing_one()
    {
        var vm = Center(null);
        vm.DayAction = "open";

        var cairoToday = Falah.Domain.Common.BusinessClock.BusinessDate(DateTimeOffset.UtcNow).ToString("yyyy-MM-dd");
        Assert.Contains(cairoToday, vm.DayActionSummary);
        Assert.Equal("فتح يوم العمل", vm.DayActionConfirmText);
    }

    [Fact]
    public void The_cash_count_is_offered_only_while_a_shift_is_open_and_says_why_not()
    {
        var closed = Center(null);
        closed.BusinessDay = Day();
        Assert.False(closed.CanStartCashCount);
        Assert.False(string.IsNullOrEmpty(closed.CashCountUnavailableHint));

        var open = Center("shift-1");
        open.BusinessDay = Day();
        Assert.True(open.CanStartCashCount);
    }
}

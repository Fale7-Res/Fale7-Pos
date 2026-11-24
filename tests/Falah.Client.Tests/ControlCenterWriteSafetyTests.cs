using System.Net;
using System.Text;
using Falah.Application.BusinessDay;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;

namespace Falah.Client.Tests;

public class ControlCenterWriteSafetyTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }
    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [Theory]
    [InlineData("open")]
    [InlineData("close")]
    [InlineData("reopen")]
    [InlineData("checklist")]
    [InlineData("exception")]
    public async Task Unknown_management_day_write_retains_its_exact_request_until_confirmation(string kind)
    {
        var bodies = new List<(string Path, string Body)>();
        var exists = kind != "open";
        var status = kind == "reopen" ? "Closed" : "Open";
        string Day() => $"{{\"id\":\"day\",\"businessDate\":\"2026-09-08\",\"status\":\"{status}\"}}";
        var http = new HttpClient(new Handler(async r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            if (r.Method == HttpMethod.Get)
            {
                if (path == "/api/business-days") return Json(exists ? "[" + Day() + "]" : "[]");
                if (path is "/api/business-day/current" or "/api/business-day/day")
                    return exists && (status != "Closed" || path.EndsWith("/day")) ? Json(Day())
                        : Json("{\"error\":\"no active day\"}", HttpStatusCode.NotFound);
                return Json("[]");
            }
            bodies.Add((path, await r.Content!.ReadAsStringAsync()));
            if (bodies.Count == 1) throw new HttpRequestException("response lost");
            if (bodies.Count == 2) return Json("{\"error\":\"rejected\"}", HttpStatusCode.UnprocessableEntity);
            exists = true;
            status = kind == "close" ? "Closed" : "Open";
            if (kind is "checklist" or "exception") return new HttpResponseMessage(HttpStatusCode.OK);
            return kind == "open" ? Json("{\"businessDayId\":\"day\",\"status\":\"Open\",\"wasReplayed\":true}") : Json(Day());
        })) { BaseAddress = new Uri("http://fixture.invalid") };
        var session = new AppSession(new ApiClient(http)) { CurrentUser = new("actor", "مدير", "manager", new[] { "all" }) };
        var vm = new ControlCenterViewModel(session);
        await vm.LoadAsync();
        if (kind == "checklist")
            await vm.ToggleChecklistItemCommand.ExecuteAsync(new ChecklistItemStatusDto("item", "مراجعة", "Closing", true, false, null, null));
        else if (kind == "exception")
        {
            vm.OpenExceptionCommand.Execute(new ControlExceptionDto("exception", "category", "warning", "تنبيه", "order", "order", "open", null, null, null));
            vm.ActionReason = "مراجعة";
            await vm.SubmitExceptionCommand.ExecuteAsync(null);
        }
        else
        {
            vm.OpenDayActionCommand.Execute(kind);
            vm.ActionReason = "مراجعة";
            await vm.SubmitDayActionCommand.ExecuteAsync(null);
        }
        Assert.Single(bodies);
        Assert.True(session.HasUnresolvedOperation);
        Assert.False(vm.CanEditAction);
        vm.CloseDayActionCommand.Execute(null);
        vm.CloseExceptionCommand.Execute(null);
        if (kind is "open" or "close" or "reopen") Assert.Equal(kind, vm.DayAction);
        if (kind == "exception") Assert.NotNull(vm.ExceptionTarget);
        vm.ActionReason = "changed"; vm.NewBusinessDateText = "invalid";
        await vm.RetryOperationCommand.ExecuteAsync(null);
        Assert.True(session.HasUnresolvedOperation);
        await vm.RetryOperationCommand.ExecuteAsync(null);
        Assert.Equal(3, bodies.Count);
        Assert.All(bodies, b => Assert.Equal(bodies[0], b));
        Assert.False(session.HasUnresolvedOperation);
        Assert.Null(vm.DayAction);
        Assert.Null(vm.ExceptionTarget);
        Assert.Null(vm.ActionError);
        Assert.Null(vm.LoadError);
    }

    /// <summary>UI review 2026-11-24: a day was closed and the next one opened at once; the open day's screen kept
    /// "تقرير الإغلاق المحفوظ لهذا اليوم" with the closed day's figures and an empty "أغلقه". Clearing the snapshot on
    /// reload never told the card (HasSnapshot) or its figures (SnapshotReconciliation) that it was gone.</summary>
    [Fact]
    public void Clearing_the_close_snapshot_hides_its_card_and_figures()
    {
        var http = new HttpClient(new Handler(_ => Task.FromResult(Json("{}", HttpStatusCode.ServiceUnavailable))))
            { BaseAddress = new Uri("http://fixture.invalid") };
        var vm = new ControlCenterViewModel(new AppSession(new ApiClient(http))
            { CurrentUser = new("actor", "مدير", "manager", new[] { "all" }) });
        vm.LatestSnapshot = System.Text.Json.JsonSerializer.Deserialize<BusinessDayCloseSnapshotDto>(
            """{"Id":"snap","BusinessDayId":"day-1","ClosedBy":"مدير","ReconciliationJson":"{}"}""");
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.LatestSnapshot = null;

        Assert.False(vm.HasSnapshot);
        Assert.Contains(nameof(ControlCenterViewModel.HasSnapshot), raised);
        Assert.Contains(nameof(ControlCenterViewModel.SnapshotReconciliation), raised);
    }

    [Fact]
    public async Task Unknown_day_history_disables_open_close_and_reopen()
    {
        var http = new HttpClient(new Handler(_ => Task.FromResult(Json("{}", HttpStatusCode.ServiceUnavailable))))
            { BaseAddress = new Uri("http://fixture.invalid") };
        var vm = new ControlCenterViewModel(new AppSession(new ApiClient(http))
            { CurrentUser = new("actor", "مدير", "manager", new[] { "all" }) });
        Assert.False(vm.CanOpenDay);
        await vm.LoadAsync();
        Assert.False(vm.CanOpenDay); Assert.False(vm.CanClose); Assert.False(vm.CanReopenDay);
        Assert.NotNull(vm.LoadError);
    }
}

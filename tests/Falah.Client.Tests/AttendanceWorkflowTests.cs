using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Falah.Application.Employees;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;

namespace Falah.Client.Tests;

public class AttendanceWorkflowTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }
    private static HttpResponseMessage Json(object value, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = JsonContent.Create(value) };
    private static AppSession Session(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) =>
        new(new ApiClient(new HttpClient(new Handler(send)) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("actor", "مدير", "manager", new[] { "attendance_manage", "employees_manage" }) };
    private static AttendanceRecordDto Record(string id = "attendance") => new(id, "employee", "موظف", new(2026, 9, 8),
        "08:00", "16:00", "صباحية", "Present", 0, 8, 0, 0, false, null, null, "biometric");

    [Fact]
    public async Task Attendance_retry_keeps_the_original_record_values_and_reason_until_confirmed()
    {
        var bodies = new List<string>();
        var session = Session(async r =>
        {
            if (r.Method == HttpMethod.Get) return Json(new[] { Record() with { WorkHours = 14 } });
            bodies.Add(await r.Content!.ReadAsStringAsync());
            if (bodies.Count == 1) throw new HttpRequestException("confirmation lost");
            if (bodies.Count == 2) return Json(new { error = "unavailable" }, HttpStatusCode.UnprocessableEntity);
            return Json(Record() with { WorkHours = 10 });
        });
        var vm = new AttendanceViewModel(session); vm.OpenCorrectionCommand.Execute(Record());
        vm.WorkHoursText = "١٠"; vm.OvertimeHoursText = "٢٫٥"; vm.CorrectionReason = "مراجعة";
        var date = vm.Date;
        await vm.SaveCorrectionCommand.ExecuteAsync(null);
        Assert.True(session.HasUnresolvedOperation); Assert.False(vm.CanEditCorrection);
        vm.CloseCorrectionCommand.Execute(null); vm.NextDayCommand.Execute(null);
        vm.OpenCorrectionCommand.Execute(Record("another"));
        Assert.Equal(date, vm.Date); Assert.Equal("attendance", vm.CorrectionRecord!.Id);
        vm.WorkHoursText = "invalid"; vm.CorrectionReason = "changed";
        await vm.SaveCorrectionCommand.ExecuteAsync(null);
        Assert.True(session.HasUnresolvedOperation);
        await vm.SaveCorrectionCommand.ExecuteAsync(null);
        Assert.Equal(3, bodies.Count); Assert.All(bodies, b => Assert.Equal(bodies[0], b));
        var sent = JsonSerializer.Deserialize<CorrectAttendanceRequest>(bodies[0], new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(10, sent.WorkHours); Assert.Equal(2.5, sent.OvertimeHours);
        Assert.False(session.HasUnresolvedOperation); Assert.False(vm.IsCorrectionOpen);
        Assert.Equal(14, Assert.Single(vm.Records).WorkHours);
    }

    [Fact]
    public async Task Date_change_during_read_discards_stale_records_and_loads_the_selected_day()
    {
        var first = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var paths = new List<string>();
        var vm = new AttendanceViewModel(Session(r =>
        {
            paths.Add(r.RequestUri!.PathAndQuery);
            return paths.Count == 1 ? first.Task : Task.FromResult(Json(new[] { Record("new-day") }));
        }));
        var load = vm.LoadAsync(); var oldDay = vm.Date;
        vm.NextDayCommand.Execute(null);
        Assert.True(vm.IsLoading); Assert.False(vm.HasRecords);
        first.SetResult(Json(new[] { Record("old-day") })); await load;
        Assert.Equal(oldDay.AddDays(1), vm.Date); Assert.Equal(2, paths.Count);
        Assert.Contains(vm.DateLabel, paths[1]); Assert.Equal("new-day", Assert.Single(vm.Records).Id);
    }

    [Fact]
    public async Task Confirmed_correction_survives_a_failed_followup_read()
    {
        var vm = new AttendanceViewModel(Session(r => Task.FromResult(r.Method == HttpMethod.Get
            ? Json(new { error = "read failed" }, HttpStatusCode.ServiceUnavailable) : Json(Record()))));
        vm.OpenCorrectionCommand.Execute(Record()); vm.CorrectionReason = "تصحيح";
        await vm.SaveCorrectionCommand.ExecuteAsync(null);
        Assert.False(vm.IsCorrectionOpen); Assert.NotNull(vm.ActionNote); Assert.NotNull(vm.LoadError);
        Assert.False(vm.HasRecords); Assert.False(vm.HasNoRecords);
    }

    [Theory]
    [InlineData("NaN", "review")]
    [InlineData("-1", "review")]
    [InlineData("8", "")]
    public async Task Invalid_correction_does_not_write(string hours, string reason)
    {
        var writes = 0;
        var vm = new AttendanceViewModel(Session(_ => { writes++; return Task.FromResult(Json(Record())); }));
        vm.OpenCorrectionCommand.Execute(Record()); vm.WorkHoursText = hours; vm.CorrectionReason = reason;
        await vm.SaveCorrectionCommand.ExecuteAsync(null);
        Assert.Equal(0, writes); Assert.NotNull(vm.CorrectionError);
    }

    [Fact]
    public void Attendance_write_cannot_be_opened_without_the_actual_permission()
    {
        var session = Session(_ => throw new InvalidOperationException("No API call expected"));
        session.CurrentUser = new("actor", "مدير", "manager", new[] { "pos" });
        var vm = new AttendanceViewModel(session); vm.OpenCorrectionCommand.Execute(Record());
        Assert.False(vm.IsCorrectionOpen);
    }

    private static EmployeesViewModel.EmployeeRow Employee(string id) => new()
    {
        Employee = JsonSerializer.Deserialize<EmployeeDto>("{\"id\":\"" + id + "\",\"name\":\"employee\"}", new JsonSerializerOptions(JsonSerializerDefaults.Web))!
    };

    [Fact]
    public async Task Audit_filter_changes_while_loading_only_publish_the_latest_filter_result()
    {
        var first = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var paths = new List<string>();
        var vm = new AuditViewModel(Session(r =>
        {
            paths.Add(r.RequestUri!.PathAndQuery);
            return paths.Count == 1 ? first.Task : Task.FromResult(Json(Array.Empty<Falah.Application.Audit.AuditEventDto>()));
        }));
        var load = vm.LoadAsync();
        vm.SelectedCategory = vm.Categories.Single(c => c.Value == "attendance");
        vm.SelectedSeverity = vm.Severities.Single(s => s.Value == "warning");
        first.SetResult(Json(new { error = "old filter failed" }, HttpStatusCode.ServiceUnavailable));
        await load;
        Assert.Equal(2, paths.Count); Assert.Contains("category=attendance", paths[1]); Assert.Contains("severity=warning", paths[1]);
        Assert.Null(vm.LoadError); Assert.True(vm.HasNoEvents); Assert.False(vm.HasEvents);
    }

    [Fact]
    public async Task Ledger_failure_is_not_an_empty_ledger_and_retry_recovers()
    {
        var reads = 0;
        var vm = new EmployeesViewModel(Session(_ => Task.FromResult(++reads == 1
            ? Json(new { error = "ledger unavailable" }, HttpStatusCode.ServiceUnavailable) : Json(Array.Empty<EmployeeLedgerEntryDto>()))));
        await vm.OpenLedgerCommand.ExecuteAsync(Employee("one"));
        Assert.NotNull(vm.LedgerError); Assert.False(vm.HasNoLedgerEntries);
        await vm.RetryLedgerCommand.ExecuteAsync(null);
        Assert.Null(vm.LedgerError); Assert.True(vm.HasNoLedgerEntries);
    }

    [Fact]
    public async Task A_closed_ledger_read_cannot_overwrite_the_next_employees_result()
    {
        var first = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var vm = new EmployeesViewModel(Session(r => r.RequestUri!.AbsolutePath.Contains("/one/")
            ? first.Task : Task.FromResult(Json(Array.Empty<EmployeeLedgerEntryDto>()))));
        var oldRead = vm.OpenLedgerCommand.ExecuteAsync(Employee("one"));
        vm.CloseLedgerCommand.Execute(null);
        // Direct command execution can still occur while an earlier async command is completing.
        var newRead = vm.OpenLedgerCommand.ExecuteAsync(Employee("two"));
        await newRead;
        first.SetResult(Json(new { error = "old failure" }, HttpStatusCode.ServiceUnavailable)); await oldRead;
        Assert.Equal("two", vm.LedgerEmployee!.Employee.Id);
        Assert.Null(vm.LedgerError); Assert.True(vm.HasNoLedgerEntries); Assert.False(vm.IsLoadingLedger);
    }
}

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Backup;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
namespace Falah.Client.Tests;
public class BackupStatusTests
{
    private sealed class Handler(BackupRecordDto response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = request.Method == HttpMethod.Get
                ? JsonContent.Create(new[] { response }) : JsonContent.Create(response) });
    }
    private static BackupRecordDto Record(string status = "Succeeded", string? verification = "Valid", string? note = null) =>
        new(Guid.NewGuid().ToString("N"), "Backup", "fixture.bak", status, 1024, verification,
            DateTimeOffset.UtcNow, "fixture", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, note, null, null);
    private static BackupViewModel ViewModel(BackupRecordDto response) => new(new AppSession(
        new ApiClient(new HttpClient(new Handler(response)) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("owner", "fixture", "owner", new[] { "all" }) });
    [Theory]
    [InlineData("Valid", " · تم التحقق")]
    [InlineData("Invalid", "فشل التحقق")]
    public void Verification_details_understand_the_server_statuses(string status, string label)
    {
        var record = Record(verification: status); var vm = ViewModel(record); vm.Backups.Add(record);
        Assert.Contains(label, vm.LastBackupDetail);
    }
    [Fact]
    public async Task Valid_verify_result_displays_the_success_note()
    {
        var record = Record(); var vm = ViewModel(record);
        await vm.VerifyBackupCommand.ExecuteAsync(record);
        Assert.Contains("النسخة سليمة", vm.ActionNote);
    }
    [Theory]
    [InlineData(null)]
    [InlineData("تمت الاستعادة لكن تعذر تسجيل ملاحظة داخل قاعدة البيانات.")]
    public async Task Successful_restore_closes_the_form_and_preserves_any_recovery_note(string? note)
    {
        var record = Record(note: note) with { Kind = "Restore" }; var vm = ViewModel(record);
        vm.OpenRestoreCommand.Execute(record); vm.RestoreReason = "fixture";
        vm.RestoreConfirmation = vm.RequiredRestorePhrase;
        await vm.SubmitRestoreCommand.ExecuteAsync(null);
        Assert.Null(vm.RestoreError); Assert.False(vm.IsRestoreOpen);
        Assert.Contains("تمت استعادة", vm.ActionNote);
        if (note is not null) Assert.Contains(note, vm.ActionNote);
    }

    [Fact]
    public async Task Invalid_verify_result_is_an_error_instead_of_a_success_note()
    {
        var record = Record(verification: "Invalid"); var vm = ViewModel(record);
        await vm.VerifyBackupCommand.ExecuteAsync(record);
        Assert.Null(vm.ActionNote); Assert.NotNull(vm.ActionError);
    }

}
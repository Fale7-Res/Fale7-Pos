using System.Net;
using Falah.Application.Treasury;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Native run 2026-11-07 (UI-0001 proof): after a restart the banner read "عملية واحدة لم
/// تُحسم نتيجتها ... (treasury.action، منذ ...)" - an internal storage key, not where to look.</summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class RecoveredNoticeLabelTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "falah-notice-" + Guid.NewGuid().ToString("N"));
    private readonly IRetainedRequestStore? _previous = RetainedRequestStorage.Current;

    public void Dispose()
    {
        RetainedRequestStorage.Current = _previous;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private sealed class Ok : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    [Fact]
    public void The_restart_notice_names_the_screen_not_the_storage_key()
    {
        var store = new FileRetainedRequestStore(_dir);
        RetainedRequestStorage.Current = store;
        store.Save("treasury.action", new DepositRequest("tre-1", "owner_deposit", 12345, "tre-main", "س", null, "u", null, null));

        var root = new MainViewModel(new AppSession(new ApiClient(new HttpClient(new Ok()) { BaseAddress = new Uri("http://fixture.invalid") })));

        Assert.NotNull(root.RecoveredOperationsNotice);
        Assert.DoesNotContain("treasury.action", root.RecoveredOperationsNotice);
        Assert.Contains("الخزينة", root.RecoveredOperationsNotice);
        root.Stop();
    }

    /// <summary>Native run 2026-11-07 (ambiguous-success proof): the recovered deposit was retried and
    /// replayed, its record left the store, yet the banner still said one operation was unresolved -
    /// it was computed once at startup and never again.</summary>
    [Fact]
    public void The_restart_notice_goes_away_once_the_recovered_operation_is_resolved()
    {
        var store = new FileRetainedRequestStore(_dir);
        RetainedRequestStorage.Current = store;
        store.Save("treasury.action", new DepositRequest("tre-1", "owner_deposit", 12345, "tre-main", "س", null, "u", null, null));
        var session = new AppSession(new ApiClient(new HttpClient(new Ok()) { BaseAddress = new Uri("http://fixture.invalid") }));
        var root = new MainViewModel(session);
        var screen = new object();
        session.SetManagementOperationPending(screen, true);

        store.Clear("treasury.action");                       // the retry replayed; the record is gone
        session.SetManagementOperationPending(screen, false);

        Assert.Null(root.RecoveredOperationsNotice);
        root.Stop();
    }
}

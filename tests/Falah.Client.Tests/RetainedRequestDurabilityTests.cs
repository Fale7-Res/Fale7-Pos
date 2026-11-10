namespace Falah.Client.Tests;

using Falah.Client.CashierWinUI.Services;
using Xunit;

/// <summary>
/// What happens to an in-flight operation when the station loses power.
///
/// The retained request already protected a lost response within one session. These tests cover the
/// case the hardware actually suffers: the process is gone, and on the next start the operator needs
/// the exact submitted bytes back so the server can be asked what happened. Recovery is passive on
/// purpose - nothing is re-sent by itself.
/// </summary>
public sealed record SampleRequest(string RequestId, long AmountPiastres, string Reason);
public sealed record OtherRequest(string RequestId, string Note);

public class RetainedRequestDurabilityTests : IDisposable
{
    private readonly string _directory;

    public RetainedRequestDurabilityTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "falah-retained-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* temp cleanup only */ }
    }

    private FileRetainedRequestStore Store() => new(_directory);

    private static ApiClient.ApiResult<bool> Ok() => new(true, true, null);
    private static ApiClient.ApiResult<bool> Unknown() => new(false, false, "timeout", OutcomeUnknown: true);
    private static ApiClient.ApiResult<bool> Rejected() => new(false, false, "rejected");

    [Fact]
    public async Task An_ambiguous_outcome_survives_the_process_that_submitted_it()
    {
        var store = Store();
        var submitted = new SampleRequest("req-1", 12345, "سلفة");

        var first = new RetainedRequest<SampleRequest>("test.owner", store);
        await first.SendAsync(submitted, _ => Task.FromResult(Unknown()));

        // The process ends here. A new one starts.
        var recovered = new RetainedRequest<SampleRequest>("test.owner", store);

        Assert.True(recovered.HasPending);
        Assert.True(recovered.WasRecovered);
        Assert.Equal(submitted, recovered.Value);
    }

    [Fact]
    public async Task A_resolved_outcome_leaves_nothing_to_recover()
    {
        var store = Store();
        var first = new RetainedRequest<SampleRequest>("test.owner", store);
        await first.SendAsync(new SampleRequest("req-1", 100, "سبب"), _ => Task.FromResult(Ok()));

        var recovered = new RetainedRequest<SampleRequest>("test.owner", store);

        Assert.False(recovered.HasPending);
        Assert.False(recovered.WasRecovered);
        Assert.Empty(store.List());
    }

    [Fact]
    public async Task A_plain_rejection_of_a_first_attempt_leaves_nothing_to_recover()
    {
        var store = Store();
        var first = new RetainedRequest<SampleRequest>("test.owner", store);
        await first.SendAsync(new SampleRequest("req-1", 100, "سبب"), _ => Task.FromResult(Rejected()));

        Assert.False(new RetainedRequest<SampleRequest>("test.owner", store).HasPending);
    }

    [Fact]
    public async Task A_rejection_after_an_ambiguous_outcome_keeps_the_request_on_disk()
    {
        // The whole point: a later "no" does not prove the earlier lost request did not commit.
        var store = Store();
        var first = new RetainedRequest<SampleRequest>("test.owner", store);
        var submitted = new SampleRequest("req-1", 999, "سبب");
        await first.SendAsync(submitted, _ => Task.FromResult(Unknown()));
        await first.SendAsync(submitted, _ => Task.FromResult(Rejected()));

        var recovered = new RetainedRequest<SampleRequest>("test.owner", store);
        Assert.True(recovered.HasPending);
        Assert.Equal(submitted, recovered.Value);
    }

    [Fact]
    public async Task The_recovered_request_is_re_sent_byte_for_byte_not_rebuilt()
    {
        var store = Store();
        var submitted = new SampleRequest("req-1", 4242, "مبلغ محدد");
        var first = new RetainedRequest<SampleRequest>("test.owner", store);
        await first.SendAsync(submitted, _ => Task.FromResult(Unknown()));

        var recovered = new RetainedRequest<SampleRequest>("test.owner", store);
        SampleRequest? sent = null;
        // The screen's retry passes a freshly built request; the retained one must win.
        await recovered.SendAsync(new SampleRequest("req-2", 1, "مبلغ مختلف"), r =>
        {
            sent = r;
            return Task.FromResult(Ok());
        });

        Assert.Equal(submitted, sent);
    }

    [Fact]
    public async Task Confirming_a_recovered_request_clears_it_for_good()
    {
        var store = Store();
        var submitted = new SampleRequest("req-1", 500, "سبب");
        await new RetainedRequest<SampleRequest>("test.owner", store)
            .SendAsync(submitted, _ => Task.FromResult(Unknown()));

        var recovered = new RetainedRequest<SampleRequest>("test.owner", store);
        await recovered.SendAsync(submitted, _ => Task.FromResult(Ok()));

        Assert.False(recovered.HasPending);
        Assert.False(new RetainedRequest<SampleRequest>("test.owner", store).HasPending);
    }

    [Fact]
    public async Task Owners_do_not_recover_each_others_requests()
    {
        var store = Store();
        await new RetainedRequest<SampleRequest>("owner.a", store)
            .SendAsync(new SampleRequest("req-a", 1, "أ"), _ => Task.FromResult(Unknown()));

        Assert.True(new RetainedRequest<SampleRequest>("owner.a", store).HasPending);
        Assert.False(new RetainedRequest<SampleRequest>("owner.b", store).HasPending);
    }

    [Fact]
    public async Task An_owner_that_submits_several_command_types_recovers_the_right_one()
    {
        // Screens whose retained request is typed as object submit more than one command shape.
        var store = Store();
        await new RetainedRequest<object>("owner.mixed", store)
            .SendAsync(new OtherRequest("req-x", "ملاحظة"), _ => Task.FromResult(Unknown()));

        var recovered = new RetainedRequest<object>("owner.mixed", store);

        var value = Assert.IsType<OtherRequest>(recovered.Value);
        Assert.Equal("req-x", value.RequestId);
        Assert.Equal("ملاحظة", value.Note);
    }

    [Fact]
    public async Task Without_an_owner_key_nothing_is_written_to_disk()
    {
        // Keeps the pre-existing in-memory-only behaviour available. An owner-less retained request
        // never touches a store at all, so no ambient state has to be set or restored here.
        var store = Store();
        var retained = new RetainedRequest<SampleRequest>(ownerKey: null, store);
        await retained.SendAsync(new SampleRequest("req-1", 1, "سبب"), _ => Task.FromResult(Unknown()));

        Assert.True(retained.HasPending);
        Assert.False(retained.WasRecovered);
        Assert.Empty(store.List());
    }

    [Fact]
    public async Task A_pending_operation_is_listed_with_the_time_it_was_submitted()
    {
        var store = Store();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        await new RetainedRequest<SampleRequest>("owner.listed", store)
            .SendAsync(new SampleRequest("req-1", 1, "سبب"), _ => Task.FromResult(Unknown()));

        var record = Assert.Single(store.List());
        Assert.Equal("owner.listed", record.OwnerKey);
        Assert.Equal("SampleRequest", record.TypeName);
        Assert.True(record.SavedAtUtc >= before);
    }

    [Fact]
    public void A_corrupt_pending_file_does_not_block_the_station()
    {
        // A half-written file from a power cut must not strand the screen that owns it.
        File.WriteAllText(Path.Combine(_directory, "owner.broken.json"), "{ not json");
        var store = Store();

        var recovered = new RetainedRequest<SampleRequest>("owner.broken", store);

        Assert.False(recovered.HasPending);
    }

    [Fact]
    public async Task A_store_that_cannot_write_still_protects_the_current_session()
    {
        // Losing persistence is not a reason to lose the in-memory guarantee that came before it.
        var failing = new FileRetainedRequestStore(_directory);
        Directory.Delete(_directory, recursive: true);
        File.WriteAllText(_directory, "not a directory");

        var retained = new RetainedRequest<SampleRequest>("owner.unwritable", failing);
        var submitted = new SampleRequest("req-1", 7, "سبب");
        await retained.SendAsync(submitted, _ => Task.FromResult(Unknown()));

        Assert.True(retained.HasPending);
        Assert.Equal(submitted, retained.Value);

        File.Delete(_directory);
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public async Task Reviewed_recovery_clears_the_durable_question_but_never_drops_an_in_flight_send()
    {
        var request = new SampleRequest("reviewed", 100, "اختبار");
        var store = Store();
        var retained = new RetainedRequest<SampleRequest>("review.owner", store);
        var completion = new TaskCompletionSource<ApiClient.ApiResult<bool>>();
        var sending = retained.SendAsync(request, _ => completion.Task);
        Assert.Throws<InvalidOperationException>(() => retained.DiscardAfterReview(true));
        completion.SetResult(Unknown()); await sending;
        Assert.Throws<InvalidOperationException>(() => retained.DiscardAfterReview(false));
        retained.DiscardAfterReview(true);
        Assert.False(retained.HasPending);
        Assert.Empty(store.List());
        Assert.False(new RetainedRequest<SampleRequest>("review.owner", store).HasPending);
    }


    [Fact]
    public void A_pending_request_is_not_readable_on_disk()
    {
        var store = Store(); var request = new SampleRequest("private", 100, "customer-01012345678");
        store.Save("private.owner", request);
        Assert.DoesNotContain(request.Reason, File.ReadAllText(Path.Combine(_directory, "private.owner.json")));
        Assert.Equal(request, Assert.IsType<SampleRequest>(store.Load("private.owner")));
    }
    [Fact]
    public void A_pending_file_written_before_protection_is_recovered_and_then_protected()
    {
        var request = new SampleRequest("legacy", 200, "legacy-01098765432");
        var path = Path.Combine(_directory, "legacy.owner.json");
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(new { Type = typeof(SampleRequest).AssemblyQualifiedName, SavedAtUtc = DateTimeOffset.UtcNow, Payload = request }));
        var store = Store();
        Assert.Equal(request, Assert.IsType<SampleRequest>(store.Load("legacy.owner")));
        Assert.DoesNotContain(request.Reason, File.ReadAllText(path));
    }


    private sealed class WrongWindowsProfile : Falah.Client.Security.ILocalDataProtector
    {
        public byte[] Protect(byte[] value) => Falah.Client.Security.WindowsUserDataProtection.Instance.Protect(value);
        public byte[] Unprotect(byte[] value) => throw new Falah.Client.Security.PendingDataProtectionException("wrong profile");
    }
    [Fact]
    public void A_protected_file_that_cannot_be_decrypted_is_preserved_and_reported()
    {
        var store = Store(); store.Save("locked.owner", new SampleRequest("locked", 100, "sensitive"));
        var path = Path.Combine(_directory, "locked.owner.json"); var before = File.ReadAllText(path);
        var wrongProfile = new FileRetainedRequestStore(_directory, protector: new WrongWindowsProfile());
        Assert.Null(wrongProfile.Load("locked.owner"));
        Assert.NotNull(wrongProfile.RecoveryError);
        Assert.Equal(before, File.ReadAllText(path));
    }

}

namespace Falah.Client.CashierWinUI.Services;

/// <summary>Keeps a submitted immutable DTO until the server confirms its outcome. A later
/// rejection cannot prove that an earlier lost response did not commit the operation.
///
/// The retained DTO is also written to disk before it is sent and removed once the outcome is known,
/// so a station that loses power mid-operation can still ask the server what happened on the next
/// start instead of losing the question entirely. Recovery is deliberately passive: the request comes
/// back so the screen's own retry can re-send the exact same bytes, and nothing is re-sent on its
/// own.</summary>
public sealed class RetainedRequest<TRequest> where TRequest : class
{
    private readonly string? _ownerKey;
    private readonly IRetainedRequestStore? _store;

    /// <summary>An owner key makes the retained request durable. It must be stable across restarts -
    /// it is what reconnects a recovered request to the screen that submitted it - so it is written
    /// explicitly rather than derived from a type or line number that refactoring would move.</summary>
    public RetainedRequest(string? ownerKey = null, IRetainedRequestStore? store = null)
    {
        _ownerKey = ownerKey;
        _store = ownerKey is null ? null : store ?? RetainedRequestStorage.Current;
        if (_ownerKey is not null) Value = _store?.Load(_ownerKey) as TRequest;
        WasRecovered = Value is not null;
    }

    public TRequest? Value { get; private set; }
    public bool HasPending => Value is not null;

    /// <summary>True when this request came back from disk rather than from this session - the
    /// operation was submitted before the process ended and its outcome is still unknown.</summary>
    public bool WasRecovered { get; private set; }

    public async Task<ApiClient.ApiResult<TResult>> SendAsync<TResult>(
        TRequest request, Func<TRequest, Task<ApiClient.ApiResult<TResult>>> send)
    {
        var wasUnresolved = Value is not null;
        Value ??= request;
        // Persisted before the send, not after: the window this protects is the one where the
        // request has left but its outcome has not come back.
        if (_ownerKey is not null && !wasUnresolved) _store?.Save(_ownerKey, Value);

        var result = await send(Value);
        if (result.Success && result.Value is not null || !wasUnresolved && !result.OutcomeUnknown)
        {
            Value = null;
            WasRecovered = false;
            if (_ownerKey is not null) _store?.Clear(_ownerKey);
        }
        return result;
    }

    public string FailureMessage(string? error) => HasPending
        ? "لم تُحسم نتيجة العملية. أعد المحاولة بنفس الطلب للتحقق قبل المغادرة."
        : error ?? "تعذر تنفيذ العملية.";
}

using Falah.Application.Users;

namespace Falah.Client.CallCenterWinUI.Services;

/// <summary>Shared, process-wide session state for the Call Center station. Unlike Cashier's
/// AppSession, this station never opens its own shift - ShiftId/ShiftName are discovered from the
/// single system-wide open shift (GET /api/shifts/current) after login and re-checked before each
/// checkout, since the Call Center desk has no drawer of its own to justify owning shift lifecycle.
/// Deliberately NOT persisted anywhere on this client, same as Cashier.</summary>
public sealed class AppSession
{
    public ApiClient Api { get; }

    /// <summary>Same LAN-configurable host as the Cashier station: this client is designed to run on
    /// a different machine from the server, so the address can never be a hardcoded localhost.</summary>
    public AppSession(ApiClient? api = null) =>
        Api = api ?? new(Environment.GetEnvironmentVariable("FALAH_SERVER_URL") ?? "http://localhost:5151");

    public UserSummaryDto? CurrentUser { get; set; }
    public string? ShiftId { get; set; }
    public string? ShiftName { get; set; }

    private readonly Dictionary<string, ViewModels.CallCenterViewModel> _drafts = new();

    /// <summary>One retained order draft per logged-in operator, so locking the screen mid-call and
    /// unlocking does not silently discard a half-taken phone order.</summary>
    public ViewModels.CallCenterViewModel GetOrderDesk(ViewModels.HeaderViewModel header)
    {
        var id = CurrentUser?.Id ?? throw new InvalidOperationException("Login required");
        if (!_drafts.TryGetValue(id, out var draft))
            _drafts[id] = draft = new ViewModels.CallCenterViewModel(this, header);
        return draft;
    }

    /// <summary>Blocks locking the screen while a financial command's outcome is still unknown -
    /// same rule the Cashier station enforces, for the same reason: walking away from an
    /// unresolved checkout is how a duplicate order gets created by the next person.</summary>
    public bool HasUnresolvedOperation => RetainedRequestStorage.Current?.RecoveryError is not null
        || _pendingOperations.Count > 0 || _drafts.Values.Any(draft => !draft.CanEditOperation);

    /// <summary>Screens other than the order desk (the delivery board) that are sending a command or hold one
    /// whose answer never came back.</summary>
    private readonly HashSet<object> _pendingOperations = new();

    public void SetOperationPending(object owner, bool pending)
    {
        if (pending) _pendingOperations.Add(owner);
        else _pendingOperations.Remove(owner);
    }

    private ViewModels.DeliveryViewModel? _deliveryBoard;
    private ViewModels.OrdersViewModel? _orderHistory;

    /// <summary>One board and one history per station, whoever is logged in: the board's retained delivery
    /// commands belong to the station's disk, not to one operator.</summary>
    public ViewModels.DeliveryViewModel DeliveryBoard => _deliveryBoard ??= new ViewModels.DeliveryViewModel(this);
    public ViewModels.OrdersViewModel OrderHistory => _orderHistory ??= new ViewModels.OrdersViewModel(this);

    public bool HasPermission(string permission) =>
        CurrentUser is not null && (CurrentUser.Permissions.Contains("all") || CurrentUser.Permissions.Contains(permission));

    /// <summary>Refreshes ShiftId/ShiftName from the server's single active shift. Returns false
    /// (leaving ShiftId null) when no shift is open anywhere - the caller must not let checkout
    /// proceed in that case, since CheckoutOrderHandler itself requires an Open shift to exist.</summary>
    public async Task<bool> RefreshCurrentShiftAsync(CancellationToken ct = default)
    {
        var result = await Api.GetCurrentShiftAsync(ct);
        if (!result.Success || result.Value is null)
        {
            ShiftId = null;
            ShiftName = null;
            return false;
        }
        ShiftId = result.Value.ShiftId;
        ShiftName = result.Value.ShiftName;
        return true;
    }
}

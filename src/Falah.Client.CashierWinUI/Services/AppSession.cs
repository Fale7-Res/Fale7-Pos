using Falah.Application.Users;

namespace Falah.Client.CashierWinUI.Services;

/// <summary>Shared, process-wide session state: who is logged in and which shift they are
/// operating under. Deliberately NOT persisted anywhere on this client - restarting the app means
/// logging in again, matching the reference app's own lock-screen-on-launch behavior.</summary>
public sealed class AppSession
{
    public ApiClient Api { get; }
    public AppSession(ApiClient? api = null) => Api = api ?? new(Environment.GetEnvironmentVariable("FALAH_SERVER_URL") ?? "http://localhost:5151");
    public ManagementRecovery Recovery { get; } = new();
    public bool HasUnresolvedOperation => RetainedRequestStorage.Current?.RecoveryError is not null || _managementOperations.Count > 0 || _drafts.Values.Any(vm => !vm.CanEditOperation || vm.IsOperationBusy);
    private readonly HashSet<object> _managementOperations = new();
    public event Action? OperationStateChanged;

    public void TrackManagementOwner(IManagementRequestOwner owner) =>
        SetManagementOperationPending(owner, owner.RecoveryRequests.Any(r => r.HasPending));

    public void SetManagementOperationPending(object owner, bool pending)
    {
        var changed = pending ? _managementOperations.Add(owner) : _managementOperations.Remove(owner);
        Recovery.Refresh(_managementOperations.OfType<IManagementRequestOwner>());
        if (changed) OperationStateChanged?.Invoke();
    }
    private readonly Dictionary<string, ViewModels.CashierViewModel> _drafts = new();
    public ViewModels.CashierViewModel GetCashier(ViewModels.HeaderViewModel header)
    {
        var id = CurrentUser?.Id ?? throw new InvalidOperationException("Login required");
        if (!_drafts.TryGetValue(id, out var vm))
            _drafts[id] = vm = new ViewModels.CashierViewModel(this, header);
        return vm;
    }
    public UserSummaryDto? CurrentUser { get; set; }
    public string? ShiftId { get; set; }
    public string? ShiftName { get; set; }

    public bool HasPermission(string permission) =>
        CurrentUser is not null && (CurrentUser.Permissions.Contains("all") || CurrentUser.Permissions.Contains(permission));

    /// <summary>Whether the user at the till is someone who could issue a manager approval for
    /// <paramref name="action"/> themselves - the same rule as the server's
    /// <c>AuthorizationPolicy.CanIssueApproval</c>, which lets them authorise their own large discount
    /// or refund. The server decides; this only saves asking a manager for a manager's PIN.</summary>
    public bool CanApproveOwn(string action) =>
        CurrentUser is not null
        && (Falah.Domain.Authorization.UserManagementRules.IsRole(CurrentUser.Role, "owner")
            || Falah.Domain.Authorization.UserManagementRules.IsRole(CurrentUser.Role, "manager")
            || (Falah.Domain.Authorization.UserManagementRules.IsRole(CurrentUser.Role, "supervisor") && HasPermission(action)));
}

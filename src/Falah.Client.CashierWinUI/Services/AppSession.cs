using Falah.Application.Users;

namespace Falah.Client.CashierWinUI.Services;

/// <summary>Shared, process-wide session state: who is logged in and which shift they are
/// operating under. Deliberately NOT persisted anywhere on this client - restarting the app means
/// logging in again, matching the reference app's own lock-screen-on-launch behavior.</summary>
public sealed class AppSession
{
    public ApiClient Api { get; } = new();
    public UserSummaryDto? CurrentUser { get; set; }
    public string? ShiftId { get; set; }
    public string? ShiftName { get; set; }

    public bool HasPermission(string permission) =>
        CurrentUser is not null && (CurrentUser.Permissions.Contains("all") || CurrentUser.Permissions.Contains(permission));
}

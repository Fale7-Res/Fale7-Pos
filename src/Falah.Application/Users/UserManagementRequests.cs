namespace Falah.Application.Users;

/// <summary>RequestId is minted once per "create user" button press and must be reused unchanged by
/// a retry - the resulting user id is deterministic from it ("user-{RequestId}"), the same
/// idempotency contract as every other create-command in this codebase. Creating a user with
/// Role="owner" is deliberately not exposed here at all - the owner account is provisioned once,
/// outside this API, and no command in this system can mint a second one or reassign the role.</summary>
public sealed record CreateUserRequest(
    string RequestId, string Name, string Role, string InitialPin, IReadOnlyList<string> Permissions, string ActorUserId);

public sealed record CreateUserResult(string UserId, bool WasReplayed);

/// <summary>Active is the only field this changes - a deactivated user's history (orders, movements,
/// approvals they issued) is untouched; they simply can no longer log in (login-by-pin already
/// filters on Active). Idempotent by nature: deactivating an already-inactive user (or reactivating
/// an already-active one) is a harmless no-op, not an error.</summary>
public sealed record SetUserActiveRequest(string UserId, bool Active, string? Reason, string ActorUserId);

/// <summary>Two distinct paths share this one command: CurrentPin set and ActorUserId == UserId is
/// self-service (the actor proves they know their own current PIN); CurrentPin omitted with
/// ActorUserId != UserId is an admin reset (gated on users_manage instead, and never permitted
/// against the owner - only the owner can change their own PIN).</summary>
public sealed record ChangePinRequest(string UserId, string? CurrentPin, string NewPin, string ActorUserId);

/// <summary>A single call can grant and revoke in the same request (e.g. a permissions-editor
/// screen saving several toggled checkboxes at once) - granting is checked against privilege
/// escalation (UserManagementRules.AssertNoPrivilegeEscalation), revoking never is.</summary>
public sealed record UpdateUserPermissionsRequest(
    string UserId, IReadOnlyList<string> Grant, IReadOnlyList<string> Revoke, string ActorUserId);

public sealed record UserDetailsDto(string Id, string Name, string Role, bool Active, IReadOnlyList<string> Permissions);

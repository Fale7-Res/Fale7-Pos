namespace Falah.Application.Users;

public sealed record UserSummaryDto(string Id, string Name, string Role, IReadOnlyList<string> Permissions);

/// <summary>The lock/login screen identifies a user by PIN alone - no separate username entry,
/// matching the reference Falah app exactly.</summary>
public sealed record LoginByPinRequest(string Pin);


/// <summary>What a successful login returns. Identical to UserSummaryDto plus the session token -
/// the only moment that token is ever visible, since the server stores only its hash.</summary>
public sealed record LoginResultDto(
    string Id, string Name, string Role, IReadOnlyList<string> Permissions, string Token);

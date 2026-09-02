namespace Falah.Application.Users;

public sealed record UserSummaryDto(string Id, string Name, string Role, IReadOnlyList<string> Permissions);

/// <summary>The lock/login screen identifies a user by PIN alone - no separate username entry,
/// matching the reference Falah app exactly.</summary>
public sealed record LoginByPinRequest(string Pin);


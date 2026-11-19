namespace Falah.Application.Users;

/// <summary>A machine whose wrong PINs are being counted (login-by-PIN guessing protection).
/// <paramref name="Source"/> is the address the server keys it by: an IPv4 address, an IPv6 /64
/// prefix, or "loopback" for the server's own machine. <paramref name="RefusedForSeconds"/> is 0 when
/// the machine may try again now.</summary>
public sealed record LoginSourceDto(
    string Source,
    int WrongPinsInWindow,
    int WrongPinsToday,
    bool Recognized,
    DateTimeOffset? LastWrongPinUtc,
    int RefusedForSeconds);

/// <summary>Forgets a machine's wrong PINs so it may log in again - a manager's answer to a till
/// stopped by someone mistyping (or guessing) at it.</summary>
public sealed record ClearLoginSourceRequest(string Source);

/// <summary>What a user administrator should know about who can log in. Nothing here changes
/// anything; every item is something the owner decides about.</summary>
/// <param name="UsersWithPublishedPins">Active accounts whose PIN is one of the demo PINs published
/// in this system's code - anyone can log in as them.</param>
/// <param name="LoginSources">Machines with wrong login PINs in the last day, and whether they are
/// refused now.</param>
/// <param name="PinCollisions">Accounts whose PIN somebody else chose as their own in the last 30
/// days (newest first): that PIN is known to the chooser and should be changed.</param>
public sealed record SecurityPostureDto(
    IReadOnlyList<string> UsersWithPublishedPins,
    IReadOnlyList<LoginSourceDto> LoginSources,
    int WrongLoginPinsLast24Hours,
    int WrongApprovalPinsLast24Hours,
    IReadOnlyList<PinCollisionDto> PinCollisions);

public sealed record PinCollisionDto(string AccountId, string AccountName, string ChosenBy, DateTimeOffset AtUtc);

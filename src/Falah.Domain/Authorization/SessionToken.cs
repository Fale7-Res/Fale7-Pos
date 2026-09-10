namespace Falah.Domain.Authorization;

using System.Security.Cryptography;

/// <summary>
/// A bearer token proving which user a request is actually from.
///
/// Before this existed, the server took the acting user's identity from the request body - an
/// <c>actorUserId</c> field the caller simply wrote. Anyone able to reach the API could read the
/// user list, copy the manager's id, and have every permission check pass. The PIN screen in the
/// client was the only thing standing in the way, and a screen guards nothing a caller can skip.
///
/// It also made the audit trail unprovable: a line saying "the manager did this" rested on the
/// caller having typed the manager's id.
///
/// The token is random, never derived from anything about the user, and only its HASH is stored -
/// so the session table is not itself a list of working credentials if it is ever read.
/// </summary>
public static class SessionToken
{
    /// <summary>256 bits. A session token is guessed, not cracked, so the only property that
    /// matters is that guessing is hopeless.</summary>
    private const int TokenSizeBytes = 32;

    public static string Issue() => Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenSizeBytes));

    /// <summary>Plain SHA-256, deliberately not a slow KDF: unlike a PIN this value has full
    /// entropy, so there is nothing for an attacker to brute-force and nothing to slow down.</summary>
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

    /// <summary>How long a session lasts without being used again. A till runs all day, so this is
    /// generous - but it is not unlimited, because a token left in a log or on a shared machine
    /// should stop working on its own.</summary>
    public static readonly TimeSpan IdleLifetime = TimeSpan.FromHours(16);

    public static bool IsExpired(DateTimeOffset lastSeenUtc, DateTimeOffset nowUtc) =>
        nowUtc - lastSeenUtc > IdleLifetime;
}

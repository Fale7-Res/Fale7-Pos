namespace Falah.Domain.Authorization;

using System.Security.Cryptography;
using Falah.Domain.Common;

/// <summary>New in this pass - the reference app has NO PIN hashing anywhere (`user.pin === pin`,
/// a plain string comparison, in both verifyUserCredentials and unlockScreen in lib/store.tsx). PBKDF2
/// with a per-user random salt and a high iteration count is used deliberately BECAUSE a 4-6 digit
/// PIN has very low entropy (at most 10^6 possibilities) - a fast hash (a single SHA-256 pass, say)
/// would let an attacker who steals the database brute-force every user's PIN in seconds; a slow,
/// tunable KDF is what actually protects a low-entropy secret at rest. Built on .NET's own BCL
/// primitive (Rfc2898DeriveBytes) - no external crypto package needed.</summary>
public static class PinHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;

    /// <summary>OWASP's 2023 minimum recommendation for PBKDF2-HMAC-SHA256. Stored per-row (not
    /// hardcoded at verify time) so a future increase never invalidates already-hashed PINs - each
    /// row remembers the iteration count it was actually hashed with.</summary>
    public const int DefaultIterations = 210_000;

    public sealed record HashedPin(string HashBase64, string SaltBase64, int Iterations);

    public static HashedPin Hash(string pin, int iterations = DefaultIterations)
    {
        AssertValidPin(pin);
        return HashWithoutValidation(pin, iterations);
    }

    /// <summary>For UserPinBackfill ONLY - hashes a PIN that was already accepted under whatever
    /// rules were (or weren't) in force when it was originally set, without re-validating it against
    /// today's strength rule. Backfilling an existing plaintext PIN must preserve "this exact PIN
    /// keeps logging this exact user in," never retroactively reject an account for a PIN that was
    /// perfectly valid when it was created. Every NEW or CHANGED PIN from this point on goes through
    /// the validated Hash() above instead.</summary>
    public static HashedPin HashWithoutValidation(string pin, int iterations = DefaultIterations)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, iterations, HashAlgorithmName.SHA256, HashSizeBytes);
        return new HashedPin(Convert.ToBase64String(hash), Convert.ToBase64String(salt), iterations);
    }

    /// <summary>Constant-time comparison (CryptographicOperations.FixedTimeEquals) - a naive ==
    /// would leak how many leading bytes matched through response-timing, defeating the point of
    /// hashing a low-entropy secret in the first place.</summary>
    public static bool Verify(string pin, string hashBase64, string saltBase64, int iterations)
    {
        var salt = Convert.FromBase64String(saltBase64);
        var expected = Convert.FromBase64String(hashBase64);
        var actual = Rfc2898DeriveBytes.Pbkdf2(pin, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    /// <summary>Ported from setEmployeePin's own validation in lib/store.tsx: 4-6 digits, and rejects
    /// an all-repeated-digit PIN like "1111" (trivially guessable, the one PIN-strength rule the
    /// reference app itself already enforces). Deliberately not adding anything beyond this evidenced
    /// rule - no invented complexity policy.</summary>
    public static void AssertValidPin(string pin)
    {
        if (string.IsNullOrEmpty(pin) || pin.Length < 4 || pin.Length > 6 || !pin.All(char.IsAsciiDigit))
            throw new DomainException("رمز PIN يجب أن يكون من 4 إلى 6 أرقام.");
        if (pin.Distinct().Count() == 1)
            throw new DomainException("رمز PIN ضعيف - لا تستخدم رقمًا مكررًا مثل 1111.");
    }
}

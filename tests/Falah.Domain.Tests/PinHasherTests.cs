using Falah.Domain.Authorization;
using Falah.Domain.Common;
using Xunit;

namespace Falah.Domain.Tests;

public class PinHasherTests
{
    [Fact]
    public void A_correct_pin_verifies_against_its_own_hash()
    {
        var hashed = PinHasher.Hash("1234");
        Assert.True(PinHasher.Verify("1234", hashed.HashBase64, hashed.SaltBase64, hashed.Iterations));
    }

    [Fact]
    public void A_wrong_pin_does_not_verify()
    {
        var hashed = PinHasher.Hash("1234");
        Assert.False(PinHasher.Verify("4321", hashed.HashBase64, hashed.SaltBase64, hashed.Iterations));
    }

    [Fact]
    public void The_same_pin_hashed_twice_produces_different_salts_and_hashes()
    {
        var first = PinHasher.Hash("9876");
        var second = PinHasher.Hash("9876");
        Assert.NotEqual(first.SaltBase64, second.SaltBase64);
        Assert.NotEqual(first.HashBase64, second.HashBase64);
        // Both still verify against their OWN salt/hash pair.
        Assert.True(PinHasher.Verify("9876", first.HashBase64, first.SaltBase64, first.Iterations));
        Assert.True(PinHasher.Verify("9876", second.HashBase64, second.SaltBase64, second.Iterations));
    }

    [Fact]
    public void Hashing_rejects_a_pin_that_is_too_short_or_too_long()
    {
        Assert.Throws<DomainException>(() => PinHasher.Hash("123"));
        Assert.Throws<DomainException>(() => PinHasher.Hash("1234567"));
    }

    [Fact]
    public void Hashing_rejects_a_non_numeric_pin()
    {
        Assert.Throws<DomainException>(() => PinHasher.Hash("12ab"));
    }

    [Fact]
    public void Hashing_rejects_an_all_repeated_digit_pin()
    {
        Assert.Throws<DomainException>(() => PinHasher.Hash("1111"));
        Assert.Throws<DomainException>(() => PinHasher.Hash("999999"));
    }

    [Fact]
    public void A_stored_iterations_value_is_honored_at_verify_time_even_if_the_default_changes_later()
    {
        var hashed = PinHasher.Hash("5678", iterations: 50_000);
        Assert.Equal(50_000, hashed.Iterations);
        Assert.True(PinHasher.Verify("5678", hashed.HashBase64, hashed.SaltBase64, 50_000));
    }
}

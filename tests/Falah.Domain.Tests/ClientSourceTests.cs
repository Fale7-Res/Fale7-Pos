namespace Falah.Domain.Tests;

using System.Net;
using Falah.Domain.Authorization;
using Xunit;

/// <summary>How PIN guessing limits tell machines apart (SEC2).</summary>
public class ClientSourceTests
{
    [Theory]
    [InlineData("192.168.1.20", "192.168.1.20")]
    [InlineData("::ffff:192.168.1.20", "192.168.1.20")] // the same machine over a dual-stack socket
    [InlineData("127.0.0.1", "loopback")]
    [InlineData("::1", "loopback")]
    [InlineData("2001:db8:5:7::10", "2001:db8:5:7::/64")]
    [InlineData("2001:db8:5:7:abcd:1234:5678:9abc", "2001:db8:5:7::/64")] // a new address in the same network is the same machine
    [InlineData("fe80::1c2b:3a4d:5e6f:7a8b", "fe80::/64")]
    public void A_machine_is_named_by_its_address_and_an_ipv6_host_by_its_network(string address, string expected) =>
        Assert.Equal(expected, ClientSource.Of(IPAddress.Parse(address)));

    [Fact]
    public void A_request_with_no_address_is_one_unknown_machine() =>
        Assert.Equal(ClientSource.Unknown, ClientSource.Of(null));

    [Fact]
    public void Different_ipv6_networks_are_different_machines() =>
        Assert.NotEqual(ClientSource.Of(IPAddress.Parse("2001:db8:5:7::1")), ClientSource.Of(IPAddress.Parse("2001:db8:5:8::1")));
}

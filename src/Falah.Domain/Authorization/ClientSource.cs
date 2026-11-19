namespace Falah.Domain.Authorization;

using System.Net;
using System.Net.Sockets;

/// <summary>
/// The machine a request came from, as PIN guessing limits count it.
///
/// An IPv4 address is one machine. An IPv6 host can give itself a new address in its /64 network at
/// will, so all of a /64 counts as one machine - otherwise rotating addresses would hand out fresh
/// allowances. Both loopback addresses are the server's own machine. Only the connection's own
/// address is used, never a forwarded-for header a caller could write themselves.
/// </summary>
public static class ClientSource
{
    public const string Unknown = "unknown";
    public const string Loopback = "loopback";

    public static string Of(IPAddress? address)
    {
        if (address is null) return Unknown;
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address)) return Loopback;
        if (address.AddressFamily != AddressFamily.InterNetworkV6) return address.ToString();

        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return $"{new IPAddress(bytes)}/64";
    }
}

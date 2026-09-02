namespace Falah.Domain.Customers;

using System.Text.RegularExpressions;

public sealed record Customer(string Id, string Phone, string? Name);

public sealed record CustomerAddress(
    string Id, string CustomerId, string? Area, string? Street, string? Building,
    string? Floor, string? Apartment, string? Landmark, string? ServiceId);

/// <summary>A customer's identity is their phone number, compared with formatting stripped out -
/// ported from normalizePhone in lib/store.tsx, so "010-1234-5678" and "01012345678" resolve to
/// the same customer.</summary>
public static class CustomerRules
{
    public static string NormalizePhone(string phone) => Regex.Replace(phone, @"\D", "");

    public static Customer? FindByPhone(IEnumerable<Customer> customers, string phone)
    {
        var normalized = NormalizePhone(phone);
        return customers.FirstOrDefault(c => NormalizePhone(c.Phone) == normalized);
    }
}

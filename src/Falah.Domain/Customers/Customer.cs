namespace Falah.Domain.Customers;

using System.Text.RegularExpressions;
using Falah.Domain.Common;

public sealed record Customer(string Id, string Phone, string? Name);

/// <summary>A saved address for quick reuse on a repeat call - NOT the source of truth for any
/// historical order (an order freezes its own delivery snapshot at checkout time; editing or
/// removing a saved address here can never reach back and change it). CreatedAtUtc/LastUsedAtUtc
/// default to `default` so existing positional construction (Domain tests predating this) keeps
/// compiling unchanged.</summary>
public sealed record CustomerAddress(
    string Id, string CustomerId, string? Area, string? Street, string? Building,
    string? Floor, string? Apartment, string? Landmark, string? ServiceId, string? ServiceName = null,
    DateTimeOffset CreatedAtUtc = default, DateTimeOffset LastUsedAtUtc = default);

/// <summary>Ported from normalizePhone/findCustomerByPhone/upsertCustomer's own matching logic in
/// lib/store.tsx. A customer's identity is their phone number, compared with formatting stripped
/// out, so "010-1234-5678" and "01012345678" resolve to the same customer - never merged or looked
/// up by name.</summary>
public static class CustomerRules
{
    /// <summary>QA-02 (audit 2026-11-23): Arabic-Indic and Persian digits are converted first, and only
    /// 0-9 are kept. `\D` alone kept Arabic-Indic digits (.NET counts them as digits), so a number pasted
    /// as "٠١٠…" became a second customer and the lookup missed the first one's saved addresses.</summary>
    public static string NormalizePhone(string phone) => Regex.Replace(InputRules.NormalizeDigits(phone ?? ""), "[^0-9]", "");

    /// <summary>The longest real number (E.164 allows 15 digits) plus room for a prefix.</summary>
    public const int MaxPhoneDigits = 20;

    public static void AssertPhone(string phone)
    {
        var normalized = NormalizePhone(phone);
        if (string.IsNullOrEmpty(normalized)) throw new DomainException("رقم هاتف العميل مطلوب.");
        if (normalized.Length > MaxPhoneDigits) throw new DomainException($"رقم الهاتف أطول من المسموح (الحد {MaxPhoneDigits} رقم).");
    }

    public static Customer? FindByPhone(IEnumerable<Customer> customers, string phone)
    {
        var normalized = NormalizePhone(phone);
        return customers.FirstOrDefault(c => NormalizePhone(c.Phone) == normalized);
    }

    /// <summary>An address is identity-matched by its geographic content only (area/street trimmed;
    /// building/floor/apartment compared as-is, empty-coalesced) - landmark and delivery-service
    /// selection are never part of the match key, so saving the same physical address again just
    /// refreshes those two fields on the SAME row rather than ever creating a second one.</summary>
    public static bool AddressMatches(CustomerAddress existing, string? area, string? street, string? building, string? floor, string? apartment) =>
        (existing.Area ?? "").Trim() == (area ?? "").Trim()
        && (existing.Street ?? "").Trim() == (street ?? "").Trim()
        && (existing.Building ?? "") == (building ?? "")
        && (existing.Floor ?? "") == (floor ?? "")
        && (existing.Apartment ?? "") == (apartment ?? "");

    public static CustomerAddress? FindMatchingAddress(
        IEnumerable<CustomerAddress> addresses, string? area, string? street, string? building, string? floor, string? apartment) =>
        addresses.FirstOrDefault(a => AddressMatches(a, area, street, building, floor, apartment));

    /// <summary>Ported from upsertCustomer's own guard (`if (!normalized || !name.trim()) return;`).
    /// The reference app's version is a silent client-side no-op because its one call site always
    /// pre-validates via firstMissingDeliveryField first - a real network API has no such guarantee,
    /// so this throws instead of silently doing nothing, which would be a confusing contract for a
    /// remote caller (e.g. Call Center) to debug.</summary>
    public static void AssertUpsertInput(string phone, string name)
    {
        AssertPhone(phone);
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("اسم العميل مطلوب.");
        InputRules.AssertMaxLength(name.Trim(), InputRules.MaxNameLength, "اسم العميل");
    }
}

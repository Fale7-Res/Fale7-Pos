namespace Falah.Application.Customers;

/// <summary>Ported from upsertCustomer in lib/store.tsx. No RequestId/idempotency-key - unlike a
/// financial/stock movement, this is a plain content-addressed upsert (phone is the identity,
/// address matched by geographic content), idempotent by construction the same way saveRecipe is:
/// re-applying the exact same save converges on the exact same customer+address rows, never a
/// duplicate. Area/Street both present is what triggers an address upsert; omit both to save just
/// the identity (e.g. a Call Center operator who has the phone/name but not yet the address).</summary>
public sealed record UpsertCustomerRequest(
    string Phone, string Name, string? Area, string? Street, string? Building, string? Floor,
    string? Apartment, string? Landmark, string? ServiceId, string? ServiceName);

public sealed record CustomerAddressDto(
    string Id, string Area, string Street, string? Building, string? Floor, string? Apartment,
    string? Landmark, string? ServiceId, string? ServiceName, DateTimeOffset CreatedAtUtc, DateTimeOffset LastUsedAtUtc);

public sealed record CustomerDto(
    string Id, string Phone, string Name, IReadOnlyList<CustomerAddressDto> Addresses, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);

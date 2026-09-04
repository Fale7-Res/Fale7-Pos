namespace Falah.Infrastructure.Persistence.Entities;

/// <summary>Ported from Customer in lib/types.ts. Phone (normalized, digits only) is the identity -
/// a customer is never looked up or merged by name. No CreatedBy/audit trail, matching the reference
/// app exactly: saving/updating a customer is normal order-taking, not a privileged action.</summary>
public class CustomerEntity
{
    public string Id { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public List<CustomerAddressEntity> Addresses { get; set; } = new();
}

/// <summary>Ported from CustomerAddress in lib/types.ts. A saved address for quick reuse on a
/// repeat call - never the source of truth for a historical order's own frozen delivery snapshot.
/// Matched by geographic content only (see CustomerRules.AddressMatches); Landmark/ServiceId/
/// ServiceName are refreshed on the matched row, never part of the match key itself.</summary>
public class CustomerAddressEntity
{
    public string Id { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public string Area { get; set; } = default!;
    public string Street { get; set; } = default!;
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? Apartment { get; set; }
    public string? Landmark { get; set; }
    /// <summary>Delivery service last used for this address - offered as a default, never forced.</summary>
    public string? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastUsedAtUtc { get; set; }
}

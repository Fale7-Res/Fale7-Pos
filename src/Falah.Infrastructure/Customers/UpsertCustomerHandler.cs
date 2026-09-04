namespace Falah.Infrastructure.Customers;

using Falah.Application.Customers;
using Falah.Domain.Customers;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Ported from upsertCustomer in lib/store.tsx. Two cashier stations (or a Cashier and Call Center)
/// may look up or save the same phone number independently; both read/write through this same
/// table, so whichever persists second just adds/refreshes one address, never colliding with or
/// overwriting the other's. No permission gate - saving a customer's details is normal order-taking,
/// exactly like the reference app (unlike inventory_manage/menu_manage-gated management commands).
/// </summary>
public sealed class UpsertCustomerHandler
{
    private readonly FalahDbContext _db;
    public UpsertCustomerHandler(FalahDbContext db) => _db = db;

    public async Task<CustomerDto> HandleAsync(UpsertCustomerRequest request, CancellationToken ct = default)
    {
        CustomerRules.AssertUpsertInput(request.Phone, request.Name);
        var normalizedPhone = CustomerRules.NormalizePhone(request.Phone);
        var trimmedName = request.Name.Trim();
        var now = DateTimeOffset.UtcNow;

        // Phase 1: find-or-create the customer row. The database's own unique index on Phone (not
        // just this check) is what actually prevents two concurrent first-time upserts for the same
        // phone from creating two customers - a race here falls back to whichever row won.
        var customerId = await _db.Customers.AsNoTracking()
            .Where(c => c.Phone == normalizedPhone).Select(c => c.Id).FirstOrDefaultAsync(ct);
        if (customerId is null)
        {
            customerId = $"cust-{Guid.NewGuid():N}";
            _db.Customers.Add(new CustomerEntity { Id = customerId, Phone = normalizedPhone, Name = trimmedName, CreatedAtUtc = now, UpdatedAtUtc = now });
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                customerId = await _db.Customers.AsNoTracking().Where(c => c.Phone == normalizedPhone).Select(c => c.Id).FirstAsync(ct);
            }
        }

        // Phase 2: update name + merge address, under a lock scoped to this customer's own address
        // rows - the only place a genuine race (two concurrent upserts for the SAME existing
        // customer with the SAME new address content) could otherwise create a duplicate.
        var hasAddress = !string.IsNullOrWhiteSpace(request.Area) && !string.IsNullOrWhiteSpace(request.Street);
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var customer = await _db.Customers
            .FromSqlRaw("SELECT * FROM Customers WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", customerId)
            .FirstAsync(ct);
        customer.Name = trimmedName;
        customer.UpdatedAtUtc = now;

        if (hasAddress)
        {
            var addressEntities = await _db.CustomerAddresses
                .FromSqlRaw("SELECT * FROM CustomerAddresses WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE CustomerId = {0}", customerId)
                .ToListAsync(ct);
            var domainAddresses = addressEntities.Select(ToDomain).ToList();
            var matched = CustomerRules.FindMatchingAddress(domainAddresses, request.Area, request.Street, request.Building, request.Floor, request.Apartment);

            if (matched is not null)
            {
                var matchedEntity = addressEntities.First(a => a.Id == matched.Id);
                matchedEntity.Building = request.Building;
                matchedEntity.Floor = request.Floor;
                matchedEntity.Apartment = request.Apartment;
                matchedEntity.Landmark = request.Landmark;
                matchedEntity.ServiceId = request.ServiceId;
                matchedEntity.ServiceName = request.ServiceName;
                matchedEntity.LastUsedAtUtc = now;
            }
            else
            {
                _db.CustomerAddresses.Add(new CustomerAddressEntity
                {
                    Id = $"addr-{Guid.NewGuid():N}", CustomerId = customerId, Area = request.Area!.Trim(), Street = request.Street!.Trim(),
                    Building = request.Building, Floor = request.Floor, Apartment = request.Apartment, Landmark = request.Landmark,
                    ServiceId = request.ServiceId, ServiceName = request.ServiceName, CreatedAtUtc = now, LastUsedAtUtc = now,
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var finalAddresses = await _db.CustomerAddresses.AsNoTracking().Where(a => a.CustomerId == customerId).ToListAsync(ct);
        return ToDto(customer, finalAddresses);
    }

    private static CustomerAddress ToDomain(CustomerAddressEntity e) =>
        new(e.Id, e.CustomerId, e.Area, e.Street, e.Building, e.Floor, e.Apartment, e.Landmark, e.ServiceId, e.ServiceName, e.CreatedAtUtc, e.LastUsedAtUtc);

    internal static CustomerDto ToDto(CustomerEntity customer, IReadOnlyList<CustomerAddressEntity> addresses) => new(
        customer.Id, customer.Phone, customer.Name,
        addresses.Select(a => new CustomerAddressDto(a.Id, a.Area, a.Street, a.Building, a.Floor, a.Apartment,
            a.Landmark, a.ServiceId, a.ServiceName, a.CreatedAtUtc, a.LastUsedAtUtc)).ToList(),
        customer.CreatedAtUtc, customer.UpdatedAtUtc);
}

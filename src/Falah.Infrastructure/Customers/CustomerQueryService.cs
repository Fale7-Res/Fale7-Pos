namespace Falah.Infrastructure.Customers;

using Falah.Application.Customers;
using Falah.Domain.Customers;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Ported from findCustomerByPhone - read-only, never creates or modifies anything, so any
/// station (Cashier or Call Center) can call it freely while taking a call, the moment enough
/// digits are typed.</summary>
public sealed class CustomerQueryService
{
    private readonly FalahDbContext _db;
    public CustomerQueryService(FalahDbContext db) => _db = db;

    /// <param name="includeHidden">CUS-0002: only the owner sees a customer they deleted.</param>
    public async Task<CustomerDto?> FindByPhoneAsync(string phone, CancellationToken ct = default, bool includeHidden = false)
    {
        var normalized = CustomerRules.NormalizePhone(phone);
        if (normalized.Length == 0) return null;

        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Phone == normalized, ct);
        if (customer is null || (customer.HiddenAtUtc is not null && !includeHidden)) return null;

        var addresses = await _db.CustomerAddresses.AsNoTracking()
            .Where(a => a.CustomerId == customer.Id).OrderByDescending(a => a.LastUsedAtUtc).ToListAsync(ct);
        return UpsertCustomerHandler.ToDto(customer, addresses);
    }

    public async Task<CustomerDto?> GetByIdAsync(string customerId, CancellationToken ct = default, bool includeHidden = false)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId, ct);
        if (customer is null || (customer.HiddenAtUtc is not null && !includeHidden)) return null;

        var addresses = await _db.CustomerAddresses.AsNoTracking()
            .Where(a => a.CustomerId == customerId).OrderByDescending(a => a.LastUsedAtUtc).ToListAsync(ct);
        return UpsertCustomerHandler.ToDto(customer, addresses);
    }
}

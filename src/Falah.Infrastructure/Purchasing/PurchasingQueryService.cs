namespace Falah.Infrastructure.Purchasing;

using Falah.Application.Purchasing;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>Read-only projections over Supplier/Purchase/SupplierLedgerEntry. Every balance figure
/// (TotalInvoiced/TotalPaid/Outstanding) is derived from the ledger on each read, exactly like the
/// reference app's own supplierBalance() - never stored, so it can never drift from the ledger that
/// is its actual source of truth.</summary>
public sealed class PurchasingQueryService
{
    private readonly FalahDbContext _db;
    public PurchasingQueryService(FalahDbContext db) => _db = db;

    public async Task<List<SupplierDto>> ListSuppliersAsync(CancellationToken ct = default)
    {
        var suppliers = await _db.Suppliers.AsNoTracking().ToListAsync(ct);
        var ledger = await _db.SupplierLedgerEntries.AsNoTracking().ToListAsync(ct);
        return suppliers.Select(s => ToDto(s, ledger.Where(e => e.SupplierId == s.Id))).ToList();
    }

    public async Task<SupplierDto?> GetSupplierAsync(string id, CancellationToken ct = default)
    {
        var supplier = await _db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (supplier is null) return null;
        var ledger = await _db.SupplierLedgerEntries.AsNoTracking().Where(e => e.SupplierId == id).ToListAsync(ct);
        return ToDto(supplier, ledger);
    }

    public async Task<List<SupplierLedgerEntryDto>> GetSupplierLedgerAsync(string supplierId, CancellationToken ct = default) =>
        (await _db.SupplierLedgerEntries.AsNoTracking().Where(e => e.SupplierId == supplierId)
            .OrderBy(e => e.CreatedAtUtc).ToListAsync(ct))
            .Select(e => new SupplierLedgerEntryDto(e.Id, e.SupplierId, e.Type, e.DebitPiastres, e.CreditPiastres, e.PurchaseId, e.Reason, e.CreatedAtUtc, e.CreatedBy))
            .ToList();

    public async Task<List<PurchaseDto>> ListPurchasesAsync(CancellationToken ct = default) =>
        (await _db.Purchases.AsNoTracking().Include(p => p.Lines).Include(p => p.Payments)
            .OrderByDescending(p => p.CreatedAtUtc).ToListAsync(ct))
            .Select(p => ReceivePurchaseHandler.ToDto(p, wasReplayed: false)).ToList();

    public async Task<PurchaseDto?> GetPurchaseAsync(string id, CancellationToken ct = default)
    {
        var purchase = await _db.Purchases.AsNoTracking().Include(p => p.Lines).Include(p => p.Payments)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        return purchase is null ? null : ReceivePurchaseHandler.ToDto(purchase, wasReplayed: false);
    }

    private static SupplierDto ToDto(Falah.Infrastructure.Persistence.Entities.SupplierEntity s, IEnumerable<Falah.Infrastructure.Persistence.Entities.SupplierLedgerEntryEntity> ledger)
    {
        var entries = ledger.ToList();
        var totalInvoiced = entries.Where(e => e.Type == "purchase_invoice").Sum(e => e.DebitPiastres);
        var totalPaid = entries.Where(e => e.Type == "supplier_payment").Sum(e => e.CreditPiastres);
        var outstanding = entries.Sum(e => e.DebitPiastres - e.CreditPiastres);
        return new SupplierDto(s.Id, s.Name, s.Phone, s.Address, s.Category, s.Active, s.Notes, totalInvoiced, totalPaid, outstanding);
    }
}

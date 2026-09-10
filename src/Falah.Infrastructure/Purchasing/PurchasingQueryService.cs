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

    /// <summary>Every invoice that still owes something, aged as of today's business date.
    ///
    /// Built from PurchaseEntity.RemainingAmountPiastres rather than by re-deriving each invoice's
    /// balance from the supplier ledger: the ledger is the source of a SUPPLIER's outstanding total,
    /// but it does not attribute payments to invoices, so it cannot say which invoice is late. The
    /// two agree because every command that changes a remaining balance recomputes it from the
    /// ledger at the moment of the write (see PurchaseEntity's own doc comment).</summary>
    public async Task<PayablesAgingDto> GetPayablesAgingAsync(CancellationToken ct = default)
    {
        var asOf = Falah.Domain.Common.BusinessClock.Today();
        var open = await _db.Purchases.AsNoTracking()
            .Where(p => p.RemainingAmountPiastres > 0 && p.Status != "cancelled")
            .OrderBy(p => p.CreatedAtUtc)
            .ToListAsync(ct);

        var supplierNames = await _db.Suppliers.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var invoices = open.Select(p =>
        {
            var receivedOn = Falah.Domain.Common.BusinessClock.BusinessDate(p.CreatedAtUtc);
            var bucket = Falah.Domain.Purchasing.PayablesAging.Classify(p.DueDate, asOf);
            return new PayableDto(
                p.Id, p.SupplierId, supplierNames.GetValueOrDefault(p.SupplierId, "مورد محذوف"), p.InvoiceNumber,
                p.RemainingAmountPiastres, receivedOn.ToString("yyyy-MM-dd"), p.DueDate?.ToString("yyyy-MM-dd"),
                Falah.Domain.Purchasing.PayablesAging.AgeInDays(receivedOn, asOf),
                Falah.Domain.Purchasing.PayablesAging.DaysOverdue(p.DueDate, asOf),
                bucket.ToString(), Falah.Domain.Purchasing.PayablesAging.Label(bucket));
        })
        // Worst first: the oldest overdue invoice is the one somebody has to deal with today.
        .OrderByDescending(i => i.DaysOverdue ?? -1).ThenByDescending(i => i.AgeInDays)
        .ToList();

        // Every bucket is emitted, including the empty ones, so the reader can see that a bucket is
        // empty rather than wonder whether it was simply left out.
        var buckets = Enum.GetValues<Falah.Domain.Purchasing.PayablesAging.Bucket>()
            .Select(b => new PayablesBucketDto(
                b.ToString(), Falah.Domain.Purchasing.PayablesAging.Label(b),
                invoices.Count(i => i.Bucket == b.ToString()),
                invoices.Where(i => i.Bucket == b.ToString()).Sum(i => i.RemainingPiastres)))
            .ToList();

        var overdue = invoices.Where(i => i.DaysOverdue is > 0).ToList();
        var unscheduled = invoices.Where(i => i.DueDate is null).ToList();

        return new PayablesAgingDto(
            asOf.ToString("yyyy-MM-dd"),
            invoices.Sum(i => i.RemainingPiastres),
            overdue.Sum(i => i.RemainingPiastres), overdue.Count,
            unscheduled.Sum(i => i.RemainingPiastres), unscheduled.Count,
            buckets, invoices);
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

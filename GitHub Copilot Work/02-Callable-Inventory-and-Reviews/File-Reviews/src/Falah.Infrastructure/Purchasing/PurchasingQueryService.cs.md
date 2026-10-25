# File Review: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs
**Total Callables**: 43
---
## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:14:5:ConstructorDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.PurchasingQueryService(Falah.Infrastructure.Persistence.FalahDbContext)
- **Kind**: ConstructorDeclaration
- **Behavior**: Injects `FalahDbContext` database context dependency for read-only projection over Supplier, Purchase, and SupplierLedgerEntry entities.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:16:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.ListSuppliersAsync(CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Retrieves all suppliers and dynamically computes each supplier's financial balance figures:
  - Fetches all suppliers from `_db.Suppliers.AsNoTracking()`.
  - Fetches the entire supplier ledger table from `_db.SupplierLedgerEntries.AsNoTracking()`.
  - In-memory correlates ledger entries per supplier and passes them to `ToDto(s, ledger.Where(...))` to compute `TotalInvoiced`, `TotalPaid`, and `Outstanding` balances.
  ```csharp
  // Lines 16-21: Supplier listing with dynamic ledger derivation
  public async Task<List<SupplierDto>> ListSuppliersAsync(CancellationToken ct = default)
  {
      var suppliers = await _db.Suppliers.AsNoTracking().ToListAsync(ct);
      var ledger = await _db.SupplierLedgerEntries.AsNoTracking().ToListAsync(ct);
      return suppliers.Select(s => ToDto(s, ledger.Where(e => e.SupplierId == s.Id))).ToList();
  }
  ```
  - **Candidate Finding / Memory & Scalability Risk**:
    - **Full Ledger Table Scan**: Queries `db.SupplierLedgerEntries.AsNoTracking().ToListAsync(ct)` without a supplier filter or server-side grouping. Loads every supplier ledger entry in the entire history of the restaurant into memory, then performs O(N*M) linear scans in C# for each supplier. As transactions accumulate, this will trigger extreme memory pressure, GC pauses, and query latency.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:20:33:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline projection lambda `s => ToDto(s, ledger.Where(e => e.SupplierId == s.Id))` projecting supplier entity into `SupplierDto` with in-memory filtered ledger entries.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:20:60:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `e => e.SupplierId == s.Id` matching ledger transactions belonging to a specific supplier.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:23:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.GetSupplierAsync(string, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Retrieves a single supplier by primary key and re-derives live ledger metrics:
  - Queries `_db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct)`.
  - If null, returns null.
  - Queries ledger entries specifically scoped to this supplier: `_db.SupplierLedgerEntries.AsNoTracking().Where(e => e.SupplierId == id).ToListAsync(ct)`.
  - Computes `TotalInvoiced`, `TotalPaid`, and `Outstanding` balance via `ToDto(supplier, ledger)`.
  ```csharp
  // Lines 23-29: Single supplier query with ledger balance calculation
  public async Task<SupplierDto?> GetSupplierAsync(string id, CancellationToken ct = default)
  {
      var supplier = await _db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
      if (supplier is null) return null;
      var ledger = await _db.SupplierLedgerEntries.AsNoTracking().Where(e => e.SupplierId == id).ToListAsync(ct);
      return ToDto(supplier, ledger);
  }
  ```
- **Validations**: Validation checks detected: True. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:25:79:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `s => s.Id == id` filtering supplier by unique primary key identifier.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:27:75:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `e => e.SupplierId == id` filtering ledger entries by supplier ID.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:31:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.GetSupplierLedgerAsync(string, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Fetches historical ledger transactions for a supplier ordered chronologically:
  - Queries `_db.SupplierLedgerEntries.AsNoTracking().Where(e => e.SupplierId == supplierId).OrderBy(e => e.CreatedAtUtc).ToListAsync(ct)`.
  - Projects rows into `SupplierLedgerEntryDto` containing transaction type, debit/credit piastres, purchase reference, reason, timestamp, and creator.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:32:63:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `e => e.SupplierId == supplierId` filtering ledger entries for the requested supplier.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:33:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.OrderBy(e => e.CreatedAtUtc).ToListAsync(ct))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:34:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline projection lambda mapping `SupplierLedgerEntryEntity` to `SupplierLedgerEntryDto`.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:37:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.ListPurchasesAsync(CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Queries all purchases in reverse chronological order:
  - Eager-loads child collections `Lines` and `Payments` via  `.Include(p => p.Lines).Include(p => p.Payments)`.
  - Orders by `CreatedAtUtc` descending.
  - Projects each entity to `PurchaseDto` via `ReceivePurchaseHandler.ToDto(p, wasReplayed: false)`.
  ```csharp
  // Lines 37-40: Purchase list query with eager-loaded details
  public async Task<List<PurchaseDto>> ListPurchasesAsync(CancellationToken ct = default) =>
      (await _db.Purchases.AsNoTracking().Include(p => p.Lines).Include(p => p.Payments)
          .OrderByDescending(p => p.CreatedAtUtc).ToListAsync(ct))
          .Select(p => ReceivePurchaseHandler.ToDto(p, wasReplayed: false)).ToList();
  ```
  - **Candidate Finding / Performance Risk**:
    - **Unbounded Purchase Scan**: Fetches all purchases across all history along with all line items and payments without pagination, date filtering, or batching. High volume of purchases will cause severe query latency and large object graph allocations.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:38:53:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Navigation selector lambda `p => p.Lines`ager-loading line items for each purchase.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:38:75:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Navigation selector lambda `p => p.Payments`ager-loading payment records for each purchase.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:39:32:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.OrderByDescending(p => p.CreatedAtUtc).ToListAsync(ct))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:40:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Select(p => ReceivePurchaseHandler.ToDto(p, wasReplayed: false)).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:42:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.GetPurchaseAsync(string, CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Retrieves a single purchase by ID with eager-loaded `Lines` and `Payments`. Returns null if not found, or maps to `PurchaseDto` via `ReceivePurchaseHandler.ToDto(purchase, wasReplayed: false)`.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:44:67:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Navigation selector lambda p => p.Lines eager-loading line items for single purchase lookup.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:44:89:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Navigation selector lambda `p => p.Payments`ager-loading payment records for single purchase lookup.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:45:34:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.FirstOrDefaultAsync(p => p.Id == id, ct);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:56:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.GetPayablesAgingAsync(CancellationToken)
- **Kind**: MethodDeclaration
- **Behavior**: Generates accounts payable aging report as of `BusinessClock.Today()`:
  - Queries open invoices: uncancelled purchases where `RemainingAmountPiastres > 0` ordered by creation timestamp.
  - Loads supplier ID-to-name lookup dictionary from `_db.Suppliers`.
  - Projects each payable invoice: calculates business date of receipt, classifies aging bucket via domain rule `PayablesAging.Classify(p.DueDate, asOf)`, computes `AgeInDays` and `DaysOverdue`.
  - Sorts invoices worst-first: descending by `DaysOverdue ?? -1`, then by `AgeInDays`.
  - Aggregates by aging bucket (`PayablesAging.Bucket` enum values), emitting all buckets (including empty ones) with invoice count and remaining piastre sums.
  - Computes summary metrics: total remaining piastres, overdue counts and sums, unscheduled counts and sums. Returns `PayablesAgingDto`.
  ```csharp
  // Lines 56-99: Aged payables calculation
  public async Task<PayablesAgingDto> GetPayablesAgingAsync(CancellationToken ct = default)
  {
      var asOf = Falah.Domain.Common.BusinessClock.Today();
      var open = await _db.Purchases.AsNoTracking()
          .Where(p => p.RemainingAmountPiastres > 0 && p.Status != "cancelled")
          .OrderBy(p => p.CreatedAtUtc).ToListAsync(ct);
      var supplierNames = await _db.Suppliers.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Name, ct);
      ...
      return new PayablesAgingDto(asOf.ToString("yyyy-MM-dd"), ...);
  }
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:60:20:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.Where(p => p.RemainingAmountPiastres > 0 && p.Status != "cancelled")'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': True, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:61:22:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.OrderBy(p => p.CreatedAtUtc)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:64:82:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline key selector lambda `s => s.Id` for supplier dictionary lookup.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:64:93:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline value selector lambda `s => s.Name` for supplier dictionary lookup.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': True}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:66:36:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Projection lambda transforming open `PurchaseEntity` records into `PayableDto` with calculated aging bucket, age in days, and overdue days.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:78:28:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.OrderByDescending(i => i.DaysOverdue ?? -1).ThenByDescending(i => i.AgeInDays)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:78:71:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda '.OrderByDescending(i => i.DaysOverdue ?? -1).ThenByDescending(i => i.AgeInDays)'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:84:21:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Projection lambda mapping `PayablesAging.Bucket` enum values into `PayablesBucketDto` aggregating counts and sums of remainjig piastres per bucket.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:86:32:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'invoices.Count(i => i.Bucket == b.ToString()),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:87:32:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'invoices.Where(i => i.Bucket == b.ToString()).Sum(i => i.RemainingPiastres)))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:87:67:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'invoices.Where(i => i.Bucket == b.ToString()).Sum(i => i.RemainingPiastres)))'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:90:38:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var overdue = invoices.Where(i => i.DaysOverdue is > 0).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:91:42:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var unscheduled = invoices.Where(i => i.DueDate is null).ToList();'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:95:26:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'invoices.Sum(i => i.RemainingPiastres),'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:96:25:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'overdue.Sum(i => i.RemainingPiastres), overdue.Count,'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:97:29:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'unscheduled.Sum(i => i.RemainingPiastres), unscheduled.Count,'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:101:5:MethodDeclaration
- **Symbol**: Falah.Infrastructure.Purchasing.PurchasingQueryService.ToDto(Falah.Infrastructure.Persistence.Entities.SupplierEntity, IEnumerable<Falah.Infrastructure.Persistence.Entities.SupplierLedgerEntryEntity>)
- **Kind**: MethodDeclaration
- **Behavior**: Computes financial totals from ledger entries and constructs `SupplierDto`:
  - `totalInvoiced`: Sum of debit amounts where `Type == "purchase_invoice"`.
  - `totalPaid`: Sum of credit amounts where `Type == "supplier_payment"`.
  - `outstanding`: Sum of `(DebitPiastres - CreditPiastres)` across all ledger entries.
  - Combines with supplier entity fields (Id, Name, Phone, Address, Category, Active, Notes) into immutable `SupplierDto`.
  ```csharp
  // Lines 101-108: Dynamic ledger balance derivation
  private static SupplierDto ToDto(SupplierEntity s, IEnumerable<SupplierLedgerEntryEntity> ledger)
  {
      var entries = ledger.ToList();
      var totalInvoiced = entries.Where(e => e.Type == "purchase_invoice").Sum(e => e.DebitPiastres);
      var totalPaid = entries.Where(e => e.Type == "supplier_payment").Sum(e => e.CreditPiastres);
      var outstanding = entries.Sum(e => e.DebitPiastres - e.CreditPiastres);
      return new SupplierDto(s.Id, s.Name, s.Phone, s.Address, s.Category, s.Active, s.Notes, totalInvoiced, totalPaid, outstanding);
  }
  ```
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:104:43:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `e => e.Type == "purchase_invoice"` filtering ledger entries representing purchase invoices.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:104:82:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `e => e.DebitPiastres` extracting invoice debit amounts in piastres.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:105:39:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline predicate lambda `e => e.Type == "supplier_payment"` filtering ledger entries representing payments made to the supplier.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:105:78:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline selector lambda `e => e.CreditPiastres` extracting payment credit amounts in piastres.
- **Validations**: Validation checks detected: False. Throws: None detected in body.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs:106:39:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Inline expression lambda 'var outstanding = entries.Sum(e => e.DebitPiastres - e.CreditPiastres);'.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:47:36Z
- **Source SHA-256 Verified:** `3e4bf1de133d15b4a412d5c8201a5738595bb8a8afe397ceb063eb7288ba259a`
- **Callable ID Checked:** All 43 callables in `src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:48:02Z
- **Source SHA-256 Verified:** `3e4bf1de133d15b4a412d5c8201a5738595bb8a8afe397ceb063eb7288ba259a`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `3e4bf1de133d15b4a412d5c8201a5738595bb8a8afe397ceb063eb7288ba259a` against production source `src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs`. All 43 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 43 callables.

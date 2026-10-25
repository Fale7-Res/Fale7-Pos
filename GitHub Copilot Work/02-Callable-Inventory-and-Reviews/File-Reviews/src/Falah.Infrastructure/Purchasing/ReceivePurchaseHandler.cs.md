# Deep Per-Callable Source Audit: `ReceivePurchaseHandler.cs`

- **File Path:** [`src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs)
- **Role:** Inbound Purchase Invoice Receipt, Stock Influx & Immediate Supplier Settlement Orchestrator
- **Author:** Financial & Database Integrity Specialist
- **Audit Date:** 2026-10-25T00:40:00Z
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Total In-Scope Callables:** 21 (1 Constructor, 3 Methods, 17 Lambdas)

---

## Callable Inventory & Detailed Behavioral Audit

### 1. `ReceivePurchaseHandler` Constructor
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:29:5:ConstructorDeclaration`
- **Signature:** `public ReceivePurchaseHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)`
- **Line Span:** Lines 29–34
- **Actual Behavior & Inputs:**
  Receives `FalahDbContext`, `IApprovalRegistry`, and `IAuditLog` via dependency injection and sets private readonly fields.
- **Callers, Dependencies & Side Effects:**
  Invoked by DI container when activating purchasing endpoint handlers.
- **Financial Integrity Assessment:**
  Ensures manager approval rules and immutable audit events are integrated into purchasing operations.
- **Classification:** Non-trivial infrastructure wiring.

---

### 2. `HandleAsync`
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:36:5:MethodDeclaration`
- **Signature:** `public async Task<PurchaseDto> HandleAsync(ReceivePurchaseRequest request, CancellationToken ct = default)`
- **Line Span:** Lines 36–229
- **Actual Behavior & Inputs:**
  Receives `ReceivePurchaseRequest` containing `RequestId`, `ActorUserId`, `SupplierId`, `InvoiceNumber`, `Lines`, `PaidPiastres`, `PaymentMethod`, `Notes`, `ManagerApprovalId`, `ManagerApproverUserId`.
  Execution pipeline:
  1. Computes deterministic `purchaseId = $"pur-{request.RequestId}"`. Checks fast idempotent replay via `FindPurchaseAsync`.
  2. Resolves actor and authorizes command via `AuthorizationPolicy.AuthorizeCommand` (`purchasing_manage`).
  3. Validates supplier existence in `_db.Suppliers`.
  4. Validates line items: projects line totals via `PurchasingRules.PurchaseLineTotal`, asserts all `InventoryItemId`s exist in `_db.InventoryItems`.
  5. Computes total invoice cost and validates payment amount via `PurchasingRules.AssertPurchasePayment`.
  6. **Vulnerability FIN-011:** If `paid.IsPositive`, determines payment account and calls `TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync` and `FinancialRules.AssertProtectedSourceBalance` **BEFORE** starting a transaction!
  7. Reads historical unit cost for each item to compute price delta for recipe recalculation.
  8. Requires open business day via `BusinessDayGate.RequireOpenAsync`.
  9. Starts database transaction (`BeginTransactionAsync`).
  10. Starts financial journal entry: `journal.TryStart($"purchase-{purchaseId}", "purchase")`.
  11. If `paid.IsPositive`, posts cash outflow `TreasuryMovement` (`purchase_payment`) from `accountId`.
  12. Inserts `PurchaseEntity` with child `Lines` and `Payments`.
  13. For each line, creates inbound `StockMovementEntity` (`Type = PurchaseReceipt`, `Direction = In`).
  14. Inserts `SupplierLedgerEntryEntity` for total invoice credit, and if paid, another entry for payment debit.
  15. Appends `AuditEvent` for purchase receipt.
  16. Completes journal entry, saves changes, commits transaction, and returns `PurchaseDto`.
  17. Catch block: handles rollback, replay lookup, and lock contention.
- **Transaction & Locking Flaw Analysis (FIN-011):**
  > [!CRITICAL]
  > **Vulnerability `FIN-011` Confirmed:** Lines 75–79 execute:
  > ```csharp
  > if (accountId is not null)
  > {
  >     var accountMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, accountId, ct);
  >     FinancialRules.AssertProtectedSourceBalance(accountMovements, accountId, paid);
  > }
  > ```
  > This query executes **BEFORE** `await using var transaction = await _db.Database.BeginTransactionAsync(ct);` at line 97!
  > Because the connection operates in default autocommit mode outside any transaction, the `WITH (UPDLOCK, ROWLOCK, HOLDLOCK)` locks are dropped as soon as the query completes. Between lines 79 and 97, the handler iterates over item IDs querying stock movements and checks the business day gate. During this multi-millisecond gap, concurrent transactions can deplete the treasury account, causing overdraft or unexpected balance assertion failures.
- **Classification:** Core purchasing orchestration method with critical concurrency locking gap.

---

### 3. Actor Lookup Lambda (`u => u.Id == request.ActorUserId`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:42:76:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.ActorUserId`
- **Line Span:** Line 42
- **Actual Behavior & Inputs:**
  Loads operator user entity from `_db.Users.AsNoTracking()` to verify purchasing permissions.
- **Classification:** Query filter lambda.

---

### 4. Approver Lookup Lambda (`u => u.Id == request.ManagerApproverUserId`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:49:83:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.ManagerApproverUserId`
- **Line Span:** Line 49
- **Actual Behavior & Inputs:**
  Loads manager user entity to validate approval signature when purchasing requires elevated manager approval.
- **Classification:** Query filter lambda.

---

### 5. Supplier Lookup Lambda (`s => s.Id == request.SupplierId`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:56:79:SimpleLambdaExpression`
- **Signature:** `s => s.Id == request.SupplierId`
- **Line Span:** Line 56
- **Actual Behavior & Inputs:**
  Verifies that the target supplier exists in `_db.Suppliers.AsNoTracking()`. Throws `DomainException` if missing.
- **Classification:** Foreign key integrity validation lambda.

---

### 6. Line Item Calculation Lambda
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:60:42:SimpleLambdaExpression`
- **Signature:** `l => new { l.InventoryItemId, l.QuantityMilliUnits, l.UnitCostPiastres, LineTotal = ... }`
- **Line Span:** Lines 60–64
- **Actual Behavior & Inputs:**
  Projects request lines into anonymous objects containing computed line totals calculated via `PurchasingRules.PurchaseLineTotal(quantity, unitCost)`.
- **Classification:** Domain rule calculation projection.

---

### 7. Item ID Extraction Lambda (`l => l.InventoryItemId`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:65:36:SimpleLambdaExpression`
- **Signature:** `l => l.InventoryItemId`
- **Line Span:** Line 65
- **Actual Behavior & Inputs:**
  Selects distinct inventory item IDs from the invoice lines.
- **Classification:** In-memory transformation projection.

---

### 8. Inventory Item Existence Count Lambda (`i => itemIds.Contains(i.Id)`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:66:81:SimpleLambdaExpression`
- **Signature:** `i => itemIds.Contains(i.Id)`
- **Line Span:** Line 66
- **Actual Behavior & Inputs:**
  Filters `_db.InventoryItems.AsNoTracking().CountAsync(...)` to ensure every ingredient ID in the invoice exists in the inventory catalog.
- **Classification:** Batch foreign key validation predicate.

---

### 9. Gross Invoice Sum Lambda (`l => l.LineTotal.Piastres`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:69:50:SimpleLambdaExpression`
- **Signature:** `l => l.LineTotal.Piastres`
- **Line Span:** Line 69
- **Actual Behavior & Inputs:**
  Sums line total piastres to compute overall invoice cost.
- **Classification:** In-memory summation projection.

---

### 10. Prior Cost Filter Lambda
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:87:24:SimpleLambdaExpression`
- **Signature:** `m => m.InventoryItemId == itemId && m.Type == nameof(StockMovementType.PurchaseReceipt) && m.UnitCostPiastres != null`
- **Line Span:** Lines 87–88
- **Actual Behavior & Inputs:**
  Filters `_db.StockMovements.AsNoTracking()` for prior purchase receipts of the item with recorded unit costs.
- **Classification:** Query filter predicate.

---

### 11. Prior Cost OrderBy Lambda (`m => m.CreatedAtUtc`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:89:36:SimpleLambdaExpression`
- **Signature:** `m => m.CreatedAtUtc`
- **Line Span:** Line 89
- **Actual Behavior & Inputs:**
  Orders prior stock movements by descending creation time to find the most recent unit cost.
- **Classification:** SQL sorting projection lambda.

---

### 12. Prior Cost UnitCost Selector Lambda (`m => m.UnitCostPiastres`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:90:25:SimpleLambdaExpression`
- **Signature:** `m => m.UnitCostPiastres`
- **Line Span:** Line 90
- **Actual Behavior & Inputs:**
  Selects unit cost piastres for the last purchase receipt.
- **Classification:** Scalar projection lambda.

---

### 13. Entity Line Mapping Lambda
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:129:38:SimpleLambdaExpression`
- **Signature:** `l => new PurchaseLineEntity { ... }`
- **Line Span:** Lines 129–137
- **Actual Behavior & Inputs:**
  Transforms in-memory lines into `PurchaseLineEntity` database records.
- **Classification:** Entity mapping projection.

---

### 14. Contention Retry Delegate Lambda
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:221:21:ParenthesizedLambdaExpression`
- **Signature:** `async () => await FindPurchaseAsync(purchaseId, ct)`
- **Line Span:** Line 221
- **Actual Behavior & Inputs:**
  Retry polling closure passed to `BalanceLockContention.AwaitSiblingAsync`.
- **Classification:** Asynchronous polling closure.

---

### 15. `FindPurchaseAsync` Helper
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:231:5:MethodDeclaration`
- **Signature:** `private Task<PurchaseEntity?> FindPurchaseAsync(string purchaseId, CancellationToken ct)`
- **Line Span:** Lines 231–233
- **Actual Behavior & Inputs:**
  Queries `_db.Purchases.AsNoTracking()` including `Lines` and `Payments` for `purchaseId`.
- **Classification:** Query helper method.

---

### 16. FindPurchase Include Lines Lambda (`p => p.Lines`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:232:46:SimpleLambdaExpression`
- **Signature:** `p => p.Lines`
- **Line Span:** Line 232
- **Actual Behavior & Inputs:**
  Eager loading inclusion of purchase line entities.
- **Classification:** EF navigation inclusion lambda.

---

### 17. FindPurchase Include Payments Lambda (`p => p.Payments`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:232:68:SimpleLambdaExpression`
- **Signature:** `p => p.Payments`
- **Line Span:** Line 232
- **Actual Behavior & Inputs:**
  Eager loading inclusion of purchase payment records.
- **Classification:** EF navigation inclusion lambda.

---

### 18. FindPurchase ID Filter Lambda (`p => p.Id == purchaseId`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:233:34:SimpleLambdaExpression`
- **Signature:** `p => p.Id == purchaseId`
- **Line Span:** Line 233
- **Actual Behavior & Inputs:**
  Primary key filter matching `purchaseId`.
- **Classification:** Primary key filter predicate.

---

### 19. `ToDto` Helper
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:235:5:MethodDeclaration`
- **Signature:** `internal static PurchaseDto ToDto(PurchaseEntity p, bool wasReplayed)`
- **Line Span:** Lines 235–242
- **Actual Behavior & Inputs:**
  Converts `PurchaseEntity` to `PurchaseDto` for client responses.
- **Classification:** Pure DTO mapping method.

---

### 20. ToDto Lines Projection Lambda
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:237:24:SimpleLambdaExpression`
- **Signature:** `l => new PurchaseLineDto(l.InventoryItemId, l.QuantityMilliUnits, l.UnitCostPiastres, l.LineTotalPiastres)`
- **Line Span:** Line 237
- **Actual Behavior & Inputs:**
  Projects `PurchaseLineEntity` to `PurchaseLineDto`.
- **Classification:** DTO mapping projection.

---

### 21. ToDto Payments Projection Lambda
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs:240:27:SimpleLambdaExpression`
- **Signature:** `pp => new PurchasePaymentDto(pp.Id, pp.AmountPiastres, pp.PaymentAccountId, pp.ApprovedBy, pp.CreatedAtUtc)`
- **Line Span:** Line 240
- **Actual Behavior & Inputs:**
  Projects `PurchasePaymentEntity` to `PurchasePaymentDto`.
- **Classification:** DTO mapping projection.

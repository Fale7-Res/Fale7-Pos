"""Persist field-specific reasoning for actually read DbContext expression getters."""
from knowledge_store import *
import json

path='src/Falah.Infrastructure/Persistence/FalahDbContext.cs'
source=json.loads((WORK/'Knowledge/Source-Symbols-CallableFacets/Files'/Path(path+'.json')).read_text(encoding='utf8'))
meanings="""
Orders|Frozen sale aggregates and mutable payment/refund state; unique CheckoutRequestId and Version token belong model, not getter.
OrderItems|Sold line snapshots; model Product/Variant Restrict FKs counter ordinary hard deletion of sold menu identities.
OrderRefunds|Refund event facts distinct from order accumulated RefundAmount; operational versus original shift allocation belongs handler/report queries.
Shifts|Opening/count/handover lifecycle; model unique active Open filter and Version token, not automatic checkout admission lock.
BusinessDays|Open/Closing/Closed/Reopened identities; current model only ID/OpenRequestId key, no active singleton or state concurrency token.
Expenses|Paid expense fact and source-account/day/date snapshot; amount and permission checks belong command.
TreasuryMovements|Posted/pending financial ledger; signed account balances derived by callers, getter does not filter Posted or enforce sufficient balance.
FinancialJournalEntries|Persisted financial idempotency keys/status; retrieving DbSet is not a global writer lease or completed-operation filter.
OrderSequence|Order numbering state; seed floor127 is configuration, getter itself does not atomically allocate or increment.
Users|Authentication/financial-permission identities; no PIN/session data read by this audit. Getter does not enforce actor ownership.
ManagerApprovals|Issued one-time financial approval records; Consumed concurrency token belongs model and saving transaction, not entityset access.
Categories|Current product grouping metadata; changing category can restate category reports independently from frozen line names.
Products|Current menu identities/prices/recipe references; authoritative checkout/pricing rules and restrictive sold-product FK external to getter.
ProductVariants|Variant identities and recipe overrides; sold variant deletion restricted by order-item relation, not by getter.
ProductImages|Typed nonfinancial image entityset only; no image download, rendering or authentication investigation resumed.
UserSessions|Bearer-session hash/expiry persistence; no credential enumeration or live session changes. Lookup/permission gate external.
PriceRules|Current offer rules; historical sale amount remains frozen in order, not recomputed by this property.
PriceRuleProducts|Offer/product membership join; composite key and cascading policy belong mapping.
PriceRuleDays|Offer day-of-week membership; composite rule/day identity mapping separate from pricing activation logic.
TaxRules|Current tax configuration; historical tax reports read frozen order amounts, getter does not recalculate any sale.
Recipes|Current menu recipe graph, intentionally affects current costing of historical sales under documented limitation.
RecipeItems|Dish ingredient quantities/kind/yield ratio; positive input and common unit conversion required in callers.
InventoryItems|Current stock unit/cost source/estimates; unit-change guard prevents historical reinterpretation only in command, not set getter.
StockMovements|Inventory receipts/usage/loss/count facts; append-only intent and sign correctness are not enforced by getter itself.
PrepRecipes|Batch output/cost graph; output FK restricts ingredient deletion but does not prevent concurrent logical graph cycles.
PrepRecipeLines|Batch ingredient quantities; parent relation cascades and ingredient reference restricts deletion, per model.
OrderPackagingRules|Once-per-order packaging cost inputs, distinct from per-item recipe coverage; missing cost candidate preserved.
Suppliers|Supplier identity metadata used by invoice/ledger references; no supplier balance computed here.
Purchases|Invoice amount/payment cache aggregate; debt comes ledger, getter does not lock outstanding balance or validate receipt signs.
PurchaseLines|Invoice quantity/unit-cost snapshots; negative-pair receipt validation FIN018 belongs command, no global Money sign assumption.
PurchasePayments|Per-invoice payment facts/idempotency; no posting or account lock on retrieval.
SupplierLedgerEntries|Debit invoice/credit payment history; outstanding derived by caller with supplier/invoice scope.
Customers|Customer identity/contact entityset; no customer data enumerated, copied or changed in audit.
CustomerAddresses|Customer address relation; no private address values inspected. Getter has no permission redaction policy.
Drivers|Delivery driver identity; cash custody tracked separately and not a property-derived balance.
DeliveryServices|Current delivery-service configuration, not guaranteed immutable historical fee snapshot.
Deliveries|Per-order dispatch/payment context; model unique OrderId and Version token distinct from one driver-custody record.
DriverCustodies|Collected cash obligations; unique DeliveryId restricts duplication but status transitions/settlement guards external.
DeliveryReassignments|Historical driver reassignment records indexed by delivery; no cash transfer performed by getter.
DeliveryEvents|Financial delivery event audit/history; indexed delivery timeline, amounts have event-specific meaning.
DriverSettlements|Actual driver cash settlement and signed difference; unique key supports retry, balance/custody state logic in handler.
PrintJobs|Queue facts, intentionally no Order FK; retrieving DbSet sends no physical job and does not prove financial success.
PrintJobAttempts|Dispatch-attempt facts/idempotency, independent of financial posting. No printer operation by this getter.
ChecklistItems|Closing/opening control requirements; configuration seed is not evidence that a live requirement was completed.
ChecklistCompletions|Day/item completion facts; reopening removes closing completions, not performed by getter.
BusinessDayCloseSnapshots|Frozen reconciliation/revision/authority metadata; getter does not serialize concurrent closes or exclude admitted writes.
ControlExceptionStates|Exception review states; marking reviewed is not real financial reconciliation or reversal.
ControlAuditEvents|Administrative/authorization/day-count audit facts; no evidence of actor permission solely from row existence.
AuditEvents|General financial command audit facts; mutable EF set does not guarantee tamper-proof append-only storage.
CashDrawerOpens|Drawer access audit/event records; retrieving records does not activate hardware or authorize money movement.
CashDenominationCounts|Finalized physical count snapshots; getter does not validate denomination arithmetic or reconstruct expected cash.
Settings|Stored operational settings including financial thresholds; no live setting values read or changed here.
CashHandovers|Movement-linked cash handover records; filtered unique nonnull movement key belongs model.
Employees|Employee financial identities/pay configuration; no private employee rows accessed during audit.
EmployeeLedgerEntries|Employee credit/debit ledger and idempotency; current balance versus period movement scope chosen by query service.
AttendanceRecords|Date/employee attendance facts; unique employee/date model key supports one entry, wage consequences belong handler.
PayrollRuns|Payroll lifecycle/period/payment metadata; filtered live-period uniqueness is not stale pay/cancel state concurrency protection.
PayrollLines|Frozen per-employee earnings/deductions/net; getter alone does not enforce sum consistency or run/employee FK.
BackupRecords|Backup/restore history metadata; historical Verified status is not fresh proof file still exists or can restore now.
"""
roles=dict(line.split('|',1) for line in meanings.strip().splitlines())
records=[]
for facet in source['facets']:
    name=facet['qualifiedSymbol'].split('.')[-1].split('::')[0]
    assert name in roles,name
    records.append({
        'stable_id':facet['stable_id'],'status':'ANALYZED','signature':facet['qualifiedSymbol'],
        'span':[facet['startLine'],facet['startColumn'],facet['endLine'],facet['endColumn']],
        'reasoning':'Expression getter returns context Set<T>() for '+name+'. '+roles[name]+' No explicit query, SaveChanges, migration or hardware action in this accessor.',
        'inputs':'Current context with injected DbContextOptions; no request or entity values.',
        'outputs':'Typed EF DbSet for '+name+', exposing query/change-tracking API to trusted application code.',
        'side_effects':'No explicit persisted write or query in accessor; framework set/service initialization is not a financial operation.',
        'trust_boundaries':'Not an authorization boundary; handlers/middleware must restrict which entityset operations are allowed.',
        'database':'Set<T> factory/access only, not enumeration. Source model/read filters and caller transaction are separate.',
        'printing_ui':'No native UI/printer access even for queue/access-log entitysets.',
        'performance':'No collection materialization, history scan or amount arithmetic in getter; actual query cost belongs caller.',
        'duplicates_conflicts':'Same generic EF set pattern intentional; each distinct entity and model policy must remain intact if consolidated.',
        'issues_hypotheses':roles[name]+' No extra defect inferred merely from public context set access.',
        'tests_missing':'Provider mapping resolution/query smoke on authorized disposable fixture; financial invariants require separate command/concurrency tests, not getter-mirroring tests.',
        'verified_callers_callees':'Exact declaration10-74 and all OnModelCreating79-480 source read; Set<T>() inherited EF behavior, live provider not invoked.'
    })
saved=save_callable_facet_review('financial_coordinator',path,records)
print(json.dumps({'saved_source_getter_facets':len(records),'original_callable_count_increment':0}))

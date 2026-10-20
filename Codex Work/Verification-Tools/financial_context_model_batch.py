"""Persist manually reviewed EF model behavior using prior Roslyn identities, not regex inventory."""
from knowledge_store import *
import json
import re

path='src/Falah.Infrastructure/Persistence/FalahDbContext.cs'
source=load_symbols(path)
lines=(ROOT/path).read_text(encoding='utf-8-sig').splitlines(keepends=True)
entity_notes="""
OrderEntity|Id PK; unique checkout request and sequence; application-managed Version concurrency token (caller increments); cascade Items/Refunds; restrictive Shift/User references. FK proves existence, not Open shift/day predicate.
OrderItemEntity|Id PK; restrictive Product/Variant references normally prohibit deleting sold identities. Historical category can still be changed on existing product.
OrderRefundEntity|Id PK and unique financial refund idempotency key. Parent Order cascade defined on Order entity; per-amount eligibility outside model.
ShiftEntity|Unique open-request and nonnull close-request keys; unique Status filtered Open enforces single active shift. Version concurrency token only guards updates with original version, not stale checkout that never updates Shift.
TreasuryMovementEntity|Id PK and unique idempotency key prevent duplicate same-key posting; do not serialize distinct-key balance decisions or verify amount signs/accounts.
BusinessDayEntity|Id PK and unique OpenRequestId only. No filtered active-day singleton, Version or Close/Reopen key concurrency guard declared. FIN009 and closed-day admission candidate need caller coordination.
ExpenseEntity|Id PK and unique idempotency; query indexes on ShiftId/PaidFromDrawer and BusinessDate. No account permission/positive amount enforcement in this block.
FinancialJournalEntryEntity|IdempotencyKey is primary key, so unique persisted operation identity. Journal insertion must share actual financial transaction; table/key is not a global admission lease.
OrderSequenceEntity|Id PK with model seed LastAllocated127 for row1. Metadata seed is not audit invocation of allocation/DB initializer.
UserEntity|Id PK. Per-user salted PIN hash intentionally not uniquely indexed; application verifies duplicate PIN against active users. Concurrency between distinct user creation requests needs separate guards, not assessed as newly proven here.
ManagerApprovalEntity|ApprovalId PK and Consumed concurrency token allow one false->true update to commit with associated command. Issued/action/actor checks belong registry policy.
UserSessionEntity|Id PK; TokenHash unique and max64. No token value inspected; uniqueness is not session ownership/permission validation.
CategoryEntity|Id PK; restrictions on referenced product category configured at Product entity, not in this tiny block.
ProductEntity|Id PK; restrictive category FK and cascade unsold variants. Product RecipeId intentionally plain nullable pointer; actual Recipe.ProductId relationship mapped elsewhere. Sold OrderItem references restrict hard deletion.
ProductVariantEntity|Id PK only here; parent Product cascade and sold-line restrictive FK configured elsewhere. Variant recipe pointer validity belongs application/mapping conventions.
ProductImageEntity|ProductId PK; content/version max80/40; cascade Product relation. Nonfinancial image lifecycle metadata only; paused image-auth/rendering work not resumed.
PriceRuleEntity|Id PK; cascade offer product/day join rows when rule deleted, not historical order price snapshots.
PriceRuleProductEntity|Composite PriceRuleId/ProductId PK; Product FK cascade removes membership when deleting unsold product; frozen sale price unaffected.
PriceRuleDayEntity|Composite rule/day-of-week PK prevents duplicate membership, not enum-range/business validity by itself.
TaxRuleEntity|Id PK only; active-tax choice/percent bounds and frozen order tax are application rules.
RecipeEntity|Id PK; Product and recipe-line cascade. No unique active recipe per product configured here. Historical cost restatement is documented current-recipe policy, not immutable accounting snapshot.
RecipeItemEntity|Id PK; restrictive inventory ingredient FK. Positive quantity/yield and aggregate recipe identity constraints belong handlers.
InventoryItemEntity|Id PK only; stock-unit/cost-source policies not DB checks in this block.
StockMovementEntity|Id PK and unique operation movement key; restrictive inventory/supplier references. Prevents repeated same-key stock deduction, not negative receipt magnitude or distinct request duplicates by business intent.
PrepRecipeEntity|Id PK; restrictive output inventory FK and cascade input lines. No graph-cycle/unique-active-output database invariant declared here.
PrepRecipeLineEntity|Id PK and restrictive ingredient FK. Graph and quantity rules not represented as FK.
OrderPackagingRuleEntity|Id PK; restrictive ingredient FK. No unique active order-type rule here; pure costing selects first active rule.
SupplierEntity|Id PK only; debt and payment calculations derive separate ledger, not supplier row.
PurchaseEntity|Id PK; unique receipt idempotency; restrictive supplier FK; cascade invoice lines/payments. No row-version on outstanding cache; distinct payments require ledger/account transaction coordination (FIN005).
PurchaseLineEntity|Id PK; restrictive inventory ingredient FK. Quantity/cost sign pair not constrained here (FIN018).
PurchasePaymentEntity|Id PK and unique payment key; parent Purchase relationship configured from aggregate. Unique key does not prevent distinct-key overpayment.
SupplierLedgerEntryEntity|Id PK, unique key, supplier/invoice query index and restrictive supplier FK. PurchaseId referential integrity not explicitly configured here; no live schema inspected.
CustomerEntity|Id PK; normalized phone unique and child-address cascade. Normalization before insert belongs caller. No actual contacts accessed.
CustomerAddressEntity|Id PK; Customer relation configured by parent. Owner/access controls not EF metadata.
DriverEntity|Id PK; financial custody references restrict deletion via child mappings.
DeliveryServiceEntity|Id PK only; service fee bounds/active selection are caller rules.
DeliveryEntity|Id PK; unique OrderId, restrictive Order/Shift/Driver references and Version concurrency token. One dispatch per order, not automatic once-only settlement protocol.
DriverCustodyEntity|Id PK; unique DeliveryId, restrictive Delivery/Driver references. Reassignment relabels existing custody by policy; no status version declared in this block.
DeliveryReassignmentEntity|Id PK and DeliveryId query index; no explicit FK shown here. Historical event validity follows handler constraints.
DeliveryEventEntity|Id PK and DeliveryId timeline index. No event amount/sign/transition validation by index.
DriverSettlementEntity|Id PK and unique settlement key. Underlying custody state/balance and financial transaction atomicity are command concerns.
PrintJobEntity|Id PK; route/status, order and request indexes; cascade attempts. No Order FK intentional: printing fact must not block financial order lifecycle. No physical jobs run by model configuration.
PrintJobAttemptEntity|Id PK and unique attempt key. Hardware acknowledgment/retry semantics not evaluated as financial success.
ChecklistItemEntity|Id PK and fixed opening/closing requirement metadata seed. Required controls not equivalent to completed actual financial reconciliation; HasData only defines model migration data here.
ChecklistCompletionEntity|Composite day/item PK gives one completion per pair. No day write-admission lock from this key.
BusinessDayCloseSnapshotEntity|Id PK, day index and restrictive day FK. No unique day/revision or filtered Authoritative constraint declared. Concurrent close/snapshot lineage remains unverified lead.
ControlExceptionStateEntity|Id PK only. Marking exception reviewed cannot be assumed monetary correction.
ControlAuditEventEntity|Id PK and CreatedAt index. Queryable audit record not cryptographically immutable or independent proof command succeeded.
AuditEventEntity|Id PK; timestamp/category/user indexes support financial action lookup. No append-only DB policy declared here.
CashDrawerOpenEntity|Id PK and ShiftId index. No drawer hardware or money effect by model configuration.
CashDenominationCountEntity|Id PK and BusinessDayId index. No denomination/sign/checked arithmetic constraints here; pure cash-count candidate preserved.
CashHandoverEntity|Id PK and filtered unique nonnull TreasuryMovementId prevent two links to same movement. Null handovers can coexist; business amount matching separate.
EmployeeEntity|Id PK and unique EmployeeNumber. Active employee status and payroll authority not DB access gate.
EmployeeLedgerEntryEntity|Id PK, employee query index and filtered unique nonnull idempotency key. Signed debit/credit classification and FK/transaction policy separate; nullable keys permit unkeyed records if caller uses them.
AttendanceRecordEntity|Id PK and unique employee/date pair; one attendance record per day enforced by metadata, not wage calculation accuracy.
PayrollRunEntity|Id PK, unique nonnull payment key and live-period unique filtered Status<>Cancelled. Prevents concurrent live period duplicates; does not prevent stale Approved->Cancelled/Paid state overwrite without version/lock.
PayrollLineEntity|Id PK and run query index only. Explicit run/employee FK and monetary component checks absent in block; conventions/snapshot/deployed state must be checked before claiming actual orphans.
BackupRecordEntity|Id PK only; filename uniqueness/verified physical file retention/restore outcome truth depend on handlers and filesystem.
"""
meaning=dict(row.split('|',1) for row in entity_notes.strip().splitlines())
# This is only source-line association for already manually read configuration blocks.
# All declaration identities/spans remain the existing Roslyn AST map.
starts=[]
for number,line in enumerate(lines,1):
    match=re.search(r'modelBuilder\.Entity<([^>]+)>',line)
    if match:starts.append((number,match.group(1)))

def slice_symbol(symbol):
    a,b=symbol['startLine']-1,symbol['endLine']-1
    def part(text,start,end=None):
        return text.encode('utf-16-le')[start*2:None if end is None else end*2].decode('utf-16-le')
    if a==b:return part(lines[a],symbol['startColumn']-1,symbol['endColumn']-1)
    return part(lines[a],symbol['startColumn']-1)+''.join(lines[a+1:b])+part(lines[b],0,symbol['endColumn']-1)

receipts=[]
for symbol in source['symbols']:
    if not symbol['isCallable']:continue
    start,end=symbol['startLine'],symbol['endLine']
    if symbol['kind']=='ConstructorDeclaration':
        reason='Empty body delegates DbContextOptions to base constructor. No custom provider override, query, migration, SaveChanges, command or financial operation. Injected options/provider lifetime outside this function.'
        entity='ContextConstructor'
    elif symbol['kind']=='MethodDeclaration':
        reason='OnModelCreating configures model metadata: operation uniqueness, restrictive/cascade relationships, four mutable concurrency-token families, filtered active-shift/live-payroll constraints and fixed model seed data. BusinessDay active singleton/state token and Payroll state token absent; no financial sign CHECK declarations. Child configuration delegates analyzed individually. EF callback does not itself execute migrations/seed business records; no runtime model/provider was invoked in audit.'
        entity='WholeModel'
    else:
        entity=next(name for line,name in reversed(starts) if line<=start)
        assert entity in meaning,entity
        prefix=lines[start-1].encode('utf-16-le')[:(symbol['startColumn']-1)*2].decode('utf-16-le')
        call=re.search(r'(\w+)(?:<[^>]*>)?\(\s*$',prefix)
        operation=call.group(1) if call else 'enclosing fluent metadata operation'
        body=slice_symbol(symbol)
        if operation=='Entity':
            reason='Entity-builder callback applies '+entity+' metadata only. '+meaning[entity]
        else:
            reason='Exact '+operation+' expression selector within '+entity+': '+body+'. EF inspects this metadata expression; it is not a financial row mutation or permission decision. '+meaning[entity]
    receipts.append({
        'stable_id':symbol['stable_id'],'status':'ANALYZED',
        'signature':symbol.get('qualifiedSymbol') or symbol['name'],
        'span':[start,symbol['startColumn'],end,symbol['endColumn']],
        'reasoning':reason,
        'inputs':'DbContextOptions or EF model/entity builder and exact expression parameters; no operational records.',
        'outputs':'Configured model metadata or expression tree selecting exact key/index/navigation/concurrency property.',
        'side_effects':'In production model construction mutates in-memory metadata; schema/data only change through separate authorized migrations/initializer, not invoked here.',
        'trust_boundaries':'Model constraints supplement handlers, not authorization, balanced journal accounting, state admission or live deployed-schema attestation.',
        'database':'Explicit source mapping read; no SQL connection/query/migration/schema change in this audit.',
        'printing_ui':'Print metadata classified narrowly for financial independence; no printing/UI workstream or hardware resumed.',
        'performance':'Indexes mapped to source query shapes; usefulness/lock ranges/plans require authorized disposable SQL measurement. No benchmark claim.',
        'duplicates_conflicts':'Frozen historical migration models may repeat current mapping intentionally. Current metadata cannot be substituted into old migration snapshots. FK existence does not mean business state Open.',
        'issues_hypotheses':meaning.get(entity,'Whole context metadata interpretation only; independent/source/runtime limits retained.'),
        'tests_missing':'Disposable-provider model/index/FK/unique/filter/concurrency tests; exact current-source schema vs migration snapshot consistency; multi-key transactions and terminal state interleavings. No live tests executed.',
        'verified_callers_callees':'All481source lines manually read. Entity meanings curated from actual configuration; declaration identities from original Roslyn map. Source-line association regex is not represented as semantic compilation.'
    })
saved=save_review('financial_coordinator',path,{
    'file_review_status':'COMPLETE_STATIC_ORIGINAL_CALLABLE_AND_SOURCE_MODEL_REVIEW',
    'evidence':'Actual entire source read;212 original callable configuration/constructor/method records individually persisted;59 supplemental getters separately receipted. Source model, not deployed SQL/schema/runtime proof.',
    'symbols':receipts
})
assert not json.loads(saved.read_text(encoding='utf8'))['remaining_callable_ids']
print(json.dumps({'saved_original_callable_receipts':len(receipts),'entity_configuration_blocks':len(starts),'supplemental_getters_separate':59}))

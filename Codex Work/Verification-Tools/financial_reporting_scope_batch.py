"""Persist the coordinator's bounded, manually read reporting-query source batch."""
from knowledge_store import *
import json

path = 'src/Falah.Infrastructure/Reports/ReportingQueryService.cs'
source = load_symbols(path)
lines = (ROOT/path).read_text(encoding='utf-8-sig').splitlines(keepends=True)
notes = {
33: 'Constructor retains scoped context only; no query, transaction, or financial write.',
35: 'Shared loader includes frozen Items, excludes Cancelled, applies Cairo half-open UTC range and optional nonempty ShiftId; materializes all matching orders. Confirms sales/tax Domain caller filtering contract here, not every caller elsewhere.',
66: 'Uses shared IsCashOnDelivery for candidate order IDs, removes only Settled custody IDs. Undelivered COD remains outside collected sales; shortage not subtracted twice. Separate orders/custody reads have no report snapshot transaction.',
85: 'Single-day sales overload converts optional DateOnly to shared range; null remains unbounded. Delegation preserves one filtering implementation.',
88: 'Loads scoped sales, maps frozen orders, resolves current COD custody and calls SalesRules. FIN010 whole-order pending subtraction passes through; no current menu/tax recomputation.',
100: 'Single-day tax overload delegates to range variant; absent date means all selected history, not current day.',
103: 'Tax report uses shared noncancelled loader and frozen tax fields via SalesRules; copies five integer totals, never queries current tax rules.',
112: 'Copies all sale/refund/discount/channel/type/custody metrics directly into DTO without new arithmetic. Mapping does not correct underlying FIN010.',
121: 'Single-day expense overload delegates range; preserves optional shift and calendar date semantics.',
124: 'Expense rows filter stored BusinessDate inclusive, optional nonempty shift, sort newest and sum amounts. All-user management scope intentional; reports_view endpoint gate not independently verified by this batch.',
141: 'Single-day refund overload delegates shared activity range, rather than filtering original sale date.',
144: 'Refund activity uses refund timestamp half-open bounds and OperationalShiftId, joins order number, preserves OriginalShiftId separately. Correctly distinct from original-order sales range; orphan refund inner-join omission depends on persistence invariants.',
164: 'Day reconciliation uses day-ID orders/expenses/Posted movements but BusinessDate shifts. Local Balance sums only that day movement deltas, not historical absolute account stock. Custody counts Collected, posted settlement and purchases separate. Multi-query reads lack local snapshot transaction; close caller exclusion and balance-label policy need trace. Source doc purchases-absent stale because purchasingPaid203 exists.',
215: 'Daily summary reuses reconciliation then separately loads day, Cairo-calendar employee ledger, BusinessDate shifts and current cashier names. Uses posted wage/overtime/advance/withdrawal, not duplicate payroll payout as expense. Different reads may straddle changes; no closed-time snapshot proof.'
}
lambda_notes = {
37:'Include Order.Items snapshots, no live menu join.', 38:'Exclude Cancelled sales.',
43:'Timestamp inclusive start, exclusive end.', 50:'Exact ShiftId equality after outer nonempty guard.',
70:'Delegate positive delivery/cash COD classification.', 71:'Project candidate order identity.',
76:'Candidate custody IDs whose status exactly Settled.', 77:'Project settled order ID.',
129:'Stored DateOnly range inclusive endpoints.', 130:'Expense shift equality after outer guard.',
132:'Newest expense timestamp ordering.', 133:'Expense DTO preserves amount/account/drawer/creator/date facts.',
135:'Expense AmountPiastres sum, not treasury movement sum.',
150:'Operational refund shift and actual refund event half-open timestamp range.',
152:'Join refund OrderId to order Id and pair original order number; inner join semantics.',
155:'Refund DTO preserves amount/payment/reason/actor/time and both shift identities.',
166:'Load exact business-day identity.', 169:'Include frozen order items.',
170:'Noncancelled exact BusinessDayId sales, distinct from calendar grouping.',
175:'Expense day-ID predicate and nullable sum with outer empty0.',
178:'Posted movements for this day ID only, excluding earlier account opening balance.',
179:'Local account inflow minus outflow; same-account transfer cancels. Day delta, not absolute balance.',
183:'Shifts by BusinessDate, not day ID.', 184:'Stored nullable ExpectedCash with null0.',
185:'Stored nullable ActualCash with null0.',
186:'Negative discrepancy filter then absolute shortage magnitude; MinValue bound untested.',
187:'Positive discrepancy filter then overage magnitude.',
191:'IDs of already scoped day orders.', 193:'All unsettled custodies, locally scoped after materialization.',
194:'Scoped order membership and actual CollectedPiastres, not original expected amount.',
196:'Posted driver_settlement on exact day.', 197:'Nullable settled amount sum with empty0.',
203:'Purchase payment type and amount sum from already loaded posted day movements.',
218:'Day lookup for ledger calendar range.', 223:'Employee ledger half-open Cairo UTC window.',
224:'Local employee-type filter, choosing debit/credit parameter explicitly.',
225:'Chosen credit or debit amount, not automatic netting.',
232:'Calendar-date shifts, not explicit day ID.',
233:'Distinct shift opener IDs, not all sale actors.',
234:'Current users matching shift opener IDs, no active-role filter.',
235:'Distinct current names; renaming can change historical display label, not money.'
}
receipts=[]
for symbol in source['symbols']:
    if not symbol['isCallable'] or symbol['endLine']>239:
        continue
    start=symbol['startLine']
    if symbol['kind'] in ('ConstructorDeclaration','MethodDeclaration'):
        reason=notes[start]
    else:
        # These exact predicates/projections were read in the bounded source batch.
        # Persist each Roslyn identity separately, including same-line lambdas.
        reason=lambda_notes[start]
        reason+=' This source operation does not independently authenticate or validate financial input; enclosing query contracts remain required.'
        if symbol['startLine']==symbol['endLine']:
            raw=lines[start-1].encode('utf-16-le')
            reason+=' Exact operation: '+raw[(symbol['startColumn']-1)*2:(symbol['endColumn']-1)*2].decode('utf-16-le')
    receipts.append({
        'stable_id':symbol['stable_id'],'status':'ANALYZED',
        'signature':symbol.get('qualifiedSymbol') or symbol['name'],
        'span':[start,symbol['startColumn'],symbol['endLine'],symbol['endColumn']],
        'reasoning':reason,
        'inputs':'Exact declared day/date/shift/query parameters or captured row fields.',
        'outputs':'Read-only DTO/query result/predicate/projected value, or missing-day exception.',
        'side_effects':'Queries/local list/set operations only; no application query actually invoked by audit.',
        'database':'Source EF scopes/joins/grouping reviewed; separate awaits have no local report snapshot transaction.',
        'trust_boundaries':'Endpoint report authorization and consistent trusted query inputs required; this service does not authenticate.',
        'printing_ui':'DTO output only. No native/printing test or balance-label observation claimed.',
        'performance':'Unpaged history/items and multiple roundtrips; no latency/SQL plan measurement.',
        'duplicates_conflicts':'Shared sales loader reused. Day-ID vs calendar-date and day-flow vs absolute account interpretation explicitly qualified.',
        'issues_hypotheses':'FIN010 source passes through; reconciliation balance presentation/snapshot consistency remain leads, not new confirmed defects.',
        'tests_missing':'Midnight/empty shift/cancellations, refund event-vs-sale dates, COD custody, prior-day opening balance, repeated-date days and snapshot consistency.',
        'verified_callers_callees':'Complete source33-239 and pure SalesRules/OrderPaymentQueries read. Source242-end not promoted.'
    })
saved=save_review('financial_coordinator',path,{
    'file_review_status':'PARTIAL_STATIC_CALLABLE_REVIEW_THROUGH_LINE239',
    'evidence':'Actual complete methods through DailySummary239 source-read. Next Range242; not whole-file completion.',
    'symbols':receipts
})
assert len(json.loads(saved.read_text(encoding='utf8'))['symbols'])==len(receipts)
atomic_json(WORK/'Deep-Audit/Database-and-Finance/report-query-scope-parent-trace.json',{
    'at_utc':now(),'baseline':BASELINE,'path':path,'hash':source['worktree_sha256'],
    'reviewed_through':239,'saved_callable_receipts':len(receipts),
    'next_exact_operation':'ReportingQueryService.Range242 then remaining report query functions.',
    'verified_contracts':[
        'Sales/tax excludes Cancelled and uses half-open Cairo UTC range; null/empty shift ignored.',
        'Refund activity actual event date/operational shift differs from sale report original-order date/lifetime refunds.',
        'Reconciliation orders/expenses/treasury by day-ID; shifts/employees by calendar date.',
        'Reconciliation Balance is day net flow; caller labels/close snapshot interpretation unverified.'
    ],'runtime_executed':False,'remaining':'Exact stable IDs in root shard remaining_callable_ids; file incomplete.'
})
print(json.dumps({'saved_partial_callable_receipts':len(receipts),'next':'Range242'}))

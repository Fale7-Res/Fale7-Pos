"""Persist twelve actually analyzed reporting methods; do not promote nested lambdas."""
import json
from knowledge_store import *

path='src/Falah.Infrastructure/Reports/ReportingQueryService.cs'
source=load_symbols(path)
notes={
241:'Converts optional first date to Cairo midnight UTC Start, optional last date to next-midnight End; nullable endpoints preserve unbounded query. Does not validate from<=to here; inverted range normally empty. Source signature instance method but no context state used.',
251:'Joins noncancelled in-range orders to frozen order lines, groups ProductName intentionally (not ID), sums quantity and line total and sorts quantity. Same-name unrelated products/variants coalesce by documented policy. Does not net order-wide discount/tax/refund or represent collected money; caller must label metric as line sales. Full matched item list materialized.',
272:'Loads frozen sold line/name/quantity/total/time, then current product/category dictionaries. Groups by frozen product NAME, picks first resolvable current category, peak Cairo hour by quantity and distinct order count, sorts line revenue. Renaming/reclassification/deletion and same-name products affect grouping/category; not immutable category history. Does not net order-level reductions/refunds. Multiple sequential queries lack one snapshot.',
307:'Same source noncancelled order-line range and name grouping, split by Cairo local hour including DST conversion after materialization; returns only hours sold sorted name/hour. Quantities/line gross totals, not actual net-collected channel figures. Repeated DST hour merges into same local-hour bucket by stated wall-clock policy.',
325:'Inner joins sold lines through current Products, groups current CategoryId and resolves current names. Deleted products are omitted while top-selling frozen-name query retains them; historical recategorization shifts categories. Scope/requirements clarification needed before defect, but exact cross-report difference preserved. Line totals are not net revenue; dictionary lookup fallback retains category ID.',
349:'Loads sold lines and per-order-type counts separately; current product/variant recipe maps, current recipes, inventory/preps and purchase history build costing context. Chooses variant recipe override then product; every sold line increments coverage, only fully known nonempty recipe increments known and cost. Uses current costs for historical sales, explicitly documented limitation. Order packaging applies once per order. Missing packaging adds name but does not affect known/sold counts, so downstream profitability can falsely mark aggregate cost complete (separate candidate). No common snapshot; unchecked totalCost additions/products need bounds. No stock or monetary posting.',
428:'Unions approved treasury/expense/stock/delivery actions in range and sorts newest. Resolves current expense creator names, other sources keep stored names. Does not include every audit source or deduplicate same financial action across treasury/expense, so this is noteworthy-action list not universal security/audit completeness certificate. Treasury query no Posted filter here; candidate operational semantics pending, not new defect. FIN017 false attributed name can flow into list.',
470:'Loads one shift, resolves current opener/closer names from distinct nonnull IDs, calls shared BuildShiftSummary. Missing shift throws; no actor ownership check here. Read endpoint policy remains external.',
487:'Loads newest take shifts then names and sequentially builds each summary. Default90 is only default, no maximum clamp/positive validation in method; caller endpoint limits must be traced before unbounded exposure claim. Per-shift summary repeats orders/custody/expenses queries, read amplification not benchmarked. No financial mutation.',
509:'Loads all shifts with stored BusinessDate, ascending opening time, resolves names and delegates same per-shift summary sequentially. No take limit; calendar identity differs from business-day-ID reconciliation. Consistency and volume need approved query tests, not assumed current measured problem.',
527:'Loads noncancelled orders by ShiftId with no date filter, computes frozen sales/current COD custody, sums expenses with same shift ID and returns stored opening/expected/actual/difference plus names. RefundAmount is lifetime on original sale, distinct operational refund activity (FIN004 boundary). No recomputation of closed expected cash or saved report snapshot.',
545:'Delivery outcome counts use Delivered/Failed/Returned event timestamps independently; fees sum orders delivered in range. Unsettled custody is all current unresolved rows, deliberately a current position distinct from period flows. Delivery duration requires both timestamps, excludes negative durations, returns null if none. Settlements range by CreatedAt with actual/shortage/overage sums, no double deduction in sales. Event counts can overlap lifecycle, not mutually exclusive current statuses. Nullable Sum protects empty fees only; extreme signed difference negation bounds remain untested.'
}
file=WORK/'Knowledge/Reviews/financial_coordinator'/Path(path+'.json')
review=json.loads(file.read_text(encoding='utf8'))
existing={r['stable_id'] for r in review['symbols']}
added=[]
for symbol in source['symbols']:
    if not symbol['isCallable'] or symbol['kind']!='MethodDeclaration' or symbol['startLine']<241:
        continue
    if symbol['stable_id'] in existing:
        continue
    row={
        'stable_id':symbol['stable_id'],'status':'ANALYZED',
        'signature':symbol.get('qualifiedSymbol') or symbol['name'],
        'span':[symbol['startLine'],symbol['startColumn'],symbol['endLine'],symbol['endColumn']],
        'reasoning':notes[symbol['startLine']],
        'inputs':'Declared optional date range, shift identity, supplied names and cancellation token.',
        'outputs':'Read-only report DTO/list/date boundaries or missing-entity error.',
        'side_effects':'Query/local aggregation only; source audit executed no query or business state mutation.',
        'database':'EF filters/joins/materialization and distinct queries reviewed; no common report snapshot transaction.',
        'trust_boundaries':'Report access/ownership gate belongs endpoint/middleware; no authentication inside these methods.',
        'printing_ui':'DTO financial meanings reviewed; no native UI, screenshot or physical output proof.',
        'performance':'Materialized histories/current lookup dictionaries and repeated shift queries; no measured load results.',
        'duplicates_conflicts':'Name-grouped line sales differ from live category join and actual net sales. Current cost versus historical revenue explicitly qualified.',
        'issues_hypotheses':'Packaging completeness candidate saved with counterevidence. Other historical grouping/volume/snapshot concerns not automatically confirmed defects.',
        'tests_missing':'Same-name/deleted/reclassified products, discounted/refunded line-vs-net sums, missing packaging and old-price recipes, shift count limits, COD/settlement boundary snapshots.',
        'verified_callers_callees':'Complete reporting source241-585 read; CostingRules/SalesRules read and OwnerDashboard BuildProfitability205-223 source traced. Nested callable individual receipts remain separate open queue.'
    }
    added.append(row)
review['symbols'].extend(added)
review['file_review_status']='SOURCE_READ_ALL_METHODS_RECEIPTED_NESTED_CALLABLES_STILL_PARTIAL'
review['evidence']='All actual method bodies read and individually analyzed;126 remaining nested callables in latter half not individually promoted merely because source was read.'
save_review('financial_coordinator',path,{k:v for k,v in review.items() if k not in ['remaining_callable_ids','updated_at_utc']})
print(json.dumps({'added_method_receipts':len(added),'remaining_nested_callables':len(source['symbols']) and len(json.loads(file.read_text(encoding='utf8'))['remaining_callable_ids'])}))

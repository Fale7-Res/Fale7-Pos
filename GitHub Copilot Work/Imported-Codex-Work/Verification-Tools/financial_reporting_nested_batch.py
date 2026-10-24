"""Manually reasoned report predicates/projections; identities come from existing Roslyn map."""
import json
from knowledge_store import *

path='src/Falah.Infrastructure/Reports/ReportingQueryService.cs'
source=load_symbols(path)
lines=(ROOT/path).read_text(encoding='utf-8-sig').splitlines(keepends=True)
notes={
255:'Noncancelled order filter with nullable half-open event timestamp bounds before line join.',
257:'Connect OrderItem.OrderId to Order.Id; projection retains frozen name, quantity and line revenue, not current product price.',
260:'Group frozen product name, intentionally coalescing same-name variants/IDs by reporting contract.',
261:'Group DTO computes item quantity and stored line-total sums; no allocation of order-wide discounts, tax or refund.',
262:'Descending quantity ranking, not cash/revenue ranking.',
276:'Noncancelled order timestamp range before sold-product join.',
278:'Join line parent OrderId to order Id, preserving one row per stored line.',
279:'Projection retains line/order/product identities, frozen name/quantity/total and sale timestamp for later peak-hour grouping.',
282:'Current product ID->current category dictionary, not frozen historical category.',
283:'Current category ID->current name dictionary; later recategorization/renaming affects labels.',
285:'Groups sold lines by frozen ProductName, not ProductId.',
286:'Grouped sold-product DTO chooses first nonnull current category, peak Cairo hour by quantity, sums lines and distinct order count. Nonempty group permits First; tied hours depend on source encounter order.',
288:'Resolve group product categories and choose first existing category ID; group may contain multiple real categories under same name.',
289:'Convert sale timestamp to Cairo wall-clock hour; DST rules apply; repeated local hour merges.',
290:'Rank hour groups by summed quantity, not row count or revenue; tie ordering not explicitly deterministic.',
293:'Aggregate quantity, distinct parent order count and line revenue separately; same order multiple lines counts once for order count.',
295:'Descending total line revenue order for daily sold-products output.',
311:'Noncancelled date-range predicate for hourly product facts.',
313:'Join stored line parent identity to order identity.',
314:'Project frozen product name, quantity, line total and timestamp; no payment/refund fields.',
318:'Group by product NAME and converted Cairo hour, with historical item name and current timezone rules.',
319:'Per-product/hour DTO sums quantity and line revenue, not net collected cash.',
320:'Sort lexically by product name then numeric hour; not revenue ranking.',
329:'Noncancelled timestamp window before category query joins.',
331:'Join parent order and keep original order item unchanged.',
332:'Inner join to current Products by ProductId and project current category plus stored line facts. Deleted product removes line from category report.',
335:'Map current category IDs to display names; no historical category snapshot.',
336:'Category grouping uses current CategoryId, unlike frozen-name rollups.',
337:'Resolve current label with ID fallback, sum quantities and stored line totals; no net-revenue allocation.',
338:'Sort categories by descending gross line total.',
353:'Noncancelled order/time predicate for recipe-cost sold lines.',
355:'Join stored line to parent order and project product/variant identity, frozen display name and quantity only; actual price unused for cost.',
362:'Independent noncancelled order range for order-packaging cardinality; separate read can observe different instant than sold lines.',
364:'Group actual orders by stored OrderType, not per item; each order contributes once.',
365:'Project order type and count, determining once-per-order packaging multiplier.',
368:'Read all packaging rules including Active flag; downstream pure selector chooses active applicable rule.',
371:'Current product recipe dictionary supports fallback when variant recipe absent; historical costs can restate by explicit limitation.',
372:'Current variant recipe dictionary gives override only when nonnull; not historical frozen recipe.',
373:'Include current recipe Items and index by recipe ID for line-cost resolution.',
376:'Map inventory ID/name/cost-source enum/positive estimate snapshot. Enum.Parse can throw on corrupt CostSource; not silently zero.',
377:'Map current prep output quantity and Active flag; no costing performed in projection.',
378:'Map current prep ingredient quantities, preserving stored units and recipe association.',
380:'Restrict purchase facts to PurchaseReceipt and nonnull unit cost; downstream LastPurchaseUnitCost separately excludes nonpositive price.',
381:'Project item/unit cost/time for latest purchase-cost selection, no stock-direction or quantity input needed by that selector.',
382:'Build domain purchase facts with signed Money; nullable value safe because prior nonnull predicate, positive-cost guard downstream.',
404:'Convert current recipe ingredient kind, quantity and optional long yield ratio into domain line; invalid enum throws, not silently treated known.',
434:'Significant-action treasury query requires nonnull ApprovedBy and event-time range, but no Posted filter in this method.',
436:'Map approved treasury facts to noteworthy-action DTO; this may overlap an expense action with same underlying operation, so list not unique financial transaction count.',
440:'Select expenses with approver recorded and event-time bounds, independently from treasury.',
444:'Resolve distinct creator IDs for selected expense actions only.',
445:'User lookup restricts requested creator IDs, no current active/role filter.',
446:'Build current creator-ID/name dictionary; deleted creator falls back to ID in next projection.',
447:'Expense action DTO uses current creator name fallback and stored approval/reason/time/amount, preserving source identity.',
452:'Approved stock movement and event-time filter; LossValue nullable remains nullable in action, not invented0.',
454:'Map stock action with stored creator/approval/reason/loss value and item reference, not physical inventory proof.',
458:'Approved delivery event timestamp filter; source uses Approver field rather than treasury ApprovedBy.',
460:'Map delivery event facts to action DTO; amount semantics remain event-specific.',
463:'Sort combined heterogeneous actions newest first; no deduplication or immutable snapshot.',
472:'Fetch shift by exact requested ID, not active shift; missing exception belongs parent method.',
474:'Exclude nullable closer ID before Cast<string>; opener assumed nonnull persisted contract.',
475:'Select matching user IDs and build current names dictionary; stale/deleted user names may remain missing.',
490:'Newest opening time order before Take(take); large caller take unbounded by this method itself.',
494:'Project opener IDs across selected history shifts.',
495:'Add nonnull closer IDs; distinct outside chain prevents repeated user lookup.',
497:'Resolve current names for bounded selected-shift user set, without user activity filter.',
512:'Filter shifts on stored BusinessDate directly; not UTC opening timestamp or BusinessDayId.',
513:'Ascending opening time for daily shift rows.',
516:'Project daily selected shift opener IDs.',
517:'Project and null-filter closer IDs before string cast and distinct.',
519:'Lookup current names for daily shift actor IDs, independent of financial event snapshots.',
534:'Sum expense amounts by exact shift ID via nullable SQL sum fallback0; no creator visibility filter in management summary.',
549:'Delivered event timestamp range; requires DeliveredAt nonnull, independent of current final status.',
550:'Project delivered OrderId list, no distinct; delivery-per-order uniqueness must come from schema.',
552:'Count FailedAt events in range even if lifecycle later changes; not exclusive current failed-status population.',
554:'Count ReturnedAt events in range independently of delivered/failed event counters.',
557:'Orders corresponding to delivered IDs contribute stored delivery fee; nullable sum empty0. No refund cancellation adjustment in this fee metric.',
559:'All currently unsettled custody, not scoped to report date; current-position metric distinct from flows.',
564:'Duration eligibility requires departure+delivery timestamps and delivery-time range.',
566:'Project only duration endpoints, no rounded value in SQL.',
569:'Subtract delivered minus departure as TotalMinutes; null-forgiving safe under preceding nonnull filter.',
570:'Discard negative duration, retaining zero as valid; no claim all historical event clocks reliable.',
574:'Settlement CreatedAt half-open nullable bounds in two Where predicates; not delivery date.',
575:'Project actual settlement and signed difference only.',
579:'Sum current unresolved CollectedPiastres, not original cash-expected sale value.',
581:'Sum actual settlement amounts as money received within period.',
582:'Shortage uses max(0,-Difference); unchecked MinValue negation may wrap before clamp if invalid extreme value stored.',
583:'Overage max(0,Difference) intentionally separate from shortage, no cancellation netting.'
}
review_path=WORK/'Knowledge/Reviews/financial_coordinator'/Path(path+'.json')
review=json.loads(review_path.read_text(encoding='utf8'))
existing={r['stable_id'] for r in review['symbols']}
added=[]
for symbol in source['symbols']:
    if not symbol['isCallable'] or symbol['stable_id'] in existing:
        continue
    assert 'Lambda' in symbol['kind'],symbol
    start,end=symbol['startLine'],symbol['endLine']
    reason=notes[start]
    if start==end:
        raw=lines[start-1].encode('utf-16-le')
        text=raw[(symbol['startColumn']-1)*2:(symbol['endColumn']-1)*2].decode('utf-16-le')
        reason+=' Exact separately identified lambda: '+text
    else:
        reason+=' Multi-line span fully read; nested callables receive separate stable-ID receipts.'
    added.append({
        'stable_id':symbol['stable_id'],'status':'ANALYZED',
        'signature':symbol.get('qualifiedSymbol') or symbol['name'],
        'span':[start,symbol['startColumn'],end,symbol['endColumn']],
        'reasoning':reason,
        'inputs':'Captured query bounds and exact row/group types in enclosing reviewed method.',
        'outputs':'Predicate/join key/projection/sort key/aggregate selector described individually.',
        'side_effects':'No explicit persisted writes; database query translation or in-memory projection according to enclosing materialization boundary.',
        'database':'Enclosing method trace preserved. Static audit executed no query; actual SQL provider translation/plans untested.',
        'trust_boundaries':'These selectors are not actor authorization. Trusted persisted facts and endpoint report permission remain required.',
        'printing_ui':'No native or physical output. DTO current-name/category semantics differ from immutable monetary facts.',
        'performance':'Enumeration/grouping/projection or SQL filter; query materialization/read amplification not benchmarked.',
        'duplicates_conflicts':'Same-line overload/nested identities kept separate; shared source patterns do not make line totals identical to net sales or current cash.',
        'issues_hypotheses':'Packaging completeness candidate and current/historical grouping caveats preserved in parent method reports; no standalone defect invented for projection.',
        'tests_missing':'Query boundaries/cardinality/current category rename/deletion, one packaging per order, nullable guards and SQL/in-memory parity on approved fixtures.',
        'verified_callers_callees':'Actual source241-585 manually read together with enclosing method reasoning. Individual lambda identity tied to original source hash.'
    })
review['symbols'].extend(added)
review['file_review_status']='COMPLETE_STATIC_CALLABLE_REVIEW_WITH_CALLER_AND_RUNTIME_LIMITS'
review['evidence']='All202 original callable bodies/predicates/projections manually source-read and reasoned; individual stable receipts. Runtime/provider/endpoint and independent depth checks remain open.'
saved=save_review('financial_coordinator',path,{k:v for k,v in review.items() if k not in ('remaining_callable_ids','updated_at_utc')})
result=json.loads(saved.read_text(encoding='utf8'))
assert not result['remaining_callable_ids']
print(json.dumps({'added_nested_receipts':len(added),'total_original_receipts':len(result['symbols']),'remaining':0}))

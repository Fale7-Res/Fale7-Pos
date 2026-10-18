"""Persist manually reasoned financial source batch; no application execution."""
import json
from knowledge_store import *

NOTES = {
    'src/Falah.Domain/Inventory/InventoryRules.cs': """
27|Requires nonblank reason and positive adjustment magnitude; deliberately does not replace human shelf count with computed stock. No direction or available-stock check here.
33|Requires reason, positive magnitude and one of Waste/Spoilage/Breakage. Loss exceeding book stock is explicitly allowed so discrepancies remain recordable; do not report that intended policy as overspend.
44|Rejects unit change if movement-history flag true; protects historical bare milliunits from reinterpretation. Flags are caller snapshots, not concurrency exclusion.
50|Sums matching item history with In positive and every other direction negative. Assumes valid positive stored magnitudes; FIN018 negative purchase therefore reduces stock. Sum overflow does not protect prior unary MinValue negation.
51|Restricts stock history to exact inventory ID; no date or lifecycle filter.
52|Converts quantity to signed delta using In versus other direction; does not validate enum or stored sign.
60|Materializes item/date inclusive window then sums separate receipts, usage, losses and signed count adjustment. Receipt/usage/loss buckets trust direction/type consistency. Inclusive upper bound requires caller day-window review before claiming double counting.
63|Item identity and optional inclusive timestamp bounds; reversed range produces empty results, no explicit rejection.
66|Purchase/ManualReceipt filter and quantity projection form receipt bucket. Each lambda has separate stable identity; quantities retain raw stored sign.
67|Usage filter and magnitude projection exclude waste and count adjustment intentionally.
68|LossTypes membership and magnitude projection cover Waste/Spoilage/Breakage only.
69|Restricts count correction bucket to CountAdjustment, keeping it separate from consumption.
70|Signs count-adjustment magnitude by In versus other direction; negative stored magnitudes reverse meaning, command guard required.
83|Adds usage and waste, returns theoretical minus recorded variance and passes count adjustment separately. Negative variance is meaningful, not proof of theft; long addition/subtraction bounds not tested.
100|Consumption is usage+waste; positive days gives rounded double daily average, otherwise null. Variance exists only when standard and actual both present. No invented allocation; extreme arithmetic/negative standard caller contract unverified.
""",
    'src/Falah.Domain/Inventory/CostingRules.cs': """
20|Public overload delegates to recursive ingredient resolver with null resolving set; no separate pricing implementation.
28|Null gives Unknown. Prep source follows first active prep and path-local cycle set, otherwise latest positive real purchase wins over positive estimate. Unknown stays nullable. Negative purchase costs filtered by LastPurchaseUnitCost (FIN018 counterevidence). Repeated collection scans, no memoization.
37|Selects first active prep with matching output; multiple active recipes depend on caller uniqueness, no ordering here.
39|Selects lines for chosen prep ID only; quantity/sign validity not checked in predicate.
62|Breadth-first active prep dependency traversal returns true only on path back to starting item, visited set terminates other cycles. Downstream CostSource is not checked before traversing prep definition, unlike price resolver. Possible structural warning versus actual costing disagreement requires fixture/caller challenge.
73|First active prep matching queued output ID; ignores downstream ingredient cost-source policy.
76|Traverses all input lines for selected prep, regardless of positive quantity or actual Prep-source status.
93|Public prep-cost wrapper seeds resolving set with own output to detect immediate self-reference; null prep passed through to invalid/unknown result.
97|Nonpositive output quantity gives unknown. Accumulates rounded costs only for known ingredients, preserves missing labels/cycle reason and known-line counts. Full nonempty completeness required for output unit cost. Negative recipe quantities need caller guard; Money accumulation may overflow before decimal scaling. No persistence.
108|Resolves prep ingredient by ID; missing item becomes missing label and unknown cost rather than known zero.
136|Non-yield uses declared quantity; Yield requires present positive ratio then rounded double division to long. Ratio is nullable long, so NaN/infinity are not possible; double division precision and quantity/cast bounds require caller validation. Stock and costing must share this conversion.
153|Resolves ingredient, converted quantity and nullable unit cost per dish line; unknown lines skipped but tracked. Separates packaging and food cost; empty recipe returns zero known lines and IsComplete false. Repeated scans and recursive calls not benchmarked.
162|Dish ingredient lookup by ID; missing reference does not silently become a known zero-cost line.
181|First active order-type packaging rule applies once per order. No rule is optional known zero; missing price returns Known false. Rule quantity passed to PurchaseLineTotal. Multiple active-rule invariant belongs configuration caller.
183|Selects first active matching order type; no multiplication by cart item count, preserving order-level packaging.
186|Resolves packaging ingredient by ID; missing ingredient propagates unknown rather than invented price.
197|Nonpositive sale price or incomplete cost yields null profitability metrics. Complete cost gives signed contribution and rounded percentages; negative margin valid. Long subtraction/double precision extreme bounds not tested; menu-level result not actual net sales/refund profit.
""",
    'src/Falah.Domain/Purchasing/PayablesAging.cs': """
21|Maps known aging buckets to Arabic labels, default no-due-date label also catches invalid enum. Classify creates known values; no amount mutation.
33|Absent agreed due date stays null; due today/future gives zero; otherwise positive calendar-day difference. Does not invent overdue status for missing terms.
40|Null classified NoDueDate, zero NotYetDue, positive intervals1-30/31-60/61-90/over90. Exact30/60/90 belongs lower bucket.
52|Calendar age from receipt floored zero for future receipts, independent of agreed due date; bounded DateOnly difference, no timezone or finance write.
"""
}

total = 0
for path, rows in NOTES.items():
    notes = {int(row.split('|', 1)[0]): row.split('|', 1)[1]
             for row in rows.strip().splitlines()}
    source = load_symbols(path)
    lines = (ROOT/path).read_text(encoding='utf-8-sig').splitlines()
    receipts = []
    for symbol in source['symbols']:
        if not symbol['isCallable']:
            continue
        reason = notes[symbol['startLine']]
        if 'Lambda' in symbol['kind'] and symbol['startLine'] == symbol['endLine']:
            raw = lines[symbol['startLine']-1].encode('utf-16-le')
            body = raw[(symbol['startColumn']-1)*2:(symbol['endColumn']-1)*2].decode('utf-16-le')
            reason += ' This separately reviewed lambda: ' + body
        receipts.append({
            'stable_id': symbol['stable_id'], 'status': 'ANALYZED',
            'signature': symbol.get('qualifiedSymbol') or symbol['name'],
            'span': [symbol['startLine'], symbol['startColumn'], symbol['endLine'], symbol['endColumn']],
            'reasoning': reason,
            'inputs': 'Exact declared in-memory facts, amounts and context collections.',
            'outputs': 'Declared pure value/record, predicate, or exception; unknown is distinct from zero.',
            'side_effects': 'Local lists/sets only, no database/server/printer operation.',
            'database': 'None. Freshness, persisted units and command validation belong infrastructure callers.',
            'trust_boundaries': 'Pure calculation does not authenticate actor or authorize financial changes.',
            'printing_ui': 'No native UI/printing; no visual observation claimed.',
            'performance': 'Repeated LINQ scans/allocations and recursion noted per method; not benchmarked.',
            'duplicates_conflicts': 'Quantity conversion must match stock deduction. Structural cycle versus source-aware costing lead unverified.',
            'issues_hypotheses': 'Bounds/cycle/caller issues remain leads, not independently confirmed defects.',
            'tests_missing': 'Unknown/zero costs, circular and shared acyclic graphs, units/rounding/bounds, aging date boundaries.',
            'verified_callers_callees': 'Complete three source files and PurchasingRules read. Unread infrastructure caller contracts not independently certified.'
        })
    saved = save_review('financial_coordinator', path, {
        'file_review_status': 'COMPLETE_STATIC_CALLABLE_REVIEW_WITH_CALLER_LIMITS',
        'evidence': 'Manual complete source read and per-function reasoning; no runtime execution for this batch. IsComplete getter remains additive scope.',
        'symbols': receipts
    })
    assert len(json.loads(saved.read_text(encoding='utf8'))['symbols']) == len(receipts)
    total += len(receipts)
print(json.dumps({'saved_callable_receipts': total, 'source_files': len(NOTES)}))

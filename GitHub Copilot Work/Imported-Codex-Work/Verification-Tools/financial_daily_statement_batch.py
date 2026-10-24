"""Persist complete source reasoning for financial daily-statement composition only."""
from knowledge_store import *
import json

path='src/Falah.Infrastructure/Reports/DailyStatementQueryService.cs'
source=load_symbols(path)
lines=(ROOT/path).read_text(encoding='utf-8-sig').splitlines(keepends=True)
notes={
45:'Constructor stores eight scoped query dependencies; no financial operation or printer interaction occurs during construction.',
60:'Builds one Cairo-calendar range, looks up first matching business-day date and sequentially composes sales/tax/products/expenses/shifts/treasury/employees/inventory/delivery/refunds/audit/status DTOs. No common snapshot transaction, so live sections can observe different instants. Current balances/status deliberately coexist with selected-day flows. Repeated-date day identity FirstOrDefault ambiguous if multiple historical days share date; policy and schema need challenge. No new persisted financial truth.',
65:'First business day matching BusinessDate; no unique-date/order/active-state predicate. Used only header identity/status, not sales calendar filtering.',
92:'Detailed expense list filters CreatedAt half-open time range, resolves current creator names and stored approver/account/shift. Header expense aggregate uses BusinessDate. New AddExpense handler98/110 stamps both from same now so normal-path mismatch hypothesis rejected; imported/legacy inconsistent data remains conditional, not confirmed API defect.',
95:'Expense timestamp half-open interval, including exact start and excluding next midnight.',
96:'Newest expense creation ordering, no second amount transformation.',
97:'Distinct expense creator IDs for batched current-name lookup.',
98:'Users restricted to selected expense creator IDs; build current name dictionary, deleted ID fallback later.',
100:'Expense DTO preserves recorded amount/category/reason/account/shift and approver; creator name current. Does not allocate or post another expense.',
105:'Shift-day total keeps opening/expected/actual and shortage/overage separate. Null expected/actual gives0 for not-yet-closed shift, not live computed balance. Math.Abs MinValue and long sum bounds depend on valid stored money. No aggregate hides signed differences into one net.',
107:'Sum opening float amounts across listed shifts; may count money reused across shifts as operational openings, not net external capital.',
108:'Expected cash selector uses persisted nullable close value or0.',
109:'Actual cash selector uses persisted counted nullable value or0.',
110:'Select negative stored discrepancy and sum absolute shortage; do not subtract overage into shortage.',
111:'Select positive discrepancy and sum overage magnitude separately.',
117:'Loads all movement statuses in calendar timestamp range, newest first, resolves order number references in one batch and returns factual ledger rows. Unlike current balance helper no Posted filter here: list can include nonposted facts if schema permits, not itself monetary aggregate. BusinessDayId deliberately not date filter.',
120:'Movement CreatedAt half-open selected-date filter; no status restriction.',
121:'Newest-first movement list, not operation-id reconciliation order.',
125:'Only nonempty order-type references selected for readable number lookup.',
126:'Distinct order ReferenceId values; avoids per-row query.',
129:'Fetch order identity set for batch display labels, without cancellation filter because historical movement remains factual.',
130:'Build order-ID/OrderNumber dictionary for display only.',
132:'Copy stored ledger identity/direction/amount/type/actors/time and optional resolved number; no recomputed account balance or write.',
141:'All-time Posted movement facts derive current five-account stock, not selected-day net flow. Confirms explicit financial contract versus day reconciliation Treasury delta. Full history materialized and summed five times; no measured SQL cost.',
144:'Only Posted status contributes to current treasury balances.',
145:'Project source/destination/amount required for signed account sum, not day identity.',
146:'Per-account local sum destination inflow minus source outflow over all Posted history. Same-account movement nets0. Long subtraction bounds not tested.',
155:'Gets employee list, selected-date attendance and calendar ledger, then emits eight per-type daily buckets and all-time current balance per employee. One awaited balance query per employee after common reads creates N+1 pattern, not measured. Salary/ExtraShift/OtherEarning/Adjustment have no dedicated daily DTO buckets despite every-type doc wording; preserves source requirements gap, not money-loss claim. Current balance intentionally not historical.',
160:'Attendance DateOnly equals selected business date; not event creation timestamp.',
161:'Attendance dictionary assumes one row per employee/date; unique schema constraint must hold, duplicates throw.',
164:'Employee ledger event timestamps in Cairo half-open interval; no payroll-period filter.',
165:'Group selected ledger rows by employee ID and materialize per-employee lists. No sign normalization.',
172:'Per-type credit sum, deliberately separate from debit; does not net opposing adjustments implicitly.',
173:'Per-type debit sum, used for deductions/payments/advances/settlements. Positive credit Adjustments not represented by these selected fields.',
192:'Selected-day stock facts grouped into receipt/usage/waste/spoilage/breakage and signed count adjustment. Packaging classification uses CURRENT item flags; purchases value comes invoice total, not amount paid. Loss summary day-scoped but low-stock/negative/unknown-cost counts current all-time. Raw milliunits across different units are activity totals, not comparable monetary or physical unit totals; caller labeling policy matters.',
195:'Stock creation timestamps half-open selected calendar range.',
196:'Read stock item/type/direction/quantity facts; quantity signing delegated to adjustment helper only.',
197:'Current packaging item ID set; historical recategorization can affect usage bucket membership.',
199:'Local type/optional packaging filter sums raw magnitude. Direction ignored intentionally for receipt/usage/loss buckets; invalid negative receipt FIN018 remains visible.',
200:'Matches movement Type string and optional current packaging-membership requirement.',
201:'Magnitude sum, not a conversion between kg/litre/piece; different unit items remain aggregated activity only.',
202:'Count-adjustment type filter isolates physical corrections from consumption totals.',
203:'Sign adjustment by In versus other direction, trusting positive magnitude invariant.',
207:'Purchases selected by creation timestamp; no payment-status filter since report value is received invoices.',
208:'Sum invoice TotalPiastres, empty SQL sum0; not paid cash or supplier outstanding.',
224:'Fetches at most500 general and500 control audit entries in range, projects source discriminator, merges/sorts. This is bounded display list, not proof exhaustive day audit trail. Underlying query truncation/inclusivity and UI disclosure need review; no log table merged/mutated.',
230:'Project general audit identity/action/user/category/details/severity/reason/approval/time with source tag.',
233:'Project control audit identity/action/actor/entity/optional amount/reason/approval/time with source tag.',
237:'Combined newest-first ordering retains possible duplicate representations of one operation from separate sources.',
240:'Compose current pending/failed/stuck print overview plus selected-day reprint audit count. No ticket sent or queue state changed by this query; not a printing-reliability audit completion.',
244:'Count order reprint action events in half-open selected time range, independent of actual paper success.',
248:'Read backup-history metadata, choose latest succeeded verified-valid backup with nonnull size, calculate current age; count selected-day backup requests/failures separately. Does not check file existence/content now; last verified metadata is historical fact, not a fresh RESTORE VERIFYONLY result. No backup/restore/pruning invoked.',
251:'Restrict metadata to Kind Backup; restore rows excluded from daily backup counters.',
252:'Succeeded+Valid+nonnull file-size metadata candidate; no fresh file or SQL verification.',
253:'Latest CompletedAt selection among verified metadata, not requested time.',
256:'Selected day backup REQUESTED time half-open range; completion may be on another day by design.',
260:'Failure count based exact Failed status; pending requests contribute total count but not failed.'
}
receipts=[]
for symbol in source['symbols']:
    if not symbol['isCallable']:continue
    start,end=symbol['startLine'],symbol['endLine'];reason=notes[start]
    if 'Lambda' in symbol['kind'] and start==end:
        raw=lines[start-1].encode('utf-16-le')
        reason+=' Exact distinct lambda: '+raw[(symbol['startColumn']-1)*2:(symbol['endColumn']-1)*2].decode('utf-16-le')
    receipts.append({
        'stable_id':symbol['stable_id'],'status':'ANALYZED',
        'signature':symbol.get('qualifiedSymbol') or symbol['name'],
        'span':[start,symbol['startColumn'],end,symbol['endColumn']],
        'reasoning':reason,
        'inputs':'Declared selected date/UTC range, scoped queries, groups/rows and cancellation token.',
        'outputs':'Read-only statement DTO or exact query projection/predicate/sum.',
        'side_effects':'Local lists/dictionaries only; source review invoked no report query, account transaction, backup or printing.',
        'database':'Composition of multiple EF reads and shared query services, without common immutable report snapshot.',
        'trust_boundaries':'Access permission belongs endpoint. Persisted monetary facts are source; current balance/status intentionally not historical reconstruction.',
        'printing_ui':'Status aggregation only; no native observation, rendering, printer job or nonfinancial specialist resumed.',
        'performance':'Sequential query fan-out and per-employee balance awaits documented; no benchmark or general performance investigation.',
        'duplicates_conflicts':'All-time account balance agrees with OwnerDashboard current balance, differs intentionally from business-day net movement view. Expense date/time difference ruled normal-path consistent by actual creator.',
        'issues_hypotheses':'Daily employee enum coverage/doc gap and bounded audit display need policy; no invented direct money loss or fresh backup-validation guarantee.',
        'tests_missing':'Historical date versus current position, expense date/time invariant, missing/deleted users, all12ledger types, audit over500 entries, invoice vs paid value, same-account movement, selected-day backup requested vs completed.',
        'verified_callers_callees':'Complete daily-statement source read; ReportingQueryService and AddExpense98/110 counterevidence read, Employee enum and DailyStatement DTO eight-bucket shape checked. Deep child services not all certified in this batch.'
    })
saved=save_review('financial_coordinator',path,{
    'file_review_status':'COMPLETE_STATIC_CALLABLE_REVIEW_WITH_COMPOSITION_LIMITS',
    'evidence':'Actual whole source and all71 exact callables reasoned, with financial counterevidence. No runtime query/test.',
    'symbols':receipts
})
assert not json.loads(saved.read_text(encoding='utf8'))['remaining_callable_ids']
atomic_json(WORK/'Deep-Audit/Database-and-Finance/daily-statement-financial-scope-review.json',{
    'at_utc':now(),'baseline':BASELINE,'path':path,'source_hash':source['worktree_sha256'],
    'callable_receipts':len(receipts),'runtime_executed':False,
    'source_supported_contracts':[
        'Current treasury all-time Posted stock intentionally separate from selected-day movement list.',
        'Employee running balance current; report flow buckets selected-date only.',
        'Expenses header calendar business-date versus details creation time normally equivalent because AddExpense creates both from same now98/110; backdated API-input hypothesis rejected.'
    ],
    'requirements_questions':[
        'Daily employee DTO covers8/12enum types; Salary/ExtraShift/OtherEarning/Adjustment absent despite every-type comment. Other employee financial report has broader grouping. Not a confirmed monetary-loss defect.',
        'Audit list capped500per source may omit earlier selected-day records; display truncation disclosure/paging contract not checked.'
    ],
    'next':'Coordinator financial ApiClient mirror comparison and remaining owned database/report functions. Independent five candidate challenges unavailable; preserve explicit pending status.'
})
print(json.dumps({'saved_callable_receipts':len(receipts),'remaining':0}))

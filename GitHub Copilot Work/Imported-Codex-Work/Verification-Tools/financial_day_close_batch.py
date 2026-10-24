"""Persist actual coordinator source review of business-day close/evaluation."""
from knowledge_store import *
import json

notes={
'src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs':{
27:'Constructor retains EF context, approval registry and reporting service; does not evaluate or close anything.',
34:'Replays request-derived snapshot before actor check. Authorizes business_day_close or issued approval, loads day and validates legal Closing/Closed transitions on pretransaction status. Begins transaction then logs start/evaluates blockers. Blocked attempt commits blocked audit then throws without changing day. Successful close reads reconciliation/exceptions/checklist, counts revision, demotes old snapshots via ExecuteUpdate, inserts authoritative snapshot and sets Closed metadata atomically. No transaction-held day admission lock/state re-read; paused legitimate financial requests can post after close (separate HIGH candidate). Distinct concurrent close/revision behavior needs independent challenge; same snapshot-key DB conflict rolls back then replays.',
37:'Replay snapshot ID is deterministic from request ID; does not additionally compare requested day/actor. Historical replay returns before fresh permissions.',
40:'Actor database ID lookup; later domain policy validates permission, session binding external.',
47:'Optional approver resolved by existing user then actual policy validates issued token only if fallback needed. Caller records authorization.Approver, not unnecessary presentation.',
54:'Tracked requested business-day lookup happens before transaction; validates status snapshot outside financial close critical section.',
80:'Serialize blocker Codes in blocked-attempt audit; no business financial change.',
84:'Project blocker DTOs for thrown exception after blocked audit transaction committed; no accidental success response.',
89:'Read checklist completion facts for exact day snapshot; no fresh required-item policy redefinition here.',
91:'Revision counts existing snapshots for day; concurrent count/insert not independently serialized by this expression.',
92:'Target prior snapshots of day for authoritative demotion, using ExecuteUpdate within transaction.',
93:'Set Authoritative=false on matching snapshots. Both property-selector and setter lambda identities retained; operation bypasses tracked-state refresh, appropriate because snapshots no-tracking here.',
102:'Frozen checklist JSON copies item ID/completed/time/actor only; serialized within closing snapshot, not mutable reference.',
125:'After DbUpdateException rollback, lookup same deterministic snapshot ID; distinct conflicting close request is not treated as same-key successful replay.',
136:'Exception builder uses calendar-day closed shift discrepancies, day-ID refunded orders/expenses, global unresolved custody/journal plus discrepancy threshold. Delegates pure ControlCenterRules. It is not blocker evaluation itself; global-versus-day scope explicitly documented. No DB write.',
139:'Closed shifts on same BusinessDate with nonnull cash difference; no exact business-day ID relation.',
140:'Projects shift identity and safe nonnull discrepancy value; source guard above establishes presence.',
142:'Refunded original sale day-ID and positive accumulated refund amount, not event timestamp/current operational shift.',
143:'Projects order identity and frozen accumulated refund magnitude.',
145:'Expenses exact day-ID and amount projection; type/creator irrelevant to exceptions threshold input.',
147:'Global unsettled custody IDs, not only target day; unresolved earlier obligations retained.',
149:'Global journal entries not Complete; no actual active-writer exclusion after read.',
153:'Convert shift discrepancy facts to pure exception input tuple, preserving signed difference.',
155:'Convert refunded order facts to exception input tuple; does not post a new refund.',
156:'Expense amount facts to exception input tuple, not duplicated expenditure.',
162:'Snapshot DTO preserves authoritative/revision/previous snapshot lineage and frozen JSON. No deserialization or correction of monetary content in mapping.'},
'src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs':{
26:'Collects all shifts/custodies/journals and pending electronic IDs globally, required active Closing checklist plus day completions, then account/shift reconciliation checks. Passes empty trip list and hardcoded persistence/writer=true by documented SQL-vs-browser rationale. DB query success proves reachability, not absence of concurrent writers. Snapshot facts do not form transaction-held financial admission gate; closed-day race candidate remains. Pure domain builds all blockers.',
28:'Project all shift IDs/statuses, no target-day filter. Any open historical shift can block current close by documented system-wide policy.',
29:'Project all custody IDs/statuses for unresolved obligation gating.',
30:'Project all journal keys/statuses; pending admitted command before journal creation remains invisible.',
32:'Select pending electronic orders by stored status and project ID globally; no order cancellation filter at this query, data-state policy separate.',
35:'Required checklist restricted to active Closing items, excluding optional/opening items.',
36:'Checklist completions restricted to target day ID; does not fabricate completed state.',
38:'For each required item, Any matching completed fact yields Completed true; duplicate completion policy/schema outside this lambda.',
44:'Transform shift status Closed boolean; unknown status treated not closed, conservatively blocking.',
46:'Transform custody Settled status boolean; unknown/unsettled conservatively blocked.',
47:'Transform journal Complete equality; unknown status considered incomplete.',
48:'Each pending electronic ID maps true because SQL predicate already selected pending state.',
62:'Checks duplicate treasury idempotency groups, derives current protected-account balances from all Posted movements and flags negative accounts. Uses day-date shift discrepancy and missing DifferenceAuthorizedBy for threshold failures. Pure checks report anomaly, do not reconcile money or lock writers. Selected day is used for drawer discrepancy only, global treasury intentionally current. Guard after sequential reads cannot prevent later write admission.',
67:'Group treasury keys and detect Count>1. Unique index should normally prevent duplicate values; this is defensive detection, not itself idempotency implementation.',
71:'Posted movement status predicate; excludes pending/cancelled movement records from account position.',
72:'Project only source/destination/amount for current balance calculation, no day scope.',
75:'Signed account delta is destination amount minus source amount; sums existing Posted records. No opening day cutoff; contrasts with day reconciliation flow.',
81:'Day-date shifts with nonnull cash discrepancy and null authorizer; legitimate direct-permission high difference without name cannot persist via Domain Shift.Close106, so do not invent a false historical-null branch.',
82:'Strictly outside positive/negative threshold flags unauthorized mismatch. Requires valid nonnegative threshold contract; negative threshold not independently tested.'}
}
total=0
for path,meaning in notes.items():
    source=load_symbols(path); lines=(ROOT/path).read_text(encoding='utf-8-sig').splitlines(keepends=True)
    receipts=[]
    for symbol in source['symbols']:
        if not symbol['isCallable']:continue
        start,end=symbol['startLine'],symbol['endLine'];reason=meaning[start]
        if 'Lambda' in symbol['kind'] and start==end:
            raw=lines[start-1].encode('utf-16-le')
            reason+=' Exact lambda: '+raw[(symbol['startColumn']-1)*2:(symbol['endColumn']-1)*2].decode('utf-16-le')
        receipts.append({
            'stable_id':symbol['stable_id'],'status':'ANALYZED',
            'signature':symbol.get('qualifiedSymbol') or symbol['name'],
            'span':[start,symbol['startColumn'],end,symbol['endColumn']],
            'reasoning':reason,
            'inputs':'Declared day/request/context/ct and captured persisted facts.',
            'outputs':'Snapshot DTO, pure exception/blocker inputs, query predicate/projection or domain error.',
            'side_effects':'Close handler in production writes day/snapshot/control audit; evaluator/exception builder read only. Audit executed neither.',
            'database':'Transaction and pretransaction reads, set-based authoritative demotion, multi-query fact snapshots explicitly analyzed. No actual DB operation in review.',
            'trust_boundaries':'Authorized close and issued approval policy retained; business-day/financial writer admission not supplied by mere SQL transaction existence.',
            'printing_ui':'No physical printing or native UI. Frozen JSON snapshot is financial reporting artifact, not a new ledger.',
            'performance':'Global histories loaded plus checklist/reconciliation queries; no SQL plan, isolation benchmark or deadlock test.',
            'duplicates_conflicts':'Domain and infrastructure checks reuse pure blockers. Current global treasury stock versus per-day flow intentional distinction pending caller naming policy.',
            'issues_hypotheses':'HIGH closed-day admission candidate saved; independent challenge and actual SQL timeline unexecuted. Concurrent distinct close revision/authoritative uniqueness is a preserved unverified lead.',
            'tests_missing':'Paused deposit then close then resume, duplicate/same/different close keys, blocked-audit commit, snapshot revision/authoritative uniqueness, global prior-day unresolved obligations.',
            'verified_callers_callees':'Complete close/evaluator and Domain blockers source read; deposit/gate/reconciliation shared source traced. Domain Shift.Close103-115 counterevidence checked.'
        })
    saved=save_review('financial_coordinator',path,{
        'file_review_status':'COMPLETE_STATIC_CALLABLE_REVIEW_WITH_CONCURRENCY_LIMITS',
        'evidence':'Actual complete source and each callable reviewed; no financial operation run.',
        'symbols':receipts
    })
    assert not json.loads(saved.read_text(encoding='utf8'))['remaining_callable_ids']
    total+=len(receipts)
print(json.dumps({'saved_callable_receipts':total,'source_files':len(notes)}))

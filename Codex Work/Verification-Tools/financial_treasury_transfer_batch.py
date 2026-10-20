"""Treasury source review and bounded safe-drop/transfer semantic comparison."""
from knowledge_store import *
notes={
'src/Falah.Infrastructure/Treasury/SafeDropHandler.cs':{
26:'Stores context/approval/audit dependencies without acquiring account lock or writing money.',
33:'Returns existing movement by request-key before local actor/input revalidation; endpoint authentication remains required. New requests require positive amount/reason, existing actor and shift, safe_drop permission or validated matching approval. RequireOpen occurs BEFORE transaction: same day-admission TOCTOU family as deposit candidate, not a fresh independent reproduction. Crucially drawer balance lock IS INSIDE transaction, counterevidence against FIN011 expansion. Posts one Drawer->Main movement, completes journal and appends audit in same SaveChanges/commit. On key/lock conflict rollback then bounded replay lookup; same key does not check payload equality. No SQL execution here.',
42:'Resolve creator by ID and map stored active/permissions to actor; supplied name/permission is not taken directly from request.',
45:'Shift existence only, deliberately not Open requirement per explicit handler policy. Do not invent a new closed-shift bug solely from this choice.',
51:'Optional supplied approver ID must resolve existing user, then authorization policy validates issued token only when actor lacks direct permission. Direct permission ignores unnecessary supplied presentation and attribution uses authorization.Approver rather than raw approver name.',
109:'Retry lookup captures same idempotency key/cancellation after rollback. Does not create a new request ID or spend again inside helper.',
119:'NoTracking first movement by unique idempotency key; no actor/key payload comparison at replay layer.',
120:'Match persisted movement.IdempotencyKey exactly. Model uniqueness prevents two committed same-key movement rows; no distinct-request balance serialization implied.',
122:'Return committed/replayed movement ID/amount and replay flag; no recomputation or additional money side effect.'},
'src/Falah.Infrastructure/Treasury/TransferHandler.cs':{
22:'Stores context/approval/audit dependencies, no treasury query or transfer during construction.',
29:'Replay first by stable prefixed key. New input requires distinct known source/destination, positive amount/reason and stored actor; treasury_manage permission or matching approval. Day gate precedes transaction, so stale admitted day risk remains. Account UPDLOCK/HOLDLOCK and protected balance check occur INSIDE transaction. Adds Posted transfer plus movement-linked CashHandover, journal completion and audit atomically in one SaveChanges/commit. ReceivedBy falls back actor when own permission authorizes; not proof physical second-person custody. On conflict rollback and same-key replay/bounded lock wait. Payload-key mismatch not checked on replay.',
41:'Creator user lookup from persisted identity before authorization; caller may not substitute arbitrary permission DTO.',
48:'Optional approver resolves existing stored user; AuthorizeCommand returns null Approver for direct permission so raw presentation does not forge recorded approver here.',
111:'After rollback await same-key movement lookup; bounded sibling commit wait only, not generic repeated financial execution.',
121:'Find committed movement by idempotency key NoTracking, independent of request amount/account comparison.',
122:'Exact key equality for replay; model unique key prevents duplicate-key commit but not distinct-key overdraft without account lock.',
124:'Return persisted transfer ID, amount, source/destination and replay Boolean; null-forgiving accounts rely transfer writer invariant, not runtime validation on result.'},
'src/Falah.Infrastructure/Treasury/BalanceLockContention.cs':{
29:'Classifies first nested SqlException Number1205 or1222 only. Other SQL connectivity/timeout/provider codes are not declared safe replay contention; does not catch every database failure.',
32:'Walks InnerException chain until first SqlException, returns null if none. Does not traverse AggregateException siblings or inspect all SqlError collection numbers. Bounded by ordinary finite exception chain; no DB activity.',
46:'Performs at most five waits40,80,120,160,200ms totaling600ms plus lookup latency. Returns first nonnull committed sibling or null. Cancellation propagates through Delay/lookup. Comment same-ID sibling is an assumption: distinct request lock conflict also returns null/retry, not fabricated replay. No transaction or write here.'}}
added=0;refs=[]
for path,mapping in notes.items():
 source=load_symbols(path);receipts=[]
 for s in source['symbols']:
  if not s['isCallable']:continue
  line=s['startLine'];assert line in mapping,(path,line)
  receipts.append({'stable_id':s['stable_id'],'status':'ANALYZED','signature':s.get('qualifiedSymbol') or s['name'],'span':[line,s['startColumn'],s['endLine'],s['endColumn']],
   'reasoning':mapping[line],'inputs':'Declared treasury request/actor/approval/key or exception/cancellation/lookup; no real financial records inspected.',
   'outputs':'Persisted movement result or exact lookup/classification/retry result described above.',
   'side_effects':'Handlers production path posts movement/journal/audit and transfer handover in transaction; this source audit invokes none. Contention helper waits/read lookups only.',
   'trust_boundaries':'Permission-or-issued-approval policy, authenticated endpoint external, caller stable request ID versus persisted unique key. Replay is not fresh authorization proof.',
   'database':'SQL Server protected source lock correctly inside caller transaction here. Day admission outside transaction remains separate invariant; no live SQL verified.',
   'printing_ui':'Returns financial UI operation result; no physical drawer/receipt dispatch in these files.',
   'performance':'Posted account history lock query can contend; bounded lookup retry600ms plus DB latency, not guaranteed one-second wallclock. No load tests.',
   'duplicates_conflicts':'SafeDrop always Drawer->Main with existing shift and safe_drop; Transfer known distinct accounts, treasury_manage, null shift and extra handover row. Shared replay/locking scaffolding intentional, business policies must not be flattened.',
   'issues_hypotheses':mapping[line],'tests_missing':'Distinct-key spend concurrency; same-key response loss and changed payload; day close between gate and transaction; rollback/approval semantics;1205/1222 nested exceptions and cancellation; handover rollback.',
   'verified_callers_callees':'Entire two handler files and contention helper read; TreasuryBalanceQuery SQL, FinancialRules protected-account guard, AuthorizationPolicy AuthorizeCommand and Posted movement factory cross-referenced. No DB or endpoint executed.'})
 save_review('financial_coordinator',path,{'symbols':receipts,'scope':'Complete static callable review with replay/admission/runtime limits.'})
 added+=len(receipts)
 refs.append({'path':path,'worktree_sha256':source['worktree_sha256'],'git_blob_sha256':source['git_blob_sha256'],'reviewed_callable_ids':[r['stable_id'] for r in receipts]})
atomic_json(WORK/'Deep-Audit/Duplication-and-Architecture/treasury-safe-drop-transfer-comparison.json',{'at_utc':now(),'baseline':BASELINE,'classification':'BOUNDED_SEMANTIC_FINANCIAL_COMPARISON_NOT_GLOBAL_DUPLICATION_COMPLETION','source_references':refs,'shared_behavior':['Stable-key first replay; positive amount and reason; permission-or-approval; pretransaction day gate; transaction-scoped protected source lock; journal plus movement plus audit atomic save; conflict rollback and bounded same-key replay.'],'intentional_differences':['SafeDrop fixed Drawer->Main with existing shift and safe_drop permission.','Transfer distinct known source/destination, treasury_manage permission, no shift, extra movement-linked CashHandover and fallback ReceivedBy actor.'],'financial_counterevidence':['Both protected balance checks inside transaction: do not expand FIN011 to every treasury writer.','Direct permission ignores raw approval presenter; uses returned validated authority attribution, unlike FIN017 path.','Both still gate day before transaction, extension of existing closed-day admission source candidate, not SQL reproduction.'],'recommendation':'Consider shared transaction/replay infrastructure only after explicit tests preserve different account/permission/shift/handover policies; no code changes implemented.','runtime_tests':'NOT_EXECUTED','independent_challenge':'PENDING for this root comparison; original registry FIN011 source challenged separately.'})
print(json.dumps({'saved_original_callable_receipts':added}))

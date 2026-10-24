"""Persist actual manual and wage/overtime/extra-shift command analysis; audit only."""
from knowledge_store import *
notes={
'src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs':{
27:'Stores context and approval registry only; no wage posted at construction.',
33:'Authorize employees_ledger from actual user or matching approval, require employee exists. Wage is signed custom long or stored DailyWage, no positive guard in this handler before Credit posting. Deterministic employee/current-Cairo-date/shift key, duplicate throws explicit daily wage error; date can change across midnight retry and ShiftId not existence checked here. No explicit transaction/employee lock around Poster balance then SaveChanges, so distinct credit cached snapshots can race; same key PK prevents two committed rows.',
35:'Stored actor exact identity lookup; missing user throws before permission policy.',
40:'Optional approver exact lookup; issued employees_ledger token validated by policy only if direct permission absent.',
47:'Employee existence lookup supplies stored DailyWage; no active lifecycle gate or wage positive check in this query.',
71:'Stores overtime context/approval dependencies, no worktime calculation or ledger write.',
77:'Hours<=0 rejected; positive signed custom/settings rate multiplied by double Hours then Math.Round default ToEven and cast to long. No finite/product bound or positive rate guard here. DTO JSON normally rejects NaN/Infinity literals, so do not claim default HTTP accepts them. Deterministic RequestId entry, duplicate returns stored fact; concurrent same-key SaveChanges race not caught locally. No explicit employee existence query/anchor transaction in this path, model FK/caller policy needed.',
80:'Resolve overtime actor stored ID and authorize through subsequent policy; not client role trust.',
85:'Resolve optional approver then employees_ledger policy; no unrelated token action accepted.',
104:'If Poster found duplicate, read exact deterministic ledger ID and return prior DTO. FirstAsync fails if only Local duplicate unsaved in same context; normal one command per request assumption. Amount/payload match not checked.',
119:'Stores context and approval registry for later extra-shift command; no employee query or ledger posting during construction.',
125:'Require ShiftCount>0 and authorize user/approval, require employee exists. Uses ShiftValue if positive else DailyWage, multiplies signed Money by int count unchecked. No total amount positive/overflow guard. RequestId deterministic credit, duplicate DTO read, no local concurrent-PK catch or explicit employee lock/transaction. Distinct rows can have stale cached BalanceAfter; actual fact sum remains separate.',
128:'Extra-shift actor exists from persisted ID before policy.',
133:'Optional approver existence; issued token/permission handled centrally.',
140:'Employee row read supplies ShiftValue and DailyWage fallback, active status not filtered.',
152:'Existing deterministic extra-shift ledger row replay read, does not compare employee/count/notes payload.'},
'src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs':{
22:'Stores injected context/approval registry only, no manual adjustment or money movement.',
28:'Parse enum case-insensitively and canonicalize, positive amount/reason and actual actor/employee, permission-or-issued approval. Enum.TryParse without IsDefined can admit numeric undefined values; category consumers may omit them while balance includes credit/debit. Computes current balance before implicit SaveChanges transaction with no employee anchor/range lock, then random-GUID ledger fact. DTO explicitly documents no RequestId; this is a disclosed idempotency gap/requirement, not an undiscovered endpoint bypass. Manual IsCredit independent Type means business classification relies admin intent; does not post treasury cash movement.',
42:'Manual actor exact stored ID, not arbitrary supplied role.',
46:'Employee existence only, no active lifecycle or financial balance sufficiency gate because administrative credit/debit adjustments are allowed.',
52:'Resolve optional approver and centralized employees_ledger validation; raw presented authority not blindly persisted.',
60:'Filter ledger balance query to requested employee; no UPDLOCK/HOLDLOCK or explicit transaction around following insert.',
61:'SQL sum credit-debit for current stock; database BIGINT overflow may error rather than unchecked local wrap. Following cached BalanceAfter can omit concurrent distinct adjustment even when both immutable facts commit.'}}
count=0
for path,mapping in notes.items():
 source=load_symbols(path);records=[]
 for s in source['symbols']:
  if not s['isCallable']:continue
  line=s['startLine'];assert line in mapping,(path,line)
  records.append({'stable_id':s['stable_id'],'status':'ANALYZED','signature':s.get('qualifiedSymbol') or s['name'],'span':[line,s['startColumn'],s['endLine'],s['endColumn']],
   'reasoning':mapping[line],'inputs':'Actual typed administrative request and persisted actor/employee/settings sources; audit reads source schema only.',
   'outputs':'Unsaved then persisted employee credit/debit DTO or exact user/ledger lookup.',
   'side_effects':'Production handler saves ledger and possibly tracked consumed approval; no treasury transfer for these earnings/manual facts. Audit invokes no command.',
   'trust_boundaries':'employees_ledger permission or issued action approval required; amount/type/rate validation separate from having permission.',
   'database':'SaveChanges implicit transaction protects save batch, not earlier balance queries; deterministic PK handles duplicates only if caller handles resulting exception. No live SQL or record creation.',
   'printing_ui':'Employee financial source behavior only; no visual inspection/native automation or printing.',
   'performance':'Unpaged balance scans and concurrent snapshot dependencies documented; no CPU/lock benchmark.',
   'duplicates_conflicts':'Three credit paths share actor/approval/Poster flow but daily duplicate intentionally errors while overtime/extra replay. Manual bypasses Poster and deterministic identity deliberately per DTO comment; consolidation must preserve explicit policy.',
   'issues_hypotheses':mapping[line],'tests_missing':'Negative custom/stored wages/rates, midpoint overtime rounding and huge product bounds, same-key concurrent credit, lost-response manual duplicate contract, undefined numeric enum, distinct-key cached running balance races.',
   'verified_callers_callees':'Whole two handler files, EmployeeLedgerPoster, typed EmployeeRequests/AttendanceRequests and manual endpoint709-720 read; no default JSON nonfinite acceptance or runtime behavior fabricated.'})
 save_review('financial_coordinator',path,{'symbols':records,'scope':'All actual callables statically reasoned; lead evidence preserved separately, no independent/runtime certification.'});count+=len(records)
p=WORK/'Deep-Audit/Database-and-Finance/employee-credit-admission-source-leads.json';j=json.loads(p.read_text(encoding='utf8'))
j['updated_at_utc']=now();j['source_contract_counterevidence']=['EmployeeRequests34-45 explicitly documents manual entry lacks RequestId and recommends keyed advance/withdrawal for deduplication. Preserve as disclosed requirements/recovery gap, not claimed hidden behavior.','Default JSON double binding rejects nonfinite numeric literals; pure handler double acceptance alone is not full HTTP NaN exploit.','Deterministic daily duplicate intentionally errors while overtime/extra replay; concurrent same-key PK conflict still may surface because handlers lack catch.']
j['request_and_endpoint_refs']=[{'path':p,'worktree_sha256':load_symbols(p)['worktree_sha256'],'lines':span} for p,span in [('src/Falah.Application/Employees/EmployeeRequests.cs',[34,45]),('src/Falah.Application/Employees/AttendanceRequests.cs',[29,47]),('src/Falah.Server/Program.cs',[709,720])]]
j['next_exact_action']='Read remaining attendance/correction and employee setting validation source to bound wage/credit snapshot leads; independent review and safe isolated tests still outstanding. Do not mark a static lead runtime-confirmed.'
atomic_json(p,j)
print(json.dumps({'new_original_callable_receipts':count}))

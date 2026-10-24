"""Standalone staff payment and shared ledger helper source review."""
from knowledge_store import *
notes={
'src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs':{
39:'Store dependencies only, no payroll or standalone payment begins in constructor.',
46:'Validate allowed Main/Bank/InstaPay account then deterministic entry/movement keys and replay. New path positive amount, stored actor/employee, pretransaction day gate. Inside transaction lock employee anchor row first, repeat replay, authorize employees_ledger, lock ledger range and validate employee amount, then lock source treasury and validate funds. Add Posted external payment/Settlement debit/journal/audit atomically. This counters blanket claim standalone different-account employee payments lack employee serialization. Day admission remains pretransaction; no Active employee guard here is a settlement policy question, not permission bypass.',
54:'Local replay reads deterministic employee entry then matching movement. Returns cached balance and movement ID or empty if ledger exists without movement; atomic writer normally creates both, corrupted/legacy partial facts not independently repaired. Replay does not revalidate payload/employee/actor but endpoint authentication separate.',
56:'Exact deterministic ledger ID lookup, not employee-bound request comparison.',
58:'Exact prefixed movement idempotency key lookup; shared transaction normally establishes pair, no unique relation enforced by this query itself.',
67:'Resolve actor from stored Users before financial authorization; no direct request permissions trusted.',
71:'Resolve employee existence before transaction, not lifecycle active validation. Locked row later Single detects deletion before posting.',
83:'Employee anchor row ID projection under UPDLOCK/ROWLOCK/HOLDLOCK inside transaction serializes first-payment even if no ledger rows exist. Parameters supplied by EF formatting, not concatenated SQL ID.',
89:'Resolve optional stored approver for action employees_ledger. Direct actor permission returns no approver; otherwise registry validates token and consumes within payment transaction.',
101:'Project credit/debit ledger under employee range lock. Money balance sums per-row credit-debit, unchecked inner subtraction versus LINQ sum bounds; valid ledger normally one side positive.',
103:'Sum locked employee credits minus debits before AssertPaymentWithinBalance; exact eligibility protected by employee and ledger locks here, no runtime deadlock/SQL plan proof.'},
'src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs':{
15:'Read persisted employee credits/debits and add tracked Added rows delta so multiple unsaved appends have correct running snapshot. Does not lock employee/range or start transaction itself. Caller owns serialization; cached BalanceAfter can be stale across independently concurrent unlocked writers even if actual ledger fact sum remains correct. Need trace all writers before promoting new finding.',
17:'Select persisted employee identity only, independent current-active or date filter.',
18:'Project persisted credit/debit required for balance, ignoring cached BalanceAfter.',
23:'Include only ChangeTracker Added employee entries; already-saved Unchanged rows excluded to avoid double count. Modified/Deleted adjustments not modeled by append-only intent.',
24:'Pending added delta credit-debit, no insertion or SaveChanges; arithmetic inside selector may overflow at extreme values.',
25:'Persisted row sum credit-debit plus pendingDelta to Money. No locked/common report snapshot by helper alone; transaction/row locking belongs each command.',
31:'Dedup deterministic ID across local tracked entries and persisted DB, then calculate current plus credit minus debit, append full fact with cached BalanceAfter/current UTC timestamp and return unsaved entity. No SaveChanges, explicit transaction, debit/credit nonnegative/type validation or employee FK check in helper. PK ultimately resolves concurrent same-ID insert; callers must handle exception/replay and maintain amount policy.',
36:'Local tracked ledger identity equality prevents double append in one context; includes tracked rows of all states, so deletion/reinsert unsupported as expected append-only model.',
37:'DB Any exact ID check is preinsert observation, not atomic reservation. Unique primary key plus caller handling required for concurrent same-key requests.'}}
total=0
for path,mapping in notes.items():
 source=load_symbols(path);records=[]
 for s in source['symbols']:
  if not s['isCallable']:continue
  a=s['startLine'];assert a in mapping,(path,a)
  records.append({'stable_id':s['stable_id'],'status':'ANALYZED','signature':s.get('qualifiedSymbol') or s['name'],'span':[a,s['startColumn'],s['endLine'],s['endColumn']],
   'reasoning':mapping[a],'inputs':'Financial request/identity/key or append facts and scoped EF tracker; no actual employee record values inspected.',
   'outputs':'Standalone payment result, current balance, unsaved ledger fact or precise selector.',
   'side_effects':'Production payment writes ledger+treasury+journal+audit transactionally; helper stages Added entity without SaveChanges. Audit executes none.',
   'trust_boundaries':'Authenticated endpoint and employees_ledger permission/issued approval versus persisted financial identities; helper is trusted internal plumbing not self-authorizing.',
   'database':'Employee anchor/range and account locks inside standalone transaction; helper itself no lock. Replay uniqueness/pairing and rollback behavior described individually.',
   'printing_ui':'Financial result only; no physical cash movement, UI interaction, drawer opening or receipt dispatch performed.',
   'performance':'History sums under serializing locks may contend, bounded retry after rollback; no load benchmark executed.',
   'duplicates_conflicts':'Standalone employee payment intentionally shares employee-anchor/range discipline with cash withdrawal and account policy with payroll; does not require a payroll run. Preserve distinctions in consolidation.',
   'issues_hypotheses':mapping[a],'tests_missing':'Concurrent same employee across distinct payment accounts, zero-ledger first payment, same key changed payload, atomic ledger/movement rollback, unsaved multiappend and already-saved tracker rows, concurrent unlocked credit BalanceAfter snapshots.',
   'verified_callers_callees':'Whole PayEmployeeHandler and EmployeeLedgerPoster source read; EmployeeRules and treasury lock/Posted factory/approval policy traced. Other writer rg hits are discovery only, not certified reviews.'})
 save_review('financial_coordinator',path,{'symbols':records,'scope':'Complete static per-callable reasoning, no integration execution or independent reviewer claim.'});total+=len(records)
atomic_json(WORK/'Deep-Audit/Database-and-Finance/employee-posting-concurrency-source-notes.json',{'at_utc':now(),'baseline':BASELINE,'source_references':[{'path':p,'worktree_sha256':load_symbols(p)['worktree_sha256'],'callable_ids':[s['stable_id'] for s in load_symbols(p)['symbols'] if s['isCallable']]} for p in notes],'counterevidence':'Standalone employee pay locks Employees anchor then employee ledger range inside transaction, including empty history. Do not claim different account payouts necessarily overdraw employee balance.','open_hypothesis':'Shared Poster derives cached BalanceAfter without self-lock; inspect all credit/manual/attendance writers for transaction serialization. Distinct immutable facts can remain correct while cached per-row display snapshot races. Not promoted or reproduced.','next_exact_action':'Read RecordDailyWageHandlers/AttendanceHandlers/RecordManualLedgerEntryHandler whole source and map each PostIfNew caller transaction, avoid treating rg discovery as completed review.','runtime':'NOT_EXECUTED','independent_challenge':'PENDING for root-origin notes.'})
print(json.dumps({'new_original_callable_receipts':total}))

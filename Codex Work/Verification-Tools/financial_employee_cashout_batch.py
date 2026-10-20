"""Complete staff drawer advance/withdrawal static review and cross-command comparison."""
from knowledge_store import *
path='src/Falah.Infrastructure/Employees/RecordEmployeeCashOutHandler.cs';source=load_symbols(path)
notes={31:'Stores context, approval and audit dependencies without accessing financial history.',
38:'Exact kind advance/withdrawal, deterministic idempotency key and early replay. Require positive amount/reason, actor and employee. Explicit transaction, lock employee anchor row then repeat replay, require stored Open shift, authorize employees_ledger, require active day, lock Drawer funds and employee ledger. Withdrawal bounded by earned balance; advance intentionally permits employee debt. Post one external Drawer cashout plus Employee Advance/Withdrawal debit with movement ID, journal and audit, save/commit atomic. Shift/day reads inside transaction alone are not durable admission locks (AsNoTracking, no version mutation); need cross-command close admission test. Catch key/lock conflicts rollback and bounded stable-key replay. Does not change cash for audit.',
44:'Early employee ledger key lookup returns stored BalanceAfter and TreasuryMovementId as replay; no new money operation or payload comparison.',
52:'Resolve actual actor user; policy evaluates persisted active/permissions.',
56:'Resolve actual employee identity before transaction; anchor-lock Single later verifies still exists.',
67:'Select employee ID from UPDLOCK ROWLOCK HOLDLOCK anchor within transaction. Serializes first cashout when ledger empty and shares anchor discipline standalone PayEmployee.',
69:'Second key replay under employee anchor closes initial observation gap for same employee; no commit needed because current path wrote nothing.',
73:'Load requested shift and explicitly require status Open; unlike SafeDrop existence-only policy. No update-lock/version mutation on shift.',
81:'Optional approver exists; AuthorizeCommand validates issued employees_ledger action if direct permission absent; recorded authority uses returned validated name.',
100:'Project employee credit/debit under explicit employee range lock, independent treasury account lock already acquired. Different lock order from PayEmployee documented, not presumed runtime deadlock.',
102:'Recompute locked employee balance using sum of credit-debit; withdrawal compares amount, advance deliberately not bound. Extreme unchecked inner subtraction and Sum overflow need boundary tests.',
147:'After rollback read committed ledger by exact idempotency key; return only saved sibling fact, no fallback invented movement.',
153:'Bounded delayed replay closure repeats exact-key NoTracking lookup with cancellation; distinct nested lambda IDs both retained. Does not reexecute debit or mint new ID.'}
records=[]
for s in source['symbols']:
 if not s['isCallable']:continue
 a=s['startLine'];assert a in notes,a
 records.append({'stable_id':s['stable_id'],'status':'ANALYZED','signature':s.get('qualifiedSymbol') or s['name'],'span':[a,s['startColumn'],s['endLine'],s['endColumn']],
  'reasoning':notes[a],'inputs':'Employee cash-out request kind/key/amount/shift/actor/approval plus scoped context; actual private data not queried by audit.',
  'outputs':'EmployeeCashOutResult or precise row/key/projection lookup; persisted signed balance preserved.',
  'side_effects':'Production debit, external drawer movement, journal and audit atomic within transaction; this audit invokes no financial or hardware command.',
  'trust_boundaries':'employees_ledger permission or issued token, positive amount, actual Open shift and active day. Advance and earned withdrawal intentionally different eligibility.',
  'database':'Employee anchor + ledger range + treasury source lock inside transaction. AsNoTracking Open state observation is not equivalent to row admission lease; no SQL concurrency test.',
  'printing_ui':'Source financial cash movement only; no physical drawer or ticket command performed.',
  'performance':'Shared-account and employee range lock contention caught for SQL1205/1222 with bounded replay; no load/deadlock proof.',
  'duplicates_conflicts':'PayEmployee uses same employee anchor but ledger-before-account, while this path account-before-ledger. Same-employee anchor serializes these two, so lock-order difference alone is not demonstrated deadlock. Cashout Drawer/shift versus standalone Main/Bank/InstaPay/day must remain distinct.',
  'issues_hypotheses':notes[a],'tests_missing':'Concurrent first advance, distinct-key withdrawal across two paths, same key changed payload, transaction rollback after approval, shift/day close admission, source-funds exhaustion, signed advance balance and response-loss replay.',
  'verified_callers_callees':'Whole161line handler read; TreasuryBalanceQuery, EmployeeRules, PayEmployee, Poster and BalanceLockContention source compared. No provider or runtime state touched.'})
save_review('financial_coordinator',path,{'symbols':records,'scope':'Complete source callable review; independent concurrency/runtime verification still outstanding.'})
atomic_json(WORK/'Deep-Audit/Duplication-and-Architecture/employee-payout-policy-comparison.json',{'at_utc':now(),'baseline':BASELINE,'sources':[{'path':p,'hash':load_symbols(p)['worktree_sha256']} for p in [path,'src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs','src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs','src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs']],'shared':'Employee identity anchor before paid debits in cashout and standalone pay; deterministic request replay and atomic ledger/treasury/journal/audit.','differences':['Cashout advance intentionally permits negative employee balance; withdrawal and standalone pay limited earned balance.','Cashout requires Open shift and Drawer source; standalone pay excludes Drawer and requires active day.','Cashout account-before-ledger versus standalone ledger-before-account; same employee anchor serializes same-employee path pair, so no automatic deadlock finding.','Correction serializes attendance row then employee ledger range; immutable audit snapshot confirms even zero-delta retry. Poster alone has no transaction/lock.'],'root_conclusion':'Useful shared infrastructure candidate, but one generic business permission/source/shift/eligibility rule would erase intentional financial policy. Full mirrored-code audit remains incomplete.','tests':'Provider-backed cross-path concurrency and rollback only on authorized isolated DB; NOT_EXECUTED.'})
print(json.dumps({'new_original_callable_receipts':len(records)}))

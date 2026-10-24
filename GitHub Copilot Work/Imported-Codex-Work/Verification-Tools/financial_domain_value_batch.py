"""Persist individual financial guard/value/state reasoning for six fully read Domain files."""
from knowledge_store import *
notes={
'src/Falah.Domain/Employees/EmployeeRules.cs':{
11:'Rejects blank name, employee number, title and phone only; does not enforce uniqueness, phone format, maximum length or actor permissions. No real employee fields read. Persistence/input boundaries must supply other invariants.',
21:'Enumerates supplied entries twice, sums credits and debits separately then subtracts Money. Caller must scope employee identity; method does not filter IDs. LINQ long Sum can throw overflow while final Money subtraction unchecked under current source. Lazy nonrepeatable enumerable could produce differing snapshots; normal materialized lists stable.',
23:'Credit.Piastres selector for all provided entries; no type/employee/date filter, intentionally unified ledger stock.',
24:'Debit.Piastres selector for same enumerable; does not net/ignore advances separately because debit is already financial meaning.',
30:'Positive amount guard rejects zero/negative Money; does not check available account or employee balance.',
37:'Rejects amount greater current balance only; negative/zero request could pass this guard alone. Handler must combine positive amount check and transactional balance lock. Equality permits spending exact balance.',
46:'Same financial comparison as withdrawal with payment-specific error; no implicit positivity or concurrent balance lock. Semantic duplicate intentional wording difference; preserve both error contracts if consolidated.'},
'src/Falah.Domain/Orders/RefundRules.cs':{
9:'Subtract already-refunded from total and floor negative remaining at0; does not require order actually collected. FIN003 paid-state authorization requires caller, not this arithmetic helper. Unchecked subtraction extreme bounds depend Money contract.',
15:'Floor requested and remaining at0 then return minimum, preventing negative/excess refund within ordinary arithmetic range. Silent clamp is not explicit invalid-request rejection or payment eligibility verification.',
22:'Disallow cancellation when any positive accumulated refund exists; zero/negative stored refund passes. Normal mutation maintains nonnegative refund; no compensation performed here.',
34:'For Closed historical shift require active shift only for Cash refund; other methods may return null current operational shift. Original open shift preserved otherwise. Explicitly no payment-collected check or active-shift actual status verification here; handler must supply facts. FIN004 current-vs-historical expected-cash mismatch persists downstream.'},
'src/Falah.Domain/Orders/OrderValueBreakdown.cs':{
11:'Compute list value=sum list unit*quantity and charged=sum line TotalPrice, floor offer discount at0, floor negative comp at0, floor collected after comp at0. Does not validate comp<=charged, quantities, tax, delivery or manual discount; these are other command rules. Collected name means customer amount, not actual treasury receipt for pending/COD.',
13:'ListUnitPrice.Piastres times Quantity inside Sum: multiplication unchecked before LINQ checked sum, so integer representation alone is not overflow safety.',
14:'Sum each OrderItem.TotalPrice frozen amount; depends line computed total, not client free-form price. Tax/fee applied outside this breakdown.'},
'src/Falah.Domain/Pricing/PricingRules.cs':{
12:'Inactive rule rejects; convert instant to configured zone, optional days constrain current local day; partially configured time window ignored. Ordered endpoints inclusive; crossing midnight OR logic. Equal start/end uses ordered branch (single instant), despite summary saying <= crossing; requirements ambiguity not assumed full-day offer. Overnight after-midnight day uses current weekday, policy tests needed.',
28:'Materialize rules matching product membership and IsActiveAt at same provided instant/zone. Multiple overlapping valid rules intentionally returned for explicit cashier selection, no arbitrary cheapest policy.',
30:'Membership-and-active predicate short-circuits time evaluation for unrelated products; list per rule assumed nonnull valid model.',
35:'Null/empty requested ID gives no rule; first matching ID then eligibility for requested product/time. Unknown/stale/inactive invalid rule falls back caller list price rather than throwing. Duplicate IDs in arbitrary input use first; persistence key normally unique.',
39:'Exact rule ID equality, not fuzzy name matching or caller-provided effective price.',
46:'Require comp positive, order value positive and comp<=value; no actor or approval check here, distinct authorization boundary outside.',
62:'Nonblank comp reason must belong fixed case-sensitive taxonomy. User free-text reason cannot replace closed category; no business write.',
74:'Require nonblank rule name, at least one product, positive fixed price and any supplied day within0..6. Intentionally no offer overlap rejection. Does not verify referenced products exist, remove duplicate IDs or validate time-window pair; caller/persistence needed.',
79:'Any day integer outside0..6 rejects; empty/null days means unconstrained days rather than no days.'},
'src/Falah.Domain/Reports/TrendRules.cs':{
24:'Shift both calendar endpoints back7days preserving weekday and span shape. DateOnly near minimum throws; reversed endpoints stay reversed, so does not repair reporting-range inconsistency.',
31:'Delta current-previous Money piastres and percentage round1decimal over abs(previous); zero denominator returns null. Negative prior bases use magnitude. Long delta can wrap and Math.Abs(long.MinValue) throws; no realistic recorded extreme amount claimed. No floating values persisted.',
40:'Wrap both long values as Money then reuse comparison; count comparison uses same long arithmetic and piastre-named output, no currency scaling.'},
'src/Falah.Domain/Shifts/Shift.cs':{
36:'New shift rejects negative opening float, records identifiers/date/time and sets Open. Does not validate blank identities/name or unique active shift; handler/DB owns those. Zero float allowed.',
48:'Creates shift with Cairo BusinessDate from single supplied instant, keeping opened timestamp same instant; no wallclock reread or persisted mutation.',
54:'Persistence reconstitution copies stored status/amount/authority/timestamps without opening constructor validation. Intentional trusted-storage hydration, not a public HTTP construction path proven exploitable. Corrupt persisted state can enter aggregate unchanged.',
74:'Delegates all supplied stored facts to reconstitution constructor; not authorization or consistency validation. Handler must load actual row and version.',
91:'Require Open status and nonnegative actual count, subtract caller expected, compute discrepancy threshold requirement and return result without mutating shift. Expected cash and threshold not independently reconstructed/validated, source caller owns facts.',
103:'Require still Open and nonblank authority+reason when result.RequiresManagerApproval. Copy result amounts/difference, actor/reason/authority and set Closed/time. Does not itself authenticate named authority, recompute expected/difference, or prove result came from PrepareBlindClose. Handler must construct trusted result and authorize; FIN017 narrowed by this nonblank guard. No live closed shift modified.'}}
properties={
21:'Immutable shift identity, no uniqueness enforcement in getter.',22:'Stored shift label, not cashier authority.',23:'Opening actor identity, not current authenticated session.',24:'Closing actor nullable until close; private setter used by Close/hydration.',25:'Opening cash frozen at creation, not current drawer balance.',26:'Actual counted cash nullable until closure; private setter records trusted close result.',27:'Expected cash snapshot supplied by financial query, not live recomputation.',28:'Persisted difference snapshot copied from BlindCloseResult; private setter does not verify arithmetic.',29:'Optional discrepancy reason; Close enforces nonblank only above threshold.',30:'Authority display name, not issued approval token; private setter alone cannot authenticate manager.',31:'Open/Closed state, transition guarded by Close; private setter/hydration not global transaction admission lock.',32:'Frozen Cairo calendar date, distinct business-day aggregate identity.',33:'Recorded opening instant, not local wall-clock reinterpretation.',34:'Nullable closing instant set on Close; no automatic monotonicity check.'}
added=0
for path,mapping in notes.items():
 source=load_symbols(path);target=WORK/'Knowledge/Reviews/financial_coordinator'/Path(path+'.json')
 old=json.loads(target.read_text(encoding='utf8')) if target.exists() else {}
 receipts=list(old.get('symbols',[]));done={r['stable_id'] for r in receipts if r.get('status')=='ANALYZED'}
 for s in source['symbols']:
  if not s['isCallable'] or s['stable_id'] in done:continue
  line=s['startLine'];reason=mapping.get(line)
  if path.endswith('/Shift.cs') and line in properties:
   sig=s.get('qualifiedSymbol') or s['name'];reason=('Getter exposes stored backing field without DB query or recomputation. ' if sig.endswith('.get') else 'Private setter assigns backing field during hydration/Close only; no public mutation capability inferred. ')+properties[line]
  assert reason,(path,line)
  receipts.append({'stable_id':s['stable_id'],'status':'ANALYZED','signature':s.get('qualifiedSymbol') or s['name'],'span':[line,s['startColumn'],s['endLine'],s['endColumn']],
   'reasoning':reason,'inputs':'Declared value objects/collections/scalars or backing field; trusted command facts distinguished from external request.',
   'outputs':'Guard, arithmetic value, date comparison or aggregate field/result documented individually above.',
   'side_effects':'Pure except Shift construction/private assignment/Close in memory; audit creates no live shift, ledger or financial transaction.',
   'trust_boundaries':'Domain guard is not endpoint authentication; persisted reconstitution trusts storage and command handlers must provide authorized facts.',
   'database':'No EF/SQL/query/transaction in these source functions; DB uniqueness/concurrency and row admission are separate.',
   'printing_ui':'Financial values/error/result states can be presented by clients, no XAML modification or native observation.',
   'performance':'Enumerable filtering/sums materialize or traverse inputs as documented; no runtime benchmark. Autoaccessors constant field access.',
   'duplicates_conflicts':'Payment/withdrawal employee balance guards semantically same with different messages; central pricing resolver uses these rule guards; no consolidation implemented.',
   'issues_hypotheses':reason,'tests_missing':'Boundary amounts/overflow, mixed pending/refund eligibility in handlers, zero/negative guards composed correctly, overnight/day/equal offer times, supplied shift result invariants, DateOnly.MinValue trends.',
   'verified_callers_callees':'All six complete Domain files read; prior handler/report financial traces supply bounded context. No claim all runtime callers were exhaustively traced in this batch.'})
  added+=1
 save_review('financial_coordinator',path,{'symbols':receipts,'scope':'Actual per-callable static financial source reasoning, independent challenge and live tests separate.'})
print(json.dumps({'new_root_callable_receipts':added}))

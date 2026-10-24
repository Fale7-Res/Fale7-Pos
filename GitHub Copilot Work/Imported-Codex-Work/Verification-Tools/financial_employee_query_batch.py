"""Read-only employee/payroll query reasoning; never enumerate actual employee data."""
from knowledge_store import *
notes={
'src/Falah.Infrastructure/Employees/EmployeeQueryService.cs':{
13:'Stores injected context only, no employee enumeration or ledger write.',
25:'Read active employees and ALL ledger facts, group each employee in memory; selected CreatedAt range feeds six flow buckets while remaining balance deliberately all-time credit-debit. Covers every enum category by name but Adjustment credit is ignored in flow buckets, linked existing FIN012 not new duplicate. Inactive staff balances excluded from this report even with obligations; owner obligations includes all ledger IDs, a scope/label difference. Reversed endpoints not normalized; no common snapshot between employee and entry queries.',
32:'Only active employee identities in financial report, unlike ListAsync which includes all lifecycle states.',
34:'Read entire ledger minimal amount/type/time/identity projection; no period SQL pushdown, repeated per-employee filtering has O(employee*ledger) shape, not measured benchmark.',
45:'Filter all ledger entries to one actual employee ID; materialized list reused for range and all-time balance.',
46:'Optional start-inclusive/end-exclusive timestamps for flow buckets, no swapping reverse dates. All-time balance unaffected empty reversed flow.',
49:'Local credit total filters in-range type membership and sums CreditPiastres. Distinct nested filter and sum have separate IDs. Does not include Adjustment credits in earnings/additions; same FIN012 family.',
50:'Local debit total filters in-range type membership and sums DebitPiastres; Adjustment debit enters deductions, Advance separate, settlement/withdrawal/payroll settlement enters payments.',
51:'All-time employee credits minus debits independent reporting interval. LINQ sums overflow checked; final subtraction unchecked under current Money/scalar build assumptions. Not historical as-of balance.',
58:'Sort report by EmployeeName using runtime culture; not financial ordering or unique identity key.',
61:'List every employee, including inactive, through full ToDto; caller permission controls sensitive fields, no query redaction here.',
62:'Map each employee to full DTO; no current balance or date filtering attached.',
64:'Find exact employee ID then map DTO; missing throws InvalidOperationException rather than domain null result. Endpoint exception mapping separate.',
65:'Exact employee ID predicate, not active-only, so historical staff can be retrieved.',
68:'Read entire employee ledger newest-first and map stored records; no pagination/secondary tie-break ordering in service.',
69:'Filter employee ledger by exact identity, no type/status/date restrictions.',
70:'Order CreatedAt descending then map full ledger DTO, preserving stored BalanceAfter rather than recomputing sorted history. Same-time entries can have nondeterministic ordering.',
74:'Load employee debit/credit only, derive all-time credits minus debits. No active employee requirement or employee existence check, missing ID returns0. Read balance is not locked authorization for spending; command must use transaction policy.',
76:'Exact employee ledger identity predicate, no date/status changes.',
77:'Select only debit and credit for balance calculation, not cached BalanceAfter.',
78:'Sum credits and debits independently then subtract. Distinct selectors stable IDs; can disagree with cached per-entry balance if prior writer race, not a live discrepancy verified here.',
81:'Map stored employee compensation, lifecycle, identity/contact/national ID/address and overtime fields to DTO. No secret values inspected or written by audit; field access is sensitive and endpoint authorization must restrict it.',
87:'Map stored debit/credit and cached BalanceAfter plus actor/approval/reference/rate/payroll facts. Stored attribution is not proof independent approval or current authority.',
92:'Optional employee and DateOnly filters for attendance; null means all, empty string is an actual ID restriction. Order date descending, no page limit; no wage recalculation or financial posting.',
95:'Apply employee identity filter when argument nonnull, including empty-string no-match semantics.',
96:'Apply exact attendance date when nonnull, no current-clock substitution.',
97:'Date-descending attendance mapping preserves persisted correction/source facts, no time ordering among same-day employees.',
100:'Copy stored check-in/out/hours/status/correction/source to DTO; does not verify machine punch evidence or recalculate late minutes.'},
'src/Falah.Infrastructure/Employees/PayrollQueryService.cs':{
10:'Stores context, does not create payroll or mutate run status.',
12:'Read all payroll runs newest-first and project state/approval/payment/review/movement metadata with WasReplayed=false because query is not command replay. No pagination or recomputation whether Paid has actual ledger; FIN006 can therefore be visible verbatim.',
13:'Descending CreatedAt run order, no period filter or tie-breaker.',
14:'Map run financial status and movement/account references as stored, not independent verification of actual payment; private employee lines omitted here.',
18:'Lookup one run by identity and reuse CreatePayrollHandler.ToDto mapping with false replay flag. Missing run throws; no approval or payment state transition executed.',
20:'Exact run ID lookup, no status/lifecycle filtering.',
25:'Return lines matching run ID, no parent-run existence guard or explicit ordering. Missing run returns empty list; model FK assumptions require separate verification, not actual orphan evidence.',
26:'Exact PayrollRunId equality; no employee-active/status filter, so historical frozen lines preserved.',
27:'Map frozen per-component payroll amounts/net/status/PaidAt; does not independently assert sum formula, post debits or change employee balance.'}}
total=0
for path,mapping in notes.items():
 source=load_symbols(path);lines=(ROOT/path).read_text(encoding='utf-8-sig').splitlines(keepends=True);records=[]
 for s in source['symbols']:
  if not s['isCallable']:continue
  a,b=s['startLine'],s['endLine'];assert a in mapping,(path,a)
  reason=mapping[a]
  if 'Lambda' in s['kind'] and a==b:
   raw=lines[a-1].encode('utf-16-le');reason+=' Exact selector: '+raw[(s['startColumn']-1)*2:(s['endColumn']-1)*2].decode('utf-16-le')
  records.append({'stable_id':s['stable_id'],'status':'ANALYZED','signature':s.get('qualifiedSymbol') or s['name'],'span':[a,s['startColumn'],b,s['endColumn']],
   'reasoning':reason,'inputs':'Query arguments, scoped context or selected stored row/group; no actual employee/customer values read.',
   'outputs':'Financial/employee/payroll/attendance DTO list or exact projection/filter/sum described individually.',
   'side_effects':'Source query paths only read EF/local materialization; audit invokes none and does not create ledger or payroll records.',
   'trust_boundaries':'Employee/payroll permission and endpoint actor checks outside service; DTO fields include private identity/compensation and require authorized callers.',
   'database':'AsNoTracking reads without transaction snapshot or spending lock; query balance not safe substitute for command concurrency guard.',
   'printing_ui':'Financial query shape only, no native UI review or printing workstream resumed.',
   'performance':'Whole ledger materialization/repeated employee scans or unpaged lists documented; no resource benchmark run.',
   'duplicates_conflicts':'Employee credit-debit all-time balance agrees Domain EmployeeRules and dashboard employee obligations; selected six buckets intentionally differ daily eight-display shape. Adjustment credit omission linked FIN012.',
   'issues_hypotheses':reason,'tests_missing':'Inactive staff outstanding balances, all ledger credit/debit categories including positive Adjustment, reversed range, empty/missing ID, cached BalanceAfter consistency, orphan run assumptions, large ledger query planning.',
   'verified_callers_callees':'Both whole source files read; EmployeeRules/DailyStatement/OwnerDashboard and existing FIN012 source compared. CreatePayrollHandler.ToDto reused by source call, no runtime hydration executed.'})
 save_review('financial_coordinator',path,{'symbols':records,'scope':'Full static callable review, privacy-conscious source-only analysis.'});total+=len(records)
atomic_json(WORK/'Deep-Audit/Database-and-Finance/employee-query-scope-comparison.json',{'at_utc':now(),'baseline':BASELINE,'paths':list(notes),'receipts':total,'source_references':[{'path':p,'worktree_sha256':load_symbols(p)['worktree_sha256']} for p in notes],'all_time_balance_contract':'EmployeeRules, EmployeeQueryService.GetBalance, DailyStatement running balance and OwnerDashboard employee obligation use credit-debit stock; current versus date flow intentionally different.','known_finding_link':'FIN012 Adjustment credit not counted in six-bucket employee flow report despite remaining balance including it; do not add duplicate finding.','scope_question':'Active-only financial report can omit inactive staff with debt, while dashboard includes all ledger IDs. Requires owner labeling/settlement policy; not newly proven lost money.','runtime_verification':'NOT_EXECUTED','privacy':'Schema field names only; no employee record values queried.'})
print(json.dumps({'new_original_callable_receipts':total}))

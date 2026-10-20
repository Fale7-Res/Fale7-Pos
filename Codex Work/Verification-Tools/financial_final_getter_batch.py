"""Persist twelve actually inspected supplemental getters; no application execution."""
from knowledge_store import *

notes = {
 'tests/Falah.Server.IntegrationTests/OwnerDashboardVerticalSliceTests.cs': {
  'Today': 'Computes BusinessClock.Today().ToString("yyyy-MM-dd") on each access; Cairo current date, not an injected frozen test clock. A test spanning Cairo midnight can observe different days; this is test determinism, not an observed production accounting defect.'},
 'tests/Falah.Server.IntegrationTests/PrintAgentTests.cs': {
  'Description': 'FakeTransport returns constant fake description; no physical device enumeration or ticket dispatch. Classified only because this test file is in the historical financial scope; no printing workstream or test process resumed.'},
 'src/Falah.Infrastructure/Backup/ScheduledBackupService.cs': {
  'Enabled': 'Reads numeric setting with default1 and compares !=0; any parsed nonzero value enables worker. Setting accepts Boolean or invariant double, no explicit finite/range check. Getter alone does not start worker or execute backup.',
  'MaxAgeHours': 'Returns parsed age threshold default20 hours without positive/finite bound. Whether invalid configuration disrupts scheduling depends on ExecuteAsync consumer; no installed configuration values inspected or worker invoked.',
  'CheckInterval': 'Converts configured minutes default30 to TimeSpan. Parser accepts invariant double without explicit finite or TimeSpan bounds; malformed text falls back, but parsed nonfinite/out-of-range value may throw in conversion. Not an observed runtime failure.',
  'KeepCount': 'Converts configured double default60 to int; fractional values truncate. No positive bound here; retention policy is in BackupRules and nonpositive count does not mean delete every backup. No pruning executed.',
  'StartupDelay': 'Converts parsed seconds default90 to TimeSpan on access. Invalid parsed numeric range can fail conversion despite text-parse fallback; actual deployment values and worker execution untested.'},
 'src/Falah.Domain/Audit/AuditEvent.cs': {
  'Events': 'Returns List.AsReadOnly wrapper around live _events. Caller cannot directly mutate via exposed collection, but later Append changes observed view; no copied immutable snapshot or thread synchronization. This is documented in-memory test implementation, not production EF log.'},
 'src/Falah.Domain/Common/BusinessClock.cs': {
  'DayCount': 'Subtracts inclusive DateOnly day numbers without normalizing From/To. Reversed input can yield zero/negative count while ToUtcRange swaps endpoints and Days yields none. Exact endpoint/report inconsistency preserved in reverse-date-range-source-lead.json; no int overflow within DateOnly domain claimed.',
  'IsSingleDay': 'Tests original From==To only, does not validate ordering or normalize. Non-single reversed range still flows through ToUtcRange endpoint swap; this Boolean is not evidence of valid reporting range.'},
 'src/Falah.Domain/Pricing/ItemPricingRules.cs': {
  'RuleApplied': 'Tests nullable applied rule ID presence, not current activation or price equality. Resolve delegates validation to PricingRules.ValidateAppliedRule and uses null ID on fallback list pricing. Manually constructed nonnull empty ID would still return true; no normal-path defect inferred.'},
 'src/Falah.Domain/Shifts/Shift.cs': {
  'IsBalanced': 'Tests precomputed Difference.IsZero only. PrepareBlindClose checks open status and nonnegative actual, derives difference and approval requirement. IsBalanced does not independently validate expected cash, other custody obligations or manager approval; not a global day reconciliation certificate.'}
}
count=0
for path, mapping in notes.items():
 source=json.loads((WORK/'Knowledge/Source-Symbols-CallableFacets/Files'/Path(path+'.json')).read_text(encoding='utf8'))
 target=WORK/'Knowledge/Reviews-CallableFacets/financial_coordinator'/Path(path+'.json')
 old=json.loads(target.read_text(encoding='utf8')) if target.exists() else {}
 records=list(old.get('facets',[])); done={r['stable_id'] for r in records}
 found=set()
 for facet in source['facets']:
  name=facet['qualifiedSymbol'].split('.')[-1].split('::')[0]
  if name not in mapping: continue
  found.add(name)
  if facet['stable_id'] in done: continue
  records.append({'stable_id':facet['stable_id'],'status':'ANALYZED','signature':facet['qualifiedSymbol'],
   'span':[facet['startLine'],facet['startColumn'],facet['endLine'],facet['endColumn']],
   'reasoning':mapping[name],
   'inputs':'Current object state, or explicitly described static clock/configuration dependency; no operational data read.',
   'outputs':'Expression result from exact source getter; financial consequence qualified in reasoning.',
   'side_effects':'No persisted write, backup dispatch, financial command, migration or printer operation by this review.',
   'trust_boundaries':'Not independently an actor/permission boundary; normal constructor/resolver/handler invariants remain separate.',
   'database':'No explicit DB query or write in these getters; EF/runtime consumers not invoked.',
   'printing_ui':'Result can support presentation/tests; no native window observation or physical printing claimed.',
   'performance':'Constant-size access/conversion; AsReadOnly creates wrapper and clock/config calls retain their real external costs.',
   'duplicates_conflicts':'BusinessDateRange date normalization differs across consumers; backup properties intentionally share Setting parser.',
   'issues_hypotheses':mapping[name],
   'tests_missing':'Boundary/invariant tests appropriate to described behavior; no integration or financial state-changing tests executed.',
   'verified_callers_callees':'Getter and surrounding declared producer/parser source inspected; production runtime outcomes not inferred from syntax.'})
  count+=1
 assert found==set(mapping),(path,found,set(mapping))
 save_callable_facet_review('financial_coordinator',path,records,old.get('generated_behavior_inputs',[]))
print(json.dumps({'new_supplemental_getter_receipts':count,'original_callable_increment':0}))

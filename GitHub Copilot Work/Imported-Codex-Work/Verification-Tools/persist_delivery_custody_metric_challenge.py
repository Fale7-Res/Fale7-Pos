"""Reconcile the delivery-custody report's labeled expected amount against its aggregation."""
from knowledge_store import *

scope={
 'src/Falah.Infrastructure/Reports/ReportingQueryService.cs':[(545,585)],
 'src/Falah.Application/Reports/ReportingDtos.cs':[(91,100)],
 'src/Falah.Domain/Delivery/DeliveryRules.cs':[(9,23)],
 'src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs':[(77,109)],
 'src/Falah.Infrastructure/Delivery/RecordDeliveryResultHandler.cs':[(86,110)],
 'src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs':[(254,289)],
 'src/Falah.Application/Reports/OwnerDashboardDtos.cs':[(110,123)],
 'src/Falah.Client.CashierWinUI/Views/ReportsView.xaml':[(288,297)],
 'src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml':[(330,335)],
 'tests/Falah.Server.IntegrationTests/ReportingVerticalSliceTests.cs':[(424,445)],
 'tests/Falah.Server.IntegrationTests/DailyStatementVerticalSliceTests.cs':[(218,229)],
}
baseline={f['path']:f for f in json.loads((WORK/'Deep-Audit/Source-Inventory/verified-baseline-hashes.json').read_text(encoding='utf8'))['files']}
refs=[]
for path,ranges in scope.items():
 raw=(ROOT/path).read_bytes(); lines=raw.decode('utf-8-sig').splitlines(); ix=load_symbols(path)
 assert sha256(raw)==ix['worktree_sha256']==baseline[path]['worktree_sha256'],path
 assert all(1<=a<=b<=len(lines) for a,b in ranges),(path,len(lines),ranges)
 symbols=[{'stable_id':s['stable_id'],'signature':s.get('qualifiedSymbol') or s['name'],'kind':s['kind'],'span':[s['startLine'],s['startColumn'],s['endLine'],s['endColumn']]}
          for s in ix['symbols'] if s['isCallable'] and any(s['startLine']<=b and s['endLine']>=a for a,b in ranges)]
 refs.append({'path':path,'worktree_sha256':sha256(raw),'source_git_blob_sha256':baseline[path]['git_blob_sha256'],'line_ranges':ranges,'relevant_symbols':symbols,
              'excerpts':[{'start':a,'end':b,'text':'\n'.join(lines[a-1:b])} for a,b in ranges]})

agent_handoff='Sub-Agents/Handoffs/financial-negative-variant-and-delivery-outcomes-independent-challenge-2026-10-21.json'
agent=json.loads((WORK/agent_handoff).read_text(encoding='utf8'))
assert agent['delivery_outcomes_challenge']['disposition'].startswith('COORDINATOR_DATE_SCOPE_CONCLUSION_CORROBORATED')
rel='Deep-Audit/Database-and-Finance/delivery-custody-expected-vs-collected-candidate-2026-10-21.json'
record={
 'id':'FIN-CAND-DELIVERY-CUSTODY-EXPECTED-001','baseline':BASELINE,
 'status':'INDEPENDENTLY_CORROBORATED_STATIC_METRIC_MISMATCH; FIELD_CONTRACT_AND_RUNTIME_IMPACT_QUALIFIED',
 'severity':'MEDIUM','confidence':'HIGH_SOURCE_CONFIDENCE_FOR_DATAFLOW; USER_MEANING_AND_RUNTIME_IMPACT_NOT_TESTED',
 'title':'Delivery report field labeled expected unsettled custody aggregates collected amount instead',
 'observed_behavior':'GetDeliveryOutcomesReportAsync filters all non-settled custody rows without a date filter, counts them, then sums CollectedPiastres into DTO field UnsettledCustodyExpectedPiastres. DeliveryRules.CreateCashCustody initializes Expected to responsibility and Collected to zero; assignment persists both. RecordDeliveryResultHandler populates Collected only when Delivered and stores any variance. OwnerDashboardQueryService independently projects unsettled custody as the sum of ExpectedPiastres. The report UI binds the mismatched DTO amount. Thus an assigned/out-for-delivery order can count as unsettled while its labeled expected amount is zero; a delivered variance can also make collected differ from expected.',
 'preconditions':'At least one non-prepaid delivery has been assigned (creating Open custody) and has not yet reached Delivered, or a delivered custody has an approved recorded variance. No real runtime state was created or queried.',
 'impact':'If the DTO/UI contract means expected cash at driver risk, the report underreports that exposure while delivery is in transit and diverges from the owner dashboard. If it intentionally means cash already collected but not settled, the aggregation may be deliberate but DTO name and tests are misleading. No loss, duplicate payment, or ledger corruption is established.',
 'source_references':refs,
 'independent_verification':{'status':'SOURCE_CORROBORATED_BY_INDEPENDENT_FINANCIAL_AGENT','reviewer':'/root/financial_integrity_continuation','artifact':agent_handoff,'scope':'Independent challenge of coordinator report path; same baseline sources, no runtime test.'},
 'related_temporal_question':'The non-settled count/amount query is global current state while delivery outcomes and settlement totals are period-filtered. DailyStatement passes a date but labels the amount “now”. Whether a historical statement should show current outstanding custody or as-of-date custody remains a separate owner policy question.',
 'counterevidence':['The application may intend this field to represent collected-but-unsettled cash; however the DTO explicitly says Expected, the existing integration assertion comments expected responsibility, and the owner dashboard uses ExpectedPiastres.','Existing reporting test checks the row only after full amount is collected, so Expected and Collected are equal in that test.','No API, SQL Server, or UI execution was performed; no current incident is known.'],
 'repair_specification':{
  'recommended_direction':'Confirm report contract. Existing DTO name, owner dashboard, and test comments support ExpectedPiastres: if that is the intended meaning, sum c.ExpectedPiastres. If the intended metric is collected-but-unsettled, rename the DTO/display and retain/add a distinct expected-at-risk metric so the two values are not conflated.',
  'related_paths':['src/Falah.Infrastructure/Reports/ReportingQueryService.cs','src/Falah.Application/Reports/ReportingDtos.cs','src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs','src/Falah.Client.CashierWinUI/Views/ReportsView.xaml','src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml','tests/Falah.Server.IntegrationTests/ReportingVerticalSliceTests.cs','tests/Falah.Server.IntegrationTests/DailyStatementVerticalSliceTests.cs'],
  'tests':['Open assigned COD custody before Delivered: report count is one and expected amount equals responsibility; collected amount is zero.','Delivered with exact collection: expected and collected equal and both projections agree.','Delivered with approved shortage/overage: expected remains original responsibility while collected reflects actual; report fields follow chosen names/contract.','After settlement, unsettled count/amount falls to zero; settled-period totals remain date-filtered.','Historical DailyStatement queried before/after a later settlement: assert current-now versus as-of-date policy explicitly.'],
  'acceptance_criteria':['DTO field name and source aggregate express the same amount.','Owner dashboard and delivery report agree for the same non-settled custody cohort when they claim to show expected custody.','In-transit and variance cases are covered by tests; no real printer or financial operation is needed.','Historical statement temporal semantics are documented separately.'],
  'risks':'Renaming DTO fields is a client wire-contract change. Switching to Expected may change displayed amount for in-transit and variance cases but does not alter ledger rows or driver settlement.'
 },
 'runtime_status':'NOT_EXECUTED; static source review only','tests_executed':[],
 'source_paths':len(refs),'reviewed_callable_ids':sorted({s['stable_id'] for r in refs for s in r['relevant_symbols']})
}
atomic_json(WORK/rel,record)

# Preserve the previous coordinator handoff and correct its mistaken field description.
prior=WORK/'Sub-Agents/Handoffs/financial-delivery-outcomes-coordinator-crosscheck-2026-10-21.json'
old=json.loads(prior.read_text(encoding='utf8'))
hist=WORK/'Knowledge/History/Financial-Coordinator'/('delivery-outcomes-crosscheck.'+sha256(prior.read_bytes())[:16]+'.json')
if not hist.exists(): atomic_json(hist,{'original_path':'Sub-Agents/Handoffs/financial-delivery-outcomes-coordinator-crosscheck-2026-10-21.json','prior_record':old})
old['reasoning']['outstanding_balance']='The query selects every non-settled custody row globally/currently, counts them, but sums CollectedPiastres into the DTO property named UnsettledCustodyExpectedPiastres. Assignment creates custody with Expected=responsibility and Collected=0; delivery result populates Collected later. The amount may therefore be zero in transit or differ after a variance. The temporal scope ambiguity is distinct from this source-confirmed aggregate/name mismatch.'
old['disposition']='COORDINATOR_DATE_SCOPE_CONCLUSION_CORROBORATED; INDEPENDENT_AGENT FOUND AND COORDINATOR VERIFIED EXPECTED_VS_COLLECTED_FIELD_MISMATCH; CONTRACT/RUNTIME IMPACT QUALIFIED'
old['related_finding_candidate']=rel
old['independent_challenge']=agent_handoff
old['source_review_correction']={'preserved_prior_record':str(hist.relative_to(WORK)).replace('\\','/'),'correction':'Prior coordinator text inaccurately called the summed CollectedPiastres an expected amount. Corrected after independent challenge and direct source verification.'}
atomic_json(prior,old)

masterp=WORK/'Deep-Audit/MASTER_FINDINGS.json'; master=json.loads(masterp.read_text(encoding='utf8'))
assert not any(f.get('id')==record['id'] for f in master['findings'])
refs_master=[{'path':r['path'],'source_git_blob_sha256':r['source_git_blob_sha256'],'worktree_sha256':r['worktree_sha256'],'evidence_ranges':r['line_ranges'],'relevant_symbols':r['relevant_symbols']} for r in refs]
candidate={'id':record['id'],'baseline':BASELINE,'severity':'MEDIUM','confidence':'INDEPENDENT_STATIC_CORROBORATION; CONTRACT_AND_RUNTIME_IMPACT_QUALIFIED','original_status':'INDEPENDENTLY_CORROBORATED_STATIC_METRIC_MISMATCH_PENDING_OWNER_FIELD_CONTRACT',
 'title':record['title'],'source_references':refs_master,'independent_verification':{'status':'SOURCE_CORROBORATED','reviewer':'/root/financial_integrity_continuation','artifact':agent_handoff,'runtime_test':'NOT_EXECUTED'},
 'verification_classification':{'source_behavior':'SOURCE_CONFIRMED_AGGREGATE_FIELD_MISMATCH','isolated_reproduction':'NOT_RUN','end_to_end_runtime':'NOT_RUN'},
 'origin':{'artifact':rel,'independent_challenge':agent_handoff,'related_temporal_question':old['related_temporal_question'] if 'related_temporal_question' in old else 'Historical current-versus-as-of custody scope remains unconfirmed.'},
 'source_record':{'sources':[{'path':r['path'],'line_ranges':r['line_ranges'],'worktree_sha256':r['worktree_sha256'],'source_git_blob_sha256':r['source_git_blob_sha256']} for r in refs],'method':'Coordinator verified independent agent evidence against custody creation, delivery result, report projection, owner dashboard, client display, and existing integration test source.','runtime':'NOT_EXECUTED'},
 'implementation_readiness':{'current_behavior':record['observed_behavior'],'preconditions':record['preconditions'],'impact':record['impact'],'proposed_remediation':record['repair_specification']['recommended_direction'],'required_regression_tests':record['repair_specification']['tests'],'acceptance_criteria':record['repair_specification']['acceptance_criteria'],'counterevidence':record['counterevidence'],'runtime_status':'NOT_EXECUTED','independent_verification':agent_handoff}}
master['findings'].append(candidate);master['finding_count']=len(master['findings']);master['updated_at_utc']=now();atomic_json(masterp,master)

report=WORK/'Deep-Audit/Database-and-Finance/DATABASE_AND_FINANCIAL_INTEGRITY.md'; text=report.read_text(encoding='utf8')
text=text.replace('unsettled custody remains current across all dates at query time. This may be intentional for an outstanding balance; no owner contract says historical statements freeze that balance. No defect is promoted. Clarify whether historical statements should show current custody or an as-of-date balance before changing behavior. No test or DB operation was run.','the unsettled custody count is current across all dates at query time, but the DTO amount is actually computed from `CollectedPiastres`, not `ExpectedPiastres`. That distinction is detailed below; the historical current-versus-as-of scope remains a separate owner contract question. No test or DB operation was run.')
marker='## Independently corroborated delivery custody metric candidate'
assert marker not in text
section=f'''\n\n{marker}\n\n**FIN-CAND-DELIVERY-CUSTODY-EXPECTED-001 — MEDIUM, independently corroborated static mismatch; field-contract/runtime impact qualified.** [Evidence and repair specification]({rel}); [financial agent challenge](../../{agent_handoff}).\n\nCorrection to the earlier coordinator handoff: `GetDeliveryOutcomesReportAsync` puts `unsettled.Sum(c => c.CollectedPiastres)` into `UnsettledCustodyExpectedPiastres`. At driver assignment, `DeliveryRules.CreateCashCustody` creates a non-settled row with `Expected = cash responsibility` and `Collected = 0`, and `AssignDeliveryHandler` persists both values. `RecordDeliveryResultHandler` sets `CollectedPiastres` only after a delivered outcome, and it can differ from Expected for a variance. The owner dashboard separately sums `ExpectedPiastres`; the report DTO name and existing test comment also say expected. An in-transit order can therefore be counted as unsettled while this report's amount is zero, and variance cases can differ. The visible report labels say unsettled custody, so the intended field contract is strongly suggested but still should be confirmed before selecting a repair.\n\nThe temporal issue remains separate: the unsettled query is global/current, while other outcome and settlement values are period-filtered; DailyStatement labels this amount “now.” Recommended direction is to sum Expected for the named field, or explicitly rename the field/display to collected-but-unsettled and expose expected-at-risk separately. No application change or runtime test was made.\n'''
atomic_text(report,text+section)

# Keep one durable coordinator checkpoint and do not overwrite the agent's own checkpoint.
cp=WORK/'Sub-Agents/Checkpoints/root-current-two-workstreams.json'; c=json.loads(cp.read_text(encoding='utf8'))
c['financial']['latest_independent_challenge']=agent_handoff;c['financial']['latest_coordinator_handoff']=rel
c['financial']['latest_coordinator_handoff_status']='Independent specialist corroborated a static mismatch: DTO Expected amount sums Collected; assignment initializes Expected>0/Collected=0; owner dashboard sums Expected. Candidate preserved; contract/runtime implications qualified.'
c['financial']['current_next']='Agent continues from GetTaxReportAsync after verifying receipts; parent has added independent challenge to the canonical candidate and reconciled historical report correction.'
c['coordinator']['last_completed']='SEM-MIRROR-046 and independent cross-validation of delivery expected-vs-collected report candidate.'
c['coordinator']['current_file_or_function']='Financial report mismatch reconciled; parent next resumes distinct semantic mirror group.'
c['at_utc']=now();atomic_json(cp,c)
nextp=WORK/'Conversation-Recovery/NEXT_ACTION.md'; n=nextp.read_text(encoding='utf8')
atomic_text(nextp,n+f'''\n\n## Independent financial cross-check correction ({now()})\n\n- Financial continuation handoff `{agent_handoff}` independently corroborates the variant-price candidate and identified a correction to the coordinator delivery-outcomes note. Parent verified source: assignment initializes Expected to cash responsibility/Collected=0; only Delivered updates Collected; report sums Collected into DTO `UnsettledCustodyExpectedPiastres`; OwnerDashboard sums Expected.\n- Corrected coordinator handoff `{str(hist.relative_to(WORK)).replace(chr(92),'/')}` is preserved; current handoff `{str(prior.relative_to(WORK)).replace(chr(92),'/')}` points to the correction. Added candidate `FIN-CAND-DELIVERY-CUSTODY-EXPECTED-001` to MASTER_FINDINGS with exact source lines, independent reviewer, contract qualification and tests. Finance report updated. Temporal current-vs-as-of issue remains separate. No source/runtime/test/DB operation.\n- Main completed SEM-MIRROR-046: `OrderValueBreakdown` has no exact-name call sites in current src/tests and is narrower than active checkout arithmetic; no active defect. Semantic evidence registry now has 46 groups/45 inputs; validation runs next. Financial agent continues `GetTaxReportAsync` after receipt check.\n''')
print(json.dumps({'candidate':record['id'],'refs':len(refs),'symbols':len(record['reviewed_callable_ids']),'independent':record['independent_verification']['status'],'history':str(hist.relative_to(WORK)).replace('\\','/')}))

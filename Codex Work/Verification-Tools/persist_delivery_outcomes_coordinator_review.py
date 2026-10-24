"""Persist a bounded static review of financial delivery-outcomes report semantics."""
from knowledge_store import *

scope = {
 'src/Falah.Infrastructure/Reports/ReportingQueryService.cs': [(545, 585)],
 'src/Falah.Infrastructure/Reports/DailyStatementQueryService.cs': [(60, 81)],
 'src/Falah.Application/Reports/ReportingDtos.cs': [(91, 100)],
 'src/Falah.Server/Program.cs': [(2059, 2060)],
 'src/Falah.Infrastructure/Persistence/Entities/DeliveryEntities.cs': [(67, 78), (123, 142)],
 'tests/Falah.Server.IntegrationTests/ReportingVerticalSliceTests.cs': [(430, 445)],
}
baseline={f['path']:f for f in json.loads((WORK/'Deep-Audit/Source-Inventory/verified-baseline-hashes.json').read_text(encoding='utf8'))['files']}
refs=[]
for path,ranges in scope.items():
 raw=(ROOT/path).read_bytes(); index=load_symbols(path)
 assert sha256(raw)==baseline[path]['worktree_sha256']==index['worktree_sha256'],path
 lines=raw.decode('utf-8-sig').splitlines(); assert all(1<=a<=b<=len(lines) for a,b in ranges),path
 symbols=[{'stable_id':s['stable_id'],'signature':s.get('qualifiedSymbol') or s['name'],'kind':s['kind'],'span':[s['startLine'],s['startColumn'],s['endLine'],s['endColumn']]}
          for s in index['symbols'] if s['isCallable'] and any(s['startLine']<=b and s['endLine']>=a for a,b in ranges)]
 refs.append({'path':path,'worktree_sha256':sha256(raw),'source_git_blob_sha256':baseline[path]['git_blob_sha256'],'line_ranges':ranges,'relevant_symbols':symbols,
              'excerpts':[{'start':a,'end':b,'text':'\n'.join(lines[a-1:b])} for a,b in ranges]})
artifact={
 'baseline':BASELINE,'reviewer':'/root coordinator','reviewed_at_utc':now(),
 'status':'BOUNDED_STATIC_REVIEW_COMPLETE_NO_DEFECT_PROMOTED;REPORTING_SEMANTICS_REQUIRES_OWNER_EXPECTATION',
 'callable':'Falah.Infrastructure.Reports.ReportingQueryService.GetDeliveryOutcomesReportAsync(DateOnly?, DateOnly?, CancellationToken)',
 'stable_callable_ids':[s['stable_id'] for r in refs for s in r['relevant_symbols'] if 'GetDeliveryOutcomesReportAsync' in s['signature']],
 'reasoning':{
  'period_flows':'Delivery outcome counts are filtered by delivered/failed/returned UTC event timestamps within the input range. Delivery fees are summed for orders corresponding to in-range delivered rows. Settlements are filtered by CreatedAtUtc in the period; shortage/overage totals are derived from each settlement difference.',
  'outstanding_balance':'Unsettled DriverCustody rows are queried only by Status != Settled, with no date filter, and the DTO returns their count and expected amount. This is a current outstanding-custody snapshot, not a period flow.',
  'historical_statement_effect':'DailyStatementQueryService passes a specific historical business date to the report. Consequently its delivery outcome and settlement fields are date-scoped, while unsettled custody remains current across all dates at query time. The current DTO does not label that distinction or provide an as-of timestamp.',
  'counterevidence':'Unsettled custody is inherently an outstanding balance and may intentionally be current regardless of requested historical date. The query names it Unsettled rather than settled-period activity. Existing integration test verifies current unsettled custody and then settlement, but does not query a historical date or assert temporal semantics. No owner requirement establishes that a historical statement must freeze this outstanding balance.',
  'disposition':'No defect promoted. Record as a report-contract clarification question: should a historical DailyStatement show current outstanding driver custody, or custody as-of that business date? If current is intended, label/provenance should say current; if historical is intended, an event/as-of model is required. Do not change report query absent owner decision.',
  'tests_needed':'If current outstanding is intended, verify period-filtered event totals alongside explicitly current outstanding balance for a past date. If as-of is intended, add data spanning two dates and assert settlement after the report date does not rewrite that historical snapshot; that likely requires a historical event or snapshot source. No test was executed.'
 },
 'runtime':'NOT_EXECUTED; static source and existing-test inspection only','new_financial_callable_receipts':0,'source_references':refs,
 'limits':['Not a full ReportingQueryService review.','No DB or API requests executed.','Historical financial statement temporal contract requires owner policy.','Global financial callable depth and 327-file completion gate remain open.']}
relative='Sub-Agents/Handoffs/financial-delivery-outcomes-coordinator-crosscheck-2026-10-21.json'
atomic_json(WORK/relative,artifact)
report=WORK/'Deep-Audit/Database-and-Finance/DATABASE_AND_FINANCIAL_INTEGRITY.md'
text=report.read_text(encoding='utf8'); marker='## Coordinator continuation — delivery outcomes and unsettled custody scope'
assert marker not in text
section=f'''\n\n{marker}\n\n[Coordinator cross-check]({relative}) reviews `ReportingQueryService.GetDeliveryOutcomesReportAsync`, its route, DTO, daily-statement caller, persistence entities and existing integration example. Delivery event counts, delivered fees and settlement totals are period-filtered; `UnsettledCustodyCount` and expected amount instead aggregate all currently unsettled custody rows without a date predicate. A historical `DailyStatement` calls this same report with one business date, so those unsettled fields represent current outstanding custody while adjacent fields are date-scoped. This may be intentional for an outstanding balance; no owner contract says historical statements freeze that balance. No defect is promoted. Clarify whether historical statements should show current custody or an as-of-date balance before changing behavior. No test or DB operation was run.\n'''
atomic_text(report,text+section)

# Refresh only parent-owned coordination state; do not rewrite the agent's evidence ledger.
cp=WORK/'Sub-Agents/Checkpoints/root-current-two-workstreams.json'; c=json.loads(cp.read_text(encoding='utf8'))
c['financial']['latest_coordinator_handoff']=relative
c['financial']['latest_coordinator_handoff_status']='GetDeliveryOutcomesReportAsync statically traced: event/settlement fields date-filtered, unsettled custody is current/global snapshot; no defect promoted pending owner temporal contract; runtime not executed.'
c['financial']['current_next']='If financial continuation agent resumes, send SEM-MIRROR-045 negative variant price candidate for independent challenge and this delivery-outcomes temporal contract question; continue from saved exact specialist queue. Parent next: distinct bounded mirrored-code group.'
c['mirror']['latest_semantic_group']='SEM-MIRROR-045'
c['mirror']['latest_semantic_artifact']='Deep-Audit/Duplication-and-Architecture/negative-variant-price-semantic-comparison.json'
c['updated_at_utc']=now();atomic_json(cp,c)

agentp=WORK/'Sub-Agents/Agent-Registry/agent-state.json'; ad=json.loads(agentp.read_text(encoding='utf8'))
for a in ad.get('agents',[]):
 if a.get('name')=='/root/financial_integrity_continuation':
  a['status']='BLOCKED';a['execution_control']='PLATFORM_USAGE_LIMIT';a['last_turn']='Live listing on 2026-10-21: errored with explicit account usage limit, stated available again Nov 20 2026 9:17 AM; no replacement or repeated launch.'
  a['latest_checkpoint']='Codex Work/Sub-Agents/Checkpoints/financial-integrity-continuation.json'
  a['latest_handoff']='Codex Work/Sub-Agents/Handoffs/financial-shifts-history-substantive-review-2026-10-21.json'
  a['remaining']='Financial global substantive/depth completion remains open; parent captured SEM-MIRROR-045 and delivery outcomes report semantics for future independent review.'
  a['execution_authorized']=True
  a['same_session_recovery']='UNAVAILABLE_USAGE_LIMIT_CONFIRMED_BY_LIVE_AGENT_LIST'
ad['updated_at_utc']=now();atomic_json(agentp,ad)

nextp=WORK/'Conversation-Recovery/NEXT_ACTION.md'; n=nextp.read_text(encoding='utf8')
block=f'''\n\n## Financial report callable continuation — delivery outcomes ({now()})\n\n- Main coordinator source-reviewed `ReportingQueryService.GetDeliveryOutcomesReportAsync` and its date-scoped daily-statement caller. Saved `{relative}` and appended the bounded result to `Deep-Audit/Database-and-Finance/DATABASE_AND_FINANCIAL_INTEGRITY.md`. Current unsettled custody is global/current, while outcomes and settlement rows are period-scoped; disposition is temporal contract clarification, no defect promoted absent owner requirement.\n- Financial agent remains errored on stated platform usage limit; original remains unavailable. No re-launch. Agent-state updated to preserve identity and blockage.\n- Financial next handoff if agent resumes: read SEM-MIRROR-045 / FIN-CAND-NEGATIVE-VARIANT-PRICE-001 for an independent challenge and this delivery report temporal-contract review; then reconcile exact source-review queue from its checkpoint.\n- Parent next: continue a distinct bounded mirror comparison, not duplicate the finance callables above. Full financial substantive coverage remains UNKNOWN/open.\n'''
atomic_text(nextp,n+block)
print(json.dumps({'handoff':relative,'callable':artifact['callable'],'symbols':len({s['stable_id'] for r in refs for s in r['relevant_symbols']}),'agent_status':'BLOCKED_USAGE_LIMIT','finding_promoted':False}))

from ui_persistent import *
from pathlib import Path
def persist_exact(path,facts,role,full=True):
 source=load_symbols(path);dest=ROOT/'Codex Work/Knowledge/Reviews/ui_visual'/Path(path+'.json')
 old=json.loads(dest.read_text(encoding='utf8')) if dest.exists() else None
 reviews=list(old['symbols']) if old else [];done={r['stable_id'] for r in reviews}
 for key,(reason,links,missing) in facts.items():
  s,=[s for s in source['symbols'] if s.get('isCallable') and (s['startLine'],s['startColumn'])==key]
  assert s['stable_id'] not in done
  reviews.append(dict(stable_id=s['stable_id'],status='ANALYZED',exact_signature=s['qualifiedSymbol'],exact_kind=s['kind'],exact_span=[*key,s['endLine'],s['endColumn']],reasoning=reason,inputs='Exact signature and captured VM/session/DTO state described in reasoning',outputs='Return value, observable state, API task or event described in exact body',side_effects='Source-only review; no UI, authentication, DB, financial operation or printer action executed.',trust_boundaries='Client cached state and command affordances versus authoritative backend actor/permission/pricing/idempotency.',database='No direct SQL executed; any HTTP/server write effects only described.',printing_ui='Native layout/accessibility and physical printing NOT_OBSERVED.',performance='Per-body collection/query/async costs described; no profiling performed.',duplicates_conflicts='Existing file report preserved; explicit source spans, not legacy name-heading promotion. Canonical dedup parent-owned.',issues_hypotheses=missing,tests_missing=missing+' NOT_EXECUTED.',verified_callers_callees=links))
 if full:assert len({r['stable_id'] for r in reviews})==sum(s.get('isCallable',False) for s in source['symbols'])
 shard=save_review('ui_visual',path,dict(file_review_status='ANALYZED_STATIC_EXACT_RECONCILIATION' if full else 'ANALYZED_STATIC_PARTIAL_EXACT_RECONCILIATION',file_reasoning=role,legacy_report_preserved='Codex Work/Deep-Audit/File-Reports/'+path+'.md',symbols=reviews,native_runtime='NOT_EXECUTED'))
 cp=ROOT/'Codex Work/Sub-Agents/Checkpoints/ui_visual.json';c=json.loads(cp.read_text(encoding='utf8'));c.update(updated_at_utc=now(),last_completed_file=path,last_completed_stable_id=reviews[-1]['stable_id'],current_partial_file=None if full else path,current_partial_symbol=None if full else 'Exact remaining IDs in coverage manifest',latest_review_shard=str(shard.relative_to(ROOT)).replace('\\','/'));atomic_json(cp,c)
 print(path,len(reviews))

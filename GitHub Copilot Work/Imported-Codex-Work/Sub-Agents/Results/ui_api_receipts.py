"""Persistence for explicitly authored endpoint facts, never source/index promotion."""
from ui_persistent import *
from pathlib import Path
def append_api(facts,label):
 path=P+'Services/ApiClient.cs';source=load_symbols(path)
 dest=ROOT/'Codex Work/Knowledge/Reviews/ui_visual'/Path(path+'.json')
 old=json.loads(dest.read_text(encoding='utf8'));reviews=list(old['symbols']);done={r['stable_id'] for r in reviews}
 for line,ll,col,route,reason in facts:
  for key,detail in [((line,5),reason),((ll,col),'Exact HTTP send delegate for '+route+'. '+reason+' Captures existing DTO/query and cancellation token without generating a fresh request ID.')]:
   s,=[s for s in source['symbols'] if s.get('isCallable') and (s['startLine'],s['startColumn'])==key]
   assert s['stable_id'] not in done
   reviews.append(dict(stable_id=s['stable_id'],status='ANALYZED',exact_signature=s['qualifiedSymbol'],exact_kind=s['kind'],exact_span=[*key,s['endLine'],s['endColumn']],reasoning=detail,inputs='Exact DTO/IDs/date/token in signature and body; '+route,outputs='Typed ApiResult through shared wrapper; '+route,side_effects='Static review only; no HTTP/DB/authentication/backup/printing action executed.',trust_boundaries='Caller query/DTO versus bearer-authorized server; client result does not establish business authorization or persistence by itself.',database='No SQL direct; server POST/PUT mutations only described, never invoked.',printing_ui='Native rendering and physical printing NOT_OBSERVED.',performance='One existing HttpClient invocation per delegate;10second default unless injected, no automatic retries.',duplicates_conflicts='Preserves all previous exact receipts; endpoint-specific source reasoning not inferred from heading match.',issues_hypotheses='Endpoint contract/permission/idempotency must be challenged against server source before runtime claims.',tests_missing='Synthetic verb/path/body/token and failure/cancellation/result classification checks NOT_EXECUTED.',verified_callers_callees=route+' -> shared ReadAsync/SendAsync/SendEmptyAsync according to source body'))
 shard=save_review('ui_visual',path,dict(file_review_status='ANALYZED_STATIC_PARTIAL_EXACT_RECONCILIATION',file_reasoning=label+' Earlier receipts preserved; exact remaining endpoint IDs retain INDEXED_NOT_ANALYZED status.',legacy_report_preserved='Codex Work/Deep-Audit/File-Reports/'+path+'.md',symbols=reviews,native_runtime='NOT_EXECUTED'))
 cp=ROOT/'Codex Work/Sub-Agents/Checkpoints/ui_visual.json';c=json.loads(cp.read_text(encoding='utf8'));c.update(updated_at_utc=now(),last_completed_file=path,last_completed_stable_id=reviews[-1]['stable_id'],current_partial_file=path,current_partial_symbol='Exact unreviewed endpoints in ui-exact-symbol-coverage.json; no unsaved partial body',latest_review_shard=str(shard.relative_to(ROOT)).replace('\\','/'));atomic_json(cp,c)
 print('Added',len(facts)*2,'ApiClient receipts; total',len(reviews))


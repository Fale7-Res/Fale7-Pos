from ui_incremental import ROOT,RESULTS,P,C,W,T,record as legacy_record
import sys,json
sys.path.insert(0,str(ROOT/'Codex Work/Verification-Tools'))
from knowledge_store import save_review,atomic_json,load_symbols,now

def test_symbol_reviews(path,facts):
    """Persist only manually supplied per-exact-span test reasoning; never infers analysis from names."""
    source=load_symbols(path); reviews=[]
    for line,col,kind,name,reason,inputs,outputs,relations,missing in facts:
        s,=[s for s in source['symbols'] if (s['startLine'],s['startColumn'],s['kind'],s['name'])==(line,col,kind,name)]
        reviews.append(dict(stable_id=s['stable_id'],status='ANALYZED',exact_signature=s['qualifiedSymbol'],exact_span=[line,col,s['endLine'],s['endColumn']],reasoning=reason,inputs=inputs,outputs=outputs,side_effects='Synthetic HTTP fixture/VM/assertion state only; source review, NOT_EXECUTED',trust_boundaries='Fixture contracts cannot prove server authorization or real transport',database='NOT_APPLICABLE direct DB; no live data touched',printing_ui='No native UI/physical printer invoked; real VM command bodies tested by fixture only',performance='Bounded test data; actual service latency/scale UNKNOWN',duplicates_conflicts='Exact syntax span and signature reconciled; no legacy heading promotion',issues_hypotheses=missing,tests_missing=missing,verified_callers_callees=relations))
    return reviews

def record(path,role,notes,tests,recommendations,symbols=None):
    if path.endswith('.cs') and symbols is None:
        raise ValueError('C# requires explicit exact-symbol review list, never heading promotion')
    source=load_symbols(path)
    legacy_record(path,role,notes,tests,recommendations)
    review=save_review('ui_visual',path,dict(file_review_status='ANALYZED_STATIC',file_reasoning=role,
        resource_binding_reasoning=[dict(anchor=a,reasoning=b) for a,b in notes],
        tests_and_limits=tests,recommendations=recommendations,symbols=symbols or [],
        observation='SOURCE_ONLY; no native screenshot/runtime evidence; no source or application state changed'))
    ledger=json.loads((RESULTS/'ui-ledger.json').read_text(encoding='utf-8-sig'))
    remaining=[i['path'] for i in ledger['files'] if not i.get('fileReviewed')]
    findings=json.loads((RESULTS/'ui-findings.json').read_text(encoding='utf-8-sig'))
    atomic_json(ROOT/'Codex Work/Sub-Agents/Checkpoints/ui_visual.json',dict(
        schema_version=1,updated_at_utc=now(),baseline=source['baseline'],last_completed_file=path,
        last_completed_stable_id=next((s['stable_id'] for s in reversed(symbols or []) if s.get('status')=='ANALYZED'),None),
        current_partial_file=None,current_partial_symbol=None,legacy_file_counts=ledger['counts'],
        exact_remaining_files=remaining,
        exact_remaining_symbols_reference='Knowledge/Source-Symbols/** minus ANALYZED IDs in Knowledge/Reviews/ui_visual/**; legacy name-heading matches excluded',
        new_findings=[f['id'] for f in findings['findings']],pending_independent_challenges='Source findings remain subject to parent canonical registry challenge; no screenshot verified',
        blockers=['Native observation unavailable'],next_action='Continue remaining original UI/test files with exact symbol reasoning; reconcile older reports separately',
        latest_review_shard=str(review.relative_to(ROOT)).replace('\\','/')))


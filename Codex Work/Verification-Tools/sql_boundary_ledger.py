"""Audit-only SQL boundary evidence persistence. Does not execute SQL/application code."""
from pathlib import Path
import hashlib,json
from knowledge_store import atomic_json,now,load_symbols,stable_symbol_id,ROOT,WORK
OUT=WORK/'Deep-Audit/Security/SQL-Boundary-Review'
def save_batch(label,entries,next_action,remaining):
    out=[]
    for e in entries:
        p=e['path'];d=load_symbols(p);raw=(ROOT/p).read_bytes()
        assert hashlib.sha256(raw).hexdigest()==d['worktree_sha256']
        a=e['call_line'];b=e.get('end_line',a)
        assert 1<=a<=b<=len(raw.decode('utf-8-sig').splitlines())
        syms=[s for s in d['symbols'] if s['isCallable'] and s['startLine']<=a<=s['endLine']]
        sym=min(syms,key=lambda s:(s['endLine']-s['startLine'],s['endColumn']-s['startColumn'])) if syms else None
        if sym: assert stable_symbol_id(p,d['git_blob_sha256'],sym)==sym['stable_id']
        out.append({**e,'source_git_blob_sha256':d['git_blob_sha256'],'worktree_sha256':d['worktree_sha256'],'enclosing_symbol':{k:sym[k] for k in ['stable_id','kind','qualifiedSymbol','startLine','endLine','startColumn','endColumn']} if sym else None,'review_method':'Independent manual source-only SQL boundary analysis','runtime_sql_execution':'NOT_EXECUTED'})
    doc={'batch':label,'baseline':'c35816ecae49633674775dd32061c6ee92f94aaf','reviewer':'ui_independent_challenge','updated_at_utc':now(),'calls':out,'scope_limit':'SQL construction/value-binding only; parameterization does not certify authorization/transactions or overall code security.'}
    atomic_json(OUT/(label+'.json'),doc)
    done=[]
    for f in sorted(OUT.glob('batch-*.json')):
        x=json.loads(f.read_text(encoding='utf8'));done.extend({'path':c['path'],'call_line':c['call_line'],'classification':c['classification'],'batch':f.name} for c in x['calls'])
    atomic_json(WORK/'Sub-Agents/Checkpoints/sql_boundary_reviewer.json',{'updated_at_utc':now(),'status':'IN_PROGRESS','reviewer':'ui_independent_challenge','completed_calls':done,'current':next_action,'remaining_queue':remaining,'next_action':next_action,'evidence_directory':'Codex Work/Deep-Audit/Security/SQL-Boundary-Review','runtime_sql_execution':'NOT_EXECUTED','source_changes':False})
    print(label,len(out),'calls saved;',len(done),'total manual boundary records')

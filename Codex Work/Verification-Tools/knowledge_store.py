"""Audit knowledge persistence only. Never imports or executes application code."""
from pathlib import Path
import json,hashlib,os,tempfile,datetime
ROOT=Path(__file__).resolve().parents[2]
WORK=ROOT/'Codex Work'
BASELINE='c35816ecae49633674775dd32061c6ee92f94aaf'
def sha256(data):return hashlib.sha256(data).hexdigest()
def now():return datetime.datetime.now(datetime.timezone.utc).isoformat()
def atomic_json(path,value):
    path=Path(path).resolve()
    if not path.is_relative_to(WORK.resolve()):raise ValueError('Writes restricted to Codex Work')
    path.parent.mkdir(parents=True,exist_ok=True)
    data=json.dumps(value,ensure_ascii=False,indent=2,allow_nan=False)+'\n'
    json.loads(data)
    fd,name=tempfile.mkstemp(prefix=path.name+'.',suffix='.tmp',dir=path.parent)
    try:
        with os.fdopen(fd,'w',encoding='utf8',newline='\n') as f:
            f.write(data);f.flush();os.fsync(f.fileno())
        os.replace(name,path)
    finally:
        if os.path.exists(name):os.unlink(name)
def stable_symbol_id(path,source_hash,symbol):
    identity=[path,source_hash,symbol['kind'],symbol.get('qualifiedSymbol') or symbol['name'],
              symbol['startLine'],symbol['startColumn'],symbol['endLine'],symbol['endColumn']]
    return 'sym-'+sha256(json.dumps(identity,ensure_ascii=False,separators=(',',':')).encode())
def load_symbols(path):
    p=WORK/'Knowledge/Source-Symbols'/Path(path+'.json')
    return json.loads(p.read_text(encoding='utf8'))
def save_review(agent,path,review):
    """One reviewer owns its shard. Explicit per-symbol reasoning is mandatory for promotion."""
    source=load_symbols(path);valid={s['stable_id']:s for s in source['symbols']}
    raw=(ROOT/path).read_bytes()
    if sha256(raw)!=source['worktree_sha256']:raise ValueError('Source changed; review must be invalidated')
    old_path=WORK/'Knowledge/Reviews'/agent/Path(path+'.json')
    old=json.loads(old_path.read_text(encoding='utf8')) if old_path.exists() else None
    reviewed=review.get('symbols',[])
    for r in reviewed:
        if r['stable_id'] not in valid:raise ValueError('Unknown symbol identity')
        if r.get('status')=='ANALYZED':
            for key in ('reasoning','inputs','outputs','side_effects','trust_boundaries','database','printing_ui','performance','duplicates_conflicts','issues_hypotheses','tests_missing','verified_callers_callees'):
                if key not in r:raise ValueError('Missing symbol review field '+key)
            if len(r['reasoning'].strip())<50:raise ValueError('Insufficient explicit reasoning')
    completed={r['stable_id'] for r in reviewed if r.get('status')=='ANALYZED'}
    if old:
        old_done={r['stable_id'] for r in old.get('symbols',[]) if r.get('status')=='ANALYZED'}
        if not old_done.issubset(completed):raise ValueError('Refusing silent loss of previously analyzed symbols')
        # Store predecessor before a real change; no history churn for identical payload.
        if old.get('review')!=review and old.get('symbols')!=review.get('symbols'):
            history=WORK/'Knowledge/History'/agent/Path(path+'.'+sha256(json.dumps(old,sort_keys=True).encode())[:16]+'.json')
            if not history.exists():atomic_json(history,old)
    payload={'schema_version':1,'baseline':BASELINE,'path':path,'source_git_blob_sha256':source['git_blob_sha256'],
             'worktree_sha256':source['worktree_sha256'],'reviewer':agent,'updated_at_utc':now(),**review,
             'remaining_callable_ids':[s['stable_id'] for s in source['symbols'] if s['isCallable'] and s['stable_id'] not in completed]}
    atomic_json(old_path,payload)
    return old_path

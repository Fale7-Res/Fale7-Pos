"""Persistent source-only native/process/timer/network audit evidence; never invokes app code."""
from pathlib import Path
import json,hashlib,subprocess
from knowledge_store import atomic_json,now,load_symbols,stable_symbol_id,BASELINE,WORK,ROOT
OUT=WORK/'Deep-Audit/Security/Native-Process-Timer-Network'
def evidence(path,a,b,note):
 d=load_symbols(path);raw=(ROOT/path).read_bytes();assert hashlib.sha256(raw).hexdigest()==d['worktree_sha256']
 assert 1<=a<=b<=len(raw.decode('utf-8-sig').splitlines())
 syms=[s for s in d['symbols'] if s['startLine']<=a<=s['endLine']];cs=[s for s in syms if s['isCallable']]
 s=min(cs or syms,key=lambda s:(s['endLine']-s['startLine'],s['endColumn']-s['startColumn'])) if syms else None
 if s:assert stable_symbol_id(path,d['git_blob_sha256'],s)==s['stable_id']
 return {'path':path,'startLine':a,'endLine':b,'source_git_blob_sha256':d['git_blob_sha256'],'worktree_sha256':d['worktree_sha256'],'symbol':{k:s[k] for k in ['stable_id','kind','qualifiedSymbol','startLine','endLine','startColumn','endColumn','isCallable']} if s else None,'evidence':note}
def batch(label,records,remaining):
 doc={'baseline':BASELINE,'reviewer':'ui_independent_challenge','updated_at_utc':now(),'records':records,'execution':'SOURCE_ONLY_NO_APP_NETWORK_FILE_OR_NATIVE_CALL_EXECUTED'}
 atomic_json(OUT/(label+'.json'),doc)
 done=[]
 for p in sorted(OUT.glob('batch-*.json')):
  d=json.loads(p.read_text(encoding='utf-8'));done.extend({'id':x['id'],'batch':p.name} for x in d['records'])
 atomic_json(WORK/'Sub-Agents/Checkpoints/native_network_reviewer.json',{'updated_at_utc':now(),'status':'IN_PROGRESS','completed_boundaries':done,'remaining_queue':remaining,'next_action':remaining[0] if remaining else 'Validate finite coverage and persist handoff','scope_limit':'Finding/boundary source review, not exhaustive methods/security clearance','execution':'SOURCE_ONLY'})
 print(label,len(records),'boundaries saved;',len(done),'total')

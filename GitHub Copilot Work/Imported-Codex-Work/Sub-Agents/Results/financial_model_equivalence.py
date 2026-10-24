"""Carry only exact source-span text equality from actual analyzed predecessor plus independently authored current parent/delta reasoning. Audit-only; does not infer from names."""
from pathlib import Path
import sys,json,hashlib,copy
sys.path.insert(0,str(Path('Codex Work/Verification-Tools').resolve()));import knowledge_store as k
from financial_manual_symbols import Review
def spantext(path,s):
 lines=(k.ROOT/path).read_text(encoding='utf-8-sig').splitlines(keepends=True)
 a,b=s['startLine']-1,s['endLine']-1
 return lines[a][s['startColumn']-1:s['endColumn']-1] if a==b else lines[a][s['startColumn']-1:]+''.join(lines[a+1:b])+lines[b][:s['endColumn']-1]
def finish(current,predecessor,context,next_action):
 assert len(context)>100
 oldsource=k.load_symbols(predecessor); newsource=current.source
 for p,ss in [(predecessor,oldsource),(current.path,newsource)]:assert k.sha256((k.ROOT/p).read_bytes())==ss['worktree_sha256']
 oldp=k.WORK/'Knowledge/Reviews/financial_integrity'/(predecessor+'.json'); old=json.loads(oldp.read_text(encoding='utf8'))
 oldr={s['stable_id']:s for s in old['symbols'] if s['status']=='ANALYZED'}
 oldbyhash={}
 for s in oldsource['symbols']:
  if s['isCallable'] and s['stable_id'] in oldr:
   t=spantext(predecessor,s);oldbyhash.setdefault(k.sha256(t.encode()),[]).append((s,t))
 manual={r['stable_id'] for r in current.records}; carried=[]
 for s in newsource['symbols']:
  if not s['isCallable'] or s['stable_id'] in manual:continue
  t=spantext(current.path,s);h=k.sha256(t.encode());matches=oldbyhash.get(h,[])
  parentprefix=(k.ROOT/current.path).read_text(encoding='utf-8-sig').splitlines()[s['startLine']-1][:s['startColumn']-1]
  if len(matches)>1:matches=[(ps,pt) for ps,pt in matches if (k.ROOT/predecessor).read_text(encoding='utf-8-sig').splitlines()[ps['startLine']-1][:ps['startColumn']-1]==parentprefix]
  assert len(matches)==1,(current.path,s['startLine'],'unmatched or ambiguous requires manual review')
  prev,pt=matches[0]; assert pt==t
  rec=copy.deepcopy(oldr[prev['stable_id']]);rec.update(stable_id=s['stable_id'],signature=s['qualifiedSymbol'],span=[s['startLine'],s['startColumn'],s['endLine'],s['endColumn']],coverage_classification='EXACT_SOURCE_EQUIVALENCE_PLUS_MANUAL_CURRENT_MODEL_CONTEXT',equivalence=dict(predecessor_path=predecessor,predecessor_stable_id=prev['stable_id'],predecessor_review_path=str(oldp.relative_to(k.WORK)).replace('\\','/'),predecessor_review_sha256=k.sha256(oldp.read_bytes()),source_span_utf8_lf_sha256=h,text_equality_verified=True,current_parent_context=context,not_fresh_independent_reread=True))
  rec['reasoning']='Exact source-span text equals analyzed predecessor '+prev['stable_id']+'. Current model applicability independently checked: '+context+' Predecessor block-specific reasoning remains applicable: '+rec['reasoning']
  rec['verified_callers_callees']='Current parent BuildTargetModel manually reviewed; builder invokes this same block. Historical line references in reused analysis below are anchored to predecessor '+predecessor+', not automatically current coordinates. '+rec['verified_callers_callees']
  current.records.append(rec);carried.append(dict(path=current.path,stable_id=s['stable_id'],**rec['equivalence']))
 for rec in current.records:
  if rec['stable_id'] in manual:rec['coverage_classification']='MANUAL_CHANGED_BLOCK_AND_CURRENT_CONTEXT'
 current.records.sort(key=lambda x:x['span'])
 current.save(next_action)
 eqp=k.WORK/'Sub-Agents/Results/financial-model-equivalence-ledger.json';eq=json.loads(eqp.read_text(encoding='utf8')) if eqp.exists() else dict(baseline=k.BASELINE,classification='Source-equivalence evidence is not fresh independent rereading or runtime validation.',files=[])
 eq['files']=[x for x in eq['files'] if x['path']!=current.path]+[dict(path=current.path,predecessor=predecessor,manual_changed_stable_ids=sorted(manual),equivalent_count=len(carried),current_parent_context=context,equivalences=carried,at_utc=k.now())];k.atomic_json(eqp,eq)
 print('Verified exact-body equivalence',len(carried),'manual changed',len(manual))

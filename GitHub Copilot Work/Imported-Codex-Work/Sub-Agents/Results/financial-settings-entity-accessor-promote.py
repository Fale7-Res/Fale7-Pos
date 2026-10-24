from pathlib import Path
import sys,json,runpy
sys.path.insert(0,str(Path('Codex Work/Verification-Tools').resolve()))
import knowledge_store as k
for d in json.loads((k.WORK/'Sub-Agents/Handoffs/financial-settings-entity-accessor-reviews.json').read_text(encoding='utf8')):
 p=d['path'];known=set()
 for agent in ('financial_integrity','financial_coordinator'):
  t=k.WORK/'Knowledge/Reviews'/agent/(p+'.json')
  if t.exists():known.update(r['stable_id'] for r in json.loads(t.read_text(encoding='utf8')).get('symbols',[]) if r.get('status')=='ANALYZED')
 assert not known.intersection(r['stable_id'] for r in d['symbols'])
 oldpath=k.WORK/'Knowledge/Reviews/financial_integrity'/(p+'.json')
 oldsymbols=json.loads(oldpath.read_text(encoding='utf8')).get('symbols',[]) if oldpath.exists() else []
 k.save_review('financial_integrity',p,dict(file_review_status='PARTIAL_STATIC_CALLABLE_REVIEW',evidence='Manually read complete source and exact caller transaction chain; each symbol individually reasoned. No execution.',symbols=oldsymbols+d['symbols']))
 print('Saved',p,len(d['symbols']),flush=True)
runpy.run_path('Codex Work/Sub-Agents/Results/financial-current-union-checkpoint.py',run_name='__main__')

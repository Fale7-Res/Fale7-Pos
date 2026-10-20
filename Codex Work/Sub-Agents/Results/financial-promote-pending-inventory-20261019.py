from pathlib import Path
import sys,json,runpy
sys.path.insert(0,str(Path('Codex Work/Verification-Tools').resolve()))
import knowledge_store as k
paths=['financial-saverecipe-reviewed-pending-save.json','financial-stockmovements-reviewed-pending-save.json']
for name in paths:
 d=json.loads((k.WORK/'Sub-Agents/Handoffs'/name).read_text(encoding='utf8'))
 p=d['path']; known=set()
 for reviewer in ('financial_integrity','financial_coordinator'):
  f=k.WORK/'Knowledge/Reviews'/reviewer/(p+'.json')
  if f.exists():known.update(r['stable_id'] for r in json.loads(f.read_text(encoding='utf8')).get('symbols',[]) if r.get('status')=='ANALYZED')
 assert not known.intersection(r['stable_id'] for r in d['symbols']), 'Already analyzed: refuse duplicate promotion'
 for r in d['symbols']:r['status']='ANALYZED'
 k.save_review('financial_integrity',p,dict(file_review_status='COMPLETE_STATIC_CALLABLE_REVIEW',evidence=d['review_method']+' Validated original knowledge_store.save_review, no application execution.',symbols=d['symbols']))
 print('Saved',p,len(d['symbols']),flush=True)
runpy.run_path('Codex Work/Sub-Agents/Results/financial-current-union-checkpoint.py',run_name='__main__')

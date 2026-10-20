from pathlib import Path
import sys,json,runpy
sys.path.insert(0,str(Path('Codex Work/Verification-Tools').resolve()))
import knowledge_store as k
d=json.loads((k.WORK/'Sub-Agents/Handoffs/financial-inventoryquery-variance-through222-reviewed.json').read_text(encoding='utf8'));p=d['path']; old=json.loads((k.WORK/'Knowledge/Reviews/financial_integrity'/(p+'.json')).read_text(encoding='utf8')); known={r['stable_id'] for r in old['symbols'] if r.get('status')=='ANALYZED'}
r=k.WORK/'Knowledge/Reviews/financial_coordinator'/(p+'.json')
if r.exists():known.update(x['stable_id'] for x in json.loads(r.read_text(encoding='utf8')).get('symbols',[]) if x.get('status')=='ANALYZED')
assert not known.intersection(x['stable_id'] for x in d['symbols'])
k.save_review('financial_integrity',p,dict(file_review_status='PARTIAL_STATIC_CALLABLE_REVIEW',evidence='Individual bounded methods18-222, remaining exact IDs not promoted. No runtime.',symbols=old['symbols']+d['symbols']))
print('Added',len(d['symbols']),flush=True)
runpy.run_path('Codex Work/Sub-Agents/Results/financial-current-union-checkpoint.py',run_name='__main__')

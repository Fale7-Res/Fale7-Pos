from pathlib import Path
import sys,json,runpy,hashlib
sys.path.insert(0,str(Path('Codex Work/Verification-Tools').resolve()))
import knowledge_store as k
f=k.WORK/'Sub-Agents/Results/financial-findings.json'
a=json.loads(f.read_text(encoding='utf8')); x=next(x for x in a if x['id']=='FIN-005')
old=json.loads(json.dumps(x)); hist=k.WORK/'Knowledge/History/financial_integrity/financial-findings'/('FIN-005.range-end.'+k.sha256(json.dumps(old,sort_keys=True).encode())[:16]+'.json')
k.atomic_json(hist,old)
for r in x['sources']:
 if r['path'].endswith('PurchasingEntities.cs') and r['start']==75 and r['end']==112:
  r['end']=111
  lines=(k.ROOT/r['path']).read_text(encoding='utf8').splitlines(); assert len(lines)==111
  r['source_excerpt']='\n'.join(lines[74:111])+'\n'
x.setdefault('citation_correction_history',[]).append(dict(at_utc=k.now(),reason='Actual source has111 lines; final payment/ledger range75-112 corrected to75-111. No reasoning or finding count change.',predecessor=str(hist.relative_to(k.WORK)),parent_gate='Deep-Audit/Database-and-Finance/financial-evidence-symbol-reference-gate.json'))
k.atomic_json(f,a); print('FIN005 end corrected111, predecessor preserved',flush=True)
d=json.loads((k.WORK/'Sub-Agents/Handoffs/financial-inventoryquery-first131-reviewed.json').read_text(encoding='utf8')); p=d['path']; known=set()
for agent in ('financial_integrity','financial_coordinator'):
 t=k.WORK/'Knowledge/Reviews'/agent/(p+'.json')
 if t.exists():known.update(r['stable_id'] for r in json.loads(t.read_text(encoding='utf8')).get('symbols',[]) if r.get('status')=='ANALYZED')
assert not known.intersection(r['stable_id'] for r in d['symbols'])
k.save_review('financial_integrity',p,dict(file_review_status='PARTIAL_STATIC_CALLABLE_REVIEW',evidence=d['review_method'],symbols=d['symbols']))
print('Saved InventoryQuery37 partial',flush=True)
runpy.run_path('Codex Work/Sub-Agents/Results/financial-current-union-checkpoint.py',run_name='__main__')

from pathlib import Path
import sys,json,runpy
sys.path.insert(0,str(Path('Codex Work/Verification-Tools').resolve()))
import knowledge_store as k
for d in json.loads((k.WORK/'Sub-Agents/Handoffs/financial-pricing-deduction-individual-reviews.json').read_text(encoding='utf8')):
 p=d['path'];known=set()
 for agent in ('financial_integrity','financial_coordinator'):
  t=k.WORK/'Knowledge/Reviews'/agent/(p+'.json')
  if t.exists():known.update(r['stable_id'] for r in json.loads(t.read_text(encoding='utf8')).get('symbols',[]) if r.get('status')=='ANALYZED')
 assert not known.intersection(r['stable_id'] for r in d['symbols'])
 k.save_review('financial_integrity',p,dict(file_review_status='COMPLETE_STATIC_CALLABLE_REVIEW',evidence='Manually read complete source and exact caller transaction chain; each symbol individually reasoned. No execution.',symbols=d['symbols']))
 print('Saved',p,len(d['symbols']),flush=True)
f=k.WORK/'Sub-Agents/Handoffs/financial-pricing-overflow-fin016-extension.json';d=json.loads(f.read_text(encoding='utf8'));old=json.loads(json.dumps(d));hist=k.WORK/'Knowledge/History/financial_integrity/financial-pricing-extension'/('prior-'+k.sha256(json.dumps(old,sort_keys=True).encode())[:16]+'.json');k.atomic_json(hist,old)
d['pricing_evidence'][0]=d['pricing_evidence'][0].replace('price authoritative at52, offer resolved54','price authoritative at53, offer resolved55')
d['pricing_evidence'][1]=d['pricing_evidence'][1].replace('TotalPrice at59','TotalPrice at60')
d.setdefault('citation_corrections',[]).append(dict(at_utc=k.now(),reason='Current exact full-file read corrects MenuPricingService price53/resolve55/frozen multiply60 narrative citations; same baseline and reasoning.',predecessor=str(hist.relative_to(k.WORK))))
k.atomic_json(f,d);print('Corrected pricing narrative lines53/55/60, preserved history',flush=True)
runpy.run_path('Codex Work/Sub-Agents/Results/financial-current-union-checkpoint.py',run_name='__main__')

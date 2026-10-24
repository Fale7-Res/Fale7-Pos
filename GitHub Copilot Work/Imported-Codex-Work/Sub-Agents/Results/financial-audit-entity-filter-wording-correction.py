from pathlib import Path
import sys,json,runpy
sys.path.insert(0,str(Path('Codex Work/Verification-Tools').resolve()))
import knowledge_store as k
p='src/Falah.Infrastructure/Persistence/Entities/AuditEntities.cs';f=k.WORK/'Knowledge/Reviews/financial_integrity'/(p+'.json');d=json.loads(f.read_text(encoding='utf8'));n=0
for r in d['symbols']:
 for key,value in list(r.items()):
  if isinstance(value,str) and 'filter authorization uses UserId not name' in value:
   r[key]=value.replace('filter authorization uses UserId not name','actor filter uses UserId; authorization belongs endpoint/command, not property/filter');n+=1
assert n==4,n
k.save_review('financial_integrity',p,dict(file_review_status=d['file_review_status'],evidence=d['evidence']+' UserName filter/authorization terminology corrected after root spot-check; no new primary analysis count.',symbols=d['symbols']))
print('Corrected4 ancillary phrases; predecessor automatically preserved',flush=True)
runpy.run_path('Codex Work/Sub-Agents/Results/financial-current-union-checkpoint.py',run_name='__main__')

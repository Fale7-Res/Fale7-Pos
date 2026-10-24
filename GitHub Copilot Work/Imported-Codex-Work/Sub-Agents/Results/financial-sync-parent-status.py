import sys,json
from pathlib import Path
sys.path.insert(0,str(Path('Codex Work/Verification-Tools').resolve()));import knowledge_store as k
p=k.WORK/'Sub-Agents/Results/financial-findings.json';fs=json.loads(p.read_text(encoding='utf8'));ch=json.loads((k.WORK/'Deep-Audit/Database-and-Finance/financial-parent-challenges.json').read_text(encoding='utf8'));by={x['id']:x for x in ch['findings']}
for f in fs:
 if f['id'] not in by:continue
 c=by[f['id']];f.setdefault('status_history',[]).append(dict(previous=f['status'],reason='Root independent static source challenge saved; no runtime reproduction.',at_utc=k.now()))
 f['status']='CONFIRMED_STATIC_INDEPENDENT_REVIEW';f['independent_challenge']=dict(reviewer='root',artifact='Deep-Audit/Database-and-Finance/financial-parent-challenges.json',disposition=c['disposition'],reasoning=c['reasoning'],counterevidence=c['counterevidence'],sources=c['sources'])
 if f['id']=='FIN-009':
  for s in f['sources']:
   if s['path']=='src/Falah.Infrastructure/Persistence/FalahDbContext.cs':
    s['reference_correction']='Old148-152 pointedtoExpenses; actualBusinessDaymapping137-141 independentlyread andverified.';s['start']=137;s['end']=141;s['source_excerpt']='\n'.join((k.ROOT/s['path']).read_text(encoding='utf-8-sig').splitlines()[136:141])
k.atomic_json(p,fs)
print('Updated FIN007/008/009 withactualrootchallenge; corrected FIN009 mapping137-141; FIN014 remainsparentcanonicalwithownchallengeartifact')

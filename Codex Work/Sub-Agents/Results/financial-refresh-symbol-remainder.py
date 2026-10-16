from pathlib import Path
import sys,json
sys.path.insert(0,str(Path('Codex Work/Verification-Tools').resolve()));import knowledge_store as k
l=json.loads((k.WORK/'Sub-Agents/Results/financial-ledger.json').read_text(encoding='utf-8-sig'));paths=sorted({r['path'] for r in l['records']+l['remaining']});remaining=[];done=[];missing=[]
for path in paths:
 try:s=k.load_symbols(path)
 except FileNotFoundError:missing.append(path);continue
 rp=k.WORK/'Knowledge/Reviews/financial_integrity'/Path(path+'.json');r=json.loads(rp.read_text(encoding='utf8')) if rp.exists() else {};ids={x['stable_id'] for x in r.get('symbols',[]) if x.get('status')=='ANALYZED'}
 for x in s['symbols']:
  if not x['isCallable']:continue
  item=dict(path=path,stable_id=x['stable_id'],kind=x['kind'],signature=x['qualifiedSymbol'],span=[x['startLine'],x['startColumn'],x['endLine'],x['endColumn']])
  (done if x['stable_id'] in ids else remaining).append(item)
k.atomic_json(k.WORK/'Sub-Agents/Results/financial-symbol-remaining.json',dict(baseline=k.BASELINE,purpose='Exact indexed remainder, NOT review proof. Legacy file completion is not callable completion.',scope_paths=paths,scope_count=len(paths),analyzed_count=len(done),remaining_count=len(remaining),missing_identity_paths=missing,analyzed_ids=[x['stable_id'] for x in done],remaining_symbols=remaining,updated_at_utc=k.now()))
p=k.WORK/'Sub-Agents/Checkpoints/financial_integrity.json';cp=json.loads(p.read_text(encoding='utf8'));cp['remaining_files']=[x['path'] for x in l['remaining']];cp['remaining_function_ids_reference']='Sub-Agents/Results/financial-symbol-remaining.json#/remaining_symbols (exact original327file scope)';cp['analyzed_callable_count']=len(done);cp['remaining_indexed_callable_count']=len(remaining);cp['updated_at_utc']=k.now();k.atomic_json(p,cp)
print('Scope',len(paths),'ANALYZED',len(done),'remaining indexed callables',len(remaining),'identity missing',len(missing))
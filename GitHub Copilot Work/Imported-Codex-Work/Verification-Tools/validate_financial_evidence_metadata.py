"""Source/reference reconciliation only; not a second semantic audit or runtime certificate."""
from knowledge_store import *
master=json.loads((WORK/'Deep-Audit/MASTER_FINDINGS.json').read_text(encoding='utf8'))
cache={};errors=[];records=[]
def spans(s):
 for key in ('line_ranges','evidence_spans','ranges'):
  if key in s:return s[key]
 if isinstance(s.get('lines'),list) and len(s['lines'])==2 and all(isinstance(x,int) for x in s['lines']):return [s['lines']]
 for start,end in [('start','end'),('start_line','end_line'),('startLine','endLine')]:
  if start in s and end in s:return [[s[start],s[end]]]
 return []
for finding in master['findings']:
 if not finding['id'].startswith('FIN'):continue
 refs=[];missing=[]
 for source in finding['source_record'].get('sources',[]):
  p=source['path']
  if p not in cache:
   raw=(ROOT/p).read_bytes();mapping=load_symbols(p)
   assert sha256(raw)==mapping['worktree_sha256'],p
   cache[p]=(raw.decode('utf-8-sig').splitlines(),mapping)
  lines,mapping=cache[p];ranges=spans(source)
  if not ranges:missing.append({'path':p,'reason':'No recognized exact evidence line range; hashes alone do not supply one.'})
  for a,b in ranges:
   if not isinstance(a,int) or not isinstance(b,int) or not 0<a<=b<=len(lines):errors.append({'id':finding['id'],'path':p,'range':[a,b],'error':'Invalid line range'});continue
  quote=source.get('source_excerpt')
  if quote is not None and len(ranges)==1:
   a,b=ranges[0]
   if '\n'.join(lines[a-1:b]).strip()!=quote.replace('\r\n','\n').strip():errors.append({'id':finding['id'],'path':p,'error':'Quoted excerpt differs from stated current span'})
  contexts=[{'stable_id':s['stable_id'],'kind':s['kind'],'signature':s.get('qualifiedSymbol') or s['name'],
     'span':[s['startLine'],s['startColumn'],s['endLine'],s['endColumn']]}
    for s in mapping['symbols'] if s['kind'] not in ('FileScopedNamespaceDeclaration','NamespaceDeclaration')
      and any(s['startLine']<=b and s['endLine']>=a for a,b in ranges)]
  refs.append({'path':p,'source_git_blob_sha256':mapping['git_blob_sha256'],'worktree_sha256':mapping['worktree_sha256'],
     'evidence_ranges':ranges,'overlapping_symbol_contexts':contexts,
     'qualification':'Mechanical symbol/span binding, NOT additional function reasoning or confirmation that every cited span is semantically relevant.'})
 records.append({'id':finding['id'],'severity':finding['severity'],'independent_status':finding['independent_verification']['status'],
    'references':refs,'missing_exact_references':missing,'runtime_status':'SEE_ORIGINAL_EVIDENCE; NO_NEW_EXECUTION',
    'source_record_sha256':sha256(json.dumps(finding['source_record'],sort_keys=True).encode())})
payload={'at_utc':now(),'baseline':BASELINE,'canonical_registry_sha256':sha256((WORK/'Deep-Audit/MASTER_FINDINGS.json').read_bytes()),
 'kind':'Financial source/evidence metadata quality gate, not financial completion',
 'financial_findings':len(records),'source_paths':len(cache),'errors':errors,
 'references_missing_exact_ranges':sum(len(r['missing_exact_references']) for r in records),'findings':records,
 'limitations':'Independent semantic challenges remain their separately authored artifacts. This mapping does not prove schedules ran, derive new analyzed callables, replace historical evidence or certify all mandatory finding fields.'}
atomic_json(WORK/'Deep-Audit/Database-and-Finance/financial-evidence-symbol-reference-gate.json',payload)
print(json.dumps({k:v for k,v in payload.items() if k not in ('findings','limitations')}))
assert not errors

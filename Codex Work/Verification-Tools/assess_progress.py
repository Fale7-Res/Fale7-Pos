"""Snapshot saved review evidence; indexing/hashing never becomes an audit receipt."""
from pathlib import Path
import json,subprocess,hashlib,collections,datetime,re,sys
R=Path(__file__).resolve().parents[2];W=R/'Codex Work';O=W/'Deep-Audit'
def load(p):return json.loads((W/p).read_text(encoding='utf-8-sig'))
def digest(b):return hashlib.sha256(b).hexdigest()
base='c35816ecae49633674775dd32061c6ee92f94aaf'
head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=R,text=True).strip()
tracked=subprocess.check_output(['git','ls-tree','-r','--name-only',base],cwd=R,text=True).splitlines()
tree=subprocess.check_output(['git','ls-tree','-r',base],cwd=R,text=True).splitlines()
objects={line.split('\t',1)[1]:line.split()[2] for line in tree}
proc=subprocess.Popen(['git','cat-file','--batch'],cwd=R,stdin=subprocess.PIPE,stdout=subprocess.PIPE)
source_hashes={};issues=[]
for path in tracked:
 proc.stdin.write((objects[path]+'\n').encode());proc.stdin.flush()
 hdr=proc.stdout.readline();size=int(hdr.split()[2]);blob=proc.stdout.read(size);proc.stdout.read(1)
 raw=(R/path).read_bytes();eq=raw.replace(b'\r\n',b'\n')==blob.replace(b'\r\n',b'\n')
 if not eq:issues.append({'path':path,'problem':'SOURCE_DIFFERS_FROM_BASELINE'})
 source_hashes[path]={'worktree':digest(raw),'git_blob':digest(blob)}
proc.stdin.close();proc.wait()
scopes={};reviewed=set();partial=set();reportpaths=[];receipt_errors=[]
for name,key in [('financial','records'),('ui','files'),('printing','files')]:
 j=load(f'Sub-Agents/Results/{name}-ledger.json');records=j[key]
 if name=='financial':records=records+j.get('remaining',[])
 counts=collections.Counter(r['status'] for r in records);scopes[name]={'scope':len(records),'counts':dict(counts)}
 complete={'REVIEWED_STATIC_COMPLETE_FILE','REVIEWED_SOURCE','STATIC_FILE_REVIEWED'}
 for row in records:
  p=row['path'];st=row['status'];report=O/'File-Reports'/Path(p+'.md')
  if st in complete:
   reviewed.add(p)
   if not report.exists():receipt_errors.append({'path':p,'error':'REVIEW_LABEL_WITHOUT_REPORT'});continue
   text=report.read_text(encoding='utf-8-sig')
   expected=row.get('worktree_sha256',row.get('working_sha256',row.get('sha256')))
   # Some UI records sha256 is Git blob, while worktree_sha256 is explicit.
   if expected and expected!=source_hashes[p]['worktree']:receipt_errors.append({'path':p,'error':'RECEIPT_HASH_MISMATCH'})
   reportpaths.append(str(report.relative_to(W)).replace('\\','/'))
  elif 'PARTIAL' in st:partial.add(p)
 existing=list((O/'File-Reports').rglob('*.md'))
ui=load('Sub-Agents/Results/ui-function-review-ledger.json');index=load('Deep-Audit/Source-Inventory/Current/FUNCTION_INDEX.json')
declarations={d['id']:d for d in index['declarations']};mapped=[];function_errors=[]
for f in ui['functions']:
 if not f.get('functionReviewed'):continue
 d=declarations.get(f['id']);evidence=R/f['reviewEvidence']
 if not d or any(d[k]!=f[k] for k in ('path','startLine','endLine')) or not evidence.exists():function_errors.append(f['id'])
 else:mapped.append(f)
financial=load('Sub-Agents/Results/financial-findings.json');printing=load('Sub-Agents/Results/printing-findings.json');uif=load('Sub-Agents/Results/ui-findings.json')['findings']
challenges=(W/'Sub-Agents/Handoffs/printing-independent-challenges.md').read_text(encoding='utf8')
ids=re.findall(r'^\| ((?:FIN|UI)-\d+) \| Confirmed',challenges,re.M)
if '| UI customer reprint/kitchen | Confirmed' in challenges:ids.append('UI-012')
if '| UI delivery printing route cards | Confirmed' in challenges:ids.append('UI-009')
ids.extend(f['id'] for f in financial if f.get('status')=='CONFIRMED_STATIC_INDEPENDENT_REVIEW')
ids.extend(f['id'] for f in printing if f.get('independent_challenge',{}).get('disposition','').startswith('CONFIRMED'))
mandatory=['EXECUTIVE_SUMMARY.md','ARCHITECTURE_AND_SOURCE_MAP.md','SOURCE_INVENTORY.csv','FUNCTION_INDEX.json','SECURITY_AND_BACKDOOR_AUDIT.md','DEPENDENCY_SECURITY_AND_SBOM.md','PICKUP_AND_DELIVERY_WORKFLOW_AUDIT.md','PRINTING_RELIABILITY_AND_PREVIEW.md','UI_UX_VISUAL_AUDIT.md','DATABASE_AND_FINANCIAL_INTEGRITY.md','PERFORMANCE_AND_MINIMUM_HARDWARE.md','CODE_DUPLICATION_AND_ARCHITECTURE.md','TEST_COVERAGE_AND_MISSING_TESTS.md','IMMEDIATE_FIXES_AND_FEATURES.md','FUTURE_IMPROVEMENTS_AND_FEATURES.md','MASTER_FINDINGS.json','AUDIT_COVERAGE.json','IMPLEMENTATION_ROADMAP.md']
deliverables=[]
for name in mandatory:
 found=[str(p.relative_to(W)).replace('\\','/') for p in W.rglob(name) if 'Recovered-' not in str(p)]
 deliverables.append({'name':name,'paths':found,'state':'EXISTS_DRAFT_NOT_FINAL' if found else 'MISSING_EXACT_DELIVERABLE'})
semantic=load('Deep-Audit/Source-Inventory/Current/evidence/semantic-diagnostics.json')
snapshot={'at_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'source_baseline':base,'current_head':head,'baseline_tracked_files':len(tracked),'hash_mismatches':issues,'source_review_receipt_errors':receipt_errors,'scopes':scopes,
 'full_file_review_receipts':len(reviewed),'partial_file_receipts':len(partial),'files_without_full_review':len(set(tracked)-reviewed),'individual_report_files':len(existing),'review_receipt_paths':sorted(reportpaths),
 'review_limits':'Full-file static review receipts verified for hashes/report presence; not all report reasoning independently re-audited. Full function review not implied.',
 'roslyn_declarations':len(declarations),'roslyn_callables':sum(bool(d.get('isCallable')) for d in declarations.values()),'semantic_project_statuses':dict(collections.Counter(p['status'] for p in semantic)),
 'ui_callable_reasoning_receipts':len(mapped),'ui_callable_receipt_errors':function_errors,'ui_callable_ledger_counts':ui['counts'],
 'all_scope_genuinely_reviewed_function_total':'UNKNOWN: financial has no precise per-callable reasoning ledger; printing separate endpoint reasoning; UI uses name-heading mapping, not independent complete function validation.',
 'independently_corroborated_saved_finding_ids':sorted(set(ids)),'independently_corroborated_saved_finding_count':len(set(ids)),
 'runtime_reproduced_findings_during_audit':0,'canonical_finding_records':{'financial':len(financial),'printing_workflow':len(printing),'ui':len(uif)},
 'finding_statuses':{'financial':dict(collections.Counter(f.get('status','UNSPECIFIED') for f in financial)),'ui':dict(collections.Counter(f.get('status','UNSPECIFIED') for f in uif))},'deliverables':deliverables}
out=O/(sys.argv[1] if len(sys.argv)>1 else 'phase-progress-evidence.json');out.write_text(json.dumps(snapshot,indent=2,ensure_ascii=False)+'\n',encoding='utf8')
print(json.dumps({k:v for k,v in snapshot.items() if k not in ('review_receipt_paths','deliverables','semantic_project_statuses')},ensure_ascii=False))

"""Current audit evidence/source checks; no publication, no operational data inspection."""
from knowledge_store import *
import subprocess,re
inventory=json.loads((WORK/'Deep-Audit/Source-Inventory/verified-baseline-hashes.json').read_text(encoding='utf8'))['files']
requests=[BASELINE+':'+r['path'] for r in inventory]
proc=subprocess.run(['git','cat-file','--batch'],input=('\n'.join(requests)+'\n').encode(),stdout=subprocess.PIPE,check=True,cwd=ROOT)
stream=proc.stdout;offset=0;mismatches=[];exact=0;newline_equivalent=0
for row in inventory:
 end=stream.index(b'\n',offset);header=stream[offset:end].split();assert len(header)==3 and header[1]==b'blob'
 size=int(header[2]);raw=stream[end+1:end+1+size];offset=end+1+size+1
 work=(ROOT/row['path']).read_bytes()
 if work==raw:exact+=1
 elif work.replace(b'\r\n',b'\n')==raw.replace(b'\r\n',b'\n'):newline_equivalent+=1
 else:mismatches.append(row['path'])
json_errors=[];json_count=0
for p in WORK.rglob('*.json'):
 if any(x in ('bin','obj','__pycache__') for x in p.relative_to(WORK).parts):continue
 try:json.loads(p.read_text(encoding='utf-8-sig'));json_count+=1
 except Exception as ex:json_errors.append({'path':str(p.relative_to(WORK)),'error_type':type(ex).__name__})
changed=subprocess.check_output(['git','diff','--name-only','HEAD','--'],text=True,cwd=ROOT).splitlines()
untracked=subprocess.check_output(['git','ls-files','--others','--exclude-standard'],text=True,cwd=ROOT).splitlines()
outside=[p for p in changed+untracked if not p.startswith('Codex Work/')]
broken=[];links=0;historical_link_origins=[]
origin_path=WORK/'Knowledge/History/financial_integrity/financial-report/legacy-markdown-origin-map.json'
origin_map={}
if origin_path.exists():
 for entry in json.loads(origin_path.read_text(encoding='utf8'))['snapshots']:
  origin_map[entry['snapshot'].replace('\\','/')]=entry
for rel in changed+untracked:
 if not rel.endswith('.md') or not rel.startswith('Codex Work/'):continue
 p=ROOT/rel
 if not p.exists():continue
 relative=p.relative_to(WORK).as_posix()
 origin=origin_map.get(relative)
 link_base=p.parent
 if origin:
  link_base=(WORK/origin['original_base']).resolve()
  assert link_base.is_relative_to(WORK.resolve())
  historical_link_origins.append({'snapshot':relative,'snapshot_sha256':sha256(p.read_bytes()),
      'declared_original_base':origin['original_base'],'origin_map':origin_path.relative_to(WORK).as_posix(),
      'qualification':'Verbatim archived document, links interpreted at declared original location; content not rewritten.'})
 text=p.read_text(encoding='utf-8-sig')
 text=re.sub(r'```.*?```','',text,flags=re.S)
 for dest in re.findall(r'\]\(([^)]+)\)',text):
  target=dest.split('#')[0].strip('<>')
  if not target or re.match(r'^[A-Za-z]+:',target):continue
  links+=1
  if not (link_base/target).exists():broken.append({'path':rel,'target':target})
coverage=json.loads((WORK/'Sub-Agents/Results/financial-coordinator-coverage.json').read_text(encoding='utf8'))
live_path=WORK/'Sub-Agents/Results/financial-current-union-progress.json'
live=json.loads(live_path.read_text(encoding='utf8')) if live_path.exists() else None
payload={'at_utc':now(),'baseline':BASELINE,'git_head':subprocess.check_output(['git','rev-parse','HEAD'],text=True,cwd=ROOT).strip(),
 'baseline_files':len(inventory),'exact_byte_matches':exact,'newline_only_matches':newline_equivalent,'baseline_mismatches':mismatches,
 'valid_json_files':json_count,'json_errors':json_errors,'changed_or_untracked_outside_workspace':outside,'checked_changed_markdown_relative_links':links,'broken_changed_markdown_links':broken,
 'historical_snapshot_link_origins':historical_link_origins,
 'financial_receipts':live['original_union_count'] if live else coverage['union_original_receipts'],
 'original_missing':live['original_remaining_count'] if live else coverage['remaining_original_count'],
 'publication':'ONE_INTERIM_PUSH_ALREADY_VERIFIED; NO_ADDITIONAL_COMMIT_OR_PUSH_PERFORMED',
 'limits':'No secrets/privacy certification of a new staged batch (none staged). Link check limited changed/untracked Markdown outside code fences. Identity/schema/source checks do not independently certify function reasoning, financial correctness, live database or hardware.'}
atomic_json(WORK/'Deep-Audit/Database-and-Finance/current-financial-validation.json',payload)
assert not mismatches and not json_errors and not outside
print(json.dumps({k:v for k,v in payload.items() if k not in ('broken_changed_markdown_links','historical_snapshot_link_origins')} | {'broken_changed_markdown_link_count':len(broken),'archived_snapshots_with_declared_link_base':len(historical_link_origins)}))

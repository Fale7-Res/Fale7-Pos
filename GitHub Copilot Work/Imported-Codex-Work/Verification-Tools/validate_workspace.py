"""Read-only content/Git checks, plus audit-only validation records and provenance updates."""
from pathlib import Path
import json,re,hashlib,subprocess,datetime
ROOT=Path(__file__).resolve().parents[2];W=ROOT/'Codex Work'
def sha(b):return hashlib.sha256(b).hexdigest()
def dump(p,j):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(j,indent=2,ensure_ascii=False)+'\n',encoding='utf8')
m=json.loads((W/'Workspace-Manifest.json').read_text(encoding='utf8'))
# Preserve original/destination-at-copy digests; record separately any working-document edits.
p=W/'Deep-Audit/Database-and-Finance/financial-summary.md'
t=p.read_text(encoding='utf8').replace('(reviews/financial-ledger.json)','(../../Sub-Agents/Results/financial-ledger.json)').replace('(reviews/financial-findings.json)','(../../Sub-Agents/Results/financial-findings.json)')
p.write_text(t,encoding='utf8')
credential=re.compile(r'''(?ix)(?<![\w/-])(?:["']?(?:password|pwd|pin|initialpin|currentpin|newpin|sessiontoken|accesstoken|apikey|bearertoken)["']?\s*[:=]\s*["'])([^"'\r\n]{3,})''')
flags=[];broken=[];files=[]
for p in sorted(W.rglob('*')):
 if not p.is_file() or any(x in ('bin','obj','__pycache__') for x in p.relative_to(W).parts):continue
 if p.relative_to(W).as_posix() in ('Workspace-Manifest.json','Verification-Tools/workspace-safety-validation.json','Conversation-Recovery/Git-Publication-File-List.txt'):continue
 data=p.read_bytes()
 try:t=data.decode('utf-8-sig')
 except UnicodeDecodeError:flags.append({'path':str(p.relative_to(ROOT)),'issue':'NON_UTF8_MANUAL_REVIEW'});continue
 for match in credential.finditer(t):
  value=match.group(1)
  if value.startswith(('$','<','{')) or value.lower() in ('redacted','placeholder','unknown','password','pin'):continue
  flags.append({'path':str(p.relative_to(ROOT)),'line':t.count('\n',0,match.start())+1,'issue':'POSSIBLE_CREDENTIAL_LITERAL_REQUIRES_REVIEW'})
 if p.suffix=='.md':
  for target in re.findall(r'\]\(([^)]+)\)',t):
   if target.startswith(('https:','http:','mailto:','#')):continue
   dest=target.split('#')[0].strip('<>')
   if dest and not (p.parent/dest).exists():broken.append({'path':str(p.relative_to(ROOT)),'target':target})
 files.append({'path':str(p.relative_to(ROOT)).replace('\\','/'),'sha256':sha(data),'bytes':len(data)})
eligible=[f['path'] for f in files]
ignored=subprocess.run(['git','check-ignore','--stdin'],input='\n'.join(eligible)+'\n',text=True,cwd=ROOT,capture_output=True).stdout.splitlines()
artifact_ignored=subprocess.run(['git','check-ignore','artifacts/local-runtime/data/placeholder.mdf'],cwd=ROOT,capture_output=True,text=True).returncode==0
assert artifact_ignored
changed=subprocess.check_output(['git','diff','--name-only'],cwd=ROOT,text=True).splitlines()
assert all(p.startswith('Codex Work/') for p in changed), 'Unexpected application-source change; stop publication'
for e in m['entries']:
 if e.get('destination_path'):
  p=ROOT/e['destination_path'];e['current_workspace_sha256']=sha(p.read_bytes())
  if e['current_workspace_sha256']!=e['destination_sha256']:
   e['working_copy_status']='DERIVED_DOCUMENT_OR_CONTINUED_AUDIT; original copy digest preserved above, original source retained'
m['generated_and_working_files']=files
m['last_validated_utc']=datetime.datetime.now(datetime.timezone.utc).isoformat()
dump(W/'Workspace-Manifest.json',m)
report={'timestamp_utc':m['last_validated_utc'],'eligible_text_files':len(files),'ignored_eligible_paths':ignored,'artifacts_rule_preserved':artifact_ignored,'possible_secret_flags':flags,'broken_markdown_links':broken,'source_changes':[],'publication':'User explicitly requested normal commit/push on main in latest message; publish only safe audit files after staged checks.','self_hash_exclusions':['Workspace-Manifest.json','Verification-Tools/workspace-safety-validation.json','Conversation-Recovery/Git-Publication-File-List.txt']}
dump(W/'Verification-Tools/workspace-safety-validation.json',report)
(W/'Conversation-Recovery/Git-Publication-File-List.txt').write_text('\n'.join(eligible)+'\n',encoding='utf8')
print(json.dumps({'files':len(files),'ignored':ignored,'flags':flags[:25],'flag_count':len(flags),'broken_links':broken},ensure_ascii=False))

"""Allowlisted recovery copy. Never moves runtime data or changes application files.
Review dry discovery and exclusions in Workspace-Manifest.json; reruns refuse changed destinations.
"""
from pathlib import Path
import csv, hashlib, json, re, subprocess, datetime, xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[2]
WORK=ROOT/'Codex Work'
AUDIT=ROOT/'artifacts/deep-audit-c35816e-20261016'
RUNTIME=ROOT/'artifacts/local-runtime'
BASE='c35816ecae49633674775dd32061c6ee92f94aaf'
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT)
def sha(data):return hashlib.sha256(data).hexdigest()
def rel(p):return p.relative_to(ROOT).as_posix()
def write_json(path,obj):
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(obj,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def sanitize(text):
    counts={'credential_literals':0,'phone_values':0}
    # Redact actual quoted credential assignments, not hashes, symbols, source line numbers or flags.
    pattern=r'''(?i)(\b(?:Pin|Password|Pwd|SessionToken|AccessToken|ApiKey|BearerToken)\b\s*[:=]\s*["'])([^"'\r\n]+)(["'])'''
    def credential(m):
        value=m.group(2)
        if any(t in value.lower() for t in ('redact','placeholder','example','not_analyzed','not_applicable','unknown')) or '$' in value or '[' in value or '(' in value:return m.group(0)
        counts['credential_literals']+=1
        return m.group(1)+'[REDACTED_CREDENTIAL]'+m.group(3)
    text=re.sub(pattern,credential,text)
    text,n=re.subn(r'(?<![\w])01[0125]\d{8}(?![\w])','[REDACTED_PHONE]',text)
    counts['phone_values']=n
    return text,counts
manifest=[]
def candidate(source,dest=None,reason='RECOVERED_CODEX_OUTPUT',origin='verified conversation-created output'):
    entry={'original_path':rel(source),'destination_path':rel(dest) if dest else None,'origin':origin,'purpose':reason,'original_sha256':None,'destination_sha256':None,'migration_status':'EXCLUDED','reason':reason,'original_preserved':True}
    if dest:
        original=source.read_bytes();entry['original_sha256']=sha(original)
        try:text=original.decode('utf-8-sig')
        except UnicodeDecodeError:
            entry['reason']='NON_UTF8_REQUIRES_MANUAL_REVIEW';manifest.append(entry);return
        sanitized,redactions=sanitize(text)
        payload=sanitized.encode('utf-8') if any(redactions.values()) else original
        entry['redactions']=redactions
        dest.parent.mkdir(parents=True,exist_ok=True)
        if dest.exists() and dest.read_bytes()!=payload:
            entry['migration_status']='CONFLICT_NOT_OVERWRITTEN';entry['reason']='Existing destination differs; manual reconciliation required';manifest.append(entry);return
        if not dest.exists():dest.write_bytes(payload)
        entry['destination_sha256']=sha(dest.read_bytes())
        if entry['destination_sha256']!=sha(payload):raise RuntimeError('Copy verification failed: '+rel(dest))
        entry['migration_status']='SANITIZED_COPY_VERIFIED' if payload!=original else 'EXACT_COPY_VERIFIED'
    manifest.append(entry)

if git('rev-parse','HEAD').decode().strip()!=BASE:raise RuntimeError('Unexpected source commit; stop before consolidation')
if git('branch','--show-current').decode().strip()!='main':raise RuntimeError('Unexpected branch')
# Classification is restricted to two roots whose creation is visible in this conversation.
for source in sorted(AUDIT.rglob('*')):
    if not source.is_file():continue
    sub=source.relative_to(AUDIT);parts=sub.parts
    if any(p in ('bin','obj','__pycache__') for p in parts) or source.suffix.lower() in ('.dll','.exe','.pdb','.cache'):
        candidate(source,reason='BUILD_OUTPUT_OR_BINARY_CACHE; remain ignored at original path');continue
    if source.suffix.lower()=='.trx':
        candidate(source,reason='RAW_TEST_RUN_WITH_MACHINE_IDENTITY; sanitized result summary generated separately');continue
    if source.suffix.lower() in ('.py','.ps1','.cs','.csproj'):
        dest=WORK/'Verification-Tools/Recovered-Source'/Path(str(sub)+'.source.txt')
        candidate(source,dest,'REFERENCE_ONLY_GENERATOR_OR_HELPER; do not execute stale generators');continue
    if parts[0]=='FILE_REPORTS':dest=WORK/'Deep-Audit/File-Reports'/Path(*parts[1:])
    elif parts[0]=='reviews':dest=WORK/'Sub-Agents/Results'/source.name
    elif parts[0]=='evidence':dest=WORK/'Deep-Audit/Source-Inventory/Recovered-Evidence'/Path(*parts[1:])
    elif source.name in ('FUNCTION_INDEX.json','SOURCE_INVENTORY.csv'):dest=WORK/'Deep-Audit/Source-Inventory/Recovered-Checkpoint'/source.name
    elif source.name in ('PICKUP_AND_DELIVERY_WORKFLOW_AUDIT.md','PRINTING_RELIABILITY_AND_PREVIEW.md','printing-summary.md'):dest=WORK/'Deep-Audit/Printing'/source.name
    elif source.name=='financial-summary.md':dest=WORK/'Deep-Audit/Database-and-Finance'/source.name
    elif source.name=='AUDIT_EXECUTION_PLAN.md':dest=WORK/'Deep-Audit'/source.name
    else:dest=WORK/'Sub-Agents/Checkpoints/Recovered-Other'/sub
    candidate(source,dest)

safe_runtime={
 'architecture-and-runtime.md':('Architecture/architecture-and-runtime.md','HISTORICAL_RUNTIME_DISCOVERY; not current live evidence'),
 'projects.json':('Architecture/projects.json','HISTORICAL_PROJECT_DEPENDENCIES'),
 'START-HERE.md':('Build-and-Runtime/Original-START-HERE.md','HISTORICAL_RUNTIME_INSTRUCTIONS; paths require new navigation guide'),
 'executables.json':('Build-and-Runtime/executables.json','HISTORICAL_PE_ARCHITECTURE_METADATA'),
 'runtime-verification.json':('Build-and-Runtime/runtime-verification.json','HISTORICAL_SANITIZED_TEST_SUMMARY'),
 'print-flow-verification.json':('Build-and-Runtime/print-flow-verification.json','HISTORICAL_FILE_TRANSPORT_TEST_SUMMARY'),
 'running-processes.json':('Build-and-Runtime/historical-running-processes.json','HISTORICAL_PID_SNAPSHOT; not proof processes still run'),
 'initialized-test-database.json':('Build-and-Runtime/isolated-database-identity.json','HISTORICAL_TEST_DATABASE_IDENTITY; no database files'),
 'build-release-final.log':('Build-and-Runtime/Build-Evidence/build-release-final.txt','REVIEWED_BUILD_LOG; no business data'),
 'build-release.log':('Build-and-Runtime/Build-Evidence/intermediate-build-lock-failure.txt','REVIEWED_BUILD_LOG; historical failure retained'),
 'restore.log':('Build-and-Runtime/Build-Evidence/restore.txt','REVIEWED_DEPENDENCY_RESTORE_LOG'),
}
for source in sorted(RUNTIME.rglob('*')):
    if not source.is_file():continue
    sub=source.relative_to(RUNTIME)
    if len(sub.parts)==1 and source.name in safe_runtime:
        dest,purpose=safe_runtime[source.name];candidate(source,WORK/dest,purpose);continue
    if len(sub.parts)==1 and source.suffix.lower()=='.ps1':
        candidate(source,WORK/'Verification-Tools/Recovered-Source/Runtime-Scripts'/Path(source.name+'.source.txt'),'REFERENCE_ONLY_PREAUDIT_RUNTIME_SCRIPT; live executable remains at original path; may mutate test DB so do not execute during audit');continue
    reason=('ACTIVE_DATABASE_OR_BACKUP_DO_NOT_READ_OR_MOVE' if source.suffix.lower() in ('.mdf','.ldf','.bak','.db','.snapshot') or 'data' in sub.parts else
      'CREDENTIAL_BEARING_RUNTIME_CONFIGURATION; excluded without reading' if 'printagent-file' in source.name else
      'PRIVATE_PRINTER_OUTPUT_OR_PREVIEW; excluded without reading' if source.suffix.lower() in ('.escpos','.png','.jpg','.pdf') else
      'RAW_RUNTIME_LOG_OR_ACTIVE_OPERATION_FILE; retain in place; not Git eligible')
    candidate(source,reason=reason)

request=Path(r'C:\Users\Mohamed\.codex\attachments\472032fa-c05a-47b1-993b-8b6a0cd5a95c\Pasted text.txt')
if request.exists():
    original=request.read_bytes();text=original.decode('utf-8-sig');sanitized,redactions=sanitize(text)
    dest=WORK/'Conversation-Recovery/Original-Deep-Audit-Request.md';dest.parent.mkdir(parents=True,exist_ok=True)
    payload=original if not any(redactions.values()) else sanitized.encode('utf-8')
    if dest.exists() and dest.read_bytes()!=payload:raise RuntimeError('Request destination already differs')
    dest.write_bytes(payload)
    manifest.append({'original_path':str(request),'destination_path':rel(dest),'origin':'user-provided attachment, not Codex-authored','purpose':'EXACT_AUDIT_REQUIREMENTS','original_sha256':sha(original),'destination_sha256':sha(payload),'migration_status':'EXACT_COPY_VERIFIED' if payload==original else 'SANITIZED_COPY_VERIFIED','original_preserved':True,'redactions':redactions})

unrelated=[{'path':rel(p),'classification':'PREEXISTING_ORIGIN_UNVERIFIED_NOT_COPIED','reason':'Not created by any visible action in this conversation; preserve unchanged'} for p in sorted((ROOT/'artifacts').iterdir()) if p.is_dir() and p not in (AUDIT,RUNTIME)]
write_json(WORK/'Workspace-Manifest.json',{'schema_version':1,'baseline_commit':BASE,'branch':'main','strategy':'COPY_ONLY_KEEP_ALL_ORIGINALS','created_at_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'entries':manifest,'unrelated_artifact_roots':unrelated,'other_locations':[{'path':'.nuget-packages','classification':'PREEXISTING_PACKAGE_CACHE_WITH_RESTORED_DEPENDENCIES; never copy binaries'},{'path':'src/**/bin and src/**/obj; tests/**/bin and tests/**/obj','classification':'PREEXISTING_BUILD_OUTPUTS_REBUILT_DURING_SETUP; never copy caches'},{'path':r'C:\Users\Mohamed\.codex\visualizations\2026\10\16\01a14315-aeb2-7852-a566-6d9ff3372eab','classification':'No conversation-created content observed in recovery inspection'}]})
print(json.dumps({'candidates':len(manifest),'exact_copies':sum(e['migration_status']=='EXACT_COPY_VERIFIED' for e in manifest),'sanitized_copies':sum(e['migration_status']=='SANITIZED_COPY_VERIFIED' for e in manifest),'excluded':sum(e['migration_status']=='EXCLUDED' for e in manifest),'conflicts':sum(e['migration_status']=='CONFLICT_NOT_OVERWRITTEN' for e in manifest),'unrelated_artifact_roots_preserved':len(unrelated)},indent=2))

"""Owner-authorized interim audit publication gate; never stages, commits, or pushes."""
import collections
import json
import re
import subprocess
from knowledge_store import *

def git(*args):
    return subprocess.check_output(['git',*args],cwd=ROOT)

assert git('branch','--show-current').decode().strip()=='main'
assert not git('diff','--cached','--name-only').strip(),'Existing staged changes require separate review'
assert not git('diff','--name-only','--','.',':!Codex Work').strip(),'Changes outside authorized workspace'

# Read actual baseline blobs in one batch instead of trusting a prior manifest.
tree=git('ls-tree','-r','-z',BASELINE).split(b'\0')
items=[]
for entry in filter(None,tree):
    meta,path=entry.split(b'\t',1);mode,kind,oid=meta.split()
    assert kind==b'blob'
    items.append((path.decode('utf8'),oid.decode()))
proc=subprocess.run(['git','cat-file','--batch'],input=''.join(oid+'\n' for _,oid in items).encode(),stdout=subprocess.PIPE,check=True,cwd=ROOT)
data=proc.stdout;offset=0;baseline_checks=[]
for path,oid in items:
    end=data.index(b'\n',offset);header=data[offset:end].split();size=int(header[2]);offset=end+1
    body=data[offset:offset+size];offset+=size+1
    actual=(ROOT/path).read_bytes()
    same=actual==body
    if not same:
        try:
            actual.decode('utf8');body.decode('utf8')
            same=actual.replace(b'\r\n',b'\n')==body.replace(b'\r\n',b'\n')
        except UnicodeError:
            pass
    if not same:raise ValueError('Baseline mismatch '+path)
    baseline_checks.append({'path':path,'baseline_blob_sha256':sha256(body),'worktree_sha256':sha256(actual),'comparison':'IDENTICAL' if actual==body else 'CRLF_LF_EQUIVALENT'})
assert len(items)==690

names=sorted(set(filter(None,(git('ls-files','-z','--','Codex Work')+git('ls-files','--others','--exclude-standard','-z','--','Codex Work')).decode('utf8').split('\0'))))
allowed={'.md','.json','.csv','.py','.cs','.csproj','.ps1','.txt'}
rules={
    'private_key':r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',
    'github_token':r'\b(?:gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{30,})\b',
    'jwt':r'\beyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\b',
    'quoted_secret':r'(?i)(?:password|pwd|api[_-]?key|access[_-]?token|bearer[_-]?token)\s*[:=]\s*["\x27][^"\x27\r\n]{3,}["\x27]',
    'connection_password':r'(?i)(?:password|pwd)\s*=\s*[^;"\x27\r\n\s]+',
    'pin_literal':r'(?i)\b[A-Za-z_$]*pin[A-Za-z_$]*\s*[:=,]\s*["\x27]\d{3,12}["\x27]'
}
compiled={label:re.compile(pattern) for label,pattern in rules.items()}
findings=[];eligible=[];excluded=[];json_count=0
for name in names:
    file=ROOT/name
    if not file.exists():raise ValueError('Unexpected missing tracked file '+name)
    if file.suffix.lower() not in allowed or any(part.lower() in ('bin','obj','__pycache__','testresults') for part in file.parts):
        excluded.append({'path':name,'reason':'Generated/binary/cache or unapproved extension'});continue
    raw=file.read_bytes();text=raw.decode('utf-8-sig')
    if file.suffix.lower()=='.json':json.loads(text);json_count+=1
    for label,regex in compiled.items():
        matches=list(regex.finditer(text))
        if matches:
            findings.append({'path':name,'rule':label,'lines':sorted({text.count('\n',0,m.start())+1 for m in matches}),'count':len(matches)})
    eligible.append({'path':name,'sha256':sha256(raw),'bytes':len(raw)})

# Findings are metadata only; do not print possible secrets.
result={'at_utc':now(),'kind':'INTERIM_ONLY_NOT_AUDIT_COMPLETION','baseline':BASELINE,
        'head_before':git('rev-parse','HEAD').decode().strip(),'baseline_file_checks':baseline_checks,
        'eligible_count':len(eligible),'eligible_files':eligible,'excluded':excluded,
        'valid_json_files':json_count,'potential_secret_locations':findings,
        'status':'BLOCKED_NEEDS_CONTEXT_REVIEW' if findings else 'CONTENT_SCAN_CLEAR_PENDING_STAGED_VERIFICATION',
        'privacy_limits':'Pattern checks supplement source/provenance review, not mathematical proof of no secret. Baseline test fixture default contact strings are source literals, not runtime/customer data; no operational logs/DB included.'}
atomic_json(WORK/'Conversation-Recovery/INTERIM_PUBLICATION_GATE.json',result)
atomic_text(WORK/'Conversation-Recovery/INTERIM_PUBLICATION_FILES.txt','\n'.join(r['path'] for r in eligible)+'\n')
print(json.dumps({'status':result['status'],'baseline_files_verified':len(items),'eligible_files':len(eligible),'valid_json_files':json_count,'potential_secret_locations':findings,'excluded_count':len(excluded)}))

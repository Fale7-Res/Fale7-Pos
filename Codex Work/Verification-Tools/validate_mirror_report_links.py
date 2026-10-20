"""Validate current canonical mirror links; historical snapshots are not current reports."""
from knowledge_store import *
import re, urllib.parse
reports=['Deep-Audit/CODE_DUPLICATION_AND_ARCHITECTURE.md','Deep-Audit/MIRRORED_CODE_AUDIT_STATUS.md']
checked=[];broken=[]
for rel in reports:
    path=WORK/rel
    for match in re.finditer(r'(?<!!)\[[^\]]+\]\(([^)]+)\)',path.read_text(encoding='utf8')):
        target=match.group(1).strip().strip('<>')
        if re.match(r'^[a-zA-Z][a-zA-Z0-9+.-]*://',target) or target.startswith('#'):continue
        target=urllib.parse.unquote(target.split('#')[0])
        resolved=(path.parent/target).resolve()
        record={'report':rel,'target':target,'exists':resolved.exists()}
        checked.append(record)
        if not record['exists']:broken.append(record)
payload={'at_utc':now(),'checked_links':len(checked),'broken_links':broken,
         'reports':[{'path':r,'sha256':sha256((WORK/r).read_bytes())} for r in reports],
         'checks':checked,'scope':'Current canonical documents, local link existence; not historical snapshot rebasing or semantic validation.'}
atomic_json(WORK/'Deep-Audit/Duplication-and-Architecture/final-report-links-validation.json',payload)
print(json.dumps({'links':len(checked),'broken':broken}))
assert not broken

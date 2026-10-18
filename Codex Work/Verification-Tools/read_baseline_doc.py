"""Display bounded baseline documentation with seed numeric credentials/phones masked."""
from pathlib import Path
import re,sys,hashlib
ROOT=Path(__file__).resolve().parents[2]
sys.stdout.reconfigure(encoding='utf8')
path=(ROOT/sys.argv[1]).resolve()
if not path.is_relative_to(ROOT/'docs'):raise SystemExit('Documentation scope only')
start=int(sys.argv[2]);end=int(sys.argv[3])
seed=(ROOT/'src/Falah.Infrastructure/Persistence/DemoDataSeeder.cs').read_text(encoding='utf-8-sig')
values=set(re.findall(r'"([0-9]{4,8})"',seed))
raw=path.read_bytes();lines=raw.decode('utf-8-sig').splitlines()
print(f'PATH {path.relative_to(ROOT).as_posix()} SHA256 {hashlib.sha256(raw).hexdigest()} LINES {start}-{min(end,len(lines))}/{len(lines)}')
for i in range(start-1,min(end,len(lines))):
    text=lines[i]
    for value in values:text=re.sub(r'(?<!\d)'+re.escape(value)+r'(?!\d)','[REDACTED-SEED-CREDENTIAL]',text)
    text=re.sub(r'(?<!\d)\d{10,12}(?!\d)','[REDACTED-PHONE]',text)
    print(f'{i+1}: {text}')

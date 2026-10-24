import subprocess
import hashlib
from collections import Counter
from pathlib import Path
import json

out_base = subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', 'c35816ecae49633674775dd32061c6ee92f94aaf'], encoding='utf-8')
paths = [l.strip() for l in out_base.splitlines() if l.strip()]

exts = Counter(Path(p).suffix.lower() if Path(p).suffix else "(no extension)" for p in paths)
print('File extension breakdown of the 690 baseline files:')
for ext, count in exts.most_common():
    print(f'  {ext}: {count}')

top_dirs = Counter(Path(p).parts[0] for p in paths)
print('\nTop-level breakdown:')
for d, count in top_dirs.most_common():
    print(f'  {d}: {count}')

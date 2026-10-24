import json
import re
from pathlib import Path

def test_engine():
    with open('Codex Work/Deep-Audit/Source-Inventory/Current/FUNCTION_INDEX.json', 'r', encoding='utf-8') as f:
        fdata = json.load(f)
    
    decls = fdata.get('declarations', [])
    callables = [d for d in decls if d.get('isCallable')]
    print(f"Loaded {len(callables)} callables.")

    # Inspect sample callable kinds
    samples = {}
    for c in callables:
        kind = c['kind']
        if kind not in samples:
            samples[kind] = c

    for kind, c in samples.items():
        print(f"\nKind: {kind}")
        print(f"  ID: {c['id']}")
        print(f"  Path: {c['path']}:{c['startLine']}-{c['endLine']}")
        print(f"  Name: {c['name']}")
        print(f"  Symbol: {c['qualifiedSymbol']}")

if __name__ == '__main__':
    test_engine()

import sys
import json

sys.stdout.reconfigure(encoding='utf-8')

with open('GitHub Copilot Work/02-Callable-Inventory-and-Reviews/CALLABLE_REVIEW_REGISTRY.json', 'r', encoding='utf-8') as f:
    reg = json.load(f)

targets = sys.argv[1:] if len(sys.argv) > 1 else [
    'src/Falah.Server/Program.cs',
    'src/Falah.Server/AuthenticationMiddleware.cs',
    'src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs'
]

for path in targets:
    matching = [r for r in reg['records'] if r['path'] == path]
    print(f"{path}: {len(matching)} callables")
    for m in matching:
        print(f"  Lines {m['lines']}: {m['kind']} | {m['symbol']} [{m['status']}]")

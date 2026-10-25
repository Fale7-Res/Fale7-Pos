import json

files = [
    ('OpenShiftHandler', 'GitHub Copilot Work/Imported-Codex-Work/Knowledge/Source-Symbols/src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs.json'),
    ('ShiftReconciliation', 'GitHub Copilot Work/Imported-Codex-Work/Knowledge/Source-Symbols/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs.json'),
    ('AddExpenseHandler', 'GitHub Copilot Work/Imported-Codex-Work/Knowledge/Source-Symbols/src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs.json'),
    ('ReceivePurchaseHandler', 'GitHub Copilot Work/Imported-Codex-Work/Knowledge/Source-Symbols/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs.json')
]

callable_kinds = {'ConstructorDeclaration', 'MethodDeclaration', 'SimpleLambdaExpression', 'ParenthesizedLambdaExpression', 'AnonymousMethodExpression'}

for name, fp in files:
    with open(fp, 'r', encoding='utf-8') as f:
        data = json.load(f)
    symbols = [s for s in data.get('symbols', []) if s.get('kind') in callable_kinds]
    print(f"=== {name}: {len(symbols)} callables ===")
    for i, s in enumerate(symbols, 1):
        print(f"{i}. [{s['id']}] {s['kind']} at line {s.get('line_span')}: {s.get('name')} {s.get('signature', '')}")
    print()

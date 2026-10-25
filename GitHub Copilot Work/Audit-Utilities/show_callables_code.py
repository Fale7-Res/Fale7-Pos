import json

files = [
    ('OpenShiftHandler', 'src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs', 'GitHub Copilot Work/Imported-Codex-Work/Knowledge/Source-Symbols/src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs.json'),
    ('ShiftReconciliation', 'src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs', 'GitHub Copilot Work/Imported-Codex-Work/Knowledge/Source-Symbols/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs.json'),
    ('AddExpenseHandler', 'src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs', 'GitHub Copilot Work/Imported-Codex-Work/Knowledge/Source-Symbols/src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs.json'),
    ('ReceivePurchaseHandler', 'src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs', 'GitHub Copilot Work/Imported-Codex-Work/Knowledge/Source-Symbols/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs.json')
]

callable_kinds = {'ConstructorDeclaration', 'MethodDeclaration', 'SimpleLambdaExpression', 'ParenthesizedLambdaExpression', 'AnonymousMethodExpression'}

for name, src_path, sym_path in files:
    with open(src_path, 'r', encoding='utf-8') as f:
        src_lines = f.readlines()
    with open(sym_path, 'r', encoding='utf-8') as f:
        sym_data = json.load(f)
    symbols = [s for s in sym_data.get('symbols', []) if s.get('kind') in callable_kinds]
    print(f"================ {name} ({len(symbols)} callables) ================")
    for i, s in enumerate(symbols, 1):
        # symbol id has file:line:col:Kind
        parts = s['id'].split(':')
        line_num = int(parts[1])
        col_num = int(parts[2])
        # print snippet around line_num
        line_text = src_lines[line_num - 1].strip() if line_num <= len(src_lines) else ""
        print(f"{i}. [{s['id']}] {s['kind']} at line {line_num}:{col_num}")
        print(f"   Signature: {s.get('signature', s.get('name', ''))}")
        print(f"   Source line {line_num}: {line_text}")
    print()

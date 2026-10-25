import json

for name, src_file, sym_file in [
    ('OpenShiftHandler', 'src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs', 'GitHub Copilot Work/Imported-Codex-Work/Knowledge/Source-Symbols/src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs.json'),
    ('ShiftReconciliation', 'src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs', 'GitHub Copilot Work/Imported-Codex-Work/Knowledge/Source-Symbols/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs.json')
]:
    with open(src_file, 'r', encoding='utf-8') as f:
        src_lines = f.readlines()
    with open(sym_file, 'r', encoding='utf-8') as f:
        sym_data = json.load(f)
    callable_kinds = {'ConstructorDeclaration', 'MethodDeclaration', 'SimpleLambdaExpression', 'ParenthesizedLambdaExpression', 'AnonymousMethodExpression'}
    symbols = [s for s in sym_data.get('symbols', []) if s.get('kind') in callable_kinds]
    print(f"=== {name} ===")
    for i, s in enumerate(symbols, 1):
        line_num = int(s['id'].split(':')[1])
        print(f"{i}. {s['id']} | {s['kind']} | line {line_num} | {src_lines[line_num - 1].strip()}")
    print()

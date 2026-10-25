# -*- coding: utf-8 -*-
import re

review_path = r"c:\Users\Mohamed\Desktop\Falah-Restaurant-CSharp\GitHub Copilot Work\02-Callable-Inventory-and-Reviews\File-Reviews\src\Falah.Infrastructure\Reports\DailyStatementQueryService.cs.md"
with open(review_path, 'r', encoding='utf-8') as f:
    text = f.read()

# Replace each of the 5 lambda entries
p1 = (
    r"## Canonical Callable ID: src/Falah\.Infrastructure/Reports/DailyStatementQueryService\.cs:165:51:SimpleLambdaExpression\n"
    r"- \*\*Symbol\*\*: lambda expression\n"
    r"- \*\*Kind\*\*: SimpleLambdaExpression\n"
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 165-165\.\n"
)
r1 = (
    "## Canonical Callable ID: src/Falah.Infrastructure/Reports/DailyStatementQueryService.cs:165:51:SimpleLambdaExpression\n"
    "- **Symbol**: lambda expression\n"
    "- **Kind**: SimpleLambdaExpression\n"
    "- **Behavior**: Inline grouping key selector `l => l.EmployeeId`. Groups ledger rows by employee identifier.\n"
)

p2 = (
    r"## Canonical Callable ID: src/Falah\.Infrastructure/Reports/DailyStatementQueryService\.cs:165:83:SimpleLambdaExpression\n"
    r"- \*\*Symbol\*\*: lambda expression\n"
    r"- \*\*Kind\*\*: SimpleLambdaExpression\n"
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 165-165\.\n"
)
r2 = (
    "## Canonical Callable ID: src/Falah.Infrastructure/Reports/DailyStatementQueryService.cs:165:83:SimpleLambdaExpression\n"
    "- **Symbol**: lambda expression\n"
    "- **Kind**: SimpleLambdaExpression\n"
    "- **Behavior**: Inline dictionary key selector `g => g.Key`. Binds employee ID as dictionary key.\n"
)

p3 = (
    r"## Canonical Callable ID: src/Falah\.Infrastructure/Reports/DailyStatementQueryService\.cs:165:95:SimpleLambdaExpression\n"
    r"- \*\*Symbol\*\*: lambda expression\n"
    r"- \*\*Kind\*\*: SimpleLambdaExpression\n"
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 165-165\.\n"
)
r3 = (
    "## Canonical Callable ID: src/Falah.Infrastructure/Reports/DailyStatementQueryService.cs:165:95:SimpleLambdaExpression\n"
    "- **Symbol**: lambda expression\n"
    "- **Kind**: SimpleLambdaExpression\n"
    "- **Behavior**: Inline dictionary value selector `g => g.ToList()`. Groups ledger entries list per employee.\n"
)

p4 = (
    r"## Canonical Callable ID: src/Falah\.Infrastructure/Reports/DailyStatementQueryService\.cs:197:75:SimpleLambdaExpression\n"
    r"- \*\*Symbol\*\*: lambda expression\n"
    r"- \*\*Kind\*\*: SimpleLambdaExpression\n"
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 197-197\.\n"
)
r4 = (
    "## Canonical Callable ID: src/Falah.Infrastructure/Reports/DailyStatementQueryService.cs:197:75:SimpleLambdaExpression\n"
    "- **Symbol**: lambda expression\n"
    "- **Kind**: SimpleLambdaExpression\n"
    "- **Behavior**: Inline predicate lambda `i => i.IsPackaging`. Filters inventory items designated as packaging materials.\n"
)

p5 = (
    r"## Canonical Callable ID: src/Falah\.Infrastructure/Reports/DailyStatementQueryService\.cs:197:102:SimpleLambdaExpression\n"
    r"- \*\*Symbol\*\*: lambda expression\n"
    r"- \*\*Kind\*\*: SimpleLambdaExpression\n"
    r"- \*\*Behavior\*\*: Executes logic for lambda expression across lines 197-197\.\n"
)
r5 = (
    "## Canonical Callable ID: src/Falah.Infrastructure/Reports/DailyStatementQueryService.cs:197:102:SimpleLambdaExpression\n"
    "- **Symbol**: lambda expression\n"
    "- **Kind**: SimpleLambdaExpression\n"
    "- **Behavior**: Inline selector lambda `i => i.Id`. Extracts packaging inventory item ID for hash set membership testing.\n"
)

for p, r in [(p1, r1), (p2, r2), (p3, r3), (p4, r4), (p5, r5)]:
    text = re.sub(p, r, text)

with open(review_path, 'w', encoding='utf-8') as f:
    f.write(text)

remaining = len(re.findall(r"Executes logic for.*", text))
print(f"Remaining generic entries in DailyStatementQueryService.cs.md: {remaining}")

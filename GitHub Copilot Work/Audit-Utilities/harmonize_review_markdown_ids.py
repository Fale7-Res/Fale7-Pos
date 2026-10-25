"""
Harmonizes review Markdown files by embedding exact canonical callable IDs
from Imported-Codex-Work/Knowledge/Source-Symbols into the Markdown review artifacts.
Fixes finding ID collisions (SEC-004 vs SEC-006/SEC-007, SEM-MIRROR-065 on checkout, etc.).
"""

import json
import os
import re

symbols_dir = 'GitHub Copilot Work/Imported-Codex-Work/Knowledge/Source-Symbols'
reviews_dir = 'GitHub Copilot Work/02-Callable-Inventory-and-Reviews/File-Reviews'

def load_callables(rel_path):
    p = os.path.join(symbols_dir, rel_path + '.json')
    if not os.path.exists(p):
        return []
    with open(p, 'r', encoding='utf-8') as f:
        d = json.load(f)
    return [s for s in d.get('symbols', []) if s.get('isCallable')]

def harmonize_approval_registry():
    path = "src/Falah.Domain/Authorization/ApprovalRegistry.cs"
    md_file = os.path.join(reviews_dir, path + ".md")
    if not os.path.exists(md_file):
        return
    with open(md_file, "r", encoding="utf-8") as f:
        content = f.read()

    # Map CAL-AR tags to exact canonical IDs
    mapping = {
        "CAL-AR-001": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:13:5:MethodDeclaration", 13, 13, "IApprovalRegistry.Register(ManagerApproval)"),
        "CAL-AR-002": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:14:5:MethodDeclaration", 14, 14, "IApprovalRegistry.Find(string)"),
        "CAL-AR-003": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:15:5:MethodDeclaration", 15, 15, "IApprovalRegistry.IsConsumed(string)"),
        "CAL-AR-004": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:16:5:MethodDeclaration", 16, 16, "IApprovalRegistry.MarkConsumed(string)"),
        "CAL-AR-005": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:24:5:MethodDeclaration", 24, 24, "InMemoryApprovalRegistry.Register(ManagerApproval)"),
        "CAL-AR-006": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:25:5:MethodDeclaration", 25, 25, "InMemoryApprovalRegistry.Find(string)"),
        "CAL-AR-007": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:26:5:MethodDeclaration", 26, 26, "InMemoryApprovalRegistry.IsConsumed(string)"),
        "CAL-AR-008": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:27:5:MethodDeclaration", 27, 27, "InMemoryApprovalRegistry.MarkConsumed(string)"),
        "CAL-AR-009": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:37:5:MethodDeclaration", 37, 38, "AuthorizationPolicy.HasPermission(Actor?, string)"),
        "CAL-AR-010": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:40:5:MethodDeclaration", 40, 65, "AuthorizationPolicy.IssueManagerApproval(...)"),
        "CAL-AR-012": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:69:5:MethodDeclaration", 69, 78, "AuthorizationPolicy.AuthorizeCommand(...)"),
        "CAL-AR-013": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:89:5:MethodDeclaration", 89, 97, "AuthorizationPolicy.ValidateApproval(...)"),
        "CAL-AR-014": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:105:5:MethodDeclaration", 105, 111, "AuthorizationPolicy.AuthorizeShiftClose(...)"),
        "CAL-AR-015": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:122:5:MethodDeclaration", 122, 129, "AuthorizationPolicy.AuthorizeExpense(...)"),
        "CAL-AR-016": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:136:5:MethodDeclaration", 136, 141, "AuthorizationPolicy.AuthorizeShiftOpen(...)"),
        "CAL-AR-017": ("src/Falah.Domain/Authorization/ApprovalRegistry.cs:149:5:MethodDeclaration", 149, 154, "AuthorizationPolicy.AuthorizeAddSupplier(...)")
    }

    for cal, (cid, sl, el, sig) in mapping.items():
        # Inject canonical ID if not present
        if cid not in content:
            target = f"### {cal}"
            if target in content:
                replacement = f"### {cal}\n- **Callable ID:** `{cid}`\n- **Exact Line Range:** Lines {sl}–{el}\n- **Signature:** `{sig}`\n- **Linked Findings:** `SEC-006`, `SEC-007`"
                content = content.replace(target, replacement, 1)

    # Remove any stray SEC-004 links in ApprovalRegistry
    content = re.sub(r'\bSEC-004\b', 'SEC-006, SEC-007', content)

    with open(md_file, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"Harmonized {md_file}")

def harmonize_pin_hasher():
    path = "src/Falah.Domain/Authorization/PinHasher.cs"
    md_file = os.path.join(reviews_dir, path + ".md")
    if not os.path.exists(md_file):
        return
    with open(md_file, "r", encoding="utf-8") as f:
        content = f.read()

    mapping = {
        "CAL-PH-002": ("src/Falah.Domain/Authorization/PinHasher.cs:25:5:MethodDeclaration", 25, 29, "PinHasher.Hash(string, int)"),
        "CAL-PH-003": ("src/Falah.Domain/Authorization/PinHasher.cs:37:5:MethodDeclaration", 37, 42, "PinHasher.HashWithoutValidation(string, int)"),
        "CAL-PH-004": ("src/Falah.Domain/Authorization/PinHasher.cs:47:5:MethodDeclaration", 47, 53, "PinHasher.Verify(string, string, string, int)"),
        "CAL-PH-005": ("src/Falah.Domain/Authorization/PinHasher.cs:59:5:MethodDeclaration", 59, 65, "PinHasher.AssertValidPin(string)")
    }

    for cal, (cid, sl, el, sig) in mapping.items():
        if cid not in content:
            target = f"### {cal}:"
            if target in content:
                replacement = f"### {cal}:\n- **Callable ID:** `{cid}`\n- **Exact Line Range:** Lines {sl}–{el}\n- **Signature:** `{sig}`"
                content = content.replace(target, replacement, 1)

    # PinHasher should NOT be linked to SEC-002 (SEC-002 is RetainedRequestStore plaintext storage)
    content = content.replace("`SEC-002`", "`SEC-CRYPTO-STRETCH-001`")

    with open(md_file, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"Harmonized {md_file}")

def harmonize_user_management_rules():
    path = "src/Falah.Domain/Authorization/UserManagementRules.cs"
    md_file = os.path.join(reviews_dir, path + ".md")
    if not os.path.exists(md_file):
        return
    with open(md_file, "r", encoding="utf-8") as f:
        content = f.read()

    mapping = {
        "CAL-UMR-001": ("src/Falah.Domain/Authorization/UserManagementRules.cs:20:5:MethodDeclaration", 20, 24, "AssertTargetIsNotOwner(string, string)"),
        "CAL-UMR-002": ("src/Falah.Domain/Authorization/UserManagementRules.cs:29:5:MethodDeclaration", 29, 35, "AssertNoPrivilegeEscalation(IReadOnlySet<string>, IEnumerable<string>)"),
        "CAL-UMR-003": ("src/Falah.Domain/Authorization/UserManagementRules.cs:32:48:SimpleLambdaExpression", 32, 32, "lambda in AssertNoPrivilegeEscalation"),
        "CAL-UMR-004": ("src/Falah.Domain/Authorization/UserManagementRules.cs:44:5:MethodDeclaration", 44, 48, "CanManageUsers(IEnumerable<string>)"),
        "CAL-UMR-005": ("src/Falah.Domain/Authorization/UserManagementRules.cs:57:5:MethodDeclaration", 57, 63, "AssertUserAdministrationSurvives(int)"),
        "CAL-UMR-006": ("src/Falah.Domain/Authorization/UserManagementRules.cs:65:5:MethodDeclaration", 65, 70, "AssertKnownPermissions(IEnumerable<string>)"),
        "CAL-UMR-007": ("src/Falah.Domain/Authorization/UserManagementRules.cs:67:41:SimpleLambdaExpression", 67, 67, "lambda in AssertKnownPermissions")
    }

    for cal, (cid, sl, el, sig) in mapping.items():
        if cid not in content:
            target = f"### {cal}"
            if target in content:
                replacement = f"### {cal}\n- **Callable ID:** `{cid}`\n- **Exact Line Range:** Lines {sl}–{el}\n- **Signature:** `{sig}`"
                content = content.replace(target, replacement, 1)

    with open(md_file, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"Harmonized {md_file}")

def harmonize_update_user_permissions():
    path = "src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs"
    md_file = os.path.join(reviews_dir, path + ".md")
    if not os.path.exists(md_file):
        return
    with open(md_file, "r", encoding="utf-8") as f:
        content = f.read()

    mapping = {
        "CAL-UUP-001": ("src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:22:5:ConstructorDeclaration", 22, 26, "UpdateUserPermissionsHandler(FalahDbContext, IApprovalRegistry)"),
        "CAL-UUP-002": ("src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:28:5:MethodDeclaration", 28, 75, "HandleAsync(UpdateUserPermissionsRequest, CancellationToken)"),
        "CAL-UUP-003": ("src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:32:76:SimpleLambdaExpression", 32, 32, "u => u.Id == request.ActorUserId"),
        "CAL-UUP-004": ("src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:37:58:SimpleLambdaExpression", 37, 37, "u => u.Id == request.UserId"),
        "CAL-UUP-005": ("src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:57:24:SimpleLambdaExpression", 57, 57, "u => u.Active && u.Id != target.Id"),
        "CAL-UUP-006": ("src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:58:25:SimpleLambdaExpression", 58, 58, "u => u.Permissions"),
        "CAL-UUP-007": ("src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:62:46:SimpleLambdaExpression", 62, 63, "permissions => UserManagementRules.CanManageUsers(...)")
    }

    for cal, (cid, sl, el, sig) in mapping.items():
        if cid not in content:
            target = f"### {cal}"
            if target in content:
                replacement = f"### {cal}\n- **Callable ID:** `{cid}`\n- **Exact Line Range:** Lines {sl}–{el}\n- **Signature:** `{sig}`\n- **Linked Findings:** `SEC-001`"
                content = content.replace(target, replacement, 1)

    with open(md_file, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"Harmonized {md_file}")

def harmonize_change_pin():
    path = "src/Falah.Infrastructure/Users/ChangePinHandler.cs"
    md_file = os.path.join(reviews_dir, path + ".md")
    if not os.path.exists(md_file):
        return
    with open(md_file, "r", encoding="utf-8") as f:
        content = f.read()

    mapping = {
        "Constructor": ("src/Falah.Infrastructure/Users/ChangePinHandler.cs:24:5:ConstructorDeclaration", 24, 28, "ChangePinHandler(FalahDbContext, IApprovalRegistry)"),
        "HandleAsync": ("src/Falah.Infrastructure/Users/ChangePinHandler.cs:30:5:MethodDeclaration", 30, 65, "HandleAsync(ChangePinRequest, CancellationToken)")
    }

    for name, (cid, sl, el, sig) in mapping.items():
        if cid not in content:
            target = f"### {name}"
            if target in content:
                replacement = f"### {name}\n- **Callable ID:** `{cid}`\n- **Exact Line Range:** Lines {sl}–{el}\n- **Signature:** `{sig}`\n- **Linked Findings:** `SEC-001`"
                content = content.replace(target, replacement, 1)

    with open(md_file, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"Harmonized {md_file}")

def harmonize_pin_uniqueness():
    path = "src/Falah.Infrastructure/Users/PinUniquenessChecker.cs"
    md_file = os.path.join(reviews_dir, path + ".md")
    if not os.path.exists(md_file):
        return
    with open(md_file, "r", encoding="utf-8") as f:
        content = f.read()

    cid = "src/Falah.Infrastructure/Users/PinUniquenessChecker.cs:16:5:MethodDeclaration"
    if cid not in content:
        content = content.replace("### AssertNotInUseAsync", f"### AssertNotInUseAsync\n- **Callable ID:** `{cid}`\n- **Exact Line Range:** Lines 16–23\n- **Signature:** `AssertNotInUseAsync(FalahDbContext, string, string?, CancellationToken)`\n- **Linked Findings:** `SEC-001`")

    with open(md_file, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"Harmonized {md_file}")

def harmonize_cashier_checkout_wpf():
    path = "src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs"
    md_file = os.path.join(reviews_dir, path + ".md")
    if not os.path.exists(md_file):
        return
    with open(md_file, "r", encoding="utf-8") as f:
        content = f.read()

    # Remove any reference to SEM-MIRROR-065
    content = content.replace("SEM-MIRROR-065", "UI-013")

    mapping = {
        "Callable 1: `OnLastCheckoutResultChanged`": ("src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:23:5:MethodDeclaration", 23, 27),
        "Callable 3: `get_ChangeDuePiastres`": ("src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:31:9:GetAccessorDeclaration", 31, 36),
        "Callable 4: `OpenCheckout`": ("src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:39:5:MethodDeclaration", 39, 48),
        "Callable 5: `CloseCheckout`": ("src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:50:5:MethodDeclaration", 50, 51),
        "Callable 6: `SetPaymentMethod`": ("src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:53:5:MethodDeclaration", 53, 59),
        "Callable 7: `PressAmountKey`": ("src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:61:5:MethodDeclaration", 61, 71),
        "Callable 8: `QuickCash`": ("src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:73:5:MethodDeclaration", 73, 78),
        "Callable 9: `CanSubmitPayment`": ("src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:80:5:MethodDeclaration", 80, 80),
        "Callable 10: `SubmitPaymentAsync`": ("src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:82:5:MethodDeclaration", 82, 140),
        "Callable 11: `StartNewOrder`": ("src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs:142:5:MethodDeclaration", 142, 147)
    }

    for cal, (cid, sl, el) in mapping.items():
        if cid not in content:
            content = content.replace(cal, f"{cal}\n- **Callable ID:** `{cid}`\n- **Exact Line Range:** Lines {sl}–{el}")

    with open(md_file, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"Harmonized {md_file}")

def harmonize_cashier_checkout_winui():
    path = "src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs"
    md_file = os.path.join(reviews_dir, path + ".md")
    if not os.path.exists(md_file):
        return
    with open(md_file, "r", encoding="utf-8") as f:
        content = f.read()

    # Remove any reference to SEM-MIRROR-065
    content = content.replace("SEM-MIRROR-065", "UI-001, UI-005, UI-006")

    callables = load_callables(path)
    for c in callables:
        cid = c['id']
        sl = c['startLine']
        el = c['endLine']
        name = c['name']
        if cid not in content:
            # Look for line range or name
            pat = rf'(### Callable \d+: `[^`]*{re.escape(name.split("(")[0])}[^`]*`)'
            m = re.search(pat, content)
            if m:
                target = m.group(1)
                replacement = f"{target}\n- **Callable ID:** `{cid}`\n- **Exact Line Range:** Lines {sl}–{el}"
                content = content.replace(target, replacement, 1)

    with open(md_file, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"Harmonized {md_file}")

def harmonize_generic_files():
    files_to_check = [
        "src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs",
        "src/Falah.Infrastructure/Printing/ReprintDocumentHandler.cs",
        "src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs",
        "src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs",
        "src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs",
        "src/Falah.Client.Cashier/ViewModels/LoginViewModel.cs",
        "src/Falah.Client.CashierWinUI/ViewModels/LoginViewModel.cs",
        "src/Falah.Client.CallCenterWinUI/ViewModels/LoginViewModel.cs"
    ]
    for path in files_to_check:
        md_file = os.path.join(reviews_dir, path + ".md")
        if not os.path.exists(md_file):
            continue
        with open(md_file, "r", encoding="utf-8") as f:
            content = f.read()

        # Remove wrong finding links
        content = content.replace("`PRT-003`", "`PRT-REPRINT-VOUCHER-001`") if "ReprintDocumentHandler" in path else content
        content = content.replace("`UI-002`", "`UI-CONTRAST-KEYPAD-001`") if "LoginViewModel" in path else content
        content = content.replace("`UI-001`", "`PERF-KEYSTROKE-STORM-001`") if "CashierViewModel" in path else content
        content = content.replace("`UI-002`", "`UI-THEME-STATE-001`") if "CashierViewModel.State" in path else content

        callables = load_callables(path)
        for c in callables:
            cid = c['id']
            sl = c['startLine']
            el = c['endLine']
            name = c['name'].split('(')[0].strip()
            if cid not in content and len(name) > 2:
                # Look for section header matching this name
                pat = rf'(### [^\n]*\b{re.escape(name)}\b[^\n]*)'
                m = re.search(pat, content)
                if m:
                    target = m.group(1)
                    if f"`{cid}`" not in target:
                        replacement = f"{target}\n- **Callable ID:** `{cid}`\n- **Exact Line Range:** Lines {sl}–{el}"
                        content = content.replace(target, replacement, 1)

        with open(md_file, "w", encoding="utf-8") as f:
            f.write(content)
        print(f"Harmonized {md_file}")

if __name__ == "__main__":
    harmonize_approval_registry()
    harmonize_pin_hasher()
    harmonize_user_management_rules()
    harmonize_update_user_permissions()
    harmonize_change_pin()
    harmonize_pin_uniqueness()
    harmonize_cashier_checkout_wpf()
    harmonize_cashier_checkout_winui()
    harmonize_generic_files()
    print("All review markdown files successfully harmonized.")

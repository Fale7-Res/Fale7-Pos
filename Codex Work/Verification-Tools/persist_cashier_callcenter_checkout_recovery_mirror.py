"""Persist a bounded source comparison of cashier and call-center checkout recovery."""
from __future__ import annotations

import hashlib
import json
import subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
AUDIT = ROOT / "Codex Work" / "Deep-Audit"
MIRROR = AUDIT / "Duplication-and-Architecture"
BASELINE = "c35816ecae49633674775dd32061c6ee92f94aaf"
ARTIFACT_REL = "Deep-Audit/Duplication-and-Architecture/cashier-callcenter-checkout-recovery-semantic-comparison.json"
ARTIFACT = ROOT / "Codex Work" / ARTIFACT_REL

def sha(path: str) -> str:
    return hashlib.sha256((ROOT / path).read_bytes()).hexdigest()

def blob(path: str) -> str:
    symbol_path = ROOT / "Codex Work" / "Knowledge" / "Source-Symbols" / (path + ".json")
    return read_json(symbol_path)["git_blob_sha256"]

def read_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))

if subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip() != "9beff52a6c9c6c4f7244cbd46dc449da91745582":
    raise SystemExit("Unexpected source/documentation HEAD; reconcile before persisting")
if subprocess.run(["git", "diff", "--quiet", BASELINE, "--", "src", "tests"], cwd=ROOT).returncode != 0:
    raise SystemExit("Application or test source differs from baseline")

now = datetime.now(timezone.utc).isoformat(timespec="seconds").replace("+00:00", "Z")
index = read_json(ROOT / "Codex Work" / "Deep-Audit" / "Source-Inventory" / "Current" / "FUNCTION_INDEX.json")
decls = index["declarations"]
refs = [
    ("src/Falah.Client.CallCenterWinUI/App.xaml.cs", [[19, 27]], "The application initializes a station-scoped file retained-request store before view-model construction.", ["Falah.Client.CallCenterWinUI.App.App()"]),
    ("src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs", [[25, 34]], "The view-model constructor calls RecoverPendingOperations during desk construction.", ["Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.CallCenterViewModel(Falah.Client.CallCenterWinUI.Services.AppSession, Falah.Client.CallCenterWinUI.ViewModels.HeaderViewModel)"]),
    ("src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs", [[68, 92], [98, 110], [178, 210]], "Call Center stores the immutable request before dispatch, restores it under the owner key at startup, and retries that same DTO; a definite first rejection clears it, while unknown results preserve it.", ["Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.PersistPending(string, object)", "Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RecoverPendingOperations()", "Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SubmitOrderAsync()", "Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SendCheckoutAsync(Falah.Application.Orders.CheckoutOrderRequest)"]),
    ("src/Falah.Client.CashierWinUI/App.xaml.cs", [[19, 27]], "Cashier also configures the file store, proving storage is available to the app; this does not by itself persist checkout DTOs.", ["Falah.Client.CashierWinUI.App.App()"]),
    ("src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs", [[176, 180], [289, 300], [401, 427]], "Cashier checkout keeps its request ID and unresolved DTO only in view-model fields. CompleteCheckoutAsync sets _pendingCheckout before dispatch and reuses it in the same process, but no RetainedRequestStore Save/Load is used by this checkout path.", ["Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SubmitPaymentAsync()", "Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CompleteCheckoutAsync(Falah.Application.Orders.CheckoutOrderRequest)"]),
    ("src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs", [[40, 45]], "The server returns the prior order for the same CheckoutRequestId; losing the client copy after commit can prevent that same-key replay path from being used by a restarted client.", ["Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, System.Threading.CancellationToken)"]),
]
symbol_map = {}
for d in decls:
    q = d.get("qualifiedSymbol")
    if q:
        symbol_map.setdefault(q, []).append(d["id"])
source_refs = []
for path, ranges, evidence, symbols in refs:
    exact_symbols = []
    by_qualified = {}
    for q in symbols:
        matches = symbol_map.get(q, [])
        if matches:
            by_qualified[q] = matches
    symbol_source = read_json(ROOT / "Codex Work" / "Knowledge" / "Source-Symbols" / (path + ".json"))
    stable_by_qualified = {s.get("qualifiedSymbol"): s for s in symbol_source["symbols"] if s.get("qualifiedSymbol")}
    for q in symbols:
        if q in by_qualified and q in stable_by_qualified:
            s = stable_by_qualified[q]
            exact_symbols.append({"stable_id": s["stable_id"], "span": [s["startLine"], s["startColumn"], s["endLine"], s["endColumn"]]})
    source_refs.append({"path": path, "worktree_sha256": sha(path), "source_git_blob_sha256": blob(path), "line_ranges": ranges, "relevant_symbols": exact_symbols, "evidence": evidence})

artifact = {
    "artifact_kind": "manual_semantic_comparison",
    "id": "SEM-MIRROR-047",
    "baseline": BASELINE,
    "created_at": now,
    "reviewer": "/root coordinator",
    "title": "Call Center checkout durably recovers an ambiguous request while cashier checkout retains it only in memory",
    "scope": "Targeted comparison of restart recovery for checkout idempotency requests across the two active WinUI stations; follows the request through client store, VM dispatch, and server replay lookup. It does not certify all station lifecycle or deployed usage.",
    "decision": "SOURCE_CONFIRMED_RECOVERY_DIFFERENCE; RELATED_TO_UI-001_NO_DUPLICATE_FINDING_CREATED",
    "behavior": {
        "call_center": "App configures FileRetainedRequestStore before view models. CallCenterViewModel constructor restores owner-scoped CheckoutOrderRequest, and SendCheckoutAsync saves before network dispatch; unknown outcomes preserve it for same-request retry, successful or definite rejected outcomes clear it.",
        "cashier": "App also configures FileRetainedRequestStore and uses RetainedRequest<T> for selected other operations, but checkout uses private CurrentCheckoutRequestId and _pendingCheckout fields. Same-process unknown outcomes reuse the request. This checkout code does not persist or restore the pending CheckoutOrderRequest across process reconstruction.",
        "server": "CheckoutOrderHandler.HandleAsync looks up CheckoutRequestId before doing work and returns the existing order on replay. The key helps only if the client retains the original request/key.",
        "consequence": "After a cashier process/power loss during an unknown checkout result, the client-side exact request is not recovered through the inspected checkout path. A later reconstructed order can carry a fresh request id and represent a second order. This is a conditional duplicate-intent/recovery risk, not proof of automatic retry, duplicate database write, or real-world occurrence. UI-001 already covers cashier checkout and related money operations; this comparison adds the cross-station contrast and is not a new finding."
    },
    "counterevidence_and_limits": [
        "Cashier's same-process retry retains the immutable DTO and server idempotency key.",
        "Call Center's startup restore can expose the same original checkout DTO after process loss.",
        "The source does not establish that an operator will recreate the same order, nor that any duplicate order occurred.",
        "No runtime/power-loss/network-fault test was performed; actual deployment and operator workflow are unverified.",
        "This comparison does not independently re-review all UI-001 surfaces; it references that existing finding rather than duplicating it."
    ],
    "related_findings": ["UI-001"],
    "recommendation": "For future remediation, align cashier checkout with a durable owner-scoped retained request already used by the Call Center and other operations. Save the immutable request before dispatch, restore and reconcile it before allowing a new order, and clear only after a definite rejection or confirmed success. Avoid persisting credentials or unrelated cart state. Add fresh-view-model recovery tests for committed-but-lost response and definite rejection; verify same request id results in one order and one set of side effects in an isolated integration database.",
    "tests": {"executed": "NONE; source inspection only", "needed": ["Construct a fresh CashierViewModel with an unresolved retained checkout and assert the original DTO/key is available before new checkout.", "Simulate server commit followed by lost response; restart client; retry exact DTO and assert one order, one inventory deduction, one financial journal entry and one intended print job.", "Simulate definite server rejection; verify correction path does not preserve an invalid request indefinitely.", "Compare cashier and Call Center process-loss recovery behavior using the same fake retained store and API outcomes."]},
    "source_references": source_refs,
    "quality": {"source_hashes_recomputed": True, "baseline_identity_checked": True, "source_and_test_diff_from_baseline": "CLEAN", "runtime_tests": "NOT_EXECUTED", "new_financial_callable_review_credit": 0}
}
ARTIFACT.write_text(json.dumps(artifact, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

registry_path = MIRROR / "semantic-comparison-registry.json"
registry = read_json(registry_path)
group = next((x for x in registry["semantic_groups"] if x.get("id") == artifact["id"]), None)
if group is None:
    registry["semantic_groups"].append({"id": artifact["id"], "decision": artifact["decision"], "artifact": ARTIFACT_REL})
else:
    group.update({"decision": artifact["decision"], "artifact": ARTIFACT_REL})
input_item = next((x for x in registry["inputs"] if x.get("path") == ARTIFACT_REL), None)
if input_item is None:
    registry["inputs"].append({"path": ARTIFACT_REL, "sha256": sha(str(ARTIFACT.relative_to(ROOT))), "source_references": len(source_refs)})
else:
    input_item.update({"sha256": sha(str(ARTIFACT.relative_to(ROOT))), "source_references": len(source_refs)})
paths = {r["path"] for item in registry["inputs"] for r in read_json(ROOT / "Codex Work" / item["path"]).get("source_references", read_json(ROOT / "Codex Work" / item["path"]).get("sources", []))}
symbols = {s["stable_id"] for item in registry["inputs"] for r in read_json(ROOT / "Codex Work" / item["path"]).get("source_references", read_json(ROOT / "Codex Work" / item["path"]).get("sources", [])) for s in r.get("relevant_symbols", [])}
registry.update({"at_utc": now, "referenced_source_paths": len(paths), "referenced_callable_context_ids": len(symbols), "new_analyzed_financial_functions": 0})
registry["source_reference_records"] = sum(len(read_json(ROOT / "Codex Work" / i["path"]).get("source_references", read_json(ROOT / "Codex Work" / i["path"]).get("sources", []))) for i in registry["inputs"])
registry["explicit_ranges"] = sum(len(r.get("line_ranges", r.get("ranges", []))) for i in registry["inputs"] for r in read_json(ROOT / "Codex Work" / i["path"]).get("source_references", read_json(ROOT / "Codex Work" / i["path"]).get("sources", [])))
registry_path.write_text(json.dumps(registry, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

report = AUDIT / "CODE_DUPLICATION_AND_ARCHITECTURE.md"
with report.open("a", encoding="utf-8") as f:
    f.write("\n\n## SEM-MIRROR-047 — Checkout recovery across Call Center and Cashier\n\n")
    f.write("The current Call Center persists its immutable checkout DTO before network dispatch under an operator-scoped key, restores it when constructing the view model, and offers the same request after an unknown outcome. The Cashier WinUI app configures the same retained-request store for other operations, but its checkout path holds `CurrentCheckoutRequestId` and `_pendingCheckout` only in the view model. Its same-process retry is sound; the inspected checkout path does not restore the in-flight request after process reconstruction. The server's replay lookup is keyed by `CheckoutRequestId`, so losing that client key can prevent a restarted cashier from reconciling the original order.\n\nThis is a conditional recovery/duplicate-intent risk, not evidence that two orders have been created. The original finding UI-001 already covers the cashier restart-recovery gap, so this comparison creates no duplicate canonical finding. Proposed future correction: persist the typed immutable request before dispatch and gate new checkout until the original outcome is reconciled. Validate with a fresh-view-model test for a committed-but-lost response and assert one order and one set of side effects. Source evidence: [comparison artifact](Duplication-and-Architecture/cashier-callcenter-checkout-recovery-semantic-comparison.json). Runtime verification was not performed.\n")

print(json.dumps({"id": artifact["id"], "source_references": len(source_refs), "group_count": len(registry["semantic_groups"]), "input_count": len(registry["inputs"]), "artifact": ARTIFACT_REL, "created_utc": now}, ensure_ascii=False))

"""Save a source-backed comparison of the two station-scoped retained request stores."""
from __future__ import annotations
import hashlib, json, subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]; WORK=ROOT/"Codex Work"; MIRROR=WORK/"Deep-Audit"/"Duplication-and-Architecture"
BASE="c35816ecae49633674775dd32061c6ee92f94aaf"; REL="Deep-Audit/Duplication-and-Architecture/callcenter-cashier-retained-store-semantic-comparison.json"
def read(p): return json.loads((WORK/p).read_text(encoding="utf-8"))
def write(p,d):
 q=WORK/p; t=q.with_suffix(q.suffix+".tmp"); t.write_text(json.dumps(d,ensure_ascii=False,indent=2)+"\n",encoding="utf-8"); t.replace(q)
def sha(p): return hashlib.sha256((ROOT/p).read_bytes()).hexdigest()
def norm(p,old,new): return (ROOT/p).read_text(encoding="utf-8-sig").replace(old,new)
assert subprocess.check_output(["git","rev-parse","HEAD"],cwd=ROOT,text=True).strip()=="9beff52a6c9c6c4f7244cbd46dc449da91745582"
assert subprocess.run(["git","diff","--quiet",BASE,"--","src","tests"],cwd=ROOT).returncode==0
call="src/Falah.Client.CallCenterWinUI/Services/RetainedRequestStore.cs"; cash="src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs"
assert norm(call,"namespace Falah.Client.CallCenterWinUI.Services;","namespace Falah.Client.CashierWinUI.Services;")==norm(cash,"namespace Falah.Client.CallCenterWinUI.Services;","namespace Falah.Client.CashierWinUI.Services;")
index=read("Deep-Audit/Source-Inventory/Current/FUNCTION_INDEX.json")["declarations"]
source_refs=[]
for p,old,new,range_ in [(call,"namespace Falah.Client.CallCenterWinUI.Services;","namespace Falah.Client.CashierWinUI.Services;",[[1,146]]),(cash,"namespace Falah.Client.CashierWinUI.Services;","namespace Falah.Client.CallCenterWinUI.Services;",[[1,146]])]:
 symbols=read("Knowledge/Source-Symbols/"+p+".json")
 selected=[s for s in symbols["symbols"] if s.get("qualifiedSymbol") and any(x in s["qualifiedSymbol"] for x in ["FileRetainedRequestStore.","IRetainedRequestStore.","RetainedRequestRecord.","RetainedRequestStorage."])]
 source_refs.append({"path":p,"worktree_sha256":sha(p),"source_git_blob_sha256":symbols["git_blob_sha256"],"line_ranges":range_,"relevant_symbols":[{"stable_id":s["stable_id"],"span":[s["startLine"],s["startColumn"],s["endLine"],s["endColumn"]]} for s in selected],"evidence":"Whole-file comparison; after substituting only the file-scoped namespace, normalized source text is byte-identical. DirectoryFor still takes station as an argument, and the two App constructors pass distinct station names. Save, clear, load and list behavior is therefore equivalent while stored records remain station-separated."})
now=datetime.now(timezone.utc).isoformat(timespec="seconds").replace("+00:00","Z")
artifact={"artifact_kind":"manual_semantic_comparison","id":"SEM-MIRROR-048","baseline":BASE,"created_at":now,"reviewer":"/root coordinator","title":"Station-scoped retained request stores are identical apart from namespace","scope":"Whole-file normalized comparison of the Call Center and Cashier WinUI retained-request storage implementations and their distinct App station arguments; no runtime file access or recovery test.","decision":"INTENTIONAL_PARALLEL_IMPLEMENTATIONS_IDENTICAL_BEHAVIOR; NO_DIVERGENCE_FOUND","comparison":{"normalization":"Replace only `namespace Falah.Client.CallCenterWinUI.Services;` with `namespace Falah.Client.CashierWinUI.Services;` in the Call Center file; normalized text equals the Cashier file exactly.","covered_behaviors":["owner-key filename sanitization","JSON envelope serialization","temporary-write then overwrite move","error callback behavior","clear semantics","type-name recovery and deserialization","list metadata"],"station_boundary":"Both App constructors initialize FileRetainedRequestStore before view models, passing `callcenter` and `cashier` respectively; DirectoryFor includes station under LocalApplicationData/Falah/pending-operations, so the implementations are shared in behavior but isolate files by station.","disposition":"No bug or divergence. This is duplicated infrastructure code with an intentional station argument. Consolidation is optional architecture work; if considered later, preserve station-scoped paths and error callback/test behavior."},"source_references":source_refs,"counterevidence":["The helper accepts arbitrary owner keys; PathFor sanitizes invalid filename characters but no cross-owner authorization is in this helper. Callers supply owner scoping separately.","A whole-file source equality result does not prove filesystem ACLs, crash durability beyond documented move semantics, or runtime recovery behavior."],"recommendation":"No code change warranted by this comparison. If these copies are later consolidated, keep station as an explicit required partition, preserve the same save-before-send and recovery contracts, and add parity tests for atomic replacement, malformed/missing types, serialization errors, and station isolation.","tests":{"executed":"No tests or runtime behavior executed; normalized source comparison and saved symbol/hash metadata only.","needed":["Existing store unit tests should run against both station configurations.","Verify each App supplies a distinct station root and that owner-key records from one station cannot be listed/loaded by the other."]},"new_financial_callable_review_credit":0}
write(REL,artifact)
regp="Deep-Audit/Duplication-and-Architecture/semantic-comparison-registry.json"; reg=read(regp)
group=next((x for x in reg["semantic_groups"] if x["id"]==artifact["id"]),None)
entry={"id":artifact["id"],"decision":artifact["decision"],"artifact":REL}
if group is None: reg["semantic_groups"].append(entry)
else: group.update(entry)
inp=next((x for x in reg["inputs"] if x["path"]==REL),None); inpdata={"path":REL,"sha256":hashlib.sha256((WORK/REL).read_bytes()).hexdigest(),"source_references":len(source_refs)}
if inp is None: reg["inputs"].append(inpdata)
else: inp.update(inpdata)
paths={r["path"] for i in reg["inputs"] for r in read(i["path"]).get("source_references",read(i["path"]).get("sources",[]))}
ids={s["stable_id"] for i in reg["inputs"] for r in read(i["path"]).get("source_references",read(i["path"]).get("sources",[])) for s in r.get("relevant_symbols",[])}
reg.update({"at_utc":now,"referenced_source_paths":len(paths),"referenced_callable_context_ids":len(ids),"source_reference_records":sum(len(read(i["path"]).get("source_references",read(i["path"]).get("sources",[]))) for i in reg["inputs"]),"explicit_ranges":sum(len(r.get("line_ranges",r.get("ranges",[]))) for i in reg["inputs"] for r in read(i["path"]).get("source_references",read(i["path"]).get("sources",[])))})
write(regp,reg)
reportp="Deep-Audit/CODE_DUPLICATION_AND_ARCHITECTURE.md"; report=(WORK/reportp).read_text(encoding="utf-8")
marker="## SEM-MIRROR-048 — Station-scoped retained request store implementations"
if marker not in report:
 with (WORK/reportp).open("a",encoding="utf-8") as f: f.write("\n\n"+marker+"\n\nThe Call Center and Cashier WinUI `FileRetainedRequestStore` files are identical after replacing only the file-scoped namespace. The comparison covered key sanitization, JSON envelope, temp-write/replace, error handling, clear, load/type recovery, and listing. Each app initializes the same helper with a different required station name (`callcenter` versus `cashier`), so the duplicate implementations preserve intentional local-data partitioning and showed no behavioral divergence. No consolidation is needed for correctness; if later centralized, preserve the explicit station partition and the tested persistence contract. Source evidence: [comparison artifact](Duplication-and-Architecture/callcenter-cashier-retained-store-semantic-comparison.json). No tests or filesystem data were touched.\n")
print(json.dumps({"id":artifact["id"],"groups":len(reg["semantic_groups"]),"inputs":len(reg["inputs"]),"paths":len(paths),"symbols":len(ids)}))
